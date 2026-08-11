using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("RECEIPIENT")]
    public class Recipient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Address1 { get; set; }

        [Required]
        [StringLength(100)]
        public string Address2 { get; set; }

        [Required]
        [StringLength(5)]
        public string PostCode { get; set; }

        [Required]
        [StringLength(50)]
        public string City { get; set; }

        [Required]
        [StringLength(30)]
        public string State { get; set; }

        [Required]
        [StringLength(75)]
        public string Phone { get; set; }

        [Required]
        [StringLength(50)]
        public string Fax { get; set; }

        [Required]
        public decimal CreditLimit { get; set; }

        [Required]
        [StringLength(1)]
        public string Status { get; set; }

        [Required]
        [StringLength(50)]
        public string Supervisor { get; set; }

       // [Required]
        public DateTime? LASTUPDATE { get; set; }

        [StringLength(20)]
        public string? LastUpdatedBy { get; set; }

        [StringLength(20)]
        public string? ICNO { get; set; }

        [Column("category", TypeName = "int")]
        public int? Category { get; set; } 
}
}
