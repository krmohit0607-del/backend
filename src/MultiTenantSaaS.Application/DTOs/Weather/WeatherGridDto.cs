namespace MultiTenantSaaS.Application.DTOs.Weather;

/// <summary>
/// One forecast-hour grid of weather samples over a lat/lon bounding box —
/// mirrors the frontend's `WeatherGrid` shape exactly (see
/// `src/data/weather/types.ts`) so the client can use it as a drop-in
/// `WeatherProvider` data source.
/// </summary>
public class WeatherGridDto
{
    public double South { get; set; }
    public double West { get; set; }
    public double North { get; set; }
    public double East { get; set; }
    public int Cols { get; set; }
    public int Rows { get; set; }

    /// <summary>Magnitude in the factor's display unit, row-major (row * cols + col).</summary>
    public double[] Mag { get; set; } = System.Array.Empty<double>();

    /// <summary>Compass bearing (deg), row-major, 0 for non-directional factors.</summary>
    public double[] Dir { get; set; } = System.Array.Empty<double>();

    /// <summary>ISO timestamp (UTC) this grid represents.</summary>
    public string? Time { get; set; }

    /// <summary>True if every cell came from the persisted cache/upstream
    /// API; false if any cell had to fall back to the synthetic model
    /// (e.g. date too far in the past/future, or upstream fetch failed).</summary>
    public bool Live { get; set; }
}
