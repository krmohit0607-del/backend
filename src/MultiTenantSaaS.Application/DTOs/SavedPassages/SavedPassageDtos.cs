namespace MultiTenantSaaS.Application.DTOs.SavedPassages;

public class SavedPassageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FromPort { get; set; }
    public string? ToPort { get; set; }
    public double TypicalDistance { get; set; }
    public double TypicalSpeed { get; set; }
    public double TypicalDays { get; set; }
    public string? RouteJson { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSavedPassageRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FromPort { get; set; }
    public string? ToPort { get; set; }
    public double TypicalDistance { get; set; }
    public double TypicalSpeed { get; set; }
    public double TypicalDays { get; set; }
    public string? RouteJson { get; set; }
}

public class UpdateSavedPassageRequestDto
{
    public string? Name { get; set; }
    public double? TypicalDays { get; set; }
    public bool? IsActive { get; set; }
}
