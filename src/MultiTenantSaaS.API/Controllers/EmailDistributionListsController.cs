using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.EmailDistributionLists;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/email-distribution-lists")]
[Authorize]
public class EmailDistributionListsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EmailDistributionListsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmailDistributionLists()
    {
        var lists = await _db.EmailDistributionLists.OrderBy(e => e.Name).ToListAsync();
        var dtos = lists.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<EmailDistributionListDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmailDistributionListById(Guid id)
    {
        var list = await _db.EmailDistributionLists.FirstOrDefaultAsync(e => e.Id == id);
        if (list == null)
            return NotFound(ApiResponse<object>.FailureResult("Email distribution list not found"));

        return Ok(ApiResponse<EmailDistributionListDto>.SuccessResult(MapToDto(list)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmailDistributionList([FromBody] CreateEmailDistributionListRequestDto dto)
    {
        var list = new EmailDistributionList
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            Name = dto.ListName,
            Description = dto.Description,
            Recipients = string.Join(";", dto.Recipients ?? new()),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.EmailDistributionLists.Add(list);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<EmailDistributionListDto>.SuccessResult(MapToDto(list)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmailDistributionList(Guid id, [FromBody] UpdateEmailDistributionListRequestDto dto)
    {
        var list = await _db.EmailDistributionLists.FirstOrDefaultAsync(e => e.Id == id);
        if (list == null)
            return NotFound(ApiResponse<object>.FailureResult("Email distribution list not found"));

        if (!string.IsNullOrWhiteSpace(dto.ListName))
            list.Name = dto.ListName;
        if (dto.Recipients != null && dto.Recipients.Count > 0)
            list.Recipients = string.Join(";", dto.Recipients);
        if (dto.IsActive.HasValue)
            list.IsActive = dto.IsActive.Value;

        list.UpdatedAt = DateTime.UtcNow;
        list.UpdatedByUserId = _currentUser.UserId;

        _db.EmailDistributionLists.Update(list);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<EmailDistributionListDto>.SuccessResult(MapToDto(list)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmailDistributionList(Guid id)
    {
        var list = await _db.EmailDistributionLists.FirstOrDefaultAsync(e => e.Id == id);
        if (list == null)
            return NotFound(ApiResponse<object>.FailureResult("Email distribution list not found"));

        _db.EmailDistributionLists.Remove(list);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Email distribution list deleted"));
    }

    private static EmailDistributionListDto MapToDto(EmailDistributionList list)
    {
        return new EmailDistributionListDto
        {
            Id = list.Id,
            ListName = list.Name,
            Description = list.Description,
            Recipients = list.Recipients?.Split(';').ToList() ?? new(),
            IsActive = list.IsActive
        };
    }
}
