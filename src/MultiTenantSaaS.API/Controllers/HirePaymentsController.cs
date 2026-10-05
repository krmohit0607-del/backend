using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.HirePayments;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.API.Controllers;

/// <summary>
/// Read-only fleet-wide reporting over the Hire Payment schedule synced from Operations'
/// Hire &amp; Claims tab (see SettingsController.SyncHireAndClaimsAsync). Writes happen only
/// through that sync — there is no direct create/update/delete here.
/// </summary>
[Route("api/hire-payments")]
[Authorize]
public class HirePaymentsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public HirePaymentsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetHirePayments(
        [FromQuery] string? voyageId = null,
        [FromQuery] string? side = null,
        [FromQuery] string? status = null,
        [FromQuery] bool openOnly = false)
    {
        var query = _db.HirePayments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(voyageId))
            query = query.Where(h => h.VoyageId == voyageId);
        if (!string.IsNullOrWhiteSpace(side))
            query = query.Where(h => h.Side == side);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(h => h.Status == status);
        if (openOnly)
            query = query.Where(h => h.Status != "Paid & Locked");

        var rows = await query.OrderBy(h => h.VoyageId).ThenBy(h => h.Side).ToListAsync();
        return Ok(ApiResponse<List<HirePaymentDto>>.SuccessResult(rows.Select(MapToDto).ToList()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHirePaymentById(Guid id)
    {
        var row = await _db.HirePayments.FirstOrDefaultAsync(h => h.Id == id);
        if (row == null)
            return NotFound(ApiResponse<object>.FailureResult("Hire payment not found"));

        return Ok(ApiResponse<HirePaymentDto>.SuccessResult(MapToDto(row)));
    }

    private static HirePaymentDto MapToDto(HirePayment h) => new()
    {
        Id = h.Id,
        VoyageId = h.VoyageId,
        VesselName = h.VesselName,
        Side = h.Side,
        InstallmentKey = h.InstallmentKey,
        IsDuplicate = h.IsDuplicate,
        Name = h.Name,
        Account = h.Account,
        FromDate = h.FromDate,
        ToDate = h.ToDate,
        DueDate = h.DueDate,
        OnHireDays = h.OnHireDays,
        OffHireDays = h.OffHireDays,
        Amount = h.Amount,
        Currency = h.Currency,
        Status = h.Status,
        Ballast = h.Ballast,
        Bunkers = h.Bunkers,
        BunkerCredit = h.BunkerCredit
    };
}
