using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Mappings;
using MultiTenantSaaS.Application.DTOs.Emissions;
using MultiTenantSaaS.Application.Features.Emissions.Commands;
using MultiTenantSaaS.Application.Features.Emissions.Queries;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantSaaS.UnitTests.Emissions;

public class EmissionsModuleTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;

    public EmissionsModuleTests()
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
    public async Task SaveEmissionsRecord_ShouldPersistAndReturnDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid());

        using var context = CreateInMemoryDbContext();
        var handler = new SaveEmissionsRecordCommandHandler(
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object,
            _mapper);

        var request = new SaveEmissionsRecordCommand(new SaveEmissionsRecordRequestDto
        {
            VoyageCode = "OPT001",
            VesselName = "MV OCEANIC PIONEER",
            ComplianceYear = "2026",
            EuaPriceEur = "78.40",
            ApprovedBy = "Captain Compliance",
            ApprovedDate = "2026-06-18"
        });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.VoyageCode.Should().Be("OPT001");
        result.Data.EuaPriceEur.Should().Be("78.40");
        result.Data.ApprovedBy.Should().Be("Captain Compliance");

        var dbItem = await context.EmissionsRecords.FirstOrDefaultAsync(e => e.VoyageCode == "OPT001");
        dbItem.Should().NotBeNull();
        dbItem!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetEmissionsByVoyage_ShouldReturnTenantRecord()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantA);

        using var context = CreateInMemoryDbContext();
        context.EmissionsRecords.Add(new EmissionsRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            VoyageCode = "OPT002",
            VesselName = "MV ATLANTIC TRADER",
            ComplianceYear = "2026",
            EuaPriceEur = "74.50"
        });
        await context.SaveChangesAsync();

        var queryHandler = new GetEmissionsByVoyageQueryHandler(context, _mapper);

        // Act
        var result = await queryHandler.Handle(new GetEmissionsByVoyageQuery("OPT002"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.VoyageCode.Should().Be("OPT002");
    }
}
