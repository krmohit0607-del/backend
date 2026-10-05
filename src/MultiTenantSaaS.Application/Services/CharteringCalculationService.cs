using MultiTenantSaaS.Application.DTOs.Chartering;
using System.Text.Json;

namespace MultiTenantSaaS.Application.Services;

/// <summary>
/// Chartering calculation service - mirrors frontend EstimateInputs/EstimateResult logic.
/// Handles voyage estimation calculations, loadable quantity, and data matching.
/// </summary>
public interface ICharteringCalculationService
{
    VoyageEstimateCalculationDto CalculateEstimate(string dataJson);
    LoadableQuantityDto CalculateLoadableQuantity(string dataJson, string lqDataJson);
    PortDataDto? FindBestMatchPort(string portName, List<PortDataDto> availablePorts);
    VesselTypeDataDto? FindVesselType(string vesselType);
}

public class CharteringCalculationService : ICharteringCalculationService
{
    private const double EarthRadiusNm = 3440.065; // Earth radius in nautical miles

    public VoyageEstimateCalculationDto CalculateEstimate(string dataJson)
    {
        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(dataJson);
            if (data.ValueKind != JsonValueKind.Object)
            {
                return new VoyageEstimateCalculationDto();
            }

            // Extract inputs from JSON
            var inputs = ExtractEstimateInputs(data);
            
            // Perform calculation
            return PerformCalculation(inputs);
        }
        catch
        {
            // Return empty/zero result on parse error
            return new VoyageEstimateCalculationDto();
        }
    }

    public LoadableQuantityDto CalculateLoadableQuantity(string dataJson, string lqDataJson)
    {
        try
        {
            var lqData = JsonSerializer.Deserialize<JsonElement>(lqDataJson);
            
            var summerDwt = GetDoubleValue(lqData, "summerDwt");
            var lightship = GetDoubleValue(lqData, "lightship");
            var densityAtPort = GetDoubleValue(lqData, "densityAtPort");
            var vlsfo = GetDoubleValue(lqData, "vlsfo");
            var mgo = GetDoubleValue(lqData, "mgo");
            var fw = GetDoubleValue(lqData, "fw");
            var constants = GetDoubleValue(lqData, "constants");
            var point = GetStringValue(lqData, "point");

            var deadweightAvailable = summerDwt - lightship;
            var totalDeductions = vlsfo + mgo + fw + constants;
            var loadableQuantity = Math.Max(0, (deadweightAvailable - totalDeductions) / densityAtPort);

            return new LoadableQuantityDto
            {
                SummerDwt = summerDwt,
                Lightship = lightship,
                DensityAtPort = densityAtPort,
                DeadweightAvailable = deadweightAvailable,
                VlsfoTons = vlsfo,
                MgoTons = mgo,
                FreshWaterTons = fw,
                ConstantsTons = constants,
                TotalDeductions = totalDeductions,
                LoadableQuantity = loadableQuantity,
                ConstrainingPoint = point,
                CalculatedAt = DateTime.UtcNow
            };
        }
        catch
        {
            return new LoadableQuantityDto();
        }
    }

    public PortDataDto? FindBestMatchPort(string portName, List<PortDataDto> availablePorts)
    {
        if (string.IsNullOrWhiteSpace(portName) || !availablePorts.Any())
        {
            return null;
        }

        var normalized = NormalizePortName(portName);

        // Exact match
        var exact = availablePorts.FirstOrDefault(p => 
            NormalizePortName(p.Name).Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        // Starts with match
        var startsWith = availablePorts.FirstOrDefault(p => 
            NormalizePortName(p.Name).StartsWith(normalized, StringComparison.OrdinalIgnoreCase));
        if (startsWith != null) return startsWith;

        // Contains match
        var contains = availablePorts.FirstOrDefault(p => 
            NormalizePortName(p.Name).Contains(normalized, StringComparison.OrdinalIgnoreCase));
        if (contains != null) return contains;

        return null;
    }

    public VesselTypeDataDto? FindVesselType(string vesselType)
    {
        if (string.IsNullOrWhiteSpace(vesselType))
        {
            return null;
        }

        // This would ideally query a database table, but for MVP we return null
        // and let frontend handle the matching
        // TODO: Implement database query for vessel types
        return null;
    }

    // ============ Private Calculation Helpers ============

    private VoyageEstimateCalculationDto PerformCalculation(EstimateInputData inputs)
    {
        var result = new VoyageEstimateCalculationDto();

        if (inputs.Cargoes == null || inputs.Ports == null)
        {
            return result;
        }

        // Calculate freight and commissions
        double freight = 0, addComm = 0, brokerage = 0, freightTax = 0, linerTermTotal = 0;
        foreach (var cargo in inputs.Cargoes)
        {
            var tf = cargo.Quantity * cargo.Frt;
            freight += tf;
            addComm += (tf * cargo.ACommPct) / 100;
            brokerage += (tf * cargo.BrkgPct) / 100;
            freightTax += (tf * cargo.FrtTaxPct) / 100;
            linerTermTotal += cargo.LinerTerm;
        }

        result.Freight = freight;
        result.AddComm = addComm;
        result.Brokerage = brokerage;
        result.FreightTax = freightTax;
        result.LinerTermTotal = linerTermTotal;

        // Calculate voyage days and fuel consumption
        var portHandledQty = CalculatePortHandledQty(inputs.Ports, inputs.Cargoes);
        
        // Track cargo on board after each port for consumption routing
        var cargoOnBoardAfter = new bool[inputs.Ports.Count];
        double cargoRemaining = 0;
        for (int idx = 0; idx < inputs.Ports.Count; idx++)
        {
            var port = inputs.Ports[idx];
            var isLoading = port.Type == "Loading" || port.Type == "Part Loading" || 
                           (port.Type == "Ballast" && (portHandledQty.ContainsKey(port.Id) && portHandledQty[port.Id] > 0));
            var isDischarging = port.Type == "Discharging" || port.Type == "Part Discharging";
            
            if (isLoading)
            {
                cargoRemaining += portHandledQty.ContainsKey(port.Id) ? portHandledQty[port.Id] : 0;
            }
            else if (isDischarging)
            {
                cargoRemaining -= portHandledQty.ContainsKey(port.Id) ? portHandledQty[port.Id] : 0;
            }
            
            cargoOnBoardAfter[idx] = cargoRemaining > 0;
        }
        
        double seaDays = 0, ecaDays = 0, ladenDays = 0, ballastDays = 0;
        double idleTotal = 0, workTotal = 0, distanceTotal = 0, ecaDistanceTotal = 0;
        double portCharge = 0, demTotal = 0, desTotal = 0;
        double foNormalSea = 0, foEcaSea = 0, foPort = 0;
        double mgoSea = 0, mgoEcaSea = 0, mgoPort = 0;

        var perLeg = new List<LegCalculationDto>();
        var cursor = inputs.StartDate;

        for (int portIdx = 0; portIdx < inputs.Ports.Count; portIdx++)
        {
            var port = inputs.Ports[portIdx];
            var spd = port.Speed > 0 ? port.Speed : 12;
            var effSpeed = Math.Max(0.1, spd * (1 - port.Wf / 100));

            double legSea, legEca;
            if (port.Distance > 0)
            {
                legSea = port.Distance / (effSpeed * 24);
                legEca = port.EcaDistance > 0 ? port.EcaDistance / (effSpeed * 24) : 0;
                legEca = Math.Min(legEca, legSea);
            }
            else
            {
                legSea = port.SeaManual;
                legEca = 0;
            }

            var normalSea = Math.Max(0, legSea - legEca);
            
            // Determine if the LEG TO this port is ballast or laden
            // The cargo status at the START of the leg (after processing the previous port)
            var cargoOnBoardAtStartOfLeg = portIdx > 0 ? cargoOnBoardAfter[portIdx - 1] : false;
            var isLadenLeg = cargoOnBoardAtStartOfLeg;
            
            // Working days: ALWAYS calculated from L/D rate + cargo when available, else manual.
            var qtyHandled = portHandledQty.ContainsKey(port.Id) ? portHandledQty[port.Id] : 0;
            var ratePerDay = port.RateUnit?.Contains("hour", StringComparison.OrdinalIgnoreCase) == true ? port.LdRate * 24 : port.LdRate;
            var work = CalculatePortWorkDays(port, qtyHandled);

            // Demurrage / despatch calculation for estimation:
            // Step 1: Allowed Laytime = Cargo Quantity ÷ L/D Rate (contract allowance)
            // Step 2: Working Days (W days) = Calculated from L/D rate + cargo
            // Step 3: Idle Time = Delays, stoppages, weather, shifting, etc. (ADDS to working time)
            // Step 4: Effective Working Time = W days + Idle (total time actually used)
            // Step 5: Calculate Dem/Des
            //   Balance = Allowed - Effective Working Time
            //   If Balance > 0: Despatch (finished early)
            //   If Balance < 0: Demurrage (took longer than allowed)
            
            double legDem = 0, legDes = 0;
            
            // ALWAYS calculate dem/des if demurrage rate is set
            if (port.Dem > 0)
            {
                double allowed = 0;
                
                // Step 1: Calculate allowed laytime from cargo qty and L/D rate
                if (ratePerDay > 0 && qtyHandled > 0)
                {
                    allowed = qtyHandled / ratePerDay;
                }
                
                // Step 4: Effective working time = actual work + idle time delays
                var effectiveWork = work + port.Idle;
                
                // Step 5: Compare allowed vs effective working time
                var balance = allowed - effectiveWork;
                
                if (allowed > 0)
                {
                    if (balance > 0)
                    {
                        // Effective work < Allowed → Despatch (owner pays charterer)
                        legDes = balance * (port.Dem / 2);
                    }
                    else if (balance < 0)
                    {
                        // Effective work > Allowed → Demurrage (charterer pays owner)
                        legDem = (-balance) * port.Dem;
                    }
                    // If balance = 0 → dem/des = 0 (perfect match)
                }
            }

            // Select fuel consumption rates based on speed mode
            MainConsData mainNormal, mainEca;
            SubConsData subNormal, subEca;
            
            var speedMode = inputs.Performance?.SpeedMode ?? "Full";
            if (speedMode == "Eco")
            {
                mainNormal = inputs.Performance?.EcoMainNormal ?? new MainConsData();
                mainEca = inputs.Performance?.EcoMainEca ?? new MainConsData();
                subNormal = inputs.Performance?.EcoSubNormal ?? new SubConsData();
                subEca = inputs.Performance?.EcoSubEca ?? new SubConsData();
            }
            else if (speedMode == "Custom")
            {
                mainNormal = inputs.Performance?.CustomMainNormal ?? new MainConsData();
                mainEca = inputs.Performance?.CustomMainEca ?? new MainConsData();
                subNormal = inputs.Performance?.CustomSubNormal ?? new SubConsData();
                subEca = inputs.Performance?.CustomSubEca ?? new SubConsData();
            }
            else // Full or default
            {
                mainNormal = inputs.Performance?.FullMainNormal ?? new MainConsData();
                mainEca = inputs.Performance?.FullMainEca ?? new MainConsData();
                subNormal = inputs.Performance?.FullSubNormal ?? new SubConsData();
                subEca = inputs.Performance?.FullSubEca ?? new SubConsData();
            }

            // Apply consumption based on port type and leg cargo status
            // Rules:
            // 1. Sea legs (Ballast, Laden): Use sea consumption based on cargo status
            // 2. Sailing Delivery/Redelivery: Use sea consumption (ballast if empty, laden if cargo on board)
            // 3. Port operations (Loading, Discharging, Bunkering, etc.): Use work/idle consumption only
            
            if (port.Distance > 0)
            {
                // Sea leg: Apply ballast or laden consumption based on cargo status
                foNormalSea += normalSea * (isLadenLeg ? mainNormal.Laden : mainNormal.Ballast);
                foEcaSea += legEca * (isLadenLeg ? mainEca.Laden : mainEca.Ballast);
                mgoSea += normalSea * subNormal.Sea;
                mgoEcaSea += legEca * subEca.Sea;
            }
            
            // Port operations: Always use work rate for work time, idle rate for idle time (regardless of cargo)
            foPort += port.Idle * mainNormal.Idle + work * mainNormal.Work;
            mgoPort += port.Idle * subNormal.Idle + work * subNormal.Work;

            seaDays += legSea;
            ecaDays += legEca;
            if (!isLadenLeg && port.Distance > 0)
                ballastDays += legSea;
            else if (isLadenLeg && port.Distance > 0)
                ladenDays += legSea;
            idleTotal += port.Idle;
            workTotal += work;
            distanceTotal += port.Distance;
            ecaDistanceTotal += port.EcaDistance;
            portCharge += port.PortCharge;
            demTotal += legDem;
            desTotal += legDes;

            var arrival = cursor.AddDays(legSea);
            var departure = arrival.AddDays(port.Idle + work);
            cursor = departure;

            perLeg.Add(new LegCalculationDto
            {
                Sea = Round(legSea, 2),
                Eca = Round(legEca, 2),
                Work = Round(work, 2),
                Dem = Round(legDem),
                Des = Round(legDes),
                Arrival = legSea > 0 ? FormatDateTime(arrival) : "—",
                Departure = FormatDateTime(departure)
            });
        }

        var portDays = idleTotal + workTotal;
        var voyageDays = seaDays + portDays;
        var vlsfoCons = foNormalSea + foPort;
        var ulsfoCons = foEcaSea;
        var mgoCons = mgoSea + mgoEcaSea + mgoPort;

        var commercial = inputs.Commercial ?? new CommercialData();
        var vlsfoExp = vlsfoCons * commercial.VlsfoPrice;
        var ulsfoExp = ulsfoCons * commercial.UlsfoPrice;
        var mgoExp = mgoCons * commercial.MgoPrice;
        var bunkerExpense = vlsfoExp + ulsfoExp + mgoExp;
        var demDes = desTotal - demTotal;

        result.SeaDays = Round(seaDays, 2);
        result.EcaDays = Round(ecaDays, 2);
        result.LadenDays = Round(ladenDays, 2);
        result.BallastDays = Round(ballastDays, 2);
        result.IdleTotal = Round(idleTotal, 2);
        result.WorkTotal = Round(workTotal, 2);
        result.PortDays = Round(portDays, 2);
        result.VoyageDays = Round(voyageDays, 2);
        result.DistanceTotal = Round(distanceTotal);
        result.EcaDistanceTotal = Round(ecaDistanceTotal);
        result.PortCharge = Round(portCharge);
        result.DemTotal = Round(demTotal);
        result.DesTotal = Round(desTotal);
        result.DemDes = Round(demDes);
        result.VlsfoCons = Round(vlsfoCons, 1);
        result.UlsfoCons = Round(ulsfoCons, 1);
        result.MgoCons = Round(mgoCons, 1);
        result.VlsfoExp = Round(vlsfoExp);
        result.UlsfoExp = Round(ulsfoExp);
        result.MgoExp = Round(mgoExp);
        result.BunkerExpense = Round(bunkerExpense);

        // Fixture-type-driven logic
        var inKind = inputs.FixType.Split('-')[0];
        var outKind = inputs.FixType.Contains('-') ? inputs.FixType.Split('-')[1] : "VOUT";
        var isTCKind = (string k) => k.StartsWith("TC");
        var weOperate = outKind == "VOUT" && inKind != "VIN";
        var isRelet = inKind == "VIN";
        var showBOD = !weOperate && !isRelet && (isTCKind(inKind) || isTCKind(outKind));

        var bodValue = commercial.BodQty * commercial.BodPrice;
        var borValue = commercial.BorQty * commercial.BorPrice;
        var bunkerAdj = bodValue - borValue;

        result.BodValue = Round(bodValue);
        result.BorValue = Round(borValue);
        result.BunkerAdj = Round(bunkerAdj);

        // Operating expense
        var freightCommOpex = outKind == "VOUT" ? addComm + brokerage + freightTax + commercial.LinerTerms + linerTermTotal : 0;
        var demDesOpex = outKind == "VOUT" ? demDes : 0;
        var portOpex = weOperate ? portCharge : 0;
        var bunkerOpex = weOperate ? bunkerExpense : 0;
        var bodOpex = showBOD ? bunkerAdj : 0;
        var fixedOpex = commercial.Cev + commercial.Ilohc + commercial.BallastBonus + commercial.RoutingService + commercial.Others;
        var opExpense = fixedOpex + freightCommOpex + demDesOpex + portOpex + bunkerOpex + bodOpex;

        // Revenue and profit
        var revenue = outKind == "VOUT" ? freight : commercial.DailyHireOut * (1 - commercial.HAddCommOutPct / 100) * voyageDays;
        var opProfit = revenue - opExpense;

        // Hire-based cost
        var netHire = isRelet
            ? (voyageDays > 0 ? commercial.FreightIn / voyageDays : commercial.FreightIn)
            : inKind == "OWN"
                ? commercial.OwnDailyCost
                : commercial.DailyHire * (1 - commercial.HAddCommPct / 100);
        var totalHire = isRelet ? commercial.FreightIn : netHire * voyageDays;
        var totalExpense = opExpense + totalHire;
        var profit = revenue - totalExpense;
        var profitPct = revenue != 0 ? (profit / revenue) * 100 : 0;
        var tce = voyageDays > 0 ? opProfit / voyageDays : 0;

        result.OpExpense = Round(opExpense);
        result.Revenue = Round(revenue);
        result.OpProfit = Round(opProfit);
        result.NetHire = Round(netHire, 2);
        result.TotalHire = Round(totalHire);
        result.TotalExpense = Round(totalExpense);
        result.Profit = Round(profit);
        result.ProfitPct = Round(profitPct, 2);
        result.Tce = Round(tce, 2);
        result.StartStr = FormatDateTime(inputs.StartDate);
        result.EndStr = FormatDateTime(cursor);
        result.PerLeg = perLeg;

        return result;
    }

    private Dictionary<string, double> CalculatePortHandledQty(List<PortData> ports, List<CargoData> cargoes)
    {
        var handled = new Dictionary<string, double>();
        var loadUsed = new HashSet<string>();
        var dischUsed = new HashSet<string>();

        foreach (var cargo in cargoes)
        {
            if (cargo.Quantity <= 0) continue;

            if (!string.IsNullOrEmpty(cargo.LoadPort))
            {
                var port = FindMatchingPort(cargo.LoadPort, ports, loadUsed);
                if (port != null)
                {
                    loadUsed.Add(port.Id);
                    if (!handled.ContainsKey(port.Id))
                        handled[port.Id] = 0;
                    handled[port.Id] += cargo.Quantity;
                }
            }

            if (!string.IsNullOrEmpty(cargo.DiscPort))
            {
                var port = FindMatchingPort(cargo.DiscPort, ports, dischUsed);
                if (port != null)
                {
                    dischUsed.Add(port.Id);
                    if (!handled.ContainsKey(port.Id))
                        handled[port.Id] = 0;
                    handled[port.Id] += cargo.Quantity;
                }
            }
        }

        return handled;
    }

    private PortData? FindMatchingPort(string targetPort, List<PortData> ports, HashSet<string> used)
    {
        var normalized = NormalizePortName(targetPort);
        var fresh = ports.FirstOrDefault(p => !used.Contains(p.Id) && NormalizePortName(p.Port) == normalized);
        if (fresh != null) return fresh;

        for (int i = ports.Count - 1; i >= 0; i--)
        {
            if (NormalizePortName(ports[i].Port) == normalized)
                return ports[i];
        }

        return null;
    }

    private double CalculatePortWorkDays(PortData port, double handledQty)
    {
        if (port.LdRate > 0 && handledQty > 0)
        {
            var perDay = port.RateUnit?.Contains("hour", StringComparison.OrdinalIgnoreCase) == true ? port.LdRate * 24 : port.LdRate;
            if (perDay > 0)
                return handledQty / perDay;
        }
        return port.Work;
    }

    private EstimateInputData ExtractEstimateInputs(JsonElement data)
    {
        var inputs = new EstimateInputData
        {
            FixType = GetStringValue(data, "fixType"),
            StartDate = GetDateTimeValue(data, "startDate"),
            Cargoes = ExtractCargoes(data),
            Ports = ExtractPorts(data),
            Performance = ExtractPerformance(data),
            Commercial = ExtractCommercial(data)
        };
        return inputs;
    }

    private List<CargoData> ExtractCargoes(JsonElement data)
    {
        var cargoes = new List<CargoData>();
        if (data.TryGetProperty("cargoes", out var cargoesEl) && cargoesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var cargo in cargoesEl.EnumerateArray())
            {
                cargoes.Add(new CargoData
                {
                    Id = GetStringValue(cargo, "id"),
                    Name = GetStringValue(cargo, "name"),
                    LoadPort = GetStringValue(cargo, "loadPort"),
                    DiscPort = GetStringValue(cargo, "dischPort"),
                    Quantity = GetDoubleValue(cargo, "quantity"),
                    Frt = GetDoubleValue(cargo, "frt"),
                    ACommPct = GetDoubleValue(cargo, "aCommPct"),
                    BrkgPct = GetDoubleValue(cargo, "brkgPct"),
                    FrtTaxPct = GetDoubleValue(cargo, "frtTaxPct"),
                    LinerTerm = GetDoubleValue(cargo, "linerTerm")
                });
            }
        }
        return cargoes;
    }

    private List<PortData> ExtractPorts(JsonElement data)
    {
        var ports = new List<PortData>();
        if (data.TryGetProperty("ports", out var portsEl) && portsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var port in portsEl.EnumerateArray())
            {
                ports.Add(new PortData
                {
                    Id = GetStringValue(port, "id"),
                    Type = GetStringValue(port, "type"),
                    Port = GetStringValue(port, "port"),
                    Distance = GetDoubleValue(port, "distance"),
                    EcaDistance = GetDoubleValue(port, "ecaDistance"),
                    Wf = GetDoubleValue(port, "wf"),
                    Speed = GetDoubleValue(port, "speed"),
                    LdRate = GetDoubleValue(port, "ldRate"),
                    Idle = GetDoubleValue(port, "idle"),
                    Work = GetDoubleValue(port, "work"),
                    SeaManual = GetDoubleValue(port, "seaManual"),
                    Dem = GetDoubleValue(port, "dem"),
                    PortCharge = GetDoubleValue(port, "portCharge"),
                    RateUnit = GetStringValue(port, "rateUnit")
                });
            }
        }
        return ports;
    }

    private PerformanceData? ExtractPerformance(JsonElement data)
    {
        if (!data.TryGetProperty("perf", out var perf))
            return null;

        var result = new PerformanceData
        {
            SpeedMode = GetStringValue(perf, "speedMode"),
            
            // Full speed mode
            FullMainNormal = ExtractMainCons(perf, "fullMainNormal"),
            FullMainEca = ExtractMainCons(perf, "fullMainEca"),
            FullSubNormal = ExtractSubCons(perf, "fullSubNormal"),
            FullSubEca = ExtractSubCons(perf, "fullSubEca"),
            
            // Eco speed mode
            EcoMainNormal = ExtractMainCons(perf, "ecoMainNormal"),
            EcoMainEca = ExtractMainCons(perf, "ecoMainEca"),
            EcoSubNormal = ExtractSubCons(perf, "ecoSubNormal"),
            EcoSubEca = ExtractSubCons(perf, "ecoSubEca"),
            
            // Custom speed mode
            CustomMainNormal = ExtractMainCons(perf, "customMainNormal"),
            CustomMainEca = ExtractMainCons(perf, "customMainEca"),
            CustomSubNormal = ExtractSubCons(perf, "customSubNormal"),
            CustomSubEca = ExtractSubCons(perf, "customSubEca"),
            
            // Legacy fields for backward compatibility
            MainNormal = ExtractMainCons(perf, "mainNormal"),
            MainEca = ExtractMainCons(perf, "mainEca"),
            SubNormal = ExtractSubCons(perf, "subNormal"),
            SubEca = ExtractSubCons(perf, "subEca")
        };
        
        return result;
    }

    private MainConsData ExtractMainCons(JsonElement perf, string key)
    {
        if (!perf.TryGetProperty(key, out var el))
            return new MainConsData();

        return new MainConsData
        {
            Ballast = GetDoubleValue(el, "ballast"),
            Laden = GetDoubleValue(el, "laden"),
            Idle = GetDoubleValue(el, "idle"),
            Work = GetDoubleValue(el, "work")
        };
    }

    private SubConsData ExtractSubCons(JsonElement perf, string key)
    {
        if (!perf.TryGetProperty(key, out var el))
            return new SubConsData();

        return new SubConsData
        {
            Sea = GetDoubleValue(el, "sea"),
            Idle = GetDoubleValue(el, "idle"),
            Work = GetDoubleValue(el, "work")
        };
    }

    private CommercialData ExtractCommercial(JsonElement data)
    {
        if (!data.TryGetProperty("commercial", out var comm))
            return new CommercialData();

        return new CommercialData
        {
            DailyHire = GetDoubleValue(comm, "dailyHire"),
            HAddCommPct = GetDoubleValue(comm, "hAddCommPct"),
            DailyHireOut = GetDoubleValue(comm, "dailyHireOut"),
            HAddCommOutPct = GetDoubleValue(comm, "hAddCommOutPct"),
            OwnDailyCost = GetDoubleValue(comm, "ownDailyCost"),
            FreightIn = GetDoubleValue(comm, "freightIn"),
            BodQty = GetDoubleValue(comm, "bodQty"),
            BodPrice = GetDoubleValue(comm, "bodPrice"),
            BorQty = GetDoubleValue(comm, "borQty"),
            BorPrice = GetDoubleValue(comm, "borPrice"),
            Cev = GetDoubleValue(comm, "cev"),
            Ilohc = GetDoubleValue(comm, "ilohc"),
            BallastBonus = GetDoubleValue(comm, "ballastBonus"),
            RoutingService = GetDoubleValue(comm, "routingService"),
            Others = GetDoubleValue(comm, "others"),
            LinerTerms = GetDoubleValue(comm, "linerTerms"),
            VlsfoPrice = GetDoubleValue(comm, "vlsfoPrice"),
            MgoPrice = GetDoubleValue(comm, "mgoPrice"),
            UlsfoPrice = GetDoubleValue(comm, "ulsfoPrice")
        };
    }

    // ============ Helper Methods ============

    private string NormalizePortName(string name)
    {
        return name.Split('<')[0].Split('(')[0].Trim().ToLowerInvariant();
    }

    private double Round(double value, int decimals = 0)
    {
        var factor = Math.Pow(10, decimals);
        return Math.Round(value * factor) / factor;
    }

    private string FormatDateTime(DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd HH:mm");
    }

    private double GetDoubleValue(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDouble();
            if (prop.ValueKind == JsonValueKind.String && double.TryParse(prop.GetString(), out var d))
                return d;
        }
        return 0;
    }

    private string GetStringValue(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString() ?? string.Empty;
        return string.Empty;
    }

    private DateTime GetDateTimeValue(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(prop.GetString(), out var dt))
                return dt;
        }
        return DateTime.UtcNow;
    }

    // ============ Internal Classes ============

    private class EstimateInputData
    {
        public string FixType { get; set; } = "TCIN-VOUT";
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public List<CargoData>? Cargoes { get; set; }
        public List<PortData>? Ports { get; set; }
        public PerformanceData? Performance { get; set; }
        public CommercialData? Commercial { get; set; }
    }

    private class CargoData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LoadPort { get; set; } = string.Empty;
        public string DiscPort { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public double Frt { get; set; }
        public double ACommPct { get; set; }
        public double BrkgPct { get; set; }
        public double FrtTaxPct { get; set; }
        public double LinerTerm { get; set; }
    }

    private class PortData
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public double Distance { get; set; }
        public double EcaDistance { get; set; }
        public double Wf { get; set; }
        public double Speed { get; set; }
        public double LdRate { get; set; }
        public double Idle { get; set; } // Turn time / free time allowance (excluded from laytime)
        public double Work { get; set; }
        public double SeaManual { get; set; }
        public double Dem { get; set; }
        public double PortCharge { get; set; }
        public string? LaytimeTerm { get; set; } // SHINC, SHEX, etc. - determines exclusions automatically
        public string? RateUnit { get; set; }
    }

    private class PerformanceData
    {
        public string? SpeedMode { get; set; }
        
        // Full speed mode consumption
        public MainConsData? FullMainNormal { get; set; }
        public MainConsData? FullMainEca { get; set; }
        public SubConsData? FullSubNormal { get; set; }
        public SubConsData? FullSubEca { get; set; }
        
        // Eco speed mode consumption
        public MainConsData? EcoMainNormal { get; set; }
        public MainConsData? EcoMainEca { get; set; }
        public SubConsData? EcoSubNormal { get; set; }
        public SubConsData? EcoSubEca { get; set; }
        
        // Custom speed mode consumption
        public MainConsData? CustomMainNormal { get; set; }
        public MainConsData? CustomMainEca { get; set; }
        public SubConsData? CustomSubNormal { get; set; }
        public SubConsData? CustomSubEca { get; set; }
        
        // Legacy fields for backward compatibility
        public MainConsData? MainNormal { get; set; }
        public MainConsData? MainEca { get; set; }
        public SubConsData? SubNormal { get; set; }
        public SubConsData? SubEca { get; set; }
    }

    private class MainConsData
    {
        public double Ballast { get; set; }
        public double Laden { get; set; }
        public double Idle { get; set; }
        public double Work { get; set; }
    }

    private class SubConsData
    {
        public double Sea { get; set; }
        public double Idle { get; set; }
        public double Work { get; set; }
    }

    private class CommercialData
    {
        public double DailyHire { get; set; }
        public double HAddCommPct { get; set; }
        public double DailyHireOut { get; set; }
        public double HAddCommOutPct { get; set; }
        public double OwnDailyCost { get; set; }
        public double FreightIn { get; set; }
        public double BodQty { get; set; }
        public double BodPrice { get; set; }
        public double BorQty { get; set; }
        public double BorPrice { get; set; }
        public double Cev { get; set; }
        public double Ilohc { get; set; }
        public double BallastBonus { get; set; }
        public double RoutingService { get; set; }
        public double Others { get; set; }
        public double LinerTerms { get; set; }
        public double VlsfoPrice { get; set; }
        public double MgoPrice { get; set; }
        public double UlsfoPrice { get; set; }
    }
}
