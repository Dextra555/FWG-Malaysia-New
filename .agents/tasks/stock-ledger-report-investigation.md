# Stock Ledger Report — End-to-End Investigation

**Project:** FWG-Malaysia-New (OBMS)  
**Date:** Investigation performed on current codebase  
**Scope:** Angular UI → .NET API → Database, full flow

---

## Summary Answer

The "Report / Stock Ledger" feature is **fully implemented** end-to-end across three tiers:

| Tier | Component | Status |
|------|-----------|--------|
| Angular UI | `StockLedgerReportComponent` at route `inventory/report/stock-ledger-report` | ✅ Exists |
| Report Server | `StockLedgerReport.aspx` (iTextSharp PDF + Excel export) | ✅ Exists |
| .NET JSON API | `GET api/Inventory/GetStockLedger` (used by operational screen, not the report) | ✅ Exists |
| Database View | `VWStockLedger` | ✅ Defined in migration |
| Stored Procedure | `sp_InventoryStockLedger` | ✅ Defined in migration |
| Crystal Report file | `StockLedger.rpt` | ✅ File exists (28 KB) but **NOT used** by `StockLedgerReport.aspx` |
| DB Migration | `17.StockLedgerReport_Migration.sql` | ✅ Exists |

**Key architectural distinction:** The Report page does **not** call the JSON REST API. It builds a direct URL to the ASPX report server (`baseReportUrl`), which runs `sp_InventoryStockLedger` → reads `VWStockLedger` → generates PDF or Excel using iTextSharp inline in the ASPX page. The JSON API endpoint (`GetStockLedger`) is used only by the operational Stock Ledger **data entry screen**, not the report.

---

## Step-by-Step Flow

### Step 1 — User Opens the Page

**URL:** `http://<app>/inventory/report/stock-ledger-report`

**Angular Route** (defined in `admin-layout-routing.module.ts` line 411):
```typescript
{ path: 'inventory/report/stock-ledger-report', component: StockLedgerReportComponent, canActivate: [AuthGuard] }
```
File: `OBMSAPP-UI\angularapp\src\app\layout\admin-layout\admin-layout-routing.module.ts`

**Component registered in:**
`OBMSAPP-UI\angularapp\src\app\modules\shared.module.ts` (line 187, 411)

---

### Step 2 — Component Initialises

**Component:** `StockLedgerReportComponent`  
**File:** `OBMSAPP-UI\angularapp\src\app\modules\report\inventory\stock-ledger-report\stock-ledger-report.component.ts`  
**Selector:** `app-stock-ledger-report`  
**Template:** `stock-ledger-report.component.html`

On `ngOnInit()`:
1. Reads `username` from `sessionStorage`
2. Calls `getUserAccessRights(username, 'Stock Ledger Report')` via `MastermoduleService`
   - Screen name **must** match the `ScreenList` table entry (registered by migration `17.StockLedgerReport_Migration.sql`)
3. If access granted, loads:
   - `service.getWarehouseMasters()` → `GET api/Inventory/GetWarehouseMasters`
   - `service.GetReportHQMaster()` → `GET api/Inventory/GetReportHQMaster` (item dropdown)

**Form fields:**
- `WarehouseID` (required dropdown)
- `ItemID` (optional, default = 0 = All Items)
- `TransactionType` (optional, default = ALL)
- `StartDate` (required, defaults to 1st of current month)
- `EndDate` (required, defaults to today)

---

### Step 3 — User Clicks "Search" (PDF)

`onSubmit()` in the component builds a URL and sets `urlSafe` for the embedded `<iframe>`:

```typescript
this.url = environment.baseReportUrl + 'Inventory/StockLedgerReport.aspx?';
this.url += 'LoginID=' + this.currentUser;
// Then appends:
localURL += '&StartDate='       + this.returnDate(raw.StartDate);
localURL += '&EndDate='         + this.returnDate(raw.EndDate);
localURL += '&WarehouseID='     + (raw.WarehouseID || '0');
localURL += '&ItemID='          + (raw.ItemID || '0');
localURL += '&TransactionType=' + (raw.TransactionType || 'ALL');
```

**Example URL:**
```
http://localhost:8080/Inventory/StockLedgerReport.aspx?LoginID=admin&StartDate=2026-01-01&EndDate=2026-01-31&WarehouseID=1&ItemID=0&TransactionType=ALL
```

The PDF renders **inline in an `<iframe>`** in the page.

---

### Step 4 — User Clicks "Export Excel"

`onExportExcel()` builds the same URL but adds `&format=excel`, then calls `window.open(...)` in a new tab — triggering a file download.

```typescript
const baseUrl = environment.baseReportUrl + 'Inventory/StockLedgerReport.aspx';
params.set('format', 'excel');
window.open(baseUrl + '?' + params.toString(), '_blank');
```

---

### Step 5 — ASPX Report Server Processes the Request

**File:** `UatReports-new\UatReports\Inventory\StockLedgerReport.aspx`  
(No separate `.aspx.cs` code-behind — all logic is **inline** `<script runat="server">`)

**`Page_Load()` flow:**
1. Reads and validates query-string params: `LoginID`, `StartDate`, `EndDate`, `WarehouseID`, `ItemID`, `TransactionType`
2. Validates `warehouseID > 0`, valid dates, `startDate <= endDate`
3. Calls `RunSP()` → executes stored procedure `sp_InventoryStockLedger` via ADO.NET
4. Calls `Lookup()` to resolve warehouse name and item name for report header
5. Based on `?format=`:
   - `pdf` (default) → calls `BuildPdf()` using **iTextSharp 5.5.13.x** → streams `application/pdf` inline
   - `excel` → calls `BuildExcel()` (HTML table with Excel MIME type) → streams `.xls` download

**DB Connection string** assembled from `Web.config` appSettings keys: `Server`, `Database`, `UserID`, `Password`

---

### Step 6 — Stored Procedure Executes

**Procedure:** `sp_InventoryStockLedger`  
**Defined in:** `Database\17.StockLedgerReport_Migration.sql` (Section 3)

```sql
CREATE PROCEDURE sp_InventoryStockLedger
    @WarehouseID     INT            = 0,
    @ItemID          INT            = 0,
    @TransactionType NVARCHAR(50)   = 'ALL',
    @FromDate        DATETIME       = NULL,
    @ToDate          DATETIME       = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- ToDate extended to 23:59:59 for full-day inclusion
    SET @ToDate = DATEADD(SECOND, 86399, CAST(@ToDate AS DATETIME));

    SELECT v.ID, v.TransactionNo, v.TransactionDate, v.TransactionType,
           v.TransactionTypeLabel, v.WarehouseID, v.WarehouseName,
           v.ItemID, v.ItemCode, v.ItemName, v.CategoryName, v.BrandName,
           v.UOMName, v.InQty, v.OutQty, v.RunningBalance,
           v.UnitCost, v.ReferenceType, v.ReferenceNo, v.Remarks
    FROM VWStockLedger v
    WHERE v.TransactionDate BETWEEN @FromDate AND @ToDate
      AND (@WarehouseID = 0 OR v.WarehouseID = @WarehouseID)
      AND (@ItemID = 0 OR v.ItemID = @ItemID)
      AND (@TransactionType = 'ALL' OR v.TransactionType = @TransactionType)
    ORDER BY v.ItemName ASC, v.TransactionDate ASC, v.ID ASC;
END
```

---

### Step 7 — Database View Provides Data

**View:** `VWStockLedger`  
**Defined in:** `Database\17.StockLedgerReport_Migration.sql` (Section 1)

Joins:
- `StockLedger` (main table — INNER JOIN)
- `WarehouseMaster` (INNER JOIN on `WarehouseID`)
- `ItemMaster` (INNER JOIN on `ItemID`)
- `InventoryCategory` (LEFT JOIN on `im.CategoryID`)
- `BrandMaster` (LEFT JOIN on `im.BrandID`)
- `UOMMaster` (LEFT JOIN on `im.UOMID`)

**RunningBalance** is a SQL window function:
```sql
SUM(sl.InQty - sl.OutQty) OVER (
    PARTITION BY sl.WarehouseID, sl.ItemID
    ORDER BY sl.TransactionDate ASC, sl.ID ASC
    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
) AS RunningBalance
```

---

## File Evidence Summary

### Angular UI Files

| File | Purpose |
|------|---------|
| `src/app/layout/admin-layout/admin-layout-routing.module.ts` line 411 | Route definition |
| `src/app/modules/shared.module.ts` line 187, 411 | Component registration |
| `src/app/modules/report/inventory/stock-ledger-report/stock-ledger-report.component.ts` | Component logic |
| `src/app/modules/report/inventory/stock-ledger-report/stock-ledger-report.component.html` | Template (form + iframe) |
| `src/app/service/inventory.service.ts` line 415–430 | `getStockLedger()` (for operational screen, not report) |
| `src/environments/environment.ts` | `baseReportUrl = 'http://localhost:8080/'` (dev), `baseUrl = 'http://localhost:5002/api/'` |

### .NET API Files

| File | Purpose |
|------|---------|
| `OBMSAPP-API/OBMS.WebAPI/Controllers/InventoryController.cs` line 1712 | `[HttpGet("GetStockLedger")]` endpoint |
| `OBMSAPP-API/OBMS.WebAPI/Repositories/Interface/IInventoryRepository.cs` line 116 | Interface contract |
| `OBMSAPP-API/OBMS.WebAPI/Repositories/Implementation/InventoryRepository.cs` line 1125 | LINQ query + running balance |
| `OBMSAPP-API/OBMS.WebAPI/Models/Domain/StockLedger.cs` | EF entity → `[Table("StockLedger")]` |
| `OBMSAPP-API/OBMS.WebAPI/Models/DTO/StockLedgerFilterDto.cs` | `StockLedgerRowDto` response DTO |

### Report Server Files

| File | Purpose |
|------|---------|
| `UatReports-new/UatReports/Inventory/StockLedgerReport.aspx` | Main report: inline C#, iTextSharp PDF + HTML Excel |
| `UatReports-new/UatReports/Inventory/StockLedger.rpt` | Crystal Report file (28 KB) — **present but NOT used** by `StockLedgerReport.aspx` |
| `UatReports-new/UatReports/Inventory/HQStockLedger.aspx` + `.aspx.cs` | HQ variant — uses Crystal Reports (`HQStockLedger.rpt`) |
| `UatReports-new/UatReports/Inventory/BranchStockLedgerReport.aspx` + `.aspx.cs` | Branch variant — uses Crystal Reports (`BranchStockLedger.rpt`) |

### Database Files

| File | Purpose |
|------|---------|
| `Database/17.StockLedgerReport_Migration.sql` | Creates `VWStockLedger` view, `sp_InventoryStockLedger` SP, and `ScreenList` entries |
| `Database/02.OpeningStock_StockLedger_Migration.sql` | Creates the `StockLedger` table itself |

---

## Architecture Notes

### Two Separate Stock Ledger Systems

There are **three** Stock Ledger reports in this project:

| Report | ASPX File | Report Engine | Crystal .rpt |
|--------|-----------|---------------|--------------|
| Stock Ledger Report (HQ, all warehouses) | `StockLedgerReport.aspx` | iTextSharp (inline) | `StockLedger.rpt` exists but NOT linked |
| HQ Stock Ledger | `HQStockLedger.aspx` | Crystal Reports | `HQStockLedger.rpt` |
| Branch Stock Ledger | `BranchStockLedgerReport.aspx` | Crystal Reports | `BranchStockLedger.rpt` |

The `StockLedgerReport.aspx` was built newer (Part 9 migration) and uses iTextSharp instead of Crystal Reports. This is a deliberate architectural choice to avoid Crystal Reports runtime dependency for new reports.

### JSON API vs Report Server

The JSON endpoint `GET api/Inventory/GetStockLedger` (at `http://localhost:5002/api/`) and the ASPX report server (at `http://localhost:8080/`) are **two separate servers**:
- JSON API: used by the operational `StockLedgerComponent` (the data-entry/viewing screen under inventory)
- ASPX Report Server: used by `StockLedgerReportComponent` (the report under Reports menu)

Both read from the same `StockLedger` table but via different paths.

---

## Conclusions and Recommendations

### ✅ What's Working Correctly

1. The Angular component, ASPX page, stored procedure, and database view are all consistently implemented.
2. RunningBalance logic in `VWStockLedger` (SQL window function) matches the C# accumulator in `InventoryRepository.GetStockLedger` exactly.
3. Transaction type labels are consistent across all three layers (Angular, C# dictionary, SQL CASE).
4. Migration `17.StockLedgerReport_Migration.sql` correctly registers `'Stock Ledger Report'` in `ScreenList` with the exact name the component passes to `getUserAccessRights`.

### ⚠️ Issues / Gaps to Address

**1. `StockLedger.rpt` is orphaned**  
`UatReports-new\UatReports\Inventory\StockLedger.rpt` (28 KB) exists but no ASPX page references it. `StockLedgerReport.aspx` uses iTextSharp directly. The `.rpt` file is either a leftover from an earlier Crystal Reports prototype or was never wired up. It should either be linked to the ASPX or deleted to avoid confusion.

**Recommendation:** Either delete `StockLedger.rpt` if the iTextSharp approach is final, or document clearly why it exists.

**2. RunningBalance scope in `VWStockLedger`**  
The `RunningBalance` in the view is computed over **ALL historical data** (not just the filtered date range). This means when the SP filters by `@FromDate`, the running balance shown in the report is the true cumulative balance at each row — not a balance that resets to 0 at the start of the filtered period. This is correct for a stock ledger, but can be surprising if users expect to see balances starting from 0 on the first day of their filter. The behaviour should be documented in the UI (e.g., a tooltip: "Balance shown is cumulative since the first transaction").

**3. Migration must be run before the report works**  
`17.StockLedgerReport_Migration.sql` must be executed to create `VWStockLedger`, `sp_InventoryStockLedger`, and the `ScreenList` entry. Without this migration:
- The report ASPX will error with "sp_InventoryStockLedger not found"
- The component will show "You do not have permissions" (no `ScreenList` entry)

**Recommendation:** Confirm this migration has been run against the target database.

**4. `baseReportUrl` environment config**  
`environment.ts` has `baseReportUrl: 'http://localhost:8080/'` for dev. The production URL (`http://124.217.236.37:8229/`) is commented out. Ensure `environment.prod.ts` has the correct production URL.

**5. iTextSharp dependency in report server**  
`StockLedgerReport.aspx` requires iTextSharp 5.5.13.x in the `bin/` folder of the report server project. If the DLL is missing, `BuildPdf()` will throw. Verify the NuGet package is restored in `UatReports-new`.

---

## Complete Flow Diagram

```
User browser
    │
    ▼
Angular StockLedgerReportComponent  (route: /inventory/report/stock-ledger-report)
    │
    ├─[ngOnInit]─► GET http://localhost:5002/api/Inventory/GetWarehouseMasters
    │              GET http://localhost:5002/api/Inventory/GetReportHQMaster
    │              (populates Warehouse and Item dropdowns)
    │
    ├─[onSubmit]─► builds URL → sets <iframe src="...">
    │              http://localhost:8080/Inventory/StockLedgerReport.aspx
    │                  ?LoginID=admin
    │                  &StartDate=2026-01-01
    │                  &EndDate=2026-01-31
    │                  &WarehouseID=1
    │                  &ItemID=0
    │                  &TransactionType=ALL
    │
    └─[onExportExcel]─► window.open(same URL + &format=excel)

ASPX Report Server (http://localhost:8080/)
    │
    ▼
StockLedgerReport.aspx  Page_Load()
    │
    ├─ Validate params (WarehouseID, dates, etc.)
    │
    ├─ RunSP() → ADO.NET SqlCommand
    │       EXEC sp_InventoryStockLedger
    │           @WarehouseID=1, @ItemID=0,
    │           @TransactionType='ALL',
    │           @FromDate='2026-01-01', @ToDate='2026-01-31'
    │
    ├─ Lookup() → SELECT Name FROM WarehouseMaster WHERE ID=@p
    │             SELECT Name FROM ItemMaster WHERE ID=@p
    │
    └─ format=pdf  → BuildPdf() (iTextSharp) → Response.BinaryWrite(pdf)
       format=excel → BuildExcel() (HTML table) → Response.BinaryWrite(xls)

Database (SQL Server — OBMSUATNEW)
    │
    ├─ sp_InventoryStockLedger
    │       └─► SELECT FROM VWStockLedger
    │                   WHERE date range + warehouse + item + txn type
    │                   ORDER BY ItemName, TransactionDate, ID
    │
    └─ VWStockLedger (view)
            ├─ StockLedger      (INNER JOIN — core ledger table)
            ├─ WarehouseMaster  (INNER JOIN)
            ├─ ItemMaster       (INNER JOIN)
            ├─ InventoryCategory (LEFT JOIN)
            ├─ BrandMaster      (LEFT JOIN)
            └─ UOMMaster        (LEFT JOIN)
               RunningBalance = SUM(InQty-OutQty) OVER (PARTITION BY WarehouseID,ItemID ORDER BY TransactionDate,ID)
```
