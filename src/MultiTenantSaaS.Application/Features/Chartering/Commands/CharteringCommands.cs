using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Chartering;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Chartering.Commands;

public record UpsertVoyageEstimateCommand(CreateVoyageEstimateRequestDto Dto, Guid? Id = null) : IRequest<ApiResponse<VoyageEstimateDto>>;

public class UpsertVoyageEstimateCommandHandler : IRequestHandler<UpsertVoyageEstimateCommand, ApiResponse<VoyageEstimateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public UpsertVoyageEstimateCommandHandler(
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

    public async Task<ApiResponse<VoyageEstimateDto>> Handle(UpsertVoyageEstimateCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;
        System.Console.WriteLine($"[DEBUG] UpsertVoyageEstimate - TenantId: {tenantId}, BookRef: {request.Dto.BookRef}, VesselName: {request.Dto.VesselName}");

        // Reject explicit saves with empty vessel names (only auto-saves are allowed to have empty names)
        if (!request.Dto.IsAutoSave && string.IsNullOrWhiteSpace(request.Dto.VesselName))
        {
            return ApiResponse<VoyageEstimateDto>.FailureResult("Vessel name is required to save an estimation.");
        }

        VoyageEstimate? estimate = null;

        // Try to parse the string ID as a Guid if provided
        Guid? idGuid = null;
        if (!string.IsNullOrEmpty(request.Dto.Id) && Guid.TryParse(request.Dto.Id, out var parsed))
        {
            idGuid = parsed;
        }

        if (request.Id.HasValue)
        {
            estimate = await _context.VoyageEstimates
                .FirstOrDefaultAsync(e => e.Id == request.Id.Value && e.TenantId == tenantId, cancellationToken);
        }
        else if (idGuid.HasValue)
        {
            estimate = await _context.VoyageEstimates
                .FirstOrDefaultAsync(e => e.Id == idGuid.Value && e.TenantId == tenantId, cancellationToken);
        }

        if (estimate == null && !string.IsNullOrWhiteSpace(request.Dto.EstimateNo))
        {
            estimate = await _context.VoyageEstimates
                .FirstOrDefaultAsync(e => e.EstimateNo == request.Dto.EstimateNo && e.TenantId == tenantId, cancellationToken);
        }

        if (estimate == null)
        {
            // Auto-save should not create new records (only update existing ones).
            // New records are only created on explicit save.
            if (request.Dto.IsAutoSave)
            {
                return ApiResponse<VoyageEstimateDto>.SuccessResult(
                    new VoyageEstimateDto 
                    { 
                        Id = Guid.NewGuid(), 
                        EstimateNo = "", 
                        VesselName = request.Dto.VesselName 
                    },
                    "Voyage estimate auto-saved locally (not yet persisted).");
            }

            var estNo = request.Dto.EstimateNo;
            if (string.IsNullOrWhiteSpace(estNo))
            {
                var yy = DateTime.UtcNow.ToString("yy");
                var mm = DateTime.UtcNow.ToString("MM");
                var count = await _context.VoyageEstimates.CountAsync(cancellationToken) + 1;
                estNo = $"EST-{yy}{mm}-{count:D2}";
            }

            // Use provided Guid ID if available, otherwise generate new one
            var newId = request.Id ?? idGuid ?? Guid.NewGuid();
            estimate = new VoyageEstimate
            {
                Id = newId,
                TenantId = tenantId,
                EstimateNo = estNo,
                VesselName = request.Dto.VesselName,
                FixType = request.Dto.FixType,
                Status = request.Dto.Status,
                Profit = request.Dto.Profit,
                Tce = request.Dto.Tce,
                Commodity = request.Dto.Commodity,
                LoadPort = request.Dto.LoadPort,
                DischargePort = request.Dto.DischargePort,
                Quantity = request.Dto.Quantity,
                FreightRate = request.Dto.FreightRate,
                DataJson = request.Dto.DataJson,
                BookRef = request.Dto.BookRef, // Store reference to book entry
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _currentUserService.UserId
            };
            _context.VoyageEstimates.Add(estimate);
        }
        else
        {
            estimate.VesselName = request.Dto.VesselName;
            estimate.FixType = request.Dto.FixType;
            estimate.Status = request.Dto.Status;
            estimate.Profit = request.Dto.Profit;
            estimate.Tce = request.Dto.Tce;
            estimate.Commodity = request.Dto.Commodity;
            estimate.LoadPort = request.Dto.LoadPort;
            estimate.DischargePort = request.Dto.DischargePort;
            estimate.Quantity = request.Dto.Quantity;
            estimate.FreightRate = request.Dto.FreightRate;
            estimate.DataJson = request.Dto.DataJson;
            estimate.UpdatedAt = DateTime.UtcNow;
            estimate.UpdatedByUserId = _currentUserService.UserId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // If this estimate was created from a cargo/tonnage book, update the book with the estimateId
        if (!string.IsNullOrEmpty(request.Dto.BookRef) && estimate.Id != Guid.Empty)
        {
            // Try to find and update cargo book by CargoCode
            var cargoBook = await _context.CargoBookEntries
                .FirstOrDefaultAsync(c => c.CargoCode == request.Dto.BookRef && c.TenantId == tenantId, cancellationToken);
            if (cargoBook != null)
            {
                cargoBook.EstimateId = estimate.Id.ToString();
                cargoBook.EstimationStatus = "Estimated"; // Update status to indicate estimate has been created
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // Try to find and update tonnage book by TonnageCode
                var tonnageBook = await _context.TonnageBookEntries
                    .FirstOrDefaultAsync(t => t.TonnageCode == request.Dto.BookRef && t.TenantId == tenantId, cancellationToken);
                if (tonnageBook != null)
                {
                    tonnageBook.EstimateId = estimate.Id.ToString();
                    tonnageBook.EstimationStatus = "Estimated"; // Update status to indicate estimate has been created
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        await _auditService.LogAsync(
            action: "UpsertVoyageEstimate",
            entityName: "VoyageEstimate",
            entityId: estimate.Id.ToString(),
            newValues: $"Estimate: {estimate.EstimateNo}, Vessel: {estimate.VesselName}, TCE: {estimate.Tce}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<VoyageEstimateDto>(estimate);
        return ApiResponse<VoyageEstimateDto>.SuccessResult(dto, "Voyage estimate saved successfully.");
    }
}

public record DeleteVoyageEstimateCommand(Guid Id) : IRequest<ApiResponse>;

public class DeleteVoyageEstimateCommandHandler : IRequestHandler<DeleteVoyageEstimateCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteVoyageEstimateCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse> Handle(DeleteVoyageEstimateCommand request, CancellationToken cancellationToken)
    {
        // Find the estimate by ID
        var estimate = await _context.VoyageEstimates
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (estimate == null)
        {
            return ApiResponse.FailureResult("Voyage estimate not found.");
        }

        // Clear the EstimateId and EstimationStatus from CargoBookEntry if linked
        if (!string.IsNullOrEmpty(estimate.BookRef))
        {
            var cargoEntry = await _context.CargoBookEntries
                .FirstOrDefaultAsync(c => c.CargoCode == estimate.BookRef, cancellationToken);
            if (cargoEntry != null)
            {
                cargoEntry.EstimateId = null;
                cargoEntry.EstimationStatus = "Not Created";
            }

            // Also check TonnageBookEntry
            var tonnageEntry = await _context.TonnageBookEntries
                .FirstOrDefaultAsync(t => t.TonnageCode == estimate.BookRef, cancellationToken);
            if (tonnageEntry != null)
            {
                tonnageEntry.EstimateId = null;
                tonnageEntry.EstimationStatus = "Not Created";
            }
        }

        // Delete the estimate
        _context.VoyageEstimates.Remove(estimate);
        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse.SuccessResult("Voyage estimate deleted successfully.");
    }
}
