using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Record for damaged or lost stock.
    /// Creates a DAMAGE_LOSS (OutQty) row in StockLedger, reducing available stock.
    /// Quantity must not exceed available stock to prevent negative stock.
    /// </summary>
    [Table("StockDamageLoss")]
    public class StockDamageLoss
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated reference number, e.g. DL0001</summary>
        public string RecordNo { get; set; } = string.Empty;

        public DateTime RecordDate { get; set; }

        public int WarehouseID { get; set; }

        public int ItemID { get; set; }

        /// <summary>Damage | Loss</summary>
        public string LossType { get; set; } = "Damage";

        public decimal Quantity { get; set; }

        public decimal UnitCost { get; set; } = 0;

        /// <summary>Mandatory reason</summary>
        public string Reason { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        /// <summary>Soft-delete flag.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("WarehouseID")]
        public virtual WarehouseMaster? Warehouse { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }
    }
}
