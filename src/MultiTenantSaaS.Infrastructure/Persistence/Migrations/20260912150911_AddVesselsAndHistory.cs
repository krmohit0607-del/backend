using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselsAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vessels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Imo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mmsi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IceClass = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Statcode5 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Statcode5Desc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VesselType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuilderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuilderCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuilderCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuilderTown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuiltYear = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StandardDesign = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LengthBp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LengthOverall = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Depth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BreadthMoulded = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deadweight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Displacement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Draught = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HullType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Holds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Teu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GasCapacity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SternLoading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InertGasSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeelLaid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeelToMastHeight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinesPerSide = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParallelBodyLength = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoroLanesLength = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineBuilder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineDesign = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineModel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnginesRpm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalKwMainEng = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FuelConsMainEng = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuxEngineTotalKw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratorsKw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThrustersTotalKw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceSpeed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Flag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Owner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Operator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassSociety = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vessels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vessels_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VesselHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VesselId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VesselHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VesselHistories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VesselHistories_Vessels_VesselId",
                        column: x => x.VesselId,
                        principalTable: "Vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VesselHistories_TenantId",
                table: "VesselHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VesselHistories_VesselId",
                table: "VesselHistories",
                column: "VesselId");

            migrationBuilder.CreateIndex(
                name: "IX_Vessels_TenantId_Imo",
                table: "Vessels",
                columns: new[] { "TenantId", "Imo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VesselHistories");

            migrationBuilder.DropTable(
                name: "Vessels");
        }
    }
}
