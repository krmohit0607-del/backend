using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Auth;
using MultiTenantSaaS.Application.Features.Auth.Commands;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Enums;
using MultiTenantSaaS.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;

    public LoginCommandHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _auditServiceMock = new Mock<IAuditService>();
    }

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnAuthResponseWithTokens()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, CompanyName = "Acme Corp", IsActive = true };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Admin User",
            Email = "admin@acme.com",
            TenantId = tenantId,
            IsActive = true
        };

        var loginDto = new LoginRequestDto { Email = "admin@acme.com", Password = "Password123!" };

        _identityServiceMock.Setup(s => s.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
        _identityServiceMock.Setup(s => s.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
        _identityServiceMock.Setup(s => s.GetUserRoleAsync(user)).ReturnsAsync(UserRoles.Admin);

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user, UserRoles.Admin)).Returns("mocked-jwt-access-token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken(user.Id, It.IsAny<string>())).Returns(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "mocked-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });

        var handler = new LoginCommandHandler(
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object);

        // Act
        var response = await handler.Handle(new LoginCommand(loginDto), CancellationToken.None);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.AccessToken.Should().Be("mocked-jwt-access-token");
        response.Data.RefreshToken.Should().Be("mocked-refresh-token");
        response.Data.Role.Should().Be(UserRoles.Admin);
        response.Data.CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task Handle_InvalidPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Admin User",
            Email = "admin@acme.com",
            IsActive = true
        };

        var loginDto = new LoginRequestDto { Email = "admin@acme.com", Password = "WrongPassword!" };

        _identityServiceMock.Setup(s => s.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
        _identityServiceMock.Setup(s => s.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(false);

        var handler = new LoginCommandHandler(
            _identityServiceMock.Object,
            _tokenServiceMock.Object,
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new LoginCommand(loginDto), CancellationToken.None));
    }
}
