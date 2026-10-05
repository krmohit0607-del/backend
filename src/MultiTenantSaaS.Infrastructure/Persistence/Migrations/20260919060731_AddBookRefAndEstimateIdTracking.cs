using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantSaaS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookRefAndEstimateIdTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Broker",
                table: "VoyageOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "VoyageOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Charterer",
                table: "VoyageOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Commodity",
                table: "VoyageOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DischargePort",
                table: "VoyageOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadPort",
                table: "VoyageOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "VoyageOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "VoyageOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "VoyageOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vessel",
                table: "VoyageOrders",
                type: "nvarchar(max)",
                nullable: true);

            // BookRef column for VoyageEstimates is already added manually to the database
            // migrationBuilder.AddColumn<string>(
            //     name: "BookRef",
            //     table: "VoyageEstimates",
            //     type: "nvarchar(max)",
            //     nullable: true);

            // EstimateId columns for TonnageBookEntries and CargoBookEntries are already added manually
            // migrationBuilder.AddColumn<string>(
            //     name: "EstimateId",
            //     table: "TonnageBookEntries",
            //     type: "nvarchar(max)",
            //     nullable: true);

            // migrationBuilder.AddColumn<string>(
            //     name: "EstimateId",
            //     table: "CargoBookEntries",
            //     type: "nvarchar(max)",
            //     nullable: true);

            migrationBuilder.CreateTable(
                name: "AdditionalServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Service = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Vendor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalServices_Voyages_VoyageId",
                        column: x => x.VoyageId,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AreaConstraints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConstraintType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GeoJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinLatitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxLatitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinLongitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxLongitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaConstraints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CargoMasters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CargoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CargoName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImoClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImsbcGroup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IbcClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IgcClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DensityMin = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DensityMax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DensityUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StowageFactor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HygroscopicRating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VentilationRequirement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemperatureControl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuitableVesselTypes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProhibitedVesselTypes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Compatibility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PicAssignment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountHolder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Swift = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Iban = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountVerified = table.Column<bool>(type: "bit", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComplianceStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComplianceCheckDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailDistributionLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recipients = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDistributionLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubSubCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyPlaintext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultTo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultCc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultBcc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvailableTokens = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnumerationValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnumerationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EnumKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EnumValue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnumerationValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinalDisbursements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedPdaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FdaNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Port = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Agent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FdaAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PdaAdvance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalancePayable = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Approval = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalDisbursements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinalDisbursements_Voyages_VoyageId",
                        column: x => x.VoyageId,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ImoShips already exists (created by the AddImoShipsAndPorts migration) — recreating it
            // here would fail with "There is already an object named 'ImoShips'".

            migrationBuilder.CreateTable(
                name: "LaytimeCalculations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaytimeTerms = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LaytimeDaysAllowed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NorTendered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NorAccepted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DischCommenced = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DischCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaysUsed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DaysAllowed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeatherDelay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShiftingDelay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExceptedDelay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetDemurragedays = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DemurrageRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DemurrageAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DespatchRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DespatchEarning = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetDemurrage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CalculatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CalculatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaytimeCalculations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaytimeCalculations_Voyages_VoyageId",
                        column: x => x.VoyageId,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Ports already exists (created by the AddImoShipsAndPorts migration) — recreating it here
            // would fail with "There is already an object named 'Ports'".

            migrationBuilder.CreateTable(
                name: "ProFormaDisbursements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PdaNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Port = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Agent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estimated = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Advance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FdaFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Approval = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProFormaDisbursements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProFormaDisbursements_Voyages_VoyageId",
                        column: x => x.VoyageId,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedPassages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypicalDistance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TypicalSpeed = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TypicalDays = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPassages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettlementMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MilestoneLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementMilestones_Voyages_VoyageId",
                        column: x => x.VoyageId,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoyageRecaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoyageFixType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CpReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CharterPartyReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdaNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FdaNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Laytime = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Demurrage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Despatch = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetResultShip = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VesselName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VesselEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VesselLoa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VesselBeam = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DraftBallast = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DraftLaden = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VesselAge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VesselClass = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineRpmMin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineRpmMax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineMcrMin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineMcrMax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScrubberFitted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScrubberType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CraneCount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CraneSwl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CraneSafeLimit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrabCount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrabWeight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrabSafeLimit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Owners = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnersCpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnersLaycanStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnersLaycanEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnersBroker = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Charterers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CharterersCpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CharterersLaycanStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CharterersLaycanEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CharterersBroker = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HirePerDay = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DemDespatch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DespatchTerm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryTerm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedeliveryPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedeliveryTerm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedeliveryDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryNotices = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CargoName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CpQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HoldCleaning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalQtyLoaded = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Ilohc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cve = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Adcom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WxClause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrokerageRate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PniClub = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArbitrationPlace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoverningLaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SanctionsClause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FreightPerMt = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BallastBonus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HullCleaningClause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedeliveryNotices = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoyageRecaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoyageRecaps_Voyages_VoyageId",
                        column: x => x.VoyageId,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfigKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConfigValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientContacts_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplateDistributionLists",
                columns: table => new
                {
                    DistributionListsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailTemplatesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplateDistributionLists", x => new { x.DistributionListsId, x.EmailTemplatesId });
                    table.ForeignKey(
                        name: "FK_EmailTemplateDistributionLists_EmailDistributionLists_DistributionListsId",
                        column: x => x.DistributionListsId,
                        principalTable: "EmailDistributionLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailTemplateDistributionLists_EmailTemplates_EmailTemplatesId",
                        column: x => x.EmailTemplatesId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PdaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Agent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Vendor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Approved = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Paid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Port = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeptStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountsStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentInvoices_ProFormaDisbursements_PdaId",
                        column: x => x.PdaId,
                        principalTable: "ProFormaDisbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AgentInvoices_Voyages_VoyageId",
                        column: x => x.VoyageId,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoyageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Settlement = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SettledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimRecords_AgentInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "AgentInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClaimRecords_Voyages_VoyageId",
                        column: x => x.VoyageId,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalServices_VoyageId",
                table: "AdditionalServices",
                column: "VoyageId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentInvoices_PdaId",
                table: "AgentInvoices",
                column: "PdaId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentInvoices_VoyageId",
                table: "AgentInvoices",
                column: "VoyageId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaConstraints_TenantId_ConstraintType",
                table: "AreaConstraints",
                columns: new[] { "TenantId", "ConstraintType" });

            migrationBuilder.CreateIndex(
                name: "IX_CargoMasters_TenantId_CargoCode",
                table: "CargoMasters",
                columns: new[] { "TenantId", "CargoCode" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CargoMasters_TenantId_CargoName",
                table: "CargoMasters",
                columns: new[] { "TenantId", "CargoName" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRecords_InvoiceId",
                table: "ClaimRecords",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimRecords_VoyageId",
                table: "ClaimRecords",
                column: "VoyageId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientContacts_ClientId",
                table: "ClientContacts",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Email",
                table: "Clients",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TenantId_Name",
                table: "Clients",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDistributionLists_TenantId_Name",
                table: "EmailDistributionLists",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateDistributionLists_EmailTemplatesId",
                table: "EmailTemplateDistributionLists",
                column: "EmailTemplatesId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_TenantId_Name",
                table: "EmailTemplates",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_EnumerationValues_EnumerationType_IsActive",
                table: "EnumerationValues",
                columns: new[] { "EnumerationType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EnumerationValues_TenantId_EnumerationType_EnumKey",
                table: "EnumerationValues",
                columns: new[] { "TenantId", "EnumerationType", "EnumKey" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinalDisbursements_VoyageId",
                table: "FinalDisbursements",
                column: "VoyageId");

            migrationBuilder.CreateIndex(
                name: "IX_LaytimeCalculations_VoyageId",
                table: "LaytimeCalculations",
                column: "VoyageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProFormaDisbursements_VoyageId",
                table: "ProFormaDisbursements",
                column: "VoyageId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPassages_TenantId_Name",
                table: "SavedPassages",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementMilestones_VoyageId_Sequence",
                table: "SettlementMilestones",
                columns: new[] { "VoyageId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_VoyageRecaps_VoyageId",
                table: "VoyageRecaps",
                column: "VoyageId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_TenantId_ConfigKey",
                table: "WorkflowConfigurations",
                columns: new[] { "TenantId", "ConfigKey" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdditionalServices");

            migrationBuilder.DropTable(
                name: "AreaConstraints");

            migrationBuilder.DropTable(
                name: "CargoMasters");

            migrationBuilder.DropTable(
                name: "ClaimRecords");

            migrationBuilder.DropTable(
                name: "ClientContacts");

            migrationBuilder.DropTable(
                name: "EmailTemplateDistributionLists");

            migrationBuilder.DropTable(
                name: "EnumerationValues");

            migrationBuilder.DropTable(
                name: "FinalDisbursements");

            migrationBuilder.DropTable(
                name: "LaytimeCalculations");

            migrationBuilder.DropTable(
                name: "SavedPassages");

            migrationBuilder.DropTable(
                name: "SettlementMilestones");

            migrationBuilder.DropTable(
                name: "VoyageRecaps");

            migrationBuilder.DropTable(
                name: "WorkflowConfigurations");

            migrationBuilder.DropTable(
                name: "AgentInvoices");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "EmailDistributionLists");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "ProFormaDisbursements");

            migrationBuilder.DropColumn(
                name: "Broker",
                table: "VoyageOrders");

            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "VoyageOrders");

            migrationBuilder.DropColumn(
                name: "Charterer",
                table: "VoyageOrders");

            migrationBuilder.DropColumn(
                name: "Commodity",
                table: "VoyageOrders");

            migrationBuilder.DropColumn(
                name: "DischargePort",
                table: "VoyageOrders");

            migrationBuilder.DropColumn(
                name: "LoadPort",
                table: "VoyageOrders");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "VoyageOrders");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "VoyageOrders");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "VoyageOrders");

            migrationBuilder.DropColumn(
                name: "Vessel",
                table: "VoyageOrders");

            // BookRef column for VoyageEstimates was not added in Up() since it was already in the database
            // migrationBuilder.DropColumn(
            //     name: "BookRef",
            //     table: "VoyageEstimates");

            // EstimateId columns were not added in Up() since they were already in the database
            // migrationBuilder.DropColumn(
            //     name: "EstimateId",
            //     table: "TonnageBookEntries");

            // migrationBuilder.DropColumn(
            //     name: "EstimateId",
            //     table: "CargoBookEntries");
        }
    }
}
