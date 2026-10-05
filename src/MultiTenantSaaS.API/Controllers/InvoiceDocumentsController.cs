using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.Services;

namespace MultiTenantSaaS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/invoice-documents")]
public class InvoiceDocumentsController : ControllerBase
{
    public record ExtractRequest(string Kind, string Text);

    [HttpPost("extract")]
    [RequestSizeLimit(300_000)]
    public IActionResult Extract([FromBody] ExtractRequest request)
    {
        if (request.Kind is not ("PDA" or "FDA" or "Agent" or "Service" or "Freight" or "Demurrage"))
            return BadRequest("Unsupported invoice type.");
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 100_000)
            return BadRequest("Document text must contain between 1 and 100000 characters.");
        return Ok(ApiResponse<Dictionary<string, object>>.SuccessResult(InvoiceDocumentParser.Extract(request.Text, request.Kind)));
    }
}