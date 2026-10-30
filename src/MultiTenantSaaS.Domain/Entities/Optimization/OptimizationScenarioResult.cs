using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Optimization;

/// <summary>
/// The computed route + cost breakdown for one scenario within an
/// <see cref="OptimizationRun"/>. Field names mirror the Route Simulator's
/// optimized-route detail panel exactly (ETD, ETA, Distance, Average Speed,
/// Time to Go, ECA Distance/Time, fuel/cost breakdown, CO2) so the API
/// response can be bound straight into that UI.
/// </summary>
public class OptimizationScenarioResult : BaseEntity
{
    public Guid OptimizationRunId { get; set; }

    /// <summary>One of the 8 scenario keys (LowestVoyageCost, EarliestEta, SafestRoute, MaintainCpSpeed, CustomSimulation, LowestFuelConsumption, LowestCo2Emissions, BalancedOptimization, SpecificEtaArrival).</summary>
    public string Scenario { get; set; } = string.Empty;

    /// <summary>JSON array of [{lat,lng,etaUtc,speedKn,cumulativeDistanceNm}] in path order.</summary>
    public string RouteWaypointsJson { get; set; } = "[]";

    public DateTime EtdUtc { get; set; }
    public DateTime EtaUtc { get; set; }
    public double DistanceNm { get; set; }
    public double AverageSpeedKn { get; set; }
    public double TimeToGoHours { get; set; }
    public double EcaDistanceNm { get; set; }
    public double EcaTimeToGoHours { get; set; }
    public double MeFuelRequiredToGoMt { get; set; }
    public double EcaFuelRequiredToGoMt { get; set; }
    public double TotalCostToGoUsd { get; set; }
    public double FuelCostToGoUsd { get; set; }
    public double TimeCostToGoUsd { get; set; }
    public double EmissionsCostToGoUsd { get; set; }
    public double MeCo2EmissionsToGoMt { get; set; }

    /// <summary>True if any arc had to hard-exclude a no-go zone / clamp to a speed-control zone — surfaced so the UI can flag "route constrained by zone X".</summary>
    public bool WasConstrainedByZones { get; set; }

    public bool IsFeasible { get; set; } = true;
    public string? InfeasibleReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual OptimizationRun? OptimizationRun { get; set; }
}
