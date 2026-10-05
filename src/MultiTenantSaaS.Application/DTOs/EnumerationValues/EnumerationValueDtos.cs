namespace MultiTenantSaaS.Application.DTOs.EnumerationValues;

public class EnumerationValueDto
{
    public Guid Id { get; set; }
    public string EnumerationType { get; set; }
    public string EnumKey { get; set; }
    public string EnumValue { get; set; }
    public int? SortOrder { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateEnumerationValueRequestDto
{
    public string EnumerationType { get; set; }
    public string EnumKey { get; set; }
    public string EnumValue { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsSystem { get; set; }
}

public class UpdateEnumerationValueRequestDto
{
    public string EnumValue { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsSystem { get; set; }
}
