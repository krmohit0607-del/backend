using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Entities.Optimization;
using MultiTenantSaaS.Domain.Entities.Performance;
using System.Text.Json;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>
/// Native C# weather/zone-aware route optimization engine. Builds a lateral
/// corridor lattice (origin -&gt; destination), then solves a time-expanded
/// DAG shortest path (forward column-by-column dynamic programming — valid
/// because the lattice only has forward arcs, so this is Dijkstra over a
/// DAG without needing a priority queue) picking the cheapest combination of
/// waypoint + discrete speed-setting per the active scenario's weighted
/// time/fuel/emission objective. Mirrors the TNM OptiNav C++ reference
/// engine's cost-model shape (see <see cref="ScenarioConfig"/>) but reads
/// weather from <see cref="IWeatherDataService"/> (our own Open-Meteo-backed
/// cache) and area constraints from the request payload instead of
/// GRIB/Redis/SQLite/GDAL.
/// </summary>
public class RouteOptimizationEngine : IRouteOptimizationEngine
{
    private const int Columns = 14;
    private const int Laterals = 5; // odd count, centre lane = direct track
    private const double Co2PerTonneFuel = 3.114; // VLSFO combustion factor, tCO2/tFuel

    private readonly IApplicationDbContext _db;
    private readonly IPerformanceDbContext _perfDb;
    private readonly IWeatherDataService _weather;

    public RouteOptimizationEngine(IApplicationDbContext db, IPerformanceDbContext perfDb, IWeatherDataService weather)
    {
        _db = db;
        _perfDb = perfDb;
        _weather = weather;
    }

    public async Task<OptimizationRunResultDto> RunAsync(OptimizationRequestDto request, CancellationToken cancellationToken = default)
    {
        var vessel = await _db.Vessels.AsNoTracking().FirstOrDefaultAsync(v => v.Imo == request.VesselImo, cancellationToken)
            ?? throw new InvalidOperationException($"Vessel with IMO '{request.VesselImo}' was not found.");

        var candidateSpeeds = VesselSpeedProfile.BuildCandidateSpeeds(vessel, request.LoadCondition);
        var dwt = double.TryParse(vessel.Deadweight, out var dwtVal) ? dwtVal : 50000;
        bool laden = string.Equals(request.LoadCondition, "Laden", StringComparison.OrdinalIgnoreCase);
        double D(string? s, double fallback) => double.TryParse(s, out var v) ? v : fallback;
        var maxSwh = D(laden ? vessel.WslMaxSwhLaden : vessel.WslMaxSwhBallast, 6.0);
        var maxWindKn = D(laden ? vessel.WslMaxWindsLaden : vessel.WslMaxWindsBallast, 45.0);

        var weatherFactorCache = await _perfDb.WeatherFactorEntries.AsNoTracking().ToListAsync(cancellationToken);

        var run = new OptimizationRun
        {
            TenantId = null,
            VoyageId = request.VoyageId,
            VesselImo = request.VesselImo,
            EtdUtc = request.EtdUtc,
            OriginLat = request.OriginLat,
            OriginLng = request.OriginLng,
            DestLat = request.DestLat,
            DestLng = request.DestLng,
            LoadCondition = request.LoadCondition,
            HireRatePerDayUsd = request.HireRatePerDayUsd,
            FoPriceUsdPerMt = request.FoPriceUsdPerMt,
            GoPriceUsdPerMt = request.GoPriceUsdPerMt,
            EuaPriceUsdPerTonneCo2 = request.EuaPriceUsdPerTonneCo2,
            ScenariosRequestedJson = JsonSerializer.Serialize(request.Scenarios),
            RestrictedAreasJson = JsonSerializer.Serialize(request.RestrictedAreas),
        };

        var resultDtos = new List<OptimizationScenarioResultDto>();
        foreach (var scenarioId in request.Scenarios.Distinct())
        {
            var config = ScenarioConfig.Resolve(scenarioId, request, vessel);
            var dto = await SolveScenarioAsync(request, config, candidateSpeeds, dwt, maxSwh, maxWindKn, weatherFactorCache, cancellationToken);
            resultDtos.Add(dto);

            run.ScenarioResults.Add(new OptimizationScenarioResult
            {
                OptimizationRunId = run.Id,
                Scenario = dto.Scenario,
                RouteWaypointsJson = JsonSerializer.Serialize(dto.Waypoints),
                EtdUtc = dto.EtdUtc,
                EtaUtc = dto.EtaUtc,
                DistanceNm = dto.DistanceNm,
                AverageSpeedKn = dto.AverageSpeedKn,
                TimeToGoHours = dto.TimeToGoHours,
                EcaDistanceNm = dto.EcaDistanceNm,
                EcaTimeToGoHours = dto.EcaTimeToGoHours,
                MeFuelRequiredToGoMt = dto.MeFuelRequiredToGoMt,
                EcaFuelRequiredToGoMt = dto.EcaFuelRequiredToGoMt,
                TotalCostToGoUsd = dto.TotalCostToGoUsd,
                FuelCostToGoUsd = dto.FuelCostToGoUsd,
                TimeCostToGoUsd = dto.TimeCostToGoUsd,
                EmissionsCostToGoUsd = dto.EmissionsCostToGoUsd,
                MeCo2EmissionsToGoMt = dto.MeCo2EmissionsToGoMt,
                WasConstrainedByZones = dto.WasConstrainedByZones,
                IsFeasible = dto.IsFeasible,
                InfeasibleReason = dto.InfeasibleReason,
            });
        }

        _db.OptimizationRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        return new OptimizationRunResultDto
        {
            OptimizationRunId = run.Id,
            RequestedAtUtc = run.RequestedAtUtc,
            Scenarios = resultDtos,
        };
    }

    private sealed record Label(double Cost, double TimeHours, double DistNm, double FuelMt, double EcaDistNm, double EcaTimeHours, double EcaFuelMt, bool Constrained, (int Col, int Lane)? Prev, double SpeedUsedKn);

    private async Task<OptimizationScenarioResultDto> SolveScenarioAsync(
        OptimizationRequestDto request,
        ScenarioConfig config,
        List<SpeedSetting> allSpeeds,
        double dwt,
        double maxSwh,
        double maxWindKn,
        List<WeatherFactorEntry> factorTable,
        CancellationToken cancellationToken)
    {
        var candidateSpeeds = config.RestrictToSingleSpeed
            ? new List<SpeedSetting> { allSpeeds[allSpeeds.Count / 2] }
            : allSpeeds;

        var origin = (Lat: request.OriginLat, Lng: request.OriginLng);
        var dest = (Lat: request.DestLat, Lng: request.DestLng);
        var directNm = OptimizationGeoMath.DistanceNm(origin.Lat, origin.Lng, dest.Lat, dest.Lng);
        var directBearing = OptimizationGeoMath.InitialBearingDeg(origin.Lat, origin.Lng, dest.Lat, dest.Lng);
        var maxDeviationNm = request.MaxDeviationNm ?? 60.0;

        var noGoRings = request.RestrictedAreas.Where(a => a.ZoneType == "no-go-zone").SelectMany(a => a.Rings).ToList();
        var ecaRings = request.RestrictedAreas.Where(a => a.ZoneType == "eca-zone").SelectMany(a => a.Rings).ToList();
        var speedZones = request.RestrictedAreas.Where(a => a.ZoneType is "speed-control-zone" or "limited-passage-zone").ToList();

        // --- Build the lattice ---
        var nodes = new (double Lat, double Lng)[Columns][];
        nodes[0] = new[] { origin };
        nodes[Columns - 1] = new[] { dest };
        var midLaneIndex = (Laterals - 1) / 2;
        for (var col = 1; col < Columns - 1; col++)
        {
            var frac = col / (double)(Columns - 1);
            var (baseLat, baseLng) = OptimizationGeoMath.Destination(origin.Lat, origin.Lng, directBearing, frac * directNm);
            var lane = new (double, double)[Laterals];
            var perpBearing = (directBearing + 90.0) % 360.0;
            var step = midLaneIndex > 0 ? maxDeviationNm / midLaneIndex : 0;
            for (var l = 0; l < Laterals; l++)
            {
                var offsetNm = (l - midLaneIndex) * step;
                lane[l] = Math.Abs(offsetNm) < 1e-9
                    ? (baseLat, baseLng)
                    : OptimizationGeoMath.Destination(baseLat, baseLng, perpBearing, offsetNm);
            }
            nodes[col] = lane;
        }

        // --- Forward DAG dynamic programming ---
        var best = new Dictionary<(int Col, int Lane), Label>
        {
            [(0, 0)] = new Label(0, 0, 0, 0, 0, 0, 0, false, null, 0)
        };

        for (var col = 0; col < Columns - 1; col++)
        {
            var nextNodes = nodes[col + 1];

            // One weather fetch per column (not per arc/speed) covering the lateral span, at an ETA estimated from a representative (median) speed — keeps external/API calls to ~Columns instead of Columns*Laterals*Speeds.
            var refSpeedKn = candidateSpeeds[candidateSpeeds.Count / 2].SpeedKn;
            var estHours = ((col + 1) / (double)(Columns - 1)) * directNm / Math.Max(1, refSpeedKn);
            var estUtc = request.EtdUtc.AddHours(estHours);
            var south = nextNodes.Min(n => n.Lat) - 0.5;
            var north = nextNodes.Max(n => n.Lat) + 0.5;
            var west = nextNodes.Min(n => n.Lng) - 0.5;
            var east = nextNodes.Max(n => n.Lng) + 0.5;

            var windGrid = await _weather.GetGridAsync("wind", estUtc, south, west, north, east, 3, 3, cancellationToken);
            var waveGrid = await _weather.GetGridAsync("waves", estUtc, south, west, north, east, 3, 3, cancellationToken);
            var currentGrid = await _weather.GetGridAsync("currents", estUtc, south, west, north, east, 3, 3, cancellationToken);

            for (var fromLane = 0; fromLane < nodes[col].Length; fromLane++)
            {
                if (!best.TryGetValue((col, fromLane), out var label)) continue;
                var (fromLat, fromLng) = nodes[col][fromLane];

                for (var toLane = 0; toLane < nextNodes.Length; toLane++)
                {
                    var (toLat, toLng) = nextNodes[toLane];
                    var midLat = (fromLat + toLat) / 2;
                    var midLng = (fromLng + toLng) / 2;

                    if (OptimizationGeoMath.PointInAnyRing(midLat, midLng, noGoRings) ||
                        OptimizationGeoMath.PointInAnyRing(toLat, toLng, noGoRings))
                    {
                        continue; // hard exclusion — no-go zone
                    }

                    var distNm = OptimizationGeoMath.DistanceNm(fromLat, fromLng, toLat, toLng);
                    if (distNm < 1e-6) continue;
                    var bearing = OptimizationGeoMath.InitialBearingDeg(fromLat, fromLng, toLat, toLng);

                    var (windMagKn, windDirDeg) = SampleGrid(windGrid, midLat, midLng);
                    var (waveMagM, waveDirDeg) = SampleGrid(waveGrid, midLat, midLng);
                    var (currentMagKn, currentDirDeg) = SampleGrid(currentGrid, midLat, midLng);

                    if (config.TightenSafetyLimits && (waveMagM > maxSwh || windMagKn > maxWindKn))
                    {
                        continue; // "Safest Route" — hard-exclude arcs that exceed the vessel's weather safety limits
                    }

                    var inEca = OptimizationGeoMath.PointInAnyRing(midLat, midLng, ecaRings);
                    var zoneSpeedCapKn = speedZones
                        .Where(z => OptimizationGeoMath.PointInAnyRing(midLat, midLng, z.Rings) && z.SpeedMaxKn.HasValue)
                        .Select(z => z.SpeedMaxKn!.Value)
                        .DefaultIfEmpty(double.MaxValue)
                        .Min();
                    var constrainedByZone = zoneSpeedCapKn < double.MaxValue;

                    var windFactorPct = LookupWeatherFactorPercent(factorTable, "Wind", dwt, BeaufortFromKnots(windMagKn), OptimizationGeoMath.FoldRelativeAngle(bearing, windDirDeg));
                    var waveFactorPct = LookupWeatherFactorPercent(factorTable, "Wave", dwt, waveMagM, OptimizationGeoMath.FoldRelativeAngle(bearing, waveDirDeg));
                    var currentFoldedAngle = OptimizationGeoMath.FoldRelativeAngle(bearing, currentDirDeg);
                    var currentFactorFraction = CurrentFactorFraction(currentFoldedAngle);
                    var currentSign = currentFoldedAngle <= 90 ? -1.0 : 1.0; // head-ish => opposing, stern-ish => favourable

                    foreach (var speedSetting in candidateSpeeds)
                    {
                        var baseSpeedKn = Math.Min(speedSetting.SpeedKn, zoneSpeedCapKn);
                        if (baseSpeedKn <= 0.5) continue;

                        var speedThroughWaterKn = baseSpeedKn * (1 - (windFactorPct + waveFactorPct) / 100.0);
                        var speedOverGroundKn = speedThroughWaterKn + currentMagKn * currentFactorFraction * currentSign;
                        if (speedOverGroundKn <= 0.5) continue; // infeasible — weather kills forward progress

                        var timeHours = distNm / speedOverGroundKn;
                        // Fuel burn is ~constant per hour at a fixed RPM regardless of weather; weather instead reduces
                        // speed-made-good, so the same hourly burn now covers less distance — correctly captured by
                        // using the unmodified reference fuel rate against the (now larger) weather-adjusted time.
                        var fuelMt = speedSetting.FuelRateMtPerDay / 24.0 * timeHours;

                        var ecaDistNm = inEca ? distNm : 0;
                        var ecaTimeHours = inEca ? timeHours : 0;
                        var ecaFuelMt = inEca ? fuelMt : 0;
                        var co2Mt = fuelMt * Co2PerTonneFuel;

                        double arcCost;
                        if (config.OptimizeRawFuelQuantity) arcCost = fuelMt;
                        else if (config.OptimizeRawCo2Quantity) arcCost = co2Mt;
                        else
                        {
                            arcCost = config.WeightTime * (request.HireRatePerDayUsd / 24.0) * timeHours
                                    + config.WeightFuel * request.FoPriceUsdPerMt * fuelMt
                                    + config.WeightEmission * request.EuaPriceUsdPerTonneCo2 * co2Mt;
                        }

                        var newCost = label.Cost + arcCost;
                        var newTime = label.TimeHours + timeHours;
                        if (col + 1 == Columns - 1 && config.TargetEtaUtc.HasValue)
                        {
                            var targetHours = (config.TargetEtaUtc.Value - request.EtdUtc).TotalHours;
                            newCost += Math.Abs(newTime - targetHours) * config.EtaPenaltyPerHourUsd;
                        }

                        var candidate = new Label(
                            newCost, newTime, label.DistNm + distNm, label.FuelMt + fuelMt,
                            label.EcaDistNm + ecaDistNm, label.EcaTimeHours + ecaTimeHours, label.EcaFuelMt + ecaFuelMt,
                            label.Constrained || constrainedByZone, (col, fromLane), speedOverGroundKn);

                        if (!best.TryGetValue((col + 1, toLane), out var existing) || candidate.Cost < existing.Cost)
                        {
                            best[(col + 1, toLane)] = candidate;
                        }
                    }
                }
            }
        }

        if (!best.TryGetValue((Columns - 1, 0), out var finalLabel))
        {
            return new OptimizationScenarioResultDto
            {
                Scenario = config.Key,
                IsFeasible = false,
                InfeasibleReason = "No feasible path found — no-go zones or safety limits excluded every route through the search corridor.",
                EtdUtc = request.EtdUtc,
                EtaUtc = request.EtdUtc,
            };
        }

        // --- Reconstruct path ---
        var waypoints = new List<RouteWaypointDto>();
        var chain = new List<(int Col, int Lane, Label Label)>();
        var cursor = ((Columns - 1, 0), finalLabel);
        while (true)
        {
            chain.Add((cursor.Item1.Item1, cursor.Item1.Item2, cursor.Item2));
            if (cursor.Item2.Prev is not { } prev) break;
            cursor = (prev, best[prev]);
        }
        chain.Reverse();
        foreach (var (col, lane, label) in chain)
        {
            var (lat, lng) = nodes[col][lane];
            waypoints.Add(new RouteWaypointDto
            {
                Lat = lat,
                Lng = lng,
                EtaUtc = request.EtdUtc.AddHours(label.TimeHours),
                SpeedKn = label.SpeedUsedKn,
                CumulativeDistanceNm = label.DistNm,
            });
        }

        return new OptimizationScenarioResultDto
        {
            Scenario = config.Key,
            IsFeasible = true,
            WasConstrainedByZones = finalLabel.Constrained,
            Waypoints = waypoints,
            EtdUtc = request.EtdUtc,
            EtaUtc = request.EtdUtc.AddHours(finalLabel.TimeHours),
            DistanceNm = finalLabel.DistNm,
            AverageSpeedKn = finalLabel.TimeHours > 0 ? finalLabel.DistNm / finalLabel.TimeHours : 0,
            TimeToGoHours = finalLabel.TimeHours,
            EcaDistanceNm = finalLabel.EcaDistNm,
            EcaTimeToGoHours = finalLabel.EcaTimeHours,
            MeFuelRequiredToGoMt = finalLabel.FuelMt,
            EcaFuelRequiredToGoMt = finalLabel.EcaFuelMt,
            FuelCostToGoUsd = request.FoPriceUsdPerMt * finalLabel.FuelMt,
            TimeCostToGoUsd = (request.HireRatePerDayUsd / 24.0) * finalLabel.TimeHours,
            EmissionsCostToGoUsd = request.EuaPriceUsdPerTonneCo2 * finalLabel.FuelMt * Co2PerTonneFuel,
            TotalCostToGoUsd = request.FoPriceUsdPerMt * finalLabel.FuelMt
                              + (request.HireRatePerDayUsd / 24.0) * finalLabel.TimeHours
                              + request.EuaPriceUsdPerTonneCo2 * finalLabel.FuelMt * Co2PerTonneFuel,
            MeCo2EmissionsToGoMt = finalLabel.FuelMt * Co2PerTonneFuel,
        };
    }

    private static (double Mag, double Dir) SampleGrid(Application.DTOs.Weather.WeatherGridDto grid, double lat, double lng)
    {
        if (grid.Cols <= 0 || grid.Rows <= 0 || grid.Mag.Length == 0) return (0, 0);
        var colFrac = grid.East > grid.West ? (lng - grid.West) / (grid.East - grid.West) : 0.5;
        var rowFrac = grid.North > grid.South ? (lat - grid.South) / (grid.North - grid.South) : 0.5;
        var col = Math.Clamp((int)Math.Round(colFrac * (grid.Cols - 1)), 0, grid.Cols - 1);
        var row = Math.Clamp((int)Math.Round(rowFrac * (grid.Rows - 1)), 0, grid.Rows - 1);
        var idx = row * grid.Cols + col;
        if (idx < 0 || idx >= grid.Mag.Length) return (0, 0);
        var dir = grid.Dir.Length > idx ? grid.Dir[idx] : 0;
        return (grid.Mag[idx], dir);
    }

    private static int BeaufortFromKnots(double knots)
    {
        double[] upperBoundsKn = { 1, 3, 6, 10, 16, 21, 27, 33, 40, 47, 55, 63 };
        for (var i = 0; i < upperBoundsKn.Length; i++)
        {
            if (knots <= upperBoundsKn[i]) return i;
        }
        return 12;
    }

    /// <summary>Signed percentage speed-loss factor (positive = adverse) for Wind/Wave, nearest-match against the calibration table ported from the legacy FleetView export.</summary>
    private static double LookupWeatherFactorPercent(List<WeatherFactorEntry> table, string weather, double dwt, double magnitude, double foldedAngleDeg)
    {
        var rows = table.Where(r => string.Equals(r.Weather, weather, StringComparison.OrdinalIgnoreCase)).ToList();
        if (rows.Count == 0) return 0;

        var nearestDwt = rows.Select(r => r.DeadWeight).Distinct().OrderBy(d => Math.Abs(d - dwt)).First();
        var atDwt = rows.Where(r => r.DeadWeight == nearestDwt).ToList();
        var nearestMag = atDwt.Select(r => r.Magnitude).Distinct().OrderBy(m => Math.Abs(m - magnitude)).First();
        var atMag = atDwt.Where(r => r.Magnitude == nearestMag).OrderBy(r => r.Angle).ToList();
        if (atMag.Count == 0) return 0;

        // piecewise-linear interpolation between the two angle rows bracketing foldedAngleDeg
        for (var i = 0; i < atMag.Count - 1; i++)
        {
            var a0 = atMag[i];
            var a1 = atMag[i + 1];
            if (foldedAngleDeg >= a0.Angle && foldedAngleDeg <= a1.Angle && a1.Angle > a0.Angle)
            {
                var t = (foldedAngleDeg - a0.Angle) / (a1.Angle - a0.Angle);
                return a0.Value + t * (a1.Value - a0.Value);
            }
        }
        return atMag.OrderBy(r => Math.Abs(r.Angle - foldedAngleDeg)).First().Value;
    }

    /// <summary>Current factor per the confirmed rule: Head/Stern = 100%, Quarter = 50%, Beam = 25%, piecewise-linear interpolation of the along-track component for angles in between.</summary>
    private static double CurrentFactorFraction(double foldedAngleDeg)
    {
        var points = new (double Angle, double Pct)[] { (0, 100), (45, 50), (90, 25), (135, 50), (180, 100) };
        for (var i = 0; i < points.Length - 1; i++)
        {
            if (foldedAngleDeg >= points[i].Angle && foldedAngleDeg <= points[i + 1].Angle)
            {
                var t = (foldedAngleDeg - points[i].Angle) / (points[i + 1].Angle - points[i].Angle);
                return (points[i].Pct + t * (points[i + 1].Pct - points[i].Pct)) / 100.0;
            }
        }
        return 1.0;
    }
}
