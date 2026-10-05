using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Vessels;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Vessels.Queries;

public record GetVesselsQuery : IRequest<ApiResponse<List<VesselDto>>>;

public class GetVesselsQueryHandler : IRequestHandler<GetVesselsQuery, ApiResponse<List<VesselDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVesselsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<VesselDto>>> Handle(GetVesselsQuery request, CancellationToken cancellationToken)
    {
        var vessels = await _context.Vessels
            .Include(v => v.History)
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<VesselDto>>(vessels);
        return ApiResponse<List<VesselDto>>.SuccessResult(dtos);
    }
}

public record GetVesselByIdQuery(string IdOrImo) : IRequest<ApiResponse<VesselDto>>;

public class GetVesselByIdQueryHandler : IRequestHandler<GetVesselByIdQuery, ApiResponse<VesselDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVesselByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<VesselDto>> Handle(GetVesselByIdQuery request, CancellationToken cancellationToken)
    {
        Vessel? vessel = null;

        if (Guid.TryParse(request.IdOrImo, out var guid))
        {
            vessel = await _context.Vessels
                .Include(v => v.History.OrderByDescending(h => h.ChangedAt))
                .FirstOrDefaultAsync(v => v.Id == guid, cancellationToken);
        }

        if (vessel == null)
        {
            var raw = request.IdOrImo.Trim().Replace("ves-", "");
            vessel = await _context.Vessels
                .Include(v => v.History.OrderByDescending(h => h.ChangedAt))
                .FirstOrDefaultAsync(v => v.Imo == raw || v.Imo == request.IdOrImo.Trim() || v.Name.ToLower() == request.IdOrImo.Trim().ToLower(), cancellationToken);
        }

        if (vessel == null)
        {
            throw new NotFoundException("Vessel", request.IdOrImo);
        }

        var dto = _mapper.Map<VesselDto>(vessel);
        return ApiResponse<VesselDto>.SuccessResult(dto);
    }
}
