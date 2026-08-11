-- ============================================================
-- Employee Transfer History — Back-fill Script
-- Purpose : Populate EmployeeHistory, EmployeeSalaryDetailHistory,
--           and EmploymentDetailsHistory with initial rows for
--           ALL existing employees who do not yet have a
--           history record in those tables.
--
-- Run this ONCE after EmployeeTransfer_History_Migration.sql
-- has already been executed.
--
-- Logic:
--   Emp_StartDate = EMPPAY_DATE_JOINED  (join date as start)
--   Emp_EndDate   = NULL                (currently active)
-- ============================================================

-- -------------------------------------------------------
-- 1. Back-fill EmployeeHistory (Combined snapshot table)
--    Join Employee + EmploymentDetails + EmployeeSalaryDetails
--    where no history row exists yet for that EMP_ID.
-- -------------------------------------------------------
INSERT INTO dbo.EmployeeHistory (
    EMP_ID,
    EMP_ROLE,
    EMP_CODE,
    EMP_NAME,
    EMP_ADDRESS1,
    EMP_ADDRESS2,
    EMP_POST_CODE,
    EMP_TOWN,
    EMP_STATE,
    EMP_NATIONAL,
    EMP_PHONE,
    EMP_HGH_EDU,
    EM_WORK_EXP,
    EMP_DATE_OF_BIRTH,
    EMP_IC_OLD,
    EMP_IC_NEW,
    EMP_IC_COLOR,
    EMP_PASSPORT_NO,
    EMP_SEX,
    EMP_RACE,
    EMP_MARTIAL_STATUS,
    EMP_SPOUSE_NAME,
    EMP_SP_IC,
    EMP_NO_CHILD,
    EMP_SP_WORK,
    EMP_PER_NAME_CONTACT,
    EMP_CONTACT_ADDRESS1,
    EMP_CONTACT_ADDRESS2,
    EMP_CONTACT_POST_CODE,
    EMP_CONTACT_TOWN,
    EMP_CONTACT_STATE,
    EMP_CONTACT_TELEPHONE,
    EMP_BRANCH_CODE,
    OldBranch,
    TransferDate,
    HasTransfered,
    EMP_MOBILEPHONE,
    EMP_CITIZEN,
    EMP_CHECKLIST,
    EMP_CLIENT,
    NewSalaryStructure,
    KDNVetting,
    SalaryStructure1000_3h,
    VisaExpiryDate,
    PassportExpiryDate,
    FOMEMA_MedicalCheckupDate,
    FOMEMA_1stAppealDate,
    FOMEMA_2ndAppealDate,
    FOMEMA_NCDMonitoringDate,
    FOMEMA_ResultDate,
    EMPPAY_JOB_TITLE,
    EMPPAY_CATEGORY,
    EMPPAY_DATE_JOINED,
    EMPPAY_DATE_CONFIRM,
    EMPPAY_DATE_PROMOTION,
    EMPPAY_DATE_RESIGNED,
    EMPPAY_BASIC_RATE,
    SALARYLAB,
    ATTENDANCEALLOWANCE,
    NewStructureATTENDANCEALLOWANCE,
    SpecialAllowance,
    AttendanceAllowanceWorkingDays,
    AttendanceAllowanceFollowCalendar,
    EMPFL_BANK,
    EMPFL_BK_ACCNO,
    EMPFL_TAX_NO,
    EMPFL_EPFNO,
    EMPFL_EPF8Pa,
    EMPFL_SOSCO_NO,
    EPFDETECT,
    PAYMODE,
    SOCSODETECT,
    TMPGUARD,
    DETECTBYND55,
    INCOMETAXDETECT,
    LASTUPDATE,
    LastUpdatedBy,
    Emp_StartDate,
    Emp_EndDate
)
SELECT
    e.EMP_ID,
    e.EMP_ROLE,
    e.EMP_CODE,
    e.EMP_NAME,
    e.EMP_ADDRESS1,
    e.EMP_ADDRESS2,
    e.EMP_POST_CODE,
    e.EMP_TOWN,
    e.EMP_STATE,
    e.EMP_NATIONAL,
    e.EMP_PHONE,
    e.EMP_HGH_EDU,
    e.EM_WORK_EXP,
    e.EMP_DATE_OF_BIRTH,
    e.EMP_IC_OLD,
    e.EMP_IC_NEW,
    e.EMP_IC_COLOR,
    e.EMP_PASSPORT_NO,
    e.EMP_SEX,
    e.EMP_RACE,
    e.EMP_MARTIAL_STATUS,
    e.EMP_SPOUSE_NAME,
    e.EMP_SP_IC,
    e.EMP_NO_CHILD,
    e.EMP_SP_WORK,
    e.EMP_PER_NAME_CONTACT,
    e.EMP_CONTACT_ADDRESS1,
    e.EMP_CONTACT_ADDRESS2,
    e.EMP_CONTACT_POST_CODE,
    e.EMP_CONTACT_TOWN,
    e.EMP_CONTACT_STATE,
    e.EMP_CONTACT_TELEPHONE,
    e.EMP_BRANCH_CODE,
    e.OldBranch,
    e.TransferDate,
    e.HasTransfered,
    e.EMP_MOBILEPHONE,
    e.EMP_CITIZEN,
    e.EMP_CHECKLIST,
    e.EMP_CLIENT,
    e.NewSalaryStructure,
    e.KDNVetting,
    e.SalaryStructure1000_3h,
    e.VisaExpiryDate,
    e.PassportExpiryDate,
    e.FOMEMA_MedicalCheckupDate,
    e.FOMEMA_1stAppealDate,
    e.FOMEMA_2ndAppealDate,
    e.FOMEMA_NCDMonitoringDate,
    e.FOMEMA_ResultDate,
    -- EmploymentDetails columns
    ed.EMPPAY_JOB_TITLE,
    ed.EMPPAY_CATEGORY,
    ed.EMPPAY_DATE_JOINED,
    ed.EMPPAY_DATE_CONFIRM,
    ed.EMPPAY_DATE_PROMOTION,
    ed.EMPPAY_DATE_RESIGNED,
    ed.EMPPAY_BASIC_RATE,
    ed.SALARYLAB,
    ed.ATTENDANCEALLOWANCE,
    ed.NewStructureATTENDANCEALLOWANCE,
    ed.SpecialAllowance,
    ed.AttendanceAllowanceWorkingDays,
    ed.AttendanceAllowanceFollowCalendar,
    -- EmployeeSalaryDetails columns
    sd.EMPFL_BANK,
    sd.EMPFL_BK_ACCNO,
    sd.EMPFL_TAX_NO,
    sd.EMPFL_EPFNO,
    sd.EMPFL_EPF8Pa,
    sd.EMPFL_SOSCO_NO,
    sd.EPFDETECT,
    sd.PAYMODE,
    sd.SOCSODETECT,
    sd.TMPGUARD,
    sd.DETECTBYND55,
    sd.INCOMETAXDETECT,
    e.LASTUPDATE,
    e.LastUpdatedBy,
    -- Emp_StartDate = join date (when available), else today
    CAST(ISNULL(ed.EMPPAY_DATE_JOINED, GETDATE()) AS DATE),
    NULL   -- Emp_EndDate NULL = currently active
FROM dbo.Employee e
INNER JOIN dbo.EmploymentDetails ed
    ON ed.EMPPAY_CODE = e.EMP_CODE
INNER JOIN dbo.EmployeeSalaryDetails sd
    ON sd.EMPFL_CODE = e.EMP_CODE
WHERE NOT EXISTS (
    SELECT 1
    FROM   dbo.EmployeeHistory h
    WHERE  h.EMP_ID = e.EMP_ID
);

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' rows inserted into EmployeeHistory.';
GO

-- -------------------------------------------------------
-- 2. Back-fill EmploymentDetailsHistory
--    Join Employee → EmploymentDetails where no history row
--    exists yet for that EMP_ID.
-- -------------------------------------------------------
INSERT INTO dbo.EmploymentDetailsHistory (
    EMP_ID,
    EMPPAY_CODE,
    EMPPAY_BRANCHCODE,
    EMPPAY_JOB_TITLE,
    EMPPAY_CATEGORY,
    EMPPAY_DATE_JOINED,
    EMPPAY_DATE_CONFIRM,
    EMPPAY_DATE_PROMOTION,
    EMPPAY_DATE_RESIGNED,
    EMPPAY_BASIC_RATE,
    SALARYLAB,
    ATTENDANCEALLOWANCE,
    NewStructureATTENDANCEALLOWANCE,
    SpecialAllowance,
    AttendanceAllowanceWorkingDays,
    AttendanceAllowanceFollowCalendar,
    KPI,
    LASTUPDATE,
    LastUpdatedBy,
    Emp_StartDate,
    Emp_EndDate
)
SELECT
    e.EMP_ID,
    ed.EMPPAY_CODE,
    ed.EMPPAY_BRANCHCODE,
    ed.EMPPAY_JOB_TITLE,
    ed.EMPPAY_CATEGORY,
    ed.EMPPAY_DATE_JOINED,
    ed.EMPPAY_DATE_CONFIRM,
    ed.EMPPAY_DATE_PROMOTION,
    ed.EMPPAY_DATE_RESIGNED,
    ed.EMPPAY_BASIC_RATE,
    ed.SALARYLAB,
    ed.ATTENDANCEALLOWANCE,
    ed.NewStructureATTENDANCEALLOWANCE,
    ed.SpecialAllowance,
    ed.AttendanceAllowanceWorkingDays,
    ed.AttendanceAllowanceFollowCalendar,
    ed.KPI,
    ed.LASTUPDATE,
    ed.LastUpdatedBy,
    -- Emp_StartDate = join date (when available), else today
    CAST(ISNULL(ed.EMPPAY_DATE_JOINED, GETDATE()) AS DATE),
    NULL   -- Emp_EndDate NULL = currently active
FROM dbo.Employee e
INNER JOIN dbo.EmploymentDetails ed
    ON ed.EMPPAY_CODE = e.EMP_CODE
WHERE NOT EXISTS (
    SELECT 1
    FROM   dbo.EmploymentDetailsHistory h
    WHERE  h.EMP_ID = e.EMP_ID
);

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' rows inserted into EmploymentDetailsHistory.';
GO

-- -------------------------------------------------------
-- 3. Back-fill EmployeeSalaryDetailHistory
--    Join Employee → EmployeeSalaryDetails where no history
--    row exists yet for that EMP_ID.
-- -------------------------------------------------------
INSERT INTO dbo.EmployeeSalaryDetailHistory (
    EMP_ID,
    EMPFL_CODE,
    EMPFL_BRANCHCODE,
    SplitSalaryPayment,
    EMPFL_BANK,
    EMPFL_BK_ACCNO,
    EMPFL_2ndBank,
    EMPFL_2ndBK_ACCNO,
    EMPFL_TAX_NO,
    EMPFL_EPFNO,
    EMPFL_EPF8Pa,
    EMPFL_SOSCO_NO,
    SKBBK,
    EPFDETECT,
    PAYMODE,
    PAYMODE2,
    SOCSODETECT,
    TMPGUARD,
    DETECTBYND55,
    INCOMETAXDETECT,
    EMP_SP_TEL_NO,
    LASTUPDATE,
    LastUpdatedBy,
    Emp_StartDate,
    Emp_EndDate
)
SELECT
    e.EMP_ID,
    sd.EMPFL_CODE,
    sd.EMPFL_BRANCHCODE,
    sd.SplitSalaryPayment,
    sd.EMPFL_BANK,
    sd.EMPFL_BK_ACCNO,
    sd.EMPFL_2ndBank,
    sd.EMPFL_2ndBK_ACCNO,
    sd.EMPFL_TAX_NO,
    sd.EMPFL_EPFNO,
    sd.EMPFL_EPF8Pa,
    sd.EMPFL_SOSCO_NO,
    sd.SKBBK,
    sd.EPFDETECT,
    sd.PAYMODE,
    sd.PAYMODE2,
    sd.SOCSODETECT,
    sd.TMPGUARD,
    sd.DETECTBYND55,
    sd.INCOMETAXDETECT,
    sd.EMP_SP_TEL_NO,
    sd.LASTUPDATE,
    sd.LastUpdatedBy,
    -- Emp_StartDate = join date from EmploymentDetails (when available)
    CAST(ISNULL(ed.EMPPAY_DATE_JOINED, GETDATE()) AS DATE),
    NULL   -- Emp_EndDate NULL = currently active
FROM dbo.Employee e
INNER JOIN dbo.EmployeeSalaryDetails sd
    ON sd.EMPFL_CODE = e.EMP_CODE
LEFT JOIN dbo.EmploymentDetails ed
    ON ed.EMPPAY_CODE = e.EMP_CODE
WHERE NOT EXISTS (
    SELECT 1
    FROM   dbo.EmployeeSalaryDetailHistory h
    WHERE  h.EMP_ID = e.EMP_ID
);

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' rows inserted into EmployeeSalaryDetailHistory.';
GO

-- -------------------------------------------------------
-- 4. Verify counts
-- -------------------------------------------------------
SELECT
    'Employee'                   AS TableName,
    COUNT(*)                     AS TotalRows
FROM dbo.Employee
UNION ALL
SELECT 'EmploymentDetailsHistory',   COUNT(*) FROM dbo.EmploymentDetailsHistory
UNION ALL
SELECT 'EmployeeSalaryDetailHistory', COUNT(*) FROM dbo.EmployeeSalaryDetailHistory
UNION ALL
SELECT 'EmployeeHistory',            COUNT(*) FROM dbo.EmployeeHistory;
GO
