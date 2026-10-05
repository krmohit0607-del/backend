using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Voyages;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Voyages.Commands;

public record CreateVoyageCommand(CreateVoyageRequestDto Dto) : IRequest<ApiResponse<VoyageDto>>;

public class CreateVoyageCommandHandler : IRequestHandler<CreateVoyageCommand, ApiResponse<VoyageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public CreateVoyageCommandHandler(
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

    public async Task<ApiResponse<VoyageDto>> Handle(CreateVoyageCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        // Generate voyage code if not provided
        var code = request.Dto.VoyageCode;
        if (string.IsNullOrWhiteSpace(code))
        {
            var count = await _context.Voyages.CountAsync(cancellationToken) + 1;
            code = $"OPT{count:D3}";
        }

        var voyage = new Voyage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VoyageCode = code,
            VoyageOrderId = request.Dto.VoyageOrderId,
            VesselName = request.Dto.VesselName,
            Imo = request.Dto.Imo,
            VesselType = request.Dto.VesselType,
            Flag = request.Dto.Flag,
            PortFrom = request.Dto.PortFrom ?? string.Empty,
            PortTo = request.Dto.PortTo ?? string.Empty,
            Status = request.Dto.Status ?? "At Sea",
            Priority = request.Dto.Priority ?? "MEDIUM",
            EtdDisplay = request.Dto.EtdDisplay,
            EtaDisplay = request.Dto.EtaDisplay,
            Client = request.Dto.Client,
            Service = request.Dto.Service ?? "PMO",
            CpSpeed = request.Dto.CpSpeed,
            CpCons = request.Dto.CpCons,
            InstSpeed = request.Dto.InstSpeed,
            InstCons = request.Dto.InstCons,
            HandoverNote = request.Dto.HandoverNote,
            Tags = request.Dto.Tags,
            AiAlert = request.Dto.AiAlert,
            Health = request.Dto.Health,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.UserId
        };

        _context.Voyages.Add(voyage);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "CreateVoyage",
            entityName: "Voyage",
            entityId: voyage.Id.ToString(),
            newValues: $"Code: {voyage.VoyageCode}, Vessel: {voyage.VesselName}, From: {voyage.PortFrom} To: {voyage.PortTo}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<VoyageDto>(voyage);
        return ApiResponse<VoyageDto>.SuccessResult(dto, "Voyage created successfully.");
    }
}

public record UpdateVoyageCommand(Guid Id, UpdateVoyageRequestDto Dto) : IRequest<ApiResponse<VoyageDto>>;

public class UpdateVoyageCommandHandler : IRequestHandler<UpdateVoyageCommand, ApiResponse<VoyageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public UpdateVoyageCommandHandler(
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

    public async Task<ApiResponse<VoyageDto>> Handle(UpdateVoyageCommand request, CancellationToken cancellationToken)
    {
        var voyage = await _context.Voyages.FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);
        if (voyage == null)
            throw new NotFoundException(nameof(Voyage), request.Id);

        var oldValues = $"PortFrom: {voyage.PortFrom}, PortTo: {voyage.PortTo}, Status: {voyage.Status}, CpSpeed: {voyage.CpSpeed}, CpCons: {voyage.CpCons}";

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Dto.VesselName))
            voyage.VesselName = request.Dto.VesselName;
        if (!string.IsNullOrEmpty(request.Dto.PortFrom))
            voyage.PortFrom = request.Dto.PortFrom;
        if (!string.IsNullOrEmpty(request.Dto.PortTo))
            voyage.PortTo = request.Dto.PortTo;
        if (!string.IsNullOrEmpty(request.Dto.Status))
            voyage.Status = request.Dto.Status;
        if (!string.IsNullOrEmpty(request.Dto.Priority))
            voyage.Priority = request.Dto.Priority;
        if (!string.IsNullOrEmpty(request.Dto.Client))
            voyage.Client = request.Dto.Client;
        if (!string.IsNullOrEmpty(request.Dto.Service))
            voyage.Service = request.Dto.Service;
        if (!string.IsNullOrEmpty(request.Dto.EtdDisplay))
            voyage.EtdDisplay = request.Dto.EtdDisplay;
        if (!string.IsNullOrEmpty(request.Dto.EtaDisplay))
            voyage.EtaDisplay = request.Dto.EtaDisplay;
        if (request.Dto.CpSpeed.HasValue)
            voyage.CpSpeed = request.Dto.CpSpeed.Value;
        if (request.Dto.CpCons.HasValue)
            voyage.CpCons = request.Dto.CpCons.Value;
        if (request.Dto.InstSpeed.HasValue)
            voyage.InstSpeed = request.Dto.InstSpeed.Value;
        if (request.Dto.InstCons.HasValue)
            voyage.InstCons = request.Dto.InstCons.Value;
        if (!string.IsNullOrEmpty(request.Dto.HandoverNote))
            voyage.HandoverNote = request.Dto.HandoverNote;
        if (!string.IsNullOrEmpty(request.Dto.Tags))
            voyage.Tags = request.Dto.Tags;
        if (!string.IsNullOrEmpty(request.Dto.AiAlert))
            voyage.AiAlert = request.Dto.AiAlert;
        if (request.Dto.Health.HasValue)
            voyage.Health = request.Dto.Health.Value;
        if (request.Dto.Price.HasValue)
            voyage.Price = request.Dto.Price.Value;
        if (!string.IsNullOrEmpty(request.Dto.PricingBasis))
            voyage.PricingBasis = request.Dto.PricingBasis;
        if (request.Dto.CostPerDay.HasValue)
            voyage.CostPerDay = request.Dto.CostPerDay.Value;
        if (request.Dto.FoCost.HasValue)
            voyage.FoCost = request.Dto.FoCost.Value;
        if (request.Dto.GoCost.HasValue)
            voyage.GoCost = request.Dto.GoCost.Value;
        if (request.Dto.EuaCost.HasValue)
            voyage.EuaCost = request.Dto.EuaCost.Value;

        voyage.UpdatedAt = DateTime.UtcNow;
        voyage.UpdatedByUserId = _currentUserService.UserId;

        _context.Voyages.Update(voyage);
        await _context.SaveChangesAsync(cancellationToken);

        var newValues = $"PortFrom: {voyage.PortFrom}, PortTo: {voyage.PortTo}, Status: {voyage.Status}, CpSpeed: {voyage.CpSpeed}, CpCons: {voyage.CpCons}";
        await _auditService.LogAsync(
            action: "UpdateVoyage",
            entityName: "Voyage",
            entityId: voyage.Id.ToString(),
            oldValues: oldValues,
            newValues: newValues,
            tenantId: _currentUserService.TenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<VoyageDto>(voyage);
        return ApiResponse<VoyageDto>.SuccessResult(dto, "Voyage updated successfully.");
    }
}

public record CreateVoyageOrderCommand(CreateVoyageOrderRequestDto Dto) : IRequest<ApiResponse<VoyageOrderDto>>;

public class CreateVoyageOrderCommandHandler : IRequestHandler<CreateVoyageOrderCommand, ApiResponse<VoyageOrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public CreateVoyageOrderCommandHandler(
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

    public async Task<ApiResponse<VoyageOrderDto>> Handle(CreateVoyageOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var order = new VoyageOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = request.Dto.OrderNumber,
            Client = request.Dto.Client,
            ClientEmail = request.Dto.ClientEmail,
            Service = request.Dto.Service ?? "PMO",
            Priority = request.Dto.Priority ?? "MEDIUM",
            Status = request.Dto.Status ?? "Active",
            Notes = request.Dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.UserId
        };

        _context.VoyageOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "CreateVoyageOrder",
            entityName: "VoyageOrder",
            entityId: order.Id.ToString(),
            newValues: $"Order: {order.OrderNumber}, Client: {order.Client}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<VoyageOrderDto>(order);
        return ApiResponse<VoyageOrderDto>.SuccessResult(dto, "Voyage order created successfully.");
    }
}

/// <summary>
/// Delete a voyage by ID. All related data (passages, legs) will be cascade-deleted.
/// </summary>
public record DeleteVoyageCommand(Guid Id) : IRequest<ApiResponse>;

public class DeleteVoyageCommandHandler : IRequestHandler<DeleteVoyageCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public DeleteVoyageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse> Handle(DeleteVoyageCommand request, CancellationToken cancellationToken)
    {
        var voyage = await _context.Voyages.FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (voyage == null)
            return ApiResponse.FailureResult("Voyage not found.");

        if (voyage.TenantId != _currentUserService.TenantId)
            return ApiResponse.FailureResult("Unauthorized: Voyage belongs to a different tenant.");

        var voyageCode = voyage.VoyageCode;
        _context.Voyages.Remove(voyage);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "DeleteVoyage",
            entityName: "Voyage",
            entityId: voyage.Id.ToString(),
            oldValues: $"Code: {voyage.VoyageCode}, Vessel: {voyage.VesselName}",
            tenantId: _currentUserService.TenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        return ApiResponse.SuccessResult("Voyage deleted successfully.");
    }
}

/// <summary>
/// Delete all voyages for the current tenant (DANGEROUS - use only for system reset).
/// </summary>
public record DeleteAllVoyagesCommand() : IRequest<ApiResponse>;

public class DeleteAllVoyagesCommandHandler : IRequestHandler<DeleteAllVoyagesCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public DeleteAllVoyagesCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse> Handle(DeleteAllVoyagesCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        var voyagesToDelete = await _context.Voyages
            .Where(v => v.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var count = voyagesToDelete.Count;

        if (count > 0)
        {
            _context.Voyages.RemoveRange(voyagesToDelete);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                action: "DeleteAllVoyages",
                entityName: "Voyage",
                entityId: tenantId.ToString(),
                oldValues: $"Deleted {count} voyages",
                tenantId: tenantId,
                userId: _currentUserService.UserId,
                cancellationToken: cancellationToken);
        }

        return ApiResponse.SuccessResult($"Deleted {count} voyages successfully.");
    }
}
