using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class AuditLog : BaseEntity, IHasTenant
{
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string Action { get; set; } = string.Empty; // E.g., "Create", "Update", "Delete", "Login"
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual Tenant? Tenant { get; set; }
}
