
DROP INDEX [UQ_Locations_LocationName] ON [dbo].[Locations];
DROP INDEX [UQ_Categories_CategoryName] ON [dbo].[Categories];
DROP INDEX [UQ_AssetType_AssetTypeName] ON [dbo].[AssetTypes];


-- Locations
CREATE UNIQUE INDEX [UX_Locations_LocationName_Active]
ON [dbo].[Locations] ([LocationName])
WHERE [IsActive] = 1;

-- Departments
CREATE UNIQUE INDEX [UX_Departments_DepartmentName_Active]
ON [dbo].[Departments] ([DepartmentName])
WHERE [IsActive] = 1;

-- Categories
CREATE UNIQUE INDEX [UX_Categories_CategoryName_Active]
ON [dbo].[Categories] ([CategoryName])
WHERE [IsActive] = 1;

-- Asset Types
CREATE UNIQUE INDEX [UX_AssetTypes_TypeName_Active]
ON [dbo].[AssetTypes] ([TypeName])
WHERE [IsActive] = 1;
GO