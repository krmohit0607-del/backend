using Microsoft.AspNetCore.Identity;
using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>, IHasTenant
{
    public string FullName { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? AssignedVesselImo { get; set; }
    public string? AssignedVesselName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual ApplicationUser? CreatedByUser { get; set; }
    public virtual ICollection<ApplicationUser> CreatedUsers { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<EmployeeModulePermission> ModulePermissions { get; set; } = new List<EmployeeModulePermission>();
}
