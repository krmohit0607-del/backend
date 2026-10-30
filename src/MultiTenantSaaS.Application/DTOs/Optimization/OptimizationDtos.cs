namespace MultiTenantSaaS.Application.DTOs.Optimization;

/// <summary>One no-go / speed-control / limited-passage / ECA zone relevant to the voyage corridor, supplied by the caller (the frontend already holds the full admin + voyage zone dataset).</summary>
public class RestrictedAreaDto
{
    public string Id { get; set; } = string.Empty;
    /// <summary>"no-go-zone" | "speed-control-zone" | "limited-passage-zone" | "eca-zone".</summary>
    public string ZoneType { get; set; } = string.Empty;
    /// <summary>One or more polygon rings, each an ordered list of [lat, lon] vertices.</summary>
    public List<List<double[]>> Rings { get; set; } = new();
    public double? RpmMin { get; set; }
    public double? RpmMax { get; set; }
    /// <summary>Speed limit in knots.</summary>
    public double? SpeedMaxKn { get; set; }
}

public class OptimizationRequestDto
{
    public string VoyageId { get; set; } = string.Empty;
    public string VesselImo { get; set; } = string.Empty;
    public List<string> Scenarios { get; set; } = new();

    public DateTime EtdUtc { get; set; }
    public double OriginLat { get; set; }
    public double OriginLng { get; set; }
    public double DestLat { get; set; }
    public double DestLng { get; set; }

    public string LoadCondition { get; set; } = "Laden"; // Ballast | Laden

    public double HireRatePerDayUsd { get; set; }
    public double FoPriceUsdPerMt { get; set; }
    public double GoPriceUsdPerMt { get; set; }
    public double EuaPriceUsdPerTonneCo2 { get; set; }

    /// <summary>Target arrival time — only used by the "SpecificEtaArrival" scenario.</summary>
    public DateTime? TargetEtaUtc { get; set; }

    /// <summary>Custom weights (0-1, need not sum to 1) — only used by the "CustomSimulation" scenario.</summary>
    public double? CustomWeightTime { get; set; }
    public double? CustomWeightFuel { get; set; }
    public double? CustomWeightEmission { get; set; }

    /// <summary>Max lateral deviation from the direct track the engine may search, in NM. Defaults to 60nm if omitted.</summary>
    public double? MaxDeviationNm { get; set; }

    public List<RestrictedAreaDto> RestrictedAreas { get; set; } = new();
}

public class RouteWaypointDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public DateTime EtaUtc { get; set; }
    public double SpeedKn { get; set; }
    public double CumulativeDistanceNm { get; set; }
}

public class OptimizationScenarioResultDto
{
    public string Scenario { get; set; } = string.Empty;
    public bool IsFeasible { get; set; } = true;
    public string? InfeasibleReason { get; set; }
    public bool WasConstrainedByZones { get; set; }

    public List<RouteWaypointDto> Waypoints { get; set; } = new();

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
}

public class OptimizationRunResultDto
{
    public Guid OptimizationRunId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public List<OptimizationScenarioResultDto> Scenarios { get; set; } = new();
}

/// <summary>Lightweight row for the "All Routes" history list (no route geometry/costs — fetch the full run for that).</summary>
public class OptimizationRunSummaryDto
{
    public Guid OptimizationRunId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime EtdUtc { get; set; }
    public string ScenariosRequestedJson { get; set; } = "[]";
}
