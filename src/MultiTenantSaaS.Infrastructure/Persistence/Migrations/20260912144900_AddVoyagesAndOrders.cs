using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVoyagesAndOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoyageOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Client = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClientEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Service = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoyageOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoyageOrders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Voyages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VoyageOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VesselName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Imo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VesselType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Flag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dwt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Built = table.Column<int>(type: "int", nullable: false),
                    Loa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Beam = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnginePower = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PortFrom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PortTo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Etd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Eta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EtdDisplay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EtaDisplay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastNoon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterimPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Client = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Service = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CpSpeed = table.Column<double>(type: "float", nullable: true),
                    CpCons = table.Column<double>(type: "float", nullable: true),
                    InstSpeed = table.Column<double>(type: "float", nullable: true),
                    InstCons = table.Column<double>(type: "float", nullable: true),
                    Health = table.Column<int>(type: "int", nullable: false),
                    Remaining = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueLt = table.Column<int>(type: "int", nullable: false),
                    DueUtc = table.Column<int>(type: "int", nullable: false),
                    OpenTasks = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiAlert = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HandoverNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PricingBasis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostPerDay = table.Column<double>(type: "float", nullable: true),
                    FoCost = table.Column<double>(type: "float", nullable: true),
                    GoCost = table.Column<double>(type: "float", nullable: true),
                    EuaCost = table.Column<double>(type: "float", nullable: true),
                    ActivePassageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voyages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Voyages_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voyages_VoyageOrders_VoyageOrderId",
                        column: x => x.VoyageOrderId,
                        principalTable: "VoyageOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Passages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RouteRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterimPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalDistanceNm = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Passages_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Passages_Voyages_VoyageId",
                        column: x => x.VoyageId,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PassageLegs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PassageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromPort = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToPort = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Etd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Eta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DistanceNm = table.Column<double>(type: "float", nullable: true),
                    Speed = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassageLegs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassageLegs_Passages_PassageId",
                        column: x => x.PassageId,
                        principalTable: "Passages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PassageLegs_PassageId",
                table: "PassageLegs",
                column: "PassageId");

            migrationBuilder.CreateIndex(
                name: "IX_Passages_TenantId",
                table: "Passages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Passages_VoyageId",
                table: "Passages",
                column: "VoyageId");

            migrationBuilder.CreateIndex(
                name: "IX_VoyageOrders_TenantId",
                table: "VoyageOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_TenantId",
                table: "Voyages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_VoyageOrderId",
                table: "Voyages",
                column: "VoyageOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PassageLegs");

            migrationBuilder.DropTable(
                name: "Passages");

            migrationBuilder.DropTable(
                name: "Voyages");

            migrationBuilder.DropTable(
                name: "VoyageOrders");
        }
    }
}
