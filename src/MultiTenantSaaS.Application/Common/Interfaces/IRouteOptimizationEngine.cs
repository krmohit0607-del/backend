using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.Application.Common.Interfaces;

/// <summary>
/// Real weather/zone-aware route optimization engine — a DAG shortest-path
/// (time-expanded, discrete-speed-setting) search over a lateral corridor
/// lattice between origin and destination, inspired by the TNM OptiNav C++
/// reference engine's cost model (weighted time + fuel + emissions, ECA
/// fuel split, ETA penalty) but implemented natively against this app's own
/// weather (<see cref="IWeatherDataService"/>), weather-factor
/// (<c>WeatherFactorEntry</c>) and vessel data — no external process,
/// GRIB/Redis/SQLite/GDAL dependency.
/// </summary>
public interface IRouteOptimizationEngine
{
    Task<OptimizationRunResultDto> RunAsync(OptimizationRequestDto request, CancellationToken cancellationToken = default);

    Task<OptimizationRunResultDto> RunAsync(OptimizationRequestDto request, IProgress<OptimizationProgress>? progress, CancellationToken cancellationToken = default);
}
