namespace MultiTenantSaaS.Application.DTOs.Accounts;

public class FinancialTransactionDto
{
    public Guid Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string Kind { get; set; } = "Payable";
    public string Category { get; set; } = "Freight";
    public string Module { get; set; } = "Operations";
    public string Company { get; set; } = "ODAS Shipping Ltd";
    public string VesselName { get; set; } = string.Empty;
    public string Voyage { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? Fixture { get; set; }
    public string Counterparty { get; set; } = string.Empty;
    public string InvoiceNo { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public double Amount { get; set; }
    public double ExchangeRate { get; set; } = 1.0;
    public string InvoiceDate { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string DueIso { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string Approval { get; set; } = "Approved";
    public string Priority { get; set; } = "Medium";
    public string Pic { get; set; } = "Accounts";
    public string? Bank { get; set; }
    public string? Method { get; set; }
    public string? PaymentDate { get; set; }
    public string? PaymentRef { get; set; }
    public string? SwiftDocUrl { get; set; }
    public string? Remarks { get; set; }
    public string? AuditJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateFinancialTransactionRequestDto
{
    public string? TransactionNo { get; set; }
    public string Kind { get; set; } = "Payable";
    public string Category { get; set; } = "Freight";
    public string Module { get; set; } = "Operations";
    public string? Company { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string Voyage { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? Fixture { get; set; }
    public string Counterparty { get; set; } = string.Empty;
    public string InvoiceNo { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public double Amount { get; set; }
    public double ExchangeRate { get; set; } = 1.0;
    public string? InvoiceDate { get; set; }
    public string? DueDate { get; set; }
    public string? DueIso { get; set; }
    public string Status { get; set; } = "Draft";
    public string Approval { get; set; } = "Approved";
    public string Priority { get; set; } = "Medium";
    public string? Pic { get; set; }
    public string? Bank { get; set; }
    public string? Method { get; set; }
    public string? PaymentDate { get; set; }
    public string? PaymentRef { get; set; }
    public string? SwiftDocUrl { get; set; }
    public string? Remarks { get; set; }
    public string? AuditJson { get; set; }
}

public class UpdateFinancialTransactionRequestDto : CreateFinancialTransactionRequestDto
{
}
