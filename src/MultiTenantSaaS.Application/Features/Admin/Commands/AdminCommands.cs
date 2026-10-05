using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Admin;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.Application.Features.Admin.Commands;

public record CreateEmployeeCommand(CreateEmployeeRequestDto Dto) : IRequest<ApiResponse<EmployeeDto>>;

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, ApiResponse<EmployeeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public CreateEmployeeCommandHandler(
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

    public async Task<ApiResponse<EmployeeDto>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.TenantId.HasValue)
        {
            throw new ForbiddenException("Tenant context is required to create an employee.");
        }

        var tenantId = _currentUserService.TenantId.Value;

        // Check if email is unique
        var existing = await _identityService.FindByEmailAsync(request.Dto.Email);
        if (existing != null)
        {
            throw new ValidationException("Email", "A user with this email address already exists.");
        }

        // Create user
        var (success, errors, employeeUser) = await _identityService.CreateUserAsync(
            email: request.Dto.Email,
            fullName: request.Dto.FullName,
            password: request.Dto.Password,
            role: UserRoles.Employee,
            tenantId: tenantId,
            createdByUserId: _currentUserService.UserId,
            phoneNumber: request.Dto.PhoneNumber);

        if (!success)
        {
            throw new ApiException($"Failed to create employee: {string.Join(", ", errors)}", 400, errors.ToList());
        }

        // If permissions provided, assign them (ensuring modules are enabled for tenant)
        if (request.Dto.Permissions != null && request.Dto.Permissions.Count != 0)
        {
            var requestedModuleIds = request.Dto.Permissions.Select(p => p.ModuleId).Distinct().ToList();

            var tenantEnabledModules = await _context.TenantModuleAccesses
                .Where(t => t.TenantId == tenantId && t.IsEnabled && t.Module!.IsGlobalActive && requestedModuleIds.Contains(t.ModuleId))
                .Select(t => t.ModuleId)
                .ToListAsync(cancellationToken);

            foreach (var perm in request.Dto.Permissions)
            {
                if (!tenantEnabledModules.Contains(perm.ModuleId))
                {
                    continue; // Skip modules not granted to tenant
                }

                var empPerm = new EmployeeModulePermission
                {
                    Id = Guid.NewGuid(),
                    EmployeeUserId = employeeUser.Id,
                    ModuleId = perm.ModuleId,
                    TenantId = tenantId,
                    CanView = perm.CanView,
                    CanCreate = perm.CanCreate,
                    CanEdit = perm.CanEdit,
                    CanDelete = perm.CanDelete,
                    AssignedByUserId = _currentUserService.UserId ?? Guid.Empty,
                    AssignedAt = DateTime.UtcNow
                };
                _context.EmployeeModulePermissions.Add(empPerm);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "CreateEmployee",
            entityName: "ApplicationUser",
            entityId: employeeUser.Id.ToString(),
            newValues: $"Employee: {employeeUser.FullName} ({employeeUser.Email})",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        // Fetch populated employee
        var created = await _context.Users
            .Include(u => u.ModulePermissions)
                .ThenInclude(p => p.Module)
            .FirstOrDefaultAsync(u => u.Id == employeeUser.Id, cancellationToken);

        var dto = _mapper.Map<EmployeeDto>(created);
        return ApiResponse<EmployeeDto>.SuccessResult(dto, "Employee created successfully.");
    }
}

public record UpdateEmployeeCommand(Guid EmployeeId, UpdateEmployeeRequestDto Dto) : IRequest<ApiResponse<EmployeeDto>>;

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, ApiResponse<EmployeeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public UpdateEmployeeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<EmployeeDto>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        var employee = await _context.Users
            .Include(u => u.ModulePermissions)
                .ThenInclude(p => p.Module)
            .FirstOrDefaultAsync(u => u.Id == request.EmployeeId && u.TenantId == tenantId, cancellationToken);

        if (employee == null)
        {
            throw new NotFoundException("Employee", request.EmployeeId);
        }

        employee.FullName = request.Dto.FullName;
        employee.PhoneNumber = request.Dto.PhoneNumber;
        employee.IsActive = request.Dto.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "UpdateEmployee",
            entityName: "ApplicationUser",
            entityId: employee.Id.ToString(),
            newValues: $"FullName: {employee.FullName}, IsActive: {employee.IsActive}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<EmployeeDto>(employee);
        return ApiResponse<EmployeeDto>.SuccessResult(dto, "Employee updated successfully.");
    }
}

public record DeactivateEmployeeCommand(Guid EmployeeId) : IRequest<ApiResponse>;

public class DeactivateEmployeeCommandHandler : IRequestHandler<DeactivateEmployeeCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public DeactivateEmployeeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse> Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        var employee = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.EmployeeId && u.TenantId == tenantId, cancellationToken);

        if (employee == null)
        {
            throw new NotFoundException("Employee", request.EmployeeId);
        }

        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;

        // Revoke active refresh tokens for the deactivated employee
        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == employee.Id && r.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var t in tokens)
        {
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedByIp = _currentUserService.IpAddress;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "DeactivateEmployee",
            entityName: "ApplicationUser",
            entityId: employee.Id.ToString(),
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        return ApiResponse.SuccessResult("Employee deactivated successfully.");
    }
}

public record SetEmployeePermissionsCommand(SetEmployeeModulePermissionsRequestDto Dto) : IRequest<ApiResponse<List<EmployeePermissionDto>>>;

public class SetEmployeePermissionsCommandHandler : IRequestHandler<SetEmployeePermissionsCommand, ApiResponse<List<EmployeePermissionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public SetEmployeePermissionsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<EmployeePermissionDto>>> Handle(SetEmployeePermissionsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new ForbiddenException("Tenant context required.");
        }

        var employee = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Dto.EmployeeUserId && u.TenantId == tenantId, cancellationToken);

        if (employee == null)
        {
            throw new NotFoundException("Employee", request.Dto.EmployeeUserId);
        }

        var requestedModuleIds = request.Dto.Permissions.Select(p => p.ModuleId).Distinct().ToList();

        // Ensure all assigned modules are actually enabled for this tenant by SuperAdmin
        var tenantGrantedModuleIds = await _context.TenantModuleAccesses
            .Where(t => t.TenantId == tenantId.Value && t.IsEnabled && t.Module!.IsGlobalActive)
            .Select(t => t.ModuleId)
            .ToListAsync(cancellationToken);

        var unassignedModules = requestedModuleIds.Except(tenantGrantedModuleIds).ToList();
        if (unassignedModules.Count != 0)
        {
            throw new ValidationException("Permissions", "Cannot assign permissions for modules that are not granted or active for your organization.");
        }

        // Remove old permissions
        var existingPerms = await _context.EmployeeModulePermissions
            .Where(p => p.EmployeeUserId == employee.Id && p.TenantId == tenantId.Value)
            .ToListAsync(cancellationToken);

        _context.EmployeeModulePermissions.RemoveRange(existingPerms);

        // Add new permissions
        var newPerms = new List<EmployeeModulePermission>();
        foreach (var perm in request.Dto.Permissions)
        {
            var empPerm = new EmployeeModulePermission
            {
                Id = Guid.NewGuid(),
                EmployeeUserId = employee.Id,
                ModuleId = perm.ModuleId,
                TenantId = tenantId.Value,
                CanView = perm.CanView,
                CanCreate = perm.CanCreate,
                CanEdit = perm.CanEdit,
                CanDelete = perm.CanDelete,
                AssignedByUserId = _currentUserService.UserId ?? Guid.Empty,
                AssignedAt = DateTime.UtcNow
            };
            newPerms.Add(empPerm);
            _context.EmployeeModulePermissions.Add(empPerm);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "SetEmployeePermissions",
            entityName: "EmployeeModulePermission",
            entityId: employee.Id.ToString(),
            newValues: $"Updated {newPerms.Count} module permissions for {employee.FullName}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var result = await _context.EmployeeModulePermissions
            .Include(p => p.Module)
            .Where(p => p.EmployeeUserId == employee.Id)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<EmployeePermissionDto>>(result);
        return ApiResponse<List<EmployeePermissionDto>>.SuccessResult(dtos, "Employee permissions updated successfully.");
    }
}
