using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Emissions;
using MultiTenantSaaS.Application.Features.Emissions.Commands;
using MultiTenantSaaS.Application.Features.Emissions.Queries;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/emissions")]
[Authorize]
public class EmissionsController : BaseApiController
{
    /// <summary>
    /// List all emissions & regulatory compliance records for caller's tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmissionsRecordDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEmissions()
    {
        var result = await Mediator.Send(new GetAllEmissionsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get emissions compliance document by Voyage Code (e.g., OPT001).
    /// </summary>
    [HttpGet("{voyageCode}")]
    [ProducesResponseType(typeof(ApiResponse<EmissionsRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmissionsByVoyage(string voyageCode)
    {
        var result = await Mediator.Send(new GetEmissionsByVoyageQuery(voyageCode));
        return Ok(result);
    }

    /// <summary>
    /// Save, update or approve an emissions & compliance document.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmissionsRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveEmissionsRecord([FromBody] SaveEmissionsRecordRequestDto dto)
    {
        var result = await Mediator.Send(new SaveEmissionsRecordCommand(dto));
        return Ok(result);
    }

    /// <summary>
    /// List saved "Report Studio" editable scenarios for a voyage.
    /// </summary>
    [HttpGet("scenarios/{voyageCode}")]
    [ProducesResponseType(typeof(ApiResponse<List<EmissionsScenarioDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScenarios(string voyageCode)
    {
        var result = await Mediator.Send(new GetEmissionsScenariosQuery(voyageCode));
        return Ok(result);
    }

    /// <summary>
    /// Create a new "Report Studio" editable scenario.
    /// </summary>
    [HttpPost("scenarios")]
    [ProducesResponseType(typeof(ApiResponse<EmissionsScenarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateScenario([FromBody] SaveEmissionsScenarioRequestDto dto)
    {
        var result = await Mediator.Send(new SaveEmissionsScenarioCommand(dto, null));
        return Ok(result);
    }

    /// <summary>
    /// Update an existing "Report Studio" editable scenario.
    /// </summary>
    [HttpPut("scenarios/{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmissionsScenarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateScenario(Guid id, [FromBody] SaveEmissionsScenarioRequestDto dto)
    {
        var result = await Mediator.Send(new SaveEmissionsScenarioCommand(dto, id));
        return Ok(result);
    }

    /// <summary>
    /// Delete a "Report Studio" editable scenario.
    /// </summary>
    [HttpDelete("scenarios/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteScenario(Guid id)
    {
        var result = await Mediator.Send(new DeleteEmissionsScenarioCommand(id));
        return Ok(result);
    }
}
