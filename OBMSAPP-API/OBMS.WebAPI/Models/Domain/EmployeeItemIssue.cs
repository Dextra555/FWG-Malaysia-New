using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("EmployeeItemIssue")]
    public class EmployeeItemIssue
    {
        [Key]
        public int ID { get; set; }
        public int AdvanceID { get; set; }
        public int ItemID { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
