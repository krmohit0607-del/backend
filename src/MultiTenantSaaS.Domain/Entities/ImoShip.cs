using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

/// <summary>
/// IMO Ship Reference Data — stores bundled IMO ship-search database (~63k ships).
/// Used as master data lookup for vessel information.
/// </summary>
public class ImoShip : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    // --- Identity ---
    public string Imo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // --- Type & Classification ---
    public string? VesselType { get; set; }
    public string? Statcode5 { get; set; }
    public string? Statcode5Desc { get; set; }

    // --- Builder ---
    public string? BuilderName { get; set; }
    public string? BuilderCountry { get; set; }
    public string? BuilderCode { get; set; }
    public string? BuilderTown { get; set; }
    public string? BuiltYear { get; set; }
    public string? StandardDesign { get; set; }

    // --- Dimensions ---
    public string? Gt { get; set; }
    public string? LengthBp { get; set; }
    public string? LengthOverall { get; set; }
    public string? Depth { get; set; }
    public string? BreadthMoulded { get; set; }
    public string? Deadweight { get; set; }
    public string? Displacement { get; set; }
    public string? Draught { get; set; }
    public string? HullType { get; set; }
    public string? Holds { get; set; }
    public string? Teu { get; set; }
    public string? GasCapacity { get; set; }

    // --- Engine ---
    public string? EngineBuilder { get; set; }
    public string? EngineDesign { get; set; }
    public string? EngineModel { get; set; }
    public string? EnginesRpm { get; set; }
    public string? TotalKwMainEng { get; set; }
    public string? FuelConsMainEng { get; set; }
    public string? AuxEngineTotalKw { get; set; }
    public string? GeneratorsKw { get; set; }

    // --- Classification & Registration ---
    public string? ClassSociety { get; set; }
    public string? Flag { get; set; }
    public string? Owner { get; set; }
    public string? Operator { get; set; }

    // --- Raw JSON for additional fields ---
    public string? AdditionalDataJson { get; set; }

    // --- Navigation ---
    public virtual Tenant? Tenant { get; set; }
}
