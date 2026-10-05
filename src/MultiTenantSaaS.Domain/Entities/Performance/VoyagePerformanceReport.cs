using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Performance;

/// <summary>
/// The saved, editable end-of-voyage Voyage Performance Report (cover
/// details, voyage summary/totals, good-weather analysis, speed summary,
/// bunker analysis, abstract and detailed-analysis rows) — one per voyage.
/// Lives in the separate Performance service database, not the main tenant
/// database. The full nested report is stored as <see cref="ReportJson"/>
/// (the frontend's `PerformanceReport` shape, serialized verbatim) so every
/// field round-trips exactly with no lossy relational mapping; the plain
/// columns alongside it exist purely so the row can be found/listed without
/// deserializing the blob.
/// </summary>
public class VoyagePerformanceReport : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    /// <summary>Main-database voyage id this report belongs to (unique per tenant).</summary>
    public string VoyageId { get; set; } = string.Empty;
    public string VesselImo { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;

    /// <summary>The full `PerformanceReport` object (meta/summary/totals/goodWeather/speed/vlsfo/lsmgo/abstract/detailed), as JSON.</summary>
    public string ReportJson { get; set; } = string.Empty;

    public string? UpdatedByEmail { get; set; }
}
