using MultiTenantSaaS.Application.DTOs.Chartering;

namespace MultiTenantSaaS.Application.Services;

/// <summary>
/// Calculation details service - provides detailed breakdowns for audit/trace capability.
/// Shows distance → speed → days → consumption → cost progression.
/// </summary>
public interface ICalculationDetailsService
{
    CalculationDetailResult GenerateCalculationDetails(string dataJson, VoyageEstimateCalculationDto calculation);
}

public class CalculationDetailsService : ICalculationDetailsService
{
    public CalculationDetailResult GenerateCalculationDetails(string dataJson, VoyageEstimateCalculationDto calculation)
    {
        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(dataJson);
            if (data.ValueKind != System.Text.Json.JsonValueKind.Object)
                return new CalculationDetailResult();

            var ports = ExtractPorts(data);
            var performance = ExtractPerformance(data);
            var commercial = ExtractCommercial(data);

            var details = new CalculationDetailResult();

            // Generate per-leg detail breakdown
            for (int i = 0; i < ports.Count && i < calculation.PerLeg.Count; i++)
            {
                var port = ports[i];
                var leg = calculation.PerLeg[i];

                var legDetail = GenerateLegDetail(i + 1, port, leg, performance, commercial);
                details.LegDetails.Add(legDetail);
            }

            // Generate summary calculations
            details.Summary = GenerateSummary(calculation, ports, commercial);

            return details;
        }
        catch
        {
            return new CalculationDetailResult();
        }
    }

    private LegDetailDto GenerateLegDetail(int legNumber, PortDataExtract port, LegCalculationDto leg, PerformanceDataExtract? perf, CommercialDataExtract commercial)
    {
        var detail = new LegDetailDto
        {
            LegNumber = legNumber,
            PortName = port.Port,
            PortType = port.Type
        };

        // Distance → Speed → Sea Days
        if (port.Distance > 0)
        {
            var speed = port.Speed > 0 ? port.Speed : 12;
            var weatherFactor = port.Wf > 0 ? port.Wf : 0;
            var effSpeed = Math.Max(0.1, speed * (1 - weatherFactor / 100));

            detail.Distance = port.Distance;
            detail.Speed = speed;
            detail.WeatherFactor = weatherFactor;
            detail.EffectiveSpeed = Math.Round(effSpeed, 2);
            detail.CalculatedSeaDays = leg.Sea;
            detail.EcaDays = leg.Eca;
            detail.NormalDays = Math.Max(0, leg.Sea - leg.Eca);
        }

        // Sea Days → Consumption
        if (perf != null && leg.Sea > 0)
        {
            var mainCons = perf.MainNormal; // Simplified: use main normal
            var isBallast = port.Type == "Ballast" || port.Type == "Delivery" || port.Type == "Redelivery";
            var consumptionRate = isBallast ? mainCons.Ballast : mainCons.Laden;

            detail.ConsumptionRate = consumptionRate;
            detail.ConsumptionRateUnit = "MT/day";
            detail.CalculatedConsumption = Math.Round(leg.Sea * consumptionRate, 1);
        }

        // Consumption × Price → Cost
        if (commercial.VlsfoPrice > 0)
        {
            detail.FuelPrice = commercial.VlsfoPrice;
            detail.FuelPriceUnit = "$/MT";
            detail.BunkerCost = Math.Round(detail.CalculatedConsumption * commercial.VlsfoPrice);
        }

        // Port handling
        detail.PortIdleDays = port.Idle;
        detail.PortWorkDays = leg.Work;
        detail.Demurrage = leg.Dem;
        detail.Despatch = leg.Des;
        detail.PortCharge = port.PortCharge;

        return detail;
    }

    private CalculationSummaryDto GenerateSummary(VoyageEstimateCalculationDto calc, List<PortDataExtract> ports, CommercialDataExtract commercial)
    {
        var summary = new CalculationSummaryDto();

        // Voyage duration breakdown
        summary.TotalSeaDays = calc.SeaDays;
        summary.EcaDays = calc.EcaDays;
        summary.BallastDays = calc.BallastDays;
        summary.LadenDays = calc.LadenDays;
        summary.PortDays = calc.PortDays;
        summary.VoyageDays = calc.VoyageDays;

        // Distance
        summary.TotalDistance = calc.DistanceTotal;
        summary.EcaDistance = calc.EcaDistanceTotal;

        // Bunker breakdown
        summary.VlsfoCons = calc.VlsfoCons;
        summary.UlsfoCons = calc.UlsfoCons;
        summary.MgoCons = calc.MgoCons;
        summary.VlsfoPrice = commercial.VlsfoPrice;
        summary.UlsfoPrice = commercial.UlsfoPrice;
        summary.MgoPrice = commercial.MgoPrice;
        summary.VlsfoExp = calc.VlsfoExp;
        summary.UlsfoExp = calc.UlsfoExp;
        summary.MgoExp = calc.MgoExp;
        summary.TotalBunkerExp = calc.BunkerExpense;

        // Revenue breakdown
        summary.Freight = calc.Freight;
        summary.AddComm = calc.AddComm;
        summary.Brokerage = calc.Brokerage;
        summary.FreightTax = calc.FreightTax;
        summary.TotalOpExpense = calc.OpExpense;
        summary.Revenue = calc.Revenue;

        // Profit
        summary.OpProfit = calc.OpProfit;
        summary.NetHire = calc.NetHire;
        summary.TotalExpense = calc.TotalExpense;
        summary.Profit = calc.Profit;
        summary.ProfitPerDay = calc.Profit > 0 ? calc.Profit / Math.Max(1, calc.VoyageDays) : 0;
        summary.TCE = calc.Tce;

        return summary;
    }

    private List<PortDataExtract> ExtractPorts(System.Text.Json.JsonElement data)
    {
        var ports = new List<PortDataExtract>();
        if (!data.TryGetProperty("ports", out var portsEl) || portsEl.ValueKind != System.Text.Json.JsonValueKind.Array)
            return ports;

        foreach (var port in portsEl.EnumerateArray())
        {
            ports.Add(new PortDataExtract
            {
                Port = GetStringValue(port, "port"),
                Type = GetStringValue(port, "type"),
                Distance = GetDoubleValue(port, "distance"),
                EcaDistance = GetDoubleValue(port, "ecaDistance"),
                Wf = GetDoubleValue(port, "wf"),
                Speed = GetDoubleValue(port, "speed"),
                Idle = GetDoubleValue(port, "idle"),
                PortCharge = GetDoubleValue(port, "portCharge")
            });
        }
        return ports;
    }

    private PerformanceDataExtract? ExtractPerformance(System.Text.Json.JsonElement data)
    {
        if (!data.TryGetProperty("perf", out var perf))
            return null;

        return new PerformanceDataExtract
        {
            MainNormal = ExtractMainCons(perf, "mainNormal"),
            MainEca = ExtractMainCons(perf, "mainEca"),
        };
    }

    private MainConsExtract ExtractMainCons(System.Text.Json.JsonElement perf, string key)
    {
        if (!perf.TryGetProperty(key, out var el))
            return new MainConsExtract();

        return new MainConsExtract
        {
            Ballast = GetDoubleValue(el, "ballast"),
            Laden = GetDoubleValue(el, "laden"),
            Idle = GetDoubleValue(el, "idle"),
            Work = GetDoubleValue(el, "work")
        };
    }

    private CommercialDataExtract ExtractCommercial(System.Text.Json.JsonElement data)
    {
        if (!data.TryGetProperty("commercial", out var commercial))
            return new CommercialDataExtract();

        return new CommercialDataExtract
        {
            VlsfoPrice = GetDoubleValue(commercial, "vlsfoPrice"),
            UlsfoPrice = GetDoubleValue(commercial, "ulsfoPrice"),
            MgoPrice = GetDoubleValue(commercial, "mgoPrice"),
            DailyHire = GetDoubleValue(commercial, "dailyHire")
        };
    }

    private string GetStringValue(System.Text.Json.JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.String)
            return prop.GetString() ?? "";
        return "";
    }

    private double GetDoubleValue(System.Text.Json.JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.Number)
            return prop.GetDouble();
        return 0;
    }
}

/// <summary>
/// Complete calculation details with leg breakdowns and summary.
/// </summary>
public class CalculationDetailResult
{
    public List<LegDetailDto> LegDetails { get; set; } = new();
    public CalculationSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Per-leg calculation detail showing progression: Distance → Speed → Days → Consumption → Cost.
/// </summary>
public class LegDetailDto
{
    public int LegNumber { get; set; }
    public string PortName { get; set; } = "";
    public string PortType { get; set; } = "";

    // Distance → Speed → Days
    public double Distance { get; set; }
    public double Speed { get; set; }
    public double WeatherFactor { get; set; }
    public double EffectiveSpeed { get; set; }
    public double CalculatedSeaDays { get; set; }
    public double EcaDays { get; set; }
    public double NormalDays { get; set; }

    // Consumption
    public double ConsumptionRate { get; set; }
    public string ConsumptionRateUnit { get; set; } = "MT/day";
    public double CalculatedConsumption { get; set; }

    // Cost
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
/// Calculation summary showing all totals and key metrics.
/// </summary>
public class CalculationSummaryDto
{
    // Voyage duration
    public double TotalSeaDays { get; set; }
    public double EcaDays { get; set; }
    public double BallastDays { get; set; }
    public double LadenDays { get; set; }
    public double PortDays { get; set; }
    public double VoyageDays { get; set; }

    // Distance
    public double TotalDistance { get; set; }
    public double EcaDistance { get; set; }

    // Bunker
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

    // Revenue
    public double Freight { get; set; }
    public double AddComm { get; set; }
    public double Brokerage { get; set; }
    public double FreightTax { get; set; }
    public double TotalOpExpense { get; set; }
    public double Revenue { get; set; }

    // Profit
    public double OpProfit { get; set; }
    public double NetHire { get; set; }
    public double TotalExpense { get; set; }
    public double Profit { get; set; }
    public double ProfitPerDay { get; set; }
    public double TCE { get; set; }
}

// Supporting data classes
public class PortDataExtract
{
    public string Port { get; set; } = "";
    public string Type { get; set; } = "";
    public double Distance { get; set; }
    public double EcaDistance { get; set; }
    public double Wf { get; set; }
    public double Speed { get; set; }
    public double Idle { get; set; }
    public double PortCharge { get; set; }
}

public class PerformanceDataExtract
{
    public MainConsExtract MainNormal { get; set; } = new();
    public MainConsExtract MainEca { get; set; } = new();
}

public class MainConsExtract
{
    public double Ballast { get; set; }
    public double Laden { get; set; }
    public double Idle { get; set; }
    public double Work { get; set; }
}

public class CommercialDataExtract
{
    public double VlsfoPrice { get; set; }
    public double UlsfoPrice { get; set; }
    public double MgoPrice { get; set; }
    public double DailyHire { get; set; }
}
