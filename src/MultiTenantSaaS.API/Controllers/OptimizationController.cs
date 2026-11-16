using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Enums;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

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
    private readonly IOptimizationJobService _jobs;
    private readonly ICurrentUserService _currentUser;
    private readonly IOptimizationMonitoringService _monitoring;
    private readonly IEtsPolicyService _etsPolicy;
    private readonly IPerformanceCalibrationService _calibration;

    public OptimizationController(
        IRouteOptimizationEngine engine, IApplicationDbContext db, IOptimizationJobService jobs, ICurrentUserService currentUser,
        IOptimizationMonitoringService monitoring, IEtsPolicyService etsPolicy, IPerformanceCalibrationService calibration)
    {
        _engine = engine;
        _db = db;
        _jobs = jobs;
        _currentUser = currentUser;
        _monitoring = monitoring;
        _etsPolicy = etsPolicy;
        _calibration = calibration;
    }

    /// <summary>The tenant's EU ETS charging rule (defaults are returned, flagged not configured, until one is saved).</summary>
    [HttpGet("ets-policy")]
    public async Task<IActionResult> GetEtsPolicy(CancellationToken cancellationToken) =>
        Ok(ApiResponse<EtsPolicyDto>.SuccessResult(await _etsPolicy.GetAsync(cancellationToken)));

    [HttpPut("ets-policy")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    public async Task<IActionResult> SaveEtsPolicy([FromBody] EtsPolicyDto policy, CancellationToken cancellationToken)
    {
        var (saved, problem) = await _etsPolicy.SaveAsync(policy, cancellationToken);
        return problem != null
            ? BadRequest(ApiResponse<EtsPolicyDto>.FailureResult(problem))
            : Ok(ApiResponse<EtsPolicyDto>.SuccessResult(saved!));
    }

    /// <summary>Fit a correction factor from verified noon reports. The result is a proposal; nothing changes until it is approved.</summary>
    [HttpPost("calibrations/fit")]
    public async Task<IActionResult> FitCalibration([FromBody] CalibrationFitRequestDto request, CancellationToken cancellationToken)
    {
        var (fitted, problem) = await _calibration.FitAsync(request, cancellationToken);
        return problem != null
            ? BadRequest(ApiResponse<PerformanceCalibrationDto>.FailureResult(problem))
            : Ok(ApiResponse<PerformanceCalibrationDto>.SuccessResult(fitted!));
    }

    [HttpGet("calibrations")]
    public async Task<IActionResult> ListCalibrations([FromQuery] string vesselImo, [FromQuery] string? loadCondition, CancellationToken cancellationToken) =>
        Ok(ApiResponse<List<PerformanceCalibrationDto>>.SuccessResult(await _calibration.ListAsync(vesselImo, loadCondition, cancellationToken)));

    [HttpPost("calibrations/{id:guid}/approve")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    public Task<IActionResult> ApproveCalibration(Guid id, CancellationToken cancellationToken) => ReviewCalibration(id, true, cancellationToken);

    [HttpPost("calibrations/{id:guid}/reject")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    public Task<IActionResult> RejectCalibration(Guid id, CancellationToken cancellationToken) => ReviewCalibration(id, false, cancellationToken);

    private async Task<IActionResult> ReviewCalibration(Guid id, bool approve, CancellationToken cancellationToken)
    {
        var (reviewed, problem) = await _calibration.ReviewAsync(id, approve, cancellationToken);
        return problem != null
            ? BadRequest(ApiResponse<PerformanceCalibrationDto>.FailureResult(problem))
            : Ok(ApiResponse<PerformanceCalibrationDto>.SuccessResult(reviewed!));
    }

    /// <summary>Compare the latest reported position/time/bunkers (or a supplied snapshot) with a stored run and, when the drift exceeds the thresholds, raise an operator-confirmable re-optimization suggestion. Never replaces the route.</summary>
    [HttpPost("runs/{runId:guid}/check")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationCheckResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckRun(Guid runId, [FromBody] OptimizationCheckRequestDto? request, CancellationToken cancellationToken)
    {
        var result = await _monitoring.CheckRunAsync(runId, request ?? new(), cancellationToken);
        return result == null
            ? NotFound(ApiResponse<OptimizationCheckResultDto>.FailureResult("Optimization run not found."))
            : Ok(ApiResponse<OptimizationCheckResultDto>.SuccessResult(result));
    }

    /// <summary>Same check against the voyage's most recent run (used after a tracksheet save).</summary>
    [HttpPost("voyages/{voyageId}/check")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationCheckResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckVoyage(string voyageId, [FromBody] OptimizationCheckRequestDto? request, CancellationToken cancellationToken)
    {
        var result = await _monitoring.CheckVoyageAsync(voyageId, request ?? new(), cancellationToken);
        return result == null
            ? NotFound(ApiResponse<OptimizationCheckResultDto>.FailureResult("This voyage has no optimization run yet."))
            : Ok(ApiResponse<OptimizationCheckResultDto>.SuccessResult(result));
    }

    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(ApiResponse<List<OptimizationRecommendationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendations([FromQuery] string voyageId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var rows = await _monitoring.GetRecommendationsAsync(voyageId, status, cancellationToken);
        return Ok(ApiResponse<List<OptimizationRecommendationDto>>.SuccessResult(rows));
    }

    [HttpPost("recommendations/{id:guid}/confirm")]
    public Task<IActionResult> ConfirmRecommendation(Guid id, CancellationToken cancellationToken) => Resolve(id, true, cancellationToken);

    [HttpPost("recommendations/{id:guid}/dismiss")]
    public Task<IActionResult> DismissRecommendation(Guid id, CancellationToken cancellationToken) => Resolve(id, false, cancellationToken);

    private async Task<IActionResult> Resolve(Guid id, bool confirm, CancellationToken cancellationToken)
    {
        var result = await _monitoring.ResolveAsync(id, confirm, cancellationToken);
        return result == null
            ? NotFound(ApiResponse<OptimizationRecommendationDto>.FailureResult("Suggestion not found or already handled."))
            : Ok(ApiResponse<OptimizationRecommendationDto>.SuccessResult(result));
    }

    /// <summary>Predicted vs actual speed, RPM, power, fuel and ROB for a run, plus monthly observed operating points. Read-only.</summary>
    [HttpGet("runs/{runId:guid}/performance-comparison")]
    [ProducesResponseType(typeof(ApiResponse<PerformanceComparisonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ComparePerformance(Guid runId, [FromQuery] string? scenario, CancellationToken cancellationToken)
    {
        var result = await _monitoring.ComparePerformanceAsync(runId, scenario, cancellationToken);
        return result == null
            ? NotFound(ApiResponse<PerformanceComparisonDto>.FailureResult("Optimization run not found."))
            : Ok(ApiResponse<PerformanceComparisonDto>.SuccessResult(result));
    }

    private static string? Validate(OptimizationRequestDto request)
    {
        if (request.Scenarios.Count == 0) return "At least one scenario must be requested.";

        var supportedScenarios = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "earliest-eta", "lowest-fuel", "lowest-cost", "lowest-co2", "safest", "balanced",
            "maintain-cp", "specific-eta", "custom-simulation",
        };
        var unsupported = request.Scenarios.FirstOrDefault(scenario => !supportedScenarios.Contains(scenario));
        if (unsupported != null) return $"Unsupported optimization scenario '{unsupported}'.";
        if (request.PerformanceBasis is not ("ACTUAL" or "CP")) return "Performance basis must be ACTUAL or CP.";

        if (request.Scenarios.Contains("specific-eta", StringComparer.OrdinalIgnoreCase))
        {
            if (request.ArrivalRule is not ("ON" or "BETWEEN" or "BEFORE" or "AFTER"))
                return "Arrival rule must be ON, BETWEEN, BEFORE, or AFTER.";
            if (!request.TargetEtaUtc.HasValue || request.TargetEtaUtc.Value <= request.EtdUtc)
                return "A valid arrival date/time after ETD is required for Just-In-Time Arrival.";
            if (request.ArrivalRule == "BETWEEN" &&
                (!request.ArrivalWindowEndUtc.HasValue || request.ArrivalWindowEndUtc.Value <= request.TargetEtaUtc.Value))
                return "BETWEEN requires an end date/time later than the arrival-window start.";
        }
        return null;
    }

    /// <summary>Start a background optimization; poll <c>jobs/{jobId}</c> for progress and the result.</summary>
    [HttpPost("jobs")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationJobStatusDto>), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartJob([FromBody] OptimizationRequestDto request)
    {
        var problem = Validate(request);
        if (problem != null) return BadRequest(ApiResponse<OptimizationJobStatusDto>.FailureResult(problem));

        var jobId = await _jobs.StartAsync(request, _currentUser.TenantId, _currentUser.UserId);
        var status = (await _jobs.GetAsync(jobId, _currentUser.TenantId))!;
        return Accepted(ApiResponse<OptimizationJobStatusDto>.SuccessResult(status));
    }

    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationJobStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJob(Guid jobId)
    {
        var status = await _jobs.GetAsync(jobId, _currentUser.TenantId);
        return status == null
            ? NotFound(ApiResponse<OptimizationJobStatusDto>.FailureResult("Optimization job not found (it may have expired)."))
            : Ok(ApiResponse<OptimizationJobStatusDto>.SuccessResult(status));
    }

    [HttpDelete("jobs/{jobId:guid}")]
    public async Task<IActionResult> CancelJob(Guid jobId)
    {
        return await _jobs.CancelAsync(jobId, _currentUser.TenantId)
            ? Ok(ApiResponse<bool>.SuccessResult(true))
            : NotFound(ApiResponse<bool>.FailureResult("Optimization job not found."));
    }

    /// <summary>Run the optimization engine for one or more scenarios and persist the results.</summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationRunResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run([FromBody] OptimizationRequestDto request, CancellationToken cancellationToken)
    {
        var problem = Validate(request);
        if (problem != null) return BadRequest(ApiResponse<OptimizationRunResultDto>.FailureResult(problem));

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

        var dto = OptimizationRunMapper.ToDto(run);

        return Ok(ApiResponse<OptimizationRunResultDto>.SuccessResult(dto));
    }
}
