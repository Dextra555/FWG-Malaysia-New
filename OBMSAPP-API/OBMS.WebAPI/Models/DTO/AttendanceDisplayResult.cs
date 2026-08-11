using Microsoft.EntityFrameworkCore;

namespace OBMS.WebAPI.Models.DTO
{
    [Keyless]
    public class AttendanceDisplayResult
    {
        public int ID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? Client { get; set; }
        public string? BranchName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string? Punch { get; set; }
        public int AttendanceID { get; set; }
    }
}
