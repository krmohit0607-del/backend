using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.AdditionalServices;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/additional-services")]
[Authorize]
public class AdditionalServicesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdditionalServicesController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAdditionalServices([FromQuery] Guid? voyageId = null)
    {
        var query = _db.AdditionalServices.AsQueryable();

        if (voyageId.HasValue)
            query = query.Where(a => a.VoyageId == voyageId);

        var services = await query.OrderBy(a => a.Service).ToListAsync();
        var dtos = services.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<AdditionalServiceDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAdditionalServiceById(Guid id)
    {
        var service = await _db.AdditionalServices.FirstOrDefaultAsync(a => a.Id == id);
        if (service == null)
            return NotFound(ApiResponse<object>.FailureResult("Additional service not found"));

        return Ok(ApiResponse<AdditionalServiceDto>.SuccessResult(MapToDto(service)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAdditionalService([FromBody] CreateAdditionalServiceRequestDto dto)
    {
        var service = new AdditionalService
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VoyageId = dto.VoyageId,
            Service = dto.Service,
            Vendor = dto.Vendor,
            InvoiceNo = dto.InvoiceNo,
            Currency = dto.Currency ?? "USD",
            Cost = dto.Cost,
            Tax = dto.Tax,
            Reason = dto.Reason,
            RequestedBy = dto.RequestedBy,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.AdditionalServices.Add(service);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<AdditionalServiceDto>.SuccessResult(MapToDto(service)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAdditionalService(Guid id, [FromBody] UpdateAdditionalServiceRequestDto dto)
    {
        var service = await _db.AdditionalServices.FirstOrDefaultAsync(a => a.Id == id);
        if (service == null)
            return NotFound(ApiResponse<object>.FailureResult("Additional service not found"));

        if (dto.Cost.HasValue)
            service.Cost = dto.Cost.Value;
        if (dto.Tax.HasValue)
            service.Tax = dto.Tax.Value;
        if (!string.IsNullOrWhiteSpace(dto.Status))
            service.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.ApprovedBy))
            service.ApprovedBy = dto.ApprovedBy;

        service.UpdatedAt = DateTime.UtcNow;
        service.UpdatedByUserId = _currentUser.UserId;

        _db.AdditionalServices.Update(service);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<AdditionalServiceDto>.SuccessResult(MapToDto(service)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAdditionalService(Guid id)
    {
        var service = await _db.AdditionalServices.FirstOrDefaultAsync(a => a.Id == id);
        if (service == null)
            return NotFound(ApiResponse<object>.FailureResult("Additional service not found"));

        _db.AdditionalServices.Remove(service);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Additional service deleted"));
    }

    private static AdditionalServiceDto MapToDto(AdditionalService service)
    {
        return new AdditionalServiceDto
        {
            Id = service.Id,
            VoyageId = service.VoyageId,
            Service = service.Service,
            Vendor = service.Vendor,
            InvoiceNo = service.InvoiceNo,
            Currency = service.Currency,
            Cost = service.Cost,
            Tax = service.Tax,
            Reason = service.Reason,
            Status = service.Status,
            ApprovedBy = service.ApprovedBy,
            ApprovedDate = service.ApprovedDate
        };
    }
}
