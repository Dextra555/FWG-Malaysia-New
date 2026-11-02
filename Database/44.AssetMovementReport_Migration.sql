-- ============================================================
-- Migration 44: Asset Movement / Allocation Report
-- Creates sp_AssetMovementReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report shows ALL asset movement transactions:
--   1. Allocation   — asset allocated to Branch/Employee/Other
--   2. Transfer     — asset moved from one holder to another
--   3. Return       — asset returned back to pool
--
-- Filters: Asset (optional), Movement Type (optional),
--          Date Range (required)
--
-- Columns: Movement Type, Doc No, Date, Asset Code, Asset Name,
--          Asset Type, Serial No, From (Party), To (Party),
--          Condition, Remarks, Prepared By
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_AssetMovementReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AssetMovementReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_AssetMovementReport
    @AssetID      INT          = 0,
    @MovementType NVARCHAR(20) = 'ALL',   -- ALL | ALLOCATION | TRANSFER | RETURN
    @FromDate     DATETIME     = NULL,
    @ToDate       DATETIME     = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = DATEADD(DAY, -30, CAST(GETDATE() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(GETDATE() AS DATE);
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));
    IF @MovementType IS NULL OR @MovementType = '' SET @MovementType = 'ALL';

    -- ── 1. Allocations ────────────────────────────────────────────────────
    SELECT
        'Allocation'                                       AS MovementType,
        'ALLOCATION'                                       AS MovementTypeCode,
        aa.AllocationNo                                    AS DocNo,
        CONVERT(VARCHAR(20), aa.AllocationDate, 106)       AS DocDate,
        CAST(aa.AllocationDate AS DATETIME)                AS DocDateRaw,
        am.AssetCode,
        am.Name                                            AS AssetName,
        ISNULL(am.AssetType, '')                           AS AssetType,
        ISNULL(am.SerialNumber, '')                        AS SerialNumber,
        ISNULL(am.BrandName, '')                           AS BrandName,
        ''                                                 AS FromParty,      -- No from for allocation
        ISNULL(aa.AllocatedToType,'') + ' : '
            + ISNULL(aa.AllocatedToName, '')               AS ToParty,
        ISNULL(aa.BranchCode, '')                          AS BranchCode,
        am.AssetStatus                                     AS CurrentStatus,
        ISNULL(am.AssetCondition, '')                      AS AssetCondition,
        ISNULL(aa.Remarks, '')                             AS Remarks,
        ISNULL(aa.LastUpdatedBy, '')                       AS PreparedBy,
        ISNULL(am.PurchaseAmount, 0)                       AS PurchaseValue

    FROM AssetAllocation aa
        INNER JOIN AssetMaster am ON am.ID = aa.AssetID
    WHERE aa.IsActive = 1
      AND am.IsActive = 1
      AND CAST(aa.AllocationDate AS DATETIME) BETWEEN @FromDate AND @ToDate
      AND (@AssetID = 0 OR aa.AssetID = @AssetID)
      AND (@MovementType IN ('ALL', 'ALLOCATION'))

    UNION ALL

    -- ── 2. Transfers ──────────────────────────────────────────────────────
    SELECT
        'Transfer'                                         AS MovementType,
        'TRANSFER'                                         AS MovementTypeCode,
        at2.TransferNo                                     AS DocNo,
        CONVERT(VARCHAR(20), at2.TransferDate, 106)        AS DocDate,
        CAST(at2.TransferDate AS DATETIME)                 AS DocDateRaw,
        am.AssetCode,
        am.Name                                            AS AssetName,
        ISNULL(am.AssetType, '')                           AS AssetType,
        ISNULL(am.SerialNumber, '')                        AS SerialNumber,
        ISNULL(am.BrandName, '')                           AS BrandName,
        ISNULL(at2.FromType,'') + ' : '
            + ISNULL(at2.FromName, '')                     AS FromParty,
        ISNULL(at2.ToType,'') + ' : '
            + ISNULL(at2.ToName, '')                       AS ToParty,
        ''                                                 AS BranchCode,
        am.AssetStatus                                     AS CurrentStatus,
        ISNULL(am.AssetCondition, '')                      AS AssetCondition,
        ISNULL(at2.Remarks, '')                            AS Remarks,
        ISNULL(at2.LastUpdatedBy, '')                      AS PreparedBy,
        ISNULL(am.PurchaseAmount, 0)                       AS PurchaseValue

    FROM AssetTransfer at2
        INNER JOIN AssetMaster am ON am.ID = at2.AssetID
    WHERE at2.IsActive = 1
      AND am.IsActive = 1
      AND CAST(at2.TransferDate AS DATETIME) BETWEEN @FromDate AND @ToDate
      AND (@AssetID = 0 OR at2.AssetID = @AssetID)
      AND (@MovementType IN ('ALL', 'TRANSFER'))

    UNION ALL

    -- ── 3. Returns ────────────────────────────────────────────────────────
    SELECT
        'Return'                                           AS MovementType,
        'RETURN'                                           AS MovementTypeCode,
        ar.ReturnNo                                        AS DocNo,
        CONVERT(VARCHAR(20), ar.ReturnDate, 106)           AS DocDate,
        CAST(ar.ReturnDate AS DATETIME)                    AS DocDateRaw,
        am.AssetCode,
        am.Name                                            AS AssetName,
        ISNULL(am.AssetType, '')                           AS AssetType,
        ISNULL(am.SerialNumber, '')                        AS SerialNumber,
        ISNULL(am.BrandName, '')                           AS BrandName,
        ISNULL(ar.ReturnedByType,'') + ' : '
            + ISNULL(ar.ReturnedByName, '')                AS FromParty,
        'Pool (Returned)'                                  AS ToParty,
        ''                                                 AS BranchCode,
        am.AssetStatus                                     AS CurrentStatus,
        ISNULL(ar.ReturnCondition, ISNULL(am.AssetCondition,''))
                                                           AS AssetCondition,
        ISNULL(ar.Remarks, '')                             AS Remarks,
        ISNULL(ar.LastUpdatedBy, '')                       AS PreparedBy,
        ISNULL(am.PurchaseAmount, 0)                       AS PurchaseValue

    FROM AssetReturn ar
        INNER JOIN AssetMaster am ON am.ID = ar.AssetID
    WHERE ar.IsActive = 1
      AND am.IsActive = 1
      AND CAST(ar.ReturnDate AS DATETIME) BETWEEN @FromDate AND @ToDate
      AND (@AssetID = 0 OR ar.AssetID = @AssetID)
      AND (@MovementType IN ('ALL', 'RETURN'))

    ORDER BY
        DocDateRaw        ASC,
        MovementTypeCode  ASC,
        DocNo             ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Movement Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Movement Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 44: sp_AssetMovementReport and ScreenList entry created successfully.';
GO
