namespace MultiTenantSaaS.Application.DTOs.MaritimeRegions;

/// <summary>
/// The named maritime region (strait, channel, passage, gulf, bay, sea or
/// ocean) at a given lat/lon, resolved from the authoritative public
/// Marine Regions gazetteer (IHO "Limits of Oceans and Seas" + the SeaVoX
/// sea/strait/channel gazetteer) — see
/// <see cref="Common.Interfaces.IMaritimeRegionService"/>.
/// </summary>
public class MaritimeRegionDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>The gazetteer's own feature-type label for this result, e.g.
    ///  "Strait", "Channel", "Passage", "Bay", "IHO Sea Area" — shown as-is
    ///  rather than remapped to a fixed enum, since the gazetteer covers far
    ///  more distinct kinds of named feature than this app needs to model
    ///  individually.</summary>
    public string? Kind { get; set; }
}
