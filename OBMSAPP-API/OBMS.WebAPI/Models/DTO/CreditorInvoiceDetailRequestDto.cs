namespace OBMS.WebAPI.Models.DTO
{
    public class CreditorInvoiceDetailRequestDto
    {
        public int ID { get; set; }
        public int InvoiceID { get; set; }
        public int SerialNo { get; set; }
        public int? ItemID { get; set; }
        public int? ItemCategoryID { get; set; }
        public int? NoOfUnits { get; set; }
        public decimal? CostPerUnit { get; set; }
        public decimal? ValuePeriod { get; set; }
        public decimal? ValuePercentage { get; set; }
        public string? ValueStatus { get; set; }
        public DateTime? InvoicePeriodFrom { get; set; }
        public DateTime? InvoicePeriodTo { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string LastUpdatedBy { get; set; }

    }
}
