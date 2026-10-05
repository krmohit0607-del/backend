using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImoShipsAndPorts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImoShips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Imo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    VesselType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Statcode5 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Statcode5Desc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuilderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuilderCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuilderCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuilderTown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuiltYear = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
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
                    EngineBuilder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineDesign = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineModel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnginesRpm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalKwMainEng = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FuelConsMainEng = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuxEngineTotalKw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratorsKw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassSociety = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Flag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Owner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Operator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImoShips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImoShips_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PortName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PortCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnLocode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    PortType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsRiver = table.Column<bool>(type: "bit", nullable: false),
                    IsCanalEntrance = table.Column<bool>(type: "bit", nullable: false),
                    Facilities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ports_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImoShips_Imo",
                table: "ImoShips",
                column: "Imo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImoShips_TenantId",
                table: "ImoShips",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Ports_PortCode",
                table: "Ports",
                column: "PortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ports_PortName_Country",
                table: "Ports",
                columns: new[] { "PortName", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_Ports_TenantId",
                table: "Ports",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImoShips");

            migrationBuilder.DropTable(
                name: "Ports");
        }
    }
}
