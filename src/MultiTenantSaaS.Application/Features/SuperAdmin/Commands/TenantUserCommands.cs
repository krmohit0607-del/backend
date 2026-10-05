using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.SuperAdmin;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.Application.Features.SuperAdmin.Commands;

public record CreateTenantUserCommand(CreateTenantUserRequestDto Dto) : IRequest<ApiResponse<TenantUserDto>>;
public record GetTenantUsersQuery(Guid TenantId) : IRequest<ApiResponse<List<TenantUserDto>>>;
public record SetTenantUserPermissionsCommand(TenantUserPermissionDto Dto) : IRequest<ApiResponse<TenantUserDto>>;
public record UpdateTenantUserCommand(Guid UserId, UpdateTenantUserRequestDto Dto) : IRequest<ApiResponse<TenantUserDto>>;
public record DeleteTenantUserCommand(Guid UserId) : IRequest<ApiResponse>;

public class TenantUserCommandHandler :
    IRequestHandler<CreateTenantUserCommand, ApiResponse<TenantUserDto>>,
    IRequestHandler<SetTenantUserPermissionsCommand, ApiResponse<TenantUserDto>>
    , IRequestHandler<UpdateTenantUserCommand, ApiResponse<TenantUserDto>>
    , IRequestHandler<DeleteTenantUserCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _currentUser;

    public TenantUserCommandHandler(IApplicationDbContext context, IIdentityService identity, ICurrentUserService currentUser)
    {
        _context = context;
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TenantUserDto>> Handle(CreateTenantUserCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == request.Dto.TenantId, cancellationToken);
        if (tenant == null) throw new NotFoundException("Tenant", request.Dto.TenantId);
        var role = request.Dto.Role switch
        {
            "Manager" => UserRoles.Admin,
            "Executive" => UserRoles.Employee,
            "Vessel" => UserRoles.VesselMaster,
            _ => request.Dto.Role
        };
        if (role is UserRoles.SuperAdmin)
            throw new ForbiddenException("A tenant user cannot be a SuperAdmin.");
        if (!UserRoles.All.Contains(role) || role == UserRoles.SuperAdmin)
            throw new ValidationException("Role", "Role must be Admin, Employee, or VesselMaster.");
        if (await _identity.FindByEmailAsync(request.Dto.Email) != null)
            throw new ValidationException("Email", "A user with this email address already exists.");

        var result = await _identity.CreateUserAsync(request.Dto.Email, request.Dto.FullName, request.Dto.Password,
            role, request.Dto.TenantId, _currentUser.UserId, request.Dto.PhoneNumber);
        if (!result.Success)
            throw new ApiException($"Failed to create user: {string.Join(", ", result.Errors)}", 400, result.Errors.ToList());

        result.User.AssignedVesselImo = request.Dto.AssignedVesselImo;
        result.User.AssignedVesselName = request.Dto.AssignedVesselName;
        await _identity.UpdateUserAsync(result.User);
        var enabledModuleIds = await _context.TenantModuleAccesses
            .Where(a => a.TenantId == request.Dto.TenantId && a.IsEnabled && a.Module!.IsGlobalActive)
            .Select(a => a.ModuleId)
            .ToListAsync(cancellationToken);
        foreach (var moduleId in request.Dto.InitialModuleIds.Distinct().Where(enabledModuleIds.Contains))
        {
            _context.EmployeeModulePermissions.Add(new EmployeeModulePermission
            {
                Id = Guid.NewGuid(), EmployeeUserId = result.User.Id, ModuleId = moduleId,
                TenantId = request.Dto.TenantId, CanView = true, AssignedByUserId = _currentUser.UserId ?? Guid.Empty,
                AssignedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<TenantUserDto>.SuccessResult(await MapUser(result.User.Id, request.Dto.TenantId, cancellationToken), "User created successfully.");
    }

    public async Task<ApiResponse<TenantUserDto>> Handle(SetTenantUserPermissionsCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Dto.UserId, cancellationToken);
        if (user == null || !user.TenantId.HasValue) throw new NotFoundException("User", request.Dto.UserId);
        var allowed = await _context.TenantModuleAccesses
            .Where(a => a.TenantId == user.TenantId.Value && a.IsEnabled && a.Module!.IsGlobalActive)
            .Select(a => a.ModuleId).ToListAsync(cancellationToken);
        var requested = request.Dto.Permissions.Where(p => allowed.Contains(p.ModuleId)).ToList();
        var current = await _context.EmployeeModulePermissions.Where(p => p.EmployeeUserId == user.Id).ToListAsync(cancellationToken);
        _context.EmployeeModulePermissions.RemoveRange(current);
        foreach (var permission in requested)
        {
            _context.EmployeeModulePermissions.Add(new EmployeeModulePermission
            {
                Id = Guid.NewGuid(), EmployeeUserId = user.Id, ModuleId = permission.ModuleId,
                TenantId = user.TenantId, CanView = permission.CanView, CanCreate = permission.CanCreate,
                CanEdit = permission.CanEdit, CanDelete = permission.CanDelete,
                AssignedByUserId = _currentUser.UserId ?? Guid.Empty, AssignedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<TenantUserDto>.SuccessResult(await MapUser(user.Id, user.TenantId.Value, cancellationToken), "User permissions updated successfully.");
    }

    public async Task<ApiResponse<TenantUserDto>> Handle(UpdateTenantUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId.HasValue, cancellationToken);
        if (user == null) throw new NotFoundException("User", request.UserId);
        user.FullName = request.Dto.FullName;
        user.PhoneNumber = request.Dto.PhoneNumber;
        user.AssignedVesselImo = request.Dto.AssignedVesselImo;
        user.AssignedVesselName = request.Dto.AssignedVesselName;
        user.IsActive = request.Dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<TenantUserDto>.SuccessResult(await MapUser(user.Id, user.TenantId!.Value, cancellationToken), "User updated successfully.");
    }

    public async Task<ApiResponse> Handle(DeleteTenantUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId.HasValue, cancellationToken);
        if (user == null) throw new NotFoundException("User", request.UserId);
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse.SuccessResult("User deactivated successfully.");
    }

    private async Task<TenantUserDto> MapUser(Guid id, Guid tenantId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.Include(u => u.ModulePermissions).ThenInclude(p => p.Module)
            .FirstAsync(u => u.Id == id, cancellationToken);
        var role = await _identity.GetUserRoleAsync(user);
        return new TenantUserDto
        {
            Id = user.Id, TenantId = tenantId, FullName = user.FullName, Email = user.Email ?? string.Empty,
            Role = role, PhoneNumber = user.PhoneNumber, AssignedVesselImo = user.AssignedVesselImo,
            AssignedVesselName = user.AssignedVesselName, IsActive = user.IsActive, CreatedAt = user.CreatedAt,
            Permissions = user.ModulePermissions.Select(p => new TenantUserPermissionViewDto
            {
                ModuleId = p.ModuleId, ModuleName = p.Module?.Name ?? string.Empty, CanView = p.CanView,
                CanCreate = p.CanCreate, CanEdit = p.CanEdit, CanDelete = p.CanDelete
            }).ToList()
        };
    }
}

public class GetTenantUsersQueryHandler : IRequestHandler<GetTenantUsersQuery, ApiResponse<List<TenantUserDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identity;
    public GetTenantUsersQueryHandler(IApplicationDbContext context, IIdentityService identity) { _context = context; _identity = identity; }
    public async Task<ApiResponse<List<TenantUserDto>>> Handle(GetTenantUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _context.Users.Include(u => u.ModulePermissions).ThenInclude(p => p.Module)
            .Where(u => u.TenantId == request.TenantId).OrderBy(u => u.FullName).ToListAsync(cancellationToken);
        var result = new List<TenantUserDto>();
        foreach (var user in users)
        {
            var role = await _identity.GetUserRoleAsync(user);
            result.Add(new TenantUserDto { Id = user.Id, TenantId = request.TenantId, FullName = user.FullName,
                Email = user.Email ?? string.Empty, Role = role, PhoneNumber = user.PhoneNumber,
                AssignedVesselImo = user.AssignedVesselImo, AssignedVesselName = user.AssignedVesselName,
                IsActive = user.IsActive, CreatedAt = user.CreatedAt,
                Permissions = user.ModulePermissions.Select(p => new TenantUserPermissionViewDto { ModuleId = p.ModuleId,
                    ModuleName = p.Module?.Name ?? string.Empty, CanView = p.CanView, CanCreate = p.CanCreate,
                    CanEdit = p.CanEdit, CanDelete = p.CanDelete }).ToList() });
        }
        return ApiResponse<List<TenantUserDto>>.SuccessResult(result);
    }
}