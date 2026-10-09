using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("PurchaseRequisitionDetail")]
    public class PurchaseRequisitionDetail
    {
        [Key]
        public int ID { get; set; }

        public int PRID { get; set; }

        public int ItemID { get; set; }

        public decimal RequiredQty { get; set; }

        public decimal? BuyingPrice { get; set; }   // Auto-filled from ItemMaster.Price; editable by user

        public int? UOMID { get; set; }

        public string? Remarks { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation — JsonIgnore prevents circular reference in API responses
        [JsonIgnore]
        [ForeignKey("PRID")]
        public virtual PurchaseRequisition? PR { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }

        [ForeignKey("UOMID")]
        public virtual UOMMaster? UOM { get; set; }
    }
}
