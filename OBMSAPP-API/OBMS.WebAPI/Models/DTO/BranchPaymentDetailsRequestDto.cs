namespace OBMS.WebAPI.Models.DTO
{
    public class BranchPaymentDetailsRequestDto
    {
        public int ID { get; set; }
        public decimal PaymentID { get; set; }
        public string Branch { get; set; }
        public decimal? InvoiceID { get; set; }
        public decimal Amount { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
