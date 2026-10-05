using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Enums;
using MultiTenantSaaS.Infrastructure.Identity;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:SecretKey", "SuperSecretEnterpriseJwtSigningKeyWithSufficientLengthForHS256Bit!"},
            {"Jwt:Issuer", "MultiTenantSaaS"},
            {"Jwt:Audience", "MultiTenantSaaS.Client"},
            {"Jwt:ExpiryMinutes", "30"},
            {"Jwt:RefreshTokenExpiryDays", "7"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeExpectedClaims()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = "jane@tenant.com",
            TenantId = tenantId
        };
        var role = UserRoles.Admin;

        // Act
        var tokenString = _tokenService.GenerateAccessToken(user, role);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        jwtToken.Issuer.Should().Be("MultiTenantSaaS");
        jwtToken.Audiences.Should().Contain("MultiTenantSaaS.Client");

        jwtToken.Claims.Should().Contain(c => c.Type == "userId" && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
        jwtToken.Claims.Should().Contain(c => c.Type == "tenantId" && c.Value == tenantId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnActiveTokenWithCorrectExpiry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ip = "127.0.0.1";

        // Act
        var refreshToken = _tokenService.GenerateRefreshToken(userId, ip);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.UserId.Should().Be(userId);
        refreshToken.Token.Should().NotBeNullOrWhiteSpace();
        refreshToken.CreatedByIp.Should().Be(ip);
        refreshToken.IsActive.Should().BeTrue();
        refreshToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));
    }
}
