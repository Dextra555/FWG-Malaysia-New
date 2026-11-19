-- ============================================================
-- Reverse EMP_ROLE: Others → Guard
-- Employee : GURUNG RAJU (2nd)
-- Passport : PA2392740
-- Date     : 2026-10-30
-- Reason   : Was incorrectly included in the Others bulk update.
--            Revert Employee, EmployeeHistory (open row), and
--            EmploymentDetails back to 'Guard'.
-- ============================================================

-- ============================================================
-- STEP 1: Preview before changing
-- ============================================================
SELECT
    e.EMP_ID,
    e.EMP_CODE,
    e.EMP_NAME,
    e.EMP_PASSPORT_NO,
    e.EMP_ROLE                      AS [Employee.EMP_ROLE],
    eh.EMP_ROLE                     AS [EmployeeHistory.EMP_ROLE],
    ed.EMPPAY_CATEGORY              AS [EmploymentDetails.EMPPAY_CATEGORY]
FROM Employee e
LEFT JOIN EmployeeHistory eh
    ON  eh.EMP_ID      = e.EMP_ID
    AND eh.Emp_EndDate IS NULL       -- open / currently-active row
LEFT JOIN EmploymentDetails ed
    ON  ed.EMPPAY_CODE = e.EMP_CODE
WHERE e.EMP_PASSPORT_NO = 'PA2392740';
GO

-- ============================================================
-- STEP 2: Update Employee table
-- ============================================================
UPDATE Employee
SET    EMP_ROLE      = 'Guard',
       LASTUPDATE    = GETDATE(),
       LastUpdatedBy = 'MIGRATION'
WHERE  EMP_PASSPORT_NO = 'PA2392740'
  AND  EMP_ROLE        = 'Others';   -- safety: only if currently Others

SELECT @@ROWCOUNT AS [Employee rows updated];
GO

-- ============================================================
-- STEP 3: Update EmployeeHistory (open row only)
-- ============================================================
UPDATE eh
SET    eh.EMP_ROLE      = 'Guard',
       eh.LASTUPDATE    = GETDATE(),
       eh.LastUpdatedBy = 'MIGRATION'
FROM   EmployeeHistory eh
INNER JOIN Employee e
    ON  e.EMP_ID           = eh.EMP_ID
WHERE  e.EMP_PASSPORT_NO   = 'PA2392740'
  AND  eh.Emp_EndDate      IS NULL      -- open / currently-active row
  AND  eh.EMP_ROLE         = 'Others';  -- safety check

SELECT @@ROWCOUNT AS [EmployeeHistory rows updated];
GO

-- ============================================================
-- STEP 4: Update EmploymentDetails
-- ============================================================
UPDATE ed
SET    ed.EMPPAY_CATEGORY = 'Guard',
       ed.LASTUPDATE      = GETDATE(),
       ed.LastUpdatedBy   = 'MIGRATION'
FROM   EmploymentDetails ed
INNER JOIN Employee e
    ON  e.EMP_CODE         = ed.EMPPAY_CODE
WHERE  e.EMP_PASSPORT_NO   = 'PA2392740'
  AND  ed.EMPPAY_CATEGORY  = 'Others';  -- safety check

SELECT @@ROWCOUNT AS [EmploymentDetails rows updated];
GO

-- ============================================================
-- STEP 5: Post-update verification — all 3 should show 'Guard'
-- ============================================================
SELECT
    e.EMP_ID,
    e.EMP_CODE,
    e.EMP_NAME,
    e.EMP_PASSPORT_NO,
    e.EMP_ROLE                      AS [Employee.EMP_ROLE],
    eh.EMP_ROLE                     AS [EmployeeHistory.EMP_ROLE],
    ed.EMPPAY_CATEGORY              AS [EmploymentDetails.EMPPAY_CATEGORY]
FROM Employee e
LEFT JOIN EmployeeHistory eh
    ON  eh.EMP_ID      = e.EMP_ID
    AND eh.Emp_EndDate IS NULL
LEFT JOIN EmploymentDetails ed
    ON  ed.EMPPAY_CODE = e.EMP_CODE
WHERE e.EMP_PASSPORT_NO = 'PA2392740';
GO
