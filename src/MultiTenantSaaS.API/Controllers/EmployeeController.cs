using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Employee;
using MultiTenantSaaS.Application.Features.Employee.Queries;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/employee")]
[Authorize(Roles = $"{UserRoles.Employee},{UserRoles.Admin},{UserRoles.SuperAdmin}")]
public class EmployeeController : BaseApiController
{
    /// <summary>
    /// List modules and permissions assigned to the current logged-in employee.
    /// </summary>
    [HttpGet("my-modules")]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeModuleAccessDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyModules()
    {
        var result = await Mediator.Send(new GetMyModulesQuery());
        return Ok(result);
    }
}
