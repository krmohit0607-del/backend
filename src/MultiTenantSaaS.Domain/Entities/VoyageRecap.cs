using MultiTenantSaaS.Domain.Common;

namespace MultiTenantSaaS.Domain.Entities;

/// <summary>
/// Voyage Operations Recap — Charter Party settlement document with vessel particulars,
/// commercial terms, cargo specifications, and legal/regulatory details.
/// </summary>
public class VoyageRecap : BaseAuditableEntity, IHasTenant
{
    public Guid? TenantId { get; set; }
    public Guid VoyageId { get; set; }

    // --- Charter Party Metadata ---
    public string? VoyageFixType { get; set; } // Time Charter, Voyage Charter
    public DateTime? CpDate { get; set; } // Charter party date
    public string? CpReference { get; set; } // CP reference number
    public string? CharterPartyReference { get; set; } // Alias for CpReference
    public string? PdaNo { get; set; } // Pro Forma Disbursement Account number
    public string? FdaNo { get; set; } // Final Disbursement Account number
    public string? Status { get; set; } = "Pending"; // Pending, Settled, Archived

    // --- Settlement Calculations ---
    public decimal Laytime { get; set; }
    public decimal Demurrage { get; set; }
    public decimal Despatch { get; set; }
    public decimal NetResultShip { get; set; }

    // --- VESSEL PARTICULARS ---
    public string? VesselName { get; set; }
    public string? VesselEmail { get; set; }
    public string? VesselLoa { get; set; } // Length overall
    public string? VesselBeam { get; set; }
    public string? DraftBallast { get; set; }
    public string? DraftLaden { get; set; }
    public string? VesselAge { get; set; }
    public string? VesselClass { get; set; }
    public string? EngineRpmMin { get; set; }
    public string? EngineRpmMax { get; set; }
    public string? EngineMcrMin { get; set; }
    public string? EngineMcrMax { get; set; }
    public string? ScrubberFitted { get; set; } // Yes/No
    public string? ScrubberType { get; set; }

    // --- CARGO HANDLING ---
    public string? CraneCount { get; set; }
    public string? CraneSwl { get; set; } // Safe working load
    public string? CraneSafeLimit { get; set; }
    public string? GrabCount { get; set; }
    public string? GrabWeight { get; set; }
    public string? GrabSafeLimit { get; set; }

    // --- OWNER SIDE (Commercial Terms) ---
    public string? Owners { get; set; }
    public DateTime? OwnersCpDate { get; set; }
    public DateTime? OwnersLaycanStart { get; set; }
    public DateTime? OwnersLaycanEnd { get; set; }
    public string? OwnersBroker { get; set; }

    // --- CHARTERER SIDE ---
    public string? Charterers { get; set; }
    public DateTime? CharterersCpDate { get; set; }
    public DateTime? CharterersLaycanStart { get; set; }
    public DateTime? CharterersLaycanEnd { get; set; }
    public string? CharterersBroker { get; set; }

    // --- HIRE TERMS ---
    public decimal? HirePerDay { get; set; }
    public string? DemDespatch { get; set; } // Demurrage/despatch terms
    public string? DespatchTerm { get; set; }
    public string? DeliveryPort { get; set; }
    public string? DeliveryTerm { get; set; }
    public DateTime? DeliveryDateTime { get; set; }

    // --- REDELIVERY ---
    public string? RedeliveryPort { get; set; }
    public string? RedeliveryTerm { get; set; }
    public DateTime? RedeliveryDateTime { get; set; }
    public string? DeliveryNotices { get; set; }

    // --- CARGO & HOLD ---
    public string? CargoName { get; set; }
    public decimal? CpQuantity { get; set; }
    public string? HoldCleaning { get; set; }
    public decimal? FinalQtyLoaded { get; set; }

    // --- IMO/SAFETY CLAUSES ---
    public string? Ilohc { get; set; } // International Load Line Harmonization Code
    public string? Cve { get; set; } // Container Vessel Exemption
    public string? Adcom { get; set; } // Dangerous goods

    // --- COMMERCIAL CLAUSES ---
    public string? WxClause { get; set; } // Weather clause
    public string? BrokerageRate { get; set; }
    public string? PniClub { get; set; }

    // --- ARBITRATION & LEGAL ---
    public string? ArbitrationPlace { get; set; }
    public string? GoverningLaw { get; set; }
    public string? SanctionsClause { get; set; }

    // --- FREIGHT SETTLEMENT ---
    public decimal? FreightPerMt { get; set; }
    public string? BallastBonus { get; set; }
    public string? HullCleaningClause { get; set; }
    public string? RedeliveryNotices { get; set; }

    // --- NAVIGATION ---
    public virtual Voyage Voyage { get; set; } = null!;
}
