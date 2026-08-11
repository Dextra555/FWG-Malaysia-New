-- =============================================================================
-- Separated Profit & Loss Views
-- Purpose : Expose two filtered views over BranchPayments (the source table
--           behind VWSummaryProfitNLoss) to separate:
--           1. VWOperationalProfitNLoss     – Sales/Income & Expenses only
--           2. VWNonOperationalTransactions – Contra, BU, Transfer, Adjustments
--
-- Root cause of previous error:
--   VWSummaryProfitNLoss only exposes Branch, TransactionDate, BranchIncome,
--   BranchCN, BranchDiscount, BranchExpenses, BranchProfit.
--   ItemCategory, PaymentPurpose and IsDeleted exist on BranchPayments, not
--   on the view. Both new views now query BranchPayments directly.
-- =============================================================================

-- ─────────────────────────────────────────────────────────────────────────────
-- Phase 1 Validation: run these first to confirm column names on BranchPayments
-- ─────────────────────────────────────────────────────────────────────────────
/*
-- 1a. Check what columns BranchPayments has
SELECT TOP 0 * FROM BranchPayments;

-- 1b. Spot-check the distinct non-operational category / purpose values
SELECT DISTINCT
    bp.ItemCategory,
    bp.PaymentPurpose,
    COUNT(*) AS TxCount
FROM BranchPayments bp
WHERE bp.IsDeleted = 0
  AND (
        bp.ItemCategory LIKE '%Contra%'   OR bp.ItemCategory LIKE '%BU%'
        OR bp.ItemCategory LIKE '%Transfer%'
        OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Contra%'
        OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Transfer%'
        OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Internal%'
        OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Adjustment%'
      )
GROUP BY bp.ItemCategory, bp.PaymentPurpose
ORDER BY TxCount DESC;

-- 1c. Check which columns VWSummaryProfitNLoss actually exposes (for reference)
SELECT TOP 0 * FROM VWSummaryProfitNLoss;
*/

-- =============================================================================
-- VIEW 1: VWOperationalProfitNLoss
-- Contains only operational Sales/Income and Expenses.
-- Contra, BU transfer, Internal transfer, and Adjustment rows are excluded.
-- Mirrors the column shape of VWSummaryProfitNLoss so existing code still works.
-- =============================================================================
IF OBJECT_ID('VWOperationalProfitNLoss', 'V') IS NOT NULL
    DROP VIEW VWOperationalProfitNLoss;
GO

CREATE VIEW VWOperationalProfitNLoss AS
SELECT
    Branch,
    YEAR(TransactionDate)   AS PaymentYear,
    MONTH(TransactionDate)  AS PaymentMonth,
    CAST(
        DATENAME(MONTH, TransactionDate) + ' '
        + CAST(YEAR(TransactionDate) AS VARCHAR(4))
    AS NVARCHAR(20))                                AS MonthLabel,
    SUM(ServiceCharges + TaxAmount + HQAmount + BranchCollection)           AS BranchIncome,
    SUM(CreditNoteAmount)                                                    AS BranchCN,
    SUM(Discount)                                                            AS BranchDiscount,
    SUM(BranchExpenses)                                                      AS BranchExpenses,
    SUM(ServiceCharges + TaxAmount + HQAmount + BranchCollection
        - Discount - CreditNoteAmount - BranchExpenses)                      AS BranchProfit
FROM (
    -- 1. Invoice Income
    SELECT
        ci.Branch,
        ci.InvoiceDate                  AS TransactionDate,
        ci.ServiceCharges               AS ServiceCharges,
        ci.TaxAmount                    AS TaxAmount,
        0                               AS HQAmount,
        0                               AS BranchCollection,
        0                               AS CreditNoteAmount,
        ci.Discount                     AS Discount,
        0                               AS BranchExpenses
    FROM ClientInvoice ci
    WHERE ci.IsDeleted = 'N'
      AND ci.Branch NOT IN ('0')

    UNION ALL

    -- 2. General Income (Receipts - non invoice adjustment)
    SELECT
        r.Branch,
        r.ReceiptDate                   AS TransactionDate,
        r.ReceiptAmount                 AS ServiceCharges,
        0                               AS TaxAmount,
        0                               AS HQAmount,
        0                               AS BranchCollection,
        r.CreditNoteAmount              AS CreditNoteAmount,
        0                               AS Discount,
        0                               AS BranchExpenses
    FROM Receipts r
    WHERE r.IsDeleted = 0
      AND r.IsInvoiceAdjustment = 0

    UNION ALL

    -- 3. Operational Expenses only (exclude Contra/BU/Transfer/Internal/Adjustment)
    SELECT
        bpd.Branch,
        bp.PaymentDate                  AS TransactionDate,
        0                               AS ServiceCharges,
        0                               AS TaxAmount,
        0                               AS HQAmount,
        0                               AS BranchCollection,
        0                               AS CreditNoteAmount,
        0                               AS Discount,
        bpd.Amount                      AS BranchExpenses
    FROM BranchPayments bp
    INNER JOIN BranchPaymentDetails bpd ON bpd.PaymentID = bp.ID
    WHERE bp.IsDeleted = 0
      AND ISNULL(bpd.IsDeleted, 0) = 0
      AND (bp.ItemCategory NOT LIKE '%Contra%'    OR bp.ItemCategory IS NULL)
      AND (bp.ItemCategory NOT LIKE '%BU%'        OR bp.ItemCategory IS NULL)
      AND (bp.ItemCategory NOT LIKE '%Transfer%'  OR bp.ItemCategory IS NULL)
      AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Contra%'     OR bp.PaymentPurpose IS NULL)
      AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Transfer%'   OR bp.PaymentPurpose IS NULL)
      AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Internal%'   OR bp.PaymentPurpose IS NULL)
      AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Adjustment%' OR bp.PaymentPurpose IS NULL)
) A
GROUP BY
    Branch,
    YEAR(TransactionDate),
    MONTH(TransactionDate),
    DATENAME(MONTH, TransactionDate) + ' ' + CAST(YEAR(TransactionDate) AS VARCHAR(4));
GO

-- =============================================================================
-- VIEW 2: VWNonOperationalTransactions
-- Contains ONLY Contra, BU transfer, Internal transfer, and Adjustment rows.
-- =============================================================================
IF OBJECT_ID('VWNonOperationalTransactions', 'V') IS NOT NULL
    DROP VIEW VWNonOperationalTransactions;
GO

CREATE VIEW VWNonOperationalTransactions AS
SELECT
    bpd.Branch                                               AS Branch,
    bp.PaymentDate                                           AS TransactionDate,
    CAST(bp.ItemCategory AS NVARCHAR(10))                    AS ItemCategoryID,
    ISNULL(ic.Name, CAST(bp.ItemCategory AS NVARCHAR(50)))   AS ItemCategory,
    CAST(ISNULL(bp.PaymentPurpose, '') AS NVARCHAR(MAX))     AS PaymentPurpose,
    bpd.Amount                                               AS Amount,
    CASE
        WHEN bpd.Amount >= 0 THEN 'Debit'
        ELSE 'Credit'
    END                                                      AS TransactionNature
FROM BranchPayments bp
INNER JOIN BranchPaymentDetails bpd ON bpd.PaymentID = bp.ID
LEFT JOIN InventoryCategory ic ON ic.ID = CAST(bp.ItemCategory AS INT)
WHERE bp.IsDeleted = 0
  AND ISNULL(bpd.IsDeleted, 0) = 0
  -- Non-operational category IDs: 1=CONTRA, 32=TERM LOAN, 39=LOAN,
  -- 46=AMOUNT DUE TO DIRECTOR, 54=LOAN REPAYMENT, 61=FIXED DEPOSIT
  AND CAST(bp.ItemCategory AS INT) IN (1, 32, 39, 46, 54, 61);
GO

-- =============================================================================
-- Phase 7 Validation: run after creating views to verify the split is correct
-- =============================================================================
/*
-- 7.1 Row counts
SELECT 'Operational'    AS Section, COUNT(*) AS RowCount FROM VWOperationalProfitNLoss;
SELECT 'NonOperational' AS Section, COUNT(*) AS RowCount FROM VWNonOperationalTransactions;

-- 7.2 Expense comparison for a sample month
DECLARE @S DATE = '2024-01-01', @E DATE = '2024-01-31';

SELECT 'BranchPayments (all)'  AS Source, SUM(bpd.Amount) AS TotalExpenses
FROM BranchPayments bp
INNER JOIN BranchPaymentDetails bpd ON bpd.PaymentID = bp.ID
WHERE bp.IsDeleted = 0 AND ISNULL(bpd.IsDeleted,0) = 0
  AND bp.PaymentDate BETWEEN @S AND @E

UNION ALL

SELECT 'Operational'           AS Source, SUM(BranchExpenses) AS TotalExpenses
FROM VWOperationalProfitNLoss
WHERE TransactionDate BETWEEN @S AND @E

UNION ALL

SELECT 'NonOperational'        AS Source, SUM(Amount) AS TotalExpenses
FROM VWNonOperationalTransactions
WHERE TransactionDate BETWEEN @S AND @E;

-- 7.3 Confirm zero leakage into Operational view
SELECT COUNT(*) AS ShouldBeZero
FROM VWOperationalProfitNLoss op
INNER JOIN BranchPayments bp ON bp.PaymentDate = op.TransactionDate
WHERE bp.ItemCategory LIKE '%Contra%'
   OR bp.ItemCategory LIKE '%BU%'
   OR bp.ItemCategory LIKE '%Transfer%'
   OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Contra%'
   OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Transfer%'
   OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Internal%'
   OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Adjustment%';
*/
