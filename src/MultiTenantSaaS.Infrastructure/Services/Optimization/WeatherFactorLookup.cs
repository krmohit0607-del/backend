using MultiTenantSaaS.Domain.Entities.Performance;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>Trilinear (deadweight x magnitude x relative angle) lookup over the wind or wave calibration table; values are signed knots of speed effect, negative = loss.</summary>
internal sealed class WeatherFactorLookup
{
    private readonly double[] _dwBands;
    private readonly double[] _magBands;
    private readonly double[] _angleBands;
    private readonly Dictionary<(double, double, double), double> _cells;

    public WeatherFactorLookup(IEnumerable<WeatherFactorEntry> entries, string weather)
    {
        var rows = entries.Where(entry => string.Equals(entry.Weather, weather, StringComparison.OrdinalIgnoreCase)).ToList();
        _dwBands = rows.Select(row => row.DeadWeight).Distinct().OrderBy(value => value).ToArray();
        _magBands = rows.Select(row => row.Magnitude).Distinct().OrderBy(value => value).ToArray();
        _angleBands = rows.Select(row => row.Angle).Distinct().OrderBy(value => value).ToArray();
        _cells = new Dictionary<(double, double, double), double>();
        foreach (var row in rows) _cells[(row.DeadWeight, row.Magnitude, row.Angle)] = row.Value;
    }

    public bool HasData => _cells.Count > 0;

    public double Effect(double deadweightMt, double magnitude, double relativeAngleDeg)
    {
        if (!HasData || magnitude < _magBands[0]) return 0;
        var (d0, d1, dt) = Bracket(_dwBands, deadweightMt);
        var (m0, m1, mt) = Bracket(_magBands, magnitude);
        var (a0, a1, at) = Bracket(_angleBands, relativeAngleDeg);

        double AtAngle(int d, int m) => Lerp(Cell(d, m, a0), Cell(d, m, a1), at);
        double AtMagnitude(int d) => Lerp(AtAngle(d, m0), AtAngle(d, m1), mt);
        return Lerp(AtMagnitude(d0), AtMagnitude(d1), dt);
    }

    private double Cell(int dw, int mag, int angle)
    {
        var key = (_dwBands[dw], _magBands[mag], _angleBands[angle]);
        if (_cells.TryGetValue(key, out var value)) return value;

        // Sparse table: use the closest populated cell at the same deadweight, else anywhere.
        var candidates = _cells.Where(cell => cell.Key.Item1 == key.Item1).DefaultIfEmpty().ToList();
        if (candidates.Count == 0 || candidates[0].Equals(default(KeyValuePair<(double, double, double), double>))) candidates = _cells.ToList();
        return candidates.OrderBy(cell => Math.Abs(cell.Key.Item2 - key.Item2) + Math.Abs(cell.Key.Item3 - key.Item3) / 180).First().Value;
    }

    private static (int Low, int High, double T) Bracket(double[] bands, double x)
    {
        var last = bands.Length - 1;
        if (x <= bands[0]) return (0, 0, 0);
        if (x >= bands[last]) return (last, last, 0);
        for (var index = 0; index < last; index++)
        {
            if (x >= bands[index] && x <= bands[index + 1])
            {
                var span = bands[index + 1] - bands[index];
                return (index, index + 1, span == 0 ? 0 : (x - bands[index]) / span);
            }
        }
        return (last, last, 0);
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
