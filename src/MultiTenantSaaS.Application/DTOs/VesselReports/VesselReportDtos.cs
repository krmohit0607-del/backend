namespace MultiTenantSaaS.Application.DTOs.VesselReports;

public class VesselReportDto
{
    public Guid Id { get; set; }
    public string ReportNo { get; set; } = string.Empty;
    public string ReportType { get; set; } = "Noon";
    public string? ReportSubtype { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string Imo { get; set; } = string.Empty;
    public string? VoyageCode { get; set; }
    public DateTime ReportDateTime { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? CurrentPort { get; set; }
    public string? NextPort { get; set; }
    public string? EtaNextPort { get; set; }
    public double? SteamingHours { get; set; }
    public double? DistanceObserved { get; set; }
    public double? DistanceEngine { get; set; }
    public double? SpeedObserved { get; set; }
    public double? SpeedEngine { get; set; }
    public double? SlipPercent { get; set; }
    public double? Course { get; set; }
    public string? WindDirection { get; set; }
    public double? WindForce { get; set; }
    public string? SeaState { get; set; }
    public string? Swell { get; set; }
    public double? Barometer { get; set; }
    public double? AirTemp { get; set; }
    public double? SeaTemp { get; set; }
    public double? VlsfoCons { get; set; }
    public double? VlsfoRob { get; set; }
    public double? LsmgoCons { get; set; }
    public double? LsmgoRob { get; set; }
    public double? HfoCons { get; set; }
    public double? HfoRob { get; set; }
    public double? MgoCons { get; set; }
    public double? MgoRob { get; set; }
    public double? Rpm { get; set; }
    public double? EngineKw { get; set; }
    public string? DraftFwd { get; set; }
    public string? DraftAft { get; set; }
    public string? Remarks { get; set; }
    public string? FormValuesJson { get; set; }
    public string? FormattedReportText { get; set; }
    public string Status { get; set; } = "Verified";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Actual voyage progress derived from the latest vessel report — used to mark the
/// vessel's current position on the voyage timeline.
/// </summary>
public class VoyageProgressDto
{
    public string? VoyageCode { get; set; }
    public string? Imo { get; set; }
    public string? VesselName { get; set; }
    public bool HasReports { get; set; }
    public string? LatestReportNo { get; set; }
    public string? ReportType { get; set; }
    public DateTime? ReportDateTime { get; set; }
    public string? CurrentPort { get; set; }
    public string? NextPort { get; set; }
    public string? EtaNextPort { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    /// <summary>True when the latest report is a departure / at-sea report (vessel underway).</summary>
    public bool AtSea { get; set; }
    /// <summary>The port the vessel is currently at (in-port) or heading to (at sea).</summary>
    public string? Stage { get; set; }
}


public class SubmitVesselReportRequestDto
{
    public string? ReportNo { get; set; }
    public string ReportType { get; set; } = "Noon";
    public string? ReportSubtype { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string Imo { get; set; } = string.Empty;
    public string? VoyageCode { get; set; }
    public DateTime? ReportDateTime { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? CurrentPort { get; set; }
    public string? NextPort { get; set; }
    public string? EtaNextPort { get; set; }
    public double? SteamingHours { get; set; }
    public double? DistanceObserved { get; set; }
    public double? DistanceEngine { get; set; }
    public double? SpeedObserved { get; set; }
    public double? SpeedEngine { get; set; }
    public double? SlipPercent { get; set; }
    public double? Course { get; set; }
    public string? WindDirection { get; set; }
    public double? WindForce { get; set; }
    public string? SeaState { get; set; }
    public string? Swell { get; set; }
    public double? Barometer { get; set; }
    public double? AirTemp { get; set; }
    public double? SeaTemp { get; set; }
    public double? VlsfoCons { get; set; }
    public double? VlsfoRob { get; set; }
    public double? LsmgoCons { get; set; }
    public double? LsmgoRob { get; set; }
    public double? HfoCons { get; set; }
    public double? HfoRob { get; set; }
    public double? MgoCons { get; set; }
    public double? MgoRob { get; set; }
    public double? Rpm { get; set; }
    public double? EngineKw { get; set; }
    public string? DraftFwd { get; set; }
    public string? DraftAft { get; set; }
    public string? Remarks { get; set; }
    public string? FormValuesJson { get; set; }
    public string? FormattedReportText { get; set; }
    public string Status { get; set; } = "Verified";
}
