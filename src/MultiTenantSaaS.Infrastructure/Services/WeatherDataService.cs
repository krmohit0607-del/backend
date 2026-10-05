using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Weather;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services;

/// <summary>
/// Persistent, tile-based weather grid cache. The first request for a given
/// (factor, UTC hour, map tile) fetches it from Open-Meteo and stores it
/// compactly (scaled Int16 arrays, not raw JSON/doubles) in
/// <see cref="WeatherGridSnapshot"/>; every later request for any viewport
/// overlapping that tile — from any user — is served straight from the
/// database, so the upstream API is only ever called once per tile per hour.
///
/// Tiles are fixed 5°x5° blocks sampled at an 11x11 (0.5°) internal
/// resolution — coarser than the live "now" forecast field the map paints,
/// which is an intentional, explicit trade-off for keeping stored rows tiny
/// (a few hundred bytes each) since this path exists for the historical/
/// date-range viewer, not high-fidelity live conditions.
/// </summary>
public class WeatherDataService : IWeatherDataService
{
    private const double TileSizeDeg = 5.0;
    private const int TileResolution = 11; // 11x11 points per tile => 0.5° spacing

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

        if (hasQuery)
        {
            foreach (var tile in tiles)
            {
                var cached = await _db.WeatherGridSnapshots.AsNoTracking().FirstOrDefaultAsync(
                    w => w.FactorId == factorId && w.TimestampUtc == hourUtc && w.TileSouth == tile.South && w.TileWest == tile.West,
                    cancellationToken);

                if (cached != null)
                {
                    tileData[tile] = (DecodeInt16(cached.MagnitudeData), cached.DirectionData != null ? DecodeInt16(cached.DirectionData) : null);
                    continue;
                }

                var fetched = await FetchTileAsync(query!, tile.South, tile.West, hourUtc, cancellationToken);
                if (fetched == null)
                {
                    allTilesLive = false;
                    continue;
                }

                tileData[tile] = fetched.Value;
                _db.WeatherGridSnapshots.Add(new WeatherGridSnapshot
                {
                    FactorId = factorId,
                    TimestampUtc = hourUtc,
                    TileSouth = tile.South,
                    TileWest = tile.West,
                    TileSizeDeg = TileSizeDeg,
                    Resolution = TileResolution,
                    MagnitudeData = EncodeInt16(fetched.Value.Mag),
                    DirectionData = fetched.Value.Dir != null ? EncodeInt16(fetched.Value.Dir) : null,
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

    private async Task<(short[] Mag, short[]? Dir)?> FetchTileAsync(FactorQuery query, double tileSouth, double tileWest, DateTime hourUtc, CancellationToken ct)
    {
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
            $"{Bases[query.Api]}?latitude={string.Join(',', lats)}&longitude={string.Join(',', lons)}" +
            $"&hourly={vars}&wind_speed_unit=kn&timezone=UTC&start_date={date}&end_date={date}";

        try
        {
            var client = _httpClientFactory.CreateClient("weather");
            client.Timeout = TimeSpan.FromSeconds(20);
            var res = await client.GetAsync(url, ct);
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
