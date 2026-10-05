using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class TenantModuleAccess : BaseEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid ModuleId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Guid GrantedByUserId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual Module? Module { get; set; }
    public virtual ApplicationUser? GrantedByUser { get; set; }
}
