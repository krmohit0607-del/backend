using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities.Optimization;
using MultiTenantSaaS.Infrastructure.Identity;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>
/// Background runner for optimization requests. Every job is stored in the database, so status and results survive an API restart and
/// interrupted jobs are re-run from their stored request on the next start (the engine is deterministic for a given request and forecast).
/// Live percent/stage and cancellation tokens are kept in memory only.
/// </summary>
public sealed class OptimizationJobService : IOptimizationJobService
{
    private const int MaxConcurrentJobs = 2;
    private const int MaxAttempts = 3;
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed class Live
    {
        public CancellationTokenSource Cancellation { get; } = new();
        public int Percent;
        public string Stage = "Queued";
    }

    private readonly ConcurrentDictionary<Guid, Live> _live = new();
    private readonly SemaphoreSlim _slots = new(MaxConcurrentJobs);
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OptimizationJobService> _logger;

    public OptimizationJobService(IServiceScopeFactory scopes, ILogger<OptimizationJobService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public async Task<Guid> StartAsync(OptimizationRequestDto request, Guid? tenantId, Guid? userId)
    {
        var job = new OptimizationJob { TenantId = tenantId, UserId = userId, RequestJson = JsonSerializer.Serialize(request, Json) };
        using (AmbientUser.Use(tenantId, userId))
        using (var scope = _scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            // Precompute the cutoff instead of `DateTime.UtcNow - Retention` inline —
            // combined with the tenant global query filter, EF Core cannot translate
            // that arithmetic and throws at every call (every job start attempt).
            var expiryCutoffUtc = DateTime.UtcNow - Retention;
            var expired = await db.OptimizationJobs.Where(j => j.FinishedAtUtc != null && j.FinishedAtUtc < expiryCutoffUtc).Take(200).ToListAsync();
            db.OptimizationJobs.RemoveRange(expired);
            db.OptimizationJobs.Add(job);
            await db.SaveChangesAsync();
        }

        Launch(job.Id, tenantId, userId, request);
        return job.Id;
    }

    public async Task<OptimizationJobStatusDto?> GetAsync(Guid jobId, Guid? tenantId)
    {
        using var identity = AmbientUser.Use(tenantId, null);
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var job = await db.OptimizationJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null || job.TenantId != tenantId) return null;

        var status = new OptimizationJobStatusDto { JobId = job.Id, Status = job.Status, Percent = job.Percent, Stage = job.Stage, Error = job.Error };
        if (job.Status == "Running" && _live.TryGetValue(job.Id, out var live))
        {
            status.Percent = Math.Clamp(live.Percent, 0, 99);
            status.Stage = live.Stage;
        }
        if (job.Status == "Completed" && job.RunId.HasValue)
        {
            var run = await db.OptimizationRuns.AsNoTracking().Include(r => r.ScenarioResults).FirstOrDefaultAsync(r => r.Id == job.RunId.Value);
            if (run != null) status.Result = OptimizationRunMapper.ToDto(run);
        }
        return status;
    }

    public async Task<bool> CancelAsync(Guid jobId, Guid? tenantId)
    {
        using var identity = AmbientUser.Use(tenantId, null);
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var job = await db.OptimizationJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null || job.TenantId != tenantId) return false;

        if (_live.TryGetValue(jobId, out var live)) live.Cancellation.Cancel();
        else if (job.Status is "Queued" or "Running")
        {
            job.Status = "Cancelled";
            job.Stage = "Cancelled";
            job.FinishedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return true;
    }

    /// <summary>Re-runs jobs that were queued or running when the API stopped.</summary>
    public async Task RecoverAsync(CancellationToken cancellationToken = default)
    {
        List<OptimizationJob> pending;
        using (var scope = _scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            pending = await db.OptimizationJobs.IgnoreQueryFilters().Where(j => j.Status == "Queued" || j.Status == "Running").ToListAsync(cancellationToken);
            foreach (var job in pending.Where(j => j.Attempts >= MaxAttempts))
            {
                job.Status = "Failed";
                job.Stage = "Failed";
                job.Error = "The optimization was interrupted by repeated server restarts. Run it again.";
                job.FinishedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var job in pending.Where(j => j.Attempts < MaxAttempts && !_live.ContainsKey(j.Id)))
        {
            var request = Deserialize(job.RequestJson);
            if (request == null)
            {
                await UpdateAsync(job.Id, job.TenantId, j => Finish(j, "Failed", "The stored request could not be read.", null));
                continue;
            }
            _logger.LogInformation("Resuming optimization job {JobId} after restart.", job.Id);
            Launch(job.Id, job.TenantId, job.UserId, request);
        }
    }

    private void Launch(Guid jobId, Guid? tenantId, Guid? userId, OptimizationRequestDto request)
    {
        _live[jobId] = new Live();
        _ = Task.Run(() => ExecuteAsync(jobId, tenantId, userId, request));
    }

    private async Task ExecuteAsync(Guid jobId, Guid? tenantId, Guid? userId, OptimizationRequestDto request)
    {
        var live = _live[jobId];
        try
        {
            try
            {
                await _slots.WaitAsync(live.Cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                await UpdateAsync(jobId, tenantId, j => Finish(j, "Cancelled", "Cancelled before it started.", null));
                return;
            }

            try
            {
                await UpdateAsync(jobId, tenantId, j => { j.Status = "Running"; j.Stage = "Starting"; j.Attempts++; });
                using var identity = AmbientUser.Use(tenantId, userId);
                using var scope = _scopes.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IRouteOptimizationEngine>();
                var progress = new Progress<OptimizationProgress>(update =>
                {
                    live.Percent = update.Percent;
                    live.Stage = update.Stage;
                });
                var result = await engine.RunAsync(request, progress, live.Cancellation.Token);
                await UpdateAsync(jobId, tenantId, j => { Finish(j, "Completed", null, result.OptimizationRunId); j.Percent = 100; });
            }
            catch (OperationCanceledException)
            {
                await UpdateAsync(jobId, tenantId, j => Finish(j, "Cancelled", "Cancelled by the user.", null));
            }
            catch (InvalidOperationException ex)
            {
                await UpdateAsync(jobId, tenantId, j => Finish(j, "Failed", ex.Message, null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Optimization job {JobId} failed", jobId);
                await UpdateAsync(jobId, tenantId, j => Finish(j, "Failed", "The optimization failed unexpectedly. See the server log for details.", null));
            }
            finally
            {
                _slots.Release();
            }
        }
        finally
        {
            if (_live.TryRemove(jobId, out var removed)) removed.Cancellation.Dispose();
        }
    }

    private static void Finish(OptimizationJob job, string status, string? error, Guid? runId)
    {
        job.Status = status;
        job.Stage = status;
        job.Error = error;
        job.RunId = runId;
        job.FinishedAtUtc = DateTime.UtcNow;
    }

    private async Task UpdateAsync(Guid jobId, Guid? tenantId, Action<OptimizationJob> change)
    {
        try
        {
            using var identity = AmbientUser.Use(tenantId, null);
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var job = await db.OptimizationJobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null) return;
            change(job);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not update optimization job {JobId}", jobId);
        }
    }

    private static OptimizationRequestDto? Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<OptimizationRequestDto>(json, Json); }
        catch (JsonException) { return null; }
    }
}

/// <summary>Resumes interrupted optimization jobs shortly after the API starts.</summary>
public sealed class OptimizationJobRecoveryService : Microsoft.Extensions.Hosting.IHostedService
{
    private readonly IOptimizationJobService _jobs;
    private readonly ILogger<OptimizationJobRecoveryService> _logger;

    public OptimizationJobRecoveryService(IOptimizationJobService jobs, ILogger<OptimizationJobRecoveryService> logger)
    {
        _jobs = jobs;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                await _jobs.RecoverAsync(cancellationToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resume interrupted optimization jobs.");
            }
        }, cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
