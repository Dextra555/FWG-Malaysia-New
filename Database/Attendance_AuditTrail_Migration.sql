-- =============================================================
-- Attendance Audit Trail Migration
-- Creates Attendance_AuditTrail and AttendanceDetails_AuditTrail
-- Run once on the target database.
-- =============================================================

-- 1. Attendance_AuditTrail
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'Attendance_AuditTrail'
)
BEGIN
    CREATE TABLE [dbo].[Attendance_AuditTrail] (
        [AuditID]                     INT             IDENTITY(1,1)  NOT NULL,
        [AttendanceID]                INT             NOT NULL,
        [Period]                      DATETIME        NOT NULL,
        [Branch]                      NVARCHAR(50)    NULL,
        [EmployeeID]                  INT             NOT NULL,
        [Shift2Type]                  INT             NOT NULL,
        [Shift2Rate]                  DECIMAL(18,2)   NOT NULL,
        [AllowanceDeduction]          DECIMAL(18,2)   NOT NULL,
        [SpecialAllowanceDeduction]   DECIMAL(18,2)   NOT NULL,
        [Bonus]                       DECIMAL(18,2)   NOT NULL,
        [KPIDeduction]                DECIMAL(18,2)   NULL,
        [LastUpdate]                  DATETIME        NOT NULL,
        [LastUpdatedBy]               NVARCHAR(100)   NULL,
        CONSTRAINT [PK_Attendance_AuditTrail] PRIMARY KEY CLUSTERED ([AuditID] ASC)
    );

    PRINT 'Attendance_AuditTrail table created.';
END
ELSE
BEGIN
    PRINT 'Attendance_AuditTrail already exists — skipped.';
END
GO

-- 2. AttendanceDetails_AuditTrail
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'AttendanceDetails_AuditTrail'
)
BEGIN
    CREATE TABLE [dbo].[AttendanceDetails_AuditTrail] (
        [AuditID]               INT             IDENTITY(1,1)  NOT NULL,
        [AttendanceDetailsID]   INT             NOT NULL,
        [AttendanceID]          INT             NOT NULL,
        [AttendanceDate]        DATETIME        NOT NULL,
        [Client]                NVARCHAR(50)    NULL,
        [TimeStart]             DATETIME        NULL,
        [TimeEnd]               DATETIME        NULL,
        [OTClient]              NVARCHAR(50)    NULL,
        [OTTimeStart]           DATETIME        NULL,
        [OTTimeEnd]             DATETIME        NULL,
        [Type]                  INT             NOT NULL,
        [LastUpdate]            DATETIME        NOT NULL,
        [LastUpdatedBy]         NVARCHAR(100)   NULL,
        CONSTRAINT [PK_AttendanceDetails_AuditTrail] PRIMARY KEY CLUSTERED ([AuditID] ASC)
    );

    PRINT 'AttendanceDetails_AuditTrail table created.';
END
ELSE
BEGIN
    PRINT 'AttendanceDetails_AuditTrail already exists — skipped.';
END
GO
