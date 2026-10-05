using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Mappings;
using MultiTenantSaaS.Application.DTOs.Chartering;
using MultiTenantSaaS.Application.Features.Chartering.Commands;
using MultiTenantSaaS.Application.Features.Chartering.Queries;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Chartering;

public class CharteringModuleTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;

    public CharteringModuleTests()
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
    public async Task UpsertVoyageEstimate_ShouldSaveEstimateAndReturnDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid());

        using var context = CreateInMemoryDbContext();
        var handler = new UpsertVoyageEstimateCommandHandler(
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object,
            _mapper);

        var request = new UpsertVoyageEstimateCommand(new CreateVoyageEstimateRequestDto
        {
            EstimateNo = "EST-2608-99",
            VesselName = "MV TEST ESTIMATOR",
            FixType = "Voyage Charter",
            Status = "Fixed",
            Profit = 95000,
            Tce = 21500,
            Commodity = "Bauxite",
            LoadPort = "Kamsar",
            DischargePort = "San Ciprian"
        });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.EstimateNo.Should().Be("EST-2608-99");
        result.Data.Tce.Should().Be(21500);

        var dbItem = await context.VoyageEstimates.FirstOrDefaultAsync(e => e.EstimateNo == "EST-2608-99");
        dbItem.Should().NotBeNull();
        dbItem!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetCargoBook_ShouldReturnTenantEntries()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantA);

        using var context = CreateInMemoryDbContext();
        context.CargoBookEntries.Add(new CargoBookEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            CargoCode = "CG-TEST-01",
            Commodity = "Grain",
            LoadPort = "New Orleans",
            DischargePort = "Alexandria"
        });
        await context.SaveChangesAsync();

        var queryHandler = new GetCargoBookQueryHandler(context, _currentUserServiceMock.Object, _mapper);

        // Act
        var result = await queryHandler.Handle(new GetCargoBookQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].CargoCode.Should().Be("CG-TEST-01");
    }
}
