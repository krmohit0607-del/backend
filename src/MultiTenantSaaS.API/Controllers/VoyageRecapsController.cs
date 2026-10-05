using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.VoyageRecaps;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/voyage-recaps")]
[Authorize]
public class VoyageRecapsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public VoyageRecapsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetVoyageRecaps([FromQuery] Guid? voyageId = null)
    {
        var query = _db.VoyageRecaps.AsQueryable();

        if (voyageId.HasValue)
            query = query.Where(v => v.VoyageId == voyageId);

        var recaps = await query.OrderBy(v => v.CreatedAt).ToListAsync();
        var dtos = recaps.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<VoyageRecapDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVoyageRecapById(Guid id)
    {
        var recap = await _db.VoyageRecaps.FirstOrDefaultAsync(v => v.Id == id);
        if (recap == null)
            return NotFound(ApiResponse<object>.FailureResult("Voyage recap not found"));

        return Ok(ApiResponse<VoyageRecapDto>.SuccessResult(MapToDto(recap)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateVoyageRecap([FromBody] CreateVoyageRecapRequestDto dto)
    {
        var recap = new VoyageRecap
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VoyageId = dto.VoyageId,
            PdaNo = dto.PdaNo,
            FdaNo = dto.FdaNo,
            CharterPartyReference = dto.CharterPartyReference,
            Laytime = dto.Laytime,
            Demurrage = dto.Demurrage,
            Despatch = dto.Despatch,
            NetResultShip = dto.NetResultShip,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.VoyageRecaps.Add(recap);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<VoyageRecapDto>.SuccessResult(MapToDto(recap)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVoyageRecap(Guid id, [FromBody] UpdateVoyageRecapRequestDto dto)
    {
        var recap = await _db.VoyageRecaps.FirstOrDefaultAsync(v => v.Id == id);
        if (recap == null)
            return NotFound(ApiResponse<object>.FailureResult("Voyage recap not found"));

        if (dto.NetResultShip.HasValue)
            recap.NetResultShip = dto.NetResultShip.Value;
        if (!string.IsNullOrWhiteSpace(dto.Status))
            recap.Status = dto.Status;

        recap.UpdatedAt = DateTime.UtcNow;
        recap.UpdatedByUserId = _currentUser.UserId;

        _db.VoyageRecaps.Update(recap);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<VoyageRecapDto>.SuccessResult(MapToDto(recap)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVoyageRecap(Guid id)
    {
        var recap = await _db.VoyageRecaps.FirstOrDefaultAsync(v => v.Id == id);
        if (recap == null)
            return NotFound(ApiResponse<object>.FailureResult("Voyage recap not found"));

        _db.VoyageRecaps.Remove(recap);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Voyage recap deleted"));
    }

    private static VoyageRecapDto MapToDto(VoyageRecap recap)
    {
        return new VoyageRecapDto
        {
            Id = recap.Id,
            VoyageId = recap.VoyageId,
            PdaNo = recap.PdaNo,
            FdaNo = recap.FdaNo,
            CharterPartyReference = recap.CharterPartyReference,
            Laytime = recap.Laytime,
            Demurrage = recap.Demurrage,
            Despatch = recap.Despatch,
            NetResultShip = recap.NetResultShip,
            Status = recap.Status
        };
    }
}
