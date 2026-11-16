using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities.Optimization;
using MultiTenantSaaS.Domain.Entities.Performance;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

public sealed class OptimizationMonitoringService : IOptimizationMonitoringService
{
    private static readonly Regex CoordinatePattern = new(@"^(\d{3,6})([NSEWnsew])$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IApplicationDbContext _db;
    private readonly IPerformanceDbContext _perf;
    private readonly ICurrentUserService _user;

    public OptimizationMonitoringService(IApplicationDbContext db, IPerformanceDbContext perf, ICurrentUserService user)
    {
        _db = db;
        _perf = perf;
        _user = user;
    }

    // ------------------------------------------------------------------ re-optimization check

    public async Task<OptimizationCheckResultDto?> CheckVoyageAsync(string voyageId, OptimizationCheckRequestDto request, CancellationToken cancellationToken = default)
    {
        var run = await _db.OptimizationRuns.AsNoTracking().Include(r => r.ScenarioResults)
            .Where(r => r.VoyageId == voyageId).OrderByDescending(r => r.RequestedAtUtc).FirstOrDefaultAsync(cancellationToken);
        return run == null ? null : await CheckAsync(run, request, cancellationToken);
    }

    public async Task<OptimizationCheckResultDto?> CheckRunAsync(Guid runId, OptimizationCheckRequestDto request, CancellationToken cancellationToken = default)
    {
        var run = await _db.OptimizationRuns.AsNoTracking().Include(r => r.ScenarioResults).FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        return run == null ? null : await CheckAsync(run, request, cancellationToken);
    }

    private async Task<OptimizationCheckResultDto> CheckAsync(OptimizationRun run, OptimizationCheckRequestDto request, CancellationToken ct)
    {
        var result = new OptimizationCheckResultDto { RunId = run.Id, VoyageId = run.VoyageId };

        var scenario = request.Scenario != null
            ? run.ScenarioResults.FirstOrDefault(s => string.Equals(s.Scenario, request.Scenario, StringComparison.OrdinalIgnoreCase))
            : run.ScenarioResults.FirstOrDefault(s => s.IsFeasible);
        if (scenario == null || !scenario.IsFeasible)
        {
            result.Notes.Add("The run has no feasible scenario to compare against.");
            return result;
        }
        result.Scenario = scenario.Scenario;

        var waypoints = Deserialize<List<RouteWaypointDto>>(scenario.RouteWaypointsJson) ?? new();
        var analysis = Deserialize<OptimizationAnalysisDto>(scenario.AnalysisJson) ?? new();
        if (waypoints.Count < 2)
        {
            result.Notes.Add("The stored route has too few waypoints to compare against.");
            return result;
        }

        var snapshot = await ResolveSnapshotAsync(run, request, result, ct);
        if (snapshot == null) return result;
        var (lat, lng, time) = (snapshot.Lat, snapshot.Lng, snapshot.Time);
        result.PositionLat = lat;
        result.PositionLng = lng;
        result.PositionTimeUtc = time;
        result.PlannedEtaUtc = scenario.EtaUtc;

        if (time < scenario.EtdUtc.AddHours(-1))
        {
            result.Notes.Add("The reported time is before the planned departure, so there is nothing to compare yet.");
            return result;
        }

        var (alongNm, crossNm, segment, fraction) = Project(waypoints, lat, lng);
        result.PlannedDistanceAlongRouteNm = alongNm;
        result.CrossTrackNm = crossNm;

        var plannedTime = TimeAtSegment(waypoints, segment, fraction);
        var behind = (time - plannedTime).TotalHours;
        result.ScheduleDeviationHours = behind;
        result.ProjectedEtaUtc = scenario.EtaUtc.AddHours(behind);

        if (request.SpeedKn is > 0) result.SpeedDeviationKn = request.SpeedKn.Value - waypoints[segment + 1].SpeedKn;

        var plannedVlsfo = Interpolate(waypoints[segment].RobVlsfoMt, waypoints[segment + 1].RobVlsfoMt, fraction);
        var plannedLsmgo = Interpolate(waypoints[segment].RobLsmgoMt, waypoints[segment + 1].RobLsmgoMt, fraction);
        var robVlsfo = request.RobVlsfoMt ?? snapshot.RobVlsfo;
        var robLsmgo = request.RobLsmgoMt ?? snapshot.RobLsmgo;
        if (robVlsfo.HasValue && plannedVlsfo is > 1) result.RobVlsfoDeviationPct = (robVlsfo.Value - plannedVlsfo.Value) / plannedVlsfo.Value * 100;
        if (robLsmgo.HasValue && plannedLsmgo is > 1) result.RobLsmgoDeviationPct = (robLsmgo.Value - plannedLsmgo.Value) / plannedLsmgo.Value * 100;
        if ((robVlsfo.HasValue || robLsmgo.HasValue) && plannedVlsfo == null && plannedLsmgo == null)
            result.Notes.Add("The run was made without bunker quantities, so bunker deviation cannot be checked.");

        if (analysis.ForecastFetchedAtUtc.HasValue) result.ForecastAgeHours = (DateTime.UtcNow - analysis.ForecastFetchedAtUtc.Value).TotalHours;

        if (Math.Abs(behind) > request.TimeDeviationThresholdHours)
            result.Reasons.Add($"The vessel is {Math.Abs(behind):0.#} h {(behind > 0 ? "behind" : "ahead of")} the planned schedule (threshold {request.TimeDeviationThresholdHours:0.#} h); projected ETA {result.ProjectedEtaUtc:yyyy-MM-dd HH:mm} UTC vs planned {scenario.EtaUtc:yyyy-MM-dd HH:mm} UTC.");
        if (crossNm > request.CrossTrackThresholdNm)
            result.Reasons.Add($"The vessel is {crossNm:0.#} NM off the planned track (threshold {request.CrossTrackThresholdNm:0.#} NM).");
        if (result.RobVlsfoDeviationPct is { } vlsfoPct && Math.Abs(vlsfoPct) > request.RobDeviationThresholdPct)
            result.Reasons.Add($"VLSFO on board is {Math.Abs(vlsfoPct):0.#}% {(vlsfoPct < 0 ? "below" : "above")} the planned ROB at this point (threshold {request.RobDeviationThresholdPct:0.#}%).");
        if (result.RobLsmgoDeviationPct is { } lsmgoPct && Math.Abs(lsmgoPct) > request.RobDeviationThresholdPct)
            result.Reasons.Add($"LSMGO on board is {Math.Abs(lsmgoPct):0.#}% {(lsmgoPct < 0 ? "below" : "above")} the planned ROB at this point (threshold {request.RobDeviationThresholdPct:0.#}%).");
        if (result.ForecastAgeHours is { } age && age > request.ForecastAgeThresholdHours)
            result.Reasons.Add($"The forecast behind this run is {age:0.#} h old (threshold {request.ForecastAgeThresholdHours:0.#} h).");

        result.ReoptimizeRecommended = result.Reasons.Count > 0;
        if (!result.ReoptimizeRecommended) return result;

        await RaiseRecommendationAsync(run, scenario.Scenario, request, result, ct);
        return result;
    }

    private sealed record Snapshot(double Lat, double Lng, DateTime Time, double? RobVlsfo, double? RobLsmgo);

    private async Task<Snapshot?> ResolveSnapshotAsync(OptimizationRun run, OptimizationCheckRequestDto request, OptimizationCheckResultDto result, CancellationToken ct)
    {
        if (request.PositionLat.HasValue && request.PositionLng.HasValue && request.PositionTimeUtc.HasValue)
        {
            result.Source = "Supplied position";
            return new Snapshot(request.PositionLat.Value, request.PositionLng.Value, request.PositionTimeUtc.Value, null, null);
        }

        var rows = await _perf.TracksheetRows.AsNoTracking().Where(r => r.VoyageId == run.VoyageId && !r.IsDefault).OrderByDescending(r => r.SortOrder).Take(40).ToListAsync(ct);
        foreach (var row in rows)
        {
            var rowLat = ParseCoordinate(row.Lat);
            var rowLng = ParseCoordinate(row.Lng);
            var rowTime = RealPerformanceProfile.ParseTimestamp(row.Date, row.Time);
            if (!rowLat.HasValue || !rowLng.HasValue || rowTime == DateTime.MinValue) continue;
            result.Source = $"Latest tracksheet report ({row.Date} {row.Time} UTC)";
            return new Snapshot(rowLat.Value, rowLng.Value, rowTime, row.VlsfoRob, row.LsmgoRob);
        }

        result.Notes.Add("No position was supplied and the voyage has no tracksheet report with a usable position and time.");
        return null;
    }

    private async Task RaiseRecommendationAsync(OptimizationRun run, string scenario, OptimizationCheckRequestDto request, OptimizationCheckResultDto result, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var open = await _db.OptimizationRecommendations
            .Where(r => r.OptimizationRunId == run.Id && r.Scenario == scenario && r.Status == "Pending").FirstOrDefaultAsync(ct);
        if (open != null)
        {
            open.ReasonsJson = JsonSerializer.Serialize(result.Reasons, Json);
            open.MetricsJson = JsonSerializer.Serialize(result, Json);
            open.LastEvaluatedAtUtc = now;
            await _db.SaveChangesAsync(ct);
            result.RecommendationAction = "updated";
            result.RecommendationId = open.Id;
            return;
        }

        var cutoff = now.AddMinutes(-Math.Max(0, request.CooldownMinutes));
        var recentlyResolved = await _db.OptimizationRecommendations
            .AnyAsync(r => r.OptimizationRunId == run.Id && r.Scenario == scenario && r.ResolvedAtUtc != null && r.ResolvedAtUtc > cutoff, ct);
        if (recentlyResolved)
        {
            result.RecommendationAction = "suppressed";
            result.Notes.Add($"A suggestion for this run was handled within the last {request.CooldownMinutes:0} min, so no new one was raised.");
            return;
        }

        var recommendation = new OptimizationRecommendation
        {
            TenantId = _user.TenantId ?? run.TenantId,
            OptimizationRunId = run.Id,
            Scenario = scenario,
            VoyageId = run.VoyageId,
            ReasonsJson = JsonSerializer.Serialize(result.Reasons, Json),
            MetricsJson = JsonSerializer.Serialize(result, Json),
        };
        _db.OptimizationRecommendations.Add(recommendation);
        await _db.SaveChangesAsync(ct);
        result.RecommendationAction = "created";
        result.RecommendationId = recommendation.Id;
    }

    public async Task<List<OptimizationRecommendationDto>> GetRecommendationsAsync(string voyageId, string? status, CancellationToken cancellationToken = default)
    {
        var query = _db.OptimizationRecommendations.AsNoTracking().Where(r => r.VoyageId == voyageId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);
        var rows = await query.OrderByDescending(r => r.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    public async Task<OptimizationRecommendationDto?> ResolveAsync(Guid recommendationId, bool confirm, CancellationToken cancellationToken = default)
    {
        var recommendation = await _db.OptimizationRecommendations.FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);
        if (recommendation == null || recommendation.Status != "Pending") return null;
        recommendation.Status = confirm ? "Confirmed" : "Dismissed";
        recommendation.ResolvedAtUtc = DateTime.UtcNow;
        recommendation.ResolvedByUserId = _user.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(recommendation);
    }

    private static OptimizationRecommendationDto ToDto(OptimizationRecommendation r) => new()
    {
        Id = r.Id, RunId = r.OptimizationRunId, Scenario = r.Scenario, VoyageId = r.VoyageId, Status = r.Status,
        Reasons = Deserialize<List<string>>(r.ReasonsJson) ?? new(),
        CreatedAtUtc = r.CreatedAtUtc, LastEvaluatedAtUtc = r.LastEvaluatedAtUtc, ResolvedAtUtc = r.ResolvedAtUtc,
    };

    // ------------------------------------------------------------------ actual vs predicted

    public async Task<PerformanceComparisonDto?> ComparePerformanceAsync(Guid runId, string? scenarioKey, CancellationToken cancellationToken = default)
    {
        var run = await _db.OptimizationRuns.AsNoTracking().Include(r => r.ScenarioResults).FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run == null) return null;
        var scenario = scenarioKey != null
            ? run.ScenarioResults.FirstOrDefault(s => string.Equals(s.Scenario, scenarioKey, StringComparison.OrdinalIgnoreCase))
            : run.ScenarioResults.FirstOrDefault(s => s.IsFeasible);

        var dto = new PerformanceComparisonDto { RunId = run.Id, VoyageId = run.VoyageId, Scenario = scenario?.Scenario ?? string.Empty };
        var waypoints = scenario == null ? new List<RouteWaypointDto>() : Deserialize<List<RouteWaypointDto>>(scenario.RouteWaypointsJson) ?? new();

        var voyageRows = await _perf.TracksheetRows.AsNoTracking().Where(r => r.VoyageId == run.VoyageId && !r.IsDefault).OrderBy(r => r.SortOrder).ToListAsync(cancellationToken);
        if (scenario == null || !scenario.IsFeasible || waypoints.Count < 2)
            dto.Notes.Add("The run has no feasible route to compare against.");
        else
            BuildPoints(dto, waypoints, voyageRows);

        if (!string.IsNullOrEmpty(run.VesselImo))
        {
            var vesselRows = await _perf.TracksheetRows.AsNoTracking().Where(r => r.VesselImo == run.VesselImo && !r.IsDefault).ToListAsync(cancellationToken);
            dto.Trend = RealPerformanceProfile.Trend(vesselRows, string.Equals(run.LoadCondition, "Laden", StringComparison.OrdinalIgnoreCase));
        }
        dto.Notes.Add("Read-only comparison: observed performance is shown for review and never overwrites the vessel's performance curves or the stored run.");
        if (dto.Trend.Count == 0) dto.Notes.Add("No weather-corrected noon-report operating points are available for the monthly trend.");
        return dto;
    }

    private static void BuildPoints(PerformanceComparisonDto dto, List<RouteWaypointDto> waypoints, List<TracksheetRow> rows)
    {
        for (var index = 1; index < rows.Count; index++)
        {
            var row = rows[index];
            var previous = rows[index - 1];
            var time = RealPerformanceProfile.ParseTimestamp(row.Date, row.Time);
            if (time == DateTime.MinValue || time < waypoints[0].EtaUtc || time > waypoints[^1].EtaUtc) continue;

            var segment = waypoints.FindIndex(1, waypoint => waypoint.EtaUtc >= time);
            if (segment < 1) continue;
            var span = (waypoints[segment].EtaUtc - waypoints[segment - 1].EtaUtc).TotalHours;
            var fraction = span > 0 ? (time - waypoints[segment - 1].EtaUtc).TotalHours / span : 0;

            double? actualFuel = null;
            if (row.Hrs is > 0 && previous.VlsfoRob.HasValue && row.VlsfoRob.HasValue && (row.VlsfoBunkered ?? 0) <= 0)
                actualFuel = Math.Max(0, (previous.VlsfoRob.Value - row.VlsfoRob.Value) / row.Hrs.Value * 24);

            dto.Points.Add(new PerformanceComparisonPointDto
            {
                TimeUtc = time,
                ActualSpeedKn = row.AvgSpeedO is > 0 ? row.AvgSpeedO : null,
                PredictedSogKn = waypoints[segment].SpeedKn,
                ActualRpm = row.Rpm is > 0 ? row.Rpm : null,
                PredictedRpm = waypoints[segment].Rpm,
                ActualPowerKw = row.EnginePower is > 0 ? row.EnginePower : null,
                PredictedPowerKw = waypoints[segment].PowerKw,
                ActualFuelMtPerDay = actualFuel,
                PredictedFuelMtPerDay = waypoints[segment].FuelRateMtPerDay,
                ActualRobVlsfoMt = row.VlsfoRob,
                PredictedRobVlsfoMt = Interpolate(waypoints[segment - 1].RobVlsfoMt, waypoints[segment].RobVlsfoMt, fraction),
            });
        }

        if (dto.Points.Count == 0)
        {
            dto.Notes.Add("No tracksheet reports fall inside the planned voyage period, so there is nothing to compare yet.");
            return;
        }

        double? Bias(Func<PerformanceComparisonPointDto, double?> actual, Func<PerformanceComparisonPointDto, double?> predicted)
        {
            var diffs = dto.Points.Where(p => actual(p).HasValue && predicted(p).HasValue).Select(p => actual(p)!.Value - predicted(p)!.Value).ToList();
            return diffs.Count > 0 ? diffs.Average() : null;
        }

        dto.SpeedBiasKn = Bias(p => p.ActualSpeedKn, p => p.PredictedSogKn);
        var speedErrors = dto.Points.Where(p => p.ActualSpeedKn.HasValue && p.PredictedSogKn.HasValue).Select(p => Math.Abs(p.ActualSpeedKn!.Value - p.PredictedSogKn!.Value)).ToList();
        dto.SpeedMeanAbsErrorKn = speedErrors.Count > 0 ? speedErrors.Average() : null;
        dto.RpmBias = Bias(p => p.ActualRpm, p => p.PredictedRpm);
        dto.PowerBiasKw = Bias(p => p.ActualPowerKw, p => p.PredictedPowerKw);
        dto.FuelBiasMtPerDay = Bias(p => p.ActualFuelMtPerDay, p => p.PredictedFuelMtPerDay);
        dto.RobVlsfoBiasMt = Bias(p => p.ActualRobVlsfoMt, p => p.PredictedRobVlsfoMt);
        var predictedFuel = dto.Points.Where(p => p.ActualFuelMtPerDay.HasValue && p.PredictedFuelMtPerDay is > 0).Select(p => p.PredictedFuelMtPerDay!.Value).ToList();
        if (dto.FuelBiasMtPerDay.HasValue && predictedFuel.Count > 0) dto.FuelBiasPct = dto.FuelBiasMtPerDay / predictedFuel.Average() * 100;
        dto.Notes.Add("Predicted values use the planned arc covering each report time. Positions are not time-aligned beyond that, so schedule slippage appears as a speed or ROB difference.");
    }

    // ------------------------------------------------------------------ geometry helpers

    internal static double? ParseCoordinate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = CoordinatePattern.Match(text.Trim());
        if (!match.Success) return null;
        var minutes = double.Parse(match.Groups[1].Value[^2..], System.Globalization.CultureInfo.InvariantCulture);
        var degrees = match.Groups[1].Value.Length > 2 ? double.Parse(match.Groups[1].Value[..^2], System.Globalization.CultureInfo.InvariantCulture) : 0;
        if (minutes >= 60) return null;
        var value = degrees + minutes / 60;
        return char.ToUpperInvariant(match.Groups[2].Value[0]) is 'S' or 'W' ? -value : value;
    }

    /// <summary>Nearest point on the stored route: distance along the route, cross-track distance (NM), segment index and fraction.</summary>
    internal static (double AlongNm, double CrossNm, int Segment, double Fraction) Project(List<RouteWaypointDto> waypoints, double lat, double lng)
    {
        var best = (Along: 0.0, Cross: double.MaxValue, Segment: 0, Fraction: 0.0);
        for (var index = 0; index < waypoints.Count - 1; index++)
        {
            var a = waypoints[index];
            var b = waypoints[index + 1];
            var scale = Math.Cos(a.Lat * Math.PI / 180) * 60;
            var abX = Wrap(b.Lng - a.Lng) * scale;
            var abY = (b.Lat - a.Lat) * 60;
            var apX = Wrap(lng - a.Lng) * scale;
            var apY = (lat - a.Lat) * 60;
            var lengthSq = abX * abX + abY * abY;
            var t = lengthSq < 1e-9 ? 0 : Math.Clamp((apX * abX + apY * abY) / lengthSq, 0, 1);
            var dx = apX - t * abX;
            var dy = apY - t * abY;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < best.Cross)
                best = (a.CumulativeDistanceNm + t * (b.CumulativeDistanceNm - a.CumulativeDistanceNm), distance, index, t);
        }
        return (best.Along, best.Cross, best.Segment, best.Fraction);
    }

    private static double Wrap(double deltaLng) => deltaLng > 180 ? deltaLng - 360 : deltaLng < -180 ? deltaLng + 360 : deltaLng;

    private static DateTime TimeAtSegment(List<RouteWaypointDto> waypoints, int segment, double fraction) =>
        waypoints[segment].EtaUtc + (waypoints[segment + 1].EtaUtc - waypoints[segment].EtaUtc) * fraction;

    private static double? Interpolate(double? from, double? to, double fraction) =>
        from.HasValue && to.HasValue ? from.Value + (to.Value - from.Value) * fraction : null;

    private static T? Deserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json, Json); }
        catch (JsonException) { return default; }
    }
}
