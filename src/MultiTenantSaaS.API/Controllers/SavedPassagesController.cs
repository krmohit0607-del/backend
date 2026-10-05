using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.SavedPassages;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/saved-passages")]
[Authorize]
public class SavedPassagesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SavedPassagesController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetSavedPassages()
    {
        var passages = await _db.SavedPassages.OrderBy(s => s.Name).ToListAsync();
        var dtos = passages.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<SavedPassageDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSavedPassageById(Guid id)
    {
        var passage = await _db.SavedPassages.FirstOrDefaultAsync(s => s.Id == id);
        if (passage == null)
            return NotFound(ApiResponse<object>.FailureResult("Saved passage not found"));

        return Ok(ApiResponse<SavedPassageDto>.SuccessResult(MapToDto(passage)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSavedPassage([FromBody] CreateSavedPassageRequestDto dto)
    {
        var passage = new SavedPassage
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            Name = dto.Name,
            Description = dto.Description,
            TypicalDistance = (decimal)dto.TypicalDistance,
            TypicalSpeed = (decimal)dto.TypicalSpeed,
            TypicalDays = (decimal)dto.TypicalDays,
            RouteJson = dto.RouteJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.SavedPassages.Add(passage);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<SavedPassageDto>.SuccessResult(MapToDto(passage)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSavedPassage(Guid id, [FromBody] UpdateSavedPassageRequestDto dto)
    {
        var passage = await _db.SavedPassages.FirstOrDefaultAsync(s => s.Id == id);
        if (passage == null)
            return NotFound(ApiResponse<object>.FailureResult("Saved passage not found"));

        if (!string.IsNullOrWhiteSpace(dto.Name))
            passage.Name = dto.Name;
        if (dto.TypicalDays.HasValue)
            passage.TypicalDays = (decimal)dto.TypicalDays;
        if (dto.IsActive.HasValue)
            passage.IsActive = dto.IsActive.Value;

        passage.UpdatedAt = DateTime.UtcNow;
        passage.UpdatedByUserId = _currentUser.UserId;

        _db.SavedPassages.Update(passage);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<SavedPassageDto>.SuccessResult(MapToDto(passage)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSavedPassage(Guid id)
    {
        var passage = await _db.SavedPassages.FirstOrDefaultAsync(s => s.Id == id);
        if (passage == null)
            return NotFound(ApiResponse<object>.FailureResult("Saved passage not found"));

        _db.SavedPassages.Remove(passage);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Saved passage deleted"));
    }

    private static SavedPassageDto MapToDto(SavedPassage passage)
    {
        return new SavedPassageDto
        {
            Id = passage.Id,
            Name = passage.Name,
            Description = passage.Description,
            FromPort = passage.FromPort,
            ToPort = passage.ToPort,
            TypicalDistance = (double)passage.TypicalDistance,
            TypicalSpeed = (double)passage.TypicalSpeed,
            TypicalDays = (double)passage.TypicalDays,
            RouteJson = passage.RouteJson,
            IsActive = passage.IsActive
        };
    }
}
