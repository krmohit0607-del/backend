using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class CargoBookEntry : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string CargoCode { get; set; } = string.Empty; // e.g. CG-2608-001
    public string Commodity { get; set; } = string.Empty;
    public string CargoType { get; set; } = "Bulk";
    public string Quantity { get; set; } = string.Empty;
    public string Tolerance { get; set; } = "±5%";
    public string LoadPort { get; set; } = string.Empty;
    public string DischargePort { get; set; } = string.Empty;
    public string? LoadRate { get; set; }
    public string? DischargeRate { get; set; }
    public string? Terms { get; set; }
    public string? LaycanStart { get; set; }
    public string? LaycanEnd { get; set; }
    public string VoyageType { get; set; } = "Voyage Charter";
    public string? OpenDate { get; set; }
    public string? NominationDeadline { get; set; }
    public string CargoStatus { get; set; } = "Open";
    public string CommercialStatus { get; set; } = "Reviewing";
    public string? Pic { get; set; }
    public string EstimationStatus { get; set; } = "Not Created";
    public string? Account { get; set; }
    public string? Remarks { get; set; }
    public string? EstimateId { get; set; } // ID of the voyage estimate created from this cargo book entry

    // Navigation property
    public virtual Tenant? Tenant { get; set; }
}

public class TonnageBookEntry : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string TonnageCode { get; set; } = string.Empty; // e.g. TN-2608-001
    public string VesselName { get; set; } = string.Empty;
    public string? Imo { get; set; }
    public string? VesselType { get; set; }
    public string? Dwt { get; set; }
    public string? Flag { get; set; }
    public string? OpenArea { get; set; }
    public string? OpenPort { get; set; }
    public string? OpenDate { get; set; }
    public string? EarliestOpen { get; set; }
    public string? LatestOpen { get; set; }
    public string VoyageType { get; set; } = "Time Charter";
    public string Source { get; set; } = "Own";
    public string CommercialStatus { get; set; } = "Open";
    public string? Pic { get; set; }
    public string EstimationStatus { get; set; } = "Estimated";
    public string? Owner { get; set; }
    public string? Remarks { get; set; }
    public string? EstimateId { get; set; } // ID of the voyage estimate created from this tonnage book entry

    // Navigation property
    public virtual Tenant? Tenant { get; set; }
}
