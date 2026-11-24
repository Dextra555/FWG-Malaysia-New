namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload for creating or updating an AssetMaster record.
    /// ID = 0 means new; ID > 0 means update.
    /// AssetCode is generated server-side on new records.
    /// </summary>
    public class AssetMasterRequestDto
    {
        public int ID { get; set; }

        /// <summary>Server-generated on new (AST0001). Preserved on edit.</summary>
        public string? AssetCode { get; set; }

        public string? Name { get; set; }

        /// <summary>Fixed Asset | Liquid Assets | Current Liabilities | Long Term Liabilities</summary>
        public string? AssetType { get; set; }

        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public string? ModelNo { get; set; }
        public string? BrandName { get; set; }
        public int? SupplierID { get; set; }

        /// <summary>FK to AssetCategory table (nullable)</summary>
        public int? CategoryID { get; set; }

        /// <summary>Branch code (free text — existing field)</summary>
        public string? Branch { get; set; }

        public double? PurchaseAmount { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }
        public string? AssetCondition { get; set; }
        public string? Remarks { get; set; }

        /// <summary>Supplier's invoice number for this asset purchase (reference).</summary>
        public string? SupplierInvoiceNo { get; set; }

        // ── Deposit & Installment fields ─────────────────────────────────────

        /// <summary>Reference / display field only — not tracked via payments.</summary>
        public decimal? DepositAmount { get; set; }

        /// <summary>FULL (default) or MONTHLY</summary>
        public string? PaymentTermType { get; set; }

        /// <summary>First month of the installment schedule (MONTHLY mode only).</summary>
        public DateTime? InstallmentStartDate { get; set; }

        /// <summary>Amount to pay per month (MONTHLY mode only).</summary>
        public decimal? MonthlyInstallmentAmount { get; set; }

        /// <summary>
        /// Total installment months.
        /// Calculated by UI: CEILING((PurchaseAmount - DepositAmount) / MonthlyInstallmentAmount).
        /// Stored here for reference; server re-calculates before generating rows.
        /// </summary>
        public int? InstallmentMonths { get; set; }

        // ── Preclosing ────────────────────────────────────────────────────────
        public bool IsPreClosing { get; set; } = false;
        public DateTime? PreClosingDate { get; set; }
        public decimal? PreClosingAmount { get; set; }

        /// <summary>
        /// When true, only IsPreClosing/PreClosingDate/PreClosingAmount are saved.
        /// Used when asset is payment-locked but user enables preclosing.
        /// </summary>
        public bool IsPreClosingOnly { get; set; } = false;

        // ── Vehicle-specific fields ───────────────────────────────────────────
        public DateTime? RoadTaxRenewalDate { get; set; }
        public DateTime? InsuranceRenewalDate { get; set; }

        // ── Office-specific fields ────────────────────────────────────────────
        public DateTime? ServiceLastDate { get; set; }

        public DateTime? LastUpdatedDate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
