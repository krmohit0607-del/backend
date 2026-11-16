using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Application.DTOs.Weather;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Entities.Optimization;
using MultiTenantSaaS.Domain.Entities.Performance;
using System.Text.Json;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>
/// Weather, zone, land, engine and bunker aware route optimizer. A lattice of lateral alternatives is built around the selected route
/// (its own waypoints are mandatory columns) and solved forward with time-aware labels, so arrival rules, engine power limits and bunker
/// reserves are honoured. Operating points come from the vessel's observed noon reports unless configured (CP) values are requested.
/// </summary>
public partial class RouteOptimizationEngine : IRouteOptimizationEngine
{
    private const int Columns = 14;
    private const int Laterals = 5;
    private const int ForecastStepHours = 6;
    private const double SupplyReachNm = 30;

    private readonly IApplicationDbContext _db;
    private readonly IPerformanceDbContext _perfDb;
    private readonly IWeatherDataService _weather;
    private readonly ICurrentUserService _currentUser;
    private readonly IEnsembleWindService? _ensemble;

    public RouteOptimizationEngine(IApplicationDbContext db, IPerformanceDbContext perfDb, IWeatherDataService weather, ICurrentUserService currentUser, IEnsembleWindService? ensemble = null)
    {
        _db = db;
        _perfDb = perfDb;
        _weather = weather;
        _currentUser = currentUser;
        _ensemble = ensemble;
    }

    public Task<OptimizationRunResultDto> RunAsync(OptimizationRequestDto request, CancellationToken cancellationToken = default) =>
        RunAsync(request, null, cancellationToken);

    public async Task<OptimizationRunResultDto> RunAsync(OptimizationRequestDto request, IProgress<OptimizationProgress>? progress, CancellationToken cancellationToken = default)
    {
        progress?.Report(new OptimizationProgress(1, "Validating request"));
        var (vessel, vesselNote) = await ResolveVesselAsync(request, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.VesselImo)) request.VesselImo = vessel.Imo;

        if (request.RouteCoordinates.Count < 2 || request.RouteCoordinates.Any(point =>
                point.Length != 2 || !double.IsFinite(point[0]) || !double.IsFinite(point[1]) ||
                point[0] < -180 || point[0] > 180 || point[1] < -90 || point[1] > 90))
            throw new InvalidOperationException("The selected route must contain at least two valid WGS84 GeoJSON coordinates in [longitude, latitude] order.");

        var routeCoordinates = request.RouteCoordinates.Select(point => (Lat: point[1], Lng: point[0])).ToArray();
        if (routeCoordinates.Zip(routeCoordinates.Skip(1), (a, b) => OptimizationGeoMath.DistanceNm(a.Lat, a.Lng, b.Lat, b.Lng)).Any(distance => distance < 0.01))
            throw new InvalidOperationException("The selected route contains duplicate consecutive coordinates.");

        var configuredSpeeds = new List<SpeedSetting>();
        string? configuredError = null;
        try
        {
            configuredSpeeds = VesselSpeedProfile.BuildCandidateSpeeds(vessel, request.LoadCondition, request.OperatingProfiles);
        }
        catch (InvalidOperationException ex)
        {
            configuredError = ex.Message;
        }

        var (calibratedSpeeds, calibrationText) = await ApplyApprovedCalibrationAsync(request, configuredSpeeds, cancellationToken);

        var hasImo = !string.IsNullOrWhiteSpace(request.VesselImo);
        var tracksheetQuery = _perfDb.TracksheetRows.AsNoTracking().Where(row => !row.IsDefault);
        tracksheetQuery = hasImo
            ? tracksheetQuery.Where(row => row.VesselImo == request.VesselImo)
            : tracksheetQuery.Where(row => row.VoyageId == request.VoyageId);
        var tracksheetRows = await tracksheetQuery
            .Take(20000)
            .ToListAsync(cancellationToken);
        var realPerformance = RealPerformanceProfile.Build(tracksheetRows, request.LoadCondition, vessel);

        var dwt = NumberParser.Positive(vessel.Deadweight)
            ?? throw new InvalidOperationException("The vessel has no valid DWT value; weather-factor optimization cannot be calculated without it.");
        var limits = VesselEngineLimits.From(vessel, request.LoadCondition, request, dwt);

        var factors = await _perfDb.WeatherFactorEntries.AsNoTracking().ToListAsync(cancellationToken);
        var wind = new WeatherFactorLookup(factors, "Wind");
        var wave = new WeatherFactorLookup(factors, "Wave");
        if (!wind.HasData || !wave.HasData)
            throw new InvalidOperationException("Validated wind and wave performance-factor tables are unavailable; no route recommendation was produced.");

        var physics = new ArcPhysicsContext(limits, wind, wave, request.FoPriceUsdPerMt, request.GoPriceUsdPerMt);
        var bunker = await BuildBunkerContextAsync(request, tracksheetRows, routeCoordinates, cancellationToken);
        var lattice = BuildLattice(request, routeCoordinates);
        var zones = new ZoneSet(request);
        var forecast = new ForecastCache();

        var commonNotes = BuildCommonNotes(limits, bunker);
        if (vesselNote != null) commonNotes.Insert(0, vesselNote);
        var ets = await ResolveEtsAsync(request, cancellationToken);
        progress?.Report(new OptimizationProgress(3, "Evaluating the planned route"));
        var baseline = await ComputeBaselineAsync(request, vessel, configuredSpeeds, physics, lattice, zones, forecast, bunker, ets, cancellationToken);

        var run = new OptimizationRun
        {
            TenantId = _currentUser.TenantId,
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
            InputSnapshotJson = JsonSerializer.Serialize(new { calculationVersion = "3", request }),
        };

        var scenarios = request.Scenarios.Distinct().ToList();
        var results = new List<OptimizationScenarioResultDto>();
        for (var scenarioIndex = 0; scenarioIndex < scenarios.Count; scenarioIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var config = ScenarioConfig.Resolve(scenarios[scenarioIndex], request, vessel);
            var notes = new List<string>();
            var basis = "CP";
            var usesConfigured = true;
            List<SpeedSetting> speeds;
            if (config.Key is "maintain-cp" or "custom-simulation" || request.PerformanceBasis == "CP")
            {
                speeds = calibratedSpeeds;
                notes.Add("Performance basis: configured CP (paper) Speed & Cons values.");
            }
            else if (realPerformance.Settings.Count > 0)
            {
                speeds = realPerformance.Settings;
                basis = "ACTUAL";
                usesConfigured = false;
                notes.AddRange(realPerformance.Notes);
            }
            else
            {
                speeds = calibratedSpeeds;
                notes.AddRange(realPerformance.Notes);
                notes.Add("Configured CP (paper) performance was used because the vessel has too little observed performance data.");
            }

            var baseFraction = scenarioIndex / (double)scenarios.Count;
            var share = 1.0 / scenarios.Count;
            var scenarioProgress = progress == null ? null : new Progress<double>(fraction =>
                progress.Report(new OptimizationProgress((int)Math.Min(97, 3 + (baseFraction + share * fraction) * 94), $"Scenario {scenarioIndex + 1} of {scenarios.Count}")));

            OptimizationScenarioResultDto dto;
            try
            {
                dto = speeds.Count == 0
                    ? Infeasible(config.Key, request, configuredError ?? string.Join(" ", realPerformance.Notes))
                    : await SolveAsync(request, config, speeds, limits, physics, lattice, zones, forecast, bunker, ets, baseline, scenarioProgress, notes, cancellationToken);
            }
            catch (ForecastUnavailableException ex)
            {
                dto = Infeasible(config.Key, request, ex.Message);
            }

            dto.Analysis.PerformanceBasis = basis;
            if (usesConfigured && calibrationText != null)
            {
                dto.Analysis.CalibrationApplied = calibrationText;
                dto.Warnings.Add(calibrationText);
            }
            dto.Warnings.AddRange(notes);
            dto.Warnings.AddRange(commonNotes);
            results.Add(dto);
        }

        RouteRanking.Apply(results, request.RankingWeights);

        foreach (var dto in results)
        {
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
                WarningsJson = JsonSerializer.Serialize(dto.Warnings),
                AnalysisJson = JsonSerializer.Serialize(dto.Analysis),
            });
        }

        progress?.Report(new OptimizationProgress(98, "Saving results"));
        _db.OptimizationRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);
        progress?.Report(new OptimizationProgress(100, "Completed"));

        return new OptimizationRunResultDto { OptimizationRunId = run.Id, RequestedAtUtc = run.RequestedAtUtc, Scenarios = results };
    }

    // ---------------------------------------------------------------- notes / bunker

    /// <summary>The vessel master record by IMO, then by name; if the vessel is not registered, a bare vessel built from the voyage's DWT.</summary>
    private async Task<(Vessel Vessel, string? Note)> ResolveVesselAsync(OptimizationRequestDto request, CancellationToken ct)
    {
        var imo = request.VesselImo?.Trim() ?? string.Empty;
        if (imo.Length > 0)
        {
            var byImo = await _db.Vessels.AsNoTracking().FirstOrDefaultAsync(v => v.Imo == imo, ct);
            if (byImo != null) return (byImo, null);
        }

        var name = request.VesselName?.Trim() ?? string.Empty;
        if (name.Length > 0)
        {
            var lower = name.ToLower();
            var byName = await _db.Vessels.AsNoTracking().FirstOrDefaultAsync(v => v.Name.ToLower() == lower, ct);
            if (byName != null) return (byName, null);
        }

        if (request.DeadweightMt is > 0)
        {
            var bare = new Vessel { Name = name, Imo = imo, Deadweight = request.DeadweightMt.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            return (bare, "The vessel is not in the vessel master, so only the voyage's DWT and the Speed & Cons rows were used: engine limits, load diagram, weather limits and calibrations were not applied. Register the vessel in Settings > Vessels for a full calculation.");
        }

        throw new InvalidOperationException("The vessel is not in the vessel master and the voyage has no deadweight (DWT) value. Register the vessel in Settings > Vessels, or enter the DWT on the voyage.");
    }

    // ---------------------------------------------------------------- notes / bunker

    private static List<string> BuildCommonNotes(VesselEngineLimits limits, BunkerContext bunker)
    {
        var notes = new List<string>
        {
            "Power boost and zone speed caps use the cube-law approximation (power and fuel proportional to speed cubed, constant SFOC); this is not a validated hydrodynamic model.",
        };
        if (limits.LoadDiagram.Count > 0)
            notes.Add($"Engine load diagram enforced between {limits.LoadDiagram.Min(p => p.Rpm):0} and {limits.LoadDiagram.Max(p => p.Rpm):0} RPM (min/max power and torque = 9.5493 x kW / RPM); outside that RPM range no diagram limit is applied.");
        else
            notes.Add("Torque and engine load-diagram limits are not evaluated because no load diagram is stored in the vessel profile; configured MCR/power fractions, RPM and critical-RPM limits are enforced.");
        if (!limits.InstalledKw.HasValue) notes.Add("Installed main-engine kW is missing, so engine load is not reported and power boost is unavailable.");
        else if (!limits.MaxLoadFraction.HasValue) notes.Add("No maximum MCR or power fraction is configured, so power boost is unavailable.");
        if (!limits.RollPeriodSec.HasValue) notes.Add("No roll period or GM is available, so synchronous and parametric roll screening was skipped.");
        if (limits.BlowerMin.HasValue && limits.BlowerMax.HasValue)
            notes.Add($"Blower cut-in/cut-off RPM band {limits.BlowerMin:0}-{limits.BlowerMax:0} is avoided: the recommended RPM is always below the cut-in or above the cut-off.");
        if (bunker.StartVlsfo == null && bunker.StartLsmgo == null)
            notes.Add("Remaining bunkers are unknown (no tracksheet ROB or supplied ROB), so reserve checks were skipped.");
        notes.AddRange(bunker.NotCounted.Select(text => $"Bunker supply not counted: {text}"));
        if (bunker.RobNote != null) notes.Add(bunker.RobNote);
        return notes;
    }

    private async Task<BunkerContext> BuildBunkerContextAsync(
        OptimizationRequestDto request, IReadOnlyList<TracksheetRow> rows, (double Lat, double Lng)[] route, CancellationToken ct)
    {
        var vlsfo = request.RobVlsfoMt;
        var lsmgo = request.RobLsmgoMt;
        string? robNote = null;
        if (!vlsfo.HasValue && !lsmgo.HasValue)
        {
            var voyageRows = rows.Where(row => row.VoyageId == request.VoyageId).OrderBy(row => row.SortOrder).ToList();
            var lastIndex = voyageRows.FindLastIndex(row => row.VlsfoRob.HasValue || row.LsmgoRob.HasValue);
            if (lastIndex >= 0)
            {
                var last = voyageRows[lastIndex];
                vlsfo = last.VlsfoRob;
                lsmgo = last.LsmgoRob;
                robNote = EstimateRobAtDeparture(request, voyageRows, lastIndex, ref vlsfo, ref lsmgo);
            }
        }

        var supplies = new List<SupplyPoint>();
        var notCounted = new List<string>();
        var requirements = string.IsNullOrWhiteSpace(request.VesselImo)
            ? new List<BunkerRequirement>()
            : await _db.BunkerRequirements.AsNoTracking().Where(b => b.Imo == request.VesselImo).ToListAsync(ct);
        string[] preBooking = { "Pending RFQ", "RFQ Sent", "Quotes Received", "Supplier Selected" };
        foreach (var requirement in requirements)
        {
            var label = $"{requirement.RequirementNo} at {requirement.BunkerPort}";
            if (preBooking.Contains(requirement.Status))
            {
                notCounted.Add($"{label} ({requirement.Status}, not confirmed).");
                continue;
            }
            if (requirement.Status != "Booked") continue;

            var fuel = requirement.FuelType.Trim().ToUpperInvariant();
            var isFo = fuel is "VLSFO";
            var isGo = fuel is "LSMGO" or "MGO";
            if (!isFo && !isGo)
            {
                notCounted.Add($"{label} (fuel grade {requirement.FuelType} is not tracked).");
                continue;
            }

            var portName = requirement.BunkerPort.Split('<', ',')[0].Trim().ToLowerInvariant();
            var port = await _db.Ports.AsNoTracking().FirstOrDefaultAsync(p => p.PortName.ToLower() == portName && p.Latitude != null && p.Longitude != null, ct);
            if (port == null)
            {
                notCounted.Add($"{label} (port coordinates unknown).");
                continue;
            }

            var lat = (double)port.Latitude!.Value;
            var lng = (double)port.Longitude!.Value;
            if (RouteDistanceToPointNm(route, lat, lng) > SupplyReachNm)
            {
                notCounted.Add($"{label} (more than {SupplyReachNm:0} NM from the selected route).");
                continue;
            }
            supplies.Add(new SupplyPoint($"{label} ({requirement.Quantity:0.#} MT {requirement.FuelType})", lat, lng, isFo ? requirement.Quantity : 0, isGo ? requirement.Quantity : 0));
        }

        var ports = await LoadPortCandidatesAsync(route, ct);
        return new BunkerContext(vlsfo, lsmgo, request.MinReserveVlsfoMt, request.MinReserveLsmgoMt, supplies.Take(60).ToList(), notCounted, ports, robNote);
    }

    /// <summary>The last report's ROB is reduced by the report's observed daily burn for the time between that report and departure (dead-reckoned start); ROB is left as reported when no burn rate or age is available.</summary>
    private static string? EstimateRobAtDeparture(OptimizationRequestDto request, List<TracksheetRow> voyageRows, int lastIndex, ref double? vlsfo, ref double? lsmgo)
    {
        var last = voyageRows[lastIndex];
        var reportAt = RealPerformanceProfile.ParseTimestamp(last.Date, last.Time);
        if (reportAt == DateTime.MinValue) return null;
        var elapsedHours = (request.EtdUtc - reportAt).TotalHours;
        if (elapsedHours <= 0.25) return null;
        if (elapsedHours > 72) return $"ROB is taken from the report of {reportAt:yyyy-MM-dd HH:mm} UTC and was not reduced because that report is more than 72 h older than the departure time.";

        var previous = lastIndex > 0 ? voyageRows[lastIndex - 1] : null;
        double? DailyBurn(double? before, double? after, double? bunkered) =>
            previous != null && last.Hrs is > 0 && before.HasValue && after.HasValue && (bunkered ?? 0) <= 0 && before >= after
                ? (before.Value - after.Value) / last.Hrs.Value * 24
                : null;

        var vlsfoBurn = DailyBurn(previous?.VlsfoRob, last.VlsfoRob, last.VlsfoBunkered);
        var lsmgoBurn = DailyBurn(previous?.LsmgoRob, last.LsmgoRob, last.LsmgoBunkered);
        if (vlsfoBurn == null && lsmgoBurn == null)
            return $"ROB is taken from the report of {reportAt:yyyy-MM-dd HH:mm} UTC and was not reduced for the {elapsedHours:0.#} h since then (no usable burn rate in that report).";

        if (vlsfoBurn.HasValue && vlsfo.HasValue) vlsfo = Math.Max(0, vlsfo.Value - vlsfoBurn.Value * elapsedHours / 24);
        if (lsmgoBurn.HasValue && lsmgo.HasValue) lsmgo = Math.Max(0, lsmgo.Value - lsmgoBurn.Value * elapsedHours / 24);
        return $"ROB was reduced for the {elapsedHours:0.#} h between the report of {reportAt:yyyy-MM-dd HH:mm} UTC and departure, using that report's observed burn (VLSFO {vlsfoBurn:0.0} MT/day, LSMGO {lsmgoBurn:0.00} MT/day).";
    }

    private async Task<List<PortCandidate>> LoadPortCandidatesAsync((double Lat, double Lng)[] route, CancellationToken ct)
    {
        var margin = BunkerPortComparison.MaxOffRouteNm / 60 + 0.5;
        var south = route.Min(point => point.Lat) - margin;
        var north = route.Max(point => point.Lat) + margin;
        var rows = await _db.Ports.AsNoTracking()
            .Where(port => port.Latitude != null && port.Longitude != null && port.Latitude >= (decimal)south && port.Latitude <= (decimal)north)
            .Select(port => new { port.PortName, port.Country, port.Latitude, port.Longitude })
            .ToListAsync(ct);
        return rows
            .Select(port => new PortCandidate(port.PortName, port.Country, (double)port.Latitude!.Value, (double)port.Longitude!.Value))
            .Where(port => RouteDistanceToPointNm(route, port.Lat, port.Lng) <= BunkerPortComparison.MaxOffRouteNm)
            .GroupBy(port => port.Name.ToLowerInvariant()).Select(group => group.First())
            .ToList();
    }

    private static double RouteDistanceToPointNm((double Lat, double Lng)[] route, double lat, double lng)
    {
        var best = double.MaxValue;
        for (var index = 0; index < route.Length - 1; index++)
            best = Math.Min(best, SegmentDistanceNm(route[index].Lat, route[index].Lng, route[index + 1].Lat, route[index + 1].Lng, lat, lng));
        return best;
    }

    private static double SegmentDistanceNm(double aLat, double aLng, double bLat, double bLng, double pLat, double pLng)
    {
        var cos = Math.Cos(OptimizationGeoMath.ToRadians((aLat + bLat) / 2));
        double X(double longitude) => (longitude - aLng) * cos * 60;
        double Y(double latitude) => (latitude - aLat) * 60;
        var bx = X(bLng); var by = Y(bLat); var px = X(pLng); var py = Y(pLat);
        var lengthSquared = bx * bx + by * by;
        var t = lengthSquared < 1e-9 ? 0 : Math.Clamp((px * bx + py * by) / lengthSquared, 0, 1);
        return Math.Sqrt(Math.Pow(px - t * bx, 2) + Math.Pow(py - t * by, 2));
    }

    // ---------------------------------------------------------------- lattice

    private sealed class ZoneSet
    {
        public ZoneSet(OptimizationRequestDto request)
        {
            NoGo = request.RestrictedAreas.Where(a => a.ZoneType == "no-go-zone").SelectMany(a => a.Rings).ToList();
            Eca = request.RestrictedAreas.Where(a => a.ZoneType == "eca-zone").SelectMany(a => a.Rings).ToList();
            Speed = request.RestrictedAreas.Where(a => a.ZoneType is "speed-control-zone" or "limited-passage-zone").ToList();
        }

        public List<List<double[]>> NoGo { get; }
        public List<List<double[]>> Eca { get; }
        public List<RestrictedAreaDto> Speed { get; }
    }

    private sealed class Lattice
    {
        public required double[] ColumnDistances { get; init; }
        public required (double Lat, double Lng)[][] Nodes { get; init; }
        public required HashSet<int> Mandatory { get; init; }
        public required int MidLane { get; init; }
        public int ColumnCount => ColumnDistances.Length;
        public Dictionary<(int Col, int From, int To), ArcGeometry?> Arcs { get; } = new();
        public Dictionary<(int Col, int From, int To, int Sub, long Slot), ArcInput> Inputs { get; } = new();
    }

    private sealed record ArcGeometry(
        double FromLat, double FromLng, double ToLat, double ToLng, double DistNm, double Bearing, double MidLat, double MidLng,
        bool InEca, double ZoneCap, double? ZoneRpmMin, double? ZoneRpmMax, bool ZoneConstrained, int[] Supplies);

    private static Lattice BuildLattice(OptimizationRequestDto request, (double Lat, double Lng)[] route)
    {
        var legs = route.Zip(route.Skip(1), (from, to) => OptimizationGeoMath.DistanceNm(from.Lat, from.Lng, to.Lat, to.Lng)).ToArray();
        var total = legs.Sum();
        var waypointDistances = new List<double> { 0 };
        foreach (var leg in legs) waypointDistances.Add(waypointDistances[^1] + leg);

        var columnDistances = Enumerable.Range(0, Columns)
            .Select(index => total * index / (Columns - 1))
            .Concat(waypointDistances.Skip(1).Take(waypointDistances.Count - 2))
            .OrderBy(distance => distance)
            .Aggregate(new List<double>(), (distances, distance) =>
            {
                if (distances.Count == 0 || distance - distances[^1] > 0.001) distances.Add(distance);
                return distances;
            }).ToArray();

        var mandatory = new HashSet<int>();
        foreach (var waypointDistance in waypointDistances.Skip(1).Take(waypointDistances.Count - 2))
        {
            var column = Array.FindIndex(columnDistances, distance => Math.Abs(distance - waypointDistance) <= 0.001);
            if (column > 0 && column < columnDistances.Length - 1) mandatory.Add(column);
        }

        var midLane = (Laterals - 1) / 2;
        var maxDeviation = request.MaxDeviationNm ?? 60.0;
        var nodes = new (double Lat, double Lng)[columnDistances.Length][];
        nodes[0] = new[] { route[0] };
        nodes[^1] = new[] { route[^1] };
        for (var col = 1; col < columnDistances.Length - 1; col++)
        {
            var distance = columnDistances[col];
            var centre = PointOnRoute(route, legs, distance);
            var before = PointOnRoute(route, legs, Math.Max(0, distance - 5));
            var after = PointOnRoute(route, legs, Math.Min(total, distance + 5));
            var perpendicular = (OptimizationGeoMath.InitialBearingDeg(before.Lat, before.Lng, after.Lat, after.Lng) + 90) % 360;
            var step = midLane > 0 ? maxDeviation / midLane : 0;
            var lane = new (double, double)[Laterals];
            for (var index = 0; index < Laterals; index++)
            {
                var offset = (index - midLane) * step;
                lane[index] = Math.Abs(offset) < 1e-9 ? centre : OptimizationGeoMath.Destination(centre.Lat, centre.Lng, perpendicular, offset);
            }
            nodes[col] = lane;
        }

        return new Lattice { ColumnDistances = columnDistances, Nodes = nodes, Mandatory = mandatory, MidLane = midLane };
    }

    private static (double Lat, double Lng) PointOnRoute((double Lat, double Lng)[] points, double[] legs, double distanceNm)
    {
        var remaining = Math.Clamp(distanceNm, 0, legs.Sum());
        for (var index = 0; index < legs.Length; index++)
        {
            if (remaining > legs[index] && index < legs.Length - 1)
            {
                remaining -= legs[index];
                continue;
            }
            var from = points[index];
            var to = points[index + 1];
            var bearing = OptimizationGeoMath.InitialBearingDeg(from.Lat, from.Lng, to.Lat, to.Lng);
            return OptimizationGeoMath.Destination(from.Lat, from.Lng, bearing, Math.Min(remaining, legs[index]));
        }
        return points[^1];
    }

    private static ArcGeometry? GetArc(Lattice lattice, ZoneSet zones, BunkerContext bunker, int col, int from, int to)
    {
        if (lattice.Arcs.TryGetValue((col, from, to), out var cached)) return cached;
        var (fromLat, fromLng) = lattice.Nodes[col][from];
        var (toLat, toLng) = lattice.Nodes[col + 1][to];
        var midLat = (fromLat + toLat) / 2;
        var midLng = (fromLng + toLng) / 2;

        ArcGeometry? arc = null;
        var distance = OptimizationGeoMath.DistanceNm(fromLat, fromLng, toLat, toLng);
        var blocked = distance < 1e-6 ||
            OptimizationGeoMath.PointInAnyRing(midLat, midLng, zones.NoGo) ||
            OptimizationGeoMath.PointInAnyRing(toLat, toLng, zones.NoGo) ||
            LandMask.SegmentCrossesLand(fromLat, fromLng, toLat, toLng);
        if (!blocked)
        {
            var active = zones.Speed.Where(zone => OptimizationGeoMath.PointInAnyRing(midLat, midLng, zone.Rings)).ToList();
            var caps = active.Where(zone => zone.SpeedMaxKn.HasValue).Select(zone => zone.SpeedMaxKn!.Value).ToList();
            var rpmMin = active.Where(zone => zone.RpmMin.HasValue).Select(zone => zone.RpmMin!.Value).DefaultIfEmpty().Max();
            var rpmMax = active.Where(zone => zone.RpmMax.HasValue).Select(zone => zone.RpmMax!.Value).DefaultIfEmpty().Min();
            var supplies = bunker.Supplies
                .Select((supply, index) => (supply, index))
                .Where(item => SegmentDistanceNm(fromLat, fromLng, toLat, toLng, item.supply.Lat, item.supply.Lng) <= SupplyReachNm)
                .Select(item => item.index).ToArray();
            arc = new ArcGeometry(
                fromLat, fromLng, toLat, toLng, distance, OptimizationGeoMath.InitialBearingDeg(fromLat, fromLng, toLat, toLng), midLat, midLng,
                OptimizationGeoMath.PointInAnyRing(midLat, midLng, zones.Eca),
                caps.Count > 0 ? caps.Min() : double.MaxValue,
                rpmMin > 0 ? rpmMin : null, rpmMax > 0 ? rpmMax : null, caps.Count > 0 || active.Any(zone => zone.RpmMin.HasValue || zone.RpmMax.HasValue), supplies);
        }
        lattice.Arcs[(col, from, to)] = arc;
        return arc;
    }

    // ---------------------------------------------------------------- forecast

    private sealed record ForecastSet(WeatherGridDto Wind, WeatherGridDto Waves, WeatherGridDto Currents, WeatherGridDto? Period);

    private sealed class ForecastCache
    {
        public Dictionary<(int Col, long Slot), ForecastSet> Sets { get; } = new();
        public DateTime? OldestFetch { get; set; }

        /// <summary>True once any slot in this run had to use historical/modelled
        /// data instead of a live forecast (still real data, never calm-weather
        /// substitution — see <see cref="WeatherGridDto.HasData"/>).</summary>
        public bool UsedHistoricalFallback { get; set; }
    }

    private async Task<ForecastSet> GetForecastAsync(Lattice lattice, int col, DateTime utc, ForecastCache cache, CancellationToken ct)
    {
        var slot = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour / ForecastStepHours * ForecastStepHours, 0, 0, DateTimeKind.Utc);
        if (cache.Sets.TryGetValue((col, slot.Ticks), out var cached)) return cached;

        var both = lattice.Nodes[col].Concat(lattice.Nodes[col + 1]).ToList();
        var south = both.Min(n => n.Lat) - 0.5;
        var north = both.Max(n => n.Lat) + 0.5;
        var west = both.Min(n => n.Lng) - 0.5;
        var east = both.Max(n => n.Lng) + 0.5;

        var wind = await _weather.GetGridAsync("wind", slot, south, west, north, east, 3, 3, ct);
        var waves = await _weather.GetGridAsync("waves", slot, south, west, north, east, 3, 3, ct);
        var currents = await _weather.GetGridAsync("currents", slot, south, west, north, east, 3, 3, ct);
        // Only a hard failure once BOTH the live forecast and the historical/modelled
        // fallback came up empty for this slot — never silently substitutes calm weather.
        if (!wind.HasData || !waves.HasData || !currents.HasData) throw new ForecastUnavailableException(slot);
        if (!wind.Live || !waves.Live || !currents.Live) cache.UsedHistoricalFallback = true;
        var period = await _weather.GetGridAsync("wavePeriod", slot, south, west, north, east, 3, 3, ct);

        foreach (var grid in new[] { wind, waves, currents })
            if (grid.FetchedAtUtc.HasValue && (cache.OldestFetch == null || grid.FetchedAtUtc < cache.OldestFetch)) cache.OldestFetch = grid.FetchedAtUtc;

        var set = new ForecastSet(wind, waves, currents, period.HasData ? period : null);
        cache.Sets[(col, slot.Ticks)] = set;
        return set;
    }

    private static (double Mag, double Dir) Sample(WeatherGridDto grid, double lat, double lng)
    {
        if (grid.Cols <= 0 || grid.Rows <= 0 || grid.Mag.Length == 0) return (0, 0);
        var colFraction = grid.East > grid.West ? (lng - grid.West) / (grid.East - grid.West) : 0.5;
        var rowFraction = grid.North > grid.South ? (lat - grid.South) / (grid.North - grid.South) : 0.5;
        var col = Math.Clamp((int)Math.Round(colFraction * (grid.Cols - 1)), 0, grid.Cols - 1);
        var row = Math.Clamp((int)Math.Round(rowFraction * (grid.Rows - 1)), 0, grid.Rows - 1);
        var index = row * grid.Cols + col;
        if (index < 0 || index >= grid.Mag.Length) return (0, 0);
        return (grid.Mag[index], grid.Dir.Length > index ? grid.Dir[index] : 0);
    }

    // ---------------------------------------------------------------- solver (see RouteOptimizationEngine.Solver.cs)

    private static List<SpeedSetting>? SelectCandidates(
        ScenarioConfig config, List<SpeedSetting> all, OptimizationRequestDto request, VesselEngineLimits limits, List<string> notes, out string? rejection)
    {
        rejection = null;
        if (config.Key == "maintain-cp")
        {
            var cp = all.FirstOrDefault(speed => speed.IsCpDefault);
            if (cp == null) rejection = "No CP-default operating profile is marked for the selected load condition.";
            return cp == null ? null : new List<SpeedSetting> { cp };
        }

        if (config.Key == "custom-simulation")
        {
            var custom = all.FirstOrDefault(speed => string.Equals(speed.Mode, "CUSTOM", StringComparison.OrdinalIgnoreCase));
            if (custom == null ||
                request.CustomRpmMin.HasValue && custom.Rpm > 0 && custom.Rpm < request.CustomRpmMin.Value ||
                request.CustomRpmMax.HasValue && custom.Rpm > 0 && custom.Rpm > request.CustomRpmMax.Value ||
                request.CustomMcrMin.HasValue && custom.McrPercent.HasValue && custom.McrPercent.Value < request.CustomMcrMin.Value ||
                request.CustomMcrMax.HasValue && custom.McrPercent.HasValue && custom.McrPercent.Value > request.CustomMcrMax.Value)
            {
                rejection = "No CUSTOM operating profile satisfies the requested RPM/MCR limits.";
                return null;
            }
            return new List<SpeedSetting> { ApplyCustomLoading(custom, request, limits, notes) };
        }

        return all;
    }

    /// <summary>Admiralty method: at the same speed, power and fuel scale with displacement to the power 2/3.</summary>
    private static SpeedSetting ApplyCustomLoading(SpeedSetting setting, OptimizationRequestDto request, VesselEngineLimits limits, List<string> notes)
    {
        if (request.CustomGmM is > 0 and < 0.15)
            notes.Add($"Custom GM {request.CustomGmM:0.00} m is below the 0.15 m IMO minimum for intact stability.");
        if (request.CustomDraftM.HasValue && !request.CustomDisplacementMt.HasValue)
            notes.Add("Custom draft is used for the roll-period estimate only; enter a displacement for it to change fuel and power.");
        if (request.CustomDisplacementMt is not > 0) return setting;
        if (limits.ReferenceDisplacementMt is not > 0)
        {
            notes.Add("Custom displacement was not applied: the vessel has no reference (summer) displacement for this load condition.");
            return setting;
        }

        var scale = Math.Pow(request.CustomDisplacementMt.Value / limits.ReferenceDisplacementMt.Value, 2.0 / 3.0);
        notes.Add($"Custom displacement {request.CustomDisplacementMt:0} MT vs reference {limits.ReferenceDisplacementMt:0} MT scales main-engine power and fuel by {scale:0.000} (Admiralty method).");
        return setting with
        {
            FuelRateMtPerDay = setting.FuelRateMtPerDay * scale,
            EcaFuelRateMtPerDay = setting.EcaFuelRateMtPerDay * scale,
            PowerKw = setting.PowerKw * scale,
        };
    }

    // ---------------------------------------------------------------- result

    private static OptimizationScenarioResultDto Infeasible(string scenario, OptimizationRequestDto request, string reason, SolverStatusDto? solver = null) => new()
    {
        Scenario = scenario,
        IsFeasible = false,
        InfeasibleReason = reason,
        EtdUtc = request.EtdUtc,
        EtaUtc = request.EtdUtc,
        Analysis = { Solver = solver ?? new SolverStatusDto { Status = "Infeasible" } },
    };

    private static OptimizationScenarioResultDto BuildResult(
        OptimizationRequestDto request, ScenarioConfig config, VesselEngineLimits limits, ArcPhysicsContext physics, Lattice lattice,
        List<Label> finals, ForecastCache forecast, BunkerContext bunker, EtsContext ets, ScenarioTotals? baseline, SolverStatusDto solver, bool trackRob,
        out List<Label> chainResult, out Label finalResult)
    {
        var notes = new List<string>();
        var feasible = true;
        string? reason = null;
        Label final;
        if (config.TargetEtaUtc.HasValue)
        {
            var onTime = finals.Where(label => LateHours(config, request.EtdUtc.AddHours(label.TimeHours)) <= 0.25).ToList();
            if (onTime.Count > 0)
            {
                final = onTime.MinBy(label => label.Cost)!;
                var early = EarlyHours(config, request.EtdUtc.AddHours(final.TimeHours));
                if (early > 0.25)
                    notes.Add($"Arrives {early:0.0} h before the requested time even at the slowest configured speed; waiting at the anchorage would be needed and is not included in time or fuel costs.");
            }
            else
            {
                final = finals.MinBy(label => label.TimeHours)!;
                feasible = false;
                var limit = config.ArrivalRule == "BETWEEN" ? config.ArrivalWindowEndUtc!.Value : config.TargetEtaUtc.Value;
                reason = $"The earliest feasible ETA is {request.EtdUtc.AddHours(final.TimeHours):yyyy-MM-dd HH:mm} UTC, later than the requested {limit:yyyy-MM-dd HH:mm} UTC.";
            }
        }
        else
        {
            final = finals.MinBy(label => label.Cost)!;
        }

        var chain = new List<Label>();
        for (var node = final; node != null; node = node.Parent) chain.Add(node);
        chain.Reverse();
        chainResult = chain;
        finalResult = final;

        var waypoints = chain.Select(label =>
        {
            var (lat, lng) = lattice.Nodes[label.At.Col][label.At.Lane];
            return new RouteWaypointDto
            {
                Lat = lat,
                Lng = lng,
                EtaUtc = request.EtdUtc.AddHours(label.TimeHours),
                SpeedKn = label.Arc?.SogKn ?? 0,
                CumulativeDistanceNm = label.DistNm,
                Rpm = label.Arc is { Rpm: > 0 } ? label.Arc.Rpm : null,
                PowerKw = label.Arc?.PowerKw,
                FuelRateMtPerDay = label.Arc?.FuelRateMtPerDay,
                RobVlsfoMt = label.RobVlsfo,
                RobLsmgoMt = label.RobLsmgo,
            };
        }).ToList();

        var etaUtc = request.EtdUtc.AddHours(final.TimeHours);
        if (final.WaitHours > 0.01)
            notes.Add($"The route includes {final.WaitHours:0.#} h of waiting at sea (engine stopped, auxiliary fuel only, position held; drift is not modelled) because it lowers the objective.");
        var analysis = BuildAnalysis(request, limits, physics, chain, final, forecast, bunker, trackRob, notes, waypoints);
        analysis.Solver = solver;
        solver.WaitingStepsInRoute = chain.Count(label => label.Arc?.IsWait == true);
        analysis.Segments = BuildSegments(request, chain, lattice, ets);
        analysis.FuelByGrade = BuildFuelByGrade(chain, physics);
        analysis.Emissions = new EmissionsChargeDto
        {
            TotalCo2Mt = final.Co2Mt,
            ChargeableCo2Mt = final.ChargeableCo2Mt,
            VoyageClassification = ets.Classification,
            CoveragePct = ets.CoveragePct,
            PhaseIn = ets.PhaseInSummary(),
            Basis = ets.Basis + " Tank-to-wake CO2 from the fuel burned; well-to-wake emissions are not calculated.",
        };

        var emissionsCost = request.EuaPriceUsdPerTonneCo2 * final.ChargeableCo2Mt;
        var timeCost = request.HireRatePerDayUsd / 24 * final.TimeHours;
        var totalCost = final.FuelCostUsd + timeCost + emissionsCost;
        if (baseline != null)
        {
            analysis.Savings = new SavingsDto
            {
                Baseline = "Planned route: the original waypoints at the CP default operating point, constant setting, no re-routing or waiting, same forecast.",
                BaselineEtaUtc = baseline.EtaUtc,
                BaselineTimeHours = baseline.TimeHours,
                BaselineFuelMt = baseline.FuelMt,
                BaselineCostUsd = baseline.CostUsd,
                BaselineCo2Mt = baseline.Co2Mt,
                FuelSavedMt = baseline.FuelMt - (final.MainFuelMt + final.AuxGoMt),
                CostSavedUsd = baseline.CostUsd - totalCost,
                TimeSavedHours = baseline.TimeHours - final.TimeHours,
                Co2SavedMt = baseline.Co2Mt - final.Co2Mt,
            };
        }

        return new OptimizationScenarioResultDto
        {
            Scenario = config.Key,
            IsFeasible = feasible,
            InfeasibleReason = reason,
            Warnings = notes,
            Analysis = analysis,
            WasConstrainedByZones = final.Constrained,
            Waypoints = waypoints,
            EtdUtc = request.EtdUtc,
            EtaUtc = etaUtc,
            DistanceNm = final.DistNm,
            AverageSpeedKn = final.TimeHours > 0 ? final.DistNm / final.TimeHours : 0,
            TimeToGoHours = final.TimeHours,
            EcaDistanceNm = final.EcaDistNm,
            EcaTimeToGoHours = final.EcaTimeHours,
            MeFuelRequiredToGoMt = final.MainFuelMt,
            EcaFuelRequiredToGoMt = final.EcaFuelMt,
            FuelCostToGoUsd = final.FuelCostUsd,
            TimeCostToGoUsd = timeCost,
            EmissionsCostToGoUsd = emissionsCost,
            TotalCostToGoUsd = totalCost,
            MeCo2EmissionsToGoMt = final.Co2Mt,
        };
    }

    private static ScenarioTotals TotalsOf(OptimizationRequestDto request, Label final) => new(
        request.EtdUtc.AddHours(final.TimeHours), final.TimeHours, final.MainFuelMt + final.AuxGoMt,
        final.FuelCostUsd + request.HireRatePerDayUsd / 24 * final.TimeHours + request.EuaPriceUsdPerTonneCo2 * final.ChargeableCo2Mt, final.Co2Mt);

    /// <summary>Audit rows: every sailed sub-step (forecast sampled at its own position and time) and every wait, in order.</summary>
    private static List<SegmentAuditDto> BuildSegments(OptimizationRequestDto request, List<Label> chain, Lattice lattice, EtsContext ets)
    {
        var rows = new List<SegmentAuditDto>();
        for (var index = 1; index < chain.Count; index++)
        {
            var previous = chain[index - 1];
            var label = chain[index];
            var outcome = label.Arc!;
            var legStart = request.EtdUtc.AddHours(previous.TimeHours);
            var (fromLat, fromLng) = lattice.Nodes[previous.At.Col][previous.At.Lane];
            var (toLat, toLng) = lattice.Nodes[label.At.Col][label.At.Lane];
            var robVlsfo = previous.RobVlsfo;
            var robLsmgo = previous.RobLsmgo;

            if (outcome.IsWait)
            {
                robLsmgo = label.RobLsmgo;
                rows.Add(new SegmentAuditDto
                {
                    Index = rows.Count, Kind = "Wait", StartUtc = legStart, EndUtc = legStart.AddHours(outcome.Hours),
                    StartLat = fromLat, StartLng = fromLng, EndLat = fromLat, EndLng = fromLng, Hours = outcome.Hours,
                    Setting = "WAIT", AuxFuelMt = outcome.AuxGoMt, Co2Mt = outcome.Co2Mt, ChargeableCo2Mt = outcome.Co2Mt * ets.Factor(legStart),
                    RobVlsfoEndMt = robVlsfo, RobLsmgoEndMt = robLsmgo,
                });
                continue;
            }

            var steps = outcome.Steps!;
            var startLat = fromLat;
            var startLng = fromLng;
            for (var s = 0; s < steps.Count; s++)
            {
                var step = steps[s];
                var o = step.Outcome;
                var start = legStart.AddHours(step.StartHours);
                var inEca = step.Input.InEca;
                if (robVlsfo.HasValue && !inEca) robVlsfo -= o.MainFuelMt;
                if (robLsmgo.HasValue) robLsmgo -= (inEca ? o.MainFuelMt : 0) + o.AuxGoMt;
                var lastStep = s == steps.Count - 1;
                rows.Add(new SegmentAuditDto
                {
                    Index = rows.Count, Kind = "Sail", StartUtc = start, EndUtc = start.AddHours(o.Hours),
                    StartLat = startLat, StartLng = startLng, EndLat = lastStep ? toLat : step.EndLat, EndLng = lastStep ? toLng : step.EndLng,
                    DistanceNm = step.Input.DistNm, Hours = o.Hours, StwKn = o.StwKn, SogKn = o.SogKn,
                    Setting = o.Setting.Mode, PowerMode = o.Mode.ToString(), Rpm = o.Rpm > 0 ? o.Rpm : null, PowerKw = o.PowerKw,
                    LoadPct = o.LoadFraction * 100,
                    MainEngineFuelMt = o.MainFuelMt, MainEngineGrade = MainGrade(o.Setting, inEca), AuxFuelMt = o.AuxGoMt,
                    Co2Mt = o.Co2Mt, ChargeableCo2Mt = o.Co2Mt * ets.Factor(start),
                    WindKn = step.Input.WindKn, WindFromDeg = step.Input.WindFromDeg, WaveM = step.Input.WaveM, WaveFromDeg = step.Input.WaveFromDeg,
                    CurrentKn = step.Input.CurrentKn, CurrentToDeg = step.Input.CurrentToDeg, InEca = inEca,
                    ExposureScore = o.ExposureScore, ExceedsVesselWeatherLimit = o.ExceedsWeatherLimit,
                    RobVlsfoEndMt = lastStep ? label.RobVlsfo : robVlsfo, RobLsmgoEndMt = lastStep ? label.RobLsmgo : robLsmgo,
                });
                startLat = step.EndLat;
                startLng = step.EndLng;
            }
        }
        return rows;
    }

    private static string MainGrade(SpeedSetting setting, bool inEca) =>
        inEca ? (string.IsNullOrWhiteSpace(setting.FuelType2) ? "LSMGO" : setting.FuelType2.Trim().ToUpperInvariant()) : setting.FuelType1.Trim().ToUpperInvariant();

    /// <summary>Fuel, tank-to-wake CO2 and cost per grade; main engine in ECA burns the low-sulphur grade and auxiliaries always burn LSMGO.</summary>
    private static List<FuelGradeTotalDto> BuildFuelByGrade(List<Label> chain, ArcPhysicsContext physics)
    {
        var totals = new Dictionary<string, double>();
        void Add(string grade, double mt) => totals[grade] = totals.GetValueOrDefault(grade) + mt;
        foreach (var label in chain.Skip(1))
        {
            var outcome = label.Arc!;
            if (outcome.IsWait) { Add("LSMGO", outcome.AuxGoMt); continue; }
            foreach (var step in outcome.Steps!)
            {
                Add(MainGrade(outcome.Setting, step.Input.InEca), step.Outcome.MainFuelMt);
                Add("LSMGO", step.Outcome.AuxGoMt);
            }
        }

        return totals.Where(pair => pair.Value > 0).Select(pair =>
        {
            var factor = VesselSpeedProfile.Co2Factor(pair.Key);
            var isGasOil = pair.Key is "LSMGO" or "MGO" or "MDO";
            return new FuelGradeTotalDto
            {
                Grade = pair.Key, Mt = pair.Value, EmissionFactor = factor, Co2Mt = pair.Value * factor,
                CostUsd = pair.Value * (isGasOil ? physics.GoPriceUsdPerMt : physics.FoPriceUsdPerMt),
            };
        }).OrderByDescending(item => item.Mt).ToList();
    }

    private static OptimizationAnalysisDto BuildAnalysis(
        OptimizationRequestDto request, VesselEngineLimits limits, ArcPhysicsContext physics, List<Label> chain, Label final,
        ForecastCache forecast, BunkerContext bunker, bool trackRob, List<string> notes, List<RouteWaypointDto> waypoints)
    {
        double CalmTotal(out bool exact)
        {
            var hours = 0.0;
            exact = true;
            foreach (var label in chain.Skip(1))
            {
                if (label.Arc!.IsWait) continue;
                foreach (var step in label.Arc.Steps!)
                {
                    var outcome = ArcPhysics.Evaluate(step.Input, label.Arc.Setting, PowerMode.HoldRpm, physics, 1, true);
                    if (outcome == null) { exact = false; hours += step.Outcome.Hours; }
                    else hours += outcome.Hours;
                }
            }
            return hours;
        }

        if (forecast.UsedHistoricalFallback)
            notes.Add("Live forecast data was unavailable for part of this route; historical/modelled weather was used for those legs instead.");

        var analysis = new OptimizationAnalysisDto
        {
            PerformanceModel = physics.Model?.Describe() ?? "Constant-power approximation (no calibrated speed-power curve): weather speed loss from the calibration tables at constant power, cube law for boost and zone caps.",
            ForecastValidFromUtc = request.EtdUtc,
            ForecastValidToUtc = request.EtdUtc.AddHours(final.TimeHours),
            ForecastFetchedAtUtc = forecast.OldestFetch,
            MaxEngineLoadPct = final.MaxLoad * 100,
            HoursOverVesselWeatherLimit = final.ExceedHours,
            HoursSynchronousRollRisk = final.SyncHours,
            HoursParametricRollRisk = final.ParametricHours,
            HoursPitchResonanceRisk = final.PitchHours,
            HoursPowerBoosted = final.BoostHours,
            WeatherExposureScore = final.Exposure,
            TotalFuelMt = final.MainFuelMt + final.AuxGoMt,
        };

        var calmHours = CalmTotal(out var calmExact);
        analysis.WeatherDelayHours = Math.Max(0, final.TimeHours - final.WaitHours - calmHours);
        if (!calmExact) notes.Add("Weather delay is approximate: some arcs could not be re-evaluated in calm water.");

        // ETA comes from the forecast-based average speed; the system's weather margin (speed x (1 - WM%)) gives the latest ETA.
        analysis.EtaEarliestUtc = request.EtdUtc.AddHours(final.TimeHours);
        if (request.WeatherMarginPct is > 0 and < 100)
        {
            var margin = request.WeatherMarginPct.Value / 100;
            analysis.EtaLatestUtc = request.EtdUtc.AddHours(final.TimeHours / (1 - margin));
            analysis.EtaRangeBasis = $"Forecast-based ETA from the predicted average speed; latest ETA applies the voyage weather margin of {request.WeatherMarginPct:0.#}% (average speed x (1 - WM%)). It is an allowance, not a statistical confidence interval.";
        }
        else
        {
            analysis.EtaLatestUtc = analysis.EtaEarliestUtc;
            notes.Add("No latest-ETA allowance is shown because the voyage has no weather margin configured.");
        }

        if (trackRob)
        {
            analysis.StartRobVlsfoMt = bunker.StartVlsfo;
            analysis.StartRobLsmgoMt = bunker.StartLsmgo;
            analysis.ArrivalRobVlsfoMt = final.RobVlsfo;
            analysis.ArrivalRobLsmgoMt = final.RobLsmgo;
            analysis.ReserveSatisfied = (final.RobVlsfo ?? double.MaxValue) >= (bunker.MinVlsfo ?? 0) && (final.RobLsmgo ?? double.MaxValue) >= (bunker.MinLsmgo ?? 0);
            analysis.SuppliesApplied = bunker.Supplies.Where((_, index) => (final.SupplyMask & (1UL << index)) != 0).Select(supply => supply.Label).ToList();
        }
        analysis.SuppliesNotCounted = bunker.NotCounted;

        if (trackRob && bunker.Ports.Count > 0)
        {
            analysis.BunkerPortOptions = BunkerPortComparison.Compare(waypoints, bunker.Ports, bunker.Supplies, request, final.TimeHours, final.MainFuelMt, final.DistNm);
            analysis.BunkerPortBasis = "Ports within 150 NM of this route, ranked by the cost of an out-and-back detour (time, fuel and hire). The run's VLSFO/LSMGO prices apply to every port, so port prices, availability and supplier quotes are not compared.";
        }

        AddWeatherReference(notes, final, limits);
        return analysis;
    }

    private static void AddWeatherReference(List<string> notes, Label route, VesselEngineLimits limits)
    {
        string Compare(string name, string unit, double peak, double? limit) => limit.HasValue
            ? $"Forecast peak {name} {peak:0.0} {unit} vs vessel limit {limit.Value:0.0} {unit}: {(peak > limit.Value ? "exceeds" : "within")} limit."
            : $"Forecast peak {name} {peak:0.0} {unit}; no vessel limit is configured for comparison.";

        notes.Add(Compare("significant wave height", "m", route.MaxWaveM, limits.MaxSwhM));
        notes.Add(Compare("wind", "kn", route.MaxWindKn, limits.MaxWindKn));
        if (route.ExceedHours > 0.01)
            notes.Add($"About {route.ExceedHours:0.#} h of this passage is forecast above a vessel weather limit (reference only; routes are not restricted by these limits).");
        if (route.SyncHours > 0.01) notes.Add($"Advisory: about {route.SyncHours:0.#} h with wave encounter period near the roll period in beam seas (synchronous rolling risk).");
        if (route.ParametricHours > 0.01) notes.Add($"Advisory: about {route.ParametricHours:0.#} h with encounter period near half the roll period in head or following seas (parametric rolling risk).");
        if (route.PitchHours > 0.01) notes.Add($"Advisory: about {route.PitchHours:0.#} h in head seas with wavelength close to the ship length (pitch resonance and slamming risk).");
        if (route.BoostHours > 0.01) notes.Add($"About {route.BoostHours:0.#} h use extra power to hold speed in weather, within the configured maximum engine load (peak {route.MaxLoad * 100:0}% of installed power).");
    }

    // ---------------------------------------------------------------- arrival rules

    private static double LateHours(ScenarioConfig config, DateTime etaUtc)
    {
        if (!config.TargetEtaUtc.HasValue) return 0;
        return config.ArrivalRule switch
        {
            "ON" or "BEFORE" => Math.Max(0, (etaUtc - config.TargetEtaUtc.Value).TotalHours),
            "BETWEEN" when config.ArrivalWindowEndUtc.HasValue => Math.Max(0, (etaUtc - config.ArrivalWindowEndUtc.Value).TotalHours),
            _ => 0,
        };
    }

    private static double EarlyHours(ScenarioConfig config, DateTime etaUtc)
    {
        if (!config.TargetEtaUtc.HasValue || config.ArrivalRule == "BEFORE") return 0;
        return Math.Max(0, (config.TargetEtaUtc.Value - etaUtc).TotalHours);
    }
}

internal sealed record SupplyPoint(string Label, double Lat, double Lng, double VlsfoMt, double LsmgoMt);

internal sealed record BunkerContext(double? StartVlsfo, double? StartLsmgo, double? MinVlsfo, double? MinLsmgo, List<SupplyPoint> Supplies, List<string> NotCounted, List<PortCandidate> Ports, string? RobNote);
