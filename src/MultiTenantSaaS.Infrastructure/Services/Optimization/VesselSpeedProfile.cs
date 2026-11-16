using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>One operating point the engine may choose for an arc; Source is CP (configured paper values) or ACTUAL (observed noon reports).</summary>
internal record SpeedSetting(
    string Mode,
    bool IsCpDefault,
    double SpeedKn,
    double Rpm,
    double? PowerKw,
    double? McrPercent,
    double FuelRateMtPerDay,
    double EcaFuelRateMtPerDay,
    double Co2Factor,
    double EcaCo2Factor,
    string FuelType1,
    string FuelType2,
    double AuxGoRateMtPerDay = 0,
    string Source = "CP",
    int SampleCount = 0);

/// <summary>Configured (CP / paper) operating points from the voyage's Speed &amp; Cons rows, filtered by the vessel's declared speed, RPM and MCR limits.</summary>
internal static class VesselSpeedProfile
{
    public static List<SpeedSetting> BuildCandidateSpeeds(
        Vessel vessel,
        string loadCondition,
        IReadOnlyCollection<MultiTenantSaaS.Application.DTOs.Optimization.OptimizationOperatingProfileDto> profiles)
    {
        var matches = profiles
            .Where(profile => string.Equals(profile.LoadCondition, loadCondition, StringComparison.OrdinalIgnoreCase))
            .Where(profile => profile.SpeedKn > 0 && profile.DailyCons1Mt > 0 && !string.IsNullOrWhiteSpace(profile.FuelType1))
            .Where(profile => profile.DailyCons2Mt <= 0 || !string.IsNullOrWhiteSpace(profile.FuelType2))
            .Where(profile => profile.DailyCons3Mt <= 0)
            .Where(profile => IsFoGrade(profile.FuelType1))
            .Where(profile => profile.DailyCons2Mt <= 0 || IsGoGrade(profile.FuelType2))
            .ToList();

        if (profiles.Any(profile => string.Equals(profile.LoadCondition, loadCondition, StringComparison.OrdinalIgnoreCase) && profile.DailyCons3Mt > 0))
            throw new InvalidOperationException("An additional fuel grade is configured, but its price and emissions factor are not available to the optimization engine. No cost or emissions result was produced.");

        if (matches.Count == 0)
            throw new InvalidOperationException($"No complete, configured speed/consumption profiles are available for the {loadCondition} condition. Enter ECO, FULL, or CUSTOM speed and fuel consumption before optimization.");

        var minSpeed = double.TryParse(vessel.MinSpeed, out var parsedMin) ? parsedMin : double.NaN;
        var maxSpeed = double.TryParse(vessel.MaxSpeed, out var parsedMax) ? parsedMax : double.NaN;
        var criticalMin = double.TryParse(vessel.CriticalRpmMin, out var parsedCriticalMin) ? parsedCriticalMin : double.NaN;
        var criticalMax = double.TryParse(vessel.CriticalRpmMax, out var parsedCriticalMax) ? parsedCriticalMax : double.NaN;
        var hasBarredRange = double.IsFinite(criticalMin) && double.IsFinite(criticalMax) && criticalMax > criticalMin;
        var minMcr = double.TryParse(vessel.MinMcr, out var parsedMinMcr) ? parsedMinMcr : double.NaN;
        var maxMcr = double.TryParse(vessel.MaxMcr, out var parsedMaxMcr) ? parsedMaxMcr : double.NaN;

        var settings = matches
            .Where(profile => !double.IsFinite(minSpeed) || profile.SpeedKn >= minSpeed)
            .Where(profile => !double.IsFinite(maxSpeed) || profile.SpeedKn <= maxSpeed)
            .Where(profile => !hasBarredRange || !profile.Rpm.HasValue || !(profile.Rpm.Value > criticalMin && profile.Rpm.Value < criticalMax))
            .Where(profile => !double.IsFinite(minMcr) || !profile.McrPercent.HasValue || profile.McrPercent.Value >= minMcr)
            .Where(profile => !double.IsFinite(maxMcr) || !profile.McrPercent.HasValue || profile.McrPercent.Value <= maxMcr)
            .Select(profile => new SpeedSetting(
                profile.Mode,
                profile.IsCpDefault,
                profile.SpeedKn,
                profile.Rpm ?? 0,
                profile.PowerKw,
                profile.McrPercent,
                profile.DailyCons1Mt,
                profile.DailyCons2Mt,
                Co2Factor(profile.FuelType1),
                profile.DailyCons2Mt > 0 ? Co2Factor(profile.FuelType2) : 0,
                profile.FuelType1,
                profile.FuelType2))
            .OrderBy(setting => setting.SpeedKn)
            .ToList();

        if (settings.Count == 0)
            throw new InvalidOperationException("All configured speed/consumption profiles violate the vessel's declared speed or critical-RPM limits.");

        return settings;
    }

    private static bool IsFoGrade(string grade) => grade.Trim().ToUpperInvariant() is "VLSFO" or "HSFO" or "HFO";

    private static bool IsGoGrade(string grade) => grade.Trim().ToUpperInvariant() is "LSMGO" or "MGO" or "MDO";

    internal static double Co2Factor(string grade) => grade.Trim().ToUpperInvariant() switch
    {
        "VLSFO" => 3.151,
        "HSFO" or "HFO" => 3.114,
        "LSMGO" or "MGO" or "MDO" => 3.206,
        _ => throw new InvalidOperationException($"No approved CO2 conversion factor is configured for fuel grade '{grade}'."),
    };
}
