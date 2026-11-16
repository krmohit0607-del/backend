using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>Read-only access to stored, verified optimization results for the AI assistant. The assistant may explain and compare these numbers but never produce its own.</summary>
public static class OptimizationResultsReader
{
    public static async Task<object> GetResultsAsync(IApplicationDbContext db, string? voyageId, Guid? runId, CancellationToken ct)
    {
        var run = await FindRunAsync(db, voyageId, runId, ct);
        if (run == null) return new { error = "No stored optimization run was found for that voyage or run id. Ask the user to run Optimize first." };

        return new
        {
            runId = run.Id,
            voyageId = run.VoyageId,
            requestedAtUtc = run.RequestedAtUtc,
            etdUtc = run.EtdUtc,
            note = "Stored, verified engine output. Quote these values as given; do not recalculate. Vessel weather limits are for reference only and were not enforced.",
            scenarios = run.ScenarioResults.OrderBy(s => s.Scenario).Select(Summarise).ToList(),
        };
    }

    public static async Task<object> CompareAsync(IApplicationDbContext db, string? voyageId, Guid? runId, CancellationToken ct)
    {
        var run = await FindRunAsync(db, voyageId, runId, ct);
        if (run == null) return new { error = "No stored optimization run was found for that voyage or run id." };

        var feasible = run.ScenarioResults.Where(s => s.IsFeasible).ToList();
        var infeasible = run.ScenarioResults.Where(s => !s.IsFeasible).Select(s => new { scenario = s.Scenario, reason = s.InfeasibleReason }).ToList();
        if (feasible.Count < 2)
            return new { runId = run.Id, error = "At least two feasible scenarios are needed to compare routes.", infeasibleScenarios = infeasible };

        double Fuel(Domain.Entities.Optimization.OptimizationScenarioResult s) => s.MeFuelRequiredToGoMt;
        var fastest = feasible.MinBy(s => s.TimeToGoHours)!;
        var cheapest = feasible.MinBy(s => s.TotalCostToGoUsd)!;
        var leanest = feasible.MinBy(Fuel)!;
        var cleanest = feasible.MinBy(s => s.MeCo2EmissionsToGoMt)!;

        var rows = feasible.Select(s => new
        {
            scenario = s.Scenario,
            etaUtc = s.EtaUtc,
            timeToGoHours = Math.Round(s.TimeToGoHours, 1),
            hoursVsFastest = Math.Round(s.TimeToGoHours - fastest.TimeToGoHours, 1),
            mainEngineFuelMt = Math.Round(Fuel(s), 1),
            fuelMtVsLowest = Math.Round(Fuel(s) - Fuel(leanest), 1),
            totalCostUsd = Math.Round(s.TotalCostToGoUsd),
            costUsdVsCheapest = Math.Round(s.TotalCostToGoUsd - cheapest.TotalCostToGoUsd),
            co2Mt = Math.Round(s.MeCo2EmissionsToGoMt, 1),
            co2MtVsLowest = Math.Round(s.MeCo2EmissionsToGoMt - cleanest.MeCo2EmissionsToGoMt, 1),
            analysis = Analysis(s),
        }).ToList();

        return new
        {
            runId = run.Id,
            voyageId = run.VoyageId,
            fastestScenario = fastest.Scenario,
            lowestCostScenario = cheapest.Scenario,
            lowestFuelScenario = leanest.Scenario,
            lowestCo2Scenario = cleanest.Scenario,
            scenarios = rows,
            infeasibleScenarios = infeasible,
            note = "Differences are computed from stored results. Explain trade-offs from these numbers only.",
        };
    }

    private static async Task<Domain.Entities.Optimization.OptimizationRun?> FindRunAsync(IApplicationDbContext db, string? voyageId, Guid? runId, CancellationToken ct)
    {
        var query = db.OptimizationRuns.AsNoTracking().Include(r => r.ScenarioResults);
        if (runId.HasValue) return await query.FirstOrDefaultAsync(r => r.Id == runId.Value, ct);
        if (string.IsNullOrWhiteSpace(voyageId)) return null;
        return await query.Where(r => r.VoyageId == voyageId).OrderByDescending(r => r.RequestedAtUtc).FirstOrDefaultAsync(ct);
    }

    private static object Summarise(Domain.Entities.Optimization.OptimizationScenarioResult s)
    {
        var warnings = Deserialize<List<string>>(s.WarningsJson) ?? new();
        return new
        {
            scenario = s.Scenario,
            feasible = s.IsFeasible,
            infeasibleReason = s.InfeasibleReason,
            etdUtc = s.EtdUtc,
            etaUtc = s.IsFeasible ? s.EtaUtc : (DateTime?)null,
            timeToGoHours = Math.Round(s.TimeToGoHours, 1),
            distanceNm = Math.Round(s.DistanceNm, 1),
            averageSpeedKn = Math.Round(s.AverageSpeedKn, 2),
            mainEngineFuelMt = Math.Round(s.MeFuelRequiredToGoMt, 1),
            ecaFuelMt = Math.Round(s.EcaFuelRequiredToGoMt, 1),
            totalCostUsd = Math.Round(s.TotalCostToGoUsd),
            fuelCostUsd = Math.Round(s.FuelCostToGoUsd),
            timeCostUsd = Math.Round(s.TimeCostToGoUsd),
            emissionsCostUsd = Math.Round(s.EmissionsCostToGoUsd),
            co2Mt = Math.Round(s.MeCo2EmissionsToGoMt, 1),
            constrainedByZones = s.WasConstrainedByZones,
            analysis = Analysis(s),
            warnings = warnings.Take(12).ToList(),
            warningsTruncated = warnings.Count > 12,
        };
    }

    private static object Analysis(Domain.Entities.Optimization.OptimizationScenarioResult s)
    {
        var a = Deserialize<OptimizationAnalysisDto>(s.AnalysisJson) ?? new();
        return new
        {
            performanceBasis = a.PerformanceBasis,
            etaEarliestUtc = a.EtaEarliestUtc,
            etaLatestUtc = a.EtaLatestUtc,
            etaRangeBasis = a.EtaRangeBasis,
            weatherDelayHours = Math.Round(a.WeatherDelayHours, 1),
            hoursOverVesselWeatherLimit = Math.Round(a.HoursOverVesselWeatherLimit, 1),
            hoursSynchronousRollRisk = Math.Round(a.HoursSynchronousRollRisk, 1),
            hoursParametricRollRisk = Math.Round(a.HoursParametricRollRisk, 1),
            maxEngineLoadPct = a.MaxEngineLoadPct,
            arrivalRobVlsfoMt = a.ArrivalRobVlsfoMt,
            arrivalRobLsmgoMt = a.ArrivalRobLsmgoMt,
            reserveSatisfied = a.ReserveSatisfied,
            paretoRank = a.ParetoRank,
            isParetoOptimal = a.IsParetoOptimal,
            rankingScore = a.RankingScore.HasValue ? Math.Round(a.RankingScore.Value, 3) : (double?)null,
            rankingExplanation = a.RankingExplanation,
            forecastFetchedAtUtc = a.ForecastFetchedAtUtc,
            performanceModel = a.PerformanceModel,
            solverStatus = a.Solver.Status,
            savingsVsPlannedRoute = a.Savings,
            fuelByGrade = a.FuelByGrade,
            emissions = a.Emissions,
            windUncertaintyPartial = a.Uncertainty,
            calibrationApplied = a.CalibrationApplied,
        };
    }

    private static T? Deserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { return default; }
    }
}
