using Microsoft.AspNetCore.Authorization;

namespace MultiTenantSaaS.API.Authorization;

public enum ModulePermissionType
{
    View = 1,
    Create = 2,
    Edit = 3,
    Delete = 4
}

public class ModuleAccessRequirement : IAuthorizationRequirement
{
    public string ModuleName { get; }
    public ModulePermissionType PermissionType { get; }

    public ModuleAccessRequirement(string moduleName, ModulePermissionType permissionType = ModulePermissionType.View)
    {
        ModuleName = moduleName;
        PermissionType = permissionType;
    }
}
