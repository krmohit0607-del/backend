using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities.Performance;

/// <summary>
/// One row of the wind/wave weather-factor calibration table — a real
/// per-deadweight, per-magnitude (Beaufort force for wind, significant wave
/// height in metres for wave), per-relative-angle (0=head..180=following/
/// stern) calibration table, seeded from a reference FleetView data export.
/// This is global reference data (not tenant-scoped), mirroring the
/// legacy's own plain <c>WeatherFactors</c> table design — it's the single
/// source of truth the frontend fetches once and uses for every wind/wave
/// factor lookup during Tracksheet "Validate" (see `weatherFactorTable.ts`).
/// </summary>
public class WeatherFactorEntry : BaseEntity
{
    /// <summary>"Wind" or "Wave".</summary>
    public string Weather { get; set; } = string.Empty;

    /// <summary>Vessel deadweight band (MT).</summary>
    public double DeadWeight { get; set; }

    /// <summary>Beaufort force (Wind) or significant wave height in metres (Wave).</summary>
    public double Magnitude { get; set; }

    /// <summary>Relative angle (deg) between the weather's "from" direction and the vessel's course, folded 0 (head) to 180 (following/stern).</summary>
    public double Angle { get; set; }

    /// <summary>The calibrated factor at this (deadweight, magnitude, angle) point (negative = adverse).</summary>
    public double Value { get; set; }
}
