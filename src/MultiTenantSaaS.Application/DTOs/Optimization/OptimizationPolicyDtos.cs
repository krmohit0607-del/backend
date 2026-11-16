namespace MultiTenantSaaS.Application.DTOs.Optimization;

public class EtsPhaseInDto
{
    public int Year { get; set; }
    public double Pct { get; set; }
}

public class EtsPolicyDto
{
    public double IntraEuCoveragePct { get; set; } = 100;
    public double ExtraEuCoveragePct { get; set; } = 50;
    public double UnknownCoveragePct { get; set; } = 100;
    public List<EtsPhaseInDto> PhaseIn { get; set; } = new()
    {
        new() { Year = 2024, Pct = 40 }, new() { Year = 2025, Pct = 70 }, new() { Year = 2026, Pct = 100 },
    };

    /// <summary>False when the tenant has not saved a policy yet; optimization then charges all CO2 at the EUA price.</summary>
    public bool IsConfigured { get; set; }
}

public class CalibrationFitRequestDto
{
    public string VesselImo { get; set; } = string.Empty;
    public string LoadCondition { get; set; } = "Laden";
    public List<OptimizationOperatingProfileDto> OperatingProfiles { get; set; } = new();
}

public class CalibrationMonthDto
{
    public string Month { get; set; } = string.Empty;
    public int Samples { get; set; }
    public double Factor { get; set; }
}

public class PerformanceCalibrationDto
{
    public Guid Id { get; set; }
    public string VesselImo { get; set; } = string.Empty;
    public string LoadCondition { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime FittedAtUtc { get; set; }
    public int SampleCount { get; set; }
    public int UsedSampleCount { get; set; }
    public DateTime? WindowFromUtc { get; set; }
    public DateTime? WindowToUtc { get; set; }
    public double FuelFactor { get; set; }
    public double SpreadPct { get; set; }
    public double? TrendPctPerMonth { get; set; }
    public string Quality { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<string> Notes { get; set; } = new();
    public List<CalibrationMonthDto> Monthly { get; set; } = new();
    public DateTime? ReviewedAtUtc { get; set; }
}
