using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Vessels;
using MultiTenantSaaS.Application.Features.Vessels.Commands;
using MultiTenantSaaS.Application.Features.Vessels.Queries;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/vessels")]
[Authorize]
public class VesselsController : BaseApiController
{
    /// <summary>
    /// List all vessels registered under caller's tenant organization.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VesselDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVessels()
    {
        var result = await Mediator.Send(new GetVesselsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get vessel details and field change history by ID or IMO.
    /// </summary>
    [HttpGet("{idOrImo}")]
    [ProducesResponseType(typeof(ApiResponse<VesselDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVessel(string idOrImo)
    {
        var result = await Mediator.Send(new GetVesselByIdQuery(idOrImo));
        return Ok(result);
    }

    /// <summary>
    /// Register a new vessel under the caller's tenant fleet.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<VesselDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVessel([FromBody] CreateVesselRequestDto dto)
    {
        var result = await Mediator.Send(new CreateVesselCommand(dto));
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Update vessel particulars and automatically record field change log.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse<VesselDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVessel(Guid id, [FromBody] UpdateVesselRequestDto dto)
    {
        var result = await Mediator.Send(new UpdateVesselCommand(id, dto));
        return Ok(result);
    }

    /// <summary>
    /// Deactivate a vessel in the tenant's registry.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVessel(Guid id)
    {
        var result = await Mediator.Send(new DeleteVesselCommand(id));
        return Ok(result);
    }
}
