-- ============================================================
-- Report Data Source: SalesInvoiceCollectionSummary.rpt
-- Purpose : Returns one row per Branch + InvoiceMonth so the
--           Crystal Report cross-tab/pivot can display:
--             Rows    = Branches  (NO, BRANCH)
--             Columns = Months    (JAN, FEB, MAR, APR, ...)
--             Values  = InvoiceAmount (ServiceCharges - Discount + Tax)
--           Footer rows (TOTAL, DEBIT NOTE, CREDIT NOTE, G.TOTAL)
--           are calculated inside the Crystal Report groups/summaries.
--
-- Parameters passed from ASPX:
--   {?StartDate}  - first day of the invoice date range
--   {?EndDate}    - last  day of the invoice date range
--   {?Branch}     - Branch code, or '0' for all branches
-- ============================================================

SELECT
    ci.Branch,
    bm.Name                                                                            AS BranchName,
    YEAR(ci.InvoiceDate)                                                               AS InvoiceYear,
    MONTH(ci.InvoiceDate)                                                              AS InvoiceMonth,
    DATENAME(MONTH, ci.InvoiceDate) + ' ' + CAST(YEAR(ci.InvoiceDate) AS VARCHAR(4)) AS MonthLabel,
    COUNT(DISTINCT ci.ID)                                                              AS InvoiceCount,
    SUM(ci.ServiceCharges - ISNULL(ci.Discount, 0) + ISNULL(ci.TaxAmount, 0))         AS InvoiceAmount,
    ISNULL(SUM(rd.CollectedAmount), 0)                                                 AS CollectedAmount,
    SUM(ci.ServiceCharges - ISNULL(ci.Discount, 0) + ISNULL(ci.TaxAmount, 0))
        - ISNULL(SUM(rd.CollectedAmount), 0)                                           AS OutstandingAmount
FROM ClientInvoice ci
INNER JOIN BranchMaster bm
    ON bm.Code = ci.Branch
LEFT JOIN (
    SELECT rd2.InvoiceID,
           SUM(rd2.Amount) AS CollectedAmount
    FROM   ReceiptDetails rd2
    INNER JOIN Receipts r ON r.ID = rd2.ReceiptID
    WHERE  ISNULL(r.IsDeleted, 0) = 0
    GROUP  BY rd2.InvoiceID
) rd ON rd.InvoiceID = ci.ID
WHERE ci.IsDeleted = 'N'
  AND ci.InvoiceDate >= {?StartDate}
  AND ci.InvoiceDate <= {?EndDate}
  AND ({?Branch} = '0' OR ci.Branch = {?Branch})
GROUP BY
    ci.Branch,
    bm.Name,
    YEAR(ci.InvoiceDate),
    MONTH(ci.InvoiceDate),
    DATENAME(MONTH, ci.InvoiceDate) + ' ' + CAST(YEAR(ci.InvoiceDate) AS VARCHAR(4))
ORDER BY
    ci.Branch,
    YEAR(ci.InvoiceDate),
    MONTH(ci.InvoiceDate);

-- ============================================================
-- Crystal Report designer instructions for SalesInvoiceCollectionSummary.rpt
-- ============================================================
-- 1. Create a new Crystal Report in the UatReports/Finance/ folder.
-- 2. Data source: Add as "SQL Command" using the query above.
--    Crystal Report parameters to create (used in WHERE clause):
--      StartDate  = DateTime
--      EndDate    = DateTime
--      Branch     = String
-- 3. Add string parameters for the company header:
--      CompanyName, CompanyAddress1, CompanyAddress2, CompanyAddress3,
--      CompanyAddress4, CompanyRegistration, CompanyPhone, LoginID
-- 4. Report Header section: display company info using the parameters above.
-- 5. Insert Cross-Tab object (Insert > Cross-Tab):
--      Rows      : {Command.Branch} (or BranchName)
--      Columns   : {Command.MonthLabel}  -- sort using InvoiceYear + InvoiceMonth
--      Summary   : Sum({Command.InvoiceAmount})
--    The Cross-Tab automatically produces a Row Grand Total = branch total across all months.
--    It also produces a Column Grand Total = monthly total across all branches (the TOTAL row).
-- 6. Row number (NO column): add a running total or formula =RowNumber() before the branch name.
-- 7. Footer rows below the Cross-Tab:
--      TOTAL       = Column Grand Total (auto from Cross-Tab)
--      DEBIT NOTE  = add a formula or sub-report querying DebitNote / CreditNote tables
--                    for the same date range; show "-" if zero.
--      CREDIT NOTE = same, show as (amount) with parentheses if negative.
--      G.TOTAL     = TOTAL + DEBIT NOTE - CREDIT NOTE
-- 8. Highlight G.TOTAL row background in yellow using Section Expert > Background Color formula.
-- 9. Apply number format #,##0.00 to all InvoiceAmount fields.
-- ============================================================
