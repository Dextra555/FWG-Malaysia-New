namespace OBMS.WebAPI.Models.DTO
{
    public class SalaryAttendenceDto
    {
        public int ID { get; set; }
        public int EMP_ID { get; set; }
        public string? EMP_NAME { get; set; }
        public string? EMP_CODE { get; set; }
        public string? EMP_IC_NEW { get; set; }
        public string? EMP_IC_OLD { get; set; }
        public string? EMP_PASSPORT_NO { get; set; }
        public string? EMPFL_BANK { get; set; }
        public string? EMPFL_BK_ACCNO { get; set; }
        public string? PAYMODE { get; set; }
        public decimal Amount { get; set; }
        public string? Particulars { get; set; }
        public DateTime? EMP_DATE_OF_BIRTH { get; set; }
        public DateTime? EMPPAY_DATE_JOINED { get; set; }
        public DateTime? EMPPAY_DATE_RESIGNED { get; set; }
        public bool? EPFDETECT { get; set; }
        public bool? SOCSODETECT { get; set; }
        public bool? INCOMETAXDETECT { get; set; }
        public string? EMPFL_EPFNO { get; set; }
        public decimal? ATTENDANCEALLOWANCE { get; set; }
        public decimal? SpecialAllowance { get; set; }
        public double? EMPPAY_BASIC_RATE { get; set; }
        public string? Name { get; set; }
        public string? SalaryStructure { get; set; }
    }
}
