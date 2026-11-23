-- =====================================================================
-- 08_DropOldUniqueNameConstraints.sql
--
-- Why this script exists:
--   01_Schema.sql created the English names as UNIQUE CONSTRAINTS:
--       UQ_Categories_CategoryName, UQ_AssetTypes_TypeName, UQ_Locations_LocationName
--   03_UpdateLookupTables_ActiveUniqueIndexes.sql tried to remove them with DROP INDEX,
--   but SQL Server refuses DROP INDEX on an index that belongs to a constraint (error 3723),
--   and one name in 03 was wrong (UQ_AssetType_AssetTypeName instead of UQ_AssetTypes_TypeName).
--
--   If the old constraints are still there, they check ALL rows (active and inactive),
--   so after deactivating "Laptop" you still cannot create a new "Laptop",
--   even though the new filtered index UX_AssetTypes_TypeName_Active allows it.
--
-- Step 1 - check first (run this SELECT alone). No rows = nothing to fix, skip this script.
--   SELECT name, OBJECT_NAME(parent_object_id) AS [Table]
--   FROM sys.key_constraints
--   WHERE name IN (N'UQ_Categories_CategoryName', N'UQ_AssetTypes_TypeName', N'UQ_Locations_LocationName');
--
-- Step 2 - run this whole script. Safe to run more than once: each DROP runs only if the constraint exists.
-- =====================================================================
SET NOCOUNT ON;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Categories_CategoryName')
    ALTER TABLE dbo.Categories DROP CONSTRAINT UQ_Categories_CategoryName;

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_AssetTypes_TypeName')
    ALTER TABLE dbo.AssetTypes DROP CONSTRAINT UQ_AssetTypes_TypeName;

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Locations_LocationName')
    ALTER TABLE dbo.Locations DROP CONSTRAINT UQ_Locations_LocationName;
GO

-- The filtered indexes from script 03 must exist; otherwise duplicate ACTIVE names would be allowed.
-- Expect 4 rows here.
SELECT name, OBJECT_NAME(object_id) AS [Table], filter_definition
FROM sys.indexes
WHERE name IN (N'UX_Categories_CategoryName_Active', N'UX_AssetTypes_TypeName_Active',
               N'UX_Departments_DepartmentName_Active', N'UX_Locations_LocationName_Active');
GO
