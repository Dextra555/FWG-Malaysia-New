using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OBMS.WebAPI.Models.DTO
{
    public class ObmsuserDto
    {
        public int UserId { get; set; }
       
        public string Password { get; set; } = null!;       
        
        public string Name { get; set; } = null!;
       
        public string Designation { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string CreatedBy { get; set; } = null!;

        public DateTime CreatedDate { get; set; }

        public DateTime LastUpdatedDate { get; set; }

        public bool IsDeleted { get; set; }

        public string? LastUpdatedBy { get; set; }

        public bool? IsView { get; set; }

        public bool? IsAdmin { get; set; }

        public string? Email { get; set; }

        public string? ContactNo { get; set; }
    }
}
