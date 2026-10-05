using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Chartering;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Chartering.Queries;

public record GetVoyageEstimatesQuery : IRequest<ApiResponse<List<VoyageEstimateDto>>>;

public class GetVoyageEstimatesQueryHandler : IRequestHandler<GetVoyageEstimatesQuery, ApiResponse<List<VoyageEstimateDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetVoyageEstimatesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<VoyageEstimateDto>>> Handle(GetVoyageEstimatesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        // Only retrieve estimations that have meaningful data (vessel name set).
        // This filters out untitled/empty estimations that were auto-saved without user input.
        var estimates = await _context.VoyageEstimates
            .Where(e => e.TenantId == tenantId && !string.IsNullOrWhiteSpace(e.VesselName))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<VoyageEstimateDto>>(estimates);
        return ApiResponse<List<VoyageEstimateDto>>.SuccessResult(dtos);
    }
}

public record GetCargoBookQuery : IRequest<ApiResponse<List<CargoBookDto>>>;

public class GetCargoBookQueryHandler : IRequestHandler<GetCargoBookQuery, ApiResponse<List<CargoBookDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetCargoBookQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<CargoBookDto>>> Handle(GetCargoBookQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        var entries = await _context.CargoBookEntries
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<CargoBookDto>>(entries);
        return ApiResponse<List<CargoBookDto>>.SuccessResult(dtos);
    }
}

public record GetTonnageBookQuery : IRequest<ApiResponse<List<TonnageBookDto>>>;

public class GetTonnageBookQueryHandler : IRequestHandler<GetTonnageBookQuery, ApiResponse<List<TonnageBookDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetTonnageBookQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<TonnageBookDto>>> Handle(GetTonnageBookQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        var entries = await _context.TonnageBookEntries
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<TonnageBookDto>>(entries);
        return ApiResponse<List<TonnageBookDto>>.SuccessResult(dtos);
    }
}
