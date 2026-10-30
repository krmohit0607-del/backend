using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>One achievable (speed, implied RPM, reference fuel rate) operating point the engine may choose for an arc.</summary>
internal record SpeedSetting(double SpeedKn, double Rpm, double FuelRateMtPerDay);

/// <summary>
/// Builds the discrete list of usable speed/RPM operating points for a
/// vessel + load condition from its telegraph table (DeadSlow/Slow/Half/Full
/// Ahead RPM + speed), excluding anything inside the critical/barred RPM
/// range — the TNM OptiNav engine has no such concept natively, so (per the
/// confirmed design) barred-RPM avoidance is enforced here, one layer above
/// the search, simply by never offering those operating points to it.
/// Fuel rate uses the admiralty cube-law approximation from the vessel's
/// service speed/consumption (the same approximation the client-side
/// synthetic optimizer already used) since no granular per-RPM fuel table
/// is modelled yet on the backend.
/// </summary>
internal static class VesselSpeedProfile
{
    public static List<SpeedSetting> BuildCandidateSpeeds(Vessel vessel, string loadCondition, double stepKn = 0.5)
    {
        bool laden = string.Equals(loadCondition, "Laden", StringComparison.OrdinalIgnoreCase);

        double D(string? s, double fallback = 0) => double.TryParse(s, out var v) ? v : fallback;

        var deadSlowRpm = D(vessel.DeadSlowRpm);
        var slowRpm = D(vessel.SlowAheadRpm);
        var halfRpm = D(vessel.HalfAheadRpm);
        var fullRpm = D(vessel.FullAheadRpm);

        var deadSlowSpeed = D(laden ? vessel.DeadSlowSpeedLaden : vessel.DeadSlowSpeedBallast);
        var slowSpeed = D(laden ? vessel.SlowAheadSpeedLaden : vessel.SlowAheadSpeedBallast);
        var halfSpeed = D(laden ? vessel.HalfAheadSpeedLaden : vessel.HalfAheadSpeedBallast);
        var fullSpeed = D(laden ? vessel.FullAheadSpeedLaden : vessel.FullAheadSpeedBallast);

        var telegraph = new List<(double Speed, double Rpm)>
        {
            (deadSlowSpeed, deadSlowRpm), (slowSpeed, slowRpm), (halfSpeed, halfRpm), (fullSpeed, fullRpm)
        }.Where(p => p.Speed > 0 && p.Rpm > 0).OrderBy(p => p.Speed).ToList();

        var minSpeed = D(vessel.MinSpeed, telegraph.Count > 0 ? telegraph[0].Speed : 8);
        var maxSpeed = D(vessel.MaxSpeed, telegraph.Count > 0 ? telegraph[^1].Speed : 18);
        if (maxSpeed <= minSpeed) maxSpeed = minSpeed + 1;

        var refSpeed = D(vessel.ServiceSpeed, telegraph.Count > 0 ? telegraph[^1].Speed : maxSpeed);
        var refConsPerDay = D(vessel.FuelConsMainEng, 30); // MT/day at reference/service speed
        if (refSpeed <= 0) refSpeed = maxSpeed;
        if (refConsPerDay <= 0) refConsPerDay = 30;

        var criticalMin = D(vessel.CriticalRpmMin, double.NaN);
        var criticalMax = D(vessel.CriticalRpmMax, double.NaN);
        bool HasBarredRange = !double.IsNaN(criticalMin) && !double.IsNaN(criticalMax) && criticalMax > criticalMin;

        double ImpliedRpm(double speedKn)
        {
            if (telegraph.Count < 2) return double.NaN;
            for (int i = 0; i < telegraph.Count - 1; i++)
            {
                var (s0, r0) = telegraph[i];
                var (s1, r1) = telegraph[i + 1];
                if (speedKn >= s0 && speedKn <= s1 && s1 > s0)
                {
                    var t = (speedKn - s0) / (s1 - s0);
                    return r0 + t * (r1 - r0);
                }
            }
            // extrapolate from nearest segment
            var (ls0, lr0) = telegraph[0];
            var (ls1, lr1) = telegraph[^1];
            if (ls1 <= ls0) return lr1;
            var tt = (speedKn - ls0) / (ls1 - ls0);
            return lr0 + tt * (lr1 - lr0);
        }

        var settings = new List<SpeedSetting>();
        for (var speed = minSpeed; speed <= maxSpeed + 1e-9; speed += stepKn)
        {
            var rpm = ImpliedRpm(speed);
            if (HasBarredRange && !double.IsNaN(rpm) && rpm > criticalMin && rpm < criticalMax)
            {
                continue; // inside the critical/barred RPM range — never offer this operating point
            }
            var fuelRate = refConsPerDay * Math.Pow(speed / refSpeed, 3);
            settings.Add(new SpeedSetting(Math.Round(speed, 2), double.IsNaN(rpm) ? 0 : Math.Round(rpm, 1), fuelRate));
        }

        if (settings.Count == 0)
        {
            // Barred range swallowed the whole usable band (unlikely) — fall back to the reference speed alone.
            settings.Add(new SpeedSetting(refSpeed, ImpliedRpm(refSpeed) is var r && !double.IsNaN(r) ? r : 0, refConsPerDay));
        }

        return settings;
    }
}
