using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.Application.Common.Interfaces;

public interface IEtsPolicyService
{
    Task<EtsPolicyDto> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates and stores the policy; returns the validation problem instead when it is invalid.</summary>
    Task<(EtsPolicyDto? Policy, string? Problem)> SaveAsync(EtsPolicyDto policy, CancellationToken cancellationToken = default);
}

/// <summary>Fits correction factors from verified noon reports. Fits are proposals; only an operator-approved one is applied by the optimizer.</summary>
public interface IPerformanceCalibrationService
{
    Task<(PerformanceCalibrationDto? Calibration, string? Problem)> FitAsync(CalibrationFitRequestDto request, CancellationToken cancellationToken = default);

    Task<List<PerformanceCalibrationDto>> ListAsync(string vesselImo, string? loadCondition, CancellationToken cancellationToken = default);

    Task<(PerformanceCalibrationDto? Calibration, string? Problem)> ReviewAsync(Guid id, bool approve, CancellationToken cancellationToken = default);
}

/// <summary>Perturbed wind from an ensemble forecast. Only wind is available, so any uncertainty derived from it is partial.</summary>
public interface IEnsembleWindService
{
    string SourceName { get; }

    Task<EnsembleWind?> GetAsync(IReadOnlyList<(double Lat, double Lng)> points, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}

/// <summary>Hourly wind per member and point: SpeedKn[member][point][hour], FromDeg likewise, starting at <see cref="StartUtc"/>.</summary>
public sealed record EnsembleWind(DateTime StartUtc, int Hours, double[][][] SpeedKn, double[][][] FromDeg);
