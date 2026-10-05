using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.AgentInvoices;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/agent-invoices")]
[Authorize]
public class AgentInvoicesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AgentInvoicesController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAgentInvoices([FromQuery] Guid? voyageId = null)
    {
        var query = _db.AgentInvoices.AsQueryable();

        if (voyageId.HasValue)
            query = query.Where(a => a.VoyageId == voyageId);

        var invoices = await query.OrderBy(a => a.InvoiceNo).ToListAsync();
        var dtos = invoices.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<AgentInvoiceDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAgentInvoiceById(Guid id)
    {
        var invoice = await _db.AgentInvoices.FirstOrDefaultAsync(a => a.Id == id);
        if (invoice == null)
            return NotFound(ApiResponse<object>.FailureResult("Agent invoice not found"));

        return Ok(ApiResponse<AgentInvoiceDto>.SuccessResult(MapToDto(invoice)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAgentInvoice([FromBody] CreateAgentInvoiceRequestDto dto)
    {
        var invoice = new AgentInvoice
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VoyageId = dto.VoyageId,
            PdaId = dto.PdaId,
            InvoiceNo = dto.InvoiceNo,
            Agent = dto.Agent,
            Vendor = dto.Vendor,
            InvoiceDate = dto.InvoiceDate,
            DueDate = dto.DueDate,
            Currency = dto.Currency ?? "USD",
            Amount = dto.Amount,
            Approved = 0,
            Paid = 0,
            Category = dto.Category ?? "FDA—Port",
            Port = dto.Port,
            DeptStatus = "Pending",
            AccountsStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.AgentInvoices.Add(invoice);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<AgentInvoiceDto>.SuccessResult(MapToDto(invoice)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAgentInvoice(Guid id, [FromBody] UpdateAgentInvoiceRequestDto dto)
    {
        var invoice = await _db.AgentInvoices.FirstOrDefaultAsync(a => a.Id == id);
        if (invoice == null)
            return NotFound(ApiResponse<object>.FailureResult("Agent invoice not found"));

        if (dto.Amount.HasValue)
            invoice.Amount = dto.Amount.Value;
        if (dto.Approved.HasValue)
            invoice.Approved = dto.Approved.Value;
        if (dto.Paid.HasValue)
            invoice.Paid = dto.Paid.Value;
        if (!string.IsNullOrWhiteSpace(dto.DeptStatus))
            invoice.DeptStatus = dto.DeptStatus;
        if (!string.IsNullOrWhiteSpace(dto.AccountsStatus))
            invoice.AccountsStatus = dto.AccountsStatus;
        if (dto.PaymentDate.HasValue)
            invoice.PaymentDate = dto.PaymentDate;

        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedByUserId = _currentUser.UserId;

        _db.AgentInvoices.Update(invoice);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<AgentInvoiceDto>.SuccessResult(MapToDto(invoice)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAgentInvoice(Guid id)
    {
        var invoice = await _db.AgentInvoices.FirstOrDefaultAsync(a => a.Id == id);
        if (invoice == null)
            return NotFound(ApiResponse<object>.FailureResult("Agent invoice not found"));

        _db.AgentInvoices.Remove(invoice);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Agent invoice deleted"));
    }

    private static AgentInvoiceDto MapToDto(AgentInvoice invoice)
    {
        return new AgentInvoiceDto
        {
            Id = invoice.Id,
            VoyageId = invoice.VoyageId,
            InvoiceNo = invoice.InvoiceNo,
            Agent = invoice.Agent,
            Vendor = invoice.Vendor,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Currency = invoice.Currency,
            Amount = invoice.Amount,
            Approved = invoice.Approved,
            Paid = invoice.Paid,
            Category = invoice.Category,
            Port = invoice.Port,
            DeptStatus = invoice.DeptStatus,
            AccountsStatus = invoice.AccountsStatus,
            PaymentDate = invoice.PaymentDate,
            Remarks = invoice.Remarks
        };
    }
}
