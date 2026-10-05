using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<ApplicationUser?> FindByEmailAsync(string email);
    Task<ApplicationUser?> FindByIdAsync(Guid userId);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task<(bool Success, string[] Errors, ApplicationUser User)> CreateUserAsync(
        string email, 
        string fullName, 
        string password, 
        string role, 
        Guid? tenantId = null, 
        Guid? createdByUserId = null,
        string? phoneNumber = null);
    Task<string> GetUserRoleAsync(ApplicationUser user);
    Task<bool> UpdateUserAsync(ApplicationUser user);
    Task<bool> SetUserActiveStatusAsync(Guid userId, bool isActive);
}
