using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEtsPolicyAndCalibration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtsPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IntraEuCoveragePct = table.Column<double>(type: "float", nullable: false),
                    ExtraEuCoveragePct = table.Column<double>(type: "float", nullable: false),
                    UnknownCoveragePct = table.Column<double>(type: "float", nullable: false),
                    PhaseInJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtsPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceCalibrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VesselImo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LoadCondition = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SampleCount = table.Column<int>(type: "int", nullable: false),
                    UsedSampleCount = table.Column<int>(type: "int", nullable: false),
                    WindowFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WindowToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FuelFactor = table.Column<double>(type: "float", nullable: false),
                    SpreadPct = table.Column<double>(type: "float", nullable: false),
                    TrendPctPerMonth = table.Column<double>(type: "float", nullable: true),
                    Quality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NotesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthlyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceCalibrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EtsPolicies_TenantId",
                table: "EtsPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceCalibrations_VesselImo_LoadCondition_Status",
                table: "PerformanceCalibrations",
                columns: new[] { "VesselImo", "LoadCondition", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtsPolicies");

            migrationBuilder.DropTable(
                name: "PerformanceCalibrations");
        }
    }
}
