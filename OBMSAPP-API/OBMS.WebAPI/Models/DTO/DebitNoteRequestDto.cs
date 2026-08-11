namespace OBMS.WebAPI.Models.DTO
{
    public class DebitNoteRequestDto
    {
        public int ID { get; set; }
        public string DebitNoteNo { get; set; }
        public string Branch { get; set; }
        public string Client { get; set; }
        public int? ClientInvoiceID { get; set; }
        public DateTime DebitNoteDate { get; set; }
        public decimal DebitNoteAmount { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Reason { get; set; }
        public string? ReferenceInvoiceNo { get; set; }
        public DateTime? DueDate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
