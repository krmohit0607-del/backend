using System.Text.Json;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>One row of the vessel's engine load diagram; blank cells are unknown and not enforced.</summary>
internal sealed record LoadDiagramPoint(double Rpm, double? MinPowerKw, double? MaxPowerKw, double? MaxTorqueKnm)
{
    private sealed class Row
    {
        public double? Rpm { get; set; }
        public double? MinPowerKw { get; set; }
        public double? MaxPowerKw { get; set; }
        public double? MaxTorqueKnm { get; set; }
    }

    public static IReadOnlyList<LoadDiagramPoint> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<LoadDiagramPoint>();
        try
        {
            var rows = JsonSerializer.Deserialize<List<Row>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new();
            return rows
                .Where(row => row.Rpm is > 0 && (row.MinPowerKw.HasValue || row.MaxPowerKw.HasValue || row.MaxTorqueKnm.HasValue))
                .Select(row => new LoadDiagramPoint(row.Rpm!.Value, Positive(row.MinPowerKw), Positive(row.MaxPowerKw), Positive(row.MaxTorqueKnm)))
                .OrderBy(point => point.Rpm)
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<LoadDiagramPoint>();
        }

        static double? Positive(double? value) => value is > 0 ? value : null;
    }

    /// <summary>Linear interpolation over the points that define the selected column; null when fewer than one defined point brackets the RPM.</summary>
    public static double? Interpolate(IReadOnlyList<LoadDiagramPoint> points, double rpm, Func<LoadDiagramPoint, double?> column)
    {
        var defined = points.Where(point => column(point).HasValue).ToList();
        if (defined.Count == 0 || rpm < defined[0].Rpm - 1e-9 || rpm > defined[^1].Rpm + 1e-9) return null;
        for (var index = 0; index < defined.Count - 1; index++)
        {
            var low = defined[index];
            var high = defined[index + 1];
            if (rpm >= low.Rpm - 1e-9 && rpm <= high.Rpm + 1e-9)
            {
                var span = high.Rpm - low.Rpm;
                var t = span < 1e-9 ? 0 : (rpm - low.Rpm) / span;
                return column(low)!.Value + (column(high)!.Value - column(low)!.Value) * t;
            }
        }
        return column(defined[^1]);
    }
}

/// <summary>Engine limits marked in the vessel's load diagram table ("Indicate limits": EPL, Min MCR, Max MCR, Min RPM, Max RPM) with the row's % load or RPM.</summary>
internal static class LoadLimitMarkers
{
    private sealed class Row
    {
        public string? Limit { get; set; }
        public double? LoadPct { get; set; }
        public double? Rpm { get; set; }
    }

    /// <param name="MaxFractions">Upper load fractions (EPL and Max MCR rows), as 0-1 fractions of installed power.</param>
    /// <param name="MinFraction">Lower load fraction (Min MCR, or the older Least MCR, row).</param>
    /// <param name="MinRpm">RPM of the Min RPM row.</param>
    /// <param name="MaxRpm">RPM of the Max RPM row.</param>
    public sealed record Result(IReadOnlyList<double> MaxFractions, double? MinFraction, double? MinRpm, double? MaxRpm)
    {
        public static Result None { get; } = new(Array.Empty<double>(), null, null, null);
    }

    public static Result Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Result.None;
        try
        {
            var rows = JsonSerializer.Deserialize<List<Row>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new();
            var marked = rows.Where(row => !string.IsNullOrWhiteSpace(row.Limit)).ToList();
            var loadRows = marked.Where(row => row.LoadPct is > 0 and <= 150).ToList();
            double Fraction(Row row) => row.LoadPct!.Value / 100;
            var max = loadRows.Where(row => Matches(row, "EPL") || Matches(row, "Max MCR")).Select(Fraction).ToList();
            var least = loadRows.Where(row => Matches(row, "Min MCR") || Matches(row, "Least MCR")).Select(Fraction).ToList();
            var minRpm = marked.Where(row => row.Rpm is > 0 && Matches(row, "Min RPM")).Select(row => row.Rpm!.Value).ToList();
            var maxRpm = marked.Where(row => row.Rpm is > 0 && Matches(row, "Max RPM")).Select(row => row.Rpm!.Value).ToList();
            return new Result(max, least.Count > 0 ? least.Max() : null, minRpm.Count > 0 ? minRpm.Max() : null, maxRpm.Count > 0 ? maxRpm.Min() : null);
        }
        catch (JsonException)
        {
            return Result.None;
        }

        static bool Matches(Row row, string name) => string.Equals(row.Limit!.Trim(), name, StringComparison.OrdinalIgnoreCase);
    }
}
