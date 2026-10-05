using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.EmailTemplates;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/email-templates")]
[Authorize]
public class EmailTemplatesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EmailTemplatesController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List all email templates for the caller's tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmailTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmailTemplates([FromQuery] string? category = null)
    {
        var query = _db.EmailTemplates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        var templates = await query
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync();

        var dtos = templates.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<EmailTemplateDto>>.SuccessResult(dtos));
    }

    /// <summary>
    /// Get a single email template by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailTemplateById(Guid id)
    {
        var template = await _db.EmailTemplates.FirstOrDefaultAsync(e => e.Id == id);
        if (template == null)
            return NotFound(ApiResponse<object>.FailureResult("Email template not found"));

        return Ok(ApiResponse<EmailTemplateDto>.SuccessResult(MapToDto(template)));
    }

    /// <summary>
    /// Create a new email template.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateEmailTemplate([FromBody] CreateEmailTemplateRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<object>.FailureResult("Template name is required"));

        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            Name = dto.Name,
            Category = dto.Category,
            SubCategory = dto.SubCategory,
            Subject = dto.Subject,
            BodyHtml = dto.HtmlBody,
            DefaultTo = dto.DefaultRecipients,
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.EmailTemplates.Add(template);
        await _db.SaveChangesAsync();

        var resultDto = MapToDto(template);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<EmailTemplateDto>.SuccessResult(resultDto, "Email template created successfully"));
    }

    /// <summary>
    /// Update an existing email template.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmailTemplate(Guid id, [FromBody] UpdateEmailTemplateRequestDto dto)
    {
        var template = await _db.EmailTemplates.FirstOrDefaultAsync(e => e.Id == id);
        if (template == null)
            return NotFound(ApiResponse<object>.FailureResult("Email template not found"));

        if (!string.IsNullOrWhiteSpace(dto.Name))
            template.Name = dto.Name;
        if (dto.Category != null)
            template.Category = dto.Category;
        if (dto.SubCategory != null)
            template.SubCategory = dto.SubCategory;
        if (dto.Subject != null)
            template.Subject = dto.Subject;
        if (dto.HtmlBody != null)
            template.BodyHtml = dto.HtmlBody;
        if (dto.DefaultRecipients != null)
            template.DefaultTo = dto.DefaultRecipients;
        if (dto.IsActive.HasValue)
            template.IsActive = dto.IsActive.Value;

        template.Version++;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = _currentUser.UserId;

        _db.EmailTemplates.Update(template);
        await _db.SaveChangesAsync();

        var resultDto = MapToDto(template);
        return Ok(ApiResponse<EmailTemplateDto>.SuccessResult(resultDto, "Email template updated successfully"));
    }

    /// <summary>
    /// Delete an email template.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmailTemplate(Guid id)
    {
        var template = await _db.EmailTemplates.FirstOrDefaultAsync(e => e.Id == id);
        if (template == null)
            return NotFound(ApiResponse<object>.FailureResult("Email template not found"));

        _db.EmailTemplates.Remove(template);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Email template deleted successfully"));
    }

    private static EmailTemplateDto MapToDto(EmailTemplate template)
    {
        return new EmailTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Category = template.Category,
            SubCategory = template.SubCategory,
            Subject = template.Subject,
            HtmlBody = template.BodyHtml,
            DefaultRecipients = template.DefaultTo,
            Tags = "",
            Version = template.Version,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
