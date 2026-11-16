using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities.Optimization;
using MultiTenantSaaS.Domain.Entities.Performance;
using MultiTenantSaaS.Infrastructure.Persistence;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class OptimizationMonitoringServiceTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTime Etd = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private static List<RouteWaypointDto> Route() => new()
    {
        new() { Lat = -20, Lng = -30, EtaUtc = Etd, SpeedKn = 0, CumulativeDistanceNm = 0, RobVlsfoMt = 400, RobLsmgoMt = 80 },
        new() { Lat = -20, Lng = -20, EtaUtc = Etd.AddHours(50), SpeedKn = 12, CumulativeDistanceNm = 564, Rpm = 70, PowerKw = 7000, FuelRateMtPerDay = 28, RobVlsfoMt = 340, RobLsmgoMt = 78 },
        new() { Lat = -20, Lng = -10, EtaUtc = Etd.AddHours(100), SpeedKn = 12, CumulativeDistanceNm = 1128, Rpm = 70, PowerKw = 7000, FuelRateMtPerDay = 28, RobVlsfoMt = 280, RobLsmgoMt = 76 },
    };

    private static (OptimizationMonitoringService Service, ApplicationDbContext Db, PerformanceDbContext Perf, Guid RunId) Setup(IEnumerable<TracksheetRow>? rows = null)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.TenantId).Returns(Tenant);
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        var perf = new PerformanceDbContext(new DbContextOptionsBuilder<PerformanceDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);

        var run = new OptimizationRun { TenantId = Tenant, VoyageId = "V1", VesselImo = "9999999", EtdUtc = Etd, LoadCondition = "Laden" };
        run.ScenarioResults.Add(new OptimizationScenarioResult
        {
            Scenario = "maintain-cp", IsFeasible = true, EtdUtc = Etd, EtaUtc = Etd.AddHours(100),
            RouteWaypointsJson = JsonSerializer.Serialize(Route()),
            AnalysisJson = JsonSerializer.Serialize(new OptimizationAnalysisDto { ForecastFetchedAtUtc = DateTime.UtcNow.AddHours(-1) }),
        });
        db.OptimizationRuns.Add(run);
        if (rows != null) perf.TracksheetRows.AddRange(rows);
        db.SaveChanges();
        perf.SaveChanges();
        return (new OptimizationMonitoringService(db, perf, user.Object), db, perf, run.Id);
    }

    private static OptimizationCheckRequestDto At(double lat, double lng, double hoursAfterEtd) =>
        new() { PositionLat = lat, PositionLng = lng, PositionTimeUtc = Etd.AddHours(hoursAfterEtd) };

    [Fact]
    public async Task OnTrackAndOnSchedulePositionRaisesNothing()
    {
        var (service, db, _, runId) = Setup();

        var result = await service.CheckRunAsync(runId, At(-20, -25, 25));

        result!.ScheduleDeviationHours.Should().BeApproximately(0, 0.1);
        result.CrossTrackNm.Should().BeLessThan(1);
        result.ReoptimizeRecommended.Should().BeFalse();
        (await db.OptimizationRecommendations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BeingBehindScheduleAndOffTrackRaisesOneSuggestionAndRepeatsAreDeduplicated()
    {
        var (service, db, _, runId) = Setup();

        var first = await service.CheckRunAsync(runId, At(-19, -25, 33)); // 8 h late at half-way, 60 NM off
        var second = await service.CheckRunAsync(runId, At(-19, -24, 35));

        first!.ReoptimizeRecommended.Should().BeTrue();
        first.ScheduleDeviationHours.Should().BeGreaterThan(3);
        first.CrossTrackNm.Should().BeGreaterThan(15);
        first.RecommendationAction.Should().Be("created");
        second!.RecommendationAction.Should().Be("updated");
        (await db.OptimizationRecommendations.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DismissedSuggestionsStaySilentDuringTheCooldown()
    {
        var (service, db, _, runId) = Setup();
        var first = await service.CheckRunAsync(runId, At(-19, -25, 33));

        (await service.ResolveAsync(first!.RecommendationId!.Value, confirm: false))!.Status.Should().Be("Dismissed");
        var again = await service.CheckRunAsync(runId, At(-19, -24, 35));

        again!.ReoptimizeRecommended.Should().BeTrue();
        again.RecommendationAction.Should().Be("suppressed");
        (await db.OptimizationRecommendations.CountAsync()).Should().Be(1);
        (await service.ResolveAsync(first.RecommendationId.Value, true)).Should().BeNull();
    }

    [Fact]
    public async Task LowerThanPlannedBunkersAreFlagged()
    {
        var (service, _, _, runId) = Setup();
        var request = At(-20, -25, 25);
        request.RobVlsfoMt = 300; // plan says 370 at this point

        var result = await service.CheckRunAsync(runId, request);

        result!.RobVlsfoDeviationPct.Should().BeLessThan(-10);
        result.Reasons.Should().Contain(r => r.Contains("VLSFO"));
    }

    [Fact]
    public async Task PositionAndBunkersDefaultToTheLatestTracksheetReport()
    {
        var rows = new[]
        {
            new TracksheetRow { TenantId = Tenant, VoyageId = "V1", VesselImo = "9999999", SortOrder = 1, Date = "02Jul2026", Time = "0100", Lat = "2000S", Lng = "02500W", VlsfoRob = 370, LsmgoRob = 79 },
        };
        var (service, _, _, runId) = Setup(rows);

        var result = await service.CheckVoyageAsync("V1", new());

        result!.Source.Should().Contain("tracksheet");
        result.PositionLat.Should().BeApproximately(-20, 1e-6);
        result.PositionLng.Should().BeApproximately(-25, 1e-6);
        result.ReoptimizeRecommended.Should().BeFalse();
    }

    [Fact]
    public async Task DefaultDepartureRowIsNotTreatedAsAReport()
    {
        var rows = new[]
        {
            new TracksheetRow { TenantId = Tenant, VoyageId = "V1", VesselImo = "9999999", SortOrder = 0, IsDefault = true, Rt = "D", Date = "01Jul2026", Time = "0000", Lat = "2000S", Lng = "03000W" },
        };
        var (service, _, _, runId) = Setup(rows);

        var result = await service.CheckVoyageAsync("V1", new());

        result!.PositionLat.Should().BeNull();
        result.ReoptimizeRecommended.Should().BeFalse();
    }

    [Theory]
    [InlineData("0545N", 5.75)]
    [InlineData("15347E", 153.783333)]
    [InlineData("2000S", -20)]
    [InlineData("02500W", -25)]
    public void TracksheetCoordinatesAreParsed(string text, double expected)
    {
        OptimizationMonitoringService.ParseCoordinate(text)!.Value.Should().BeApproximately(expected, 1e-5);
    }

    [Fact]
    public void InvalidCoordinatesAreRejected()
    {
        OptimizationMonitoringService.ParseCoordinate("abc").Should().BeNull();
        OptimizationMonitoringService.ParseCoordinate("0570N").Should().BeNull();
    }

    [Fact]
    public async Task ComparisonReportsBiasAndLeavesTheCalibrationAlone()
    {
        var rows = new[]
        {
            new TracksheetRow { TenantId = Tenant, VoyageId = "V1", VesselImo = "9999999", SortOrder = 0, Date = "01Jul2026", Time = "0000", VlsfoRob = 400 },
            new TracksheetRow { TenantId = Tenant, VoyageId = "V1", VesselImo = "9999999", SortOrder = 1, Date = "02Jul2026", Time = "0000", Hrs = 24, AvgSpeedO = 11, Rpm = 72, EnginePower = 7400, VlsfoRob = 370 },
        };
        var (service, _, perf, runId) = Setup(rows);

        var result = await service.ComparePerformanceAsync(runId, null);

        result!.Points.Should().ContainSingle();
        result.SpeedBiasKn.Should().BeApproximately(-1, 1e-9);
        result.RpmBias.Should().BeApproximately(2, 1e-9);
        result.FuelBiasMtPerDay.Should().BeApproximately(30 - 28, 1e-9);
        result.Notes.Should().Contain(n => n.Contains("never overwrites"));
        (await perf.TracksheetRows.CountAsync()).Should().Be(2);
    }

    [Fact]
    public void ProjectionFindsTheAlongTrackDistanceAndCrossTrackOffset()
    {
        var (along, cross, segment, fraction) = OptimizationMonitoringService.Project(Route(), -19, -25);

        along.Should().BeApproximately(282, 5);
        cross.Should().BeApproximately(60, 2);
        segment.Should().Be(0);
        fraction.Should().BeApproximately(0.5, 0.02);
    }
}
