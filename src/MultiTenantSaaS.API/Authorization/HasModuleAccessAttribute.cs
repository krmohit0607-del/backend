using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MultiTenantSaaS.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class HasModuleAccessAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "ModuleAccess_";

    public HasModuleAccessAttribute(string moduleName, ModulePermissionType permissionType = ModulePermissionType.View)
    {
        Policy = $"{PolicyPrefix}{moduleName}_{permissionType}";
    }
}

public class ModuleAccessPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public ModuleAccessPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasModuleAccessAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var raw = policyName.Substring(HasModuleAccessAttribute.PolicyPrefix.Length);
            var parts = raw.Split('_');

            var moduleName = parts[0];
            var permissionType = ModulePermissionType.View;

            if (parts.Length > 1 && Enum.TryParse<ModulePermissionType>(parts[1], out var parsedType))
            {
                permissionType = parsedType;
            }

            var policy = new AuthorizationPolicyBuilder();
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new ModuleAccessRequirement(moduleName, permissionType));

            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
