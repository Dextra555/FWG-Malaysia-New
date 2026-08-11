namespace OBMS.WebAPI.Models.DTO
{
    public class StockIssueDetailRequestDto
    {
        public int ID { get; set; }
        public int InvoiceID { get; set; }
        public int ItemID { get; set; }
        public int ItemCategoryID { get; set; }
        public int NoOfUnits { get; set; }
        public decimal CostPerUnit { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
