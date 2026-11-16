using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Entities.Optimization;
using MultiTenantSaaS.Domain.Entities.Performance;
using MultiTenantSaaS.Infrastructure.Persistence;
using MultiTenantSaaS.Infrastructure.Services;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class EtsContextTests
{
    [Fact]
    public void FactorIsCoverageTimesPhaseInForTheEmissionYear()
    {
        var ets = new EtsContext("Extra-EU", 50, new[] { (2024, 40.0), (2025, 70.0), (2026, 100.0) }, "test");

        ets.Factor(new DateTime(2023, 12, 31)).Should().Be(0);
        ets.Factor(new DateTime(2024, 6, 1)).Should().BeApproximately(0.2, 1e-9);
        ets.Factor(new DateTime(2025, 6, 1)).Should().BeApproximately(0.35, 1e-9);
        ets.Factor(new DateTime(2026, 6, 1)).Should().BeApproximately(0.5, 1e-9);
        ets.Factor(new DateTime(2031, 6, 1)).Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void WithoutAPolicyEverythingIsCharged()
    {
        EtsContext.Unconfigured().Factor(new DateTime(2026, 1, 1)).Should().Be(1);
    }

    [Theory]
    [InlineData("Germany", true)]
    [InlineData("nl", true)]
    [InlineData("Norway", true)]
    [InlineData("United Kingdom", false)]
    [InlineData("", false)]
    public void EuAndEeaCountriesAreRecognised(string country, bool expected) =>
        EtsCountries.IsEuEea(country).Should().Be(expected);
}

public class EtsPolicyServiceTests
{
    private static EtsPolicyService Service(out ApplicationDbContext db)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.TenantId).Returns(Guid.NewGuid());
        db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        return new EtsPolicyService(db, user.Object);
    }

    [Fact]
    public async Task DefaultsAreReturnedUnconfiguredUntilSaved()
    {
        var service = Service(out _);

        var policy = await service.GetAsync();

        policy.IsConfigured.Should().BeFalse();
        policy.PhaseIn.Select(p => p.Pct).Should().Equal(40, 70, 100);
    }

    [Fact]
    public async Task ValidPolicyIsSavedAndReadBack()
    {
        var service = Service(out _);
        var (saved, problem) = await service.SaveAsync(new EtsPolicyDto
        {
            IntraEuCoveragePct = 100, ExtraEuCoveragePct = 50, UnknownCoveragePct = 0,
            PhaseIn = { new EtsPhaseInDto { Year = 2027, Pct = 100 } },
        });
        problem.Should().BeNull();
        saved!.IsConfigured.Should().BeTrue();

        var read = await service.GetAsync();
        read.IsConfigured.Should().BeTrue();
        read.UnknownCoveragePct.Should().Be(0);
        read.PhaseIn.Select(p => p.Year).Should().Contain(2027);
    }

    [Fact]
    public async Task InvalidPoliciesAreRejected()
    {
        var service = Service(out _);

        (await service.SaveAsync(new EtsPolicyDto { ExtraEuCoveragePct = 150 })).Problem.Should().NotBeNull();
        (await service.SaveAsync(new EtsPolicyDto { PhaseIn = new() { new() { Year = 2025, Pct = 10 }, new() { Year = 2025, Pct = 20 } } })).Problem.Should().Contain("once");
        (await service.SaveAsync(new EtsPolicyDto { PhaseIn = new() { new() { Year = 2010, Pct = 10 } } })).Problem.Should().NotBeNull();
        (await service.SaveAsync(new EtsPolicyDto { PhaseIn = new() })).Problem.Should().NotBeNull();
    }
}

public class PerformanceCalibrationServiceTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static (PerformanceCalibrationService Service, ApplicationDbContext Db) Setup(IEnumerable<TracksheetRow> rows)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.TenantId).Returns(Tenant);
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        var perf = new PerformanceDbContext(new DbContextOptionsBuilder<PerformanceDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        db.Vessels.Add(new Vessel { TenantId = Tenant, Name = "T", Imo = "9999999", Deadweight = "50000" });
        perf.TracksheetRows.AddRange(rows);
        db.SaveChanges();
        perf.SaveChanges();
        return (new PerformanceCalibrationService(db, perf, user.Object), db);
    }

    private static CalibrationFitRequestDto Request() => new()
    {
        VesselImo = "9999999", LoadCondition = "Laden",
        OperatingProfiles =
        {
            new() { Mode = "ECO", LoadCondition = "Laden", SpeedKn = 10, FuelType1 = "VLSFO", DailyCons1Mt = 20, FuelType2 = "LSMGO", DailyCons2Mt = 0.5 },
            new() { Mode = "CUSTOM", LoadCondition = "Laden", SpeedKn = 12, FuelType1 = "VLSFO", DailyCons1Mt = 30, FuelType2 = "LSMGO", DailyCons2Mt = 0.5 },
            new() { Mode = "FULL", IsCpDefault = true, LoadCondition = "Laden", SpeedKn = 14, FuelType1 = "VLSFO", DailyCons1Mt = 40, FuelType2 = "LSMGO", DailyCons2Mt = 0.5 },
        },
    };

    // Noon reports at 12 kn burning `burn` MT/day (the CP curve says 30).
    private static List<TracksheetRow> Reports(int count, double burn)
    {
        var rows = new List<TracksheetRow>();
        var start = new DateTime(2026, 5, 1);
        for (var i = 0; i < count; i++)
        {
            var day = start.AddDays(i);
            rows.Add(new TracksheetRow
            {
                TenantId = Tenant, VoyageId = "H1", VesselImo = "9999999", SortOrder = i, Rt = i == 0 ? "D" : "N",
                Date = day.ToString("ddMMMyyyy", System.Globalization.CultureInfo.InvariantCulture), Time = "1200",
                Hrs = 24, AvgSpeedO = 12, Rpm = 70, EnginePower = 7000, Amount = 50000, VlsfoRob = 1000 - burn * i, LsmgoRob = 200 - 0.1 * i,
            });
        }
        return rows;
    }

    [Fact]
    public async Task FitFindsTheFuelFactorAndMonthlySeries()
    {
        var (service, _) = Setup(Reports(41, 33));

        var (fit, problem) = await service.FitAsync(Request());

        problem.Should().BeNull();
        fit!.FuelFactor.Should().BeApproximately(1.1, 1e-6);
        fit.UsedSampleCount.Should().Be(40);
        fit.Quality.Should().Be("Good");
        fit.Status.Should().Be("Proposed");
        fit.Monthly.Should().HaveCountGreaterThanOrEqualTo(2);
        fit.Notes.Should().Contain(n => n.Contains("never") || n.Contains("cannot separate"));
    }

    [Fact]
    public async Task OutliersAreDroppedFromTheFit()
    {
        var rows = Reports(41, 33);
        rows[20].VlsfoRob = rows[19].VlsfoRob - 52; // one wildly high burn day; later ROBs shift accordingly
        for (var i = 21; i < rows.Count; i++) rows[i].VlsfoRob = rows[i].VlsfoRob - 19;
        var (service, _) = Setup(rows);

        var (fit, _) = await service.FitAsync(Request());

        fit!.FuelFactor.Should().BeApproximately(1.1, 0.02);
    }

    [Fact]
    public async Task TooLittleDataCannotBeApproved()
    {
        var (service, _) = Setup(Reports(5, 33));

        var (fit, _) = await service.FitAsync(Request());
        var (approved, problem) = await service.ReviewAsync(fit!.Id, approve: true);

        fit.Quality.Should().Be("Insufficient");
        approved.Should().BeNull();
        problem.Should().Contain("too little data");
    }

    [Fact]
    public async Task ApprovalSupersedesThePreviousAndKeepsHistory()
    {
        var (service, db) = Setup(Reports(41, 33));
        var first = (await service.FitAsync(Request())).Calibration!;
        (await service.ReviewAsync(first.Id, true)).Calibration!.Status.Should().Be("Approved");

        var second = (await service.FitAsync(Request())).Calibration!;
        (await service.ReviewAsync(second.Id, true)).Calibration!.Status.Should().Be("Approved");

        var all = await service.ListAsync("9999999", "Laden");
        all.Count(c => c.Status == "Approved").Should().Be(1);
        all.Single(c => c.Id == first.Id).Status.Should().Be("Superseded");
        (await service.ReviewAsync(first.Id, false)).Problem.Should().Contain("Only a proposed");
        (await db.PerformanceCalibrations.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task UnknownVesselAndBadLoadConditionAreRefused()
    {
        var (service, _) = Setup(Array.Empty<TracksheetRow>());

        (await service.FitAsync(new CalibrationFitRequestDto { VesselImo = "1", LoadCondition = "Laden" })).Problem.Should().Contain("not found");
        (await service.FitAsync(new CalibrationFitRequestDto { VesselImo = "9999999", LoadCondition = "Full" })).Problem.Should().Contain("Ballast or Laden");
    }
}

public class EnsembleParsingTests
{
    [Fact]
    public void MembersAreReadPerLocationAndAlignedToTheStartHour()
    {
        const string json = """
        [
          {"hourly":{"time":["2026-07-01T00:00","2026-07-01T01:00","2026-07-01T02:00"],
            "wind_speed_10m":[10,11,12],"wind_speed_10m_member01":[20,21,22],
            "wind_direction_10m":[90,90,90],"wind_direction_10m_member01":[180,180,180]}},
          {"hourly":{"time":["2026-07-01T00:00","2026-07-01T01:00","2026-07-01T02:00"],
            "wind_speed_10m":[1,2,3],"wind_speed_10m_member01":[4,5,6],
            "wind_direction_10m":[0,0,0],"wind_direction_10m_member01":[270,270,270]}}
        ]
        """;

        var wind = OpenMeteoEnsembleWindService.Parse(json, 2, new DateTime(2026, 7, 1, 1, 0, 0, DateTimeKind.Utc))!;

        wind.SpeedKn.Length.Should().Be(2); // control + member01
        wind.Hours.Should().Be(2);
        wind.SpeedKn[1][0].Should().Equal(21, 22);
        wind.FromDeg[1][1].Should().Equal(270, 270);
    }

    [Fact]
    public void MismatchedLocationCountIsRejected()
    {
        OpenMeteoEnsembleWindService.Parse("{\"hourly\":{\"time\":[\"2026-07-01T00:00\"],\"wind_speed_10m\":[1],\"wind_direction_10m\":[1]}}", 2, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc))
            .Should().BeNull();
    }
}
