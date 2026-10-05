using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Mappings;
using MultiTenantSaaS.Application.DTOs.Vessels;
using MultiTenantSaaS.Application.Features.Vessels.Commands;
using MultiTenantSaaS.Application.Features.Vessels.Queries;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Vessels;

public class VesselsModuleTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;

    public VesselsModuleTests()
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
    public async Task CreateVessel_ShouldPersistVesselAndReturnDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid());

        using var context = CreateInMemoryDbContext();
        var handler = new CreateVesselCommandHandler(
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object,
            _mapper);

        var request = new CreateVesselCommand(new CreateVesselRequestDto
        {
            Name = "MV SEASPAN EMERALD",
            Imo = "9412345",
            Mmsi = "563001234",
            VesselType = "Container Ship",
            Deadweight = "120000",
            Flag = "Hong Kong"
        });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("MV SEASPAN EMERALD");
        result.Data.Imo.Should().Be("9412345");

        var dbItem = await context.Vessels.FirstOrDefaultAsync(v => v.Imo == "9412345");
        dbItem.Should().NotBeNull();
        dbItem!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task UpdateVessel_ShouldRecordHistoryAndTrackChanges()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(c => c.Email).Returns("admin@acme.com");

        using var context = CreateInMemoryDbContext();
        var vessel = new Vessel
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "OLD SHIP NAME",
            Imo = "9999999",
            Flag = "Liberia"
        };
        context.Vessels.Add(vessel);
        await context.SaveChangesAsync();

        var handler = new UpdateVesselCommandHandler(
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object,
            _mapper);

        var updateRequest = new UpdateVesselCommand(vessel.Id, new UpdateVesselRequestDto
        {
            Name = "NEW SHIP NAME",
            Imo = "9999999",
            Flag = "Panama"
        });

        // Act
        var result = await handler.Handle(updateRequest, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("NEW SHIP NAME");
        result.Data.Flag.Should().Be("Panama");

        var historyList = await context.VesselHistories.Where(h => h.VesselId == vessel.Id).ToListAsync();
        historyList.Should().NotBeEmpty();
        historyList.Should().Contain(h => h.FieldName == "Vessel Name" && h.FromValue == "OLD SHIP NAME" && h.ToValue == "NEW SHIP NAME");
        historyList.Should().Contain(h => h.FieldName == "Flag" && h.FromValue == "Liberia" && h.ToValue == "Panama");
    }

    [Fact]
    public async Task GetVessels_ShouldBeTenantIsolated()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantA);

        using var context = CreateInMemoryDbContext();
        context.Vessels.Add(new Vessel
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            Name = "Tenant A Vessel",
            Imo = "1111111"
        });
        await context.SaveChangesAsync();

        var queryHandler = new GetVesselsQueryHandler(context, _mapper);

        // Act
        var result = await queryHandler.Handle(new GetVesselsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Imo.Should().Be("1111111");
    }
}
