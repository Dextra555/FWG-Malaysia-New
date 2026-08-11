using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("SalaryStructure")]
    public class SalaryStructure
    {
        [Key]
        public int SalaryId { get; set; }

        [MaxLength(20)]
        public string? BranchCode { get; set; }

        [MaxLength(10)]
        public string? EmployeeType { get; set; }

        [MaxLength(1)]
        public string? EmployeeNationality { get; set; }

        [MaxLength(20)]
        public string? Name { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal GeneralDayRate { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal GeneralDayHours { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal GeneralDayOTRate { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal OffDayRate { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal OffDayOTRate { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal HolidayRate { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal HolidayOTRate { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal WorkingDays { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal WorkingHours { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal SalaryBand { get; set; }

        [Column(TypeName = "numeric(18,7)")]
        public decimal TravelAllowance { get; set; }

        [MaxLength(20)]
        public string? LastUpdatedBy { get; set; }

        [MaxLength(8)]
        public string? Active { get; set; }

        [MaxLength(10)]
        public string? Status { get; set; }

        public DateTime LastUpdatedDate { get; set; }

        public bool EICC { get; set; }

        public bool NonStructure { get; set; }
    }

}
