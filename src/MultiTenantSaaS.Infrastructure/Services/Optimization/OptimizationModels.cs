using System.Globalization;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

internal static class NumberParser
{
    public static double? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var numeric = new string(value.Where(character => char.IsDigit(character) || character is '.' or '-').ToArray());
        return double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    public static double? Positive(string? value) => Parse(value) is > 0 and var parsed ? parsed : null;
}

internal sealed class ForecastUnavailableException : Exception
{
    public ForecastUnavailableException(DateTime utc) : base($"Required forecast data is unavailable near {utc:yyyy-MM-dd HH:mm} UTC; no calm-weather substitution was used.") { }
}

internal enum PowerMode { HoldRpm, Boost }

/// <summary>Engine, hull and weather-limit data used by the arc model; every value is optional and only enforced when the vessel record has it.</summary>
internal sealed record VesselEngineLimits(
    double DeadweightMt,
    double? InstalledKw,
    double? MaxLoadFraction,
    double? MinLoadFraction,
    double? MaxRpm,
    double? MinRpm,
    double? CriticalRpmMin,
    double? CriticalRpmMax,
    double? BlowerMin,
    double? BlowerMax,
    double? LengthM,
    double? BreadthM,
    double? DraftM,
    double? ReferenceDisplacementMt,
    double? MaxSwhM,
    double? MaxWindKn,
    double? RollPeriodSec,
    IReadOnlyList<LoadDiagramPoint> LoadDiagram)
{
    public double? MaxPowerKw => InstalledKw.HasValue && MaxLoadFraction.HasValue ? InstalledKw * MaxLoadFraction : null;

    public static VesselEngineLimits From(Vessel vessel, string loadCondition, OptimizationRequestDto request, double deadweightMt)
    {
        var laden = string.Equals(loadCondition, "Laden", StringComparison.OrdinalIgnoreCase);
        static double? Fraction(double? value) => value is null ? null : value > 1.5 ? value / 100 : value;

        var markers = LoadLimitMarkers.Parse(vessel.LoadDiagramJson);
        var fractions = new[] { NumberParser.Positive(vessel.MaxMcr) / 100, Fraction(NumberParser.Positive(vessel.MaxPowerFraction)) }
            .Concat(markers.MaxFractions.Select(fraction => (double?)fraction))
            .Where(value => value.HasValue).Select(value => value!.Value).ToList();
        var draft = request.CustomDraftM
            ?? NumberParser.Positive(laden ? vessel.DefaultLadenDraft ?? vessel.SummerDraft ?? vessel.Draught : vessel.DefaultBallastDraft);

        var length = NumberParser.Positive(vessel.LengthBp) ?? NumberParser.Positive(vessel.LengthOverall);
        var breadth = NumberParser.Positive(vessel.BreadthMoulded);
        var roll = request.RollPeriodSec ?? RollPeriod(request.CustomGmM, breadth, draft, length);

        return new VesselEngineLimits(
            deadweightMt,
            NumberParser.Positive(vessel.TotalKwMainEng),
            fractions.Count > 0 ? fractions.Min() : null,
            Fraction(NumberParser.Positive(vessel.MinPowerFraction)) ?? markers.MinFraction,
            new[] { NumberParser.Positive(vessel.MaxRpm), markers.MaxRpm }.Where(value => value.HasValue).Select(value => value!.Value).Cast<double?>().DefaultIfEmpty(null).Min(),
            markers.MinRpm,
            NumberParser.Positive(vessel.CriticalRpmMin),
            NumberParser.Positive(vessel.CriticalRpmMax),
            NumberParser.Positive(laden ? vessel.BlowerLadenMin : vessel.BlowerBallastMin),
            NumberParser.Positive(laden ? vessel.BlowerLadenMax : vessel.BlowerBallastMax),
            length,
            breadth,
            draft,
            laden ? NumberParser.Positive(vessel.Displacement) : null,
            NumberParser.Positive(laden ? vessel.WslMaxSwhLaden : vessel.WslMaxSwhBallast),
            NumberParser.Positive(laden ? vessel.WslMaxWindsLaden : vessel.WslMaxWindsBallast),
            roll,
            LoadDiagramPoint.Parse(vessel.LoadDiagramJson));
    }

    /// <summary>Main-engine torque (kNm) from power (kW) and RPM.</summary>
    public static double TorqueKnm(double powerKw, double rpm) => 9.5493 * powerKw / rpm;

    /// <summary>Violation text when the operating point is outside the load diagram; limits apply only between the first and last tabulated RPM (no extrapolation).</summary>
    public bool ViolatesLoadDiagram(double rpm, double? powerKw)
    {
        if (LoadDiagram.Count == 0 || !powerKw.HasValue || rpm <= 0) return false;
        var minRpm = LoadDiagram.Min(point => point.Rpm);
        var maxRpm = LoadDiagram.Max(point => point.Rpm);
        if (rpm < minRpm - 1e-9 || rpm > maxRpm + 1e-9) return false;

        var maxPower = LoadDiagramPoint.Interpolate(LoadDiagram, rpm, point => point.MaxPowerKw);
        var minPower = LoadDiagramPoint.Interpolate(LoadDiagram, rpm, point => point.MinPowerKw);
        var maxTorque = LoadDiagramPoint.Interpolate(LoadDiagram, rpm, point => point.MaxTorqueKnm);
        if (maxPower.HasValue && powerKw.Value > maxPower.Value + 1e-6) return true;
        if (minPower.HasValue && powerKw.Value < minPower.Value - 1e-6) return true;
        return maxTorque.HasValue && TorqueKnm(powerKw.Value, rpm) > maxTorque.Value + 1e-6;
    }

    /// <summary>IMO weather-criterion estimate of the natural roll period (s) from GM, breadth, draft and length.</summary>
    public static double? RollPeriod(double? gmM, double? breadthM, double? draftM, double? lengthM)
    {
        if (gmM is not > 0 || breadthM is not > 0 || draftM is not > 0 || lengthM is not > 0) return null;
        var c = 0.373 + 0.023 * (draftM.Value / breadthM.Value) - 0.043 * (lengthM.Value / 100);
        return 2 * c * breadthM.Value / Math.Sqrt(gmM.Value);
    }
}

internal sealed record ArcInput(
    double DistNm,
    double BearingDeg,
    double WindKn,
    double WindFromDeg,
    double WaveM,
    double WaveFromDeg,
    double? WavePeriodS,
    double CurrentKn,
    double CurrentToDeg,
    bool InEca,
    double ZoneSpeedCapKn,
    double? ZoneRpmMin,
    double? ZoneRpmMax,
    bool ZoneConstrained);

internal sealed record ArcOutcome(
    SpeedSetting Setting,
    PowerMode Mode,
    double SogKn,
    double StwKn,
    double Hours,
    double MainFuelMt,
    double AuxGoMt,
    double Co2Mt,
    double FuelCostUsd,
    double Rpm,
    double? PowerKw,
    double? LoadFraction,
    double FuelRateMtPerDay,
    bool ExceedsWeatherLimit,
    double SyncRollHours,
    double ParametricRollHours,
    double PitchHours,
    double BlowerHours,
    double ExposureScore,
    IReadOnlyList<SegmentStep>? Steps = null,
    double? ExceedHours = null,
    bool IsWait = false)
{
    public double ExceedHoursValue => ExceedHours ?? (ExceedsWeatherLimit ? Hours : 0);

    /// <summary>Hour-weighted aggregate of sub-steps that were each evaluated against the forecast at their own position and time.</summary>
    public static ArcOutcome Combine(SpeedSetting setting, PowerMode mode, IReadOnlyList<SegmentStep> steps, double distanceNm)
    {
        if (steps.Count == 1) return steps[0].Outcome with { Steps = steps };

        var hours = steps.Sum(step => step.Outcome.Hours);
        double Weighted(Func<ArcOutcome, double> value) => steps.Sum(step => value(step.Outcome) * step.Outcome.Hours) / hours;
        var hasPower = steps.All(step => step.Outcome.PowerKw.HasValue);
        var hasLoad = steps.All(step => step.Outcome.LoadFraction.HasValue);
        return new ArcOutcome(
            setting, mode, distanceNm / hours, Weighted(o => o.StwKn), hours,
            steps.Sum(step => step.Outcome.MainFuelMt), steps.Sum(step => step.Outcome.AuxGoMt), steps.Sum(step => step.Outcome.Co2Mt), steps.Sum(step => step.Outcome.FuelCostUsd),
            Weighted(o => o.Rpm), hasPower ? Weighted(o => o.PowerKw!.Value) : null, hasLoad ? steps.Max(step => step.Outcome.LoadFraction!.Value) : null,
            Weighted(o => o.FuelRateMtPerDay), steps.Any(step => step.Outcome.ExceedsWeatherLimit),
            steps.Sum(step => step.Outcome.SyncRollHours), steps.Sum(step => step.Outcome.ParametricRollHours), steps.Sum(step => step.Outcome.PitchHours),
            steps.Sum(step => step.Outcome.BlowerHours), steps.Sum(step => step.Outcome.ExposureScore),
            steps, steps.Sum(step => step.Outcome.ExceedHoursValue));
    }
}

/// <summary>One sub-step of a lattice arc: the forecast sampled at its midpoint and start time, and the equilibrium outcome there.</summary>
internal sealed record SegmentStep(ArcInput Input, double StartHours, double Lat, double Lng, double EndLat, double EndLng, ArcOutcome Outcome);

internal sealed record ArcOption(ArcInput Input, ArcOutcome Outcome, double CalmHours);
