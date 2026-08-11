
namespace OBMS.WebAPI.Models.DTO
{
    public class EmployeeDto
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
        public string? EMP_NATIONAL { get; set; }
        public string EMP_PHONE { get; set; }
        public string EMP_HGH_EDU { get; set; }
        public string EM_WORK_EXP { get; set; }
        public DateTime? EMP_DATE_OF_BIRTH { get; set; }
        public string EMP_IC_OLD { get; set; }
        public string EMP_IC_NEW { get; set; }
        public string EMP_IC_COLOR { get; set; }
        public string EMP_PASSPORT_NO { get; set; }
        public string EMP_SEX { get; set; }
        public string EMP_RACE { get; set; }
        public string EMP_MARTIAL_STATUS { get; set; }
        public string? EMP_SPOUSE_NAME { get; set; }
        public string? EMP_SP_IC { get; set; }
        public int? EMP_NO_CHILD { get; set; }
        public bool EMP_SP_WORK { get; set; }
        public string? EMP_PER_NAME_CONTACT { get; set; }
        public string EMP_CONTACT_ADDRESS1 { get; set; }
        public string EMP_CONTACT_ADDRESS2 { get; set; }
        public string EMP_CONTACT_POST_CODE { get; set; }
        public string EMP_CONTACT_TOWN { get; set; }
        public string EMP_CONTACT_STATE { get; set; }
        public string EMP_CONTACT_TELEPHONE { get; set; }
        public string EMP_BRANCH_CODE { get; set; }
        public string? OldBranch { get; set; }
        public DateTime? TransferDate { get; set; }
        public bool HasTransfered { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public string EMP_MOBILEPHONE { get; set; }
        public int EMP_CITIZEN { get; set; }
        public int EMP_CHECKLIST { get; set; }
        public string? EMP_CLIENT { get; set; }
        public char? NewSalaryStructure { get; set; }
        public bool KDNVetting { get; set; }
        public char? SalaryStructure1000_3h { get; set; }
        public DateTime? EMPPAY_DATE_RESIGNED { get; set; }
        public DateTime EMPPAY_DATE_JOINED { get; set; }
        public bool TMPGUARD { get; set; }
        public DateTime Period { get; set; }
        public bool EPFDETECT { get; set; }
        public bool SOCSODETECT { get; set; }
        public bool? INCOMETAXDETECT { get; set; }
        public bool DETECTBYND55 { get; set; }

        public DateTime? FOMEMA_MedicalCheckupDate { get; set; }

        public DateTime? FOMEMA_1stAppealDate { get; set; }

        public DateTime? FOMEMA_2ndAppealDate { get; set; }

        public DateTime? FOMEMA_NCDMonitoringDate { get; set; }

        public DateTime? FOMEMA_ResultDate { get; set; }
    }
}
