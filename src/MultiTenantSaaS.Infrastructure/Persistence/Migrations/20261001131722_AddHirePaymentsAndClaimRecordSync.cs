using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHirePaymentsAndClaimRecordSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClaimRecords_AgentInvoices_InvoiceId",
                table: "ClaimRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ClaimRecords_Voyages_VoyageId",
                table: "ClaimRecords");

            migrationBuilder.DropIndex(
                name: "IX_ClaimRecords_InvoiceId",
                table: "ClaimRecords");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "ClaimRecords");

            migrationBuilder.AlterColumn<string>(
                name: "VoyageId",
                table: "ClaimRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentsJson",
                table: "ClaimRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimKey",
                table: "ClaimRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "ClaimRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "ClaimRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VesselName",
                table: "ClaimRecords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowStatus",
                table: "ClaimRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HirePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VesselName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Side = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InstallmentKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDuplicate = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Account = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OnHireDays = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OffHireDays = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ballast = table.Column<bool>(type: "bit", nullable: false),
                    Bunkers = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BunkerCredit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HirePayments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRecords_VoyageId_ClaimKey",
                table: "ClaimRecords",
                columns: new[] { "VoyageId", "ClaimKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HirePayments_VoyageId",
                table: "HirePayments",
                column: "VoyageId");

            migrationBuilder.CreateIndex(
                name: "IX_HirePayments_VoyageId_Side_InstallmentKey",
                table: "HirePayments",
                columns: new[] { "VoyageId", "Side", "InstallmentKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HirePayments");

            migrationBuilder.DropIndex(
                name: "IX_ClaimRecords_VoyageId_ClaimKey",
                table: "ClaimRecords");

            migrationBuilder.DropColumn(
                name: "AttachmentsJson",
                table: "ClaimRecords");

            migrationBuilder.DropColumn(
                name: "ClaimKey",
                table: "ClaimRecords");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "ClaimRecords");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "ClaimRecords");

            migrationBuilder.DropColumn(
                name: "VesselName",
                table: "ClaimRecords");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "ClaimRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "VoyageId",
                table: "ClaimRecords",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceId",
                table: "ClaimRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRecords_InvoiceId",
                table: "ClaimRecords",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimRecords_AgentInvoices_InvoiceId",
                table: "ClaimRecords",
                column: "InvoiceId",
                principalTable: "AgentInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimRecords_Voyages_VoyageId",
                table: "ClaimRecords",
                column: "VoyageId",
                principalTable: "Voyages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
