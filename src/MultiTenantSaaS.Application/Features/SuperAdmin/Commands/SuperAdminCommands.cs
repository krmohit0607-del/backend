using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.SuperAdmin;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.Application.Features.SuperAdmin.Commands;

public record CreateAdminCommand(CreateAdminRequestDto Dto) : IRequest<ApiResponse<TenantDto>>;

public class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand, ApiResponse<TenantDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public CreateAdminCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IMapper mapper)
    {
        _context = context;
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<TenantDto>> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if email already exists
        var existingUser = await _identityService.FindByEmailAsync(request.Dto.AdminEmail);
        if (existingUser != null)
        {
            throw new ValidationException("AdminEmail", "A user with this email address already exists.");
        }

        // 2. Create Admin User first (with null tenantId to satisfy FK constraint)
        var (success, errors, adminUser) = await _identityService.CreateUserAsync(
            email: request.Dto.AdminEmail,
            fullName: request.Dto.AdminFullName,
            password: request.Dto.AdminPassword,
            role: UserRoles.Admin,
            tenantId: null,
            createdByUserId: _currentUserService.UserId,
            phoneNumber: request.Dto.AdminPhoneNumber);

        if (!success)
        {
            throw new ApiException($"Failed to create Admin user: {string.Join(", ", errors)}", 400, errors.ToList());
        }

        // 3. Create Tenant entity referencing AdminUserId
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            CompanyName = request.Dto.CompanyName,
            AdminUserId = adminUser.Id,
            IsActive = true,
            SubscriptionStatus = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Update Admin user with TenantId
        adminUser.TenantId = tenant.Id;
        await _identityService.UpdateUserAsync(adminUser);

        // 5. Grant initial modules if provided
        if (request.Dto.InitialModuleIds != null && request.Dto.InitialModuleIds.Count != 0)
        {
            var validModules = await _context.Modules
                .Where(m => request.Dto.InitialModuleIds.Contains(m.Id) && m.IsGlobalActive)
                .ToListAsync(cancellationToken);

            foreach (var mod in validModules)
            {
                var access = new TenantModuleAccess
                {
                    TenantId = tenant.Id,
                    ModuleId = mod.Id,
                    IsEnabled = true,
                    GrantedByUserId = _currentUserService.UserId ?? adminUser.Id,
                    GrantedAt = DateTime.UtcNow
                };
                _context.TenantModuleAccesses.Add(access);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "CreateTenantAdmin",
            entityName: "Tenant",
            entityId: tenant.Id.ToString(),
            newValues: $"Tenant: {tenant.CompanyName}, Admin: {adminUser.Email}",
            tenantId: tenant.Id,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = new TenantDto
        {
            Id = tenant.Id,
            CompanyName = tenant.CompanyName,
            AdminUserId = adminUser.Id,
            AdminFullName = adminUser.FullName,
            AdminEmail = adminUser.Email ?? string.Empty,
            IsActive = tenant.IsActive,
            SubscriptionStatus = tenant.SubscriptionStatus,
            CreatedAt = tenant.CreatedAt,
            ActiveUsersCount = 1,
            EnabledModulesCount = request.Dto.InitialModuleIds?.Count ?? 0
        };

        return ApiResponse<TenantDto>.SuccessResult(dto, "Tenant and Admin created successfully.");
    }
}

public record UpdateTenantStatusCommand(Guid TenantId, UpdateTenantStatusRequestDto Dto) : IRequest<ApiResponse<TenantDto>>;

public class UpdateTenantStatusCommandHandler : IRequestHandler<UpdateTenantStatusCommand, ApiResponse<TenantDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public UpdateTenantStatusCommandHandler(
        IApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<TenantDto>> Handle(UpdateTenantStatusCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .Include(t => t.AdminUser)
            .Include(t => t.Users)
            .Include(t => t.TenantModuleAccesses)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new NotFoundException("Tenant", request.TenantId);
        }

        var oldStatus = $"Active: {tenant.IsActive}, Subscription: {tenant.SubscriptionStatus}";

        tenant.IsActive = request.Dto.IsActive;
        if (request.Dto.SubscriptionStatus.HasValue)
        {
            tenant.SubscriptionStatus = request.Dto.SubscriptionStatus.Value;
        }
        tenant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "UpdateTenantStatus",
            entityName: "Tenant",
            entityId: tenant.Id.ToString(),
            oldValues: oldStatus,
            newValues: $"Active: {tenant.IsActive}, Subscription: {tenant.SubscriptionStatus}",
            tenantId: tenant.Id,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<TenantDto>(tenant);
        return ApiResponse<TenantDto>.SuccessResult(dto, "Tenant status updated successfully.");
    }
}

public record CreateModuleCommand(CreateModuleRequestDto Dto) : IRequest<ApiResponse<ModuleDto>>;

public class CreateModuleCommandHandler : IRequestHandler<CreateModuleCommand, ApiResponse<ModuleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateModuleCommandHandler(
        IApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ModuleDto>> Handle(CreateModuleCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.Dto.Name.Trim().ToUpperInvariant();
        var exists = await _context.Modules.AnyAsync(m => m.NormalizedName == normalized, cancellationToken);
        if (exists)
        {
            throw new ValidationException("Name", $"A module with name '{request.Dto.Name}' already exists.");
        }

        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = request.Dto.Name.Trim(),
            NormalizedName = normalized,
            Description = request.Dto.Description,
            IsGlobalActive = request.Dto.IsGlobalActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Modules.Add(module);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "CreateModule",
            entityName: "Module",
            entityId: module.Id.ToString(),
            newValues: $"Module: {module.Name}, GlobalActive: {module.IsGlobalActive}",
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<ModuleDto>(module);
        return ApiResponse<ModuleDto>.SuccessResult(dto, "Module created successfully.");
    }
}

public record SetTenantModuleAccessCommand(SetTenantModuleAccessRequestDto Dto) : IRequest<ApiResponse<TenantModuleAccessDto>>;

public class SetTenantModuleAccessCommandHandler : IRequestHandler<SetTenantModuleAccessCommand, ApiResponse<TenantModuleAccessDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public SetTenantModuleAccessCommandHandler(
        IApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<TenantModuleAccessDto>> Handle(SetTenantModuleAccessCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == request.Dto.TenantId, cancellationToken);
        if (tenant == null)
        {
            throw new NotFoundException("Tenant", request.Dto.TenantId);
        }

        var module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == request.Dto.ModuleId, cancellationToken);
        if (module == null)
        {
            throw new NotFoundException("Module", request.Dto.ModuleId);
        }

        var existingAccess = await _context.TenantModuleAccesses
            .Include(t => t.Module)
            .Include(t => t.GrantedByUser)
            .FirstOrDefaultAsync(t => t.TenantId == request.Dto.TenantId && t.ModuleId == request.Dto.ModuleId, cancellationToken);

        if (existingAccess == null)
        {
            existingAccess = new TenantModuleAccess
            {
                Id = Guid.NewGuid(),
                TenantId = request.Dto.TenantId,
                ModuleId = request.Dto.ModuleId,
                IsEnabled = request.Dto.IsEnabled,
                GrantedByUserId = _currentUserService.UserId ?? Guid.Empty,
                GrantedAt = DateTime.UtcNow
            };
            _context.TenantModuleAccesses.Add(existingAccess);
        }
        else
        {
            existingAccess.IsEnabled = request.Dto.IsEnabled;
            existingAccess.GrantedByUserId = _currentUserService.UserId ?? existingAccess.GrantedByUserId;
            existingAccess.GrantedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Reload module & granted user for mapping if needed
        existingAccess.Module = module;
        if (_currentUserService.UserId.HasValue)
        {
            existingAccess.GrantedByUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, cancellationToken);
        }

        await _auditService.LogAsync(
            action: request.Dto.IsEnabled ? "GrantTenantModule" : "RevokeTenantModule",
            entityName: "TenantModuleAccess",
            entityId: existingAccess.Id.ToString(),
            newValues: $"Tenant: {tenant.CompanyName}, Module: {module.Name}, Enabled: {request.Dto.IsEnabled}",
            tenantId: tenant.Id,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<TenantModuleAccessDto>(existingAccess);
        return ApiResponse<TenantModuleAccessDto>.SuccessResult(dto, $"Module access {(request.Dto.IsEnabled ? "granted" : "revoked")} successfully.");
    }
}

public record DeleteTenantCommand(Guid TenantId) : IRequest<ApiResponse>;

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public DeleteTenantCommandHandler(
        IApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<ApiResponse> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Users)
            .Include(t => t.AdminUser)
            .Include(t => t.TenantModuleAccesses)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new NotFoundException("Tenant", request.TenantId);
        }

        var tenantName = tenant.CompanyName;

        // Remove all tenant module accesses
        _context.TenantModuleAccesses.RemoveRange(tenant.TenantModuleAccesses);

        // Deactivate all users in the tenant
        foreach (var user in tenant.Users)
        {
            await _identityService.SetUserActiveStatusAsync(user.Id, false);
        }

        // Remove the tenant itself
        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "DeleteTenant",
            entityName: "Tenant",
            entityId: tenant.Id.ToString(),
            oldValues: $"Tenant: {tenantName}, Users: {tenant.Users.Count}",
            tenantId: tenant.Id,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        return ApiResponse.SuccessResult($"Tenant '{tenantName}' and all associated data have been permanently deleted.");
    }
}
