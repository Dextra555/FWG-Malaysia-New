using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// A single line item inside a Branch Stock Request.
    /// One row per item/UOM combination requested.
    /// </summary>
    [Table("StockRequestDetail")]
    public class StockRequestDetail
    {
        [Key]
        public int ID { get; set; }

        public int RequestID { get; set; }

        public int ItemID { get; set; }

        public int? UOMID { get; set; }

        public decimal RequestedQty { get; set; }

        public decimal? BuyingPrice { get; set; }   // Auto-filled from ItemMaster.Price; editable by user

        public int? ClientID { get; set; }

        public string? Remarks { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // ── Navigation ────────────────────────────────────────────────────────
        [JsonIgnore]
        public virtual StockRequest? Request { get; set; }

        [JsonIgnore]
        public virtual ItemMaster? Item { get; set; }

        [JsonIgnore]
        public virtual UOMMaster? UOM { get; set; }

        [JsonIgnore]
        public virtual ClientMaster? Client { get; set; }
    }
}
