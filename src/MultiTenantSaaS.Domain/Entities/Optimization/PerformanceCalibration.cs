using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Optimization;

/// <summary>
/// A fitted correction between a vessel's configured (CP) consumption and what its weather-corrected noon reports show. It is only a proposal until an
/// operator approves it; the optimizer applies the latest approved one to configured values and never rewrites the curves themselves.
/// </summary>
public class PerformanceCalibration : BaseEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string VesselImo { get; set; } = string.Empty;
    public string LoadCondition { get; set; } = "Laden";
    public string ModelVersion { get; set; } = "3";

    public DateTime FittedAtUtc { get; set; } = DateTime.UtcNow;
    public int SampleCount { get; set; }
    public int UsedSampleCount { get; set; }
    public DateTime? WindowFromUtc { get; set; }
    public DateTime? WindowToUtc { get; set; }

    /// <summary>Median of observed main-engine fuel divided by the CP curve's fuel at the observed calm speed; applied to configured fuel and power.</summary>
    public double FuelFactor { get; set; } = 1;
    public double SpreadPct { get; set; }
    public double? TrendPctPerMonth { get; set; }

    /// <summary>Good | Limited | Insufficient</summary>
    public string Quality { get; set; } = "Insufficient";

    /// <summary>Proposed | Approved | Rejected | Superseded</summary>
    public string Status { get; set; } = "Proposed";

    public string NotesJson { get; set; } = "[]";
    public string MonthlyJson { get; set; } = "[]";
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }
}
