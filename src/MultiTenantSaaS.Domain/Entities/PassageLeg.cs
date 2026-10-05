using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class PassageLeg : BaseEntity
{
    public Guid PassageId { get; set; }
    public int Sequence { get; set; }
    public string? Type { get; set; } // D, RD, B, L
    public string FromPort { get; set; } = string.Empty;
    public string ToPort { get; set; } = string.Empty;
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public double? DistanceNm { get; set; }
    public double? Speed { get; set; }
    public string Status { get; set; } = "Planned";

    // Navigation property
    public virtual Passage? Passage { get; set; }
}
