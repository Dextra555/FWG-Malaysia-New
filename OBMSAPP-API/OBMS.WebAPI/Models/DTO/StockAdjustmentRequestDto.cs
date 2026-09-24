namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload sent from Angular when saving a Stock Adjustment.
    /// AdjustmentType: PLUS (physical > system) | MINUS (physical < system).
    /// MINUS is validated against available stock.
    /// </summary>
    public class StockAdjustmentRequestDto
    {
        public int ID { get; set; }

        /// <summary>AdjustmentNo is generated server-side.</summary>
        public string? AdjustmentNo { get; set; }

        public DateTime AdjustmentDate { get; set; }

        public int WarehouseID { get; set; }

        public int ItemID { get; set; }

        /// <summary>PLUS | MINUS</summary>
        public string AdjustmentType { get; set; } = string.Empty;

        public decimal AdjustmentQty { get; set; }

        public decimal UnitCost { get; set; }

        /// <summary>Mandatory reason for the adjustment</summary>
        public string Reason { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }
    }
}
