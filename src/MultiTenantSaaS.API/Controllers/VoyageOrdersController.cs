using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.VoyageOrders;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/voyage-orders")]
[Authorize]
public class VoyageOrdersController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public VoyageOrdersController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetVoyageOrders()
    {
        var orders = await _db.VoyageOrders.OrderBy(v => v.OrderNumber).ToListAsync();
        var dtos = orders.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<VoyageOrderDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVoyageOrderById(Guid id)
    {
        var order = await _db.VoyageOrders.FirstOrDefaultAsync(v => v.Id == id);
        if (order == null)
            return NotFound(ApiResponse<object>.FailureResult("Voyage order not found"));

        return Ok(ApiResponse<VoyageOrderDto>.SuccessResult(MapToDto(order)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateVoyageOrder([FromBody] CreateVoyageOrderRequestDto dto)
    {
        var order = new VoyageOrder
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            OrderNumber = dto.OrderNo,
            Vessel = dto.Vessel,
            Owner = dto.Owner,
            Charterer = dto.Charterer,
            Broker = dto.Broker,
            LoadPort = dto.LoadPort,
            DischargePort = dto.DischargePort,
            Cargo = dto.Cargo,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Commodity = dto.Commodity,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.VoyageOrders.Add(order);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<VoyageOrderDto>.SuccessResult(MapToDto(order)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVoyageOrder(Guid id, [FromBody] UpdateVoyageOrderRequestDto dto)
    {
        var order = await _db.VoyageOrders.FirstOrDefaultAsync(v => v.Id == id);
        if (order == null)
            return NotFound(ApiResponse<object>.FailureResult("Voyage order not found"));

        if (!string.IsNullOrWhiteSpace(dto.Status))
            order.Status = dto.Status;
        if (dto.Quantity.HasValue)
            order.Quantity = dto.Quantity.Value;

        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedByUserId = _currentUser.UserId;

        _db.VoyageOrders.Update(order);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<VoyageOrderDto>.SuccessResult(MapToDto(order)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVoyageOrder(Guid id)
    {
        var order = await _db.VoyageOrders.FirstOrDefaultAsync(v => v.Id == id);
        if (order == null)
            return NotFound(ApiResponse<object>.FailureResult("Voyage order not found"));

        _db.VoyageOrders.Remove(order);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Voyage order deleted"));
    }

    private static VoyageOrderDto MapToDto(VoyageOrder order)
    {
        return new VoyageOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNumber,
            Vessel = order.Vessel,
            Owner = order.Owner,
            Charterer = order.Charterer,
            Broker = order.Broker,
            LoadPort = order.LoadPort,
            DischargePort = order.DischargePort,
            Cargo = order.Cargo,
            Quantity = order.Quantity,
            Unit = order.Unit,
            Commodity = order.Commodity,
            Status = order.Status
        };
    }
}
