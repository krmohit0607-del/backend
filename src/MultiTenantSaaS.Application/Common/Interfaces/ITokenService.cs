using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, string role);
    RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress);
    bool ValidateRefreshToken(RefreshToken token);
}
