using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("Attendance")]
    public class Attendance
    {
        [Key]
        public int ID { get; set; }
        public DateTime Period { get; set; }
        public string? Branch { get; set; }
        public int EmployeeID { get; set; }
        public int Shift2Type { get; set; }
        public decimal Shift2Rate { get; set; }
        public decimal AllowanceDeduction { get; set; }
        public decimal SpecialAllowanceDeduction { get; set; }
        public decimal Bonus { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
        public decimal? KPIDeduction { get; set; }
    }
}
