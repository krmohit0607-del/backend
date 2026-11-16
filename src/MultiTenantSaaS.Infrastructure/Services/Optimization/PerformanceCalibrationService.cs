using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities.Optimization;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>
/// Fits a vessel-specific correction factor: observed main-engine fuel divided by the CP curve's fuel at the observed weather-corrected calm speed.
/// Only reports that already passed the existing data-quality filters are used; outliers beyond 3 robust standard deviations are dropped; a monthly series
/// shows drift (for example hull fouling). A fit is a proposal until approved; approval only makes the optimizer scale configured fuel and power, nothing is overwritten.
/// </summary>
public sealed class PerformanceCalibrationService : IPerformanceCalibrationService
{
    private const int GoodSamples = 30;
    private const int LimitedSamples = 10;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IApplicationDbContext _db;
    private readonly IPerformanceDbContext _perf;
    private readonly ICurrentUserService _user;

    public PerformanceCalibrationService(IApplicationDbContext db, IPerformanceDbContext perf, ICurrentUserService user)
    {
        _db = db;
        _perf = perf;
        _user = user;
    }

    public async Task<(PerformanceCalibrationDto? Calibration, string? Problem)> FitAsync(CalibrationFitRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.VesselImo)) return (null, "A vessel IMO is required.");
        if (request.LoadCondition is not ("Ballast" or "Laden")) return (null, "Load condition must be Ballast or Laden.");
        var vessel = await _db.Vessels.AsNoTracking().FirstOrDefaultAsync(v => v.Imo == request.VesselImo, ct);
        if (vessel == null) return (null, $"Vessel with IMO '{request.VesselImo}' was not found.");

        VesselPerformanceModel? model;
        try
        {
            model = VesselPerformanceModel.Build(VesselSpeedProfile.BuildCandidateSpeeds(vessel, request.LoadCondition, request.OperatingProfiles));
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }
        if (model == null) return (null, "At least two distinct, increasing configured operating points are needed to fit a calibration.");

        var rows = await _perf.TracksheetRows.AsNoTracking().Where(row => row.VesselImo == request.VesselImo && !row.IsDefault).Take(20000).ToListAsync(ct);
        var samples = RealPerformanceProfile.ExtractSamples(rows, request.LoadCondition == "Laden")
            .Where(sample => sample.At != DateTime.MinValue)
            .ToList();

        var notes = new List<string>
        {
            "Uses weather-corrected noon reports only (calm-water speed after wind, wave and current correction); reports with large corrections, bunkering or changeovers are already excluded.",
            "The factor scales configured fuel and power together; it cannot separate hull resistance from engine efficiency.",
        };
        var inRange = samples.Where(sample => sample.CalmSpeedKn >= model.MinSpeedKn * 0.9 && sample.CalmSpeedKn <= model.MaxSpeedKn * 1.1).ToList();
        if (inRange.Count < samples.Count) notes.Add($"{samples.Count - inRange.Count} reports outside the configured speed range were not used (no extrapolation).");

        var points = inRange
            .Select(sample => (sample.At, Ratio: sample.MainCons / model.FuelMtPerDay(model.CalmX(sample.CalmSpeedKn))))
            .Where(item => double.IsFinite(item.Ratio) && item.Ratio is > 0.5 and < 2)
            .ToList();

        var median = points.Count > 0 ? Median(points.Select(item => item.Ratio)) : 1;
        var mad = points.Count > 0 ? Median(points.Select(item => Math.Abs(item.Ratio - median))) : 0;
        var robustSigma = 1.4826 * mad;
        var kept = robustSigma > 0 ? points.Where(item => Math.Abs(item.Ratio - median) <= 3 * robustSigma).ToList() : points;
        if (kept.Count < points.Count) notes.Add($"{points.Count - kept.Count} outlying reports were excluded (beyond 3 robust standard deviations).");

        var factor = kept.Count > 0 ? Median(kept.Select(item => item.Ratio)) : 1;
        var spread = kept.Count > 0 && factor > 0 ? 1.4826 * Median(kept.Select(item => Math.Abs(item.Ratio - factor))) / factor * 100 : 0;

        var monthly = kept
            .GroupBy(item => item.At.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .OrderBy(group => group.Key)
            .Select(group => new CalibrationMonthDto { Month = group.Key, Samples = group.Count(), Factor = Median(group.Select(item => item.Ratio)) })
            .ToList();
        double? trend = null;
        if (monthly.Count(month => month.Samples >= 3) >= 3)
        {
            var series = monthly.Where(month => month.Samples >= 3).Select((month, index) => (X: (double)index, Y: month.Factor)).ToList();
            var meanX = series.Average(item => item.X);
            var meanY = series.Average(item => item.Y);
            var denominator = series.Sum(item => (item.X - meanX) * (item.X - meanX));
            if (denominator > 0) trend = series.Sum(item => (item.X - meanX) * (item.Y - meanY)) / denominator / meanY * 100;
            notes.Add("The monthly trend is the fitted change in the factor per month, an indicator of hull or engine condition drift; the single factor is a median over the whole window.");
        }
        else
        {
            notes.Add("Fewer than three months with at least three reports each, so no drift trend is reported.");
        }

        var quality = kept.Count >= GoodSamples && spread <= 8 ? "Good" : kept.Count >= LimitedSamples ? "Limited" : "Insufficient";
        if (quality == "Insufficient") notes.Add($"Only {kept.Count} usable reports (at least {LimitedSamples} are needed); this fit cannot be approved.");
        if (quality == "Limited") notes.Add("Limited data or a wide spread; review before approving.");

        var previous = await _db.PerformanceCalibrations.Where(c => c.VesselImo == request.VesselImo && c.LoadCondition == request.LoadCondition && c.Status == "Proposed").ToListAsync(ct);
        foreach (var item in previous) item.Status = "Superseded";

        var calibration = new PerformanceCalibration
        {
            TenantId = _user.TenantId ?? vessel.TenantId,
            VesselImo = request.VesselImo,
            LoadCondition = request.LoadCondition,
            SampleCount = samples.Count,
            UsedSampleCount = kept.Count,
            WindowFromUtc = kept.Count > 0 ? kept.Min(item => item.At) : null,
            WindowToUtc = kept.Count > 0 ? kept.Max(item => item.At) : null,
            FuelFactor = factor,
            SpreadPct = spread,
            TrendPctPerMonth = trend,
            Quality = quality,
            NotesJson = JsonSerializer.Serialize(notes, Json),
            MonthlyJson = JsonSerializer.Serialize(monthly, Json),
        };
        _db.PerformanceCalibrations.Add(calibration);
        await _db.SaveChangesAsync(ct);
        return (ToDto(calibration), null);
    }

    public async Task<List<PerformanceCalibrationDto>> ListAsync(string vesselImo, string? loadCondition, CancellationToken ct = default)
    {
        var query = _db.PerformanceCalibrations.AsNoTracking().Where(c => c.VesselImo == vesselImo);
        if (!string.IsNullOrWhiteSpace(loadCondition)) query = query.Where(c => c.LoadCondition == loadCondition);
        var rows = await query.OrderByDescending(c => c.FittedAtUtc).Take(50).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<(PerformanceCalibrationDto? Calibration, string? Problem)> ReviewAsync(Guid id, bool approve, CancellationToken ct = default)
    {
        var calibration = await _db.PerformanceCalibrations.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (calibration == null) return (null, "Calibration not found.");
        if (calibration.Status != "Proposed") return (null, "Only a proposed calibration can be approved or rejected.");
        if (approve && calibration.Quality == "Insufficient") return (null, "This calibration has too little data to approve.");

        if (approve)
        {
            var current = await _db.PerformanceCalibrations
                .Where(c => c.VesselImo == calibration.VesselImo && c.LoadCondition == calibration.LoadCondition && c.Status == "Approved").ToListAsync(ct);
            foreach (var item in current) item.Status = "Superseded";
        }
        calibration.Status = approve ? "Approved" : "Rejected";
        calibration.ReviewedAtUtc = DateTime.UtcNow;
        calibration.ReviewedByUserId = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return (ToDto(calibration), null);
    }

    private static PerformanceCalibrationDto ToDto(PerformanceCalibration c) => new()
    {
        Id = c.Id, VesselImo = c.VesselImo, LoadCondition = c.LoadCondition, ModelVersion = c.ModelVersion, FittedAtUtc = c.FittedAtUtc,
        SampleCount = c.SampleCount, UsedSampleCount = c.UsedSampleCount, WindowFromUtc = c.WindowFromUtc, WindowToUtc = c.WindowToUtc,
        FuelFactor = c.FuelFactor, SpreadPct = c.SpreadPct, TrendPctPerMonth = c.TrendPctPerMonth, Quality = c.Quality, Status = c.Status,
        Notes = Read<List<string>>(c.NotesJson) ?? new(), Monthly = Read<List<CalibrationMonthDto>>(c.MonthlyJson) ?? new(), ReviewedAtUtc = c.ReviewedAtUtc,
    };

    private static T? Read<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json, Json); }
        catch (JsonException) { return default; }
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        if (sorted.Length == 0) return 0;
        return sorted.Length % 2 == 1 ? sorted[sorted.Length / 2] : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2;
    }
}
