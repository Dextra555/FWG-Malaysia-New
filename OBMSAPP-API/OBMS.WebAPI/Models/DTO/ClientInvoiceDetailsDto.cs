namespace OBMS.WebAPI.Models.DTO
{
    public class ClientInvoiceDetailsDto
    {
        public decimal ID { get; set; }
        public decimal ClientInvoiceID { get; set; }
        public decimal AgreementDetailID { get; set; }
        public decimal AgreementID { get; set; }
        public DateTime AgreementDate { get; set; }
        public int NoOfGuards { get; set; }
        public decimal Rate { get; set; }
        public decimal NoOfHours { get; set; }
        public decimal NoOfDays { get; set; }
        public bool FollowCalender { get; set; }
        public decimal MonthTotal { get; set; }
        public bool HasDiscount { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool IsTaxable { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime LASTUPDATE { get; set; }

        // Optional: parameterless constructor for EF Core or manual initialization
        public ClientInvoiceDetailsDto() { }

        // Optional: constructor matching your old ADO mapping
        public ClientInvoiceDetailsDto(
            decimal id,
            decimal clientInvoiceID,
            decimal agreementDetailID,
            decimal agreementID,
            DateTime agreementDate,
            int noOfGuards,
            decimal rate,
            decimal noOfHours,
            decimal noOfDays,
            bool followCalender,
            decimal monthTotal,
            bool hasDiscount,
            decimal discountAmount,
            bool isTaxable,
            decimal taxAmount,
            DateTime lastUpdate
        )
        {
            ID = id;
            ClientInvoiceID = clientInvoiceID;
            AgreementDetailID = agreementDetailID;
            AgreementID = agreementID;
            AgreementDate = agreementDate;
            NoOfGuards = noOfGuards;
            Rate = rate;
            NoOfHours = noOfHours;
            NoOfDays = noOfDays;
            FollowCalender = followCalender;
            MonthTotal = monthTotal;
            HasDiscount = hasDiscount;
            DiscountAmount = discountAmount;
            IsTaxable = isTaxable;
            TaxAmount = taxAmount;
            LASTUPDATE = lastUpdate;
        }
    }
}
