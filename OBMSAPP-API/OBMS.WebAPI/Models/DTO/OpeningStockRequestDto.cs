namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload sent from Angular when saving or updating an Opening Stock entry.
    /// ID = 0 means new record; ID > 0 means update existing.
    /// TotalValue is NOT sent by client — it is always calculated server-side as OpeningQty * UnitCost.
    /// </summary>
    public class OpeningStockRequestDto
    {
        public int ID { get; set; }

        /// <summary>ReferenceNo is generated server-side on new; preserved on edit.</summary>
        public string? ReferenceNo { get; set; }

        public int WarehouseID { get; set; }

        public int ItemID { get; set; }

        public DateTime OpeningDate { get; set; }

        public decimal OpeningQty { get; set; }

        public decimal UnitCost { get; set; }

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }
    }
}
