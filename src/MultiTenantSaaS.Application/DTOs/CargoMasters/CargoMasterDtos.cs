namespace MultiTenantSaaS.Application.DTOs.CargoMasters;

public class CargoMasterDto
{
    public Guid Id { get; set; }
    public string CargoCode { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImoClass { get; set; }
    public string Hazard { get; set; }
    public double? Density { get; set; }
    public double? Viscosity { get; set; }
    public double? FlashPoint { get; set; }
    public string StorageConditions { get; set; }
    public string Compatibility { get; set; }
    public string VesselSuitability { get; set; }
    public string SpecialRequirements { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCargoMasterRequestDto
{
    public string CargoCode { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImoClass { get; set; }
    public string Hazard { get; set; }
    public double? Density { get; set; }
    public double? Viscosity { get; set; }
    public double? FlashPoint { get; set; }
    public string StorageConditions { get; set; }
    public string Compatibility { get; set; }
    public string VesselSuitability { get; set; }
    public string SpecialRequirements { get; set; }
}

public class UpdateCargoMasterRequestDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImoClass { get; set; }
    public string Hazard { get; set; }
    public double? Density { get; set; }
    public double? Viscosity { get; set; }
    public double? FlashPoint { get; set; }
    public string StorageConditions { get; set; }
    public string Compatibility { get; set; }
    public string VesselSuitability { get; set; }
    public string SpecialRequirements { get; set; }
    public bool? IsActive { get; set; }
}
