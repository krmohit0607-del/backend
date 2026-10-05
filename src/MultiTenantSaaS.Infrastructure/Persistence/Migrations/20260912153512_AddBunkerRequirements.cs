using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBunkerRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BunkerRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequirementNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VesselName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Imo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Leg = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Route = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoadPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DischargePort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BunkerPort = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Eta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredOn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredIso = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FuelType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    RobArrival = table.Column<double>(type: "float", nullable: false),
                    ExpectedCons = table.Column<double>(type: "float", nullable: false),
                    ChartererInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuppliersInvited = table.Column<int>(type: "int", nullable: false),
                    Supplier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PricePerMt = table.Column<double>(type: "float", nullable: true),
                    TotalCost = table.Column<double>(type: "float", nullable: true),
                    PoNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookedOn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfirmNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuppliedQty = table.Column<double>(type: "float", nullable: true),
                    DeliveredQty = table.Column<double>(type: "float", nullable: true),
                    SupplyDateTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceAmount = table.Column<double>(type: "float", nullable: true),
                    PaymentTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueIso = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AmountPaid = table.Column<double>(type: "float", nullable: true),
                    PaymentRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuotesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FuelLinesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalChargesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BunkerRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BunkerRequirements_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BunkerRequirements_TenantId",
                table: "BunkerRequirements",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BunkerRequirements");
        }
    }
}
