namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload for ApproveSA / RejectSA endpoints.
    /// ApprovedBy and IsSuperAdmin are ALWAYS overwritten by the controller
    /// from the JWT — never trust client-supplied values for authorization.
    /// </summary>
    public class SAApprovalDto
    {
        /// <summary>Overwritten server-side from JWT ClaimTypes.Name.</summary>
        public string ApprovedBy { get; set; } = string.Empty;

        /// <summary>Overwritten server-side from JWT "IsAdmin" == "2".</summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>Required for Reject; optional for Approve.</summary>
        public string? ApprovalRemarks { get; set; }
    }
}
