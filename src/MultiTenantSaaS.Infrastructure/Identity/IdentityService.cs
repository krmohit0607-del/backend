using Microsoft.AspNetCore.Identity;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser?> FindByIdAsync(Guid userId)
    {
        return await _userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<(bool Success, string[] Errors, ApplicationUser User)> CreateUserAsync(
        string email,
        string fullName,
        string password,
        string role,
        Guid? tenantId = null,
        Guid? createdByUserId = null,
        string? phoneNumber = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            TenantId = tenantId,
            CreatedByUserId = createdByUserId,
            PhoneNumber = phoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description).ToArray(), user);
        }

        // Ensure role exists
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new ApplicationRole(role));
        }

        await _userManager.AddToRoleAsync(user, role);

        return (true, Array.Empty<string>(), user);
    }

    public async Task<string> GetUserRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? string.Empty;
    }

    public async Task<bool> UpdateUserAsync(ApplicationUser user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> SetUserActiveStatusAsync(Guid userId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}
