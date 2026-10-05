using MultiTenantSaaS.Application.DTOs.Weather;

namespace MultiTenantSaaS.Application.Common.Interfaces;

public interface IWeatherDataService
{
    /// <summary>
    /// Get a weather grid for `factorId` at `timestampUtc` over the given
    /// bounds, resampled to `cols` x `rows`. Transparently fetches-and-caches
    /// any missing map tiles from the upstream weather API (Open-Meteo
    /// forecast/marine for future hours, the historical archive API for
    /// past ones) the first time they're requested; every call after that
    /// for an overlapping viewport is served from the database.
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
