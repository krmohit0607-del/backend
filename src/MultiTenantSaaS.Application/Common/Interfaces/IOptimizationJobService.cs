using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.Application.Common.Interfaces;

/// <summary>Runs optimization requests in the background so the client can poll progress and cancel. Jobs are stored in the database, visible only to the tenant that started them, and resumed after an API restart; the finished run itself is persisted by the engine.</summary>
public interface IOptimizationJobService
{
    Task<Guid> StartAsync(OptimizationRequestDto request, Guid? tenantId, Guid? userId);

    Task<OptimizationJobStatusDto?> GetAsync(Guid jobId, Guid? tenantId);

    /// <summary>Requests cancellation. Returns false when the job does not exist for the tenant.</summary>
    Task<bool> CancelAsync(Guid jobId, Guid? tenantId);

    /// <summary>Re-runs jobs that were queued or running when the API stopped.</summary>
    Task RecoverAsync(CancellationToken cancellationToken = default);
}
