-- View: Employee Visa and Passport Expiry Reminder
-- Shows employees whose Visa expires in 4 months or Passport expires in 18 months

IF EXISTS (SELECT * FROM sys.views WHERE name = 'VWEmployeeExpiryReminder')
    DROP VIEW VWEmployeeExpiryReminder;
GO

CREATE VIEW VWEmployeeExpiryReminder AS
SELECT 
    E.EMP_ID,
    E.EMP_CODE,
    E.EMP_NAME,
    E.EMP_BRANCH_CODE,
    E.EMP_PASSPORT_NO,
    E.EMP_IC_NEW,
    E.EMP_CITIZEN, -- 0=Malaysian, 1=Foreigner, 2=PR
    E.VisaExpiryDate,
    E.PassportExpiryDate,
    ED.EMPPAY_DATE_JOINED,
    ED.EMPPAY_CATEGORY,
    ED.EMPPAY_DATE_RESIGNED,
    
    -- Calculate days until expiry
    CASE 
        WHEN E.VisaExpiryDate IS NOT NULL 
        THEN DATEDIFF(DAY, GETDATE(), E.VisaExpiryDate)
        ELSE NULL
    END AS DaysToVisaExpiry,
    
    CASE 
        WHEN E.PassportExpiryDate IS NOT NULL 
        THEN DATEDIFF(DAY, GETDATE(), E.PassportExpiryDate)
        ELSE NULL
    END AS DaysToPassportExpiry,
    
    -- Flag which document is expiring
    CASE
        WHEN E.VisaExpiryDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), E.VisaExpiryDate) <= 120 AND DATEDIFF(DAY, GETDATE(), E.VisaExpiryDate) >= 0
        THEN 'Visa Expiry'
        WHEN E.PassportExpiryDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), E.PassportExpiryDate) <= 540 AND DATEDIFF(DAY, GETDATE(), E.PassportExpiryDate) >= 0
        THEN 'Passport Expiry'
        ELSE 'Both Expiring'
    END AS ExpiryType

FROM Employee E
LEFT JOIN EmploymentDetails ED ON E.EMP_CODE = ED.EMPPAY_CODE
WHERE 
    E.EMP_CITIZEN = 1  -- Foreigners only
    AND (ED.EMPPAY_DATE_RESIGNED IS NULL OR ED.EMPPAY_DATE_RESIGNED > GETDATE())  -- Active employees only
    AND (
        -- Visa expiring in next 4 months (120 days)
        (E.VisaExpiryDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), E.VisaExpiryDate) BETWEEN 0 AND 120)
        OR
        -- Passport expiring in next 18 months (540 days)
        (E.PassportExpiryDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), E.PassportExpiryDate) BETWEEN 0 AND 540)
    );
GO
