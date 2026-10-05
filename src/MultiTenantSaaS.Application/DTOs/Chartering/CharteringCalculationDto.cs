namespace MultiTenantSaaS.Application.DTOs.Chartering;

/// <summary>
/// Extended Voyage Estimate DTO that includes full calculation results.
/// Mirrors the frontend EstimateResult structure.
/// </summary>
public class VoyageEstimateCalculationDto
{
    // Summary metrics
    public double Freight { get; set; }
    public double AddComm { get; set; }
    public double Brokerage { get; set; }
    public double FreightTax { get; set; }
    public double LinerTermTotal { get; set; }

    // Days breakdown
    public double SeaDays { get; set; }
    public double EcaDays { get; set; }
    public double LadenDays { get; set; }
    public double BallastDays { get; set; }
    public double IdleTotal { get; set; }
    public double WorkTotal { get; set; }
    public double PortDays { get; set; }
    public double VoyageDays { get; set; }

    // Distance
    public double DistanceTotal { get; set; }
    public double EcaDistanceTotal { get; set; }

    // Port charges and laytime
    public double PortCharge { get; set; }
    public double DemTotal { get; set; }
    public double DesTotal { get; set; }
    public double DemDes { get; set; }

    // Bunker consumption and expense
    public double VlsfoCons { get; set; }
    public double UlsfoCons { get; set; }
    public double MgoCons { get; set; }
    public double VlsfoExp { get; set; }
    public double UlsfoExp { get; set; }
    public double MgoExp { get; set; }
    public double BunkerExpense { get; set; }

    // Bunker adjustments (BOD/BOR)
    public double BodValue { get; set; }
    public double BorValue { get; set; }
    public double BunkerAdj { get; set; }

    // Operating expense and result
    public double OpExpense { get; set; }
    public double Revenue { get; set; }
    public double OpProfit { get; set; }
    public double NetHire { get; set; }
    public double TotalHire { get; set; }
    public double TotalExpense { get; set; }
    public double Profit { get; set; }
    public double ProfitPct { get; set; }
    public double Tce { get; set; }

    // Voyage date range
    public string? StartStr { get; set; }
    public string? EndStr { get; set; }

    // Per-leg breakdown
    public List<LegCalculationDto> PerLeg { get; set; } = new();
}

/// <summary>
/// Per-leg calculation details.
/// </summary>
public class LegCalculationDto
{
    public double Sea { get; set; }
    public double Eca { get; set; }
    public double Work { get; set; }
    public double Dem { get; set; }
    public double Des { get; set; }
    public string? Arrival { get; set; }
    public string? Departure { get; set; }
}

/// <summary>
/// Port and vessel data from the database for matching/enrichment.
/// </summary>
public class PortDataDto
{
    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Code { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class VesselTypeDataDto
{
    public string Type { get; set; } = string.Empty;
    public int? DefaultDwt { get; set; }
    public double? DefaultDraft { get; set; }
    public double? DefaultTpc { get; set; }
}

public class AccountDataDto
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? ContactEmail { get; set; }
}

/// <summary>
/// Loadable Quantity Calculation DTO
/// </summary>
public class LoadableQuantityDto
{
    public double SummerDwt { get; set; }
    public double Lightship { get; set; }
    public double DensityAtPort { get; set; }
    public double DeadweightAvailable { get; set; }
    public double VlsfoTons { get; set; }
    public double MgoTons { get; set; }
    public double FreshWaterTons { get; set; }
    public double ConstantsTons { get; set; }
    public double TotalDeductions { get; set; }
    public double LoadableQuantity { get; set; }
    public string? ConstrainingPoint { get; set; }
    public DateTime? CalculatedAt { get; set; }
}
