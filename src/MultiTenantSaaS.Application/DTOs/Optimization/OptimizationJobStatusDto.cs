namespace MultiTenantSaaS.Application.DTOs.Optimization;

public class OptimizationJobStatusDto
{
    public Guid JobId { get; set; }

    /// <summary>Queued | Running | Completed | Failed | Cancelled</summary>
    public string Status { get; set; } = "Queued";

    public int Percent { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string? Error { get; set; }
    public OptimizationRunResultDto? Result { get; set; }
}
