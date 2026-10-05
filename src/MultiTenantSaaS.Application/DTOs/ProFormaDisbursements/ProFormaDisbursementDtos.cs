namespace MultiTenantSaaS.Application.DTOs.ProFormaDisbursements;

public class ProFormaDisbursementDto
{
    public Guid Id { get; set; }
    public Guid VoyageId { get; set; }
    public string PdaNo { get; set; }
    public string Port { get; set; }
    public string Agent { get; set; }
    public string Currency { get; set; }
    public decimal Estimated { get; set; }
    public decimal Advance { get; set; }
    public decimal FdaFinal { get; set; }
    public string Status { get; set; }
    public string? Approval { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Remarks { get; set; }
    public List<AgentInvoiceDto> InvoiceItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProFormaDisbursementRequestDto
{
    public Guid VoyageId { get; set; }
    public string PdaNo { get; set; }
    public string Port { get; set; }
    public string Agent { get; set; }
    public string Currency { get; set; }
    public decimal Estimated { get; set; }
    public decimal Advance { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateProFormaDisbursementRequestDto
{
    public string? Port { get; set; }
    public string? Agent { get; set; }
    public decimal? Estimated { get; set; }
    public decimal? Advance { get; set; }
    public string? Status { get; set; }
    public string? Approval { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Remarks { get; set; }
}

public class AgentInvoiceDto
{
    public Guid Id { get; set; }
    public Guid VoyageId { get; set; }
    public string InvoiceNo { get; set; }
    public string Agent { get; set; }
    public decimal Amount { get; set; }
    public decimal Approved { get; set; }
    public decimal Paid { get; set; }
    public string DeptStatus { get; set; }
    public string Department { get; set; }
    public string AccountingStatus { get; set; }
    public string PaymentStatus { get; set; }
    public DateTime? DueDate { get; set; }
}
