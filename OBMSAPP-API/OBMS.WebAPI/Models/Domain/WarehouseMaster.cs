using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("WarehouseMaster")]
    public class WarehouseMaster
    {
        [Key]
        public int ID { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public int BranchID { get; set; }

        public bool IsActive { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        [ForeignKey("BranchID")]
        public virtual BranchMaster Branch { get; set; }
    }
}