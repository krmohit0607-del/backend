using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.LaytimeCalculations;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/laytime-calculations")]
[Authorize]
public class LaytimeCalculationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public LaytimeCalculationsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetLaytimeCalculations([FromQuery] Guid? voyageId = null)
    {
        var query = _db.LaytimeCalculations.AsQueryable();

        if (voyageId.HasValue)
            query = query.Where(l => l.VoyageId == voyageId);

        var calcs = await query.OrderBy(l => l.CreatedAt).ToListAsync();
        var dtos = calcs.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<LaytimeCalculationDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLaytimeCalculationById(Guid id)
    {
        var calc = await _db.LaytimeCalculations.FirstOrDefaultAsync(l => l.Id == id);
        if (calc == null)
            return NotFound(ApiResponse<object>.FailureResult("Laytime calculation not found"));

        return Ok(ApiResponse<LaytimeCalculationDto>.SuccessResult(MapToDto(calc)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateLaytimeCalculation([FromBody] CreateLaytimeCalculationRequestDto dto)
    {
        var calc = new LaytimeCalculation
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VoyageId = dto.VoyageId,
            LaytimeDaysAllowed = (decimal)(dto.AllowedLaytimeDays ?? 0),
            DaysUsed = (decimal)(dto.UsedLaytimeDays ?? 0),
            DaysAllowed = (decimal)(dto.AllowedLaytimeDays ?? 0),
            WeatherDelay = (decimal)(dto.WeatherDays ?? 0),
            ShiftingDelay = (decimal)(dto.ShiftingDays ?? 0),
            ExceptedDelay = (decimal)(dto.ExceptedDays ?? 0),
            NetDemurragedays = (decimal)(dto.DemurrageDays ?? 0),
            DemurrageRate = (decimal)(dto.DemurrageRate ?? 0),
            DespatchRate = (decimal)(dto.DespatchRate ?? 0),
            DemurrageAmount = 0,
            DespatchEarning = 0,
            NetDemurrage = 0,
            Currency = "USD",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.LaytimeCalculations.Add(calc);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<LaytimeCalculationDto>.SuccessResult(MapToDto(calc)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLaytimeCalculation(Guid id, [FromBody] UpdateLaytimeCalculationRequestDto dto)
    {
        var calc = await _db.LaytimeCalculations.FirstOrDefaultAsync(l => l.Id == id);
        if (calc == null)
            return NotFound(ApiResponse<object>.FailureResult("Laytime calculation not found"));

        if (dto.UsedLaytimeDays.HasValue)
            calc.DaysUsed = (decimal)dto.UsedLaytimeDays;
        if (dto.AllowedLaytimeDays.HasValue)
            calc.DaysAllowed = (decimal)dto.AllowedLaytimeDays;
        if (dto.WeatherDays.HasValue)
            calc.WeatherDelay = (decimal)dto.WeatherDays;
        if (dto.ShiftingDays.HasValue)
            calc.ShiftingDelay = (decimal)dto.ShiftingDays;
        if (dto.ExceptedDays.HasValue)
            calc.ExceptedDelay = (decimal)dto.ExceptedDays;
        if (dto.DemurrageRate.HasValue)
            calc.DemurrageRate = (decimal)dto.DemurrageRate;
        if (dto.DespatchRate.HasValue)
            calc.DespatchRate = (decimal)dto.DespatchRate;
        if (!string.IsNullOrWhiteSpace(dto.Status))
            calc.Status = dto.Status;

        calc.UpdatedAt = DateTime.UtcNow;
        calc.UpdatedByUserId = _currentUser.UserId;

        _db.LaytimeCalculations.Update(calc);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<LaytimeCalculationDto>.SuccessResult(MapToDto(calc)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLaytimeCalculation(Guid id)
    {
        var calc = await _db.LaytimeCalculations.FirstOrDefaultAsync(l => l.Id == id);
        if (calc == null)
            return NotFound(ApiResponse<object>.FailureResult("Laytime calculation not found"));

        _db.LaytimeCalculations.Remove(calc);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Laytime calculation deleted"));
    }

    private static LaytimeCalculationDto MapToDto(LaytimeCalculation calc)
    {
        return new LaytimeCalculationDto
        {
            Id = calc.Id,
            VoyageId = calc.VoyageId,
            AllowedLaytimeDays = (double)calc.LaytimeDaysAllowed,
            UsedLaytimeDays = (double)calc.DaysUsed,
            WeatherDays = (double)calc.WeatherDelay,
            ShiftingDays = (double)calc.ShiftingDelay,
            ExceptedDays = (double)calc.ExceptedDelay,
            DemurrageDays = (double)calc.NetDemurragedays,
            DespatchDays = null, // Not in entity
            DemurrageRate = (double)calc.DemurrageRate,
            DespatchRate = (double)calc.DespatchRate,
            DemurrageTotal = (decimal)calc.DemurrageAmount,
            DespatchTotal = (decimal)calc.DespatchEarning,
            Status = calc.Status,
            CreatedAt = calc.CreatedAt,
            UpdatedAt = calc.UpdatedAt
        };
    }
}
