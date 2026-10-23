# OBMS Malaysia — Inventory Module Analysis

**Date generated:** 2026-10-15
**Purpose:** Comprehensive read-only analysis of the Inventory module. All findings are verified
directly from source files. Nothing is invented. Unverified items are explicitly marked
`⚠ NOT VERIFIED` or `⚠ NOT FOUND`.

---

## Table of Contents

1. [Architecture and Folder Structure](#1-architecture-and-folder-structure)
2. [Database Tables and Relationships](#2-database-tables-and-relationships)
3. [Purchase and Stock Workflows](#3-purchase-and-stock-workflows)
4. [Existing Tax / SST-Related Fields and Logic](#4-existing-tax--sst-related-fields-and-logic)
5. [Inventory Reports](#5-inventory-reports)
6. [Dependencies on Finance or Shared Components](#6-dependencies-on-finance-or-shared-components)
7. [Missing Functionality and Implementation Risks](#7-missing-functionality-and-implementation-risks)
8. [Files Likely to Require Changes for Inventory-Only SST](#8-files-likely-to-require-changes-for-inventory-only-sst)

---

## 1. Architecture and Folder Structure

### 1-A. Overall System Architecture

```
Browser (Angular SPA — Port 8644 UAT / 80 Live)
  │
  │  HTTP/REST API calls
  ▼
.NET 7 Web API  (OBMSAPP-API — Port 8227 UAT / 64499 Live)
  │  InventoryController  →  IInventoryRepository  →  EF Core (OBMSDbContext)
  │  FinanceController    →  IFinanceRepository    →  EF Core (OBMSDbContext)
  │  ApprovalController   →  IApprovalRepository   →  EF Core (OBMSDbContext)
  ▼
SQL Server (OBMSUATNEW database, .\SQLEXPRESS)
  │
  │  Shared database — Inventory, Finance, Payroll, HR all use the same DB
  ▼
ASP.NET WebForms Report Server  (UatReports-new — Port 8229 UAT / 8008 Live)
  │  Inventory ASPX Reports — iTextSharp inline PDF
  │  Finance/Payroll Reports  — Crystal Reports .rpt files
  ▼
Browser streams PDF inline
```

### 1-B. Repository Folder Structure

```
d:\FWG-Malasiya\
├── OBMSAPP-API\OBMS.WebAPI\
│   ├── Controllers\
│   │   ├── InventoryController.cs        ← All inventory API endpoints (~150+ routes)
│   │   ├── FinanceController.cs          ← Finance endpoints; reads GRN/Supplier/Category
│   │   ├── ApprovalController.cs         ← Approval workflow (shared by Inventory + Finance)
│   │   ├── CreditDebitNoteController.cs  ← Finance only
│   │   ├── NotificationController.cs     ← Shared
│   │   └── RegisterController.cs         ← Auth / User access rights
│   ├── Repositories\
│   │   ├── Interface\
│   │   │   └── IInventoryRepository.cs   ← Contract (~100+ method signatures)
│   │   └── Implementation\
│   │       └── InventoryRepository.cs    ← EF Core implementation (NO stored procedures)
│   ├── Models\
│   │   ├── Domain\                       ← 80+ entity classes
│   │   │   ├── [all inventory entities — see Section 2]
│   │   │   └── [Finance/Payroll/HR entities share the same folder]
│   │   ├── DTO\                          ← Request/Response DTOs
│   │   └── OBMSDbContext.cs              ← Single shared EF Core DbContext
│   ├── BusinessObjects\                  ← Legacy ADO.NET helpers (NOT used by InventoryRepository)
│   └── Utility\
│       └── Constants.cs                  ← GST/SST date range constants (string fields)
│
├── OBMSAPP-UI\angularapp\src\app\
│   ├── modules\
│   │   ├── inventory\                    ← 37+ sub-folders — see Section 1-C
│   │   ├── finance\                      ← payments, receipts, invoice, credit/debit note, cheque
│   │   ├── payroll\                      ← salary, advance, loan, uniform loan, attendance
│   │   └── administration\              ← users, access rights, approval setup, notifications
│   ├── service\
│   │   ├── inventory.service.ts          ← All inventory API calls (Parts 2–9)
│   │   ├── mastermodule.service.ts       ← Shared lookups: suppliers, categories, user access
│   │   ├── finance.service.ts            ← Finance-specific API calls
│   │   └── auth.service.ts / auth.guard.ts / auth.interceptor.ts
│   ├── layout\admin-layout\
│   │   └── admin-layout-routing.module.ts  ← All routes, all with canActivate: [AuthGuard]
│   └── environments\
│       ├── environment.ts                ← Local dev URLs + TAX constant
│       └── environment.prod.ts           ← Production URLs (FWG Live active, UAT commented out)
│
├── UatReports-new\UatReports\
│   ├── Inventory\                        ← 16 iTextSharp ASPX reports (inventory-owned)
│   ├── Finance\                          ← 60+ Crystal Reports (finance-owned)
│   ├── Accounting\                       ← 9 Crystal Reports (accounting-owned)
│   ├── Payroll\                          ← 50+ Crystal Reports (payroll-owned)
│   ├── Master\                           ← 5 mixed reports
│   ├── bin\                              ← DLLs: itextsharp.dll, BouncyCastle.Crypto.dll,
│   │                                         CrystalDecisions.*.dll
│   ├── Web.config                        ← DB connection + company info + GST/SST date keys
│   └── ReportParamLog.txt                ← Error log (shows missing SPs on server)
│
└── Database\                             ← 28 numbered + ~20 unnumbered migration .sql files
    ├── 01–17.Inventory_*.sql             ← Inventory tables, SPs, views
    ├── 18–25.Asset/Report/Approval_*.sql ← Inventory asset + PDF report SPs
    ├── 10, 19.GRN*_Migration.sql         ← GRN → Finance bridge migrations
    ├── 26–28.PR/BSR/Uniform_*.sql        ← PR task queue, screen access
    └── [Finance, HR, Payroll migrations — separate files]
```

### 1-C. Inventory Angular Module Components

| Sub-folder | Component | Notes |
|---|---|---|
| `category-master` | NewCategoryComponent | Item category CRUD |
| `brand-master` | NewBrandComponent | Brand CRUD |
| `uom-master` | NewUOMComponent | Unit of Measure CRUD |
| `warehouse-master` | NewWarehouseComponent | Warehouse CRUD |
| `item-master/new-item` | NewItemComponent | Item master — Price field (cost price) |
| `supplier-master` | NewSupplierComponent | Supplier CRUD |
| `recepient-master` | NewRecepientComponent | Recipient CRUD |
| `asset-master` | NewAssetComponent | Asset register |
| `opening-stock/new-opening-stock` | NewOpeningStockComponent | UnitCost + TotalValue |
| `stock-ledger` | StockLedgerComponent | Filter + view ledger entries |
| `purchase-requisition/new-pr` | NewPRComponent | BuyingPrice per line |
| `purchase-order/new-po` | NewPOComponent | UnitPrice + LineTotal + TotalAmount |
| `goods-receipt/new-grn` | NewGRNComponent | UnitCost from PO; zero-cost guard |
| `purchase-return/new-return` | NewReturnComponent | UnitCost per line |
| `stock-issue/new-stock-issue` | NewStockIssueComponent | UnitCost per line |
| `stock-issue-return/new-stock-issue-return` | NewStockIssueReturnComponent | UnitCost |
| `stock-transfer/new-stock-transfer` | NewStockTransferComponent | Qty only |
| `branch-stock-request/new-bsr` | NewBSRComponent | BuyingPrice per line |
| `stock-adjustment/new-stock-adjustment` | NewStockAdjustmentComponent | UnitCost + approval workflow |
| `stock-damage-loss/new-stock-damage-loss` | NewStockDamageLossComponent | UnitCost |
| `asset-allocation` | AssetAllocationComponent | |
| `asset-transfer` | AssetTransferComponent | |
| `asset-return` | AssetReturnComponent | |
| `asset-maintenance` | AssetMaintenanceComponent | Cost on completion |
| `asset-damage` | AssetDamageComponent | EstimatedRepairCost |
| `asset-disposal` | AssetDisposalComponent | DisposalValue + approval workflow |
| `asset-movement-history/:assetID` | AssetMovementHistoryComponent | Read-only |
| `asset-location/:assetID` | AssetLocationComponent | Location tracking |
| `stock-balance-summary` | StockBalanceSummaryComponent | Read-only |
| `employee-item-holding` | EmployeeItemHoldingComponent | Read-only |
| `employee-issue-register` | EmployeeIssueRegisterComponent | Read-only |
| `inventory-timesheet` | InventoryTimesheetComponent | Monthly issue timesheet |
| `report/hq-stock-ledger` | HQStockLedgerComponent | Crystal Reports iframe |
| `report/branch-stock-ledger` | BranchStockLedgerComponent | Crystal Reports iframe |
| `report/stock-ledger-report` | StockLedgerReportComponent | iTextSharp iframe |

### 1-D. Technology Stack (Verified)

| Layer | Technology |
|---|---|
| Frontend | Angular 14+ (TypeScript), Angular Material, ngx-select |
| API | .NET 7 C# Web API, Entity Framework Core, JWT Bearer auth |
| Database | SQL Server (SQLEXPRESS), ODMSUATNEW database |
| ORM | EF Core — InventoryRepository uses pure LINQ, no raw SQL/stored procedures |
| PDF Reports | iTextSharp 5.5.13.3 + BouncyCastle.Crypto 1.8.9.0 (Inventory reports) |
| Crystal Reports | v13.0.4000.0 (Finance, Payroll, Accounting, some Master reports) |
| Real-time | SignalR (notifications for approvals, low stock alerts) |

---

## 2. Database Tables and Relationships

### 2-A. Inventory-Owned Tables (complete column catalogue)

All tables are idempotent (IF NOT EXISTS guards). `LASTUPDATE` and `LastUpdatedBy` columns exist on every table and are omitted from individual listings below for brevity.

---

#### Master Tables

**BrandMaster** — `Database/01.Inventory_Master_Migration.sql`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| ID | INT IDENTITY(1,1) | NOT NULL | PK |
| Code | NVARCHAR(50) | NOT NULL | Unique |
| Name | NVARCHAR(200) | NOT NULL | |
| Description | NVARCHAR(500) | NULL | |
| IsActive | BIT | NOT NULL DEFAULT 1 | |

**UOMMaster** — `Database/01.Inventory_Master_Migration.sql`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| ID | INT IDENTITY(1,1) | NOT NULL | PK |
| Code | NVARCHAR(50) | NOT NULL | Unique |
| Name | NVARCHAR(200) | NOT NULL | |
| ShortName | NVARCHAR(50) | NULL | |
| IsPrimary | BIT | NOT NULL DEFAULT 0 | |

Seeded units: Nos, Pair, Box, Set, Kg, Gram, Litre, Millilitre, Meter

**WarehouseMaster** — `Database/01.Inventory_Master_Migration.sql`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| ID | INT IDENTITY(1,1) | NOT NULL | PK |
| Code | NVARCHAR(50) | NOT NULL | Unique |
| Name | NVARCHAR(200) | NOT NULL | |
| Address | NVARCHAR(500) | NULL | |
| BranchID | INT | NOT NULL | No FK — BranchMaster PK is varchar Code |
| IsActive | BIT | NOT NULL DEFAULT 1 | |

**ItemMaster** — Pre-existing table, columns added by migrations

| Column | Type | Nullable | Added by | Notes |
|--------|------|----------|----------|-------|
| ID | INT IDENTITY(1,1) | NOT NULL | Pre-existing | PK |
| Name | NVARCHAR | NOT NULL | Pre-existing | |
| CategoryID | INT | NOT NULL | Pre-existing | FK → InventoryCategory(ID) |
| **Price** | DECIMAL | NOT NULL | Pre-existing | **Buying/cost price — used as CostPerUnit** |
| **SellPrice** | DECIMAL? | NULL | Pre-existing | Not exposed in UI; retained for DB compatibility |
| **Quantity** | DECIMAL? | NULL | Pre-existing | Not exposed; stock tracked via StockLedger |
| Code | NVARCHAR(50) | NULL | Migration 01 | Auto-generated ITM0001… |
| BrandID | INT | NULL | Migration 01 | FK → BrandMaster(ID) |
| UOMID | INT | NULL | Migration 01 | FK → UOMMaster(ID) |
| ReorderLevel | DECIMAL(18,2) | NULL | Migration 01 | |
| ReorderQuantity | DECIMAL(18,2) | NULL | Migration 01 | |
| WarehouseID | INT | NULL | Migration 13 | FK → WarehouseMaster(ID) ON DELETE SET NULL |

**InventoryCategory** — Pre-existing, extended

| Column | Added by | Notes |
|--------|----------|-------|
| ParentCategoryID | Migration 01 | FK → InventoryCategory(ID) — self-referential |

---

#### Opening Stock & Ledger

**OpeningStock** — `Database/02.OpeningStock_StockLedger_Migration.sql`

| Column | Type | Notes |
|--------|------|-------|
| ID | INT IDENTITY(1,1) | PK |
| ReferenceNo | NVARCHAR(20) | e.g. OS0001 |
| WarehouseID | INT | FK → WarehouseMaster(ID) |
| ItemID | INT | FK → ItemMaster(ID) |
| OpeningDate | DATE | |
| OpeningQty | DECIMAL(18,2) | |
| **UnitCost** | DECIMAL(18,4) | Cost per unit at opening |
| **TotalValue** | DECIMAL(18,4) | Stored = OpeningQty × UnitCost |
| IsActive | BIT DEFAULT 1 | |

Unique filtered index: (WarehouseID, ItemID) WHERE IsActive = 1

**StockLedger** — `Database/02.OpeningStock_StockLedger_Migration.sql`

| Column | Type | Notes |
|--------|------|-------|
| ID | INT IDENTITY(1,1) | PK |
| TransactionNo | NVARCHAR(20) | |
| TransactionDate | DATETIME | |
| TransactionType | NVARCHAR(50) | See transaction types below |
| WarehouseID | INT | FK → WarehouseMaster(ID) |
| ItemID | INT | FK → ItemMaster(ID) |
| InQty | DECIMAL(18,2) DEFAULT 0 | Qty flowing INTO warehouse |
| OutQty | DECIMAL(18,2) DEFAULT 0 | Qty flowing OUT of warehouse |
| **UnitCost** | DECIMAL(18,4) NULL | Cost per unit at time of movement |
| ReferenceType | NVARCHAR(50) NULL | e.g. 'Opening Stock', 'Goods Receipt' |
| ReferenceID | INT NULL | FK to source document ID |
| ReferenceNo | NVARCHAR(50) NULL | e.g. 'GRN0001' |
| Remarks | NVARCHAR(500) NULL | |

**TransactionType values:**

| Value | Meaning | InQty | OutQty |
|-------|---------|-------|--------|
| `OPENING` | Opening Stock entry | ✓ | — |
| `STOCK_IN` | GRN/Goods Receipt | ✓ | — |
| `ISSUE` | Stock Issue to employee/branch | — | ✓ |
| `ISSUE_RETURN` | Issue Return | ✓ | — |
| `TRANSFER_IN` | Transfer in (destination warehouse) | ✓ | — |
| `TRANSFER_OUT` | Transfer out (source warehouse) | — | ✓ |
| `ADJ_PLUS` | Positive Adjustment | ✓ | — |
| `ADJ_MINUS` | Negative Adjustment | — | ✓ |
| `PURCHASE_RTN` | Purchase Return to supplier | — | ✓ |
| `DAMAGE_LOSS` | Damage or Loss record | — | ✓ |

**Current stock formula:** `SUM(InQty) - SUM(OutQty)` per `(WarehouseID, ItemID)`

**VWStockLedger** — `Database/17.StockLedgerReport_Migration.sql`

A SQL view joining StockLedger with WarehouseMaster, ItemMaster, InventoryCategory, BrandMaster, UOMMaster. Adds:
- `TransactionTypeLabel` — human-readable label via CASE
- `RunningBalance` — `SUM(InQty - OutQty) OVER (PARTITION BY WarehouseID, ItemID ORDER BY TransactionDate, ID ROWS UNBOUNDED PRECEDING TO CURRENT ROW)`

Running balance is partitioned over ALL history, not just the filtered date window, ensuring true cumulative balance.

---

#### Purchase Flow Tables

**PurchaseRequisition** — `Database/03.PurchaseFlow_Migration.sql`

| Column | Type | Notes |
|--------|------|-------|
| ID | INT IDENTITY(1,1) | PK |
| PRNumber | NVARCHAR(20) | Unique WHERE IsActive=1. Auto: PR0001… |
| PRDate / RequiredByDate | DATE | |
| Status | NVARCHAR(20) DEFAULT 'Draft' | Draft → Submitted → Approved / Rejected |
| Remarks | NVARCHAR(500) NULL | |
| ApprovedBy / ApprovedDate / ApprovalRemarks | Various NULL | |
| SubmittedBy / SubmittedDate | NVARCHAR(100) / DATETIME NULL | Added Migration 08 |
| IsActive | BIT DEFAULT 1 | |

**PurchaseRequisitionDetail** — `Database/03.PurchaseFlow_Migration.sql`

| Column | Type | Notes |
|--------|------|-------|
| ID | INT IDENTITY(1,1) | PK |
| PRID | INT | FK → PurchaseRequisition(ID) |
| ItemID | INT | FK → ItemMaster(ID) |
| RequiredQty | DECIMAL(18,2) | |
| **BuyingPrice** | DECIMAL(18,4) NULL | Added Migration 14. Auto-filled from ItemMaster.Price; user-editable |
| UOMID | INT NULL | FK → UOMMaster(ID) |

**PurchaseOrder** — `Database/03.PurchaseFlow_Migration.sql`

| Column | Type | Notes |
|--------|------|-------|
| ID | INT IDENTITY(1,1) | PK |
| PONumber | NVARCHAR(20) | Unique WHERE IsActive=1. Auto: PO0001… |
| PRID | INT NULL | FK → PurchaseRequisition(ID) — **nullable**, PO can exist without PR |
| SupplierID | INT | FK → Suppliers(Id) |
| WarehouseID | INT | FK → WarehouseMaster(ID) |
| PODate / ExpectedDate | DATE | |
| Status | NVARCHAR(30) DEFAULT 'Draft' | Draft → Submitted → Approved → Partially Received → Fully Received / Rejected / Cancelled |
| **TotalAmount** | DECIMAL(18,4) DEFAULT 0 | |
| SubmittedBy / SubmittedDate | Added Migration 08 | |
| RejectReason | NVARCHAR(1000) NULL | Added Migration 08 |
| IsActive | BIT DEFAULT 1 | |

**PurchaseOrderDetail** — `Database/03.PurchaseFlow_Migration.sql`

| Column | Type | Notes |
|--------|------|-------|
| ID | INT IDENTITY(1,1) | PK |
| POID | INT | FK → PurchaseOrder(ID) |
| ItemID | INT | FK → ItemMaster(ID) |
| UOMID | INT NULL | FK → UOMMaster(ID) |
| OrderedQty | DECIMAL(18,2) | |
| ReceivedQty | DECIMAL(18,2) DEFAULT 0 | Cumulative across all GRNs |
| **UnitPrice** | DECIMAL(18,4) DEFAULT 0 | |
| **TotalPrice** | DECIMAL(18,4) DEFAULT 0 | Stored = OrderedQty × UnitPrice |

**GoodsReceiptNote** — `Database/03.PurchaseFlow_Migration.sql`

| Column | Type | Notes |
|--------|------|-------|
| ID | INT IDENTITY(1,1) | PK |
| GRNNumber | NVARCHAR(20) | Unique WHERE IsActive=1. Auto: GRN0001… |
| POID | INT | FK → PurchaseOrder(ID) |
| WarehouseID | INT | FK → WarehouseMaster(ID) |
| GRNDate | DATE | |
| DeliveryNoteRef | NVARCHAR(100) NULL | |
| SupplierInvoiceNo | NVARCHAR(100) NULL | Added Migration 11 |
| IsActive | BIT DEFAULT 1 | |

**GoodsReceiptNoteDetail** — `Database/03.PurchaseFlow_Migration.sql`

| Column | Type | Notes |
|--------|------|-------|
| GRNID | INT | FK → GoodsReceiptNote(ID) |
| PODetailID | INT | FK → PurchaseOrderDetail(ID) |
| ItemID | INT | FK → ItemMaster(ID) |
| ReceivedQty | DECIMAL(18,2) | |
| **UnitCost** | DECIMAL(18,4) DEFAULT 0 | Carries forward from PO UnitPrice |

**PurchaseReturn / PurchaseReturnDetail** — `Database/03.PurchaseFlow_Migration.sql`
- Return links to GRN via `GRNID` and to GRN detail via `GRNDetailID`
- Detail carries `UnitCost DECIMAL(18,4)`

---

#### Stock Operation Tables (all created in `Database/04.StockOperations_Part4_Migration.sql`)

Each detail table carries a `UnitCost DECIMAL(18,4) DEFAULT 0` for stock valuation. No tax columns on any.

| Table | Key Columns | Ledger Entry |
|-------|-------------|--------------|
| StockIssueDoc | IssueNo, IssueDate, WarehouseID, IssueToType (Employee/Guard/Branch/Other) | — |
| StockIssueDocDetail | IssueDocID, ItemID, IssueQty, **UnitCost** | ISSUE (OutQty) |
| StockIssueReturn | ReturnNo, ReturnDate, OriginalIssueDocID, WarehouseID | — |
| StockIssueReturnDetail | ReturnID, OriginalIssueDetailID, ItemID, ReturnQty, **UnitCost** | ISSUE_RETURN (InQty) |
| StockTransfer | TransferNo, TransferDate, SourceWarehouseID, DestWarehouseID | — |
| StockTransferDetail | TransferID, ItemID, TransferQty, **UnitCost** | TRANSFER_OUT + TRANSFER_IN (paired) |
| StockAdjustment | AdjustmentNo, AdjustmentDate, WarehouseID, ItemID, AdjustmentType (PLUS/MINUS), AdjustmentQty, **UnitCost**, Status | ADJ_PLUS / ADJ_MINUS (only on Approve) |
| StockDamageLoss | RecordNo, RecordDate, WarehouseID, ItemID, LossType (Damage/Loss), Quantity, **UnitCost** | DAMAGE_LOSS (OutQty) |

**StockAdjustment approval columns** (added `Database/16.StockAdjustment_ApprovalWorkflow_Migration.sql`):
`Status NVARCHAR(20) DEFAULT 'Draft'`, `SubmittedBy`, `SubmittedDate`, `ApprovedBy`, `ApprovedDate`, `ApprovalRemarks`

**Critical rule (Migration 16):** StockAdjustment ledger entries are created ONLY when Status = 'Approved', not at Save time.

---

#### Branch Stock Request (BSR)

**StockRequest** — `Database/09.BranchStockRequest_Migration.sql`

| Column | Type | Notes |
|--------|------|-------|
| RequestNo | NVARCHAR(20) | Auto: BSR0001… |
| RequestingBranchID | INT | No FK — BranchMaster uses varchar Code |
| SourceWarehouseID | INT | FK → WarehouseMaster(ID) |
| RequestedBy | NVARCHAR(200) | From JWT |
| Status | NVARCHAR(20) DEFAULT 'Draft' | Draft → Submitted → Approved / Rejected |
| LinkedTransferID | INT NULL | FK → StockTransfer(ID) — set after fulfilling transfer created |

**StockRequestDetail** — extended by Migrations 12 and 14

| Column | Added | Notes |
|--------|-------|-------|
| RequestedQty | Migration 09 | |
| ClientID | Migration 12 | FK → ClientMaster(ID) NULL |
| **BuyingPrice** | Migration 14 | DECIMAL(18,4) NULL — auto-filled from ItemMaster.Price |

---

#### Asset Management Tables (all `Database/06.AssetManagement_Part6_Migration.sql`)

**AssetMaster** — pre-existing table, extended:

| Key Financial Column | Type | Notes |
|---|---|---|
| **PurchaseAmount** | double? (C# domain) / DECIMAL in DB | Original purchase price |
| PurchaseDate | DATE NULL | |
| WarrantyExpiryDate | DATE NULL | |
| AssetStatus | NVARCHAR(30) DEFAULT 'Available' | Available/Allocated/In_Transit/Under_Maintenance/Damaged/Disposed/Lost |

Location tracking columns added Migration 07: `CurrentLocation`, `LocationStatus`, `LocationUpdatedBy`, `LocationUpdatedDate`

**AssetDisposal** — key financial column: `DisposalValue DECIMAL(18,2) NULL DEFAULT 0`
Approval columns added Migration 18: `Status DEFAULT 'Draft'`, `SubmittedBy`, `SubmittedDate`, `ApprovedDate`, `ApprovalRemarks`

**AssetMaintenance** — `MaintenanceCost DECIMAL(18,2) DEFAULT 0`
**AssetDamage** — `EstimatedRepairCost DECIMAL(18,2) DEFAULT 0`

Other asset tables (AssetAllocation, AssetTransfer, AssetReturn, AssetMovementHistory, AssetLocationLog) carry no financial fields beyond text references.

---

#### StockIssueUniformLoanLink (Bridge table — `Database/12.StockIssueUniformLoanLink_Migration.sql`)

Links `StockIssueDoc(ID)` to `SalaryAdvance(ID)`. Unique filtered index on `StockIssueDocID WHERE IsActive = 1` — one issue can only be linked to one active uniform loan. This is a Payroll ↔ Inventory bridge table.

---

### 2-B. Approval / Notification Tables (Shared — `Database/08.ApprovalNotification_Part7_Migration.sql`)

These tables are shared across all modules but are populated and read exclusively via inventory-triggered events for inventory document types.

| Table | Purpose |
|-------|---------|
| ApprovalConfiguration | Per document-type on/off switch. Columns: DocumentType, DisplayName, ApprovalRequired BIT |
| ApprovalHistory | Immutable audit trail. Columns: DocumentType, DocumentID, DocumentNumber, Action (Submitted/Approved/Rejected/Cancelled), ActionBy, ActionDate, Comments, PreviousStatus, NewStatus |
| AppNotification | Push notifications. Columns: NotificationType, Title, Message, RecipientUser, ReferenceType, ReferenceID, IsRead |
| ApprovalRouting | Maps (DocumentType + RequesterUser) → ApproverUser. One active row per requester per doc type |
| PRProcessingTask | Purchase dept task queue for approved PRs. ClaimedBy, Status (Pending/Claimed/Completed) |

**Inventory document types registered in ApprovalConfiguration:**

| DocumentType | ApprovalRequired (default) |
|---|---|
| PURCHASE_REQUISITION | 1 (required) |
| PURCHASE_ORDER | 1 (required) |
| STOCK_ADJUSTMENT | 0 (optional) |
| STOCK_TRANSFER | 0 (optional) |
| DAMAGE_LOSS | 0 (optional) |
| ASSET_DISPOSAL | 0 (optional) |
| BRANCH_STOCK_REQUEST | 1 (required) |

**VW_PendingApprovals view** (latest fix in `Database/15.ApprovalSystem_BugFix_Migration.sql`):
Unions PurchaseRequisition, PurchaseOrder, and StockRequest where Status = 'Submitted'. StockAdjustment and AssetDisposal pending records do NOT appear in this view — they have their own approval endpoints.

---

### 2-C. Finance Bridge Tables (Inventory creates, Finance reads)

**BranchPaymentDetails.GRNID** — `Database/10.GRNPaymentLink_Migration.sql`
- Column added: `GRNID INT NULL` with FK → GoodsReceiptNote(ID)
- Finance Payments screen references this to link a payment to a GRN directly

**CreditorInvoice** — Finance-owned table. `SaveGRN()` in InventoryRepository creates a row automatically:

| Field written by SaveGRN | Value |
|---|---|
| Branch | WarehouseMaster → BranchMaster.Code |
| Supplier | PurchaseOrder.SupplierID |
| RecID | GoodsReceiptNote.ID |
| InvoiceNo | SupplierInvoiceNo (or GRNNumber if blank) |
| InvoiceType | `"G"` (GRN type) |
| InvoiceDate | GRNDate |
| **Total** | `SUM(ReceivedQty × UnitCost)` — no tax |
| PaymentDate | GRNDate |

This CreditorInvoice row then flows into Finance's `vwPaymentDue` view and the Payments screen.

---

### 2-D. Relationship Diagram

```
ItemMaster ──────────────────────────────────────────────────────────────────┐
   │ (CategoryID)         (BrandID)          (UOMID)          (WarehouseID)  │
   ▼                                                                          │
InventoryCategory    BrandMaster         UOMMaster          WarehouseMaster  │
                                                                    │         │
                                                                    ▼         │
PurchaseRequisition ─────────────────────────────────────────── (BranchID → BranchMaster)
   │ ID
   ▼ (PRID nullable)
PurchaseOrder ──────────────────── SupplierID → Suppliers
   │ ID                            WarehouseID → WarehouseMaster
   ▼ (POID)
GoodsReceiptNote ────────────────── WarehouseID → WarehouseMaster
   │ ID               ────────────────► CreditorInvoice (InvoiceType='G')
   ▼ (GRNID)                       └──► BranchPaymentDetails.GRNID
PurchaseReturn
   │
   ▼ (GRNID)
PurchaseReturnDetail

StockLedger ◄─── written by: GRN save (STOCK_IN), Issue (ISSUE),
                              IssueReturn (ISSUE_RETURN), Transfer (TRANSFER_OUT/IN),
                              Adjustment on Approve (ADJ_PLUS/ADJ_MINUS),
                              DamageLoss (DAMAGE_LOSS), PurchaseReturn (PURCHASE_RTN),
                              OpeningStock save (OPENING)
```

---

## 3. Purchase and Stock Workflows

### 3-A. Purchase Workflow (PR → PO → GRN)

```
1. User creates Purchase Requisition (PR)
   ├── Header: PRDate, RequiredByDate, Remarks
   ├── Lines: ItemID, RequiredQty, BuyingPrice (auto-filled from ItemMaster.Price)
   └── Status: Draft

2. User submits PR → Status: Submitted
   ├── If ApprovalRequired = true AND routing configured:
   │     → ApprovalHistory row created
   │     → Notification sent to approver via AppNotification
   │     → PRProcessingTask row created (Purchase dept inbox)
   └── If ApprovalRequired = false OR no routing:
         → Auto-approved immediately

3. Approver approves/rejects PR
   ├── Approve: Status = 'Approved'
   │     → Notification sent back to requester
   │     → PRProcessingTask available for Purchase dept to claim
   └── Reject: Status = 'Rejected', rejection reason recorded in ApprovalRemarks

4. Purchase dept creates Purchase Order (PO)
   ├── Can be created directly (PRID = null) OR from approved PR (PRID = PR.ID)
   ├── Lines: ItemID, OrderedQty, UnitPrice (auto-filled from ItemMaster.Price / PR BuyingPrice)
   ├── TotalAmount = SUM(OrderedQty × UnitPrice)
   └── Status: Draft

5. User submits PO → Status: Submitted (same approval flow as PR)

6. Approver approves PO → Status: 'Approved'

7. User creates Goods Receipt Note (GRN) against approved PO
   ├── ReceivedQty per line (≤ PendingQty; UnitCost = PO UnitPrice)
   ├── GUARD: blocks save if any receiving line has UnitCost = 0
   └── On save (transactional):
       a. GoodsReceiptNote + GoodsReceiptNoteDetail rows created
       b. PurchaseOrderDetail.ReceivedQty updated per line
       c. PurchaseOrder.Status → 'Partially Received' or 'Fully Received'
       d. StockLedger STOCK_IN entry per line (with UnitCost)
       e. CreditorInvoice row created (InvoiceType='G', Total = Σ ReceivedQty × UnitCost)
       f. Low-stock notifications triggered per item

8. Finance creates Payment against the CreditorInvoice (GRN-based)
   └── BranchPaymentDetails.GRNID links back to GoodsReceiptNote.ID
```

**Key constraints:**
- Multiple GRNs allowed per PO (partial deliveries)
- GRN cannot be deleted if a Finance Payment or Purchase Return already exists against it
- PR can be submitted only with ≥1 line
- PO can exist without a PR (direct purchase)
- PO cannot be GRN'd unless Status = 'Approved' or 'Partially Received'

---

### 3-B. Stock Operations Workflows

All stock operations write to the StockLedger table in a single EF Core transaction. The stock balance is always `SUM(InQty) - SUM(OutQty)` from StockLedger — `ItemMaster.Quantity` is deprecated.

| Operation | Source | Ledger entry | Validation |
|---|---|---|---|
| Opening Stock | New opening record | OPENING (InQty) | Unique per item+warehouse |
| Stock Issue | Warehouse → Employee / Guard / Branch / Other | ISSUE (OutQty) | Cannot exceed current stock |
| Issue Return | Return against original issue | ISSUE_RETURN (InQty) | Cannot exceed originally issued qty |
| Stock Transfer | Warehouse A → Warehouse B | TRANSFER_OUT (OutQty from A) + TRANSFER_IN (InQty to B) | Source ≠ Destination; cannot exceed available stock |
| Stock Adjustment | Manual correction | ADJ_PLUS (InQty) or ADJ_MINUS (OutQty) | **Written ONLY on Approval**, not on Save |
| Damage / Loss | Write-off | DAMAGE_LOSS (OutQty) | Cannot exceed available stock |
| Purchase Return | Return to supplier against GRN | PURCHASE_RTN (OutQty) | Cannot exceed GRN received qty |
| BSR (Branch Stock Request) | Branch requests stock from warehouse | No ledger entry — BSR only requests; stock moves via StockTransfer after approval | BSR must be Approved; linked StockTransfer created separately |

---

### 3-C. Approval Workflow (Shared Mechanism)

All inventory documents that require approval follow the same pattern:

```
Draft  →  Submit  →  Submitted  →  Approve / Reject
```

Documents with approval workflows: PR, PO, Stock Adjustment, Asset Disposal, BSR

The workflow is loosely coupled:
- Each document table has its own `Status`, `SubmittedBy`, `SubmittedDate`, `ApprovedBy`, `ApprovedDate`, `ApprovalRemarks` columns
- `ApprovalConfiguration.ApprovalRequired` (BIT) is the master on/off switch per document type
- `ApprovalRouting` maps (DocumentType + RequesterUser) → ApproverUser
- If ApprovalRequired = false OR no routing configured → document auto-approves on Submit
- `ApprovalHistory` records every action (immutable, no FK constraint)
- `AppNotification` sends in-app notifications via SignalR

---

## 4. Existing Tax / SST-Related Fields and Logic

This is the most critical section for any SST implementation work.

### 4-A. Summary Finding

> **There is ZERO tax / SST / GST logic anywhere in the Inventory module.**
> Tax fields exist exclusively in the Finance module (ClientInvoice, Receipts, CreditNote, DebitNote).

This was verified exhaustively across:
- All `.cs` API files in `OBMSAPP-API\`
- All `.ts` and `.html` Angular files in `OBMSAPP-UI\angularapp\src\`
- All `.sql` migration files in `Database\`
- All `.aspx` report files in `UatReports-new\UatReports\Inventory\`

---

### 4-B. Tax Fields in Finance Module (NOT Inventory)

These exist and are confirmed verified. Listed here as context for what Inventory SST implementation would need to mirror or integrate with.

| Table | Column | Type | Owner Module |
|-------|--------|------|---|
| ClientInvoice | TaxAmount | DECIMAL | Finance |
| Receipts | TaxPercentage | DECIMAL | Finance |
| Receipts | TaxAmount | DECIMAL | Finance |
| CreditNote | TaxPercentage | DECIMAL(18,2) | Finance |
| CreditNote | TaxAmount | DECIMAL(18,2) | Finance |
| DebitNote | TaxPercentage | DECIMAL(18,2) | Finance |
| DebitNote | TaxAmount | DECIMAL(18,2) | Finance |

Tax fields in Finance reports:
- `Finance/InvoiceAgeingReport.aspx.cs` — `ci.TaxAmount` in every aging bucket: `ServiceCharges - Discount + TaxAmount - payments`
- `Finance/InvoiceCollectionBranchSummaryReport.aspx.cs` — comment explicitly labels it: `"Tax Amount (SST)"`
- Finance FinanceRepository.cs — reads `TaxAmount` and `TaxPercentage` from invoices and receipts

---

### 4-C. GST/SST Date Range Constants (Verified In Three Locations)

| Location | Key | Value |
|---|---|---|
| `UatReports-new/UatReports/Web.config` | GSTStart6 / GSTEnd6 | 01/01/2011 → 05/31/2018 |
| `UatReports-new/UatReports/Web.config` | GSTStart0 / GSTEnd0 | 06/01/2018 → 08/31/2018 |
| `UatReports-new/UatReports/Web.config` | SSTStart6 / SSTEnd6 | 09/01/2018 → 12/31/2099 |
| `UatReports-new/UatReports/Web.config` | SSTStart8 / SSTEnd8 | 01/03/2024 → 31/12/9999 |
| `OBMSAPP-UI/.../environment.ts` | Same keys as `TAX` export | Identical values |
| `OBMSAPP-API/.../Utility/Constants.cs` | Same keys as string constants | Identical values |

**Critical finding:** The `TAX` constant exported from `environment.ts` is **NOT imported or used by any inventory or finance Angular component**. It is defined but dormant — none of the inventory forms (PR, PO, GRN, etc.) import it. It was likely added in anticipation of future tax work.

---

### 4-D. Current Inventory Pricing Model (What Exists Today)

All inventory pricing is simple: **quantity × unit price = line total**, no tax.

| Field | Where | Purpose |
|---|---|---|
| `ItemMaster.Price` | ItemMaster | Master cost price; auto-fills BuyingPrice/UnitPrice on PR/PO |
| `PurchaseRequisitionDetail.BuyingPrice` | PR lines | Informational cost reference; added Migration 14 |
| `PurchaseOrderDetail.UnitPrice` | PO lines | Ordered unit price |
| `PurchaseOrderDetail.TotalPrice` | PO lines | Stored = OrderedQty × UnitPrice |
| `PurchaseOrder.TotalAmount` | PO header | Sum of all TotalPrice |
| `GoodsReceiptNoteDetail.UnitCost` | GRN lines | Carries from PO UnitPrice |
| `StockLedger.UnitCost` | Every ledger row | Cost at time of movement |
| `OpeningStock.UnitCost` / `.TotalValue` | Opening entry | Cost per unit + stored total |
| `StockIssueDocDetail.UnitCost` | Issue lines | Stock valuation |
| `StockTransferDetail.UnitCost` | Transfer lines | Stock valuation |
| `StockAdjustment.UnitCost` | Adjustment | Stock valuation |
| `StockDamageLoss.UnitCost` | Damage/Loss | Stock valuation |
| `AssetMaster.PurchaseAmount` | Asset | Original purchase price (typed `double?` — see risk) |
| `AssetDisposal.DisposalValue` | Disposal | Proceeds from disposal |

---

## 5. Inventory Reports

### 5-A. Technology Split

| Folder | Technology | Count |
|--------|------------|-------|
| `Inventory/` | **iTextSharp 5.5.13.3** — inline .aspx (all logic in `<script runat="server">`) | **16 reports** |
| `Finance/` | Crystal Reports 13.0.4000.0 (.rpt + .aspx.cs code-behind) | 60+ reports |
| `Accounting/` | Crystal Reports | 9 reports |
| `Payroll/` | Crystal Reports | 50+ reports |
| `Master/` | Mixed | 5 reports |

All inventory reports use iTextSharp for PDF generation, served directly from the ASP.NET WebForms report server. The Inventory folder is entirely self-contained — no shared base class or code-behind file is used (logic is inline).

---

### 5-B. Inventory Report Catalogue

| Report File | SP Called | Parameters | PDF Columns | SST/Tax |
|---|---|---|---|---|
| `StockLedgerReport.aspx` | `sp_InventoryStockLedger` | LoginID, StartDate, EndDate, WarehouseID, ItemID, TransactionType, format | Date, Txn No, Type, Item Code, Item Name, UOM, In Qty, Out Qty, Balance, Unit Cost, Ref No, Remarks | **None** |
| `GRNReport.aspx` | `sp_Report_GRN` | LoginID, GRNID | #, Code, Item Name, Category, UOM, Ordered Qty, Received Qty, Unit Cost (RM), Total (RM) | **None** |
| `POReport.aspx` | `sp_Report_PO` | LoginID, POID | #, Code, Item Name, Category, UOM, Ordered, Received, Pending, Unit Price (RM), Total (RM) | **None** |
| `PRReport.aspx` | `sp_Report_PR` | LoginID, PRID | #, Item Code, Item Name, Category, UOM, Qty, Remarks | **None** |
| `BSRReport.aspx` | `sp_Report_BSR` | LoginID, BSRID | #, Code, Item Name, Category, UOM, Qty, Client, Remarks | **None** |
| `StockIssueReport.aspx` | `sp_Report_StockIssue` | LoginID, IssueDocID | #, Code, Item Name, Category, UOM, Issued Qty, Unit Cost (RM), Total (RM) | **None** |
| `IssueReturnReport.aspx` | `sp_Report_StockIssueReturn` | LoginID, IssueReturnID | #, Code, Item Name, Category, UOM, Return Qty, Unit Cost (RM), Total (RM) | **None** |
| `StockTransferReport.aspx` | `sp_Report_StockTransfer` | LoginID, TransferID | #, Code, Item Name, Category, UOM, Transfer Qty, Unit Cost (RM), Total (RM) | **None** |
| `StockAdjustmentReport.aspx` | `sp_Report_StockAdjustment` | LoginID, AdjustmentID | #, Code, Item Name, Category, UOM, Adjustment Qty (±), Unit Cost (RM), Total (RM) | **None** |
| `AssetMasterReport.aspx` | `sp_Report_AssetMaster` | LoginID, AssetID | Asset details, PurchaseAmount (RM) | **None** |
| `AssetAllocationReport.aspx` | `sp_Report_AssetAllocation` | LoginID, AllocationID | Allocation details | **None** |
| `AssetTransferReport.aspx` | `sp_Report_AssetTransfer` | LoginID, TransferID | Transfer details | **None** |
| `AssetReturnReport.aspx` | `sp_Report_AssetReturn` | LoginID, ReturnID | Return details | **None** |
| `AssetMaintenanceReport.aspx` | `sp_Report_AssetMaintenance` | LoginID, MaintenanceID | Maintenance details, Cost (RM) | **None** |
| `AssetDamageReport.aspx` | `sp_Report_AssetDamage` | LoginID, DamageID | Damage details, EstimatedRepairCost (RM) | **None** |
| `AssetDisposalReport.aspx` | `sp_Report_AssetDisposal` | LoginID, DisposalID | Disposal details, DisposalValue (RM) | **None** |

Additionally, `BranchStockLedgerReport.aspx` and `HQStockLedger.aspx` exist in the Inventory folder but use Crystal Reports .rpt (not iTextSharp). Their stored procedures are embedded inside the .rpt binary file and are not visible in the ASPX.

---

### 5-C. Stored Procedures for Inventory Reports (complete list)

All 17 SPs are defined in the migration files, are SELECT-only, and contain no tax/SST logic:

| SP | Defined In |
|---|---|
| `sp_InventoryStockLedger` | Migration 17 |
| `sp_Report_PR` | Migration 20 |
| `sp_Report_PO` | Migration 20 |
| `sp_Report_GRN` | Migration 20 |
| `sp_Report_BSR` | Migration 20 |
| `sp_Report_StockIssue` | Migration 21 |
| `sp_Report_StockIssueReturn` | Migration 22 |
| `sp_Report_StockTransfer` | Migration 23 |
| `sp_Report_StockAdjustment` | Migration 24 |
| `sp_Report_AssetMaster` | Migration 25 |
| `sp_Report_AssetAllocation` | Migration 25 |
| `sp_Report_AssetTransfer` | Migration 25 |
| `sp_Report_AssetReturn` | Migration 25 |
| `sp_Report_AssetMaintenance` | Migration 25 |
| `sp_Report_AssetDamage` | Migration 25 |
| `sp_Report_AssetDisposal` | Migration 25 |

**⚠ Active server issue (from ReportParamLog.txt, verified 2026-10-10):** The following SPs are missing on the UAT server — their migration files have NOT been run yet:
- `sp_InventoryStockLedger` (Migration 17)
- `sp_Report_StockIssue` (Migration 21)
- `sp_Report_StockIssueReturn` (Migration 22)
- `sp_Report_StockTransfer` (Migration 23)

Additionally, as of 2026-10-07, `BouncyCastle.Crypto.dll` was missing from the server `bin/` folder, causing PDF generation failures for all iTextSharp reports.

---

### 5-D. Report Server Configuration (Web.config)

```xml
<!-- Company identity -->
<add key="CompanyCode"   value="FWG"/>
<add key="CompanyName"   value="FREIGHTWATCH G PATOMOTOH SECURITY SERVICES. SDN.BHD."/>
<add key="Registration"  value="(199501027914)"/>
<add key="Address1"      value="No. 24, 1ST Floor, Taman Bukit Emas,"/>
<add key="City"          value="Seremban"/>
<add key="State"         value="N.S.D.K"/>

<!-- DB connection -->
<add key="Server"        value=".\SQLEXPRESS"/>
<add key="Database"      value="OBMSUATNEW"/>
<add key="UserID"        value="sa"/>
<add key="Password"      value="server@123"/>

<!-- GST/SST date ranges (read by Finance Crystal Reports) -->
<add key="GSTStart6"     value="01/01/2011"/>
<add key="GSTEnd6"       value="05/31/2018"/>
<add key="GSTStart0"     value="06/01/2018"/>
<add key="GSTEnd0"       value="08/31/2018"/>
<add key="SSTStart6"     value="09/01/2018"/>
<add key="SSTEnd6"       value="12/31/2099"/>
<add key="SSTStart8"     value="01/03/2024"/>
<add key="SSTEnd8"       value="31/12/9999"/>
```

---

## 6. Dependencies on Finance or Shared Components

### 6-A. Dependency Map

```
Inventory Module
    │
    ├── WRITES TO (Finance tables)
    │    └── CreditorInvoice — SaveGRN() creates a row (InvoiceType='G')
    │         └── Finance Payment screen reads this to show GRN-based payables
    │
    ├── SHARED TABLES (both modules read/write)
    │    ├── Suppliers — Inventory uses in PO; Finance uses in Payments; FinanceController.GetSuppliers reads it
    │    ├── InventoryCategory — FinanceController.GetInventoryCategories reads it for payment categorisation
    │    └── BranchPaymentDetails.GRNID — Finance Payments writes this; Inventory SaveGRN checks it to block deletion
    │
    ├── USES (shared infrastructure)
    │    ├── OBMSDbContext — single shared EF Core context
    │    ├── OBMSPermissions / ScreenList — RBAC, shared by all modules
    │    ├── ApprovalConfiguration / ApprovalHistory / AppNotification / ApprovalRouting — shared approval framework
    │    ├── BranchMaster / OBMSUsers / OBMSBranches — pre-existing shared master data
    │    └── SignalR NotificationHub — real-time notification delivery
    │
    └── CROSS-MODULE LINKS (Payroll ↔ Inventory)
         └── StockIssueUniformLoanLink — bridges StockIssueDoc (Inventory) to SalaryAdvance (Payroll)
              └── PayrollModule uses Inventory service to link issues to uniform loans
```

### 6-B. Finance → Inventory (Finance depends on Inventory)

| Finance endpoint / screen | What it reads from Inventory |
|---|---|
| `FinanceController.GetGRNsForPayment` (line ~1091) | Directly queries GoodsReceiptNotes, GoodsReceiptNoteDetails, PurchaseOrders, Suppliers, WarehouseMasters, BranchMasters to compute GRN balance for payment |
| `FinanceController.GetInventoryCategories` | Reads InventoryCategory table for payment category selection |
| `FinanceController.GetSuppliers` | Reads Suppliers table (shared) |
| `payments.component.ts` (Angular) | Calls `getGRNsForPayment(supplierID)` when PaymentRefType = 'G'; loads GRN-based payables |

**Direction:** Finance → Inventory. Finance reads inventory data. Inventory does NOT call Finance endpoints.

### 6-C. Inventory → Finance (Inventory creates Finance records)

| Inventory action | Finance record created |
|---|---|
| `SaveGRN()` in InventoryRepository | Creates `CreditorInvoice` row (InvoiceType='G') automatically within the same transaction |
| `DeleteGRN()` in InventoryRepository | Checks `BranchPaymentDetails.GRNID` — blocks deletion if a payment exists |

### 6-D. Shared Services Used by Inventory Components

| Service | Method used by Inventory | Purpose |
|---|---|---|
| `mastermodule.service.ts` | `getUserAccessRights(user, screenName)` | Per-screen CRUD permission check (every inventory component calls this) |
| `mastermodule.service.ts` | `getSuppliers(category)` | Supplier dropdown in PO form (routes via Finance controller) |
| `mastermodule.service.ts` | `getInventoryCategories(cat)` | Category dropdown (also used by Finance) |
| `auth.guard.ts` | `canActivate` | All inventory routes are guarded |
| `auth.interceptor.ts` | JWT Bearer token injection | Every API call |
| `datasharing.service.ts` | `getUsername()` | Fallback username resolution |

---

## 7. Missing Functionality and Implementation Risks

### 7-A. Known Active Server Issues (Verified from ReportParamLog.txt)

| Issue | Evidence | Impact | Fix |
|---|---|---|---|
| Migration 17 not run on server | `ERROR: Could not find stored procedure 'sp_InventoryStockLedger'` (2026-10-07) | StockLedgerReport.aspx fails for all users | Run migration 17 on OBMSUATNEW |
| Migration 21 not run | `ERROR: Could not find stored procedure 'sp_Report_StockIssue'` (2026-10-10) | StockIssueReport.aspx fails | Run migration 21 |
| Migration 22 not run | `ERROR: Could not find stored procedure 'sp_Report_StockIssueReturn'` (2026-10-10) | IssueReturnReport.aspx fails | Run migration 22 |
| Migration 23 not run | `ERROR: Could not find stored procedure 'sp_Report_StockTransfer'` (2026-10-10) | StockTransferReport.aspx fails | Run migration 23 |
| BouncyCastle.Crypto.dll missing from server bin/ | `ERROR: Could not load assembly 'BouncyCastle.Crypto, Version=1.8.9.0'` (2026-10-07) | All iTextSharp PDF reports fail | Copy BouncyCastle.Crypto.dll to server UatReports\bin\ |

### 7-B. Code Quality / Design Risks

| Risk | Location | Severity |
|---|---|---|
| `AssetMaster.PurchaseAmount` is typed `double?` in the C# domain model | `Models/Domain/AssetMaster.cs` | Medium — `double` is imprecise for financial values; should be `decimal?` |
| Duplicate DbSet registration | `OBMSDbContext.cs` — both `DbSet<Supplier> suppliers` and `DbSet<Supplier> Suppliers` registered | Low — EF Core may tolerate it but it is confusing and may cause issues in queries |
| `StockAdjustment` is a single-item-per-document table | Migration 04 | Medium — If multi-item adjustments are needed later, this requires a schema change |
| `BranchStockLedgerReport.aspx` and `HQStockLedger.aspx` are Crystal Reports inside the Inventory folder | `UatReports/Inventory/` | Low — these two deviate from the iTextSharp pattern used by all other inventory reports |
| `ItemMaster.Quantity` and `ItemMaster.SellPrice` exist on the DB table but are intentionally excluded from the DTO and UI | `Models/Domain/ItemMaster.cs` | Low — legacy columns; could confuse future developers |
| `environment.prod.ts` has UAT URLs commented out | `environments/environment.prod.ts` | Medium — deploying to UAT requires manual code change to swap URLs |
| `TAX` constant exported from `environment.ts` is imported nowhere | `environments/environment.ts` | Low — dead code, but indicates prior intent to use tax rates client-side |

### 7-C. Missing Features (Not Implemented)

| Feature | Status |
|---|---|
| Purchase Order multi-GRN reporting | The GRNReport shows one GRN. There is no consolidated "all GRNs for this PO" report |
| StockAdjustment PDF report linked from adjustment approval screen | ⚠ NOT VERIFIED — the report ASPX exists but whether the Angular component has the print button is not confirmed |
| WarehouseLocation / bin-location sub-level | NOT FOUND — WarehouseMaster has no Location child table. Location is tracked on AssetMaster only |
| Supplier price list / price history | NOT FOUND — UnitPrice on PO is entered manually each time; no price history table |
| Purchase order amendment / version history | NOT FOUND — PO edit is blocked once submitted; no amendment log |
| Barcode / QR scanning for stock issue | NOT FOUND |
| Inventory valuation (FIFO / AVCO / LIFO) | NOT FOUND — StockLedger stores UnitCost per movement but there is no cost layer calculation. Current stock value = `SUM(InQty × UnitCost) - SUM(OutQty × UnitCost)` is available from the ledger but no API endpoint or report calculates it |

---

## 8. Files Likely to Require Changes for Inventory-Only SST

This section identifies the exact files that would need to change to add SST (Sales and Service Tax) to the Inventory purchase workflow (specifically to PO → GRN → CreditorInvoice). It is forward-looking and not yet implemented.

**Scope assumption:** SST on supplier purchases (input tax) only — i.e., tax on items bought via PO/GRN. Not on stock issues, transfers, or adjustments.

---

### 8-A. Database — New Migration Required

A new migration file (e.g. `29.InventorySST_Migration.sql`) would be needed to add:

| Table | Column to add | Type | Notes |
|---|---|---|---|
| `PurchaseOrderDetail` | `SSTPercent` | DECIMAL(5,2) NULL DEFAULT 0 | SST % per line (0, 6, or 8) |
| `PurchaseOrderDetail` | `SSTAmount` | DECIMAL(18,4) NULL DEFAULT 0 | Computed = `(OrderedQty × UnitPrice) × SSTPercent / 100` |
| `PurchaseOrder` | `SSTTotal` | DECIMAL(18,4) NULL DEFAULT 0 | Sum of all line SSTAmount |
| `PurchaseOrder` | `GrandTotal` | DECIMAL(18,4) NULL DEFAULT 0 | `TotalAmount + SSTTotal` |
| `GoodsReceiptNoteDetail` | `SSTPercent` | DECIMAL(5,2) NULL DEFAULT 0 | Carried from PO line |
| `GoodsReceiptNoteDetail` | `SSTAmount` | DECIMAL(18,4) NULL DEFAULT 0 | `ReceivedQty × UnitCost × SSTPercent / 100` |
| `GoodsReceiptNote` | `SSTTotal` | DECIMAL(18,4) NULL DEFAULT 0 | Sum of GRN line SST |
| `GoodsReceiptNote` | `GrandTotal` | DECIMAL(18,4) NULL DEFAULT 0 | `Σ(ReceivedQty × UnitCost) + SSTTotal` |

The CreditorInvoice `Total` field written by `SaveGRN` would then need to include SST in the total.

---

### 8-B. API — Files to Change

| File | Change required |
|---|---|
| `Models/Domain/PurchaseOrderDetail.cs` | Add `decimal? SSTPercent`, `decimal? SSTAmount` |
| `Models/Domain/PurchaseOrder.cs` | Add `decimal? SSTTotal`, `decimal? GrandTotal` |
| `Models/Domain/GoodsReceiptNoteDetail.cs` | Add `decimal? SSTPercent`, `decimal? SSTAmount` |
| `Models/Domain/GoodsReceiptNote.cs` | Add `decimal? SSTTotal`, `decimal? GrandTotal` |
| `Models/DTO/PORequestDto.cs` | Add SST fields to detail DTO |
| `Models/DTO/POListDto.cs` / `PODetailListDto.cs` | Add SST fields for read display |
| `Models/DTO/GRNRequestDto.cs` | Add `SSTPercent` on detail lines (carries from PO) |
| `Models/DTO/GRNDetailListDto.cs` | Add SST fields for read display |
| `Repositories/Implementation/InventoryRepository.cs` — `SavePO()` | Compute `SSTAmount = (OrderedQty × UnitPrice) × SSTPercent / 100` per line; update `PurchaseOrder.SSTTotal` and `GrandTotal` |
| `Repositories/Implementation/InventoryRepository.cs` — `SaveGRN()` | Carry `SSTPercent` from PO detail → GRN detail; compute `GRN.SSTTotal`; update `CreditorInvoice.Total` to include SST |

---

### 8-C. Angular UI — Files to Change

| File | Change required |
|---|---|
| `modules/inventory/purchase-order/new-po/new-po.component.ts` | Add `SSTPercent` to `lineForm`; compute `SSTAmount` and `SSTTotal` |
| `modules/inventory/purchase-order/new-po/new-po.component.html` | Add SST % column, SST Amount column, and SST Total / Grand Total in footer |
| `modules/inventory/goods-receipt/new-grn/new-grn.component.ts` | Display `SSTPercent` per line (read-only, from PO); display `SSTTotal` and `GrandTotal` in view mode |
| `modules/inventory/goods-receipt/new-grn/new-grn.component.html` | Add SST columns in view table and footer |
| `service/inventory.service.ts` | No endpoint changes needed — just payload structure changes |

---

### 8-D. Reports — Files to Change

| File | Change required |
|---|---|
| `UatReports-new/UatReports/Inventory/POReport.aspx` | Add SST % column, SST Amount column, SST Total row, Grand Total row to PDF |
| `UatReports-new/UatReports/Inventory/GRNReport.aspx` | Add SST Amount column, SST Total row, Grand Total row to PDF |
| `Database/[migration for sp_Report_PO]` | Update `sp_Report_PO` to return `SSTPercent`, `SSTAmount`, `SSTTotal`, `GrandTotal` |
| `Database/[migration for sp_Report_GRN]` | Update `sp_Report_GRN` to return `SSTPercent`, `SSTAmount`, `SSTTotal`, `GrandTotal` |

---

### 8-E. Files That Do NOT Need to Change for Inventory SST

The following inventory components, tables, and reports are downstream of GRN and deal only in quantities and UnitCost — SST on supplier purchases does not flow into them:

- `StockLedger` table and `sp_InventoryStockLedger` — stock movements track qty and UnitCost (pre-SST cost), not tax
- `StockIssueReport.aspx`, `StockTransferReport.aspx`, `StockAdjustmentReport.aspx`, `IssueReturnReport.aspx`, `StockLedgerReport.aspx`
- `StockIssueDoc / StockIssueDocDetail` — no price context
- `StockTransfer / StockTransferDetail` — no price context
- `StockAdjustment` — uses UnitCost (pre-tax)
- `PRReport.aspx`, `BSRReport.aspx` — these are internal requests, no external tax
- All Asset reports
- `ApprovalConfiguration / ApprovalHistory / AppNotification` — no financial data
- Finance module files — Finance already has its own TaxAmount on ClientInvoice

---

### 8-F. Risk Register for Inventory SST Implementation

| Risk | Severity | Mitigation |
|---|---|---|
| `SaveGRN()` creates a `CreditorInvoice` row — if `Total` changes to include SST, Finance Payment screens will show higher amounts. Finance team must agree. | **High** | Coordinate with Finance before changing `CreditorInvoice.Total` formula |
| `CheckGRNDependency()` blocks GRN deletion if a payment exists. If a GRN was created pre-SST and a payment posted, retroactively adding SST would be impossible. | High | Ensure migration applies only to NEW GRNs; existing data is not touched |
| `PurchaseOrderDetail.TotalPrice` is stored (not computed). Changing the formula to include SST changes the stored value — audit trail gap. | Medium | Keep `TotalPrice` = `OrderedQty × UnitPrice` (ex-SST); add separate `SSTAmount` column |
| `CreditorInvoice` is a Finance-owned table. Inventory writes to it in `SaveGRN()`. Adding an SST field to `CreditorInvoice` requires a Finance migration and may affect Finance reports. | Medium | Add `SSTAmount` column to `CreditorInvoice`; Finance reports opt-in to display it |
| The `TAX` constant in `environment.ts` defines date ranges for 6% and 8% SST. The correct rate depends on the GRN date. The Angular PO form would need to auto-calculate the applicable rate based on date. | Medium | Use `TAX.SSTStart8` (2024-03-01) as the 8% threshold; earlier dates use 6% |
| `sp_Report_PO` and `sp_Report_GRN` are defined in migration files. Updating them requires `CREATE OR ALTER PROCEDURE` in a new migration. The old SP will remain if the migration is not run on the server. | Low | Include both `DROP IF EXISTS` + `CREATE` in the migration for safety |
| `AssetMaster.PurchaseAmount` is `double?` not `decimal?`. If SST is added to asset purchases, precision issues could arise. | Low | Fix the C# domain model type before adding SST to asset purchases |

---

*End of OBMS Inventory Analysis document.*
*Generated from source file inspection — no code was modified.*
