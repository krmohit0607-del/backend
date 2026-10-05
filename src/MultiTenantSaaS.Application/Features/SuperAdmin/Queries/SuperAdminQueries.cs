using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.SuperAdmin;

namespace MultiTenantSaaS.Application.Features.SuperAdmin.Queries;

public record GetAdminsQuery : IRequest<ApiResponse<List<TenantDto>>>;

public class GetAdminsQueryHandler : IRequestHandler<GetAdminsQuery, ApiResponse<List<TenantDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAdminsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<TenantDto>>> Handle(GetAdminsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _context.Tenants
            .Include(t => t.AdminUser)
            .Include(t => t.Users)
            .Include(t => t.TenantModuleAccesses)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<TenantDto>>(tenants);
        return ApiResponse<List<TenantDto>>.SuccessResult(dtos);
    }
}

public record GetModulesQuery : IRequest<ApiResponse<List<ModuleDto>>>;

public class GetModulesQueryHandler : IRequestHandler<GetModulesQuery, ApiResponse<List<ModuleDto>>>
{
    private static readonly string[] SystemModuleNames = { "CHARTERING", "OPERATIONS", "BUNKER", "POSTFIX", "EMISSIONS", "PERFORMANCE", "ACCOUNTS" };
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetModulesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ModuleDto>>> Handle(GetModulesQuery request, CancellationToken cancellationToken)
    {
        var modules = await _context.Modules
            .Where(m => SystemModuleNames.Contains(m.NormalizedName))
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<ModuleDto>>(modules);
        return ApiResponse<List<ModuleDto>>.SuccessResult(dtos);
    }
}

public record GetTenantModuleAccessQuery(Guid TenantId) : IRequest<ApiResponse<List<TenantModuleAccessDto>>>;

public class GetTenantModuleAccessQueryHandler : IRequestHandler<GetTenantModuleAccessQuery, ApiResponse<List<TenantModuleAccessDto>>>
{
    private static readonly string[] SystemModuleNames = { "CHARTERING", "OPERATIONS", "BUNKER", "POSTFIX", "EMISSIONS", "PERFORMANCE", "ACCOUNTS" };
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTenantModuleAccessQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<TenantModuleAccessDto>>> Handle(GetTenantModuleAccessQuery request, CancellationToken cancellationToken)
    {
        var tenantExists = await _context.Tenants.AnyAsync(t => t.Id == request.TenantId, cancellationToken);
        if (!tenantExists)
        {
            throw new NotFoundException("Tenant", request.TenantId);
        }

        var accessList = await _context.TenantModuleAccesses
            .Include(a => a.Module)
            .Include(a => a.GrantedByUser)
            .Where(a => a.TenantId == request.TenantId && a.Module != null && SystemModuleNames.Contains(a.Module.NormalizedName))
            .OrderBy(a => a.Module != null ? a.Module.Name : string.Empty)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<TenantModuleAccessDto>>(accessList);
        return ApiResponse<List<TenantModuleAccessDto>>.SuccessResult(dtos);
    }
}
