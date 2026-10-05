using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Admin;
using MultiTenantSaaS.Application.Features.Admin.Commands;
using MultiTenantSaaS.Application.Features.Admin.Queries;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/admin")]
[Authorize(Roles = UserRoles.SuperAdmin)]
public class AdminController : BaseApiController
{
    /// <summary>
    /// Create a new Employee under the current Admin's Tenant organization.
    /// </summary>
    [HttpPost("employees")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequestDto dto)
    {
        var result = await Mediator.Send(new CreateEmployeeCommand(dto));
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// List all Employees belonging to the current Admin's Tenant organization.
    /// </summary>
    [HttpGet("employees")]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees()
    {
        var result = await Mediator.Send(new GetEmployeesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Update Employee details (Full name, phone number, active state).
    /// </summary>
    [HttpPut("employees/{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequestDto dto)
    {
        var result = await Mediator.Send(new UpdateEmployeeCommand(id, dto));
        return Ok(result);
    }

    /// <summary>
    /// Deactivate an Employee under the current Admin's Tenant.
    /// </summary>
    [HttpDelete("employees/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateEmployee(Guid id)
    {
        var result = await Mediator.Send(new DeactivateEmployeeCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// List all modules enabled for the Admin's tenant organization (granted by SuperAdmin).
    /// </summary>
    [HttpGet("modules")]
    [ProducesResponseType(typeof(ApiResponse<List<AdminTenantModuleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantModules()
    {
        var result = await Mediator.Send(new GetAdminModulesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Assign module-level permissions (View, Create, Edit, Delete) to an Employee.
    /// Only modules already enabled for the Tenant can be assigned.
    /// </summary>
    [HttpPost("employee-permissions")]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeePermissionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetEmployeePermissions([FromBody] SetEmployeeModulePermissionsRequestDto dto)
    {
        var result = await Mediator.Send(new SetEmployeePermissionsCommand(dto));
        return Ok(result);
    }
}
