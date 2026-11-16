using FluentAssertions;
using MultiTenantSaaS.Domain.Entities.Performance;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class WeatherFactorLookupTests
{
    private static WeatherFactorEntry Cell(double dw, double mag, double angle, double value) =>
        new() { Weather = "Wave", DeadWeight = dw, Magnitude = mag, Angle = angle, Value = value };

    private static WeatherFactorLookup Table() => new(new[]
    {
        Cell(10000, 2, 0, -1), Cell(10000, 2, 180, 0), Cell(10000, 4, 0, -3), Cell(10000, 4, 180, -1),
        Cell(50000, 2, 0, -0.5), Cell(50000, 2, 180, 0), Cell(50000, 4, 0, -1.5), Cell(50000, 4, 180, -0.5),
    }, "Wave");

    [Fact]
    public void ExactCellsAreReturned()
    {
        Table().Effect(10000, 4, 0).Should().BeApproximately(-3, 1e-9);
    }

    [Fact]
    public void ValuesBetweenCellsAreInterpolatedOnEveryAxis()
    {
        var table = Table();
        table.Effect(10000, 3, 0).Should().BeApproximately(-2, 1e-9);        // magnitude midpoint
        table.Effect(10000, 2, 90).Should().BeApproximately(-0.5, 1e-9);     // angle midpoint
        table.Effect(30000, 2, 0).Should().BeApproximately(-0.75, 1e-9);     // deadweight midpoint
    }

    [Fact]
    public void BelowTheSmallestMagnitudeThereIsNoEffectAndAboveTheLargestTheEdgeIsHeld()
    {
        var table = Table();
        table.Effect(10000, 1, 0).Should().Be(0);
        table.Effect(10000, 9, 0).Should().BeApproximately(-3, 1e-9);
    }
}
