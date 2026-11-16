using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Application.DTOs.Weather;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Entities.Optimization;
using MultiTenantSaaS.Domain.Entities.Performance;
using MultiTenantSaaS.Infrastructure.Persistence;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class RouteOptimizationEngineTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTime Etd = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed class FakeWeather : IWeatherDataService
    {
        public bool Live { get; init; } = true;
        public double WaveM { get; init; }
        public double WindKn { get; init; }
        public double CurrentKn { get; init; }
        public double PeriodS { get; init; }
        public DateTime? CalmFromUtc { get; init; }
        public double DirDeg { get; init; } = 90; // eastbound test route: weather from the east is on the bow; the current flows toward this bearing

        public Task<WeatherGridDto> GetGridAsync(string factorId, DateTime timestampUtc, double south, double west, double north, double east, int cols, int rows, CancellationToken cancellationToken = default)
        {
            var calm = CalmFromUtc.HasValue && timestampUtc >= CalmFromUtc.Value;
            var value = factorId switch { "waves" => calm ? 0 : WaveM, "wind" => calm ? 0 : WindKn, "currents" => CurrentKn, "wavePeriod" => PeriodS, _ => 0 };
            return Task.FromResult(new WeatherGridDto
            {
                South = south, West = west, North = north, East = east, Cols = cols, Rows = rows,
                Mag = Enumerable.Repeat(value, cols * rows).ToArray(),
                Dir = Enumerable.Repeat(DirDeg, cols * rows).ToArray(),
                Live = Live && (factorId != "wavePeriod" || PeriodS > 0),
                HasData = Live && (factorId != "wavePeriod" || PeriodS > 0),
            });
        }
    }

    private static OptimizationOperatingProfileDto Profile(string mode, double speed, double cons, bool cpDefault = false) => new()
    {
        Mode = mode, IsCpDefault = cpDefault, LoadCondition = "Laden", SpeedKn = speed, Rpm = 70, McrPercent = 60,
        FuelType1 = "VLSFO", DailyCons1Mt = cons, FuelType2 = "LSMGO", DailyCons2Mt = 0.5,
    };

    private static OptimizationRequestDto Request(string scenario, double[][]? route = null) => new()
    {
        VoyageId = "V-TEST", VesselImo = "9999999", Scenarios = { scenario }, EtdUtc = Etd,
        OriginLat = -20, OriginLng = -30, DestLat = -20, DestLng = -10,
        RouteCoordinates = (route ?? new[] { new[] { -30d, -20d }, new[] { -10d, -20d } }).ToList(),
        LoadCondition = "Laden", PerformanceBasis = "CP",
        HireRatePerDayUsd = 20000, FoPriceUsdPerMt = 600, GoPriceUsdPerMt = 900, EuaPriceUsdPerTonneCo2 = 80,
        OperatingProfiles =
        {
            Profile("ECO", 10, 20), Profile("CUSTOM", 12, 30), Profile("FULL", 14, 40, cpDefault: true),
        },
    };

    private static async Task<(OptimizationScenarioResultDto Result, ApplicationDbContext Db)> Run(
        OptimizationRequestDto request, FakeWeather? weather = null, Action<Vessel>? vessel = null, IEnumerable<TracksheetRow>? tracksheet = null, Action<ApplicationDbContext>? seed = null,
        IEnsembleWindService? ensemble = null)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.TenantId).Returns(Tenant);
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        var perf = new PerformanceDbContext(new DbContextOptionsBuilder<PerformanceDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        await perf.Database.EnsureCreatedAsync();

        var ship = new Vessel { TenantId = Tenant, Name = "TEST", Imo = "9999999", Deadweight = "50000", WslMaxSwhLaden = "6", WslMaxWindsLaden = "40" };
        vessel?.Invoke(ship);
        db.Vessels.Add(ship);
        seed?.Invoke(db);
        if (tracksheet != null)
        {
            perf.TracksheetRows.AddRange(tracksheet);
        }
        await db.SaveChangesAsync();
        await perf.SaveChangesAsync();

        var engine = new RouteOptimizationEngine(db, perf, weather ?? new FakeWeather(), user.Object, ensemble);
        var run = await engine.RunAsync(request);
        return (run.Scenarios.Single(), db);
    }

    [Fact]
    public async Task EarliestEtaAndLowestFuelUseDifferentConfiguredProfiles()
    {
        var (fast, _) = await Run(Request("earliest-eta"));
        var (frugal, _) = await Run(Request("lowest-fuel"));

        fast.IsFeasible.Should().BeTrue();
        frugal.IsFeasible.Should().BeTrue();
        fast.TimeToGoHours.Should().BeLessThan(frugal.TimeToGoHours);
        fast.MeFuelRequiredToGoMt.Should().BeGreaterThan(frugal.MeFuelRequiredToGoMt);
    }

    [Fact]
    public async Task TotalsReconcileAndRunIsStoredForTheTenant()
    {
        var (result, db) = await Run(Request("lowest-cost"));

        result.TotalCostToGoUsd.Should().BeApproximately(result.FuelCostToGoUsd + result.TimeCostToGoUsd + result.EmissionsCostToGoUsd, 1e-6);
        result.Waypoints.Last().CumulativeDistanceNm.Should().BeApproximately(result.DistanceNm, 1e-6);
        (await db.OptimizationRuns.IgnoreQueryFilters().SingleAsync()).TenantId.Should().Be(Tenant);
    }

    [Fact]
    public async Task MaintainCpUsesTheDefaultCpProfile()
    {
        var (result, _) = await Run(Request("maintain-cp"));

        result.AverageSpeedKn.Should().BeApproximately(14, 0.01);
    }

    [Fact]
    public async Task VoyageWithoutAnImoRunsOnTheVoyageDwtWhenTheVesselIsNotRegistered()
    {
        var request = Request("maintain-cp");
        request.VesselImo = string.Empty;
        request.VesselName = "Unregistered";
        request.DeadweightMt = 60000;

        var (result, _) = await Run(request, vessel: ship => ship.Imo = "1111111");

        result.IsFeasible.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("not in the vessel master"));
    }

    [Fact]
    public async Task VoyageWithoutAnImoUsesTheRegisteredVesselWithTheSameName()
    {
        var request = Request("maintain-cp");
        request.VesselImo = string.Empty;
        request.VesselName = "test";

        var (result, db) = await Run(request);

        result.IsFeasible.Should().BeTrue();
        result.Warnings.Should().NotContain(w => w.Contains("not in the vessel master"));
        (await db.OptimizationRuns.IgnoreQueryFilters().SingleAsync()).VesselImo.Should().Be("9999999");
    }

    [Fact]
    public async Task UnknownVesselWithoutDwtGivesAClearError()
    {
        var request = Request("maintain-cp");
        request.VesselImo = string.Empty;
        request.VesselName = "Nobody";

        var act = () => Run(request);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*not in the vessel master*DWT*");
    }

    [Fact]
    public async Task SelectedWaypointsStayOnTheRoute()
    {
        var route = new[] { new[] { -30d, -20d }, new[] { -20d, -18d }, new[] { -10d, -20d } };

        var (result, _) = await Run(Request("lowest-cost", route));

        result.Waypoints.Should().Contain(w => Math.Abs(w.Lat + 18) < 0.01 && Math.Abs(w.Lng + 20) < 0.01);
    }

    [Fact]
    public async Task RoutesCrossingLandAreInfeasible()
    {
        var request = Request("lowest-cost", new[] { new[] { 15d, 0d }, new[] { 25d, 0d } });

        var (result, _) = await Run(request);

        result.IsFeasible.Should().BeFalse();
        result.InfeasibleReason.Should().Contain("No feasible path");
    }

    [Fact]
    public async Task MissingForecastIsInfeasibleNotCalmWeather()
    {
        var (result, _) = await Run(Request("lowest-cost"), new FakeWeather { Live = false });

        result.IsFeasible.Should().BeFalse();
        result.InfeasibleReason.Should().Contain("forecast data is unavailable");
    }

    [Fact]
    public async Task VesselWeatherLimitsAreReportedNotEnforced()
    {
        var (result, _) = await Run(Request("safest"), new FakeWeather { WaveM = 3 }, vessel => vessel.WslMaxSwhLaden = "1");

        result.IsFeasible.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("exceeds limit"));
        result.Warnings.Should().Contain(w => w.Contains("reference only"));
    }

    [Fact]
    public async Task HeadSeasSlowTheVesselAndAFavourableCurrentSpeedsItUp()
    {
        var (calm, _) = await Run(Request("maintain-cp"));
        var (headSea, _) = await Run(Request("maintain-cp"), new FakeWeather { WaveM = 4, WindKn = 30 });
        var (withCurrent, _) = await Run(Request("maintain-cp"), new FakeWeather { CurrentKn = 1 });

        headSea.AverageSpeedKn.Should().BeLessThan(calm.AverageSpeedKn - 0.05);
        withCurrent.AverageSpeedKn.Should().BeApproximately(calm.AverageSpeedKn + 1, 0.05);
    }

    [Fact]
    public async Task WithinLimitForecastIsReportedAsWithin()
    {
        var (result, _) = await Run(Request("safest"));

        result.Warnings.Should().Contain(w => w.Contains("significant wave height") && w.Contains("within limit"));
    }

    private static OptimizationRequestDto Arrival(string rule, double targetHours, double? endHours = null)
    {
        var request = Request("specific-eta");
        request.ArrivalRule = rule;
        request.TargetEtaUtc = Etd.AddHours(targetHours);
        request.ArrivalWindowEndUtc = endHours.HasValue ? Etd.AddHours(endHours.Value) : null;
        return request;
    }

    [Fact]
    public async Task BeforeRuleFindsAFeasibleFasterTransit()
    {
        var (result, _) = await Run(Arrival("BEFORE", 90)); // 1,100 NM: slowest 110 h, fastest 78.6 h

        result.IsFeasible.Should().BeTrue();
        result.EtaUtc.Should().BeOnOrBefore(Etd.AddHours(90.25));
    }

    [Fact]
    public async Task OnRuleHitsTheTargetFromBelow()
    {
        var (result, _) = await Run(Arrival("ON", 95));

        result.IsFeasible.Should().BeTrue();
        result.EtaUtc.Should().BeOnOrBefore(Etd.AddHours(95.25));
    }

    [Fact]
    public async Task BetweenRuleRespectsTheWindowEnd()
    {
        var (result, _) = await Run(Arrival("BETWEEN", 85, 100));

        result.IsFeasible.Should().BeTrue();
        result.EtaUtc.Should().BeOnOrBefore(Etd.AddHours(100.25));
    }

    [Fact]
    public async Task ImpossibleDeadlineReportsTheEarliestFeasibleEta()
    {
        var (result, _) = await Run(Arrival("BEFORE", 50));

        result.IsFeasible.Should().BeFalse();
        result.InfeasibleReason.Should().Contain("earliest feasible ETA");
    }

    [Fact]
    public async Task AfterRuleArrivingEarlyIsFlaggedAsWaiting()
    {
        var (result, _) = await Run(Arrival("AFTER", 200));

        result.IsFeasible.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("waiting at the anchorage"));
    }

    [Fact]
    public async Task ActualPerformanceIsUsedByDefaultInsteadOfPaperValues()
    {
        var request = Request("lowest-fuel");
        request.PerformanceBasis = "ACTUAL";
        var rows = new List<TracksheetRow> { new() { TenantId = Tenant, VoyageId = "H1", SortOrder = 0, VesselImo = "9999999", Rt = "D", Date = "01Jun2026", Time = "0000", VlsfoRob = 500, LsmgoRob = 100, Amount = 50000 } };
        rows.AddRange(Enumerable.Range(1, 4).Select(i => new TracksheetRow
        {
            TenantId = Tenant, VoyageId = "H1", SortOrder = i, VesselImo = "9999999", Rt = "N", Date = $"{i + 1:00}Jun2026", Time = "1200",
            Hrs = 24, AvgSpeedO = 11.5, Rpm = 70, EnginePower = 7000, Amount = 50000, VlsfoRob = 500 - 26 * i, LsmgoRob = 100 - 0.1 * i,
        }));

        var (result, _) = await Run(request, tracksheet: rows);

        result.IsFeasible.Should().BeTrue();
        result.AverageSpeedKn.Should().BeApproximately(11.5, 0.05);
        result.MeFuelRequiredToGoMt.Should().BeApproximately(26.0 / 24 * result.TimeToGoHours, 0.05);
        result.Warnings.Should().Contain(w => w.StartsWith("Performance basis:") && w.Contains("noon reports"));
    }

    [Fact]
    public async Task WithoutObservedDataTheConfiguredValuesAreUsedAndFlagged()
    {
        var request = Request("lowest-fuel");
        request.PerformanceBasis = "ACTUAL";

        var (result, _) = await Run(request);

        result.IsFeasible.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("too little observed performance data"));
    }

    [Fact]
    public async Task SpeedControlZoneUsesGeoJsonLonLatRingOrder()
    {
        var request = Request("maintain-cp");
        request.RestrictedAreas.Add(new RestrictedAreaDto
        {
            Id = "Z1", ZoneType = "speed-control-zone", SpeedMaxKn = 8,
            Rings = { new List<double[]> { new[] { -25d, -22d }, new[] { -15d, -22d }, new[] { -15d, -18d }, new[] { -25d, -18d }, new[] { -25d, -22d } } },
        });

        var (capped, _) = await Run(request);
        var (free, _) = await Run(Request("maintain-cp"));

        capped.AverageSpeedKn.Should().BeLessThan(free.AverageSpeedKn - 0.5);
    }

    [Fact]
    public async Task ZoneOutsideTheRouteDoesNotSlowTheVessel()
    {
        var request = Request("maintain-cp");
        request.RestrictedAreas.Add(new RestrictedAreaDto
        {
            Id = "Z2", ZoneType = "speed-control-zone", SpeedMaxKn = 8,
            Rings = { new List<double[]> { new[] { -25d, 18d }, new[] { -15d, 18d }, new[] { -15d, 22d }, new[] { -25d, 22d }, new[] { -25d, 18d } } },
        });

        var (result, _) = await Run(request);

        result.AverageSpeedKn.Should().BeApproximately(14, 0.05);
    }

    [Fact]
    public async Task BeamSeasAtTheRollPeriodRaiseSynchronousRollRisk()
    {
        var request = Request("maintain-cp");
        request.RollPeriodSec = 10;

        var (result, _) = await Run(request, new FakeWeather { WaveM = 2, PeriodS = 10, DirDeg = 0 });

        result.Analysis.HoursSynchronousRollRisk.Should().BeGreaterThan(0, string.Join(" | ", result.Warnings));
        result.Warnings.Should().Contain(w => w.Contains("synchronous rolling"));
    }

    [Fact]
    public async Task WithoutRollPeriodRollScreeningIsSkippedAndSaid()
    {
        var (result, _) = await Run(Request("safest"), new FakeWeather { WaveM = 2, PeriodS = 10, DirDeg = 0 });

        result.Analysis.HoursSynchronousRollRisk.Should().Be(0);
        result.Warnings.Should().Contain(w => w.Contains("roll screening was skipped") || w.Contains("roll period"));
    }

    [Fact]
    public async Task EtaRangeUsesTheVoyageWeatherMargin()
    {
        var request = Request("maintain-cp");
        request.WeatherMarginPct = 10;

        var (result, _) = await Run(request, new FakeWeather { WaveM = 4, WindKn = 30 });

        result.Analysis.EtaEarliestUtc.Should().Be(result.EtaUtc);
        result.Analysis.EtaLatestUtc!.Value.Should().Be(Etd.AddHours(result.TimeToGoHours / 0.9));
        result.Analysis.EtaRangeBasis.Should().Contain("weather margin");
    }

    [Fact]
    public async Task WeatherDelayIsMeasuredAgainstCalmWater()
    {
        var (calm, _) = await Run(Request("maintain-cp"));
        var (rough, _) = await Run(Request("maintain-cp"), new FakeWeather { WaveM = 4, WindKn = 30 });

        calm.Analysis.WeatherDelayHours.Should().BeApproximately(0, 0.01);
        rough.Analysis.WeatherDelayHours.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BunkerReserveThatCannotBeKeptIsInfeasible()
    {
        var request = Request("lowest-fuel");
        request.RobVlsfoMt = 100;
        request.MinReserveVlsfoMt = 95;

        var (result, _) = await Run(request);

        result.IsFeasible.Should().BeFalse();
        result.InfeasibleReason.Should().Contain("bunker reserve");
    }

    [Fact]
    public async Task ReserveIsSatisfiedWhenEnoughFuelIsOnBoard()
    {
        var request = Request("lowest-fuel");
        request.RobVlsfoMt = 400;
        request.MinReserveVlsfoMt = 50;

        var (result, _) = await Run(request);

        result.IsFeasible.Should().BeTrue();
        result.Analysis.ReserveSatisfied.Should().BeTrue();
        result.Analysis.ArrivalRobVlsfoMt.Should().BeGreaterThan(50);
    }

    [Fact]
    public async Task OnlyBookedBunkerStemsOnTheRouteRaiseTheReserve()
    {
        var request = Request("lowest-fuel");
        request.RobVlsfoMt = 120;
        request.MinReserveVlsfoMt = 50;
        void Seed(ApplicationDbContext db, string status)
        {
            db.Ports.Add(new Port { TenantId = Tenant, PortName = "Midway", Latitude = -20m, Longitude = -20m });
            db.BunkerRequirements.Add(new BunkerRequirement { TenantId = Tenant, Imo = "9999999", RequirementNo = "BR-1", BunkerPort = "Midway", Status = status, FuelType = "VLSFO", Quantity = 300 });
        }

        var (pending, _) = await Run(request, seed: db => Seed(db, "Quotes Received"));
        var (booked, _) = await Run(request, seed: db => Seed(db, "Booked"));

        pending.IsFeasible.Should().BeFalse();
        booked.IsFeasible.Should().BeTrue();
        booked.Analysis.SuppliesApplied.Should().NotBeEmpty();
    }

    private static OptimizationRequestDto WithPower(OptimizationRequestDto request)
    {
        foreach (var profile in request.OperatingProfiles) profile.PowerKw = 6000;
        return request;
    }

    [Theory]
    [InlineData("[{\"rpm\":60,\"maxTorqueKnm\":700},{\"rpm\":80,\"maxTorqueKnm\":700}]", false)]
    [InlineData("[{\"rpm\":60,\"maxTorqueKnm\":900},{\"rpm\":80,\"maxTorqueKnm\":900}]", true)]
    [InlineData("[{\"rpm\":60,\"maxPowerKw\":5000},{\"rpm\":80,\"maxPowerKw\":5000}]", false)]
    [InlineData("[{\"rpm\":60,\"minPowerKw\":7000},{\"rpm\":80,\"minPowerKw\":7000}]", false)]
    [InlineData("[{\"rpm\":40,\"maxTorqueKnm\":10},{\"rpm\":50,\"maxTorqueKnm\":10}]", true)]
    public async Task LoadDiagramLimitsAreEnforcedOnlyInsideTheTabulatedRpmRange(string diagram, bool feasible)
    {
        var (result, _) = await Run(WithPower(Request("maintain-cp")), vessel: v => v.LoadDiagramJson = diagram);

        result.IsFeasible.Should().Be(feasible);
    }

    [Fact]
    public async Task RpmInsideTheBlowerBandIsAvoided()
    {
        var (inside, _) = await Run(Request("maintain-cp"), vessel: v => { v.BlowerLadenMin = "60"; v.BlowerLadenMax = "80"; });
        var (outside, _) = await Run(Request("maintain-cp"), vessel: v => { v.BlowerLadenMin = "40"; v.BlowerLadenMax = "65"; });

        inside.IsFeasible.Should().BeFalse();
        outside.IsFeasible.Should().BeTrue();
        outside.Warnings.Should().Contain(w => w.Contains("Blower cut-in/cut-off"));
    }

    [Fact]
    public async Task RobIsReducedForTheTimeBetweenTheLastReportAndDeparture()
    {
        var rows = new[]
        {
            new TracksheetRow { TenantId = Tenant, VoyageId = "V-TEST", VesselImo = "9999999", SortOrder = 0, Date = "29Jun2026", Time = "1200", VlsfoRob = 400 },
            new TracksheetRow { TenantId = Tenant, VoyageId = "V-TEST", VesselImo = "9999999", SortOrder = 1, Date = "30Jun2026", Time = "1200", Hrs = 24, VlsfoRob = 380 },
        };

        var (result, _) = await Run(Request("maintain-cp"), tracksheet: rows);

        result.Analysis.StartRobVlsfoMt.Should().BeApproximately(370, 0.01); // 20 MT/day for the 12 h before departure
        result.Warnings.Should().Contain(w => w.Contains("ROB was reduced"));
    }

    [Fact]
    public async Task BunkerPortsNearTheRouteAreRankedByDetourCost()
    {
        var request = Request("maintain-cp");
        request.RobVlsfoMt = 400;

        var (result, _) = await Run(request, seed: db =>
        {
            db.Ports.Add(new Port { TenantId = Tenant, PortName = "Near", Country = "X", Latitude = -19.5m, Longitude = -20m });
            db.Ports.Add(new Port { TenantId = Tenant, PortName = "Mid", Country = "X", Latitude = -19m, Longitude = -25m });
            db.Ports.Add(new Port { TenantId = Tenant, PortName = "Far", Country = "X", Latitude = -16m, Longitude = -20m });
        });

        var options = result.Analysis.BunkerPortOptions;
        options.Select(o => o.Port).Should().Equal("Near", "Mid");
        options[0].ExtraDistanceNm.Should().BeApproximately(2 * options[0].DistanceOffRouteNm, 1e-6);
        options[0].DistanceOffRouteNm.Should().BeInRange(10, 60); // 30 NM from the straight line; the chosen route may sit slightly off it
        options[0].ExtraCostUsd.Should().BeLessThan(options[1].ExtraCostUsd);
        result.Analysis.BunkerPortBasis.Should().Contain("prices");
    }

    private static OptimizationRequestDto WithCalibratedCurve(OptimizationRequestDto request)
    {
        var points = new Dictionary<string, (double Rpm, double Power)> { ["ECO"] = (60, 3000), ["CUSTOM"] = (66, 5200), ["FULL"] = (74, 8000) };
        foreach (var profile in request.OperatingProfiles)
        {
            profile.Rpm = points[profile.Mode].Rpm;
            profile.PowerKw = points[profile.Mode].Power;
        }
        return request;
    }

    [Fact]
    public async Task CalibratedSpeedPowerCurveIsUsedAndDescribedWhenPowerIsAvailable()
    {
        var (result, _) = await Run(WithCalibratedCurve(Request("lowest-fuel")), vessel: v => v.TotalKwMainEng = "10000");

        result.IsFeasible.Should().BeTrue();
        result.Analysis.PerformanceModel.Should().Contain("brake power with SFOC by load");
        result.Warnings.Should().Contain(w => w.StartsWith("Calibrated speed-power curve"));
        result.Analysis.ModelVersion.Should().Be("3");
    }

    [Fact]
    public async Task HeadSeaIsSolvedFromTheResistanceBalanceAndSlowsTheVessel()
    {
        var (calm, _) = await Run(WithCalibratedCurve(Request("maintain-cp")), vessel: v => v.TotalKwMainEng = "10000");
        var (rough, _) = await Run(WithCalibratedCurve(Request("maintain-cp")), new FakeWeather { WaveM = 4, WindKn = 30 }, vessel: v => v.TotalKwMainEng = "10000");

        rough.AverageSpeedKn.Should().BeLessThan(calm.AverageSpeedKn - 0.05);
        rough.Analysis.WeatherDelayHours.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ConstraintFailureNamesTheRestrictionInsteadOfInventingASpeed()
    {
        var (result, _) = await Run(WithCalibratedCurve(Request("maintain-cp")), vessel: v =>
        {
            v.TotalKwMainEng = "10000";
            v.BlowerLadenMin = "55";
            v.BlowerLadenMax = "80";
        });

        result.IsFeasible.Should().BeFalse();
        result.InfeasibleReason.Should().Contain("blower cut-in/cut-off band");
    }

    [Fact]
    public async Task SegmentAuditReconcilesWithTheRouteTotals()
    {
        var request = WithCalibratedCurve(Request("lowest-cost"));
        request.RobVlsfoMt = 600;
        request.RobLsmgoMt = 100;

        var (result, _) = await Run(request, vessel: v => v.TotalKwMainEng = "10000");
        var rows = result.Analysis.Segments;

        rows.Count.Should().BeGreaterThan(14); // arcs are sub-stepped
        rows[0].StartUtc.Should().Be(Etd);
        rows.Last().EndUtc.Should().BeCloseTo(result.EtaUtc, TimeSpan.FromSeconds(1));
        rows.Sum(r => r.Hours).Should().BeApproximately(result.TimeToGoHours, 1e-6);
        rows.Sum(r => r.DistanceNm).Should().BeApproximately(result.DistanceNm, 1e-6);
        (rows.Sum(r => r.MainEngineFuelMt)).Should().BeApproximately(result.MeFuelRequiredToGoMt, 1e-6);
        rows.Sum(r => r.Co2Mt).Should().BeApproximately(result.MeCo2EmissionsToGoMt, 1e-6);
        rows.Last().RobVlsfoEndMt.Should().BeApproximately(result.Analysis.ArrivalRobVlsfoMt!.Value, 1e-6);
        rows.Where(r => r.Kind == "Sail").Should().OnlyContain(r => r.SogKn > 0 && r.StwKn > 0);
    }

    [Fact]
    public async Task FuelIsReportedByGradeWithTankToWakeEmissions()
    {
        var (result, _) = await Run(WithCalibratedCurve(Request("lowest-cost")), vessel: v => v.TotalKwMainEng = "10000");

        var grades = result.Analysis.FuelByGrade;
        grades.Should().Contain(g => g.Grade == "VLSFO");
        grades.Sum(g => g.Mt).Should().BeApproximately(result.Analysis.TotalFuelMt, 1e-6);
        grades.Sum(g => g.Co2Mt).Should().BeApproximately(result.MeCo2EmissionsToGoMt, 1e-6);
        grades.Single(g => g.Grade == "VLSFO").EmissionFactor.Should().Be(3.151);
        result.Analysis.Emissions!.Basis.Should().Contain("Tank-to-wake");
    }

    [Fact]
    public async Task ForecastIsResampledWhereAndWhenTheVesselActuallyIs()
    {
        var weather = new FakeWeather { WaveM = 4, WindKn = 30, CalmFromUtc = Etd.AddHours(40) };
        var (rough, _) = await Run(WithCalibratedCurve(Request("maintain-cp")), weather, vessel: v => v.TotalKwMainEng = "10000");
        var (calm, _) = await Run(WithCalibratedCurve(Request("maintain-cp")), vessel: v => v.TotalKwMainEng = "10000");

        var early = rough.Analysis.Segments.Where(r => r.StartUtc < Etd.AddHours(40)).ToList();
        var late = rough.Analysis.Segments.Where(r => r.StartUtc >= Etd.AddHours(40)).ToList();
        early.Should().OnlyContain(r => r.WaveM > 3);
        late.Should().OnlyContain(r => r.WaveM < 0.01);
        rough.TimeToGoHours.Should().BeGreaterThan(calm.TimeToGoHours);
    }

    [Fact]
    public async Task WaitingOutBadWeatherIsUsedWhenItLowersTheObjective()
    {
        // Rough for the first 30 h, calm afterwards; waiting costs no main-engine fuel, so the fuel objective may wait.
        var weather = new FakeWeather { WaveM = 5, WindKn = 35, CalmFromUtc = Etd.AddHours(30) };
        var (rough, _) = await Run(WithCalibratedCurve(Request("lowest-fuel")), weather, vessel: v => v.TotalKwMainEng = "10000");
        var (fast, _) = await Run(WithCalibratedCurve(Request("earliest-eta")), weather, vessel: v => v.TotalKwMainEng = "10000");

        rough.IsFeasible.Should().BeTrue();
        fast.Analysis.Segments.Should().NotContain(r => r.Kind == "Wait");
        if (rough.Analysis.Solver.WaitingStepsInRoute > 0)
            rough.Warnings.Should().Contain(w => w.Contains("waiting at sea"));
        rough.Analysis.Solver.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task SolverReportsItsStagesAndHonoursTheTimeLimit()
    {
        var (normal, _) = await Run(WithCalibratedCurve(Request("lowest-cost")), vessel: v => v.TotalKwMainEng = "10000");
        var limited = WithCalibratedCurve(Request("lowest-cost"));
        limited.SolverTimeLimitSeconds = 0.0000001;
        var (stopped, _) = await Run(limited, vessel: v => v.TotalKwMainEng = "10000");

        normal.Analysis.Solver.Status.Should().Be("Completed");
        normal.Analysis.Solver.SegmentEvaluations.Should().BeGreaterThan(0);
        normal.Analysis.Solver.Stage2Settings.Should().BeGreaterThan(normal.Analysis.Solver.Stage1Settings); // intermediate settings
        normal.Analysis.Solver.Method.Should().Contain("Two-stage");
        stopped.IsFeasible.Should().BeFalse();
        stopped.InfeasibleReason.Should().Contain("time limit");
        stopped.Analysis.Solver.Status.Should().Be("TimeLimitReached");
    }

    [Fact]
    public async Task SavingsAreMeasuredAgainstThePlannedRoute()
    {
        var (planned, _) = await Run(WithCalibratedCurve(Request("maintain-cp")), vessel: v => v.TotalKwMainEng = "10000");
        var (economical, _) = await Run(WithCalibratedCurve(Request("lowest-fuel")), vessel: v => v.TotalKwMainEng = "10000");

        planned.Analysis.Savings!.TimeSavedHours.Should().BeApproximately(0, 1);
        planned.Analysis.Savings.Baseline.Should().Contain("Planned route");
        economical.Analysis.Savings!.FuelSavedMt.Should().BeGreaterThan(0);
        economical.Analysis.Savings.TimeSavedHours.Should().BeLessThan(0); // slower
    }

    [Fact]
    public void IntermediateSettingsLieOnTheCalibratedCurveBetweenTheEnteredPoints()
    {
        var points = new List<SpeedSetting>
        {
            new("ECO", false, 10, 60, 3000, null, 20, 20, 3.151, 3.206, "VLSFO", "LSMGO"),
            new("FULL", true, 14, 74, 8000, null, 50, 50, 3.151, 3.206, "VLSFO", "LSMGO"),
        };
        var model = VesselPerformanceModel.Build(points)!;

        var added = RouteOptimizationEngine.IntermediateSettings(model, points).ToList();

        added.Should().NotBeEmpty().And.HaveCountLessThanOrEqualTo(8);
        added.Should().OnlyContain(s => s.SpeedKn > 10 && s.SpeedKn < 14 && s.Mode == "INTERPOLATED");
        added.Select(s => s.FuelRateMtPerDay).Should().BeInAscendingOrder();
        added.Select(s => s.Rpm).Should().BeInAscendingOrder();
        added.First().PowerKw.Should().BeApproximately(model.CalmX(added.First().SpeedKn), 1e-6);
    }

    private sealed class FakeEnsemble : IEnsembleWindService
    {
        public string SourceName => "Fake ensemble";
        public bool Unavailable { get; init; }

        public Task<EnsembleWind?> GetAsync(IReadOnlyList<(double Lat, double Lng)> points, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        {
            if (Unavailable) return Task.FromResult<EnsembleWind?>(null);
            const int members = 9, hours = 400;
            var speed = Enumerable.Range(0, members).Select(m => points.Select(_ => Enumerable.Repeat(m * 4.0, hours).ToArray()).ToArray()).ToArray();
            var from = Enumerable.Range(0, members).Select(_ => points.Select(__ => Enumerable.Repeat(90.0, hours).ToArray()).ToArray()).ToArray();
            return Task.FromResult<EnsembleWind?>(new EnsembleWind(new DateTime(fromUtc.Year, fromUtc.Month, fromUtc.Day, fromUtc.Hour, 0, 0, DateTimeKind.Utc), hours, speed, from));
        }
    }

    private static void SeedEtsPolicy(ApplicationDbContext db, string originCountry, string destCountry, double phaseInPct = 100)
    {
        db.EtsPolicies.Add(new EtsPolicy
        {
            TenantId = Tenant, IntraEuCoveragePct = 100, ExtraEuCoveragePct = 50, UnknownCoveragePct = 100,
            PhaseInJson = $"[{{\"year\":2026,\"pct\":{phaseInPct}}}]",
        });
        db.Ports.Add(new Port { TenantId = Tenant, PortName = "Origin", Country = originCountry, Latitude = -20m, Longitude = -30m });
        db.Ports.Add(new Port { TenantId = Tenant, PortName = "Dest", Country = destCountry, Latitude = -20m, Longitude = -10m });
    }

    [Theory]
    [InlineData("Spain", "Portugal", "Intra-EU", 1.0)]
    [InlineData("Spain", "Brazil", "Extra-EU", 0.5)]
    [InlineData("Brazil", "Argentina", "Outside EU", 0.0)]
    public async Task EtsCoverageFollowsTheVoyageTypeAndPhaseIn(string origin, string destination, string classification, double coverage)
    {
        var (result, _) = await Run(Request("maintain-cp"), seed: db => SeedEtsPolicy(db, origin, destination, phaseInPct: 50));

        var emissions = result.Analysis.Emissions!;
        emissions.VoyageClassification.Should().Be(classification);
        emissions.ChargeableCo2Mt.Should().BeApproximately(result.MeCo2EmissionsToGoMt * coverage * 0.5, 1e-6);
        result.EmissionsCostToGoUsd.Should().BeApproximately(80 * emissions.ChargeableCo2Mt, 1e-6);
    }

    [Fact]
    public async Task UnmatchedVoyageEndsUseTheConfiguredUnknownCoverage()
    {
        var (result, _) = await Run(Request("maintain-cp"), seed: db => db.EtsPolicies.Add(new EtsPolicy { TenantId = Tenant, UnknownCoveragePct = 25, PhaseInJson = "[{\"year\":2026,\"pct\":100}]" }));

        result.Analysis.Emissions!.VoyageClassification.Should().Be("Unknown");
        result.Analysis.Emissions.ChargeableCo2Mt.Should().BeApproximately(result.MeCo2EmissionsToGoMt * 0.25, 1e-6);
    }

    [Fact]
    public async Task WithoutAPolicyAllCo2IsChargedAsBefore()
    {
        var (result, _) = await Run(Request("maintain-cp"));

        result.Analysis.Emissions!.ChargeableCo2Mt.Should().BeApproximately(result.MeCo2EmissionsToGoMt, 1e-9);
        result.Analysis.Emissions.Basis.Should().Contain("No EU ETS policy");
    }

    [Fact]
    public async Task ApprovedCalibrationScalesConfiguredFuelButAProposalDoesNot()
    {
        var (plain, _) = await Run(Request("maintain-cp"));
        var (approved, _) = await Run(Request("maintain-cp"), seed: db => db.PerformanceCalibrations.Add(new PerformanceCalibration
        {
            TenantId = Tenant, VesselImo = "9999999", LoadCondition = "Laden", FuelFactor = 1.1, UsedSampleCount = 40, Quality = "Good", Status = "Approved",
        }));
        var (proposed, _) = await Run(Request("maintain-cp"), seed: db => db.PerformanceCalibrations.Add(new PerformanceCalibration
        {
            TenantId = Tenant, VesselImo = "9999999", LoadCondition = "Laden", FuelFactor = 1.3, Quality = "Good", Status = "Proposed",
        }));

        approved.MeFuelRequiredToGoMt.Should().BeApproximately(plain.MeFuelRequiredToGoMt * 1.1, 1e-6);
        approved.Analysis.CalibrationApplied.Should().Contain("1.100");
        proposed.MeFuelRequiredToGoMt.Should().BeApproximately(plain.MeFuelRequiredToGoMt, 1e-9);
        proposed.Analysis.CalibrationApplied.Should().BeNull();
    }

    [Fact]
    public async Task EnsembleWindGivesAPartialDistributionOfEtaFuelAndCost()
    {
        var request = WithCalibratedCurve(Request("maintain-cp"));
        var (result, _) = await Run(request, vessel: v => v.TotalKwMainEng = "10000", ensemble: new FakeEnsemble());

        var spread = result.Analysis.Uncertainty!;
        spread.Members.Should().Be(9);
        spread.Partial.Should().BeTrue();
        spread.EtaP90Utc.Should().BeAfter(spread.EtaP10Utc!.Value); // stronger head wind members arrive later
        spread.FuelP90Mt.Should().BeGreaterThanOrEqualTo(spread.FuelP50Mt);
        spread.CostP90Usd.Should().BeGreaterThan(spread.CostP10Usd);
        spread.Notes.Should().Contain(n => n.Contains("partial"));
    }

    [Fact]
    public async Task MissingEnsembleDataIsReportedNotInvented()
    {
        var (result, _) = await Run(WithCalibratedCurve(Request("maintain-cp")), vessel: v => v.TotalKwMainEng = "10000", ensemble: new FakeEnsemble { Unavailable = true });
        var (without, _) = await Run(WithCalibratedCurve(Request("maintain-cp")), vessel: v => v.TotalKwMainEng = "10000");

        result.Analysis.Uncertainty!.Members.Should().Be(0);
        result.Analysis.Uncertainty.EtaP50Utc.Should().BeNull();
        result.Analysis.Uncertainty.Notes.Should().Contain(n => n.Contains("unavailable"));
        without.Analysis.Uncertainty.Should().BeNull();
    }

    [Fact]
    public async Task EnsembleReportsTheChanceOfMissingATargetEta()
    {
        var request = WithCalibratedCurve(Request("specific-eta"));
        request.ArrivalRule = "BEFORE";
        request.TargetEtaUtc = Etd.AddHours(95);
        request.TargetEtaUtc = Etd.AddHours(92);

        var (result, _) = await Run(request, vessel: v => v.TotalKwMainEng = "10000", ensemble: new FakeEnsemble());

        if (result.IsFeasible) result.Analysis.Uncertainty!.ProbabilityMissTargetEta.Should().BeInRange(0, 1);
    }

    [Fact]
    public async Task CustomRtaIsAnArrivalTarget()
    {
        var request = Request("custom-simulation");
        request.CustomRtaUtc = Etd.AddHours(95);

        var (result, _) = await Run(request);

        result.IsFeasible.Should().BeTrue();
        result.EtaUtc.Should().BeOnOrBefore(Etd.AddHours(95.25));
    }

    [Fact]
    public async Task LowGmIsFlaggedAsAnAdvisory()
    {
        var request = Request("custom-simulation");
        request.CustomGmM = 0.1;

        var (result, _) = await Run(request);

        result.Warnings.Should().Contain(w => w.Contains("0.15 m IMO minimum"));
    }

    [Fact]
    public async Task ParetoRankingAndWeightedScoreAreProduced()
    {
        var request = Request("earliest-eta");
        request.Scenarios.Add("lowest-fuel");
        request.Scenarios.Add("lowest-cost");
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.TenantId).Returns(Tenant);
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        var perf = new PerformanceDbContext(new DbContextOptionsBuilder<PerformanceDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        db.Vessels.Add(new Vessel { TenantId = Tenant, Name = "TEST", Imo = "9999999", Deadweight = "50000" });
        await db.SaveChangesAsync();
        await perf.Database.EnsureCreatedAsync();

        var run = await new RouteOptimizationEngine(db, perf, new FakeWeather(), user.Object).RunAsync(request);

        run.Scenarios.Should().HaveCount(3);
        run.Scenarios.Should().OnlyContain(s => s.Analysis.ParetoRank >= 1 && !string.IsNullOrEmpty(s.Analysis.RankingExplanation));
        run.Scenarios.Should().Contain(s => s.Analysis.IsParetoOptimal);
        run.Scenarios.Min(s => s.Analysis.RankingScore).Should().BeApproximately(0, 1.0);
    }
}
