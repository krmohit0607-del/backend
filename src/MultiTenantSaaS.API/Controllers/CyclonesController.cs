using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Cyclones;

namespace MultiTenantSaaS.API.Controllers;

/// <summary>
/// Active tropical cyclones (hurricanes/typhoons/tropical storms/depressions)
/// from NOAA's National Hurricane Center and the Joint Typhoon Warning
/// Center, for the map weather overlay.
/// </summary>
[Route("api/cyclones")]
[Authorize]
public class CyclonesController : ControllerBase
{
    private readonly ICycloneDataService _cyclones;

    public CyclonesController(ICycloneDataService cyclones)
    {
        _cyclones = cyclones;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var cyclones = await _cyclones.GetActiveCyclonesAsync(cancellationToken);
        return Ok(ApiResponse<List<CycloneDto>>.SuccessResult(cyclones));
    }
}
