namespace MultiTenantSaaS.Application.DTOs.Cyclones;

/// <summary>
/// A single active tropical cyclone/typhoon/depression, normalised from
/// either NOAA's National Hurricane Center or the Joint Typhoon Warning
/// Center so the map can render both sources the same way.
/// </summary>
public class CycloneDto
{
    /// <summary>Source-native identifier (e.g. NOAA "ep182026" or JTWC "26W").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>"NOAA" or "JTWC".</summary>
    public string Source { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Raw classification code (e.g. "HU", "TS", "TD", "TY", "STY").</summary>
    public string? Classification { get; set; }

    /// <summary>Human-readable classification (e.g. "Hurricane", "Typhoon").</summary>
    public string? ClassificationLabel { get; set; }

    /// <summary>Basin label (e.g. "Eastern Pacific", "Northwest Pacific").</summary>
    public string? Basin { get; set; }

    public string? WarningNumber { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double? MaxSustainedWindKt { get; set; }
    public double? PressureMb { get; set; }
    public double? MovementDirectionDeg { get; set; }
    public double? MovementSpeedKt { get; set; }

    public DateTime? LastUpdateUtc { get; set; }

    /// <summary>Link to the official text advisory/warning.</summary>
    public string? AdvisoryUrl { get; set; }

    /// <summary>Link to the official forecast track/cone graphic (page or image).</summary>
    public string? GraphicUrl { get; set; }

    /// <summary>Direct image URL (satellite or warning graphic), when the source exposes one.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Ordered track points (current position first, then forecast positions).</summary>
    public List<CycloneLatLngDto> Track { get; set; } = new();

    /// <summary>Forecast cone-of-uncertainty polygon ring, when the source provides one (NOAA only).</summary>
    public List<CycloneLatLngDto> ConePolygon { get; set; } = new();
}

public class CycloneLatLngDto
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}

