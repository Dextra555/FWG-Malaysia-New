-- Fix: Add Pending Approvals and Approval History permissions for superadmin and valli

IF NOT EXISTS (SELECT 1 FROM OBMSPermissions WHERE Name='superadmin' AND ScreenName='Pending Approvals')
  INSERT INTO OBMSPermissions (Name, ScreenName, [Create], [Read], [Update], [Delete], LastUpdatedBy)
  VALUES ('superadmin','Pending Approvals',0,1,1,0,'system');

IF NOT EXISTS (SELECT 1 FROM OBMSPermissions WHERE Name='superadmin' AND ScreenName='Approval History')
  INSERT INTO OBMSPermissions (Name, ScreenName, [Create], [Read], [Update], [Delete], LastUpdatedBy)
  VALUES ('superadmin','Approval History',0,1,0,0,'system');

IF NOT EXISTS (SELECT 1 FROM OBMSPermissions WHERE Name='valli' AND ScreenName='Pending Approvals')
  INSERT INTO OBMSPermissions (Name, ScreenName, [Create], [Read], [Update], [Delete], LastUpdatedBy)
  VALUES ('valli','Pending Approvals',0,1,1,0,'system');

IF NOT EXISTS (SELECT 1 FROM OBMSPermissions WHERE Name='valli' AND ScreenName='Approval History')
  INSERT INTO OBMSPermissions (Name, ScreenName, [Create], [Read], [Update], [Delete], LastUpdatedBy)
  VALUES ('valli','Approval History',0,1,0,0,'system');

SELECT Name, ScreenName, [Read], [Update] FROM OBMSPermissions
WHERE ScreenName IN ('Pending Approvals','Approval History')
ORDER BY Name;
