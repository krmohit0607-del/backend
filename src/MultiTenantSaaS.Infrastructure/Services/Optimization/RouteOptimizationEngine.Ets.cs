using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

public partial class RouteOptimizationEngine
{
    private const double PortMatchNm = 60;

    /// <summary>EU ETS charging rule for this run: the tenant's policy applied to the voyage type implied by the nearest ports at both ends.</summary>
    private async Task<EtsContext> ResolveEtsAsync(OptimizationRequestDto request, CancellationToken ct)
    {
        var policy = await _db.EtsPolicies.AsNoTracking().FirstOrDefaultAsync(ct);
        if (policy == null) return EtsContext.Unconfigured();

        var origin = await NearestPortCountryAsync(request.OriginLat, request.OriginLng, ct);
        var destination = await NearestPortCountryAsync(request.DestLat, request.DestLng, ct);
        var phaseIn = EtsPolicyMapper.PhaseIn(policy.PhaseInJson).Select(item => (item.Year, item.Pct)).ToList();

        string classification;
        double coverage;
        if (origin == null || destination == null)
        {
            classification = "Unknown";
            coverage = policy.UnknownCoveragePct;
        }
        else
        {
            var inEu = new[] { EtsCountries.IsEuEea(origin), EtsCountries.IsEuEea(destination) };
            (classification, coverage) = inEu.All(flag => flag) ? ("Intra-EU", policy.IntraEuCoveragePct)
                : inEu.Any(flag => flag) ? ("Extra-EU", policy.ExtraEuCoveragePct)
                : ("Outside EU", 0.0);
        }

        var ends = origin == null || destination == null
            ? "the voyage ends could not be matched to a port within 60 NM, so the unknown-voyage coverage applies"
            : $"{origin} to {destination}";
        return new EtsContext(classification, coverage, phaseIn, $"EU ETS policy: {classification} voyage ({ends}), {coverage:0.#}% covered, phase-in {string.Join(", ", phaseIn.Select(item => $"{item.Year} {item.Pct:0}%"))}.");
    }

    /// <summary>The latest operator-approved calibration scales the configured (CP) fuel and power; the entered values themselves are never changed.</summary>
    private async Task<(List<SpeedSetting> Speeds, string? Note)> ApplyApprovedCalibrationAsync(OptimizationRequestDto request, List<SpeedSetting> configured, CancellationToken ct)
    {
        if (configured.Count == 0) return (configured, null);
        var approved = await _db.PerformanceCalibrations.AsNoTracking()
            .Where(c => c.VesselImo == request.VesselImo && c.LoadCondition == request.LoadCondition && c.Status == "Approved")
            .OrderByDescending(c => c.FittedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (approved == null || Math.Abs(approved.FuelFactor - 1) < 1e-6) return (configured, null);

        var factor = approved.FuelFactor;
        var scaled = configured.Select(setting => setting with
        {
            FuelRateMtPerDay = setting.FuelRateMtPerDay * factor,
            EcaFuelRateMtPerDay = setting.EcaFuelRateMtPerDay * factor,
            PowerKw = setting.PowerKw * factor,
        }).ToList();
        return (scaled, $"Approved calibration of {approved.FittedAtUtc:yyyy-MM-dd} scales configured fuel and power by {factor:0.000} ({approved.UsedSampleCount} noon reports, {approved.Quality.ToLowerInvariant()} data quality).");
    }

    private async Task<string?> NearestPortCountryAsync(double lat, double lng, CancellationToken ct)
    {
        var margin = PortMatchNm / 60 + 0.5;
        var candidates = await _db.Ports.AsNoTracking()
            .Where(port => port.Latitude != null && port.Longitude != null && port.Latitude >= (decimal)(lat - margin) && port.Latitude <= (decimal)(lat + margin))
            .Select(port => new { port.Country, port.Latitude, port.Longitude })
            .ToListAsync(ct);
        var best = candidates
            .Select(port => (port.Country, Distance: OptimizationGeoMath.DistanceNm(lat, lng, (double)port.Latitude!.Value, (double)port.Longitude!.Value)))
            .Where(item => item.Distance <= PortMatchNm)
            .OrderBy(item => item.Distance)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(best.Country) ? null : best.Country;
    }
}
