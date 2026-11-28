-- ============================================================
-- Migration 65: Create sp_StaffItemHoldingReport
--
-- Used by: UatReports-new/UatReports/Inventory/StaffItemHoldingReport.aspx
-- Purpose: Returns net items currently held (issued - returned) per
--          employee × item × client, only where NetHolding > 0.
--
-- Parameters:
--   @IssueToType VARCHAR(20)  -- 'ALL' | 'Staff' | 'Guard' | 'Other Guards'
--   @EmployeeID  INT          -- 0 = All employees
--
-- IDEMPOTENT: DROP + CREATE. Safe to re-run.
-- Target DB  : OBMSUATNEW
-- ============================================================

PRINT 'Migration 65: Creating sp_StaffItemHoldingReport...';

IF OBJECT_ID('dbo.sp_StaffItemHoldingReport', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_StaffItemHoldingReport;
    PRINT 'sp_StaffItemHoldingReport: dropped existing.';
END
GO

CREATE PROCEDURE dbo.sp_StaffItemHoldingReport
    @IssueToType VARCHAR(20) = 'ALL',
    @EmployeeID  INT         = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Step 1: Aggregate issued qty per employee x item x client
    ;WITH Issued AS (
        SELECT
            si.IssueToID                            AS EmployeeID,
            ISNULL(si.IssueToName, '')              AS EmployeeName,
            si.IssueToType,
            sd.ItemID,
            ISNULL(sd.ClientID, 0)                  AS ClientID,
            SUM(sd.IssueQty)                        AS TotalIssued,
            MAX(ISNULL(sd.UnitCost, 0))             AS UnitCost,
            MAX(si.IssueDate)                       AS LastIssueDate
        FROM  StockIssueDoc        si
        INNER JOIN StockIssueDocDetail sd ON sd.IssueDocID = si.ID
        WHERE si.IsActive    = 1
          AND si.IssueToType IN ('Staff', 'Guard', 'Other Guards')
          AND (@IssueToType = 'ALL' OR si.IssueToType = @IssueToType)
          AND (@EmployeeID  = 0     OR si.IssueToID   = @EmployeeID)
        GROUP BY
            si.IssueToID, si.IssueToName, si.IssueToType,
            sd.ItemID, sd.ClientID
    ),
    -- Step 2: Aggregate returned qty per employee x item x client
    Returned AS (
        SELECT
            si2.IssueToID                           AS EmployeeID,
            sd2.ItemID,
            ISNULL(sd2.ClientID, 0)                 AS ClientID,
            SUM(rd.ReturnQty)                       AS TotalReturned
        FROM  StockIssueReturnDetail  rd
        INNER JOIN StockIssueDocDetail sd2 ON sd2.ID  = rd.OriginalIssueDetailID
        INNER JOIN StockIssueDoc       si2 ON si2.ID  = sd2.IssueDocID
        WHERE si2.IsActive    = 1
          AND si2.IssueToType IN ('Staff', 'Guard', 'Other Guards')
          AND (@IssueToType = 'ALL' OR si2.IssueToType = @IssueToType)
          AND (@EmployeeID  = 0     OR si2.IssueToID   = @EmployeeID)
        GROUP BY
            si2.IssueToID, sd2.ItemID, sd2.ClientID
    )
    SELECT
        iss.EmployeeName,
        iss.EmployeeID,
        iss.IssueToType,
        ISNULL(cl.Name, '')                         AS ClientName,
        ISNULL(im.Code,  '')                        AS ItemCode,
        im.Name                                     AS ItemName,
        ISNULL(ic.Name,  '')                        AS CategoryName,
        ISNULL(um.ShortName, '')                    AS UOMName,
        iss.TotalIssued,
        ISNULL(ret.TotalReturned, 0)                AS TotalReturned,
        iss.TotalIssued - ISNULL(ret.TotalReturned, 0)   AS NetHolding,
        iss.UnitCost,
        (iss.TotalIssued - ISNULL(ret.TotalReturned, 0))
            * iss.UnitCost                          AS TotalValue,
        iss.LastIssueDate
    FROM  Issued iss
    INNER JOIN ItemMaster        im  ON im.ID  = iss.ItemID
    LEFT  JOIN InventoryCategory ic  ON ic.ID  = im.CategoryID
    LEFT  JOIN UOMMaster         um  ON um.ID  = im.UOMID
    LEFT  JOIN ClientMaster      cl  ON cl.ID  = iss.ClientID
    LEFT  JOIN Returned          ret ON ret.EmployeeID = iss.EmployeeID
                                    AND ret.ItemID     = iss.ItemID
                                    AND ret.ClientID   = iss.ClientID
    WHERE (iss.TotalIssued - ISNULL(ret.TotalReturned, 0)) > 0
    ORDER BY
        iss.EmployeeName,
        im.Name;
END
GO

PRINT 'Migration 65: sp_StaffItemHoldingReport created successfully.';
