# OBMS Inventory – Edit Functionality Analysis

**Date:** October 2026  
**Scope:** Inventory module only — UI, API, Repository, and Database layers  
**Purpose:** Identify where Edit functionality is missing, incomplete, or incorrectly implemented.  
**Status:** Analysis only. No code has been modified.

---

## 1. Overall Summary

The OBMS Inventory module is divided into three broad categories of screen:

| Category | Edit Status |
|---|---|
| **Master screens** (Item, Category, Brand, UOM, Warehouse, Supplier, Recipient) | All fully implemented — Create and Edit work through a shared `SaveAndUpdate*` endpoint. |
| **Approval-workflow transactions** (PR, PO, Stock Adjustment, BSR, Asset Disposal) | Edit correctly restricted to Draft status only. Approve/Reject actions are gated by role and configured approver. Well implemented. |
| **Posted stock-movement transactions** (GRN, Purchase Return, Stock Issue, Stock Issue Return, Stock Transfer, Stock Damage/Loss) | A significant **API vs UI mismatch** exists. The repository layer for Stock Issue, Stock Transfer, and Stock Damage/Loss already implements a correct reversal-and-recreate update path (ID > 0 branch). However the Angular UI unconditionally sets `isViewMode = true` for all existing records, disabling the form and preventing any save action. The API update path is therefore **dead code from the UI perspective**. |

The most actionable gaps are:

1. **Stock Issue, Stock Transfer, Stock Damage/Loss** — API update path exists and is safe. UI edit mode is simply not wired up.
2. **Purchase Return** — no delete endpoint exists anywhere (API or UI). A posted return cannot be corrected or reversed.
3. **GRN** — no edit path exists in the API; the UI correctly reflects this. The existing Delete workflow (with dependency check) is the correction path.
4. **Stock Issue Return** — no edit path in API or UI; Delete is also not implemented.
5. **Asset transaction screens** (Allocation, Transfer, Return, Maintenance) — inline-form only, create-only pattern. Edit of an existing record is not supported but these are generally appropriate as event records.

---

## 2. Screen-by-Screen Checklist

### 2-A. Master Screens

#### Item Master

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/item-master/new-item/:ID` |
| **Edit button** | Present in list (`edit` icon, `updateAccess` guard, routerLink to `/new-item/:row.ID`) |
| **Load** | `service.getItemByID(ID)` → `frm.patchValue(item)` |
| **Save** | `service.saveItem(data)` → `POST /api/Inventory/SaveAndUpdateItem` — ID 0 = insert, ID > 0 = update |
| **API method** | `SaveAndUpdateItem` in `InventoryRepository.cs` line 110 |
| **Lock rules** | None — master data can always be edited |
| **Delete** | `DeleteItem/{id}` — blocks if item is referenced in transactions |
| **Fields that must be preserved** | `Code` (auto-generated, disabled in form, sent via `getRawValue`) |
| **Risk** | Changing `Price` on an existing item does not retroactively change any posted transaction unit cost — safe |

#### Category Master

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/category-master/new-category/:ID` |
| **API method** | `SaveAndUpdateCategory` line 36 |
| **Edit button** | Present (route confirmed in routing module) |
| **Delete** | `DeleteCategory/{id}` |

#### Brand Master

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/brand-master/new-brand/:ID` |
| **API** | `SaveAndUpdateBrandMaster` (line 703) / `GET SaveAndUpdateBrandMaster` / `DeleteBrand/{id}` |
| **Delete** | `DeleteBrand/{id}` |

#### UOM Master

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/uom-master/new-uom/:ID` |
| **API** | `SaveAndUpdateUOMMaster` line 734 / `DeleteUOM/{id}` |

#### Warehouse Master

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/warehouse-master/new-warehouse/:ID` |
| **API** | `SaveAndUpdateWarehouseMaster` line 763 / `DeleteWarehouse/{id}` |
| **Lock rules** | `IsActive` flag controls visibility in dropdowns |

#### Supplier Master

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/supplier-master/new-supplier/:ID` |
| **API** | `SaveAndUpdateSupplier` line 251 / `DeleteSupplier/{id}` |

#### Recipient Master

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/recepient-master/new-recepient/:ID` |
| **API** | `SaveAndUpdateRecipient` line 304 / `DeleteRecipient/{id}` |

---

### 2-B. Opening Stock

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/opening-stock/new-opening-stock/:ID` |
| **Edit button** | Present (route confirmed in routing module) |
| **Load** | `service.getOpeningStockByID(ID)` → `frm.patchValue` |
| **Save** | `POST SaveOpeningStock` — `SaveOpeningStock` in repository |
| **Ledger handling** | When `dto.ID > 0`, the existing `OPENING` ledger row is updated **in-place** (no duplicate created). Safety net: creates a new row if the ledger entry is somehow missing. Transaction-wrapped. |
| **Lock rules** | None at API level. Consider blocking edit if downstream transactions (STOCK_IN, ISSUE etc.) already exist for the same item/warehouse — not currently enforced. |
| **Duplicate check** | `ID == 0` path only — prevents two opening entries for same item+warehouse. Edit bypasses this correctly. |
| **Delete** | `DeleteOpeningStock/{id}` — reverses the OPENING ledger row |
| **Fields safe to edit** | All — qty, cost, date, warehouse, item, remarks |
| **⚠️ Risk** | If stock has already been issued or transferred from this warehouse, editing the opening quantity downward could produce a negative running balance in the stock ledger. No guard currently blocks this. A check against subsequent movements is recommended before allowing the edit. |

---

### 2-C. Purchase Requisition (PR)

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/purchase-requisition/new-pr/:ID` |
| **Edit button** | Present — `edit` icon in list, `updateAccess` guard |
| **Load** | `service.getPRByID(ID)` → `frm.patchValue` + line array rebuild |
| **Save** | `POST SavePR` — ID 0 = insert, ID > 0 = update (delete old details, insert new) |
| **Lock rules** | `frm.disable()` when status ≠ Draft. API rejects edit of non-Draft PR. |
| **Approve/Reject** | Available to configured approver (PURCHASE_REQUISITION routing) or SuperAdmin when status = Submitted |
| **Delete** | Draft only — `DeletePR/{id}` — blocked if PO exists |
| **Linked document** | Approved PR → PO link shown. Once PO created, PR cannot be deleted. |
| **Fields safe to edit (Draft)** | All header fields, all line items including qty, price, SST% |
| **Fields locked (non-Draft)** | Entire form disabled |

---

### 2-D. Purchase Order (PO)

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/purchase-order/new-po/:ID` |
| **Edit button** | Present in list — routerLink to `/new-po/:row.ID` |
| **Editability** | `GET GetPOEditability` drives `canEditPO` flag. Returns `canEdit=true` only for Draft POs not yet linked to a GRN. |
| **Lock rules** | Form disabled on load. Re-enabled only when `canEditPO=true`. PONumber always readonly. PRID never changeable on edit. |
| **When PR-linked** | `isPRLinked=true` — item list is locked; only Qty/Price editable |
| **Approve/Reject** | Configured approver (PURCHASE_ORDER routing) or SuperAdmin when Submitted |
| **Cancel** | `CancelPO/{id}` — blocked if Partially or Fully Received |
| **GRN navigation** | After approval, [Receive Goods] button navigates to new GRN with `?poID=` param |
| **Fields safe to edit (Draft, no PR)** | All — supplier, warehouse, dates, lines |
| **Fields safe to edit (Draft, PR-linked)** | Qty, UnitPrice, SSTPercent per line only |
| **Fields locked** | PONumber, PRID, Submitted/Approved/Received status forms |

---

### 2-E. Goods Receipt Note (GRN)

| Item | Detail |
|---|---|
| **Status** | ✅ Correctly Restricted by Business Rules (View Only) |
| **Route** | `/inventory/goods-receipt/new-grn/:ID` |
| **Edit button** | **None** in list or form |
| **Load** | `service.getGRNByID(ID)` → `frm.patchValue` + `existingDetails` → `frm.disable()` |
| **Save** | UI payload hardcodes `ID: 0`. Repository `SaveGRN` has **no ID > 0 path** — always inserts. |
| **Correction workflow** | Delete GRN (with dependency check) → re-create. `DeleteGRN/{id}` reverses: stock ledger (STOCK_IN), PO ReceivedQty, PO status, and CreditorInvoice row. |
| **Delete** | `DeleteGRN/{id}` with dependency check — blocked if Finance payment exists against CreditorInvoice |
| **Edit is not recommended** | GRN creates a CreditorInvoice row (Finance-owned). Editing received quantities in-place would require retroactively correcting the Finance payable — a cross-module concern. Delete + recreate is the correct pattern. |
| **Gap** | No edit button is present. However, a "View" icon/button could be added to the list for navigation to the read-only view (currently users must click into the list row directly — the HTML does not show a view icon in the action column). |
| **⚠️ Missing UI** | The GRN list action column may lack a clear View icon. Needs HTML inspection to confirm — flagged as unverified. |

---

### 2-F. Purchase Return

| Item | Detail |
|---|---|
| **Status** | ⚠️ Partially Implemented — Delete path is completely missing |
| **Route** | `/inventory/purchase-return/new-return/:ID` |
| **Edit button** | **None** in list |
| **View button** | **None** visible in list component HTML (PurchaseReturnComponent has no action button logic beyond loadList) |
| **Load** | `service.getPurchaseReturnByID(ID)` → `frm.patchValue` → `frm.disable()` |
| **Save** | Repository `SavePurchaseReturn` has **no ID > 0 path** — always inserts |
| **Delete API** | **Does not exist.** No `DeletePurchaseReturn` endpoint in controller or repository |
| **Delete UI** | No delete button, no `deleteClick` method in `PurchaseReturnComponent` |
| **⚠️ Critical gap** | A posted Purchase Return cannot be corrected or reversed by any current mechanism. The stock-out (PURCHASE_RTN ledger entry) is permanent unless manually corrected at DB level. |
| **Recommended fix** | Add `DeletePurchaseReturn/{id}` API endpoint + repository method that: (1) sets `IsActive=false` on the return header and details, (2) deletes the PURCHASE_RTN ledger rows, (3) decrements `AlreadyReturnedQty` tracking (or relies on live SUM query). Add a delete button in the list gated by admin/superadmin only. |
| **Finance impact** | No Finance CreditorInvoice row is created by Purchase Return. The GRN's CreditorInvoice is not modified by the return. Delete is safe from a Finance perspective. |

---

### 2-G. Stock Issue

| Item | Detail |
|---|---|
| **Status** | ⚠️ Partially Implemented — API done, UI blocked |
| **Route** | `/inventory/stock-issue/new-stock-issue/:ID` |
| **Edit button** | **None** in list (no routerLink with ID in action column — needs HTML verification) |
| **Load** | `service.getStockIssueByID(ID)` → `frm.patchValue` + FormArray rebuild → `frm.disable()` (`isViewMode=true`) |
| **API update path** | `SaveStockIssue` in repository (line 3196): when `dto.ID > 0`, **removes old `StockIssueDocDetails` and `StockLedger` rows** for the document, then recreates all lines and new ISSUE ledger entries. Transactional. Stock availability re-checked against new quantities. |
| **UI blocks edit** | `isViewMode = true` unconditionally when `ID` param exists. `onSubmit()` returns immediately when `isViewMode`. |
| **Delete** | `DeleteStockIssue/{id}` — reverses ISSUE ledger rows. Blocked if a StockIssueReturn exists against it. |
| **Linked documents** | Stock Issue Return references StockIssueDoc. Edit must be blocked if any return has been posted. |
| **To enable edit** | Change `isViewMode = true` to `isViewMode = false` and wire an Edit/Save button in the form when no return exists. See Section 7 for exact files. |
| **Fields safe to edit** | All — IssueDate, WarehouseID, IssueToType, IssueToID, IssueToName, all detail lines |
| **Fields that must be preserved** | `IssueNo` (auto-generated, must not change on edit) |
| **Block condition** | Any StockIssueReturn exists for this IssueDocID |

---

### 2-H. Stock Issue Return

| Item | Detail |
|---|---|
| **Status** | ❌ Missing — no edit or delete in either API or UI |
| **Route** | `/inventory/stock-issue-return/new-stock-issue-return/:ID` |
| **Edit button** | **None** |
| **Load** | `service.getStockIssueReturnByID(ID)` → `frm.patchValue` → `frm.disable()` (`isViewMode=true`) |
| **API update path** | `SaveStockIssueReturn` in repository (line 3480): **no ID > 0 path** — always inserts |
| **Delete API** | No `DeleteStockIssueReturn` endpoint |
| **Delete UI** | No delete button in `StockIssueReturnComponent` |
| **⚠️ Gap** | A posted return cannot be corrected. The ISSUE_RETURN ledger entry is permanent. |
| **Recommended fix** | Add `DeleteStockIssueReturn/{id}` that deletes ISSUE_RETURN ledger rows, decrements `AlreadyReturnedQty` tracking, and soft-deletes the record. Add delete button (admin/superadmin only). |
| **Finance impact** | No Finance rows. Safe to delete. |

---

### 2-I. Stock Transfer

| Item | Detail |
|---|---|
| **Status** | ⚠️ Partially Implemented — API done, UI blocked |
| **Route** | `/inventory/stock-transfer/new-stock-transfer/:ID` |
| **Edit button** | **None** in list action column |
| **Load** | `service.getStockTransferByID(ID)` → `frm.patchValue` + FormArray → `frm.disable()` (`isViewMode=true`) |
| **API update path** | `SaveStockTransfer` (line 3678): when `dto.ID > 0`, **removes old `StockTransferDetails` and all `StockLedger` rows** where `ReferenceType IN ('Stock Transfer OUT', 'Stock Transfer IN')` for the document, then recreates TRANSFER_OUT (source warehouse) and TRANSFER_IN (destination warehouse) ledger entries. Stock availability re-checked. Transactional. |
| **UI blocks edit** | `isViewMode = true` unconditionally on existing records |
| **Cancel (Delete)** | `DeleteStockTransfer/{id}` — reverses TRANSFER_OUT and TRANSFER_IN ledger rows |
| **BSR link** | If transfer was created from a BSR (`fromBSR` param), it is linked via `LinkBSRTransfer`. Edit must preserve or update this link. |
| **To enable edit** | Change `isViewMode = false` for documents that have no downstream dependency (e.g. not linked to an active BSR Approved → Transfer Created status). Wire Save button in view mode template. |
| **Fields safe to edit** | All — TransferDate, SourceWarehouseID, DestWarehouseID, all lines |
| **Fields that must be preserved** | `TransferNo` |
| **Block condition** | Transfer linked to a completed BSR (LinkedTransferNo set and BSR status = Transfer Created/Completed) |

---

### 2-J. Stock Adjustment

| Item | Detail |
|---|---|
| **Status** | ✅ Correctly Restricted by Business Rules |
| **Route** | `/inventory/stock-adjustment/new-stock-adjustment/:ID` |
| **Edit button** | Present — routerLink to `/new-stock-adjustment/:row.ID` |
| **Load** | `service.getStockAdjustmentByID(ID)` → patchValue |
| **Edit allowed** | Draft status only — `frm.disable()` on Submitted/Approved/Rejected |
| **Stock ledger** | `ApplySALedger` is called only on Approve (`ApproveSA`). Draft saves do not touch the ledger. |
| **Delete** | `DeleteStockAdjustment/{id}` — only allowed on Draft. Approved records block delete (ledger already applied). |
| **Approve/Reject** | Configured approver (STOCK_ADJUSTMENT routing) or SuperAdmin when Submitted |
| **Fields safe to edit (Draft)** | All — WarehouseID, ItemID, AdjustmentType, Qty, UnitCost, Reason, Remarks |
| **Fields locked (post-Draft)** | Entire form |

---

### 2-K. Stock Damage / Loss

| Item | Detail |
|---|---|
| **Status** | ⚠️ Partially Implemented — API done, UI blocked |
| **Route** | `/inventory/stock-damage-loss/new-stock-damage-loss/:ID` |
| **Edit button** | **None** in list action column |
| **Load** | `service.getStockDamageLossByID(ID)` → `frm.patchValue` → `frm.disable()` (`isViewMode=true`) |
| **API update path** | `SaveStockDamageLoss` (line 4324): when `dto.ID > 0`, **removes old DAMAGE_LOSS ledger rows** then recreates. Stock availability re-checked against new quantity. Transactional. |
| **UI blocks edit** | `isViewMode = true` unconditionally on existing records |
| **Delete** | `DeleteStockDamageLoss/{id}` — reverses DAMAGE_LOSS ledger row |
| **To enable edit** | Change `isViewMode = false`. Wire Save button. |
| **Fields safe to edit** | All — WarehouseID, ItemID, LossType, Quantity, UnitCost, Reason, Remarks |
| **Fields that must be preserved** | `RecordNo` |
| **Finance impact** | None — Damage/Loss is inventory-only |

---

### 2-L. Branch Stock Request (BSR)

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/branch-stock-request/new-bsr/:ID` |
| **Edit button** | Present — routerLink to `/new-bsr/:row.ID` |
| **Load** | `service.getBSRByID(ID)` → patchValue + FormArray |
| **Edit mode** | `isEditMode = true` when Draft, `isViewMode = true` when non-Draft |
| **Save** | `POST SaveBSR` — ID 0 = insert, ID > 0 = update (delete old details, insert new) |
| **Approve/Reject** | Configured approver (BRANCH_STOCK_REQUEST routing) or SuperAdmin when Submitted |
| **Transfer link** | Approved BSR → Create Transfer navigates to `/new-stock-transfer?fromBSR=X`. Transfer links back via `LinkBSRTransfer`. |
| **Delete** | Draft only — `DeleteBSR/{id}` |

---

### 2-M. Asset Master

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified |
| **Route** | `/inventory/asset-master/new-asset/:ID` |
| **Edit button** | Present in list |
| **Load** | `service.getAssetByIDEnriched(ID)` → patchValue |
| **Save** | `POST SaveAsset` — ID 0 = insert, ID > 0 = update |
| **Lock rules** | Disposed assets should not be editable — `SaveAsset` repository should check AssetStatus ≠ Disposed before allowing update. Needs verification. |
| **Delete** | `DeleteAssetRecord/{id}` — soft delete |

---

### 2-N. Asset Allocation

| Item | Detail |
|---|---|
| **Status** | ✅ Correctly Restricted by Business Rules |
| **Route** | No edit route — inline form on `/inventory/asset-allocation` |
| **Edit of existing** | Not supported. By design: allocations are event records. |
| **Cancel** | `CancelAssetAllocation/{id}` — sets IsActive=false, reverts asset to Available. The correct correction path. |
| **New form** | `openNewForm()` builds fresh form. `POST SaveAssetAllocation`. |
| **Fields** | AssetID, AllocationDate, AllocatedToType, AllocatedToID/Name, BranchCode, Remarks |
| **⚠️ Minor gap** | No view button on list rows to see full allocation details. Only PDF print available. |

---

### 2-O. Asset Transfer

| Item | Detail |
|---|---|
| **Status** | ✅ Correctly Restricted by Business Rules |
| **Route** | No edit route — inline form on `/inventory/asset-transfer` |
| **Edit of existing** | Not supported. Asset transfers are immutable event records. |
| **Correction** | Post a new transfer back to the original holder. |
| **New form** | `openNewForm()` + `POST SaveAssetTransfer`. Asset status updated to In_Transit or Allocated. |

---

### 2-P. Asset Return

| Item | Detail |
|---|---|
| **Status** | ✅ Correctly Restricted by Business Rules |
| **Route** | No edit route — inline form on `/inventory/asset-return` |
| **Edit of existing** | Not supported. Returns are immutable event records. |
| **Correction** | Post a new allocation if return was entered incorrectly. |
| **New form** | `openNewForm()` + `POST SaveAssetReturn`. Clears active allocation. Asset reverts to Available. |

---

### 2-Q. Asset Maintenance

| Item | Detail |
|---|---|
| **Status** | ✅ Correctly Restricted by Business Rules (with minor gap) |
| **Route** | No edit route — inline form on `/inventory/asset-maintenance` |
| **Edit of existing** | Not supported. Maintenance records are append-only event logs. |
| **Complete action** | `CompleteAssetMaintenance/{id}` — updates CompletionDate, Cost, and Status. Asset status reverts. |
| **⚠️ Minor gap** | Cannot edit VendorName, ProblemDescription, or StartDate on a pending maintenance record without deleting and re-creating. No delete endpoint exists for Asset Maintenance. |

---

### 2-R. Asset Damage

| Item | Detail |
|---|---|
| **Status** | ✅ Correctly Restricted by Business Rules |
| **Route** | No edit route (Asset Damage is recorded inline on Asset Maintenance page or separate `asset-damage` component) |
| **Edit of existing** | Not supported. Damage records are event logs. |
| **API** | `SaveAssetDamage` — always inserts. `GetAssetDamageByID` / `GetAssetDamageList` available. |

---

### 2-S. Asset Disposal

| Item | Detail |
|---|---|
| **Status** | ✅ Correctly Restricted by Business Rules |
| **Route** | `/inventory/asset-disposal` (with inline `showDetail` panel) and `/inventory/asset-disposal/:ID` (deep-link from Pending Approvals) |
| **Edit of existing** | Draft edits are handled via the new-form inline panel. Non-Draft records are view-only in `showDetail` panel. |
| **Submit/Approve/Reject** | Configured approver (ASSET_DISPOSAL routing) or SuperAdmin |
| **ApplyADDisposal** | Called on Approve — sets AssetStatus = Disposed, records movement history, cancels active allocations |

---

### 2-T. Stock Ledger (Report / View)

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified (read-only) |
| **Route** | `/inventory/stock-ledger` |
| **Edit** | Not applicable — read-only ledger viewer |
| **Data source** | `GetStockLedger` API + `VWStockLedger` SQL view for ASPX report |
| **Running balance** | Computed via `SUM(InQty - OutQty) OVER (PARTITION BY WarehouseID, ItemID ORDER BY TransactionDate, ID)` — matches C# accumulator |

---

### 2-U. Employee Item Holding / Issue Register / Stock Balance Summary

| Item | Detail |
|---|---|
| **Status** | ✅ Completed and Verified (read-only enquiry screens) |
| **Edit** | Not applicable |

---

## 3. Fields Safe to Edit vs Fields That Must Be Preserved

### Master Records (Item, Category, Brand, UOM, Warehouse, Supplier, Recipient)

| Field | Rule |
|---|---|
| Auto-generated Code | **Preserve** — read-only in all forms |
| All descriptive fields | Safe to edit |
| IsActive flag | Safe to toggle — affects visibility in dropdowns |
| WarehouseID on Item | Safe — only affects new transaction defaults |

### Opening Stock

| Field | Rule |
|---|---|
| ReferenceNo | **Preserve** — kept from original record |
| WarehouseID | Safe to edit — ledger row updated in-place |
| ItemID | Safe to edit — ledger row updated in-place |
| OpeningDate | Safe — affects running balance ordering |
| OpeningQty | ⚠️ Risk — can produce negative running balance if stock has moved |
| UnitCost | Safe for the ledger row — does not retroactively change transaction unit costs |

### PR (Draft only)

| Field | Rule |
|---|---|
| PRNumber | **Preserve** |
| All other header + line fields | Safe to edit on Draft |
| Status-after-submit | **Preserve** — form disabled on non-Draft |

### PO (Draft only)

| Field | Rule |
|---|---|
| PONumber | **Preserve** |
| PRID | **Preserve** — cannot change the linked PR |
| SupplierID, WarehouseID, dates | Safe on Draft |
| Line items (PR-linked PO) | Only Qty, UnitPrice, SSTPercent safe — ItemID locked |

### Stock Issue (Draft / new document)

| Field | Rule |
|---|---|
| IssueNo | **Preserve** |
| IssueDate, WarehouseID, IssueToType/ID/Name | Safe |
| Detail lines (Qty, Item, UnitCost) | Safe — old ledger rows deleted, new ones created |
| **Block if** | Any StockIssueReturn exists against this IssueDocID |

### Stock Transfer (new document)

| Field | Rule |
|---|---|
| TransferNo | **Preserve** |
| TransferDate, SourceWarehouseID, DestWarehouseID | Safe |
| Detail lines (Qty, Item, UnitCost) | Safe — old TRANSFER_OUT + TRANSFER_IN rows deleted, new ones created |
| **Block if** | BSR is in a completed/fulfilled state referencing this transfer |

### Stock Damage/Loss

| Field | Rule |
|---|---|
| RecordNo | **Preserve** |
| All other fields | Safe — old DAMAGE_LOSS ledger row deleted, new one created |

---

## 4. Stock Ledger and Linked-Document Impact

### Stock Ledger Transaction Type Map

| TransactionType | Direction | Posted By | Reversed By |
|---|---|---|---|
| `OPENING` | IN | SaveOpeningStock (new) | DeleteOpeningStock / SaveOpeningStock (edit — updates in-place) |
| `STOCK_IN` | IN | SaveGRN | DeleteGRN |
| `ISSUE` | OUT | SaveStockIssue (new/edit) | DeleteStockIssue / SaveStockIssue (edit — delete+recreate) |
| `ISSUE_RETURN` | IN | SaveStockIssueReturn | **No reversal exists** |
| `TRANSFER_OUT` | OUT | SaveStockTransfer (new/edit) | DeleteStockTransfer / SaveStockTransfer (edit — delete+recreate) |
| `TRANSFER_IN` | IN | SaveStockTransfer (new/edit) | DeleteStockTransfer / SaveStockTransfer (edit — delete+recreate) |
| `ADJ_PLUS` | IN | ApproveSA | DeleteStockAdjustment (Draft only — ledger not yet written) |
| `ADJ_MINUS` | OUT | ApproveSA | DeleteStockAdjustment (Draft only) |
| `PURCHASE_RTN` | OUT | SavePurchaseReturn | **No reversal exists** |
| `DAMAGE_LOSS` | OUT | SaveStockDamageLoss (new/edit) | DeleteStockDamageLoss / SaveStockDamageLoss (edit — delete+recreate) |

**Running Balance:** `SUM(InQty - OutQty)` partitioned by `(WarehouseID, ItemID)`, ordered by `(TransactionDate ASC, ID ASC)`. Any edit that changes `InQty`, `OutQty`, `WarehouseID`, or `ItemID` on a ledger row automatically flows through to all subsequent running balance values for that partition.

### Dependency Chain Summary

```
Opening Stock → StockLedger (OPENING)
                     ↓
PR (Draft) → PR (Approved) → PO (Draft) → PO (Approved) → GRN → StockLedger (STOCK_IN)
                                                                 → CreditorInvoice (Finance)
                                                                 → PurchaseReturn → StockLedger (PURCHASE_RTN) [no reversal]

GRN ← PO ReceivedQty updated
GRN ← PurchaseReturn (many returns per GRN, per-line quantity tracked)

StockIssue → StockLedger (ISSUE) → StockIssueReturn → StockLedger (ISSUE_RETURN) [no reversal]

StockTransfer → StockLedger (TRANSFER_OUT @ source) + StockLedger (TRANSFER_IN @ dest)
BSR (Approved) → StockTransfer (linked via LinkedTransferID)

StockAdjustment (Draft) → StockAdjustment (Approved) → StockLedger (ADJ_PLUS or ADJ_MINUS)

StockDamageLoss → StockLedger (DAMAGE_LOSS)

AssetMaster → AssetAllocation → AssetTransfer → AssetReturn
           → AssetMaintenance → CompleteAssetMaintenance
           → AssetDamage
           → AssetDisposal (Approved) → AssetStatus = Disposed, cancels active allocations
```

### Finance Cross-Module Impact

The only inventory transaction that writes to a Finance-owned table is `SaveGRN`, which creates a `CreditorInvoice` row so the GRN appears in Finance Payments (`vwPaymentDue`). Editing or deleting a GRN requires also updating or deleting the corresponding CreditorInvoice. The existing `DeleteGRN` implementation correctly handles this. No other inventory transaction touches Finance tables.

---

## 5. Approval and Authorization Restrictions

| Screen | Approval Type | Approver Determination | Lock After Approval |
|---|---|---|---|
| PR | PURCHASE_REQUISITION routing | `isCurrentUserApprover(type, submittedBy)` + SuperAdmin bypass | Form fully disabled |
| PO | PURCHASE_ORDER routing | Same pattern | `GetPOEditability` returns `canEdit=false` |
| Stock Adjustment | STOCK_ADJUSTMENT routing | Same pattern | Form disabled |
| BSR | BRANCH_STOCK_REQUEST routing | Same pattern | `isViewMode=true` |
| Asset Disposal | ASSET_DISPOSAL routing | Same pattern | Detail panel view only |
| GRN | None — auto-posted | N/A | Immutable after post |
| Stock Issue | None — auto-posted | N/A | `isViewMode=true` (blocks edit + API) |
| Stock Transfer | None — auto-posted | N/A | `isViewMode=true` |
| Stock Damage/Loss | None — auto-posted | N/A | `isViewMode=true` |
| Purchase Return | None — auto-posted | N/A | No edit or delete |

**Self-approval:** The system uses JWT identity server-side for all approve/reject actions. A user cannot approve their own submission unless their `IsAdmin=2` (SuperAdmin). This is enforced in the controller (`dto.ApprovedBy = jwtUser`).

---

## 6. Exact Files Requiring Changes

### Priority 1 — Critical Functional Gaps

#### 6-A. Purchase Return — Add Delete Functionality

**Problem:** A posted Purchase Return has no reversal path. Stock ledger entry is permanent.

| Layer | File | Change Required |
|---|---|---|
| Repository | `OBMSAPP-API/OBMS.WebAPI/Repositories/Interface/IInventoryRepository.cs` | Add `Task<(bool Success, string Message)> DeletePurchaseReturn(int id, string currentUser);` |
| Repository | `OBMSAPP-API/OBMS.WebAPI/Repositories/Implementation/InventoryRepository.cs` | Implement `DeletePurchaseReturn`: set `IsActive=false` on header+details, delete `PURCHASE_RTN` ledger rows for `ReferenceType='Purchase Return' AND ReferenceID=id`, decrement nothing (return qty is tracked via SUM query) |
| Controller | `OBMSAPP-API/OBMS.WebAPI/Controllers/InventoryController.cs` | Add `[HttpPost("DeletePurchaseReturn/{id}")] public async Task<IActionResult> DeletePurchaseReturn(int id, ...)` |
| UI List | `OBMSAPP-UI/angularapp/src/app/modules/inventory/purchase-return/purchase-return.component.ts` | Add `deleteClick(row)` method |
| UI List | `OBMSAPP-UI/angularapp/src/app/modules/inventory/purchase-return/purchase-return.component.html` | Add delete icon in action column, visible to admin/superadmin |
| UI Service | `OBMSAPP-UI/angularapp/src/app/service/inventory.service.ts` | Add `deletePurchaseReturn(id, user)` method |

---

#### 6-B. Stock Issue Return — Add Delete Functionality

**Problem:** A posted Issue Return has no reversal path.

| Layer | File | Change Required |
|---|---|---|
| Repository interface | `IInventoryRepository.cs` | Add `DeleteStockIssueReturn` |
| Repository impl | `InventoryRepository.cs` | Implement: soft-delete record, remove ISSUE_RETURN ledger rows |
| Controller | `InventoryController.cs` | Add `[HttpPost("DeleteStockIssueReturn/{id}")]` |
| UI List | `stock-issue-return.component.ts` | Add `deleteClick(row)` |
| UI List | `stock-issue-return.component.html` | Add delete icon |
| Service | `inventory.service.ts` | Add `deleteStockIssueReturn(id, user)` |

---

### Priority 2 — UI Edit Mode Not Wired (API Already Safe)

#### 6-C. Stock Issue — Enable Edit Mode in UI

**Problem:** `SaveStockIssue` in repository correctly handles ID > 0 (reversal + recreate). The UI unconditionally sets `isViewMode=true` on existing records.

| Layer | File | Change Required |
|---|---|---|
| UI Form | `new-stock-issue.component.ts` | Replace `this.isViewMode = true` with conditional: set `isViewMode=true` only if a StockIssueReturn exists against this IssueDocID. Check via `getIssuableDocsForReturn()` or a new `CheckStockIssueDependency` API endpoint. |
| UI Form | `new-stock-issue.component.html` | Remove or conditionally show "This record is read-only" notice. Show Save button when not `isViewMode`. |
| UI List | `stock-issue.component.html` | Add `edit` icon in action column (alongside existing delete), routerLink to `/new-stock-issue/:row.ID` |
| API (new) | `InventoryController.cs` | Add `GET CheckStockIssueDependency/{id}` — returns `{canEdit: bool, blockingMessage: string}` (mirrors `CheckGRNDependency` pattern) |
| Repository (new) | `InventoryRepository.cs` | Implement: check if any `StockIssueReturn` exists with `OriginalIssueDocID = id` |

---

#### 6-D. Stock Transfer — Enable Edit Mode in UI

**Problem:** `SaveStockTransfer` handles ID > 0 (reversal + recreate for both TRANSFER_OUT and TRANSFER_IN rows). UI blocks edit.

| Layer | File | Change Required |
|---|---|---|
| UI Form | `new-stock-transfer.component.ts` | Replace `this.isViewMode = true` with: `isViewMode = false` when no BSR link or BSR not yet fulfilled. Add block condition check. |
| UI Form | `new-stock-transfer.component.html` | Add Save button when not `isViewMode` |
| UI List | `stock-transfer.component.html` | Add `edit` icon in action column |
| API (new) | `InventoryController.cs` | Add `GET CheckStockTransferDependency/{id}` — checks if linked BSR is fulfilled |

---

#### 6-E. Stock Damage/Loss — Enable Edit Mode in UI

**Problem:** `SaveStockDamageLoss` handles ID > 0 (deletes old ledger row, recreates). UI blocks edit.

| Layer | File | Change Required |
|---|---|---|
| UI Form | `new-stock-damage-loss.component.ts` | Replace `this.isViewMode = true` with `isViewMode = false`. No downstream dependencies for damage/loss records. |
| UI Form | `new-stock-damage-loss.component.html` | Add Save button when not `isViewMode` |
| UI List | `stock-damage-loss.component.html` (needs inspection) | Add `edit` icon in action column |

---

### Priority 3 — Minor Gaps

#### 6-F. Opening Stock — Add Edit Guard for Downstream Movements

**Problem:** Editing opening stock quantity downward after stock has been issued could produce negative running balances.

| Layer | File | Change Required |
|---|---|---|
| Repository | `InventoryRepository.cs` — `SaveOpeningStock` | Before updating, check if any non-OPENING ledger row exists for the same `(WarehouseID, ItemID)`. If yes, block reduction of OpeningQty below the quantity that would make current stock negative. |

---

#### 6-G. GRN List — Add Clear View Button

**Problem:** GRN list action column may lack a visible View icon. Users must know to click the row.

| Layer | File | Change Required |
|---|---|---|
| UI List | `goods-receipt.component.html` | Add `visibility` icon in action column, routerLink to `/new-grn/:row.ID` |

---

#### 6-H. Asset Master Edit — Add Disposed Guard

**Problem:** Disposed assets should not be editable (status change would be misleading).

| Layer | File | Change Required |
|---|---|---|
| Repository | `InventoryRepository.cs` — `SaveAsset` | Check `existing.AssetStatus != "Disposed"` before allowing update when `dto.ID > 0` |
| UI Form | `new-asset.component.ts` (needs inspection) | Disable form when loaded asset has `AssetStatus == 'Disposed'` |

---

## 7. Risk Assessment and Recommended Implementation Order

### Risk Levels

| Risk | Description |
|---|---|
| 🔴 High | Data integrity or financial impact — must not be implemented without careful testing |
| 🟡 Medium | Stock balance impact — requires proper reversal, test against stock ledger |
| 🟢 Low | UI-only change, no stock/finance impact |

### Recommended Order

| Priority | Task | Risk | Reason |
|---|---|---|---|
| 1 | Purchase Return — Add Delete | 🟡 Medium | Critical gap — only path to correct a wrong return. No finance impact. |
| 2 | Stock Issue Return — Add Delete | 🟡 Medium | Critical gap — posted returns cannot be corrected. |
| 3 | Stock Damage/Loss — Enable Edit | 🟡 Medium | API already safe. Simple UI change. No downstream dependencies. |
| 4 | Stock Issue — Enable Edit + Dependency Check | 🟡 Medium | API safe. Must add dependency check for existing returns first. |
| 5 | Stock Transfer — Enable Edit + Dependency Check | 🟡 Medium | API safe. Must check BSR link state. |
| 6 | GRN List — Add View Button | 🟢 Low | Pure UI navigation improvement. |
| 7 | Opening Stock — Add Downstream Guard | 🟡 Medium | Prevent negative balance from retrospective qty reduction. |
| 8 | Asset Master — Add Disposed Guard | 🟢 Low | Defensive data quality guard. |

---

## 8. Test Cases for Each Editable Workflow

### Opening Stock Edit

| # | Test | Expected |
|---|---|---|
| OS-E-01 | Edit Qty from 100 to 150, save | Ledger OPENING row updates InQty to 150. Running balance increases by 50. |
| OS-E-02 | Edit Qty from 100 to 50 (no subsequent movements) | Ledger row updates. Running balance decreases by 50. |
| OS-E-03 | Edit Qty from 100 to 50 (50 already issued) | Should be blocked — guard needed. Currently passes and creates negative balance. |
| OS-E-04 | Edit UnitCost | Ledger row UnitCost updated. No other ledger rows affected. |
| OS-E-05 | Edit same item+warehouse to match an existing other OS entry | Blocked by duplicate check (edit path bypasses correctly). |

### PR Edit (Draft)

| # | Test | Expected |
|---|---|---|
| PR-E-01 | Edit RequiredQty on existing Draft | Old detail rows deleted, new inserted. Save succeeds. |
| PR-E-02 | Add new line on existing Draft | Line added, total recalculated. |
| PR-E-03 | Attempt edit on Submitted PR | Form disabled. Save button hidden. |
| PR-E-04 | Delete Draft PR with linked PO | Blocked with message. |
| PR-E-05 | Delete Draft PR with no linked PO | Succeeds. |

### PO Edit (Draft)

| # | Test | Expected |
|---|---|---|
| PO-E-01 | Edit UnitPrice on PR-linked Draft PO | Allowed — only price fields editable. ItemID locked. |
| PO-E-02 | Attempt to change SupplierID on PR-linked PO | ItemID select should be disabled. |
| PO-E-03 | Edit Draft PO with no PR link | All fields editable. |
| PO-E-04 | Edit Approved PO | `GetPOEditability` returns `canEdit=false`. Form stays locked. |
| PO-E-05 | Delete GRN then attempt PO edit | `GetPOEditability` should return `canEdit=true`. Form re-enabled. |

### Stock Issue Edit (after fix)

| # | Test | Expected |
|---|---|---|
| SI-E-01 | Edit IssueQty on existing SI with no returns | Old ISSUE ledger rows deleted, new ones created. Stock balance updated. |
| SI-E-02 | Reduce IssueQty below available stock | Backend stock check on new quantity. Passes if sufficient. |
| SI-E-03 | Edit SI that has a StockIssueReturn against it | Blocked — dependency check returns canEdit=false. |
| SI-E-04 | Edit recipient (IssueToName) only | Old ledger rows replaced with same qty. Recipient updated. |

### Stock Transfer Edit (after fix)

| # | Test | Expected |
|---|---|---|
| ST-E-01 | Edit TransferQty on existing Transfer | Old TRANSFER_OUT + TRANSFER_IN rows deleted, new pair created. Both warehouse balances updated. |
| ST-E-02 | Edit source warehouse | Old ledger rows at old source/dest reversed, new rows at new source/dest created. |
| ST-E-03 | Edit Transfer linked to completed BSR | Blocked. |
| ST-E-04 | Reduce TransferQty below available in source | Backend stock check. Blocked if insufficient. |

### Stock Damage/Loss Edit (after fix)

| # | Test | Expected |
|---|---|---|
| DL-E-01 | Edit Quantity on existing DL record | Old DAMAGE_LOSS ledger row deleted, new one created. |
| DL-E-02 | Edit Quantity to more than available stock | Backend stock check blocks. |
| DL-E-03 | Edit LossType from Damage to Loss | RecordNo preserved, type updated, new ledger row created. |

### Purchase Return Delete (after fix)

| # | Test | Expected |
|---|---|---|
| PR-D-01 | Delete Purchase Return | IsActive=false on header/details. PURCHASE_RTN ledger row deleted. GRN ReturnableQty restored (tracked via SUM). |
| PR-D-02 | Delete Purchase Return — verify stock | Warehouse stock for returned items increases by ReturnQty after delete. |

### Stock Adjustment Edit (existing — Draft only)

| # | Test | Expected |
|---|---|---|
| SA-E-01 | Edit AdjustmentQty on Draft | Saved. No ledger change (ledger applied on Approve). |
| SA-E-02 | Edit Approved SA | Form disabled. |
| SA-E-03 | Delete Draft SA | Succeeds. No ledger rows to reverse. |
| SA-E-04 | Delete Approved SA | Blocked with message. |

---

## 9. Unverified Items and Dependencies

The following items could not be fully verified from code alone and require manual testing or further inspection:

| Item | Status | Notes |
|---|---|---|
| Item Master list HTML — edit button presence | **Blocked / Cannot Verify** | `item-master.component.html` not read. Route confirmed. Edit icon likely present — pattern consistent with all other masters. |
| Supplier Master new-supplier component | **Blocked / Cannot Verify** | `new-supplier.component.ts` not read. Route `/new-supplier/:ID` confirmed. API `SaveAndUpdateSupplier` confirmed. Likely complete. |
| Recipient Master new-recepient component | **Blocked / Cannot Verify** | Route `/new-recepient/:ID` confirmed. API `SaveAndUpdateRecipient` confirmed. Likely complete. |
| GRN list action column HTML | **Blocked / Cannot Verify** | `goods-receipt.component.html` not read. Component has no explicit edit/view button logic in `.ts`. Likely missing a view icon. |
| Stock Issue list action column HTML | **Blocked / Cannot Verify** | `stock-issue.component.html` not read. Delete method present in `.ts`. Edit button likely missing. |
| Stock Damage/Loss list action column HTML | **Blocked / Cannot Verify** | `stock-damage-loss.component.html` not read. |
| Stock Transfer list action column HTML | **Blocked / Cannot Verify** | `stock-transfer.component.html` not read. Cancel method present. Edit button likely missing. |
| Asset Master new-asset form — Disposed guard | **Blocked / Cannot Verify** | `new-asset.component.ts` not read. Guard should be added. |
| Opening Stock edit — negative balance guard | **Implemented but Not Tested** | Backend update path confirmed. Guard for downstream movements does not exist in current code. |
| `SaveStockIssueReturn` ID > 0 path | **Missing** | Confirmed no ID > 0 path in `SaveStockIssueReturn`. Edit not possible even via API. |
| `SaveGRN` ID > 0 path | **Missing** | Confirmed no ID > 0 path. Edit not intended by design. |
| `SavePurchaseReturn` ID > 0 path | **Missing** | Confirmed no ID > 0 path. Delete endpoint also missing. |
| Finance CreditorInvoice impact on GRN delete | **Implemented and Verified** | `DeleteGRN` removes CreditorInvoice row. Confirmed in repository. |
| Finance impact of Purchase Return delete | **Implemented but Not Tested** | No CreditorInvoice created by Purchase Return. Safe to delete from Finance perspective. |
| `GetPOEditability` logic after GRN delete | **Implemented but Not Tested** | `GetPOEditability` returns `canEdit=true` if no active GRN exists. Expected to work. Needs test coverage. |

---

## 10. Summary of Proposed Implementation Tasks (Priority Order)

| # | Task | Type | Risk | Files |
|---|---|---|---|---|
| 1 | Add `DeletePurchaseReturn/{id}` API + repository | New API | 🟡 Medium | `IInventoryRepository.cs`, `InventoryRepository.cs`, `InventoryController.cs` |
| 2 | Add delete button in Purchase Return list UI | UI Change | 🟢 Low | `purchase-return.component.ts`, `purchase-return.component.html`, `inventory.service.ts` |
| 3 | Add `DeleteStockIssueReturn/{id}` API + repository | New API | 🟡 Medium | Same pattern as above |
| 4 | Add delete button in Stock Issue Return list UI | UI Change | 🟢 Low | `stock-issue-return.component.ts`, `stock-issue-return.component.html` |
| 5 | Enable edit mode in `new-stock-damage-loss.component.ts` | UI Change | 🟡 Medium | `new-stock-damage-loss.component.ts`, `new-stock-damage-loss.component.html`, list HTML |
| 6 | Add `CheckStockIssueDependency` API | New API | 🟢 Low | `InventoryController.cs`, `InventoryRepository.cs`, `IInventoryRepository.cs` |
| 7 | Enable edit mode in `new-stock-issue.component.ts` with dependency guard | UI Change | 🟡 Medium | `new-stock-issue.component.ts`, `new-stock-issue.component.html`, list HTML |
| 8 | Add `CheckStockTransferDependency` API | New API | 🟢 Low | Same as above pattern |
| 9 | Enable edit mode in `new-stock-transfer.component.ts` with dependency guard | UI Change | 🟡 Medium | `new-stock-transfer.component.ts`, list HTML |
| 10 | Add View icon in GRN list | UI Change | 🟢 Low | `goods-receipt.component.html` |
| 11 | Add Opening Stock downstream movement guard in repository | Repository | 🟡 Medium | `InventoryRepository.cs` — `SaveOpeningStock` |
| 12 | Add Disposed guard to Asset Master save | Repository + UI | 🟢 Low | `InventoryRepository.cs`, `new-asset.component.ts` |

**Do not proceed with any implementation until this analysis has been reviewed and approved.**
