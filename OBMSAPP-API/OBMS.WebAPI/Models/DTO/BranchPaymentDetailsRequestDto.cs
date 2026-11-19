namespace OBMS.WebAPI.Models.DTO
{
    public class BranchPaymentDetailsRequestDto
    {
        public int ID { get; set; }
        public decimal PaymentID { get; set; }
        public string Branch { get; set; }
        public decimal? InvoiceID { get; set; }
        /// <summary>
        /// Optional GRN reference. Present only when the user pays
        /// against a Goods Receipt Note (not a CreditorInvoice).
        /// </summary>
        public int? GRNID { get; set; }
        public decimal Amount { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
