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

        public int ItemID { get; set; }

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
        public virtual ItemMaster? Item { get; set; }

        [ForeignKey("UOMID")]
        public virtual UOMMaster? UOM { get; set; }
    }
}
