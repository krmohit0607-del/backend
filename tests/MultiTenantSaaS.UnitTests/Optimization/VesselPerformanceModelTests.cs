using FluentAssertions;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Entities.Performance;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class VesselPerformanceModelTests
{
    private static SpeedSetting Point(string mode, double speed, double rpm, double power, double fuel, bool cp = false) =>
        new(mode, cp, speed, rpm, power, null, fuel, fuel, 3.151, 3.206, "VLSFO", "LSMGO");

    // Speed-power exponent between points is about 3.1 (not an assumed cube law).
    private static List<SpeedSetting> Curve() => new()
    {
        Point("ECO", 10, 60, 3000, 20),
        Point("CUSTOM", 12, 66, 5200, 33),
        Point("FULL", 14, 74, 8000, 50, cp: true),
    };

    [Fact]
    public void CalmPowerPassesThroughEveryOperatingPoint()
    {
        var model = VesselPerformanceModel.Build(Curve())!;

        model.CalmX(10).Should().BeApproximately(3000, 1e-6);
        model.CalmX(12).Should().BeApproximately(5200, 1e-6);
        model.CalmX(14).Should().BeApproximately(8000, 1e-6);
        model.Basis.Should().Be(PowerBasis.BrakePowerKw);
    }

    [Fact]
    public void InterpolationFollowsTheLocalExponentOfTheData()
    {
        var model = VesselPerformanceModel.Build(Curve())!;
        var exponent = Math.Log(5200.0 / 3000) / Math.Log(12.0 / 10);

        model.CalmX(11).Should().BeApproximately(3000 * Math.Pow(1.1, exponent), 1e-6);
        exponent.Should().BeGreaterThan(2.9).And.BeLessThan(3.2);
    }

    [Fact]
    public void FuelUsesSfocByLoadAndMatchesTheOperatingPoints()
    {
        var model = VesselPerformanceModel.Build(Curve())!;

        model.FuelMtPerDay(3000).Should().BeApproximately(20, 1e-6);
        model.FuelMtPerDay(8000).Should().BeApproximately(50, 1e-6);
        var between = model.FuelMtPerDay(6600);
        between.Should().BeInRange(33, 50);
    }

    [Fact]
    public void RpmIsInterpolatedAndFollowsThePropellerLawWithASinglePoint()
    {
        var model = VesselPerformanceModel.Build(Curve())!;
        model.RpmAt(5200).Should().BeApproximately(66, 1e-6);
        model.RpmAt(6600)!.Value.Should().BeInRange(66, 74);

        var single = Curve().Select(s => s with { Rpm = s.Mode == "FULL" ? 74 : 0 }).ToList();
        VesselPerformanceModel.Build(single)!.RpmAt(8000 / 8.0)!.Value.Should().BeApproximately(74 / 2.0, 1e-6);
    }

    [Fact]
    public void NonMonotonePointsAreSmoothedAndFlatPointsRejected()
    {
        var noisy = Curve();
        noisy[1] = Point("CUSTOM", 12, 66, 9000, 33);
        var model = VesselPerformanceModel.Build(noisy)!;

        model.Notes.Should().Contain(note => note.Contains("averaged"));
        model.CalmX(12).Should().BeLessThan(model.CalmX(14) * (1 + 1e-9));

        var flat = Curve().Select(s => s with { PowerKw = 6000 }).ToList();
        VesselPerformanceModel.Build(flat).Should().BeNull();
        VesselPerformanceModel.Build(Curve().Take(1)).Should().BeNull();
    }

    [Fact]
    public void MissingPowerFallsBackToFuelAsProxyAndSaysSo()
    {
        var model = VesselPerformanceModel.Build(Curve().Select(s => s with { PowerKw = null }))!;

        model.Basis.Should().Be(PowerBasis.FuelProxy);
        model.Notes.Should().Contain(note => note.Contains("proxy"));
        model.FuelMtPerDay(33).Should().Be(33);
    }

    [Fact]
    public void ExtrapolationUsesABoundedExponentAndTheValidRangeIsLimited()
    {
        var model = VesselPerformanceModel.Build(Curve())!;

        model.CalmX(15).Should().BeInRange(8000 * Math.Pow(15 / 14.0, 2), 8000 * Math.Pow(15 / 14.0, 5));
        model.ValidMinSpeedKn.Should().Be(5);
        model.ValidMaxSpeedKn.Should().BeApproximately(16.1, 1e-9);
    }

    [Fact]
    public void EquilibriumReproducesTheTabulatedSpeedLossAtTheReferenceSpeed()
    {
        var model = VesselPerformanceModel.Build(Curve())!;
        var force = WeatherResistanceCalculator.ForceFromSpeedLoss(model, -2);
        var forces = new WeatherForces(force, 0, 0, 0, model.ReferenceSpeedKn);

        WeatherResistanceCalculator.SolveSpeed(model, 1, forces, 8000)!.Value.Should().BeApproximately(12, 1e-4);
    }

    [Fact]
    public void ExtraPowerToHoldTheTargetSpeedMatchesTheResistanceIncrease()
    {
        var model = VesselPerformanceModel.Build(Curve())!;
        var forces = new WeatherForces(WeatherResistanceCalculator.ForceFromSpeedLoss(model, -2), 0, 0, 0, model.ReferenceSpeedKn);

        var needed = model.CalmX(14) + forces.ExtraX(14);

        WeatherResistanceCalculator.SolveSpeed(model, 1, forces, needed)!.Value.Should().BeApproximately(14, 1e-4);
        needed.Should().BeGreaterThan(8000);
    }

    [Fact]
    public void WindForceGrowsWithRelativeWindSpeed()
    {
        var forces = new WeatherForces(0, 100, 30, 0, 14);

        forces.WindForce(14).Should().BeApproximately(100, 1e-9);
        forces.WindForce(10).Should().BeLessThan(100); // slower ship in a head wind sees a lower relative speed
        new WeatherForces(0, 100, 30, 180, 14).WindForce(10).Should().BeGreaterThan(100); // in a following wind a slower ship sees a higher relative speed
    }

    [Fact]
    public void SpeedOutsideTheCalibratedRangeIsAConstraintFailureNotAnInventedSpeed()
    {
        var model = VesselPerformanceModel.Build(Curve())!;

        WeatherResistanceCalculator.SolveSpeed(model, 1, WeatherForces.None, 1)
            .Should().BeNull();
        WeatherResistanceCalculator.SolveSpeed(model, 1, WeatherForces.None, 80000).Should().BeNull();
    }
}

public class CalibratedArcPhysicsTests
{
    private const double Bearing = 90;

    private static SpeedSetting Full() => new("FULL", true, 14, 74, 8000, null, 50, 50, 3.151, 3.206, "VLSFO", "LSMGO");

    private static List<SpeedSetting> Curve() => new()
    {
        new("ECO", false, 10, 60, 3000, null, 20, 20, 3.151, 3.206, "VLSFO", "LSMGO"),
        new("CUSTOM", false, 12, 66, 5200, null, 33, 33, 3.151, 3.206, "VLSFO", "LSMGO"),
        Full(),
    };

    private static ArcPhysicsContext Context(Action<Vessel>? vessel = null, RejectionTally? tally = null)
    {
        var ship = new Vessel { Deadweight = "50000", TotalKwMainEng = "10000", MaxMcr = "90" };
        vessel?.Invoke(ship);
        var limits = VesselEngineLimits.From(ship, "Laden", new OptimizationRequestDto(), 50000);
        var waves = new[]
        {
            new WeatherFactorEntry { Weather = "Wave", DeadWeight = 50000, Magnitude = 2, Angle = 0, Value = -2 },
            new WeatherFactorEntry { Weather = "Wave", DeadWeight = 50000, Magnitude = 4, Angle = 0, Value = -2 },
            new WeatherFactorEntry { Weather = "Wave", DeadWeight = 50000, Magnitude = 2, Angle = 180, Value = 0 },
            new WeatherFactorEntry { Weather = "Wave", DeadWeight = 50000, Magnitude = 4, Angle = 180, Value = 0 },
        };
        return new ArcPhysicsContext(limits, new WeatherFactorLookup(Array.Empty<WeatherFactorEntry>(), "Wind"), new WeatherFactorLookup(waves, "Wave"), 600, 900)
        {
            Model = VesselPerformanceModel.Build(Curve()),
            Tally = tally,
        };
    }

    private static ArcInput Arc(double waveM = 0, double waveFrom = Bearing, double currentKn = 0, double currentTo = Bearing) =>
        new(100, Bearing, 0, 0, waveM, waveFrom, null, currentKn, currentTo, false, 99, null, null, false);

    [Fact]
    public void CalmWaterReproducesTheOperatingPoint()
    {
        var outcome = ArcPhysics.Evaluate(Arc(), Full(), PowerMode.HoldRpm, Context())!;

        outcome.StwKn.Should().BeApproximately(14, 1e-4);
        outcome.SogKn.Should().BeApproximately(14, 1e-4);
        outcome.FuelRateMtPerDay.Should().BeApproximately(50, 1e-6);
        outcome.Rpm.Should().BeApproximately(74, 1e-6);
        outcome.PowerKw.Should().BeApproximately(8000, 1e-6);
    }

    [Fact]
    public void HeadSeaLosesSpeedAtFixedPowerAndBoostRecoversPartOfIt()
    {
        var context = Context();
        var hold = ArcPhysics.Evaluate(Arc(waveM: 3), Full(), PowerMode.HoldRpm, context)!;
        var boost = ArcPhysics.Evaluate(Arc(waveM: 3), Full(), PowerMode.Boost, context)!;

        hold.StwKn.Should().BeApproximately(12, 1e-3);
        boost.StwKn.Should().BeGreaterThan(hold.StwKn + 0.3).And.BeLessThan(14);
        boost.PowerKw.Should().BeApproximately(9000, 1e-6); // limited by 90% of 10,000 kW
        boost.Rpm.Should().BeGreaterThan(74);
        boost.FuelRateMtPerDay.Should().BeGreaterThan(hold.FuelRateMtPerDay);
    }

    [Fact]
    public void BoostIsBackedOffToTheRpmLimit()
    {
        var boost = ArcPhysics.Evaluate(Arc(waveM: 3), Full(), PowerMode.Boost, Context(v => v.MaxRpm = "76"))!;

        boost.Rpm.Should().BeLessThanOrEqualTo(76 + 1e-6);
        boost.PowerKw.Should().BeLessThan(9000).And.BeGreaterThan(8000);
    }

    [Fact]
    public void LoweringTheSettingInHeavyWeatherCutsFuelAndSpeed()
    {
        var context = Context();
        var full = ArcPhysics.Evaluate(Arc(waveM: 3), Full(), PowerMode.HoldRpm, context)!;
        var eco = ArcPhysics.Evaluate(Arc(waveM: 3), Curve()[0], PowerMode.HoldRpm, context)!;

        eco.StwKn.Should().BeLessThan(full.StwKn);
        eco.FuelRateMtPerDay.Should().BeLessThan(full.FuelRateMtPerDay);
        eco.StwKn.Should().BeLessThan(10);
    }

    [Fact]
    public void FollowingCurrentRaisesSogAndCrossCurrentCreatesACrabAngle()
    {
        var context = Context();
        var following = ArcPhysics.Evaluate(Arc(currentKn: 2, currentTo: Bearing), Full(), PowerMode.HoldRpm, context)!;
        var crossing = ArcPhysics.Evaluate(Arc(currentKn: 3, currentTo: Bearing + 90), Full(), PowerMode.HoldRpm, context)!;

        following.SogKn.Should().BeApproximately(16, 1e-3);
        var crab = Math.Asin(3.0 / 14);
        crossing.SogKn.Should().BeApproximately(14 * Math.Cos(crab), 1e-3);
    }

    [Fact]
    public void ACrossCurrentThatCannotBeCounteredIsRejectedAndTallied()
    {
        var tally = new RejectionTally();

        var outcome = ArcPhysics.Evaluate(Arc(currentKn: 14, currentTo: Bearing + 90), Full(), PowerMode.HoldRpm, Context(tally: tally));

        outcome.Should().BeNull();
        tally.Summary().Should().Contain("cross current");
    }

    [Theory]
    [InlineData("CriticalRpmMin", "70", "CriticalRpmMax", "78", "critical RPM")]
    [InlineData("BlowerLadenMin", "70", "BlowerLadenMax", "80", "blower")]
    public void RestrictedRpmRangesRejectTheSettingWithAReason(string minName, string min, string maxName, string max, string reason)
    {
        var tally = new RejectionTally();
        var context = Context(v =>
        {
            typeof(Vessel).GetProperty(minName)!.SetValue(v, min);
            typeof(Vessel).GetProperty(maxName)!.SetValue(v, max);
        }, tally);

        ArcPhysics.Evaluate(Arc(), Full(), PowerMode.HoldRpm, context).Should().BeNull();
        tally.Summary().Should().Contain(reason);
    }

    [Fact]
    public void TorqueAndPowerLimitsFromTheLoadDiagramApplyToTheRecommendedPoint()
    {
        var tally = new RejectionTally();
        // 8000 kW at 74 RPM is 1032 kNm.
        var limited = Context(v => v.LoadDiagramJson = "[{\"rpm\":60,\"maxTorqueKnm\":1000},{\"rpm\":78,\"maxTorqueKnm\":1000}]", tally);
        var allowed = Context(v => v.LoadDiagramJson = "[{\"rpm\":60,\"maxTorqueKnm\":1100},{\"rpm\":78,\"maxTorqueKnm\":1100}]");

        ArcPhysics.Evaluate(Arc(), Full(), PowerMode.HoldRpm, limited).Should().BeNull();
        tally.Summary().Should().Contain("load diagram");
        ArcPhysics.Evaluate(Arc(), Full(), PowerMode.HoldRpm, allowed).Should().NotBeNull();
    }

    [Fact]
    public void CustomProfileScaledByDisplacementKeepsTheCurveShape()
    {
        var scaled = Full() with { PowerKw = 8800, FuelRateMtPerDay = 55, EcaFuelRateMtPerDay = 55 };

        var outcome = ArcPhysics.Evaluate(Arc(), scaled, PowerMode.HoldRpm, Context())!;

        outcome.StwKn.Should().BeApproximately(14, 1e-4);
        outcome.FuelRateMtPerDay.Should().BeApproximately(55, 1e-6);
    }
}
