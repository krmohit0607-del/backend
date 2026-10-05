using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Performance;

/// <summary>
/// One noon/event row of a voyage's tracksheet (fuel ROB, distances, speed,
/// engine and weather observations) — lives in the separate Performance
/// service database (<c>PerformanceDbContext</c>), not the main tenant
/// database, so this module can be offered/scaled independently while still
/// correlating back to a tenant/voyage by plain id (no cross-database FK).
/// Mirrors the frontend `TrackRow` type field-for-field.
/// </summary>
public class TracksheetRow : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    /// <summary>Main-database voyage id this row belongs to.</summary>
    public string VoyageId { get; set; } = string.Empty;
    public string VesselImo { get; set; } = string.Empty;

    /// <summary>Preserves the grid's row order (rows can be inserted/removed anywhere).</summary>
    public int SortOrder { get; set; }

    public string NextPort { get; set; } = string.Empty;
    public string Rt { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public double? Hrs { get; set; }
    public string Lat { get; set; } = string.Empty;
    public string Lng { get; set; } = string.Empty;

    public double? VlsfoRob { get; set; }
    public double? VlsfoBunkered { get; set; }
    public double? VlsfoCorrected { get; set; }
    public double? LsmgoRob { get; set; }
    public double? LsmgoBunkered { get; set; }
    public double? LsmgoCorrected { get; set; }
    public double? NoneRob { get; set; }
    public double? NoneBunkered { get; set; }
    public double? NoneCorrected { get; set; }

    public double? DistR { get; set; }
    public double? DistO { get; set; }
    public double? DtgO { get; set; }
    public double? AvgSpeedO { get; set; }

    public double? Rpm { get; set; }
    public double? EnginePower { get; set; }
    public double? Slip { get; set; }
    public double? Course { get; set; }
    public double? Amount { get; set; }

    public string WindO { get; set; } = string.Empty;
    public string WavesO { get; set; } = string.Empty;
    public double WindF { get; set; }
    public double WaveF { get; set; }
    public double CurrF { get; set; }
    public string AvgF { get; set; } = string.Empty;
}
