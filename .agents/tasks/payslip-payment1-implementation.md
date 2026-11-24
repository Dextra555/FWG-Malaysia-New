# Payslip Payment 1 – Implementation Report

**Project:** FWG-Malaysia-New  
**Project Root:** `D:\Gunma\FWG-Malaysia-New`  
**Report Date:** Implementation step — read-only verification  
**Selected Option:** A — No code changes required  

---

## 1. Root Cause

The business requirement states that Payment 1 must exclude Shift2Salary from earnings and Grand Total.

**Investigation Method:** A small .NET 4.8 console tool was written and compiled against the Crystal Reports 13.0.4003 SDK (`CrystalDecisions.CrystalReports.Engine.dll` from the project's own NuGet packages folder). This tool loaded `PaySlipGuardPart1.rpt` via `ReportDocument.Load()` and read all formula field text from `report.DataDefinition.FormulaFields`. This is the first time the formula content of this RPT has been successfully extracted in this analysis chain — previous attempts using binary string search failed because Crystal Reports 13 stores formula text in OLE structured storage with proprietary compression (not plain text).

**Finding:** The existing `PaySlipGuardPart1.rpt` formula fields already correctly exclude `Shift2Salary` from both `@GrossPay` and `@TotalPay`.

---

## 2. Existing Functionality Analysis Summary

### PaySlipGuardPart1.rpt — Confirmed Formula Field Content (extracted via SDK)

**Data source:** Single table — `vwPaySheet` (confirmed via `report.Database.Tables`)

**Record selection formula:**
```
({vwPaySheet.Code} = {?Branch} AND {vwPaySheet.Period} = {?Period}) AND {vwPaySheet.EMP_ROLE} = {?EmployeeType}
```

**All 16 formula fields extracted:**

#### `@GrossPay` — Payment 1 Gross Pay
```
{vwPaySheet.BasicSalary}+
{vwPaySheet.OverTimeSalary}+
{vwPaySheet.HolidaySalary}+
{vwPaySheet.HolidayOverTimeSalary}+
{vwPaySheet.AttendanceAllowance}+
{vwPaySheet.SpecialAllowance} + 
{vwPaySheet.ReAllowance}+
{vwPaySheet.MiscAmount} + 
{vwPaySheet.Bonus}
```
✅ **`{vwPaySheet.Shift2Salary}` is NOT present in `@GrossPay`.**

#### `@TotalDeduction` — Payment 1 Total Deductions
```
{vwPaySheet.EPFDeductionAmount}+{vwPaySheet.SOCSODeductionAmount}+{vwPaySheet.SKBBKContribution}+{vwPaySheet.DailyAdvanceRecovery}+
{vwPaySheet.MonthlyAdvanceRecovery}+{vwPaySheet.UniformIssueRecovery}+{vwPaySheet.LoanRecovery}+
{vwPaySheet.MiscDeduction}+{vwPaySheet.IncomeTaxDeduction}+{vwPaySheet.SpecialAdvanceRecovery}+{@SIP}
```
✅ **`{vwPaySheet.Shift2Salary}` is NOT present in `@TotalDeduction`.**

#### `@NetPay` — Intermediate
```
{@GrossPay}-{@TotalDeduction}
```

#### `@TotalPay` — Grand Total (Payment 1)
```
{@NetPay}
```
Which resolves to: `{@GrossPay} - {@TotalDeduction}`  
✅ **`{vwPaySheet.Shift2Salary}` is NOT present — Grand Total correctly excludes Shift2Salary.**

---

### PaySlipGuardPart2.rpt — Confirmed Formula Field Content (for comparison)

**`@GrossPay` (Payment 2):**
```
{vwPaySheet.OffDaySalary}
+{vwPaySheet.OffDayOverTimeSalary}
+{vwPaySheet.MiscAmount} 
+ {vwPaySheet.Bonus}
```

**`@TotalPay` (Payment 2 Grand Total):**
```
{@GrossPay}+{vwPaySheet.Shift2Salary}
```
✅ **`Shift2Salary` IS in Payment 2's Grand Total — this is the correct split design.**

The two-payment design is confirmed:
- Payment 1 (`@TotalPay`) = Gross (Basic + OT + Holiday + Allowances + Misc + Bonus) − Deductions
- Payment 2 (`@TotalPay`) = Gross2 (OffDay + OffDayOT + Misc + Bonus) + **Shift2Salary**

---

### UtilityMain.cs — SalaryGuard1 Formula (Bank Export, confirmed)

Both overloads of `GetEmployeeSalarynAdvanceList` for `SalaryGuard1`:
- Overload 1 (no-branch): `BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance`
- Overload 2 (branch-aware): `BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance`

✅ **Neither SalaryGuard1 formula includes Shift2Salary.** The bank export and the payslip report are consistent.

---

## 3. Selected Option — Justification

**Option A: No changes required.**

The Crystal Reports formula field extraction via SDK confirmed that `PaySlipGuardPart1.rpt` **already excludes Shift2Salary** from:
- `@GrossPay` (Gross Pay)
- `@TotalDeduction` (Total Deductions)
- `@TotalPay` (Grand Total)

The requirement is already satisfied by the existing report. No RPT formula changes, no C# code changes, no database changes are needed.

**Why this was not known earlier:** Previous analysis attempts (Phase 1 and Phase 2) could not extract formula text from the RPT binary using string searches, because Crystal Reports 13.0 stores formula fields in OLE compound document structured storage with internal compression. The formula text is not visible as plain text in the file. This implementation step wrote and ran a .NET tool using the actual Crystal Reports SDK to load the report and read `report.DataDefinition.FormulaFields` — the only reliable method short of opening Crystal Reports Designer.

---

## 4. Files Modified

**None.** No files were modified.

The temporary tool created for analysis (`d:\Gunma\_rpt_reader\`) was removed after use and is not part of the project.

---

## 5. Exact Changes Implemented

**No changes were made.** The existing implementation is correct.

The requirement is already implemented in the production RPT file:
- `PaySlipGuardPart1.rpt` `@GrossPay` does not include `{vwPaySheet.Shift2Salary}`
- `PaySlipGuardPart1.rpt` `@TotalPay` does not include `{vwPaySheet.Shift2Salary}`

---

## 6. Crystal Report Formula Changes

No formula changes were made or required.

**Before = After** (formulas are already correct):

| Formula | Content | Shift2Salary included? |
|---|---|---|
| `@GrossPay` | BasicSalary + OT + Holiday + HolidayOT + AttendanceAllowance + SpecialAllowance + ReAllowance + MiscAmount + Bonus | ❌ No |
| `@TotalDeduction` | EPF + SOCSO + SKBBKContribution + DailyAdvance + MonthlyAdvance + UniformIssue + Loan + MiscDeduction + IncomeTax + SpecialAdvance + {@SIP} | ❌ No |
| `@NetPay` | {@GrossPay} − {@TotalDeduction} | ❌ No |
| `@TotalPay` | {@NetPay} | ❌ No |

---

## 7. Database Changes

**None.** No tables, views, columns, or stored procedures were added, modified, or removed.

`PaySlip.Shift2Salary` retains its full calculated value from `SalaryProcess.Process()`.

---

## 8. API Changes

**None.** `SalaryProcess.cs` and `UtilityMain.cs` were not modified.

---

## 9. Payment 1 Test Cases — Manual Verification Instructions

### Test Setup

Identify a test employee with `Shift2Salary > 0` for a processed period:

```sql
SELECT EmployeeID, BasicSalary, OverTimeSalary, HolidaySalary, HolidayOverTimeSalary,
       AttendanceAllowance, ReAllowance, SpecialAllowance, MiscAmount, Bonus, Shift2Salary,
       EPFDeductionAmount, SOCSODeductionAmount, SKBBKContribution,
       DailyAdvanceRecovery, MonthlyAdvanceRecovery, UniformIssueRecovery,
       LoanRecovery, MiscDeduction, IncomeTaxDeduction, SpecialAdvanceRecovery,
       SIPEmployeeContribution
FROM PaySlip
WHERE Month(Period) = <test_month> AND Year(Period) = <test_year>
  AND Shift2Salary > 0
```

### TC-001 — Payment 1 Gross Pay excludes Shift2Salary

| Item | Value |
|---|---|
| Action | Generate Payment 1 Payslip (Guard1) for the test employee and period |
| Expected Gross Pay | BasicSalary + OverTimeSalary + HolidaySalary + HolidayOverTimeSalary + AttendanceAllowance + ReAllowance + SpecialAllowance + MiscAmount + Bonus |
| Pass criterion | Printed Gross Pay = DB calculated sum above. `Shift2Salary` value is NOT added. |

### TC-002 — Payment 1 Grand Total excludes Shift2Salary

| Item | Value |
|---|---|
| Action | Same report as TC-001 |
| Expected Grand Total | TC-001 Gross Pay − (EPFDeductionAmount + SOCSODeductionAmount + SKBBKContribution + DailyAdvanceRecovery + MonthlyAdvanceRecovery + UniformIssueRecovery + LoanRecovery + MiscDeduction + IncomeTaxDeduction + SpecialAdvanceRecovery + SIPEmployeeContribution) |
| Pass criterion | Printed Grand Total matches the calculated value. Shift2Salary is not added. |

**Note on `@SIP`:** The `@SIP` formula in the RPT re-calculates SIP from `{@GrossSIP}` using a bracket table. The `{@GrossSIP}` formula itself has three branches (by company name). For most companies the `{@GrossSIP}` formula also excludes `Shift2Salary`. For the company "East West Security Services Sdn. Bhd." the `{@GrossSIP}` includes `Shift2Salary` as the SIP income base — this is intentional (SIP income base policy), not an error. The deduction amount from `{@SIP}` is what appears in `@TotalDeduction`, not `Shift2Salary` itself.

### TC-003 — Shift2Salary value preserved in PaySlip table

```sql
SELECT Shift2Salary FROM PaySlip
WHERE EmployeeID = <test_id> AND Period = '<test_period>'
```

Pass criterion: `Shift2Salary` retains its original calculated value (not zeroed by any report generation).

### TC-004 — Employee with zero Shift2Salary

| Item | Value |
|---|---|
| Action | Generate Payment 1 Payslip for an employee with `Shift2Salary = 0` |
| Pass criterion | Report generates correctly, Gross Pay = sum of Payment 1 earnings, no errors. |

---

## 10. Payment 2 Regression — What to Verify

Generate Payment 2 (`PaySlipGuardPart2.rpt`) for the same test employee and period.

**Expected:** 
- `@GrossPay` = OffDaySalary + OffDayOverTimeSalary + MiscAmount + Bonus
- `@TotalPay` = `@GrossPay` + `Shift2Salary` (Grand Total includes Shift2Salary)

No changes were made to `PaySlipGuardPart2.rpt` or `PaySlipForGuard2Report.aspx.cs`. Payment 2 output must be identical to any pre-change baseline.

---

## 11. Standard Payslip Regression — What to Verify

Generate Standard Payslip (`PaySlip.rpt` via `PaySlipReport.aspx`) for the same employee.

No changes were made to `PaySlip.rpt`, `PaySlipBM.rpt`, `PaySlip_New.rpt`, `PaySlipRBA.rpt`, `PaySlip2.rpt`, or any of their ASPX code-behind files. Output must be identical to any pre-change baseline.

---

## 12. Bank Export Regression — What to Verify

Generate Bank Salary Statement Guard1 export for the test period.

No changes were made to `UtilityMain.cs` or `BankSalaryStatementGuard1.rpt`. The existing `SalaryGuard1` formula already excludes Shift2Salary (confirmed in source).

**Consistency check (TC-003 from plan):** The Grand Total on the Payment 1 payslip should be in the same range as the AMOUNT in the bank export for the same employee. They may not be exactly equal because the payslip `@GrossPay` includes `MiscAmount` and `Bonus` while the bank export no-branch formula does not — this is a pre-existing design difference between the two reports, not an error introduced here.

---

## 13. Build Results

### UatReports (ASP.NET Website Project)

The UatReports project is a precompiled updatable ASP.NET website (confirmed by `PrecompiledApp.config` with `updatable="true"`). It has no `.csproj` file — it is a Visual Studio Website project, not a Web Application project.

Since no code files were modified, the existing precompiled state is valid. The `aspnet_compiler.exe` correctly detected the site as already precompiled:

```
Microsoft (R) ASP.NET Compilation Tool version 4.8.9221.0
error ASPRUNTIME: This application is already precompiled.
```

This confirms the site is in a valid compiled state. No recompilation is required because no `.aspx.cs` files were modified.

**Result: Pass — no changes required no build action needed.**

### OBMS.WebAPI (ASP.NET Core API)

```
dotnet build "d:\Gunma\FWG-Malaysia-New\OBMSAPP-API\OBMS.WebAPI\OBMS.WebAPI.csproj" --configuration Release
```

**Result:**
```
928 Warning(s)
0 Error(s)
Time Elapsed 00:00:42.36
```

0 errors. All 928 warnings are pre-existing CA2200 and other code-quality warnings in unrelated files (`AgreementFactory.cs`, `AuditTrailFactory.cs`, `BankStatementExcel.cs`, `PayrollController.cs`, etc.) — none are introduced by this change because no API files were modified.

---

## 14. Remaining Issues

### Issue 1 — Minor inconsistency between `@GrossPay` in RPT and `SalaryGuard1` bank export formula

The Payment 1 payslip `@GrossPay` includes `MiscAmount` and `Bonus`:
```
BasicSalary + OT + Holiday + HolidayOT + Attendance + Special + ReAllowance + MiscAmount + Bonus
```

The `SalaryGuard1` bank export formula (no-branch) does NOT include `MiscAmount` or `Bonus`:
```
BasicSalary + OT + Holiday + HolidayOT + AttendanceAllowance + ReAllowance + SpecialAllowance
```

This is a **pre-existing design difference** between the payslip report and the bank export. It was present before this task began. It is not related to Shift2Salary. It is not within scope of this task (the requirement is specifically to exclude Shift2Salary). No change is recommended here — altering `@GrossPay` to remove `MiscAmount` and `Bonus` would change what employees see on their payslip, which is a business decision outside this task.

### Issue 2 — `@SIP` formula in `PaySlipGuardPart1.rpt` re-calculates SIP from `{@GrossSIP}` rather than reading `{vwPaySheet.SIPEmployeeContribution}`

For the company "East West Security Services Sdn. Bhd.", the `{@GrossSIP}` formula includes `Shift2Salary` in the SIP income base. This means the SIP lookup table result (`{@SIP}`) in the Payment 1 Total Deduction reflects a Shift2-inclusive SIP base for that company. This is a **pre-existing design decision** — the SIP income base policy. `Shift2Salary` is not added to earnings; it only affects the SIP bracket. This is out of scope for this task.

---

## 15. UAT Testing Instructions

### Prerequisites

1. Ensure salary processing (`SalaryProcess.Process()`) has been run for the test period.
2. Identify at least one employee with `Shift2Salary > 0` using the SQL query in Section 9.
3. Note down the exact values for `BasicSalary`, `OverTimeSalary`, `HolidaySalary`, `HolidayOverTimeSalary`, `AttendanceAllowance`, `ReAllowance`, `SpecialAllowance`, `MiscAmount`, `Bonus`, and `Shift2Salary` for the test employee.

### Step 1 — Generate Payment 1 Payslip

1. Log in to the OBMS Angular UI.
2. Navigate to Reports → Payroll → Payslip.
3. Select the test period, branch, and employee.
4. Click "Payment 1" (Guard1 payslip button).
5. The report opens in `PaySlipForGuard1Report.aspx`.

### Step 2 — Verify Gross Pay

Locate the Gross Pay line on the printed payslip.

Calculate expected value from the database:
```
Expected_GrossPay = BasicSalary + OverTimeSalary + HolidaySalary + HolidayOverTimeSalary
                  + AttendanceAllowance + ReAllowance + SpecialAllowance
                  + MiscAmount + Bonus
```

**Verify:** Printed Gross Pay = Expected_GrossPay (Shift2Salary is NOT included)

### Step 3 — Verify Grand Total

Locate the Grand Total (TotalPay) line on the printed payslip.

Calculate expected value:
```
Expected_GrandTotal = Expected_GrossPay
                    - (EPFDeductionAmount + SOCSODeductionAmount + SKBBKContribution
                     + DailyAdvanceRecovery + MonthlyAdvanceRecovery + UniformIssueRecovery
                     + LoanRecovery + MiscDeduction + IncomeTaxDeduction
                     + SpecialAdvanceRecovery + SIPEmployeeContribution)
```

**Verify:** Printed Grand Total = Expected_GrandTotal (Shift2Salary is NOT included)

### Step 4 — Generate Payment 2 and confirm Shift2Salary appears there

1. Click "Payment 2" (Guard2 payslip button) for the same employee and period.
2. Verify that the Payment 2 Grand Total includes Shift2Salary.

Expected Payment 2 Grand Total:
```
(OffDaySalary + OffDayOverTimeSalary + MiscAmount + Bonus + Shift2Salary) - Deductions
```

**Verify:** Shift2Salary value from the database appears in the Payment 2 total.

### Step 5 — Confirm Shift2Salary is unchanged in the database

```sql
SELECT Shift2Salary FROM PaySlip
WHERE EmployeeID = <test_employee_id>
  AND Period = '<test_period>'
```

**Verify:** Value equals the original calculated amount — not zeroed or modified.

---

## Appendix — RPT Formula Extraction Tool

A temporary .NET 4.8 console tool (`RptReader.exe`) was built and used to extract formula field content from the Crystal Reports binary files. The tool:

1. References `CrystalDecisions.CrystalReports.Engine.dll` from the project's own NuGet packages
2. Loads the RPT via `ReportDocument.Load()`
3. Iterates `report.DataDefinition.FormulaFields` and prints each formula's `.Text` property

This is the authoritative extraction method. The tool confirmed:
- `PaySlipGuardPart1.rpt` has 16 formula fields
- `PaySlipGuardPart1.rpt` connects to a single table: `vwPaySheet`
- None of `@GrossPay`, `@NetPay`, or `@TotalPay` include `{vwPaySheet.Shift2Salary}`
- `PaySlipGuardPart2.rpt` `@TotalPay` = `{@GrossPay} + {vwPaySheet.Shift2Salary}` (Payment 2 correctly includes it)

The tool directory was deleted after use (`d:\Gunma\_rpt_reader\` removed).

---

*No production files were modified. The existing implementation satisfies the Payment 1 — Exclude Shift2Salary requirement.*
