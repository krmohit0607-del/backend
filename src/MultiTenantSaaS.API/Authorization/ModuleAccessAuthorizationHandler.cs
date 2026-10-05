using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.API.Authorization;

public class ModuleAccessAuthorizationHandler : AuthorizationHandler<ModuleAccessRequirement>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ModuleAccessAuthorizationHandler> _logger;

    public ModuleAccessAuthorizationHandler(
        IApplicationDbContext context,
        ILogger<ModuleAccessAuthorizationHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ModuleAccessRequirement requirement)
    {
        var user = context.User;
        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            _logger.LogWarning("Authorization failed: User is unauthenticated.");
            return;
        }

        var role = user.FindFirstValue(ClaimTypes.Role);
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("userId") ?? user.FindFirstValue("sub");
        var tenantIdClaim = user.FindFirstValue("tenantId");

        // 1. SuperAdmin has unrestricted access to all modules
        if (string.Equals(role, UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Authorization failed: Invalid UserId claim.");
            return;
        }

        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            _logger.LogWarning("Authorization failed: Non-SuperAdmin user {UserId} lacks TenantId claim.", userId);
            return;
        }

        var normalizedModuleName = requirement.ModuleName.Trim().ToUpperInvariant();

        // Check if module is globally active in the system
        var module = await _context.Modules
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.NormalizedName == normalizedModuleName);

        if (module == null || !module.IsGlobalActive)
        {
            _logger.LogWarning("Authorization failed: Module '{ModuleName}' is either nonexistent or globally disabled.", requirement.ModuleName);
            return;
        }

        // Gate 1: Check if tenant has module enabled by SuperAdmin
        var isTenantModuleEnabled = await _context.TenantModuleAccesses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(t => t.TenantId == tenantId && t.ModuleId == module.Id && t.IsEnabled);

        if (!isTenantModuleEnabled)
        {
            _logger.LogWarning("Authorization failed: Tenant {TenantId} does not have module '{ModuleName}' enabled.", tenantId, requirement.ModuleName);
            return;
        }

        // 2. Admin role: if tenant has access, Admin has full CRUD access to this module
        if (string.Equals(role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }

        // 3. Employee role: Gate 2: check employee-level granular permission
        if (string.Equals(role, UserRoles.Employee, StringComparison.OrdinalIgnoreCase))
        {
            var permission = await _context.EmployeeModulePermissions
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.EmployeeUserId == userId && p.ModuleId == module.Id && p.TenantId == tenantId);

            if (permission == null)
            {
                _logger.LogWarning("Authorization failed: Employee {UserId} has no permissions assigned for module '{ModuleName}'.", userId, requirement.ModuleName);
                return;
            }

            var hasPermission = requirement.PermissionType switch
            {
                ModulePermissionType.View => permission.CanView,
                ModulePermissionType.Create => permission.CanCreate,
                ModulePermissionType.Edit => permission.CanEdit,
                ModulePermissionType.Delete => permission.CanDelete,
                _ => false
            };

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Authorization failed: Employee {UserId} lacks '{Permission}' permission on module '{ModuleName}'.",
                    userId, requirement.PermissionType, requirement.ModuleName);
            }
        }
    }
}
