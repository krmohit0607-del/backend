using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Employee;

namespace MultiTenantSaaS.Application.Features.Employee.Queries;

public record GetMyModulesQuery : IRequest<ApiResponse<List<EmployeeModuleAccessDto>>>;

public class GetMyModulesQueryHandler : IRequestHandler<GetMyModulesQuery, ApiResponse<List<EmployeeModuleAccessDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyModulesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<List<EmployeeModuleAccessDto>>> Handle(GetMyModulesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentUserService.TenantId;

        if (!userId.HasValue)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        // Query permissions for employee where:
        // 1. Employee has module permission
        // 2. Module is globally active
        // 3. Module is enabled for the tenant
        var permissions = await _context.EmployeeModulePermissions
            .Include(p => p.Module)
            .Where(p => p.EmployeeUserId == userId.Value &&
                        p.Module!.IsGlobalActive &&
                        _context.TenantModuleAccesses.Any(t => t.TenantId == tenantId && t.ModuleId == p.ModuleId && t.IsEnabled))
            .Select(p => new EmployeeModuleAccessDto
            {
                ModuleId = p.ModuleId,
                ModuleName = p.Module!.Name,
                Description = p.Module.Description,
                CanView = p.CanView,
                CanCreate = p.CanCreate,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete
            })
            .OrderBy(m => m.ModuleName)
            .ToListAsync(cancellationToken);

        return ApiResponse<List<EmployeeModuleAccessDto>>.SuccessResult(permissions);
    }
}
