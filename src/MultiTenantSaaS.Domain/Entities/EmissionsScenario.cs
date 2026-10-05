using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

/// <summary>
/// A named, user-editable "what-if" snapshot of a voyage's emissions figures — lives alongside the
/// real (read-only) <see cref="EmissionsRecord"/> so operators can freely override values and
/// regenerate a report without ever touching the voyage's actual calculated data.
/// </summary>
public class EmissionsScenario : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public string VoyageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>The editable override field values the user typed (raw inputs).</summary>
    public string? InputsJson { get; set; }
    /// <summary>The recalculated snapshot derived from InputsJson, for reporting/audit.</summary>
    public string? MetricsJson { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByName { get; set; }

    public virtual Tenant? Tenant { get; set; }
}
