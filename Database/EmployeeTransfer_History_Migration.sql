-- ============================================================
-- Employee Transfer History Migration
-- Description: Creates EmployeeSalaryDetailHistory and
--              EmploymentDetailsHistory tables, and adds
--              Emp_StartDate / Emp_EndDate columns to
--              the existing EmployeeHistory table.
--
-- IMPORTANT: Each section is separated by GO so SQL Server
--            compiles each batch after the previous DDL
--            has been committed. This prevents "Invalid
--            column name" errors from the back-fill UPDATE.
-- ============================================================

-- -------------------------------------------------------
-- 1a. Add Emp_StartDate to EmployeeHistory
-- -------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.EmployeeHistory')
      AND name = 'Emp_StartDate'
)
BEGIN
    ALTER TABLE dbo.EmployeeHistory
    ADD Emp_StartDate DATE NULL;
    PRINT 'Column Emp_StartDate added to EmployeeHistory.';
END
ELSE
    PRINT 'Column Emp_StartDate already exists on EmployeeHistory.';
GO

-- -------------------------------------------------------
-- 1b. Add Emp_EndDate to EmployeeHistory
-- -------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.EmployeeHistory')
      AND name = 'Emp_EndDate'
)
BEGIN
    ALTER TABLE dbo.EmployeeHistory
    ADD Emp_EndDate DATE NULL;
    PRINT 'Column Emp_EndDate added to EmployeeHistory.';
END
ELSE
    PRINT 'Column Emp_EndDate already exists on EmployeeHistory.';
GO

-- -------------------------------------------------------
-- 2. Create EmployeeSalaryDetailHistory
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeSalaryDetailHistory')
BEGIN
    CREATE TABLE dbo.EmployeeSalaryDetailHistory (
        EMPFL_HISTORY_ID     INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        EMP_ID               INT            NOT NULL,
        EMPFL_CODE           NVARCHAR(50)   NOT NULL,
        EMPFL_BRANCHCODE     NVARCHAR(20)   NOT NULL,
        SplitSalaryPayment   BIT            NULL,
        EMPFL_BANK           NVARCHAR(50)   NULL,
        EMPFL_BK_ACCNO       NVARCHAR(25)   NULL,
        EMPFL_2ndBank        NVARCHAR(50)   NULL,
        EMPFL_2ndBK_ACCNO    NVARCHAR(25)   NULL,
        EMPFL_TAX_NO         NVARCHAR(25)   NULL,
        EMPFL_EPFNO          NVARCHAR(25)   NULL,
        EMPFL_EPF8Pa         BIT            NULL,
        EMPFL_SOSCO_NO       NVARCHAR(25)   NULL,
        SKBBK                BIT            NULL,
        EPFDETECT            BIT            NOT NULL DEFAULT(0),
        PAYMODE              NVARCHAR(20)   NOT NULL,
        PAYMODE2             NVARCHAR(20)   NULL,
        SOCSODETECT          BIT            NOT NULL DEFAULT(0),
        TMPGUARD             BIT            NOT NULL DEFAULT(0),
        DETECTBYND55         BIT            NOT NULL DEFAULT(0),
        INCOMETAXDETECT      BIT            NULL,
        EMP_SP_TEL_NO        NVARCHAR(20)   NULL,
        LASTUPDATE           DATETIME       NOT NULL,
        LastUpdatedBy        NVARCHAR(20)   NULL,
        Emp_StartDate        DATE           NULL,
        Emp_EndDate          DATE           NULL
    );
    PRINT 'Table EmployeeSalaryDetailHistory created.';
END
ELSE
    PRINT 'Table EmployeeSalaryDetailHistory already exists.';
GO

-- -------------------------------------------------------
-- 3. Create EmploymentDetailsHistory
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmploymentDetailsHistory')
BEGIN
    CREATE TABLE dbo.EmploymentDetailsHistory (
        EMPPAY_HISTORY_ID                   INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        EMP_ID                              INT             NOT NULL,
        EMPPAY_CODE                         NVARCHAR(50)    NOT NULL,
        EMPPAY_BRANCHCODE                   NVARCHAR(20)    NOT NULL,
        EMPPAY_JOB_TITLE                    NVARCHAR(50)    NULL,
        EMPPAY_CATEGORY                     NVARCHAR(50)    NULL,
        EMPPAY_DATE_JOINED                  DATE            NULL,
        EMPPAY_DATE_CONFIRM                 DATE            NULL,
        EMPPAY_DATE_PROMOTION               DATE            NULL,
        EMPPAY_DATE_RESIGNED                DATE            NULL,
        EMPPAY_BASIC_RATE                   FLOAT           NOT NULL DEFAULT(0),
        SALARYLAB                           DECIMAL(18,2)   NOT NULL DEFAULT(0),
        ATTENDANCEALLOWANCE                 DECIMAL(18,2)   NULL,
        NewStructureATTENDANCEALLOWANCE     DECIMAL(18,2)   NOT NULL DEFAULT(0),
        SpecialAllowance                    DECIMAL(18,2)   NOT NULL DEFAULT(0),
        AttendanceAllowanceWorkingDays      DECIMAL(18,2)   NULL,
        AttendanceAllowanceFollowCalendar   NVARCHAR(1)     NULL,
        KPI                                 DECIMAL(18,2)   NULL,
        LASTUPDATE                          DATETIME        NOT NULL,
        LastUpdatedBy                       NVARCHAR(20)    NULL,
        Emp_StartDate                       DATE            NULL,
        Emp_EndDate                         DATE            NULL
    );
    PRINT 'Table EmploymentDetailsHistory created.';
END
ELSE
    PRINT 'Table EmploymentDetailsHistory already exists.';
GO

-- -------------------------------------------------------
-- 4. Back-fill Emp_StartDate on existing EmployeeHistory rows
--    This runs in a NEW batch (after GO) so the column
--    definitely exists by the time this UPDATE executes.
-- -------------------------------------------------------
UPDATE dbo.EmployeeHistory
SET    Emp_StartDate = CAST(EMPPAY_DATE_JOINED AS DATE)
WHERE  Emp_StartDate IS NULL
  AND  EMPPAY_DATE_JOINED IS NOT NULL;

PRINT 'Back-fill complete for existing EmployeeHistory rows.';
GO
