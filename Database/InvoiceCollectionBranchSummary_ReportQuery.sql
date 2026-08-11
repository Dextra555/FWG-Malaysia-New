-- ============================================================
-- Report Data Source: InvoiceCollectionBranchSummary.rpt
-- Purpose : One row per Branch showing:
--   BRANCH | Service Charges | Adjustment | Actual Amount | Tax Amount |
--   Invoice Amount | Collection Amount | CN Amount | Balance
--
-- Column Mapping:
--   ServiceCharges   = SUM(ClientInvoice.ServiceCharges)
--   Adjustment       = SUM(ClientInvoice.Discount)           -- Discount = Adjustment
--   ActualAmount     = SUM(ServiceCharges - Discount)        -- before tax
--   TaxAmount        = SUM(ClientInvoice.TaxAmount)
--   InvoiceAmount    = SUM(ServiceCharges - Discount + TaxAmount)
--   CollectionAmount = SUM(ReceiptDetails.Amount) via Receipts (not deleted)
--   CNAmount         = SUM(Receipts.CreditNoteAmount) for receipts in period
--   Balance          = InvoiceAmount - CollectionAmount - CNAmount
--
-- Parameters (Crystal Report):
--   {?StartDate}  DateTime  - start of invoice date range
--   {?EndDate}    DateTime  - end of invoice date range
--   {?Branch}     String    - specific branch code, or '0' / '' for all branches
-- ============================================================

SELECT
    ci.Branch,

    -- Service Charges (raw contract value)
    ISNULL(SUM(ci.ServiceCharges), 0)                                           AS ServiceCharges,

    -- Adjustment = Discount given
    ISNULL(SUM(ci.Discount), 0)                                                 AS Adjustment,

    -- Actual Amount = ServiceCharges minus Discount (before tax)
    ISNULL(SUM(ci.ServiceCharges - ISNULL(ci.Discount, 0)), 0)                  AS ActualAmount,

    -- Tax Amount (SST)
    ISNULL(SUM(ci.TaxAmount), 0)                                                AS TaxAmount,

    -- Invoice Amount = Actual + Tax
    ISNULL(SUM(ci.ServiceCharges - ISNULL(ci.Discount, 0) + ISNULL(ci.TaxAmount, 0)), 0) AS InvoiceAmount,

    -- Collection Amount = payments received against invoices in this period
    ISNULL((
        SELECT SUM(rd.Amount)
        FROM   ReceiptDetails rd
        INNER JOIN Receipts r ON r.ID = rd.ReceiptID AND r.IsDeleted = 0
        WHERE  rd.InvoiceID IN (
                   SELECT ID FROM ClientInvoice ci2
                   WHERE  ci2.Branch = ci.Branch
                     AND  ci2.IsDeleted = 'N'
                     AND  ci2.InvoiceDate >= {?StartDate}
                     AND  ci2.InvoiceDate <= {?EndDate}
               )
    ), 0)                                                                       AS CollectionAmount,

    -- CN Amount = Credit Note Amount from receipts for this branch in this period
    ISNULL((
        SELECT SUM(r.CreditNoteAmount)
        FROM   Receipts r
        WHERE  r.Branch = ci.Branch
          AND  r.IsDeleted = 0
          AND  r.ReceiptDate >= {?StartDate}
          AND  r.ReceiptDate <= {?EndDate}
    ), 0)                                                                       AS CNAmount,

    -- Balance = Invoice Amount - Collection Amount - CN Amount
    ISNULL(SUM(ci.ServiceCharges - ISNULL(ci.Discount, 0) + ISNULL(ci.TaxAmount, 0)), 0)
        - ISNULL((
            SELECT SUM(rd.Amount)
            FROM   ReceiptDetails rd
            INNER JOIN Receipts r ON r.ID = rd.ReceiptID AND r.IsDeleted = 0
            WHERE  rd.InvoiceID IN (
                       SELECT ID FROM ClientInvoice ci2
                       WHERE  ci2.Branch = ci.Branch
                         AND  ci2.IsDeleted = 'N'
                         AND  ci2.InvoiceDate >= {?StartDate}
                         AND  ci2.InvoiceDate <= {?EndDate}
                   )
        ), 0)
        - ISNULL((
            SELECT SUM(r.CreditNoteAmount)
            FROM   Receipts r
            WHERE  r.Branch = ci.Branch
              AND  r.IsDeleted = 0
              AND  r.ReceiptDate >= {?StartDate}
              AND  r.ReceiptDate <= {?EndDate}
        ), 0)                                                                   AS Balance

FROM ClientInvoice ci
WHERE ci.IsDeleted = 'N'
  AND ci.InvoiceDate >= {?StartDate}
  AND ci.InvoiceDate <= {?EndDate}
  AND ('{?Branch}' = '0' OR '{?Branch}' = '' OR ci.Branch = '{?Branch}')
GROUP BY ci.Branch
ORDER BY ci.Branch;

-- ============================================================
-- HOW TO USE IN CRYSTAL REPORTS DESIGNER
-- ============================================================
-- 1. Open SAP Crystal Reports → New Blank Report
-- 2. Database Expert → My Connections → \SQLEXPRESS → obmuat
--    → Add Command → paste the SQL above
--    (Remove the {? } parameter syntax — Crystal will auto-detect
--     them when you add parameters in the next step)
-- 3. Create Parameters in Field Explorer → Parameter Fields:
--      StartDate           DateTime
--      EndDate             DateTime
--      Branch              String
--      CompanyName         String
--      CompanyAddress1     String
--      CompanyAddress2     String
--      CompanyAddress3     String
--      CompanyAddress4     String
--      CompanyRegistration String
--      CompanyPhone        String
--      LoginID             String
-- 4. Design the Report Header with company details
-- 5. Page Header = column headers (see below)
-- 6. Details band = one row per branch
-- 7. Report Footer = Grand Total row (SUM of each column)
-- ============================================================
