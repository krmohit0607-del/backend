using MultiTenantSaaS.Domain.Common;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public Guid AdminUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ApplicationUser? AdminUser { get; set; }
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<TenantModuleAccess> TenantModuleAccesses { get; set; } = new List<TenantModuleAccess>();
}
