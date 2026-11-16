using System.Globalization;
using System.Text.RegularExpressions;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Entities.Performance;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

internal sealed record RealPerformanceResult(List<SpeedSetting> Settings, int ReportsUsed, List<string> Notes);

/// <summary>
/// Operating points from the vessel's own noon reports (tracksheet), independent of CP compliance:
/// speed is corrected back to calm water with the logged wind/wave/current effects so the engine can
/// re-apply forecast weather without double counting. Only observed RPM groups are offered; nothing is extrapolated.
/// </summary>
internal static class RealPerformanceProfile
{
    public const int MinSamplesPerPoint = 3;
    private const int MaxReports = 120;
    private const double MaxWeatherCorrectionKn = 1.0;
    private const double MaxCurrentCorrectionKn = 2.0;

    private static readonly Regex DatePattern = new(@"(\d{1,2})\s*([A-Za-z]{3,})\.?\s*(\d{4})", RegexOptions.Compiled);
    private static readonly string[] Months = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };

    internal sealed record Sample(DateTime At, double Rpm, double CalmSpeedKn, double? PowerKw, double MainCons, double AuxCons);

    public static RealPerformanceResult Build(IEnumerable<TracksheetRow> rows, string loadCondition, Vessel vessel)
    {
        var laden = string.Equals(loadCondition, "Laden", StringComparison.OrdinalIgnoreCase);
        var samples = ExtractSamples(rows, laden);

        var notes = new List<string>();
        samples = samples.OrderByDescending(sample => sample.At).Take(MaxReports).ToList();

        var minSpeed = ParseNumber(vessel.MinSpeed);
        var maxSpeed = ParseNumber(vessel.MaxSpeed);
        var criticalMin = ParseNumber(vessel.CriticalRpmMin);
        var criticalMax = ParseNumber(vessel.CriticalRpmMax);
        var minMcr = ParseNumber(vessel.MinMcr);
        var maxMcr = ParseNumber(vessel.MaxMcr);
        var totalKw = ParseNumber(vessel.TotalKwMainEng);
        var mcrUnverifiable = false;

        var settings = new List<SpeedSetting>();
        var unusedGroups = 0;
        foreach (var group in samples.GroupBy(sample => Math.Round(sample.Rpm)))
        {
            var items = group.ToList();
            if (items.Count < MinSamplesPerPoint)
            {
                unusedGroups++;
                continue;
            }

            var rpm = Median(items.Select(item => item.Rpm));
            var speed = Median(items.Select(item => item.CalmSpeedKn));
            var powers = items.Where(item => item.PowerKw.HasValue).Select(item => item.PowerKw!.Value).ToList();
            double? power = powers.Count > 0 ? Median(powers) : null;
            double? mcr = power.HasValue && totalKw is > 0 ? power.Value / totalKw.Value * 100 : null;

            if (minSpeed.HasValue && speed < minSpeed.Value) continue;
            if (maxSpeed.HasValue && speed > maxSpeed.Value) continue;
            if (criticalMin.HasValue && criticalMax.HasValue && criticalMax > criticalMin && rpm > criticalMin && rpm < criticalMax) continue;
            if (mcr.HasValue && (minMcr.HasValue && mcr.Value < minMcr.Value || maxMcr.HasValue && mcr.Value > maxMcr.Value)) continue;
            if (!mcr.HasValue && (minMcr.HasValue || maxMcr.HasValue)) mcrUnverifiable = true;

            var mainCons = Median(items.Select(item => item.MainCons));
            settings.Add(new SpeedSetting(
                "ACTUAL", false, speed, rpm, power, mcr,
                mainCons, mainCons,
                VesselSpeedProfile.Co2Factor("VLSFO"), VesselSpeedProfile.Co2Factor("LSMGO"),
                "VLSFO", "LSMGO",
                Median(items.Select(item => item.AuxCons)), "ACTUAL", items.Count));
        }

        settings = settings.OrderBy(setting => setting.SpeedKn).ToList();
        var used = settings.Sum(setting => setting.SampleCount);
        if (settings.Count == 0)
        {
            notes.Add($"Only {samples.Count} usable {loadCondition.ToLowerInvariant()} noon reports are available; at least {MinSamplesPerPoint} reports at the same RPM are needed for an operating point.");
        }
        else
        {
            notes.Add($"Performance basis: {used} recent weather-corrected {loadCondition.ToLowerInvariant()} noon reports grouped into {settings.Count} observed RPM operating points (no extrapolation).");
            notes.Add("ECA: main-engine fuel is assumed to switch to LSMGO at its observed mass rate; auxiliary LSMGO use is added in every zone.");
            if (unusedGroups > 0) notes.Add($"{unusedGroups} RPM groups with fewer than {MinSamplesPerPoint} reports were not used.");
            if (mcrUnverifiable) notes.Add("The vessel's MCR limits could not be verified because engine power or total main-engine kW is missing.");
        }

        return new RealPerformanceResult(settings, used, notes);
    }

    internal static List<Sample> ExtractSamples(IEnumerable<TracksheetRow> rows, bool laden)
    {
        var samples = new List<Sample>();
        foreach (var voyage in rows.GroupBy(row => row.VoyageId))
        {
            var ordered = voyage.OrderBy(row => row.SortOrder).ToList();
            for (var index = 1; index < ordered.Count; index++)
            {
                if (TryCreateSample(ordered[index - 1], ordered[index], laden, out var sample)) samples.Add(sample);
            }
        }
        return samples;
    }

    /// <summary>Monthly observed operating points (weather-corrected speed, power and main-engine fuel per RPM) for trend monitoring. Read-only: the calibration itself is never changed automatically.</summary>
    public static List<OperatingPointTrendDto> Trend(IEnumerable<TracksheetRow> rows, bool laden)
    {
        return ExtractSamples(rows, laden)
            .Where(sample => sample.At != DateTime.MinValue)
            .GroupBy(sample => (sample.At.Year, sample.At.Month, Rpm: Math.Round(sample.Rpm)))
            .OrderBy(group => group.Key.Year).ThenBy(group => group.Key.Month).ThenBy(group => group.Key.Rpm)
            .Select(group =>
            {
                var powers = group.Where(item => item.PowerKw.HasValue).Select(item => item.PowerKw!.Value).ToList();
                return new OperatingPointTrendDto
                {
                    Month = $"{group.Key.Year:0000}-{group.Key.Month:00}",
                    Rpm = group.Key.Rpm,
                    Samples = group.Count(),
                    MedianCalmSpeedKn = Median(group.Select(item => item.CalmSpeedKn)),
                    MedianPowerKw = powers.Count > 0 ? Median(powers) : null,
                    MedianMainFuelMtPerDay = Median(group.Select(item => item.MainCons)),
                };
            }).ToList();
    }

    private static bool TryCreateSample(TracksheetRow previous, TracksheetRow current, bool laden, out Sample sample)
    {
        sample = null!;
        if (!string.Equals(current.Rt?.Trim(), "N", StringComparison.OrdinalIgnoreCase)) return false;
        if (current.Hrs is not { } hours || hours < 18 || hours > 30) return false;
        if (current.AvgSpeedO is not { } observedSpeed || observedSpeed <= 0) return false;
        if (current.Rpm is not { } rpm || rpm <= 0) return false;
        if (current.Amount is not { } cargo || (cargo > 0) != laden) return false;
        if (previous.VlsfoRob is not { } previousVlsfo || current.VlsfoRob is not { } currentVlsfo) return false;
        if ((current.VlsfoBunkered ?? 0) > 0 || (current.LsmgoBunkered ?? 0) > 0 || (current.NoneBunkered ?? 0) > 0) return false;

        var mainCons = (previousVlsfo - currentVlsfo) / hours * 24;
        if (mainCons <= 0) return false;

        var auxCons = previous.LsmgoRob.HasValue && current.LsmgoRob.HasValue
            ? Math.Max(0, (previous.LsmgoRob.Value - current.LsmgoRob.Value) / hours * 24)
            : 0;
        if (auxCons > 0.15 * mainCons) return false; // main engine was on LSMGO (ECA / changeover)
        if (previous.NoneRob.HasValue && current.NoneRob.HasValue && (previous.NoneRob.Value - current.NoneRob.Value) / hours * 24 > 0.05) return false;

        var weatherEffect = current.WindF + current.WaveF;
        if (Math.Abs(weatherEffect) > MaxWeatherCorrectionKn || Math.Abs(current.CurrF) > MaxCurrentCorrectionKn) return false;

        var calmSpeed = observedSpeed - weatherEffect - current.CurrF;
        if (calmSpeed < 3 || calmSpeed > 25) return false;

        sample = new Sample(ParseTimestamp(current.Date, current.Time), rpm, calmSpeed, current.EnginePower > 0 ? current.EnginePower : null, mainCons, auxCons);
        return true;
    }

    public static DateTime ParseTimestamp(string? date, string? time)
    {
        var match = date == null ? Match.Empty : DatePattern.Match(date);
        if (!match.Success) return DateTime.MinValue;
        var month = Array.IndexOf(Months, match.Groups[2].Value[..3].ToLowerInvariant()) + 1;
        if (month == 0) return DateTime.MinValue;
        var digits = new string((time ?? string.Empty).Where(char.IsDigit).ToArray()).PadLeft(4, '0');
        var hour = int.Parse(digits[..2], CultureInfo.InvariantCulture);
        var minute = int.Parse(digits[2..4], CultureInfo.InvariantCulture);
        try
        {
            return new DateTime(int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture), month,
                int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), Math.Min(hour, 23), Math.Min(minute, 59), 0, DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(value => value).ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    private static double? ParseNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var numeric = new string(value.Where(character => char.IsDigit(character) || character is '.' or '-').ToArray());
        return double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }
}
