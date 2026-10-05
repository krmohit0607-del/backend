using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Emissions;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Emissions.Commands;

public record SaveEmissionsRecordCommand(SaveEmissionsRecordRequestDto Dto) : IRequest<ApiResponse<EmissionsRecordDto>>;

public class SaveEmissionsRecordCommandHandler : IRequestHandler<SaveEmissionsRecordCommand, ApiResponse<EmissionsRecordDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public SaveEmissionsRecordCommandHandler(
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

    public async Task<ApiResponse<EmissionsRecordDto>> Handle(SaveEmissionsRecordCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        var cleanCode = request.Dto.VoyageCode.Trim().ToUpperInvariant();

        var record = await _context.EmissionsRecords
            .FirstOrDefaultAsync(e => e.VoyageCode.ToUpper() == cleanCode && e.TenantId == tenantId, cancellationToken);

        if (record == null)
        {
            record = new EmissionsRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VoyageCode = cleanCode,
                VesselName = request.Dto.VesselName ?? "Unknown Vessel",
                ComplianceYear = request.Dto.ComplianceYear ?? DateTime.UtcNow.Year.ToString(),
                Trade = request.Dto.Trade,
                EuaPriceEur = request.Dto.EuaPriceEur ?? "72.50",
                Co2AdjustmentT = request.Dto.Co2AdjustmentT ?? "0",
                ComplianceJson = request.Dto.ComplianceJson,
                AdjustmentsJson = request.Dto.AdjustmentsJson,
                MetricsJson = request.Dto.MetricsJson,
                ApprovedBy = request.Dto.ApprovedBy,
                ApprovedDate = request.Dto.ApprovedDate,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _currentUserService.UserId
            };
            _context.EmissionsRecords.Add(record);
        }
        else
        {
            record.VesselName = request.Dto.VesselName ?? record.VesselName;
            record.ComplianceYear = request.Dto.ComplianceYear ?? record.ComplianceYear;
            record.Trade = request.Dto.Trade ?? record.Trade;
            record.EuaPriceEur = request.Dto.EuaPriceEur ?? record.EuaPriceEur;
            record.Co2AdjustmentT = request.Dto.Co2AdjustmentT ?? record.Co2AdjustmentT;
            record.ComplianceJson = request.Dto.ComplianceJson ?? record.ComplianceJson;
            record.AdjustmentsJson = request.Dto.AdjustmentsJson ?? record.AdjustmentsJson;
            record.MetricsJson = request.Dto.MetricsJson ?? record.MetricsJson;
            record.ApprovedBy = request.Dto.ApprovedBy ?? record.ApprovedBy;
            record.ApprovedDate = request.Dto.ApprovedDate ?? record.ApprovedDate;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedByUserId = _currentUserService.UserId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "SaveEmissionsRecord",
            entityName: "EmissionsRecord",
            entityId: record.Id.ToString(),
            newValues: $"Voyage: {record.VoyageCode}, Year: {record.ComplianceYear}, ApprovedBy: {record.ApprovedBy}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<EmissionsRecordDto>(record);
        return ApiResponse<EmissionsRecordDto>.SuccessResult(dto, "Emissions record saved successfully.");
    }
}

public record SaveEmissionsScenarioCommand(SaveEmissionsScenarioRequestDto Dto, Guid? Id) : IRequest<ApiResponse<EmissionsScenarioDto>>;

public class SaveEmissionsScenarioCommandHandler : IRequestHandler<SaveEmissionsScenarioCommand, ApiResponse<EmissionsScenarioDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public SaveEmissionsScenarioCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<EmissionsScenarioDto>> Handle(SaveEmissionsScenarioCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        EmissionsScenario? scenario = request.Id.HasValue
            ? await _context.EmissionsScenarios.FirstOrDefaultAsync(s => s.Id == request.Id.Value && s.TenantId == tenantId, cancellationToken)
            : null;

        if (scenario == null)
        {
            scenario = new EmissionsScenario
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VoyageCode = request.Dto.VoyageCode.Trim(),
                Name = request.Dto.Name,
                InputsJson = request.Dto.InputsJson,
                MetricsJson = request.Dto.MetricsJson,
                Notes = request.Dto.Notes,
                CreatedByName = request.Dto.CreatedByName,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _currentUserService.UserId,
            };
            _context.EmissionsScenarios.Add(scenario);
        }
        else
        {
            scenario.Name = request.Dto.Name;
            scenario.InputsJson = request.Dto.InputsJson ?? scenario.InputsJson;
            scenario.MetricsJson = request.Dto.MetricsJson ?? scenario.MetricsJson;
            scenario.Notes = request.Dto.Notes ?? scenario.Notes;
            scenario.UpdatedAt = DateTime.UtcNow;
            scenario.UpdatedByUserId = _currentUserService.UserId;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<EmissionsScenarioDto>.SuccessResult(_mapper.Map<EmissionsScenarioDto>(scenario), "Scenario saved.");
    }
}

public record DeleteEmissionsScenarioCommand(Guid Id) : IRequest<ApiResponse>;

public class DeleteEmissionsScenarioCommandHandler : IRequestHandler<DeleteEmissionsScenarioCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteEmissionsScenarioCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse> Handle(DeleteEmissionsScenarioCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        var scenario = await _context.EmissionsScenarios.FirstOrDefaultAsync(s => s.Id == request.Id && s.TenantId == tenantId, cancellationToken);
        if (scenario != null)
        {
            _context.EmissionsScenarios.Remove(scenario);
            await _context.SaveChangesAsync(cancellationToken);
        }
        return ApiResponse.SuccessResult("Scenario deleted.");
    }
}
