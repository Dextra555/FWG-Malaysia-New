-- Migration: Create vwPaySlipGuard1 view
-- Purpose: Provides a Payment-1-safe view of PaySlip where Shift2Salary is always 0.
-- Used by PaySlipForGuard1Report.aspx.cs to remap the Crystal Report datasource.
-- Shift2Salary belongs to Payment 2 (PaySlipGuardPart2.rpt); it must NOT appear in Payment 1 totals.
-- All other columns are passed through unchanged so Crystal Reports can bind without schema errors.

CREATE OR ALTER VIEW vwPaySlipGuard1 AS
SELECT
    ID,
    Period,
    EmployeeID,
    BasicSalaryDays,
    BasicSalaryRate,
    BasicSalary,
    OverTimeSalaryHours,
    OverTimeSalaryRate,
    OverTimeSalary,
    OffDaySalaryDays,
    OffDaySalaryRate,
    OffDaySalary,
    OffDayOverTimeSalaryHours,
    OffDayOverTimeSalaryRate,
    OffDayOverTimeSalary,
    HolidaySalaryDays,
    HolidaySalaryRate,
    HolidaySalary,
    HolidayOverTimeSalaryHours,
    HolidayOverTimeSalaryRate,
    HolidayOverTimeSalary,
    Shift2SalaryDaysHours,
    Shift2SalaryRateType,
    Shift2SalaryRate,
    0 AS Shift2Salary,          -- zeroed out for Payment 1; Shift2Salary belongs to Payment 2
    ReAllowanceDays,
    ReAllowance,
    AttendanceAllowance,
    SpecialAllowance,
    EPFDeductionAmount,
    SOCSODeductionAmount,
    EPFEmployerContribution,
    SOCSOEmployerContribution,
    DailyAdvanceRecovery,
    SpecialAdvanceRecovery,
    MonthlyAdvanceRecovery,
    UniformIssueRecovery,
    LoanRecovery,
    IncomeTaxDeduction,
    MiscAmount,
    MiscDeduction,
    SalaryPayMode,
    Bonus,
    LastUpdate,
    LastUpdatedBy,
    SIPEmployeeContribution,
    SIPEmployerContribution
FROM PaySlip;
