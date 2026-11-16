using MultiTenantSaaS.Application.DTOs.MaritimeRegions;

namespace MultiTenantSaaS.Application.Common.Interfaces;

/// <summary>
/// Resolves the named maritime region (strait, channel, passage, gulf, bay,
/// sea or ocean) at a given lat/lon from the public Marine Regions gazetteer
/// (marineregions.org — the IHO's official "Limits of Oceans and Seas"
/// standard plus the SeaVoX sea/strait/channel gazetteer), for labelling
/// waypoints on the map with something more useful than a generic "WP N".
/// Implementations should cache aggressively — this is static geography, a
/// public research institute's free service, and not meant to be hit once
/// per waypoint per page load.
/// </summary>
public interface IMaritimeRegionService
{
    /// <summary>Returns `null` if the lookup fails (offline, upstream error/
    ///  timeout) or the gazetteer has no usable named-feature result for
    ///  this point — never throws for those cases.</summary>
    Task<MaritimeRegionDto?> GetRegionAsync(double lat, double lon, CancellationToken cancellationToken = default);
}
