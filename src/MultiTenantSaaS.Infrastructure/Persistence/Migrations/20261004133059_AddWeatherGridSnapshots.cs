using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherGridSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherGridSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactorId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TileSouth = table.Column<double>(type: "float", nullable: false),
                    TileWest = table.Column<double>(type: "float", nullable: false),
                    TileSizeDeg = table.Column<double>(type: "float", nullable: false),
                    Resolution = table.Column<int>(type: "int", nullable: false),
                    MagnitudeData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DirectionData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FetchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherGridSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherGridSnapshots_FactorId_TimestampUtc_TileSouth_TileWest",
                table: "WeatherGridSnapshots",
                columns: new[] { "FactorId", "TimestampUtc", "TileSouth", "TileWest" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherGridSnapshots");
        }
    }
}
