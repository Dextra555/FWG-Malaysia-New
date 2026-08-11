using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("StockIssueDetail")]
    public class StockIssueDetail
    {
        [Key]
        public int ID { get; set; }
        public int InvoiceID { get; set; }
        public int ItemID { get; set; }
        public int ItemCategoryID { get; set; }
        public int NoOfUnits { get; set; }
        public decimal CostPerUnit { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
