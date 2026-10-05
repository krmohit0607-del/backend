using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class Passage : BaseEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid VoyageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RouteRef { get; set; }
    public string? InterimPort { get; set; }
    public double? TotalDistanceNm { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual Voyage? Voyage { get; set; }
    public virtual ICollection<PassageLeg> Legs { get; set; } = new List<PassageLeg>();
}
