using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.SettlementMilestones;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/settlement-milestones")]
[Authorize]
public class SettlementMilestonesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SettlementMilestonesController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettlementMilestones([FromQuery] Guid? voyageId = null)
    {
        var query = _db.SettlementMilestones.AsQueryable();

        if (voyageId.HasValue)
            query = query.Where(s => s.VoyageId == voyageId);

        var milestones = await query.OrderBy(s => s.Sequence).ToListAsync();
        var dtos = milestones.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<SettlementMilestoneDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSettlementMilestoneById(Guid id)
    {
        var milestone = await _db.SettlementMilestones.FirstOrDefaultAsync(s => s.Id == id);
        if (milestone == null)
            return NotFound(ApiResponse<object>.FailureResult("Settlement milestone not found"));

        return Ok(ApiResponse<SettlementMilestoneDto>.SuccessResult(MapToDto(milestone)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSettlementMilestone([FromBody] CreateSettlementMilestoneRequestDto dto)
    {
        var milestone = new SettlementMilestone
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VoyageId = dto.VoyageId,
            MilestoneLabel = dto.MilestoneLabel,
            ScheduledDate = dto.ScheduledDate,
            CompletedBy = "",
            Status = "Todo",
            Sequence = dto.Sequence ?? 0,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.SettlementMilestones.Add(milestone);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<SettlementMilestoneDto>.SuccessResult(MapToDto(milestone)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSettlementMilestone(Guid id, [FromBody] UpdateSettlementMilestoneRequestDto dto)
    {
        var milestone = await _db.SettlementMilestones.FirstOrDefaultAsync(s => s.Id == id);
        if (milestone == null)
            return NotFound(ApiResponse<object>.FailureResult("Settlement milestone not found"));

        if (!string.IsNullOrWhiteSpace(dto.Status))
            milestone.Status = dto.Status;
        if (dto.CompletedDate.HasValue)
            milestone.CompletedDate = dto.CompletedDate;
        if (!string.IsNullOrWhiteSpace(dto.CompletedBy))
            milestone.CompletedBy = dto.CompletedBy;

        milestone.UpdatedAt = DateTime.UtcNow;
        milestone.UpdatedByUserId = _currentUser.UserId;

        _db.SettlementMilestones.Update(milestone);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<SettlementMilestoneDto>.SuccessResult(MapToDto(milestone)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSettlementMilestone(Guid id)
    {
        var milestone = await _db.SettlementMilestones.FirstOrDefaultAsync(s => s.Id == id);
        if (milestone == null)
            return NotFound(ApiResponse<object>.FailureResult("Settlement milestone not found"));

        _db.SettlementMilestones.Remove(milestone);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Settlement milestone deleted"));
    }

    private static SettlementMilestoneDto MapToDto(SettlementMilestone milestone)
    {
        return new SettlementMilestoneDto
        {
            Id = milestone.Id,
            VoyageId = milestone.VoyageId,
            MilestoneLabel = milestone.MilestoneLabel,
            ScheduledDate = milestone.ScheduledDate,
            CompletedDate = milestone.CompletedDate,
            CompletedBy = milestone.CompletedBy,
            Status = milestone.Status,
            Sequence = milestone.Sequence,
            Notes = milestone.Notes
        };
    }
}
