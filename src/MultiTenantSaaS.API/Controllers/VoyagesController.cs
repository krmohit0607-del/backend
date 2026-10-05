using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Voyages;
using MultiTenantSaaS.Application.DTOs.Passages;
using MultiTenantSaaS.Application.Features.Voyages.Commands;
using MultiTenantSaaS.Application.Features.Voyages.Queries;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/voyages")]
[Authorize]
public class VoyagesController : BaseApiController
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public VoyagesController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List all voyages accessible to the caller's tenant organization.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VoyageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVoyages()
    {
        var result = await Mediator.Send(new GetVoyagesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get details of a single voyage by ID or VoyageCode (e.g., OPT001).
    /// </summary>
    [HttpGet("{idOrCode}")]
    [ProducesResponseType(typeof(ApiResponse<VoyageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVoyageById(string idOrCode)
    {
        var result = await Mediator.Send(new GetVoyageByIdQuery(idOrCode));
        return Ok(result);
    }

    /// <summary>
    /// Create a new voyage under the current user's tenant organization.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VoyageDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVoyage([FromBody] CreateVoyageRequestDto dto)
    {
        var result = await Mediator.Send(new CreateVoyageCommand(dto));
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Update an existing voyage (ports, status, speeds, costs, etc.).
    /// Used by Operations module to sync changes back to Chartering and other modules.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VoyageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVoyage(Guid id, [FromBody] UpdateVoyageRequestDto dto)
    {
        var result = await Mediator.Send(new UpdateVoyageCommand(id, dto));
        return Ok(result);
    }

    /// <summary>
    /// List all passages for a specific voyage.
    /// </summary>
    [HttpGet("{voyageId}/passages")]
    [ProducesResponseType(typeof(ApiResponse<List<PassageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVoyagePassages(Guid voyageId)
    {
        var passages = await _db.Passages
            .Where(p => p.VoyageId == voyageId)
            .Include(p => p.Legs)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var dtos = passages.Select(MapPassageToDto).ToList();
        return Ok(ApiResponse<List<PassageDto>>.SuccessResult(dtos));
    }

    /// <summary>
    /// Create a new passage for a voyage.
    /// </summary>
    [HttpPost("{voyageId}/passages")]
    [ProducesResponseType(typeof(ApiResponse<PassageDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVoyagePassage(Guid voyageId, [FromBody] CreatePassageRequestDto dto)
    {
        var voyage = await _db.Voyages.FirstOrDefaultAsync(v => v.Id == voyageId);
        if (voyage == null)
            return NotFound(ApiResponse<object>.FailureResult("Voyage not found"));

        var passage = new Passage
        {
            Id = Guid.NewGuid(),
            VoyageId = voyageId,
            TenantId = _currentUser.TenantId,
            Name = dto.Name,
            RouteRef = dto.RouteRef,
            InterimPort = dto.InterimPort,
            TotalDistanceNm = dto.TotalDistanceNm,
            Status = dto.Status ?? "Active",
            IsActive = dto.SetAsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Passages.Add(passage);

        if (dto.Legs != null && dto.Legs.Count > 0)
        {
            foreach (var legDto in dto.Legs)
            {
                var leg = new PassageLeg
                {
                    Id = Guid.NewGuid(),
                    PassageId = passage.Id,
                    Sequence = legDto.Sequence,
                    Type = legDto.Type,
                    FromPort = legDto.FromPort,
                    ToPort = legDto.ToPort,
                    Etd = legDto.Etd,
                    Eta = legDto.Eta,
                    DistanceNm = legDto.DistanceNm,
                    Speed = legDto.Speed,
                    Status = legDto.Status ?? "Planned"
                };
                _db.PassageLegs.Add(leg);
            }
        }

        if (dto.SetAsActive)
        {
            voyage.ActivePassageId = passage.Id;
            _db.Voyages.Update(voyage);
        }

        await _db.SaveChangesAsync();

        var resultDto = MapPassageToDto(passage);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<PassageDto>.SuccessResult(resultDto, "Passage created successfully"));
    }

    /// <summary>
    /// Set the active passage for a voyage.
    /// </summary>
    [HttpPut("{voyageId}/active-passage")]
    [ProducesResponseType(typeof(ApiResponse<VoyageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActivePassage(Guid voyageId, [FromBody] SetActivePassageRequestDto dto)
    {
        var voyage = await _db.Voyages.FirstOrDefaultAsync(v => v.Id == voyageId);
        if (voyage == null)
            return NotFound(ApiResponse<object>.FailureResult("Voyage not found"));

        if (dto.PassageId.HasValue)
        {
            var passage = await _db.Passages.FirstOrDefaultAsync(p => p.Id == dto.PassageId && p.VoyageId == voyageId);
            if (passage == null)
                return NotFound(ApiResponse<object>.FailureResult("Passage not found for this voyage"));
        }

        voyage.ActivePassageId = dto.PassageId;
        voyage.UpdatedAt = DateTime.UtcNow;
        voyage.UpdatedByUserId = _currentUser.UserId;

        _db.Voyages.Update(voyage);
        await _db.SaveChangesAsync();

        var result = await Mediator.Send(new GetVoyageByIdQuery(voyage.Id.ToString()));
        return Ok(result);
    }

    /// <summary>
    /// Delete a specific voyage by ID.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVoyage(Guid id)
    {
        var result = await Mediator.Send(new DeleteVoyageCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Delete ALL voyages for the current tenant (DANGEROUS - system reset only).
    /// </summary>
    [HttpDelete("admin/delete-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAllVoyages()
    {
        var result = await Mediator.Send(new DeleteAllVoyagesCommand());
        return Ok(result);
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

public class SetActivePassageRequestDto
{
    public Guid? PassageId { get; set; }
}
