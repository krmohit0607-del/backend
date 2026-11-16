namespace MultiTenantSaaS.Application.DTOs.Optimization;

/// <summary>One audited step of the chosen route: a sailing sub-step (forecast re-sampled at its position and time) or a wait.</summary>
public class SegmentAuditDto
{
    public int Index { get; set; }
    /// <summary>Sail | Wait</summary>
    public string Kind { get; set; } = "Sail";
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public double EndLat { get; set; }
    public double EndLng { get; set; }
    public double DistanceNm { get; set; }
    public double Hours { get; set; }
    public double StwKn { get; set; }
    public double SogKn { get; set; }
    public string Setting { get; set; } = string.Empty;
    /// <summary>HoldRpm | Boost</summary>
    public string PowerMode { get; set; } = string.Empty;
    public double? Rpm { get; set; }
    public double? PowerKw { get; set; }
    public double? LoadPct { get; set; }
    public double MainEngineFuelMt { get; set; }
    public string MainEngineGrade { get; set; } = string.Empty;
    public double AuxFuelMt { get; set; }
    public string AuxGrade { get; set; } = "LSMGO";
    public double Co2Mt { get; set; }
    public double ChargeableCo2Mt { get; set; }
    public double WindKn { get; set; }
    public double WindFromDeg { get; set; }
    public double WaveM { get; set; }
    public double WaveFromDeg { get; set; }
    public double CurrentKn { get; set; }
    public double CurrentToDeg { get; set; }
    public bool InEca { get; set; }
    public double ExposureScore { get; set; }
    public bool ExceedsVesselWeatherLimit { get; set; }
    public double? RobVlsfoEndMt { get; set; }
    public double? RobLsmgoEndMt { get; set; }
}

public class FuelGradeTotalDto
{
    public string Grade { get; set; } = string.Empty;
    public double Mt { get; set; }
    public double Co2Mt { get; set; }
    public double CostUsd { get; set; }
    /// <summary>Tank-to-wake CO2 factor, t CO2 per t fuel.</summary>
    public double EmissionFactor { get; set; }
}

public class EmissionsChargeDto
{
    public double TotalCo2Mt { get; set; }
    public double ChargeableCo2Mt { get; set; }
    /// <summary>Intra-EU | Extra-EU | Outside EU | Unknown</summary>
    public string VoyageClassification { get; set; } = "Unknown";
    public double CoveragePct { get; set; }
    public string PhaseIn { get; set; } = string.Empty;
    public string Basis { get; set; } = string.Empty;
}

public class SavingsDto
{
    public string Baseline { get; set; } = string.Empty;
    public string BaselinePerformanceBasis { get; set; } = "CP";
    public DateTime BaselineEtaUtc { get; set; }
    public double BaselineTimeHours { get; set; }
    public double BaselineFuelMt { get; set; }
    public double BaselineCostUsd { get; set; }
    public double BaselineCo2Mt { get; set; }
    /// <summary>Positive = this route is better than the planned route.</summary>
    public double FuelSavedMt { get; set; }
    public double CostSavedUsd { get; set; }
    public double TimeSavedHours { get; set; }
    public double Co2SavedMt { get; set; }
}

public class SolverStatusDto
{
    /// <summary>Completed | TimeLimitReached | Infeasible</summary>
    public string Status { get; set; } = "Completed";
    public string Method { get; set; } = string.Empty;
    public double TimeLimitSeconds { get; set; }
    public long ElapsedMs { get; set; }
    public long Stage1ElapsedMs { get; set; }
    public long Stage2ElapsedMs { get; set; }
    public long SegmentEvaluations { get; set; }
    public int Stage1Settings { get; set; }
    public int Stage2Settings { get; set; }
    public int Stage2Nodes { get; set; }
    public int WaitingStepsInRoute { get; set; }
    public List<string> Notes { get; set; } = new();
}

public class UncertaintyDto
{
    public string Source { get; set; } = string.Empty;
    public int Members { get; set; }
    /// <summary>True: only wind is perturbed; waves and currents are the single deterministic forecast.</summary>
    public bool Partial { get; set; } = true;
    public DateTime? EtaP10Utc { get; set; }
    public DateTime? EtaP50Utc { get; set; }
    public DateTime? EtaP90Utc { get; set; }
    public double FuelP10Mt { get; set; }
    public double FuelP50Mt { get; set; }
    public double FuelP90Mt { get; set; }
    public double CostP10Usd { get; set; }
    public double CostP50Usd { get; set; }
    public double CostP90Usd { get; set; }
    public double? ProbabilityMissTargetEta { get; set; }
    public double? ProbabilityReserveShortfall { get; set; }
    public List<string> Notes { get; set; } = new();
}
