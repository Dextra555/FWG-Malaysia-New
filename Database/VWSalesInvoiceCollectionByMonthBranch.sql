-- ============================================================
-- View: VWSalesInvoiceCollectionByMonthBranch
-- Purpose: Aggregates invoice amounts and actual collections
--          (from receipts) grouped by Branch and Invoice Month.
-- Usage  : Used by Sales Invoice Collection Report.
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'VWSalesInvoiceCollectionByMonthBranch')
    DROP VIEW VWSalesInvoiceCollectionByMonthBranch;
GO

CREATE VIEW VWSalesInvoiceCollectionByMonthBranch AS
SELECT
    ci.Branch,
    bm.Name                                                          AS BranchName,
    YEAR(ci.InvoiceDate)                                             AS InvoiceYear,
    MONTH(ci.InvoiceDate)                                            AS InvoiceMonth,
    COUNT(DISTINCT ci.ID)                                            AS InvoiceCount,
    SUM(ci.ServiceCharges - ISNULL(ci.Discount, 0) + ISNULL(ci.TaxAmount, 0))
                                                                     AS TotalInvoiceAmount,
    ISNULL(SUM(rd.CollectedAmount), 0)                               AS TotalCollected,
    SUM(ci.ServiceCharges - ISNULL(ci.Discount, 0) + ISNULL(ci.TaxAmount, 0))
        - ISNULL(SUM(rd.CollectedAmount), 0)                         AS OutstandingAmount
FROM ClientInvoice ci
INNER JOIN BranchMaster bm
    ON bm.Code = ci.Branch
LEFT JOIN (
    -- Sum receipt detail amounts per invoice
    SELECT
        rd.InvoiceID,
        SUM(rd.Amount) AS CollectedAmount
    FROM ReceiptDetails rd
    INNER JOIN Receipts r ON r.ID = rd.ReceiptID
    WHERE ISNULL(r.IsDeleted, 0) = 0
    GROUP BY rd.InvoiceID
) rd ON rd.InvoiceID = ci.ID
WHERE ci.IsDeleted = 'N'
GROUP BY
    ci.Branch,
    bm.Name,
    YEAR(ci.InvoiceDate),
    MONTH(ci.InvoiceDate);
GO

-- Quick test query after creating the view:
-- SELECT * FROM VWSalesInvoiceCollectionByMonthBranch ORDER BY InvoiceYear, InvoiceMonth, Branch
