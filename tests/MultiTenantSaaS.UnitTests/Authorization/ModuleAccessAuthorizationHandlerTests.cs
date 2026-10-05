using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MultiTenantSaaS.API.Authorization;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Enums;
using MultiTenantSaaS.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Authorization;

public class ModuleAccessAuthorizationHandlerTests
{
    private readonly Mock<ILogger<ModuleAccessAuthorizationHandler>> _loggerMock;

    public ModuleAccessAuthorizationHandlerTests()
    {
        _loggerMock = new Mock<ILogger<ModuleAccessAuthorizationHandler>>();
    }

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task HandleRequirementAsync_SuperAdmin_ShouldAlwaysSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var handler = new ModuleAccessAuthorizationHandler(context, _loggerMock.Object);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.SuperAdmin)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var requirement = new ModuleAccessRequirement("Inventory", ModulePermissionType.Delete);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Admin_WithEnabledTenantModule_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = "Inventory",
            NormalizedName = "INVENTORY",
            IsGlobalActive = true
        };
        context.Modules.Add(module);

        var tenantAccess = new TenantModuleAccess
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ModuleId = module.Id,
            IsEnabled = true,
            GrantedByUserId = Guid.NewGuid()
        };
        context.TenantModuleAccesses.Add(tenantAccess);
        await context.SaveChangesAsync();

        var handler = new ModuleAccessAuthorizationHandler(context, _loggerMock.Object);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin),
            new Claim("tenantId", tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var requirement = new ModuleAccessRequirement("Inventory", ModulePermissionType.Create);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Admin_WithoutTenantModule_ShouldFail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = "Payroll",
            NormalizedName = "PAYROLL",
            IsGlobalActive = true
        };
        context.Modules.Add(module);
        await context.SaveChangesAsync();

        var handler = new ModuleAccessAuthorizationHandler(context, _loggerMock.Object);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin),
            new Claim("tenantId", tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var requirement = new ModuleAccessRequirement("Payroll", ModulePermissionType.View);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Employee_WithGrantedPermission_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var employeeUserId = Guid.NewGuid();

        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = "Reports",
            NormalizedName = "REPORTS",
            IsGlobalActive = true
        };
        context.Modules.Add(module);

        context.TenantModuleAccesses.Add(new TenantModuleAccess
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ModuleId = module.Id,
            IsEnabled = true,
            GrantedByUserId = Guid.NewGuid()
        });

        context.EmployeeModulePermissions.Add(new EmployeeModulePermission
        {
            Id = Guid.NewGuid(),
            EmployeeUserId = employeeUserId,
            ModuleId = module.Id,
            TenantId = tenantId,
            CanView = true,
            CanCreate = false,
            AssignedByUserId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var handler = new ModuleAccessAuthorizationHandler(context, _loggerMock.Object);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, employeeUserId.ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Employee),
            new Claim("tenantId", tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var requirement = new ModuleAccessRequirement("Reports", ModulePermissionType.View);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Employee_LackingCreatePermission_ShouldFail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var tenantId = Guid.NewGuid();
        var employeeUserId = Guid.NewGuid();

        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = "Reports",
            NormalizedName = "REPORTS",
            IsGlobalActive = true
        };
        context.Modules.Add(module);

        context.TenantModuleAccesses.Add(new TenantModuleAccess
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ModuleId = module.Id,
            IsEnabled = true,
            GrantedByUserId = Guid.NewGuid()
        });

        context.EmployeeModulePermissions.Add(new EmployeeModulePermission
        {
            Id = Guid.NewGuid(),
            EmployeeUserId = employeeUserId,
            ModuleId = module.Id,
            TenantId = tenantId,
            CanView = true,
            CanCreate = false, // Does not have create permission
            AssignedByUserId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var handler = new ModuleAccessAuthorizationHandler(context, _loggerMock.Object);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, employeeUserId.ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Employee),
            new Claim("tenantId", tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var requirement = new ModuleAccessRequirement("Reports", ModulePermissionType.Create);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeFalse();
    }
}
