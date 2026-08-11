using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("EmployeeSalaryDetails")]
    public class EmployeeSalaryDetails
    {
        [Key]
        public int EMPFL_ID { get; set; }
        [StringLength(50)]
        public string EMPFL_CODE { get; set; }
        [StringLength(20)]
        public string EMPFL_BRANCHCODE { get; set; }
        [StringLength(50)]
        public bool? SplitSalaryPayment { get; set; }
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
        public DateTime LASTUPDATE { get; set; }
        [StringLength(20)]
        public string LastUpdatedBy { get; set; }
        public bool? INCOMETAXDETECT { get; set; }
        public string? EMP_SP_TEL_NO { get; set; }

    }
}
