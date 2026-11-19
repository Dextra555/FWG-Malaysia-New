using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using System.Security.Claims;

namespace OBMS.WebAPI.Controllers
{
    /// <summary>
    /// Reusable Approval Workflow endpoints.
    ///
    /// Covers:
    ///   - Pending Approvals (user-wise filtered — only shows documents assigned to the caller)
    ///   - Approval History
    ///   - PO Submit / Approve / Reject   (PR actions stay in InventoryController)
    ///   - Approval Configuration management
    ///   - Approval Routing CRUD (user-wise requester → approver mapping)
    ///
    /// Security:
    ///   All approve / reject actions read the actor identity from the JWT claim
    ///   (HttpContext.User → ClaimTypes.Name).  Client-supplied "ApprovedBy" /
    ///   "RejectedBy" values are IGNORED for authorization purposes.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApprovalController : ControllerBase
    {
        private readonly IApprovalRepository _approval;

        public ApprovalController(IApprovalRepository approval)
        {
            _approval = approval;
        }

        // ── Helper ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the authenticated username from the JWT.
        /// Throws if the token does not contain a Name claim.
        /// </summary>
        private string GetJwtUser()
        {
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(name))
                throw new UnauthorizedAccessException(
                    "Unable to identify the current user. Please log in again.");
            return name;
        }

        /// <summary>
        /// Returns true when the authenticated user has the SuperAdmin role.
        /// SuperAdmin is determined by the JWT "IsAdmin" claim equal to "2",
        /// which is set at login from OBMSUsers.IsAdmin.
        /// BUG-01/02/03/04/05 FIX: replaces all hard-coded "superadmin" username checks.
        /// </summary>
        private bool GetJwtIsSuperAdmin()
        {
            return User.FindFirst("IsAdmin")?.Value == "2";
        }

        // ── Pending Approvals ─────────────────────────────────────────────────

        /// <summary>
        /// Returns documents currently pending approval that are assigned to the
        /// authenticated caller via ApprovalRouting.
        /// SuperAdmin (IsAdmin='2' in JWT) sees all pending documents.
        /// </summary>
        [HttpGet("GetPendingApprovals")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            try
            {
                var approverUser  = GetJwtUser();
                var isSuperAdmin  = GetJwtIsSuperAdmin(); // BUG-03 FIX
                var list = await _approval.GetPendingApprovals(approverUser, isSuperAdmin);
                return Ok(list);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to load pending approvals. Please try again.", detail = ex.Message });
            }
        }

        // ── Approval History ──────────────────────────────────────────────────

        /// <summary>Returns the approval history for a specific document.</summary>
        [HttpGet("GetApprovalHistory")]
        public async Task<IActionResult> GetApprovalHistory(
            [FromQuery] string documentType,
            [FromQuery] int documentID)
        {
            if (string.IsNullOrEmpty(documentType) || documentID <= 0)
                return BadRequest(new { message = "Document type and document ID are required." });

            try
            {
                var history = await _approval.GetApprovalHistory(documentType, documentID);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to load approval history. Please try again.", detail = ex.Message });
            }
        }

        /// <summary>Returns approval history with optional filters (for Approval History screen).</summary>
        [HttpGet("GetAllApprovalHistory")]
        public async Task<IActionResult> GetAllApprovalHistory(
            [FromQuery] string?   documentType = null,
            [FromQuery] int?      documentID   = null,
            [FromQuery] DateTime? dateFrom     = null,
            [FromQuery] DateTime? dateTo       = null)
        {
            try
            {
                var history = await _approval.GetAllApprovalHistory(documentType, documentID, dateFrom, dateTo);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to load approval history. Please try again.", detail = ex.Message });
            }
        }

        // ── PO Submit ─────────────────────────────────────────────────────────

        /// <summary>
        /// Submit a Purchase Order for approval (Draft → Submitted).
        /// Uses the JWT-authenticated username as the submitter.
        /// Returns 400 if no approver is configured for this user.
        /// </summary>
        [HttpPost("SubmitPO/{id}")]
        public async Task<IActionResult> SubmitPO(int id, [FromBody] SubmitDocumentDto dto)
        {
            try
            {
                // Use the JWT identity — ignore dto.CurrentUser for security
                var jwtUser = GetJwtUser();
                var (success, message) = await _approval.SubmitPO(id, jwtUser);
                if (!success) return BadRequest(new { message });
                return Ok(new { message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to submit purchase order. Please try again.", detail = ex.Message });
            }
        }

        // ── PO Approve ────────────────────────────────────────────────────────

        /// <summary>
        /// Approve a Purchase Order.
        /// The approver identity is read from the JWT — dto.ApprovedBy is ignored.
        /// Returns 403 if the caller is not the configured approver for this document's submitter.
        /// </summary>
        [HttpPost("ApprovePOWithHistory/{id}")]
        public async Task<IActionResult> ApprovePOWithHistory(
            int id, [FromBody] ApproveDocumentDto dto)
        {
            try
            {
                var jwtUser      = GetJwtUser();
                var isSuperAdmin = GetJwtIsSuperAdmin(); // BUG-04 FIX
                var (success, message) = await _approval.ApprovePOWithHistory(
                    id, jwtUser, isSuperAdmin, dto?.Comments);
                if (!success) return BadRequest(new { message });
                return Ok(new { message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to complete approval. Please try again.", detail = ex.Message });
            }
        }

        // ── PO Reject ─────────────────────────────────────────────────────────

        /// <summary>
        /// Reject a Purchase Order. Reject reason is mandatory.
        /// The rejector identity is read from the JWT — dto.RejectedBy is ignored.
        /// </summary>
        [HttpPost("RejectPO/{id}")]
        public async Task<IActionResult> RejectPO(int id, [FromBody] PORejectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.RejectReason))
                return BadRequest(new { message = "Please provide a rejection reason." });

            try
            {
                var jwtUser      = GetJwtUser();
                var isSuperAdmin = GetJwtIsSuperAdmin(); // BUG-04 FIX
                var (success, message) = await _approval.RejectPO(
                    id, jwtUser, isSuperAdmin, dto.RejectReason);
                if (!success) return BadRequest(new { message });
                return Ok(new { message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to reject purchase order. Please try again.", detail = ex.Message });
            }
        }

        // ── Approval Configuration ────────────────────────────────────────────

        /// <summary>Returns all document type approval configuration settings.</summary>
        [HttpGet("GetApprovalConfigurations")]
        public async Task<IActionResult> GetApprovalConfigurations()
        {
            try
            {
                var configs = await _approval.GetApprovalConfigurations();
                return Ok(configs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to load approval configurations.", detail = ex.Message });
            }
        }

        /// <summary>Update whether approval is required for a document type.</summary>
        [HttpPost("UpdateApprovalConfiguration/{id}")]
        public async Task<IActionResult> UpdateApprovalConfiguration(
            int id, [FromBody] UpdateApprovalConfigDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.LastUpdatedBy))
                return BadRequest(new { message = "User is required." });

            try
            {
                var (success, message) = await _approval.UpdateApprovalConfiguration(id, dto);
                if (!success) return BadRequest(new { message });
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to update approval configuration.", detail = ex.Message });
            }
        }

        // ── Approval Routing CRUD ─────────────────────────────────────────────

        /// <summary>
        /// Checks whether the currently authenticated user is the configured approver
        /// for a given requester and document type.
        /// Returns { IsApprover: true/false, ApproverUser: "..." }
        /// Angular uses this to show/hide Approve and Reject buttons.
        /// </summary>
        [HttpGet("IsCurrentUserApprover")]
        public async Task<IActionResult> IsCurrentUserApprover(
            [FromQuery] string documentType,
            [FromQuery] string requesterUser)
        {
            try
            {
                var jwtUser = GetJwtUser();

                // BUG-02 FIX: SuperAdmin is determined by JWT "IsAdmin" claim == "2"
                // (set from OBMSUsers.IsAdmin at login), NOT by hard-coded username string.
                bool isSuperAdmin = GetJwtIsSuperAdmin();

                // SuperAdmin has global approval authority including their own submissions.
                // Self-approval block does NOT apply to SuperAdmin.
                if (isSuperAdmin)
                {
                    return Ok(new
                    {
                        IsApprover   = true,
                        ApproverUser = jwtUser,
                        CurrentUser  = jwtUser
                    });
                }

                // Normal users: self-approval is always blocked.
                // Check ApprovalRouting — only the configured approver can approve.
                bool isSelfApproval = !string.IsNullOrWhiteSpace(requesterUser) &&
                                      string.Equals(jwtUser, requesterUser,
                                                    StringComparison.OrdinalIgnoreCase);

                var approverUser = await _approval.GetApproverForUser(requesterUser, documentType);
                bool isApprover  = !isSelfApproval &&
                                   !string.IsNullOrEmpty(approverUser) &&
                                   string.Equals(approverUser, jwtUser,
                                                 StringComparison.OrdinalIgnoreCase);

                return Ok(new
                {
                    IsApprover   = isApprover,
                    ApproverUser = approverUser ?? string.Empty,
                    CurrentUser  = jwtUser
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to check approver status.", detail = ex.Message });
            }
        }

        /// <summary>Returns all ApprovalRouting rows (active and inactive) for the admin screen.</summary>
        [HttpGet("GetApprovalRoutings")]        public async Task<IActionResult> GetApprovalRoutings()
        {
            try
            {
                var list = await _approval.GetApprovalRoutings();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to load approval routing configurations.", detail = ex.Message });
            }
        }

        /// <summary>Returns a single ApprovalRouting row by ID.</summary>
        [HttpGet("GetApprovalRoutingById/{id}")]
        public async Task<IActionResult> GetApprovalRoutingById(int id)
        {
            try
            {
                var row = await _approval.GetApprovalRoutingById(id);
                if (row == null) return NotFound(new { message = "Approval routing record not found." });
                return Ok(row);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to load approval routing record.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new or updates an existing ApprovalRouting row.
        /// Enforces: requester ≠ approver, no duplicate active mapping.
        /// </summary>
        [HttpPost("SaveApprovalRouting")]
        public async Task<IActionResult> SaveApprovalRouting([FromBody] SaveApprovalRoutingDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required." });

            // Stamp the authenticated user as last updater
            try
            {
                dto.LastUpdatedBy = GetJwtUser();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            try
            {
                var (success, message) = await _approval.SaveApprovalRouting(dto);
                if (!success) return BadRequest(new { message });
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to save approval routing configuration.", detail = ex.Message });
            }
        }

        /// <summary>Soft-deactivates an ApprovalRouting row (IsActive = false).</summary>
        [HttpPost("DeactivateApprovalRouting/{id}")]
        public async Task<IActionResult> DeactivateApprovalRouting(int id)
        {
            try
            {
                var updatedBy = GetJwtUser();
                var (success, message) = await _approval.DeactivateApprovalRouting(id, updatedBy);
                if (!success) return BadRequest(new { message });
                return Ok(new { message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to deactivate approval routing configuration.", detail = ex.Message });
            }
        }
    }
}
