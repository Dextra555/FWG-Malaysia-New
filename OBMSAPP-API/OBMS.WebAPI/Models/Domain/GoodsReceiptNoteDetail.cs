using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("GoodsReceiptNoteDetail")]
    public class GoodsReceiptNoteDetail
    {
        [Key]
        public int ID { get; set; }

        public int GRNID { get; set; }

        public int PODetailID { get; set; }

        public int ItemID { get; set; }

        public int? UOMID { get; set; }

        public decimal ReceivedQty { get; set; }

        public decimal UnitCost { get; set; }

        /// <summary>
        /// SST rate for this GRN line, carried from PurchaseOrderDetail.SSTPercent.
        /// Not re-entered by the user — inherited from the approved PO.
        /// </summary>
        public decimal SSTPercent { get; set; } = 0;

        /// <summary>
        /// Stored SST amount for this GRN line.
        /// Formula: ReceivedQty × UnitCost × SSTPercent / 100.
        /// </summary>
        public decimal SSTAmount { get; set; } = 0;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation
        [JsonIgnore]
        [ForeignKey("GRNID")]
        public virtual GoodsReceiptNote? GRN { get; set; }

        [JsonIgnore]
        [ForeignKey("PODetailID")]
        public virtual PurchaseOrderDetail? PODetail { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }

        [ForeignKey("UOMID")]
        public virtual UOMMaster? UOM { get; set; }
    }
}
