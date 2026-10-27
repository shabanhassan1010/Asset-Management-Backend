IF DB_ID(N'AssetManagementDb') IS NULL
BEGIN
    CREATE DATABASE [AssetManagementDb];
END
GO

USE [AssetManagementDb];
GO


/* =====================================================================
   SECTION 1 — Organisational lookups
   These describe the company, not the assets. Cached with a long TTL
   by the API (R5.1).
   ===================================================================== */

IF OBJECT_ID(N'[dbo].[Categories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Categories]
    (
        [Id]           INT           IDENTITY(1,1) NOT NULL,
        [CategoryName] NVARCHAR(100) NOT NULL,
        [Description]  NVARCHAR(500) NULL,
        -- Every FK below is NO ACTION, so a referenced row can never be
        -- deleted. IsActive is how a dead lookup leaves the dropdowns.
        [IsActive]     BIT           NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT (1),

        CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Categories_CategoryName] UNIQUE ([CategoryName])
    );
END
GO

IF OBJECT_ID(N'[dbo].[AssetTypes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssetTypes]
    (
        [Id]          INT           IDENTITY(1,1) NOT NULL,
        [TypeName]    NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive]    BIT           NOT NULL CONSTRAINT [DF_AssetTypes_IsActive] DEFAULT (1),

        CONSTRAINT [PK_AssetTypes] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_AssetTypes_TypeName] UNIQUE ([TypeName])
    );
END
GO

IF OBJECT_ID(N'[dbo].[Departments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Departments]
    (
        [Id]             INT           IDENTITY(1,1) NOT NULL,
        [DepartmentName] NVARCHAR(150) NOT NULL,
        [Code]           NVARCHAR(20)  NOT NULL,
        [IsActive]       BIT           NOT NULL CONSTRAINT [DF_Departments_IsActive] DEFAULT (1),

        CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED ([Id]),
        -- Code is a business key: two departments must not share one.
        CONSTRAINT [UQ_Departments_Code] UNIQUE ([Code])
    );
    -- NOTE: no FK to Locations on purpose. A department is not confined to
    -- one place (IT has people on two floors and in the warehouse), and an
    -- asset must be able to move between locations independently of which
    -- department owns it (R3, R3.1). The asset carries its own LocationId.
END
GO

IF OBJECT_ID(N'[dbo].[Locations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Locations]
    (
        [Id]           INT           IDENTITY(1,1) NOT NULL,
        [LocationName] NVARCHAR(150) NOT NULL,
        [Address]      NVARCHAR(300) NULL,
        [IsActive]     BIT           NOT NULL CONSTRAINT [DF_Locations_IsActive] DEFAULT (1),

        CONSTRAINT [PK_Locations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Locations_LocationName] UNIQUE ([LocationName])
    );
END
GO

IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Employees]
    (
        [Id]           INT           IDENTITY(1,1) NOT NULL,
        [EmployeeCode] NVARCHAR(30)  NOT NULL,
        [FullName]     NVARCHAR(200) NOT NULL,
        -- Nullable (not everyone has a work email) but unique when present
        -- -> filtered unique index below, not a UNIQUE constraint.
        [Email]        NVARCHAR(256) NULL,
        [Phone]        NVARCHAR(30)  NULL,
        [DepartmentId] INT           NOT NULL,
        [IsActive]     BIT           NOT NULL CONSTRAINT [DF_Employees_IsActive] DEFAULT (1),

        CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Employees_EmployeeCode] UNIQUE ([EmployeeCode]),
        CONSTRAINT [FK_Employees_Departments] FOREIGN KEY ([DepartmentId])
            REFERENCES [dbo].[Departments]([Id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_Employees_Email'
               AND [object_id] = OBJECT_ID(N'[dbo].[Employees]'))
    CREATE UNIQUE NONCLUSTERED INDEX [UX_Employees_Email]
        ON [dbo].[Employees]([Email]) WHERE [Email] IS NOT NULL;
GO

-- SQL Server does NOT index foreign keys automatically.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Employees_DepartmentId'
               AND [object_id] = OBJECT_ID(N'[dbo].[Employees]'))
    CREATE NONCLUSTERED INDEX [IX_Employees_DepartmentId]
        ON [dbo].[Employees]([DepartmentId]);
GO

-- Supports "Which assets are assigned to Ahmed?" (R4) when the name is
-- matched by prefix. A leading-wildcard LIKE will still scan — the
-- employee table is small, so that is an accepted trade-off.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Employees_FullName'
               AND [object_id] = OBJECT_ID(N'[dbo].[Employees]'))
    CREATE NONCLUSTERED INDEX [IX_Employees_FullName]
        ON [dbo].[Employees]([FullName]) INCLUDE ([DepartmentId]);
GO


/* =====================================================================
   SECTION 2 — ASP.NET Core Identity (R1)
   Hand-written so EF Core can scaffold them database-first.
   Customisations on AspNetUsers: IsActive, CreatedAt, and the EmployeeId
   bridge that makes "show me MY assets" possible (R4).
   ===================================================================== */

IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetRoles]
    (
        [Id]               NVARCHAR(450) NOT NULL,
        [Name]             NVARCHAR(256) NULL,
        [NormalizedName]   NVARCHAR(256) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL,

        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex]
        ON [dbo].[AspNetRoles]([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
END
GO

IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUsers]
    (
        [Id]                   NVARCHAR(450)     NOT NULL,
        [UserName]             NVARCHAR(256)     NULL,
        [NormalizedUserName]   NVARCHAR(256)     NULL,
        [Email]                NVARCHAR(256)     NULL,
        [NormalizedEmail]      NVARCHAR(256)     NULL,
        [EmailConfirmed]       BIT               NOT NULL,
        [PasswordHash]         NVARCHAR(MAX)     NULL,
        [SecurityStamp]        NVARCHAR(MAX)     NULL,
        [ConcurrencyStamp]     NVARCHAR(MAX)     NULL,
        [PhoneNumber]          NVARCHAR(MAX)     NULL,
        [PhoneNumberConfirmed] BIT               NOT NULL,
        [TwoFactorEnabled]     BIT               NOT NULL,
        [LockoutEnd]           DATETIMEOFFSET(7) NULL,
        [LockoutEnabled]       BIT               NOT NULL,
        [AccessFailedCount]    INT               NOT NULL,

        -- Custom columns (R1.7: Admin can disable a user)
        [IsActive]  BIT          NOT NULL CONSTRAINT [DF_AspNetUsers_IsActive]  DEFAULT (1),
        [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedAt] DEFAULT (SYSUTCDATETIME()),

        -- THE BRIDGE. A login account maps to at most one employee.
        -- NULL -> service or Admin account with no person behind it.
        -- This id goes into the JWT as a claim at login, which turns
        -- "show me my assets" into a single indexed lookup (R4).
        [EmployeeId] INT NULL,

        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AspNetUsers_Employees] FOREIGN KEY ([EmployeeId])
            REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX [EmailIndex] ON [dbo].[AspNetUsers]([NormalizedEmail]);
    CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex]
        ON [dbo].[AspNetUsers]([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

    -- One employee cannot hold two login accounts. Filtered so the many
    -- NULLs (accounts with no employee) do not collide with each other.
    CREATE UNIQUE NONCLUSTERED INDEX [UX_AspNetUsers_EmployeeId]
        ON [dbo].[AspNetUsers]([EmployeeId]) WHERE [EmployeeId] IS NOT NULL;
END
GO

IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetRoleClaims]
    (
        [Id]         INT           IDENTITY(1,1) NOT NULL,
        [RoleId]     NVARCHAR(450) NOT NULL,
        [ClaimType]  NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,

        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles] FOREIGN KEY ([RoleId])
            REFERENCES [dbo].[AspNetRoles]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims]([RoleId]);
END
GO

IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserClaims]
    (
        [Id]         INT           IDENTITY(1,1) NOT NULL,
        [UserId]     NVARCHAR(450) NOT NULL,
        [ClaimType]  NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,

        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims]([UserId]);
END
GO

IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserLogins]
    (
        [LoginProvider]       NVARCHAR(450) NOT NULL,
        [ProviderKey]         NVARCHAR(450) NOT NULL,
        [ProviderDisplayName] NVARCHAR(MAX) NULL,
        [UserId]              NVARCHAR(450) NOT NULL,

        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins]([UserId]);
END
GO

IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles]
    (
        [UserId] NVARCHAR(450) NOT NULL,
        [RoleId] NVARCHAR(450) NOT NULL,

        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles] FOREIGN KEY ([RoleId])
            REFERENCES [dbo].[AspNetRoles]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles]([RoleId]);
END
GO

IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUserTokens]
    (
        [UserId]        NVARCHAR(450) NOT NULL,
        [LoginProvider] NVARCHAR(450) NOT NULL,
        [Name]          NVARCHAR(450) NOT NULL,
        [Value]         NVARCHAR(MAX) NULL,

        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );
END
GO

-- R1.6 — refresh tokens with rotation.
IF OBJECT_ID(N'[dbo].[RefreshTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RefreshTokens]
    (
        [Id]              INT           IDENTITY(1,1) NOT NULL,
        [UserId]          NVARCHAR(450) NOT NULL,
        -- 200 chars is generous: 64 random bytes is ~88 chars in Base64.
        -- NVARCHAR(MAX) would make the unique index below impossible.
        [Token]           NVARCHAR(200) NOT NULL,
        [ExpiresAt]       DATETIME2(7)  NOT NULL,
        [IsRevoked]       BIT           NOT NULL CONSTRAINT [DF_RefreshTokens_IsRevoked] DEFAULT (0),
        [CreatedAt]       DATETIME2(7)  NOT NULL CONSTRAINT [DF_RefreshTokens_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        -- Rotation: the token that replaced this one. Lets you detect
        -- replay of an already-rotated token and revoke the whole chain.
        [ReplacedByToken] NVARCHAR(200) NULL,

        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_RefreshTokens_AspNetUsers] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );

    -- Every refresh call looks the token up by value. Without this it scans.
    CREATE UNIQUE NONCLUSTERED INDEX [UX_RefreshTokens_Token] ON [dbo].[RefreshTokens]([Token]);
    -- "Revoke every session for this user" on logout / password change.
    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens]([UserId]);
END
GO


/* =====================================================================
   SECTION 3 — Assets (R2)
   Answers "where is it now". History lives in AssetTransfers.
   ===================================================================== */

IF OBJECT_ID(N'[dbo].[Assets]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Assets]
    (
        [Id]                 INT            IDENTITY(1,1) NOT NULL,

        -- Human-readable tag on the physical sticker, e.g. IT-LAP-0042.
        [AssetCode]          NVARCHAR(50)   NOT NULL,
        [AssetName]          NVARCHAR(200)  NOT NULL,
        -- NVARCHAR(1000) rather than MAX: MAX is stored off-row and cannot
        -- be indexed. 1000 characters is ample for a description.
        [Description]        NVARCHAR(1000) NULL,

        -- Classification. Required — related entities, never free text.
        [CategoryId]         INT            NOT NULL,
        [AssetTypeId]        INT            NOT NULL,

        -- Hardware identity (R2 field list).
        -- Manufacturer is what makes "How many Dell laptops do we have?"
        -- answerable by the AI feature (R4).
        [Manufacturer]       NVARCHAR(100)  NULL,
        [Model]              NVARCHAR(100)  NULL,
        -- Nullable, but unique where present (filtered index below).
        [SerialNumber]       NVARCHAR(100)  NULL,

        -- Current custody. All nullable: a new asset sits in stock with
        -- no department, no holder, but it still has a physical location.
        [DepartmentId]       INT            NULL,
        [AssignedEmployeeId] INT            NULL,
        [LocationId]         INT            NULL,

        -- Lifecycle. The ONLY state field: 'Retired' is the soft-delete
        -- state (R2.5). There is deliberately no separate IsRetired /
        -- IsDeleted flag — two fields for one fact can disagree.
        [Status]             NVARCHAR(50)   NOT NULL CONSTRAINT [DF_Assets_Status] DEFAULT (N'Available'),

        -- DATE, not DATETIME2: nobody buys a laptop at 14:32:07.1234567,
        -- and DATE removes a class of equality and time-zone bugs.
        [PurchaseDate]       DATE           NULL,
        -- Explicit precision on money. Admin-only in the API (R2.6).
        [PurchaseCost]       DECIMAL(18,2)  NULL,
        [WarrantyExpiryDate] DATE           NULL,

        -- R3.5 — optimistic concurrency. EF Core maps ROWVERSION to a
        -- concurrency token, so the second of two simultaneous transfers
        -- throws DbUpdateConcurrencyException instead of silently winning.
        [RowVersion]         ROWVERSION     NOT NULL,

        -- R2.7 — audit. AssetTransfers records who MOVED an asset; these
        -- record who created and last edited it (cost gets disputed).
        [CreatedAt]          DATETIME2(7)   NOT NULL CONSTRAINT [DF_Assets_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedByUserId]    NVARCHAR(450)  NULL,
        [UpdatedAt]          DATETIME2(7)   NULL,
        [UpdatedByUserId]    NVARCHAR(450)  NULL,

        CONSTRAINT [PK_Assets] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Assets_AssetCode] UNIQUE ([AssetCode]),

        CONSTRAINT [FK_Assets_Categories] FOREIGN KEY ([CategoryId])
            REFERENCES [dbo].[Categories]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Assets_AssetTypes] FOREIGN KEY ([AssetTypeId])
            REFERENCES [dbo].[AssetTypes]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Assets_Departments] FOREIGN KEY ([DepartmentId])
            REFERENCES [dbo].[Departments]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Assets_Employees] FOREIGN KEY ([AssignedEmployeeId])
            REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Assets_Locations] FOREIGN KEY ([LocationId])
            REFERENCES [dbo].[Locations]([Id]) ON DELETE NO ACTION,

        -- Four legal states, nothing else. Without this one lowercase
        -- "assigned" silently breaks every status filter in the app.
        CONSTRAINT [CK_Assets_Status] CHECK
            ([Status] IN (N'Available', N'Assigned', N'Under Maintenance', N'Retired')),

        -- An asset marked Assigned must actually have a holder.
        CONSTRAINT [CK_Assets_AssignedHasEmployee] CHECK
            ([Status] <> N'Assigned' OR [AssignedEmployeeId] IS NOT NULL),

        -- Cost is either unknown or a real amount, never negative.
        CONSTRAINT [CK_Assets_PurchaseCost] CHECK
            ([PurchaseCost] IS NULL OR [PurchaseCost] >= 0)
    );

    -- Unique but nullable -> filtered. Many assets have no serial number;
    -- no two may share one (R2).
    CREATE UNIQUE NONCLUSTERED INDEX [UX_Assets_SerialNumber]
        ON [dbo].[Assets]([SerialNumber]) WHERE [SerialNumber] IS NOT NULL;

    -- R2.2 filters + dashboard counts. The INCLUDE means the common list
    -- query is answered from the index without touching the table.
    CREATE NONCLUSTERED INDEX [IX_Assets_Status_DepartmentId]
        ON [dbo].[Assets]([Status], [DepartmentId])
        INCLUDE ([AssetCode], [AssetName], [AssignedEmployeeId], [LocationId], [CategoryId], [AssetTypeId]);

    -- "My assets" — the hot path for the role-aware UI and the AI feature.
    CREATE NONCLUSTERED INDEX [IX_Assets_AssignedEmployeeId]
        ON [dbo].[Assets]([AssignedEmployeeId]);

    -- "Assets in this location" (R2.2 filter).
    CREATE NONCLUSTERED INDEX [IX_Assets_LocationId]
        ON [dbo].[Assets]([LocationId]);

    -- "How many Dell laptops do we have?" (R4) — manufacturer first,
    -- then narrowed by type.
    CREATE NONCLUSTERED INDEX [IX_Assets_Manufacturer_AssetTypeId]
        ON [dbo].[Assets]([Manufacturer], [AssetTypeId]);

    -- R2.2 free-text search by code or name.
    CREATE NONCLUSTERED INDEX [IX_Assets_AssetName]
        ON [dbo].[Assets]([AssetName]);
END
GO


/* =====================================================================
   SECTION 4 — AssetTransfers (R3)
   Answers "where has it been". One row per movement, never updated.

   Both sides of the move are stored (From* and To*) rather than only the
   destination, so a historical row stays truthful after the asset moves
   again. Reading the location off Assets instead would make every history
   row report today's location — see the README.
   ===================================================================== */

IF OBJECT_ID(N'[dbo].[AssetTransfers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AssetTransfers]
    (
        [Id]                  INT           IDENTITY(1,1) NOT NULL,
        [AssetId]             INT           NOT NULL,

        -- R3.1 — previous assignment and new assignment.
        [FromEmployeeId]      INT           NULL,
        [ToEmployeeId]        INT           NULL,
        [FromDepartmentId]    INT           NULL,
        [ToDepartmentId]      INT           NULL,
        [FromLocationId]      INT           NULL,
        [ToLocationId]        INT           NULL,

        [TransferDate]        DATETIME2(7)  NOT NULL CONSTRAINT [DF_AssetTransfers_TransferDate] DEFAULT (SYSUTCDATETIME()),
        -- NOT NULL on purpose: every move must be justified.
        [Reason]              NVARCHAR(500) NOT NULL,
        -- Who performed it (R3.1).
        [TransferredByUserId] NVARCHAR(450) NOT NULL,

        CONSTRAINT [PK_AssetTransfers] PRIMARY KEY CLUSTERED ([Id]),

        CONSTRAINT [FK_AssetTransfers_Assets] FOREIGN KEY ([AssetId])
            REFERENCES [dbo].[Assets]([Id]) ON DELETE NO ACTION,

        CONSTRAINT [FK_AssetTransfers_FromEmployee] FOREIGN KEY ([FromEmployeeId])
            REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssetTransfers_ToEmployee] FOREIGN KEY ([ToEmployeeId])
            REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssetTransfers_FromDepartment] FOREIGN KEY ([FromDepartmentId])
            REFERENCES [dbo].[Departments]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssetTransfers_ToDepartment] FOREIGN KEY ([ToDepartmentId])
            REFERENCES [dbo].[Departments]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssetTransfers_FromLocation] FOREIGN KEY ([FromLocationId])
            REFERENCES [dbo].[Locations]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssetTransfers_ToLocation] FOREIGN KEY ([ToLocationId])
            REFERENCES [dbo].[Locations]([Id]) ON DELETE NO ACTION,

        CONSTRAINT [FK_AssetTransfers_AspNetUsers] FOREIGN KEY ([TransferredByUserId])
            REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,

        -- R3.4 — a transfer must change something. This is the database
        -- half of the rule; the service also rejects a transfer where
        -- every To* equals the corresponding From*, and rejects a future
        -- TransferDate. Those comparisons belong in code, not in a CHECK.
        CONSTRAINT [CK_AssetTransfers_HasDestination] CHECK
            ([ToEmployeeId]   IS NOT NULL OR
             [ToDepartmentId] IS NOT NULL OR
             [ToLocationId]   IS NOT NULL)
    );

    -- R3.2 — history for one asset in chronological order. The most
    -- common join in the app. DESC in the key means no sort at runtime.
    CREATE NONCLUSTERED INDEX [IX_AssetTransfers_AssetId_TransferDate]
        ON [dbo].[AssetTransfers]([AssetId], [TransferDate] DESC)
        INCLUDE ([FromEmployeeId], [ToEmployeeId], [FromDepartmentId],
                 [ToDepartmentId], [FromLocationId], [ToLocationId], [Reason]);

    CREATE NONCLUSTERED INDEX [IX_AssetTransfers_TransferredByUserId]
        ON [dbo].[AssetTransfers]([TransferredByUserId]);
END
GO