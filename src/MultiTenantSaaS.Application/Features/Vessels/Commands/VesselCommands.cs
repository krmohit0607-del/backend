using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Vessels;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Vessels.Commands;

public record CreateVesselCommand(CreateVesselRequestDto Dto) : IRequest<ApiResponse<VesselDto>>;

public class CreateVesselCommandHandler : IRequestHandler<CreateVesselCommand, ApiResponse<VesselDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public CreateVesselCommandHandler(
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

    public async Task<ApiResponse<VesselDto>> Handle(CreateVesselCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var existing = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Imo == request.Dto.Imo && v.TenantId == tenantId, cancellationToken);

        if (existing != null)
        {
            throw new ValidationException("Imo", $"A vessel with IMO {request.Dto.Imo} already exists in your fleet.");
        }

        var vessel = new Vessel
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Dto.Name,
            ShortName = request.Dto.ShortName,
            Imo = request.Dto.Imo,
            Mmsi = request.Dto.Mmsi,
            Email = request.Dto.Email,
            IceClass = request.Dto.IceClass,
            Statcode5 = request.Dto.Statcode5,
            Statcode5Desc = request.Dto.Statcode5Desc,
            VesselType = request.Dto.VesselType,
            BuilderName = request.Dto.BuilderName,
            BuilderCountry = request.Dto.BuilderCountry,
            BuilderCode = request.Dto.BuilderCode,
            BuilderTown = request.Dto.BuilderTown,
            BuiltYear = request.Dto.BuiltYear,
            StandardDesign = request.Dto.StandardDesign,
            Gt = request.Dto.Gt,
            LengthBp = request.Dto.LengthBp,
            LengthOverall = request.Dto.LengthOverall,
            Depth = request.Dto.Depth,
            BreadthMoulded = request.Dto.BreadthMoulded,
            Deadweight = request.Dto.Deadweight,
            Displacement = request.Dto.Displacement,
            Draught = request.Dto.Draught,
            HullType = request.Dto.HullType,
            Holds = request.Dto.Holds,
            Teu = request.Dto.Teu,
            GasCapacity = request.Dto.GasCapacity,
            SternLoading = request.Dto.SternLoading,
            InertGasSystem = request.Dto.InertGasSystem,
            KeelLaid = request.Dto.KeelLaid,
            KeelToMastHeight = request.Dto.KeelToMastHeight,
            LinesPerSide = request.Dto.LinesPerSide,
            ParallelBodyLength = request.Dto.ParallelBodyLength,
            RoroLanesLength = request.Dto.RoroLanesLength,
            EngineBuilder = request.Dto.EngineBuilder,
            EngineDesign = request.Dto.EngineDesign,
            EngineModel = request.Dto.EngineModel,
            EnginesRpm = request.Dto.EnginesRpm,
            TotalKwMainEng = request.Dto.TotalKwMainEng,
            FuelConsMainEng = request.Dto.FuelConsMainEng,
            AuxEngineTotalKw = request.Dto.AuxEngineTotalKw,
            GeneratorsKw = request.Dto.GeneratorsKw,
            ThrustersTotalKw = request.Dto.ThrustersTotalKw,
            ServiceSpeed = request.Dto.ServiceSpeed,
            Flag = request.Dto.Flag,
            Owner = request.Dto.Owner,
            Operator = request.Dto.Operator,
            ClassSociety = request.Dto.ClassSociety,
            EcdisModel = request.Dto.EcdisModel,
            AutoSendForecast = request.Dto.AutoSendForecast,
            AutoSendForecastTime = request.Dto.AutoSendForecastTime,
            Weather4x = request.Dto.Weather4x,
            Weather4xDuration = request.Dto.Weather4xDuration,
            AutoSendReports = request.Dto.AutoSendReports,
            Scrubber = request.Dto.Scrubber,
            ScrubberType = request.Dto.ScrubberType,
            MeType = request.Dto.MeType,
            DefaultBallastDraft = request.Dto.DefaultBallastDraft,
            DefaultLadenDraft = request.Dto.DefaultLadenDraft,
            SummerDraft = request.Dto.SummerDraft,
            MinRpm = request.Dto.MinRpm,
            MaxRpm = request.Dto.MaxRpm,
            MinMcr = request.Dto.MinMcr,
            MaxMcr = request.Dto.MaxMcr,
            MinSpeed = request.Dto.MinSpeed,
            MaxSpeed = request.Dto.MaxSpeed,
            MinPowerFraction = request.Dto.MinPowerFraction,
            MaxPowerFraction = request.Dto.MaxPowerFraction,
            NominalPowerFraction = request.Dto.NominalPowerFraction,
            BlowerBallastMin = request.Dto.BlowerBallastMin,
            BlowerBallastMax = request.Dto.BlowerBallastMax,
            BlowerLadenMin = request.Dto.BlowerLadenMin,
            BlowerLadenMax = request.Dto.BlowerLadenMax,
            CriticalRpmMin = request.Dto.CriticalRpmMin,
            CriticalRpmMax = request.Dto.CriticalRpmMax,
            DeadSlowRpm = request.Dto.DeadSlowRpm,
            SlowAheadRpm = request.Dto.SlowAheadRpm,
            HalfAheadRpm = request.Dto.HalfAheadRpm,
            FullAheadRpm = request.Dto.FullAheadRpm,
            DeadSlowSpeedBallast = request.Dto.DeadSlowSpeedBallast,
            DeadSlowSpeedLaden = request.Dto.DeadSlowSpeedLaden,
            SlowAheadSpeedBallast = request.Dto.SlowAheadSpeedBallast,
            SlowAheadSpeedLaden = request.Dto.SlowAheadSpeedLaden,
            HalfAheadSpeedBallast = request.Dto.HalfAheadSpeedBallast,
            HalfAheadSpeedLaden = request.Dto.HalfAheadSpeedLaden,
            FullAheadSpeedBallast = request.Dto.FullAheadSpeedBallast,
            FullAheadSpeedLaden = request.Dto.FullAheadSpeedLaden,
            WslMaxSwhBallast = request.Dto.WslMaxSwhBallast,
            WslMaxSwhLaden = request.Dto.WslMaxSwhLaden,
            WslMaxWindsBallast = request.Dto.WslMaxWindsBallast,
            WslMaxWindsLaden = request.Dto.WslMaxWindsLaden,
            WslMaxSeaStateBallast = request.Dto.WslMaxSeaStateBallast,
            WslMaxSeaStateLaden = request.Dto.WslMaxSeaStateLaden,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.UserId
        };

        _context.Vessels.Add(vessel);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "CreateVessel",
            entityName: "Vessel",
            entityId: vessel.Id.ToString(),
            newValues: $"Name: {vessel.Name}, IMO: {vessel.Imo}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<VesselDto>(vessel);
        return ApiResponse<VesselDto>.SuccessResult(dto, "Vessel created successfully.");
    }
}

public record UpdateVesselCommand(Guid Id, UpdateVesselRequestDto Dto) : IRequest<ApiResponse<VesselDto>>;

public class UpdateVesselCommandHandler : IRequestHandler<UpdateVesselCommand, ApiResponse<VesselDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public UpdateVesselCommandHandler(
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

    public async Task<ApiResponse<VesselDto>> Handle(UpdateVesselCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var vessel = await _context.Vessels
            .Include(v => v.History)
            .FirstOrDefaultAsync(v => v.Id == request.Id && v.TenantId == tenantId, cancellationToken);

        if (vessel == null)
        {
            throw new NotFoundException("Vessel", request.Id);
        }

        var changer = _currentUserService.Email ?? "System User";

        // Track field changes for history
        void CheckAndLogChange(string fieldName, string? oldVal, string? newVal)
        {
            oldVal ??= string.Empty;
            newVal ??= string.Empty;
            if (oldVal != newVal)
            {
                _context.VesselHistories.Add(new VesselHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    VesselId = vessel.Id,
                    FieldName = fieldName,
                    FromValue = oldVal,
                    ToValue = newVal,
                    ChangedBy = changer,
                    ChangedAt = DateTime.UtcNow
                });
            }
        }

        CheckAndLogChange("Vessel Name", vessel.Name, request.Dto.Name);
        CheckAndLogChange("Short Name", vessel.ShortName, request.Dto.ShortName);
        CheckAndLogChange("IMO", vessel.Imo, request.Dto.Imo);
        CheckAndLogChange("MMSI", vessel.Mmsi, request.Dto.Mmsi);
        CheckAndLogChange("Email", vessel.Email, request.Dto.Email);
        CheckAndLogChange("Flag", vessel.Flag, request.Dto.Flag);
        CheckAndLogChange("Owner", vessel.Owner, request.Dto.Owner);
        CheckAndLogChange("Engine Power", vessel.TotalKwMainEng, request.Dto.TotalKwMainEng);

        // Update properties
        vessel.Name = request.Dto.Name;
        vessel.ShortName = request.Dto.ShortName;
        vessel.Imo = request.Dto.Imo;
        vessel.Mmsi = request.Dto.Mmsi;
        vessel.Email = request.Dto.Email;
        vessel.IceClass = request.Dto.IceClass;
        vessel.Statcode5 = request.Dto.Statcode5;
        vessel.Statcode5Desc = request.Dto.Statcode5Desc;
        vessel.VesselType = request.Dto.VesselType;
        vessel.BuilderName = request.Dto.BuilderName;
        vessel.BuilderCountry = request.Dto.BuilderCountry;
        vessel.BuilderCode = request.Dto.BuilderCode;
        vessel.BuilderTown = request.Dto.BuilderTown;
        vessel.BuiltYear = request.Dto.BuiltYear;
        vessel.StandardDesign = request.Dto.StandardDesign;
        vessel.Gt = request.Dto.Gt;
        vessel.LengthBp = request.Dto.LengthBp;
        vessel.LengthOverall = request.Dto.LengthOverall;
        vessel.Depth = request.Dto.Depth;
        vessel.BreadthMoulded = request.Dto.BreadthMoulded;
        vessel.Deadweight = request.Dto.Deadweight;
        vessel.Displacement = request.Dto.Displacement;
        vessel.Draught = request.Dto.Draught;
        vessel.HullType = request.Dto.HullType;
        vessel.Holds = request.Dto.Holds;
        vessel.Teu = request.Dto.Teu;
        vessel.GasCapacity = request.Dto.GasCapacity;
        vessel.SternLoading = request.Dto.SternLoading;
        vessel.InertGasSystem = request.Dto.InertGasSystem;
        vessel.KeelLaid = request.Dto.KeelLaid;
        vessel.KeelToMastHeight = request.Dto.KeelToMastHeight;
        vessel.LinesPerSide = request.Dto.LinesPerSide;
        vessel.ParallelBodyLength = request.Dto.ParallelBodyLength;
        vessel.RoroLanesLength = request.Dto.RoroLanesLength;
        vessel.EngineBuilder = request.Dto.EngineBuilder;
        vessel.EngineDesign = request.Dto.EngineDesign;
        vessel.EngineModel = request.Dto.EngineModel;
        vessel.EnginesRpm = request.Dto.EnginesRpm;
        vessel.TotalKwMainEng = request.Dto.TotalKwMainEng;
        vessel.FuelConsMainEng = request.Dto.FuelConsMainEng;
        vessel.AuxEngineTotalKw = request.Dto.AuxEngineTotalKw;
        vessel.GeneratorsKw = request.Dto.GeneratorsKw;
        vessel.ThrustersTotalKw = request.Dto.ThrustersTotalKw;
        vessel.ServiceSpeed = request.Dto.ServiceSpeed;
        vessel.Flag = request.Dto.Flag;
        vessel.Owner = request.Dto.Owner;
        vessel.Operator = request.Dto.Operator;
        vessel.ClassSociety = request.Dto.ClassSociety;
        vessel.EcdisModel = request.Dto.EcdisModel;
        vessel.AutoSendForecast = request.Dto.AutoSendForecast;
        vessel.AutoSendForecastTime = request.Dto.AutoSendForecastTime;
        vessel.Weather4x = request.Dto.Weather4x;
        vessel.Weather4xDuration = request.Dto.Weather4xDuration;
        vessel.AutoSendReports = request.Dto.AutoSendReports;
        vessel.Scrubber = request.Dto.Scrubber;
        vessel.ScrubberType = request.Dto.ScrubberType;
        vessel.MeType = request.Dto.MeType;
        vessel.DefaultBallastDraft = request.Dto.DefaultBallastDraft;
        vessel.DefaultLadenDraft = request.Dto.DefaultLadenDraft;
        vessel.SummerDraft = request.Dto.SummerDraft;
        vessel.MinRpm = request.Dto.MinRpm;
        vessel.MaxRpm = request.Dto.MaxRpm;
        vessel.MinMcr = request.Dto.MinMcr;
        vessel.MaxMcr = request.Dto.MaxMcr;
        vessel.MinSpeed = request.Dto.MinSpeed;
        vessel.MaxSpeed = request.Dto.MaxSpeed;
        vessel.MinPowerFraction = request.Dto.MinPowerFraction;
        vessel.MaxPowerFraction = request.Dto.MaxPowerFraction;
        vessel.NominalPowerFraction = request.Dto.NominalPowerFraction;
        vessel.BlowerBallastMin = request.Dto.BlowerBallastMin;
        vessel.BlowerBallastMax = request.Dto.BlowerBallastMax;
        vessel.BlowerLadenMin = request.Dto.BlowerLadenMin;
        vessel.BlowerLadenMax = request.Dto.BlowerLadenMax;
        vessel.CriticalRpmMin = request.Dto.CriticalRpmMin;
        vessel.CriticalRpmMax = request.Dto.CriticalRpmMax;
        vessel.DeadSlowRpm = request.Dto.DeadSlowRpm;
        vessel.SlowAheadRpm = request.Dto.SlowAheadRpm;
        vessel.HalfAheadRpm = request.Dto.HalfAheadRpm;
        vessel.FullAheadRpm = request.Dto.FullAheadRpm;
        vessel.DeadSlowSpeedBallast = request.Dto.DeadSlowSpeedBallast;
        vessel.DeadSlowSpeedLaden = request.Dto.DeadSlowSpeedLaden;
        vessel.SlowAheadSpeedBallast = request.Dto.SlowAheadSpeedBallast;
        vessel.SlowAheadSpeedLaden = request.Dto.SlowAheadSpeedLaden;
        vessel.HalfAheadSpeedBallast = request.Dto.HalfAheadSpeedBallast;
        vessel.HalfAheadSpeedLaden = request.Dto.HalfAheadSpeedLaden;
        vessel.FullAheadSpeedBallast = request.Dto.FullAheadSpeedBallast;
        vessel.FullAheadSpeedLaden = request.Dto.FullAheadSpeedLaden;
        vessel.WslMaxSwhBallast = request.Dto.WslMaxSwhBallast;
        vessel.WslMaxSwhLaden = request.Dto.WslMaxSwhLaden;
        vessel.WslMaxWindsBallast = request.Dto.WslMaxWindsBallast;
        vessel.WslMaxWindsLaden = request.Dto.WslMaxWindsLaden;
        vessel.WslMaxSeaStateBallast = request.Dto.WslMaxSeaStateBallast;
        vessel.WslMaxSeaStateLaden = request.Dto.WslMaxSeaStateLaden;
        vessel.IsActive = request.Dto.IsActive;
        vessel.UpdatedAt = DateTime.UtcNow;
        vessel.UpdatedByUserId = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "UpdateVessel",
            entityName: "Vessel",
            entityId: vessel.Id.ToString(),
            newValues: $"Updated vessel: {vessel.Name} (IMO: {vessel.Imo})",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<VesselDto>(vessel);
        return ApiResponse<VesselDto>.SuccessResult(dto, "Vessel updated successfully.");
    }
}

public record DeleteVesselCommand(Guid Id) : IRequest<ApiResponse>;

public class DeleteVesselCommandHandler : IRequestHandler<DeleteVesselCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public DeleteVesselCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse> Handle(DeleteVesselCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var vessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == request.Id && v.TenantId == tenantId, cancellationToken);

        if (vessel == null)
        {
            throw new NotFoundException("Vessel", request.Id);
        }

        vessel.IsActive = false;
        vessel.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "DeactivateVessel",
            entityName: "Vessel",
            entityId: vessel.Id.ToString(),
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        return ApiResponse.SuccessResult("Vessel deactivated successfully.");
    }
}
