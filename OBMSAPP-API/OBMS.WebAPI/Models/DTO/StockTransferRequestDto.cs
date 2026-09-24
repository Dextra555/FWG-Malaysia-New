namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload sent from Angular when saving a Stock Transfer.
    /// Source and destination warehouses must be different.
    /// Each detail line creates TRANSFER_OUT + TRANSFER_IN ledger rows.
    /// </summary>
    public class StockTransferRequestDto
    {
        public int ID { get; set; }

        /// <summary>TransferNo is generated server-side.</summary>
        public string? TransferNo { get; set; }

        public DateTime TransferDate { get; set; }

        public int SourceWarehouseID { get; set; }

        public int DestWarehouseID { get; set; }

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }

        public List<StockTransferDetailRequestDto> Details { get; set; } = new();
    }

    public class StockTransferDetailRequestDto
    {
        public int ID { get; set; }

        public int ItemID { get; set; }

        public int? UOMID { get; set; }

        public decimal TransferQty { get; set; }

        public decimal UnitCost { get; set; }

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }
    }
}
