using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Result model for Employee Visa & Passport List report.
    /// Populated via raw SQL query — no backing table.
    /// </summary>
    public class EmployeeVisaPassportListView
    {
        [Key]
        public int EMP_ID { get; set; }
        public string EMP_CODE { get; set; } = string.Empty;
        public string EMP_NAME { get; set; } = string.Empty;
        public string EMP_BRANCH_CODE { get; set; } = string.Empty;
        public string EMP_PASSPORT_NO { get; set; } = string.Empty;
        public string EMP_IC_NEW { get; set; } = string.Empty;
        public DateTime? VisaExpiryDate { get; set; }
        public DateTime? PassportExpiryDate { get; set; }
        public DateTime? EMPPAY_DATE_JOINED { get; set; }
        public string? EMPPAY_CATEGORY { get; set; }
        public int? DaysToVisaExpiry { get; set; }
        public int? DaysToPassportExpiry { get; set; }
        public string VisaStatus { get; set; } = string.Empty;
        public string PassportStatus { get; set; } = string.Empty;
    }
}
