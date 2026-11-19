namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Active employee/guard returned when selecting a recipient in New Stock Issue.
    /// Filtered by the warehouse's branch and by IssueToType (Guard / Staff).
    /// </summary>
    public class EmployeeForIssueDto
    {
        public int EMP_ID { get; set; }
        public string EMP_CODE { get; set; } = string.Empty;
        public string EMP_NAME { get; set; } = string.Empty;
        public string EMP_ROLE { get; set; } = string.Empty;
        public string EMP_BRANCH_CODE { get; set; } = string.Empty;
    }
}
