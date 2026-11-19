using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("PurchaseReturnDetail")]
    public class PurchaseReturnDetail
    {
        [Key]
        public int ID { get; set; }

        public int ReturnID { get; set; }

        public int GRNDetailID { get; set; }

        public int ItemID { get; set; }

        public int? UOMID { get; set; }

        public decimal ReturnQty { get; set; }

        public decimal UnitCost { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation
        [JsonIgnore]
        [ForeignKey("ReturnID")]
        public virtual PurchaseReturn? Return { get; set; }

        [JsonIgnore]
        [ForeignKey("GRNDetailID")]
        public virtual GoodsReceiptNoteDetail? GRNDetail { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }

        [ForeignKey("UOMID")]
        public virtual UOMMaster? UOM { get; set; }
    }
}
