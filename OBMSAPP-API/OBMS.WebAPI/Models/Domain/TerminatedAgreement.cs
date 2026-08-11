using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("TerminatedAgreements")]
    public class TerminatedAgreement
    {

        [Key]
        public int ID { get; set; }

        [StringLength(20)]
        public string? Branch { get; set; }

        [StringLength(20)]
        public string? Client { get; set; }

        [StringLength(3950)]
        public string? Reason { get; set; }

        [Required]
        public DateTime TerminationDate { get; set; }

        [StringLength(3950)]
        public string? Note { get; set; }

        [Required]
        public DateTime LASTUPDATE { get; set; }

        [StringLength(20)]
        public string? LastUpdatedBy
        {
            get; set;
        }
    }
}
