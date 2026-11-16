using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Optimization;

/// <summary>
/// One invocation of the route-optimization engine for a voyage — the
/// request parameters (market factors, ETD, requested scenarios) plus the
/// resulting <see cref="OptimizationScenarioResult"/> rows (one per
/// requested scenario). This is what backs the "All Routes" history and
/// lets a comparison/view link re-open the exact route + weather + costs
/// that were computed at run time, rather than recomputing with today's
/// weather.
/// </summary>
public class OptimizationRun : BaseEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string VoyageId { get; set; } = string.Empty;
    public string? VesselImo { get; set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime EtdUtc { get; set; }

    public double OriginLat { get; set; }
    public double OriginLng { get; set; }
    public double DestLat { get; set; }
    public double DestLng { get; set; }

    /// <summary>"Ballast" or "Laden" — selects which telegraph/blower/safety columns on the vessel apply.</summary>
    public string LoadCondition { get; set; } = "Laden";

    public double HireRatePerDayUsd { get; set; }
    public double FoPriceUsdPerMt { get; set; }
    public double GoPriceUsdPerMt { get; set; }
    public double EuaPriceUsdPerTonneCo2 { get; set; }

    /// <summary>JSON array of requested scenario keys, e.g. ["LowestVoyageCost","EarliestEta"].</summary>
    public string ScenariosRequestedJson { get; set; } = "[]";

    /// <summary>JSON array of the restricted/ECA/no-go/speed-control areas used for this run (locked at run time).</summary>
    public string RestrictedAreasJson { get; set; } = "[]";

    /// <summary>Full request snapshot: route geometry, operating profiles, scenario constraints, and market assumptions.</summary>
    public string InputSnapshotJson { get; set; } = "{}";

    public string Status { get; set; } = "Completed"; // Pending, Running, Completed, Failed
    public string? ErrorMessage { get; set; }

    public virtual ICollection<OptimizationScenarioResult> ScenarioResults { get; set; } = new List<OptimizationScenarioResult>();
}
