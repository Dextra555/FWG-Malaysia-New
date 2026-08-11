namespace OBMS.WebAPI.Models.DTO
{
    public class EmployeeLoanDto
    {
        public int Id { get; set; }
        public DateTime AdvanceDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; }
        public string Particulars { get; set; }
        public string? EMP_NAME { get; set; }
        public string? EMP_IC_NEW { get; set; }
    }
}
