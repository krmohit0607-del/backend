namespace MultiTenantSaaS.Application.DTOs.Chartering;

public class VoyageEstimateDto
{
    public Guid Id { get; set; }
    public string EstimateNo { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string FixType { get; set; } = "Voyage Charter";
    public string Status { get; set; } = "Draft";
    public double Profit { get; set; }
    public double Tce { get; set; }
    public string? Commodity { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public double Quantity { get; set; }
    public double FreightRate { get; set; }
    public string? DataJson { get; set; }
    public string? BookRef { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateVoyageEstimateRequestDto
{
    public string? Id { get; set; }
    public string? EstimateNo { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string FixType { get; set; } = "Voyage Charter";
    public string Status { get; set; } = "Draft";
    public double Profit { get; set; }
    public double Tce { get; set; }
    public string? Commodity { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public double Quantity { get; set; }
    public double FreightRate { get; set; }
    public string? DataJson { get; set; }
    /// <summary>
    /// If true, this is an auto-save (debounced) and should only update existing records,
    /// not create new ones. New records are only created on explicit save.
    /// </summary>
    public bool IsAutoSave { get; set; } = false;
    /// <summary>
    /// Reference to the cargo or tonnage book entry that created this estimate.
    /// Used to link the estimate back to the book.
    /// </summary>
    public string? BookRef { get; set; }
}

public class CargoBookDto
{
    public Guid Id { get; set; }
    public string CargoCode { get; set; } = string.Empty;
    public string Commodity { get; set; } = string.Empty;
    public string CargoType { get; set; } = "Bulk";
    public string Quantity { get; set; } = string.Empty;
    public string Tolerance { get; set; } = "±5%";
    public string LoadPort { get; set; } = string.Empty;
    public string DischargePort { get; set; } = string.Empty;
    public string? LoadRate { get; set; }
    public string? DischargeRate { get; set; }
    public string? Terms { get; set; }
    public string? LaycanStart { get; set; }
    public string? LaycanEnd { get; set; }
    public string VoyageType { get; set; } = "Voyage Charter";
    public string? OpenDate { get; set; }
    public string? NominationDeadline { get; set; }
    public string CargoStatus { get; set; } = "Open";
    public string CommercialStatus { get; set; } = "Reviewing";
    public string? Pic { get; set; }
    public string EstimationStatus { get; set; } = "Not Created";
    public string? Account { get; set; }
    public string? Remarks { get; set; }
    /// <summary>
    /// ID of the estimate created from this cargo book entry.
    /// When null/empty, no estimate has been created yet.
    /// </summary>
    public string? EstimateId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TonnageBookDto
{
    public Guid Id { get; set; }
    public string TonnageCode { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string? Imo { get; set; }
    public string? VesselType { get; set; }
    public string? Dwt { get; set; }
    public string? Flag { get; set; }
    public string? OpenArea { get; set; }
    public string? OpenPort { get; set; }
    public string? OpenDate { get; set; }
    public string? EarliestOpen { get; set; }
    public string? LatestOpen { get; set; }
    public string VoyageType { get; set; } = "Time Charter";
    public string Source { get; set; } = "Own";
    public string CommercialStatus { get; set; } = "Open";
    public string? Pic { get; set; }
    public string EstimationStatus { get; set; } = "Estimated";
    public string? Owner { get; set; }
    public string? Remarks { get; set; }
    /// <summary>
    /// ID of the estimate created from this tonnage book entry.
    /// When null/empty, no estimate has been created yet.
    /// </summary>
    public string? EstimateId { get; set; }
    public DateTime CreatedAt { get; set; }
}
