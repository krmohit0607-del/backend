IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004142153_InitialPerformance'
)
BEGIN
    CREATE TABLE [TracksheetRows] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] nvarchar(100) NOT NULL,
        [VesselImo] nvarchar(20) NOT NULL,
        [SortOrder] int NOT NULL,
        [NextPort] nvarchar(max) NOT NULL,
        [Rt] nvarchar(max) NOT NULL,
        [Date] nvarchar(max) NOT NULL,
        [Time] nvarchar(max) NOT NULL,
        [Hrs] float NULL,
        [Lat] nvarchar(max) NOT NULL,
        [Lng] nvarchar(max) NOT NULL,
        [VlsfoRob] float NULL,
        [VlsfoBunkered] float NULL,
        [VlsfoCorrected] float NULL,
        [LsmgoRob] float NULL,
        [LsmgoBunkered] float NULL,
        [LsmgoCorrected] float NULL,
        [NoneRob] float NULL,
        [NoneBunkered] float NULL,
        [NoneCorrected] float NULL,
        [DistR] float NULL,
        [DistO] float NULL,
        [DtgO] float NULL,
        [AvgSpeedO] float NULL,
        [Rpm] float NULL,
        [EnginePower] float NULL,
        [Slip] float NULL,
        [Course] float NULL,
        [Amount] float NULL,
        [WindO] nvarchar(max) NOT NULL,
        [WavesO] nvarchar(max) NOT NULL,
        [WindF] float NOT NULL,
        [WaveF] float NOT NULL,
        [CurrF] float NOT NULL,
        [AvgF] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_TracksheetRows] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004142153_InitialPerformance'
)
BEGIN
    CREATE TABLE [VoyagePerformanceReports] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] nvarchar(100) NOT NULL,
        [VesselImo] nvarchar(20) NOT NULL,
        [VesselName] nvarchar(200) NOT NULL,
        [ReportJson] nvarchar(max) NOT NULL,
        [UpdatedByEmail] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VoyagePerformanceReports] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004142153_InitialPerformance'
)
BEGIN
    CREATE INDEX [IX_TracksheetRows_TenantId_VoyageId_SortOrder] ON [TracksheetRows] ([TenantId], [VoyageId], [SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004142153_InitialPerformance'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VoyagePerformanceReports_TenantId_VoyageId] ON [VoyagePerformanceReports] ([TenantId], [VoyageId]) WHERE [TenantId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004142153_InitialPerformance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004142153_InitialPerformance', N'8.0.8');
END;
GO

COMMIT;
GO

