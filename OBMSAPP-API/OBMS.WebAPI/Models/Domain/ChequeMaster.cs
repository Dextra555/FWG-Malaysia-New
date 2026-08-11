using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ChequeMaster")]
    public class ChequeMaster
    {
        [Key]
        public int ID { get; set; }
        public int BankID { get; set; }
        public int ChequeStart { get; set; }
        public int ChequeEnd { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
