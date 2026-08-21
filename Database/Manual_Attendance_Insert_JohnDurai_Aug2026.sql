-- =============================================================
-- Manual Attendance Insert - JohnDurai
-- Date    : 19-Aug-2026
-- Month   : August 2026 (Period)
-- By      : Manual Entry
-- =============================================================

-- STEP 1: Employee ID confirm pannunga
SELECT EMP_ID, EMP_NAME, EMP_CODE
FROM Employee
WHERE EMP_NAME LIKE '%johndurai%'
   OR EMP_NAME LIKE '%john durai%';

GO

-- =============================================================
-- STEP 2 + 3: Attendance header + Detail insert
-- =============================================================

DECLARE @EmployeeID     INT;
DECLARE @AttendanceID   INT;
DECLARE @Branch         NVARCHAR(50)  = NULL;        -- Branch name irundha set pannunga
DECLARE @Period         DATETIME      = '2026-08-01'; -- August 2026 period
DECLARE @EnteredBy      NVARCHAR(100) = 'MANUAL';

-- Get EmployeeID
SELECT @EmployeeID = EMP_ID
FROM Employee
WHERE EMP_NAME LIKE '%johndurai%'
   OR EMP_NAME LIKE '%john durai%';

IF @EmployeeID IS NULL
BEGIN
    PRINT 'ERROR: Employee "johndurai" not found! EMP_NAME check pannunga.';
    RETURN;
END

PRINT 'Employee found. EMP_ID = ' + CAST(@EmployeeID AS VARCHAR);

-- -------------------------------------------------------
-- Attendance header (August 2026) already irukka check
-- -------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM Attendance
    WHERE EmployeeID = @EmployeeID
      AND MONTH(Period) = 8
      AND YEAR(Period)  = 2026
)
BEGIN
    PRINT 'Attendance header already exists for August 2026 - reusing it.';
    
    SELECT @AttendanceID = ID
    FROM Attendance
    WHERE EmployeeID = @EmployeeID
      AND MONTH(Period) = 8
      AND YEAR(Period)  = 2026;
END
ELSE
BEGIN
    -- Insert Attendance header
    INSERT INTO Attendance (
        Period,
        Branch,
        EmployeeID,
        Shift2Type,
        Shift2Rate,
        AllowanceDeduction,
        SpecialAllowanceDeduction,
        Bonus,
        KPIDeduction,
        LastUpdate,
        LastUpdatedBy
    )
    VALUES (
        @Period,      -- Period   : 2026-08-01
        @Branch,      -- Branch   : NULL or branch name
        @EmployeeID,  -- EmployeeID
        0,            -- Shift2Type
        0.00,         -- Shift2Rate
        0.00,         -- AllowanceDeduction
        0.00,         -- SpecialAllowanceDeduction
        0.00,         -- Bonus
        0.00,         -- KPIDeduction
        GETDATE(),    -- LastUpdate
        @EnteredBy    -- LastUpdatedBy
    );

    SET @AttendanceID = SCOPE_IDENTITY();
    PRINT 'Attendance header inserted. AttendanceID = ' + CAST(@AttendanceID AS VARCHAR);
END

-- -------------------------------------------------------
-- AttendanceDetails - 19-Aug-2026 already irukka check
-- -------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM AttendanceDetails
    WHERE AttendanceID = @AttendanceID
      AND CAST(AttendanceDate AS DATE) = '2026-08-19'
)
BEGIN
    PRINT 'WARNING: AttendanceDetails for 19-Aug-2026 already exists! Skipping insert.';
END
ELSE
BEGIN
    INSERT INTO AttendanceDetails (
        AttendanceID,
        AttendanceDate,
        Client,
        TimeStart,
        TimeEnd,
        OTClient,
        OTTimeStart,
        OTTimeEnd,
        Type,
        LastUpdate,
        LastUpdatedBy
    )
    VALUES (
        @AttendanceID,          -- AttendanceID
        '2026-08-19',           -- AttendanceDate : 19-Aug-2026
        NULL,                   -- Client         : set pannunga if needed
        '2026-08-19 08:00:00',  -- TimeStart       : adjust pannunga
        '2026-08-19 17:00:00',  -- TimeEnd         : adjust pannunga
        NULL,                   -- OTClient
        NULL,                   -- OTTimeStart
        NULL,                   -- OTTimeEnd
        1,                      -- Type : 1 = Present
        GETDATE(),              -- LastUpdate
        @EnteredBy              -- LastUpdatedBy
    );

    PRINT 'AttendanceDetails inserted: JohnDurai - 19-Aug-2026';
END

GO

-- =============================================================
-- STEP 4: Verify
-- =============================================================
SELECT
    e.EMP_NAME          AS EmployeeName,
    e.EMP_CODE          AS EmployeeCode,
    a.Period,
    a.Branch,
    ad.AttendanceDate,
    ad.TimeStart,
    ad.TimeEnd,
    ad.Type,
    ad.LastUpdatedBy
FROM AttendanceDetails  ad
JOIN Attendance         a  ON a.ID     = ad.AttendanceID
JOIN Employee           e  ON e.EMP_ID = a.EmployeeID
WHERE (e.EMP_NAME LIKE '%johndurai%' OR e.EMP_NAME LIKE '%john durai%')
  AND CAST(ad.AttendanceDate AS DATE) = '2026-08-19'
ORDER BY ad.AttendanceDate DESC;
