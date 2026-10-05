namespace MultiTenantSaaS.Application.DTOs.Bunker;

public class BunkerRequirementDto
{
    public Guid Id { get; set; }
    public string RequirementNo { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Pending RFQ";
    public string VesselName { get; set; } = string.Empty;
    public string? Imo { get; set; }
    public string? Reference { get; set; }
    public string? Leg { get; set; }
    public string? Route { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string BunkerPort { get; set; } = string.Empty;
    public string? Eta { get; set; }
    public string? RequiredOn { get; set; }
    public string? RequiredIso { get; set; }
    public string? LaycanStart { get; set; }
    public string? LaycanEnd { get; set; }
    public string FuelType { get; set; } = "VLSFO";
    public string Grade { get; set; } = "ISO 8217:2017 RMG 380";
    public double Quantity { get; set; }
    public double RobArrival { get; set; }
    public double ExpectedCons { get; set; }
    public string? ChartererInstructions { get; set; }
    public string? OwnerInstructions { get; set; }
    public int SuppliersInvited { get; set; }
    public string? Supplier { get; set; }
    public double? PricePerMt { get; set; }
    public double? TotalCost { get; set; }
    public string? PoNo { get; set; }
    public string? ContractRef { get; set; }
    public string? BookedOn { get; set; }
    public string? ConfirmNo { get; set; }
    public string? DeliveryMethod { get; set; }
    public double? SuppliedQty { get; set; }
    public double? DeliveredQty { get; set; }
    public string? SupplyDateTime { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvoiceDate { get; set; }
    public double? InvoiceAmount { get; set; }
    public string? PaymentTerms { get; set; }
    public string? DueDate { get; set; }
    public string? DueIso { get; set; }
    public double? AmountPaid { get; set; }
    public string? PaymentRef { get; set; }
    public string? PaymentDate { get; set; }
    public string ApprovalStatus { get; set; } = "Not Submitted";
    public string PaymentStatus { get; set; } = "None";
    public string? QuotesJson { get; set; }
    public string? FuelLinesJson { get; set; }
    public string? AdditionalChargesJson { get; set; }
    public string? ClaimsJson { get; set; }
    public string? AuditJson { get; set; }
    public string? DocumentsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateBunkerRequirementRequestDto
{
    public string? RequirementNo { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Pending RFQ";
    public string VesselName { get; set; } = string.Empty;
    public string? Imo { get; set; }
    public string? Reference { get; set; }
    public string? Leg { get; set; }
    public string? Route { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string BunkerPort { get; set; } = string.Empty;
    public string? Eta { get; set; }
    public string? RequiredOn { get; set; }
    public string? RequiredIso { get; set; }
    public string? LaycanStart { get; set; }
    public string? LaycanEnd { get; set; }
    public string FuelType { get; set; } = "VLSFO";
    public string Grade { get; set; } = "ISO 8217:2017 RMG 380";
    public double Quantity { get; set; }
    public double RobArrival { get; set; }
    public double ExpectedCons { get; set; }
    public string? ChartererInstructions { get; set; }
    public string? OwnerInstructions { get; set; }
    public int SuppliersInvited { get; set; }
    public string? Supplier { get; set; }
    public double? PricePerMt { get; set; }
    public double? TotalCost { get; set; }
    public string? PoNo { get; set; }
    public string? ContractRef { get; set; }
    public string? BookedOn { get; set; }
    public string? ConfirmNo { get; set; }
    public string? DeliveryMethod { get; set; }
    public double? SuppliedQty { get; set; }
    public double? DeliveredQty { get; set; }
    public string? SupplyDateTime { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvoiceDate { get; set; }
    public double? InvoiceAmount { get; set; }
    public string? PaymentTerms { get; set; }
    public string? DueDate { get; set; }
    public string? DueIso { get; set; }
    public double? AmountPaid { get; set; }
    public string? PaymentRef { get; set; }
    public string? PaymentDate { get; set; }
    public string ApprovalStatus { get; set; } = "Not Submitted";
    public string PaymentStatus { get; set; } = "None";
    public string? QuotesJson { get; set; }
    public string? FuelLinesJson { get; set; }
    public string? AdditionalChargesJson { get; set; }
    public string? ClaimsJson { get; set; }
    public string? AuditJson { get; set; }
    public string? DocumentsJson { get; set; }
}

public class UpdateBunkerRequirementRequestDto : CreateBunkerRequirementRequestDto
{
}
