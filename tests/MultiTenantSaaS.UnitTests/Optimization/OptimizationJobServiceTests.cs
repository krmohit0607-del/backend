using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities.Optimization;
using MultiTenantSaaS.Infrastructure.Identity;
using MultiTenantSaaS.Infrastructure.Persistence;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class OptimizationJobServiceTests
{
    private sealed class FakeEngine : IRouteOptimizationEngine
    {
        private readonly IApplicationDbContext _db;
        public FakeEngine(IApplicationDbContext db) => _db = db;

        public static Guid? SeenTenant;
        public static TaskCompletionSource Gate = new();

        public Task<OptimizationRunResultDto> RunAsync(OptimizationRequestDto request, CancellationToken cancellationToken = default) =>
            RunAsync(request, null, cancellationToken);

        public async Task<OptimizationRunResultDto> RunAsync(OptimizationRequestDto request, IProgress<OptimizationProgress>? progress, CancellationToken cancellationToken = default)
        {
            SeenTenant = AmbientUser.Value?.TenantId;
            progress?.Report(new OptimizationProgress(40, "Halfway"));
            if (request.VoyageId == "FAIL") throw new InvalidOperationException("boom");
            if (request.VoyageId == "WAIT") await Gate.Task.WaitAsync(cancellationToken);

            var run = new OptimizationRun { TenantId = AmbientUser.Value?.TenantId, VoyageId = request.VoyageId };
            run.ScenarioResults.Add(new OptimizationScenarioResult { Scenario = "maintain-cp", IsFeasible = true });
            _db.OptimizationRuns.Add(run);
            await _db.SaveChangesAsync(cancellationToken);
            return new OptimizationRunResultDto { OptimizationRunId = run.Id };
        }
    }

    private static (OptimizationJobService Service, IServiceProvider Provider) Setup()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IRouteOptimizationEngine, FakeEngine>();
        var provider = services.BuildServiceProvider();
        return (new OptimizationJobService(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<OptimizationJobService>.Instance), provider);
    }

    private static async Task<OptimizationJobStatusDto> Settle(OptimizationJobService service, Guid id, Guid? tenant)
    {
        for (var i = 0; i < 200; i++)
        {
            var status = (await service.GetAsync(id, tenant))!;
            if (status.Status is "Completed" or "Failed" or "Cancelled") return status;
            await Task.Delay(25);
        }
        throw new TimeoutException("Job did not finish.");
    }

    [Fact]
    public async Task CompletedJobCarriesTheStoredResultAndRunsAsTheStartingTenant()
    {
        var tenant = Guid.NewGuid();
        var (service, _) = Setup();
        var id = await service.StartAsync(new OptimizationRequestDto { VoyageId = "OK" }, tenant, Guid.NewGuid());

        var status = await Settle(service, id, tenant);

        status.Status.Should().Be("Completed");
        status.Percent.Should().Be(100);
        status.Result!.Scenarios.Should().ContainSingle();
        FakeEngine.SeenTenant.Should().Be(tenant);
    }

    [Fact]
    public async Task EngineFailuresAreReportedOnTheJob()
    {
        var (service, _) = Setup();
        var id = await service.StartAsync(new OptimizationRequestDto { VoyageId = "FAIL" }, null, null);

        var status = await Settle(service, id, null);

        status.Status.Should().Be("Failed");
        status.Error.Should().Be("boom");
    }

    [Fact]
    public async Task CancellingStopsARunningJob()
    {
        FakeEngine.Gate = new TaskCompletionSource();
        var (service, _) = Setup();
        var id = await service.StartAsync(new OptimizationRequestDto { VoyageId = "WAIT" }, null, null);

        (await service.CancelAsync(id, null)).Should().BeTrue();
        var status = await Settle(service, id, null);

        status.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task JobsAreInvisibleToOtherTenants()
    {
        FakeEngine.Gate = new TaskCompletionSource();
        var (service, _) = Setup();
        var id = await service.StartAsync(new OptimizationRequestDto { VoyageId = "WAIT" }, Guid.NewGuid(), null);

        (await service.GetAsync(id, Guid.NewGuid())).Should().BeNull();
        (await service.CancelAsync(id, Guid.NewGuid())).Should().BeFalse();
        FakeEngine.Gate.SetResult();
    }

    [Fact]
    public async Task JobsInterruptedByARestartAreResumedFromTheirStoredRequest()
    {
        var tenant = Guid.NewGuid();
        var (service, provider) = Setup();
        Guid jobId;
        using (AmbientUser.Use(tenant, null))
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var job = new OptimizationJob
            {
                TenantId = tenant, Status = "Running", Attempts = 1,
                RequestJson = JsonSerializer.Serialize(new OptimizationRequestDto { VoyageId = "OK" }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            };
            db.OptimizationJobs.Add(job);
            await db.SaveChangesAsync();
            jobId = job.Id;
        }

        await service.RecoverAsync();
        var status = await Settle(service, jobId, tenant);

        status.Status.Should().Be("Completed");
        status.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task JobsInterruptedTooManyTimesAreFailedInsteadOfLooping()
    {
        var (service, provider) = Setup();
        Guid jobId;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var job = new OptimizationJob { Status = "Running", Attempts = 3, RequestJson = "{}" };
            db.OptimizationJobs.Add(job);
            await db.SaveChangesAsync();
            jobId = job.Id;
        }

        await service.RecoverAsync();

        (await service.GetAsync(jobId, null))!.Status.Should().Be("Failed");
    }
}
