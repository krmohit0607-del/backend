using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.SuperAdmin;
using MultiTenantSaaS.Application.Features.SuperAdmin.Commands;
using MultiTenantSaaS.Application.Features.SuperAdmin.Queries;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/superadmin")]
[Authorize(Roles = UserRoles.SuperAdmin)]
public class SuperAdminController : BaseApiController
{
    /// <summary>
    /// Create a new Admin and Organization (Tenant) with optional initial modules.
    /// </summary>
    [HttpPost("admins")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequestDto dto)
    {
        var result = await Mediator.Send(new CreateAdminCommand(dto));
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// List all Admins and Tenant organizations in the platform.
    /// </summary>
    [HttpGet("admins")]
    [ProducesResponseType(typeof(ApiResponse<List<TenantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdmins()
    {
        var result = await Mediator.Send(new GetAdminsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Activate, deactivate or update the subscription status of a Tenant.
    /// </summary>
    [HttpPut("admins/{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenantStatus(Guid id, [FromBody] UpdateTenantStatusRequestDto dto)
    {
        var result = await Mediator.Send(new UpdateTenantStatusCommand(id, dto));
        return Ok(result);
    }

    /// <summary>
    /// Permanently delete a Tenant and all associated data (users, module access, etc).
    /// </summary>
    [HttpDelete("admins/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        var result = await Mediator.Send(new DeleteTenantCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// List all system-wide modules.
    /// </summary>
    [HttpGet("modules")]
    [ProducesResponseType(typeof(ApiResponse<List<ModuleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModules()
    {
        var result = await Mediator.Send(new GetModulesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Create a new platform module.
    /// </summary>
    [HttpPost("modules")]
    [ProducesResponseType(typeof(ApiResponse<ModuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequestDto dto)
    {
        var result = await Mediator.Send(new CreateModuleCommand(dto));
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Grant or revoke a module to a specific Tenant.
    /// </summary>
    [HttpPost("tenant-module-access")]
    [ProducesResponseType(typeof(ApiResponse<TenantModuleAccessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTenantModuleAccess([FromBody] SetTenantModuleAccessRequestDto dto)
    {
        var result = await Mediator.Send(new SetTenantModuleAccessCommand(dto));
        return Ok(result);
    }

    /// <summary>
    /// View what modules are currently assigned to a specific Tenant.
    /// </summary>
    [HttpGet("tenant-module-access/{tenantId}")]
    [ProducesResponseType(typeof(ApiResponse<List<TenantModuleAccessDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantModuleAccess(Guid tenantId)
    {
        var result = await Mediator.Send(new GetTenantModuleAccessQuery(tenantId));
        return Ok(result);
    }

    [HttpGet("tenants/{tenantId}/users")]
    public async Task<IActionResult> GetTenantUsers(Guid tenantId)
        => Ok(await Mediator.Send(new GetTenantUsersQuery(tenantId)));

    [HttpPost("tenants/users")]
    public async Task<IActionResult> CreateTenantUser([FromBody] CreateTenantUserRequestDto dto)
        => StatusCode(StatusCodes.Status201Created, await Mediator.Send(new CreateTenantUserCommand(dto)));

    [HttpPut("tenants/users/{userId}/permissions")]
    public async Task<IActionResult> SetTenantUserPermissions(Guid userId, [FromBody] TenantUserPermissionDto dto)
    {
        dto.UserId = userId;
        return Ok(await Mediator.Send(new SetTenantUserPermissionsCommand(dto)));
    }

    [HttpPut("tenants/users/{userId}")]
    public async Task<IActionResult> UpdateTenantUser(Guid userId, [FromBody] UpdateTenantUserRequestDto dto)
        => Ok(await Mediator.Send(new UpdateTenantUserCommand(userId, dto)));

    [HttpDelete("tenants/users/{userId}")]
    public async Task<IActionResult> DeleteTenantUser(Guid userId)
        => Ok(await Mediator.Send(new DeleteTenantUserCommand(userId)));
}
