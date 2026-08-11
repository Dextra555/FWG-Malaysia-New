-- ============================================================
-- Debug Script: Why Employee 912 Salary Process is Failing
-- ============================================================
-- SalaryProcess.Process() method uses this flow:
--   1. SELECT from Attendance JOIN Employee (must exist)
--   2. For each employee: SELECT from EmploymentDetails 
--      JOIN Employee JOIN EmployeeSalaryDetails JOIN SalaryStructure
--      LEFT JOIN Client (from AttendanceDetails + ClientMaster)
-- If any INNER JOIN fails -> employee silently skipped
-- ============================================================

DECLARE @EmpID INT = 912
-- Change this to the period you are trying to process (e.g., '2026-07-01')
DECLARE @Period DATE = '2026-07-01'

-- ============================================================
-- STEP 1: Check Employee record exists
-- ============================================================
SELECT '1. Employee Record' AS [Check],
    EMP_ID, EMP_CODE, EMP_NAME, EMP_ROLE, EMP_BRANCH_CODE, HasTransfered
FROM Employee
WHERE EMP_ID = @EmpID

-- ============================================================
-- STEP 2: Check Attendance exists for this Period & Branch
-- ============================================================
SELECT '2. Attendance Record' AS [Check],
    A.ID, A.EmployeeID, A.Branch, A.Period, A.AllowanceDeduction, 
    A.SpecialAllowanceDeduction, A.KPI, A.KPIDeduction
FROM Attendance A
WHERE A.EmployeeID = @EmpID
  AND MONTH(A.Period) = MONTH(@Period)
  AND YEAR(A.Period)  = YEAR(@Period)

-- ============================================================
-- STEP 3: Simulate the main Attendance JOIN in Process()
--   (this is the query that builds the employee loop)
-- ============================================================
SELECT '3. Attendance+Employee JOIN' AS [Check],
    A.Branch, A.EmployeeID, E.EMP_NAME, E.EMP_ROLE
FROM Attendance A
INNER JOIN Employee E ON E.EMP_ID = A.EmployeeID
WHERE A.EmployeeID = @EmpID
  AND A.Period = @Period
-- NOTE: Process() passes Branch param too - check if branch matches
-- Remove the Period filter above and test with specific branch if needed

-- ============================================================
-- STEP 4: Check EmploymentDetails record
-- ============================================================
SELECT '4. EmploymentDetails' AS [Check],
    ED.EMPPAY_CODE, ED.EMPPAY_DATE_JOINED, ED.EMPPAY_DATE_RESIGNED,
    ED.EMPPAY_BASIC_RATE, ED.Salarylab, ED.PAYMODE,
    ED.AttendanceAllowance, ED.SpecialAllowance,
    ED.AttendanceAllowanceWorkingDays, ED.AttendanceAllowanceFollowCalendar
FROM EmploymentDetails ED
INNER JOIN Employee E ON ED.EMPPAY_CODE = E.EMP_CODE
WHERE E.EMP_ID = @EmpID

-- ============================================================
-- STEP 5: Check EmployeeSalaryDetails record
-- ============================================================
SELECT '5. EmployeeSalaryDetails' AS [Check],
    ESD.EMPFL_CODE, ESD.EMPFL_EPF8Pa, ESD.EPFDETECT, ESD.SOCSODETECT,
    ESD.DETECTBYND55, ESD.INCOMETAXDETECT, ESD.SKBBK, ESD.TMPGUARD,
    ESD.EMPFL_BANK, ESD.EMPFL_BK_ACCNO, ESD.PAYMODE
FROM EmployeeSalaryDetails ESD
INNER JOIN Employee E ON ESD.EMPFL_CODE = E.EMP_CODE
WHERE E.EMP_ID = @EmpID

-- ============================================================
-- STEP 6: Check SalaryStructure linked via EmploymentDetails
-- ============================================================
SELECT '6. SalaryStructure' AS [Check],
    SS.SalaryID, SS.Name, SS.SalaryBand, SS.WorkingDays, SS.WorkingHours,
    SS.GeneralDayRate, SS.GeneralDayHours, SS.GeneralDayOTRate,
    SS.OffDayRate, SS.OffDayOTRate, SS.HolidayRate, SS.HolidayOTRate,
    SS.TravelAllowance, SS.EmployeeNationality, SS.EICC, SS.NonStructure,
    ED.Salarylab
FROM EmploymentDetails ED
INNER JOIN Employee E ON ED.EMPPAY_CODE = E.EMP_CODE
INNER JOIN SalaryStructure SS ON SS.SalaryID = ED.Salarylab
WHERE E.EMP_ID = @EmpID

-- ============================================================
-- STEP 7: Full Employee Query used inside Process() loop
--   If this returns NO rows -> employee is silently skipped
-- ============================================================
SELECT '7. Full Employee Query (used in Process loop)' AS [Check], *
FROM EmploymentDetails
INNER JOIN Employee ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
INNER JOIN EmployeeSalaryDetails ON EmployeeSalaryDetails.EMPFL_CODE = Employee.EMP_CODE
INNER JOIN SalaryStructure ON SalaryStructure.SalaryID = EmploymentDetails.Salarylab
LEFT OUTER JOIN (
    SELECT TOP 1 A.Employeeid, C.Name, C.KPIHours 
    FROM Attendance A
    INNER JOIN AttendanceDetails B ON A.id = B.attendanceID 
    INNER JOIN ClientMaster C ON B.Client = C.Code AND A.Branch = C.Branch
    WHERE A.EmployeeID = @EmpID AND A.period = @Period
) Client ON Employee.EMP_ID = Client.EmployeeID
WHERE EMP_ID = @EmpID

-- ============================================================
-- STEP 8: Check what is NULL / missing from INNER JOINs
-- ============================================================
SELECT '8. JOIN Breakdown - find missing link' AS [Check],
    E.EMP_ID,
    E.EMP_CODE,
    CASE WHEN ED.EMPPAY_CODE IS NULL THEN 'MISSING EmploymentDetails' ELSE 'OK' END AS EmploymentDetails_Status,
    CASE WHEN ESD.EMPFL_CODE IS NULL THEN 'MISSING EmployeeSalaryDetails' ELSE 'OK' END AS SalaryDetails_Status,
    CASE WHEN SS.SalaryID IS NULL THEN 'MISSING SalaryStructure (check Salarylab=' + CAST(ISNULL(ED.Salarylab,0) AS VARCHAR) + ')' ELSE 'OK' END AS SalaryStructure_Status
FROM Employee E
LEFT JOIN EmploymentDetails ED ON ED.EMPPAY_CODE = E.EMP_CODE
LEFT JOIN EmployeeSalaryDetails ESD ON ESD.EMPFL_CODE = E.EMP_CODE
LEFT JOIN SalaryStructure SS ON SS.SalaryID = ED.Salarylab
WHERE E.EMP_ID = @EmpID

-- ============================================================
-- STEP 9: Check if SalaryProcess is already Locked for this period
-- ============================================================
SELECT '9. SalaryProcess Lock Status' AS [Check],
    ID, Branch, Period, EmployeeType, IsLocked, Remarks, LastUpdatedBy, LastUpdate
FROM SalaryProcess
WHERE Period = @Period
  AND Branch = (SELECT EMP_BRANCH_CODE FROM Employee WHERE EMP_ID = @EmpID)

-- ============================================================
-- SUMMARY: Most common reasons employee is skipped:
-- 1. EmploymentDetails missing -> fix: insert record
-- 2. EmployeeSalaryDetails missing -> fix: insert record  
-- 3. SalaryStructure.SalaryID doesn't match EmploymentDetails.Salarylab -> fix: update Salarylab
-- 4. SalaryProcess already Locked -> fix: unlock or contact admin
-- 5. Attendance.Period exact date mismatch (e.g., '2026-07-15' vs '2026-07-01')
-- 6. Branch mismatch between Attendance and what Process() receives
-- ============================================================
