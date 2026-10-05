using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

/// <summary>
/// Pro-Forma Disbursement Account — vessel cost estimate for a voyage port/service.
/// </summary>
public class ProFormaDisbursement : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid VoyageId { get; set; }

    public string PdaNo { get; set; } = string.Empty; // e.g., PDA-001
    public string Port { get; set; } = string.Empty; // Load, Bunker, Discharge port
    public string? Agent { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Estimated { get; set; }
    public decimal Advance { get; set; }
    public decimal FdaFinal { get; set; }
    public string Status { get; set; } = "Pending"; // PDA Approved, FDA Received, etc.
    public string? Approval { get; set; } // Pending, Approved, Rejected
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Remarks { get; set; }

    public virtual Voyage Voyage { get; set; } = null!;
    public virtual ICollection<AgentInvoice> InvoiceItems { get; set; } = new List<AgentInvoice>();
}

/// <summary>
/// Final Disbursement Account — actual final settlement for voyage costs.
/// </summary>
public class FinalDisbursement : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid VoyageId { get; set; }
    public Guid? RelatedPdaId { get; set; } // Link to Pro-Forma if applicable

    public string FdaNo { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string? Agent { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal FdaAmount { get; set; }
    public decimal PdaAdvance { get; set; }
    public decimal BalancePayable { get; set; }
    public string Status { get; set; } = "Pending"; // FDA Received, FDA Approved, etc.
    public string? Approval { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Remarks { get; set; }

    public virtual Voyage Voyage { get; set; } = null!;
}

/// <summary>
/// Agent Invoice — vendor bill for port services, bunkers, agency fees, etc.
/// </summary>
public class AgentInvoice : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid VoyageId { get; set; }
    public Guid? PdaId { get; set; } // Link to PDA if applicable

    public string InvoiceNo { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Amount { get; set; }
    public decimal Approved { get; set; }
    public decimal Paid { get; set; }
    public string Category { get; set; } = string.Empty; // FDA—Port, Towage, Survey, etc.
    public string? Port { get; set; }
    public string DeptStatus { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string AccountsStatus { get; set; } = "Pending"; // Pending, Scheduled, Paid, etc.
    public DateTime? PaymentDate { get; set; }
    public string? PaymentRef { get; set; }
    public string? Remarks { get; set; }

    public virtual Voyage Voyage { get; set; } = null!;
    public virtual ProFormaDisbursement? Pda { get; set; }
}

/// <summary>
/// Additional Service Charge — incidental costs like crew change, fresh water, sludge disposal.
/// </summary>
public class AdditionalService : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid VoyageId { get; set; }

    public string Service { get; set; } = string.Empty; // Launch Boat, Fresh Water, Crew Change, etc.
    public string? Vendor { get; set; }
    public string? InvoiceNo { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Cost { get; set; }
    public decimal Tax { get; set; }
    public string? Reason { get; set; }
    public string? RequestedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string? Remarks { get; set; }

    public virtual Voyage Voyage { get; set; } = null!;
}

/// <summary>
/// Claim Record — demurrage claims, cargo claims, offhire claims, etc. Synced from the Operations
/// recap (Hire &amp; Claims tab) by key, so reports can query real rows across the fleet.
/// </summary>
public class ClaimRecord : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    /// <summary>Frontend voyage id/code — not a strict FK, since not every voyage has a backend Voyage row.</summary>
    public string VoyageId { get; set; } = string.Empty;
    public string? VesselName { get; set; }
    /// <summary>Natural key matching the frontend claim row's id, for idempotent upserts from Operations.</summary>
    public string ClaimKey { get; set; } = string.Empty;

    public string ClaimType { get; set; } = string.Empty; // Demurrage, Cargo, Offhire
    public string ClaimReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Open"; // Open, Under Review, Settled, Rejected
    public string? Owner { get; set; } // Charge To: Charterer, Receiver, Owner, Shipper
    public decimal Settlement { get; set; }
    public DateTime? DueDate { get; set; }
    public string? PaymentStatus { get; set; }
    public string? WorkflowStatus { get; set; } // Draft, Sent For Approval, Approved, Sent For Payment, Paid & Locked
    public string? AttachmentsJson { get; set; }
    public DateTime? SettledDate { get; set; }
    public string? SettledBy { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// Hire Payment — one installment of the owners' or charterers' hire payment schedule for a
/// time-charter voyage. Synced from the Operations recap (Hire tab); amounts/dates are frozen
/// once a row reaches a locked status (Sent For Payment / Paid &amp; Locked) and recalculated
/// live for every other row as the voyage itinerary/remaining days change.
/// </summary>
public class HirePayment : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    /// <summary>Frontend voyage id/code — not a strict FK, since not every voyage has a backend Voyage row.</summary>
    public string VoyageId { get; set; } = string.Empty;
    public string? VesselName { get; set; }

    /// <summary>Owners (vessel hired-in) or Charterers (vessel hired-out) side of a dual time-charter.</summary>
    public string Side { get; set; } = "Owners";
    /// <summary>Installment number as shown on screen ("1", "2", …), or the duplicate row's own id.</summary>
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
    public string Status { get; set; } = "Draft"; // Draft, Sent For Approval, Approved, Sent For Payment, Paid & Locked
    public bool Ballast { get; set; }
    public decimal Bunkers { get; set; }
    public decimal BunkerCredit { get; set; }
}

/// <summary>
/// Settlement Milestone — voyage settlement timeline tracking.
/// </summary>
public class SettlementMilestone : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid VoyageId { get; set; }

    public string MilestoneLabel { get; set; } = string.Empty; // Fixture Confirmed, Loading Complete, FDA Received, etc.
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string CompletedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "Todo"; // Todo, Current, Done
    public int Sequence { get; set; } // Sort order
    public string? Notes { get; set; }

    public virtual Voyage Voyage { get; set; } = null!;
}
