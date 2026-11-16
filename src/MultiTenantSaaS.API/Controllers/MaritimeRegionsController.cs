using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.MaritimeRegions;

namespace MultiTenantSaaS.API.Controllers;

/// <summary>
/// Named maritime region (strait/channel/passage/gulf/bay/sea/ocean) lookup
/// by lat/lon, for labelling route waypoints on the map — see
/// <see cref="IMaritimeRegionService"/>.
/// </summary>
[Route("api/maritime-regions")]
[Authorize]
public class MaritimeRegionsController : ControllerBase
{
    private readonly IMaritimeRegionService _regions;

    public MaritimeRegionsController(IMaritimeRegionService regions)
    {
        _regions = regions;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] double lat, [FromQuery] double lon, CancellationToken cancellationToken)
    {
        var region = await _regions.GetRegionAsync(lat, lon, cancellationToken);
        return Ok(ApiResponse<MaritimeRegionDto?>.SuccessResult(region));
    }
}
