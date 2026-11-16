using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Optimization;

/// <summary>Tenant's EU ETS charging rule for optimization costs: coverage by voyage type and the phase-in share per emission year.</summary>
public class EtsPolicy : BaseEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    /// <summary>Share of CO2 covered on voyages between two EU/EEA ports.</summary>
    public double IntraEuCoveragePct { get; set; } = 100;

    /// <summary>Share covered on voyages with one EU/EEA end.</summary>
    public double ExtraEuCoveragePct { get; set; } = 50;

    /// <summary>Share applied when the voyage ends cannot be matched to a known port country.</summary>
    public double UnknownCoveragePct { get; set; } = 100;

    /// <summary>JSON array [{year, pct}]: share of covered emissions surrendered for each emission year; later years keep the last value.</summary>
    public string PhaseInJson { get; set; } = "[{\"year\":2024,\"pct\":40},{\"year\":2025,\"pct\":70},{\"year\":2026,\"pct\":100}]";

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}
