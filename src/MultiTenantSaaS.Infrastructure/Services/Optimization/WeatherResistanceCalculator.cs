namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>Added-resistance terms for one arc, expressed as X per knot of speed through water (X = kW or MT/day, see <see cref="VesselPerformanceModel"/>).</summary>
internal readonly record struct WeatherForces(double WaveForce, double WindReferenceForce, double WindKn, double WindAngleDeg, double ReferenceSpeedKn)
{
    public static readonly WeatherForces None = new(0, 0, 0, 0, 0);

    private double RelativeWindSquared(double speedKn)
    {
        var alpha = WindAngleDeg * Math.PI / 180;
        return speedKn * speedKn + WindKn * WindKn + 2 * speedKn * WindKn * Math.Cos(alpha);
    }

    /// <summary>Wind force scaled by relative wind speed squared (R_wind = 0.5 rho A C_X V_rel^2); wave force is held constant over the speed range.</summary>
    public double WindForce(double speedKn)
    {
        var reference = RelativeWindSquared(ReferenceSpeedKn);
        return WindReferenceForce * (reference < 1e-6 ? 1 : RelativeWindSquared(speedKn) / reference);
    }

    /// <summary>Extra X needed to sustain <paramref name="speedKn"/> through water against wind and waves.</summary>
    public double ExtraX(double speedKn) => (WaveForce + WindForce(speedKn)) * speedKn;
}

/// <summary>
/// Converts the vessel's calibrated wind and wave speed-loss tables (knots lost at constant power at the reference speed) into added-resistance forces,
/// so the power-resistance equilibrium can be solved for any engine setting. Efficiencies cancel because the force is carried in the same units as the calm curve:
/// F = (X_calm(V_ref) - X_calm(V_ref + dV)) / (V_ref + dV).
/// Assumptions: wave added resistance is independent of ship speed; wind resistance scales with relative wind speed squared; the wind and wave tables are
/// additive. Validation status: calibrated lookup, not validated against model tests. Not used for calm water.
/// </summary>
internal static class WeatherResistanceCalculator
{
    public static WeatherForces Compute(VesselPerformanceModel model, ArcPhysicsContext ctx, ArcInput arc, double headingDeg, double weatherScale)
    {
        var limits = ctx.Limits;
        var windAngle = ArcPhysics.Fold(headingDeg, arc.WindFromDeg);
        var windLoss = ctx.Wind.Effect(limits.DeadweightMt, ArcPhysics.Beaufort(arc.WindKn * weatherScale), windAngle);
        var waveLoss = ctx.Wave.Effect(limits.DeadweightMt, arc.WaveM * weatherScale, ArcPhysics.Fold(headingDeg, arc.WaveFromDeg));
        return new WeatherForces(
            ForceFromSpeedLoss(model, waveLoss),
            ForceFromSpeedLoss(model, windLoss),
            arc.WindKn * weatherScale,
            windAngle,
            model.ReferenceSpeedKn);
    }

    public static double ForceFromSpeedLoss(VesselPerformanceModel model, double speedChangeKn)
    {
        if (Math.Abs(speedChangeKn) < 1e-9) return 0;
        var reference = model.ReferenceSpeedKn;
        var degraded = Math.Max(0.3 * reference, reference + speedChangeKn);
        return (model.CalmX(reference) - model.CalmX(degraded)) / degraded;
    }

    /// <summary>Speed through water (kn) at which available X balances calm-water plus added resistance; null outside the model's valid speed range.</summary>
    public static double? SolveSpeed(VesselPerformanceModel model, double scale, WeatherForces forces, double availableX)
    {
        double Balance(double speed) => scale * model.CalmX(speed) + forces.ExtraX(speed) - availableX;

        var low = model.ValidMinSpeedKn;
        var high = model.ValidMaxSpeedKn;
        if (Balance(low) > 0 || Balance(high) < 0) return null;
        for (var iteration = 0; iteration < 60; iteration++)
        {
            var middle = 0.5 * (low + high);
            if (Balance(middle) > 0) high = middle; else low = middle;
        }
        return 0.5 * (low + high);
    }
}
