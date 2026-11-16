using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Optimization;

/// <summary>
/// A re-optimization suggestion raised when the vessel's reported position, time or bunkers drift away from a stored run's prediction.
/// It never replaces a route: an operator confirms (then runs a new optimization) or dismisses it, and the original run stays in history.
/// </summary>
public class OptimizationRecommendation : BaseEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid OptimizationRunId { get; set; }
    public string Scenario { get; set; } = string.Empty;
    public string VoyageId { get; set; } = string.Empty;

    /// <summary>Pending | Confirmed | Dismissed</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>JSON array of human-readable reasons.</summary>
    public string ReasonsJson { get; set; } = "[]";

    /// <summary>JSON snapshot of the measured deviations that triggered it.</summary>
    public string MetricsJson { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastEvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
}
