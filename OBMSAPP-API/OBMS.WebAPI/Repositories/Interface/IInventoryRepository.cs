using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IInventoryRepository
    {
        Task<InventoryCategory> SaveAndUpdateCategory(InventoryCategory category);

        Task<List<Object>> GetCategories();

        Task<ActionResult<InventoryCategory>> GetCategoryByID(int ID);

        Task<List<Object>> GetCategoriesByCat(string cat);

        Task<ItemMaster> SaveAndUpdateItem(ItemMaster item);

        Task<List<Object>> GetItems();

        Task<Dictionary<string, Object>> GetItemByID(int ID);

        Task<AssetMaster> SaveAndUpdateAssetMaster(AssetMaster item);

        Task<List<Object>> GetAssetMasters(string userName);

        Task<AssetMaster> GetAssetByID(int ID, string userName);

        Task<Dictionary<string, Object>> GetSupplierMaster(int ID);

        Task<Dictionary<string, Object>> GetSupplierCode();

        Task<Supplier> SaveAndUpdateSupplier(Supplier obj);

        Task<List<Object>> GetSuppliers(string? category = null);

        Task<Supplier> GetSupplierByID(int ID);

        Task<Dictionary<string, Object>> GetRecipientMaster();

        Task<Recipient> SaveAndUpdateRecipient(Recipient obj);

        Task<List<Object>> GetRecipients();

        Task<Recipient> GetRecipientByID(int ID);

        Task<Dictionary<string, Object>> GetUtilityBillsMasterList(string userID);

        Task<Dictionary<string, Object>> GetPaytoByCategory(int ID);

        Task<Dictionary<string, Object>> GetUtilityMasterList(string invCat, string supplierCat, string userID);

        Task<CreditorInvoice> SaveAndUpdateUtility(CreditorInvoice creditorInvoice);

        Task<CreditorInvoiceDetails> SaveAndUpdateUtilityDetails(CreditorInvoiceDetails creditorInvoiceDetails);

        Task<Dictionary<string, Object>> GetMaterialMasterList(string userID);

        Task<StockIssues> SaveAndUpdateMaterial(StockIssues stockIssues);

        Task<StockIssueDetail> SaveAndUpdateMaterialDetails(StockIssueDetail stockIssueDetail);

        Task<bool> DeleteItemAsync(int id, string currentUser);

        bool DeleteAssetType(int id);

        bool DeleteRecipientAsync(int id);

        Task<bool> DeleteCategoryAsync(int id, string currentUser);

        bool DeleteSupplierAsync(int id);

        // Part 1: New Inventory Masters
        Task<BrandMaster> SaveAndUpdateBrandMaster(BrandMaster brand);
        Task<List<Object>> GetBrandMasters();
        Task<BrandMaster> GetBrandByID(int ID);
        bool DeleteBrandAsync(int id);

        Task<UOMMaster> SaveAndUpdateUOMMaster(UOMMaster uom);
        Task<List<Object>> GetUOMMasters();
        Task<UOMMaster> GetUOMByID(int ID);
        bool DeleteUOMAsync(int id);

        Task<WarehouseMaster> SaveAndUpdateWarehouseMaster(WarehouseMaster warehouse);
        Task<List<Object>> GetWarehouseMasters(string? userName = null);
        Task<WarehouseMaster> GetWarehouseByID(int ID);
        bool DeleteWarehouseAsync(int id);

        // ── Part 2: Opening Stock ────────────────────────────────────────────────

        /// <summary>Returns opening stock entries visible to the given user (superadmin sees all; others see only their allowed branches).</summary>
        Task<List<OpeningStockListDto>> GetOpeningStockList(string userName);

        /// <summary>Returns a single opening stock entry by ID, enriched.</summary>
        Task<OpeningStockListDto?> GetOpeningStockByID(int id);

        /// <summary>
        /// Saves (new) or updates (existing) an Opening Stock entry.
        /// Transactionally maintains StockLedger: creates or updates exactly one OPENING ledger row.
        /// Returns (success, userMessage, saved entity).
        /// </summary>
        Task<(bool Success, string Message, OpeningStock? Record)> SaveOpeningStock(
            OpeningStockRequestDto dto);

        /// <summary>
        /// Soft-deletes an opening stock entry and removes the corresponding OPENING ledger row.
        /// </summary>
        Task<(bool Success, string Message)> DeleteOpeningStock(int id, string currentUser);

        // ── Part 2: Stock Ledger ─────────────────────────────────────────────────

        /// <summary>
        /// Returns ledger rows filtered by warehouse, optional item, date range,
        /// optional transaction type, optional category, and optional size.
        /// Includes running balance per row.
        /// </summary>
        Task<List<StockLedgerRowDto>> GetStockLedger(
            int warehouseID,
            int? itemID,
            DateTime dateFrom,
            DateTime dateTo,
            string? transactionType,
            int? categoryID = null,
            string? size = null);

        /// <summary>
        /// Returns distinct non-null Size values for items belonging to the given
        /// category (and its sub-categories). categoryID = 0 returns sizes for all categories.
        /// </summary>
        Task<List<ItemSizeDto>> GetItemSizesByCategory(int categoryID);

        /// <summary>Returns current stock balance for a given item in a warehouse.</summary>
        Task<decimal> GetCurrentStock(int warehouseID, int itemID);

        /// <summary>
        /// Returns the total pending BSR quantity for a given item across all
        /// active Branch Stock Requests that are in Submitted or Approved status
        /// (i.e. requested but not yet fulfilled).
        /// </summary>
        Task<decimal> GetPendingBSRQtyByItem(int itemID);

        // ── Part 3: Purchase Requisition ─────────────────────────────────────────

        Task<List<PRListDto>> GetPRList(string userName);
        Task<PRListDto?> GetPRByID(int id);
        Task<(bool Success, string Message, PurchaseRequisition? Record)> SavePR(PRRequestDto dto);
        Task<(bool Success, string Message)> SubmitPR(int id, string currentUser);
        Task<(bool Success, string Message)> ApprovePR(int id, PRApprovalDto dto);
        Task<(bool Success, string Message)> RejectPR(int id, PRApprovalDto dto);
        Task<(bool Success, string Message)> DeletePR(int id, string currentUser);
        Task<(bool Success, string Message)> UpdatePRReference(UpdatePRRefDto dto);

        // ── Part 3: PR Processing Tasks (Purchase department claim/inbox) ─────────

        /// <summary>
        /// Returns all active processing tasks (Pending or Claimed).
        /// Used for the Purchase department inbox view.
        /// </summary>
        Task<List<PRProcessingTask>> GetPRProcessingTasks();

        /// <summary>
        /// Returns the processing task for a single PR, or null if none exists.
        /// Called by the PR form to display claim state.
        /// </summary>
        Task<PRProcessingTask?> GetPRProcessingTaskByPR(int prid);

        /// <summary>
        /// Atomically claims an unclaimed (Pending) task for <paramref name="claimedBy"/>.
        /// Uses a conditional UPDATE — only the first concurrent caller wins.
        /// Returns (false, reason) if already claimed or not in Pending state.
        /// </summary>
        Task<(bool Success, string Message)> ClaimPR(int prid, string claimedBy);

        // ── Part 3: Purchase Order ────────────────────────────────────────────────

        Task<List<POListDto>> GetPOList(string userName);
        Task<POListDto?> GetPOByID(int id);
        Task<POListDto?> GetPOByPRID(int prID);
        Task<List<PRListDto>> GetApprovedPRsForPO();
        Task<(bool Success, string Message, PurchaseOrder? Record)> SavePO(PORequestDto dto);
        Task<(bool Success, string Message)> ApprovePO(int id, POApprovalDto dto);
        Task<(bool Success, string Message)> CancelPO(int id, string currentUser);
        Task<POEditabilityDto> GetPOEditability(int id);
        /// <summary>Updates only Remarks on a Purchase Order. Items/quantities/stock are NOT changed.</summary>
        Task<(bool Success, string Message)> UpdatePOReference(UpdatePORefDto dto);

        // ── Part 3: GRN ───────────────────────────────────────────────────────────

        Task<List<GRNListDto>> GetGRNList(string userName);
        Task<GRNListDto?> GetGRNByID(int id);
        Task<GRNListDto?> GetGRNByPOID(int poID);
        Task<List<POListDto>> GetApprovedPOsForGRN();
        Task<List<POLineForGRNDto>> GetPOLinesForGRN(int poID);
        Task<(bool Success, string Message, GoodsReceiptNote? Record)> SaveGRN(GRNRequestDto dto);
        Task<(bool CanDelete, string? BlockingMessage, string? BlockingDocNumber)> CheckGRNDependency(int id);
        Task<(bool Success, string Message)> DeleteGRN(int id, string currentUser);
        /// <summary>
        /// Updates only SupplierInvoiceNo, DeliveryNoteRef, and Remarks on a posted GRN.
        /// Does NOT touch stock ledger, PO quantities, or CreditorInvoice.
        /// </summary>
        Task<(bool Success, string Message)> UpdateGRNReference(UpdateGRNRefDto dto);

        // ── Part 3: Purchase Return ───────────────────────────────────────────────

        Task<List<PurchaseReturnListDto>> GetPurchaseReturnList(string userName);
        Task<PurchaseReturnListDto?> GetPurchaseReturnByID(int id);
        Task<List<GRNListDto>> GetGRNsForReturn();
        Task<List<GRNLineForReturnDto>> GetGRNLinesForReturn(int grnID);
        Task<(bool Success, string Message, PurchaseReturn? Record)> SavePurchaseReturn(PurchaseReturnRequestDto dto);

        // ── Part 4: Stock Issue ───────────────────────────────────────────────────

        Task<List<StockIssueDocListDto>> GetStockIssueList(string userName);
        Task<StockIssueDocListDto?> GetStockIssueByID(int id);
        Task<(bool Success, string Message, StockIssueDoc? Record)> SaveStockIssue(StockIssueDocRequestDto dto);
        Task<(bool Success, string Message)> DeleteStockIssue(int id, string currentUser);

        // ── Part 4: Stock Issue Return ────────────────────────────────────────────

        /// <summary>Returns issue documents with returnable qty per line (for the return form dropdown).</summary>
        Task<List<StockIssueDocListDto>> GetIssuableDocsForReturn();
        /// <summary>Returns detail lines of a single issue doc enriched with already-returned qty.</summary>
        Task<List<StockIssueDocDetailListDto>> GetIssueDetailsForReturn(int issueDocID);
        Task<List<StockIssueReturnListDto>> GetStockIssueReturnList(string userName);
        Task<StockIssueReturnListDto?> GetStockIssueReturnByID(int id);
        Task<(bool Success, string Message, StockIssueReturn? Record)> SaveStockIssueReturn(StockIssueReturnRequestDto dto);

        // ── Part 4: Stock Transfer ────────────────────────────────────────────────

        Task<List<StockTransferListDto>> GetStockTransferList(string userName);
        Task<StockTransferListDto?> GetStockTransferByID(int id);
        Task<(bool Success, string Message, StockTransfer? Record)> SaveStockTransfer(StockTransferRequestDto dto);
        Task<(bool Success, string Message)> DeleteStockTransfer(int id, string currentUser);

        // ── Part 4: Stock Adjustment ──────────────────────────────────────────────

        Task<List<StockAdjustmentListDto>> GetStockAdjustmentList(string userName);
        Task<StockAdjustmentListDto?> GetStockAdjustmentByID(int id);
        Task<(bool Success, string Message, StockAdjustment? Record)> SaveStockAdjustment(StockAdjustmentRequestDto dto);
        Task<(bool Success, string Message)> DeleteStockAdjustment(int id, string currentUser);
        Task<(bool Success, string Message)> SubmitSA(int id, string currentUser);
        Task<(bool Success, string Message)> ApproveSA(int id, SAApprovalDto dto);
        Task<(bool Success, string Message)> RejectSA(int id, SAApprovalDto dto);

        // ── Part 4: Damage / Loss ─────────────────────────────────────────────────

        Task<List<StockDamageLossListDto>> GetStockDamageLossList(string userName);
        Task<StockDamageLossListDto?> GetStockDamageLossByID(int id);
        Task<(bool Success, string Message, StockDamageLoss? Record)> SaveStockDamageLoss(StockDamageLossRequestDto dto);
        Task<(bool Success, string Message)> DeleteStockDamageLoss(int id, string currentUser);

        // ── Part 5: Employee/Guard Item Holding ───────────────────────────────────

        /// <summary>
        /// Returns a summary list of all employees/guards who have any net holding > 0.
        /// Used for the top-level employee picker on the holding card screen.
        /// </summary>
        Task<List<EmployeeHoldingSummaryDto>> GetEmployeeHoldingSummaryList();

        /// <summary>
        /// Returns the per-item net holding for a single employee/guard.
        /// IssueToType filter: "Employee" | "Guard" | null (both).
        /// </summary>
        Task<List<EmployeeHoldingDto>> GetEmployeeHoldingDetail(int employeeID, string? issueToType);

        /// <summary>
        /// Returns ALL employees/guards with any net holding, with full item detail.
        /// Used to export or print a complete holding register.
        /// </summary>
        Task<List<EmployeeHoldingDto>> GetAllEmployeeHoldings(string? issueToType);

        // ── Part 5: Stock Balance Summary ─────────────────────────────────────────

        /// <summary>
        /// Returns current stock balance for every item in every warehouse that has
        /// any ledger movement. Optionally filtered to a single warehouse.
        /// Flags items at or below reorder level.
        /// </summary>
        Task<List<StockBalanceSummaryDto>> GetStockBalanceSummary(int? warehouseID, int? itemID);

        // ── Part 5: Employee Issue Register ──────────────────────────────────────

        /// <summary>
        /// Returns all issue lines for Employee/Guard type issues within a date range.
        /// Optionally filtered by a single employee ID.
        /// Includes returned qty and net qty per line.
        /// </summary>
        Task<List<EmployeeIssueRegisterDto>> GetEmployeeIssueRegister(
            DateTime dateFrom,
            DateTime dateTo,
            int? employeeID,
            string? issueToType,
            string? userName = null);

        // ── Part 6: Asset Master ──────────────────────────────────────────────────

        Task<List<AssetListDto>> GetAssetList(string userName);
        Task<AssetListDto?> GetAssetByIDEnriched(int id, string userName);
        Task<(bool Success, string Message, AssetMaster? Record)> SaveAsset(AssetMasterRequestDto dto);
        Task<(bool Success, string Message)> DeleteAsset(int id, string currentUser);

        // ── Part 6: Asset Allocation ──────────────────────────────────────────────

        Task<List<AssetAllocationListDto>> GetAssetAllocationList(bool activeOnly, string userName);
        Task<AssetAllocationListDto?> GetAssetAllocationByID(int id, string userName);
        Task<List<AssetAllocationListDto>> GetActiveAllocationsByAsset(int assetID, string userName);
        Task<(bool Success, string Message, AssetAllocation? Record)> SaveAssetAllocation(AssetAllocationRequestDto dto);
        Task<(bool Success, string Message)> CancelAssetAllocation(int id, string currentUser);

        // ── Part 6: Asset Transfer ────────────────────────────────────────────────

        Task<List<AssetTransferListDto>> GetAssetTransferList(string userName);
        Task<AssetTransferListDto?> GetAssetTransferByID(int id, string userName);
        Task<(bool Success, string Message, AssetTransfer? Record)> SaveAssetTransfer(AssetTransferRequestDto dto);

        // ── Part 6: Asset Return ──────────────────────────────────────────────────

        Task<List<AssetReturnListDto>> GetAssetReturnList(string userName);
        Task<AssetReturnListDto?> GetAssetReturnByID(int id, string userName);
        Task<(bool Success, string Message, AssetReturn? Record)> SaveAssetReturn(AssetReturnRequestDto dto);

        // ── Part 6: Asset Maintenance ─────────────────────────────────────────────

        Task<List<AssetMaintenanceListDto>> GetAssetMaintenanceList(int? assetID, string userName);
        Task<AssetMaintenanceListDto?> GetAssetMaintenanceByID(int id, string userName);
        Task<(bool Success, string Message, AssetMaintenance? Record)> SaveAssetMaintenance(AssetMaintenanceRequestDto dto);
        Task<(bool Success, string Message)> CompleteAssetMaintenance(int id, DateTime completionDate, decimal cost, string remarks, string currentUser);

        // ── Part 6: Asset Damage ──────────────────────────────────────────────────

        Task<List<AssetDamageListDto>> GetAssetDamageList(int? assetID, string userName);
        Task<AssetDamageListDto?> GetAssetDamageByID(int id, string userName);
        Task<(bool Success, string Message, AssetDamage? Record)> SaveAssetDamage(AssetDamageRequestDto dto);

        // ── Part 6: Asset Disposal ────────────────────────────────────────────────

        Task<List<AssetDisposalListDto>> GetAssetDisposalList(string userName);
        Task<AssetDisposalListDto?> GetAssetDisposalByID(int id, string userName);
        Task<(bool Success, string Message, AssetDisposal? Record)> SaveAssetDisposal(AssetDisposalRequestDto dto);
        Task<(bool Success, string Message)> SubmitAD(int id, string currentUser);
        Task<(bool Success, string Message)> ApproveAD(int id, ADApprovalDto dto);
        Task<(bool Success, string Message)> RejectAD(int id, ADApprovalDto dto);

        // ── Part 6: Asset Movement History ────────────────────────────────────────

        Task<List<AssetMovementHistoryDto>> GetAssetMovementHistory(int assetID, string userName);

        // ── Asset Location Tracking ────────────────────────────────────────────

        /// <summary>Updates the current location of an asset and logs the change.</summary>
        Task<(bool Success, string Message)> UpdateAssetLocation(AssetLocationUpdateDto dto);

        /// <summary>Returns full location history for a single asset.</summary>
        Task<List<AssetLocationLogDto>> GetAssetLocationLog(int assetID, string userName);

        /// <summary>Returns all assets with Defect_Location or Parked_Away status.</summary>
        Task<List<AssetListDto>> GetDefectLocationAssets(string userName);

        // ── Part 9: Branch Stock Request ─────────────────────────────────────────

        /// <summary>Returns active stock requests visible to the given user (superadmin sees all; others see only their allowed branches).</summary>
        Task<List<StockRequestListDto>> GetBSRList(string userName);

        /// <summary>Returns a single stock request with full line items.</summary>
        Task<StockRequestListDto?> GetBSRByID(int id);

        /// <summary>
        /// Saves (new) or updates (existing) a Draft stock request.
        /// Cannot edit a Submitted/Approved/Rejected request.
        /// </summary>
        Task<(bool Success, string Message, StockRequest? Record)> SaveBSR(
            StockRequestSaveDto dto, string currentUser);

        /// <summary>
        /// Submits a Draft request for approval.
        /// If no routing exists → direct-approve flow (same as PR/PO pattern).
        /// </summary>
        Task<(bool Success, string Message)> SubmitBSR(int id, string currentUser);

        /// <summary>Approves a Submitted stock request. Authorisation checked via ApprovalRouting.</summary>
        Task<(bool Success, string Message)> ApproveBSR(int id, StockRequestApprovalDto dto);

        /// <summary>Rejects a Submitted stock request. ApprovalRemarks required.</summary>
        Task<(bool Success, string Message)> RejectBSR(int id, StockRequestApprovalDto dto);

        /// <summary>
        /// Soft-deletes a Draft stock request.
        /// Cannot delete Submitted/Approved/Rejected requests.
        /// </summary>
        Task<(bool Success, string Message)> DeleteBSR(int id, string currentUser);

        /// <summary>
        /// Links an Approved stock request to a StockTransfer that was created to fulfil it.
        /// Called after the user creates the transfer from the approved BSR form.
        /// </summary>
        Task<(bool Success, string Message)> LinkBSRTransfer(int id, int transferID, string currentUser);

    // ── Inventory Dashboard ───────────────────────────────────────────────────

    /// <summary>
    /// Executes sp_InventoryDashboard and returns all KPI, chart, and summary
    /// data in a single call. Reads from StockLedger as the single source of truth.
    /// </summary>
    Task<InventoryDashboardDto> GetInventoryDashboard();

    // ── Stock Issue → Uniform Loan integration ────────────────────────────────
    /// <summary>
    /// Returns active employees/guards for the given warehouse's branch,
    /// filtered by issueToType ("Guard" → Guard/FGuard, "Staff" → Staff).
    /// </summary>
    Task<List<EmployeeForIssueDto>> GetEmployeesByWarehouse(int warehouseID, string issueToType);
    Task<List<EmployeeForIssueDto>> GetEmployeesByBranch(string branchCode, string issueToType);

    /// <summary>
    /// Returns all StockIssueDocs issued to the given employee (by EMP_ID)
    /// that are NOT already linked to an active Uniform Loan (SalaryAdvance TransType=4).
    /// </summary>
    Task<List<PendingStockIssueDto>> GetPendingStockIssuesForEmployee(int employeeID);
    Task<List<PendingStockIssueDto>> GetAllPendingStockIssues();

    /// <summary>
    /// Creates a StockIssueUniformLoanLink row to mark a Stock Issue as linked to a Uniform Loan.
    /// Enforces that the same Stock Issue cannot be linked to more than one active loan.
    /// </summary>
    Task<(bool Success, string Message)> LinkStockIssueToUniformLoan(LinkStockIssueToLoanRequestDto dto);
    }
}
