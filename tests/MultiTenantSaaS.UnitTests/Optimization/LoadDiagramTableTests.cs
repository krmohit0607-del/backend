using FluentAssertions;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class LoadDiagramTableTests
{
    private const string Json = """
        [
          {"key":"dslh","label":"Dead Slow Ahead (DSLH)","limit":"","loadPct":null,"rpm":30,"powerKw":900,"ballastSpeedKn":6,"ballastConsMt":4},
          {"key":"load-80","label":"80% load","limit":"EPL","loadPct":80,"rpm":70,"powerKw":9000,"minPowerKw":null,"maxPowerKw":9500,"maxTorqueKnm":null},
          {"key":"load-100","label":"100% load","limit":"Max MCR","loadPct":100,"rpm":76,"powerKw":11500},
          {"key":"load-40","label":"40% load","limit":"Least MCR","loadPct":40,"rpm":55,"powerKw":4600}
        ]
        """;

    [Fact]
    public void EplMaxAndLeastMcrMarkersBecomeEngineLoadLimits()
    {
        var markers = LoadLimitMarkers.Parse(Json);

        markers.MaxFractions.Should().BeEquivalentTo(new[] { 0.8, 1.0 });
        markers.MinFraction.Should().Be(0.4);
    }

    [Fact]
    public void MinMcrIsTheSameAsLeastMcr()
    {
        LoadLimitMarkers.Parse("""[{"limit":"Min MCR","loadPct":35}]""").MinFraction.Should().Be(0.35);
    }

    [Fact]
    public void MinAndMaxRpmMarkersBecomeRpmLimits()
    {
        var markers = LoadLimitMarkers.Parse("""[{"limit":"Min RPM","rpm":45},{"limit":"Max RPM","rpm":78},{"limit":"Max RPM","rpm":76}]""");

        markers.MinRpm.Should().Be(45);
        markers.MaxRpm.Should().Be(76);
    }

    [Fact]
    public void OperatingDataWithoutLimitsIsNotEnforcedAsALoadDiagram()
    {
        var points = LoadDiagramPoint.Parse(Json);

        points.Should().ContainSingle().Which.Rpm.Should().Be(70);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    public void MissingOrInvalidJsonGivesNoMarkers(string? json)
    {
        var markers = LoadLimitMarkers.Parse(json);

        markers.MaxFractions.Should().BeEmpty();
        markers.MinFraction.Should().BeNull();
        markers.MinRpm.Should().BeNull();
        markers.MaxRpm.Should().BeNull();
    }
}
