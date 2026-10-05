namespace MultiTenantSaaS.Application.DTOs.WorkflowConfigurations;

public class WorkflowConfigurationDto
{
    public Guid Id { get; set; }
    public string ConfigName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ConfigJson { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWorkflowConfigurationRequestDto
{
    public string ConfigName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ConfigJson { get; set; }
}

public class UpdateWorkflowConfigurationRequestDto
{
    public string? ConfigName { get; set; }
    public string? ConfigJson { get; set; }
    public bool? IsActive { get; set; }
}
