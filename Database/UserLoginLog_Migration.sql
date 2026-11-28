-- =============================================================
-- UserLoginLog_Migration.sql
-- Login / Logout history table
-- Run once on OBMSUATNEW
-- =============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[OBMSUserLoginLog]')
      AND type = N'U'
)
BEGIN
    CREATE TABLE [dbo].[OBMSUserLoginLog] (
        [ID]        INT            IDENTITY(1,1) NOT NULL,
        [UserName]  NVARCHAR(200)  NOT NULL,
        [LoginTime] DATETIME       NOT NULL DEFAULT GETDATE(),
        [LogoutTime] DATETIME      NULL,
        [IsActive]  BIT            NOT NULL DEFAULT 1,
        CONSTRAINT [PK_OBMSUserLoginLog] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
    PRINT 'OBMSUserLoginLog table created.';
END
ELSE
BEGIN
    PRINT 'OBMSUserLoginLog table already exists — skipped.';
END
