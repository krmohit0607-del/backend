namespace MultiTenantSaaS.Application.DTOs.ClaimRecords;

public class ClaimRecordDto
{
    public Guid Id { get; set; }
    public string VoyageId { get; set; } = string.Empty;
    public string? VesselName { get; set; }
    public string ClaimKey { get; set; } = string.Empty;
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public decimal Settlement { get; set; }
    public DateTime? DueDate { get; set; }
    public string? PaymentStatus { get; set; }
    public string? WorkflowStatus { get; set; }
    public DateTime? SettledDate { get; set; }
    public string? Remarks { get; set; }
}

public class CreateClaimRecordRequestDto
{
    public string VoyageId { get; set; } = string.Empty;
    public string? VesselName { get; set; }
    public string? ClaimKey { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Owner { get; set; }
}

public class UpdateClaimRecordRequestDto
{
    public decimal? Amount { get; set; }
    public decimal? Settlement { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public string? WorkflowStatus { get; set; }
    public string? SettledBy { get; set; }
}

