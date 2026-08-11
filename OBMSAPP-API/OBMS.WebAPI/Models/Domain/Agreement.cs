using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("Agreement")]
    public class Agreement
    {
        [Key]
        public int ID { get; set; }
        public string Branch { get; set; }
        public string Client { get; set; }

        [StringLength(2000)]
        public string WorkPlace { get; set; }
        public DateTime AgreementDate { get; set; }
        public DateTime AgreementEndDate { get; set; }

        [StringLength(4000)]
        public string Note { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        public bool? IsValid { get; set; }
    }
}
