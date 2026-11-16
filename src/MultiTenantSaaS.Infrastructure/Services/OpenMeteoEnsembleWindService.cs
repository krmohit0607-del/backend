using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.Infrastructure.Services;

/// <summary>Open-Meteo ensemble API (GFS 0.25 degree, 10 m wind). Marine wave and current ensembles are not offered, so only wind members are returned.</summary>
public sealed class OpenMeteoEnsembleWindService : IEnsembleWindService
{
    private const string Model = "gfs025";
    private const string Endpoint = "https://ensemble-api.open-meteo.com/v1/ensemble";
    private const int MaxPoints = 12;
    private const int MaxHorizonDays = 15;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenMeteoEnsembleWindService> _logger;

    public OpenMeteoEnsembleWindService(IHttpClientFactory httpClientFactory, ILogger<OpenMeteoEnsembleWindService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string SourceName => $"Open-Meteo ensemble ({Model}, wind only)";

    public async Task<EnsembleWind?> GetAsync(IReadOnlyList<(double Lat, double Lng)> points, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        if (points.Count == 0 || points.Count > MaxPoints) return null;
        var start = new DateTime(fromUtc.Year, fromUtc.Month, fromUtc.Day, fromUtc.Hour, 0, 0, DateTimeKind.Utc);
        var end = toUtc > start.AddDays(MaxHorizonDays) ? start.AddDays(MaxHorizonDays) : toUtc;
        if (end <= start) return null;

        var inv = CultureInfo.InvariantCulture;
        var url = $"{Endpoint}?latitude={string.Join(",", points.Select(p => p.Lat.ToString("0.####", inv)))}" +
                  $"&longitude={string.Join(",", points.Select(p => p.Lng.ToString("0.####", inv)))}" +
                  $"&hourly=wind_speed_10m,wind_direction_10m&models={Model}&wind_speed_unit=kn&timezone=UTC" +
                  $"&start_date={start:yyyy-MM-dd}&end_date={end:yyyy-MM-dd}";
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(url, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ensemble wind request returned HTTP {Status}", (int)response.StatusCode);
                return null;
            }
            return Parse(await response.Content.ReadAsStringAsync(timeout.Token), points.Count, start);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (cancellationToken.IsCancellationRequested) throw;
            _logger.LogWarning(ex, "Ensemble wind data could not be retrieved");
            return null;
        }
    }

    internal static EnsembleWind? Parse(string json, int pointCount, DateTime start)
    {
        using var document = JsonDocument.Parse(json);
        var locations = document.RootElement.ValueKind == JsonValueKind.Array ? document.RootElement.EnumerateArray().ToList() : new List<JsonElement> { document.RootElement };
        if (locations.Count != pointCount) return null;

        var speeds = new List<List<double[]>>();
        var directions = new List<List<double[]>>();
        var hours = int.MaxValue;
        foreach (var location in locations)
        {
            if (!location.TryGetProperty("hourly", out var hourly) || !hourly.TryGetProperty("time", out var times)) return null;
            var timeList = times.EnumerateArray().Select(item => item.GetString()).ToList();
            var first = timeList.FindIndex(text => DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed) && parsed >= start);
            if (first < 0) return null;

            var speedKeys = hourly.EnumerateObject().Where(property => property.Name.StartsWith("wind_speed_10m", StringComparison.Ordinal)).OrderBy(property => property.Name, StringComparer.Ordinal).ToList();
            var directionKeys = hourly.EnumerateObject().Where(property => property.Name.StartsWith("wind_direction_10m", StringComparison.Ordinal)).OrderBy(property => property.Name, StringComparer.Ordinal).ToList();
            if (speedKeys.Count == 0 || speedKeys.Count != directionKeys.Count) return null;

            speeds.Add(speedKeys.Select(property => Series(property.Value, first)).ToList());
            directions.Add(directionKeys.Select(property => Series(property.Value, first)).ToList());
            hours = Math.Min(hours, speeds[^1].Min(series => series.Length));
        }
        if (hours <= 1 || hours == int.MaxValue) return null;

        var members = speeds[0].Count;
        if (speeds.Any(list => list.Count != members)) return null;
        var speed = new double[members][][];
        var from = new double[members][][];
        for (var member = 0; member < members; member++)
        {
            speed[member] = speeds.Select(location => location[member].Take(hours).ToArray()).ToArray();
            from[member] = directions.Select(location => location[member].Take(hours).ToArray()).ToArray();
        }
        return new EnsembleWind(start, hours, speed, from);
    }

    private static double[] Series(JsonElement array, int first) =>
        array.EnumerateArray().Skip(first).Select(item => item.ValueKind == JsonValueKind.Number ? item.GetDouble() : double.NaN).ToArray();
}
