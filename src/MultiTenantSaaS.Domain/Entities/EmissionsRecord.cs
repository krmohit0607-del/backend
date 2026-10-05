using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class EmissionsRecord : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string VoyageCode { get; set; } = string.Empty; // e.g. OPT001
    public string VesselName { get; set; } = string.Empty;
    public string ComplianceYear { get; set; } = string.Empty;
    public string? Trade { get; set; }
    public string EuaPriceEur { get; set; } = "72.50";
    public string Co2AdjustmentT { get; set; } = "0";
    public string? ComplianceJson { get; set; }
    public string? AdjustmentsJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedDate { get; set; }

    // Navigation property
    public virtual Tenant? Tenant { get; set; }
}
