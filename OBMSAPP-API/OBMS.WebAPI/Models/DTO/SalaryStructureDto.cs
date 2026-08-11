namespace OBMS.WebAPI.Models.DTO
{
    public class SalaryStructureDto
    {
        public int SalaryId { get; set; }
        public string? BranchCode { get; set; }
        public string? EmployeeType { get; set; }
        public string? EmployeeNationality { get; set; }
        public string? Name { get; set; }
        public decimal GeneralDayRate { get; set; }
        public decimal GeneralDayHours { get; set; }
        public decimal GeneralDayOTRate { get; set; }
        public decimal OffDayRate { get; set; }
        public decimal OffDayOTRate { get; set; }
        public decimal HolidayRate { get; set; }
        public decimal HolidayOTRate { get; set; }
        public decimal WorkingDays { get; set; }
        public decimal WorkingHours { get; set; }
        public decimal SalaryBand { get; set; }
        public decimal TravelAllowance { get; set; }
        public string? Status { get; set; }
        public bool EICC { get; set; }
        public bool NonStructure { get; set; }
        public string? Active { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
