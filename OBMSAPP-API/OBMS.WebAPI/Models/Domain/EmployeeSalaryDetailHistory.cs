using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Stores a snapshot of EmployeeSalaryDetails for each branch period.
    /// A new row is inserted on new employee creation and on every branch transfer.
    /// Emp_StartDate = effective date of this salary record at the branch.
    /// Emp_EndDate   = last day at this branch (NULL = currently active).
    /// </summary>
    [Table("EmployeeSalaryDetailHistory")]
    public class EmployeeSalaryDetailHistory
    {
        [Key]
        public int EMPFL_HISTORY_ID { get; set; }

        /// <summary>Reference to Employee.EMP_ID</summary>
        public int EMP_ID { get; set; }

        [StringLength(50)]
        public string EMPFL_CODE { get; set; }

        [StringLength(20)]
        public string EMPFL_BRANCHCODE { get; set; }

        public bool? SplitSalaryPayment { get; set; }

        [StringLength(50)]
        public string? EMPFL_BANK { get; set; }

        [StringLength(25)]
        public string? EMPFL_BK_ACCNO { get; set; }

        [StringLength(50)]
        public string? EMPFL_2ndBank { get; set; }

        [StringLength(25)]
        public string? EMPFL_2ndBK_ACCNO { get; set; }

        [StringLength(25)]
        public string? EMPFL_TAX_NO { get; set; }

        [StringLength(25)]
        public string? EMPFL_EPFNO { get; set; }

        public bool? EMPFL_EPF8Pa { get; set; }

        [StringLength(25)]
        public string? EMPFL_SOSCO_NO { get; set; }

        public bool? SKBBK { get; set; }

        public bool EPFDETECT { get; set; }

        [StringLength(20)]
        public string PAYMODE { get; set; }

        public string? PAYMODE2 { get; set; }

        public bool SOCSODETECT { get; set; }

        public bool TMPGUARD { get; set; }

        public bool DETECTBYND55 { get; set; }

        public bool? INCOMETAXDETECT { get; set; }

        public string? EMP_SP_TEL_NO { get; set; }

        public DateTime LASTUPDATE { get; set; }

        [StringLength(20)]
        public string? LastUpdatedBy { get; set; }

        // ----------------------------------------------------------
        // Transfer / history period columns
        // ----------------------------------------------------------

        /// <summary>Effective start date for this salary record at this branch.</summary>
        public DateTime? Emp_StartDate { get; set; }

        /// <summary>
        /// Effective end date for this salary record at this branch.
        /// NULL means this is the currently active record.
        /// </summary>
        public DateTime? Emp_EndDate { get; set; }
    }
}
