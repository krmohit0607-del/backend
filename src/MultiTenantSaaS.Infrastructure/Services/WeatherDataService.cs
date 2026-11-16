using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Weather;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services;

/// <summary>
/// Persistent, tile-based weather grid cache — now the single source for
/// both the live map overlay and the historical/date-range viewer (see
/// <see cref="GetGridAsync"/>). The first request for a given (factor, UTC
/// hour, map tile) fetches it from Open-Meteo and stores it compactly
/// (scaled Int16 arrays, not raw JSON/doubles) in
/// <see cref="WeatherGridSnapshot"/>; every later request for any viewport
/// overlapping that tile — from any user — is served straight from the
/// database, so the upstream API is only ever called once per tile per
/// hour (plus an occasional refresh for hours still ahead of "now"; see
/// <see cref="LiveForecastRefreshInterval"/>). Tiles are fixed geographic
/// blocks — unlike a per-viewport fetch, panning/zooming never changes
/// which underlying samples back a given point, so the rendered field
/// stays continuous and doesn't jump or "break" as the map moves, including
/// across the antimeridian.
///
/// Tiles are fixed 5°x5° blocks sampled at an 11x11 (0.5°) internal
/// resolution.
/// </summary>
public class WeatherDataService : IWeatherDataService
{
    private const double TileSizeDeg = 5.0;
    private const int TileResolution = 11; // 11x11 points per tile => 0.5° spacing

    // Open-Meteo's public endpoints enforce a shared per-IP rate limit; once either
    // one starts returning 429 (seen across *all* app instances, since this is
    // static/process-wide), stop issuing further requests to that base for a short
    // cooldown instead of burning the limit further on every tile this run needs —
    // an optimization run alone can touch dozens of distinct tiles/hours.
    private static readonly ConcurrentDictionary<string, DateTime> RateLimitedUntilUtc = new();
    private static readonly TimeSpan RateLimitCooldown = TimeSpan.FromSeconds(60);

    private sealed record FactorQuery(string Api, string MagVar, string? DirVar, double? ScaleToUnit);

    private static readonly Dictionary<string, FactorQuery> Factors = new()
    {
        ["wind"] = new FactorQuery("forecast", "wind_speed_10m", "wind_direction_10m", null),
        ["gusts"] = new FactorQuery("forecast", "wind_gusts_10m", "wind_direction_10m", null),
        ["pressure"] = new FactorQuery("forecast", "surface_pressure", null, null),
        ["precipitation"] = new FactorQuery("forecast", "precipitation", null, null),
        ["airTemp"] = new FactorQuery("forecast", "temperature_2m", null, null),
        ["waves"] = new FactorQuery("marine", "wave_height", "wave_direction", null),
        ["swell"] = new FactorQuery("marine", "swell_wave_height", "swell_wave_direction", null),
        ["wavePeriod"] = new FactorQuery("marine", "wave_period", null, null),
        ["swellPeriod"] = new FactorQuery("marine", "swell_wave_period", null, null),
        ["seaTemp"] = new FactorQuery("marine", "sea_surface_temperature", null, null),
        // Open-Meteo reports current speed in km/h; the frontend (and this
        // cache) always deals in knots.
        ["currents"] = new FactorQuery("marine", "ocean_current_velocity", "ocean_current_direction", 0.539957),
    };

    private static readonly Dictionary<string, string> Bases = new()
    {
        ["forecast"] = "https://api.open-meteo.com/v1/forecast",
        ["marine"] = "https://marine-api.open-meteo.com/v1/marine",
    };

    // Historical fallback, used only once the live endpoint above can't serve a slot
    // (upstream rate limit/outage, or the date is outside the live forecast horizon).
    // "forecast" moves to Open-Meteo's dedicated ERA5 reanalysis archive (gap-free back
    // to 1940, on separate infrastructure so it isn't affected by the live endpoint's
    // rate limit); "marine" reuses its own live host, which already blends in ERA5-Ocean
    // reanalysis for older dates on the same endpoint/parameters.
    private static readonly Dictionary<string, string> HistoricalBases = new()
    {
        ["forecast"] = "https://archive-api.open-meteo.com/v1/archive",
        ["marine"] = "https://marine-api.open-meteo.com/v1/marine",
    };

    // How many years back to retry the historical endpoint on the same calendar day/hour
    // as a seasonal proxy, for slots beyond the live forecast horizon (i.e. in the future,
    // where no true historical reading can exist yet).
    private const int MaxClimatologyLookbackYears = 3;

    // How long a cached tile for an hour that hasn't elapsed yet (today onward) is
    // trusted before it's treated as stale and re-fetched — Open-Meteo revises its
    // forecast for a given hour as newer model runs come in, right up until that hour
    // actually occurs, so caching it forever would freeze the live map at whatever was
    // first fetched for it. Hours already in the past are unaffected (cached forever,
    // same as historical data, since what already happened doesn't change).
    private static readonly TimeSpan LiveForecastRefreshInterval = TimeSpan.FromMinutes(45);

    private readonly IApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherDataService> _logger;

    public WeatherDataService(IApplicationDbContext db, IHttpClientFactory httpClientFactory, ILogger<WeatherDataService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<WeatherGridDto> GetGridAsync(
        string factorId,
        DateTime timestampUtc,
        double south,
        double west,
        double north,
        double east,
        int cols,
        int rows,
        CancellationToken cancellationToken = default)
    {
        cols = Math.Max(2, Math.Min(200, cols));
        rows = Math.Max(2, Math.Min(200, rows));
        var hourUtc = new DateTime(timestampUtc.Year, timestampUtc.Month, timestampUtc.Day, timestampUtc.Hour, 0, 0, DateTimeKind.Utc);

        var hasQuery = Factors.TryGetValue(factorId, out var query);
        var tiles = EnumerateTiles(south, west, north, east);
        var tileData = new Dictionary<(double South, double West), (short[] Mag, short[]? Dir)>();
        var allTilesLive = hasQuery && tiles.Count > 0;
        var allTilesHaveData = hasQuery && tiles.Count > 0;
        DateTime? oldestFetch = null;

        if (hasQuery)
        {
            // Hours that are still "now or ahead" (today's current hour through the
            // full forecast horizon) represent a live forecast that Open-Meteo can
            // revise as newer model runs come in — unlike an hour that's already
            // elapsed (fixed/historical, safe to cache forever), so those get a
            // refresh window instead of being served from the cache indefinitely.
            var nowHourUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc);
            var isUpcomingOrCurrentHour = hourUtc >= nowHourUtc;

            foreach (var tile in tiles)
            {
                var cached = await _db.WeatherGridSnapshots.FirstOrDefaultAsync(
                    w => w.FactorId == factorId && w.TimestampUtc == hourUtc && w.TileSouth == tile.South && w.TileWest == tile.West,
                    cancellationToken);

                var isStale = cached != null && isUpcomingOrCurrentHour && DateTime.UtcNow - cached.FetchedAtUtc > LiveForecastRefreshInterval;
                if (cached != null && !isStale)
                {
                    tileData[tile] = (DecodeInt16(cached.MagnitudeData), cached.DirectionData != null ? DecodeInt16(cached.DirectionData) : null);
                    oldestFetch = oldestFetch == null || cached.FetchedAtUtc < oldestFetch ? cached.FetchedAtUtc : oldestFetch;
                    continue;
                }

                var fetched = await FetchTileWithFallbackAsync(query!, tile.South, tile.West, hourUtc, cancellationToken);
                if (fetched == null)
                {
                    if (cached != null)
                    {
                        // Refresh attempt failed (e.g. upstream rate limit) — keep
                        // serving the stale-but-still-real snapshot rather than
                        // reporting the tile as missing outright.
                        tileData[tile] = (DecodeInt16(cached.MagnitudeData), cached.DirectionData != null ? DecodeInt16(cached.DirectionData) : null);
                        oldestFetch = oldestFetch == null || cached.FetchedAtUtc < oldestFetch ? cached.FetchedAtUtc : oldestFetch;
                        continue;
                    }
                    allTilesLive = false;
                    allTilesHaveData = false;
                    continue;
                }
                if (fetched.Value.Historical) allTilesLive = false;

                tileData[tile] = fetched.Value.Data;
                oldestFetch = oldestFetch == null || DateTime.UtcNow < oldestFetch ? DateTime.UtcNow : oldestFetch;
                if (cached != null)
                {
                    cached.MagnitudeData = EncodeInt16(fetched.Value.Data.Mag);
                    cached.DirectionData = fetched.Value.Data.Dir != null ? EncodeInt16(fetched.Value.Data.Dir) : null;
                    cached.FetchedAtUtc = DateTime.UtcNow;
                    continue;
                }
                _db.WeatherGridSnapshots.Add(new WeatherGridSnapshot
                {
                    FactorId = factorId,
                    TimestampUtc = hourUtc,
                    TileSouth = tile.South,
                    TileWest = tile.West,
                    TileSizeDeg = TileSizeDeg,
                    Resolution = TileResolution,
                    MagnitudeData = EncodeInt16(fetched.Value.Data.Mag),
                    DirectionData = fetched.Value.Data.Dir != null ? EncodeInt16(fetched.Value.Data.Dir) : null,
                    FetchedAtUtc = DateTime.UtcNow,
                });
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist weather tile cache for {Factor} {Hour}", factorId, hourUtc);
            }
        }
        else
        {
            allTilesLive = false;
            allTilesHaveData = false;
        }

        var mag = new double[cols * rows];
        var dir = new double[cols * rows];
        for (var r = 0; r < rows; r += 1)
        {
            var lat = north + (south - north) * (rows <= 1 ? 0 : (double)r / (rows - 1));
            for (var c = 0; c < cols; c += 1)
            {
                var lon = west + (east - west) * (cols <= 1 ? 0 : (double)c / (cols - 1));
                var (m, d) = SampleTile(tileData, lat, lon);
                mag[r * cols + c] = m;
                dir[r * cols + c] = d;
            }
        }

        return new WeatherGridDto
        {
            South = south,
            West = west,
            North = north,
            East = east,
            Cols = cols,
            Rows = rows,
            Mag = mag,
            Dir = dir,
            Time = hourUtc.ToString("o"),
            Live = allTilesLive,
            HasData = allTilesHaveData,
            FetchedAtUtc = oldestFetch,
        };
    }

    /// <summary>Every 5°x5° tile whose bounds intersect the requested viewport.</summary>
    private static List<(double South, double West)> EnumerateTiles(double south, double west, double north, double east)
    {
        var tiles = new List<(double, double)>();
        var startLat = Math.Floor(south / TileSizeDeg) * TileSizeDeg;
        var startLon = Math.Floor(west / TileSizeDeg) * TileSizeDeg;
        for (var lat = startLat; lat < north; lat += TileSizeDeg)
        {
            for (var lon = startLon; lon < east; lon += TileSizeDeg)
            {
                if (tiles.Count >= 64) return tiles; // sane upper bound per request
                tiles.Add((lat, lon));
            }
        }
        return tiles;
    }

    /// <summary>Bilinear-sample whichever cached/fetched tile contains (lat, lon); 0 if that tile wasn't available.</summary>
    private static (double Mag, double Dir) SampleTile(
        Dictionary<(double South, double West), (short[] Mag, short[]? Dir)> tileData,
        double lat,
        double lon)
    {
        var tileSouth = Math.Floor(lat / TileSizeDeg) * TileSizeDeg;
        var tileWest = Math.Floor(lon / TileSizeDeg) * TileSizeDeg;
        if (!tileData.TryGetValue((tileSouth, tileWest), out var tile)) return (0, 0);

        var fy = Math.Clamp((lat - tileSouth) / TileSizeDeg, 0, 1) * (TileResolution - 1);
        var fx = Math.Clamp((lon - tileWest) / TileSizeDeg, 0, 1) * (TileResolution - 1);
        var r0 = (int)Math.Floor(fy);
        var c0 = (int)Math.Floor(fx);
        var r1 = Math.Min(TileResolution - 1, r0 + 1);
        var c1 = Math.Min(TileResolution - 1, c0 + 1);
        var ty = fy - r0;
        var tx = fx - c0;

        double At(short[] arr, int r, int c) => arr[r * TileResolution + c] / 10.0;

        var mag =
            At(tile.Mag, r0, c0) * (1 - tx) * (1 - ty) +
            At(tile.Mag, r0, c1) * tx * (1 - ty) +
            At(tile.Mag, r1, c0) * (1 - tx) * ty +
            At(tile.Mag, r1, c1) * tx * ty;

        double dir = 0;
        if (tile.Dir != null)
        {
            // Nearest-neighbour for direction (angles don't average linearly).
            var nr = ty < 0.5 ? r0 : r1;
            var nc = tx < 0.5 ? c0 : c1;
            dir = tile.Dir[nr * TileResolution + nc];
        }
        return (mag, dir);
    }

    /// <summary>
    /// Live-first, historical-fallback tile fetch: tries the live forecast/marine
    /// endpoint for the exact requested hour, and only if that fails (upstream
    /// outage/rate limit, or the hour is outside the live forecast horizon) falls
    /// back to the historical archive — first for the exact date (covers hours the
    /// live API has already aged out but the archive hasn't ingested yet... once it
    /// has), then the same calendar day/hour in earlier years as a seasonal proxy
    /// for dates still in the future. Never fabricates calm-weather data: if every
    /// attempt fails, the tile is simply reported missing to the caller.
    /// </summary>
    private async Task<((short[] Mag, short[]? Dir) Data, bool Historical)?> FetchTileWithFallbackAsync(
        FactorQuery query, double tileSouth, double tileWest, DateTime hourUtc, CancellationToken ct)
    {
        var live = await FetchTileAsync(Bases[query.Api], query.Api, query, tileSouth, tileWest, hourUtc, ct);
        if (live != null) return (live.Value, false);

        if (!HistoricalBases.TryGetValue(query.Api, out var historicalBase)) return null;
        var historicalKey = $"{query.Api}-historical";

        for (var yearsBack = 0; yearsBack <= MaxClimatologyLookbackYears; yearsBack++)
        {
            var candidate = hourUtc.AddYears(-yearsBack);
            if (candidate > DateTime.UtcNow) continue; // never ask the archive for data that hasn't happened yet
            var historical = await FetchTileAsync(historicalBase, historicalKey, query, tileSouth, tileWest, candidate, ct);
            if (historical != null) return (historical.Value, true);
        }
        return null;
    }

    private async Task<(short[] Mag, short[]? Dir)?> FetchTileAsync(
        string baseUrl, string rateLimitKey, FactorQuery query, double tileSouth, double tileWest, DateTime hourUtc, CancellationToken ct)
    {
        if (RateLimitedUntilUtc.TryGetValue(rateLimitKey, out var until) && until > DateTime.UtcNow)
        {
            _logger.LogDebug("Skipping {Api} fetch; still in a 429 cooldown until {Until:O}", rateLimitKey, until);
            return null;
        }

        var lats = new List<string>();
        var lons = new List<string>();
        for (var r = 0; r < TileResolution; r += 1)
        {
            var lat = Math.Max(-85, Math.Min(85, tileSouth + TileSizeDeg * r / (TileResolution - 1)));
            for (var c = 0; c < TileResolution; c += 1)
            {
                var lon = ((tileWest + TileSizeDeg * c / (TileResolution - 1) + 540) % 360) - 180;
                lats.Add(lat.ToString("F3", CultureInfo.InvariantCulture));
                lons.Add(lon.ToString("F3", CultureInfo.InvariantCulture));
            }
        }

        var vars = query.DirVar != null ? $"{query.MagVar},{query.DirVar}" : query.MagVar;
        var date = hourUtc.ToString("yyyy-MM-dd");
        var url =
            $"{baseUrl}?latitude={string.Join(',', lats)}&longitude={string.Join(',', lons)}" +
            $"&hourly={vars}&wind_speed_unit=kn&timezone=UTC&start_date={date}&end_date={date}";

        try
        {
            var client = _httpClientFactory.CreateClient("weather");
            client.Timeout = TimeSpan.FromSeconds(20);
            var res = await client.GetAsync(url, ct);
            if (res.StatusCode == HttpStatusCode.TooManyRequests)
            {
                RateLimitedUntilUtc[rateLimitKey] = DateTime.UtcNow + RateLimitCooldown;
                _logger.LogWarning("Open-Meteo {Api} returned 429; pausing further {Api} fetches for {Seconds}s", rateLimitKey, rateLimitKey, RateLimitCooldown.TotalSeconds);
                return null;
            }
            if (!res.IsSuccessStatusCode) return null;

            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            var cells = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToArray() : new[] { root };
            if (cells.Length != TileResolution * TileResolution) return null;

            var mag = new short[TileResolution * TileResolution];
            short[]? dir = query.DirVar != null ? new short[TileResolution * TileResolution] : null;
            var hourIndex = hourUtc.Hour;

            for (var i = 0; i < cells.Length; i += 1)
            {
                if (!cells[i].TryGetProperty("hourly", out var hourly)) return null;
                var rawMag = ReadArrayValue(hourly, query.MagVar, hourIndex);
                if (query.ScaleToUnit.HasValue) rawMag *= query.ScaleToUnit.Value;
                mag[i] = (short)Math.Round(rawMag * 10);
                if (dir != null && query.DirVar != null)
                {
                    dir[i] = (short)Math.Round(ReadArrayValue(hourly, query.DirVar, hourIndex));
                }
            }
            return (mag, dir);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Weather tile fetch failed for {Api} {Var}", query.Api, query.MagVar);
            return null;
        }
    }

    private static double ReadArrayValue(JsonElement hourly, string prop, int index)
    {
        if (!hourly.TryGetProperty(prop, out var arr) || arr.ValueKind != JsonValueKind.Array) return 0;
        var items = arr.EnumerateArray().ToArray();
        if (index < 0 || index >= items.Length) return 0;
        return items[index].ValueKind == JsonValueKind.Number ? items[index].GetDouble() : 0;
    }

    private static string EncodeInt16(short[] values)
    {
        var bytes = new byte[values.Length * 2];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

    private static short[] DecodeInt16(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var values = new short[bytes.Length / 2];
        Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
        return values;
    }
}
