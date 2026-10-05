namespace MultiTenantSaaS.Application.DTOs.AreaConstraints;

public class AreaConstraintDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConstraintType { get; set; } = string.Empty;
    public string? GeoJson { get; set; }
    public decimal? MinLatitude { get; set; }
    public decimal? MaxLatitude { get; set; }
    public decimal? MinLongitude { get; set; }
    public decimal? MaxLongitude { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAreaConstraintRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConstraintType { get; set; } = string.Empty; // Exclusion, CanalEntry, IceZone, PiracyRisk
    public string? GeoJson { get; set; }
    public decimal? MinLatitude { get; set; }
    public decimal? MaxLatitude { get; set; }
    public decimal? MinLongitude { get; set; }
    public decimal? MaxLongitude { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateAreaConstraintRequestDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ConstraintType { get; set; }
    public string? GeoJson { get; set; }
    public decimal? MinLatitude { get; set; }
    public decimal? MaxLatitude { get; set; }
    public decimal? MinLongitude { get; set; }
    public decimal? MaxLongitude { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool? IsActive { get; set; }
    public string? Remarks { get; set; }
}
