using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Bunker;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Bunker.Queries;

public record GetBunkerRequirementsQuery : IRequest<ApiResponse<List<BunkerRequirementDto>>>;

public class GetBunkerRequirementsQueryHandler : IRequestHandler<GetBunkerRequirementsQuery, ApiResponse<List<BunkerRequirementDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetBunkerRequirementsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<BunkerRequirementDto>>> Handle(GetBunkerRequirementsQuery request, CancellationToken cancellationToken)
    {
        var list = await _context.BunkerRequirements
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<BunkerRequirementDto>>(list);
        return ApiResponse<List<BunkerRequirementDto>>.SuccessResult(dtos);
    }
}

public record GetBunkerRequirementByIdQuery(string IdOrNo) : IRequest<ApiResponse<BunkerRequirementDto>>;

public class GetBunkerRequirementByIdQueryHandler : IRequestHandler<GetBunkerRequirementByIdQuery, ApiResponse<BunkerRequirementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetBunkerRequirementByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<BunkerRequirementDto>> Handle(GetBunkerRequirementByIdQuery request, CancellationToken cancellationToken)
    {
        BunkerRequirement? req = null;

        if (Guid.TryParse(request.IdOrNo, out var guid))
        {
            req = await _context.BunkerRequirements.FirstOrDefaultAsync(b => b.Id == guid, cancellationToken);
        }

        if (req == null)
        {
            var cleanNo = request.IdOrNo.Trim().ToUpperInvariant();
            req = await _context.BunkerRequirements.FirstOrDefaultAsync(b => b.RequirementNo.ToUpper() == cleanNo, cancellationToken);
        }

        if (req == null)
        {
            throw new NotFoundException("BunkerRequirement", request.IdOrNo);
        }

        var dto = _mapper.Map<BunkerRequirementDto>(req);
        return ApiResponse<BunkerRequirementDto>.SuccessResult(dto);
    }
}
