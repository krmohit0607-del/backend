using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class VesselHistory : BaseEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid VesselId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? FromValue { get; set; }
    public string? ToValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Vessel? Vessel { get; set; }
    public virtual Tenant? Tenant { get; set; }
}
