using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("EmploymentDetails")]
    public class EmploymentDetails
    {
        [Key]
        public int EMPPAY_ID { get; set; }

        public string EMPPAY_CODE { get; set; }

        //[StringLength(20)]
        public string EMPPAY_BRANCHCODE { get; set; }
        ////[StringLength(50)]
        public string EMPPAY_JOB_TITLE { get; set; }
        ////[StringLength(50)]
        public string EMPPAY_CATEGORY { get; set; }
        public DateTime EMPPAY_DATE_JOINED { get; set; }
        public DateTime? EMPPAY_DATE_CONFIRM { get; set; }
        public DateTime? EMPPAY_DATE_PROMOTION { get; set; }
        public DateTime? EMPPAY_DATE_RESIGNED { get; set; }
        public double EMPPAY_BASIC_RATE { get; set; }
        public decimal SALARYLAB { get; set; }
        public decimal? ATTENDANCEALLOWANCE { get; set; }
        public decimal NewStructureATTENDANCEALLOWANCE { get; set; }
        public decimal SpecialAllowance { get; set; }
        public DateTime LASTUPDATE { get; set; }
        //[StringLength(20)]
        public string LastUpdatedBy { get; set; }
        public decimal? AttendanceAllowanceWorkingDays { get; set; }
        public string? AttendanceAllowanceFollowCalendar { get; set; }
        public decimal? KPI { get; set; }

    }
}
