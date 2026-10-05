using System.Text.Json;

namespace MultiTenantSaaS.Application.DTOs.Performance;

/// <summary>
/// The report body is accepted/returned as opaque JSON (<see cref="JsonElement"/>)
/// so every field of the frontend's `PerformanceReport` shape round-trips
/// exactly without the backend needing its own copy of that (large, nested)
/// shape — it only needs to know the few fields it indexes by.
/// </summary>
public class SavePerformanceReportRequestDto
{
    public string VoyageId { get; set; } = string.Empty;
    public string VesselImo { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public JsonElement Report { get; set; }
}

public class PerformanceReportResponseDto
{
    public string VoyageId { get; set; } = string.Empty;
    public string VesselImo { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public JsonElement Report { get; set; }
    public DateTime UpdatedAt { get; set; }
}
