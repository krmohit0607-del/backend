using System.Diagnostics;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>
/// Two-stage solver. Stage 1 is a coarse pass (configured operating points, one forecast sample per arc, no waiting) that finds a feasible
/// route and the corridor around the best candidates. Stage 2 searches that corridor in detail: sub-stepped arcs with the forecast re-sampled
/// at each sub-step's position and time, intermediate engine settings on the calibrated curve, and optional waiting. Labels carry cost, time
/// and bunker ROB, so arrival-time-dependent weather is handled by dominance on those dimensions rather than by a static shortest path.
/// Optimality is limited to the lattice (14 columns x 5 lateral lanes), the discrete settings and the label cap.
/// </summary>
public partial class RouteOptimizationEngine
{
    private const double SubStepNm = 40;
    private const int MaxSubSteps = 6;
    private const double DefaultTimeLimitSeconds = 90;
    private const double MaxWaitHours = 48;
    private const int CoarseMaxLabels = 40;
    private const int FineMaxLabels = 60;
    private static readonly double[] WaitOptionsHours = { 6, 12, 24 };

    private sealed record Label(
        double Cost, double TimeHours, double DistNm, double MainFuelMt, double EcaDistNm, double EcaTimeHours, double EcaFuelMt, double AuxGoMt,
        double Co2Mt, double ChargeableCo2Mt, double FuelCostUsd, double Exposure, double ExceedHours, double SyncHours, double ParametricHours, double PitchHours,
        double BlowerHours, double BoostHours, double WaitHours, double MaxWaveM, double MaxWindKn, double MaxLoad, bool Constrained,
        double? RobVlsfo, double? RobLsmgo, ulong SupplyMask, Label? Parent, (int Col, int Lane) At, ArcOutcome? Arc)
    {
        public static Label Start(double? robVlsfo, double? robLsmgo) => new(
            Cost: 0, TimeHours: 0, DistNm: 0, MainFuelMt: 0, EcaDistNm: 0, EcaTimeHours: 0, EcaFuelMt: 0, AuxGoMt: 0, Co2Mt: 0, ChargeableCo2Mt: 0, FuelCostUsd: 0,
            Exposure: 0, ExceedHours: 0, SyncHours: 0, ParametricHours: 0, PitchHours: 0, BlowerHours: 0, BoostHours: 0, WaitHours: 0, MaxWaveM: 0, MaxWindKn: 0, MaxLoad: 0,
            Constrained: false, RobVlsfo: robVlsfo, RobLsmgo: robLsmgo, SupplyMask: 0, Parent: null, At: (0, 0), Arc: null);
    }

    private sealed record StageOptions(bool Fine, bool AllowWaiting, HashSet<(int Col, int Lane)>? AllowedNodes, int MaxLabels, DateTime Deadline, PowerMode[] Modes);

    private sealed record StageResult(List<Label>? Finals, int ReserveRejections, bool TimedOut, long Evaluations);

    private sealed record SolveOutcome(List<Label>? Finals, string? Reason, SolverStatusDto Solver, bool TrackRob);

    private async Task<OptimizationScenarioResultDto> SolveAsync(
        OptimizationRequestDto request, ScenarioConfig config, List<SpeedSetting> allSpeeds, VesselEngineLimits limits, ArcPhysicsContext physics,
        Lattice lattice, ZoneSet zones, ForecastCache forecast, BunkerContext bunker, EtsContext ets, ScenarioTotals? baseline,
        IProgress<double>? progress, List<string> notes, CancellationToken ct)
    {
        var candidates = SelectCandidates(config, allSpeeds, request, limits, notes, out var rejection);
        if (candidates == null) return Infeasible(config.Key, request, rejection!);

        var tally = new RejectionTally();
        var model = VesselPerformanceModel.Build(allSpeeds);
        physics = physics with { Model = model, Tally = tally };
        if (model != null)
        {
            notes.Add(model.Describe());
            notes.AddRange(model.Notes);
        }
        else
        {
            notes.Add("Fewer than two distinct operating points are available, so no calibrated speed-power curve could be built; weather speed loss at constant power and a cube law for boost and zone caps are used instead (approximation).");
        }

        var trackRob = bunker.StartVlsfo.HasValue || bunker.StartLsmgo.HasValue;
        var solver = new SolverStatusDto
        {
            Method = "Two-stage time-dependent label-setting over a lateral lattice (coarse corridor search, then sub-stepped detailed search with intermediate settings and waiting); dominance on cost, time and bunker ROB.",
        };
        var limitSeconds = request.SolverTimeLimitSeconds is > 0 ? request.SolverTimeLimitSeconds.Value : DefaultTimeLimitSeconds;
        solver.TimeLimitSeconds = limitSeconds;
        var deadline = DateTime.UtcNow.AddSeconds(limitSeconds);
        PowerMode[] modes = { PowerMode.HoldRpm, PowerMode.Boost };

        var watch = Stopwatch.StartNew();
        var coarse = await RunStageAsync(request, config, candidates, physics, lattice, zones, forecast, bunker, ets,
            new StageOptions(false, false, null, CoarseMaxLabels, deadline, modes), Scaled(progress, 0, 0.35), ct);
        solver.Stage1ElapsedMs = watch.ElapsedMilliseconds;
        solver.Stage1Settings = candidates.Count;
        solver.SegmentEvaluations = coarse.Evaluations;

        if (coarse.TimedOut)
        {
            solver.Status = "TimeLimitReached";
            solver.ElapsedMs = watch.ElapsedMilliseconds;
            return Infeasible(config.Key, request, $"The solver time limit of {limitSeconds:0} s was reached before any feasible route was found.", solver);
        }

        var fineCandidates = candidates.ToList();
        if (!config.RestrictToSingleSpeed && model != null) fineCandidates.AddRange(IntermediateSettings(model, candidates));
        var corridor = coarse.Finals is { Count: > 0 } ? CorridorNodes(coarse.Finals, lattice) : null;

        var fineStart = watch.ElapsedMilliseconds;
        var fine = await RunStageAsync(request, config, fineCandidates, physics, lattice, zones, forecast, bunker, ets,
            new StageOptions(true, true, corridor, FineMaxLabels, deadline, modes), Scaled(progress, 0.35, 0.65), ct);
        solver.Stage2ElapsedMs = watch.ElapsedMilliseconds - fineStart;
        solver.Stage2Settings = fineCandidates.Count;
        solver.Stage2Nodes = corridor?.Count ?? lattice.Nodes.Sum(column => column.Length);
        solver.SegmentEvaluations += fine.Evaluations;
        solver.ElapsedMs = watch.ElapsedMilliseconds;

        List<Label>? finals;
        var reserveRejections = fine.ReserveRejections;
        if (fine.Finals is { Count: > 0 })
        {
            finals = fine.Finals;
            solver.Status = "Completed";
        }
        else if (fine.TimedOut && coarse.Finals is { Count: > 0 })
        {
            finals = coarse.Finals;
            solver.Status = "TimeLimitReached";
            solver.Notes.Add($"The detailed search hit the {limitSeconds:0} s limit; the best coarse-stage route is returned (one forecast sample per arc, configured settings only, no waiting).");
        }
        else if (coarse.Finals is { Count: > 0 })
        {
            finals = coarse.Finals;
            solver.Status = "Completed";
            solver.Notes.Add("The detailed search found no feasible route inside the corridor, so the coarse-stage route is returned.");
        }
        else
        {
            finals = null;
            solver.Status = "Infeasible";
            reserveRejections += coarse.ReserveRejections;
        }

        if (finals == null)
        {
            var reason = reserveRejections > 0
                ? "No route keeps the required bunker reserve (or avoids running out of fuel) on the way to the destination, counting only confirmed supplies."
                : "No feasible path found \u2014 land, no-go zones or vessel engine/RPM limits excluded every route through the search corridor.";
            if (reserveRejections == 0 && tally.Any) reason += $" Operating points were rejected for: {tally.Summary()}.";
            return Infeasible(config.Key, request, reason, solver);
        }

        solver.Notes.Add("Label dominance on cost, time and ROB; at most " + FineMaxLabels + " labels per node. Optimality holds only within the lattice and discrete settings.");
        solver.Notes.Add("Scenarios are evaluated one after another; parallel evaluation is not used because forecast and lattice caches are shared.");
        var built = BuildResult(request, config, limits, physics, lattice, finals, forecast, bunker, ets, baseline, solver, trackRob, out var chain, out var final);
        if (built.IsFeasible)
            built.Analysis.Uncertainty = await EvaluateEnsembleAsync(request, config, physics, chain, final, bunker, ets, ct);
        return built;
    }

    private static IProgress<double>? Scaled(IProgress<double>? progress, double from, double span) =>
        progress == null ? null : new Progress<double>(fraction => progress.Report(from + span * fraction));

    private static HashSet<(int Col, int Lane)> CorridorNodes(List<Label> finals, Lattice lattice)
    {
        var nodes = new HashSet<(int Col, int Lane)>();
        foreach (var final in finals.OrderBy(label => label.Cost).Take(6).Concat(finals.OrderBy(label => label.TimeHours).Take(1)))
        {
            for (var node = final; node != null; node = node.Parent)
            {
                var (col, lane) = node.At;
                for (var delta = -1; delta <= 1; delta++)
                {
                    var neighbour = lane + delta;
                    if (neighbour >= 0 && neighbour < lattice.Nodes[col].Length) nodes.Add((col, neighbour));
                }
            }
        }
        for (var col = 1; col < lattice.ColumnCount - 1; col++) nodes.Add((col, lattice.MidLane));
        return nodes;
    }

    /// <summary>Settings between the configured ones, taken from the calibrated curve at about 0.5 kn steps (at most 8), so heavy-weather and fuel-saving choices are not limited to the entered points.</summary>
    internal static IEnumerable<SpeedSetting> IntermediateSettings(VesselPerformanceModel model, List<SpeedSetting> basePoints)
    {
        if (basePoints.Count == 0) yield break;
        var low = basePoints.Min(setting => setting.SpeedKn);
        var high = basePoints.Max(setting => setting.SpeedKn);
        if (high - low < 0.6) yield break;

        var step = Math.Max(0.5, (high - low) / 9);
        for (var speed = low + step; speed < high - step / 2; speed += step)
        {
            if (basePoints.Any(setting => Math.Abs(setting.SpeedKn - speed) < 0.2)) continue;
            var nearest = basePoints.MinBy(setting => Math.Abs(setting.SpeedKn - speed))!;
            var x = model.CalmX(speed);
            var fuel = model.FuelMtPerDay(x);
            var ratio = nearest.FuelRateMtPerDay > 0 ? nearest.EcaFuelRateMtPerDay / nearest.FuelRateMtPerDay : 1;
            yield return nearest with
            {
                Mode = "INTERPOLATED",
                IsCpDefault = false,
                SpeedKn = speed,
                Rpm = model.RpmAt(x) ?? 0,
                PowerKw = model.Basis == PowerBasis.BrakePowerKw ? x : null,
                McrPercent = null,
                FuelRateMtPerDay = fuel,
                EcaFuelRateMtPerDay = fuel * ratio,
                Source = "INTERPOLATED",
                SampleCount = 0,
            };
        }
    }

    private async Task<StageResult> RunStageAsync(
        OptimizationRequestDto request, ScenarioConfig config, List<SpeedSetting> candidates, ArcPhysicsContext physics, Lattice lattice, ZoneSet zones,
        ForecastCache forecast, BunkerContext bunker, EtsContext ets, StageOptions options, IProgress<double>? progress, CancellationToken ct)
    {
        var trackRob = bunker.StartVlsfo.HasValue || bunker.StartLsmgo.HasValue;
        var last = lattice.ColumnCount - 1;
        var auxRate = candidates.Min(setting => setting.AuxGoRateMtPerDay);
        var reserveRejections = 0;
        long evaluations = 0;

        bool Allowed(int col, int lane) => col == 0 || col == last || options.AllowedNodes == null || options.AllowedNodes.Contains((col, lane));

        // ROB and reserve bookkeeping shared by sailing and waiting steps.
        Label? Advance(Label origin, ArcGeometry? arc, ArcOutcome outcome, (int Col, int Lane) at)
        {
            var startUtc = request.EtdUtc.AddHours(origin.TimeHours);
            var inEca = arc?.InEca ?? false;
            var vlsfoUse = inEca ? 0 : outcome.MainFuelMt;
            var lsmgoUse = (inEca ? outcome.MainFuelMt : 0) + outcome.AuxGoMt;

            var mask = origin.SupplyMask;
            double supplyVlsfo = 0, supplyLsmgo = 0;
            if (arc != null)
            {
                foreach (var index in arc.Supplies)
                {
                    if ((mask & (1UL << index)) != 0) continue;
                    mask |= 1UL << index;
                    supplyVlsfo += bunker.Supplies[index].VlsfoMt;
                    supplyLsmgo += bunker.Supplies[index].LsmgoMt;
                }
            }

            double? robVlsfo = origin.RobVlsfo, robLsmgo = origin.RobLsmgo;
            if (robVlsfo.HasValue)
            {
                var after = robVlsfo.Value - vlsfoUse + supplyVlsfo;
                if (after < (bunker.MinVlsfo ?? 0)) { reserveRejections++; return null; }
                robVlsfo = after;
            }
            if (robLsmgo.HasValue)
            {
                var after = robLsmgo.Value - lsmgoUse + supplyLsmgo;
                if (after < (bunker.MinLsmgo ?? 0)) { reserveRejections++; return null; }
                robLsmgo = after;
            }

            var chargeable = outcome.Steps is { Count: > 0 } steps
                ? steps.Sum(step => step.Outcome.Co2Mt * ets.Factor(startUtc.AddHours(step.StartHours)))
                : outcome.Co2Mt * ets.Factor(startUtc);
            var steps2 = outcome.Steps;
            return origin with
            {
                Cost = origin.Cost + ArcCost(config, request, outcome, chargeable),
                TimeHours = origin.TimeHours + outcome.Hours,
                DistNm = origin.DistNm + (arc?.DistNm ?? 0),
                MainFuelMt = origin.MainFuelMt + outcome.MainFuelMt,
                EcaDistNm = origin.EcaDistNm + (inEca ? arc!.DistNm : 0),
                EcaTimeHours = origin.EcaTimeHours + (inEca ? outcome.Hours : 0),
                EcaFuelMt = origin.EcaFuelMt + (inEca ? outcome.MainFuelMt : 0),
                AuxGoMt = origin.AuxGoMt + outcome.AuxGoMt,
                Co2Mt = origin.Co2Mt + outcome.Co2Mt,
                ChargeableCo2Mt = origin.ChargeableCo2Mt + chargeable,
                FuelCostUsd = origin.FuelCostUsd + outcome.FuelCostUsd,
                Exposure = origin.Exposure + outcome.ExposureScore,
                ExceedHours = origin.ExceedHours + outcome.ExceedHoursValue,
                SyncHours = origin.SyncHours + outcome.SyncRollHours,
                ParametricHours = origin.ParametricHours + outcome.ParametricRollHours,
                PitchHours = origin.PitchHours + outcome.PitchHours,
                BlowerHours = origin.BlowerHours + outcome.BlowerHours,
                BoostHours = origin.BoostHours + (outcome.Mode == PowerMode.Boost && !outcome.IsWait ? outcome.Hours : 0),
                WaitHours = origin.WaitHours + (outcome.IsWait ? outcome.Hours : 0),
                MaxWaveM = steps2 == null ? origin.MaxWaveM : Math.Max(origin.MaxWaveM, steps2.Max(step => step.Input.WaveM)),
                MaxWindKn = steps2 == null ? origin.MaxWindKn : Math.Max(origin.MaxWindKn, steps2.Max(step => step.Input.WindKn)),
                MaxLoad = Math.Max(origin.MaxLoad, outcome.LoadFraction ?? 0),
                Constrained = origin.Constrained || (arc?.ZoneConstrained ?? false),
                RobVlsfo = robVlsfo,
                RobLsmgo = robLsmgo,
                SupplyMask = mask,
                Parent = origin,
                At = at,
                Arc = outcome,
            };
        }

        Label? WaitLabel(Label origin, double hours, (int Col, int Lane) at)
        {
            var auxMt = auxRate / 24 * hours;
            var outcome = new ArcOutcome(
                candidates[0], PowerMode.HoldRpm, 0, 0, hours, 0, auxMt, auxMt * VesselSpeedProfile.Co2Factor("LSMGO"), auxMt * physics.GoPriceUsdPerMt,
                0, null, null, 0, false, 0, 0, 0, 0, 0, null, null, true);
            return Advance(origin, null, outcome, at);
        }

        var best = new Dictionary<(int Col, int Lane), List<Label>>
        {
            [(0, 0)] = new() { Label.Start(bunker.StartVlsfo, bunker.StartLsmgo) },
        };

        for (var col = 0; col < last; col++)
        {
            ct.ThrowIfCancellationRequested();
            if (DateTime.UtcNow > options.Deadline) return new StageResult(null, reserveRejections, true, evaluations);
            progress?.Report((col + 1.0) / last);

            for (var fromLane = 0; fromLane < lattice.Nodes[col].Length; fromLane++)
            {
                if (!Allowed(col, fromLane)) continue;
                if (lattice.Mandatory.Contains(col) && fromLane != lattice.MidLane) continue;
                if (!best.TryGetValue((col, fromLane), out var fromLabels)) continue;

                var departures = new List<Label>(fromLabels);
                if (options.AllowWaiting)
                {
                    foreach (var label in fromLabels)
                    {
                        foreach (var hours in WaitOptionsHours)
                        {
                            if (label.WaitHours + hours > MaxWaitHours) continue;
                            if (WaitLabel(label, hours, (col, fromLane)) is { } waited) departures.Add(waited);
                        }
                    }
                }

                for (var toLane = 0; toLane < lattice.Nodes[col + 1].Length; toLane++)
                {
                    if (!Allowed(col + 1, toLane)) continue;
                    if (lattice.Mandatory.Contains(col + 1) && toLane != lattice.MidLane) continue;
                    var arc = GetArc(lattice, zones, bunker, col, fromLane, toLane);
                    if (arc == null) continue;
                    var subSteps = options.Fine ? Math.Clamp((int)Math.Ceiling(arc.DistNm / SubStepNm), 1, MaxSubSteps) : 1;

                    foreach (var label in departures)
                    {
                        var startUtc = request.EtdUtc.AddHours(label.TimeHours);
                        foreach (var setting in candidates)
                        {
                            foreach (var mode in options.Modes)
                            {
                                evaluations++;
                                var outcome = await EvaluateSegmentAsync(lattice, forecast, arc, col, fromLane, toLane, startUtc, setting, mode, physics, subSteps, ct);
                                if (outcome == null) continue;
                                if (Advance(label, arc, outcome, (col + 1, toLane)) is not { } next) continue;
                                if (!best.TryGetValue((col + 1, toLane), out var targets)) best[(col + 1, toLane)] = targets = new List<Label>();
                                targets.Add(next);
                            }
                        }
                    }
                }
            }

            foreach (var key in best.Keys.Where(key => key.Col == col + 1).ToList())
                best[key] = PruneLabels(best[key], trackRob, options.MaxLabels);
        }

        best.TryGetValue((last, 0), out var finals);
        return new StageResult(finals is { Count: > 0 } ? finals : null, reserveRejections, false, evaluations);
    }

    /// <summary>One lattice arc sailed in sub-steps; each sub-step samples the forecast at its midpoint and at the time the vessel actually gets there.</summary>
    private async Task<ArcOutcome?> EvaluateSegmentAsync(
        Lattice lattice, ForecastCache forecast, ArcGeometry arc, int col, int fromLane, int toLane, DateTime startUtc, SpeedSetting setting, PowerMode mode,
        ArcPhysicsContext physics, int subSteps, CancellationToken ct)
    {
        var steps = new List<SegmentStep>(subSteps);
        var elapsed = 0.0;
        for (var index = 0; index < subSteps; index++)
        {
            var to = (index + 1) / (double)subSteps;
            var mid = (index + 0.5) / subSteps;
            var lat = arc.FromLat + (arc.ToLat - arc.FromLat) * mid;
            var lng = arc.FromLng + (arc.ToLng - arc.FromLng) * mid;

            var at = startUtc.AddHours(elapsed);
            var set = await GetForecastAsync(lattice, col, at, forecast, ct);
            var slot = new DateTime(at.Year, at.Month, at.Day, at.Hour / ForecastStepHours * ForecastStepHours, 0, 0, DateTimeKind.Utc).Ticks;
            var key = (col, fromLane, toLane, index * 100 + subSteps, slot);
            if (!lattice.Inputs.TryGetValue(key, out var input))
            {
                var (windKn, windFrom) = Sample(set.Wind, lat, lng);
                var (waveM, waveFrom) = Sample(set.Waves, lat, lng);
                var (currentKn, currentTo) = Sample(set.Currents, lat, lng);
                var period = set.Period == null ? (double?)null : Sample(set.Period, lat, lng).Mag;
                input = new ArcInput(arc.DistNm / subSteps, arc.Bearing, windKn, windFrom, waveM, waveFrom, period > 0 ? period : null, currentKn, currentTo,
                    arc.InEca, arc.ZoneCap, arc.ZoneRpmMin, arc.ZoneRpmMax, arc.ZoneConstrained);
                lattice.Inputs[key] = input;
            }

            var outcome = ArcPhysics.Evaluate(input, setting, mode, physics);
            if (outcome == null) return null;
            steps.Add(new SegmentStep(
                input, elapsed, lat, lng,
                arc.FromLat + (arc.ToLat - arc.FromLat) * to, arc.FromLng + (arc.ToLng - arc.FromLng) * to, outcome));
            elapsed += outcome.Hours;
        }
        return ArcOutcome.Combine(setting, mode, steps, arc.DistNm);
    }

    private static double ArcCost(ScenarioConfig config, OptimizationRequestDto request, ArcOutcome outcome, double chargeableCo2Mt)
    {
        if (config.TightenSafetyLimits) return outcome.ExposureScore + 1e-6 * (request.HireRatePerDayUsd / 24 * outcome.Hours + outcome.FuelCostUsd);
        if (config.Key == "earliest-eta") return outcome.Hours;
        if (config.OptimizeRawFuelQuantity) return outcome.MainFuelMt + outcome.AuxGoMt;
        if (config.OptimizeRawCo2Quantity) return outcome.Co2Mt;
        return config.WeightTime * (request.HireRatePerDayUsd / 24) * outcome.Hours
             + config.WeightFuel * outcome.FuelCostUsd
             + config.WeightEmission * request.EuaPriceUsdPerTonneCo2 * chargeableCo2Mt;
    }

    /// <summary>The planned route for savings comparisons: the original waypoints (middle lane only) at the CP default operating point, held constant, with the same forecast.</summary>
    private async Task<ScenarioTotals?> ComputeBaselineAsync(
        OptimizationRequestDto request, Vessel vessel, List<SpeedSetting> configuredSpeeds, ArcPhysicsContext physics, Lattice lattice, ZoneSet zones,
        ForecastCache forecast, BunkerContext bunker, EtsContext ets, CancellationToken ct)
    {
        var plan = configuredSpeeds.FirstOrDefault(setting => setting.IsCpDefault);
        if (plan == null) return null;

        var nodes = new HashSet<(int Col, int Lane)>();
        for (var col = 1; col < lattice.ColumnCount - 1; col++) nodes.Add((col, lattice.MidLane));
        var context = physics with { Model = VesselPerformanceModel.Build(configuredSpeeds), Tally = null };
        var config = ScenarioConfig.Resolve("maintain-cp", request, vessel);
        try
        {
            var stage = await RunStageAsync(request, config, new List<SpeedSetting> { plan }, context, lattice, zones, forecast, bunker, ets,
                new StageOptions(true, false, nodes, FineMaxLabels, DateTime.UtcNow.AddSeconds(60), new[] { PowerMode.HoldRpm }), null, ct);
            return stage.Finals is { Count: > 0 } ? TotalsOf(request, stage.Finals.MinBy(label => label.Cost)!) : null;
        }
        catch (ForecastUnavailableException)
        {
            return null;
        }
    }

    /// <summary>Pareto labels on cost, time and (when tracked) ROB; the weather depends on arrival time, so a cheaper-but-later label is not discarded.</summary>
    private static List<Label> PruneLabels(List<Label> labels, bool trackRob, int maxLabels)
    {
        if (labels.Count <= 1) return labels;

        var kept = new List<Label>();
        foreach (var label in labels.OrderBy(label => label.Cost).ThenBy(label => label.TimeHours))
        {
            var dominated = kept.Any(other =>
                other.TimeHours <= label.TimeHours + 1e-9 &&
                (!trackRob || (other.RobVlsfo ?? 0) >= (label.RobVlsfo ?? 0) - 1e-9 && (other.RobLsmgo ?? 0) >= (label.RobLsmgo ?? 0) - 1e-9));
            if (!dominated) kept.Add(label);
        }

        if (kept.Count <= maxLabels) return kept;
        return kept.GroupBy(label => (int)(label.TimeHours / 0.5)).Select(group => group.MinBy(label => label.Cost)!).Take(maxLabels).ToList();
    }
}
