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

        /// <summary>Branch code (free text) — existing field kept as-is</summary>
        public string? Branch { get; set; }

        public double? PurchaseAmount { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }

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

        // Navigation
        [ForeignKey("SupplierID")]
        public virtual Supplier? Supplier { get; set; }
    }
}
