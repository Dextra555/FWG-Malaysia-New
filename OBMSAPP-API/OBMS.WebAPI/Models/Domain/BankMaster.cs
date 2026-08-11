using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("BankMaster")]
    public class BankMaster
    {
        [Key]
        public int BankId { get; set; }
        public string BankCode { get; set; }
        public string Accname { get; set; }
        public string Accno { get; set; }
        public string PREFIX { get; set; }
        public string AccShortName { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string LastUpdatedBy { get; set; }
    }
}
