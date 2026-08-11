namespace OBMS.WebAPI.Models.DTO
{
    public class SalaryAdvanceDto
    {
        public int ID { get; set; }
        public int EMP_ID { get; set; }
        public string? EMP_NAME { get; set; }
        public string? EMP_RACE { get; set; }
        public string? EMP_ROLE { get; set; }
        public string? EMP_CODE { get; set; }
        public string? EMP_IC_NEW { get; set; }
        public string? EMP_IC_OLD { get; set; }
        public string? EMP_PASSPORT_NO { get; set; }
        public string? EMPFL_BANK { get; set; }
        public string? EMPFL_BK_ACCNO { get; set; }
        public string? PAYMODE { get; set; }       
        public decimal Amount { get; set; }      
        public string? Particulars { get; set; }
    }
}
