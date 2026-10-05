using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Mappings;
using MultiTenantSaaS.Application.DTOs.VesselReports;
using MultiTenantSaaS.Application.Features.VesselReports.Commands;
using MultiTenantSaaS.Application.Features.VesselReports.Queries;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Infrastructure.Persistence;
using Xunit;

namespace MultiTenantSaaS.UnitTests.VesselReports;

public class VesselReportsModuleTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;

    public VesselReportsModuleTests()
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
    public async Task SubmitVesselReport_ShouldPersistAndReturnDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid());

        using var context = CreateInMemoryDbContext();
        var handler = new SubmitVesselReportCommandHandler(
            context,
            _currentUserServiceMock.Object,
            _auditServiceMock.Object,
            _mapper);

        var request = new SubmitVesselReportCommand(new SubmitVesselReportRequestDto
        {
            ReportNo = "VR-TEST-001",
            ReportType = "Noon",
            VesselName = "MV OCEANIC PIONEER",
            Imo = "9417878",
            VoyageCode = "OPT001",
            Latitude = "01° 15' N",
            Longitude = "103° 50' E",
            SteamingHours = 24.0,
            DistanceObserved = 345.5,
            SpeedObserved = 14.4,
            VlsfoCons = 28.5,
            VlsfoRob = 412.0
        });

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ReportNo.Should().Be("VR-TEST-001");
        result.Data.SpeedObserved.Should().Be(14.4);

        var dbItem = await context.VesselReports.FirstOrDefaultAsync(r => r.ReportNo == "VR-TEST-001");
        dbItem.Should().NotBeNull();
        dbItem!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetVesselReports_ShouldFilterByTenantAndImo()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.TenantId).Returns(tenantA);

        using var context = CreateInMemoryDbContext();
        context.VesselReports.AddRange(
            new VesselReport
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                ReportNo = "VR-A-1",
                VesselName = "Ship A",
                Imo = "1111111",
                ReportType = "Noon"
            },
            new VesselReport
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                ReportNo = "VR-A-2",
                VesselName = "Ship B",
                Imo = "2222222",
                ReportType = "Departure"
            }
        );
        await context.SaveChangesAsync();

        var queryHandler = new GetVesselReportsQueryHandler(context, _mapper);

        // Act
        var result = await queryHandler.Handle(new GetVesselReportsQuery(Imo: "1111111"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Imo.Should().Be("1111111");
    }
}
