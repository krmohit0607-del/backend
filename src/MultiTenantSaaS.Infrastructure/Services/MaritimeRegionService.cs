using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.MaritimeRegions;

namespace MultiTenantSaaS.Infrastructure.Services;

/// <summary>
/// Looks up the named maritime region at a lat/lon via the Marine Regions
/// gazetteer (<see href="https://www.marineregions.org"/>) — a free public
/// REST service run by the Flanders Marine Institute (VLIZ), combining the
/// IHO's official "Limits of Oceans and Seas" (S-23) standard with the
/// SeaVoX sea/strait/channel gazetteer, so results cover both broad named
/// seas/oceans and narrow named straits/channels/passages worldwide — not
/// just a hand-picked subset.
///
/// Results are cached indefinitely in-process, keyed by a rounded lat/lon —
/// this is static geography (it never changes), and the gazetteer is a
/// public research institute's free service that must not be hit once per
/// waypoint per page load across every user of this app.
/// </summary>
public class MaritimeRegionService : IMaritimeRegionService
{
    private const string BaseUrl = "https://www.marineregions.org/rest/getGazetteerRecordsByLatLong.json";

    // Placetypes worth showing as a "where is this" label, most specific
    // first — a narrow named strait/channel/passage takes priority over the
    // broad named sea/ocean around it, which in turn takes priority over a
    // looser "part of the X Sea" intersect region. Placetypes not listed
    // here (Island, EEZ, Territorial Sea, Marine Ecoregion/Province, Large
    // Marine Ecosystem, Archipelagic Waters, ...) are deliberately excluded:
    // they're real gazetteer entries but describe jurisdiction/ecology/land,
    // not a navigational place name a waypoint should be labelled with.
    private static readonly string[][] PlaceTypePriority =
    {
        new[] { "Strait", "Channel", "Passage", "Sound", "Bay", "Gulf" },
        new[] { "IHO Sea Area" },
        new[] { "SeaVoX SeaArea - sub-region" },
        new[] { "Marine Region" },
        new[] { "General Sea Area" },
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MaritimeRegionService> _logger;
    private static readonly ConcurrentDictionary<string, MaritimeRegionDto?> Cache = new();

    public MaritimeRegionService(IHttpClientFactory httpClientFactory, ILogger<MaritimeRegionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<MaritimeRegionDto?> GetRegionAsync(double lat, double lon, CancellationToken cancellationToken = default)
    {
        if (double.IsNaN(lat) || double.IsNaN(lon)) return null;

        var key = CacheKey(lat, lon);
        if (Cache.TryGetValue(key, out var cached)) return cached;

        MaritimeRegionDto? result = null;
        try
        {
            result = await FetchAsync(lat, lon, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Marine Regions gazetteer lookup failed for ({Lat}, {Lon})", lat, lon);
        }

        // Cache the miss/failure too (as null) so a point with genuinely no
        // gazetteer coverage, or a transient upstream error, doesn't get
        // retried on every single render — just once per process lifetime.
        Cache[key] = result;
        return result;
    }

    private static string CacheKey(double lat, double lon) =>
        $"{lat.ToString("F4", CultureInfo.InvariantCulture)},{lon.ToString("F4", CultureInfo.InvariantCulture)}";

    private async Task<MaritimeRegionDto?> FetchAsync(double lat, double lon, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("maritime-regions");
        client.Timeout = TimeSpan.FromSeconds(8);
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            client.DefaultRequestHeaders.Add("User-Agent", "FleetViewRoutePlanner/1.0 (+shipping route planning)");

        var url = $"{BaseUrl}/{lat.ToString(CultureInfo.InvariantCulture)}/{lon.ToString(CultureInfo.InvariantCulture)}/";
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var records = await client.GetFromJsonAsync<List<GazetteerRecord>>(url, options, ct);
        if (records == null || records.Count == 0) return null;

        foreach (var tier in PlaceTypePriority)
        {
            var match = records.FirstOrDefault(r =>
                !string.IsNullOrWhiteSpace(r.PreferredGazetteerName) &&
                r.PlaceType != null &&
                tier.Contains(r.PlaceType, StringComparer.OrdinalIgnoreCase));
            if (match != null)
            {
                return new MaritimeRegionDto { Name = match.PreferredGazetteerName!, Kind = match.PlaceType };
            }
        }
        return null;
    }

    private sealed class GazetteerRecord
    {
        [JsonPropertyName("placeType")]
        public string? PlaceType { get; set; }

        [JsonPropertyName("preferredGazetteerName")]
        public string? PreferredGazetteerName { get; set; }
    }
}
