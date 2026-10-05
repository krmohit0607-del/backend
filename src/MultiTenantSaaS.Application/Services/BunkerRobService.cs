using MultiTenantSaaS.Application.DTOs.Chartering;

namespace MultiTenantSaaS.Application.Services;

/// <summary>
/// Bunker ROB (Remaining On Board) tracking service.
/// Tracks fuel remaining throughout the voyage, prevents negative ROB.
/// </summary>
public interface IBunkerRobService
{
    BunkerRobCalculationResult CalculateROB(string dataJson, VoyageEstimateCalculationDto calculation);
}

public class BunkerRobService : IBunkerRobService
{
    public BunkerRobCalculationResult CalculateROB(string dataJson, VoyageEstimateCalculationDto calculation)
    {
        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(dataJson);
            if (data.ValueKind != System.Text.Json.JsonValueKind.Object)
                return new BunkerRobCalculationResult();

            var commercial = ExtractCommercial(data);
            var ports = ExtractPorts(data);

            return CalculateROBProgression(commercial, ports, calculation);
        }
        catch
        {
            return new BunkerRobCalculationResult();
        }
    }

    private BunkerRobCalculationResult CalculateROBProgression(CommercialData commercial, List<PortData> ports, VoyageEstimateCalculationDto calc)
    {
        var result = new BunkerRobCalculationResult
        {
            InitialFOROB = commercial.BodQty,
            InitialDOROB = commercial.BodQty * 0.1, // Assume 10% DO
            FoRobProgression = new List<RobLegDto>(),
            DoRobProgression = new List<RobLegDto>(),
            NegativeRobWarnings = new List<string>()
        };

        double foRob = commercial.BodQty;
        double doRob = commercial.BodQty * 0.1;
        var cursor = 0;

        // If calculation has per-leg breakdown, use it
        if (calc.PerLeg != null)
        {
            for (int i = 0; i < calc.PerLeg.Count && i < ports.Count; i++)
            {
                var leg = calc.PerLeg[i];
                var port = ports[i];

                // Estimate consumption for this leg (simplified)
                double legFOCons = 0, legDOCons = 0;
                if (leg.Sea > 0)
                {
                    legFOCons = leg.Sea * 25; // Avg 25 MT/day (varies by vessel)
                    legDOCons = leg.Sea * 2.5; // Avg 2.5 MT/day
                }
                legFOCons += leg.Work * 5; // Port work consumption

                foRob -= legFOCons;
                doRob -= legDOCons;

                // Bunker supply at port (if configured)
                var supplyFO = port.Distance > 0 ? 0 : 0; // Simplified: no supply unless configured
                foRob += supplyFO;

                if (foRob < 0)
                    result.NegativeRobWarnings.Add($"Port {i + 1} ({port.Port}): Negative FO ROB of {foRob:F1} MT");

                if (doRob < 0)
                    result.NegativeRobWarnings.Add($"Port {i + 1} ({port.Port}): Negative DO ROB of {doRob:F1} MT");

                result.FoRobProgression.Add(new RobLegDto
                {
                    LegNumber = i + 1,
                    PortName = port.Port,
                    OpeningROB = foRob + legFOCons,
                    Consumption = legFOCons,
                    Supply = supplyFO,
                    ClosingROB = Math.Max(0, foRob),
                    HasWarning = foRob < 0
                });

                result.DoRobProgression.Add(new RobLegDto
                {
                    LegNumber = i + 1,
                    PortName = port.Port,
                    OpeningROB = doRob + legDOCons,
                    Consumption = legDOCons,
                    Supply = 0,
                    ClosingROB = Math.Max(0, doRob),
                    HasWarning = doRob < 0
                });

                cursor++;
            }
        }

        result.FinalFOROB = Math.Max(0, foRob);
        result.FinalDOROB = Math.Max(0, doRob);

        return result;
    }

    private CommercialData ExtractCommercial(System.Text.Json.JsonElement data)
    {
        if (!data.TryGetProperty("commercial", out var commercial))
            return new CommercialData();

        return new CommercialData
        {
            BodQty = GetDoubleValue(commercial, "bodQty"),
            BodPrice = GetDoubleValue(commercial, "bodPrice"),
            BorQty = GetDoubleValue(commercial, "borQty"),
            BorPrice = GetDoubleValue(commercial, "borPrice"),
            VlsfoPrice = GetDoubleValue(commercial, "vlsfoPrice"),
            UlsfoPrice = GetDoubleValue(commercial, "ulsfoPrice"),
            MgoPrice = GetDoubleValue(commercial, "mgoPrice"),
            DailyHire = GetDoubleValue(commercial, "dailyHire"),
            Cev = GetDoubleValue(commercial, "cev"),
            Ilohc = GetDoubleValue(commercial, "ilohc"),
            BallastBonus = GetDoubleValue(commercial, "ballastBonus"),
            RoutingService = GetDoubleValue(commercial, "routingService"),
            Others = GetDoubleValue(commercial, "others")
        };
    }

    private List<PortData> ExtractPorts(System.Text.Json.JsonElement data)
    {
        var ports = new List<PortData>();
        if (!data.TryGetProperty("ports", out var portsEl) || portsEl.ValueKind != System.Text.Json.JsonValueKind.Array)
            return ports;

        foreach (var port in portsEl.EnumerateArray())
        {
            ports.Add(new PortData
            {
                Id = GetStringValue(port, "id"),
                Port = GetStringValue(port, "port"),
                Distance = GetDoubleValue(port, "distance"),
                EcaDistance = GetDoubleValue(port, "ecaDistance"),
                Dem = GetDoubleValue(port, "dem"),
                PortCharge = GetDoubleValue(port, "portCharge")
            });
        }
        return ports;
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
/// Result of ROB (Remaining On Board) calculation across the voyage.
/// </summary>
public class BunkerRobCalculationResult
{
    public double InitialFOROB { get; set; }
    public double InitialDOROB { get; set; }
    public double FinalFOROB { get; set; }
    public double FinalDOROB { get; set; }
    public List<RobLegDto> FoRobProgression { get; set; } = new();
    public List<RobLegDto> DoRobProgression { get; set; } = new();
    public List<string> NegativeRobWarnings { get; set; } = new();
    
    public bool HasNegativeROB => NegativeRobWarnings.Count > 0;
}

/// <summary>
/// Per-leg ROB breakdown.
/// </summary>
public class RobLegDto
{
    public int LegNumber { get; set; }
    public string PortName { get; set; } = "";
    public double OpeningROB { get; set; }
    public double Consumption { get; set; }
    public double Supply { get; set; }
    public double ClosingROB { get; set; }
    public bool HasWarning { get; set; }
}

// Supporting data classes (extract from JSON)
public class CommercialData
{
    public double DailyHire { get; set; }
    public double BodQty { get; set; }
    public double BodPrice { get; set; }
    public double BorQty { get; set; }
    public double BorPrice { get; set; }
    public double VlsfoPrice { get; set; }
    public double UlsfoPrice { get; set; }
    public double MgoPrice { get; set; }
    public double Cev { get; set; }
    public double Ilohc { get; set; }
    public double BallastBonus { get; set; }
    public double RoutingService { get; set; }
    public double Others { get; set; }
}

public class PortData
{
    public string Id { get; set; } = "";
    public string Port { get; set; } = "";
    public double Distance { get; set; }
    public double EcaDistance { get; set; }
    public double Dem { get; set; }
    public double PortCharge { get; set; }
}
