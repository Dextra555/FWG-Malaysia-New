using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;

namespace OBMS.WebAPI.Repositories.Implementation
{
    /// <summary>
    /// Handles cross-document approval workflow:
    ///   - Pending approvals (user-wise filtered via ApprovalRouting)
    ///   - Immutable approval history recording
    ///   - PO submit / approve / reject with JWT-identity security
    ///   - Approval configuration management
    ///   - ApprovalRouting CRUD (user-wise requester→approver mapping)
    ///
    /// PR approve/reject/submit remain in InventoryRepository and call
    /// RecordHistory + GetApproverForUser on this repository.
    /// </summary>
    public class ApprovalRepository : IApprovalRepository
    {
        private readonly OBMSDbContext _db;
        private readonly INotificationRepository _notif;

        public ApprovalRepository(OBMSDbContext db, INotificationRepository notif)
        {
            _db    = db;
            _notif = notif;
        }

        // ════════════════════════════════════════════════════════════════════
        // PENDING APPROVALS  — user-wise filtered
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns only documents whose RequesterUser maps to
        /// <paramref name="approverUser"/> via an active ApprovalRouting row.
        /// Superadmin sees everything.
        /// </summary>
        public async Task<List<PendingApprovalDto>> GetPendingApprovals(string approverUser, bool isSuperAdmin)
        {
            var result = new List<PendingApprovalDto>();
            var now    = DateTime.Now;
            // BUG-03 FIX: isSuperAdmin is now passed in from the controller where it is
            // derived from the JWT "IsAdmin" claim (value "2"). The old hard-coded
            // string.Equals(approverUser, "superadmin") check has been removed.

            // ── Collect all requester users whose approver = approverUser ──
            List<string> allowedRequesters;
            if (isSuperAdmin)
            {
                allowedRequesters = new List<string>(); // sentinel — see below
            }
            else
            {
                // Use EF Core–translatable comparison (no .ToLower() on the DB column)
                var lowerApprover = approverUser.ToLower();
                allowedRequesters = await _db.ApprovalRoutings
                    .Where(r => r.IsActive &&
                                r.ApproverUser.ToLower() == lowerApprover)
                    .Select(r => r.RequesterUser)   // keep original casing from DB
                    .ToListAsync();

                // No routing configured → return empty list immediately
                if (!allowedRequesters.Any())
                    return result;
            }

            // Build a lowercase set for case-insensitive matching.
            // This is necessary because:
            //   - allowedRequesters contains OBMSUsers.Name (e.g. "Dharan") as stored in DB
            //   - SubmittedBy contains the JWT username typed at login (e.g. "dharan")
            // DB collation may be CI or CS — we cannot rely on it.
            // Materialising both to lowercase in-memory guarantees consistent behaviour.
            var lowerAllowedRequesters = allowedRequesters
                .Select(r => r.ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // ── Purchase Requisitions ──
            // Fetch ALL submitted PRs from DB first (no in-memory lambda in EF predicate),
            // then filter case-insensitively in memory.
            var allSubmittedPRs = await _db.PurchaseRequisitions
                .Where(p => p.Status == "Submitted" && p.IsActive)
                .OrderBy(p => p.SubmittedDate ?? p.LASTUPDATE)
                .Select(p => new
                {
                    p.ID, p.PRNumber, p.PRDate, p.SubmittedBy,
                    p.LastUpdatedBy, p.SubmittedDate, p.LASTUPDATE, p.Status
                })
                .ToListAsync();

            var filteredPRs = isSuperAdmin
                ? allSubmittedPRs
                : allSubmittedPRs
                    .Where(p => p.SubmittedBy != null &&
                                lowerAllowedRequesters.Contains(p.SubmittedBy.ToLowerInvariant()))
                    .ToList();

            // Resolve item counts for the filtered set only (avoids N+1 for the full table)
            var prIds = filteredPRs.Select(p => p.ID).ToHashSet();
            var prItemCounts = await _db.PurchaseRequisitionDetails
                .Where(d => prIds.Contains(d.PRID))
                .GroupBy(d => d.PRID)
                .Select(g => new { PRID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PRID, x => x.Count);

            var prs = filteredPRs.Select(p => new PendingApprovalDto
            {
                DocumentType        = "PURCHASE_REQUISITION",
                DocumentTypeDisplay = "Purchase Requisition",
                DocumentID          = p.ID,
                DocumentNumber      = p.PRNumber,
                DocumentDate        = p.PRDate,
                RequestedBy         = p.SubmittedBy ?? p.LastUpdatedBy ?? string.Empty,
                SubmittedDate       = p.SubmittedDate ?? p.LASTUPDATE,
                CurrentStatus       = p.Status,
                WarehouseName       = null,
                BranchName          = null,
                TotalAmount         = null,
                ItemCount           = prItemCounts.TryGetValue(p.ID, out var c) ? c : 0
            }).ToList();

            foreach (var dto in prs)
                dto.PendingSinceDays = dto.SubmittedDate.HasValue
                    ? (int)(now - dto.SubmittedDate.Value).TotalDays : 0;

            result.AddRange(prs);

            // ── Purchase Orders ──
            // Fetch ALL submitted POs from DB first, then filter case-insensitively in memory.
            var allSubmittedPOs = await (
                from po in _db.PurchaseOrders
                join wh in _db.WarehouseMasters on po.WarehouseID equals wh.ID into whJoin
                from wh in whJoin.DefaultIfEmpty()
                where po.Status == "Submitted" && po.IsActive
                orderby po.SubmittedDate ?? po.LASTUPDATE
                select new
                {
                    po.ID, po.PONumber, po.PODate, po.SubmittedBy,
                    po.LastUpdatedBy, po.SubmittedDate, po.LASTUPDATE,
                    po.Status, po.TotalAmount,
                    WarehouseName = wh != null ? wh.Name : string.Empty
                })
                .ToListAsync();

            var filteredPOs = isSuperAdmin
                ? allSubmittedPOs
                : allSubmittedPOs
                    .Where(x => x.SubmittedBy != null &&
                                lowerAllowedRequesters.Contains(x.SubmittedBy.ToLowerInvariant()))
                    .ToList();

            var pos = filteredPOs.Select(x => new PendingApprovalDto
            {
                DocumentType        = "PURCHASE_ORDER",
                DocumentTypeDisplay = "Purchase Order",
                DocumentID          = x.ID,
                DocumentNumber      = x.PONumber,
                DocumentDate        = x.PODate,
                RequestedBy         = x.SubmittedBy ?? x.LastUpdatedBy ?? string.Empty,
                SubmittedDate       = x.SubmittedDate ?? x.LASTUPDATE,
                CurrentStatus       = x.Status,
                WarehouseName       = x.WarehouseName,
                BranchName          = null,
                TotalAmount         = x.TotalAmount
            }).ToList();

            foreach (var dto in pos)
                dto.PendingSinceDays = dto.SubmittedDate.HasValue
                    ? (int)(now - dto.SubmittedDate.Value).TotalDays : 0;

            result.AddRange(pos);

            // ── Branch Stock Requests ──────────────────────────────────────────
            var allSubmittedBSRs = await (
                from sr in _db.StockRequests
                join br in _db.BranchMasters on sr.RequestingBranchID equals br.ID into brJoin
                from br in brJoin.DefaultIfEmpty()
                where sr.Status == "Submitted" && sr.IsActive
                orderby sr.SubmittedDate ?? sr.LASTUPDATE
                select new
                {
                    sr.ID, sr.RequestNo, sr.RequestDate, sr.SubmittedBy,
                    sr.LastUpdatedBy, sr.SubmittedDate, sr.LASTUPDATE, sr.Status,
                    BranchName = br != null ? br.Name : string.Empty
                })
                .ToListAsync();

            var filteredBSRs = isSuperAdmin
                ? allSubmittedBSRs
                : allSubmittedBSRs
                    .Where(x => x.SubmittedBy != null &&
                                lowerAllowedRequesters.Contains(x.SubmittedBy.ToLowerInvariant()))
                    .ToList();

            var bsrIds       = filteredBSRs.Select(x => x.ID).ToHashSet();
            var bsrItemCounts = await _db.StockRequestDetails
                .Where(d => bsrIds.Contains(d.RequestID))
                .GroupBy(d => d.RequestID)
                .Select(g => new { RequestID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RequestID, x => x.Count);

            var bsrs = filteredBSRs.Select(x => new PendingApprovalDto
            {
                DocumentType        = "BRANCH_STOCK_REQUEST",
                DocumentTypeDisplay = "Branch Stock Request",
                DocumentID          = x.ID,
                DocumentNumber      = x.RequestNo,
                DocumentDate        = x.RequestDate,
                RequestedBy         = x.SubmittedBy ?? x.LastUpdatedBy ?? string.Empty,
                SubmittedDate       = x.SubmittedDate ?? x.LASTUPDATE,
                CurrentStatus       = x.Status,
                WarehouseName       = null,
                BranchName          = x.BranchName,
                TotalAmount         = null,
                ItemCount           = bsrItemCounts.TryGetValue(x.ID, out var bc) ? bc : 0
            }).ToList();

            foreach (var dto in bsrs)
                dto.PendingSinceDays = dto.SubmittedDate.HasValue
                    ? (int)(now - dto.SubmittedDate.Value).TotalDays : 0;

            result.AddRange(bsrs);

            // ── Stock Adjustments ──────────────────────────────────────────────
            var allSubmittedSAs = await (
                from sa in _db.StockAdjustments
                join wh in _db.WarehouseMasters on sa.WarehouseID equals wh.ID into whJoin
                from wh in whJoin.DefaultIfEmpty()
                where sa.Status == "Submitted" && sa.IsActive
                orderby sa.SubmittedDate ?? sa.LASTUPDATE
                select new
                {
                    sa.ID, sa.AdjustmentNo, sa.AdjustmentDate, sa.SubmittedBy,
                    sa.LastUpdatedBy, sa.SubmittedDate, sa.LASTUPDATE, sa.Status,
                    WarehouseName = wh != null ? wh.Name : string.Empty
                })
                .ToListAsync();

            var filteredSAs = isSuperAdmin
                ? allSubmittedSAs
                : allSubmittedSAs
                    .Where(x => x.SubmittedBy != null &&
                                lowerAllowedRequesters.Contains(x.SubmittedBy.ToLowerInvariant()))
                    .ToList();

            var sas = filteredSAs.Select(x => new PendingApprovalDto
            {
                DocumentType        = "STOCK_ADJUSTMENT",
                DocumentTypeDisplay = "Stock Adjustment",
                DocumentID          = x.ID,
                DocumentNumber      = x.AdjustmentNo,
                DocumentDate        = x.AdjustmentDate,
                RequestedBy         = x.SubmittedBy ?? x.LastUpdatedBy ?? string.Empty,
                SubmittedDate       = x.SubmittedDate ?? x.LASTUPDATE,
                CurrentStatus       = x.Status,
                WarehouseName       = x.WarehouseName,
                BranchName          = null,
                TotalAmount         = null,
                ItemCount           = 1   // SA is always single-item
            }).ToList();

            foreach (var dto in sas)
                dto.PendingSinceDays = dto.SubmittedDate.HasValue
                    ? (int)(now - dto.SubmittedDate.Value).TotalDays : 0;

            result.AddRange(sas);

            // ── Asset Disposals ────────────────────────────────────────────────
            var allSubmittedADs = await (
                from ad in _db.AssetDisposals
                join a in _db.AssetMasters on ad.AssetID equals a.ID into aJoin
                from a in aJoin.DefaultIfEmpty()
                where ad.Status == "Submitted" && ad.IsActive
                orderby ad.SubmittedDate ?? ad.LASTUPDATE
                select new
                {
                    ad.ID, ad.DisposalNo, ad.DisposalDate, ad.SubmittedBy,
                    ad.LastUpdatedBy, ad.SubmittedDate, ad.LASTUPDATE, ad.Status,
                    AssetName = a != null ? a.Name : string.Empty
                })
                .ToListAsync();

            var filteredADs = isSuperAdmin
                ? allSubmittedADs
                : allSubmittedADs
                    .Where(x => x.SubmittedBy != null &&
                                lowerAllowedRequesters.Contains(x.SubmittedBy.ToLowerInvariant()))
                    .ToList();

            var ads = filteredADs.Select(x => new PendingApprovalDto
            {
                DocumentType        = "ASSET_DISPOSAL",
                DocumentTypeDisplay = "Asset Disposal",
                DocumentID          = x.ID,
                DocumentNumber      = x.DisposalNo,
                DocumentDate        = x.DisposalDate,
                RequestedBy         = x.SubmittedBy ?? x.LastUpdatedBy ?? string.Empty,
                SubmittedDate       = x.SubmittedDate ?? x.LASTUPDATE,
                CurrentStatus       = x.Status,
                WarehouseName       = x.AssetName,  // reuse WarehouseName field to show asset name
                BranchName          = null,
                TotalAmount         = null,
                ItemCount           = 1
            }).ToList();

            foreach (var dto in ads)
                dto.PendingSinceDays = dto.SubmittedDate.HasValue
                    ? (int)(now - dto.SubmittedDate.Value).TotalDays : 0;

            result.AddRange(ads);

            return result.OrderBy(r => r.SubmittedDate).ToList();
        }

        // ════════════════════════════════════════════════════════════════════
        // APPROVAL HISTORY
        // ════════════════════════════════════════════════════════════════════

        public async Task<List<ApprovalHistoryDto>> GetApprovalHistory(
            string documentType, int documentID)
        {
            return await _db.ApprovalHistories
                .Where(h => h.DocumentType == documentType && h.DocumentID == documentID)
                .OrderBy(h => h.ActionDate)
                .Select(h => new ApprovalHistoryDto
                {
                    ID             = h.ID,
                    DocumentType   = h.DocumentType,
                    DocumentID     = h.DocumentID,
                    DocumentNumber = h.DocumentNumber,
                    Action         = h.Action,
                    ActionBy       = h.ActionBy,
                    ActionDate     = h.ActionDate,
                    Comments       = h.Comments,
                    PreviousStatus = h.PreviousStatus,
                    NewStatus      = h.NewStatus
                })
                .ToListAsync();
        }

        public async Task<List<ApprovalHistoryDto>> GetAllApprovalHistory(
            string? documentType, int? documentID,
            DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _db.ApprovalHistories.AsQueryable();

            if (!string.IsNullOrEmpty(documentType))
                query = query.Where(h => h.DocumentType == documentType);
            if (documentID.HasValue)
                query = query.Where(h => h.DocumentID == documentID.Value);
            if (dateFrom.HasValue)
                query = query.Where(h => h.ActionDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(h => h.ActionDate <= dateTo.Value.AddDays(1));

            return await query
                .OrderByDescending(h => h.ActionDate)
                .Take(500)
                .Select(h => new ApprovalHistoryDto
                {
                    ID             = h.ID,
                    DocumentType   = h.DocumentType,
                    DocumentID     = h.DocumentID,
                    DocumentNumber = h.DocumentNumber,
                    Action         = h.Action,
                    ActionBy       = h.ActionBy,
                    ActionDate     = h.ActionDate,
                    Comments       = h.Comments,
                    PreviousStatus = h.PreviousStatus,
                    NewStatus      = h.NewStatus
                })
                .ToListAsync();
        }

        public async Task RecordHistory(
            string documentType, int documentID, string documentNumber,
            string action, string actionBy,
            string? previousStatus, string? newStatus, string? comments)
        {
            _db.ApprovalHistories.Add(new ApprovalHistory
            {
                DocumentType   = documentType,
                DocumentID     = documentID,
                DocumentNumber = documentNumber,
                Action         = action,
                ActionBy       = actionBy,
                ActionDate     = DateTime.Now,
                PreviousStatus = previousStatus,
                NewStatus      = newStatus,
                Comments       = comments
            });
            await _db.SaveChangesAsync();
        }

        // ════════════════════════════════════════════════════════════════════
        // PO SUBMIT
        // ════════════════════════════════════════════════════════════════════

        public async Task<(bool Success, string Message)> SubmitPO(int id, string currentUser)
        {
            var po = await _db.PurchaseOrders.FindAsync(id);
            if (po == null)
                return (false, "This purchase order could not be found.");
            if (po.Status != "Draft")
                return (false, $"Only a Draft purchase order can be submitted. This order is currently {po.Status}.");

            var hasDetails = await _db.PurchaseOrderDetails.AnyAsync(x => x.POID == id);
            if (!hasDetails)
                return (false, "Please add at least one item to the purchase order before submitting.");

            // ── BUG-06 FIX: ApprovalConfiguration is now the master on/off switch. ──
            // If ApprovalRequired = false for PURCHASE_ORDER → direct-approve flow regardless
            // of any ApprovalRouting configuration.
            bool approvalRequired = await IsApprovalRequired("PURCHASE_ORDER");
            if (!approvalRequired)
            {
                // Approval is turned off for POs → auto-approve immediately.
                po.Status        = "Approved";
                po.ApprovedBy    = currentUser;
                po.ApprovedDate  = DateTime.Now;
                po.SubmittedBy   = currentUser;
                po.SubmittedDate = DateTime.Now;
                po.LASTUPDATE    = DateTime.Now;
                po.LastUpdatedBy = currentUser;
                await _db.SaveChangesAsync();
                return (true, $"Purchase Order {po.PONumber} has been approved. Goods can now be received against this order.");
            }

            // ── Routing check ──────────────────────────────────────────────
            // Approval is required. Check whether an active routing row exists
            // for this requester → approver mapping.
            // If no routing exists → direct-approve flow (no approver configured for this user).
            // If routing exists → submit for approval flow.
            var approverUser = await GetApproverForUser(currentUser, "PURCHASE_ORDER");
            if (string.IsNullOrEmpty(approverUser))
            {
                // ── No approval routing configured → direct approved flow ──
                // PO goes straight to Approved so GRN can proceed immediately.
                // No Pending Approval, no notification, no ApprovalHistory.
                po.Status        = "Approved";
                po.ApprovedBy    = currentUser;
                po.ApprovedDate  = DateTime.Now;
                po.SubmittedBy   = currentUser;
                po.SubmittedDate = DateTime.Now;
                po.LASTUPDATE    = DateTime.Now;
                po.LastUpdatedBy = currentUser;
                await _db.SaveChangesAsync();
                return (true, $"Purchase Order {po.PONumber} has been approved. Goods can now be received against this order.");
            }

            // Capture values used after the transaction commits (avoid closure over
            // mutable po fields that may change if EF reloads the entity).
            string poNumber = po.PONumber;
            int    poID     = po.ID;

            var strategy = _db.Database.CreateExecutionStrategy();
            var (ok, msg) = await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    string prev      = po.Status;
                    po.Status        = "Submitted";
                    po.SubmittedBy   = currentUser;
                    po.SubmittedDate = DateTime.Now;
                    po.LASTUPDATE    = DateTime.Now;
                    po.LastUpdatedBy = currentUser;
                    await _db.SaveChangesAsync();

                    await RecordHistory("PURCHASE_ORDER", poID, poNumber,
                        "Submitted", currentUser, prev, "Submitted", null);

                    await tx.CommitAsync();
                    return (true, $"Purchase Order {poNumber} has been submitted for approval.");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    return (false, $"Unable to submit purchase order. {ex.Message}");
                }
            });

            // Send notification AFTER the transaction is committed so the
            // notification's own SaveChangesAsync runs on a clean (non-transacted)
            // DbContext connection and is not silently swallowed.
            if (ok)
            {
                await _notif.SendApprovalNotification(
                    "PENDING_APPROVAL", "PURCHASE_ORDER", poNumber, poID,
                    approverUser, currentUser, null);
            }

            return (ok, msg);
        }

        // ════════════════════════════════════════════════════════════════════
        // PO APPROVE — JWT identity enforced
        // ════════════════════════════════════════════════════════════════════

        public async Task<(bool Success, string Message)> ApprovePOWithHistory(
            int id, string authenticatedUser, bool isSuperAdmin, string? comments)
        {
            var po = await _db.PurchaseOrders.FindAsync(id);
            if (po == null)
                return (false, "This purchase order could not be found.");

            if (po.Status != "Draft" && po.Status != "Submitted")
                return (false,
                    $"This purchase order cannot be approved in its current status ({po.Status}).");

            // ── Security: verify the caller is the configured approver ─────
            // BUG-04 FIX: isSuperAdmin is passed from the controller where it is
            // derived from the JWT "IsAdmin" claim. No more hard-coded "superadmin" string.
            var submitter = po.SubmittedBy ?? po.LastUpdatedBy ?? string.Empty;
            var authorized = await IsAuthorizedApprover(
                authenticatedUser, submitter, "PURCHASE_ORDER");

            // SuperAdmin has global authority — self-approval block does not apply.
            // Normal users: cannot approve their own submission.
            bool isSelfApproval  = !isSuperAdmin &&
                                   !string.IsNullOrWhiteSpace(submitter) &&
                                   string.Equals(authenticatedUser, submitter,
                                                 StringComparison.OrdinalIgnoreCase);
            if (isSelfApproval)
                return (false, "You cannot approve a document that you submitted yourself.");
            if (!authorized && !isSuperAdmin)
                return (false, "You are not authorized to approve this document.");

            string poNumberApprove  = po.PONumber;
            int    poIDApprove      = po.ID;
            string? submittedByApprove = po.SubmittedBy;

            var strategy = _db.Database.CreateExecutionStrategy();
            var (okA, msgA) = await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    string prev      = po.Status;
                    po.Status        = "Approved";
                    po.ApprovedBy    = authenticatedUser;
                    po.ApprovedDate  = DateTime.Now;
                    po.LASTUPDATE    = DateTime.Now;
                    po.LastUpdatedBy = authenticatedUser;
                    await _db.SaveChangesAsync();

                    await RecordHistory("PURCHASE_ORDER", poIDApprove, poNumberApprove,
                        "Approved", authenticatedUser, prev, "Approved", comments);

                    await tx.CommitAsync();
                    return (true,
                        $"Purchase Order {poNumberApprove} has been approved. " +
                        "Goods can now be received against this order.");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    return (false, $"Unable to approve purchase order. {ex.Message}");
                }
            });

            // Send notification after commit — avoids DbContext conflict inside transaction
            if (okA && !string.IsNullOrEmpty(submittedByApprove))
                await _notif.SendApprovalNotification(
                    "APPROVED", "PURCHASE_ORDER", poNumberApprove, poIDApprove,
                    submittedByApprove, authenticatedUser, comments);

            return (okA, msgA);
        }

        // ════════════════════════════════════════════════════════════════════
        // PO REJECT — JWT identity enforced
        // ════════════════════════════════════════════════════════════════════

        public async Task<(bool Success, string Message)> RejectPO(
            int id, string authenticatedUser, bool isSuperAdmin, string rejectReason)
        {
            if (string.IsNullOrWhiteSpace(rejectReason))
                return (false,
                    "Please provide a rejection reason so the requester knows what to correct.");

            var po = await _db.PurchaseOrders.FindAsync(id);
            if (po == null)
                return (false, "This purchase order could not be found.");
            if (po.Status != "Draft" && po.Status != "Submitted")
                return (false,
                    $"This purchase order cannot be rejected in its current status ({po.Status}).");

            // ── Security: verify caller is the configured approver ─────────
            // BUG-04 FIX: isSuperAdmin is passed from the controller where it is
            // derived from the JWT "IsAdmin" claim. No more hard-coded "superadmin" string.
            var submitter = po.SubmittedBy ?? po.LastUpdatedBy ?? string.Empty;
            var authorized = await IsAuthorizedApprover(
                authenticatedUser, submitter, "PURCHASE_ORDER");

            // SuperAdmin has global authority — self-rejection block does not apply.
            // Normal users: cannot reject their own submission.
            bool isSelfRejection = !isSuperAdmin &&
                                   !string.IsNullOrWhiteSpace(submitter) &&
                                   string.Equals(authenticatedUser, submitter,
                                                 StringComparison.OrdinalIgnoreCase);
            if (isSelfRejection)
                return (false, "You cannot reject a document that you submitted yourself.");
            if (!authorized && !isSuperAdmin)
                return (false, "You are not authorized to reject this document.");

            string poNumberReject     = po.PONumber;
            int    poIDReject         = po.ID;
            string? submittedByReject = po.SubmittedBy;

            var strategy = _db.Database.CreateExecutionStrategy();
            var (okR, msgR) = await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    string prev      = po.Status;
                    po.Status        = "Rejected";
                    po.ApprovedBy    = authenticatedUser;
                    po.ApprovedDate  = DateTime.Now;
                    po.RejectReason  = rejectReason;
                    po.LASTUPDATE    = DateTime.Now;
                    po.LastUpdatedBy = authenticatedUser;
                    await _db.SaveChangesAsync();

                    await RecordHistory("PURCHASE_ORDER", poIDReject, poNumberReject,
                        "Rejected", authenticatedUser, prev, "Rejected", rejectReason);

                    await tx.CommitAsync();
                    return (true, $"Purchase Order {poNumberReject} has been rejected.");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    return (false, $"Unable to reject purchase order. {ex.Message}");
                }
            });

            // Send notification after commit — avoids DbContext conflict inside transaction
            if (okR && !string.IsNullOrEmpty(submittedByReject))
                await _notif.SendApprovalNotification(
                    "REJECTED", "PURCHASE_ORDER", poNumberReject, poIDReject,
                    submittedByReject, authenticatedUser, rejectReason);

            return (okR, msgR);
        }

        // ════════════════════════════════════════════════════════════════════
        // APPROVAL CONFIGURATION
        // ════════════════════════════════════════════════════════════════════

        public async Task<List<ApprovalConfigDto>> GetApprovalConfigurations()
        {
            return await _db.ApprovalConfigurations
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayName)
                .Select(c => new ApprovalConfigDto
                {
                    ID               = c.ID,
                    DocumentType     = c.DocumentType,
                    DisplayName      = c.DisplayName,
                    ApprovalRequired = c.ApprovalRequired,
                    ApproverRole     = c.ApproverRole,
                    Remarks          = c.Remarks
                })
                .ToListAsync();
        }

        public async Task<bool> IsApprovalRequired(string documentType)
        {
            var config = await _db.ApprovalConfigurations
                .FirstOrDefaultAsync(c => c.DocumentType == documentType && c.IsActive);
            return config?.ApprovalRequired ?? false;
        }

        public async Task<(bool Success, string Message)> UpdateApprovalConfiguration(
            int id, UpdateApprovalConfigDto dto)
        {
            var config = await _db.ApprovalConfigurations.FindAsync(id);
            if (config == null)
                return (false, "Approval configuration record not found.");

            config.ApprovalRequired = dto.ApprovalRequired;
            config.ApproverRole     = dto.ApproverRole;
            config.Remarks          = dto.Remarks;
            config.LASTUPDATE       = DateTime.Now;
            config.LastUpdatedBy    = dto.LastUpdatedBy;
            await _db.SaveChangesAsync();
            return (true, "Approval configuration updated successfully.");
        }

        // ════════════════════════════════════════════════════════════════════
        // APPROVAL ROUTING — CRUD
        // ════════════════════════════════════════════════════════════════════

        private static readonly Dictionary<string, string> _docTypeDisplayNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["PURCHASE_REQUISITION"] = "Purchase Requisition",
                ["PURCHASE_ORDER"]       = "Purchase Order",
                ["BRANCH_STOCK_REQUEST"] = "Branch Stock Request",   // BUG-E FIX: was missing
                ["STOCK_ADJUSTMENT"]     = "Stock Adjustment",
                ["STOCK_TRANSFER"]       = "Stock Transfer",
                ["DAMAGE_LOSS"]          = "Damage / Loss",
                ["ASSET_DISPOSAL"]       = "Asset Disposal"
            };

        private static string DocTypeDisplay(string docType) =>
            _docTypeDisplayNames.TryGetValue(docType, out var d) ? d : docType;

        public async Task<List<ApprovalRoutingDto>> GetApprovalRoutings()
        {
            return await _db.ApprovalRoutings
                .OrderBy(r => r.DocumentType)
                .ThenBy(r => r.RequesterUser)
                .Select(r => new ApprovalRoutingDto
                {
                    ID               = r.ID,
                    DocumentType     = r.DocumentType,
                    DocumentTypeDisplay = r.DocumentType == "PURCHASE_REQUISITION" ? "Purchase Requisition"
                                       : r.DocumentType == "PURCHASE_ORDER"        ? "Purchase Order"
                                       : r.DocumentType == "BRANCH_STOCK_REQUEST"  ? "Branch Stock Request"   // BUG-E FIX
                                       : r.DocumentType == "STOCK_ADJUSTMENT"      ? "Stock Adjustment"
                                       : r.DocumentType == "STOCK_TRANSFER"        ? "Stock Transfer"
                                       : r.DocumentType == "DAMAGE_LOSS"           ? "Damage / Loss"
                                       : r.DocumentType == "ASSET_DISPOSAL"        ? "Asset Disposal"
                                       : r.DocumentType,
                    RequesterUser    = r.RequesterUser,
                    ApproverUser     = r.ApproverUser,
                    IsActive         = r.IsActive,
                    LastUpdatedBy    = r.LastUpdatedBy,
                    LASTUPDATE       = r.LASTUPDATE
                })
                .ToListAsync();
        }

        public async Task<ApprovalRoutingDto?> GetApprovalRoutingById(int id)
        {
            var r = await _db.ApprovalRoutings.FindAsync(id);
            if (r == null) return null;

            return new ApprovalRoutingDto
            {
                ID                  = r.ID,
                DocumentType        = r.DocumentType,
                DocumentTypeDisplay = DocTypeDisplay(r.DocumentType),
                RequesterUser       = r.RequesterUser,
                ApproverUser        = r.ApproverUser,
                IsActive            = r.IsActive,
                LastUpdatedBy       = r.LastUpdatedBy,
                LASTUPDATE          = r.LASTUPDATE
            };
        }

        public async Task<(bool Success, string Message)> SaveApprovalRouting(
            SaveApprovalRoutingDto dto)
        {
            // ── Validation ─────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(dto.DocumentType))
                return (false, "Please select the document type.");
            if (string.IsNullOrWhiteSpace(dto.RequesterUser))
                return (false, "Please select the requester.");
            if (string.IsNullOrWhiteSpace(dto.ApproverUser))
                return (false, "Please select the approver.");

            if (string.Equals(dto.RequesterUser.Trim(), dto.ApproverUser.Trim(),
                              StringComparison.OrdinalIgnoreCase))
                return (false, "Requester and approver cannot be the same user.");

            // ── Verify both users exist and are not deleted ─────────────────
            var requesterExists = await _db.Obmsusers.AnyAsync(
                u => u.Name == dto.RequesterUser && !u.IsDeleted);
            if (!requesterExists)
                return (false,
                    $"User '{dto.RequesterUser}' was not found or has been deleted.");

            var approverExists = await _db.Obmsusers.AnyAsync(
                u => u.Name == dto.ApproverUser && !u.IsDeleted);
            if (!approverExists)
                return (false,
                    $"User '{dto.ApproverUser}' was not found or has been deleted.");

            if (dto.ID == 0)
            {
                // ── Create new ─────────────────────────────────────────────
                // Check for duplicate active mapping
                var duplicate = await _db.ApprovalRoutings.AnyAsync(r =>
                    r.DocumentType   == dto.DocumentType &&
                    r.RequesterUser.ToLower() == dto.RequesterUser.ToLower() &&
                    r.IsActive &&
                    r.ID != dto.ID);

                if (duplicate)
                    return (false,
                        "An active approval configuration already exists for this " +
                        "requester and document type. " +
                        "Please deactivate the existing one before creating a new one.");

                _db.ApprovalRoutings.Add(new ApprovalRouting
                {
                    DocumentType  = dto.DocumentType,
                    RequesterUser = dto.RequesterUser.Trim(),
                    ApproverUser  = dto.ApproverUser.Trim(),
                    IsActive      = dto.IsActive,
                    LastUpdatedBy = dto.LastUpdatedBy,
                    LASTUPDATE    = DateTime.Now
                });
            }
            else
            {
                // ── Update existing ────────────────────────────────────────
                var existing = await _db.ApprovalRoutings.FindAsync(dto.ID);
                if (existing == null)
                    return (false, "Approval routing record not found.");

                // Duplicate check (exclude current record)
                if (dto.IsActive)
                {
                    var duplicate = await _db.ApprovalRoutings.AnyAsync(r =>
                        r.DocumentType   == dto.DocumentType &&
                        r.RequesterUser.ToLower() == dto.RequesterUser.ToLower() &&
                        r.IsActive &&
                        r.ID != dto.ID);

                    if (duplicate)
                        return (false,
                            "An active approval configuration already exists for this " +
                            "requester and document type.");
                }

                existing.DocumentType  = dto.DocumentType;
                existing.RequesterUser = dto.RequesterUser.Trim();
                existing.ApproverUser  = dto.ApproverUser.Trim();
                existing.IsActive      = dto.IsActive;
                existing.LastUpdatedBy = dto.LastUpdatedBy;
                existing.LASTUPDATE    = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            return (true, "Approval configuration saved successfully.");
        }

        public async Task<(bool Success, string Message)> DeactivateApprovalRouting(
            int id, string updatedBy)
        {
            var row = await _db.ApprovalRoutings.FindAsync(id);
            if (row == null)
                return (false, "Approval routing record not found.");
            if (!row.IsActive)
                return (false, "This routing configuration is already inactive.");

            row.IsActive      = false;
            row.LastUpdatedBy = updatedBy;
            row.LASTUPDATE    = DateTime.Now;
            await _db.SaveChangesAsync();
            return (true, "Approval routing configuration deactivated.");
        }

        // ════════════════════════════════════════════════════════════════════
        // ROUTING HELPERS
        // ════════════════════════════════════════════════════════════════════

        public async Task<string?> GetApproverForUser(
            string requesterUser, string documentType)
        {
            if (string.IsNullOrWhiteSpace(requesterUser)) return null;

            var row = await _db.ApprovalRoutings
                .Where(r => r.IsActive &&
                            r.DocumentType == documentType &&
                            r.RequesterUser.ToLower() == requesterUser.ToLower())
                .FirstOrDefaultAsync();

            return row?.ApproverUser;
        }

        public async Task<bool> IsAuthorizedApprover(
            string authenticatedUser, string requesterUser, string documentType)
        {
            if (string.IsNullOrWhiteSpace(authenticatedUser) ||
                string.IsNullOrWhiteSpace(requesterUser))
                return false;

            return await _db.ApprovalRoutings.AnyAsync(r =>
                r.IsActive &&
                r.DocumentType   == documentType &&
                r.RequesterUser.ToLower() == requesterUser.ToLower() &&
                r.ApproverUser.ToLower()  == authenticatedUser.ToLower());
        }
    }
}
