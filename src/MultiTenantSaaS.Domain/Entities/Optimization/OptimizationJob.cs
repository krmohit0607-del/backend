using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Optimization;

/// <summary>A queued or running optimization request, stored so jobs survive an API restart (they are re-run from <see cref="RequestJson"/>).</summary>
public class OptimizationJob : BaseEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }

    /// <summary>Queued | Running | Completed | Failed | Cancelled</summary>
    public string Status { get; set; } = "Queued";
    public int Percent { get; set; }
    public string Stage { get; set; } = "Queued";
    public string? Error { get; set; }
    public string RequestJson { get; set; } = "{}";
    public Guid? RunId { get; set; }
    public int Attempts { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
}
