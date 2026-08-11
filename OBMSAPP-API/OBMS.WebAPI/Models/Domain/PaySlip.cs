using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("PaySlip")]
    public class PaySlip
    {
        [Key]
        public int ID { get; set; }
        public DateTime Period { get; set; }
        public int EmployeeID { get; set; }
        public decimal? BasicSalaryDays { get; set; }
        public decimal BasicSalaryRate { get; set; }
        public decimal BasicSalary { get; set; }
        public int OverTimeSalaryHours { get; set; }
        public decimal OverTimeSalaryRate { get; set; }
        public decimal OverTimeSalary { get; set; }
        public decimal? OffDaySalaryDays { get; set; }
        public decimal OffDaySalaryRate { get; set; }
        public decimal OffDaySalary { get; set; }
        public int OffDayOverTimeSalaryHours { get; set; }
        public decimal OffDayOverTimeSalaryRate { get; set; }
        public decimal OffDayOverTimeSalary { get; set; }
        public decimal? HolidaySalaryDays { get; set; }
        public decimal HolidaySalaryRate { get; set; }
        public decimal HolidaySalary { get; set; }
        public int HolidayOverTimeSalaryHours { get; set; }
        public decimal HolidayOverTimeSalaryRate { get; set; }
        public decimal HolidayOverTimeSalary { get; set; }
        public decimal Shift2SalaryDaysHours { get; set; }
        public int Shift2SalaryRateType { get; set; }
        public decimal Shift2SalaryRate { get; set; }
        public decimal Shift2Salary { get; set; }
        public decimal? ReAllowanceDays { get; set; }
        public decimal ReAllowance { get; set; }
        public decimal AttendanceAllowance { get; set; }
        public decimal SpecialAllowance { get; set; }
        public decimal EPFDeductionAmount { get; set; }
        public decimal SOCSODeductionAmount { get; set; }
        public decimal EPFEmployerContribution { get; set; }
        public decimal SOCSOEmployerContribution { get; set; }
        public decimal DailyAdvanceRecovery { get; set; }
        public decimal SpecialAdvanceRecovery { get; set; }
        public decimal MonthlyAdvanceRecovery { get; set; }
        public decimal? UniformIssueRecovery { get; set; }
        public decimal LoanRecovery { get; set; }
        public decimal IncomeTaxDeduction { get; set; }
        public decimal MiscAmount { get; set; }
        public decimal MiscDeduction { get; set; }
        public string SalaryPayMode { get; set; }
        public decimal Bonus { get; set; }
        public DateTime LastUpdate { get; set; }
        public string LastUpdatedBy { get; set; }
        public decimal? SIPEmployeeContribution { get; set; }
        public decimal? SIPEmployerContribution { get; set; }
    }
}
