namespace OBMS.WebAPI.Models.DTO
{
    public class ItemMasterDto
    {
        public int ID { get; set; }
        public string? Code { get; set; }           // Read-only in UI; generated server-side on insert
        public int CategoryID { get; set; }
        public int? BrandID { get; set; }
        public int? UOMID { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }          // Buying/cost price — used by Material Issue as CostPerUnit
        public string? Remarks { get; set; }
        public decimal? ReorderLevel { get; set; }
        public decimal? ReorderQuantity { get; set; }
        public int? WarehouseID { get; set; }           // Default warehouse for this item
        public string? LastUpdatedBy { get; set; }
        // AdvanceID and ItemID removed — were never mapped or used
        // SellPrice removed from UI contract — DB column retained but not exposed
        // Quantity removed from UI contract — not to be manually maintained in Item Master
    }
}
