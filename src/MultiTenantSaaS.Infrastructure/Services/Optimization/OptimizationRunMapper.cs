using System.Text.Json;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities.Optimization;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>Rebuilds the API result for a stored run.</summary>
public static class OptimizationRunMapper
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static OptimizationRunResultDto ToDto(OptimizationRun run) => new()
    {
        OptimizationRunId = run.Id,
        RequestedAtUtc = run.RequestedAtUtc,
        Scenarios = run.ScenarioResults.Select(s => new OptimizationScenarioResultDto
        {
            Scenario = s.Scenario,
            IsFeasible = s.IsFeasible,
            InfeasibleReason = s.InfeasibleReason,
            WasConstrainedByZones = s.WasConstrainedByZones,
            Warnings = Read<List<string>>(s.WarningsJson) ?? new(),
            Analysis = Read<OptimizationAnalysisDto>(s.AnalysisJson) ?? new(),
            Waypoints = Read<List<RouteWaypointDto>>(s.RouteWaypointsJson) ?? new(),
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

    private static T? Read<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json, Json); }
        catch (JsonException) { return default; }
    }
}
