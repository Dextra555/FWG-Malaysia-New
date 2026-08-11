using System.ComponentModel.DataAnnotations;
using System.Data.SqlTypes;

namespace OBMS.WebAPI.Models.DTO
{
    public class EmployeeRequestDto
    {

        public int EMP_ID { get; set; }

        public string EMP_ROLE { get; set; }

        public string EMP_CODE { get; set; }

        public string EMP_NAME { get; set; }

        public string EMP_ADDRESS1 { get; set; }

        public string EMP_ADDRESS2 { get; set; }

        public string EMP_POST_CODE { get; set; }

        public string EMP_TOWN { get; set; }

        public string EMP_STATE { get; set; }

        public string EMP_NATIONAL { get; set; }

        public string EMP_PHONE { get; set; }

        public string EMP_HGH_EDU { get; set; }

        public string EM_WORK_EXP { get; set; }

        public DateTime EMP_DATE_OF_BIRTH { get; set; }

        public string EMP_IC_OLD { get; set; }

        public string EMP_IC_NEW { get; set; }

        public string EMP_IC_COLOR { get; set; }

        public string EMP_PASSPORT_NO { get; set; }

        public string EMP_SEX { get; set; }

        public string EMP_RACE { get; set; }

        public string EMP_MARTIAL_STATUS { get; set; }

        public string EMP_SPOUSE_NAME { get; set; }

        public string EMP_SP_IC { get; set; }

        public int EMP_NO_CHILD { get; set; }

        public bool EMP_SP_WORK { get; set; }

        public string EMP_PER_NAME_CONTACT { get; set; }

        public string EMP_CONTACT_ADDRESS1 { get; set; }

        public string EMP_CONTACT_ADDRESS2 { get; set; }

        public string EMP_CONTACT_POST_CODE { get; set; }

        public string EMP_CONTACT_TOWN { get; set; }

        public string EMP_CONTACT_STATE { get; set; }

        public string EMP_CONTACT_TELEPHONE { get; set; }

        public string EMP_BRANCH_CODE { get; set; }

        public string OldBranch { get; set; }

        public DateTime TransferDate { get; set; }

        public bool HasTransfered { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string LastUpdatedBy { get; set; }

        public string EMP_MOBILEPHONE { get; set; }

        public int EMP_CITIZEN { get; set; }

        public int EMP_CHECKLIST { get; set; }

        public string EMP_CLIENT { get; set; }

        public char NewSalaryStructure { get; set; }

        public bool KDNVetting { get; set; }

        public char SalaryStructure1000_3h { get; set; }

        public DateTime? VisaExpiryDate { get; set; }

        public DateTime? PassportExpiryDate { get; set; }


        public int EMPPAY_ID { get; set; }
        public string EMPPAY_JOB_TITLE { get; set; }
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


        public int EMPFL_ID { get; set; }
        public string? EMPFL_BANK { get; set; }
        public string? EMPFL_BK_ACCNO { get; set; }
        public string? EMPFL_TAX_NO { get; set; }
        public string? EMPFL_EPFNO { get; set; }
        public bool? EMPFL_EPF8Pa { get; set; }
        public string? EMPFL_SOSCO_NO { get; set; }
        public bool? SKBBK { get; set; }
        public bool EPFDETECT { get; set; }
        public string PAYMODE { get; set; }
        public bool SOCSODETECT { get; set; }
        public bool TMPGUARD { get; set; }
        public bool DETECTBYND55 { get; set; }
        public bool? INCOMETAXDETECT { get; set; }
        public string? EMP_SP_TEL_NO { get; set; }
        public decimal? KPI { get; set; }
        public string? PAYMODE2 { get; set; }
        public bool SplitSalaryPayment { get; set; }
        public string? EMPFL_2ndBank { get; set; }
        public string? EMPFL_2ndBK_ACCNO { get; set; }

        public DateTime? FOMEMA_MedicalCheckupDate { get; set; }

        public DateTime? FOMEMA_1stAppealDate { get; set; }

        public DateTime? FOMEMA_2ndAppealDate { get; set; }

        public DateTime? FOMEMA_NCDMonitoringDate { get; set; }

        public DateTime? FOMEMA_ResultDate { get; set; }

        // ----------------------------------------------------------
        // Employee Transfer fields (branch change in edit mode)
        // ----------------------------------------------------------

        /// <summary>
        /// Set to true by the UI when EMP_BRANCH_CODE has been changed
        /// on an existing employee (edit mode only).
        /// </summary>
        public bool IsBranchChanged { get; set; }

        /// <summary>
        /// The effective start date for the new branch.
        /// Required when IsBranchChanged = true.
        /// The previous EmployeeHistory row will be closed with
        /// Emp_EndDate = BranchStartDate - 1 day.
        /// </summary>
        public DateTime? BranchStartDate { get; set; }
    }
}
