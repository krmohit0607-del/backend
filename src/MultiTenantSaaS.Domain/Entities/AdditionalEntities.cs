using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

/// <summary>
/// Client — Account record for owners, charterers, brokers, operators, and service providers.
/// Supports multi-tenancy with per-tenant client database.
/// </summary>
public class Client : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    public string Kind { get; set; } = string.Empty; // Account, ServiceProvider
    public string Category { get; set; } = string.Empty; // Owner, Charterer, Broker, Operator, or service type
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? WebsiteUrl { get; set; }

    // --- Login/Access ---
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? Role { get; set; } // Administrator, Manager, User, Viewer
    public bool IsActive { get; set; } = true;

    // --- Internal Assignment ---
    public string? PicAssignment { get; set; } // ODAS PIC (Principal In Charge)

    // --- Banking Details ---
    public string? BankName { get; set; }
    public string? AccountHolder { get; set; }
    public string? AccountNumber { get; set; }
    public string? Swift { get; set; }
    public string? Iban { get; set; }
    public bool BankAccountVerified { get; set; }

    // --- Metadata ---
    public string? ExternalId { get; set; } // Reference to external system
    public string? ComplianceStatus { get; set; } // Verified, Pending, Rejected
    public DateTime? ComplianceCheckDate { get; set; }
    public string? Notes { get; set; }

    public virtual ICollection<ClientContact> Contacts { get; set; } = new List<ClientContact>();
}

/// <summary>
/// Client Contact — Individual contact persons for a client account.
/// </summary>
public class ClientContact : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid ClientId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; } // Operations Manager, Finance Manager, etc.
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Client Client { get; set; } = null!;
}

/// <summary>
/// Laytime Calculation — Detailed laytime and demurrage tracking for voyage.
/// </summary>
public class LaytimeCalculation : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid VoyageId { get; set; }

    // --- CP Terms ---
    public string? LaytimeTerms { get; set; } // SHINC, SHEX, SHEX EIU, WWD, Reversible, etc.
    public decimal LaytimeDaysAllowed { get; set; }

    // --- Actual Dates ---
    public DateTime? NorTendered { get; set; } // Notice of Readiness
    public DateTime? NorAccepted { get; set; }
    public DateTime? DischCommenced { get; set; }
    public DateTime? DischCompleted { get; set; }

    // --- Calculation Fields ---
    public decimal DaysUsed { get; set; }
    public decimal DaysAllowed { get; set; }
    public decimal WeatherDelay { get; set; }
    public decimal ShiftingDelay { get; set; }
    public decimal ExceptedDelay { get; set; } // Exceptions per CP
    public decimal NetDemurragedays { get; set; }

    // --- Demurrage/Despatch ---
    public string? Currency { get; set; } = "USD";
    public decimal DemurrageRate { get; set; }
    public decimal DemurrageAmount { get; set; }
    public decimal DespatchRate { get; set; }
    public decimal DespatchEarning { get; set; }
    public decimal NetDemurrage { get; set; }

    // --- Status ---
    public string Status { get; set; } = "Pending"; // Pending, Calculated, Settled
    public DateTime? CalculatedDate { get; set; }
    public string? CalculatedBy { get; set; }
    public string? Remarks { get; set; }

    public virtual Voyage Voyage { get; set; } = null!;
}

/// <summary>
/// Email Template — Customizable email templates with tokens/variables.
/// </summary>
public class EmailTemplate : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Chartering, Operations, Bunker, etc.
    public string? SubCategory { get; set; }
    public string? SubSubCategory { get; set; }
    public string Description { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyPlaintext { get; set; }

    // --- Recipients ---
    public string? DefaultTo { get; set; } // Comma-separated emails or token placeholders
    public string? DefaultCc { get; set; }
    public string? DefaultBcc { get; set; }

    // --- Metadata ---
    public string? RecipientType { get; set; } // Account, Vessel, ServiceProvider, etc.
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } // System template vs. user-created
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? AvailableTokens { get; set; } // JSON array of available token names

    public virtual ICollection<EmailDistributionList> DistributionLists { get; set; } = new List<EmailDistributionList>();
}

/// <summary>
/// Email Distribution List — Group of recipients for bulk email sending.
/// </summary>
public class EmailDistributionList : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Recipients { get; set; } // JSON array of email strings or company references
    public bool IsActive { get; set; } = true;

    public virtual ICollection<EmailTemplate> EmailTemplates { get; set; } = new List<EmailTemplate>();
}

/// <summary>
/// Enumeration Value — Standardized master data for dropdowns (voyage types, port types, fuel grades, etc.).
/// Enables per-tenant customization of business values.
/// </summary>
public class EnumerationValue : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    public string EnumerationType { get; set; } = string.Empty; // VoyageType, PortType, LaytimeTerm, etc.
    public string EnumKey { get; set; } = string.Empty; // Unique code within enum type
    public string EnumValue { get; set; } = string.Empty; // Display value
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } // System default vs. custom

    // Composite unique key per tenant
}

/// <summary>
/// Cargo Master — Reference database of cargo types with properties and classifications.
/// </summary>
public class CargoMaster : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    public string CargoCode { get; set; } = string.Empty;
    public string CargoName { get; set; } = string.Empty;
    public string? Category { get; set; } // Dry Bulk, Liquid Bulk, Gas, Container, etc.
    public string? SubCategory { get; set; }
    public string? Description { get; set; }

    // --- IMO Classification ---
    public string? UnNumber { get; set; }
    public string? ImoClassification { get; set; }
    public string? ImsbcGroup { get; set; }
    public string? IbcClassification { get; set; }
    public string? IgcClassification { get; set; }

    // --- Physical Properties ---
    public decimal? DensityMin { get; set; }
    public decimal? DensityMax { get; set; }
    public string? DensityUnit { get; set; } // t/m³, kg/m³
    public string? StowageFactor { get; set; }
    public string? HygroscopicRating { get; set; }
    public string? VentilationRequirement { get; set; }
    public string? TemperatureControl { get; set; }

    // --- Vessel Requirements ---
    public string? SuitableVesselTypes { get; set; } // JSON array
    public string? ProhibitedVesselTypes { get; set; } // JSON array
    public string? Compatibility { get; set; } // Compatibility with other cargoes

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Area Constraint — Geospatial boundary definitions (exclusion zones, canal restrictions, etc.).
/// </summary>
public class AreaConstraint : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConstraintType { get; set; } = string.Empty; // Exclusion, CanalEntry, IceZone, PiracyRisk, etc.
    
    // Geospatial data (WKT or GeoJSON)
    public string? GeoJson { get; set; }
    
    // Bounding box
    public decimal? MinLatitude { get; set; }
    public decimal? MaxLatitude { get; set; }
    public decimal? MinLongitude { get; set; }
    public decimal? MaxLongitude { get; set; }
    
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }
}

/// <summary>
/// Saved Passage — Template voyage leg route for reuse in voyage planning.
/// </summary>
public class SavedPassage : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Route definition
    public string? FromPort { get; set; }
    public string? ToPort { get; set; }
    public string? RouteJson { get; set; } // JSON of legs with waypoints
    
    public decimal? TypicalDistance { get; set; }
    public decimal? TypicalSpeed { get; set; }
    public decimal? TypicalDays { get; set; }
    
    // Metadata
    public int TimesUsed { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Workflow Configuration — Business rule flags and status transition rules per tenant.
/// </summary>
public class WorkflowConfiguration : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }

    public string ConfigKey { get; set; } = string.Empty; // Key name
    public string ConfigValue { get; set; } = string.Empty; // JSON or plain value
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Examples: "AllowedVoyageStatuses", "RequiredFieldsForSettlement", "ApprovalChainRules"
}
