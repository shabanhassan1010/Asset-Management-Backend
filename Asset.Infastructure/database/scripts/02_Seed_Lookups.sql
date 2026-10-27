/* =====================================================================
   AssetManagementDb  —  02_Seed_Lookups.sql
   Reference data: roles, categories, asset types, departments,
   locations, employees.

   Run after 01_Schema.sql. Idempotent — re-running inserts nothing new.

   NOTE ON USERS (R1.5): login accounts are NOT created here. Password
   hashes must be produced by ASP.NET Core Identity's hasher, never
   written by hand, so the demo Admin and User accounts are created at
   startup by the API's seeder, which then links each one to an employee
   below via AspNetUsers.EmployeeId. Credentials are documented in the
   README.
   ===================================================================== */

USE [AssetManagementDb];
GO

/* ---------------------------------------------------------------------
   Roles (R1) — exactly two, as the permission matrix requires.
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [NormalizedName] = N'ADMIN')
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (CONVERT(NVARCHAR(450), NEWID()), N'Admin', N'ADMIN', CONVERT(NVARCHAR(MAX), NEWID()));

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [NormalizedName] = N'USER')
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (CONVERT(NVARCHAR(450), NEWID()), N'User', N'USER', CONVERT(NVARCHAR(MAX), NEWID()));
GO


/* ---------------------------------------------------------------------
   Categories
   --------------------------------------------------------------------- */
INSERT INTO [dbo].[Categories] ([CategoryName], [Description])
SELECT v.[n], v.[d]
FROM (VALUES
    (N'IT Equipment',    N'Computers, phones, networking hardware'),
    (N'Office Furniture', N'Desks, chairs, cabinets'),
    (N'Vehicles',        N'Company cars and vans'),
    (N'Peripherals',     N'Monitors, keyboards, docking stations')
) AS v([n], [d])
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Categories] c WHERE c.[CategoryName] = v.[n]);
GO


/* ---------------------------------------------------------------------
   Asset types
   --------------------------------------------------------------------- */
INSERT INTO [dbo].[AssetTypes] ([TypeName], [Description])
SELECT v.[n], v.[d]
FROM (VALUES
    (N'Laptop',         N'Portable computer'),
    (N'Desktop',        N'Fixed workstation'),
    (N'Monitor',        N'External display'),
    (N'Printer',        N'Shared or personal printer'),
    (N'Mobile Phone',   N'Company handset'),
    (N'Docking Station', N'Laptop dock'),
    (N'Desk',           N'Office desk'),
    (N'Chair',          N'Office chair'),
    (N'Van',            N'Light commercial vehicle')
) AS v([n], [d])
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[AssetTypes] t WHERE t.[TypeName] = v.[n]);
GO


/* ---------------------------------------------------------------------
   Departments
   Presales is included because it appears in the R4 sample question
   ("laptops currently assigned to the Presales department").
   --------------------------------------------------------------------- */
INSERT INTO [dbo].[Departments] ([DepartmentName], [Code])
SELECT v.[n], v.[c]
FROM (VALUES
    (N'Information Technology', N'IT'),
    (N'Presales',               N'PRE'),
    (N'Finance',                N'FIN'),
    (N'Human Resources',        N'HR'),
    (N'Operations',             N'OPS')
) AS v([n], [c])
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Departments] d WHERE d.[Code] = v.[c]);
GO


/* ---------------------------------------------------------------------
   Locations
   Deliberately NOT linked to Departments: a department is not confined
   to one place, and an asset must be able to move between locations
   independently of the department that owns it (R3).
   --------------------------------------------------------------------- */
INSERT INTO [dbo].[Locations] ([LocationName], [Address])
SELECT v.[n], v.[a]
FROM (VALUES
    (N'HQ - Floor 1',            N'Head office, ground floor'),
    (N'HQ - Floor 2',            N'Head office, second floor'),
    (N'HQ - Server Room',        N'Head office, basement'),
    (N'Alexandria Warehouse',    N'Main storage facility'),
    (N'Remote / Work From Home', NULL)
) AS v([n], [a])
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Locations] l WHERE l.[LocationName] = v.[n]);
GO


/* ---------------------------------------------------------------------
   Employees
   Department resolved by Code so this script does not depend on the
   identity values produced above.
   --------------------------------------------------------------------- */
INSERT INTO [dbo].[Employees] ([EmployeeCode], [FullName], [Email], [Phone], [DepartmentId])
SELECT v.[code], v.[name], v.[email], v.[phone], d.[Id]
FROM (VALUES
    (N'EMP-001', N'Ahmed Kamal',    N'ahmed.kamal@example.com',    N'+20 100 000 0001', N'PRE'),
    (N'EMP-002', N'Sara Mahmoud',   N'sara.mahmoud@example.com',   N'+20 100 000 0002', N'PRE'),
    (N'EMP-003', N'Omar Fathy',     N'omar.fathy@example.com',     N'+20 100 000 0003', N'IT'),
    (N'EMP-004', N'Nour Abdelaziz', N'nour.abdelaziz@example.com', N'+20 100 000 0004', N'IT'),
    (N'EMP-005', N'Mona Adel',      N'mona.adel@example.com',      N'+20 100 000 0005', N'FIN'),
    (N'EMP-006', N'Youssef Hany',   N'youssef.hany@example.com',   N'+20 100 000 0006', N'OPS'),
    (N'EMP-007', N'Heba Samir',     N'heba.samir@example.com',     N'+20 100 000 0007', N'HR')
) AS v([code], [name], [email], [phone], [deptCode])
JOIN [dbo].[Departments] d ON d.[Code] = v.[deptCode]
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] e WHERE e.[EmployeeCode] = v.[code]);
GO

/*Asset Table  */

INSERT INTO [dbo].[Assets]
(
    [AssetCode], [AssetName], [Description],
    [CategoryId], [AssetTypeId],
    [Manufacturer], [Model], [SerialNumber],
    [DepartmentId], [AssignedEmployeeId], [LocationId],
    [Status], [PurchaseDate], [PurchaseCost], [WarrantyExpiryDate]
)
SELECT
    v.[code], v.[name], v.[descr],
    c.[Id], t.[Id],
    v.[mfr], v.[model], v.[serial],
    d.[Id], e.[Id], l.[Id],
    v.[status], v.[purchDate], v.[cost], v.[warranty]
FROM (VALUES
    -- Presales laptops, assigned  -> R4 question 1
    (N'IT-LAP-0001', N'Dell Latitude 5540',  N'Presales demo laptop',
     N'IT Equipment', N'Laptop', N'Dell', N'Latitude 5540', N'DL5540-0001',
     N'PRE', N'EMP-001', N'HQ - Floor 2', 2,
     CAST(N'2024-03-11' AS DATE), 32500.00, CAST(N'2027-03-11' AS DATE)),
 
    (N'IT-LAP-0002', N'Dell Latitude 5540',  N'Presales demo laptop',
     N'IT Equipment', N'Laptop', N'Dell', N'Latitude 5540', N'DL5540-0002',
     N'PRE', N'EMP-002', N'HQ - Floor 2', 2,
     CAST(N'2024-03-11' AS DATE), 32500.00, CAST(N'2027-03-11' AS DATE)),
 
    -- More Dell laptops elsewhere -> R4 question 3 counts these too
    (N'IT-LAP-0003', N'Dell Precision 3580', N'Engineering workstation laptop',
     N'IT Equipment', N'Laptop', N'Dell', N'Precision 3580', N'DP3580-0003',
     N'IT', N'EMP-003', N'HQ - Floor 1', 2,
     CAST(N'2024-06-02' AS DATE), 48750.00, CAST(N'2027-06-02' AS DATE)),
 
    (N'IT-LAP-0004', N'Dell Latitude 3540',  N'Spare laptop, unallocated',
     N'IT Equipment', N'Laptop', N'Dell', N'Latitude 3540', N'DL3540-0004',
     NULL, NULL, N'Alexandria Warehouse', 1,
     CAST(N'2025-01-20' AS DATE), 24900.00, CAST(N'2028-01-20' AS DATE)),
 
    -- Non-Dell laptops, so the manufacturer filter is doing real work
    (N'IT-LAP-0005', N'HP EliteBook 840 G10', N'Finance laptop',
     N'IT Equipment', N'Laptop', N'HP', N'EliteBook 840 G10', N'HP840-0005',
     N'FIN', N'EMP-005', N'HQ - Floor 1', 2,
     CAST(N'2024-09-14' AS DATE), 41200.00, CAST(N'2027-09-14' AS DATE)),
 
    (N'IT-LAP-0006', N'Lenovo ThinkPad T14',  N'HR laptop, in for repair',
     N'IT Equipment', N'Laptop', N'Lenovo', N'ThinkPad T14', N'LN-T14-0006',
     N'HR', NULL, N'HQ - Server Room', 3,
     CAST(N'2023-11-05' AS DATE), 36400.00, CAST(N'2026-11-05' AS DATE)),
 
    -- Ahmed also holds a phone and a monitor -> R4 question 4 returns 3 rows
    (N'IT-PHN-0001', N'Samsung Galaxy A55',   N'Presales company handset',
     N'IT Equipment', N'Mobile Phone', N'Samsung', N'Galaxy A55', N'SG-A55-0001',
     N'PRE', N'EMP-001', N'HQ - Floor 2', 2,
     CAST(N'2025-02-18' AS DATE), 12300.00, CAST(N'2027-02-18' AS DATE)),
 
    (N'PR-MON-0001', N'Dell U2723QE 27"',     N'4K monitor',
     N'Peripherals', N'Monitor', N'Dell', N'U2723QE', N'DU2723-0001',
     N'PRE', N'EMP-001', N'HQ - Floor 2', 2,
     CAST(N'2024-04-01' AS DATE), 18600.00, CAST(N'2027-04-01' AS DATE)),
 
    -- Available stock -> R4 question 2
    (N'PR-MON-0002', N'Dell P2422H 24"',      N'Spare monitor',
     N'Peripherals', N'Monitor', N'Dell', N'P2422H', N'DP2422-0002',
     NULL, NULL, N'Alexandria Warehouse', 1,
     CAST(N'2025-05-09' AS DATE), 7450.00, CAST(N'2028-05-09' AS DATE)),
 
    (N'PR-DCK-0001', N'Dell WD19S Dock',      N'USB-C docking station',
     N'Peripherals', N'Docking Station', N'Dell', N'WD19S', N'DW19S-0001',
     NULL, NULL, N'Alexandria Warehouse', 1,
     CAST(N'2024-08-22' AS DATE), 5900.00, CAST(N'2027-08-22' AS DATE)),
 
    (N'OF-CHR-0001', N'Ergonomic Task Chair', N'Mesh back, adjustable arms',
     N'Office Furniture', N'Chair', N'Herman Miller', N'Aeron', NULL,
     N'OPS', N'EMP-006', N'HQ - Floor 1', 2,
     CAST(N'2023-02-14' AS DATE), 22000.00, NULL),
 
    (N'OF-DSK-0001', N'Sit-Stand Desk 160cm', N'Electric height adjustment',
     N'Office Furniture', N'Desk', N'IKEA', N'Bekant', NULL,
     NULL, NULL, N'HQ - Floor 1', 1,
     CAST(N'2023-02-14' AS DATE), 9800.00, NULL),
 
    -- A retired asset. Status = 4 (Retired) IS the soft delete (R2.5):
    -- there is no separate flag. Note it keeps its history and its last
    -- known location, and the service refuses to transfer it (R3.4).
    (N'IT-DSK-0001', N'Dell OptiPlex 7080',   N'Replaced by laptop rollout',
     N'IT Equipment', N'Desktop', N'Dell', N'OptiPlex 7080', N'DO7080-0001',
     NULL, NULL, N'Alexandria Warehouse', 4,
     CAST(N'2019-07-30' AS DATE), 27600.00, CAST(N'2022-07-30' AS DATE)),
 
    (N'VH-VAN-0001', N'Toyota HiAce',         N'Delivery van',
     N'Vehicles', N'Van', N'Toyota', N'HiAce', N'TH-VAN-0001',
     N'OPS', NULL, N'Alexandria Warehouse', 1,
     CAST(N'2022-10-03' AS DATE), 985000.00, CAST(N'2025-10-03' AS DATE))
) AS v([code], [name], [descr], [categoryName], [typeName], [mfr], [model],
       [serial], [deptCode], [empCode], [locName], [status], [purchDate],
       [cost], [warranty])
JOIN      [dbo].[Categories]  c ON c.[CategoryName] = v.[categoryName]
JOIN      [dbo].[AssetTypes]  t ON t.[TypeName]     = v.[typeName]
LEFT JOIN [dbo].[Departments] d ON d.[Code]         = v.[deptCode]
LEFT JOIN [dbo].[Employees]   e ON e.[EmployeeCode] = v.[empCode]
LEFT JOIN [dbo].[Locations]   l ON l.[LocationName] = v.[locName]
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Assets] a WHERE a.[AssetCode] = v.[code]);
GO



/* ---------------------------------------------------------------------
   Verification
   --------------------------------------------------------------------- */
SELECT N'AspNetRoles' AS [table], COUNT(*) AS [rows] FROM [dbo].[AspNetRoles]
UNION ALL SELECT N'Categories',  COUNT(*) FROM [dbo].[Categories]
UNION ALL SELECT N'AssetTypes',  COUNT(*) FROM [dbo].[AssetTypes]
UNION ALL SELECT N'Departments', COUNT(*) FROM [dbo].[Departments]
UNION ALL SELECT N'Locations',   COUNT(*) FROM [dbo].[Locations]
UNION ALL SELECT N'Employees',   COUNT(*) FROM [dbo].[Employees];
GO
