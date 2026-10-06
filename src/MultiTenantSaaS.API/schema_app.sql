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
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Description] nvarchar(max) NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [Modules] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [NormalizedName] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsGlobalActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Modules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [IPAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [EmployeeModulePermissions] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeUserId] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [CanView] bit NOT NULL,
        [CanCreate] bit NOT NULL,
        [CanEdit] bit NOT NULL,
        [CanDelete] bit NOT NULL,
        [AssignedByUserId] uniqueidentifier NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployeeModulePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeModulePermissions_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Token] nvarchar(200) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByIp] nvarchar(max) NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedByIp] nvarchar(max) NULL,
        [ReplacedByToken] nvarchar(max) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [TenantModuleAccesses] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [IsEnabled] bit NOT NULL,
        [GrantedByUserId] uniqueidentifier NOT NULL,
        [GrantedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TenantModuleAccesses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantModuleAccesses_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyName] nvarchar(150) NOT NULL,
        [AdminUserId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [SubscriptionStatus] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [LastLoginAt] datetime2 NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NOT NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Users_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TenantId] ON [AuditLogs] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeModulePermissions_AssignedByUserId] ON [EmployeeModulePermissions] ([AssignedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeeModulePermissions_EmployeeUserId_ModuleId] ON [EmployeeModulePermissions] ([EmployeeUserId], [ModuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeModulePermissions_ModuleId] ON [EmployeeModulePermissions] ([ModuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeModulePermissions_TenantId] ON [EmployeeModulePermissions] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Modules_NormalizedName] ON [Modules] ([NormalizedName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TenantModuleAccesses_GrantedByUserId] ON [TenantModuleAccesses] ([GrantedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TenantModuleAccesses_ModuleId] ON [TenantModuleAccesses] ([ModuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TenantModuleAccesses_TenantId_ModuleId] ON [TenantModuleAccesses] ([TenantId], [ModuleId]) WHERE [TenantId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tenants_AdminUserId] ON [Tenants] ([AdminUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_CreatedByUserId] ON [Users] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_TenantId] ON [Users] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [AspNetUserClaims] ADD CONSTRAINT [FK_AspNetUserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [AspNetUserLogins] ADD CONSTRAINT [FK_AspNetUserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] ADD CONSTRAINT [FK_AspNetUserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [AspNetUserTokens] ADD CONSTRAINT [FK_AspNetUserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [EmployeeModulePermissions] ADD CONSTRAINT [FK_EmployeeModulePermissions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [EmployeeModulePermissions] ADD CONSTRAINT [FK_EmployeeModulePermissions_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [EmployeeModulePermissions] ADD CONSTRAINT [FK_EmployeeModulePermissions_Users_EmployeeUserId] FOREIGN KEY ([EmployeeUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [RefreshTokens] ADD CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [TenantModuleAccesses] ADD CONSTRAINT [FK_TenantModuleAccesses_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [TenantModuleAccesses] ADD CONSTRAINT [FK_TenantModuleAccesses_Users_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    ALTER TABLE [Tenants] ADD CONSTRAINT [FK_Tenants_Users_AdminUserId] FOREIGN KEY ([AdminUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912054538_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912054538_InitialCreate', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912064241_AddVesselFieldsToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [AssignedVesselImo] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912064241_AddVesselFieldsToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [AssignedVesselName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912064241_AddVesselFieldsToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912064241_AddVesselFieldsToUser', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE TABLE [VoyageOrders] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [OrderNumber] nvarchar(50) NOT NULL,
        [Client] nvarchar(100) NOT NULL,
        [ClientEmail] nvarchar(max) NULL,
        [Service] nvarchar(max) NOT NULL,
        [Priority] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VoyageOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VoyageOrders_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE TABLE [Voyages] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageCode] nvarchar(50) NOT NULL,
        [VoyageOrderId] uniqueidentifier NULL,
        [VesselName] nvarchar(100) NOT NULL,
        [Imo] nvarchar(max) NULL,
        [VesselType] nvarchar(max) NULL,
        [Flag] nvarchar(max) NULL,
        [Dwt] nvarchar(max) NULL,
        [Built] int NOT NULL,
        [Loa] nvarchar(max) NULL,
        [Beam] nvarchar(max) NULL,
        [EnginePower] nvarchar(max) NULL,
        [PortFrom] nvarchar(max) NOT NULL,
        [PortTo] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Priority] nvarchar(max) NOT NULL,
        [Etd] datetime2 NULL,
        [Eta] datetime2 NULL,
        [EtdDisplay] nvarchar(max) NULL,
        [EtaDisplay] nvarchar(max) NULL,
        [LastNoon] nvarchar(max) NULL,
        [RouteRef] nvarchar(max) NULL,
        [InterimPort] nvarchar(max) NULL,
        [Pic] nvarchar(max) NULL,
        [Client] nvarchar(max) NULL,
        [ClientEmail] nvarchar(max) NULL,
        [Service] nvarchar(max) NULL,
        [CpSpeed] float NULL,
        [CpCons] float NULL,
        [InstSpeed] float NULL,
        [InstCons] float NULL,
        [Health] int NOT NULL,
        [Remaining] nvarchar(max) NULL,
        [DueLt] int NOT NULL,
        [DueUtc] int NOT NULL,
        [OpenTasks] int NOT NULL,
        [Tags] nvarchar(max) NULL,
        [AiAlert] nvarchar(max) NULL,
        [HandoverNote] nvarchar(max) NULL,
        [OpenStatus] nvarchar(max) NOT NULL,
        [Price] decimal(18,2) NULL,
        [PricingBasis] nvarchar(max) NULL,
        [CostPerDay] float NULL,
        [FoCost] float NULL,
        [GoCost] float NULL,
        [EuaCost] float NULL,
        [ActivePassageId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_Voyages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Voyages_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Voyages_VoyageOrders_VoyageOrderId] FOREIGN KEY ([VoyageOrderId]) REFERENCES [VoyageOrders] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE TABLE [Passages] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [RouteRef] nvarchar(max) NULL,
        [InterimPort] nvarchar(max) NULL,
        [TotalDistanceNm] float NULL,
        [Status] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Passages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Passages_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Passages_Voyages_VoyageId] FOREIGN KEY ([VoyageId]) REFERENCES [Voyages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE TABLE [PassageLegs] (
        [Id] uniqueidentifier NOT NULL,
        [PassageId] uniqueidentifier NOT NULL,
        [Sequence] int NOT NULL,
        [Type] nvarchar(max) NULL,
        [FromPort] nvarchar(100) NOT NULL,
        [ToPort] nvarchar(100) NOT NULL,
        [Etd] datetime2 NULL,
        [Eta] datetime2 NULL,
        [DistanceNm] float NULL,
        [Speed] float NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PassageLegs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PassageLegs_Passages_PassageId] FOREIGN KEY ([PassageId]) REFERENCES [Passages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE INDEX [IX_PassageLegs_PassageId] ON [PassageLegs] ([PassageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE INDEX [IX_Passages_TenantId] ON [Passages] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE INDEX [IX_Passages_VoyageId] ON [Passages] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE INDEX [IX_VoyageOrders_TenantId] ON [VoyageOrders] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE INDEX [IX_Voyages_TenantId] ON [Voyages] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    CREATE INDEX [IX_Voyages_VoyageOrderId] ON [Voyages] ([VoyageOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912144900_AddVoyagesAndOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912144900_AddVoyagesAndOrders', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912150911_AddVesselsAndHistory'
)
BEGIN
    CREATE TABLE [Vessels] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Name] nvarchar(150) NOT NULL,
        [ShortName] nvarchar(max) NULL,
        [Imo] nvarchar(50) NOT NULL,
        [Mmsi] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [IceClass] nvarchar(max) NULL,
        [Statcode5] nvarchar(max) NULL,
        [Statcode5Desc] nvarchar(max) NULL,
        [VesselType] nvarchar(max) NULL,
        [BuilderName] nvarchar(max) NULL,
        [BuilderCountry] nvarchar(max) NULL,
        [BuilderCode] nvarchar(max) NULL,
        [BuilderTown] nvarchar(max) NULL,
        [BuiltYear] nvarchar(max) NULL,
        [StandardDesign] nvarchar(max) NULL,
        [Gt] nvarchar(max) NULL,
        [LengthBp] nvarchar(max) NULL,
        [LengthOverall] nvarchar(max) NULL,
        [Depth] nvarchar(max) NULL,
        [BreadthMoulded] nvarchar(max) NULL,
        [Deadweight] nvarchar(max) NULL,
        [Displacement] nvarchar(max) NULL,
        [Draught] nvarchar(max) NULL,
        [HullType] nvarchar(max) NULL,
        [Holds] nvarchar(max) NULL,
        [Teu] nvarchar(max) NULL,
        [GasCapacity] nvarchar(max) NULL,
        [SternLoading] nvarchar(max) NULL,
        [InertGasSystem] nvarchar(max) NULL,
        [KeelLaid] nvarchar(max) NULL,
        [KeelToMastHeight] nvarchar(max) NULL,
        [LinesPerSide] nvarchar(max) NULL,
        [ParallelBodyLength] nvarchar(max) NULL,
        [RoroLanesLength] nvarchar(max) NULL,
        [EngineBuilder] nvarchar(max) NULL,
        [EngineDesign] nvarchar(max) NULL,
        [EngineModel] nvarchar(max) NULL,
        [EnginesRpm] nvarchar(max) NULL,
        [TotalKwMainEng] nvarchar(max) NULL,
        [FuelConsMainEng] nvarchar(max) NULL,
        [AuxEngineTotalKw] nvarchar(max) NULL,
        [GeneratorsKw] nvarchar(max) NULL,
        [ThrustersTotalKw] nvarchar(max) NULL,
        [ServiceSpeed] nvarchar(max) NULL,
        [Flag] nvarchar(max) NULL,
        [Owner] nvarchar(max) NULL,
        [Operator] nvarchar(max) NULL,
        [ClassSociety] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_Vessels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vessels_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912150911_AddVesselsAndHistory'
)
BEGIN
    CREATE TABLE [VesselHistories] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VesselId] uniqueidentifier NOT NULL,
        [FieldName] nvarchar(100) NOT NULL,
        [FromValue] nvarchar(max) NULL,
        [ToValue] nvarchar(max) NULL,
        [ChangedBy] nvarchar(100) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_VesselHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VesselHistories_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VesselHistories_Vessels_VesselId] FOREIGN KEY ([VesselId]) REFERENCES [Vessels] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912150911_AddVesselsAndHistory'
)
BEGIN
    CREATE INDEX [IX_VesselHistories_TenantId] ON [VesselHistories] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912150911_AddVesselsAndHistory'
)
BEGIN
    CREATE INDEX [IX_VesselHistories_VesselId] ON [VesselHistories] ([VesselId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912150911_AddVesselsAndHistory'
)
BEGIN
    CREATE INDEX [IX_Vessels_TenantId_Imo] ON [Vessels] ([TenantId], [Imo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912150911_AddVesselsAndHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912150911_AddVesselsAndHistory', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912152132_AddCharteringAndEstimates'
)
BEGIN
    CREATE TABLE [CargoBookEntries] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [CargoCode] nvarchar(50) NOT NULL,
        [Commodity] nvarchar(150) NOT NULL,
        [CargoType] nvarchar(max) NOT NULL,
        [Quantity] nvarchar(max) NOT NULL,
        [Tolerance] nvarchar(max) NOT NULL,
        [LoadPort] nvarchar(max) NOT NULL,
        [DischargePort] nvarchar(max) NOT NULL,
        [LoadRate] nvarchar(max) NULL,
        [DischargeRate] nvarchar(max) NULL,
        [Terms] nvarchar(max) NULL,
        [LaycanStart] nvarchar(max) NULL,
        [LaycanEnd] nvarchar(max) NULL,
        [VoyageType] nvarchar(max) NOT NULL,
        [OpenDate] nvarchar(max) NULL,
        [NominationDeadline] nvarchar(max) NULL,
        [CargoStatus] nvarchar(max) NOT NULL,
        [CommercialStatus] nvarchar(max) NOT NULL,
        [Pic] nvarchar(max) NULL,
        [EstimationStatus] nvarchar(max) NOT NULL,
        [Account] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_CargoBookEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CargoBookEntries_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912152132_AddCharteringAndEstimates'
)
BEGIN
    CREATE TABLE [TonnageBookEntries] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [TonnageCode] nvarchar(50) NOT NULL,
        [VesselName] nvarchar(150) NOT NULL,
        [Imo] nvarchar(max) NULL,
        [VesselType] nvarchar(max) NULL,
        [Dwt] nvarchar(max) NULL,
        [Flag] nvarchar(max) NULL,
        [OpenArea] nvarchar(max) NULL,
        [OpenPort] nvarchar(max) NULL,
        [OpenDate] nvarchar(max) NULL,
        [EarliestOpen] nvarchar(max) NULL,
        [LatestOpen] nvarchar(max) NULL,
        [VoyageType] nvarchar(max) NOT NULL,
        [Source] nvarchar(max) NOT NULL,
        [CommercialStatus] nvarchar(max) NOT NULL,
        [Pic] nvarchar(max) NULL,
        [EstimationStatus] nvarchar(max) NOT NULL,
        [Owner] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_TonnageBookEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TonnageBookEntries_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912152132_AddCharteringAndEstimates'
)
BEGIN
    CREATE TABLE [VoyageEstimates] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [EstimateNo] nvarchar(50) NOT NULL,
        [VesselName] nvarchar(150) NOT NULL,
        [FixType] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Profit] float NOT NULL,
        [Tce] float NOT NULL,
        [Commodity] nvarchar(max) NULL,
        [LoadPort] nvarchar(max) NULL,
        [DischargePort] nvarchar(max) NULL,
        [Quantity] float NOT NULL,
        [FreightRate] float NOT NULL,
        [DataJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VoyageEstimates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VoyageEstimates_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912152132_AddCharteringAndEstimates'
)
BEGIN
    CREATE INDEX [IX_CargoBookEntries_TenantId] ON [CargoBookEntries] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912152132_AddCharteringAndEstimates'
)
BEGIN
    CREATE INDEX [IX_TonnageBookEntries_TenantId] ON [TonnageBookEntries] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912152132_AddCharteringAndEstimates'
)
BEGIN
    CREATE INDEX [IX_VoyageEstimates_TenantId] ON [VoyageEstimates] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912152132_AddCharteringAndEstimates'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912152132_AddCharteringAndEstimates', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912153512_AddBunkerRequirements'
)
BEGIN
    CREATE TABLE [BunkerRequirements] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [RequirementNo] nvarchar(50) NOT NULL,
        [Priority] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [VesselName] nvarchar(150) NOT NULL,
        [Imo] nvarchar(max) NULL,
        [Reference] nvarchar(max) NULL,
        [Leg] nvarchar(max) NULL,
        [Route] nvarchar(max) NULL,
        [LoadPort] nvarchar(max) NULL,
        [DischargePort] nvarchar(max) NULL,
        [BunkerPort] nvarchar(150) NOT NULL,
        [Eta] nvarchar(max) NULL,
        [RequiredOn] nvarchar(max) NULL,
        [RequiredIso] nvarchar(max) NULL,
        [FuelType] nvarchar(max) NOT NULL,
        [Grade] nvarchar(max) NOT NULL,
        [Quantity] float NOT NULL,
        [RobArrival] float NOT NULL,
        [ExpectedCons] float NOT NULL,
        [ChartererInstructions] nvarchar(max) NULL,
        [OwnerInstructions] nvarchar(max) NULL,
        [SuppliersInvited] int NOT NULL,
        [Supplier] nvarchar(max) NULL,
        [PricePerMt] float NULL,
        [TotalCost] float NULL,
        [PoNo] nvarchar(max) NULL,
        [ContractRef] nvarchar(max) NULL,
        [BookedOn] nvarchar(max) NULL,
        [ConfirmNo] nvarchar(max) NULL,
        [DeliveryMethod] nvarchar(max) NULL,
        [SuppliedQty] float NULL,
        [DeliveredQty] float NULL,
        [SupplyDateTime] nvarchar(max) NULL,
        [InvoiceNo] nvarchar(max) NULL,
        [InvoiceDate] nvarchar(max) NULL,
        [InvoiceAmount] float NULL,
        [PaymentTerms] nvarchar(max) NULL,
        [DueDate] nvarchar(max) NULL,
        [DueIso] nvarchar(max) NULL,
        [AmountPaid] float NULL,
        [PaymentRef] nvarchar(max) NULL,
        [PaymentDate] nvarchar(max) NULL,
        [ApprovalStatus] nvarchar(max) NOT NULL,
        [PaymentStatus] nvarchar(max) NOT NULL,
        [QuotesJson] nvarchar(max) NULL,
        [FuelLinesJson] nvarchar(max) NULL,
        [AdditionalChargesJson] nvarchar(max) NULL,
        [ClaimsJson] nvarchar(max) NULL,
        [AuditJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_BunkerRequirements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BunkerRequirements_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912153512_AddBunkerRequirements'
)
BEGIN
    CREATE INDEX [IX_BunkerRequirements_TenantId] ON [BunkerRequirements] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912153512_AddBunkerRequirements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912153512_AddBunkerRequirements', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912155301_AddEmissionsRecords'
)
BEGIN
    CREATE TABLE [EmissionsRecords] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageCode] nvarchar(50) NOT NULL,
        [VesselName] nvarchar(150) NOT NULL,
        [ComplianceYear] nvarchar(max) NOT NULL,
        [Trade] nvarchar(max) NULL,
        [EuaPriceEur] nvarchar(max) NOT NULL,
        [Co2AdjustmentT] nvarchar(max) NOT NULL,
        [ComplianceJson] nvarchar(max) NULL,
        [AdjustmentsJson] nvarchar(max) NULL,
        [ApprovedBy] nvarchar(max) NULL,
        [ApprovedDate] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmissionsRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmissionsRecords_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912155301_AddEmissionsRecords'
)
BEGIN
    CREATE INDEX [IX_EmissionsRecords_TenantId] ON [EmissionsRecords] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912155301_AddEmissionsRecords'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912155301_AddEmissionsRecords', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912160452_AddFinancialTransactions'
)
BEGIN
    CREATE TABLE [FinancialTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [TransactionNo] nvarchar(50) NOT NULL,
        [Kind] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [Module] nvarchar(max) NOT NULL,
        [Company] nvarchar(max) NOT NULL,
        [VesselName] nvarchar(150) NOT NULL,
        [Voyage] nvarchar(max) NOT NULL,
        [Reference] nvarchar(max) NOT NULL,
        [Fixture] nvarchar(max) NULL,
        [Counterparty] nvarchar(150) NOT NULL,
        [InvoiceNo] nvarchar(max) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [Amount] float NOT NULL,
        [ExchangeRate] float NOT NULL,
        [InvoiceDate] nvarchar(max) NOT NULL,
        [DueDate] nvarchar(max) NOT NULL,
        [DueIso] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Approval] nvarchar(max) NOT NULL,
        [Priority] nvarchar(max) NOT NULL,
        [Pic] nvarchar(max) NOT NULL,
        [Bank] nvarchar(max) NULL,
        [Method] nvarchar(max) NULL,
        [PaymentDate] nvarchar(max) NULL,
        [PaymentRef] nvarchar(max) NULL,
        [SwiftDocUrl] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [AuditJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_FinancialTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FinancialTransactions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912160452_AddFinancialTransactions'
)
BEGIN
    CREATE INDEX [IX_FinancialTransactions_TenantId] ON [FinancialTransactions] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912160452_AddFinancialTransactions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912160452_AddFinancialTransactions', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912161729_AddVesselReports'
)
BEGIN
    CREATE TABLE [VesselReports] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [ReportNo] nvarchar(50) NOT NULL,
        [ReportType] nvarchar(max) NOT NULL,
        [ReportSubtype] nvarchar(max) NULL,
        [VesselName] nvarchar(150) NOT NULL,
        [Imo] nvarchar(50) NOT NULL,
        [VoyageCode] nvarchar(max) NULL,
        [ReportDateTime] datetime2 NOT NULL,
        [Latitude] nvarchar(max) NULL,
        [Longitude] nvarchar(max) NULL,
        [CurrentPort] nvarchar(max) NULL,
        [NextPort] nvarchar(max) NULL,
        [EtaNextPort] nvarchar(max) NULL,
        [SteamingHours] float NULL,
        [DistanceObserved] float NULL,
        [DistanceEngine] float NULL,
        [SpeedObserved] float NULL,
        [SpeedEngine] float NULL,
        [SlipPercent] float NULL,
        [Course] float NULL,
        [WindDirection] nvarchar(max) NULL,
        [WindForce] float NULL,
        [SeaState] nvarchar(max) NULL,
        [Swell] nvarchar(max) NULL,
        [Barometer] float NULL,
        [AirTemp] float NULL,
        [SeaTemp] float NULL,
        [VlsfoCons] float NULL,
        [VlsfoRob] float NULL,
        [LsmgoCons] float NULL,
        [LsmgoRob] float NULL,
        [HfoCons] float NULL,
        [HfoRob] float NULL,
        [MgoCons] float NULL,
        [MgoRob] float NULL,
        [Rpm] float NULL,
        [EngineKw] float NULL,
        [DraftFwd] nvarchar(max) NULL,
        [DraftAft] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [FormValuesJson] nvarchar(max) NULL,
        [FormattedReportText] nvarchar(max) NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VesselReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VesselReports_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912161729_AddVesselReports'
)
BEGIN
    CREATE INDEX [IX_VesselReports_TenantId] ON [VesselReports] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912161729_AddVesselReports'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912161729_AddVesselReports', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914160454_AddUserSettings'
)
BEGIN
    CREATE TABLE [UserSettings] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Key] nvarchar(150) NOT NULL,
        [ValueJson] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_UserSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSettings_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserSettings_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914160454_AddUserSettings'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserSettings_TenantId_Key] ON [UserSettings] ([TenantId], [Key]) WHERE [TenantId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914160454_AddUserSettings'
)
BEGIN
    CREATE INDEX [IX_UserSettings_UpdatedByUserId] ON [UserSettings] ([UpdatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914160454_AddUserSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260914160454_AddUserSettings', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915_AddImoShipsAndPorts'
)
BEGIN
    CREATE TABLE [ImoShips] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Imo] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [VesselType] nvarchar(100) NULL,
        [Statcode5] nvarchar(50) NULL,
        [Statcode5Desc] nvarchar(max) NULL,
        [BuilderName] nvarchar(max) NULL,
        [BuilderCountry] nvarchar(max) NULL,
        [BuilderCode] nvarchar(max) NULL,
        [BuilderTown] nvarchar(max) NULL,
        [BuiltYear] nvarchar(4) NULL,
        [StandardDesign] nvarchar(max) NULL,
        [Gt] nvarchar(max) NULL,
        [LengthBp] nvarchar(max) NULL,
        [LengthOverall] nvarchar(max) NULL,
        [Depth] nvarchar(max) NULL,
        [BreadthMoulded] nvarchar(max) NULL,
        [Deadweight] nvarchar(max) NULL,
        [Displacement] nvarchar(max) NULL,
        [Draught] nvarchar(max) NULL,
        [HullType] nvarchar(max) NULL,
        [Holds] nvarchar(max) NULL,
        [Teu] nvarchar(max) NULL,
        [GasCapacity] nvarchar(max) NULL,
        [EngineBuilder] nvarchar(max) NULL,
        [EngineDesign] nvarchar(max) NULL,
        [EngineModel] nvarchar(max) NULL,
        [EnginesRpm] nvarchar(max) NULL,
        [TotalKwMainEng] nvarchar(max) NULL,
        [FuelConsMainEng] nvarchar(max) NULL,
        [AuxEngineTotalKw] nvarchar(max) NULL,
        [GeneratorsKw] nvarchar(max) NULL,
        [ClassSociety] nvarchar(max) NULL,
        [Flag] nvarchar(max) NULL,
        [Owner] nvarchar(max) NULL,
        [Operator] nvarchar(max) NULL,
        [AdditionalDataJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_ImoShips] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ImoShips_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915_AddImoShipsAndPorts'
)
BEGIN
    CREATE TABLE [Ports] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [PortName] nvarchar(150) NOT NULL,
        [PortCode] nvarchar(50) NOT NULL,
        [UnLocode] nvarchar(10) NULL,
        [Country] nvarchar(100) NOT NULL,
        [Region] nvarchar(100) NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        [PortType] nvarchar(50) NULL,
        [IsRiver] bit NOT NULL,
        [IsCanalEntrance] bit NOT NULL,
        [Facilities] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [AdditionalDataJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_Ports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Ports_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915_AddImoShipsAndPorts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ImoShips_Imo] ON [ImoShips] ([Imo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915_AddImoShipsAndPorts'
)
BEGIN
    CREATE INDEX [IX_ImoShips_TenantId] ON [ImoShips] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915_AddImoShipsAndPorts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Ports_PortCode] ON [Ports] ([PortCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915_AddImoShipsAndPorts'
)
BEGIN
    CREATE INDEX [IX_Ports_PortName_Country] ON [Ports] ([PortName], [Country]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915_AddImoShipsAndPorts'
)
BEGIN
    CREATE INDEX [IX_Ports_TenantId] ON [Ports] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915_AddImoShipsAndPorts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915_AddImoShipsAndPorts', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [Broker] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [Cargo] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [Charterer] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [Commodity] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [DischargePort] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [LoadPort] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [Owner] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [Quantity] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [Unit] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    ALTER TABLE [VoyageOrders] ADD [Vessel] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [AdditionalServices] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] uniqueidentifier NOT NULL,
        [Service] nvarchar(200) NOT NULL,
        [Vendor] nvarchar(max) NULL,
        [InvoiceNo] nvarchar(max) NULL,
        [Currency] nvarchar(max) NOT NULL,
        [Cost] decimal(18,2) NOT NULL,
        [Tax] decimal(18,2) NOT NULL,
        [Reason] nvarchar(max) NULL,
        [RequestedBy] nvarchar(max) NULL,
        [ApprovedBy] nvarchar(max) NULL,
        [ApprovedDate] datetime2 NULL,
        [Status] nvarchar(50) NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_AdditionalServices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AdditionalServices_Voyages_VoyageId] FOREIGN KEY ([VoyageId]) REFERENCES [Voyages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [AreaConstraints] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Name] nvarchar(300) NOT NULL,
        [Description] nvarchar(max) NULL,
        [ConstraintType] nvarchar(100) NOT NULL,
        [GeoJson] nvarchar(max) NULL,
        [MinLatitude] decimal(18,2) NULL,
        [MaxLatitude] decimal(18,2) NULL,
        [MinLongitude] decimal(18,2) NULL,
        [MaxLongitude] decimal(18,2) NULL,
        [ValidFrom] datetime2 NULL,
        [ValidUntil] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_AreaConstraints] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [CargoMasters] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [CargoCode] nvarchar(50) NOT NULL,
        [CargoName] nvarchar(300) NOT NULL,
        [Category] nvarchar(100) NULL,
        [SubCategory] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [UnNumber] nvarchar(max) NULL,
        [ImoClassification] nvarchar(max) NULL,
        [ImsbcGroup] nvarchar(max) NULL,
        [IbcClassification] nvarchar(max) NULL,
        [IgcClassification] nvarchar(max) NULL,
        [DensityMin] decimal(18,2) NULL,
        [DensityMax] decimal(18,2) NULL,
        [DensityUnit] nvarchar(max) NULL,
        [StowageFactor] nvarchar(max) NULL,
        [HygroscopicRating] nvarchar(max) NULL,
        [VentilationRequirement] nvarchar(max) NULL,
        [TemperatureControl] nvarchar(max) NULL,
        [SuitableVesselTypes] nvarchar(max) NULL,
        [ProhibitedVesselTypes] nvarchar(max) NULL,
        [Compatibility] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_CargoMasters] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [Clients] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Kind] nvarchar(50) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Name] nvarchar(300) NOT NULL,
        [Location] nvarchar(max) NULL,
        [Email] nvarchar(256) NULL,
        [ContactName] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [WebsiteUrl] nvarchar(max) NULL,
        [Username] nvarchar(100) NULL,
        [PasswordHash] nvarchar(max) NULL,
        [Role] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [PicAssignment] nvarchar(max) NULL,
        [BankName] nvarchar(max) NULL,
        [AccountHolder] nvarchar(max) NULL,
        [AccountNumber] nvarchar(max) NULL,
        [Swift] nvarchar(max) NULL,
        [Iban] nvarchar(max) NULL,
        [BankAccountVerified] bit NOT NULL,
        [ExternalId] nvarchar(max) NULL,
        [ComplianceStatus] nvarchar(max) NULL,
        [ComplianceCheckDate] datetime2 NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_Clients] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [EmailDistributionLists] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Name] nvarchar(300) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Recipients] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmailDistributionLists] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [EmailTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Name] nvarchar(300) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [SubCategory] nvarchar(max) NULL,
        [SubSubCategory] nvarchar(max) NULL,
        [Description] nvarchar(max) NOT NULL,
        [Subject] nvarchar(500) NOT NULL,
        [BodyHtml] nvarchar(max) NOT NULL,
        [BodyPlaintext] nvarchar(max) NULL,
        [DefaultTo] nvarchar(max) NULL,
        [DefaultCc] nvarchar(max) NULL,
        [DefaultBcc] nvarchar(max) NULL,
        [RecipientType] nvarchar(max) NULL,
        [Version] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsSystem] bit NOT NULL,
        [ApprovedBy] nvarchar(max) NULL,
        [ApprovedDate] datetime2 NULL,
        [AvailableTokens] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmailTemplates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [EnumerationValues] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [EnumerationType] nvarchar(100) NOT NULL,
        [EnumKey] nvarchar(100) NOT NULL,
        [EnumValue] nvarchar(300) NOT NULL,
        [Description] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsSystem] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EnumerationValues] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [FinalDisbursements] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] uniqueidentifier NOT NULL,
        [RelatedPdaId] uniqueidentifier NULL,
        [FdaNo] nvarchar(50) NOT NULL,
        [Port] nvarchar(100) NOT NULL,
        [Agent] nvarchar(max) NULL,
        [Currency] nvarchar(max) NOT NULL,
        [FdaAmount] decimal(18,2) NOT NULL,
        [PdaAdvance] decimal(18,2) NOT NULL,
        [BalancePayable] decimal(18,2) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Approval] nvarchar(max) NULL,
        [ReceivedDate] datetime2 NULL,
        [ApprovedDate] datetime2 NULL,
        [ApprovedBy] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_FinalDisbursements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FinalDisbursements_Voyages_VoyageId] FOREIGN KEY ([VoyageId]) REFERENCES [Voyages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [LaytimeCalculations] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] uniqueidentifier NOT NULL,
        [LaytimeTerms] nvarchar(100) NULL,
        [LaytimeDaysAllowed] decimal(18,2) NOT NULL,
        [NorTendered] datetime2 NULL,
        [NorAccepted] datetime2 NULL,
        [DischCommenced] datetime2 NULL,
        [DischCompleted] datetime2 NULL,
        [DaysUsed] decimal(18,2) NOT NULL,
        [DaysAllowed] decimal(18,2) NOT NULL,
        [WeatherDelay] decimal(18,2) NOT NULL,
        [ShiftingDelay] decimal(18,2) NOT NULL,
        [ExceptedDelay] decimal(18,2) NOT NULL,
        [NetDemurragedays] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NULL,
        [DemurrageRate] decimal(18,2) NOT NULL,
        [DemurrageAmount] decimal(18,2) NOT NULL,
        [DespatchRate] decimal(18,2) NOT NULL,
        [DespatchEarning] decimal(18,2) NOT NULL,
        [NetDemurrage] decimal(18,2) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [CalculatedDate] datetime2 NULL,
        [CalculatedBy] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_LaytimeCalculations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LaytimeCalculations_Voyages_VoyageId] FOREIGN KEY ([VoyageId]) REFERENCES [Voyages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [ProFormaDisbursements] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] uniqueidentifier NOT NULL,
        [PdaNo] nvarchar(50) NOT NULL,
        [Port] nvarchar(100) NOT NULL,
        [Agent] nvarchar(max) NULL,
        [Currency] nvarchar(max) NOT NULL,
        [Estimated] decimal(18,2) NOT NULL,
        [Advance] decimal(18,2) NOT NULL,
        [FdaFinal] decimal(18,2) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Approval] nvarchar(max) NULL,
        [ApprovedDate] datetime2 NULL,
        [ApprovedBy] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_ProFormaDisbursements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProFormaDisbursements_Voyages_VoyageId] FOREIGN KEY ([VoyageId]) REFERENCES [Voyages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [SavedPassages] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [Name] nvarchar(300) NOT NULL,
        [Description] nvarchar(max) NULL,
        [FromPort] nvarchar(max) NULL,
        [ToPort] nvarchar(max) NULL,
        [RouteJson] nvarchar(max) NULL,
        [TypicalDistance] decimal(18,2) NULL,
        [TypicalSpeed] decimal(18,2) NULL,
        [TypicalDays] decimal(18,2) NULL,
        [TimesUsed] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_SavedPassages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [SettlementMilestones] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] uniqueidentifier NOT NULL,
        [MilestoneLabel] nvarchar(200) NOT NULL,
        [ScheduledDate] datetime2 NULL,
        [CompletedDate] datetime2 NULL,
        [CompletedBy] nvarchar(max) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Sequence] int NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_SettlementMilestones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SettlementMilestones_Voyages_VoyageId] FOREIGN KEY ([VoyageId]) REFERENCES [Voyages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [VoyageRecaps] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] uniqueidentifier NOT NULL,
        [VoyageFixType] nvarchar(max) NULL,
        [CpDate] datetime2 NULL,
        [CpReference] nvarchar(100) NULL,
        [CharterPartyReference] nvarchar(max) NULL,
        [PdaNo] nvarchar(50) NULL,
        [FdaNo] nvarchar(max) NULL,
        [Status] nvarchar(max) NULL,
        [Laytime] decimal(18,2) NOT NULL,
        [Demurrage] decimal(18,2) NOT NULL,
        [Despatch] decimal(18,2) NOT NULL,
        [NetResultShip] decimal(18,2) NOT NULL,
        [VesselName] nvarchar(max) NULL,
        [VesselEmail] nvarchar(max) NULL,
        [VesselLoa] nvarchar(max) NULL,
        [VesselBeam] nvarchar(max) NULL,
        [DraftBallast] nvarchar(max) NULL,
        [DraftLaden] nvarchar(max) NULL,
        [VesselAge] nvarchar(max) NULL,
        [VesselClass] nvarchar(max) NULL,
        [EngineRpmMin] nvarchar(max) NULL,
        [EngineRpmMax] nvarchar(max) NULL,
        [EngineMcrMin] nvarchar(max) NULL,
        [EngineMcrMax] nvarchar(max) NULL,
        [ScrubberFitted] nvarchar(max) NULL,
        [ScrubberType] nvarchar(max) NULL,
        [CraneCount] nvarchar(max) NULL,
        [CraneSwl] nvarchar(max) NULL,
        [CraneSafeLimit] nvarchar(max) NULL,
        [GrabCount] nvarchar(max) NULL,
        [GrabWeight] nvarchar(max) NULL,
        [GrabSafeLimit] nvarchar(max) NULL,
        [Owners] nvarchar(max) NULL,
        [OwnersCpDate] datetime2 NULL,
        [OwnersLaycanStart] datetime2 NULL,
        [OwnersLaycanEnd] datetime2 NULL,
        [OwnersBroker] nvarchar(max) NULL,
        [Charterers] nvarchar(max) NULL,
        [CharterersCpDate] datetime2 NULL,
        [CharterersLaycanStart] datetime2 NULL,
        [CharterersLaycanEnd] datetime2 NULL,
        [CharterersBroker] nvarchar(max) NULL,
        [HirePerDay] decimal(18,2) NULL,
        [DemDespatch] nvarchar(max) NULL,
        [DespatchTerm] nvarchar(max) NULL,
        [DeliveryPort] nvarchar(max) NULL,
        [DeliveryTerm] nvarchar(max) NULL,
        [DeliveryDateTime] datetime2 NULL,
        [RedeliveryPort] nvarchar(max) NULL,
        [RedeliveryTerm] nvarchar(max) NULL,
        [RedeliveryDateTime] datetime2 NULL,
        [DeliveryNotices] nvarchar(max) NULL,
        [CargoName] nvarchar(max) NULL,
        [CpQuantity] decimal(18,2) NULL,
        [HoldCleaning] nvarchar(max) NULL,
        [FinalQtyLoaded] decimal(18,2) NULL,
        [Ilohc] nvarchar(max) NULL,
        [Cve] nvarchar(max) NULL,
        [Adcom] nvarchar(max) NULL,
        [WxClause] nvarchar(max) NULL,
        [BrokerageRate] nvarchar(max) NULL,
        [PniClub] nvarchar(max) NULL,
        [ArbitrationPlace] nvarchar(max) NULL,
        [GoverningLaw] nvarchar(max) NULL,
        [SanctionsClause] nvarchar(max) NULL,
        [FreightPerMt] decimal(18,2) NULL,
        [BallastBonus] nvarchar(max) NULL,
        [HullCleaningClause] nvarchar(max) NULL,
        [RedeliveryNotices] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VoyageRecaps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VoyageRecaps_Voyages_VoyageId] FOREIGN KEY ([VoyageId]) REFERENCES [Voyages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [WorkflowConfigurations] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [ConfigKey] nvarchar(200) NOT NULL,
        [ConfigValue] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_WorkflowConfigurations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [ClientContacts] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [ClientId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Title] nvarchar(max) NULL,
        [Email] nvarchar(256) NULL,
        [Phone] nvarchar(max) NULL,
        [Mobile] nvarchar(max) NULL,
        [IsPrimary] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_ClientContacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientContacts_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [EmailTemplateDistributionLists] (
        [DistributionListsId] uniqueidentifier NOT NULL,
        [EmailTemplatesId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_EmailTemplateDistributionLists] PRIMARY KEY ([DistributionListsId], [EmailTemplatesId]),
        CONSTRAINT [FK_EmailTemplateDistributionLists_EmailDistributionLists_DistributionListsId] FOREIGN KEY ([DistributionListsId]) REFERENCES [EmailDistributionLists] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmailTemplateDistributionLists_EmailTemplates_EmailTemplatesId] FOREIGN KEY ([EmailTemplatesId]) REFERENCES [EmailTemplates] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [AgentInvoices] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] uniqueidentifier NOT NULL,
        [PdaId] uniqueidentifier NULL,
        [InvoiceNo] nvarchar(100) NOT NULL,
        [Agent] nvarchar(200) NOT NULL,
        [Vendor] nvarchar(max) NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Approved] decimal(18,2) NOT NULL,
        [Paid] decimal(18,2) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Port] nvarchar(max) NULL,
        [DeptStatus] nvarchar(50) NOT NULL,
        [AccountsStatus] nvarchar(50) NOT NULL,
        [PaymentDate] datetime2 NULL,
        [PaymentRef] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_AgentInvoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgentInvoices_ProFormaDisbursements_PdaId] FOREIGN KEY ([PdaId]) REFERENCES [ProFormaDisbursements] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_AgentInvoices_Voyages_VoyageId] FOREIGN KEY ([VoyageId]) REFERENCES [Voyages] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE TABLE [ClaimRecords] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] uniqueidentifier NOT NULL,
        [InvoiceId] uniqueidentifier NULL,
        [ClaimType] nvarchar(100) NOT NULL,
        [ClaimReference] nvarchar(100) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Owner] nvarchar(max) NULL,
        [Settlement] decimal(18,2) NOT NULL,
        [SettledDate] datetime2 NULL,
        [SettledBy] nvarchar(max) NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_ClaimRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClaimRecords_AgentInvoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [AgentInvoices] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ClaimRecords_Voyages_VoyageId] FOREIGN KEY ([VoyageId]) REFERENCES [Voyages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_AdditionalServices_VoyageId] ON [AdditionalServices] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_AgentInvoices_PdaId] ON [AgentInvoices] ([PdaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_AgentInvoices_VoyageId] ON [AgentInvoices] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_AreaConstraints_TenantId_ConstraintType] ON [AreaConstraints] ([TenantId], [ConstraintType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CargoMasters_TenantId_CargoCode] ON [CargoMasters] ([TenantId], [CargoCode]) WHERE [TenantId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_CargoMasters_TenantId_CargoName] ON [CargoMasters] ([TenantId], [CargoName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_ClaimRecords_InvoiceId] ON [ClaimRecords] ([InvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_ClaimRecords_VoyageId] ON [ClaimRecords] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_ClientContacts_ClientId] ON [ClientContacts] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_Clients_Email] ON [Clients] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_Clients_TenantId_Name] ON [Clients] ([TenantId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_EmailDistributionLists_TenantId_Name] ON [EmailDistributionLists] ([TenantId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_EmailTemplateDistributionLists_EmailTemplatesId] ON [EmailTemplateDistributionLists] ([EmailTemplatesId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_EmailTemplates_TenantId_Name] ON [EmailTemplates] ([TenantId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_EnumerationValues_EnumerationType_IsActive] ON [EnumerationValues] ([EnumerationType], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EnumerationValues_TenantId_EnumerationType_EnumKey] ON [EnumerationValues] ([TenantId], [EnumerationType], [EnumKey]) WHERE [TenantId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_FinalDisbursements_VoyageId] ON [FinalDisbursements] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LaytimeCalculations_VoyageId] ON [LaytimeCalculations] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_ProFormaDisbursements_VoyageId] ON [ProFormaDisbursements] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_SavedPassages_TenantId_Name] ON [SavedPassages] ([TenantId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_SettlementMilestones_VoyageId_Sequence] ON [SettlementMilestones] ([VoyageId], [Sequence]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    CREATE INDEX [IX_VoyageRecaps_VoyageId] ON [VoyageRecaps] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WorkflowConfigurations_TenantId_ConfigKey] ON [WorkflowConfigurations] ([TenantId], [ConfigKey]) WHERE [TenantId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919060731_AddBookRefAndEstimateIdTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260919060731_AddBookRefAndEstimateIdTracking', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919115055_AddEstimateIdToCargoAndTonnageBooks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260919115055_AddEstimateIdToCargoAndTonnageBooks', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    ALTER TABLE [ClaimRecords] DROP CONSTRAINT [FK_ClaimRecords_AgentInvoices_InvoiceId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    ALTER TABLE [ClaimRecords] DROP CONSTRAINT [FK_ClaimRecords_Voyages_VoyageId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    DROP INDEX [IX_ClaimRecords_InvoiceId] ON [ClaimRecords];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClaimRecords]') AND [c].[name] = N'InvoiceId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ClaimRecords] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [ClaimRecords] DROP COLUMN [InvoiceId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    DROP INDEX [IX_ClaimRecords_VoyageId] ON [ClaimRecords];
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClaimRecords]') AND [c].[name] = N'VoyageId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ClaimRecords] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [ClaimRecords] ALTER COLUMN [VoyageId] nvarchar(100) NOT NULL;
    CREATE INDEX [IX_ClaimRecords_VoyageId] ON [ClaimRecords] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    ALTER TABLE [ClaimRecords] ADD [AttachmentsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    ALTER TABLE [ClaimRecords] ADD [ClaimKey] nvarchar(100) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    ALTER TABLE [ClaimRecords] ADD [DueDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    ALTER TABLE [ClaimRecords] ADD [PaymentStatus] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    ALTER TABLE [ClaimRecords] ADD [VesselName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    ALTER TABLE [ClaimRecords] ADD [WorkflowStatus] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    CREATE TABLE [HirePayments] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageId] nvarchar(100) NOT NULL,
        [VesselName] nvarchar(200) NULL,
        [Side] nvarchar(20) NOT NULL,
        [InstallmentKey] nvarchar(100) NOT NULL,
        [IsDuplicate] bit NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Account] nvarchar(200) NOT NULL,
        [FromDate] datetime2 NULL,
        [ToDate] datetime2 NULL,
        [DueDate] datetime2 NULL,
        [OnHireDays] decimal(18,2) NOT NULL,
        [OffHireDays] decimal(18,2) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Ballast] bit NOT NULL,
        [Bunkers] decimal(18,2) NOT NULL,
        [BunkerCredit] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_HirePayments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClaimRecords_VoyageId_ClaimKey] ON [ClaimRecords] ([VoyageId], [ClaimKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    CREATE INDEX [IX_HirePayments_VoyageId] ON [HirePayments] ([VoyageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HirePayments_VoyageId_Side_InstallmentKey] ON [HirePayments] ([VoyageId], [Side], [InstallmentKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001131722_AddHirePaymentsAndClaimRecordSync'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261001131722_AddHirePaymentsAndClaimRecordSync', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003122605_AddBunkerDocumentsJson'
)
BEGIN
    ALTER TABLE [BunkerRequirements] ADD [DocumentsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003122605_AddBunkerDocumentsJson'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003122605_AddBunkerDocumentsJson', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125247_AddBunkerLaycan'
)
BEGIN
    ALTER TABLE [BunkerRequirements] ADD [LaycanEnd] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125247_AddBunkerLaycan'
)
BEGIN
    ALTER TABLE [BunkerRequirements] ADD [LaycanStart] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125247_AddBunkerLaycan'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003125247_AddBunkerLaycan', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003140026_AddEmissionsMetricsAndScenarios'
)
BEGIN
    ALTER TABLE [EmissionsRecords] ADD [MetricsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003140026_AddEmissionsMetricsAndScenarios'
)
BEGIN
    CREATE TABLE [EmissionsScenarios] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NULL,
        [VoyageCode] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [InputsJson] nvarchar(max) NULL,
        [MetricsJson] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedByName] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmissionsScenarios] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmissionsScenarios_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003140026_AddEmissionsMetricsAndScenarios'
)
BEGIN
    CREATE INDEX [IX_EmissionsScenarios_TenantId] ON [EmissionsScenarios] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003140026_AddEmissionsMetricsAndScenarios'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003140026_AddEmissionsMetricsAndScenarios', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004133059_AddWeatherGridSnapshots'
)
BEGIN
    CREATE TABLE [WeatherGridSnapshots] (
        [Id] uniqueidentifier NOT NULL,
        [FactorId] nvarchar(32) NOT NULL,
        [TimestampUtc] datetime2 NOT NULL,
        [TileSouth] float NOT NULL,
        [TileWest] float NOT NULL,
        [TileSizeDeg] float NOT NULL,
        [Resolution] int NOT NULL,
        [MagnitudeData] nvarchar(max) NOT NULL,
        [DirectionData] nvarchar(max) NULL,
        [FetchedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_WeatherGridSnapshots] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004133059_AddWeatherGridSnapshots'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WeatherGridSnapshots_FactorId_TimestampUtc_TileSouth_TileWest] ON [WeatherGridSnapshots] ([FactorId], [TimestampUtc], [TileSouth], [TileWest]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004133059_AddWeatherGridSnapshots'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004133059_AddWeatherGridSnapshots', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [AutoSendForecast] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [AutoSendForecastTime] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [AutoSendReports] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [BlowerBallastMax] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [BlowerBallastMin] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [BlowerLadenMax] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [BlowerLadenMin] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [CriticalRpmMax] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [CriticalRpmMin] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [DeadSlowRpm] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [DeadSlowSpeedBallast] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [DeadSlowSpeedLaden] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [DefaultBallastDraft] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [DefaultLadenDraft] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [EcdisModel] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [FullAheadRpm] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [FullAheadSpeedBallast] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [FullAheadSpeedLaden] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [HalfAheadRpm] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [HalfAheadSpeedBallast] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [HalfAheadSpeedLaden] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [MaxMcr] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [MaxPowerFraction] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [MaxRpm] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [MaxSpeed] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [MeType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [MinMcr] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [MinPowerFraction] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [MinRpm] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [MinSpeed] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [NominalPowerFraction] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [Scrubber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [ScrubberType] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [SlowAheadRpm] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [SlowAheadSpeedBallast] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [SlowAheadSpeedLaden] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [SummerDraft] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [Weather4x] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [Weather4xDuration] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [WslMaxSeaStateBallast] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [WslMaxSeaStateLaden] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [WslMaxSwhBallast] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [WslMaxSwhLaden] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [WslMaxWindsBallast] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    ALTER TABLE [Vessels] ADD [WslMaxWindsLaden] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004151338_AddVesselPerformanceProfile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004151338_AddVesselPerformanceProfile', N'8.0.8');
END;
GO

COMMIT;
GO

