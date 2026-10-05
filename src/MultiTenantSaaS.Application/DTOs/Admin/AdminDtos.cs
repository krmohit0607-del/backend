namespace MultiTenantSaaS.Application.DTOs.Admin;

public class CreateEmployeeRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public List<EmployeePermissionInputDto>? Permissions { get; set; }
}

public class UpdateEmployeeRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}

public class EmployeeDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<EmployeePermissionDto> Permissions { get; set; } = new();
}

public class EmployeePermissionInputDto
{
    public Guid ModuleId { get; set; }
    public bool CanView { get; set; } = true;
    public bool CanCreate { get; set; } = false;
    public bool CanEdit { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}

public class SetEmployeeModulePermissionsRequestDto
{
    public Guid EmployeeUserId { get; set; }
    public List<EmployeePermissionInputDto> Permissions { get; set; } = new();
}

public class EmployeePermissionDto
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class AdminTenantModuleDto
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabledForTenant { get; set; }
    public bool IsGlobalActive { get; set; }
}
