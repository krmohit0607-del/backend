namespace MultiTenantSaaS.Application.DTOs.AgentInvoices;

public class AgentInvoiceDto
{
    public Guid Id { get; set; }
    public Guid VoyageId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Amount { get; set; }
    public decimal Approved { get; set; }
    public decimal Paid { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Port { get; set; }
    public string DeptStatus { get; set; } = string.Empty;
    public string AccountsStatus { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string? Remarks { get; set; }
}

public class CreateAgentInvoiceRequestDto
{
    public Guid VoyageId { get; set; }
    public Guid? PdaId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? Currency { get; set; }
    public decimal Amount { get; set; }
    public string? Category { get; set; }
    public string? Port { get; set; }
}

public class UpdateAgentInvoiceRequestDto
{
    public decimal? Amount { get; set; }
    public decimal? Approved { get; set; }
    public decimal? Paid { get; set; }
    public string? DeptStatus { get; set; }
    public string? AccountsStatus { get; set; }
    public DateTime? PaymentDate { get; set; }
}
