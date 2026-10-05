using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Mappings;
using MultiTenantSaaS.Application.DTOs.Bunker;
using MultiTenantSaaS.Application.Features.Bunker.Commands;
using MultiTenantSaaS.Application.Features.Bunker.Queries;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Bunker;

public class BunkerModuleTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;

    public BunkerModuleTests()
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
    public async Task CreateBunkerRequirement_ShouldPersistAndReturnDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid());

        using var context = CreateInMemoryDbContext();
        var handler = new CreateBunkerRequirementCommandHandler(
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object,
            _mapper);

        var request = new CreateBunkerRequirementCommand(new CreateBunkerRequirementRequestDto
        {
            RequirementNo = "BR-TEST-001",
            VesselName = "MV OCEANIC PIONEER",
            BunkerPort = "Singapore",
            FuelType = "VLSFO",
            Quantity = 1000,
            PricePerMt = 675.0,
            TotalCost = 675000.0,
            Status = "Booked"
        });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RequirementNo.Should().Be("BR-TEST-001");
        result.Data.Quantity.Should().Be(1000);

        var dbItem = await context.BunkerRequirements.FirstOrDefaultAsync(b => b.RequirementNo == "BR-TEST-001");
        dbItem.Should().NotBeNull();
        dbItem!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetBunkerRequirements_ShouldReturnTenantRecordsOnly()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantA);

        using var context = CreateInMemoryDbContext();
        context.BunkerRequirements.Add(new BunkerRequirement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            RequirementNo = "BR-TENANT-A",
            VesselName = "Vessel A",
            BunkerPort = "Rotterdam",
            Quantity = 500
        });
        await context.SaveChangesAsync();

        var queryHandler = new GetBunkerRequirementsQueryHandler(context, _mapper);

        // Act
        var result = await queryHandler.Handle(new GetBunkerRequirementsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].RequirementNo.Should().Be("BR-TENANT-A");
    }
}
