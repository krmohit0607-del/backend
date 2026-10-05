using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselPerformanceProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutoSendForecast",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoSendForecastTime",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoSendReports",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlowerBallastMax",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlowerBallastMin",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlowerLadenMax",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlowerLadenMin",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CriticalRpmMax",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CriticalRpmMin",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeadSlowRpm",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeadSlowSpeedBallast",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeadSlowSpeedLaden",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBallastDraft",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLadenDraft",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EcdisModel",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullAheadRpm",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullAheadSpeedBallast",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullAheadSpeedLaden",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HalfAheadRpm",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HalfAheadSpeedBallast",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HalfAheadSpeedLaden",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaxMcr",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaxPowerFraction",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaxRpm",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaxSpeed",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeType",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MinMcr",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MinPowerFraction",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MinRpm",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MinSpeed",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NominalPowerFraction",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scrubber",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScrubberType",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlowAheadRpm",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlowAheadSpeedBallast",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlowAheadSpeedLaden",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummerDraft",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Weather4x",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Weather4xDuration",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WslMaxSeaStateBallast",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WslMaxSeaStateLaden",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WslMaxSwhBallast",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WslMaxSwhLaden",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WslMaxWindsBallast",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WslMaxWindsLaden",
                table: "Vessels",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoSendForecast",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "AutoSendForecastTime",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "AutoSendReports",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "BlowerBallastMax",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "BlowerBallastMin",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "BlowerLadenMax",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "BlowerLadenMin",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "CriticalRpmMax",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "CriticalRpmMin",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "DeadSlowRpm",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "DeadSlowSpeedBallast",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "DeadSlowSpeedLaden",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "DefaultBallastDraft",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "DefaultLadenDraft",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "EcdisModel",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "FullAheadRpm",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "FullAheadSpeedBallast",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "FullAheadSpeedLaden",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "HalfAheadRpm",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "HalfAheadSpeedBallast",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "HalfAheadSpeedLaden",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "MaxMcr",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "MaxPowerFraction",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "MaxRpm",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "MaxSpeed",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "MeType",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "MinMcr",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "MinPowerFraction",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "MinRpm",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "MinSpeed",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "NominalPowerFraction",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "Scrubber",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "ScrubberType",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "SlowAheadRpm",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "SlowAheadSpeedBallast",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "SlowAheadSpeedLaden",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "SummerDraft",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "Weather4x",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "Weather4xDuration",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "WslMaxSeaStateBallast",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "WslMaxSeaStateLaden",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "WslMaxSwhBallast",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "WslMaxSwhLaden",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "WslMaxWindsBallast",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "WslMaxWindsLaden",
                table: "Vessels");
        }
    }
}
