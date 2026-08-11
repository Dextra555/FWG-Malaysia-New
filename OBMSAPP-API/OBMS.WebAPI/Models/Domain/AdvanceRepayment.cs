using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("AdvanceRepayment")]
    public class AdvanceRepayment
    {

        [Key]
        public int ID { get; set; }

        public decimal AdvanceID { get; set; }

        public decimal PaySlipID { get; set; }

        public decimal Amount { get; set; }

        public DateTime LastUpdate { get; set; }

        public string? LastUpdatedBy { get; set; }
    }

}
