namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload sent from Angular when saving a Damage/Loss record.
    /// Quantity must not exceed available stock.
    /// Creates a DAMAGE_LOSS (OutQty) row in StockLedger.
    /// </summary>
    public class StockDamageLossRequestDto
    {
        public int ID { get; set; }

        /// <summary>RecordNo is generated server-side.</summary>
        public string? RecordNo { get; set; }

        public DateTime RecordDate { get; set; }

        public int WarehouseID { get; set; }

        public int ItemID { get; set; }

        /// <summary>Damage | Loss</summary>
        public string LossType { get; set; } = "Damage";

        public decimal Quantity { get; set; }

        public decimal UnitCost { get; set; }

        /// <summary>Mandatory reason</summary>
        public string Reason { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }
    }
}
