using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Emissions;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Emissions.Queries;

public record GetEmissionsByVoyageQuery(string VoyageCode) : IRequest<ApiResponse<EmissionsRecordDto>>;

public class GetEmissionsByVoyageQueryHandler : IRequestHandler<GetEmissionsByVoyageQuery, ApiResponse<EmissionsRecordDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEmissionsByVoyageQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<EmissionsRecordDto>> Handle(GetEmissionsByVoyageQuery request, CancellationToken cancellationToken)
    {
        var cleanCode = request.VoyageCode.Trim().ToUpperInvariant();
        var record = await _context.EmissionsRecords
            .FirstOrDefaultAsync(e => e.VoyageCode.ToUpper() == cleanCode, cancellationToken);

        if (record == null)
        {
            throw new NotFoundException("EmissionsRecord", request.VoyageCode);
        }

        var dto = _mapper.Map<EmissionsRecordDto>(record);
        return ApiResponse<EmissionsRecordDto>.SuccessResult(dto);
    }
}

public record GetAllEmissionsQuery : IRequest<ApiResponse<List<EmissionsRecordDto>>>;

public class GetAllEmissionsQueryHandler : IRequestHandler<GetAllEmissionsQuery, ApiResponse<List<EmissionsRecordDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllEmissionsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<EmissionsRecordDto>>> Handle(GetAllEmissionsQuery request, CancellationToken cancellationToken)
    {
        var list = await _context.EmissionsRecords
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<EmissionsRecordDto>>(list);
        return ApiResponse<List<EmissionsRecordDto>>.SuccessResult(dtos);
    }
}

public record GetEmissionsScenariosQuery(string VoyageCode) : IRequest<ApiResponse<List<EmissionsScenarioDto>>>;

public class GetEmissionsScenariosQueryHandler : IRequestHandler<GetEmissionsScenariosQuery, ApiResponse<List<EmissionsScenarioDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEmissionsScenariosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<EmissionsScenarioDto>>> Handle(GetEmissionsScenariosQuery request, CancellationToken cancellationToken)
    {
        var cleanCode = request.VoyageCode.Trim().ToUpperInvariant();
        var list = await _context.EmissionsScenarios
            .Where(s => s.VoyageCode.ToUpper() == cleanCode)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
        return ApiResponse<List<EmissionsScenarioDto>>.SuccessResult(_mapper.Map<List<EmissionsScenarioDto>>(list));
    }
}
