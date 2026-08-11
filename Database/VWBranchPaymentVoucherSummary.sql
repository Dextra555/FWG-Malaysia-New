-- ============================================================
-- View: VWBranchPaymentVoucherSummary
-- Purpose: Groups all payment transactions by VoucherNo and
--          calculates accumulated totals per voucher per branch.
-- Usage  : Used by Branch Payment Summary report.
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'VWBranchPaymentVoucherSummary')
    DROP VIEW VWBranchPaymentVoucherSummary;
GO

CREATE VIEW VWBranchPaymentVoucherSummary AS
SELECT
    bp.VoucherNo,
    bpd.Branch,
    bp.PaymentDate,
    bp.CreditorType,
    bp.PaymentType,
    bp.PaymentPurpose,
    bp.PaymentTo,
    bp.Particulars,
    bp.ItemCategory,
    COUNT(*)            AS TransactionCount,
    SUM(bpd.Amount)     AS TotalAmount,
    MIN(bp.PaymentDate) AS FirstPaymentDate,
    MAX(bp.PaymentDate) AS LastPaymentDate
FROM BranchPayments bp
INNER JOIN BranchPaymentDetails bpd ON bp.ID = bpd.PaymentID
WHERE bp.IsDeleted = 0
  AND ISNULL(bpd.IsDeleted, 0) = 0
GROUP BY
    bp.VoucherNo,
    bpd.Branch,
    bp.PaymentDate,
    bp.CreditorType,
    bp.PaymentType,
    bp.PaymentPurpose,
    bp.PaymentTo,
    bp.Particulars,
    bp.ItemCategory;
GO

-- Quick test query after creating the view:
-- SELECT * FROM VWBranchPaymentVoucherSummary ORDER BY PaymentDate DESC

-- ============================================================
-- Crystal Report Command SQL: BranchPaymentSummary.rpt
-- Parameters: {?StartDate}, {?EndDate}, {?BankId}  (Number)
--             {?BankId} = 0 means All Banks (no filter)
-- ============================================================

SELECT
    CAST(bp.VoucherNo AS INT)                    AS VoucherNoSort,
    CONVERT(VARCHAR(10), bp.PaymentDate, 103)    AS PaymentDate,
    bp.PaymentTo,
    bp.Particulars,
    CASE bp.PaymentType
        WHEN 1 THEN 'Cheque'
        WHEN 2 THEN 'Cash'
        WHEN 3 THEN 'Contra'
        WHEN 4 THEN 'Online Fund Transfer'
        ELSE 'Other'
    END                                          AS PaymentTypeName,
    ISNULL(bm.Accname, '')                       AS BankName,
    ISNULL(bp.ChequeNo, '')                      AS ChequeNo,
    COUNT(bpd.ID)                                AS TransactionCount,
    SUM(bpd.Amount)                              AS TotalAmount
FROM BranchPayments bp
INNER JOIN BranchPaymentDetails bpd ON bpd.PaymentID = bp.ID
LEFT  JOIN BankMaster bm             ON bm.BankId    = bp.BankID
WHERE bp.PaymentDate BETWEEN {?StartDate} AND {?EndDate}
  AND bp.IsDeleted = 0
  AND ISNULL(bpd.IsDeleted, 0) = 0
  AND ({?BankId} = 0 OR bp.BankID = {?BankId})
GROUP BY
    bp.ID,
    bp.VoucherNo,
    bp.PaymentDate,
    bp.PaymentTo,
    bp.Particulars,
    bp.PaymentType,
    bm.Accname,
    bp.ChequeNo
ORDER BY VoucherNoSort
