namespace OBMS.WebAPI.Models.DTO
{
    public class SalaryAdvanceRequestDto
    {
        public int ID { get; set; }
        public int EmployeeID { get; set; }
        public DateTime AdvanceTakenDate { get; set; }
        public DateTime AdvanceDate { get; set; }
        public string? VoucherNo { get; set; }
        public decimal Amount { get; set; }
        public int NoOfInstallments { get; set; }
        public string? PaymentType { get; set; }
        public string? Particulars { get; set; }
        public int TransType { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
