namespace MultiTenantSaaS.Application.DTOs.AdditionalServices;

public class AdditionalServiceDto
{
    public Guid Id { get; set; }
    public Guid VoyageId { get; set; }
    public string Service { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public string? InvoiceNo { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Cost { get; set; }
    public decimal Tax { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
}

public class CreateAdditionalServiceRequestDto
{
    public Guid VoyageId { get; set; }
    public string Service { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public string? InvoiceNo { get; set; }
    public string? Currency { get; set; }
    public decimal Cost { get; set; }
    public decimal Tax { get; set; }
    public string? Reason { get; set; }
    public string? RequestedBy { get; set; }
}

public class UpdateAdditionalServiceRequestDto
{
    public decimal? Cost { get; set; }
    public decimal? Tax { get; set; }
    public string? Status { get; set; }
    public string? ApprovedBy { get; set; }
}
