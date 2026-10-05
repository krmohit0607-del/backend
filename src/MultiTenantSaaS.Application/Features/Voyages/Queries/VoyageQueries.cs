using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Voyages;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Voyages.Queries;

public record GetVoyagesQuery : IRequest<ApiResponse<List<VoyageDto>>>;

public class GetVoyagesQueryHandler : IRequestHandler<GetVoyagesQuery, ApiResponse<List<VoyageDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVoyagesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<VoyageDto>>> Handle(GetVoyagesQuery request, CancellationToken cancellationToken)
    {
        var voyages = await _context.Voyages
            .Include(v => v.Passages)
                .ThenInclude(p => p.Legs)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<VoyageDto>>(voyages);
        return ApiResponse<List<VoyageDto>>.SuccessResult(dtos);
    }
}

public record GetVoyageByIdQuery(string IdOrCode) : IRequest<ApiResponse<VoyageDto>>;

public class GetVoyageByIdQueryHandler : IRequestHandler<GetVoyageByIdQuery, ApiResponse<VoyageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVoyageByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<VoyageDto>> Handle(GetVoyageByIdQuery request, CancellationToken cancellationToken)
    {
        Voyage? voyage = null;

        if (Guid.TryParse(request.IdOrCode, out var guid))
        {
            voyage = await _context.Voyages
                .Include(v => v.Passages)
                    .ThenInclude(p => p.Legs)
                .FirstOrDefaultAsync(v => v.Id == guid, cancellationToken);
        }

        if (voyage == null)
        {
            var normalizedCode = request.IdOrCode.Trim().ToUpperInvariant();
            voyage = await _context.Voyages
                .Include(v => v.Passages)
                    .ThenInclude(p => p.Legs)
                .FirstOrDefaultAsync(v => v.VoyageCode.ToUpper() == normalizedCode, cancellationToken);
        }

        if (voyage == null)
        {
            throw new NotFoundException("Voyage", request.IdOrCode);
        }

        var dto = _mapper.Map<VoyageDto>(voyage);
        return ApiResponse<VoyageDto>.SuccessResult(dto);
    }
}

public record GetVoyageOrdersQuery : IRequest<ApiResponse<List<VoyageOrderDto>>>;

public class GetVoyageOrdersQueryHandler : IRequestHandler<GetVoyageOrdersQuery, ApiResponse<List<VoyageOrderDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVoyageOrdersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<VoyageOrderDto>>> Handle(GetVoyageOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.VoyageOrders
            .Include(o => o.Voyages)
                .ThenInclude(v => v.Passages)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<VoyageOrderDto>>(orders);
        return ApiResponse<List<VoyageOrderDto>>.SuccessResult(dtos);
    }
}
