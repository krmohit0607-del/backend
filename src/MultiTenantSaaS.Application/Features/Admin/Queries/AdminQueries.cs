using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Admin;
using MultiTenantSaaS.Domain.Enums;

namespace MultiTenantSaaS.Application.Features.Admin.Queries;

public record GetEmployeesQuery : IRequest<ApiResponse<List<EmployeeDto>>>;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, ApiResponse<List<EmployeeDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetEmployeesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<EmployeeDto>>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new ForbiddenException("Tenant context required.");
        }

        var employees = await _context.Users
            .Include(u => u.ModulePermissions)
                .ThenInclude(p => p.Module)
            .Where(u => u.TenantId == tenantId.Value && u.Id != _currentUserService.UserId)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<EmployeeDto>>(employees);
        return ApiResponse<List<EmployeeDto>>.SuccessResult(dtos);
    }
}

public record GetAdminModulesQuery : IRequest<ApiResponse<List<AdminTenantModuleDto>>>;

public class GetAdminModulesQueryHandler : IRequestHandler<GetAdminModulesQuery, ApiResponse<List<AdminTenantModuleDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAdminModulesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<List<AdminTenantModuleDto>>> Handle(GetAdminModulesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new ForbiddenException("Tenant context required.");
        }

        var modules = await _context.TenantModuleAccesses
            .Include(t => t.Module)
            .Where(t => t.TenantId == tenantId.Value && t.IsEnabled && t.Module!.IsGlobalActive)
            .Select(t => new AdminTenantModuleDto
            {
                ModuleId = t.ModuleId,
                ModuleName = t.Module!.Name,
                Description = t.Module.Description,
                IsEnabledForTenant = t.IsEnabled,
                IsGlobalActive = t.Module.IsGlobalActive
            })
            .OrderBy(m => m.ModuleName)
            .ToListAsync(cancellationToken);

        return ApiResponse<List<AdminTenantModuleDto>>.SuccessResult(modules);
    }
}
