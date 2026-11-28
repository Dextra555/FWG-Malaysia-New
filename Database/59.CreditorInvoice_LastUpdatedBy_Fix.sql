-- ============================================================
-- Migration 59 : CreditorInvoice.LastUpdatedBy size fix
--
-- Problem : Column was VARCHAR(20) — usernames longer than 20
--           characters caused SqlException on SaveAsset,
--           returning HTTP 500 "Unable to save the asset".
--
-- Fix     : Widen to VARCHAR(100) — matches all other
--           LastUpdatedBy columns in the system.
--
-- Safe to run multiple times (ALTER COLUMN is idempotent).
-- Target  : OBMSUATNEW + FWGServerDB
-- ============================================================

PRINT 'Migration 59: CreditorInvoice.LastUpdatedBy size fix';

ALTER TABLE CreditorInvoice
    ALTER COLUMN LastUpdatedBy VARCHAR(100);

PRINT 'CreditorInvoice.LastUpdatedBy → VARCHAR(100) done.';
