namespace MultiTenantSaaS.Application.DTOs.Optimization;

/// <summary>Position/time/bunker snapshot to compare against a stored run. Anything omitted is taken from the voyage's latest tracksheet report.</summary>
public class OptimizationCheckRequestDto
{
    public string? Scenario { get; set; }
    public double? PositionLat { get; set; }
    public double? PositionLng { get; set; }
    public DateTime? PositionTimeUtc { get; set; }
    public double? SpeedKn { get; set; }
    public double? RobVlsfoMt { get; set; }
    public double? RobLsmgoMt { get; set; }

    public double TimeDeviationThresholdHours { get; set; } = 3;
    public double CrossTrackThresholdNm { get; set; } = 15;
    public double RobDeviationThresholdPct { get; set; } = 10;
    public double ForecastAgeThresholdHours { get; set; } = 12;

    /// <summary>After an operator confirms or dismisses a suggestion for the same run, no new one is raised for this long.</summary>
    public double CooldownMinutes { get; set; } = 60;
}

public class OptimizationCheckResultDto
{
    public Guid RunId { get; set; }
    public string Scenario { get; set; } = string.Empty;
    public string VoyageId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime? PositionTimeUtc { get; set; }
    public double? PositionLat { get; set; }
    public double? PositionLng { get; set; }

    public double? CrossTrackNm { get; set; }
    /// <summary>Positive = behind the planned schedule at this point of the route.</summary>
    public double? ScheduleDeviationHours { get; set; }
    public double? PlannedDistanceAlongRouteNm { get; set; }
    public DateTime? PlannedEtaUtc { get; set; }
    /// <summary>Planned ETA shifted by the current schedule deviation (assumes the rest of the plan is sailed as predicted).</summary>
    public DateTime? ProjectedEtaUtc { get; set; }
    public double? SpeedDeviationKn { get; set; }
    public double? RobVlsfoDeviationPct { get; set; }
    public double? RobLsmgoDeviationPct { get; set; }
    public double? ForecastAgeHours { get; set; }

    public List<string> Reasons { get; set; } = new();
    public List<string> Notes { get; set; } = new();
    public bool ReoptimizeRecommended { get; set; }

    /// <summary>none | created | updated | suppressed</summary>
    public string RecommendationAction { get; set; } = "none";
    public Guid? RecommendationId { get; set; }
}

public class OptimizationRecommendationDto
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string Scenario { get; set; } = string.Empty;
    public string VoyageId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastEvaluatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class OperatingPointTrendDto
{
    public string Month { get; set; } = string.Empty;
    public double Rpm { get; set; }
    public int Samples { get; set; }
    public double MedianCalmSpeedKn { get; set; }
    public double? MedianPowerKw { get; set; }
    public double MedianMainFuelMtPerDay { get; set; }
}

public class PerformanceComparisonPointDto
{
    public DateTime TimeUtc { get; set; }
    public double? ActualSpeedKn { get; set; }
    public double? PredictedSogKn { get; set; }
    public double? ActualRpm { get; set; }
    public double? PredictedRpm { get; set; }
    public double? ActualPowerKw { get; set; }
    public double? PredictedPowerKw { get; set; }
    public double? ActualFuelMtPerDay { get; set; }
    public double? PredictedFuelMtPerDay { get; set; }
    public double? ActualRobVlsfoMt { get; set; }
    public double? PredictedRobVlsfoMt { get; set; }
}

public class PerformanceComparisonDto
{
    public Guid RunId { get; set; }
    public string Scenario { get; set; } = string.Empty;
    public string VoyageId { get; set; } = string.Empty;
    public List<PerformanceComparisonPointDto> Points { get; set; } = new();

    /// <summary>Mean of (actual - predicted); negative speed bias = slower than predicted.</summary>
    public double? SpeedBiasKn { get; set; }
    public double? SpeedMeanAbsErrorKn { get; set; }
    public double? RpmBias { get; set; }
    public double? PowerBiasKw { get; set; }
    public double? FuelBiasMtPerDay { get; set; }
    public double? FuelBiasPct { get; set; }
    public double? RobVlsfoBiasMt { get; set; }

    public List<OperatingPointTrendDto> Trend { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}
