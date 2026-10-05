using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.CargoMasters;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/cargo-masters")]
[Authorize]
public class CargoMastersController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CargoMastersController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List all cargo masters for the caller's tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CargoMasterDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCargoMasters([FromQuery] string? imoClass = null)
    {
        var query = _db.CargoMasters.AsQueryable();

        if (!string.IsNullOrWhiteSpace(imoClass))
            query = query.Where(c => c.ImoClassification == imoClass);

        var cargos = await query
            .OrderBy(c => c.CargoName)
            .ToListAsync();

        var dtos = cargos.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<CargoMasterDto>>.SuccessResult(dtos));
    }

    /// <summary>
    /// Get a single cargo master by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CargoMasterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCargoMasterById(Guid id)
    {
        var cargo = await _db.CargoMasters.FirstOrDefaultAsync(c => c.Id == id);
        if (cargo == null)
            return NotFound(ApiResponse<object>.FailureResult("Cargo master not found"));

        return Ok(ApiResponse<CargoMasterDto>.SuccessResult(MapToDto(cargo)));
    }

    /// <summary>
    /// Get a cargo master by code.
    /// </summary>
    [HttpGet("by-code/{cargoCode}")]
    [ProducesResponseType(typeof(ApiResponse<CargoMasterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCargoMasterByCode(string cargoCode)
    {
        var cargo = await _db.CargoMasters.FirstOrDefaultAsync(c => c.CargoCode == cargoCode);
        if (cargo == null)
            return NotFound(ApiResponse<object>.FailureResult("Cargo master not found"));

        return Ok(ApiResponse<CargoMasterDto>.SuccessResult(MapToDto(cargo)));
    }

    /// <summary>
    /// Create a new cargo master.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CargoMasterDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCargoMaster([FromBody] CreateCargoMasterRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CargoCode) || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<object>.FailureResult("Cargo code and name are required"));

        // Check for duplicate code
        var existing = await _db.CargoMasters.FirstOrDefaultAsync(c => c.CargoCode == dto.CargoCode && c.TenantId == _currentUser.TenantId);
        if (existing != null)
            return BadRequest(ApiResponse<object>.FailureResult("A cargo with this code already exists"));

        var cargo = new CargoMaster
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            CargoCode = dto.CargoCode,
            CargoName = dto.Name,
            Description = dto.Description,
            ImoClassification = dto.ImoClass,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.CargoMasters.Add(cargo);
        await _db.SaveChangesAsync();

        var resultDto = MapToDto(cargo);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CargoMasterDto>.SuccessResult(resultDto, "Cargo master created successfully"));
    }

    /// <summary>
    /// Update an existing cargo master.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CargoMasterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCargoMaster(Guid id, [FromBody] UpdateCargoMasterRequestDto dto)
    {
        var cargo = await _db.CargoMasters.FirstOrDefaultAsync(c => c.Id == id);
        if (cargo == null)
            return NotFound(ApiResponse<object>.FailureResult("Cargo master not found"));

        if (!string.IsNullOrWhiteSpace(dto.Name))
            cargo.CargoName = dto.Name;
        if (dto.Description != null)
            cargo.Description = dto.Description;
        if (dto.ImoClass != null)
            cargo.ImoClassification = dto.ImoClass;
        if (dto.IsActive.HasValue)
            cargo.IsActive = dto.IsActive.Value;

        cargo.UpdatedAt = DateTime.UtcNow;
        cargo.UpdatedByUserId = _currentUser.UserId;

        _db.CargoMasters.Update(cargo);
        await _db.SaveChangesAsync();

        var resultDto = MapToDto(cargo);
        return Ok(ApiResponse<CargoMasterDto>.SuccessResult(resultDto, "Cargo master updated successfully"));
    }

    /// <summary>
    /// Delete a cargo master.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCargoMaster(Guid id)
    {
        var cargo = await _db.CargoMasters.FirstOrDefaultAsync(c => c.Id == id);
        if (cargo == null)
            return NotFound(ApiResponse<object>.FailureResult("Cargo master not found"));

        _db.CargoMasters.Remove(cargo);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Cargo master deleted successfully"));
    }

    private static CargoMasterDto MapToDto(CargoMaster cargo)
    {
        return new CargoMasterDto
        {
            Id = cargo.Id,
            CargoCode = cargo.CargoCode,
            Name = cargo.CargoName,
            Description = cargo.Description,
            ImoClass = cargo.ImoClassification,
            Hazard = "",
            Density = null,
            Viscosity = null,
            FlashPoint = null,
            StorageConditions = "",
            Compatibility = cargo.Compatibility,
            VesselSuitability = cargo.SuitableVesselTypes,
            SpecialRequirements = "",
            IsActive = cargo.IsActive,
            CreatedAt = cargo.CreatedAt,
            UpdatedAt = cargo.UpdatedAt
        };
    }
}
