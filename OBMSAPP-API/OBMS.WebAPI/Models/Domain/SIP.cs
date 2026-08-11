using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("SIP")]
    public class SIP
    {
        [Key]
        public int SIP_id { get; set; }
        public decimal SIP_from { get; set; }
        public decimal SIP_to { get; set; }
        public decimal SIP_worker { get; set; }
        public decimal SIP_boss { get; set; }
        public decimal SIP_total { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
