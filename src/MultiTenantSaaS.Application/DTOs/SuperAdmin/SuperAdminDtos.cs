using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.Application.DTOs.SuperAdmin;

public class CreateAdminRequestDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string? AdminPhoneNumber { get; set; }
    public List<Guid>? InitialModuleIds { get; set; }
}

public class TenantDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid AdminUserId { get; set; }
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActiveUsersCount { get; set; }
    public int EnabledModulesCount { get; set; }
}

public class UpdateTenantStatusRequestDto
{
    public bool IsActive { get; set; }
    public SubscriptionStatus? SubscriptionStatus { get; set; }
}

public class CreateModuleRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGlobalActive { get; set; } = true;
}

public class ModuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGlobalActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SetTenantModuleAccessRequestDto
{
    public Guid TenantId { get; set; }
    public Guid ModuleId { get; set; }
    public bool IsEnabled { get; set; }
}

public class TenantModuleAccessDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? ModuleDescription { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsGlobalActive { get; set; }
    public DateTime GrantedAt { get; set; }
    public string GrantedByUserName { get; set; } = string.Empty;
}

public class CreateTenantUserRequestDto
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Employee;
    public List<Guid> InitialModuleIds { get; set; } = new();
    public string? PhoneNumber { get; set; }
    public string? AssignedVesselImo { get; set; }
    public string? AssignedVesselName { get; set; }
}

public class UpdateTenantUserRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AssignedVesselImo { get; set; }
    public string? AssignedVesselName { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TenantUserDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Employee;
    public string? PhoneNumber { get; set; }
    public string? AssignedVesselImo { get; set; }
    public string? AssignedVesselName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TenantUserPermissionViewDto> Permissions { get; set; } = new();
}

public class TenantUserPermissionDto
{
    public Guid UserId { get; set; }
    public List<EmployeePermissionInputDto> Permissions { get; set; } = new();
}

public class EmployeePermissionInputDto
{
    public Guid ModuleId { get; set; }
    public bool CanView { get; set; } = true;
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class TenantUserPermissionViewDto : EmployeePermissionInputDto
{
    public string ModuleName { get; set; } = string.Empty;
}
