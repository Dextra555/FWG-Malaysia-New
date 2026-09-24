using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("PurchaseReturn")]
    public class PurchaseReturn
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated: RTN0001, RTN0002 …</summary>
        public string ReturnNumber { get; set; } = string.Empty;

        public int GRNID { get; set; }

        public int WarehouseID { get; set; }

        public DateTime ReturnDate { get; set; }

        public string Reason { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation — JsonIgnore prevents circular reference in API responses
        [JsonIgnore]
        [ForeignKey("GRNID")]
        public virtual GoodsReceiptNote? GRN { get; set; }

        [JsonIgnore]
        [ForeignKey("WarehouseID")]
        public virtual WarehouseMaster? Warehouse { get; set; }

        [JsonIgnore]
        public virtual ICollection<PurchaseReturnDetail> Details { get; set; }
            = new List<PurchaseReturnDetail>();
    }
}
