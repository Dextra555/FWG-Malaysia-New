using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("PurchaseOrderDetail")]
    public class PurchaseOrderDetail
    {
        [Key]
        public int ID { get; set; }

        public int POID { get; set; }

        /// <summary>
        /// FK to ItemMaster. NULL when IsServiceLine = true (free-text service line).
        /// </summary>
        public int? ItemID { get; set; }

        /// <summary>
        /// Free-text description for service / non-inventory lines.
        /// Populated only when IsServiceLine = true (ItemID is null).
        /// </summary>
        public string? ServiceDescription { get; set; }

        /// <summary>
        /// True when this line represents a service or ad-hoc charge
        /// that is not tracked in the ItemMaster.
        /// </summary>
        public bool IsServiceLine { get; set; } = false;

        public int? UOMID { get; set; }

        public decimal OrderedQty { get; set; }

        /// <summary>Cumulative total received across all GRNs against this PO line.</summary>
        public decimal ReceivedQty { get; set; } = 0;

        public decimal UnitPrice { get; set; }

        /// <summary>Stored = OrderedQty × UnitPrice, calculated by API before save.</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// SST rate applied to this line (e.g. 0, 6, 8).
        /// User-configurable per line — not all items are SST-applicable.
        /// </summary>
        public decimal SSTPercent { get; set; } = 0;

        /// <summary>
        /// Stored SST amount for this line.
        /// Formula: OrderedQty × UnitPrice × SSTPercent / 100.
        /// Calculated by API on save; TotalPrice remains ex-SST.
        /// </summary>
        public decimal SSTAmount { get; set; } = 0;

        public string? Remarks { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation
        [JsonIgnore]
        [ForeignKey("POID")]
        public virtual PurchaseOrder? PO { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }   // null for service lines

        [ForeignKey("UOMID")]
        public virtual UOMMaster? UOM { get; set; }
    }
}
