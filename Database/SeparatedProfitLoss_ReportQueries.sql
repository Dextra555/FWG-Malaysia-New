-- =============================================================================
-- Crystal Report SQL Queries for SeparatedProfitLossSummary.rpt
--
-- IMPORTANT: Both queries use BranchPayments + BranchPaymentDetails directly.
-- VWSummaryProfitNLoss does NOT expose ItemCategory, PaymentPurpose, IsDeleted.
--
-- How to use in Crystal Reports Designer:
--   1. Database Expert > Add Command  → paste QUERY 1 as "Command"
--   2. Database Expert > Add Command  → paste QUERY 2 as "Command_1" (for Subreport)
--
-- Parameters declared in Crystal Reports:
--   {?StartDate}  DateTime
--   {?EndDate}    DateTime
--   {?Branch}     String   ("0" or "" = all branches)
-- =============================================================================


-- =============================================================================
-- QUERY 1 — Main Report (Command): Operational P&L grouped by month
-- Excludes Contra, BU, Transfer, Internal, Adjustment entries
-- =============================================================================
SELECT
    (CASE
        WHEN MONTH(bp.PaymentDate) = 1  THEN 'JAN'
        WHEN MONTH(bp.PaymentDate) = 2  THEN 'FEB'
        WHEN MONTH(bp.PaymentDate) = 3  THEN 'MAR'
        WHEN MONTH(bp.PaymentDate) = 4  THEN 'APR'
        WHEN MONTH(bp.PaymentDate) = 5  THEN 'MAY'
        WHEN MONTH(bp.PaymentDate) = 6  THEN 'JUN'
        WHEN MONTH(bp.PaymentDate) = 7  THEN 'JULY'
        WHEN MONTH(bp.PaymentDate) = 8  THEN 'AUG'
        WHEN MONTH(bp.PaymentDate) = 9  THEN 'SEPT'
        WHEN MONTH(bp.PaymentDate) = 10 THEN 'OCT'
        WHEN MONTH(bp.PaymentDate) = 11 THEN 'NOV'
        WHEN MONTH(bp.PaymentDate) = 12 THEN 'DEC'
    END)                    AS Month,
    MONTH(bp.PaymentDate)   AS MonthNo,
    bpd.Branch              AS Branch,
    SUM(bpd.Amount)         AS Expenses,
    CAST(0 AS DECIMAL(18,2)) AS Income,
    CAST(0 AS DECIMAL(18,2)) AS CN,
    CAST(0 AS DECIMAL(18,2)) AS Discount,
    0 - SUM(bpd.Amount)     AS Profit
FROM BranchPayments bp
INNER JOIN BranchPaymentDetails bpd ON bpd.PaymentID = bp.ID
WHERE bp.IsDeleted = 0
  AND ISNULL(bpd.IsDeleted, 0) = 0
  AND bp.PaymentDate BETWEEN {?StartDate} AND {?EndDate}
  AND (bp.ItemCategory NOT LIKE '%Contra%'   OR bp.ItemCategory IS NULL)
  AND (bp.ItemCategory NOT LIKE '%BU%'       OR bp.ItemCategory IS NULL)
  AND (bp.ItemCategory NOT LIKE '%Transfer%' OR bp.ItemCategory IS NULL)
  AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Contra%'    OR bp.PaymentPurpose IS NULL)
  AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Transfer%'  OR bp.PaymentPurpose IS NULL)
  AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Internal%'  OR bp.PaymentPurpose IS NULL)
  AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Adjustment%' OR bp.PaymentPurpose IS NULL)
GROUP BY
    MONTH(bp.PaymentDate),
    bpd.Branch
ORDER BY
    MONTH(bp.PaymentDate)


-- =============================================================================
-- QUERY 2 — Subreport (Command): Non-Operational Transactions
-- Only Contra, BU, Transfer, Internal, Adjustment entries
-- =============================================================================
SELECT
    (CASE
        WHEN MONTH(bp.PaymentDate) = 1  THEN 'JAN'
        WHEN MONTH(bp.PaymentDate) = 2  THEN 'FEB'
        WHEN MONTH(bp.PaymentDate) = 3  THEN 'MAR'
        WHEN MONTH(bp.PaymentDate) = 4  THEN 'APR'
        WHEN MONTH(bp.PaymentDate) = 5  THEN 'MAY'
        WHEN MONTH(bp.PaymentDate) = 6  THEN 'JUN'
        WHEN MONTH(bp.PaymentDate) = 7  THEN 'JULY'
        WHEN MONTH(bp.PaymentDate) = 8  THEN 'AUG'
        WHEN MONTH(bp.PaymentDate) = 9  THEN 'SEPT'
        WHEN MONTH(bp.PaymentDate) = 10 THEN 'OCT'
        WHEN MONTH(bp.PaymentDate) = 11 THEN 'NOV'
        WHEN MONTH(bp.PaymentDate) = 12 THEN 'DEC'
    END)                                                AS Month,
    MONTH(bp.PaymentDate)                               AS MonthNo,
    bpd.Branch                                          AS Branch,
    ISNULL(bp.ItemCategory, '')                         AS Category,
    CAST(ISNULL(bp.PaymentPurpose, '') AS NVARCHAR(255)) AS Purpose,
    SUM(bpd.Amount)                                     AS Amount,
    CASE
        WHEN SUM(bpd.Amount) >= 0 THEN 'Debit'
        ELSE 'Credit'
    END                                                 AS TransactionNature
FROM BranchPayments bp
INNER JOIN BranchPaymentDetails bpd ON bpd.PaymentID = bp.ID
WHERE bp.IsDeleted = 0
  AND ISNULL(bpd.IsDeleted, 0) = 0
  AND bp.PaymentDate BETWEEN {?StartDate} AND {?EndDate}
  AND (
        bp.ItemCategory LIKE '%Contra%'
        OR bp.ItemCategory LIKE '%BU%'
        OR bp.ItemCategory LIKE '%Transfer%'
        OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Contra%'
        OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Transfer%'
        OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Internal%'
        OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Adjustment%'
      )
GROUP BY
    MONTH(bp.PaymentDate),
    bpd.Branch,
    ISNULL(bp.ItemCategory, ''),
    CAST(ISNULL(bp.PaymentPurpose, '') AS NVARCHAR(255))
ORDER BY
    MONTH(bp.PaymentDate), bpd.Branch
