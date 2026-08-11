using OBMS.WebAPI.Models.DTO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("Employee")]
    public class Employee
    {

        [Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

        public DateTime LASTUPDATE { get; set; }

        //[StringLength(20)]
        public string? LastUpdatedBy { get; set; }

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

        public DateTime? VisaExpiryDate { get; set; }

        public DateTime? PassportExpiryDate { get; set; }

        public DateTime? FOMEMA_MedicalCheckupDate { get; set; }

        public DateTime? FOMEMA_1stAppealDate { get; set; }

        public DateTime? FOMEMA_2ndAppealDate { get; set; }

        public DateTime? FOMEMA_NCDMonitoringDate { get; set; }

        public DateTime? FOMEMA_ResultDate { get; set; }

    }
}
