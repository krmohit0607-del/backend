using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Weather;

namespace MultiTenantSaaS.API.Controllers;

/// <summary>
/// Persistent weather grid cache — backs the map's historical/date-range
/// viewer. See <see cref="IWeatherDataService"/> for the tile-cache design.
/// </summary>
[Route("api/weather")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly IWeatherDataService _weather;

    public WeatherController(IWeatherDataService weather)
    {
        _weather = weather;
    }

    [HttpGet("grid")]
    public async Task<IActionResult> GetGrid(
        [FromQuery] string factorId,
        [FromQuery] DateTime timestamp,
        [FromQuery] double south,
        [FromQuery] double west,
        [FromQuery] double north,
        [FromQuery] double east,
        [FromQuery] int cols,
        [FromQuery] int rows,
        CancellationToken cancellationToken)
    {
        var grid = await _weather.GetGridAsync(factorId, timestamp.ToUniversalTime(), south, west, north, east, cols, rows, cancellationToken);
        return Ok(ApiResponse<WeatherGridDto>.SuccessResult(grid));
    }
}
