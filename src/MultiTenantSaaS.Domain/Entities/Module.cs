using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class Module : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGlobalActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<TenantModuleAccess> TenantModuleAccesses { get; set; } = new List<TenantModuleAccess>();
    public virtual ICollection<EmployeeModulePermission> EmployeePermissions { get; set; } = new List<EmployeeModulePermission>();
}
