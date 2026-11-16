using FluentAssertions;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Entities.Performance;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class RealPerformanceProfileTests
{
    private static TracksheetRow Departure(double vlsfo = 500, double lsmgo = 100) => new()
    {
        VoyageId = "V1", SortOrder = 0, Rt = "D", Date = "01Jul2026", Time = "0000",
        VlsfoRob = vlsfo, LsmgoRob = lsmgo, Amount = 50000,
    };

    private static TracksheetRow Noon(int order, double vlsfo, double lsmgo, double rpm = 70, double speed = 11, double windF = 0, double waveF = 0, double currF = 0, double amount = 50000) => new()
    {
        VoyageId = "V1", SortOrder = order, Rt = "N", Date = $"{order + 1:00}Jul2026", Time = "1200",
        Hrs = 24, AvgSpeedO = speed, Rpm = rpm, EnginePower = 7000, Amount = amount,
        VlsfoRob = vlsfo, LsmgoRob = lsmgo, WindF = windF, WaveF = waveF, CurrF = currF,
    };

    private static List<TracksheetRow> Voyage(Func<int, TracksheetRow> noon, int noons = 4) =>
        new[] { Departure() }.Concat(Enumerable.Range(1, noons).Select(noon)).ToList();

    [Fact]
    public void BuildsCalmWaterPointFromObservedReports()
    {
        var rows = Voyage(i => Noon(i, 500 - 30 * i, 100 - 0.1 * i, speed: 11, windF: -0.2, waveF: -0.1, currF: -0.5));

        var result = RealPerformanceProfile.Build(rows, "Laden", new Vessel());

        var point = result.Settings.Should().ContainSingle().Subject;
        point.Source.Should().Be("ACTUAL");
        point.SpeedKn.Should().BeApproximately(11.8, 0.001); // observed + lost speed given back
        point.FuelRateMtPerDay.Should().BeApproximately(30, 0.001);
        point.AuxGoRateMtPerDay.Should().BeApproximately(0.1, 0.001);
        point.SampleCount.Should().Be(4);
    }

    [Fact]
    public void RpmGroupsWithTooFewReportsAreNotOffered()
    {
        var rows = Voyage(i => Noon(i, 500 - 30 * i, 100), noons: 2);

        var result = RealPerformanceProfile.Build(rows, "Laden", new Vessel());

        result.Settings.Should().BeEmpty();
        result.Notes.Should().ContainSingle().Which.Should().Contain("at least 3");
    }

    [Fact]
    public void OnlyTheRequestedLoadConditionIsUsed()
    {
        var rows = Voyage(i => Noon(i, 500 - 30 * i, 100, amount: 0));

        RealPerformanceProfile.Build(rows, "Laden", new Vessel()).Settings.Should().BeEmpty();
        RealPerformanceProfile.Build(rows, "Ballast", new Vessel()).Settings.Should().HaveCount(1);
    }

    [Fact]
    public void FuelChangeoverReportsAreExcluded()
    {
        var rows = Voyage(i => Noon(i, 500 - 20 * i, 100 - 10 * i));

        RealPerformanceProfile.Build(rows, "Laden", new Vessel()).Settings.Should().BeEmpty();
    }

    [Fact]
    public void ReportsWithBunkeringAreExcluded()
    {
        var rows = Voyage(i =>
        {
            var row = Noon(i, 500 - 30 * i, 100);
            row.VlsfoBunkered = 200;
            return row;
        });

        RealPerformanceProfile.Build(rows, "Laden", new Vessel()).Settings.Should().BeEmpty();
    }

    [Fact]
    public void CriticalRpmRangeIsNeverOffered()
    {
        var rows = Voyage(i => Noon(i, 500 - 30 * i, 100, rpm: 72));
        var vessel = new Vessel { CriticalRpmMin = "70", CriticalRpmMax = "75" };

        RealPerformanceProfile.Build(rows, "Laden", vessel).Settings.Should().BeEmpty();
    }
}
