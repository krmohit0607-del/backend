namespace MultiTenantSaaS.Application.DTOs.Voyages;

public class VoyageDto
{
    public Guid Id { get; set; }
    public string VoyageCode { get; set; } = string.Empty;
    public Guid? VoyageOrderId { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string? Imo { get; set; }
    public string? VesselType { get; set; }
    public string? Flag { get; set; }
    public string? Dwt { get; set; }
    public int Built { get; set; }
    public string? Loa { get; set; }
    public string? Beam { get; set; }
    public string? EnginePower { get; set; }
    public string PortFrom { get; set; } = string.Empty;
    public string PortTo { get; set; } = string.Empty;
    public string Status { get; set; } = "At Sea";
    public string Priority { get; set; } = "MEDIUM";
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public string? EtdDisplay { get; set; }
    public string? EtaDisplay { get; set; }
    public string? LastNoon { get; set; }
    public string? RouteRef { get; set; }
    public string? InterimPort { get; set; }
    public string? Pic { get; set; }
    public string? Client { get; set; }
    public string? ClientEmail { get; set; }
    public string? Service { get; set; }
    public double? CpSpeed { get; set; }
    public double? CpCons { get; set; }
    public double? InstSpeed { get; set; }
    public double? InstCons { get; set; }
    public int Health { get; set; } = 90;
    public string? Remaining { get; set; }
    public int DueLt { get; set; }
    public int DueUtc { get; set; }
    public int OpenTasks { get; set; }
    public string? Tags { get; set; }
    public string? AiAlert { get; set; }
    public string? HandoverNote { get; set; }
    public string OpenStatus { get; set; } = "OPEN";
    public decimal? Price { get; set; }
    public string? PricingBasis { get; set; }
    public double? CostPerDay { get; set; }
    public double? FoCost { get; set; }
    public double? GoCost { get; set; }
    public double? EuaCost { get; set; }
    public Guid? ActivePassageId { get; set; }
    public List<PassageDto> Passages { get; set; } = new();
}

public class PassageDto
{
    public Guid Id { get; set; }
    public Guid VoyageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RouteRef { get; set; }
    public string? InterimPort { get; set; }
    public double? TotalDistanceNm { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; }
    public List<PassageLegDto> Legs { get; set; } = new();
}

public class PassageLegDto
{
    public Guid Id { get; set; }
    public Guid PassageId { get; set; }
    public int Sequence { get; set; }
    public string? Type { get; set; }
    public string FromPort { get; set; } = string.Empty;
    public string ToPort { get; set; } = string.Empty;
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public double? DistanceNm { get; set; }
    public double? Speed { get; set; }
    public string Status { get; set; } = "Planned";
}

public class VoyageOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string Service { get; set; } = "PMO";
    public string Priority { get; set; } = "MEDIUM";
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<VoyageDto> Voyages { get; set; } = new();
}

public class CreateVoyageRequestDto
{
    public string? VoyageCode { get; set; }
    public Guid? VoyageOrderId { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string? Imo { get; set; }
    public string? VesselType { get; set; }
    public string? Flag { get; set; }
    public string? PortFrom { get; set; }
    public string? PortTo { get; set; }
    public string? Status { get; set; } = "At Sea";
    public string? Priority { get; set; } = "MEDIUM";
    public string? EtdDisplay { get; set; }
    public string? EtaDisplay { get; set; }
    public string? Client { get; set; }
    public string? Service { get; set; }
    public double? CpSpeed { get; set; }
    public double? CpCons { get; set; }
    public double? InstSpeed { get; set; }
    public double? InstCons { get; set; }
    public string? HandoverNote { get; set; }
    public string? Tags { get; set; }
    public string? AiAlert { get; set; }
    public int Health { get; set; } = 90;
}

public class UpdateVoyageRequestDto
{
    public string? VesselName { get; set; }
    public string? PortFrom { get; set; }
    public string? PortTo { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Client { get; set; }
    public string? Service { get; set; }
    public string? EtdDisplay { get; set; }
    public string? EtaDisplay { get; set; }
    public double? CpSpeed { get; set; }
    public double? CpCons { get; set; }
    public double? InstSpeed { get; set; }
    public double? InstCons { get; set; }
    public string? HandoverNote { get; set; }
    public string? Tags { get; set; }
    public string? AiAlert { get; set; }
    public int? Health { get; set; }
    public decimal? Price { get; set; }
    public string? PricingBasis { get; set; }
    public double? CostPerDay { get; set; }
    public double? FoCost { get; set; }
    public double? GoCost { get; set; }
    public double? EuaCost { get; set; }
}

public class CreateVoyageOrderRequestDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? Service { get; set; } = "PMO";
    public string? Priority { get; set; } = "MEDIUM";
    public string? Status { get; set; } = "Active";
    public string? Notes { get; set; }
}
