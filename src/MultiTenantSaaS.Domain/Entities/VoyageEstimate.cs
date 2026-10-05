using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class VoyageEstimate : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string EstimateNo { get; set; } = string.Empty; // e.g. EST-2608-01
    public string VesselName { get; set; } = string.Empty;
    public string FixType { get; set; } = "Voyage Charter";
    public string Status { get; set; } = "Draft"; // Draft, Fixed, Reviewing, Cancelled
    public double Profit { get; set; }
    public double Tce { get; set; }
    public string? Commodity { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public double Quantity { get; set; }
    public double FreightRate { get; set; }
    public string? DataJson { get; set; } // Full snapshot for reopening the estimate
    public string? BookRef { get; set; } // Reference to cargo/tonnage book entry that created this estimate

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
}
