using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OptimizationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VesselImo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EtdUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginLat = table.Column<double>(type: "float", nullable: false),
                    OriginLng = table.Column<double>(type: "float", nullable: false),
                    DestLat = table.Column<double>(type: "float", nullable: false),
                    DestLng = table.Column<double>(type: "float", nullable: false),
                    LoadCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HireRatePerDayUsd = table.Column<double>(type: "float", nullable: false),
                    FoPriceUsdPerMt = table.Column<double>(type: "float", nullable: false),
                    GoPriceUsdPerMt = table.Column<double>(type: "float", nullable: false),
                    EuaPriceUsdPerTonneCo2 = table.Column<double>(type: "float", nullable: false),
                    ScenariosRequestedJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RestrictedAreasJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OptimizationScenarioResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptimizationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scenario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RouteWaypointsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EtdUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EtaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DistanceNm = table.Column<double>(type: "float", nullable: false),
                    AverageSpeedKn = table.Column<double>(type: "float", nullable: false),
                    TimeToGoHours = table.Column<double>(type: "float", nullable: false),
                    EcaDistanceNm = table.Column<double>(type: "float", nullable: false),
                    EcaTimeToGoHours = table.Column<double>(type: "float", nullable: false),
                    MeFuelRequiredToGoMt = table.Column<double>(type: "float", nullable: false),
                    EcaFuelRequiredToGoMt = table.Column<double>(type: "float", nullable: false),
                    TotalCostToGoUsd = table.Column<double>(type: "float", nullable: false),
                    FuelCostToGoUsd = table.Column<double>(type: "float", nullable: false),
                    TimeCostToGoUsd = table.Column<double>(type: "float", nullable: false),
                    EmissionsCostToGoUsd = table.Column<double>(type: "float", nullable: false),
                    MeCo2EmissionsToGoMt = table.Column<double>(type: "float", nullable: false),
                    WasConstrainedByZones = table.Column<bool>(type: "bit", nullable: false),
                    IsFeasible = table.Column<bool>(type: "bit", nullable: false),
                    InfeasibleReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationScenarioResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimizationScenarioResults_OptimizationRuns_OptimizationRunId",
                        column: x => x.OptimizationRunId,
                        principalTable: "OptimizationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRuns_VoyageId_RequestedAtUtc",
                table: "OptimizationRuns",
                columns: new[] { "VoyageId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationScenarioResults_OptimizationRunId_Scenario",
                table: "OptimizationScenarioResults",
                columns: new[] { "OptimizationRunId", "Scenario" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OptimizationScenarioResults");

            migrationBuilder.DropTable(
                name: "OptimizationRuns");
        }
    }
}
