using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Domain.Entities.Performance;
using MultiTenantSaaS.Infrastructure.Persistence;
using MultiTenantSaaS.Infrastructure.Services.Assistant;

namespace MultiTenantSaaS.UnitTests.Assistant;

public class AssistantToolsTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();

    private static (ApplicationDbContext Db, PerformanceDbContext Perf, Voyage Voyage) Setup()
    {
        var current = new Mock<ICurrentUserService>();
        current.Setup(u => u.TenantId).Returns(Tenant);
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, current.Object);
        var perf = new PerformanceDbContext(new DbContextOptionsBuilder<PerformanceDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, current.Object);

        var voyage = new Voyage { TenantId = Tenant, VoyageCode = "OPT001", VesselName = "MV Alpha", PortFrom = "Tema", PortTo = "Rotterdam", Status = "At Sea", Client = "Acme" };
        db.Voyages.Add(voyage);
        db.Voyages.Add(new Voyage { TenantId = Tenant, VoyageCode = "OPT002", VesselName = "MV Beta", PortFrom = "Santos", PortTo = "Qingdao", Status = "Planned" });
        db.BunkerRequirements.Add(new BunkerRequirement { TenantId = Tenant, RequirementNo = "BR-1", VesselName = "MV Alpha", BunkerPort = "Las Palmas", Status = "Pending RFQ", Priority = "Low" });
        db.FinancialTransactions.Add(new FinancialTransaction { TenantId = Tenant, TransactionNo = "TXN-1", Kind = "Payable", Currency = "USD", Amount = 1000, DueIso = "2020-01-01", Status = "Draft", VesselName = "MV Alpha" });
        db.FinancialTransactions.Add(new FinancialTransaction { TenantId = Tenant, TransactionNo = "TXN-2", Kind = "Payable", Currency = "USD", Amount = 500, DueIso = "2020-01-01", Status = "Paid", VesselName = "MV Alpha" });
        perf.TracksheetRows.Add(new TracksheetRow { TenantId = Tenant, VoyageId = "OPT001", SortOrder = 0, Date = "01Jul", VlsfoRob = 400, Hrs = 24, DistR = 280 });
        perf.TracksheetRows.Add(new TracksheetRow { TenantId = Tenant, VoyageId = "OPT001", SortOrder = 1, Date = "02Jul", VlsfoRob = 372, Hrs = 24, DistR = 290, AvgSpeedO = 12 });
        db.SaveChanges();
        perf.SaveChanges();
        return (db, perf, voyage);
    }

    private static AssistantToolContext Context(string? activeVoyageId = null, Guid? user = null) => new()
    {
        UserId = user ?? User,
        TenantId = Tenant,
        UiContext = activeVoyageId is null ? default : JsonDocument.Parse($"{{\"activeVoyage\":{{\"id\":\"{activeVoyageId}\"}}}}").RootElement
    };

    private static JsonElement Args(object value) => JsonSerializer.SerializeToElement(value);

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);

    private static AssistantToolRegistry Registry(params IAssistantTool[] tools) =>
        new(tools, new MemoryCache(new MemoryCacheOptions()), Mock.Of<IAuditService>(), NullLogger<AssistantToolRegistry>.Instance);

    [Fact]
    public async Task ListVoyagesFiltersByVessel()
    {
        var (db, _, _) = Setup();

        var result = Json(await new ListVoyagesTool(db).ExecuteAsync(Context(), Args(new { vessel = "alpha" }), default));

        result.GetProperty("total").GetInt32().Should().Be(1);
        result.GetProperty("voyages")[0].GetProperty("code").GetString().Should().Be("OPT001");
    }

    [Fact]
    public async Task OpenVoyageIsUsedWhenNoVoyageIsNamed()
    {
        var (db, perf, voyage) = Setup();

        var result = Json(await new GetTracksheetSummaryTool(db, perf).ExecuteAsync(Context(voyage.Id.ToString()), Args(new { }), default));

        result.GetProperty("rowCount").GetInt32().Should().Be(2);
        result.GetProperty("vlsfo").GetProperty("consumedMt").GetDouble().Should().BeApproximately(28, 0.001);
        result.GetProperty("averageSpeedKn").GetDouble().Should().Be(12);
    }

    [Fact]
    public async Task VoyageToolsAskWhichVoyageWhenNoneIsOpen()
    {
        var (db, perf, _) = Setup();

        var result = Json(await new GetTracksheetSummaryTool(db, perf).ExecuteAsync(Context(), Args(new { }), default));

        result.GetProperty("error").GetString().Should().Contain("No voyage");
    }

    [Fact]
    public async Task FinanceSummaryCountsOnlyUnsettledItemsAsOverdue()
    {
        var (db, _, _) = Setup();

        var result = Json(await new FinanceSummaryTool(db).ExecuteAsync(Context(), Args(new { }), default));

        var payable = result.GetProperty("totals")[0];
        payable.GetProperty("overdueAmount").GetDouble().Should().Be(1000);
        payable.GetProperty("settledAmount").GetDouble().Should().Be(500);
    }

    [Fact]
    public async Task BunkerChangeIsOnlyAProposalUntilConfirmed()
    {
        var (db, _, _) = Setup();
        var registry = Registry(new UpdateBunkerRequirementTool(db));

        var outcome = await registry.ExecuteAsync("propose_bunker_update", Args(new { requirementNo = "BR-1", field = "status", value = "rfq sent" }), Context(), default);

        outcome.Action.Should().NotBeNull();
        (await db.BunkerRequirements.AsNoTracking().SingleAsync()).Status.Should().Be("Pending RFQ");

        var confirmed = await registry.ConfirmAsync(outcome.Action!.ActionId, Context(), default);

        confirmed.Success.Should().BeTrue();
        (await db.BunkerRequirements.AsNoTracking().SingleAsync()).Status.Should().Be("RFQ Sent");
        (await registry.ConfirmAsync(outcome.Action.ActionId, Context(), default)).Success.Should().BeFalse();
    }

    [Fact]
    public async Task ProposalCannotBeConfirmedByAnotherUser()
    {
        var (db, _, _) = Setup();
        var registry = Registry(new UpdateBunkerRequirementTool(db));
        var outcome = await registry.ExecuteAsync("propose_bunker_update", Args(new { requirementNo = "BR-1", field = "priority", value = "High" }), Context(), default);

        var other = await registry.ConfirmAsync(outcome.Action!.ActionId, Context(user: Guid.NewGuid()), default);

        other.Success.Should().BeFalse();
        (await db.BunkerRequirements.AsNoTracking().SingleAsync()).Priority.Should().Be("Low");
    }

    [Fact]
    public async Task InvalidBunkerValuesAreRejectedWithoutAProposal()
    {
        var (db, _, _) = Setup();
        var registry = Registry(new UpdateBunkerRequirementTool(db));

        var outcome = await registry.ExecuteAsync("propose_bunker_update", Args(new { requirementNo = "BR-1", field = "status", value = "Paid" }), Context(), default);

        outcome.Action.Should().BeNull();
        Json(outcome.Result).GetProperty("error").GetString().Should().Contain("must be one of");
    }

    [Fact]
    public async Task VoyageUpdateResolvesTheOpenVoyageAtProposalTime()
    {
        var (db, _, voyage) = Setup();
        var registry = Registry(new UpdateVoyageTool(db));

        var outcome = await registry.ExecuteAsync("propose_voyage_update", Args(new { field = "priority", value = "high" }), Context(voyage.Id.ToString()), default);
        var confirmed = await registry.ConfirmAsync(outcome.Action!.ActionId, Context(), default);

        confirmed.Success.Should().BeTrue();
        (await db.Voyages.AsNoTracking().SingleAsync(v => v.Id == voyage.Id)).Priority.Should().Be("HIGH");
    }

    [Fact]
    public void ToolDefinitionsAreUniqueAndDescribeRequiredArguments()
    {
        var (db, perf, _) = Setup();
        var tools = new IAssistantTool[] { new ListVoyagesTool(db), new UpdateBunkerRequirementTool(db), new GetTracksheetSummaryTool(db, perf) };

        var definitions = Registry(tools).Definitions().Select(Json).ToList();

        definitions.Select(d => d.GetProperty("function").GetProperty("name").GetString()).Should().OnlyHaveUniqueItems();
        var bunker = definitions.Single(d => d.GetProperty("function").GetProperty("name").GetString() == "propose_bunker_update");
        bunker.GetProperty("function").GetProperty("parameters").GetProperty("required").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo("requirementNo", "field", "value");
    }
}
