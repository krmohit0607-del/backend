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

    /// <summary>True if every cell came from the live forecast/marine API
    /// (persisted cache counts, since it was live when first fetched);
    /// false if any cell is outside the live forecast window and had to
    /// fall back to historical/modelled data (see <see cref="HasData"/>) or
    /// couldn't be found at all.</summary>
    public bool Live { get; set; }

    /// <summary>True if every cell has real sampled data — either live (see
    /// <see cref="Live"/>) or a historical/modelled fallback for dates
    /// outside the live forecast window; false only when a cell has no data
    /// whatsoever (both the live and historical fetch attempts failed).
    /// Callers that must not silently treat "no data" as calm weather
    /// (e.g. route optimization) should gate on this instead of
    /// <see cref="Live"/>.</summary>
    public bool HasData { get; set; }

    /// <summary>When the oldest contributing tile was fetched from the upstream forecast API.</summary>
    public DateTime? FetchedAtUtc { get; set; }
}
