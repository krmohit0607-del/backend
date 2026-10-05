using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.FinalDisbursements;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/final-disbursements")]
[Authorize]
public class FinalDisbursementsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FinalDisbursementsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetFinalDisbursements([FromQuery] Guid? voyageId = null)
    {
        var query = _db.FinalDisbursements.AsQueryable();

        if (voyageId.HasValue)
            query = query.Where(f => f.VoyageId == voyageId);

        var fdas = await query.OrderBy(f => f.FdaNo).ToListAsync();
        var dtos = fdas.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<FinalDisbursementDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFinalDisbursementById(Guid id)
    {
        var fda = await _db.FinalDisbursements.FirstOrDefaultAsync(f => f.Id == id);
        if (fda == null)
            return NotFound(ApiResponse<object>.FailureResult("FDA not found"));

        return Ok(ApiResponse<FinalDisbursementDto>.SuccessResult(MapToDto(fda)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateFinalDisbursement([FromBody] CreateFinalDisbursementRequestDto dto)
    {
        var fda = new FinalDisbursement
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VoyageId = dto.VoyageId,
            RelatedPdaId = dto.RelatedPdaId,
            FdaNo = dto.FdaNo,
            Port = dto.Port,
            Agent = dto.Agent,
            Currency = dto.Currency ?? "USD",
            FdaAmount = dto.FdaAmount,
            PdaAdvance = dto.PdaAdvance,
            BalancePayable = dto.BalancePayable,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.FinalDisbursements.Add(fda);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<FinalDisbursementDto>.SuccessResult(MapToDto(fda)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFinalDisbursement(Guid id, [FromBody] UpdateFinalDisbursementRequestDto dto)
    {
        var fda = await _db.FinalDisbursements.FirstOrDefaultAsync(f => f.Id == id);
        if (fda == null)
            return NotFound(ApiResponse<object>.FailureResult("FDA not found"));

        if (dto.FdaAmount.HasValue)
            fda.FdaAmount = dto.FdaAmount.Value;
        if (dto.PdaAdvance.HasValue)
            fda.PdaAdvance = dto.PdaAdvance.Value;
        if (dto.BalancePayable.HasValue)
            fda.BalancePayable = dto.BalancePayable.Value;
        if (!string.IsNullOrWhiteSpace(dto.Status))
            fda.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.Approval))
            fda.Approval = dto.Approval;

        fda.UpdatedAt = DateTime.UtcNow;
        fda.UpdatedByUserId = _currentUser.UserId;

        _db.FinalDisbursements.Update(fda);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<FinalDisbursementDto>.SuccessResult(MapToDto(fda)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFinalDisbursement(Guid id)
    {
        var fda = await _db.FinalDisbursements.FirstOrDefaultAsync(f => f.Id == id);
        if (fda == null)
            return NotFound(ApiResponse<object>.FailureResult("FDA not found"));

        _db.FinalDisbursements.Remove(fda);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("FDA deleted"));
    }

    private static FinalDisbursementDto MapToDto(FinalDisbursement fda)
    {
        return new FinalDisbursementDto
        {
            Id = fda.Id,
            VoyageId = fda.VoyageId,
            FdaNo = fda.FdaNo,
            Port = fda.Port,
            Agent = fda.Agent,
            Currency = fda.Currency,
            FdaAmount = fda.FdaAmount,
            PdaAdvance = fda.PdaAdvance,
            BalancePayable = fda.BalancePayable,
            Status = fda.Status,
            Approval = fda.Approval,
            ReceivedDate = fda.ReceivedDate,
            ApprovedDate = fda.ApprovedDate,
            Remarks = fda.Remarks
        };
    }
}
