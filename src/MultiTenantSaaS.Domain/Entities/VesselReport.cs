using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class VesselReport : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string ReportNo { get; set; } = string.Empty; // e.g., VR-2609-001
    public string ReportType { get; set; } = "Noon"; // Noon, Departure, Arrival, BOSP, EOSP, Shifting, Bunkering
    public string? ReportSubtype { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string Imo { get; set; } = string.Empty;
    public string? VoyageCode { get; set; }
    public DateTime ReportDateTime { get; set; } = DateTime.UtcNow;

    // Geographic Position
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? CurrentPort { get; set; }
    public string? NextPort { get; set; }
    public string? EtaNextPort { get; set; }

    // Navigation & Speed Metrics
    public double? SteamingHours { get; set; }
    public double? DistanceObserved { get; set; }
    public double? DistanceEngine { get; set; }
    public double? SpeedObserved { get; set; }
    public double? SpeedEngine { get; set; }
    public double? SlipPercent { get; set; }
    public double? Course { get; set; }

    // Weather & Sea Telemetry
    public string? WindDirection { get; set; }
    public double? WindForce { get; set; }
    public string? SeaState { get; set; }
    public string? Swell { get; set; }
    public double? Barometer { get; set; }
    public double? AirTemp { get; set; }
    public double? SeaTemp { get; set; }

    // Fuel Consumption & ROB (MT)
    public double? VlsfoCons { get; set; }
    public double? VlsfoRob { get; set; }
    public double? LsmgoCons { get; set; }
    public double? LsmgoRob { get; set; }
    public double? HfoCons { get; set; }
    public double? HfoRob { get; set; }
    public double? MgoCons { get; set; }
    public double? MgoRob { get; set; }

    // Machinery & Cargo
    public double? Rpm { get; set; }
    public double? EngineKw { get; set; }
    public string? DraftFwd { get; set; }
    public string? DraftAft { get; set; }
    public string? Remarks { get; set; }
    public string? FormValuesJson { get; set; }
    public string? FormattedReportText { get; set; }
    public string Status { get; set; } = "Verified";

    // Navigation property
    public virtual Tenant? Tenant { get; set; }
}
