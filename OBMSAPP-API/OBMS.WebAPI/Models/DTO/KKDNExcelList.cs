using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.DTO
{
    [Table("KKDNExcelListView")]
    [Keyless]
    public class KKDNExcelListView
    {
        public int EmpID { get; set; }
        public string BranchCode { get; set; }
        public string Name { get; set; }
        public string IC { get; set; }
        public DateTime DOB { get; set; }
        public string Nationality { get; set; }
        public string Citizen { get; set; }
        public string Race { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public DateTime DateJoin { get; set; }
        public string JobTitle { get; set; }
        public string EPF { get; set; }
        public string SOSCO { get; set; }
        public bool KDNVetting { get; set; }
        public bool HasTransfer { get; set; }
    }

}
