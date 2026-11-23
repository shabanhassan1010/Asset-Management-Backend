SET NOCOUNT ON;
GO

-- Categories
IF COL_LENGTH(N'dbo.Categories', N'CategoryNameAr') IS NULL
    ALTER TABLE dbo.Categories
    ADD CategoryNameAr NVARCHAR(100) NULL;

-- Asset Types
IF COL_LENGTH(N'dbo.AssetTypes', N'TypeNameAr') IS NULL
    ALTER TABLE dbo.AssetTypes
    ADD TypeNameAr NVARCHAR(100) NULL;

-- Departments
IF COL_LENGTH(N'dbo.Departments', N'DepartmentNameAr') IS NULL
    ALTER TABLE dbo.Departments
    ADD DepartmentNameAr NVARCHAR(150) NULL;

-- Locations
IF COL_LENGTH(N'dbo.Locations', N'LocationNameAr') IS NULL
    ALTER TABLE dbo.Locations
    ADD LocationNameAr NVARCHAR(150) NULL;

-- Employees: keep the column for future Arabic names,
-- but do not populate employee translations now.
IF COL_LENGTH(N'dbo.Employees', N'FullNameAr') IS NULL
    ALTER TABLE dbo.Employees
    ADD FullNameAr NVARCHAR(200) NULL;
GO