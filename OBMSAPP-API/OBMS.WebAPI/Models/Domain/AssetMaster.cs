using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("AssetMaster")]
    public class AssetMaster
    {
        public AssetMaster() { }

        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated code, e.g. AST0001</summary>
        public string? AssetCode { get; set; }

        public string? Name { get; set; }

        /// <summary>Trade type: Fixed Asset | Liquid Assets | Current Liabilities | Long Term Liabilities</summary>
        public string? AssetType { get; set; }

        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public string? ModelNo { get; set; }
        public string? BrandName { get; set; }

        /// <summary>FK to Suppliers table (nullable)</summary>
        public int? SupplierID { get; set; }

        /// <summary>FK to InventoryCategory table (nullable) — Purchase categories (Cat='P')</summary>
        public int? CategoryID { get; set; }

        /// <summary>Branch code (free text) — existing field kept as-is</summary>
        public string? Branch { get; set; }

        public double? PurchaseAmount { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }

        /// <summary>Supplier's own invoice number for this asset purchase.</summary>
        public string? SupplierInvoiceNo { get; set; }

        /// <summary>
        /// Current lifecycle status.
        /// Values: Available | Allocated | In_Transit | Under_Maintenance | Damaged | Disposed | Lost
        /// </summary>
        public string AssetStatus { get; set; } = "Available";

        /// <summary>Physical condition: Good | Fair | Poor | Damaged</summary>
        public string? AssetCondition { get; set; }

        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string? LastUpdatedBy { get; set; }

        // ── Location Tracking ─────────────────────────────────────────────────
        /// <summary>Current physical location of the asset (free text)</summary>
        public string? CurrentLocation { get; set; }

        /// <summary>Normal | Defect_Location | Parked_Away | Unknown</summary>
        public string? LocationStatus { get; set; }

        /// <summary>Guard/staff who last updated the location</summary>
        public string? LocationUpdatedBy { get; set; }

        public DateTime? LocationUpdatedDate { get; set; }

        // ── Deposit & Installment Payment fields ─────────────────────────────

        /// <summary>
        /// Deposit / advance amount paid upfront.
        /// Reference / display field only — not tracked via CreditorInvoice.
        /// </summary>
        public decimal? DepositAmount { get; set; }

        /// <summary>
        /// FULL = single payment (no installments generated).
        /// MONTHLY = split purchase amount (minus deposit) into monthly installments.
        /// </summary>
        public string? PaymentTermType { get; set; }

        /// <summary>First month of the installment schedule.</summary>
        public DateTime? InstallmentStartDate { get; set; }

        /// <summary>Amount to pay each month.</summary>
        public decimal? MonthlyInstallmentAmount { get; set; }

        /// <summary>
        /// Total number of installment months.
        /// Auto-calculated: CEILING((PurchaseAmount - DepositAmount) / MonthlyInstallmentAmount).
        /// </summary>
        public int? InstallmentMonths { get; set; }

        // ── Preclosing ────────────────────────────────────────────────────────
        /// <summary>True when owner opts to close all remaining installments early.</summary>
        public bool IsPreClosing { get; set; } = false;

        /// <summary>Date when preclosing was initiated.</summary>
        public DateTime? PreClosingDate { get; set; }

        /// <summary>Total remaining amount at time of preclosing.</summary>
        public decimal? PreClosingAmount { get; set; }

        // ── Vehicle-specific fields ───────────────────────────────────────────
        /// <summary>Road tax renewal date (only relevant when AssetType = Vehicle).</summary>
        public DateTime? RoadTaxRenewalDate { get; set; }

        /// <summary>Insurance renewal date (only relevant when AssetType = Vehicle).</summary>
        public DateTime? InsuranceRenewalDate { get; set; }

        // ── Office-specific fields ────────────────────────────────────────────
        /// <summary>Service last date (only relevant when Category AssetType = Office).</summary>
        public DateTime? ServiceLastDate { get; set; }

        // Navigation
        [ForeignKey("SupplierID")]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey("CategoryID")]
        public virtual InventoryCategory? Category { get; set; }

        public virtual ICollection<AssetInstallment> Installments { get; set; } = new List<AssetInstallment>();
    }
}
