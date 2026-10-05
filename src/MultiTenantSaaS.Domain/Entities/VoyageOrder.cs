using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class VoyageOrder : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? Vessel { get; set; }
    public string? Owner { get; set; }
    public string? Charterer { get; set; }
    public string? Broker { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string? Cargo { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Commodity { get; set; }
    public string Client { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string Service { get; set; } = "PMO";
    public string Priority { get; set; } = "MEDIUM";
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<Voyage> Voyages { get; set; } = new List<Voyage>();
}
