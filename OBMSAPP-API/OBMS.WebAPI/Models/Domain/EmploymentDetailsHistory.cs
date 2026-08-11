using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Stores a snapshot of EmploymentDetails for each branch period.
    /// A new row is inserted on new employee creation and on every branch transfer.
    /// Emp_StartDate = effective date of this employment record at the branch.
    /// Emp_EndDate   = last day at this branch (NULL = currently active).
    /// </summary>
    [Table("EmploymentDetailsHistory")]
    public class EmploymentDetailsHistory
    {
        [Key]
        public int EMPPAY_HISTORY_ID { get; set; }

        /// <summary>Reference to Employee.EMP_ID</summary>
        public int EMP_ID { get; set; }

        [StringLength(50)]
        public string EMPPAY_CODE { get; set; }

        [StringLength(20)]
        public string EMPPAY_BRANCHCODE { get; set; }

        [StringLength(50)]
        public string? EMPPAY_JOB_TITLE { get; set; }

        [StringLength(50)]
        public string? EMPPAY_CATEGORY { get; set; }

        public DateTime? EMPPAY_DATE_JOINED { get; set; }

        public DateTime? EMPPAY_DATE_CONFIRM { get; set; }

        public DateTime? EMPPAY_DATE_PROMOTION { get; set; }

        public DateTime? EMPPAY_DATE_RESIGNED { get; set; }

        public double EMPPAY_BASIC_RATE { get; set; }

        public decimal SALARYLAB { get; set; }

        public decimal? ATTENDANCEALLOWANCE { get; set; }

        public decimal NewStructureATTENDANCEALLOWANCE { get; set; }

        public decimal SpecialAllowance { get; set; }

        public decimal? AttendanceAllowanceWorkingDays { get; set; }

        public string? AttendanceAllowanceFollowCalendar { get; set; }

        public decimal? KPI { get; set; }

        public DateTime LASTUPDATE { get; set; }

        [StringLength(20)]
        public string? LastUpdatedBy { get; set; }

        // ----------------------------------------------------------
        // Transfer / history period columns
        // ----------------------------------------------------------

        /// <summary>Effective start date of this employment record at this branch.</summary>
        public DateTime? Emp_StartDate { get; set; }

        /// <summary>
        /// Effective end date of this employment record at this branch.
        /// NULL means this is the currently active record.
        /// </summary>
        public DateTime? Emp_EndDate { get; set; }
    }
}
