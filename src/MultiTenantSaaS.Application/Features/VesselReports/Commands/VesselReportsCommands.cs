using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.VesselReports;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.VesselReports.Commands;

public record SubmitVesselReportCommand(SubmitVesselReportRequestDto Dto) : IRequest<ApiResponse<VesselReportDto>>;

public class SubmitVesselReportCommandHandler : IRequestHandler<SubmitVesselReportCommand, ApiResponse<VesselReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public SubmitVesselReportCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<VesselReportDto>> Handle(SubmitVesselReportCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var repNo = request.Dto.ReportNo;
        if (string.IsNullOrWhiteSpace(repNo))
        {
            var yy = DateTime.UtcNow.ToString("yy");
            var mm = DateTime.UtcNow.ToString("MM");
            var count = await _context.VesselReports.CountAsync(cancellationToken) + 1;
            repNo = $"VR-{yy}{mm}-{count:D3}";
        }

        var report = new VesselReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportNo = repNo,
            ReportType = request.Dto.ReportType,
            ReportSubtype = request.Dto.ReportSubtype,
            VesselName = request.Dto.VesselName,
            Imo = request.Dto.Imo,
            VoyageCode = request.Dto.VoyageCode,
            ReportDateTime = request.Dto.ReportDateTime ?? DateTime.UtcNow,
            Latitude = request.Dto.Latitude,
            Longitude = request.Dto.Longitude,
            CurrentPort = request.Dto.CurrentPort,
            NextPort = request.Dto.NextPort,
            EtaNextPort = request.Dto.EtaNextPort,
            SteamingHours = request.Dto.SteamingHours,
            DistanceObserved = request.Dto.DistanceObserved,
            DistanceEngine = request.Dto.DistanceEngine,
            SpeedObserved = request.Dto.SpeedObserved,
            SpeedEngine = request.Dto.SpeedEngine,
            SlipPercent = request.Dto.SlipPercent,
            Course = request.Dto.Course,
            WindDirection = request.Dto.WindDirection,
            WindForce = request.Dto.WindForce,
            SeaState = request.Dto.SeaState,
            Swell = request.Dto.Swell,
            Barometer = request.Dto.Barometer,
            AirTemp = request.Dto.AirTemp,
            SeaTemp = request.Dto.SeaTemp,
            VlsfoCons = request.Dto.VlsfoCons,
            VlsfoRob = request.Dto.VlsfoRob,
            LsmgoCons = request.Dto.LsmgoCons,
            LsmgoRob = request.Dto.LsmgoRob,
            HfoCons = request.Dto.HfoCons,
            HfoRob = request.Dto.HfoRob,
            MgoCons = request.Dto.MgoCons,
            MgoRob = request.Dto.MgoRob,
            Rpm = request.Dto.Rpm,
            EngineKw = request.Dto.EngineKw,
            DraftFwd = request.Dto.DraftFwd,
            DraftAft = request.Dto.DraftAft,
            Remarks = request.Dto.Remarks,
            FormValuesJson = request.Dto.FormValuesJson,
            FormattedReportText = request.Dto.FormattedReportText,
            Status = request.Dto.Status ?? "Verified",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.UserId
        };

        _context.VesselReports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "SubmitVesselReport",
            entityName: "VesselReport",
            entityId: report.Id.ToString(),
            newValues: $"Report: {report.ReportNo}, Vessel: {report.VesselName}, Type: {report.ReportType}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<VesselReportDto>(report);
        return ApiResponse<VesselReportDto>.SuccessResult(dto, "Vessel report submitted successfully.");
    }
}
