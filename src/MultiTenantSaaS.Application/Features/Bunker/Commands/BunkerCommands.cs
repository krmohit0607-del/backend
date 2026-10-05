using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Bunker;
using MultiTenantSaaS.Application.Features.Bunker;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Bunker.Commands;

public record CreateBunkerRequirementCommand(CreateBunkerRequirementRequestDto Dto) : IRequest<ApiResponse<BunkerRequirementDto>>;

public class CreateBunkerRequirementCommandHandler : IRequestHandler<CreateBunkerRequirementCommand, ApiResponse<BunkerRequirementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public CreateBunkerRequirementCommandHandler(
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

    public async Task<ApiResponse<BunkerRequirementDto>> Handle(CreateBunkerRequirementCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var reqNo = request.Dto.RequirementNo;
        if (string.IsNullOrWhiteSpace(reqNo))
        {
            var yy = DateTime.UtcNow.ToString("yy");
            var mm = DateTime.UtcNow.ToString("MM");
            var count = await _context.BunkerRequirements.CountAsync(cancellationToken) + 1;
            reqNo = $"BR-{yy}{mm}-{count:D3}";
        }

        var req = new BunkerRequirement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequirementNo = reqNo,
            Priority = request.Dto.Priority ?? "Medium",
            Status = request.Dto.Status ?? "Pending RFQ",
            VesselName = request.Dto.VesselName,
            Imo = request.Dto.Imo,
            Reference = request.Dto.Reference,
            Leg = request.Dto.Leg,
            Route = request.Dto.Route,
            LoadPort = request.Dto.LoadPort,
            DischargePort = request.Dto.DischargePort,
            BunkerPort = request.Dto.BunkerPort,
            Eta = request.Dto.Eta,
            RequiredOn = request.Dto.RequiredOn,
            RequiredIso = request.Dto.RequiredIso,
            LaycanStart = request.Dto.LaycanStart,
            LaycanEnd = request.Dto.LaycanEnd,
            FuelType = request.Dto.FuelType ?? "VLSFO",
            Grade = request.Dto.Grade ?? "ISO 8217:2017 RMG 380",
            Quantity = request.Dto.Quantity,
            RobArrival = request.Dto.RobArrival,
            ExpectedCons = request.Dto.ExpectedCons,
            ChartererInstructions = request.Dto.ChartererInstructions,
            OwnerInstructions = request.Dto.OwnerInstructions,
            SuppliersInvited = request.Dto.SuppliersInvited,
            Supplier = request.Dto.Supplier,
            PricePerMt = request.Dto.PricePerMt,
            TotalCost = request.Dto.TotalCost,
            PoNo = request.Dto.PoNo,
            ContractRef = request.Dto.ContractRef,
            BookedOn = request.Dto.BookedOn,
            ConfirmNo = request.Dto.ConfirmNo,
            DeliveryMethod = request.Dto.DeliveryMethod,
            SuppliedQty = request.Dto.SuppliedQty,
            DeliveredQty = request.Dto.DeliveredQty,
            SupplyDateTime = request.Dto.SupplyDateTime,
            InvoiceNo = request.Dto.InvoiceNo,
            InvoiceDate = request.Dto.InvoiceDate,
            InvoiceAmount = request.Dto.InvoiceAmount,
            PaymentTerms = request.Dto.PaymentTerms,
            DueDate = request.Dto.DueDate,
            DueIso = request.Dto.DueIso,
            AmountPaid = request.Dto.AmountPaid,
            PaymentRef = request.Dto.PaymentRef,
            PaymentDate = request.Dto.PaymentDate,
            ApprovalStatus = request.Dto.ApprovalStatus ?? "Not Submitted",
            PaymentStatus = request.Dto.PaymentStatus ?? "None",
            QuotesJson = request.Dto.QuotesJson,
            FuelLinesJson = request.Dto.FuelLinesJson,
            AdditionalChargesJson = request.Dto.AdditionalChargesJson,
            ClaimsJson = request.Dto.ClaimsJson,
            AuditJson = request.Dto.AuditJson,
            DocumentsJson = request.Dto.DocumentsJson,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.UserId
        };

        BunkerFinance.ValidateWorkflow(req.Status, req.ApprovalStatus);
        BunkerFinance.RecomputeTotals(req);

        _context.BunkerRequirements.Add(req);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "CreateBunkerRequirement",
            entityName: "BunkerRequirement",
            entityId: req.Id.ToString(),
            newValues: $"Req: {req.RequirementNo}, Vessel: {req.VesselName}, Port: {req.BunkerPort}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<BunkerRequirementDto>(req);
        return ApiResponse<BunkerRequirementDto>.SuccessResult(dto, "Bunker requirement created successfully.");
    }
}

public record UpdateBunkerRequirementCommand(Guid Id, UpdateBunkerRequirementRequestDto Dto) : IRequest<ApiResponse<BunkerRequirementDto>>;

public class UpdateBunkerRequirementCommandHandler : IRequestHandler<UpdateBunkerRequirementCommand, ApiResponse<BunkerRequirementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public UpdateBunkerRequirementCommandHandler(
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

    public async Task<ApiResponse<BunkerRequirementDto>> Handle(UpdateBunkerRequirementCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var req = await _context.BunkerRequirements
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == tenantId, cancellationToken);

        if (req == null)
        {
            throw new NotFoundException("BunkerRequirement", request.Id);
        }

        req.Priority = request.Dto.Priority ?? req.Priority;
        req.Status = request.Dto.Status ?? req.Status;
        req.VesselName = request.Dto.VesselName ?? req.VesselName;
        req.Imo = request.Dto.Imo ?? req.Imo;
        req.Reference = request.Dto.Reference ?? req.Reference;
        req.Leg = request.Dto.Leg ?? req.Leg;
        req.Route = request.Dto.Route ?? req.Route;
        req.LoadPort = request.Dto.LoadPort ?? req.LoadPort;
        req.DischargePort = request.Dto.DischargePort ?? req.DischargePort;
        req.BunkerPort = request.Dto.BunkerPort ?? req.BunkerPort;
        req.Eta = request.Dto.Eta ?? req.Eta;
        req.RequiredOn = request.Dto.RequiredOn ?? req.RequiredOn;
        req.RequiredIso = request.Dto.RequiredIso ?? req.RequiredIso;
        req.LaycanStart = request.Dto.LaycanStart ?? req.LaycanStart;
        req.LaycanEnd = request.Dto.LaycanEnd ?? req.LaycanEnd;
        req.FuelType = request.Dto.FuelType ?? req.FuelType;
        req.Grade = request.Dto.Grade ?? req.Grade;
        req.Quantity = request.Dto.Quantity;
        req.RobArrival = request.Dto.RobArrival;
        req.ExpectedCons = request.Dto.ExpectedCons;
        req.ChartererInstructions = request.Dto.ChartererInstructions ?? req.ChartererInstructions;
        req.OwnerInstructions = request.Dto.OwnerInstructions ?? req.OwnerInstructions;
        req.SuppliersInvited = request.Dto.SuppliersInvited;
        req.Supplier = request.Dto.Supplier ?? req.Supplier;
        req.PricePerMt = request.Dto.PricePerMt ?? req.PricePerMt;
        req.TotalCost = request.Dto.TotalCost ?? req.TotalCost;
        req.PoNo = request.Dto.PoNo ?? req.PoNo;
        req.ContractRef = request.Dto.ContractRef ?? req.ContractRef;
        req.BookedOn = request.Dto.BookedOn ?? req.BookedOn;
        req.ConfirmNo = request.Dto.ConfirmNo ?? req.ConfirmNo;
        req.DeliveryMethod = request.Dto.DeliveryMethod ?? req.DeliveryMethod;
        req.SuppliedQty = request.Dto.SuppliedQty ?? req.SuppliedQty;
        req.DeliveredQty = request.Dto.DeliveredQty ?? req.DeliveredQty;
        req.SupplyDateTime = request.Dto.SupplyDateTime ?? req.SupplyDateTime;
        req.InvoiceNo = request.Dto.InvoiceNo ?? req.InvoiceNo;
        req.InvoiceDate = request.Dto.InvoiceDate ?? req.InvoiceDate;
        req.InvoiceAmount = request.Dto.InvoiceAmount ?? req.InvoiceAmount;
        req.PaymentTerms = request.Dto.PaymentTerms ?? req.PaymentTerms;
        req.DueDate = request.Dto.DueDate ?? req.DueDate;
        req.DueIso = request.Dto.DueIso ?? req.DueIso;
        req.AmountPaid = request.Dto.AmountPaid ?? req.AmountPaid;
        req.PaymentRef = request.Dto.PaymentRef ?? req.PaymentRef;
        req.PaymentDate = request.Dto.PaymentDate ?? req.PaymentDate;
        req.ApprovalStatus = request.Dto.ApprovalStatus ?? req.ApprovalStatus;
        req.PaymentStatus = request.Dto.PaymentStatus ?? req.PaymentStatus;
        req.QuotesJson = request.Dto.QuotesJson ?? req.QuotesJson;
        req.FuelLinesJson = request.Dto.FuelLinesJson ?? req.FuelLinesJson;
        req.AdditionalChargesJson = request.Dto.AdditionalChargesJson ?? req.AdditionalChargesJson;
        req.ClaimsJson = request.Dto.ClaimsJson ?? req.ClaimsJson;
        req.AuditJson = request.Dto.AuditJson ?? req.AuditJson;
        req.DocumentsJson = request.Dto.DocumentsJson ?? req.DocumentsJson;
        req.UpdatedAt = DateTime.UtcNow;
        req.UpdatedByUserId = _currentUserService.UserId;

        BunkerFinance.ValidateWorkflow(req.Status, req.ApprovalStatus);
        BunkerFinance.RecomputeTotals(req);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "UpdateBunkerRequirement",
            entityName: "BunkerRequirement",
            entityId: req.Id.ToString(),
            newValues: $"Status: {req.Status}, Supplier: {req.Supplier}, TotalCost: {req.TotalCost}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<BunkerRequirementDto>(req);
        return ApiResponse<BunkerRequirementDto>.SuccessResult(dto, "Bunker requirement updated successfully.");
    }
}

public record DeleteBunkerRequirementCommand(Guid Id) : IRequest<ApiResponse>;

public class DeleteBunkerRequirementCommandHandler : IRequestHandler<DeleteBunkerRequirementCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteBunkerRequirementCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse> Handle(DeleteBunkerRequirementCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var req = await _context.BunkerRequirements
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == tenantId, cancellationToken);

        if (req != null)
        {
            _context.BunkerRequirements.Remove(req);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse.SuccessResult("Bunker requirement deleted successfully.");
    }
}
