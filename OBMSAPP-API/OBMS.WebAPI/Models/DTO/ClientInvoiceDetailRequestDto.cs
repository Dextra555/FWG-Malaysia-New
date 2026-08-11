namespace OBMS.WebAPI.Models.DTO
{
    public class ClientInvoiceDetailRequestDto
    {
        public int ID { get; set; }
        public decimal AgreementDetailID { get; set; }

        public decimal AgreementID { get; set; }

        public DateTime AgreementDate { get; set; }

        public int NoOfGuards { get; set; }

        public decimal Rate { get; set; }

        public decimal NoOfHours { get; set; }

        public decimal NoOfDays { get; set; }

        public bool FollowCalender { get; set; }

        public bool HasDiscount { get; set; }

        public decimal DiscountAmount { get; set; }

        public bool IsTaxable { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal MonthTotal { get; set; }
    }
}
