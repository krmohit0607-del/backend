namespace MultiTenantSaaS.Application.DTOs.Passages;

public class CreatePassageRequestDto
{
    public Guid VoyageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RouteRef { get; set; }
    public string? InterimPort { get; set; }
    public double? TotalDistanceNm { get; set; }
    public string? Status { get; set; } = "Active";
    public bool SetAsActive { get; set; }
    public List<CreatePassageLegRequestDto> Legs { get; set; } = new();
}

public class UpdatePassageRequestDto
{
    public string? Name { get; set; }
    public string? RouteRef { get; set; }
    public string? InterimPort { get; set; }
    public double? TotalDistanceNm { get; set; }
    public string? Status { get; set; }
}

public class CreatePassageLegRequestDto
{
    public int Sequence { get; set; }
    public string? Type { get; set; }
    public string FromPort { get; set; } = string.Empty;
    public string ToPort { get; set; } = string.Empty;
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public double? DistanceNm { get; set; }
    public double? Speed { get; set; }
    public string? Status { get; set; } = "Planned";
}

public class UpdatePassageLegRequestDto
{
    public string? Type { get; set; }
    public string? FromPort { get; set; }
    public string? ToPort { get; set; }
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public double? DistanceNm { get; set; }
    public double? Speed { get; set; }
    public string? Status { get; set; }
}
