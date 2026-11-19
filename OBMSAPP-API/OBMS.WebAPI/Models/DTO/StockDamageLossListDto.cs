namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Enriched row returned in the Damage/Loss list and detail view.
    /// </summary>
    public class StockDamageLossListDto
    {
        public int ID { get; set; }
        public string RecordNo { get; set; } = string.Empty;
        public DateTime RecordDate { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UOMName { get; set; } = string.Empty;
        /// <summary>Damage | Loss</summary>
        public string LossType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
