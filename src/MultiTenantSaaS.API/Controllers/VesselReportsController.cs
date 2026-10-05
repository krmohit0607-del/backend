using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.VesselReports;
using MultiTenantSaaS.Application.Features.VesselReports.Commands;
using MultiTenantSaaS.Application.Features.VesselReports.Queries;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/vessel-reports")]
[Authorize]
public class VesselReportsController : BaseApiController
{
    /// <summary>
    /// List all vessel reports (Noon, Departure, Arrival, BOSP, EOSP) for caller's tenant.
    /// Filterable by IMO or VoyageCode.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VesselReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports([FromQuery] string? imo, [FromQuery] string? voyageCode)
    {
        var result = await Mediator.Send(new GetVesselReportsQuery(imo, voyageCode));
        return Ok(result);
    }

    /// <summary>
    /// Get the vessel's actual voyage progress (current / next port + stage) derived from
    /// the latest vessel report. Used to mark the current position on the voyage timeline.
    /// </summary>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(ApiResponse<VoyageProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVoyageProgress([FromQuery] string? imo, [FromQuery] string? voyageCode)
    {
        var result = await Mediator.Send(new GetVoyageProgressQuery(imo, voyageCode));
        return Ok(result);
    }

    /// <summary>
    /// Get single vessel report by ID or Report Number (e.g., VR-2609-001).
    /// </summary>
    [HttpGet("{idOrNo}")]
    [ProducesResponseType(typeof(ApiResponse<VesselReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(string idOrNo)
    {
        var result = await Mediator.Send(new GetVesselReportByIdQuery(idOrNo));
        return Ok(result);
    }

    /// <summary>
    /// Submit a vessel report (telemetry, noon logs, fuel consumptions/ROB, weather observations).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VesselReportDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitVesselReportRequestDto dto)
    {
        var result = await Mediator.Send(new SubmitVesselReportCommand(dto));
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
