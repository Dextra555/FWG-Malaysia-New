-- ============================================================
-- OBMS Approval System Bug Fix Migration
-- File: 15.ApprovalSystem_BugFix_Migration.sql
--
-- IDEMPOTENT — safe to re-run on any environment.
--
-- Fixes:
--   BUG-D: VW_PendingApprovals missing BRANCH_STOCK_REQUEST.
--          The original view only unioned PR + PO.  The BSR
--          (StockRequest) table was added in migration 09 and
--          the backend EF query already covers it — this fix
--          aligns the view for any direct SQL consumers /
--          reports / future use.
--
--   BUG-B: InventoryRepository.ApprovePO (legacy path) was
--          missing the self-approval guard that
--          ApprovePOWithHistory already has.
--          Fixed in C# (InventoryRepository.cs) — no DB change.
--
--   BUG-A: login.component.ts successLogin() did not stamp
--          sessionStorage 'userrole'.  Fixed in Angular —
--          no DB change.
--
--   BUG-E: _docTypeDisplayNames dictionary and GetApprovalRoutings
--          ternary chain were missing BRANCH_STOCK_REQUEST.
--          Fixed in C# (ApprovalRepository.cs) — no DB change.
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Approval System Bug Fix Migration';
PRINT 'Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- FIX BUG-D : Recreate VW_PendingApprovals to include BSR
-- The view is dropped and recreated (idempotent).
-- ─────────────────────────────────────────────────────────────

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'VW_PendingApprovals')
    DROP VIEW VW_PendingApprovals;
GO

CREATE VIEW VW_PendingApprovals AS
-- Purchase Requisitions pending approval (Status = 'Submitted')
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
  AND po.IsActive = 1

UNION ALL

-- BUG-D FIX: Branch Stock Requests pending approval (Status = 'Submitted').
-- This leg was entirely missing from previous versions of the view.
-- The backend EF query (ApprovalRepository.GetPendingApprovals) already
-- covered BSRs correctly — this aligns the view for reporting / direct SQL use.
SELECT
    'BRANCH_STOCK_REQUEST'  AS DocumentType,
    'Branch Stock Request'  AS DocumentTypeDisplay,
    sr.ID                   AS DocumentID,
    sr.RequestNo            AS DocumentNumber,
    CONVERT(DATE, sr.RequestDate) AS DocumentDate,
    ISNULL(sr.SubmittedBy, sr.LastUpdatedBy) AS RequestedBy,
    sr.SubmittedDate        AS SubmittedDate,
    sr.Status               AS CurrentStatus,
    NULL                    AS WarehouseName,
    br.Name                 AS BranchName,
    NULL                    AS TotalAmount
FROM StockRequest sr
LEFT JOIN BranchMaster br ON sr.RequestingBranchID = br.ID
WHERE sr.Status = 'Submitted'
  AND sr.IsActive = 1;
GO

PRINT 'VW_PendingApprovals: recreated — now includes PR, PO, and BSR.';
GO

-- ─────────────────────────────────────────────────────────────
-- VERIFICATION QUERIES  (informational — not required for fix)
-- ─────────────────────────────────────────────────────────────

-- Count pending documents by type
SELECT DocumentType, COUNT(*) AS PendingCount
FROM VW_PendingApprovals
GROUP BY DocumentType
ORDER BY DocumentType;

-- Full list of pending documents
SELECT DocumentType, DocumentNumber, RequestedBy, SubmittedDate, CurrentStatus
FROM VW_PendingApprovals
ORDER BY SubmittedDate;

-- Verify ApprovalConfiguration has all 7 document types
SELECT DocumentType, DisplayName, ApprovalRequired, IsActive
FROM ApprovalConfiguration
ORDER BY DocumentType;

PRINT '============================================================';
PRINT 'OBMS Approval System Bug Fix Migration completed.';
PRINT '';
PRINT 'CODE FIXES ALSO APPLIED (no DB action required):';
PRINT '  BUG-A: login.component.ts successLogin() now always sets userrole.';
PRINT '  BUG-B: InventoryRepository.ApprovePO self-approval guard added.';
PRINT '  BUG-E: ApprovalRepository BRANCH_STOCK_REQUEST display name added.';
PRINT '============================================================';
GO
