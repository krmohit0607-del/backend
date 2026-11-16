namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>Counts why operating points were rejected, so an infeasible result can say which constraint blocked it.</summary>
internal sealed class RejectionTally
{
    private readonly Dictionary<string, int> _counts = new();

    public void Add(string reason) => _counts[reason] = _counts.GetValueOrDefault(reason) + 1;

    public bool Any => _counts.Count > 0;

    public string Summary() => string.Join(", ", _counts.OrderByDescending(pair => pair.Value).Take(5).Select(pair => $"{pair.Key} ({pair.Value})"));
}

/// <summary>Engine operating restrictions: RPM limits, critical RPM range, zone RPM limits, blower cut-in/cut-off band, load diagram and maximum power.</summary>
internal static class EngineConstraintValidator
{
    public const string MaxRpm = "maximum RPM";
    public const string MinRpm = "minimum RPM";
    public const string CriticalRpm = "critical RPM range";
    public const string ZoneRpm = "zone RPM limit";
    public const string Blower = "blower cut-in/cut-off band";
    public const string LoadDiagram = "engine load diagram (power or torque)";
    public const string MaxPower = "maximum engine power";

    /// <summary>The violated restriction, or null when the operating point is permitted. Unknown values (no RPM or power) are not enforced.</summary>
    public static string? Check(VesselEngineLimits limits, ArcInput arc, double rpm, double? powerKw)
    {
        if (rpm > 0)
        {
            if (limits.MaxRpm.HasValue && rpm > limits.MaxRpm.Value + 1e-9) return MaxRpm;
            if (limits.MinRpm.HasValue && rpm < limits.MinRpm.Value - 1e-9) return MinRpm;
            if (limits.CriticalRpmMin.HasValue && limits.CriticalRpmMax.HasValue && rpm > limits.CriticalRpmMin && rpm < limits.CriticalRpmMax) return CriticalRpm;
            if (arc.ZoneRpmMax.HasValue && rpm > arc.ZoneRpmMax.Value) return ZoneRpm;
            if (arc.ZoneRpmMin.HasValue && rpm < arc.ZoneRpmMin.Value) return ZoneRpm;
            // Blower cut-in/cut-off: the recommended RPM stays below the cut-in or above the cut-off, never inside the transition band.
            if (limits.BlowerMin.HasValue && limits.BlowerMax.HasValue && limits.BlowerMax > limits.BlowerMin && rpm > limits.BlowerMin && rpm < limits.BlowerMax) return Blower;
            if (limits.ViolatesLoadDiagram(rpm, powerKw)) return LoadDiagram;
        }
        if (powerKw.HasValue && limits.MaxPowerKw.HasValue && powerKw.Value > limits.MaxPowerKw.Value + 1e-6) return MaxPower;
        return null;
    }
}
