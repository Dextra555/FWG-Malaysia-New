namespace OBMS.WebAPI.Models.DTO
{
    public class StockIssuesRequestDto
    {
        public int ID { get; set; }
        public string Branch { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string InvoiceRemarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        public StockIssueDetailRequestDto[]? details { get; set; }
    }
}
