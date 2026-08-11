using OBMS.WebAPI.Models.Domain;

namespace OBMS.WebAPI.Models.DTO
{
    public class AttendanceModel
    {
        public Attendance? attendanceModel { get; set; }
        public List<AttendanceDetails>? attendanceDetails { get; set; }
    }
}
