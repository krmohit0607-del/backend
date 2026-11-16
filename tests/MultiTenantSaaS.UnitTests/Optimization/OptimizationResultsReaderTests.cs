using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.DTOs.Optimization;
using MultiTenantSaaS.Domain.Entities.Optimization;
using MultiTenantSaaS.Infrastructure.Persistence;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.UnitTests.Optimization;

public class OptimizationResultsReaderTests
{
    private static ApplicationDbContext Db()
    {
        var tenant = Guid.NewGuid();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.TenantId).Returns(tenant);
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, user.Object);
        var run = new OptimizationRun { TenantId = tenant, VoyageId = "V1" };
        run.ScenarioResults.Add(new OptimizationScenarioResult { Scenario = "earliest-eta", IsFeasible = true, TimeToGoHours = 80, MeFuelRequiredToGoMt = 120, TotalCostToGoUsd = 150000, MeCo2EmissionsToGoMt = 380, AnalysisJson = JsonSerializer.Serialize(new OptimizationAnalysisDto { PerformanceBasis = "ACTUAL" }) });
        run.ScenarioResults.Add(new OptimizationScenarioResult { Scenario = "lowest-fuel", IsFeasible = true, TimeToGoHours = 100, MeFuelRequiredToGoMt = 90, TotalCostToGoUsd = 140000, MeCo2EmissionsToGoMt = 285 });
        run.ScenarioResults.Add(new OptimizationScenarioResult { Scenario = "specific-eta", IsFeasible = false, InfeasibleReason = "The earliest feasible ETA is later than requested." });
        db.OptimizationRuns.Add(run);
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task ResultsIncludeInfeasibilityReasonsFromTheStoredRun()
    {
        var json = JsonSerializer.Serialize(await OptimizationResultsReader.GetResultsAsync(Db(), "V1", null, default));

        json.Should().Contain("The earliest feasible ETA is later than requested.");
        json.Should().Contain("ACTUAL");
    }

    [Fact]
    public async Task ComparisonDifferencesComeFromStoredNumbers()
    {
        var json = JsonSerializer.Serialize(await OptimizationResultsReader.CompareAsync(Db(), "V1", null, default));

        json.Should().Contain("\"fastestScenario\":\"earliest-eta\"");
        json.Should().Contain("\"lowestFuelScenario\":\"lowest-fuel\"");
        json.Should().Contain("\"fuelMtVsLowest\":30");
        json.Should().Contain("\"hoursVsFastest\":20");
    }

    [Fact]
    public async Task UnknownVoyageReturnsAnErrorInsteadOfInventedData()
    {
        var json = JsonSerializer.Serialize(await OptimizationResultsReader.GetResultsAsync(Db(), "NOPE", null, default));

        json.Should().Contain("No stored optimization run");
    }
}
