using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Cyclones;

namespace MultiTenantSaaS.Infrastructure.Services;

/// <summary>
/// Fetches active tropical cyclones from two public government sources:
///   - NOAA National Hurricane Center — clean JSON feed (Atlantic / Eastern
///     &amp; Central Pacific).
///   - Joint Typhoon Warning Center (JTWC) — no public API; best-effort
///     HTML + fixed-format text bulletin parsing (Western Pacific / North
///     Indian Ocean / Southern Hemisphere). A single storm failing to parse
///     is skipped rather than failing the whole request.
///
/// Results are cached briefly in-memory — these feeds update every few
/// hours at most, polling on every map redraw would be wasteful and rude to
/// a public government server.
/// </summary>
public class CycloneDataService : ICycloneDataService
{
    private const string NoaaUrl = "https://www.nhc.noaa.gov/CurrentStorms.json";
    private const string JtwcUrl = "https://www.metoc.navy.mil/jtwc/jtwc.html";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private static readonly Dictionary<string, string> NoaaClassificationLabels = new()
    {
        ["TD"] = "Tropical Depression",
        ["STD"] = "Subtropical Depression",
        ["TS"] = "Tropical Storm",
        ["STS"] = "Subtropical Storm",
        ["HU"] = "Hurricane",
        ["EX"] = "Extratropical Cyclone",
        ["PTC"] = "Post-Tropical Cyclone",
    };

    private static readonly Dictionary<string, string> JtwcBasinLabels = new()
    {
        ["wp"] = "Northwest Pacific",
        ["ep"] = "Eastern Pacific",
        ["cp"] = "Central Pacific",
        ["io"] = "North Indian Ocean",
        ["sh"] = "Southern Hemisphere",
    };

    private static readonly Dictionary<string, string> JtwcClassificationCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tropical Depression"] = "TD",
        ["Tropical Storm"] = "TS",
        ["Typhoon"] = "TY",
        ["Super Typhoon"] = "STY",
        ["Hurricane"] = "HU",
        ["Cyclone"] = "TC",
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CycloneDataService> _logger;

    private readonly object _cacheLock = new();
    private List<CycloneDto>? _cached;
    private DateTime _cachedAtUtc;

    public CycloneDataService(IHttpClientFactory httpClientFactory, ILogger<CycloneDataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<CycloneDto>> GetActiveCyclonesAsync(CancellationToken cancellationToken = default)
    {
        lock (_cacheLock)
        {
            if (_cached != null && DateTime.UtcNow - _cachedAtUtc < CacheDuration)
                return _cached;
        }

        var client = _httpClientFactory.CreateClient("cyclones");
        client.Timeout = TimeSpan.FromSeconds(15);
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            client.DefaultRequestHeaders.Add("User-Agent", "FleetViewWeatherOverlay/1.0 (+shipping route planning)");

        var results = new List<CycloneDto>();

        try
        {
            results.AddRange(await FetchNoaaAsync(client, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch NOAA active storms feed");
        }

        try
        {
            results.AddRange(await FetchJtwcAsync(client, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch JTWC active warnings page");
        }

        lock (_cacheLock)
        {
            _cached = results;
            _cachedAtUtc = DateTime.UtcNow;
        }
        return results;
    }

    // --- NOAA National Hurricane Center ---------------------------------

    private sealed class NoaaFeed
    {
        [JsonPropertyName("activeStorms")]
        public List<NoaaStorm>? ActiveStorms { get; set; }
    }

    private sealed class NoaaStorm
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Classification { get; set; }
        public string? Intensity { get; set; }
        public string? Pressure { get; set; }
        public double? LatitudeNumeric { get; set; }
        public double? LongitudeNumeric { get; set; }
        public double? MovementDir { get; set; }
        public double? MovementSpeed { get; set; }
        public DateTime? LastUpdate { get; set; }
        public NoaaLink? PublicAdvisory { get; set; }
        public NoaaLink? ForecastGraphics { get; set; }
        public NoaaKmzLink? ForecastTrack { get; set; }
        public NoaaKmzLink? TrackCone { get; set; }
    }

    private sealed class NoaaLink
    {
        public string? Url { get; set; }
    }

    private sealed class NoaaKmzLink
    {
        public string? KmzFile { get; set; }
    }

    private async Task<List<CycloneDto>> FetchNoaaAsync(HttpClient client, CancellationToken ct)
    {
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var feed = await client.GetFromJsonAsync<NoaaFeed>(NoaaUrl, options, ct);
        var storms = feed?.ActiveStorms ?? new List<NoaaStorm>();

        var cyclones = new List<CycloneDto>();
        foreach (var s in storms)
        {
            if (!s.LatitudeNumeric.HasValue || !s.LongitudeNumeric.HasValue) continue;

            var cyclone = new CycloneDto
            {
                Id = s.Id ?? Guid.NewGuid().ToString("N"),
                Source = "NOAA",
                Name = s.Name ?? "Unnamed",
                Classification = s.Classification,
                ClassificationLabel = s.Classification != null && NoaaClassificationLabels.TryGetValue(s.Classification, out var label)
                    ? label
                    : s.Classification,
                Basin = null,
                Latitude = s.LatitudeNumeric!.Value,
                Longitude = s.LongitudeNumeric!.Value,
                MaxSustainedWindKt = ParseDouble(s.Intensity),
                PressureMb = ParseDouble(s.Pressure),
                MovementDirectionDeg = s.MovementDir,
                MovementSpeedKt = s.MovementSpeed,
                LastUpdateUtc = s.LastUpdate,
                AdvisoryUrl = s.PublicAdvisory?.Url,
                GraphicUrl = s.ForecastGraphics?.Url,
                ImageUrl = null, // NOAA's current-conditions graphic is an HTML page, not a raw image
            };

            if (s.ForecastTrack?.KmzFile != null)
            {
                try
                {
                    var kml = ExtractKmlFromKmz(await client.GetByteArrayAsync(s.ForecastTrack.KmzFile, ct));
                    cyclone.Track = ParseKmlPoints(kml);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not fetch/parse NOAA forecast track for {Id}", cyclone.Id);
                }
            }
            if (cyclone.Track.Count == 0)
            {
                // Always have at least the current position so the map can place something.
                cyclone.Track.Add(new CycloneLatLngDto { Lat = cyclone.Latitude, Lon = cyclone.Longitude });
            }

            if (s.TrackCone?.KmzFile != null)
            {
                try
                {
                    var kml = ExtractKmlFromKmz(await client.GetByteArrayAsync(s.TrackCone.KmzFile, ct));
                    cyclone.ConePolygon = ParseKmlPolygon(kml);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not fetch/parse NOAA forecast cone for {Id}", cyclone.Id);
                }
            }

            cyclones.Add(cyclone);
        }
        return cyclones;
    }

    private static double? ParseDouble(string? s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;

    // --- KMZ (zipped KML) helpers — NOAA publishes forecast track/cone as
    // KMZ, not GeoJSON. Best-effort: malformed/unexpected structure just
    // yields an empty list rather than throwing. ---

    private static string ExtractKmlFromKmz(byte[] kmzBytes)
    {
        using var ms = new MemoryStream(kmzBytes);
        using var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
        var entry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase));
        if (entry == null) return string.Empty;
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    /// <summary>Every &lt;Point&gt; placemark's coordinate, in document order — the forecast track points.</summary>
    private static List<CycloneLatLngDto> ParseKmlPoints(string kml)
    {
        var points = new List<CycloneLatLngDto>();
        if (string.IsNullOrWhiteSpace(kml)) return points;
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(kml);
            var ns = doc.Root?.Name.Namespace ?? string.Empty;
            foreach (var pointEl in doc.Descendants(ns + "Point"))
            {
                var coordText = pointEl.Element(ns + "coordinates")?.Value;
                var parsed = ParseCoordinateList(coordText);
                if (parsed.Count > 0) points.Add(parsed[0]);
            }
        }
        catch (Exception)
        {
            // Malformed/unexpected KML — return whatever was parsed so far (likely empty).
        }
        return points;
    }

    /// <summary>The first polygon's outer boundary ring — the cone of uncertainty.</summary>
    private static List<CycloneLatLngDto> ParseKmlPolygon(string kml)
    {
        if (string.IsNullOrWhiteSpace(kml)) return new List<CycloneLatLngDto>();
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(kml);
            var ns = doc.Root?.Name.Namespace ?? string.Empty;
            var coordsEl = doc.Descendants(ns + "outerBoundaryIs").FirstOrDefault()?.Descendants(ns + "coordinates").FirstOrDefault()
                ?? doc.Descendants(ns + "Polygon").FirstOrDefault()?.Descendants(ns + "coordinates").FirstOrDefault();
            return ParseCoordinateList(coordsEl?.Value);
        }
        catch (Exception)
        {
            return new List<CycloneLatLngDto>();
        }
    }

    /// <summary>KML `coordinates` text is whitespace-separated "lon,lat[,alt]" tuples.</summary>
    private static List<CycloneLatLngDto> ParseCoordinateList(string? text)
    {
        var result = new List<CycloneLatLngDto>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        foreach (var tuple in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = tuple.Split(',');
            if (parts.Length >= 2
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
            {
                result.Add(new CycloneLatLngDto { Lat = lat, Lon = lon });
            }
        }
        return result;
    }

    // --- Joint Typhoon Warning Center (best-effort HTML/text parsing) ---

    private static readonly Regex JtwcStormBlock = new(
        @"(Tropical\s+Depression|Tropical\s+Storm|Super\s+Typhoon|Typhoon|Hurricane|Cyclone)\s+(\d{2}[A-Z])\s*\(([^)]+)\)\s*Warning\s*#\s*(\d+)[\s\S]{0,400}?/jtwc/products/([a-z]{2}\d{4})web\.txt",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JtwcPosition = new(
        @"NEAR\s+(\d+\.?\d*)\s*([NS])\s+(\d+\.?\d*)\s*([EW])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Current position ("NEAR") and every forecast position ("HHMMSSZ --- lat lon")
    // share the same trailing "lat N/S lon E/W" shape, so one pattern walks the
    // whole bulletin in order to build the forecast track line.
    private static readonly Regex JtwcTrackPoint = new(
        @"(\d{1,3}\.?\d*)\s*([NS])\s+(\d{1,3}\.?\d*)\s*([EW])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JtwcWind = new(
        @"MAX\s+SUSTAINED\s+WINDS[\s\S]{0,10}?-\s*(\d+)\s*KT,?\s*GUSTS\s*(\d+)\s*KT",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JtwcPressure = new(
        @"MINIMUM\s+CENTRAL\s+PRESSURE\s+AT\s+\d+Z\s+IS\s+(\d+)\s*MB",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private async Task<List<CycloneDto>> FetchJtwcAsync(HttpClient client, CancellationToken ct)
    {
        var html = await client.GetStringAsync(JtwcUrl, ct);
        var flattened = Regex.Replace(Regex.Replace(html, "<[^>]+>", " "), @"\s+", " ");

        var cyclones = new List<CycloneDto>();
        foreach (Match m in JtwcStormBlock.Matches(flattened))
        {
            try
            {
                var cyclone = await ParseJtwcStormAsync(client, m, ct);
                if (cyclone != null) cyclones.Add(cyclone);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Skipped a JTWC storm that failed to parse");
            }
        }
        return cyclones;
    }

    private async Task<CycloneDto?> ParseJtwcStormAsync(HttpClient client, Match headerMatch, CancellationToken ct)
    {
        var classificationLabel = Regex.Replace(headerMatch.Groups[1].Value.Trim(), @"\s+", " ");
        var designator = headerMatch.Groups[2].Value.ToUpperInvariant(); // e.g. "26W"
        var name = headerMatch.Groups[3].Value.Trim();
        var warningNumber = headerMatch.Groups[4].Value;
        var prefix = headerMatch.Groups[5].Value.ToLowerInvariant(); // e.g. "wp2626"
        var basinCode = prefix.Length >= 2 ? prefix[..2] : null;

        var advisoryUrl = $"https://www.metoc.navy.mil/jtwc/products/{prefix}web.txt";
        var imageUrl = $"https://www.metoc.navy.mil/jtwc/products/{prefix}.gif";

        string bulletin;
        try
        {
            bulletin = await client.GetStringAsync(advisoryUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch JTWC bulletin for {Designator}", designator);
            return null;
        }

        var posMatch = JtwcPosition.Match(bulletin);
        if (!posMatch.Success) return null; // no position, can't place it on the map

        var lat = double.Parse(posMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        if (posMatch.Groups[2].Value.Equals("S", StringComparison.OrdinalIgnoreCase)) lat = -lat;
        var lon = double.Parse(posMatch.Groups[3].Value, CultureInfo.InvariantCulture);
        if (posMatch.Groups[4].Value.Equals("W", StringComparison.OrdinalIgnoreCase)) lon = -lon;

        var windMatch = JtwcWind.Match(bulletin);
        var pressureMatch = JtwcPressure.Match(bulletin);

        // Forecast track: stop at REMARKS (it repeats the current position in
        // prose, which would otherwise duplicate/pollute the track line).
        var remarksIdx = bulletin.IndexOf("REMARKS", StringComparison.OrdinalIgnoreCase);
        var trackSection = remarksIdx > 0 ? bulletin[..remarksIdx] : bulletin;
        var track = new List<CycloneLatLngDto>();
        foreach (Match tm in JtwcTrackPoint.Matches(trackSection))
        {
            var tLat = double.Parse(tm.Groups[1].Value, CultureInfo.InvariantCulture);
            if (tm.Groups[2].Value.Equals("S", StringComparison.OrdinalIgnoreCase)) tLat = -tLat;
            var tLon = double.Parse(tm.Groups[3].Value, CultureInfo.InvariantCulture);
            if (tm.Groups[4].Value.Equals("W", StringComparison.OrdinalIgnoreCase)) tLon = -tLon;
            track.Add(new CycloneLatLngDto { Lat = tLat, Lon = tLon });
        }
        if (track.Count == 0) track.Add(new CycloneLatLngDto { Lat = lat, Lon = lon });

        return new CycloneDto
        {
            Id = $"jtwc-{designator}",
            Source = "JTWC",
            Name = name,
            Classification = JtwcClassificationCodes.TryGetValue(classificationLabel, out var code) ? code : null,
            ClassificationLabel = classificationLabel,
            Basin = basinCode != null && JtwcBasinLabels.TryGetValue(basinCode, out var basinLabel) ? basinLabel : null,
            WarningNumber = warningNumber,
            Latitude = lat,
            Longitude = lon,
            MaxSustainedWindKt = windMatch.Success ? ParseDouble(windMatch.Groups[1].Value) : null,
            PressureMb = pressureMatch.Success ? ParseDouble(pressureMatch.Groups[1].Value) : null,
            AdvisoryUrl = advisoryUrl,
            GraphicUrl = imageUrl,
            ImageUrl = imageUrl,
            Track = track,
        };
    }
}
