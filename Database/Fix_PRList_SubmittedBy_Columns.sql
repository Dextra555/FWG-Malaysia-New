-- ============================================================
-- FIX : GetPRList / GetPOList - "Unable to load" Error
--
-- Root Cause:
--   PurchaseRequisition.SubmittedBy / SubmittedDate columns
--   PurchaseOrder.SubmittedBy / SubmittedDate columns
--   are missing in the database.
--
--   EF Core maps these columns in the domain model, so the
--   SELECT query throws a SqlException → controller catches it
--   → returns { success: false, message: "Unable to load..." }
--
-- Fix:
--   Add the 4 missing columns (IDEMPOTENT — safe to re-run).
--
-- Target: OBMSUATNEW (or live equivalent)
-- ============================================================

PRINT '========================================';
PRINT 'FIX: Adding SubmittedBy/SubmittedDate...';
PRINT '========================================';

-- ─── PurchaseRequisition ─────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='PurchaseRequisition' AND COLUMN_NAME='SubmittedBy')
BEGIN
    ALTER TABLE PurchaseRequisition ADD SubmittedBy NVARCHAR(100) NULL;
    PRINT 'PurchaseRequisition.SubmittedBy  → ADDED';
END
ELSE
    PRINT 'PurchaseRequisition.SubmittedBy  → already exists, skipping.';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='PurchaseRequisition' AND COLUMN_NAME='SubmittedDate')
BEGIN
    ALTER TABLE PurchaseRequisition ADD SubmittedDate DATETIME NULL;
    PRINT 'PurchaseRequisition.SubmittedDate → ADDED';
END
ELSE
    PRINT 'PurchaseRequisition.SubmittedDate → already exists, skipping.';


-- ─── PurchaseOrder ───────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='PurchaseOrder' AND COLUMN_NAME='SubmittedBy')
BEGIN
    ALTER TABLE PurchaseOrder ADD SubmittedBy NVARCHAR(100) NULL;
    PRINT 'PurchaseOrder.SubmittedBy  → ADDED';
END
ELSE
    PRINT 'PurchaseOrder.SubmittedBy  → already exists, skipping.';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='PurchaseOrder' AND COLUMN_NAME='SubmittedDate')
BEGIN
    ALTER TABLE PurchaseOrder ADD SubmittedDate DATETIME NULL;
    PRINT 'PurchaseOrder.SubmittedDate → ADDED';
END
ELSE
    PRINT 'PurchaseOrder.SubmittedDate → already exists, skipping.';


-- ─── Verification ────────────────────────────────────────────

PRINT '';
PRINT '-- Verify columns now exist --';
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('PurchaseRequisition', 'PurchaseOrder')
  AND COLUMN_NAME IN ('SubmittedBy', 'SubmittedDate')
ORDER BY TABLE_NAME, COLUMN_NAME;

PRINT '';
PRINT '========================================';
PRINT 'FIX Complete. Retry GetPRList API now.';
PRINT '========================================';
