using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("GoodsReceiptNote")]
    public class GoodsReceiptNote
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated: GRN0001, GRN0002 …</summary>
        public string GRNNumber { get; set; } = string.Empty;

        public int POID { get; set; }

        public int WarehouseID { get; set; }

        public DateTime GRNDate { get; set; }

        public string? DeliveryNoteRef { get; set; }

        /// <summary>Supplier's own invoice number as printed on their delivery/invoice document.</summary>
        public string? SupplierInvoiceNo { get; set; }

        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation — JsonIgnore prevents circular reference in API responses
        [JsonIgnore]
        [ForeignKey("POID")]
        public virtual PurchaseOrder? PO { get; set; }

        [JsonIgnore]
        [ForeignKey("WarehouseID")]
        public virtual WarehouseMaster? Warehouse { get; set; }

        [JsonIgnore]
        public virtual ICollection<GoodsReceiptNoteDetail> Details { get; set; }
            = new List<GoodsReceiptNoteDetail>();
    }
}
