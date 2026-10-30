using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.API.Controllers;

/// <summary>
/// Native route-optimization engine API — runs the weather/zone-aware
/// Dijkstra-over-DAG solver (see <c>RouteOptimizationEngine</c>) for one or
/// more scenarios and persists every run so the "All Routes" history and
/// comparison links can re-open the exact route/weather/costs from the
/// moment the run was made.
/// </summary>
[Route("api/optimization")]
[Authorize]
public class OptimizationController : ControllerBase
{
    private readonly IRouteOptimizationEngine _engine;
    private readonly IApplicationDbContext _db;

    public OptimizationController(IRouteOptimizationEngine engine, IApplicationDbContext db)
    {
        _engine = engine;
        _db = db;
    }

    /// <summary>Run the optimization engine for one or more scenarios and persist the results.</summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationRunResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run([FromBody] OptimizationRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Scenarios.Count == 0)
        {
            return BadRequest(ApiResponse<OptimizationRunResultDto>.FailureResult("At least one scenario must be requested."));
        }

        try
        {
            var result = await _engine.RunAsync(request, cancellationToken);
            return Ok(ApiResponse<OptimizationRunResultDto>.SuccessResult(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OptimizationRunResultDto>.FailureResult(ex.Message));
        }
    }

    /// <summary>All optimization runs for a voyage, newest first — backs the "All Routes" tab.</summary>
    [HttpGet("runs")]
    [ProducesResponseType(typeof(ApiResponse<List<OptimizationRunSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuns([FromQuery] string voyageId, CancellationToken cancellationToken)
    {
        var runs = await _db.OptimizationRuns
            .Where(r => r.VoyageId == voyageId)
            .OrderByDescending(r => r.RequestedAtUtc)
            .Select(r => new OptimizationRunSummaryDto
            {
                OptimizationRunId = r.Id,
                RequestedAtUtc = r.RequestedAtUtc,
                EtdUtc = r.EtdUtc,
                ScenariosRequestedJson = r.ScenariosRequestedJson,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<OptimizationRunSummaryDto>>.SuccessResult(runs));
    }

    /// <summary>One run's full scenario results (route + locked weather-derived costs), for the "View"/comparison links.</summary>
    [HttpGet("runs/{runId}")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationRunResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRun(Guid runId, CancellationToken cancellationToken)
    {
        var run = await _db.OptimizationRuns
            .Include(r => r.ScenarioResults)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (run == null)
        {
            return NotFound(ApiResponse<OptimizationRunResultDto>.FailureResult("Optimization run not found."));
        }

        var dto = new OptimizationRunResultDto
        {
            OptimizationRunId = run.Id,
            RequestedAtUtc = run.RequestedAtUtc,
            Scenarios = run.ScenarioResults.Select(s => new OptimizationScenarioResultDto
            {
                Scenario = s.Scenario,
                IsFeasible = s.IsFeasible,
                InfeasibleReason = s.InfeasibleReason,
                WasConstrainedByZones = s.WasConstrainedByZones,
                Waypoints = System.Text.Json.JsonSerializer.Deserialize<List<RouteWaypointDto>>(s.RouteWaypointsJson) ?? new(),
                EtdUtc = s.EtdUtc,
                EtaUtc = s.EtaUtc,
                DistanceNm = s.DistanceNm,
                AverageSpeedKn = s.AverageSpeedKn,
                TimeToGoHours = s.TimeToGoHours,
                EcaDistanceNm = s.EcaDistanceNm,
                EcaTimeToGoHours = s.EcaTimeToGoHours,
                MeFuelRequiredToGoMt = s.MeFuelRequiredToGoMt,
                EcaFuelRequiredToGoMt = s.EcaFuelRequiredToGoMt,
                TotalCostToGoUsd = s.TotalCostToGoUsd,
                FuelCostToGoUsd = s.FuelCostToGoUsd,
                TimeCostToGoUsd = s.TimeCostToGoUsd,
                EmissionsCostToGoUsd = s.EmissionsCostToGoUsd,
                MeCo2EmissionsToGoMt = s.MeCo2EmissionsToGoMt,
            }).ToList(),
        };

        return Ok(ApiResponse<OptimizationRunResultDto>.SuccessResult(dto));
    }
}
