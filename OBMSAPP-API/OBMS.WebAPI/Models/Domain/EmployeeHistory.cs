using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("EmployeeHistory")]
    public class EmployeeHistory
    {
        [Key]
        public int EMP_HISTORY_ID { get; set; } 

        public int EMP_ID { get; set; }

        //[StringLength(50)]
        public string EMP_ROLE { get; set; }

        //[StringLength(50)]
        public string EMP_CODE { get; set; }

        //[StringLength(50)]
        public string EMP_NAME { get; set; }

        //[StringLength(100)]
        public string EMP_ADDRESS1 { get; set; }

        //[StringLength(100)]
        public string EMP_ADDRESS2 { get; set; }

        //[StringLength(5)]
        public string EMP_POST_CODE { get; set; }

        //[StringLength(50)]
        public string EMP_TOWN { get; set; }

        //[StringLength(30)]
        public string EMP_STATE { get; set; }

        //[StringLength(50)]
        public string? EMP_NATIONAL { get; set; }

        //[StringLength(20)]
        public string EMP_PHONE { get; set; }

        //[StringLength(50)]
        public string EMP_HGH_EDU { get; set; }

        //[StringLength(50)]
        public string EM_WORK_EXP { get; set; }

        public DateTime? EMP_DATE_OF_BIRTH { get; set; }

        //[StringLength(20)]
        public string EMP_IC_OLD { get; set; }

        //[StringLength(20)]
        public string EMP_IC_NEW { get; set; }

        //[StringLength(25)]
        public string EMP_IC_COLOR { get; set; }

        //[StringLength(30)]
        public string EMP_PASSPORT_NO { get; set; }

        //[StringLength(50)]
        public string EMP_SEX { get; set; }

        //[StringLength(25)]
        public string EMP_RACE { get; set; }

        //[StringLength(10)]
        public string EMP_MARTIAL_STATUS { get; set; }

        //[StringLength(50)]
        public string? EMP_SPOUSE_NAME { get; set; }

        //[StringLength(20)]
        public string? EMP_SP_IC { get; set; }

        public int? EMP_NO_CHILD { get; set; }

        public bool EMP_SP_WORK { get; set; }

        //[StringLength(50)]
        public string? EMP_PER_NAME_CONTACT { get; set; }

        //[StringLength(100)]
        public string EMP_CONTACT_ADDRESS1 { get; set; }

        //[StringLength(100)]
        public string EMP_CONTACT_ADDRESS2 { get; set; }

        //[StringLength(5)]
        public string EMP_CONTACT_POST_CODE { get; set; }

        //[StringLength(50)]
        public string EMP_CONTACT_TOWN { get; set; }

        //[StringLength(30)]
        public string EMP_CONTACT_STATE { get; set; }

        //[StringLength(20)]
        public string EMP_CONTACT_TELEPHONE { get; set; }

        //[StringLength(20)]
        public string EMP_BRANCH_CODE { get; set; }

        //[StringLength(20)]
        public string? OldBranch { get; set; }

        public DateTime? TransferDate { get; set; }

        public bool HasTransfered { get; set; }

        public DateTime? VisaExpiryDate { get; set; }

        public DateTime? PassportExpiryDate { get; set; }

        public DateTime? FOMEMA_MedicalCheckupDate { get; set; }

        public DateTime? FOMEMA_1stAppealDate { get; set; }

        public DateTime? FOMEMA_2ndAppealDate { get; set; }

        public DateTime? FOMEMA_NCDMonitoringDate { get; set; }

        public DateTime? FOMEMA_ResultDate { get; set; }

      
        //[StringLength(20)]
        public string EMP_MOBILEPHONE { get; set; }

        public int EMP_CITIZEN { get; set; }

        public int EMP_CHECKLIST { get; set; }

        //[StringLength(10)]
        public string? EMP_CLIENT { get; set; }

        //[StringLength(1)]
        public char? NewSalaryStructure { get; set; }

        public bool KDNVetting { get; set; }

        //[StringLength(1)]
        public char? SalaryStructure1000_3h { get; set; }

        public string EMPPAY_JOB_TITLE { get; set; }
        ////[StringLength(50)]
        public string EMPPAY_CATEGORY { get; set; }
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

        [StringLength(50)]
        public string? EMPFL_BANK { get; set; }

        [StringLength(25)]
        public string? EMPFL_BK_ACCNO { get; set; }
        [StringLength(25)]
        public string? EMPFL_TAX_NO { get; set; }
        [StringLength(25)]
        public string? EMPFL_EPFNO { get; set; }
        public bool? EMPFL_EPF8Pa { get; set; }
        [StringLength(25)]
        public string? EMPFL_SOSCO_NO { get; set; }
        public bool EPFDETECT { get; set; }
        [StringLength(20)]
        public string PAYMODE { get; set; }
        public bool SOCSODETECT { get; set; }
        public bool TMPGUARD { get; set; }
        public bool DETECTBYND55 { get; set; }
        public DateTime LASTUPDATE { get; set; }
        [StringLength(20)]
        public string LastUpdatedBy { get; set; }
        public bool? INCOMETAXDETECT { get; set; }

        // ----------------------------------------------------------
        // Transfer / history period columns (Employee Transfer feature)
        // ----------------------------------------------------------

        /// <summary>
        /// Effective start date for this employee record at this branch.
        /// Set to EMPPAY_DATE_JOINED on new employee creation.
        /// Set to the selected branch-change date on transfer.
        /// </summary>
        public DateTime? Emp_StartDate { get; set; }

        /// <summary>
        /// Last active date for this employee record at this branch.
        /// NULL = currently active at this branch.
        /// Set to (new branch start date - 1 day) when the employee is transferred.
        /// </summary>
        public DateTime? Emp_EndDate { get; set; }
    }
}
