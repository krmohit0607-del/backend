namespace MultiTenantSaaS.Application.DTOs.HirePayments;

public class HirePaymentDto
{
    public Guid Id { get; set; }
    public string VoyageId { get; set; } = string.Empty;
    public string? VesselName { get; set; }
    public string Side { get; set; } = "Owners";
    public string InstallmentKey { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal OnHireDays { get; set; }
    public decimal OffHireDays { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Draft";
    public bool Ballast { get; set; }
    public decimal Bunkers { get; set; }
    public decimal BunkerCredit { get; set; }
}
