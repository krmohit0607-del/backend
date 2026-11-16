namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

internal sealed record ArcPhysicsContext(
    VesselEngineLimits Limits,
    WeatherFactorLookup Wind,
    WeatherFactorLookup Wave,
    double FoPriceUsdPerMt,
    double GoPriceUsdPerMt)
{
    /// <summary>Calibrated speed-power curve; when null the constant-power approximation below is used.</summary>
    public VesselPerformanceModel? Model { get; init; }
    public RejectionTally? Tally { get; init; }
}

/// <summary>
/// Speed, power, fuel and sea-state model for one route arc. Wind/wave effects are signed knots from the calibration table (constant power).
/// Power boost and zone speed caps use the cube law (power and fuel proportional to speed cubed, constant SFOC) — an approximation, not a validated hydrodynamic model.
/// </summary>
internal static class ArcPhysics
{
    private const double G = 9.80665;

    public static ArcOutcome? Evaluate(ArcInput arc, SpeedSetting setting, PowerMode mode, ArcPhysicsContext ctx, double weatherScale = 1, bool calm = false) =>
        ctx.Model != null && (ctx.Model.Basis == PowerBasis.FuelProxy || setting.PowerKw is > 0)
            ? EvaluateCalibrated(arc, setting, mode, ctx, ctx.Model, weatherScale, calm)
            : EvaluateApproximate(arc, setting, mode, ctx, weatherScale, calm);

    /// <summary>Fallback when fewer than two usable operating points exist: speed loss from the weather tables at constant power and a cube law for boost and zone caps.</summary>
    private static ArcOutcome? EvaluateApproximate(ArcInput arc, SpeedSetting setting, PowerMode mode, ArcPhysicsContext ctx, double weatherScale, bool calm)
    {
        var limits = ctx.Limits;
        if (setting.SpeedKn <= 0) return null;
        if (arc.InEca && setting.EcaFuelRateMtPerDay <= 0) return null;

        // Zone speed caps lower the RPM: speed linear, power and fuel cubic.
        var baseSpeed = Math.Min(setting.SpeedKn, arc.ZoneSpeedCapKn);
        if (baseSpeed <= 0.5) return null;
        var ratio = baseSpeed / setting.SpeedKn;
        var fuelScale = ratio * ratio * ratio;
        var rpm = setting.Rpm * ratio;
        var power = setting.PowerKw * fuelScale;

        var windEffect = 0.0;
        var waveEffect = 0.0;
        if (!calm)
        {
            windEffect = ctx.Wind.Effect(limits.DeadweightMt, Beaufort(arc.WindKn * weatherScale), Fold(arc.BearingDeg, arc.WindFromDeg));
            waveEffect = ctx.Wave.Effect(limits.DeadweightMt, arc.WaveM * weatherScale, Fold(arc.BearingDeg, arc.WaveFromDeg));
        }
        var weatherEffect = windEffect + waveEffect;
        var current = arc.CurrentKn * Math.Cos((arc.CurrentToDeg - arc.BearingDeg) * Math.PI / 180);

        var stw = baseSpeed + weatherEffect;
        if (mode == PowerMode.Boost)
        {
            if (weatherEffect >= -0.01 || stw <= 1 || !power.HasValue || !limits.MaxPowerKw.HasValue) return null;
            var wanted = Math.Pow(baseSpeed / stw, 3);
            var allowed = limits.MaxPowerKw.Value / power.Value;
            if (allowed <= 1.001) return null;
            var used = Math.Min(wanted, allowed);
            var gain = Math.Cbrt(used);
            stw *= gain;
            rpm *= gain;
            power *= used;
            fuelScale *= used;
            if (limits.MaxRpm.HasValue && rpm > limits.MaxRpm.Value) return null;
        }

        var sog = stw + current;
        if (sog <= 0.5) return null;

        if (rpm > 0)
        {
            if (limits.MinRpm.HasValue && rpm < limits.MinRpm.Value - 1e-9) return null;
            if (limits.CriticalRpmMin.HasValue && limits.CriticalRpmMax.HasValue && rpm > limits.CriticalRpmMin && rpm < limits.CriticalRpmMax) return null;
            if (arc.ZoneRpmMax.HasValue && rpm > arc.ZoneRpmMax.Value) return null;
            if (arc.ZoneRpmMin.HasValue && rpm < arc.ZoneRpmMin.Value) return null;
            // Blower cut-in/cut-off band: the engine should run below or above it, not inside the transition.
            if (limits.BlowerMin.HasValue && limits.BlowerMax.HasValue && limits.BlowerMax > limits.BlowerMin && rpm > limits.BlowerMin && rpm < limits.BlowerMax) return null;
            if (limits.ViolatesLoadDiagram(rpm, power)) return null;
        }

        var hours = arc.DistNm / sog;
        var ecaRate = setting.EcaFuelRateMtPerDay * fuelScale;
        var normalRate = setting.FuelRateMtPerDay * fuelScale;
        var rate = arc.InEca ? ecaRate : normalRate;
        var mainFuel = rate / 24 * hours;
        var auxGo = setting.AuxGoRateMtPerDay / 24 * hours;
        var co2 = mainFuel * (arc.InEca ? setting.EcaCo2Factor : setting.Co2Factor) + auxGo * VesselSpeedProfile.Co2Factor("LSMGO");
        var cost = (arc.InEca ? ctx.GoPriceUsdPerMt : ctx.FoPriceUsdPerMt) * mainFuel + ctx.GoPriceUsdPerMt * auxGo;

        var exceeds = limits.MaxSwhM.HasValue && arc.WaveM > limits.MaxSwhM.Value || limits.MaxWindKn.HasValue && arc.WindKn > limits.MaxWindKn.Value;
        var (sync, parametric, pitch) = SeaStateRisk(arc, stw, limits);

        var exposure = hours * (arc.WaveM * arc.WaveM + Math.Pow(arc.WindKn / 10, 2)) * (exceeds ? 10 : 1);
        exposure += hours * 5 * ((sync ? 1 : 0) + (parametric ? 1 : 0) + (pitch ? 1 : 0));

        return new ArcOutcome(
            setting, mode, sog, stw, hours, mainFuel, auxGo, co2, cost, rpm, power,
            power.HasValue && limits.InstalledKw.HasValue ? power / limits.InstalledKw : null,
            rate, exceeds, sync ? hours : 0, parametric ? hours : 0, pitch ? hours : 0, 0, exposure);
    }

    /// <summary>
    /// Power-resistance equilibrium on the calibrated curve: the engine setting fixes available X; speed through water solves
    /// X_calm(V) + F_wave V + F_wind(V) V = X. Currents are vectors: the crab angle keeps the intended track, SOG is the along-track ground speed.
    /// Boost raises X (within maximum power, RPM and load-diagram limits) to hold the calm-water speed. Returns null, tallied by reason, when no permitted operating point exists.
    /// </summary>
    private static ArcOutcome? EvaluateCalibrated(ArcInput arc, SpeedSetting setting, PowerMode mode, ArcPhysicsContext ctx, VesselPerformanceModel model, double weatherScale, bool calm)
    {
        var limits = ctx.Limits;
        var tally = ctx.Tally;
        if (setting.SpeedKn <= 0) return null;
        if (arc.InEca && setting.EcaFuelRateMtPerDay <= 0) return null;

        var proxy = model.Basis == PowerBasis.FuelProxy;
        var settingX = proxy ? setting.FuelRateMtPerDay : setting.PowerKw!.Value;
        var scale = settingX / model.CalmX(setting.SpeedKn); // 1 on the curve; differs for the displacement-scaled Custom profile
        var baseSpeed = Math.Min(setting.SpeedKn, arc.ZoneSpeedCapKn);
        if (baseSpeed <= 0.5) return null;
        var holdX = scale * model.CalmX(baseSpeed);

        double? PowerOf(double x) => !proxy ? x : setting.PowerKw is > 0 ? setting.PowerKw * x / settingX : null;
        string? Violation(double x) => EngineConstraintValidator.Check(limits, arc, model.RpmAt(x / scale) ?? 0, PowerOf(x));

        var relativeCurrent = (arc.CurrentToDeg - arc.BearingDeg) * Math.PI / 180;
        var currentAlong = arc.CurrentKn * Math.Cos(relativeCurrent);
        var currentCross = arc.CurrentKn * Math.Sin(relativeCurrent);

        bool Solve(double x, out double stw, out double crabDeg)
        {
            stw = 0;
            crabDeg = 0;
            var heading = arc.BearingDeg;
            for (var iteration = 0; iteration < 3; iteration++)
            {
                var forces = calm ? WeatherForces.None : WeatherResistanceCalculator.Compute(model, ctx, arc, heading, weatherScale);
                if (WeatherResistanceCalculator.SolveSpeed(model, scale, forces, x) is not { } speed)
                {
                    tally?.Add("speed outside the calibrated range");
                    return false;
                }
                if (Math.Abs(currentCross) >= 0.95 * speed)
                {
                    tally?.Add("cross current too strong to keep the track");
                    return false;
                }
                stw = speed;
                crabDeg = Math.Asin(-currentCross / speed) * 180 / Math.PI;
                heading = arc.BearingDeg + crabDeg;
            }
            return true;
        }

        var x = holdX;
        if (mode == PowerMode.Boost)
        {
            if (calm || Violation(holdX) != null) return null;
            double? cap = !limits.MaxPowerKw.HasValue ? null : !proxy ? limits.MaxPowerKw : setting.PowerKw is > 0 ? limits.MaxPowerKw / setting.PowerKw * settingX : null;
            if (!cap.HasValue) return null;

            var needed = holdX + WeatherResistanceCalculator.Compute(model, ctx, arc, arc.BearingDeg, weatherScale).ExtraX(baseSpeed);
            var target = Math.Min(needed, cap.Value);
            if (target <= holdX * 1.001) return null;

            x = target;
            if (Violation(x) != null)
            {
                var low = holdX;
                var high = target;
                for (var iteration = 0; iteration < 24; iteration++)
                {
                    var middle = 0.5 * (low + high);
                    if (Violation(middle) == null) low = middle; else high = middle;
                }
                x = low;
                if (x <= holdX * 1.001) return null;
            }
        }
        else if (Violation(holdX) is { } violation)
        {
            tally?.Add(violation);
            return null;
        }

        if (!Solve(x, out var stwKn, out var crabDegrees)) return null;
        var sog = stwKn * Math.Cos(crabDegrees * Math.PI / 180) + currentAlong;
        if (sog <= 0.5)
        {
            tally?.Add("speed over ground too low");
            return null;
        }

        var rpm = model.RpmAt(x / scale) ?? 0;
        var power = PowerOf(x);
        var fuelRate = scale * model.FuelMtPerDay(x / scale);
        var ecaRatio = setting.FuelRateMtPerDay > 0 ? setting.EcaFuelRateMtPerDay / setting.FuelRateMtPerDay : 1;
        var rate = arc.InEca ? fuelRate * ecaRatio : fuelRate;

        var hours = arc.DistNm / sog;
        var mainFuel = rate / 24 * hours;
        var auxGo = setting.AuxGoRateMtPerDay / 24 * hours;
        var co2 = mainFuel * (arc.InEca ? setting.EcaCo2Factor : setting.Co2Factor) + auxGo * VesselSpeedProfile.Co2Factor("LSMGO");
        var cost = (arc.InEca ? ctx.GoPriceUsdPerMt : ctx.FoPriceUsdPerMt) * mainFuel + ctx.GoPriceUsdPerMt * auxGo;

        var exceeds = limits.MaxSwhM.HasValue && arc.WaveM > limits.MaxSwhM.Value || limits.MaxWindKn.HasValue && arc.WindKn > limits.MaxWindKn.Value;
        var (sync, parametric, pitch) = SeaStateRisk(arc, stwKn, limits);
        var exposure = hours * (arc.WaveM * arc.WaveM + Math.Pow(arc.WindKn / 10, 2)) * (exceeds ? 10 : 1);
        exposure += hours * 5 * ((sync ? 1 : 0) + (parametric ? 1 : 0) + (pitch ? 1 : 0));

        return new ArcOutcome(
            setting, mode, sog, stwKn, hours, mainFuel, auxGo, co2, cost, rpm, power,
            power.HasValue && limits.InstalledKw.HasValue ? power / limits.InstalledKw : null,
            rate, exceeds, sync ? hours : 0, parametric ? hours : 0, pitch ? hours : 0, 0, exposure);
    }

    /// <summary>Advisory screening from wave period: encounter period against the roll period (synchronous at beam, parametric at head/following) and wavelength against ship length (pitch).</summary>
    private static (bool Sync, bool Parametric, bool Pitch) SeaStateRisk(ArcInput arc, double stwKn, VesselEngineLimits limits)
    {
        if (arc.WavePeriodS is not > 1 || arc.WaveM < 1) return (false, false, false);
        var tw = arc.WavePeriodS.Value;
        var alpha = Fold(arc.BearingDeg, arc.WaveFromDeg); // 0 = head sea, 180 = following
        var omega = 2 * Math.PI / tw;
        var encounterOmega = omega + omega * omega * (stwKn * 0.514444) / G * Math.Cos(alpha * Math.PI / 180);
        var encounterPeriod = encounterOmega > 0.01 ? 2 * Math.PI / encounterOmega : double.PositiveInfinity;

        var sync = false;
        var parametric = false;
        if (limits.RollPeriodSec is > 0)
        {
            var roll = limits.RollPeriodSec.Value;
            sync = alpha is >= 60 and <= 120 && Math.Abs(encounterPeriod - roll) / roll < 0.2;
            parametric = (alpha < 45 || alpha > 135) && Math.Abs(encounterPeriod - roll / 2) / (roll / 2) < 0.2;
        }

        var pitch = false;
        if (limits.LengthM is > 0 && alpha < 45)
        {
            var wavelength = G * tw * tw / (2 * Math.PI);
            var ratio = wavelength / limits.LengthM.Value;
            pitch = ratio is >= 0.8 and <= 1.2;
        }
        return (sync, parametric, pitch);
    }

    public static int Beaufort(double knots)
    {
        double[] upperBoundsKn = { 1, 3, 6, 10, 16, 21, 27, 33, 40, 47, 55, 63 };
        for (var index = 0; index < upperBoundsKn.Length; index++)
        {
            if (knots <= upperBoundsKn[index]) return index;
        }
        return 12;
    }

    public static double Fold(double courseDeg, double weatherFromDeg)
    {
        var diff = Math.Abs(((weatherFromDeg - courseDeg) % 360 + 360) % 360);
        return diff > 180 ? 360 - diff : diff;
    }
}
