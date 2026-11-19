# OBMS Inventory Dashboard — Analysis Document

## 1. Files Inspected

### Angular UI
| File | Key Finding |
|------|------------|
| `admin-layout-routing.module.ts` | Flat route table, all 100+ routes use `canActivate: [AuthGuard]` |
| `sidebar.component.html / .ts` | Menu sections, `toggleSubmenu` driven by string comparison |
| `header.component.html / .ts` | Inventory click → `toggleSubmenu('inventory')` → navigates `/dashboard` |
| `dashboard.component.ts / .html` | Current dashboard is a welcome page + conditional Finance iframe |
| `auth.guard.ts` | Pure token-presence check — no role/permission at route level |
| `auth.service.ts` | JWT in sessionStorage; 10-min inactivity idle → logout on next action |
| `inventory.service.ts` | All 80+ inventory API methods catalogued |
| `mastermodule.service.ts` | `getUserAccessRights(userName, screenName)` → `GET register/GetUserAccessRights` |
| `environment.ts / environment.prod.ts` | `baseUrl`, `baseReportUrl` confirmed |
| `package.json` | `ng-apexcharts`, `ng2-charts`, `chart.js` all installed — any can be used |

### .NET API
| File | Key Finding |
|------|------------|
| `InventoryController.cs` | 140+ endpoints; NO `[Authorize]`; calls `_inventoryRepository` methods |
| `IInventoryRepository.cs` | 80+ method signatures; write ops return `(bool Success, string Message)` tuples |
| `OBMSDbContext.cs` | All DbSets registered; EF Core primary access pattern |
| `Program.cs` | JWT bearer, CORS with explicit origins, DI registrations |
| `RegisterController.cs` | Login → JWT with `ClaimTypes.Name` + `"IsAdmin"` claim; `GetUserAccessRights` endpoint |

### SQL Database
| File | Key Finding |
|------|------------|
| `02.OpeningStock_StockLedger_Migration.sql` | `StockLedger` is the central table; `SUM(InQty) - SUM(OutQty)` = balance |
| `04.StockOperations_Part4_Migration.sql` | All 8 stock operation tables defined |
| `17.StockLedgerReport_Migration.sql` | `VWStockLedger` view + `sp_InventoryStockLedger` SP |
| `16.StockAdjustment_ApprovalWorkflow_Migration.sql` | SA gets ledger row only on Approve, not on Save |

---

## 2. Key Architecture Decisions

### Permission System
- `OBMSPermissions` table: `(Name, ScreenName, Read, Create, Update, Delete)`
- Screen name registered in `ScreenList` table; category = `'Inventory'`
- Angular calls `getUserAccessRights(username, 'Inventory Dashboard')` on component init
- Superadmin check: `this.currentUser === 'superadmin'` OR `this.userRole === 'superadmin'`
- New screen name: **`'Inventory Dashboard'`** — must be inserted into `ScreenList`

### Navigation Behaviour
- `header.component.ts` `toggleSubmenu('inventory')` → calls `_dataService.setMenuName('inventory')` then navigates to `/dashboard`
- Currently all menus navigate to `/dashboard` except `master` which goes to employee-expiry-reminder
- **Plan**: Add `inventory/dashboard` route and update `toggleSubmenu` case to navigate there instead; keep `/dashboard` for non-inventory menus

### No Lazy Loading
- All components are in `SharedModule` → declared in `AdminLayoutModule`
- New dashboard component must be declared in `SharedModule` and imported in `admin-layout-routing.module.ts`

---

## 3. Existing SPs and APIs to Reuse

### Directly Reusable via API
| Endpoint | What it returns | Dashboard use |
|----------|----------------|---------------|
| `GET Inventory/GetInventoryItems` | List of all items with IsActive | Total Items KPI |
| `GET Inventory/GetStockBalanceSummary` | Per-item, per-warehouse balance + reorder flag | Low Stock Items, Total Qty, Warehouse values |
| `GET Inventory/GetStockLedger` | Filtered ledger rows with InQty/OutQty | Inward/Outward chart |
| `GET Inventory/GetPRList` | All PR docs with Status | Pending Issues KPI |
| `GET Inventory/GetStockTransferList` | All transfer docs | Pending Transfers KPI |
| `GET Inventory/GetWarehouseMasters` | Warehouses | Warehouse filter dropdown |

### Reusable Existing SPs (SQL)
| SP | What it does |
|----|-------------|
| `sp_InventoryStockLedger` | Filtered stock ledger — reuse parameter pattern |
| `VWStockLedger` | Stock ledger view with running balance |

---

## 4. New SP Required

A single new SP `sp_InventoryDashboard` will return all dashboard data in one call, to avoid 6+ round-trips from Angular:

```sql
-- Result Set 1: KPI Cards
SELECT
  TotalItems, TotalStockValue, TotalStockQty,
  PendingTransfers, PendingIssues, LowStockItems

-- Result Set 2: Warehouse Stock Value (Donut chart)
SELECT WarehouseName, StockValue, StockQty

-- Result Set 3: Inward vs Outward This Month (Bar chart, by date)
SELECT DateLabel, InwardQty, OutwardQty

-- Result Set 4: Stock Summary per Warehouse (Table)
SELECT WarehouseName, OpeningQty, InwardQty, OutwardQty, ClosingQty, StockValue
```

**KPI calculation rules:**
- **Total Items**: `COUNT(*) FROM ItemMaster WHERE IsActive = 1` (or equivalent, depending on pre-existing schema)
- **Total Stock Value**: `SUM(InQty - OutQty) * UnitCost` using last known unit cost per (WarehouseID, ItemID) from StockLedger
- **Total Stock Qty**: `SUM(InQty - OutQty)` from StockLedger GROUP BY ItemID (across all warehouses, no double-counting: transfer IN + OUT cancel out globally)
- **Pending Transfers**: `COUNT(*) FROM StockTransfer WHERE IsActive = 1` (no status column on StockTransfer — all active = "pending" after creation in this schema)
- **Pending Issues**: `COUNT(*) FROM StockIssueDoc WHERE IsActive = 1` (stock issues are posted directly — count recent ones this month)
- **Low Stock Items**: Items where current balance ≤ `ReorderLevel` on ItemMaster (ReorderLevel IS NOT NULL)

**Transfer double-count avoidance:**
- Global total qty uses `SUM(InQty - OutQty)` which auto-cancels TRANSFER_IN/TRANSFER_OUT since each transfer writes one of each. No special handling needed.
- Warehouse-level donut: each warehouse gets its own `SUM(InQty - OutQty)` — correct.

---

## 5. New .NET Endpoint Required

```csharp
// InventoryController.cs
[HttpGet("GetInventoryDashboard")]
public async Task<IActionResult> GetInventoryDashboard()

// IInventoryRepository.cs
Task<InventoryDashboardDto> GetInventoryDashboard();

// InventoryRepository.cs — calls sp_InventoryDashboard
```

**Response DTO: `InventoryDashboardDto`**
```csharp
public class InventoryDashboardDto {
    public int TotalItems { get; set; }
    public decimal TotalStockValue { get; set; }
    public decimal TotalStockQty { get; set; }
    public int PendingTransfers { get; set; }
    public int PendingIssues { get; set; }
    public int LowStockItems { get; set; }
    public List<WarehouseStockDto> WarehouseStocks { get; set; }
    public List<StockMovementDto> MonthlyMovements { get; set; }
    public List<WarehouseSummaryDto> WarehouseSummaries { get; set; }
}
```

---

## 6. Navigation and Permission Integration Plan

### Route Change
- Add `inventory/dashboard` route in `admin-layout-routing.module.ts`
- Update `header.component.ts` `toggleSubmenu('inventory')` case to navigate to `/inventory/dashboard`
- Keep `*` wildcard and other routes unchanged

### Sidebar Change
- Add "Dashboard" as the **first item** in the inventory submenu in `sidebar.component.html`, with `routerLink="/inventory/dashboard"`

### Permission Registration
- Migration SQL inserts `'Inventory Dashboard'` into `ScreenList` with `Category = 'Inventory'`
- Component calls `getUserAccessRights(username, 'Inventory Dashboard')` on init
- Admins/Superadmin bypass the read-access gate as per all other components

---

## 7. Files Expected to Change

| Layer | File | Change |
|-------|------|--------|
| SQL | `Database/29.InventoryDashboard_Migration.sql` | NEW — SP + ScreenList entry |
| API | `OBMSAPP-API/OBMS.WebAPI/Models/DTO/InventoryDashboardDto.cs` | NEW — response DTOs |
| API | `OBMSAPP-API/OBMS.WebAPI/Repositories/Interface/IInventoryRepository.cs` | ADD method signature |
| API | `OBMSAPP-API/OBMS.WebAPI/Repositories/Implementation/InventoryRepository.cs` | ADD implementation |
| API | `OBMSAPP-API/OBMS.WebAPI/Controllers/InventoryController.cs` | ADD endpoint |
| Angular | `src/app/modules/inventory/inventory-dashboard/` | NEW component (4 files) |
| Angular | `src/app/layout/admin-layout/admin-layout-routing.module.ts` | ADD route |
| Angular | `src/app/components/header/header.component.ts` | UPDATE `toggleSubmenu('inventory')` |
| Angular | `src/app/components/sidebar/sidebar.component.html` | ADD dashboard menu item |
| Angular | `src/app/modules/shared.module.ts` | DECLARE new component |

---

## 8. KPI Definitions and Data Sources

| KPI | Source Table | Formula | Currency |
|-----|-------------|---------|---------|
| Total Items | `ItemMaster` | `COUNT(*)` where `IsActive=1` | Count |
| Total Stock Value | `StockLedger` + `ItemMaster` | `SUM((InQty-OutQty) * LastUnitCost)` per warehouse+item | RM |
| Total Stock Quantity | `StockLedger` | `SUM(InQty-OutQty)` across all warehouses (transfers self-cancel) | Units |
| Pending Transfers | `StockTransfer` | `COUNT(*)` where `IsActive=1`, current month | Count |
| Pending Issues | `StockIssueDoc` | `COUNT(*)` where `IsActive=1`, current month | Count |
| Low Stock Items | `StockLedger` + `ItemMaster` | Items where balance ≤ `ReorderLevel` (non-null) | Count |

---

## 9. Test Cases

1. Total Items count matches `SELECT COUNT(*) FROM ItemMaster WHERE IsActive=1`
2. Total Stock Value = manually sum `(InQty-OutQty) * UnitCost` from StockLedger for one item and compare
3. Transfers between warehouses A→B show in each warehouse's donut but cancel at the global total qty
4. Donut chart percentages sum to 100%
5. Inward/Outward bars — verify a known GRN's qty appears in the correct date bucket
6. Low stock count matches items where `getStockBalanceSummary` returns `AtReorderLevel=true`
7. Superadmin always sees the dashboard regardless of permission settings
8. Regular user with no 'Inventory Dashboard' permission sees a warning message, not blank/crash
9. Direct URL navigation to `/inventory/dashboard` works after browser refresh
10. Clicking Inventory in top nav opens dashboard (not the generic dashboard)

---

## 10. Implementation Risks

| Risk | Mitigation |
|------|-----------|
| `StockAdjustment` ledger entry only on Approve — not on Save | SP reads from `StockLedger` directly (not StockAdjustment table), so correctly approved-only adjustments are included |
| No status column on `StockTransfer` — all saved transfers are "committed" | Pending Transfers KPI counts this month's transfers; documented limitation |
| `UnitCost` can be 0 or NULL in ledger for some transaction types | Use `ISNULL(UnitCost, 0)` and `COALESCE` last non-zero cost per item; document as estimated valuation |
| `ReorderLevel` is NULL for items not configured | Only flag items with non-null ReorderLevel below current balance |
| SharedModule declaration required | Must add component to SharedModule, not just create files |
| ApexCharts `ng-apexcharts` is installed — version 1.7.x supports Angular 13 | Use ApexCharts (both donut and bar) — single library, consistent look |

---

## 11. Chart Library Decision

**Use `ng-apexcharts` (ApexCharts).**
- Already installed: `apexcharts: ^3.35.3`, `ng-apexcharts: ^1.7.1`
- Compatible with Angular 13
- Supports donut charts (`type: 'donut'`) and bar/grouped charts (`type: 'bar'`)
- Consistent styling across both chart types
- No additional install needed
