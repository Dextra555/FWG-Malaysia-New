using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("OBMSBanks")]
    public class OBMSBanks
    {
        [Key]
        public int ID { get; set; }
        public string? Name { get; set; }
        public int BankID { get; set; }
        public bool? IsAllowed { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
