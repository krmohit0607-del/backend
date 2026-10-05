using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Mappings;
using MultiTenantSaaS.Application.DTOs.Voyages;
using MultiTenantSaaS.Application.Features.Voyages.Commands;
using MultiTenantSaaS.Application.Features.Voyages.Queries;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Voyages;

public class VoyagesTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;

    public VoyagesTests()
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
    public async Task CreateVoyage_ShouldAddVoyageToDatabaseAndReturnDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid());

        using var context = CreateInMemoryDbContext();
        var handler = new CreateVoyageCommandHandler(
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object,
            _mapper);

        var request = new CreateVoyageCommand(new CreateVoyageRequestDto
        {
            VoyageCode = "OPT100",
            VesselName = "MV TEST SHIP",
            PortFrom = "Singapore",
            PortTo = "Rotterdam",
            Status = "At Sea",
            Priority = "HIGH",
            Health = 95
        });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.VoyageCode.Should().Be("OPT100");
        result.Data.VesselName.Should().Be("MV TEST SHIP");

        var dbItem = await context.Voyages.FirstOrDefaultAsync(v => v.VoyageCode == "OPT100");
        dbItem.Should().NotBeNull();
        dbItem!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetVoyages_ShouldReturnOnlyTenantVoyages()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantA);

        using var context = CreateInMemoryDbContext();
        context.Voyages.Add(new Voyage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            VoyageCode = "OPT-A",
            VesselName = "Vessel A",
            PortFrom = "Port A",
            PortTo = "Port B"
        });

        await context.SaveChangesAsync();

        var queryHandler = new GetVoyagesQueryHandler(context, _mapper);

        // Act
        var result = await queryHandler.Handle(new GetVoyagesQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].VoyageCode.Should().Be("OPT-A");
    }
}
