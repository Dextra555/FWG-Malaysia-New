namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Enriched read model returned to the Angular list and detail views.
    /// Includes denormalised display names so Angular does not need to join client-side.
    /// </summary>
    public class OpeningStockListDto
    {
        public int ID { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string UOMName { get; set; } = string.Empty;
        public DateTime OpeningDate { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
