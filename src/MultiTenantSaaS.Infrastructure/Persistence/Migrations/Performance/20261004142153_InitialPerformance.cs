using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations.Performance
{
    /// <inheritdoc />
    public partial class InitialPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TracksheetRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VesselImo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    NextPort = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Time = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hrs = table.Column<double>(type: "float", nullable: true),
                    Lat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lng = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VlsfoRob = table.Column<double>(type: "float", nullable: true),
                    VlsfoBunkered = table.Column<double>(type: "float", nullable: true),
                    VlsfoCorrected = table.Column<double>(type: "float", nullable: true),
                    LsmgoRob = table.Column<double>(type: "float", nullable: true),
                    LsmgoBunkered = table.Column<double>(type: "float", nullable: true),
                    LsmgoCorrected = table.Column<double>(type: "float", nullable: true),
                    NoneRob = table.Column<double>(type: "float", nullable: true),
                    NoneBunkered = table.Column<double>(type: "float", nullable: true),
                    NoneCorrected = table.Column<double>(type: "float", nullable: true),
                    DistR = table.Column<double>(type: "float", nullable: true),
                    DistO = table.Column<double>(type: "float", nullable: true),
                    DtgO = table.Column<double>(type: "float", nullable: true),
                    AvgSpeedO = table.Column<double>(type: "float", nullable: true),
                    Rpm = table.Column<double>(type: "float", nullable: true),
                    EnginePower = table.Column<double>(type: "float", nullable: true),
                    Slip = table.Column<double>(type: "float", nullable: true),
                    Course = table.Column<double>(type: "float", nullable: true),
                    Amount = table.Column<double>(type: "float", nullable: true),
                    WindO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WavesO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WindF = table.Column<double>(type: "float", nullable: false),
                    WaveF = table.Column<double>(type: "float", nullable: false),
                    CurrF = table.Column<double>(type: "float", nullable: false),
                    AvgF = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TracksheetRows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoyagePerformanceReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VesselImo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VesselName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoyagePerformanceReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TracksheetRows_TenantId_VoyageId_SortOrder",
                table: "TracksheetRows",
                columns: new[] { "TenantId", "VoyageId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VoyagePerformanceReports_TenantId_VoyageId",
                table: "VoyagePerformanceReports",
                columns: new[] { "TenantId", "VoyageId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TracksheetRows");

            migrationBuilder.DropTable(
                name: "VoyagePerformanceReports");
        }
    }
}
