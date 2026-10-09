using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using System.Security.Claims;

namespace OBMS.WebAPI.Controllers
{
    /// <summary>
    /// Branch Stock Request endpoints.
    ///
    /// Flow:
    ///   1. POST SaveBSR          — create/update a Draft request
    ///   2. POST SubmitBSR/{id}   — Draft → Submitted (or Approved if no routing)
    ///   3. POST ApproveBSR/{id}  — Submitted → Approved  (approver only)
    ///   4. POST RejectBSR/{id}   — Submitted → Rejected  (approver only)
    ///   5. POST LinkBSRTransfer/{id} — link an Approved request to its StockTransfer
    ///
    /// The actual stock movement is done by the existing SaveStockTransfer endpoint
    /// (InventoryController). After creating the transfer the Angular form calls
    /// LinkBSRTransfer to record the link.
    ///
    /// Security:
    ///   All mutating actions (Submit/Approve/Reject) read the actor identity
    ///   from the JWT claim — client-supplied usernames are ignored.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockRequestController : ControllerBase
    {
        private readonly IInventoryRepository _repo;
        private readonly OBMSDbContext        _db;

        public StockRequestController(IInventoryRepository repo, OBMSDbContext db)
        {
            _repo = repo;
            _db   = db;
        }

        // ── JWT helper ────────────────────────────────────────────────────────

        private string GetJwtUser()
        {
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(name))
                throw new UnauthorizedAccessException(
                    "Unable to identify the current user. Please log in again.");
            return name;
        }

        // ════════════════════════════════════════════════════════════════════
        // LIST + READ
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Returns all active stock requests (list view — no detail lines).</summary>
        [HttpGet("GetBSRList")]
        public async Task<IActionResult> GetBSRList()
        {
            try
            {
                return Ok(await _repo.GetBSRList());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetBSRList] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load the stock request list right now. Please try again."
                });
            }
        }

        /// <summary>Returns a single stock request with full line items and available stock.</summary>
        [HttpGet("GetBSRByID")]
        public async Task<IActionResult> GetBSRByID(int ID)
        {
            try
            {
                var record = await _repo.GetBSRByID(ID);
                if (record == null)
                    return NotFound(new { success = false, message = "Stock request not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetBSRByID] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load the stock request right now. Please try again."
                });
            }
        }

        /// <summary>
        /// Returns the next auto-generated SR number (e.g. SR0001).
        /// Called on form init so the field is pre-populated.
        /// </summary>
        [HttpGet("GetBSRCode")]
        public IActionResult GetBSRCode()
        {
            try
            {
                var maxRow = _db.StockRequests
                    .Where(x => x.RequestNo.StartsWith("SR") && x.IsActive)
                    .OrderByDescending(x => x.RequestNo)
                    .FirstOrDefault();

                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.RequestNo))
                    ? "SR0001"
                    : int.TryParse(maxRow.RequestNo.Substring(2), out int n)
                        ? "SR" + (n + 1).ToString("D4")
                        : "SR0001";

                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetBSRCode] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to generate reference number. Please try again."
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // SAVE (create / update Draft)
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates a new (Draft) or updates an existing Draft stock request.
        /// RequestedBy is always stamped from the JWT — client value is ignored.
        /// </summary>
        [HttpPost("SaveBSR")]
        public async Task<IActionResult> SaveBSR([FromBody] StockRequestSaveDto dto)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Request body is required." });

            // ── Header validations ──────────────────────────────────────────
            if (dto.RequestingBranchID <= 0)
                return BadRequest(new { success = false, message = "Please select the requesting branch." });
            if (dto.SourceWarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select the source warehouse." });
            if (dto.RequestDate == default)
                return BadRequest(new { success = false, message = "Please enter the request date." });
            if (dto.Details == null || !dto.Details.Any())
                return BadRequest(new { success = false, message = "Please add at least one item to the request." });

            try
            {
                var currentUser = GetJwtUser();
                var (success, message, record) = await _repo.SaveBSR(dto, currentUser);
                if (!success)
                    return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, requestNo = record?.RequestNo });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveBSR] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to save the stock request right now. Please try again. " +
                              "If the problem continues, contact your administrator."
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // SUBMIT
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Submits a Draft stock request for approval (Draft → Submitted).
        /// If no active ApprovalRouting exists for this user + BRANCH_STOCK_REQUEST,
        /// the request is auto-approved immediately (direct-approve flow).
        /// </summary>
        [HttpPost("SubmitBSR/{id}")]
        public async Task<IActionResult> SubmitBSR(int id)
        {
            try
            {
                var currentUser = GetJwtUser();
                var (success, message) = await _repo.SubmitBSR(id, currentUser);
                if (!success)
                    return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SubmitBSR] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to submit the stock request right now. Please try again."
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // APPROVE
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Approves a Submitted stock request.
        /// The approver identity is read from the JWT — dto.ApprovedBy is overwritten.
        /// Returns 400 if the caller is not the configured approver.
        /// </summary>
        [HttpPost("ApproveBSR/{id}")]
        public async Task<IActionResult> ApproveBSR(int id, [FromBody] StockRequestApprovalDto dto)
        {
            try
            {
                var jwtUser = GetJwtUser();
                if (dto == null) dto = new StockRequestApprovalDto();
                dto.ApprovedBy   = jwtUser;                                   // always use JWT identity
                dto.IsSuperAdmin = User.FindFirst("IsAdmin")?.Value == "2";   // BUG-05 FIX

                var (success, message) = await _repo.ApproveBSR(id, dto);
                if (!success)
                    return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ApproveBSR] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to approve the stock request right now. Please try again."
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // REJECT
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Rejects a Submitted stock request.
        /// ApprovalRemarks (rejection reason) is mandatory.
        /// The rejector identity is read from the JWT.
        /// </summary>
        [HttpPost("RejectBSR/{id}")]
        public async Task<IActionResult> RejectBSR(int id, [FromBody] StockRequestApprovalDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.ApprovalRemarks))
                return BadRequest(new
                {
                    success = false,
                    message = "Please enter a reason for rejection so the requester knows what to correct."
                });

            try
            {
                var jwtUser = GetJwtUser();
                dto.ApprovedBy   = jwtUser;                                   // always use JWT identity
                dto.IsSuperAdmin = User.FindFirst("IsAdmin")?.Value == "2";   // BUG-05 FIX

                var (success, message) = await _repo.RejectBSR(id, dto);
                if (!success)
                    return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RejectBSR] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to reject the stock request right now. Please try again."
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Soft-deletes a Draft stock request.
        /// Submitted / Approved / Rejected requests cannot be deleted.
        /// </summary>
        [HttpPost("DeleteBSR/{id}")]
        public async Task<IActionResult> DeleteBSR(int id)
        {
            try
            {
                var currentUser = GetJwtUser();
                var (success, message) = await _repo.DeleteBSR(id, currentUser);
                if (!success)
                    return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DeleteBSR] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to delete the stock request right now. Please try again."
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // LINK TRANSFER
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Links an Approved stock request to a StockTransfer that has been
        /// created to fulfil it.  Called from the Angular form after SaveStockTransfer
        /// returns the new TransferID.
        /// </summary>
        [HttpPost("LinkBSRTransfer/{id}")]
        public async Task<IActionResult> LinkBSRTransfer(int id, [FromBody] StockRequestLinkTransferDto dto)
        {
            if (dto == null || dto.LinkedTransferID <= 0)
                return BadRequest(new
                {
                    success = false,
                    message = "Please provide a valid Stock Transfer ID."
                });

            try
            {
                var currentUser = GetJwtUser();
                var (success, message) = await _repo.LinkBSRTransfer(id, dto.LinkedTransferID, currentUser);
                if (!success)
                    return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[LinkBSRTransfer] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to link the transfer right now. Please try again."
                });
            }
        }
    }
}
