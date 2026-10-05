using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Accounts;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Accounts.Commands;

public record CreateTransactionCommand(CreateFinancialTransactionRequestDto Dto) : IRequest<ApiResponse<FinancialTransactionDto>>;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, ApiResponse<FinancialTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public CreateTransactionCommandHandler(
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

    public async Task<ApiResponse<FinancialTransactionDto>> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var txnNo = request.Dto.TransactionNo;
        if (string.IsNullOrWhiteSpace(txnNo))
        {
            var count = await _context.FinancialTransactions.CountAsync(cancellationToken) + 4401;
            txnNo = $"TXN-{count}";
        }

        var txn = new FinancialTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TransactionNo = txnNo,
            Kind = request.Dto.Kind,
            Category = request.Dto.Category,
            Module = request.Dto.Module,
            Company = request.Dto.Company ?? "ODAS Shipping Ltd",
            VesselName = request.Dto.VesselName,
            Voyage = request.Dto.Voyage,
            Reference = request.Dto.Reference,
            Fixture = request.Dto.Fixture,
            Counterparty = request.Dto.Counterparty,
            InvoiceNo = request.Dto.InvoiceNo,
            Currency = request.Dto.Currency,
            Amount = request.Dto.Amount,
            ExchangeRate = request.Dto.ExchangeRate,
            InvoiceDate = request.Dto.InvoiceDate ?? DateTime.UtcNow.ToString("dd MMM yyyy"),
            DueDate = request.Dto.DueDate ?? DateTime.UtcNow.AddDays(15).ToString("dd MMM yyyy"),
            DueIso = request.Dto.DueIso ?? DateTime.UtcNow.AddDays(15).ToString("yyyy-MM-dd"),
            Status = request.Dto.Status,
            Approval = request.Dto.Approval,
            Priority = request.Dto.Priority,
            Pic = request.Dto.Pic ?? "Accounts",
            Bank = request.Dto.Bank,
            Method = request.Dto.Method,
            PaymentDate = request.Dto.PaymentDate,
            PaymentRef = request.Dto.PaymentRef,
            SwiftDocUrl = request.Dto.SwiftDocUrl,
            Remarks = request.Dto.Remarks,
            AuditJson = request.Dto.AuditJson,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.UserId
        };

        _context.FinancialTransactions.Add(txn);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "CreateTransaction",
            entityName: "FinancialTransaction",
            entityId: txn.Id.ToString(),
            newValues: $"No: {txn.TransactionNo}, Kind: {txn.Kind}, Amount: {txn.Amount} {txn.Currency}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<FinancialTransactionDto>(txn);
        return ApiResponse<FinancialTransactionDto>.SuccessResult(dto, "Transaction created successfully.");
    }
}

public record UpdateTransactionCommand(Guid Id, UpdateFinancialTransactionRequestDto Dto) : IRequest<ApiResponse<FinancialTransactionDto>>;

public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand, ApiResponse<FinancialTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;

    public UpdateTransactionCommandHandler(
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

    public async Task<ApiResponse<FinancialTransactionDto>> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        var txn = await _context.FinancialTransactions
            .FirstOrDefaultAsync(f => f.Id == request.Id && f.TenantId == tenantId, cancellationToken);

        if (txn == null)
        {
            throw new NotFoundException("FinancialTransaction", request.Id);
        }

        txn.Kind = request.Dto.Kind;
        txn.Category = request.Dto.Category;
        txn.Module = request.Dto.Module;
        txn.Company = request.Dto.Company ?? txn.Company;
        txn.VesselName = request.Dto.VesselName;
        txn.Voyage = request.Dto.Voyage;
        txn.Reference = request.Dto.Reference;
        txn.Fixture = request.Dto.Fixture;
        txn.Counterparty = request.Dto.Counterparty;
        txn.InvoiceNo = request.Dto.InvoiceNo;
        txn.Currency = request.Dto.Currency;
        txn.Amount = request.Dto.Amount;
        txn.ExchangeRate = request.Dto.ExchangeRate;
        txn.InvoiceDate = request.Dto.InvoiceDate ?? txn.InvoiceDate;
        txn.DueDate = request.Dto.DueDate ?? txn.DueDate;
        txn.DueIso = request.Dto.DueIso ?? txn.DueIso;
        txn.Status = request.Dto.Status;
        txn.Approval = request.Dto.Approval;
        txn.Priority = request.Dto.Priority;
        txn.Pic = request.Dto.Pic ?? txn.Pic;
        txn.Bank = request.Dto.Bank;
        txn.Method = request.Dto.Method;
        txn.PaymentDate = request.Dto.PaymentDate;
        txn.PaymentRef = request.Dto.PaymentRef;
        txn.SwiftDocUrl = request.Dto.SwiftDocUrl;
        txn.Remarks = request.Dto.Remarks;
        txn.AuditJson = request.Dto.AuditJson ?? txn.AuditJson;
        txn.UpdatedAt = DateTime.UtcNow;
        txn.UpdatedByUserId = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "UpdateTransaction",
            entityName: "FinancialTransaction",
            entityId: txn.Id.ToString(),
            newValues: $"Status: {txn.Status}, Approval: {txn.Approval}, PaymentRef: {txn.PaymentRef}",
            tenantId: tenantId,
            userId: _currentUserService.UserId,
            cancellationToken: cancellationToken);

        var dto = _mapper.Map<FinancialTransactionDto>(txn);
        return ApiResponse<FinancialTransactionDto>.SuccessResult(dto, "Transaction updated successfully.");
    }
}
