using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Accounts;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Application.Features.Accounts.Queries;

public record GetTransactionsQuery : IRequest<ApiResponse<List<FinancialTransactionDto>>>;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, ApiResponse<List<FinancialTransactionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTransactionsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<FinancialTransactionDto>>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var list = await _context.FinancialTransactions
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<FinancialTransactionDto>>(list);
        return ApiResponse<List<FinancialTransactionDto>>.SuccessResult(dtos);
    }
}

public record GetTransactionByIdQuery(string IdOrNo) : IRequest<ApiResponse<FinancialTransactionDto>>;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, ApiResponse<FinancialTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTransactionByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<FinancialTransactionDto>> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        FinancialTransaction? txn = null;

        if (Guid.TryParse(request.IdOrNo, out var guid))
        {
            txn = await _context.FinancialTransactions.FirstOrDefaultAsync(f => f.Id == guid, cancellationToken);
        }

        if (txn == null)
        {
            var cleanNo = request.IdOrNo.Trim().ToUpperInvariant();
            txn = await _context.FinancialTransactions.FirstOrDefaultAsync(f => f.TransactionNo.ToUpper() == cleanNo, cancellationToken);
        }

        if (txn == null)
        {
            throw new NotFoundException("FinancialTransaction", request.IdOrNo);
        }

        var dto = _mapper.Map<FinancialTransactionDto>(txn);
        return ApiResponse<FinancialTransactionDto>.SuccessResult(dto);
    }
}
