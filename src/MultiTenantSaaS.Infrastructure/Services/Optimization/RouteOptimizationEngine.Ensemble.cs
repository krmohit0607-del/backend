using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

public partial class RouteOptimizationEngine
{
    private const int MaxEnsemblePoints = 12;

    /// <summary>
    /// Scenario-based wind uncertainty: the chosen route and settings are re-sailed once per ensemble member with that member's wind at the position and time of each
    /// sub-step. Waves, currents and wave period stay deterministic, so the spread is partial. Routes are not re-optimized per member, and reserves are checked at arrival.
    /// </summary>
    private async Task<UncertaintyDto?> EvaluateEnsembleAsync(
        OptimizationRequestDto request, ScenarioConfig config, ArcPhysicsContext physics, List<Label> chain, Label final, BunkerContext bunker, EtsContext ets, CancellationToken ct)
    {
        if (_ensemble == null || !request.IncludeEnsemble) return null;

        var sailing = chain.Skip(1).Where(label => !label.Arc!.IsWait).SelectMany(label => label.Arc!.Steps!).ToList();
        if (sailing.Count == 0) return null;

        var sampleCount = Math.Min(MaxEnsemblePoints, sailing.Count);
        var pointStepIndex = Enumerable.Range(0, sampleCount).Select(index => (int)((index + 0.5) * sailing.Count / sampleCount)).ToArray();
        var points = pointStepIndex.Select(index => (sailing[index].Lat, sailing[index].Lng)).ToList();
        var nearest = sailing.Select(step => Enumerable.Range(0, points.Count).MinBy(index => Math.Abs(points[index].Lat - step.Lat) + Math.Abs(points[index].Lng - step.Lng))).ToArray();

        var result = new UncertaintyDto { Source = _ensemble.SourceName };
        var data = await _ensemble.GetAsync(points, request.EtdUtc, request.EtdUtc.AddHours(final.TimeHours * 1.3 + 24), ct);
        if (data == null || data.SpeedKn.Length == 0)
        {
            result.Notes.Add("Ensemble wind data was unavailable for this route and period, so no probabilistic ETA, fuel or cost is shown.");
            return result;
        }

        var offsetHours = (request.EtdUtc - data.StartUtc).TotalHours;
        var etaHours = new List<double>();
        var fuel = new List<double>();
        var cost = new List<double>();
        var missEta = 0;
        var shortfall = 0;
        long fallbackSteps = 0, totalSteps = 0;
        var trackRob = bunker.StartVlsfo.HasValue || bunker.StartLsmgo.HasValue;
        var supplyVlsfo = bunker.Supplies.Where((_, index) => (final.SupplyMask & (1UL << index)) != 0).Sum(supply => supply.VlsfoMt);
        var supplyLsmgo = bunker.Supplies.Where((_, index) => (final.SupplyMask & (1UL << index)) != 0).Sum(supply => supply.LsmgoMt);

        for (var member = 0; member < data.SpeedKn.Length; member++)
        {
            double t = 0, vlsfo = 0, lsmgo = 0, fuelCost = 0, chargeable = 0;
            var stepIndex = 0;
            foreach (var label in chain.Skip(1))
            {
                var arc = label.Arc!;
                if (arc.IsWait)
                {
                    chargeable += arc.Co2Mt * ets.Factor(request.EtdUtc.AddHours(t));
                    t += arc.Hours;
                    lsmgo += arc.AuxGoMt;
                    fuelCost += arc.FuelCostUsd;
                    continue;
                }

                foreach (var step in arc.Steps!)
                {
                    var (speed, from) = WindAt(data, member, nearest[stepIndex++], offsetHours + t);
                    var input = double.IsNaN(speed) || double.IsNaN(from) ? step.Input : step.Input with { WindKn = speed, WindFromDeg = from };
                    var outcome = ArcPhysics.Evaluate(input, arc.Setting, step.Outcome.Mode, physics);
                    totalSteps++;
                    if (outcome == null) { outcome = step.Outcome; fallbackSteps++; }

                    chargeable += outcome.Co2Mt * ets.Factor(request.EtdUtc.AddHours(t));
                    t += outcome.Hours;
                    if (step.Input.InEca) lsmgo += outcome.MainFuelMt; else vlsfo += outcome.MainFuelMt;
                    lsmgo += outcome.AuxGoMt;
                    fuelCost += outcome.FuelCostUsd;
                }
            }

            etaHours.Add(t);
            fuel.Add(vlsfo + lsmgo);
            cost.Add(fuelCost + request.HireRatePerDayUsd / 24 * t + request.EuaPriceUsdPerTonneCo2 * chargeable);
            if (config.TargetEtaUtc.HasValue && LateHours(config, request.EtdUtc.AddHours(t)) > 0.25) missEta++;
            if (trackRob &&
                ((bunker.StartVlsfo.HasValue && bunker.StartVlsfo.Value - vlsfo + supplyVlsfo < (bunker.MinVlsfo ?? 0)) ||
                 (bunker.StartLsmgo.HasValue && bunker.StartLsmgo.Value - lsmgo + supplyLsmgo < (bunker.MinLsmgo ?? 0))))
                shortfall++;
        }

        var members = etaHours.Count;
        result.Members = members;
        result.EtaP10Utc = request.EtdUtc.AddHours(Percentile(etaHours, 0.1));
        result.EtaP50Utc = request.EtdUtc.AddHours(Percentile(etaHours, 0.5));
        result.EtaP90Utc = request.EtdUtc.AddHours(Percentile(etaHours, 0.9));
        result.FuelP10Mt = Percentile(fuel, 0.1);
        result.FuelP50Mt = Percentile(fuel, 0.5);
        result.FuelP90Mt = Percentile(fuel, 0.9);
        result.CostP10Usd = Percentile(cost, 0.1);
        result.CostP50Usd = Percentile(cost, 0.5);
        result.CostP90Usd = Percentile(cost, 0.9);
        if (config.TargetEtaUtc.HasValue) result.ProbabilityMissTargetEta = missEta / (double)members;
        if (trackRob) result.ProbabilityReserveShortfall = shortfall / (double)members;

        result.Notes.Add($"{members} wind ensemble members re-sailed on the chosen route and settings. Waves, currents and wave period use the single deterministic forecast, so the spread understates total uncertainty (partial). The route is not re-optimized per member.");
        if (trackRob) result.Notes.Add("Reserve shortfall is checked at arrival only.");
        if (fallbackSteps > 0.05 * totalSteps) result.Notes.Add($"{fallbackSteps} of {totalSteps} sub-steps could not be re-evaluated under member wind and kept their deterministic result.");
        return result;
    }

    /// <summary>Member wind at an hour offset from the ensemble start, interpolated linearly in time using vector components.</summary>
    private static (double SpeedKn, double FromDeg) WindAt(EnsembleWind data, int member, int point, double hourOffset)
    {
        var speeds = data.SpeedKn[member][point];
        var dirs = data.FromDeg[member][point];
        var clamped = Math.Clamp(hourOffset, 0, speeds.Length - 1);
        var low = (int)Math.Floor(clamped);
        var high = Math.Min(low + 1, speeds.Length - 1);
        var t = clamped - low;
        double Component(int index, bool useEast) => speeds[index] * (useEast ? Math.Sin(dirs[index] * Math.PI / 180) : Math.Cos(dirs[index] * Math.PI / 180));
        var east = Component(low, true) * (1 - t) + Component(high, true) * t;
        var north = Component(low, false) * (1 - t) + Component(high, false) * t;
        if (double.IsNaN(east) || double.IsNaN(north)) return (double.NaN, double.NaN);
        var speed = Math.Sqrt(east * east + north * north);
        var from = (Math.Atan2(east, north) * 180 / Math.PI + 360) % 360;
        return (speed, from);
    }

    private static double Percentile(List<double> values, double p)
    {
        var sorted = values.OrderBy(value => value).ToList();
        var position = p * (sorted.Count - 1);
        var low = (int)Math.Floor(position);
        var high = Math.Min(low + 1, sorted.Count - 1);
        return sorted[low] + (sorted[high] - sorted[low]) * (position - low);
    }
}
