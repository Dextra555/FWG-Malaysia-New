-- ============================================================
-- OBMS Approval Routing Bug Fix Migration
--
-- IDEMPOTENT — safe to re-run.
--
-- Fixes:
--   1. VW_PendingApprovals: RequestedBy was using LastUpdatedBy
--      instead of SubmittedBy. Fixed to use SubmittedBy (falling
--      back to LastUpdatedBy for legacy records that pre-date
--      Part 7 and have no SubmittedBy value yet).
--
-- Root Cause:
--   The original VW_PendingApprovals view used LastUpdatedBy as
--   RequestedBy. If the PR/PO was edited by a different user
--   before submission, the RequestedBy shown in reports was wrong.
--   The backend EF query (ApprovalRepository.GetPendingApprovals)
--   always used SubmittedBy correctly — this fix aligns the view
--   with the backend behaviour.
--
-- Note on case-sensitivity:
--   The backend was also updated (ApprovalRepository.cs) to use
--   a case-insensitive in-memory comparison when matching
--   SubmittedBy against ApprovalRouting.RequesterUser. This
--   prevents routing failures when users log in with different
--   casing than their stored OBMSUsers.Name.
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Approval Routing Bug Fix Migration';
PRINT 'Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- FIX 1 : Recreate VW_PendingApprovals with SubmittedBy
-- The view is dropped and recreated because ALTER VIEW requires
-- the full definition — same effect as CREATE OR ALTER.
-- ─────────────────────────────────────────────────────────────

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'VW_PendingApprovals')
    DROP VIEW VW_PendingApprovals;
GO

CREATE VIEW VW_PendingApprovals AS
-- Purchase Requisitions pending approval (Status = 'Submitted')
-- FIX: use SubmittedBy (the JWT user who submitted) as RequestedBy,
--      not LastUpdatedBy (which may be a different user who edited the draft).
SELECT
    'PURCHASE_REQUISITION'  AS DocumentType,
    'Purchase Requisition'  AS DocumentTypeDisplay,
    pr.ID                   AS DocumentID,
    pr.PRNumber             AS DocumentNumber,
    CONVERT(DATE, pr.PRDate) AS DocumentDate,
    ISNULL(pr.SubmittedBy, pr.LastUpdatedBy) AS RequestedBy,
    pr.SubmittedDate        AS SubmittedDate,
    pr.Status               AS CurrentStatus,
    NULL                    AS WarehouseName,
    NULL                    AS BranchName,
    NULL                    AS TotalAmount
FROM PurchaseRequisition pr
WHERE pr.Status = 'Submitted'
  AND pr.IsActive = 1

UNION ALL

-- Purchase Orders pending approval (Status = 'Submitted')
-- FIX: use SubmittedBy (the JWT user who submitted) as RequestedBy,
--      not LastUpdatedBy (which may be a different user who edited the draft).
SELECT
    'PURCHASE_ORDER'        AS DocumentType,
    'Purchase Order'        AS DocumentTypeDisplay,
    po.ID                   AS DocumentID,
    po.PONumber             AS DocumentNumber,
    CONVERT(DATE, po.PODate) AS DocumentDate,
    ISNULL(po.SubmittedBy, po.LastUpdatedBy) AS RequestedBy,
    po.SubmittedDate        AS SubmittedDate,
    po.Status               AS CurrentStatus,
    wh.Name                 AS WarehouseName,
    NULL                    AS BranchName,
    po.TotalAmount          AS TotalAmount
FROM PurchaseOrder po
LEFT JOIN WarehouseMaster wh ON po.WarehouseID = wh.ID
WHERE po.Status = 'Submitted'
  AND po.IsActive = 1;
GO

PRINT 'VW_PendingApprovals: recreated with SubmittedBy as RequestedBy.';
GO

-- ─────────────────────────────────────────────────────────────
-- VERIFICATION QUERIES  (informational — not required for fix)
-- Run these after applying the migration to confirm the data
-- looks correct before testing the UI.
-- ─────────────────────────────────────────────────────────────

-- Check ApprovalRouting table
SELECT
    ID,
    DocumentType,
    RequesterUser,
    ApproverUser,
    IsActive,
    LASTUPDATE
FROM ApprovalRouting
ORDER BY DocumentType, RequesterUser;

-- Check submitted PRs and whether SubmittedBy is populated
SELECT
    ID,
    PRNumber,
    Status,
    SubmittedBy,
    SubmittedDate,
    LastUpdatedBy
FROM PurchaseRequisition
WHERE Status = 'Submitted'
  AND IsActive = 1
ORDER BY SubmittedDate DESC;

-- Check submitted POs and whether SubmittedBy is populated
SELECT
    ID,
    PONumber,
    Status,
    SubmittedBy,
    SubmittedDate,
    LastUpdatedBy
FROM PurchaseOrder
WHERE Status = 'Submitted'
  AND IsActive = 1
ORDER BY SubmittedDate DESC;

-- Cross-check: does ApprovalRouting.RequesterUser match PR.SubmittedBy?
-- (Case-insensitive — SQL Server CI collation)
SELECT
    pr.PRNumber,
    pr.SubmittedBy,
    ar.RequesterUser    AS RoutingRequester,
    ar.ApproverUser     AS RoutingApprover,
    ar.IsActive         AS RoutingActive,
    CASE
        WHEN pr.SubmittedBy COLLATE SQL_Latin1_General_CP1_CI_AS
           = ar.RequesterUser COLLATE SQL_Latin1_General_CP1_CI_AS
        THEN 'MATCH'
        ELSE 'MISMATCH'
    END                 AS CaseInsensitiveMatch
FROM PurchaseRequisition pr
LEFT JOIN ApprovalRouting ar
    ON ar.DocumentType = 'PURCHASE_REQUISITION'
    AND ar.RequesterUser COLLATE SQL_Latin1_General_CP1_CI_AS
      = pr.SubmittedBy   COLLATE SQL_Latin1_General_CP1_CI_AS
    AND ar.IsActive = 1
WHERE pr.Status = 'Submitted'
  AND pr.IsActive = 1;

PRINT '============================================================';
PRINT 'OBMS Approval Routing Bug Fix Migration completed.';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '  1. Run this script on your database.';
PRINT '  2. Deploy the updated API (ApprovalRepository.cs fix).';
PRINT '  3. Test the approval flow end-to-end.';
PRINT '============================================================';
GO
