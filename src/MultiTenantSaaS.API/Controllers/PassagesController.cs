using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Passages;
using MultiTenantSaaS.Application.DTOs.Voyages;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/passages")]
[Authorize]
public class PassagesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PassagesController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get a single passage with its legs by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PassageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPassageById(Guid id)
    {
        var passage = await _db.Passages
            .Include(p => p.Legs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (passage == null)
            return NotFound(ApiResponse<object>.FailureResult("Passage not found"));

        var dto = MapPassageToDto(passage);
        return Ok(ApiResponse<PassageDto>.SuccessResult(dto));
    }

    /// <summary>
    /// Update a passage.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PassageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePassage(Guid id, [FromBody] UpdatePassageRequestDto dto)
    {
        var passage = await _db.Passages.FirstOrDefaultAsync(p => p.Id == id);
        if (passage == null)
            return NotFound(ApiResponse<object>.FailureResult("Passage not found"));

        if (!string.IsNullOrWhiteSpace(dto.Name))
            passage.Name = dto.Name;
        if (dto.RouteRef != null)
            passage.RouteRef = dto.RouteRef;
        if (dto.InterimPort != null)
            passage.InterimPort = dto.InterimPort;
        if (dto.TotalDistanceNm.HasValue)
            passage.TotalDistanceNm = dto.TotalDistanceNm;
        if (dto.Status != null)
            passage.Status = dto.Status;

        passage.CreatedAt = DateTime.UtcNow;

        _db.Passages.Update(passage);
        await _db.SaveChangesAsync();

        var updated = await _db.Passages.Include(p => p.Legs).FirstOrDefaultAsync(p => p.Id == id);
        var resultDto = MapPassageToDto(updated);
        return Ok(ApiResponse<PassageDto>.SuccessResult(resultDto, "Passage updated successfully"));
    }

    /// <summary>
    /// Delete a passage.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePassage(Guid id)
    {
        var passage = await _db.Passages.FirstOrDefaultAsync(p => p.Id == id);
        if (passage == null)
            return NotFound(ApiResponse<object>.FailureResult("Passage not found"));

        _db.Passages.Remove(passage);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Passage deleted successfully"));
    }

    private static PassageDto MapPassageToDto(Passage passage)
    {
        return new PassageDto
        {
            Id = passage.Id,
            VoyageId = passage.VoyageId,
            Name = passage.Name,
            RouteRef = passage.RouteRef,
            InterimPort = passage.InterimPort,
            TotalDistanceNm = passage.TotalDistanceNm,
            Status = passage.Status,
            IsActive = passage.IsActive,
            Legs = passage.Legs?.Select(l => new PassageLegDto
            {
                Id = l.Id,
                PassageId = l.PassageId,
                Sequence = l.Sequence,
                Type = l.Type,
                FromPort = l.FromPort,
                ToPort = l.ToPort,
                Etd = l.Etd,
                Eta = l.Eta,
                DistanceNm = l.DistanceNm,
                Speed = l.Speed,
                Status = l.Status
            }).ToList() ?? new()
        };
    }
}
