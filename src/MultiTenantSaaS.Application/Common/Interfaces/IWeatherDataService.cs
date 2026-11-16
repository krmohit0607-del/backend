using MultiTenantSaaS.Application.DTOs.Weather;

namespace MultiTenantSaaS.Application.Common.Interfaces;

public interface IWeatherDataService
{
    /// <summary>
    /// Get a weather grid for `factorId` at `timestampUtc` over the given
    /// bounds, resampled to `cols` x `rows`. Serves every request — live/
    /// forecast hours and historical ones alike — from a persistent,
    /// geographically fixed tile cache: the first request for a given
    /// (factor, hour, tile) transparently fetches-and-caches it from the
    /// upstream weather API (Open-Meteo forecast/marine for live hours,
    /// falling back to the historical archive when live data isn't
    /// available), and every later request for an overlapping viewport is
    /// served from the database — including by other users. Because the
    /// cache is keyed by fixed geography rather than the caller's exact
    /// viewport, panning/zooming (including across the antimeridian) never
    /// changes which underlying samples back a given point.
    /// </summary>
    Task<WeatherGridDto> GetGridAsync(
        string factorId,
        DateTime timestampUtc,
        double south,
        double west,
        double north,
        double east,
        int cols,
        int rows,
        CancellationToken cancellationToken = default);
}
