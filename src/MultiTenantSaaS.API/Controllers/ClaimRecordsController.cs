using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.ClaimRecords;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/claim-records")]
[Authorize]
public class ClaimRecordsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClaimRecordsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetClaimRecords([FromQuery] string? voyageId = null, [FromQuery] string? status = null)
    {
        var query = _db.ClaimRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(voyageId))
            query = query.Where(c => c.VoyageId == voyageId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        var claims = await query.OrderBy(c => c.ClaimReference).ToListAsync();
        var dtos = claims.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ClaimRecordDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClaimRecordById(Guid id)
    {
        var claim = await _db.ClaimRecords.FirstOrDefaultAsync(c => c.Id == id);
        if (claim == null)
            return NotFound(ApiResponse<object>.FailureResult("Claim record not found"));

        return Ok(ApiResponse<ClaimRecordDto>.SuccessResult(MapToDto(claim)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateClaimRecord([FromBody] CreateClaimRecordRequestDto dto)
    {
        var claim = new ClaimRecord
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VoyageId = dto.VoyageId,
            VesselName = dto.VesselName,
            ClaimKey = string.IsNullOrWhiteSpace(dto.ClaimKey) ? Guid.NewGuid().ToString("n") : dto.ClaimKey,
            ClaimType = dto.ClaimType,
            ClaimReference = dto.ClaimReference,
            Amount = dto.Amount,
            Currency = dto.Currency ?? "USD",
            Status = "Open",
            Owner = dto.Owner,
            Settlement = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.ClaimRecords.Add(claim);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<ClaimRecordDto>.SuccessResult(MapToDto(claim)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClaimRecord(Guid id, [FromBody] UpdateClaimRecordRequestDto dto)
    {
        var claim = await _db.ClaimRecords.FirstOrDefaultAsync(c => c.Id == id);
        if (claim == null)
            return NotFound(ApiResponse<object>.FailureResult("Claim record not found"));

        if (dto.Amount.HasValue)
            claim.Amount = dto.Amount.Value;
        if (dto.Settlement.HasValue)
            claim.Settlement = dto.Settlement.Value;
        if (!string.IsNullOrWhiteSpace(dto.Status))
            claim.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.PaymentStatus))
            claim.PaymentStatus = dto.PaymentStatus;
        if (!string.IsNullOrWhiteSpace(dto.WorkflowStatus))
            claim.WorkflowStatus = dto.WorkflowStatus;
        if (!string.IsNullOrWhiteSpace(dto.SettledBy))
            claim.SettledBy = dto.SettledBy;

        claim.UpdatedAt = DateTime.UtcNow;
        claim.UpdatedByUserId = _currentUser.UserId;

        _db.ClaimRecords.Update(claim);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<ClaimRecordDto>.SuccessResult(MapToDto(claim)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClaimRecord(Guid id)
    {
        var claim = await _db.ClaimRecords.FirstOrDefaultAsync(c => c.Id == id);
        if (claim == null)
            return NotFound(ApiResponse<object>.FailureResult("Claim record not found"));

        _db.ClaimRecords.Remove(claim);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Claim record deleted"));
    }

    private static ClaimRecordDto MapToDto(ClaimRecord claim)
    {
        return new ClaimRecordDto
        {
            Id = claim.Id,
            VoyageId = claim.VoyageId,
            VesselName = claim.VesselName,
            ClaimKey = claim.ClaimKey,
            ClaimType = claim.ClaimType,
            ClaimReference = claim.ClaimReference,
            Amount = claim.Amount,
            Currency = claim.Currency,
            Status = claim.Status,
            Owner = claim.Owner,
            Settlement = claim.Settlement,
            DueDate = claim.DueDate,
            PaymentStatus = claim.PaymentStatus,
            WorkflowStatus = claim.WorkflowStatus,
            SettledDate = claim.SettledDate,
            Remarks = claim.Remarks
        };
    }
}
