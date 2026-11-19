# OBMS Inventory Module — Quick Reference Guide

**Company:** Freightwatch G Patomotoh Security Services Sdn. Bhd.
**Version:** 1.0 | **Date:** November 2026

---

## Master Data Setup (Do This First)

| Screen | Path | Key Fields | Save Button |
|--------|------|------------|-------------|
| Category Master | Finance → Category Master | Name*, Type (P/A) | Save |
| Brand Master | Inventory → Brand Master | Name* | Save |
| UOM Master | Inventory → UOM Master | Name*, Short Name | Save |
| Warehouse Master | Inventory → Warehouse Master | Name*, Branch* | Save |
| Item Master | Inventory → Item Master | Category*, UOM*, Name*, Price* | Save |
| Supplier Master | Finance → Supplier Master | Name* | Save |
| Asset Master | Inventory → Asset Master | Name*, Type*, Branch* | Save |

> **Setup order:** Category → Brand → UOM → Warehouse → Item → Supplier

---

## Opening Stock

| Step | Action |
|------|--------|
| 1 | Inventory → Opening Stock → + New |
| 2 | Select Warehouse* and Item* |
| 3 | Enter Opening Qty* and Unit Cost* |
| 4 | Total Value auto-calculates |
| 5 | Click Save |

> One opening stock per item per warehouse. Edit existing — do not duplicate.

---

## Purchase Workflow

### Purchase Requisition (PR)

| Step | Action | Button |
|------|--------|--------|
| 1 | Inventory → Purchase Requisition → + New | |
| 2 | Enter PR Date* and Required By Date* | |
| 3 | Add items: select Item, enter Required Qty | Add |
| 4 | Save as Draft | Save Draft |
| 5 | Send for approval | Submit for Approval |

**Statuses:** Draft → Submitted → Approved / Rejected

### Purchase Order (PO)

| Step | Action | Button |
|------|--------|--------|
| 1 | Inventory → Purchase Order → + New | |
| 2 | Select Supplier* and Warehouse* | |
| 3 | Select approved PR (optional — items auto-fill) | |
| 4 | Enter Ordered Qty and Unit Price per line | Add |
| 5 | Total Amount auto-calculates | |
| 6 | Save as Draft | Save Draft |
| 7 | Send for approval | Submit for Approval |

**Statuses:** Draft → Submitted → Approved → Partially Received → Fully Received

### Goods Receipt (GRN)

| Step | Action | Button |
|------|--------|--------|
| 1 | Inventory → Goods Receipt → + New | |
| 2 | Select Approved PO* (items auto-load) | |
| 3 | Enter Supplier Invoice No and Delivery Note Ref | |
| 4 | Enter Receiving Qty per line (cannot exceed pending) | |
| 5 | Save | Save GRN |

> **On save:** Stock increases + Finance CreditorInvoice created.
> **Delete:** Blocked if payment or purchase return exists.

---

## Stock Operations

### Stock Issue

| Step | Action |
|------|--------|
| 1 | Inventory → Stock Issue → + New |
| 2 | Select Warehouse* and Issue To Type* |
| 3 | Select recipient (Staff/Guard/Branch/Other) |
| 4 | Add items: Item, Issue Qty (≤ available stock), Client* |
| 5 | Click Save, then Print PDF |

### Stock Issue Return

| Step | Action |
|------|--------|
| 1 | Inventory → Issue Return → + New |
| 2 | Select Original Issue |
| 3 | Enter Return Qty per line (≤ returnable qty) |
| 4 | Click Save |

> **Cannot be deleted or edited after saving.**

### Stock Transfer

| Step | Action |
|------|--------|
| 1 | Inventory → Stock Transfer → + New |
| 2 | Select Source Warehouse* and Destination Warehouse* |
| 3 | Add items: Item, Transfer Qty (≤ available stock) |
| 4 | Click Save |

### Stock Adjustment

| Step | Action |
|------|--------|
| 1 | Inventory → Stock Adjustment → + New |
| 2 | Select Warehouse*, Item*, Adjustment Type (PLUS/MINUS) |
| 3 | Enter Adjustment Qty* and Reason* |
| 4 | Save Draft → Submit for Approval |

> **Stock changes only on Approval, not on Save.**

### Damage / Loss

| Step | Action |
|------|--------|
| 1 | Inventory → Damage / Loss → + New |
| 2 | Select Warehouse*, Item*, Loss Type (Damage/Loss) |
| 3 | Enter Quantity* and Reason* |
| 4 | Click Save (stock reduces immediately) |

### Branch Stock Request (BSR)

| Step | Action |
|------|--------|
| 1 | Inventory → Branch Stock Request → + New |
| 2 | Select Requesting Branch* and Source Warehouse* |
| 3 | Add items: Item, Requested Qty, Client |
| 4 | Save Draft → Submit for Approval |
| 5 | When Approved: click Create Transfer |

---

## Asset Management

| Screen | Purpose | Key Action |
|--------|---------|------------|
| Asset Master | Register assets | Save (status: Available) |
| Asset Allocation | Assign asset to person/branch | Save (status: Allocated) |
| Asset Transfer | Move asset between holders | Save (event record) |
| Asset Return | Return asset to pool | Save (status: Available) |
| Asset Maintenance | Schedule repair | Save → Complete when done |
| Asset Damage | Record damage | Save (status: Damaged) |
| Asset Disposal | Dispose asset | Save Draft → Submit → Approve (status: Disposed, permanent) |
| Asset Location | Track physical location | Save (logs change) |
| Movement History | View full lifecycle | Read-only |

---

## Approval Quick Reference

| Document | Approval Required? | Who Approves? |
|----------|-------------------|---------------|
| Purchase Requisition | Yes | Configured approver or SuperAdmin |
| Purchase Order | Yes | Configured approver or SuperAdmin |
| Branch Stock Request | Yes | Configured approver or SuperAdmin |
| Stock Adjustment | Optional | Configured approver (if enabled) |
| Stock Transfer | Optional | Configured approver (if enabled) |
| Damage / Loss | Optional | Configured approver (if enabled) |
| Asset Disposal | Optional | Configured approver (if enabled) |

**To approve:** Open document → click Approve → enter remarks
**To reject:** Open document → click Reject → enter rejection reason (mandatory)
**Pending approvals:** Administration → Pending Approvals

---

## Reports Quick Reference

| Report | Filters | Output |
|--------|---------|--------|
| Stock Ledger Report | Warehouse*, Date Range*, Item, Type, Category | PDF + Excel |
| Current Stock Report | Warehouse, Item, Category | PDF + Excel |
| Stock Movement Report | Warehouse, Item, Date Range, Client | PDF + Excel |
| Low Stock Report | Warehouse, Category | PDF + Excel |
| Inventory Valuation | Warehouse, Category, As Of Date | PDF + Excel |
| GRN Voucher | GRN ID (from view screen) | PDF only |
| PO Voucher | PO ID (from view screen) | PDF only |
| PR Voucher | PR ID (from view screen) | PDF only |
| Stock Issue Voucher | Issue ID (from view screen) | PDF only |
| Asset Allocation Voucher | Allocation ID (from print icon) | PDF only |

> **To generate:** Open report screen → select filters → click Search (PDF) or Export Excel

---

## Common Error Quick Fixes

| Error | Fix |
|-------|-----|
| Required field missing | Fill all fields marked with * |
| Insufficient stock | Reduce quantity or receive stock via GRN first |
| Cannot delete item | Item has transactions — edit instead |
| Cannot delete GRN | Payment or return exists — reverse those first |
| Cannot submit document | Only Draft can be submitted |
| Report not opening | Contact admin — check report server + DLLs |
| No permission to view | Contact admin to grant access rights |
| Duplicate opening stock | Edit existing entry, do not create new |

---

## Stock Ledger Transaction Types

| Code | Label | Effect |
|------|-------|--------|
| OPENING | Opening Stock | Stock IN (+) |
| STOCK_IN | Stock In / GRN | Stock IN (+) |
| ISSUE | Stock Issue | Stock OUT (-) |
| ISSUE_RETURN | Issue Return | Stock IN (+) |
| TRANSFER_IN | Transfer In | Stock IN (+) |
| TRANSFER_OUT | Transfer Out | Stock OUT (-) |
| ADJ_PLUS | Adjustment (In) | Stock IN (+) — on Approve only |
| ADJ_MINUS | Adjustment (Out) | Stock OUT (-) — on Approve only |
| PURCHASE_RTN | Purchase Return | Stock OUT (-) |
| DAMAGE_LOSS | Damage / Loss | Stock OUT (-) |

> **Balance = SUM(InQty) - SUM(OutQty)** per item per warehouse

---

*Quick Reference Guide — OBMS Inventory Module v1.0 — November 2026*
