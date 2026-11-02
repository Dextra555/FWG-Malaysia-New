-- ============================================================
-- Migration 43: Branch-wise Asset Report
-- Creates sp_BranchAssetReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report shows all assets grouped by Branch (from
-- AssetAllocation where AllocatedToType = 'Branch').
-- Also shows assets with no allocation (Unallocated).
--
-- Filters: Branch (optional), AssetStatus (optional),
--          AssetType (optional)
--
-- Columns: Branch, Allocation No, Allocation Date,
--          Asset Code, Asset Name, Asset Type, Serial No,
--          Brand/Model, Purchase Date, Purchase Value,
--          Current Status, Condition, Location
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_BranchAssetReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_BranchAssetReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_BranchAssetReport
    @BranchID    INT           = 0,    -- 0 = All Branches (incl. Unallocated)
    @AssetStatus NVARCHAR(30)  = 'ALL', -- ALL | Available | Allocated | Disposed | Damaged | Under_Maintenance
    @AssetType   NVARCHAR(50)  = 'ALL'  -- ALL | Fixed Asset | Liquid Assets | etc.
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Section 1: Allocated assets (AllocatedToType = 'Branch') ──────────
    SELECT
        ISNULL(bm.Name, aa.AllocatedToName)   AS BranchName,
        ISNULL(bm.Code, aa.BranchCode)        AS BranchCode,
        aa.AllocationNo,
        CONVERT(VARCHAR(20), aa.AllocationDate, 106) AS AllocationDate,
        am.AssetCode,
        am.Name                               AS AssetName,
        ISNULL(am.AssetType, '')              AS AssetType,
        ISNULL(am.SerialNumber, '')           AS SerialNumber,
        ISNULL(am.BrandName, '')              AS BrandName,
        ISNULL(am.ModelNo, '')                AS ModelNo,
        CASE WHEN am.PurchaseDate IS NOT NULL
             THEN CONVERT(VARCHAR(20), am.PurchaseDate, 106)
             ELSE ''
        END                                   AS PurchaseDate,
        ISNULL(am.PurchaseAmount, 0)          AS PurchaseValue,
        am.AssetStatus,
        ISNULL(am.AssetCondition, '')         AS AssetCondition,
        ISNULL(am.CurrentLocation, '')        AS CurrentLocation,
        ISNULL(am.Remarks, '')                AS AssetRemarks,
        ISNULL(aa.Remarks, '')                AS AllocationRemarks,
        ISNULL(aa.LastUpdatedBy, '')          AS AllocatedBy

    FROM AssetAllocation aa
        INNER JOIN AssetMaster am ON am.ID = aa.AssetID
        LEFT  JOIN BranchMaster bm ON bm.Code = aa.BranchCode
    WHERE aa.IsActive = 1
      AND am.IsActive = 1
      AND aa.AllocatedToType = 'Branch'
      AND (@BranchID    = 0 OR bm.ID = @BranchID)
      AND (@AssetStatus = 'ALL' OR am.AssetStatus = @AssetStatus)
      AND (@AssetType   = 'ALL' OR am.AssetType   = @AssetType)

    UNION ALL

    -- ── Section 2: Unallocated assets (no active Branch allocation) ────────
    SELECT
        'Unallocated'                          AS BranchName,
        ''                                     AS BranchCode,
        ''                                     AS AllocationNo,
        ''                                     AS AllocationDate,
        am.AssetCode,
        am.Name                                AS AssetName,
        ISNULL(am.AssetType, '')               AS AssetType,
        ISNULL(am.SerialNumber, '')            AS SerialNumber,
        ISNULL(am.BrandName, '')               AS BrandName,
        ISNULL(am.ModelNo, '')                 AS ModelNo,
        CASE WHEN am.PurchaseDate IS NOT NULL
             THEN CONVERT(VARCHAR(20), am.PurchaseDate, 106)
             ELSE ''
        END                                    AS PurchaseDate,
        ISNULL(am.PurchaseAmount, 0)           AS PurchaseValue,
        am.AssetStatus,
        ISNULL(am.AssetCondition, '')          AS AssetCondition,
        ISNULL(am.CurrentLocation, '')         AS CurrentLocation,
        ISNULL(am.Remarks, '')                 AS AssetRemarks,
        ''                                     AS AllocationRemarks,
        ISNULL(am.LastUpdatedBy, '')           AS AllocatedBy

    FROM AssetMaster am
    WHERE am.IsActive = 1
      AND NOT EXISTS (
            SELECT 1 FROM AssetAllocation aa2
            WHERE aa2.AssetID = am.ID
              AND aa2.IsActive = 1
              AND aa2.AllocatedToType = 'Branch'
          )
      AND @BranchID   = 0    -- Only show Unallocated if "All Branches" is selected
      AND (@AssetStatus = 'ALL' OR am.AssetStatus = @AssetStatus)
      AND (@AssetType   = 'ALL' OR am.AssetType   = @AssetType)

    ORDER BY
        BranchName    ASC,
        AssetType     ASC,
        AssetName     ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Branch Asset Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Branch Asset Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 43: sp_BranchAssetReport and ScreenList entry created successfully.';
GO
