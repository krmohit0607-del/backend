using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

/// <summary>
/// Port Reference Data — stores bundled World Port Index (~3,669 ports from NGA WPI).
/// Used as master data lookup for port information.
/// </summary>
public class Port : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    // --- Identity ---
    public string PortName { get; set; } = string.Empty;
    public string PortCode { get; set; } = string.Empty;  // e.g., unique identifier
    public string? UnLocode { get; set; }  // UN/LOCODE code

    // --- Location ---
    public string Country { get; set; } = string.Empty;
    public string? Region { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // --- Type & Classification ---
    public string? PortType { get; set; }  // e.g., "Major", "Regional", etc.
    public bool IsRiver { get; set; }
    public bool IsCanalEntrance { get; set; }

    // --- Port Capabilities/Facilities (as delimited string or JSON) ---
    public string? Facilities { get; set; }  // e.g., "Containers|Bulk|Breakbulk"
    public string? Remarks { get; set; }

    // --- Raw JSON for additional fields ---
    public string? AdditionalDataJson { get; set; }

    // --- Navigation ---
    public virtual Tenant? Tenant { get; set; }
}
