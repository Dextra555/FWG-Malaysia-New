using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IApprovalRepository
    {
        // ── Pending Approvals ─────────────────────────────────────────────────
        /// <summary>
        /// Returns documents pending approval that are assigned to <paramref name="approverUser"/>.
        /// Only documents where ApprovalRouting maps the requester → this approver are returned.
        /// </summary>
        Task<List<PendingApprovalDto>> GetPendingApprovals(string approverUser);

        // ── Approval History ──────────────────────────────────────────────────
        Task<List<ApprovalHistoryDto>> GetApprovalHistory(string documentType, int documentID);
        Task<List<ApprovalHistoryDto>> GetAllApprovalHistory(string? documentType, int? documentID, DateTime? dateFrom, DateTime? dateTo);

        /// <summary>Write a single immutable history record.</summary>
        Task RecordHistory(string documentType, int documentID, string documentNumber,
                           string action, string actionBy,
                           string? previousStatus, string? newStatus, string? comments);

        // ── PO Submit + Approve + Reject ──────────────────────────────────────
        Task<(bool Success, string Message)> SubmitPO(int id, string currentUser);

        /// <summary>
        /// Approves a PO.  <paramref name="authenticatedUser"/> is read from the JWT —
        /// it must match the configured ApproverUser in ApprovalRouting.
        /// </summary>
        Task<(bool Success, string Message)> ApprovePOWithHistory(int id, string authenticatedUser, string? comments);

        /// <summary>
        /// Rejects a PO.  <paramref name="authenticatedUser"/> is read from the JWT —
        /// it must match the configured ApproverUser in ApprovalRouting.
        /// </summary>
        Task<(bool Success, string Message)> RejectPO(int id, string authenticatedUser, string rejectReason);

        // ── Approval Configuration ────────────────────────────────────────────
        Task<List<ApprovalConfigDto>> GetApprovalConfigurations();
        Task<bool> IsApprovalRequired(string documentType);
        Task<(bool Success, string Message)> UpdateApprovalConfiguration(int id, UpdateApprovalConfigDto dto);

        // ── Approval Routing (User-Wise) ──────────────────────────────────────

        /// <summary>Returns all routing rows (active + inactive) for the list screen.</summary>
        Task<List<ApprovalRoutingDto>> GetApprovalRoutings();

        /// <summary>Returns a single routing row by ID.</summary>
        Task<ApprovalRoutingDto?> GetApprovalRoutingById(int id);

        /// <summary>
        /// Creates a new row (ID == 0) or updates an existing row.
        /// Enforces: requester ≠ approver, no duplicate active mapping for the same
        /// DocumentType + RequesterUser combination.
        /// </summary>
        Task<(bool Success, string Message)> SaveApprovalRouting(SaveApprovalRoutingDto dto);

        /// <summary>Soft-deactivates a routing row (IsActive = false).</summary>
        Task<(bool Success, string Message)> DeactivateApprovalRouting(int id, string updatedBy);

        /// <summary>
        /// Looks up the configured approver for a given requester and document type.
        /// Returns null when no active routing row exists.
        /// </summary>
        Task<string?> GetApproverForUser(string requesterUser, string documentType);

        /// <summary>
        /// Verifies that <paramref name="authenticatedUser"/> is the configured approver
        /// for <paramref name="requesterUser"/> on the given document type.
        /// Used by ApprovePR / RejectPR / ApprovePO / RejectPO to authorise the action.
        /// </summary>
        Task<bool> IsAuthorizedApprover(string authenticatedUser, string requesterUser, string documentType);
    }
}
