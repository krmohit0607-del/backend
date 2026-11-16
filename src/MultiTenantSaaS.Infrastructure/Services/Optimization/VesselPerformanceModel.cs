namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

internal enum PowerBasis
{
    /// <summary>Every operating point has brake power (kW); SFOC is derived per point and interpolated against engine power.</summary>
    BrakePowerKw,

    /// <summary>Power is missing for at least one point, so main-engine fuel rate stands in for power (constant SFOC assumed).</summary>
    FuelProxy,
}

/// <summary>
/// Calm-water speed-power-RPM-fuel model calibrated from the vessel's own operating points (configured Speed &amp; Cons rows or observed noon reports).
/// X is brake power in kW, or main-engine fuel in MT/day when power is not available for every point. X(V) is a monotone piecewise power law
/// (log-log interpolation between points), so no single speed exponent is assumed. Outside the data range it extrapolates with a bounded exponent
/// and is only valid between <see cref="ValidMinSpeedKn"/> and <see cref="ValidMaxSpeedKn"/>.
/// Validation status: calibrated to the supplied points only; not validated against sea trials or model tests.
/// </summary>
internal sealed class VesselPerformanceModel
{
    private const double MinExtrapolationExponent = 2.0;
    private const double MaxExtrapolationExponent = 5.0;

    private sealed record Point(double SpeedKn, double X, double FuelMtPerDay);

    private readonly Point[] _points;
    private readonly (double X, double Rpm)[] _rpm;
    private readonly (double X, double Sfoc)[] _sfoc;

    private VesselPerformanceModel(Point[] points, (double X, double Rpm)[] rpm, (double X, double Sfoc)[] sfoc, PowerBasis basis, double referenceSpeedKn, List<string> notes)
    {
        _points = points;
        _rpm = rpm;
        _sfoc = sfoc;
        Basis = basis;
        ReferenceSpeedKn = referenceSpeedKn;
        Notes = notes;
    }

    public PowerBasis Basis { get; }
    public double ReferenceSpeedKn { get; }
    public IReadOnlyList<string> Notes { get; }
    public int PointCount => _points.Length;
    public double MinSpeedKn => _points[0].SpeedKn;
    public double MaxSpeedKn => _points[^1].SpeedKn;
    public double ValidMinSpeedKn => 0.5 * MinSpeedKn;
    public double ValidMaxSpeedKn => 1.15 * MaxSpeedKn;
    public double MaxDataX => _points[^1].X;

    public string Describe() =>
        $"Calibrated speed-power curve from {PointCount} operating points ({(Basis == PowerBasis.BrakePowerKw ? "brake power with SFOC by load" : "fuel rate as power proxy, constant SFOC assumed")}), " +
        $"data range {MinSpeedKn:0.0}-{MaxSpeedKn:0.0} kn, valid {ValidMinSpeedKn:0.0}-{ValidMaxSpeedKn:0.0} kn; calibrated to supplied data only, not validated against sea trials or model tests.";

    public static VesselPerformanceModel? Build(IEnumerable<SpeedSetting> settings)
    {
        var usable = settings.Where(setting => setting.SpeedKn > 0 && setting.FuelRateMtPerDay > 0).ToList();
        if (usable.Count < 2) return null;

        var basis = usable.All(setting => setting.PowerKw is > 0) ? PowerBasis.BrakePowerKw : PowerBasis.FuelProxy;
        var notes = new List<string>();
        if (basis == PowerBasis.FuelProxy)
            notes.Add("Brake power is not available for every operating point, so main-engine fuel rate is used as the power proxy (constant SFOC assumed) and engine load, torque and load-diagram checks are skipped.");

        var merged = usable
            .GroupBy(setting => Math.Round(setting.SpeedKn, 2))
            .Select(group => new
            {
                Speed = group.Average(setting => setting.SpeedKn),
                X = basis == PowerBasis.BrakePowerKw ? group.Average(setting => setting.PowerKw!.Value) : group.Average(setting => setting.FuelRateMtPerDay),
                Fuel = group.Average(setting => setting.FuelRateMtPerDay),
                Rpm = group.Where(setting => setting.Rpm > 0).Select(setting => setting.Rpm).DefaultIfEmpty(0).Average(),
            })
            .OrderBy(point => point.Speed)
            .ToList();
        if (merged.Count < 2) return null;

        var blocks = PoolMonotone(merged.Select(point => point.X).ToArray());
        if (blocks.Any(block => block.Count > 1))
            notes.Add("Operating points were not monotone (power or fuel fell as speed rose); the offending points were averaged to keep the speed-power curve physical.");
        var pooled = blocks.Select(block =>
        {
            var items = merged.Skip(block.Start).Take(block.Count).ToList();
            var rpms = items.Where(item => item.Rpm > 0).Select(item => item.Rpm).ToList();
            return new { Speed = items.Average(item => item.Speed), X = items.Average(item => item.X), Fuel = items.Average(item => item.Fuel), Rpm = rpms.Count > 0 ? rpms.Average() : 0 };
        }).ToList();
        if (pooled.Count < 2) return null;
        for (var index = 1; index < pooled.Count; index++)
            if (pooled[index].X <= pooled[index - 1].X * (1 + 1e-9)) return null;

        var points = pooled.Select(point => new Point(point.Speed, point.X, point.Fuel)).ToArray();
        var rpm = pooled.Select(point => (point.X, point.Rpm)).Where(item => item.Rpm > 0).OrderBy(item => item.X).ToArray();
        (double X, double Sfoc)[] sfoc = basis == PowerBasis.BrakePowerKw
            ? points.Select(point => (X: point.X, Sfoc: point.FuelMtPerDay / 24 * 1e6 / point.X)).ToArray()
            : Array.Empty<(double X, double Sfoc)>();
        if (sfoc.Any(item => item.Sfoc is < 100 or > 400))
            notes.Add("Derived SFOC is outside 100-400 g/kWh for at least one operating point; check the power and consumption entries.");

        var reference = usable.FirstOrDefault(setting => setting.IsCpDefault)?.SpeedKn ?? points[points.Length / 2].SpeedKn;
        return new VesselPerformanceModel(points, rpm, sfoc, basis, reference, notes);
    }

    /// <summary>Calm-water X (kW or MT/day) at speed through water; log-log interpolation, bounded-exponent extrapolation.</summary>
    public double CalmX(double speedKn)
    {
        if (speedKn <= 0) return 0;
        var segment = 0;
        while (segment < _points.Length - 2 && speedKn > _points[segment + 1].SpeedKn) segment++;
        var low = _points[segment];
        var high = _points[segment + 1];
        var exponent = Math.Log(high.X / low.X) / Math.Log(high.SpeedKn / low.SpeedKn);
        if (speedKn < _points[0].SpeedKn || speedKn > _points[^1].SpeedKn)
            exponent = Math.Clamp(exponent, MinExtrapolationExponent, MaxExtrapolationExponent);
        return low.X * Math.Pow(speedKn / low.SpeedKn, exponent);
    }

    /// <summary>Main-engine RPM at X; propeller-law exponent (1/3) with one point, interpolated curve with more. Null when no RPM data exists.</summary>
    public double? RpmAt(double x)
    {
        if (_rpm.Length == 0 || x <= 0) return null;
        if (_rpm.Length == 1) return _rpm[0].Rpm * Math.Pow(x / _rpm[0].X, 1.0 / 3.0);

        var segment = 0;
        while (segment < _rpm.Length - 2 && x > _rpm[segment + 1].X) segment++;
        var low = _rpm[segment];
        var high = _rpm[segment + 1];
        var exponent = Math.Log(high.Rpm / low.Rpm) / Math.Log(high.X / low.X);
        if (x < _rpm[0].X || x > _rpm[^1].X) exponent = Math.Clamp(exponent, 0.2, 0.6);
        return low.Rpm * Math.Pow(x / low.X, exponent);
    }

    /// <summary>Main-engine fuel (MT/day) at X: SFOC interpolated against engine power (held at the end values outside the data), or X itself for the proxy basis.</summary>
    public double FuelMtPerDay(double x)
    {
        if (Basis == PowerBasis.FuelProxy) return x;
        double sfoc;
        if (x <= _sfoc[0].X) sfoc = _sfoc[0].Sfoc;
        else if (x >= _sfoc[^1].X) sfoc = _sfoc[^1].Sfoc;
        else
        {
            var segment = 0;
            while (segment < _sfoc.Length - 2 && x > _sfoc[segment + 1].X) segment++;
            var t = (x - _sfoc[segment].X) / (_sfoc[segment + 1].X - _sfoc[segment].X);
            sfoc = _sfoc[segment].Sfoc + (_sfoc[segment + 1].Sfoc - _sfoc[segment].Sfoc) * t;
        }
        return x * sfoc / 1e6 * 24;
    }

    /// <summary>Pool-adjacent-violators: consecutive speed-ordered points are merged until their mean X is non-decreasing.</summary>
    private static List<(int Start, int Count)> PoolMonotone(double[] values)
    {
        var blocks = new List<(int Start, int Count, double Sum)>();
        for (var index = 0; index < values.Length; index++)
        {
            blocks.Add((index, 1, values[index]));
            while (blocks.Count > 1 && blocks[^2].Sum / blocks[^2].Count > blocks[^1].Sum / blocks[^1].Count)
            {
                var merged = (blocks[^2].Start, blocks[^2].Count + blocks[^1].Count, blocks[^2].Sum + blocks[^1].Sum);
                blocks.RemoveRange(blocks.Count - 2, 2);
                blocks.Add(merged);
            }
        }
        return blocks.Select(block => (block.Start, block.Count)).ToList();
    }
}
