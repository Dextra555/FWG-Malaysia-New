using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Immutable stock transaction log.
    /// Every stock movement (opening, in, out, transfer, adjustment) creates one row.
    /// Current stock balance = SUM(InQty) - SUM(OutQty) per Warehouse per Item.
    /// This table is maintained exclusively by the API — never directly edited by users.
    /// </summary>
    [Table("StockLedger")]
    public class StockLedger
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated, e.g. SL00001</summary>
        public string TransactionNo { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// Transaction type code. Values:
        ///   OPENING       – Opening Stock
        ///   STOCK_IN      – Stock In / GRN (future)
        ///   ISSUE         – Material Issue / Stock Out (future)
        ///   ISSUE_RETURN  – Issue Return (future)
        ///   TRANSFER_IN   – Transfer In (future)
        ///   TRANSFER_OUT  – Transfer Out (future)
        ///   ADJ_PLUS      – Positive Adjustment (future)
        ///   ADJ_MINUS     – Negative Adjustment (future)
        ///   PURCHASE_RTN  – Purchase Return (future)
        ///   DAMAGE_LOSS   – Damage / Loss (future)
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        public int WarehouseID { get; set; }

        public int ItemID { get; set; }

        /// <summary>Quantity flowing INTO the warehouse for this transaction</summary>
        public decimal InQty { get; set; } = 0;

        /// <summary>Quantity flowing OUT of the warehouse for this transaction</summary>
        public decimal OutQty { get; set; } = 0;

        public decimal? UnitCost { get; set; }

        /// <summary>Human-readable label for the source type, e.g. "Opening Stock"</summary>
        public string? ReferenceType { get; set; }

        /// <summary>Primary key of the source record (e.g. OpeningStock.ID)</summary>
        public int? ReferenceID { get; set; }

        /// <summary>Human-readable reference number, e.g. "OS0001"</summary>
        public string? ReferenceNo { get; set; }

        public string? Remarks { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("WarehouseID")]
        public virtual WarehouseMaster? Warehouse { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }
    }
}
