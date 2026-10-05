using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.WorkflowConfigurations;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/workflow-configurations")]
[Authorize]
public class WorkflowConfigurationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public WorkflowConfigurationsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetWorkflowConfigurations()
    {
        var configs = await _db.WorkflowConfigurations.OrderBy(w => w.ConfigKey).ToListAsync();
        var dtos = configs.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<WorkflowConfigurationDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetWorkflowConfigurationById(Guid id)
    {
        var config = await _db.WorkflowConfigurations.FirstOrDefaultAsync(w => w.Id == id);
        if (config == null)
            return NotFound(ApiResponse<object>.FailureResult("Workflow configuration not found"));

        return Ok(ApiResponse<WorkflowConfigurationDto>.SuccessResult(MapToDto(config)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkflowConfiguration([FromBody] CreateWorkflowConfigurationRequestDto dto)
    {
        var config = new WorkflowConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            ConfigKey = dto.ConfigName,
            Description = dto.Description,
            ConfigValue = dto.ConfigJson ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.WorkflowConfigurations.Add(config);
        await _db.SaveChangesAsync();

        return StatusCode(201, ApiResponse<WorkflowConfigurationDto>.SuccessResult(MapToDto(config)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkflowConfiguration(Guid id, [FromBody] UpdateWorkflowConfigurationRequestDto dto)
    {
        var config = await _db.WorkflowConfigurations.FirstOrDefaultAsync(w => w.Id == id);
        if (config == null)
            return NotFound(ApiResponse<object>.FailureResult("Workflow configuration not found"));

        if (!string.IsNullOrWhiteSpace(dto.ConfigName))
            config.ConfigKey = dto.ConfigName;
        if (!string.IsNullOrWhiteSpace(dto.ConfigJson))
            config.ConfigValue = dto.ConfigJson;
        if (dto.IsActive.HasValue)
            config.IsActive = dto.IsActive.Value;

        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedByUserId = _currentUser.UserId;

        _db.WorkflowConfigurations.Update(config);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<WorkflowConfigurationDto>.SuccessResult(MapToDto(config)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkflowConfiguration(Guid id)
    {
        var config = await _db.WorkflowConfigurations.FirstOrDefaultAsync(w => w.Id == id);
        if (config == null)
            return NotFound(ApiResponse<object>.FailureResult("Workflow configuration not found"));

        _db.WorkflowConfigurations.Remove(config);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Workflow configuration deleted"));
    }

    private static WorkflowConfigurationDto MapToDto(WorkflowConfiguration config)
    {
        return new WorkflowConfigurationDto
        {
            Id = config.Id,
            ConfigName = config.ConfigKey,
            Description = config.Description,
            ConfigJson = config.ConfigValue,
            IsActive = config.IsActive
        };
    }
}
