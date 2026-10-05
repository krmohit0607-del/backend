using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.ProFormaDisbursements;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/pro-forma-disbursements")]
[Authorize]
public class ProFormaDisbursementsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ProFormaDisbursementsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetProFormaDisbursements([FromQuery] Guid? voyageId = null)
    {
        var query = _db.ProFormaDisbursements.Include(p => p.InvoiceItems).AsQueryable();

        if (voyageId.HasValue)
            query = query.Where(p => p.VoyageId == voyageId);

        var pdas = await query.OrderBy(p => p.PdaNo).ToListAsync();
        var dtos = pdas.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ProFormaDisbursementDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProFormaDisbursementById(Guid id)
    {
        var pda = await _db.ProFormaDisbursements
            .Include(p => p.InvoiceItems)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pda == null)
            return NotFound(ApiResponse<object>.FailureResult("PDA not found"));

        return Ok(ApiResponse<ProFormaDisbursementDto>.SuccessResult(MapToDto(pda)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProFormaDisbursement([FromBody] CreateProFormaDisbursementRequestDto dto)
    {
        var pda = new ProFormaDisbursement
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VoyageId = dto.VoyageId,
            PdaNo = dto.PdaNo,
            Port = dto.Port,
            Agent = dto.Agent,
            Currency = dto.Currency ?? "USD",
            Estimated = dto.Estimated,
            Advance = dto.Advance,
            FdaFinal = 0,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.ProFormaDisbursements.Add(pda);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<ProFormaDisbursementDto>.SuccessResult(MapToDto(pda)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProFormaDisbursement(Guid id, [FromBody] UpdateProFormaDisbursementRequestDto dto)
    {
        var pda = await _db.ProFormaDisbursements
            .Include(p => p.InvoiceItems)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pda == null)
            return NotFound(ApiResponse<object>.FailureResult("PDA not found"));

        if (!string.IsNullOrWhiteSpace(dto.Port))
            pda.Port = dto.Port;
        if (dto.Estimated.HasValue)
            pda.Estimated = dto.Estimated.Value;
        if (dto.Advance.HasValue)
            pda.Advance = dto.Advance.Value;
        if (!string.IsNullOrWhiteSpace(dto.Status))
            pda.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.Approval))
            pda.Approval = dto.Approval;

        pda.UpdatedAt = DateTime.UtcNow;
        pda.UpdatedByUserId = _currentUser.UserId;

        _db.ProFormaDisbursements.Update(pda);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<ProFormaDisbursementDto>.SuccessResult(MapToDto(pda)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProFormaDisbursement(Guid id)
    {
        var pda = await _db.ProFormaDisbursements.FirstOrDefaultAsync(p => p.Id == id);
        if (pda == null)
            return NotFound(ApiResponse<object>.FailureResult("PDA not found"));

        _db.ProFormaDisbursements.Remove(pda);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("PDA deleted successfully"));
    }

    private static ProFormaDisbursementDto MapToDto(ProFormaDisbursement pda)
    {
        return new ProFormaDisbursementDto
        {
            Id = pda.Id,
            VoyageId = pda.VoyageId,
            PdaNo = pda.PdaNo,
            Port = pda.Port,
            Agent = pda.Agent,
            Currency = pda.Currency,
            Estimated = pda.Estimated,
            Advance = pda.Advance,
            FdaFinal = pda.FdaFinal,
            Status = pda.Status,
            Approval = pda.Approval,
            ApprovedDate = pda.ApprovedDate,
            ApprovedBy = pda.ApprovedBy,
            Remarks = pda.Remarks,
            InvoiceItems = pda.InvoiceItems?.Select(i => new AgentInvoiceDto
            {
                Id = i.Id,
                VoyageId = i.VoyageId,
                InvoiceNo = i.InvoiceNo,
                Agent = i.Agent,
                Amount = i.Amount,
                Approved = i.Approved,
                Paid = i.Paid,
                DeptStatus = i.DeptStatus
            }).ToList() ?? new()
        };
    }
}
