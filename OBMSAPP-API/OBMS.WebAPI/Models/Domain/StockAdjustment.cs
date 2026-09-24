using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Stock Adjustment record for physical count discrepancies.
    /// AdjustmentType PLUS  → StockLedger ADJ_PLUS  (InQty)
    /// AdjustmentType MINUS → StockLedger ADJ_MINUS (OutQty)
    /// MINUS adjustments are validated against available stock to prevent negative stock.
    /// </summary>
    [Table("StockAdjustment")]
    public class StockAdjustment
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated reference number, e.g. ADJ0001</summary>
        public string AdjustmentNo { get; set; } = string.Empty;

        public DateTime AdjustmentDate { get; set; }

        public int WarehouseID { get; set; }

        public int ItemID { get; set; }

        /// <summary>PLUS = stock added; MINUS = stock removed</summary>
        public string AdjustmentType { get; set; } = string.Empty;

        public decimal AdjustmentQty { get; set; }

        public decimal UnitCost { get; set; } = 0;

        /// <summary>Mandatory reason for the adjustment</summary>
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
