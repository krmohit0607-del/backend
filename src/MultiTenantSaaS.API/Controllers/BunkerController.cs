using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Bunker;
using MultiTenantSaaS.Application.Features.Bunker.Commands;
using MultiTenantSaaS.Application.Features.Bunker.Queries;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/bunker")]
[Authorize]
public class BunkerController : BaseApiController
{
    /// <summary>
    /// List all bunker fuel requirements and procurement cycles for caller's tenant.
    /// </summary>
    [HttpGet("requirements")]
    [ProducesResponseType(typeof(ApiResponse<List<BunkerRequirementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequirements()
    {
        var result = await Mediator.Send(new GetBunkerRequirementsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get a single bunker requirement by ID or RequirementNo (e.g., BR-2606-024).
    /// </summary>
    [HttpGet("requirements/{idOrNo}")]
    [ProducesResponseType(typeof(ApiResponse<BunkerRequirementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequirement(string idOrNo)
    {
        var result = await Mediator.Send(new GetBunkerRequirementByIdQuery(idOrNo));
        return Ok(result);
    }

    /// <summary>
    /// Raise a new bunker procurement requirement.
    /// </summary>
    [HttpPost("requirements")]
    [ProducesResponseType(typeof(ApiResponse<BunkerRequirementDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRequirement([FromBody] CreateBunkerRequirementRequestDto dto)
    {
        var result = await Mediator.Send(new CreateBunkerRequirementCommand(dto));
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Update bunker procurement lifecycle, quote selection, BDN receipt or invoice approval.
    /// </summary>
    [HttpPut("requirements/{id}")]
    [ProducesResponseType(typeof(ApiResponse<BunkerRequirementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRequirement(Guid id, [FromBody] UpdateBunkerRequirementRequestDto dto)
    {
        var result = await Mediator.Send(new UpdateBunkerRequirementCommand(id, dto));
        return Ok(result);
    }

    /// <summary>
    /// Delete a bunker requirement.
    /// </summary>
    [HttpDelete("requirements/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRequirement(Guid id)
    {
        var result = await Mediator.Send(new DeleteBunkerRequirementCommand(id));
        return Ok(result);
    }
}
