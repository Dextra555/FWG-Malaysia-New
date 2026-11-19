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

        /// <summary>
        /// Estimated SST rate for this PR line (e.g. 0, 6, 8).
        /// Informational only — carried to PurchaseOrderDetail.SSTPercent when a PO is raised from this PR.
        /// </summary>
        public decimal SSTPercent { get; set; } = 0;

        /// <summary>
        /// Estimated SST amount for this PR line.
        /// Formula: RequiredQty × BuyingPrice × SSTPercent / 100.
        /// Calculated by API on save; BuyingPrice is not changed.
        /// </summary>
        public decimal SSTAmount { get; set; } = 0;

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
