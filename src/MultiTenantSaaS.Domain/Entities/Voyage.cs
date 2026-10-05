using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class Voyage : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string VoyageCode { get; set; } = string.Empty; // e.g., "OPT001"
    public Guid? VoyageOrderId { get; set; }

    // Vessel & particulars
    public string VesselName { get; set; } = string.Empty;
    public string? Imo { get; set; }
    public string? VesselType { get; set; }
    public string? Flag { get; set; }
    public string? Dwt { get; set; }
    public int Built { get; set; }
    public string? Loa { get; set; }
    public string? Beam { get; set; }
    public string? EnginePower { get; set; }

    // Route & status
    public string PortFrom { get; set; } = string.Empty;
    public string PortTo { get; set; } = string.Empty;
    public string Status { get; set; } = "At Sea";
    public string Priority { get; set; } = "MEDIUM";
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public string? EtdDisplay { get; set; }
    public string? EtaDisplay { get; set; }
    public string? LastNoon { get; set; }
    public string? RouteRef { get; set; }
    public string? InterimPort { get; set; }

    // Operational telemetry
    public string? Pic { get; set; }
    public string? Client { get; set; }
    public string? ClientEmail { get; set; }
    public string? Service { get; set; }
    public double? CpSpeed { get; set; }
    public double? CpCons { get; set; }
    public double? InstSpeed { get; set; }
    public double? InstCons { get; set; }
    public int Health { get; set; } = 90;
    public string? Remaining { get; set; }
    public int DueLt { get; set; }
    public int DueUtc { get; set; }
    public int OpenTasks { get; set; }
    public string? Tags { get; set; }
    public string? AiAlert { get; set; }
    public string? HandoverNote { get; set; }
    public string OpenStatus { get; set; } = "OPEN";

    // Financials
    public decimal? Price { get; set; }
    public string? PricingBasis { get; set; }
    public double? CostPerDay { get; set; }
    public double? FoCost { get; set; }
    public double? GoCost { get; set; }
    public double? EuaCost { get; set; }

    public Guid? ActivePassageId { get; set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual VoyageOrder? VoyageOrder { get; set; }
    public virtual ICollection<Passage> Passages { get; set; } = new List<Passage>();
}
