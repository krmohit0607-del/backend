using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCharteringAndEstimates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CargoBookEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CargoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Commodity = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CargoType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tolerance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoadPort = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DischargePort = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoadRate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DischargeRate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Terms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LaycanStart = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LaycanEnd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoyageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpenDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NominationDeadline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CargoStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommercialStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Account = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoBookEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CargoBookEntries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TonnageBookEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TonnageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VesselName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Imo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VesselType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dwt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Flag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EarliestOpen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LatestOpen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoyageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommercialStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TonnageBookEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TonnageBookEntries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoyageEstimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimateNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VesselName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FixType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Profit = table.Column<double>(type: "float", nullable: false),
                    Tce = table.Column<double>(type: "float", nullable: false),
                    Commodity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoadPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DischargePort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    FreightRate = table.Column<double>(type: "float", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoyageEstimates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoyageEstimates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CargoBookEntries_TenantId",
                table: "CargoBookEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TonnageBookEntries_TenantId",
                table: "TonnageBookEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VoyageEstimates_TenantId",
                table: "VoyageEstimates",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CargoBookEntries");

            migrationBuilder.DropTable(
                name: "TonnageBookEntries");

            migrationBuilder.DropTable(
                name: "VoyageEstimates");
        }
    }
}
