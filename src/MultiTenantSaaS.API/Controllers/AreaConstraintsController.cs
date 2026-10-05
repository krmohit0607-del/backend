using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.AreaConstraints;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/area-constraints")]
[Authorize]
public class AreaConstraintsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AreaConstraintsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAreaConstraints([FromQuery] string? constraintType = null)
    {
        var query = _db.AreaConstraints.AsQueryable();

        if (!string.IsNullOrWhiteSpace(constraintType))
            query = query.Where(a => a.ConstraintType == constraintType);

        var constraints = await query.OrderBy(a => a.Name).ToListAsync();
        var dtos = constraints.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<AreaConstraintDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAreaConstraintById(Guid id)
    {
        var constraint = await _db.AreaConstraints.FirstOrDefaultAsync(a => a.Id == id);
        if (constraint == null)
            return NotFound(ApiResponse<object>.FailureResult("Area constraint not found"));

        return Ok(ApiResponse<AreaConstraintDto>.SuccessResult(MapToDto(constraint)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAreaConstraint([FromBody] CreateAreaConstraintRequestDto dto)
    {
        var constraint = new AreaConstraint
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            Name = dto.Name,
            ConstraintType = dto.ConstraintType,
            Description = dto.Description,
            MinLatitude = dto.MinLatitude,
            MinLongitude = dto.MinLongitude,
            MaxLatitude = dto.MaxLatitude,
            MaxLongitude = dto.MaxLongitude,
            ValidFrom = dto.ValidFrom,
            ValidUntil = dto.ValidUntil,
            GeoJson = dto.GeoJson,
            IsActive = true,
            Remarks = dto.Remarks,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.AreaConstraints.Add(constraint);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<AreaConstraintDto>.SuccessResult(MapToDto(constraint)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAreaConstraint(Guid id, [FromBody] UpdateAreaConstraintRequestDto dto)
    {
        var constraint = await _db.AreaConstraints.FirstOrDefaultAsync(a => a.Id == id);
        if (constraint == null)
            return NotFound(ApiResponse<object>.FailureResult("Area constraint not found"));

        if (!string.IsNullOrWhiteSpace(dto.Name))
            constraint.Name = dto.Name;
        if (dto.MinLatitude.HasValue)
            constraint.MinLatitude = dto.MinLatitude;
        if (dto.MaxLatitude.HasValue)
            constraint.MaxLatitude = dto.MaxLatitude;
        if (dto.MinLongitude.HasValue)
            constraint.MinLongitude = dto.MinLongitude;
        if (dto.MaxLongitude.HasValue)
            constraint.MaxLongitude = dto.MaxLongitude;
        if (!string.IsNullOrWhiteSpace(dto.ConstraintType))
            constraint.ConstraintType = dto.ConstraintType;
        if (!string.IsNullOrWhiteSpace(dto.Description))
            constraint.Description = dto.Description;
        if (dto.ValidFrom.HasValue)
            constraint.ValidFrom = dto.ValidFrom;
        if (dto.ValidUntil.HasValue)
            constraint.ValidUntil = dto.ValidUntil;
        if (!string.IsNullOrWhiteSpace(dto.GeoJson))
            constraint.GeoJson = dto.GeoJson;
        if (!string.IsNullOrWhiteSpace(dto.Remarks))
            constraint.Remarks = dto.Remarks;
        if (dto.IsActive.HasValue)
            constraint.IsActive = dto.IsActive.Value;

        constraint.UpdatedAt = DateTime.UtcNow;
        constraint.UpdatedByUserId = _currentUser.UserId;

        _db.AreaConstraints.Update(constraint);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<AreaConstraintDto>.SuccessResult(MapToDto(constraint)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAreaConstraint(Guid id)
    {
        var constraint = await _db.AreaConstraints.FirstOrDefaultAsync(a => a.Id == id);
        if (constraint == null)
            return NotFound(ApiResponse<object>.FailureResult("Area constraint not found"));

        _db.AreaConstraints.Remove(constraint);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Area constraint deleted"));
    }

    private static AreaConstraintDto MapToDto(AreaConstraint constraint)
    {
        return new AreaConstraintDto
        {
            Id = constraint.Id,
            Name = constraint.Name,
            ConstraintType = constraint.ConstraintType,
            Description = constraint.Description,
            MinLatitude = constraint.MinLatitude,
            MinLongitude = constraint.MinLongitude,
            MaxLatitude = constraint.MaxLatitude,
            MaxLongitude = constraint.MaxLongitude,
            ValidFrom = constraint.ValidFrom,
            ValidUntil = constraint.ValidUntil,
            GeoJson = constraint.GeoJson,
            IsActive = constraint.IsActive,
            Remarks = constraint.Remarks,
            CreatedAt = constraint.CreatedAt,
            UpdatedAt = constraint.UpdatedAt ?? constraint.CreatedAt
        };
    }
}
