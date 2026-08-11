using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("SalaryProcess")]
    public class SalaryProcess
    {
        [Key]
        public int ID { get; set; }
        public DateTime Period { get; set; }
        public string? Branch { get; set; }
        public string? EmployeeType { get; set; }
        public bool IsLocked { get; set; }
        public string? Remarks { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
