namespace MultiTenantSaaS.Application.Features.Settings.Commands;

public class SaveImoShipsRequest
{
    public List<ImoShipDto> Ships { get; set; } = new();
}

public class ImoShipDto
{
    public string? Imo { get; set; }
    public string? Name { get; set; }
    public string? VesselType { get; set; }
    public string? Statcode5 { get; set; }
    public string? Statcode5Desc { get; set; }
    public string? BuilderName { get; set; }
    public string? BuilderCountry { get; set; }
    public string? BuiltYear { get; set; }
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
    public string? EngineBuilder { get; set; }
    public string? EngineDesign { get; set; }
    public string? EngineModel { get; set; }
    public string? EnginesRpm { get; set; }
    public string? TotalKwMainEng { get; set; }
    public string? FuelConsMainEng { get; set; }
    public string? AuxEngineTotalKw { get; set; }
    public string? GeneratorsKw { get; set; }
    public string? ClassSociety { get; set; }
    public string? Flag { get; set; }
    public string? Owner { get; set; }
    public string? Operator { get; set; }
}

public class SavePortsRequest
{
    public List<PortDto> Ports { get; set; } = new();
}

public class PortDto
{
    public string? PortName { get; set; }
    public string? PortCode { get; set; }
    public string? UnLocode { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PortType { get; set; }
    public bool IsRiver { get; set; }
    public bool IsCanalEntrance { get; set; }
    public string? Facilities { get; set; }
    public string? Remarks { get; set; }
}
