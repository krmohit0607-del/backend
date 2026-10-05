using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VesselName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Voyage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fixture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Counterparty = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    InvoiceNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    ExchangeRate = table.Column<double>(type: "float", nullable: false),
                    InvoiceDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueIso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Approval = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bank = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SwiftDocUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_TenantId",
                table: "FinancialTransactions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialTransactions");
        }
    }
}
