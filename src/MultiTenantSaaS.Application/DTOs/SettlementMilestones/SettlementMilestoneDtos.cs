namespace MultiTenantSaaS.Application.DTOs.SettlementMilestones;

public class SettlementMilestoneDto
{
    public Guid Id { get; set; }
    public Guid VoyageId { get; set; }
    public string MilestoneLabel { get; set; } = string.Empty;
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string CompletedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string? Notes { get; set; }
}

public class CreateSettlementMilestoneRequestDto
{
    public Guid VoyageId { get; set; }
    public string MilestoneLabel { get; set; } = string.Empty;
    public DateTime? ScheduledDate { get; set; }
    public int? Sequence { get; set; }
    public string? Notes { get; set; }
}

public class UpdateSettlementMilestoneRequestDto
{
    public string? Status { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? CompletedBy { get; set; }
}
