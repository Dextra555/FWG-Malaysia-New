namespace OBMS.WebAPI.Models.DTO
{
    public class AgreementDetailsRequestDto
    {
        public int ID { get; set; }
        public int AgreementID { get; set; }
        public string Description { get; set; }
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
        public int DiscountHour { get; set; }
        public string Category { get; set; }
        public string Reason { get; set; }
        public string LastUpdatedBy { get; set; }
    }
}
