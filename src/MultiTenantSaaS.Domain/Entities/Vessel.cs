using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

public class Vessel : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    // --- Identity ---
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string Imo { get; set; } = string.Empty;
    public string? Mmsi { get; set; }
    public string? Email { get; set; }
    public string? IceClass { get; set; }

    // --- Type ---
    public string? Statcode5 { get; set; }
    public string? Statcode5Desc { get; set; }
    public string? VesselType { get; set; }

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
    public string? SternLoading { get; set; }
    public string? InertGasSystem { get; set; }
    public string? KeelLaid { get; set; }
    public string? KeelToMastHeight { get; set; }
    public string? LinesPerSide { get; set; }
    public string? ParallelBodyLength { get; set; }
    public string? RoroLanesLength { get; set; }

    // --- Engine ---
    public string? EngineBuilder { get; set; }
    public string? EngineDesign { get; set; }
    public string? EngineModel { get; set; }
    public string? EnginesRpm { get; set; }
    public string? TotalKwMainEng { get; set; }
    public string? FuelConsMainEng { get; set; }
    public string? AuxEngineTotalKw { get; set; }
    public string? GeneratorsKw { get; set; }
    public string? ThrustersTotalKw { get; set; }
    public string? ServiceSpeed { get; set; }

    // --- Commercial / other ---
    public string? Flag { get; set; }
    public string? Owner { get; set; }
    public string? Operator { get; set; }
    public string? ClassSociety { get; set; }
    public bool IsActive { get; set; } = true;

    // --- Performance profile (Vessel Profile tab — Create Voyage / Performance module) ---
    // Saved once per vessel so operators don't have to re-enter it on every new voyage;
    // the Vessel Profile tab prefills from here and writes edits back. Where a concept
    // already exists above (email, LOA/beam, displacement/deadweight, engine model) that
    // same column is reused instead of duplicating it here.
    public string? EcdisModel { get; set; }
    public string? AutoSendForecast { get; set; }
    public string? AutoSendForecastTime { get; set; }
    public string? Weather4x { get; set; }
    public string? Weather4xDuration { get; set; }
    public string? AutoSendReports { get; set; }
    public string? Scrubber { get; set; }
    public string? ScrubberType { get; set; }
    public string? MeType { get; set; }
    public string? DefaultBallastDraft { get; set; }
    public string? DefaultLadenDraft { get; set; }
    public string? SummerDraft { get; set; }

    // Engine limits & constraints
    public string? MinRpm { get; set; }
    public string? MaxRpm { get; set; }
    public string? MinMcr { get; set; }
    public string? MaxMcr { get; set; }
    public string? MinSpeed { get; set; }
    public string? MaxSpeed { get; set; }
    public string? MinPowerFraction { get; set; }
    public string? MaxPowerFraction { get; set; }
    public string? NominalPowerFraction { get; set; }
    public string? BlowerBallastMin { get; set; }
    public string? BlowerBallastMax { get; set; }
    public string? BlowerLadenMin { get; set; }
    public string? BlowerLadenMax { get; set; }
    public string? CriticalRpmMin { get; set; }
    public string? CriticalRpmMax { get; set; }

    // Telegraph table (RPM + Speed per order, ballast/laden)
    public string? DeadSlowRpm { get; set; }
    public string? SlowAheadRpm { get; set; }
    public string? HalfAheadRpm { get; set; }
    public string? FullAheadRpm { get; set; }
    public string? DeadSlowSpeedBallast { get; set; }
    public string? DeadSlowSpeedLaden { get; set; }
    public string? SlowAheadSpeedBallast { get; set; }
    public string? SlowAheadSpeedLaden { get; set; }
    public string? HalfAheadSpeedBallast { get; set; }
    public string? HalfAheadSpeedLaden { get; set; }
    public string? FullAheadSpeedBallast { get; set; }
    public string? FullAheadSpeedLaden { get; set; }

    // Weather safety limits (ballast / laden)
    public string? WslMaxSwhBallast { get; set; }
    public string? WslMaxSwhLaden { get; set; }
    public string? WslMaxWindsBallast { get; set; }
    public string? WslMaxWindsLaden { get; set; }
    public string? WslMaxSeaStateBallast { get; set; }
    public string? WslMaxSeaStateLaden { get; set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<VesselHistory> History { get; set; } = new List<VesselHistory>();
}
