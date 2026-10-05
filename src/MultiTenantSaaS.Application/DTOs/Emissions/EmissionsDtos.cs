namespace MultiTenantSaaS.Application.DTOs.Emissions;

public class EmissionsRecordDto
{
    public Guid Id { get; set; }
    public string VoyageCode { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string ComplianceYear { get; set; } = string.Empty;
    public string? Trade { get; set; }
    public string EuaPriceEur { get; set; } = "72.50";
    public string Co2AdjustmentT { get; set; } = "0";
    public string? ComplianceJson { get; set; }
    public string? AdjustmentsJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SaveEmissionsRecordRequestDto
{
    public string VoyageCode { get; set; } = string.Empty;
    public string? VesselName { get; set; }
    public string? ComplianceYear { get; set; }
    public string? Trade { get; set; }
    public string? EuaPriceEur { get; set; }
    public string? Co2AdjustmentT { get; set; }
    public string? ComplianceJson { get; set; }
    public string? AdjustmentsJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedDate { get; set; }
}

public class EmissionsScenarioDto
{
    public Guid Id { get; set; }
    public string VoyageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? InputsJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SaveEmissionsScenarioRequestDto
{
    public string VoyageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? InputsJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByName { get; set; }
}

