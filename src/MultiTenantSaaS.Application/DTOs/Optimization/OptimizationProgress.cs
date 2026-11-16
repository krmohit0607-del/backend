namespace MultiTenantSaaS.Application.DTOs.Optimization;

/// <summary>Coarse progress report from a running optimization.</summary>
public record OptimizationProgress(int Percent, string Stage);
