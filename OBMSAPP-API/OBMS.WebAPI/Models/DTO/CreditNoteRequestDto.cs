namespace OBMS.WebAPI.Models.DTO
{
    public class CreditNoteRequestDto
    {
        public int ID { get; set; }
        public string CreditNoteNo { get; set; }
        public string Branch { get; set; }
        public string Client { get; set; }
        public int? ClientInvoiceID { get; set; }
        public DateTime CreditNoteDate { get; set; }
        public decimal CreditNoteAmount { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Reason { get; set; }
        public string? ReferenceInvoiceNo { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
