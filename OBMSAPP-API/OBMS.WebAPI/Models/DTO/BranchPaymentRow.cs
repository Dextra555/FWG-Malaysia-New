namespace OBMS.WebAPI.Models.DTO
{
    public class BranchPaymentRow
    {
        public int ID { get; set; }
        public int PaymentID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal? Amount { get; set; }
    }
}
