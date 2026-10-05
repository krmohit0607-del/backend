namespace MultiTenantSaaS.Application.DTOs.FinalDisbursements;

public class FinalDisbursementDto
{
    public Guid Id { get; set; }
    public Guid VoyageId { get; set; }
    public string FdaNo { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string? Agent { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal FdaAmount { get; set; }
    public decimal PdaAdvance { get; set; }
    public decimal BalancePayable { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Approval { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? Remarks { get; set; }
}

public class CreateFinalDisbursementRequestDto
{
    public Guid VoyageId { get; set; }
    public Guid? RelatedPdaId { get; set; }
    public string FdaNo { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string? Agent { get; set; }
    public string? Currency { get; set; }
    public decimal FdaAmount { get; set; }
    public decimal PdaAdvance { get; set; }
    public decimal BalancePayable { get; set; }
}

public class UpdateFinalDisbursementRequestDto
{
    public decimal? FdaAmount { get; set; }
    public decimal? PdaAdvance { get; set; }
    public decimal? BalancePayable { get; set; }
    public string? Status { get; set; }
    public string? Approval { get; set; }
}
