namespace OBMS.WebAPI.Models.DTO
{
    public class ItemMasterDto
    {
        public int ID { get; set; }
        public int CategoryID { get; set; }
        public int AdvanceID { get; set; }
        public int ItemID { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public decimal? SellPrice { get; set; }
        public string? Remarks { get; set; }
        public decimal? Quantity { get; set; }
    }
}
