using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.Application.Common.Interfaces;

/// <summary>Compares reported progress with a stored optimization run, raises operator-confirmed re-optimization suggestions, and compares actual with predicted performance. Never replaces a route or changes calibration automatically.</summary>
public interface IOptimizationMonitoringService
{
    Task<OptimizationCheckResultDto?> CheckRunAsync(Guid runId, OptimizationCheckRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Checks against the voyage's most recent run.</summary>
    Task<OptimizationCheckResultDto?> CheckVoyageAsync(string voyageId, OptimizationCheckRequestDto request, CancellationToken cancellationToken = default);

    Task<List<OptimizationRecommendationDto>> GetRecommendationsAsync(string voyageId, string? status, CancellationToken cancellationToken = default);

    /// <summary>Resolves a pending suggestion. Returns null when it does not exist or is no longer pending.</summary>
    Task<OptimizationRecommendationDto?> ResolveAsync(Guid recommendationId, bool confirm, CancellationToken cancellationToken = default);

    Task<PerformanceComparisonDto?> ComparePerformanceAsync(Guid runId, string? scenario, CancellationToken cancellationToken = default);
}
