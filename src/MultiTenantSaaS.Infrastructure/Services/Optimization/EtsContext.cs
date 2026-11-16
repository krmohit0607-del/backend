namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>Per-run EU ETS charging rule: the share of tank-to-wake CO2 that is chargeable at a given time (voyage coverage x phase-in for that calendar year).</summary>
internal sealed class EtsContext
{
    private readonly (int Year, double Pct)[] _phaseIn;

    public EtsContext(string classification, double coveragePct, IEnumerable<(int Year, double Pct)> phaseIn, string basis)
    {
        Classification = classification;
        CoveragePct = coveragePct;
        _phaseIn = phaseIn.OrderBy(item => item.Year).ToArray();
        Basis = basis;
    }

    public string Classification { get; }
    public double CoveragePct { get; }
    public string Basis { get; }
    public bool IsConfigured => _phaseIn.Length > 0;

    /// <summary>No ETS policy configured: every tonne of CO2 is charged at the run's EUA price (the behaviour before policies existed).</summary>
    public static EtsContext Unconfigured() => new("Unknown", 100, Array.Empty<(int, double)>(), "No EU ETS policy is configured, so all CO2 is charged at the run's EUA price.");

    public double PhaseInPct(int year)
    {
        if (_phaseIn.Length == 0) return 100;
        if (year < _phaseIn[0].Year) return 0;
        var match = _phaseIn.LastOrDefault(item => item.Year <= year);
        return match.Pct;
    }

    /// <summary>Chargeable fraction of CO2 emitted at <paramref name="utc"/>.</summary>
    public double Factor(DateTime utc) => CoveragePct / 100 * PhaseInPct(utc.Year) / 100;

    public string PhaseInSummary() => _phaseIn.Length == 0 ? "100% in every year (no policy)" : string.Join(", ", _phaseIn.Select(item => $"{item.Year}: {item.Pct:0}%"));
}

/// <summary>Totals used to compare a route with the planned-route baseline.</summary>
internal sealed record ScenarioTotals(DateTime EtaUtc, double TimeHours, double FuelMt, double CostUsd, double Co2Mt);
