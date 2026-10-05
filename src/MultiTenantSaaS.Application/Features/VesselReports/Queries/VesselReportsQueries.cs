using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.VesselReports;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.VesselReports.Queries;

public record GetVesselReportsQuery(string? Imo = null, string? VoyageCode = null) : IRequest<ApiResponse<List<VesselReportDto>>>;

public class GetVesselReportsQueryHandler : IRequestHandler<GetVesselReportsQuery, ApiResponse<List<VesselReportDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVesselReportsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<VesselReportDto>>> Handle(GetVesselReportsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.VesselReports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Imo))
        {
            var cleanImo = request.Imo.Trim();
            query = query.Where(r => r.Imo == cleanImo);
        }

        if (!string.IsNullOrWhiteSpace(request.VoyageCode))
        {
            var cleanCode = request.VoyageCode.Trim().ToUpperInvariant();
            query = query.Where(r => r.VoyageCode != null && r.VoyageCode.ToUpper() == cleanCode);
        }

        var reports = await query
            .OrderByDescending(r => r.ReportDateTime)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<VesselReportDto>>(reports);
        return ApiResponse<List<VesselReportDto>>.SuccessResult(dtos);
    }
}

public record GetVesselReportByIdQuery(string IdOrNo) : IRequest<ApiResponse<VesselReportDto>>;

public class GetVesselReportByIdQueryHandler : IRequestHandler<GetVesselReportByIdQuery, ApiResponse<VesselReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVesselReportByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<VesselReportDto>> Handle(GetVesselReportByIdQuery request, CancellationToken cancellationToken)
    {
        VesselReport? report = null;

        if (Guid.TryParse(request.IdOrNo, out var guid))
        {
            report = await _context.VesselReports.FirstOrDefaultAsync(r => r.Id == guid, cancellationToken);
        }

        if (report == null)
        {
            var cleanNo = request.IdOrNo.Trim().ToUpperInvariant();
            report = await _context.VesselReports.FirstOrDefaultAsync(r => r.ReportNo.ToUpper() == cleanNo, cancellationToken);
        }

        if (report == null)
        {
            throw new NotFoundException("VesselReport", request.IdOrNo);
        }

        var dto = _mapper.Map<VesselReportDto>(report);
        return ApiResponse<VesselReportDto>.SuccessResult(dto);
    }
}

public record GetVoyageProgressQuery(string? Imo = null, string? VoyageCode = null) : IRequest<ApiResponse<VoyageProgressDto>>;

public class GetVoyageProgressQueryHandler : IRequestHandler<GetVoyageProgressQuery, ApiResponse<VoyageProgressDto>>
{
    private readonly IApplicationDbContext _context;

    public GetVoyageProgressQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<VoyageProgressDto>> Handle(GetVoyageProgressQuery request, CancellationToken cancellationToken)
    {
        var query = _context.VesselReports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Imo))
        {
            var cleanImo = request.Imo.Trim();
            query = query.Where(r => r.Imo == cleanImo);
        }

        if (!string.IsNullOrWhiteSpace(request.VoyageCode))
        {
            var cleanCode = request.VoyageCode.Trim().ToUpperInvariant();
            query = query.Where(r => r.VoyageCode != null && r.VoyageCode.ToUpper() == cleanCode);
        }

        var latest = await query
            .OrderByDescending(r => r.ReportDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        var progress = new VoyageProgressDto
        {
            VoyageCode = request.VoyageCode,
            Imo = request.Imo,
            HasReports = latest != null,
        };

        if (latest != null)
        {
            // Departure / at-sea reports mean the vessel is underway toward the next port;
            // arrival / in-port reports mean it is at the current port.
            var type = latest.ReportType ?? string.Empty;
            var atSea = type.Contains("COSP", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Departure", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Sea", StringComparison.OrdinalIgnoreCase)
                || type.Contains("BOSP", StringComparison.OrdinalIgnoreCase);

            progress.VesselName = latest.VesselName;
            progress.Imo = latest.Imo;
            progress.VoyageCode = latest.VoyageCode ?? request.VoyageCode;
            progress.LatestReportNo = latest.ReportNo;
            progress.ReportType = latest.ReportType;
            progress.ReportDateTime = latest.ReportDateTime;
            progress.CurrentPort = latest.CurrentPort;
            progress.NextPort = latest.NextPort;
            progress.EtaNextPort = latest.EtaNextPort;
            progress.Latitude = latest.Latitude;
            progress.Longitude = latest.Longitude;
            progress.AtSea = atSea;
            progress.Stage = atSea ? (latest.NextPort ?? latest.CurrentPort) : latest.CurrentPort;
        }

        return ApiResponse<VoyageProgressDto>.SuccessResult(progress);
    }
}

