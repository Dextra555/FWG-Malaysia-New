-- ============================================================
-- Migration: Add ClientID to StockRequestDetail
-- Adds per-line client tracking to Branch Stock Requests
-- ============================================================

PRINT '============================================================';
PRINT 'BSR Detail ClientID Migration - Start';
PRINT '============================================================';

-- Add ClientID column to StockRequestDetail
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'StockRequestDetail' AND COLUMN_NAME = 'ClientID'
)
BEGIN
    ALTER TABLE StockRequestDetail
        ADD ClientID INT NULL;

    ALTER TABLE StockRequestDetail
        ADD CONSTRAINT FK_SRD_Client FOREIGN KEY (ClientID) REFERENCES ClientMaster(ID);

    CREATE INDEX IX_SRD_ClientID ON StockRequestDetail(ClientID);

    PRINT 'StockRequestDetail: ClientID column added.';
END
ELSE
    PRINT 'StockRequestDetail: ClientID already exists. Skipping.';

PRINT '============================================================';
PRINT 'BSR Detail ClientID Migration - Complete.';
PRINT '============================================================';
