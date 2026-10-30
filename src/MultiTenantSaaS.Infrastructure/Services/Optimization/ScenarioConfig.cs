using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>Per-scenario weight/constraint configuration the DAG solver uses — this is the "8 scenarios map to 8 configs of one real algorithm" design (not 8 separate algorithms), mirroring how the TNM OptiNav reference engine itself only varies its objective weights/constraints between runs.</summary>
internal record ScenarioConfig(
    string Key,
    double WeightTime,
    double WeightFuel,
    double WeightEmission,
    bool OptimizeRawFuelQuantity,
    bool OptimizeRawCo2Quantity,
    bool RestrictToSingleSpeed,
    bool TightenSafetyLimits,
    DateTime? TargetEtaUtc,
    double EtaPenaltyPerHourUsd)
{
    public static ScenarioConfig Resolve(string scenarioId, MultiTenantSaaS.Application.DTOs.Optimization.OptimizationRequestDto request, Vessel vessel)
    {
        switch (scenarioId)
        {
            case "earliest-eta":
                return new ScenarioConfig(scenarioId, 1, 0, 0, false, false, false, false, null, 0);
            case "lowest-fuel":
                return new ScenarioConfig(scenarioId, 0, 1, 0, true, false, false, false, null, 0);
            case "lowest-co2":
                return new ScenarioConfig(scenarioId, 0, 0, 1, false, true, false, false, null, 0);
            case "lowest-cost":
                return new ScenarioConfig(scenarioId, 1, 1, 1, false, false, false, false, null, 0);
            case "balanced":
                return new ScenarioConfig(scenarioId, 0.6, 1, 1, false, false, false, false, null, 0);
            case "safest":
                return new ScenarioConfig(scenarioId, 1, 1, 1, false, false, false, true, null, 0);
            case "maintain-cp":
                return new ScenarioConfig(scenarioId, 1, 1, 1, false, false, true, false, null, 0);
            case "specific-eta":
                return new ScenarioConfig(scenarioId, 1, 1, 1, false, false, false, false, request.TargetEtaUtc, 5000);
            case "custom":
                return new ScenarioConfig(scenarioId,
                    request.CustomWeightTime ?? 1, request.CustomWeightFuel ?? 1, request.CustomWeightEmission ?? 1,
                    false, false, false, false, null, 0);
            default:
                return new ScenarioConfig(scenarioId, 1, 1, 1, false, false, false, false, null, 0);
        }
    }
}
