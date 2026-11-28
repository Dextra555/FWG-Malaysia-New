# FWG-Malaysia-New — Inventory API & UI Full Investigation Report

**Investigation Date:** 2025  
**Requested by:** "FWG-Malaysia-New etha invotory realted enna api used ui enna url full ste by ste list etha"  
**Interpretation of Request:** Tamil/colloquial — "etha" (எத) = "which/what". The question asks: *"In FWG-Malaysia-New, for inventory, which APIs are used, which UI URLs are there — full step-by-step list of what."*  
There is **no** code feature named "etha" or "ethiopia" in this codebase — the entire OBMS Inventory module is the subject.

---

## Summary Answer

The FWG-Malaysia-New OBMS system has a **fully built inventory module** with:

- **2 API controllers** dedicated to inventory: `InventoryController` (100+ endpoints) and `StockRequestController` (10 endpoints)
- **1 Angular service file**: `inventory.service.ts` — all HTTP calls go through this
- **36 UI routes** under the `/inventory/` path prefix
- **17 inventory report routes** under `/inventory/report/`
- **Base API URL**: `http://localhost:5002/api/` (dev); all inventory calls prefix with `Inventory/` or `StockRequest/` or `Approval/`

---

## Project Structure

```
FWG-Malaysia-New/
├── OBMSAPP-API/OBMS.WebAPI/
│   └── Controllers/
│       ├── InventoryController.cs      ← Main inventory controller
│       ├── StockRequestController.cs   ← Branch Stock Request controller
│       └── ApprovalController.cs       ← PO/PR/SA/AD approval workflow
└── OBMSAPP-UI/angularapp/src/app/
    ├── service/
    │   └── inventory.service.ts        ← Single service for ALL inventory HTTP calls
    ├── modules/inventory/              ← 36+ Angular components
    └── layout/admin-layout/
        └── admin-layout-routing.module.ts  ← All UI routes defined here
```

---

## PART 1 — API Endpoints: InventoryController

Base route: `api/Inventory`  
Controller file: `d:\Gunma\FWG-Malaysia-New\OBMSAPP-API\OBMS.WebAPI\Controllers\InventoryController.cs`

### 1A — Inventory Masters (Category, Item, Supplier, Recipient, Asset, Brand, UOM, Warehouse)

| # | HTTP Verb | Full API URL | Action Method | Purpose |
|---|-----------|-------------|---------------|---------|
| 1 | POST | `api/Inventory/SaveAndUpdateCategory` | `SaveAndUpdateCategory` | Create/edit inventory category |
| 2 | GET | `api/Inventory/GetInventoryCategories` | `GetCategories` | List all categories |
| 3 | GET | `api/Inventory/GetInventoryCategoryByID?ID={id}` | `GetInventoryCategoryByID` | Get single category |
| 4 | GET | `api/Inventory/GetCategoriesByCat?cat={cat}` | `GetCategoriesByCat` | Get categories by type (I/P) |
| 5 | GET | `api/Inventory/GetInventoryItemMaster?cat={cat}` | `GetInventoryItemMaster` | Get item master with categories |
| 6 | POST | `api/Inventory/SaveAndUpdateItem` | `SaveAndUpdateItem` | Create/edit item |
| 7 | GET | `api/Inventory/GetItemCode` | `GetItemCode` | Generate next ITMxxxx code |
| 8 | GET | `api/Inventory/GetInventoryItems` | `GetItems` | List all items |
| 9 | GET | `api/Inventory/GetInventoryItemByID?ID={id}` | `GetItemByID` | Get single item |
| 10 | GET | `api/Inventory/GetItemByCategoryId?categoryId={id}` | `GetItemByCategoryId` | Items filtered by category |
| 11 | GET | `api/Inventory/GetItemsForStock` | `GetItemsForStock` | Items enriched with Category/Brand/UOM/Warehouse |
| 12 | POST | `api/Inventory/DeleteItem/{id}?currentUser={user}` | `DeleteItem` | Soft-delete item |
| 13 | POST | `api/Inventory/DeleteCategory/{id}?currentUser={user}` | `DeleteCategory` | Soft-delete category |
| 14 | POST | `api/Inventory/SaveAndUpdateAssetMaster` | `SaveAndUpdateAssetMaster` | Create/edit asset master (old) |
| 15 | GET | `api/Inventory/GetAssetMasters` | `GetAssetMasters` | List asset masters (old) |
| 16 | GET | `api/Inventory/GetInventoryAssetByID?ID={id}` | `GetAssetByID` | Get asset master by ID (old) |
| 17 | POST | `api/Inventory/DeleteAsset/{id}` | `DeleteAsset` | Delete asset type |
| 18 | POST | `api/Inventory/SaveAndUpdateSupplier` | `SaveAndUpdateSupplier` | Create/edit supplier |
| 19 | GET | `api/Inventory/GetSuppliers?category={cat}` | `GetSuppliers` | List suppliers (optional category filter) |
| 20 | GET | `api/Inventory/GetSupplierCode` | `GetSupplierCode` | Generate next supplier code |
| 21 | GET | `api/Inventory/GetSupplierByID?ID={id}` | `GetSupplierByID` | Get single supplier |
| 22 | POST | `api/Inventory/DeleteSupplier/{id}` | `DeleteSupplier` | Delete supplier |
| 23 | POST | `api/Inventory/SaveAndUpdateRecipient` | `SaveAndUpdateRecipient` | Create/edit recipient |
| 24 | GET | `api/Inventory/GetRecipients` | `GetRecipients` | List all recipients |
| 25 | GET | `api/Inventory/GetRecipientMaster` | `GetRecipientMaster` | Recipient master list |
| 26 | GET | `api/Inventory/GetRecipientCode` | `GetRecipientCode` | Generate next RCxxxx code |
| 27 | GET | `api/Inventory/GetRecipientByID?ID={id}` | `GetRecipientByID` | Get single recipient |
| 28 | POST | `api/Inventory/DeleteRecipient/{id}` | `DeleteRecipient` | Delete recipient |
| 29 | POST | `api/Inventory/SaveAndUpdateBrandMaster` | `SaveAndUpdateBrandMaster` | Create/edit brand |
| 30 | GET | `api/Inventory/GetBrandMasters` | `GetBrandMasters` | List all brands |
| 31 | GET | `api/Inventory/GetBrandCode` | `GetBrandCode` | Generate next BRxxxx code |
| 32 | GET | `api/Inventory/GetBrandByID?ID={id}` | `GetBrandByID` | Get single brand |
| 33 | POST | `api/Inventory/DeleteBrand/{id}` | `DeleteBrand` | Delete brand |
| 34 | POST | `api/Inventory/SaveAndUpdateUOMMaster` | `SaveAndUpdateUOMMaster` | Create/edit Unit of Measure |
| 35 | GET | `api/Inventory/GetUOMMasters` | `GetUOMMasters` | List all UOMs |
| 36 | GET | `api/Inventory/GetUOMCode` | `GetUOMCode` | Generate next UMxxxx code |
| 37 | GET | `api/Inventory/GetUOMByID?ID={id}` | `GetUOMByID` | Get single UOM |
| 38 | POST | `api/Inventory/DeleteUOM/{id}` | `DeleteUOM` | Delete UOM |
| 39 | POST | `api/Inventory/SaveAndUpdateWarehouseMaster` | `SaveAndUpdateWarehouseMaster` | Create/edit warehouse |
| 40 | GET | `api/Inventory/GetWarehouseMasters?userName={user}` | `GetWarehouseMasters` | List warehouses (filtered by user) |
| 41 | GET | `api/Inventory/GetWarehouseCode` | `GetWarehouseCode` | Generate next WHxxxx code |
| 42 | GET | `api/Inventory/GetWarehouseByID?ID={id}` | `GetWarehouseByID` | Get single warehouse |
| 43 | POST | `api/Inventory/DeleteWarehouse/{id}` | `DeleteWarehouse` | Delete warehouse |

### 1B — Utility Bills / Purchase Bills / Material Issue (Legacy)

| # | HTTP Verb | Full API URL | Action Method | Purpose |
|---|-----------|-------------|---------------|---------|
| 44 | GET | `api/Inventory/GetUtilityBillsMasterList?userID={id}` | `GetUtilityBillsMasterList` | List utility bills master |
| 45 | GET | `api/Inventory/GetUtilityMasterList?invCat={cat}&supplierCat={cat}&userID={id}` | `GetUtilityMasterList` | Utility master list with filters |
| 46 | GET | `api/Inventory/GetUtilitySearchList?branch={b}&supplier={s}&Itype={t}` | `GetUtilitySearchList` | Search utility/creditor invoices |
| 47 | GET | `api/Inventory/GetUtilityBillSearchList?branch={b}&supplier={s}&invoiceType={t}...` | `GetUtilityBillSearchList` | Advanced utility bill search |
| 48 | GET | `api/Inventory/GetUtilityDetailsByID?ID={id}` | `GetUtilityDetailsByID` | Get utility invoice details |
| 49 | GET | `api/Inventory/GetUtilityDetailListByInvoiceId?InvoiceID={id}` | `GetUtilityDetailListByInvoiceId` | Get utility detail lines |
| 50 | POST | `api/Inventory/SaveAndUpdateUtility` | `SaveAndUpdateUtility` | Create/edit creditor invoice |
| 51 | POST | `api/Inventory/DeleteUtilityDetailById?Id={id}` | `DeleteUtilityDetailById` | Delete utility detail line |
| 52 | GET | `api/Inventory/GetPaytoByCategory?ID={id}` | `GetPaytoByCategory` | Get pay-to records by category |
| 53 | GET | `api/Inventory/GetMaterialMasterList?userID={id}` | `GetMaterialMasterList` | Material (stock issue legacy) master |
| 54 | GET | `api/Inventory/GetMaterialInvoiceByBranch?branchID={id}` | `GetMaterialInvoiceByBranch` | Next invoice number for branch |
| 55 | GET | `api/Inventory/GetMaterialInvoiceItemByCategoryID?ID={id}` | `GetMaterialInvoiceItemByCategoryID` | Items in category |
| 56 | POST | `api/Inventory/SaveAndUpdateMaterial` | `SaveAndUpdateMaterial` | Create/edit stock issue (legacy) |
| 57 | GET | `api/Inventory/GetMaterialSearchList?branch={b}` | `GetMaterialSearchList` | Search stock issues by branch |
| 58 | GET | `api/Inventory/GetMaterialDetailListByInvoiceId?InvoiceID={id}` | `GetMaterialDetailListByInvoiceId` | Get stock issue detail lines |
| 59 | GET | `api/Inventory/GetReportHQMaster` | `GetReportHQMaster` | HQ item master for reports |
| 60 | GET | `api/Inventory/CreditorInvoice_CheckOnBranchPayments/{invoiceID}` | `CreditorInvoice_CheckOnBranchPayments` | Check if invoice linked to branch payment |
| 61 | GET | `api/Inventory/DeleteExpensesByID/{id}?currentUser={user}` | `DeleteExpensesByID` | Delete expense record |

### 1C — Part 2: Opening Stock & Stock Ledger

| # | HTTP Verb | Full API URL | Action Method | Purpose |
|---|-----------|-------------|---------------|---------|
| 62 | GET | `api/Inventory/GetOpeningStockList` | `GetOpeningStockList` | List all opening stock entries |
| 63 | GET | `api/Inventory/GetOpeningStockByID?ID={id}` | `GetOpeningStockByID` | Get single opening stock entry |
| 64 | GET | `api/Inventory/GetOpeningStockCode` | `GetOpeningStockCode` | Generate next OSxxxx code |
| 65 | POST | `api/Inventory/SaveOpeningStock` | `SaveOpeningStock` | Create/edit opening stock |
| 66 | POST | `api/Inventory/DeleteOpeningStock/{id}?currentUser={user}` | `DeleteOpeningStock` | Soft-delete opening stock |
| 67 | POST | `api/Inventory/BulkImportOpeningStock` | `BulkImportOpeningStock` | Bulk Excel import of opening stock |
| 68 | GET | `api/Inventory/GetStockLedger?warehouseID={w}&itemID={i}&dateFrom={d}&dateTo={d}...` | `GetStockLedger` | Stock ledger with running balance |
| 69 | GET | `api/Inventory/GetItemSizesByCategory?categoryID={id}` | `GetItemSizesByCategory` | Sizes for filter dropdown |
| 70 | GET | `api/Inventory/GetStockIssueClients` | `GetStockIssueClients` | Distinct client names from stock issues |
| 71 | GET | `api/Inventory/GetCurrentStock?warehouseID={w}&itemID={i}` | `GetCurrentStock` | Current stock balance for item/warehouse |
| 72 | GET | `api/Inventory/GetPendingBSRQtyByItem?itemID={id}` | `GetPendingBSRQtyByItem` | Pending BSR demand for an item |

### 1D — Part 3: Purchase Flow (PR → PO → GRN → Return)

| # | HTTP Verb | Full API URL | Action Method | Purpose |
|---|-----------|-------------|---------------|---------|
| 73 | GET | `api/Inventory/GetPRList` | `GetPRList` | List Purchase Requisitions |
| 74 | GET | `api/Inventory/GetPRByID?ID={id}` | `GetPRByID` | Get single PR |
| 75 | GET | `api/Inventory/GetPRCode` | `GetPRCode` | Generate next PRxxxx code |
| 76 | POST | `api/Inventory/SavePR` | `SavePR` | Create/edit PR |
| 77 | POST | `api/Inventory/SubmitPR/{id}?currentUser={user}` | `SubmitPR` | Submit PR for approval |
| 78 | POST | `api/Inventory/ApprovePR/{id}` | `ApprovePR` | Approve PR |
| 79 | POST | `api/Inventory/RejectPR/{id}` | `RejectPR` | Reject PR |
| 80 | POST | `api/Inventory/DeletePR/{id}?currentUser={user}` | `DeletePR` | Soft-delete PR |
| 81 | POST | `api/Inventory/UpdatePRReference` | `UpdatePRReference` | Update PR reference fields only |
| 82 | GET | `api/Inventory/GetApprovedPRsForPO` | `GetApprovedPRsForPO` | Approved PRs ready for PO |
| 83 | GET | `api/Inventory/GetPRProcessingTasks` | `GetPRProcessingTasks` | PR purchase dept work queue |
| 84 | GET | `api/Inventory/GetPRProcessingTaskByPR?prid={id}` | `GetPRProcessingTaskByPR` | Claim state for a PR |
| 85 | POST | `api/Inventory/ClaimPR/{prid}` | `ClaimPR` | Claim a PR processing task |
| 86 | GET | `api/Inventory/GetPOList` | `GetPOList` | List Purchase Orders |
| 87 | GET | `api/Inventory/GetPOByID?ID={id}` | `GetPOByID` | Get single PO |
| 88 | GET | `api/Inventory/GetPOByPRID?prID={id}` | `GetPOByPRID` | Get PO linked to a PR |
| 89 | GET | `api/Inventory/GetPOCode` | `GetPOCode` | Generate next POxxxx code |
| 90 | POST | `api/Inventory/SavePO` | `SavePO` | Create/edit PO |
| 91 | POST | `api/Inventory/ApprovePO/{id}` | `ApprovePO` | Approve PO (legacy) |
| 92 | POST | `api/Inventory/CancelPO/{id}?currentUser={user}` | `CancelPO` | Cancel PO |
| 93 | GET | `api/Inventory/GetPOEditability?id={id}` | `GetPOEditability` | Check if PO can be edited |
| 94 | POST | `api/Inventory/UpdatePOReference` | `UpdatePOReference` | Update PO remarks only |
| 95 | GET | `api/Inventory/GetGRNList` | `GetGRNList` | List Goods Receipt Notes |
| 96 | GET | `api/Inventory/GetGRNByID?ID={id}` | `GetGRNByID` | Get single GRN |
| 97 | GET | `api/Inventory/GetGRNByPOID?poID={id}` | `GetGRNByPOID` | Get GRN linked to PO |
| 98 | GET | `api/Inventory/GetGRNCode` | `GetGRNCode` | Generate next GRNxxxx code |
| 99 | GET | `api/Inventory/GetApprovedPOsForGRN` | `GetApprovedPOsForGRN` | Approved POs ready for GRN |
| 100 | GET | `api/Inventory/GetPOLinesForGRN?POID={id}` | `GetPOLinesForGRN` | PO line items for GRN entry |
| 101 | POST | `api/Inventory/SaveGRN` | `SaveGRN` | Create GRN |
| 102 | GET | `api/Inventory/CheckGRNDependency?id={id}` | `CheckGRNDependency` | Check if GRN can be deleted |
| 103 | POST | `api/Inventory/DeleteGRN/{id}?currentUser={user}` | `DeleteGRN` | Delete GRN |
| 104 | POST | `api/Inventory/UpdateGRNReference` | `UpdateGRNReference` | Update GRN reference fields only |
| 105 | GET | `api/Inventory/GetPurchaseReturnList` | `GetPurchaseReturnList` | List Purchase Returns |
| 106 | GET | `api/Inventory/GetPurchaseReturnByID?ID={id}` | `GetPurchaseReturnByID` | Get single purchase return |
| 107 | GET | `api/Inventory/GetReturnCode` | `GetReturnCode` | Generate next RTNxxxx code |
| 108 | GET | `api/Inventory/GetGRNsForReturn` | `GetGRNsForReturn` | GRNs eligible for return |
| 109 | GET | `api/Inventory/GetGRNLinesForReturn?GRNID={id}` | `GetGRNLinesForReturn` | GRN lines for return entry |
| 110 | POST | `api/Inventory/SavePurchaseReturn` | `SavePurchaseReturn` | Create purchase return |

### 1E — Part 4: Stock Operations

| # | HTTP Verb | Full API URL | Action Method | Purpose |
|---|-----------|-------------|---------------|---------|
| 111 | GET | `api/Inventory/GetStockIssueList` | `GetStockIssueList` | List Stock Issues |
| 112 | GET | `api/Inventory/GetStockIssueByID?ID={id}` | `GetStockIssueByID` | Get single stock issue |
| 113 | GET | `api/Inventory/GetStockIssueCode` | `GetStockIssueCode` | Generate next SIxxxx code |
| 114 | POST | `api/Inventory/SaveStockIssue` | `SaveStockIssue` | Create/edit stock issue |
| 115 | POST | `api/Inventory/DeleteStockIssue/{id}?currentUser={user}` | `DeleteStockIssue` | Delete stock issue |
| 116 | GET | `api/Inventory/GetIssuableDocsForReturn` | `GetIssuableDocsForReturn` | Stock issues eligible for return |
| 117 | GET | `api/Inventory/GetIssueDetailsForReturn?issueDocID={id}` | `GetIssueDetailsForReturn` | Issue lines for return entry |
| 118 | GET | `api/Inventory/GetStockIssueReturnList` | `GetStockIssueReturnList` | List Stock Issue Returns |
| 119 | GET | `api/Inventory/GetStockIssueReturnByID?ID={id}` | `GetStockIssueReturnByID` | Get single issue return |
| 120 | GET | `api/Inventory/GetStockIssueReturnCode` | `GetStockIssueReturnCode` | Generate next SIRxxxx code |
| 121 | POST | `api/Inventory/SaveStockIssueReturn` | `SaveStockIssueReturn` | Create stock issue return |
| 122 | GET | `api/Inventory/GetStockTransferList` | `GetStockTransferList` | List Stock Transfers |
| 123 | GET | `api/Inventory/GetStockTransferByID?ID={id}` | `GetStockTransferByID` | Get single stock transfer |
| 124 | GET | `api/Inventory/GetStockTransferCode` | `GetStockTransferCode` | Generate next TRFxxxx code |
| 125 | POST | `api/Inventory/SaveStockTransfer` | `SaveStockTransfer` | Create stock transfer |
| 126 | POST | `api/Inventory/DeleteStockTransfer/{id}?currentUser={user}` | `DeleteStockTransfer` | Delete stock transfer |
| 127 | GET | `api/Inventory/GetStockAdjustmentList` | `GetStockAdjustmentList` | List Stock Adjustments |
| 128 | GET | `api/Inventory/GetStockAdjustmentByID?ID={id}` | `GetStockAdjustmentByID` | Get single adjustment |
| 129 | GET | `api/Inventory/GetStockAdjustmentCode` | `GetStockAdjustmentCode` | Generate next ADJxxxx code |
| 130 | POST | `api/Inventory/SaveStockAdjustment` | `SaveStockAdjustment` | Create/edit stock adjustment |
| 131 | POST | `api/Inventory/DeleteStockAdjustment/{id}?currentUser={user}` | `DeleteStockAdjustment` | Delete stock adjustment |
| 132 | POST | `api/Inventory/SubmitSA/{id}` | `SubmitSA` | Submit adjustment for approval |
| 133 | POST | `api/Inventory/ApproveSA/{id}` | `ApproveSA` | Approve stock adjustment |
| 134 | POST | `api/Inventory/RejectSA/{id}` | `RejectSA` | Reject stock adjustment |
| 135 | GET | `api/Inventory/GetStockDamageLossList` | `GetStockDamageLossList` | List Damage/Loss records |
| 136 | GET | `api/Inventory/GetStockDamageLossByID?ID={id}` | `GetStockDamageLossByID` | Get single damage/loss record |
| 137 | GET | `api/Inventory/GetStockDamageLossCode` | `GetStockDamageLossCode` | Generate next code |
| 138 | POST | `api/Inventory/SaveStockDamageLoss` | `SaveStockDamageLoss` | Create/edit damage/loss record |
| 139 | POST | `api/Inventory/DeleteStockDamageLoss/{id}?currentUser={user}` | `DeleteStockDamageLoss` | Delete damage/loss record |

### 1F — Part 5: Employee / Guard Tracking & Reports

| # | HTTP Verb | Full API URL | Action Method | Purpose |
|---|-----------|-------------|---------------|---------|
| 140 | GET | `api/Inventory/GetEmployeesByWarehouse?warehouseID={w}&issueToType={t}` | `GetEmployeesByWarehouse` | Employees for issue-to dropdown |
| 141 | GET | `api/Inventory/GetEmployeesByBranch?branchCode={b}&issueToType={t}` | `GetEmployeesByBranch` | Employees by branch |
| 142 | GET | `api/Inventory/GetPendingStockIssuesForEmployee?employeeID={id}` | `GetPendingStockIssuesForEmployee` | Unlinked stock issues for employee |
| 143 | GET | `api/Inventory/GetAllPendingStockIssues` | `GetAllPendingStockIssues` | All pending stock issues |
| 144 | POST | `api/Inventory/LinkStockIssueToUniformLoan` | `LinkStockIssueToUniformLoan` | Link stock issue to uniform loan |
| 145 | GET | `api/Inventory/GetInventoryTimesheet?branch={b}&year={y}&month={m}` | `GetInventoryTimesheet` | Monthly inventory timesheet |
| 146 | GET | `api/Inventory/GetEmployeeHoldingSummaryList` | `GetEmployeeHoldingSummaryList` | Employee item holding summary |
| 147 | GET | `api/Inventory/GetEmployeeHoldingDetail?employeeID={id}&issueToType={t}` | `GetEmployeeHoldingDetail` | Detailed items held by employee |
| 148 | GET | `api/Inventory/GetAllEmployeeHoldings?issueToType={t}` | `GetAllEmployeeHoldings` | All employee holdings |
| 149 | GET | `api/Inventory/GetStockBalanceSummary?warehouseID={w}&itemID={i}` | `GetStockBalanceSummary` | Stock balance summary |
| 150 | GET | `api/Inventory/GetEmployeeIssueRegister?dateFrom={d}&dateTo={d}&...` | `GetEmployeeIssueRegister` | Employee issue register report |

### 1G — Part 6: Asset Management

| # | HTTP Verb | Full API URL | Action Method | Purpose |
|---|-----------|-------------|---------------|---------|
| 151 | GET | `api/Inventory/GetAssetList` | `GetAssetList` | List all assets (new module) |
| 152 | GET | `api/Inventory/GetAssetByIDEnriched?ID={id}` | `GetAssetByIDEnriched` | Get asset with full detail |
| 153 | GET | `api/Inventory/GetAssetCode` | `GetAssetCode` | Generate next asset code |
| 154 | POST | `api/Inventory/SaveAsset` | `SaveAsset` | Create/edit asset record |
| 155 | POST | `api/Inventory/DeleteAssetRecord/{id}?currentUser={user}` | `DeleteAssetRecord` | Soft-delete asset |
| 156 | GET | `api/Inventory/GetAssetInstallments?assetID={id}` | `GetAssetInstallments` | Asset installment schedule |
| 157 | GET | `api/Inventory/GetAssetPaymentLockStatus?assetID={id}` | `GetAssetPaymentLockStatus` | Check payment lock |
| 158 | GET | `api/Inventory/GetAssetInstallmentsForPayment?supplierID={s}&paymentId={p}` | `GetAssetInstallmentsForPayment` | Installments for payment screen |
| 159 | GET | `api/Inventory/GetAssetAllocationList?activeOnly={bool}` | `GetAssetAllocationList` | List asset allocations |
| 160 | GET | `api/Inventory/GetAssetAllocationByID?ID={id}` | `GetAssetAllocationByID` | Get single allocation |
| 161 | GET | `api/Inventory/GetActiveAllocationsByAsset?assetID={id}` | `GetActiveAllocationsByAsset` | Active allocations for an asset |
| 162 | GET | `api/Inventory/GetAllocationCode` | `GetAllocationCode` | Generate next allocation code |
| 163 | POST | `api/Inventory/SaveAssetAllocation` | `SaveAssetAllocation` | Create/edit allocation |
| 164 | POST | `api/Inventory/CancelAssetAllocation/{id}?currentUser={user}` | `CancelAssetAllocation` | Cancel allocation |
| 165 | GET | `api/Inventory/GetAssetTransferList` | `GetAssetTransferList` | List asset transfers |
| 166 | GET | `api/Inventory/GetAssetTransferByID?ID={id}` | `GetAssetTransferByID` | Get single asset transfer |
| 167 | GET | `api/Inventory/GetTransferCode` | `GetTransferCode` | Generate next transfer code |
| 168 | POST | `api/Inventory/SaveAssetTransfer` | `SaveAssetTransfer` | Create asset transfer |
| 169 | GET | `api/Inventory/GetAssetReturnList` | `GetAssetReturnList` | List asset returns |
| 170 | GET | `api/Inventory/GetAssetReturnByID?ID={id}` | `GetAssetReturnByID` | Get single asset return |
| 171 | GET | `api/Inventory/GetReturnCodeAsset` | `GetReturnCodeAsset` | Generate next asset return code |
| 172 | POST | `api/Inventory/SaveAssetReturn` | `SaveAssetReturn` | Create asset return |
| 173 | GET | `api/Inventory/GetAssetMaintenanceList?assetID={id}` | `GetAssetMaintenanceList` | List maintenance records |
| 174 | GET | `api/Inventory/GetAssetMaintenanceByID?ID={id}` | `GetAssetMaintenanceByID` | Get single maintenance |
| 175 | GET | `api/Inventory/GetMaintenanceCode` | `GetMaintenanceCode` | Generate next code |
| 176 | POST | `api/Inventory/SaveAssetMaintenance` | `SaveAssetMaintenance` | Create/edit maintenance |
| 177 | POST | `api/Inventory/CompleteAssetMaintenance/{id}` | `CompleteAssetMaintenance` | Mark maintenance complete |
| 178 | GET | `api/Inventory/GetAssetDamageList?assetID={id}` | `GetAssetDamageList` | List asset damage records |
| 179 | GET | `api/Inventory/GetAssetDamageByID?ID={id}` | `GetAssetDamageByID` | Get single damage record |
| 180 | GET | `api/Inventory/GetDamageCode` | `GetDamageCode` | Generate next damage code |
| 181 | POST | `api/Inventory/SaveAssetDamage` | `SaveAssetDamage` | Create/edit asset damage |
| 182 | GET | `api/Inventory/GetAssetDisposalList` | `GetAssetDisposalList` | List asset disposals |
| 183 | GET | `api/Inventory/GetAssetDisposalByID?ID={id}` | `GetAssetDisposalByID` | Get single disposal |
| 184 | GET | `api/Inventory/GetDisposalCode` | `GetDisposalCode` | Generate next disposal code |
| 185 | POST | `api/Inventory/SaveAssetDisposal` | `SaveAssetDisposal` | Create/edit asset disposal |
| 186 | POST | `api/Inventory/SubmitAD/{id}` | `SubmitAD` | Submit disposal for approval |
| 187 | POST | `api/Inventory/ApproveAD/{id}` | `ApproveAD` | Approve asset disposal |
| 188 | POST | `api/Inventory/RejectAD/{id}` | `RejectAD` | Reject asset disposal |
| 189 | GET | `api/Inventory/GetAssetMovementHistory?assetID={id}` | `GetAssetMovementHistory` | Asset movement history |
| 190 | POST | `api/Inventory/UpdateAssetLocation` | `UpdateAssetLocation` | Update asset GPS/location |
| 191 | GET | `api/Inventory/GetAssetLocationLog?assetID={id}` | `GetAssetLocationLog` | Location log for asset |
| 192 | GET | `api/Inventory/GetDefectLocationAssets` | `GetDefectLocationAssets` | Assets with defect locations |

### 1H — Part 7: Approval Workflow (via ApprovalController)

| # | HTTP Verb | Full API URL | Action Method | Purpose |
|---|-----------|-------------|---------------|---------|
| 193 | GET | `api/Approval/GetPendingApprovals` | `GetPendingApprovals` | Pending approvals for current user |
| 194 | GET | `api/Approval/GetApprovalHistory?documentType={t}&documentID={id}` | `GetApprovalHistory` | History for one document |
| 195 | GET | `api/Approval/GetAllApprovalHistory?...` | `GetAllApprovalHistory` | All approval history (filtered) |
| 196 | POST | `api/Approval/SubmitPO/{id}` | `SubmitPO` | Submit PO for approval |
| 197 | POST | `api/Approval/ApprovePOWithHistory/{id}` | `ApprovePOWithHistory` | Approve PO (current, with history) |
| 198 | POST | `api/Approval/RejectPO/{id}` | `RejectPO` | Reject PO |
| 199 | GET | `api/Approval/GetApprovalConfigurations` | `GetApprovalConfigurations` | List approval configurations |
| 200 | POST | `api/Approval/UpdateApprovalConfiguration/{id}` | `UpdateApprovalConfiguration` | Update approval config |
| 201 | GET | `api/Approval/GetApprovalRoutings` | `GetApprovalRoutings` | List user-wise approval routings |
| 202 | GET | `api/Approval/IsCurrentUserApprover?documentType={t}&requesterUser={u}` | `IsCurrentUserApprover` | Check if user is approver |
| 203 | POST | `api/Approval/SaveApprovalRouting` | `SaveApprovalRouting` | Save approval routing |
| 204 | POST | `api/Approval/DeactivateApprovalRouting/{id}` | `DeactivateApprovalRouting` | Deactivate routing |

### 1I — Part 9: Dashboard & Branch Stock Request (StockRequestController)

| # | HTTP Verb | Full API URL | Action Method | Purpose |
|---|-----------|-------------|---------------|---------|
| 205 | GET | `api/Inventory/GetInventoryDashboard` | `GetInventoryDashboard` | All dashboard KPIs in one call |
| 206 | GET | `api/StockRequest/GetBSRList` | `GetBSRList` | List Branch Stock Requests |
| 207 | GET | `api/StockRequest/GetBSRByID?ID={id}` | `GetBSRByID` | Get single BSR with lines |
| 208 | GET | `api/StockRequest/GetBSRCode` | `GetBSRCode` | Generate next SRxxxx code |
| 209 | POST | `api/StockRequest/SaveBSR` | `SaveBSR` | Create/edit Draft BSR |
| 210 | POST | `api/StockRequest/SubmitBSR/{id}` | `SubmitBSR` | Submit BSR for approval |
| 211 | POST | `api/StockRequest/ApproveBSR/{id}` | `ApproveBSR` | Approve BSR |
| 212 | POST | `api/StockRequest/RejectBSR/{id}` | `RejectBSR` | Reject BSR |
| 213 | POST | `api/StockRequest/DeleteBSR/{id}` | `DeleteBSR` | Soft-delete BSR |
| 214 | POST | `api/StockRequest/LinkBSRTransfer/{id}` | `LinkBSRTransfer` | Link BSR to fulfilling StockTransfer |

---

## PART 2 — Angular Service File

**File:** `d:\Gunma\FWG-Malaysia-New\OBMSAPP-UI\angularapp\src\app\service\inventory.service.ts`

This is the **single service** that handles ALL inventory HTTP calls. It has 150+ methods organized in sections matching the API parts. All methods inject `HttpClient` and read `environment.baseUrl` (`http://localhost:5002/api/`).

**Key method-to-API mappings (selected):**

| Service Method | HTTP Call |
|----------------|-----------|
| `saveCategory(body)` | POST `Inventory/SaveAndUpdateCategory` |
| `getCategories()` | GET `Inventory/GetInventoryCategories` |
| `saveItem(body)` | POST `Inventory/SaveAndUpdateItem` |
| `getItems()` | GET `Inventory/GetInventoryItems` |
| `saveSupplier(body)` | POST `Inventory/SaveAndUpdateSupplier` |
| `getSuppliers(category?)` | GET `Inventory/GetSuppliers` |
| `saveRecipient(body)` | POST `Inventory/SaveAndUpdateRecipient` |
| `saveBrand(body)` | POST `Inventory/SaveAndUpdateBrandMaster` |
| `saveUOM(body)` | POST `Inventory/SaveAndUpdateUOMMaster` |
| `saveWarehouse(body)` | POST `Inventory/SaveAndUpdateWarehouseMaster` |
| `getOpeningStockList()` | GET `Inventory/GetOpeningStockList` |
| `saveOpeningStock(body)` | POST `Inventory/SaveOpeningStock` |
| `bulkImportOpeningStock(body)` | POST `Inventory/BulkImportOpeningStock` |
| `getStockLedger(...)` | GET `Inventory/GetStockLedger` |
| `getCurrentStock(w, i)` | GET `Inventory/GetCurrentStock` |
| `getPRList()` | GET `Inventory/GetPRList` |
| `savePR(body)` | POST `Inventory/SavePR` |
| `submitPR(id, user)` | POST `Inventory/SubmitPR/{id}` |
| `approvePR(id, body)` | POST `Inventory/ApprovePR/{id}` |
| `getPOList()` | GET `Inventory/GetPOList` |
| `savePO(body)` | POST `Inventory/SavePO` |
| `approvePOWithHistory(id, body)` | POST `Approval/ApprovePOWithHistory/{id}` |
| `getGRNList()` | GET `Inventory/GetGRNList` |
| `saveGRN(body)` | POST `Inventory/SaveGRN` |
| `getPurchaseReturnList()` | GET `Inventory/GetPurchaseReturnList` |
| `savePurchaseReturn(body)` | POST `Inventory/SavePurchaseReturn` |
| `getStockIssueList()` | GET `Inventory/GetStockIssueList` |
| `saveStockIssue(body)` | POST `Inventory/SaveStockIssue` |
| `getStockIssueReturnList()` | GET `Inventory/GetStockIssueReturnList` |
| `saveStockIssueReturn(body)` | POST `Inventory/SaveStockIssueReturn` |
| `getStockTransferList()` | GET `Inventory/GetStockTransferList` |
| `saveStockTransfer(body)` | POST `Inventory/SaveStockTransfer` |
| `getStockAdjustmentList()` | GET `Inventory/GetStockAdjustmentList` |
| `saveStockAdjustment(body)` | POST `Inventory/SaveStockAdjustment` |
| `getStockDamageLossList()` | GET `Inventory/GetStockDamageLossList` |
| `saveStockDamageLoss(body)` | POST `Inventory/SaveStockDamageLoss` |
| `getAssetList()` | GET `Inventory/GetAssetList` |
| `saveAssetRecord(body)` | POST `Inventory/SaveAsset` |
| `saveAssetAllocation(body)` | POST `Inventory/SaveAssetAllocation` |
| `saveAssetTransfer(body)` | POST `Inventory/SaveAssetTransfer` |
| `saveAssetReturn(body)` | POST `Inventory/SaveAssetReturn` |
| `saveAssetMaintenance(body)` | POST `Inventory/SaveAssetMaintenance` |
| `saveAssetDamage(body)` | POST `Inventory/SaveAssetDamage` |
| `saveAssetDisposal(body)` | POST `Inventory/SaveAssetDisposal` |
| `getInventoryDashboard()` | GET `Inventory/GetInventoryDashboard` |
| `getBSRList()` | GET `StockRequest/GetBSRList` |
| `saveBSR(body)` | POST `StockRequest/SaveBSR` |
| `submitBSR(id)` | POST `StockRequest/SubmitBSR/{id}` |
| `approveBSR(id, body)` | POST `StockRequest/ApproveBSR/{id}` |
| `linkBSRTransfer(id, transferID)` | POST `StockRequest/LinkBSRTransfer/{id}` |

---

## PART 3 — UI Routes (Angular)

Routing file: `d:\Gunma\FWG-Malaysia-New\OBMSAPP-UI\angularapp\src\app\layout\admin-layout\admin-layout-routing.module.ts`

### Complete Inventory UI Route → Component Map

| UI Route (browser URL) | Angular Component | Component File |
|------------------------|-------------------|----------------|
| `/inventory/dashboard` | `InventoryDashboardComponent` | `inventory-dashboard/inventory-dashboard.component.ts` |
| `/inventory/category-master` | `CategoryMasterComponent` | `category-master/category-master.component.ts` |
| `/inventory/category-master/new-category` | `NewCatergoryComponent` | `category-master/new-catergory/new-catergory.component.ts` |
| `/inventory/category-master/new-category/:ID` | `NewCatergoryComponent` | (edit mode) |
| `/inventory/item-master` | `ItemMasterComponent` | `item-master/item-master.component.ts` |
| `/inventory/item-master/new-item` | `NewItemComponent` | `item-master/new-item/new-item.component.ts` |
| `/inventory/item-master/new-item/:ID` | `NewItemComponent` | (edit mode) |
| `/inventory/supplier-master` | `SupplierMasterComponent` | `supplier-master/supplier-master.component.ts` |
| `/inventory/supplier-master/new-supplier` | `NewSupplierComponent` | `supplier-master/new-supplier/new-supplier.component.ts` |
| `/inventory/supplier-master/new-supplier/:ID` | `NewSupplierComponent` | (edit mode) |
| `/inventory/recepient-master` | `RecepientMasterComponent` | `recepient-master/recepient-master.component.ts` |
| `/inventory/recepient-master/new-recepient` | `NewRecepientComponent` | `recepient-master/new-recepient/new-recepient.component.ts` |
| `/inventory/recepient-master/new-recepient/:ID` | `NewRecepientComponent` | (edit mode) |
| `/inventory/brand-master` | `BrandMasterComponent` | `brand-master/brand-master.component.ts` |
| `/inventory/brand-master/new-brand` | `NewBrandComponent` | `brand-master/new-brand/new-brand.component.ts` |
| `/inventory/brand-master/new-brand/:ID` | `NewBrandComponent` | (edit mode) |
| `/inventory/uom-master` | `UOMMasterComponent` | `uom-master/uom-master.component.ts` |
| `/inventory/uom-master/new-uom` | `NewUOMComponent` | `uom-master/new-uom/new-uom.component.ts` |
| `/inventory/uom-master/new-uom/:ID` | `NewUOMComponent` | (edit mode) |
| `/inventory/warehouse-master` | `WarehouseMasterComponent` | `warehouse-master/warehouse-master.component.ts` |
| `/inventory/warehouse-master/new-warehouse` | `NewWarehouseComponent` | `warehouse-master/new-warehouse/new-warehouse.component.ts` |
| `/inventory/warehouse-master/new-warehouse/:ID` | `NewWarehouseComponent` | (edit mode) |
| `/inventory/asset-master` | `AssetMasterComponent` | `asset-master/asset-master.component.ts` |
| `/inventory/asset-master/new-asset` | `NewAssetComponent` | `asset-master/new-asset/new-asset.component.ts` |
| `/inventory/asset-master/new-asset/:ID` | `NewAssetComponent` | (edit mode) |
| `/inventory/utility-bills` | `UtilityBillsComponent` | `utility-bills/utility-bills.component.ts` |
| `/inventory/purchase-bills` | `PurchaseBillsComponent` | `purchase-bills/purchase-bills.component.ts` |
| `/inventory/material-issue` | `MaterialIssueComponent` | `material-issue/material-issue.component.ts` |
| `/inventory/opening-stock` | `OpeningStockComponent` | `opening-stock/opening-stock.component.ts` |
| `/inventory/opening-stock/new-opening-stock` | `NewOpeningStockComponent` | `opening-stock/new-opening-stock/new-opening-stock.component.ts` |
| `/inventory/opening-stock/new-opening-stock/:ID` | `NewOpeningStockComponent` | (edit mode) |
| `/inventory/stock-ledger` | `StockLedgerComponent` | `stock-ledger/stock-ledger.component.ts` |
| `/inventory/purchase-requisition` | `PurchaseRequisitionComponent` | `purchase-requisition/purchase-requisition.component.ts` |
| `/inventory/purchase-requisition/new-pr` | `NewPRComponent` | `purchase-requisition/new-pr/new-pr.component.ts` |
| `/inventory/purchase-requisition/new-pr/:ID` | `NewPRComponent` | (edit mode) |
| `/inventory/purchase-order` | `PurchaseOrderComponent` | `purchase-order/purchase-order.component.ts` |
| `/inventory/purchase-order/new-po` | `NewPOComponent` | `purchase-order/new-po/new-po.component.ts` |
| `/inventory/purchase-order/new-po/:ID` | `NewPOComponent` | (edit mode) |
| `/inventory/goods-receipt` | `GoodsReceiptComponent` | `goods-receipt/goods-receipt.component.ts` |
| `/inventory/goods-receipt/new-grn` | `NewGRNComponent` | `goods-receipt/new-grn/new-grn.component.ts` |
| `/inventory/goods-receipt/new-grn/:ID` | `NewGRNComponent` | (view mode) |
| `/inventory/goods-receipt/serial-entry` | `GrnSerialEntryComponent` | `goods-receipt/grn-serial-entry/grn-serial-entry.component.ts` |
| `/inventory/purchase-return` | `PurchaseReturnComponent` | `purchase-return/purchase-return.component.ts` |
| `/inventory/purchase-return/new-return` | `NewReturnComponent` | `purchase-return/new-return/new-return.component.ts` |
| `/inventory/purchase-return/new-return/:ID` | `NewReturnComponent` | (edit mode) |
| `/inventory/stock-issue` | `StockIssueComponent` | `stock-issue/stock-issue.component.ts` |
| `/inventory/stock-issue/new-stock-issue` | `NewStockIssueComponent` | `stock-issue/new-stock-issue/new-stock-issue.component.ts` |
| `/inventory/stock-issue/new-stock-issue/:ID` | `NewStockIssueComponent` | (edit mode) |
| `/inventory/stock-issue-return` | `StockIssueReturnComponent` | `stock-issue-return/stock-issue-return.component.ts` |
| `/inventory/stock-issue-return/new-stock-issue-return` | `NewStockIssueReturnComponent` | `stock-issue-return/new-stock-issue-return/new-stock-issue-return.component.ts` |
| `/inventory/stock-issue-return/new-stock-issue-return/:ID` | `NewStockIssueReturnComponent` | (edit mode) |
| `/inventory/stock-transfer` | `StockTransferComponent` | `stock-transfer/stock-transfer.component.ts` |
| `/inventory/stock-transfer/new-stock-transfer` | `NewStockTransferComponent` | `stock-transfer/new-stock-transfer/new-stock-transfer.component.ts` |
| `/inventory/stock-transfer/new-stock-transfer/:ID` | `NewStockTransferComponent` | (edit mode) |
| `/inventory/branch-stock-request` | `BranchStockRequestComponent` | `branch-stock-request/branch-stock-request.component.ts` |
| `/inventory/branch-stock-request/new-bsr` | `NewBSRComponent` | `branch-stock-request/new-bsr/new-bsr.component.ts` |
| `/inventory/branch-stock-request/new-bsr/:ID` | `NewBSRComponent` | (edit mode) |
| `/inventory/stock-adjustment` | `StockAdjustmentComponent` | `stock-adjustment/stock-adjustment.component.ts` |
| `/inventory/stock-adjustment/new-stock-adjustment` | `NewStockAdjustmentComponent` | `stock-adjustment/new-stock-adjustment/new-stock-adjustment.component.ts` |
| `/inventory/stock-adjustment/new-stock-adjustment/:ID` | `NewStockAdjustmentComponent` | (edit mode) |
| `/inventory/stock-damage-loss` | `StockDamageLossComponent` | `stock-damage-loss/stock-damage-loss.component.ts` |
| `/inventory/stock-damage-loss/new-stock-damage-loss` | `NewStockDamageLossComponent` | `stock-damage-loss/new-stock-damage-loss/new-stock-damage-loss.component.ts` |
| `/inventory/stock-damage-loss/new-stock-damage-loss/:ID` | `NewStockDamageLossComponent` | (edit mode) |
| `/inventory/employee-item-holding` | `EmployeeItemHoldingComponent` | `employee-item-holding/employee-item-holding.component.ts` |
| `/inventory/stock-balance-summary` | `StockBalanceSummaryComponent` | `stock-balance-summary/stock-balance-summary.component.ts` |
| `/inventory/employee-issue-register` | `EmployeeIssueRegisterComponent` | `employee-issue-register/employee-issue-register.component.ts` |
| `/inventory/asset-allocation` | `AssetAllocationComponent` | `asset-allocation/asset-allocation.component.ts` |
| `/inventory/asset-transfer` | `AssetTransferComponent` | `asset-transfer/asset-transfer.component.ts` |
| `/inventory/asset-return` | `AssetReturnComponent` | `asset-return/asset-return.component.ts` |
| `/inventory/asset-maintenance` | `AssetMaintenanceComponent` | `asset-maintenance/asset-maintenance.component.ts` |
| `/inventory/asset-damage` | `AssetDamageComponent` | `asset-damage/asset-damage.component.ts` |
| `/inventory/asset-disposal` | `AssetDisposalComponent` | `asset-disposal/asset-disposal.component.ts` |
| `/inventory/asset-disposal/:ID` | `AssetDisposalComponent` | (view/edit mode) |
| `/inventory/asset-movement-history/:assetID` | `AssetMovementHistoryComponent` | `asset-movement-history/asset-movement-history.component.ts` |
| `/inventory/asset-location` | `AssetLocationComponent` | `asset-location/asset-location.component.ts` |
| `/inventory/asset-location/:assetID` | `AssetLocationComponent` | (specific asset) |

### Inventory Report Routes

| UI Route | Component |
|----------|-----------|
| `/inventory/report/hq-stock-ledger` | `HQStockLedgerComponent` |
| `/inventory/report/branch-stock-ledger` | `BranchStockLedgerComponent` |
| `/inventory/report/stock-ledger-report` | `StockLedgerReportComponent` |
| `/inventory/report/current-stock-report` | `CurrentStockReportComponent` |
| `/inventory/report/stock-movement-report` | `StockMovementReportComponent` |
| `/inventory/report/stock-in-grn-report` | `StockInGRNReportComponent` |
| `/inventory/report/stock-issue-list-report` | `StockIssueListReportComponent` |
| `/inventory/report/item-category-stock-report` | `ItemCategoryStockReportComponent` |
| `/inventory/report/warehouse-stock-report` | `WarehouseStockReportComponent` |
| `/inventory/report/supplier-purchase-report` | `SupplierPurchaseReportComponent` |
| `/inventory/report/low-stock-report` | `LowStockReportComponent` |
| `/inventory/report/stock-transfer-report` | `StockTransferReportComponent` |
| `/inventory/report/stock-adj-damage-loss-report` | `StockAdjDamageLossReportComponent` |
| `/inventory/report/purchase-issue-return-report` | `PurchaseIssueReturnReportComponent` |
| `/inventory/report/inventory-valuation-report` | `InventoryValuationReportComponent` |
| `/inventory/report/branch-asset-report` | `BranchAssetReportComponent` |
| `/inventory/report/asset-movement-report` | `AssetMovementReportComponent` |

---

## PART 4 — Full Step-by-Step Mapping: UI Route → Component → Service Method → API Endpoint

### Group A: Inventory Masters

| UI Route | Component | Service Method | API Endpoint | Verb |
|----------|-----------|----------------|-------------|------|
| `/inventory/category-master` | `CategoryMasterComponent` | `getCategories()` | `api/Inventory/GetInventoryCategories` | GET |
| `/inventory/category-master/new-category` | `NewCatergoryComponent` | `saveCategory(body)` | `api/Inventory/SaveAndUpdateCategory` | POST |
| `/inventory/category-master/new-category/:ID` | `NewCatergoryComponent` | `getCategoryByID(id)` → `saveCategory(body)` | `api/Inventory/GetInventoryCategoryByID` → `SaveAndUpdateCategory` | GET/POST |
| `/inventory/item-master` | `ItemMasterComponent` | `getItems()` | `api/Inventory/GetInventoryItems` | GET |
| `/inventory/item-master/new-item` | `NewItemComponent` | `getItemCode()` + `saveItem(body)` | `api/Inventory/GetItemCode` + `SaveAndUpdateItem` | GET/POST |
| `/inventory/item-master/new-item/:ID` | `NewItemComponent` | `getItemByID(id)` → `saveItem(body)` | `api/Inventory/GetInventoryItemByID` + `SaveAndUpdateItem` | GET/POST |
| `/inventory/supplier-master` | `SupplierMasterComponent` | `getSuppliers()` | `api/Inventory/GetSuppliers` | GET |
| `/inventory/supplier-master/new-supplier` | `NewSupplierComponent` | `getSupplierCode()` + `saveSupplier(body)` | `api/Inventory/GetSupplierCode` + `SaveAndUpdateSupplier` | GET/POST |
| `/inventory/recepient-master` | `RecepientMasterComponent` | `getRecipients()` | `api/Inventory/GetRecipients` | GET |
| `/inventory/recepient-master/new-recepient` | `NewRecepientComponent` | `getRecipientCode()` + `saveRecipient(body)` | `api/Inventory/GetRecipientCode` + `SaveAndUpdateRecipient` | GET/POST |
| `/inventory/brand-master` | `BrandMasterComponent` | `getBrandMasters()` | `api/Inventory/GetBrandMasters` | GET |
| `/inventory/brand-master/new-brand` | `NewBrandComponent` | `getBrandCode()` + `saveBrand(body)` | `api/Inventory/GetBrandCode` + `SaveAndUpdateBrandMaster` | GET/POST |
| `/inventory/uom-master` | `UOMMasterComponent` | `getUOMMasters()` | `api/Inventory/GetUOMMasters` | GET |
| `/inventory/uom-master/new-uom` | `NewUOMComponent` | `getUOMCode()` + `saveUOM(body)` | `api/Inventory/GetUOMCode` + `SaveAndUpdateUOMMaster` | GET/POST |
| `/inventory/warehouse-master` | `WarehouseMasterComponent` | `getWarehouseMasters()` | `api/Inventory/GetWarehouseMasters` | GET |
| `/inventory/warehouse-master/new-warehouse` | `NewWarehouseComponent` | `getWarehouseCode()` + `saveWarehouse(body)` | `api/Inventory/GetWarehouseCode` + `SaveAndUpdateWarehouseMaster` | GET/POST |

### Group B: Opening Stock & Stock Ledger

| UI Route | Component | Service Method | API Endpoint | Verb |
|----------|-----------|----------------|-------------|------|
| `/inventory/opening-stock` | `OpeningStockComponent` | `getOpeningStockList()` | `api/Inventory/GetOpeningStockList` | GET |
| `/inventory/opening-stock/new-opening-stock` | `NewOpeningStockComponent` | `getOpeningStockCode()` + `getItemsForStock()` + `getWarehouseMasters()` + `saveOpeningStock(body)` | `GetOpeningStockCode` + `GetItemsForStock` + `GetWarehouseMasters` + `SaveOpeningStock` | GET/POST |
| `/inventory/opening-stock/new-opening-stock/:ID` | `NewOpeningStockComponent` | `getOpeningStockByID(id)` + `saveOpeningStock(body)` | `GetOpeningStockByID` + `SaveOpeningStock` | GET/POST |
| `/inventory/stock-ledger` | `StockLedgerComponent` | `getStockLedger(...)` + `getWarehouseMasters()` + `getItemsForStock()` | `api/Inventory/GetStockLedger` | GET |

### Group C: Purchase Flow

| UI Route | Component | Service Method | API Endpoint | Verb |
|----------|-----------|----------------|-------------|------|
| `/inventory/purchase-requisition` | `PurchaseRequisitionComponent` | `getPRList()` | `api/Inventory/GetPRList` | GET |
| `/inventory/purchase-requisition/new-pr` | `NewPRComponent` | `getPRCode()` + `savePR(body)` + `submitPR()` | `GetPRCode` + `SavePR` + `SubmitPR/{id}` | GET/POST |
| `/inventory/purchase-requisition/new-pr/:ID` | `NewPRComponent` | `getPRByID(id)` + `savePR(body)` + `approvePR()` / `rejectPR()` | `GetPRByID` + `SavePR` + `ApprovePR/{id}` / `RejectPR/{id}` | GET/POST |
| `/inventory/purchase-order` | `PurchaseOrderComponent` | `getPOList()` | `api/Inventory/GetPOList` | GET |
| `/inventory/purchase-order/new-po` | `NewPOComponent` | `getPOCode()` + `getApprovedPRsForPO()` + `savePO(body)` + `submitPO()` | `GetPOCode` + `GetApprovedPRsForPO` + `SavePO` + `Approval/SubmitPO/{id}` | GET/POST |
| `/inventory/purchase-order/new-po/:ID` | `NewPOComponent` | `getPOByID(id)` + `approvePOWithHistory()` / `rejectPO()` | `GetPOByID` + `Approval/ApprovePOWithHistory/{id}` | GET/POST |
| `/inventory/goods-receipt` | `GoodsReceiptComponent` | `getGRNList()` | `api/Inventory/GetGRNList` | GET |
| `/inventory/goods-receipt/new-grn` | `NewGRNComponent` | `getGRNCode()` + `getApprovedPOsForGRN()` + `getPOLinesForGRN()` + `saveGRN(body)` | `GetGRNCode` + `GetApprovedPOsForGRN` + `GetPOLinesForGRN` + `SaveGRN` | GET/POST |
| `/inventory/purchase-return` | `PurchaseReturnComponent` | `getPurchaseReturnList()` | `api/Inventory/GetPurchaseReturnList` | GET |
| `/inventory/purchase-return/new-return` | `NewReturnComponent` | `getReturnCode()` + `getGRNsForReturn()` + `getGRNLinesForReturn()` + `savePurchaseReturn(body)` | `GetReturnCode` + `GetGRNsForReturn` + `GetGRNLinesForReturn` + `SavePurchaseReturn` | GET/POST |

### Group D: Stock Operations

| UI Route | Component | Service Method | API Endpoint | Verb |
|----------|-----------|----------------|-------------|------|
| `/inventory/stock-issue` | `StockIssueComponent` | `getStockIssueList()` | `api/Inventory/GetStockIssueList` | GET |
| `/inventory/stock-issue/new-stock-issue` | `NewStockIssueComponent` | `getStockIssueCode()` + `getWarehouseMasters()` + `getItemsForStock()` + `saveStockIssue(body)` | `GetStockIssueCode` + `GetWarehouseMasters` + `GetItemsForStock` + `SaveStockIssue` | GET/POST |
| `/inventory/stock-issue-return` | `StockIssueReturnComponent` | `getStockIssueReturnList()` | `api/Inventory/GetStockIssueReturnList` | GET |
| `/inventory/stock-issue-return/new-stock-issue-return` | `NewStockIssueReturnComponent` | `getStockIssueReturnCode()` + `getIssuableDocsForReturn()` + `getIssueDetailsForReturn()` + `saveStockIssueReturn(body)` | `GetStockIssueReturnCode` + `GetIssuableDocsForReturn` + `GetIssueDetailsForReturn` + `SaveStockIssueReturn` | GET/POST |
| `/inventory/stock-transfer` | `StockTransferComponent` | `getStockTransferList()` | `api/Inventory/GetStockTransferList` | GET |
| `/inventory/stock-transfer/new-stock-transfer` | `NewStockTransferComponent` | `getStockTransferCode()` + `getWarehouseMasters()` + `getItemsForStock()` + `saveStockTransfer(body)` | `GetStockTransferCode` + `GetWarehouseMasters` + `GetItemsForStock` + `SaveStockTransfer` | GET/POST |
| `/inventory/branch-stock-request` | `BranchStockRequestComponent` | `getBSRList()` | `api/StockRequest/GetBSRList` | GET |
| `/inventory/branch-stock-request/new-bsr` | `NewBSRComponent` | `getBSRCode()` + `getWarehouseMasters()` + `saveBSR(body)` + `submitBSR(id)` | `StockRequest/GetBSRCode` + `StockRequest/SaveBSR` + `StockRequest/SubmitBSR/{id}` | GET/POST |
| `/inventory/stock-adjustment` | `StockAdjustmentComponent` | `getStockAdjustmentList()` | `api/Inventory/GetStockAdjustmentList` | GET |
| `/inventory/stock-adjustment/new-stock-adjustment` | `NewStockAdjustmentComponent` | `getStockAdjustmentCode()` + `saveStockAdjustment(body)` + `submitSA(id)` | `GetStockAdjustmentCode` + `SaveStockAdjustment` + `SubmitSA/{id}` | GET/POST |
| `/inventory/stock-damage-loss` | `StockDamageLossComponent` | `getStockDamageLossList()` | `api/Inventory/GetStockDamageLossList` | GET |
| `/inventory/stock-damage-loss/new-stock-damage-loss` | `NewStockDamageLossComponent` | `getStockDamageLossCode()` + `saveStockDamageLoss(body)` | `GetStockDamageLossCode` + `SaveStockDamageLoss` | GET/POST |

### Group E: Reports & Tracking

| UI Route | Component | Service Method | API Endpoint | Verb |
|----------|-----------|----------------|-------------|------|
| `/inventory/employee-item-holding` | `EmployeeItemHoldingComponent` | `getEmployeeHoldingSummaryList()` / `getAllEmployeeHoldings()` | `api/Inventory/GetEmployeeHoldingSummaryList` / `GetAllEmployeeHoldings` | GET |
| `/inventory/stock-balance-summary` | `StockBalanceSummaryComponent` | `getStockBalanceSummary(w, i)` | `api/Inventory/GetStockBalanceSummary` | GET |
| `/inventory/employee-issue-register` | `EmployeeIssueRegisterComponent` | `getEmployeeIssueRegister(...)` | `api/Inventory/GetEmployeeIssueRegister` | GET |
| `/inventory/dashboard` | `InventoryDashboardComponent` | `getInventoryDashboard()` | `api/Inventory/GetInventoryDashboard` | GET |

### Group F: Asset Management

| UI Route | Component | Service Method | API Endpoint | Verb |
|----------|-----------|----------------|-------------|------|
| `/inventory/asset-master` | `AssetMasterComponent` | `getAssetList()` | `api/Inventory/GetAssetList` | GET |
| `/inventory/asset-master/new-asset` | `NewAssetComponent` | `getAssetCode()` + `saveAssetRecord(body)` | `GetAssetCode` + `SaveAsset` | GET/POST |
| `/inventory/asset-allocation` | `AssetAllocationComponent` | `getAssetAllocationList()` + `saveAssetAllocation(body)` | `GetAssetAllocationList` + `SaveAssetAllocation` | GET/POST |
| `/inventory/asset-transfer` | `AssetTransferComponent` | `getAssetTransferList()` + `saveAssetTransfer(body)` | `GetAssetTransferList` + `SaveAssetTransfer` | GET/POST |
| `/inventory/asset-return` | `AssetReturnComponent` | `getAssetReturnList()` + `saveAssetReturn(body)` | `GetAssetReturnList` + `SaveAssetReturn` | GET/POST |
| `/inventory/asset-maintenance` | `AssetMaintenanceComponent` | `getAssetMaintenanceList()` + `saveAssetMaintenance(body)` + `completeAssetMaintenance(id)` | `GetAssetMaintenanceList` + `SaveAssetMaintenance` + `CompleteAssetMaintenance/{id}` | GET/POST |
| `/inventory/asset-damage` | `AssetDamageComponent` | `getAssetDamageList()` + `saveAssetDamage(body)` | `GetAssetDamageList` + `SaveAssetDamage` | GET/POST |
| `/inventory/asset-disposal` | `AssetDisposalComponent` | `getAssetDisposalList()` + `saveAssetDisposal(body)` + `submitAD(id)` + `approveAD(id)` | `GetAssetDisposalList` + `SaveAssetDisposal` + `SubmitAD/{id}` + `ApproveAD/{id}` | GET/POST |
| `/inventory/asset-movement-history/:assetID` | `AssetMovementHistoryComponent` | `getAssetMovementHistory(assetID)` | `api/Inventory/GetAssetMovementHistory` | GET |
| `/inventory/asset-location` | `AssetLocationComponent` | `getAssetLocationLog(assetID)` + `updateAssetLocation(body)` | `GetAssetLocationLog` + `UpdateAssetLocation` | GET/POST |

---

## PART 5 — "etha" Keyword Search Result

A full codebase search was run for: `etha`, `ethiopia`, `newInventory`, `new-inventory`, `NewInventory` across all `.cs`, `.ts`, and `.html` files.

**Result: Zero matches found.**

The word "etha" in the original question is **Tamil colloquial** (எத / எந்த = "which/what"), not a feature name. The inventory system described in this report is the complete OBMS Inventory module for FWG Malaysia — there is no separate "etha" or "Ethiopia" feature.

---

## Conclusions

1. **214 API endpoints** exist across `InventoryController` + `StockRequestController` + `ApprovalController`.
2. **1 service file** (`inventory.service.ts`) handles all 214 calls — no duplication or split services.
3. **83 UI routes** (including report routes) all defined in `admin-layout-routing.module.ts`.
4. The module is built in **9 parts** (Masters → Opening Stock → Purchase Flow → Stock Operations → Tracking → Asset Management → Approval → Reports → BSR).
5. **All components are under** `src/app/modules/inventory/` — no inventory components in other module directories.
6. The API base URL for development is `http://localhost:5002/api/`.

## File Locations Quick Reference

| Item | Path |
|------|------|
| Main API Controller | `OBMSAPP-API\OBMS.WebAPI\Controllers\InventoryController.cs` |
| BSR Controller | `OBMSAPP-API\OBMS.WebAPI\Controllers\StockRequestController.cs` |
| Angular Service | `OBMSAPP-UI\angularapp\src\app\service\inventory.service.ts` |
| Angular Routes | `OBMSAPP-UI\angularapp\src\app\layout\admin-layout\admin-layout-routing.module.ts` |
| Angular Components | `OBMSAPP-UI\angularapp\src\app\modules\inventory\` |
| Environment (Base URL) | `OBMSAPP-UI\angularapp\src\environments\environment.ts` |
