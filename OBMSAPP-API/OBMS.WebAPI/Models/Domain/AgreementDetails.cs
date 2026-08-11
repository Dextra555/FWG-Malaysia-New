using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OBMS.WebAPI.Models.Domain
{
    [Table("AgreementDetails")]
    public class AgreementDetails
    {


        [Key]
        public int ID { get; set; }

        public int AgreementID { get; set; }

        [Required]
        public DateTime AgreementDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Client { get; set; }

        [Required]
        [StringLength(20)]
        public string Branch { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        public int NoOfGuards { get; set; }

        [Required]
        public decimal Rate { get; set; }

        [Required]
        public decimal NoOfHours { get; set; }

        [Required]
        public decimal NoOfDays { get; set; }

        [Required]
        public bool FollowCalender { get; set; }

        [Required]
        public bool HasDiscount { get; set; }

        [Required]
        public decimal DiscountAmount { get; set; }

        [Required]
        public bool IsTaxable { get; set; }

        [Required]
        public decimal TaxAmount { get; set; }

        [Required]
        public DateTime LASTUPDATE { get; set; }

        [StringLength(20)]
        public string? LastUpdatedBy { get; set; }

        [Required]
        public decimal MonthTotal { get; set; }

        [Required]
        public int DiscountHour { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(250)]
        public string? Reason { get; set; }
    }


}

