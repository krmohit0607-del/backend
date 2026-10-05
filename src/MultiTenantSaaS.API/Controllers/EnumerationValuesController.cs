using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.EnumerationValues;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/enumeration-values")]
[Authorize]
public class EnumerationValuesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EnumerationValuesController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetEnumerationValues([FromQuery] string? type = null)
    {
        var query = _db.EnumerationValues.AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.EnumerationType == type);

        var values = await query.OrderBy(e => e.SortOrder).ToListAsync();
        var dtos = values.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<EnumerationValueDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEnumerationValueById(Guid id)
    {
        var value = await _db.EnumerationValues.FirstOrDefaultAsync(e => e.Id == id);
        if (value == null)
            return NotFound(ApiResponse<object>.FailureResult("Enumeration value not found"));

        return Ok(ApiResponse<EnumerationValueDto>.SuccessResult(MapToDto(value)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateEnumerationValue([FromBody] CreateEnumerationValueRequestDto dto)
    {
        var value = new EnumerationValue
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            EnumerationType = dto.EnumerationType,
            EnumKey = dto.EnumKey,
            EnumValue = dto.EnumValue,
            SortOrder = dto.SortOrder ?? 0,
            IsSystem = dto.IsSystem ?? false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.EnumerationValues.Add(value);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<EnumerationValueDto>.SuccessResult(MapToDto(value)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEnumerationValue(Guid id, [FromBody] UpdateEnumerationValueRequestDto dto)
    {
        var value = await _db.EnumerationValues.FirstOrDefaultAsync(e => e.Id == id);
        if (value == null)
            return NotFound(ApiResponse<object>.FailureResult("Enumeration value not found"));

        if (!string.IsNullOrWhiteSpace(dto.EnumValue))
            value.EnumValue = dto.EnumValue;
        if (dto.SortOrder.HasValue)
            value.SortOrder = dto.SortOrder.Value;
        if (dto.IsSystem.HasValue)
            value.IsSystem = dto.IsSystem.Value;

        value.UpdatedAt = DateTime.UtcNow;
        value.UpdatedByUserId = _currentUser.UserId;

        _db.EnumerationValues.Update(value);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<EnumerationValueDto>.SuccessResult(MapToDto(value)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEnumerationValue(Guid id)
    {
        var value = await _db.EnumerationValues.FirstOrDefaultAsync(e => e.Id == id);
        if (value == null)
            return NotFound(ApiResponse<object>.FailureResult("Enumeration value not found"));

        _db.EnumerationValues.Remove(value);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Enumeration value deleted"));
    }

    private static EnumerationValueDto MapToDto(EnumerationValue value)
    {
        return new EnumerationValueDto
        {
            Id = value.Id,
            EnumerationType = value.EnumerationType,
            EnumKey = value.EnumKey,
            EnumValue = value.EnumValue,
            SortOrder = value.SortOrder,
            IsSystem = value.IsSystem,
            CreatedAt = value.CreatedAt,
            UpdatedAt = value.UpdatedAt
        };
    }
}
