namespace MultiTenantSaaS.Application.DTOs.Employee;

public class EmployeeModuleAccessDto
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
