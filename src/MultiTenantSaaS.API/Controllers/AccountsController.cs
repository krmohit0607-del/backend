using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Accounts;
using MultiTenantSaaS.Application.Features.Accounts.Commands;
using MultiTenantSaaS.Application.Features.Accounts.Queries;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/accounts")]
[Authorize]
public class AccountsController : BaseApiController
{
    /// <summary>
    /// List all financial transactions, payable invoices and receivable claims for caller's tenant.
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(ApiResponse<List<FinancialTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions()
    {
        var result = await Mediator.Send(new GetTransactionsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get single financial transaction record by ID or Transaction Number (e.g., TXN-4401).
    /// </summary>
    [HttpGet("transactions/{idOrNo}")]
    [ProducesResponseType(typeof(ApiResponse<FinancialTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(string idOrNo)
    {
        var result = await Mediator.Send(new GetTransactionByIdQuery(idOrNo));
        return Ok(result);
    }

    /// <summary>
    /// Create a financial transaction entry (Hire, Freight, Bunker, PDA, FDA, Claims, etc.).
    /// </summary>
    [HttpPost("transactions")]
    [ProducesResponseType(typeof(ApiResponse<FinancialTransactionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateFinancialTransactionRequestDto dto)
    {
        var result = await Mediator.Send(new CreateTransactionCommand(dto));
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Update transaction workflow status, approval state, execution or reconciliation details.
    /// </summary>
    [HttpPut("transactions/{id}")]
    [ProducesResponseType(typeof(ApiResponse<FinancialTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateFinancialTransactionRequestDto dto)
    {
        var result = await Mediator.Send(new UpdateTransactionCommand(id, dto));
        return Ok(result);
    }
}
