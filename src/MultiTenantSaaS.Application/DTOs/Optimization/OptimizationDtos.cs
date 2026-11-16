namespace MultiTenantSaaS.Application.DTOs.Optimization;

/// <summary>One no-go / speed-control / limited-passage / ECA zone relevant to the voyage corridor, supplied by the caller (the frontend already holds the full admin + voyage zone dataset).</summary>
public class RestrictedAreaDto
{
    public string Id { get; set; } = string.Empty;
    /// <summary>"no-go-zone" | "speed-control-zone" | "limited-passage-zone" | "eca-zone".</summary>
    public string ZoneType { get; set; } = string.Empty;
    /// <summary>One or more polygon rings in WGS84 GeoJSON order, each an ordered list of [longitude, latitude] vertices.</summary>
    public List<List<double[]>> Rings { get; set; } = new();
    public double? RpmMin { get; set; }
    public double? RpmMax { get; set; }
    /// <summary>Speed limit in knots.</summary>
    public double? SpeedMaxKn { get; set; }
}

public class OptimizationOperatingProfileDto
{
    public string Mode { get; set; } = string.Empty;
    public bool IsCpDefault { get; set; }
    public string LoadCondition { get; set; } = string.Empty;
    public double SpeedKn { get; set; }
    public double? Rpm { get; set; }
    public double? PowerKw { get; set; }
    public double? McrPercent { get; set; }
    public string FuelType1 { get; set; } = string.Empty;
    public double DailyCons1Mt { get; set; }
    public string FuelType2 { get; set; } = string.Empty;
    public double DailyCons2Mt { get; set; }
    public string FuelType3 { get; set; } = string.Empty;
    public double DailyCons3Mt { get; set; }
}

public class OptimizationRequestDto
{
    public string VoyageId { get; set; } = string.Empty;
    public string VesselImo { get; set; } = string.Empty;

    /// <summary>Used to find the vessel when no IMO is given or the IMO is not registered.</summary>
    public string? VesselName { get; set; }

    /// <summary>The voyage's own deadweight; the only vessel value needed when the vessel is not in the vessel master.</summary>
    public double? DeadweightMt { get; set; }
    public List<string> Scenarios { get; set; } = new();

    public DateTime EtdUtc { get; set; }
    public double OriginLat { get; set; }
    public double OriginLng { get; set; }
    public double DestLat { get; set; }
    public double DestLng { get; set; }

    /// <summary>Selected remaining route geometry as WGS84 GeoJSON positions in [longitude, latitude] order.</summary>
    public List<double[]> RouteCoordinates { get; set; } = new();

    public string LoadCondition { get; set; } = "Laden"; // Ballast | Laden

    /// <summary>ACTUAL (default) uses the vessel's observed noon-report performance; CP uses the configured Speed &amp; Cons rows. Maintain CP and Custom Simulation always use configured rows.</summary>
    public string PerformanceBasis { get; set; } = "ACTUAL";
    public List<OptimizationOperatingProfileDto> OperatingProfiles { get; set; } = new();

    public double HireRatePerDayUsd { get; set; }
    public double FoPriceUsdPerMt { get; set; }
    public double GoPriceUsdPerMt { get; set; }
    public double EuaPriceUsdPerTonneCo2 { get; set; }

    /// <summary>Arrival rule: ON, BETWEEN, BEFORE, or AFTER. Applied when the SpecificEtaArrival/JIT scenario is requested.</summary>
    public string ArrivalRule { get; set; } = "ON";

    /// <summary>ON target, BETWEEN window start, BEFORE deadline, or AFTER earliest permitted arrival.</summary>
    public DateTime? TargetEtaUtc { get; set; }

    /// <summary>Inclusive end of the arrival window; required only for BETWEEN.</summary>
    public DateTime? ArrivalWindowEndUtc { get; set; }

    /// <summary>Custom weights (0-1, need not sum to 1) — only used by the "CustomSimulation" scenario.</summary>
    public double? CustomWeightTime { get; set; }
    public double? CustomWeightFuel { get; set; }
    public double? CustomWeightEmission { get; set; }
    public double? CustomRpmMin { get; set; }
    public double? CustomRpmMax { get; set; }
    public double? CustomMcrMin { get; set; }
    public double? CustomMcrMax { get; set; }
    public double? CustomDraftM { get; set; }
    public double? CustomDisplacementMt { get; set; }
    public double? CustomGmM { get; set; }

    /// <summary>Required time of arrival for Custom Simulation (arrive by this time; early arrival means waiting).</summary>
    public DateTime? CustomRtaUtc { get; set; }

    /// <summary>Vessel natural roll period in seconds; when absent it is derived from the custom GM if available.</summary>
    public double? RollPeriodSec { get; set; }

    /// <summary>The voyage's weather margin (%): the latest ETA is computed with average speed x (1 - WM%).</summary>
    public double? WeatherMarginPct { get; set; }

    /// <summary>Minimum bunker reserves that must remain on board at every waypoint.</summary>
    public double? MinReserveVlsfoMt { get; set; }
    public double? MinReserveLsmgoMt { get; set; }

    /// <summary>Remaining bunkers at the route start; when absent the latest tracksheet ROB for the voyage is used.</summary>
    public double? RobVlsfoMt { get; set; }
    public double? RobLsmgoMt { get; set; }

    /// <summary>Relative importance of each objective when ranking scenario results (need not sum to 1).</summary>
    public RankingWeightsDto? RankingWeights { get; set; }

    /// <summary>Max lateral deviation from the direct track the engine may search, in NM. Defaults to 60nm if omitted.</summary>
    public double? MaxDeviationNm { get; set; }

    /// <summary>Wall-clock budget per scenario for the detailed search; when reached the best coarse-stage route is returned. Defaults to 90 s.</summary>
    public double? SolverTimeLimitSeconds { get; set; }

    /// <summary>Re-evaluate each chosen route over wind ensemble members when the ensemble service is available.</summary>
    public bool IncludeEnsemble { get; set; } = true;

    public List<RestrictedAreaDto> RestrictedAreas { get; set; } = new();
}

public class RankingWeightsDto
{
    public double Time { get; set; } = 1;
    public double Fuel { get; set; } = 1;
    public double Cost { get; set; } = 1;
    public double Emissions { get; set; } = 1;
    public double Weather { get; set; } = 1;
}

public class RouteWaypointDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public DateTime EtaUtc { get; set; }
    public double SpeedKn { get; set; }
    public double CumulativeDistanceNm { get; set; }
    public double? Rpm { get; set; }
    public double? PowerKw { get; set; }
    public double? FuelRateMtPerDay { get; set; }
    public double? RobVlsfoMt { get; set; }
    public double? RobLsmgoMt { get; set; }
}

public class OptimizationAnalysisDto
{
    public string PerformanceBasis { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = "3";
    /// <summary>Which speed-power model produced this result and its validity range and validation status.</summary>
    public string PerformanceModel { get; set; } = string.Empty;
    public string ForecastSource { get; set; } = "Open-Meteo forecast and marine APIs";
    public DateTime? ForecastValidFromUtc { get; set; }
    public DateTime? ForecastValidToUtc { get; set; }
    public DateTime? ForecastFetchedAtUtc { get; set; }

    /// <summary>Extra passage time caused by forecast wind and waves versus calm water, same currents.</summary>
    public double WeatherDelayHours { get; set; }
    public DateTime? EtaEarliestUtc { get; set; }
    public DateTime? EtaLatestUtc { get; set; }
    public string? EtaRangeBasis { get; set; }

    public double MaxEngineLoadPct { get; set; }
    public double HoursOverVesselWeatherLimit { get; set; }
    public double HoursSynchronousRollRisk { get; set; }
    public double HoursParametricRollRisk { get; set; }
    public double HoursPitchResonanceRisk { get; set; }
    public double HoursPowerBoosted { get; set; }
    public double WeatherExposureScore { get; set; }
    public double TotalFuelMt { get; set; }

    public double? StartRobVlsfoMt { get; set; }
    public double? StartRobLsmgoMt { get; set; }
    public double? ArrivalRobVlsfoMt { get; set; }
    public double? ArrivalRobLsmgoMt { get; set; }
    public bool? ReserveSatisfied { get; set; }
    public List<string> SuppliesApplied { get; set; } = new();
    public List<string> SuppliesNotCounted { get; set; } = new();

    /// <summary>Ports near the route ranked by detour cost. All ports use the run's fuel prices; availability and supplier quotes are not checked.</summary>
    public List<BunkerPortOptionDto> BunkerPortOptions { get; set; } = new();
    public string? BunkerPortBasis { get; set; }

    public int ParetoRank { get; set; }
    public bool IsParetoOptimal { get; set; }
    public double? RankingScore { get; set; }
    public string? RankingExplanation { get; set; }

    public SolverStatusDto Solver { get; set; } = new();
    public SavingsDto? Savings { get; set; }
    public EmissionsChargeDto? Emissions { get; set; }
    public List<FuelGradeTotalDto> FuelByGrade { get; set; } = new();
    public UncertaintyDto? Uncertainty { get; set; }
    /// <summary>Approved calibration correction applied to configured (CP) values, if any.</summary>
    public string? CalibrationApplied { get; set; }
    public List<SegmentAuditDto> Segments { get; set; } = new();
}

public class BunkerPortOptionDto
{
    public string Port { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double DistanceOffRouteNm { get; set; }
    /// <summary>Out-and-back straight-line detour.</summary>
    public double ExtraDistanceNm { get; set; }
    public double ExtraHours { get; set; }
    public double ExtraFuelMt { get; set; }
    public double ExtraCostUsd { get; set; }
    public double AlongRouteNm { get; set; }
    public DateTime EtaAtClosestPointUtc { get; set; }
    public double? RobVlsfoAtPointMt { get; set; }
    public double? RobLsmgoAtPointMt { get; set; }
    public bool HasBookedSupply { get; set; }
}

public class OptimizationScenarioResultDto
{
    public string Scenario { get; set; } = string.Empty;
    public bool IsFeasible { get; set; } = true;
    public string? InfeasibleReason { get; set; }
    public List<string> Warnings { get; set; } = new();
    public OptimizationAnalysisDto Analysis { get; set; } = new();
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
