/* =====================================================================
   04 — Sync asset departments with their assigned employee (DATA FIX)

   Why:
     Assets has its own DepartmentId column. Until now, changing an
     employee's department (Lookup data > Employees > Edit) did NOT move
     the assets assigned to that employee, so the asset list kept showing
     the old department.

     The API is fixed (EmployeeCommandHandler now moves the assets and
     writes a transfer-history row for each one). This script repairs the
     rows that are ALREADY wrong in the database.

   What it does (one transaction — all or nothing):
     1. Writes one AssetTransfers row per asset it moves (same history row
        the API writes), so the audit trail stays complete.
     2. Sets Assets.DepartmentId = the assigned employee's DepartmentId.

   Scope:
     - Only assets that have an assigned employee.
     - Only assets that are NOT retired (Status 4 = Retired; a retired
       asset's record is history and must not change).
     - Only rows where the two departments are different.

   No schema change: no table, column or constraint is added, so there
   is nothing to re-scaffold in EF Core.

   Safe to run more than once: the second run finds nothing to fix.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;   -- any error rolls the whole transaction back

BEGIN TRANSACTION;

-- Preview: the rows that will be fixed (shown in the Results tab).
SELECT  a.Id            AS AssetId,
        a.AssetCode,
        e.FullName      AS AssignedEmployee,
        a.DepartmentId  AS CurrentAssetDepartmentId,
        e.DepartmentId  AS EmployeeDepartmentId
FROM    dbo.Assets    AS a
JOIN    dbo.Employees AS e ON e.Id = a.AssignedEmployeeId
WHERE   a.Status <> 4
  AND   (a.DepartmentId IS NULL OR a.DepartmentId <> e.DepartmentId);

-- TransferredByUserId is NOT NULL and points to AspNetUsers, so the fix is
-- recorded as done by the first active Admin account.
DECLARE @AdminUserId NVARCHAR(450) =
(
    SELECT TOP (1) u.Id
    FROM   dbo.AspNetUsers     AS u
    JOIN   dbo.AspNetUserRoles AS ur ON ur.UserId = u.Id
    JOIN   dbo.AspNetRoles     AS r  ON r.Id = ur.RoleId
    WHERE  r.NormalizedName = N'ADMIN'
      AND  u.IsActive = 1
    ORDER BY u.CreatedAt
);

IF @AdminUserId IS NULL
BEGIN
    RAISERROR(N'No active Admin user was found, so the transfer history cannot be written. Nothing was changed.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- 1) History first (it needs the OLD department, which step 2 overwrites).
INSERT INTO dbo.AssetTransfers
       (AssetId, FromEmployeeId, ToEmployeeId, FromDepartmentId, ToDepartmentId,
        FromLocationId, ToLocationId, TransferDate, Reason, TransferredByUserId)
SELECT  a.Id,
        a.AssignedEmployeeId, a.AssignedEmployeeId,   -- same person
        a.DepartmentId,       e.DepartmentId,         -- old -> new department
        a.LocationId,         a.LocationId,           -- same place
        SYSUTCDATETIME(),
        N'Moved automatically: the assigned employee changed department.',
        @AdminUserId
FROM    dbo.Assets    AS a
JOIN    dbo.Employees AS e ON e.Id = a.AssignedEmployeeId
WHERE   a.Status <> 4
  AND   (a.DepartmentId IS NULL OR a.DepartmentId <> e.DepartmentId);

-- 2) Move the assets.
UPDATE  a
SET     a.DepartmentId = e.DepartmentId,
        a.UpdatedAt    = SYSUTCDATETIME(),
        a.UpdatedByUserId = @AdminUserId
FROM    dbo.Assets    AS a
JOIN    dbo.Employees AS e ON e.Id = a.AssignedEmployeeId
WHERE   a.Status <> 4
  AND   (a.DepartmentId IS NULL OR a.DepartmentId <> e.DepartmentId);

PRINT CONCAT(@@ROWCOUNT, N' asset(s) moved to their employee''s department.');

COMMIT TRANSACTION;
GO

/* After running it: the API caches asset lists in Redis.
   The next edit in the app refreshes them, or flush Redis once:
       redis-cli FLUSHDB
*/
