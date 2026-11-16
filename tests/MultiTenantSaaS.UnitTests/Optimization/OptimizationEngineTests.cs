using FluentAssertions;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class OptimizationEngineTests
{
    private static OptimizationOperatingProfileDto Profile(string mode, double speed, double cons, bool cpDefault = false, double? rpm = 80, double? mcr = 60) => new()
    {
        Mode = mode,
        IsCpDefault = cpDefault,
        LoadCondition = "Laden",
        SpeedKn = speed,
        Rpm = rpm,
        McrPercent = mcr,
        FuelType1 = "VLSFO",
        DailyCons1Mt = cons,
        FuelType2 = "LSMGO",
        DailyCons2Mt = 0.5,
    };

    [Fact]
    public void ProfilesKeepFullEcoAndCustomIndependent()
    {
        var settings = VesselSpeedProfile.BuildCandidateSpeeds(new Vessel(), "Laden", new[]
        {
            Profile("ECO", 11, 24),
            Profile("FULL", 14, 38),
            Profile("CUSTOM", 12.5, 29),
        });

        settings.Select(s => (s.Mode, s.SpeedKn, s.FuelRateMtPerDay))
            .Should().BeEquivalentTo(new[] { ("ECO", 11d, 24d), ("CUSTOM", 12.5, 29d), ("FULL", 14d, 38d) });
    }

    [Fact]
    public void MissingProfilesProduceNoDefaultCurve()
    {
        var act = () => VesselSpeedProfile.BuildCandidateSpeeds(new Vessel(), "Laden", Array.Empty<OptimizationOperatingProfileDto>());
        act.Should().Throw<InvalidOperationException>().WithMessage("*No complete*");
    }

    [Fact]
    public void CriticalRpmRangeIsNeverOffered()
    {
        var vessel = new Vessel { CriticalRpmMin = "70", CriticalRpmMax = "75" };
        var settings = VesselSpeedProfile.BuildCandidateSpeeds(vessel, "Laden", new[]
        {
            Profile("ECO", 11, 24, rpm: 72),
            Profile("FULL", 14, 38, rpm: 80),
        });

        settings.Should().ContainSingle(s => s.Mode == "FULL");
    }

    [Fact]
    public void UnsupportedThirdFuelIsRejectedInsteadOfUnderpriced()
    {
        var profile = Profile("ECO", 11, 24);
        profile.FuelType3 = "Biogas";
        profile.DailyCons3Mt = 0.1;

        var act = () => VesselSpeedProfile.BuildCandidateSpeeds(new Vessel(), "Laden", new[] { profile });
        act.Should().Throw<InvalidOperationException>().WithMessage("*additional fuel grade*");
    }

    [Fact]
    public void LandMaskRejectsOverlandSegmentsAndAllowsOpenWater()
    {
        // Across central Africa vs. mid South Atlantic.
        LandMask.SegmentCrossesLand(0, 15, 0, 25).Should().BeTrue();
        LandMask.SegmentCrossesLand(-20, -20, -20, -10).Should().BeFalse();
    }
}
