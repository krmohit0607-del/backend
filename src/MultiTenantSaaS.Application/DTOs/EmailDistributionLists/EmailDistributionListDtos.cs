namespace MultiTenantSaaS.Application.DTOs.EmailDistributionLists;

public class EmailDistributionListDto
{
    public Guid Id { get; set; }
    public string ListName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Recipients { get; set; } = new();
    public bool IsActive { get; set; }
}

public class CreateEmailDistributionListRequestDto
{
    public string ListName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string>? Recipients { get; set; }
}

public class UpdateEmailDistributionListRequestDto
{
    public string? ListName { get; set; }
    public List<string>? Recipients { get; set; }
    public bool? IsActive { get; set; }
}
