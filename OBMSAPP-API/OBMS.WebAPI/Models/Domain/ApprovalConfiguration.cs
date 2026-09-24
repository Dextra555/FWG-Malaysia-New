using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Controls whether approval is required for each document type.
    /// One row per document type — configurable without code changes.
    /// </summary>
    [Table("ApprovalConfiguration")]
    public class ApprovalConfiguration
    {
        [Key]
        public int ID { get; set; }

        /// <summary>PURCHASE_REQUISITION | PURCHASE_ORDER | STOCK_ADJUSTMENT | STOCK_TRANSFER | DAMAGE_LOSS | ASSET_DISPOSAL</summary>
        [Required, MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public bool ApprovalRequired { get; set; } = true;

        /// <summary>Informational label, e.g. "Inventory Manager". Not enforced by code — for display only.</summary>
        [MaxLength(100)]
        public string? ApproverRole { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? LastUpdatedBy { get; set; }
    }
}
