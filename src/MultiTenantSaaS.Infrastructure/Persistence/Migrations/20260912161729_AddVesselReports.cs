using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VesselReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportSubtype = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VesselName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Imo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VoyageCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Longitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EtaNextPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SteamingHours = table.Column<double>(type: "float", nullable: true),
                    DistanceObserved = table.Column<double>(type: "float", nullable: true),
                    DistanceEngine = table.Column<double>(type: "float", nullable: true),
                    SpeedObserved = table.Column<double>(type: "float", nullable: true),
                    SpeedEngine = table.Column<double>(type: "float", nullable: true),
                    SlipPercent = table.Column<double>(type: "float", nullable: true),
                    Course = table.Column<double>(type: "float", nullable: true),
                    WindDirection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WindForce = table.Column<double>(type: "float", nullable: true),
                    SeaState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Swell = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Barometer = table.Column<double>(type: "float", nullable: true),
                    AirTemp = table.Column<double>(type: "float", nullable: true),
                    SeaTemp = table.Column<double>(type: "float", nullable: true),
                    VlsfoCons = table.Column<double>(type: "float", nullable: true),
                    VlsfoRob = table.Column<double>(type: "float", nullable: true),
                    LsmgoCons = table.Column<double>(type: "float", nullable: true),
                    LsmgoRob = table.Column<double>(type: "float", nullable: true),
                    HfoCons = table.Column<double>(type: "float", nullable: true),
                    HfoRob = table.Column<double>(type: "float", nullable: true),
                    MgoCons = table.Column<double>(type: "float", nullable: true),
                    MgoRob = table.Column<double>(type: "float", nullable: true),
                    Rpm = table.Column<double>(type: "float", nullable: true),
                    EngineKw = table.Column<double>(type: "float", nullable: true),
                    DraftFwd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DraftAft = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormattedReportText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VesselReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VesselReports_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VesselReports_TenantId",
                table: "VesselReports",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VesselReports");
        }
    }
}
