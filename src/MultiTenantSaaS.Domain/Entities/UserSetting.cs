namespace MultiTenantSaaS.Domain.Entities;

public class UserSetting
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
    public Tenant? Tenant { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}
