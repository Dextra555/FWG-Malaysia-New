# OBMS PR and PO Edit Functionality Analysis

## Phase 1 — Analyze Existing Implementation

### A. GRN Edit Reference — UI/UX Reference

**Component:** `new-grn.component.ts` and `new-grn.component.html`

**Edit Reference Button Click Handler:**
- `startEditRef()` — toggles `isEditingRef = true`, prepopulates reference fields from current form values
- Shows "Edit Reference" button only in view mode (`isView && !isEditingRef`) with tooltip: "Edit Supplier Invoice No, Delivery Note and Remarks only. Quantities and stock are not changed."
- Three reference fields become editable inline: SupplierInvoiceNo, DeliveryNoteRef, Remarks

**Fields That Become Editable:**
- SupplierInvoiceNo (text input)
- DeliveryNoteRef (text input)  
- Remarks (text input)

**Save and Cancel Behavior:**
- `saveRef()` — POSTs only the 3 reference fields to `Inventory/UpdateGRNReference` API endpoint. No stock/ledger/PO changes.
- `cancelEditRef()` — simply sets `isEditingRef = false`, discards changes
- Save button labeled "Save Reference", disabled during API call (`refSaving`)
- Success message: "Reference updated successfully."
- Error message shows backend error

**API Endpoint and DTO:**
- `POST /api/Inventory/UpdateGRNReference`
- DTO: `{ ID: number; SupplierInvoiceNo?: string; DeliveryNoteRef?: string; Remarks?: string }`
- Only reference/header information is updated — does NOT support full GRN editing

**Repository and Database Update Logic:**
- Repository method `UpdateGRNReference(dto)` updates only the 3 reference fields in the GRN header
- Uses JWT-authenticated user for `LastUpdatedBy`, never trusts client-supplied value
- No stock ledger or PO quantity changes

**Validation and Permission Checks:**
- Button only shown when `isView && !isEditingRef && (userAccessModel.updateAccess || currentUser === 'superadmin')`
- Form is in view mode (`isView = true` when loading existing GRN by ID)
- Reference edit does not bypass any document status checks — it only updates 3 fields

**Conclusion:** GRN Edit Reference updates ONLY reference/header information (SupplierInvoiceNo, DeliveryNoteRef, Remarks). It does NOT support full document editing. It's an inline reference edit feature.

---

### B. Purchase Requisition (PR) — Existing Edit Behavior

**Component:** `new-pr.component.ts` and `new-pr.component.html`

**Existing Edit Button:**
- PR list page (`purchase-requisition.component.html`): "View / Edit" button navigates to `/inventory/purchase-requisition/new-pr, row.ID`
- The button is always visible when `userAccessModel.updateAccess || currentUser === 'superadmin'`
- Uses `<mat-icon>edit</mat-icon>` with tooltip "View / Edit"

**Draft and Non-Draft Status Handling:**
- When loading existing PR (`loadExisting()`), if `d.Status !== 'Draft'`, the form is disabled: `this.frm.disable();` (line 198)
- `canEdit()` method: `return !this.isEdit || this.currentStatus === 'Draft';` — only Draft PRs can be edited
- Save Draft button only visible when `canEdit()` is true
- Non-Draft statuses (Submitted, Approved, Rejected, PO Posted, GRN Posted) cannot be edited
- The form shows status banners explaining the current state

**PR Item Details and Quantity Validation:**
- Lines include: ItemID, RequiredQty, BuyingPrice, SSTPercent, UOMID, Remarks
- `canSubmit()`: `return this.lines.length > 0 && (!this.isEdit || this.currentStatus === 'Draft');`
- SST calculation: `subTotal + sstTotal = grandTotal`
- Item validation: RequiredQty must be > 0, at least one line required

**Save API and Repository Update Logic:**
- `savePR(body)` — POST to `Inventory/SavePR`
- Repository `SavePR(dto)` checks:
  - At least one detail line required
  - All RequiredQty > 0
  - On edit (ID != 0): confirms PR Status is "Draft", otherwise returns error "This purchase requisition has already been {status} and cannot be edited."
  - Generates PRNumber on new PR save
  - Uses transaction for multi-table updates
  - On edit: removes existing detail lines before re-adding new ones
  - Preserves LASTUPDATE/LastUpdatedBy audit fields

**Linked Purchase Order Dependencies:**
- When PR status is 'Approved', backend checks if a PO has already been created: `service.getPOByPRID(+this.ID)`
- If `linkedPO` exists, status banner shows "PO Already Posted" with link to open the PO
- PR deletion warns if DisplayStatus shows 'PO Posted' or 'GRN Posted'
- `canCreatePO()`: `return this.isEdit && this.currentStatus === 'Approved';` — can only create PO from Approved PR

**Existing Approval Workflow:**
- `onSubmitForApproval()` — submits PR for approval
- `onApprove()` — approves PR (configured approver or superadmin)
- `onReject()` — rejects PR with reason
- After approval, PR status changes to 'Approved', PO can be created

**Conclusion:** PR editing is currently limited to Draft status only. The "View/Edit" button navigates to the form but fields are disabled for non-Draft statuses. There is no separate "Edit Reference" mode — the entire document is editable when in Draft.

---

### C. Purchase Order (PO) — Existing Edit Behavior

**Component:** `new-po.component.ts` and `new-po.component.html`

**Existing Edit Button:**
- PO list page (`purchase-order.component.html`): "View / Edit" button navigates to `/inventory/purchase-order/new-po, row.ID`
- The button is visible when `userAccessModel.updateAccess || currentUser === 'superadmin'`

**GetPOEditability or Equivalent Editability Checks:**
- On PO form load, `loadExisting()` calls `service.getPOEditability(+this.ID)` (line 285)
- Backend returns `POEditabilityDto`: `{ CanEdit, IsPRLinked, LinkedPRNumber, Status, BlockReason }`
- Frontend uses these to configure field locks and edit mode
- Form is initially disabled (`this.frm.disable()`) and re-enabled when `canEditPO` is true

**Draft Status Restrictions:**
- Draft POs are always editable (`canEdit = true` when status is Draft)
- Approved POs are editable ONLY if no active GRN exists
- Any other status (Partially Received, Fully Received, Cancelled, Rejected) is blocked

**GRN-Linked PO Restrictions:**
- If a GRN has been posted for the PO, editing is blocked
- `GetPOEditability` checks for active GRN and blocks edit if one exists
- Error message: "Goods Receipt XX has been posted for this PO. Delete the GRN first to re-enable editing."
- PO with GRN cannot be edited — must delete GRN first

**PR-Linked PO Item Restrictions:**
- If PO was created from a PR (`existing.PRID.HasValue`), the item list is locked
- Only Quantities and Unit Prices can be edited per line
- Items cannot be added, removed, or swapped
- Backend validation in `SavePO`: compares existing item IDs with incoming item IDs, returns error if they differ
- UI shows "Item List Locked" banner with explanation
- Inline inputs for Qty and UnitPrice are shown when `isPRLinked && canEdit()`

**Quantity, Unit Price and SST Editing:**
- When `isPRLinked && canEdit()`: Qty and UnitPrice have inline editable inputs
- SST% can also be edited inline when PR-linked
- Line totals are recalculated on every change via `calcLineTotal()` and `calcTotal()`
- When not PR-linked: full line editing is available (Item selection, Qty, Price, SST)

**Existing PO Update API and Repository Logic:**
- `savePO(body)` — POST to `Inventory/SavePO`
- Repository `SavePO(dto)` checks:
  - At least one detail line required
  - All OrderedQty > 0
  - UnitPrice cannot be negative
  - On edit (ID != 0):
    - Status must be "Draft" or "Approved" (otherwise blocked)
    - Checks for active GRN (blocks if exists)
    - If PR-linked: validates item list hasn't changed (only Qty/Price allowed)
    - Validates linked PR if provided (must be Approved)
    - Duplicate PO guard (new PO must not have same PONumber as existing active PO)
    - Uses transaction for multi-table updates
    - Preserves audit fields (LASTUPDATE, LastUpdatedBy)

---

## Summary of Existing Behavior

| Feature | GRN | PR | PO |
|---------|-----|-----|-----|
| Edit Reference (reference-only) | ✅ 3 fields (Invoice, Note, Remarks) | ❌ Not supported | ❌ Not supported |
| Edit entire document | ❌ Only New mode | ✅ Draft only | ✅ Draft or Approved (no GRN) |
| Status restrictions | Reference edit only, no status check | Draft only | Draft always, Approved only if no GRN |
| PO-linked restrictions | N/A | PO Posted/GRN Posted blocks delete | GRN blocks edit; PR-linked locks items |
| Save/Cancel in edit mode | Save Reference / Cancel Edit | Save Draft / Cancel (navigate back) | Save Draft / Cancel |
| API: reference update | `POST /api/Inventory/UpdateGRNReference` | `POST /api/Inventory/SavePR` | `POST /api/Inventory/SavePO` |
| API: editability check | N/A | Backend checks Draft status | `GET /api/Inventory/GetPOEditability` |
| Totals recalculation | N/A (reference only) | `calcTotal()` on line changes | `calcTotal()` / `calcLineTotal()` on every change |

## Phase 1 — Missing Functionality

1. **PR**: No dedicated "Edit Reference" mode. The entire document is edited when in Draft, but there's no selective reference edit feature matching the GRN pattern.

2. **PO**: Existing edit functionality works but needs UI/UX consistency with GRN reference design (clear Edit button, Save/Cancel pattern, reference field handling).

3. **Both**: Consistent success/error notification patterns, button placements, and form validation following OBMS theme.

## Phase 2 — PR Edit Functionality Implementation Plan

### Changes Required:

1. **PR List Page** (`purchase-requisition.component.html`):
   - Add dedicated "Edit" button alongside existing "View/Edit" 
   - Show Edit only when PR is in Draft status
   - Edit navigates to new-pr with the PR ID in edit mode

2. **PR Form** (`new-pr.component.ts` / `new-pr.component.html`):
   - Add `isEditingRef` state and reference field editing (matching GRN pattern)
   - Add `startEditRef()`, `cancelEditRef()`, `saveRef()` methods
   - Add reference field inline editing for SupplierInvoiceNo, DeliveryNoteRef, Remarks
   - Buttons: "Edit Reference", "Save Reference", "Cancel Edit" (visible in view mode)
   - Keep existing Save Draft, Submit, Approve/Reject actions

3. **PR Restrictions:**
   - Edit button only visible when `currentStatus === 'Draft'`
   - If PR is linked to a PO (DisplayStatus === 'PO Posted' or 'GRN Posted'), disable editing and show warning
   - Preserve PR number — never allow PRNumber modification
   - If PO already created from PR, prevent changes to item list that would invalidate the PO

### APIs and Repositories Reused:
- `savePR()` — existing API, no changes needed
- `GetPOEditability()` — not needed for PR, but status check is done in repository
- Repository `SavePR(dto)` already handles Draft status validation

---

## Phase 3 — PO Edit Functionality Implementation Plan

### Changes Required:

1. **PO Form** (`new-po.component.ts` / `new-po.component.html`):
   - Improve editability UI to match GRN reference design
   - Add clear "Edit" action with consistent button placement
   - Reference field editing (SupplierInvoiceNo, DeliveryNoteRef, Remarks) — matching GRN pattern
   - Save/Cancel buttons with consistent styling
   - Preserve PO number, PR reference, supplier, warehouse identity

2. **PO Editability Enhancements:**
   - Already has `GetPOEditability` — ensure frontend respects `canEditPO` and `isPRLinked`
   - Add reference field editing when edit is permitted
   - When `isPRLinked` is true: only Qty, UnitPrice, SST editable; items locked
   - When not PR-linked: full editing permitted (within status restrictions)

3. **PO Restrictions:**
   - Preserve existing `GetPOEditability` checks
   - Do not allow editing if GRN exists (already implemented)
   - For PR-linked POs: preserve item list, only allow Qty/Price/SST changes
   - Do not reset received quantities or overwrite GRN relationships
   - Preserve supplier, warehouse references

### APIs and Repositories Reused:
- `getPOEditability()` — already implemented, no changes needed
- `savePO()` — already implemented, no changes needed  
- `UpdateGRNReference()` — can be reused for PO reference editing if needed
- Repository `SavePO(dto)` already handles all dependency checks

---

## Phase 4 — Common UI/UX Pattern (GRN Reference)

**Visual Pattern to Follow:**
- Maroon and white theme (OBMS theme colors)
- Consistent Edit button placement (top-right or per-row)
- Clear read-only vs edit mode distinction
- Save and Cancel buttons grouped together
- Proper form validation with error messages
- SweetAlert/toast notifications (existing pattern: `toast: true, position: 'top', showConfirmButton: false, timer: 8000`)
- Responsive layout using mat-card, row/col grid

**Specific Patterns from GRN:**
- Edit Reference button: `btn btn-outline-primary mr-2` with mat-icon edit
- Save Reference button: `btn wlt-c-furg-btn-primary mr-2` with mat-icon save
- Cancel Edit button: `btn btn-outline-secondary mr-2` with mat-icon close
- Success toast: `width: '750px', timer: 8000`
- Reference tooltip: "Edit Supplier Invoice No, Delivery Note and Remarks only. Quantities and stock are not changed."

---

## Phase 5 — Database and API Safety

- Reuse existing `savePR()` and `savePO()` APIs — no new APIs needed
- Repository update logic already uses transactions
- Preserve document numbers (PRNumber, PONumber) — never editable
- Preserve audit fields (LASTUPDATE, LastUpdatedBy) — stamped by backend via JWT
- Prevent duplicate PR/PO item rows — handled by repository
- Preserve downstream GRN, stock ledger, approval relationships — existing checks remain
- No new database tables or migrations needed
- Do not modify Finance-owned tables or business logic