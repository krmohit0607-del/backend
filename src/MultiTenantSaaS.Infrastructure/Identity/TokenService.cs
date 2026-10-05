using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Identity;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(ApplicationUser user, string role)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? "SuperSecretEnterpriseJwtSigningKeyWithSufficientLengthForHS256Bit!";
        var issuer = _configuration["Jwt:Issuer"] ?? "MultiTenantSaaS";
        var audience = _configuration["Jwt:Audience"] ?? "MultiTenantSaaS.Client";
        var expiryMinutes = double.TryParse(_configuration["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("userId", user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.TenantId.HasValue)
        {
            var tenantIdValue = user.TenantId.Value.ToString();
            System.Console.WriteLine($"[DEBUG] TokenService.GenerateAccessToken - Adding tenantId claim: {tenantIdValue} for user {user.Email}");
            claims.Add(new Claim("tenantId", tenantIdValue));
        }
        else
        {
            System.Console.WriteLine($"[DEBUG] TokenService.GenerateAccessToken - User {user.Email} has NO TenantId");
        }

        if (!string.IsNullOrEmpty(user.AssignedVesselImo))
        {
            claims.Add(new Claim("vesselImo", user.AssignedVesselImo));
        }

        if (!string.IsNullOrEmpty(user.AssignedVesselName))
        {
            claims.Add(new Claim("vesselName", user.AssignedVesselName));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress)
    {
        var days = double.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var val) ? val : 7;

        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(randomNumber),
            ExpiresAt = DateTime.UtcNow.AddDays(days),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }

    public bool ValidateRefreshToken(RefreshToken token)
    {
        return token.IsActive;
    }
}
