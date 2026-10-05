namespace MultiTenantSaaS.Application.DTOs.Chartering;

/// <summary>
/// Validation result for estimation inputs.
/// Contains errors and warnings that guide users on missing/invalid data.
/// </summary>
public class EstimationValidationResultDto
{
    public List<ValidationIssueDto> Issues { get; set; } = new();
    
    public bool IsValid => !Issues.Any(x => x.Level == "Error");
    
    public List<string> GetErrors()
        => Issues.Where(x => x.Level == "Error").Select(x => x.Message).ToList();

    public List<string> GetWarnings()
        => Issues.Where(x => x.Level == "Warning").Select(x => x.Message).ToList();
}

/// <summary>
/// Single validation issue (error or warning).
/// </summary>
public class ValidationIssueDto
{
    public string Field { get; set; } = "";
    public string Message { get; set; } = "";
    public string Level { get; set; } = "Warning"; // "Error" or "Warning"
}

/// <summary>
/// Bunker ROB calculation result across the entire voyage.
/// </summary>
public class BunkerRobCalculationResultDto
{
    public double InitialFOROB { get; set; }
    public double InitialDOROB { get; set; }
    public double FinalFOROB { get; set; }
    public double FinalDOROB { get; set; }
    
    public List<RobLegDetailDto> FoRobProgression { get; set; } = new();
    public List<RobLegDetailDto> DoRobProgression { get; set; } = new();
    public List<string> NegativeRobWarnings { get; set; } = new();
    
    public bool HasNegativeROB { get; set; }
}

/// <summary>
/// Per-leg ROB (Remaining On Board) detail.
/// </summary>
public class RobLegDetailDto
{
    public int LegNumber { get; set; }
    public string PortName { get; set; } = "";
    public double OpeningROB { get; set; }
    public double Consumption { get; set; }
    public double Supply { get; set; }
    public double ClosingROB { get; set; }
    public bool HasWarning { get; set; }
}

/// <summary>
/// Complete calculation details with per-leg breakdown and summary.
/// Used for audit trail and "expand details" feature.
/// </summary>
public class CalculationDetailResultDto
{
    public List<LegCalculationDetailDto> LegDetails { get; set; } = new();
    public CalculationSummaryDetailDto Summary { get; set; } = new();
}

/// <summary>
/// Per-leg calculation details: Distance → Speed → Days → Consumption → Cost.
/// </summary>
public class LegCalculationDetailDto
{
    public int LegNumber { get; set; }
    public string PortName { get; set; } = "";
    public string PortType { get; set; } = "";

    // Distance → Speed → Days progression
    public double Distance { get; set; }
    public double Speed { get; set; }
    public double WeatherFactor { get; set; }
    public double EffectiveSpeed { get; set; }
    public double CalculatedSeaDays { get; set; }
    public double EcaDays { get; set; }
    public double NormalDays { get; set; }

    // Consumption calculation
    public double ConsumptionRate { get; set; }
    public string ConsumptionRateUnit { get; set; } = "MT/day";
    public double CalculatedConsumption { get; set; }

    // Cost calculation
    public double FuelPrice { get; set; }
    public string FuelPriceUnit { get; set; } = "$/MT";
    public double BunkerCost { get; set; }

    // Port details
    public double PortIdleDays { get; set; }
    public double PortWorkDays { get; set; }
    public double Demurrage { get; set; }
    public double Despatch { get; set; }
    public double PortCharge { get; set; }
}

/// <summary>
/// Calculation summary: all totals and key voyage metrics.
/// </summary>
public class CalculationSummaryDetailDto
{
    // Voyage duration breakdown
    public double TotalSeaDays { get; set; }
    public double EcaDays { get; set; }
    public double BallastDays { get; set; }
    public double LadenDays { get; set; }
    public double PortDays { get; set; }
    public double VoyageDays { get; set; }

    // Distance breakdown
    public double TotalDistance { get; set; }
    public double EcaDistance { get; set; }

    // Bunker consumption and cost
    public double VlsfoCons { get; set; }
    public double UlsfoCons { get; set; }
    public double MgoCons { get; set; }
    public double VlsfoPrice { get; set; }
    public double UlsfoPrice { get; set; }
    public double MgoPrice { get; set; }
    public double VlsfoExp { get; set; }
    public double UlsfoExp { get; set; }
    public double MgoExp { get; set; }
    public double TotalBunkerExp { get; set; }

    // Revenue breakdown
    public double Freight { get; set; }
    public double AddComm { get; set; }
    public double Brokerage { get; set; }
    public double FreightTax { get; set; }
    public double TotalOpExpense { get; set; }
    public double Revenue { get; set; }

    // Profit and key metrics
    public double OpProfit { get; set; }
    public double NetHire { get; set; }
    public double TotalExpense { get; set; }
    public double Profit { get; set; }
    public double ProfitPerDay { get; set; }
    public double TCE { get; set; }
}
