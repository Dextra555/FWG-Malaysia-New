using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("MiscTrans")]
    public class MiscTrans
    {
        [Key]
        public int ID { get; set; }
        public DateTime TransDate { get; set; }
        public int EmployeeID { get; set; }
        public int TransType { get; set; }
        public decimal Amount { get; set; }
        public string? Particulars { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
