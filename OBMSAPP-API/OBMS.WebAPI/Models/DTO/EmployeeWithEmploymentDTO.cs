using OBMS.WebAPI.Models.Domain;

namespace OBMS.WebAPI.Models.DTO
{
    public class EmployeeWithEmploymentDTO
    {
        public Employee Employee { get; set; }
        public DateTime? EMPPAY_DATE_JOINED { get; set; }
        public string? EMPPAY_CATEGORY { get; set; }
    }
}
