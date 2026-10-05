using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Mappings;
using MultiTenantSaaS.Application.DTOs.Accounts;
using MultiTenantSaaS.Application.Features.Accounts.Commands;
using MultiTenantSaaS.Application.Features.Accounts.Queries;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Accounts;

public class AccountsModuleTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;

    public AccountsModuleTests()
    {
        var configurationProvider = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = configurationProvider.CreateMapper();

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _auditServiceMock = new Mock<IAuditService>();
    }

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateTransaction_ShouldPersistAndReturnDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid());

        using var context = CreateInMemoryDbContext();
        var handler = new CreateTransactionCommandHandler(
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object,
            _mapper);

        var request = new CreateTransactionCommand(new CreateFinancialTransactionRequestDto
        {
            TransactionNo = "TXN-TEST-001",
            Kind = "Payable",
            Category = "Bunker",
            Module = "Bunker",
            VesselName = "MV OCEANIC PIONEER",
            Voyage = "V-24/26",
            Reference = "VOY-2606-024",
            Counterparty = "Ocean Bunkers",
            InvoiceNo = "INV-OB-9999",
            Currency = "USD",
            Amount = 145000,
            Status = "Due"
        });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TransactionNo.Should().Be("TXN-TEST-001");
        result.Data.Amount.Should().Be(145000);

        var dbItem = await context.FinancialTransactions.FirstOrDefaultAsync(f => f.TransactionNo == "TXN-TEST-001");
        dbItem.Should().NotBeNull();
        dbItem!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetTransactions_ShouldBeTenantScoped()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantA);

        using var context = CreateInMemoryDbContext();
        context.FinancialTransactions.Add(new FinancialTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            TransactionNo = "TXN-A-1",
            Kind = "Payable",
            Category = "Freight",
            VesselName = "Ship A",
            Counterparty = "Charterer A",
            InvoiceNo = "INV-A",
            Amount = 50000
        });
        await context.SaveChangesAsync();

        var queryHandler = new GetTransactionsQueryHandler(context, _mapper);

        // Act
        var result = await queryHandler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].TransactionNo.Should().Be("TXN-A-1");
    }
}
