IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Categories_CategoryNameAr_Active'
)
    CREATE UNIQUE INDEX UX_Categories_CategoryNameAr_Active
    ON dbo.Categories(CategoryNameAr)
    WHERE IsActive = 1 AND CategoryNameAr IS NOT NULL;


IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_AssetTypes_TypeNameAr_Active'
)
    CREATE UNIQUE INDEX UX_AssetTypes_TypeNameAr_Active
    ON dbo.AssetTypes(TypeNameAr)
    WHERE IsActive = 1 AND TypeNameAr IS NOT NULL;


IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Departments_DepartmentNameAr_Active'
)
    CREATE UNIQUE INDEX UX_Departments_DepartmentNameAr_Active
    ON dbo.Departments(DepartmentNameAr)
    WHERE IsActive = 1 AND DepartmentNameAr IS NOT NULL;


IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Locations_LocationNameAr_Active'
)
    CREATE UNIQUE INDEX UX_Locations_LocationNameAr_Active
    ON dbo.Locations(LocationNameAr)
    WHERE IsActive = 1 AND LocationNameAr IS NOT NULL;
GO