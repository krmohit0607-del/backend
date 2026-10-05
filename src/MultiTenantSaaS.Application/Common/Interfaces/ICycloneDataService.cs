using MultiTenantSaaS.Application.DTOs.Cyclones;

namespace MultiTenantSaaS.Application.Common.Interfaces;

/// <summary>
/// Fetches active tropical cyclones from public government sources (NOAA
/// National Hurricane Center, Joint Typhoon Warning Center) for the map
/// overlay. Implementations should cache briefly — these are public,
/// infrequently-updated feeds, not meant to be hit on every request.
/// </summary>
public interface ICycloneDataService
{
    Task<List<CycloneDto>> GetActiveCyclonesAsync(CancellationToken cancellationToken = default);
}
