using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ClientLegalDemandAction")]
    public class ClientLegalDemandAction
    {
        [Key]
        public int ID { get; set; }
        public string? Branch { get; set; }
        public string? Client { get; set; }
        public string? ActionTaken { get; set; }  // e.g., 'L' or other codes
        public DateTime DateIssue { get; set; }
        public string? Remarks { get; set; }
        public string? DeletionRemarks { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
