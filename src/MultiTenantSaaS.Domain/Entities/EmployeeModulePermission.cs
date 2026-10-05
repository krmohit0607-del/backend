using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class EmployeeModulePermission : BaseEntity, IHasTenant
{
    public Guid EmployeeUserId { get; set; }
    public Guid ModuleId { get; set; }
    public Guid? TenantId { get; set; }
    public bool CanView { get; set; } = true;
    public bool CanCreate { get; set; } = false;
    public bool CanEdit { get; set; } = false;
    public bool CanDelete { get; set; } = false;
    public Guid AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ApplicationUser? EmployeeUser { get; set; }
    public virtual Module? Module { get; set; }
    public virtual Tenant? Tenant { get; set; }
    public virtual ApplicationUser? AssignedByUser { get; set; }
}
