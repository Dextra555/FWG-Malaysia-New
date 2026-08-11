using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("BranchMaster")]
    public partial class BranchMaster
    {
        [Key]      
        public int ID { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string PostCode { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Phone { get; set; }

        public string Fax { get; set; }

        public string BankName { get; set; }

        public string BankBranch { get; set; }

        public string BankAccount { get; set; }

        public string PersonIncharge { get; set; }

        public string Email { get; set; }

        public string Description { get; set; }

        public string ShortName { get; set; }

        public bool IsHeadQuarters { get; set; }

        public string UbsCode { get; set; }

        public DateTime LastUpdate { get; set; }

        public string? LastUpdatedBy { get; set; }

        public string? ParentBranch { get; set; }
    }
}
