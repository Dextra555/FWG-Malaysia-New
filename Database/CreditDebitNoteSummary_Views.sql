-- ============================================================
-- Views: VWCreditNoteByMonthBranch  &  VWDebitNoteByMonthBranch
-- Purpose : Month-wise Credit Note / Debit Note summary per Branch
--           Used by Crystal Report cross-tab (rows=Branch, cols=Month)
-- Run on  : OBMS database
-- ============================================================

-- ─── Credit Note View ───────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'VWCreditNoteByMonthBranch')
    DROP VIEW VWCreditNoteByMonthBranch;
GO

CREATE VIEW VWCreditNoteByMonthBranch AS
SELECT
    cn.Branch,
    bm.Name                                                                             AS BranchName,
    YEAR(cn.CreditNoteDate)                                                             AS NoteYear,
    MONTH(cn.CreditNoteDate)                                                            AS NoteMonth,
    DATENAME(MONTH, cn.CreditNoteDate) + ' ' + CAST(YEAR(cn.CreditNoteDate) AS VARCHAR(4))
                                                                                        AS MonthLabel,
    COUNT(cn.ID)                                                                        AS NoteCount,
    SUM(cn.CreditNoteAmount)                                                            AS CreditNoteAmount,
    SUM(ISNULL(cn.TaxAmount, 0))                                                        AS TaxAmount,
    SUM(cn.TotalAmount)                                                                 AS TotalAmount
FROM CreditNote cn
INNER JOIN BranchMaster bm
    ON bm.Code = cn.Branch
WHERE cn.IsDeleted = 0
GROUP BY
    cn.Branch,
    bm.Name,
    YEAR(cn.CreditNoteDate),
    MONTH(cn.CreditNoteDate),
    DATENAME(MONTH, cn.CreditNoteDate) + ' ' + CAST(YEAR(cn.CreditNoteDate) AS VARCHAR(4));
GO

-- Quick test:
-- SELECT * FROM VWCreditNoteByMonthBranch ORDER BY NoteYear, NoteMonth, Branch
GO

-- ─── Debit Note View ────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'VWDebitNoteByMonthBranch')
    DROP VIEW VWDebitNoteByMonthBranch;
GO

CREATE VIEW VWDebitNoteByMonthBranch AS
SELECT
    dn.Branch,
    bm.Name                                                                             AS BranchName,
    YEAR(dn.DebitNoteDate)                                                              AS NoteYear,
    MONTH(dn.DebitNoteDate)                                                             AS NoteMonth,
    DATENAME(MONTH, dn.DebitNoteDate) + ' ' + CAST(YEAR(dn.DebitNoteDate) AS VARCHAR(4))
                                                                                        AS MonthLabel,
    COUNT(dn.ID)                                                                        AS NoteCount,
    SUM(dn.DebitNoteAmount)                                                             AS DebitNoteAmount,
    SUM(ISNULL(dn.TaxAmount, 0))                                                        AS TaxAmount,
    SUM(dn.TotalAmount)                                                                 AS TotalAmount
FROM DebitNote dn
INNER JOIN BranchMaster bm
    ON bm.Code = dn.Branch
WHERE dn.IsDeleted = 0
GROUP BY
    dn.Branch,
    bm.Name,
    YEAR(dn.DebitNoteDate),
    MONTH(dn.DebitNoteDate),
    DATENAME(MONTH, dn.DebitNoteDate) + ' ' + CAST(YEAR(dn.DebitNoteDate) AS VARCHAR(4));
GO

-- Quick test:
-- SELECT * FROM VWDebitNoteByMonthBranch ORDER BY NoteYear, NoteMonth, Branch
GO

-- ============================================================
-- Crystal Report SQL Command (use as "Add Command" data source)
-- for CreditNoteSummaryByMonth.rpt
-- Parameters: {?StartDate}, {?EndDate}, {?Branch}
-- ============================================================
/*
SELECT
    v.Branch,
    v.BranchName,
    v.NoteYear,
    v.NoteMonth,
    v.MonthLabel,
    v.NoteCount,
    v.CreditNoteAmount,
    v.TaxAmount,
    v.TotalAmount
FROM VWCreditNoteByMonthBranch v
WHERE
    -- filter by date: reconstruct a representative date for range check
    DATEFROMPARTS(v.NoteYear, v.NoteMonth, 1) >= DATEFROMPARTS(YEAR({?StartDate}), MONTH({?StartDate}), 1)
    AND DATEFROMPARTS(v.NoteYear, v.NoteMonth, 1) <= DATEFROMPARTS(YEAR({?EndDate}), MONTH({?EndDate}), 1)
    AND ({?Branch} = '0' OR v.Branch = {?Branch})
ORDER BY
    v.Branch,
    v.NoteYear,
    v.NoteMonth;
*/

-- ============================================================
-- Crystal Report SQL Command for DebitNoteSummaryByMonth.rpt
-- ============================================================
/*
SELECT
    v.Branch,
    v.BranchName,
    v.NoteYear,
    v.NoteMonth,
    v.MonthLabel,
    v.NoteCount,
    v.DebitNoteAmount,
    v.TaxAmount,
    v.TotalAmount
FROM VWDebitNoteByMonthBranch v
WHERE
    DATEFROMPARTS(v.NoteYear, v.NoteMonth, 1) >= DATEFROMPARTS(YEAR({?StartDate}), MONTH({?StartDate}), 1)
    AND DATEFROMPARTS(v.NoteYear, v.NoteMonth, 1) <= DATEFROMPARTS(YEAR({?EndDate}), MONTH({?EndDate}), 1)
    AND ({?Branch} = '0' OR v.Branch = {?Branch})
ORDER BY
    v.Branch,
    v.NoteYear,
    v.NoteMonth;
*/

-- ============================================================
-- Crystal Report designer notes for CreditNoteSummaryByMonth.rpt
-- (same pattern applies to DebitNoteSummaryByMonth.rpt)
-- ============================================================
-- 1. Create new Crystal Report: UatReports/Finance/CreditNoteSummaryByMonth.rpt
-- 2. Data source: "Add Command" → paste the Credit Note SQL Command above.
--    Create parameters: StartDate (DateTime), EndDate (DateTime), Branch (String)
-- 3. Company header parameters (String):
--    CompanyName, CompanyAddress1..4, CompanyRegistration, CompanyPhone, LoginID
-- 4. Report Header: company name, address, registration, phone.
-- 5. Insert Cross-Tab (Insert > Cross-Tab):
--    Rows    : Branch  (show BranchName for display)
--    Columns : MonthLabel  (sort order: NoteYear ASC, NoteMonth ASC)
--    Summary : Sum({Command.TotalAmount})
--    Enable Row Grand Total  (= branch total across months)
--    Enable Column Grand Total (= monthly total across all branches)
-- 6. Below cross-tab add a "Prepared By:" label + {?LoginID}
-- 7. Number format: #,##0.00 on all amount fields.
-- 8. Report title:
--      Credit Note: "CREDIT NOTE SUMMARY REPORT"
--      Debit Note : "DEBIT NOTE SUMMARY REPORT"
-- ============================================================
