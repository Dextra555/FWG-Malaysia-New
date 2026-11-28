-- ============================================================
-- Migration 64: Create sp_StaffIssueRegisterReport
--
-- Used by: UatReports-new/UatReports/Inventory/StaffIssueRegisterReport.aspx
-- Purpose: Returns Staff/Guard issue lines with returned qty, net qty,
--          filtered by date range, issue type, employee, and deduction flag.
--
-- Parameters:
--   @FromDate    DATE
--   @ToDate      DATE
--   @IssueToType VARCHAR(20)  -- 'ALL' | 'Staff' | 'Guard' | 'Other Guards'
--   @EmployeeID  INT          -- 0 = All
--   @IsDeduction TINYINT      -- 2 = ALL, 1 = Deduction only, 0 = Non-deduction only
--
-- IDEMPOTENT: DROP + CREATE. Safe to re-run.
-- Target DB  : OBMSUATNEW
-- ============================================================

PRINT 'Migration 64: Creating sp_StaffIssueRegisterReport...';

IF OBJECT_ID('dbo.sp_StaffIssueRegisterReport', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_StaffIssueRegisterReport;
    PRINT 'sp_StaffIssueRegisterReport: dropped existing.';
END
GO

CREATE PROCEDURE dbo.sp_StaffIssueRegisterReport
    @FromDate    DATE,
    @ToDate      DATE,
    @IssueToType VARCHAR(20) = 'ALL',
    @EmployeeID  INT         = 0,
    @IsDeduction TINYINT     = 2        -- 2=ALL, 1=Deduction, 0=Non-Deduction
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        si.IssueNo,
        si.IssueDate,
        wh.Name                                     AS WarehouseName,
        si.IssueToType,
        si.IssueToName                              AS EmployeeName,
        si.IssueToID                                AS EmployeeID,
        ISNULL(cl.Name, '')                         AS ClientName,
        si.IsDeduction,
        im.Code                                     AS ItemCode,
        im.Name                                     AS ItemName,
        ISNULL(ic.Name, '')                         AS CategoryName,
        ISNULL(um.ShortName, '')                    AS UOMName,
        sd.IssueQty,
        ISNULL(sd.UnitCost, 0)                      AS UnitCost,
        sd.IssueQty * ISNULL(sd.UnitCost, 0)        AS TotalValue,
        ISNULL((
            SELECT SUM(rd.ReturnQty)
            FROM   StockIssueReturnDetail rd
            WHERE  rd.OriginalIssueDetailID = sd.ID
        ), 0)                                       AS ReturnedQty,
        sd.IssueQty - ISNULL((
            SELECT SUM(rd.ReturnQty)
            FROM   StockIssueReturnDetail rd
            WHERE  rd.OriginalIssueDetailID = sd.ID
        ), 0)                                       AS NetQty,
        ISNULL(sd.Remarks, '')                      AS Remarks,
        ISNULL(si.LastUpdatedBy, '')                AS IssuedBy
    FROM  StockIssueDoc        si
    INNER JOIN StockIssueDocDetail sd  ON sd.IssueDocID  = si.ID
    INNER JOIN WarehouseMaster      wh  ON wh.ID           = si.WarehouseID
    INNER JOIN ItemMaster           im  ON im.ID           = sd.ItemID
    LEFT  JOIN InventoryCategory    ic  ON ic.ID           = im.CategoryID
    LEFT  JOIN UOMMaster            um  ON um.ID           = im.UOMID
    LEFT  JOIN ClientMaster         cl  ON cl.ID           = sd.ClientID
    WHERE si.IsActive    = 1
      AND si.IssueToType IN ('Staff', 'Guard', 'Other Guards')
      AND CAST(si.IssueDate AS DATE) BETWEEN @FromDate AND @ToDate
      AND (@IssueToType = 'ALL'  OR si.IssueToType = @IssueToType)
      AND (@EmployeeID  = 0      OR si.IssueToID   = @EmployeeID)
      AND (@IsDeduction = 2
           OR (@IsDeduction = 1 AND si.IsDeduction = 1)
           OR (@IsDeduction = 0 AND si.IsDeduction = 0))
    ORDER BY
        si.IssueToName,
        si.IssueDate,
        im.Name;
END
GO

PRINT 'Migration 64: sp_StaffIssueRegisterReport created successfully.';
