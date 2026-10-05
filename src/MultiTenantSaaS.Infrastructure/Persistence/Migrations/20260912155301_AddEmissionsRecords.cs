using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmissionsRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmissionsRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VesselName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ComplianceYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EuaPriceEur = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Co2AdjustmentT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComplianceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdjustmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmissionsRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmissionsRecords_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmissionsRecords_TenantId",
                table: "EmissionsRecords",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmissionsRecords");
        }
    }
}
