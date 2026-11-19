-- ============================================================
-- Sync EmployeeHistory & EmploymentDetails after
-- EMP_ROLE = 'Others' update via direct SQL (Oct 2026)
--
-- Problem : Employee table was updated directly via SQL
--           (OtherGuards_EmpRole_Update_ByPassport.sql).
--           EmployeeHistory still has the old EMP_ROLE,
--           and EmploymentDetails.EMPPAY_CATEGORY may not
--           reflect "Others" either.
--
-- What this script does:
--   STEP 1 : Preview — show all affected employees
--   STEP 2 : Update EMP_ROLE in EmployeeHistory (open rows only,
--            i.e. Emp_EndDate IS NULL = currently active snapshot)
--   STEP 3 : Update EMPPAY_CATEGORY in EmploymentDetails to
--            'Others' where it does not already match
--   STEP 4 : Post-update verification
-- ============================================================

-- Passport list (same 89 employees from OtherGuards_EmpRole_Update_ByPassport.sql)
DECLARE @PassportList TABLE (PassportNo NVARCHAR(20));
INSERT INTO @PassportList VALUES
    ('PA0165234'), ('11342748'),  ('PA1231441'), ('PA2002428'), ('PA2564328'),
    ('PA3660886'), ('10945832'),  ('12366238'),  ('PA0764361'), ('10739164'),
    ('11842688'),  ('BA0062610'), ('12450242'),  ('09780478'),  ('PA0501499'),
    ('PA0799712'), ('08260119'),  ('PA0277663'), ('PA1181118'), ('PA2690201'),
    ('PA0889133'), ('PA1006822'), ('BA0076219'), ('PA3964870'), ('PA4448919'),
    ('PA1335940'), ('PA4420396'), ('PA4383786'), ('PA0254708'), ('PA3996354'),
    ('PA2410082'), ('PA4079643'), ('PA1729568'), ('PA1601259'), ('PA3199120'),
    ('PA1379429'), ('PA4559252'), ('PA2088271'), ('PA2304953'), ('12253527'),
    ('PA1516590'), ('PA4417105'), ('PA4399708'), ('PA4322201'), ('PA2721501'),
    ('PA4518584'), ('PA4435135'), ('PA2882420'), ('11595265'),  ('PA3735877'),
    ('PA3357708'), ('PA4560156'), ('PA4399992'), ('PA0882782'), ('11949900'),
    ('PA4248712'), ('PA3266444'), ('11085782'),  ('PA1571109'), ('PA4785965'),
    ('PA3101856'), ('PA4556821'), ('12489330'),  ('PA0623092'), ('PA3741758'),
    ('PA0253094'), ('PA4646880'), ('11333126'),  ('PA3124287'), ('PA1249550'),
    ('PA2392740'), ('PA4039916'), ('PA3280892'), ('11940160'),  ('PA1766368'),
    ('12221687'),  ('BA0218367'), ('PA1965669'), ('PA0603734'), ('12362556'),
    ('PA1544145'), ('PA0326525'), ('12486595'),  ('PA4669863'), ('PA3034318'),
    ('PA0856141'), ('PA0849948'), ('PA0138405'), ('PA4271383');

-- ============================================================
-- STEP 1 : Preview — verify before updating
-- ============================================================
SELECT
    e.EMP_ID,
    e.EMP_CODE,
    e.EMP_NAME,
    e.EMP_PASSPORT_NO,
    e.EMP_ROLE                          AS [Employee.EMP_ROLE],
    eh.EMP_ROLE                         AS [EmployeeHistory.EMP_ROLE (open row)],
    ed.EMPPAY_CATEGORY                  AS [EmploymentDetails.EMPPAY_CATEGORY]
FROM Employee e
INNER JOIN @PassportList pl ON pl.PassportNo = e.EMP_PASSPORT_NO
LEFT JOIN EmployeeHistory eh
    ON eh.EMP_ID = e.EMP_ID
   AND eh.Emp_EndDate IS NULL          -- open / currently-active history row
LEFT JOIN EmploymentDetails ed
    ON ed.EMPPAY_CODE = e.EMP_CODE
ORDER BY e.EMP_NAME;
GO

-- ============================================================
-- STEP 2 : Update EMP_ROLE in EmployeeHistory (open rows only)
-- ============================================================
DECLARE @PassportList TABLE (PassportNo NVARCHAR(20));
INSERT INTO @PassportList VALUES
    ('PA0165234'), ('11342748'),  ('PA1231441'), ('PA2002428'), ('PA2564328'),
    ('PA3660886'), ('10945832'),  ('12366238'),  ('PA0764361'), ('10739164'),
    ('11842688'),  ('BA0062610'), ('12450242'),  ('09780478'),  ('PA0501499'),
    ('PA0799712'), ('08260119'),  ('PA0277663'), ('PA1181118'), ('PA2690201'),
    ('PA0889133'), ('PA1006822'), ('BA0076219'), ('PA3964870'), ('PA4448919'),
    ('PA1335940'), ('PA4420396'), ('PA4383786'), ('PA0254708'), ('PA3996354'),
    ('PA2410082'), ('PA4079643'), ('PA1729568'), ('PA1601259'), ('PA3199120'),
    ('PA1379429'), ('PA4559252'), ('PA2088271'), ('PA2304953'), ('12253527'),
    ('PA1516590'), ('PA4417105'), ('PA4399708'), ('PA4322201'), ('PA2721501'),
    ('PA4518584'), ('PA4435135'), ('PA2882420'), ('11595265'),  ('PA3735877'),
    ('PA3357708'), ('PA4560156'), ('PA4399992'), ('PA0882782'), ('11949900'),
    ('PA4248712'), ('PA3266444'), ('11085782'),  ('PA1571109'), ('PA4785965'),
    ('PA3101856'), ('PA4556821'), ('12489330'),  ('PA0623092'), ('PA3741758'),
    ('PA0253094'), ('PA4646880'), ('11333126'),  ('PA3124287'), ('PA1249550'),
    ('PA2392740'), ('PA4039916'), ('PA3280892'), ('11940160'),  ('PA1766368'),
    ('12221687'),  ('BA0218367'), ('PA1965669'), ('PA0603734'), ('12362556'),
    ('PA1544145'), ('PA0326525'), ('12486595'),  ('PA4669863'), ('PA3034318'),
    ('PA0856141'), ('PA0849948'), ('PA0138405'), ('PA4271383');

UPDATE eh
SET    eh.EMP_ROLE      = 'Others',
       eh.LASTUPDATE    = GETDATE(),
       eh.LastUpdatedBy = 'MIGRATION'
FROM   EmployeeHistory eh
INNER JOIN Employee e ON e.EMP_ID = eh.EMP_ID
INNER JOIN @PassportList pl ON pl.PassportNo = e.EMP_PASSPORT_NO
WHERE  eh.Emp_EndDate IS NULL          -- only the currently-active open row
  AND  eh.EMP_ROLE   <> 'Others';     -- skip rows already correct

SELECT @@ROWCOUNT AS [EmployeeHistory rows updated];
GO

-- ============================================================
-- STEP 3 : Update EMPPAY_CATEGORY in EmploymentDetails
--          (only if it still shows the old category)
-- ============================================================
DECLARE @PassportList TABLE (PassportNo NVARCHAR(20));
INSERT INTO @PassportList VALUES
    ('PA0165234'), ('11342748'),  ('PA1231441'), ('PA2002428'), ('PA2564328'),
    ('PA3660886'), ('10945832'),  ('12366238'),  ('PA0764361'), ('10739164'),
    ('11842688'),  ('BA0062610'), ('12450242'),  ('09780478'),  ('PA0501499'),
    ('PA0799712'), ('08260119'),  ('PA0277663'), ('PA1181118'), ('PA2690201'),
    ('PA0889133'), ('PA1006822'), ('BA0076219'), ('PA3964870'), ('PA4448919'),
    ('PA1335940'), ('PA4420396'), ('PA4383786'), ('PA0254708'), ('PA3996354'),
    ('PA2410082'), ('PA4079643'), ('PA1729568'), ('PA1601259'), ('PA3199120'),
    ('PA1379429'), ('PA4559252'), ('PA2088271'), ('PA2304953'), ('12253527'),
    ('PA1516590'), ('PA4417105'), ('PA4399708'), ('PA4322201'), ('PA2721501'),
    ('PA4518584'), ('PA4435135'), ('PA2882420'), ('11595265'),  ('PA3735877'),
    ('PA3357708'), ('PA4560156'), ('PA4399992'), ('PA0882782'), ('11949900'),
    ('PA4248712'), ('PA3266444'), ('11085782'),  ('PA1571109'), ('PA4785965'),
    ('PA3101856'), ('PA4556821'), ('12489330'),  ('PA0623092'), ('PA3741758'),
    ('PA0253094'), ('PA4646880'), ('11333126'),  ('PA3124287'), ('PA1249550'),
    ('PA2392740'), ('PA4039916'), ('PA3280892'), ('11940160'),  ('PA1766368'),
    ('12221687'),  ('BA0218367'), ('PA1965669'), ('PA0603734'), ('12362556'),
    ('PA1544145'), ('PA0326525'), ('12486595'),  ('PA4669863'), ('PA3034318'),
    ('PA0856141'), ('PA0849948'), ('PA0138405'), ('PA4271383');

UPDATE ed
SET    ed.EMPPAY_CATEGORY = 'Others',
       ed.LASTUPDATE      = GETDATE(),
       ed.LastUpdatedBy   = 'MIGRATION'
FROM   EmploymentDetails ed
INNER JOIN Employee e ON e.EMP_CODE = ed.EMPPAY_CODE
INNER JOIN @PassportList pl ON pl.PassportNo = e.EMP_PASSPORT_NO
WHERE  ed.EMPPAY_CATEGORY <> 'Others';   -- skip rows already correct

SELECT @@ROWCOUNT AS [EmploymentDetails rows updated];
GO

-- ============================================================
-- STEP 4 : Post-update verification — all 3 tables should show 'Others'
-- ============================================================
DECLARE @PassportList TABLE (PassportNo NVARCHAR(20));
INSERT INTO @PassportList VALUES
    ('PA0165234'), ('11342748'),  ('PA1231441'), ('PA2002428'), ('PA2564328'),
    ('PA3660886'), ('10945832'),  ('12366238'),  ('PA0764361'), ('10739164'),
    ('11842688'),  ('BA0062610'), ('12450242'),  ('09780478'),  ('PA0501499'),
    ('PA0799712'), ('08260119'),  ('PA0277663'), ('PA1181118'), ('PA2690201'),
    ('PA0889133'), ('PA1006822'), ('BA0076219'), ('PA3964870'), ('PA4448919'),
    ('PA1335940'), ('PA4420396'), ('PA4383786'), ('PA0254708'), ('PA3996354'),
    ('PA2410082'), ('PA4079643'), ('PA1729568'), ('PA1601259'), ('PA3199120'),
    ('PA1379429'), ('PA4559252'), ('PA2088271'), ('PA2304953'), ('12253527'),
    ('PA1516590'), ('PA4417105'), ('PA4399708'), ('PA4322201'), ('PA2721501'),
    ('PA4518584'), ('PA4435135'), ('PA2882420'), ('11595265'),  ('PA3735877'),
    ('PA3357708'), ('PA4560156'), ('PA4399992'), ('PA0882782'), ('11949900'),
    ('PA4248712'), ('PA3266444'), ('11085782'),  ('PA1571109'), ('PA4785965'),
    ('PA3101856'), ('PA4556821'), ('12489330'),  ('PA0623092'), ('PA3741758'),
    ('PA0253094'), ('PA4646880'), ('11333126'),  ('PA3124287'), ('PA1249550'),
    ('PA2392740'), ('PA4039916'), ('PA3280892'), ('11940160'),  ('PA1766368'),
    ('12221687'),  ('BA0218367'), ('PA1965669'), ('PA0603734'), ('12362556'),
    ('PA1544145'), ('PA0326525'), ('12486595'),  ('PA4669863'), ('PA3034318'),
    ('PA0856141'), ('PA0849948'), ('PA0138405'), ('PA4271383');

SELECT
    e.EMP_ID,
    e.EMP_CODE,
    e.EMP_NAME,
    e.EMP_PASSPORT_NO,
    e.EMP_ROLE                          AS [Employee.EMP_ROLE],
    eh.EMP_ROLE                         AS [EmployeeHistory.EMP_ROLE],
    ed.EMPPAY_CATEGORY                  AS [EmploymentDetails.EMPPAY_CATEGORY]
FROM Employee e
INNER JOIN @PassportList pl ON pl.PassportNo = e.EMP_PASSPORT_NO
LEFT JOIN EmployeeHistory eh
    ON eh.EMP_ID = e.EMP_ID
   AND eh.Emp_EndDate IS NULL
LEFT JOIN EmploymentDetails ed
    ON ed.EMPPAY_CODE = e.EMP_CODE
ORDER BY e.EMP_NAME;
GO
