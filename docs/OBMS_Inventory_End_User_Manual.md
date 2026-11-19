# OBMS Inventory Module — End User Manual

**Company:** Freightwatch G Patomotoh Security Services Sdn. Bhd.
**System:** OBMS (Online Business Management System)
**Module:** Inventory Management
**Version:** 1.0
**Date:** November 2026

---

## Revision Information

| Version | Date | Description | Prepared By |
|---------|------|-------------|------------|
| 1.0 | November 2026 | Initial End User Manual | OBMS Development Team |

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Master Data Entry Guide](#2-master-data-entry-guide)
3. [Opening Stock Entry](#3-opening-stock-entry)
4. [Purchase Process — Step by Step](#4-purchase-process--step-by-step)
5. [Stock Management](#5-stock-management)
6. [Asset Management User Guide](#6-asset-management-user-guide)
7. [Reports User Guide](#7-reports-user-guide)
8. [Approval and Notifications](#8-approval-and-notifications)
9. [Daily Inventory User Workflow](#9-daily-inventory-user-workflow)
10. [Troubleshooting Guide](#10-troubleshooting-guide)
11. [Glossary](#11-glossary)

---

## 1. Getting Started

### 1.1 How to Log In to OBMS

1. Open your web browser (Google Chrome or Microsoft Edge recommended).
2. Enter the OBMS web address provided by your administrator.
3. On the login page, enter your **Username** and **Password**.
4. Click the **Login** button.
5. If your credentials are correct, you will see the main dashboard.

> **Note:** If you see "Access Denied" or cannot log in, contact your system administrator to verify your user account and permissions.

### 1.2 How to Open the Inventory Module

After logging in, the left sidebar shows all available modules. The **Inventory** section contains all inventory-related screens organized into groups.

1. Look at the **left sidebar** of the screen.
2. Find the **Inventory** menu heading.
3. Click on it to expand the inventory sub-menu.
4. You will see two groups:
   - **Main Menu** — operational screens (Item Master, Purchase, Stock Operations, Asset Management)
   - **Reports** — report generation screens

### 1.3 Inventory Menu Navigation

**Main Menu:**

| Menu Item | Purpose |
|-----------|---------|
| Item Master | Manage all inventory items |
| Brand Master | Manage item brands |
| UOM Master | Manage units of measure |
| Warehouse Master | Manage warehouses |
| Opening Stock | Record initial stock quantities |
| Purchase Requisition | Create purchase requests |
| Purchase Order | Create and manage purchase orders |
| Goods Receipt | Receive goods against purchase orders |
| Purchase Return | Return goods to suppliers |
| Branch Stock Request | Branch requests for stock from HQ |
| Stock Transfer | Move stock between warehouses |
| Stock Issue | Issue stock to staff, guards, or branches |
| Issue Return | Record returns of issued stock |
| Stock Adjustment | Adjust stock for count discrepancies |
| Damage / Loss | Record damaged or lost items |
| Asset Master | Register and manage assets |
| Asset Allocation | Allocate assets to people or branches |
| Asset Transfer | Transfer assets between holders |
| Asset Return | Record asset returns |
| Asset Maintenance | Schedule and track maintenance |
| Asset Damage | Record asset damage |
| Asset Disposal | Dispose of assets with approval |
| Asset Location | Track asset locations |

**Reports Sub-Menu:**

| Menu Item | Purpose |
|-----------|---------|
| Stock Ledger Report | Detailed stock movement history (PDF/Excel) |
| Current Stock Report | Current stock levels across warehouses |
| Stock Movement Report | Inward vs outward movement summary |
| Stock In (GRN) Report | Goods received report |
| Stock Issue Report | List of all stock issues |
| Item & Category Stock | Stock by item and category |
| Warehouse Stock Report | Stock by warehouse |
| Supplier Purchase Report | Purchases by supplier |
| Low Stock Report | Items at or below reorder level |
| Stock Transfer Report | Transfer history |
| Stock Adj & Damage Loss | Adjustments and damage/loss summary |
| Purchase & Issue Return | Purchase and issue return summary |
| Inventory Valuation | Stock value calculation |
| Branch Asset Report | Assets by branch |
| Asset Movement Report | Asset allocation/transfer history |
| Stock Ledger | On-screen stock ledger view (with print) |
| Stock Balance Summary | Quick stock balance overview |
| Staff Item Holding | Items currently held by staff |
| Staff Issue Register | Issue history by staff |

### 1.4 Dashboard Overview

The **Inventory Dashboard** provides a quick overview of your inventory status.

**Dashboard shows:**

| Section | What It Displays |
|---------|-----------------|
| KPI Cards (top row) | Total Items, Total Stock Value (RM), Total Stock Quantity |
| KPI Cards (second row) | Pending Transfers, Pending Issues, Low Stock Items, Pending Approvals |
| Donut Chart | Stock value distribution across warehouses |
| Bar Chart | Monthly inward vs outward quantities (5-day intervals) |
| Low Stock Alerts | Top 20 items at or below reorder level |
| Pending Activities | Counts of POs pending, GRNs pending, transfers pending, issues this month, adjustments pending |
| Recent Transactions | Last 20 stock movements |
| Item/Warehouse Table | Stock quantity pivoted by item and warehouse |

### 1.5 How to Identify Available Actions and Buttons

| Button Style | Appearance | Action |
|--------------|------------|--------|
| **Primary** | Dark navy blue | Save, Submit |
| **Gold** | Gold/yellow | Submit for Approval, Print PDF |
| **Outline** | Navy border, white background | Cancel, Export Excel |
| **Delete** | Red text or icon | Delete (with confirmation dialog) |
| **Edit** | Pencil icon | Open record for editing |

### 1.6 How to Navigate Between Screens

- Click any sidebar menu item to open that screen.
- On list screens, click **+ New** to create a new record.
- On list screens, click the **Edit** (pencil) icon to edit an existing record.
- Click **Cancel** to return to the list without saving.
- After saving, the system automatically returns to the list screen.

---

## 2. Master Data Entry Guide

Before recording any stock transactions, you must set up your master data. The recommended setup order is:

1. **Category Master** (classifies your items)
2. **Brand Master** (optional — brands for items)
3. **UOM Master** (units of measure like Nos, Pairs, Boxes)
4. **Warehouse Master** (your storage locations)
5. **Item Master** (your inventory items — depends on Category, Brand, UOM, Warehouse)
6. **Supplier Master** (your vendors for purchasing)
7. **Asset Master** (for fixed assets like vehicles, equipment)

> **Note:** Category Master and Supplier Master appear under the **Finance** section in the sidebar, not under Inventory.

---

### 2.1 Category Master

**How to open:** Sidebar → Finance → Category Master

**Purpose:** Categories classify your inventory items (e.g., Uniform, Equipment, Stationery). Each category has a type (P for Product, A for Asset) which determines whether items appear in inventory or asset screens.

**How to create a new record:**

1. Click **+ New** on the Category Master list screen.
2. Fill in the form fields (see below).
3. Click **Save** to save the category.
4. A success message appears and the list refreshes.

**Field-by-field explanation:**

| Field | Required? | Description | Sample Value |
|-------|-----------|-------------|--------------|
| Code | Auto | System-generated code (read-only) | CAT0001 |
| Name | Yes | Category name | Uniform |
| Type | Yes | "P" for Product (inventory items) or "A" for Asset | P |
| Parent Category | No | Select a parent category for sub-categories | — |
| Remarks | No | Additional notes | Security uniforms |

**How to edit:** Click the edit (pencil) icon next to a category in the list. Modify fields and click Save.

**How to delete:** Click the delete (trash) icon. A confirmation dialog appears. Click **Confirm** to delete. The system blocks deletion if the category is referenced by existing items.

**Common validation errors:**

| Error | Solution |
|-------|----------|
| "Name is required" | Enter a category name |
| "Cannot delete — category is in use" | Remove or reassign all items using this category first |

---

### 2.2 Brand Master

**How to open:** Sidebar → Inventory → Brand Master

**Purpose:** Record item brands (e.g., FWG Standard, Nike, Adidas). Brands are optional on items.

**Field-by-field explanation:**

| Field | Required? | Description | Sample Value |
|-------|-----------|-------------|--------------|
| Code | Auto | System-generated (read-only) | BR0001 |
| Name | Yes | Brand name | FWG Standard |
| Description | No | Additional description | Standard uniform brand |

**Edit and Delete:** Same pattern as Category Master.

---

### 2.3 UOM Master (Unit of Measure)

**How to open:** Sidebar → Inventory → UOM Master

**Purpose:** Define units of measure for items (e.g., Nos, Pairs, Boxes, Sets). The system comes with 9 pre-loaded UOMs.

| Field | Required? | Description | Sample Value |
|-------|-----------|-------------|--------------|
| Code | Auto | System-generated (read-only) | UM0010 |
| Name | Yes | Full unit name | Pairs |
| Short Name | No | Abbreviation | Pr |
| Is Primary | No | Check if this is a base unit | — |

**Sample values:** Nos, Pairs, Boxes, Sets, Rolls, Bottles, Units, Packs, Cartons

---

### 2.4 Warehouse Master

**How to open:** Sidebar → Inventory → Warehouse Master

**Purpose:** Define your warehouse/storage locations. Each warehouse is linked to a branch. Stock balances are tracked per warehouse per item.

| Field | Required? | Description | Sample Value |
|-------|-----------|-------------|--------------|
| Code | Auto | System-generated (read-only) | WH0001 |
| Name | Yes | Warehouse name | Main Store — Seremban |
| Address | No | Warehouse address | No. 24, Taman Bukit Emas |
| Branch | Yes | Select the branch this warehouse belongs to | Seremban HQ |
| IsActive | Yes | Uncheck to hide from dropdowns | Checked |

> **Important:** Only active warehouses appear in dropdowns on transaction screens. Deactivate a warehouse instead of deleting it if it has existing stock.

---

### 2.5 Item Master

**How to open:** Sidebar → Inventory → Item Master

**Purpose:** Create and manage your inventory items. Each item has a category, optional brand, UOM, and optional default warehouse. The Price field represents the buying/cost price.

**How to create a new record:**

1. Click **+ New** on the Item Master list.
2. The Item Code is auto-generated (e.g., ITM0001) and cannot be edited.
3. Select the **Category Type** (P for Product or A for Asset) to load the relevant categories.
4. Fill in all required fields.
5. Click **Save**.

**Field-by-field explanation:**

| Field | Required? | Description | Sample Value |
|-------|-----------|-------------|--------------|
| Code | Auto | System-generated (read-only) | ITM0042 |
| Category | Yes | Select item category (filtered by type) | Uniform |
| Brand | No | Select brand (optional) | FWG Standard |
| UOM | Yes | Select unit of measure | Nos |
| Default Warehouse | No | Default warehouse for this item | Main Store |
| Item Name | Yes | Name of the item | Security Uniform Shirt (L) |
| Price | Yes | Cost/buying price (auto-fills on PR, PO, GRN) | 45.00 |
| Reorder Level | No | Stock level that triggers low-stock alert | 50 |
| Reorder Quantity | No | Suggested quantity to reorder | 200 |
| Size | No | Size designation (S, M, L, XL, 42, etc.) | L |
| Remarks | No | Additional notes | For new recruits |

> **Note:** SellPrice and Quantity fields exist in the database but are intentionally excluded from the user interface. Stock quantity is always calculated from the Stock Ledger.

**Sample entry:**

```
Code:           ITM0042 (auto)
Category:       Uniform
Brand:          FWG Standard
UOM:            Nos
Warehouse:      Main Store — Seremban
Item Name:      Security Uniform Shirt (L)
Price:          45.00
Reorder Level:  50
Reorder Qty:    200
Size:           L
```

**How to edit:** Click the edit (pencil) icon in the list. All fields except Code can be modified.

**How to delete:** Click the delete icon. The system blocks deletion if the item is referenced in any transaction.

**Common validation errors:**

| Error | Solution |
|-------|----------|
| "Category is required" | Select a category from the dropdown |
| "UOM is required" | Select a unit of measure |
| "Name is required" | Enter an item name |
| "Price is required" | Enter the cost price |
| "Cannot delete — item is in use" | The item has transactions. Edit it instead |

**What to do after saving:** The item is now available in all transaction dropdowns. Proceed to record Opening Stock for this item.

---

### 2.6 Supplier Master

**How to open:** Sidebar → Finance → Supplier Master

**Purpose:** Record supplier/vendor details for purchasing. Suppliers are used in Purchase Orders and linked to Finance payments.

| Field | Required? | Description | Sample Value |
|-------|-----------|-------------|--------------|
| Code | Auto | System-generated (read-only) | SUP001 |
| Name | Yes | Supplier name | ABC Uniform Supplier Sdn Bhd |
| Category | No | Category this supplier serves | Uniform |
| Pay To | No | Payment recipient name | ABC Uniform Sdn Bhd |

---

### 2.7 Recipient Master

**How to open:** Accessible via direct URL if enabled by administrator (hidden in sidebar by default)

**Purpose:** Record recipient types for stock issue transactions.

| Field | Required? | Description |
|-------|-----------|-------------|
| Code | Auto | System-generated (read-only) |
| Name | Yes | Recipient name |
| Type | Yes | Recipient type |

> **Note:** This screen is hidden in the sidebar by default. Your administrator can make it visible if needed.

---

## 3. Opening Stock Entry

### 3.1 Why Opening Stock Is Required

Opening Stock records the initial quantity of each item in each warehouse before you start regular transactions. Every opening stock entry creates a permanent record in the Stock Ledger (type: OPENING), which becomes the starting balance for that item.

> **Rule:** Only one opening stock entry is allowed per item per warehouse. If you need to change it, edit the existing entry rather than creating a new one.

### 3.2 How to Create an Opening Stock Entry

**How to open:** Sidebar → Inventory → Opening Stock → Click **+ New**

**Step-by-step:**

1. The **Reference No** is auto-generated (e.g., OS0001) — do not edit.
2. Select the **Opening Date** — defaults to today.
3. Select the **Warehouse** (required) — only active warehouses appear.
4. Select the **Item** (required) — searchable dropdown with all items.
5. When you select an item, the system auto-displays its Code, Category, Brand, and UOM below the dropdown.
6. The **Unit Cost** auto-fills from the item's Price field — you can override it.
7. Enter the **Opening Qty** (must be greater than zero).
8. The **Total Value** auto-calculates as Opening Qty × Unit Cost (read-only).
9. Enter **Remarks** if needed.
10. Click **Save**.

**Illustration — Opening Stock Form:**

```
*Illustration — Not an Actual Application Screenshot*

┌──────────────────────────────────────────────────────┐
│  New Opening Stock                                    │
├──────────────────────────────────────────────────────┤
│  Reference No:  [OS0001]  (auto, read-only)          │
│  Opening Date:  [01-Nov-2026]                    *   │
│                                                       │
│  Warehouse:     [Main Store — Seremban]  ▼       *   │
│  Item:          [ITM0042 — Uniform Shirt (L)] ▼  *   │
│                                                       │
│  ┌── Item Details ──────────────────────────────┐    │
│  │ Code: ITM0042  Category: Uniform             │    │
│  │ Brand: FWG Standard   UOM: Nos               │    │
│  └───────────────────────────────────────────────┘   │
│                                                       │
│  Opening Qty:   [500]                            *    │
│  Unit Cost:     [45.00]                          *    │
│  Total Value:   22,500.00  (auto-calculated)         │
│  Remarks:       [Initial stock for 2026]              │
│                                                       │
│              [Cancel]    [Save Opening Stock]         │
└──────────────────────────────────────────────────────┘
  * = Required field
```

### 3.3 Sample Entry

| Field | Value |
|-------|-------|
| Reference No | OS0001 (auto) |
| Opening Date | 01-Nov-2026 |
| Warehouse | Main Store — Seremban |
| Item | ITM0042 — Security Uniform Shirt (L) |
| Opening Qty | 500 |
| Unit Cost | 45.00 |
| Total Value | 22,500.00 (auto) |
| Remarks | Initial stock for 2026 |

### 3.4 How to Verify Stock After Saving

After saving, the opening stock quantity appears in the Stock Ledger as an OPENING transaction with InQty = 500. To verify:

1. Go to **Inventory → Stock Ledger**.
2. Select the same warehouse.
3. Set the date range to include the opening date.
4. Click **Search** — you will see the OPENING entry with a running balance of 500.

### 3.5 Bulk Import

The Opening Stock list screen has a **Bulk Import** button that allows importing multiple entries from an Excel file. The file must contain: Item Code, Warehouse Code, Opening Qty, Unit Cost, Opening Date. The system resolves codes automatically and reports per-row success/failure.

### 3.6 Editing and Deleting

- **Edit:** Click the edit icon on the list. Modify any field and save. The Stock Ledger OPENING row is updated in place.
- **Delete:** Soft-deletes the record and removes the corresponding OPENING ledger row.
- **Caution:** If stock has already been issued or transferred from this warehouse, editing the opening quantity downward could produce a negative running balance. Always check current stock before reducing an opening quantity.

---

## 4. Purchase Process — Step by Step

The purchase process follows this workflow:

```
Purchase Requisition (PR) → Approval → Purchase Order (PO) → Approval → Goods Receipt (GRN)
```

Each step depends on the previous one. You cannot create a PO from an unapproved PR, and you cannot receive goods (GRN) against an unapproved PO.

---

### 4.1 Purchase Requisition (PR)

**How to open:** Sidebar → Inventory → Purchase Requisition → Click **+ New**

**Purpose:** A Purchase Requisition is a formal request to purchase items. It must be submitted and approved before a Purchase Order can be created.

#### Creating a PR — Step by Step

1. The **PR Number** is auto-generated (e.g., PR0012) — read-only.
2. Select the **PR Date** (defaults to today).
3. Select the **Required By Date** — the date by which you need the items.
4. Enter **Remarks** if needed.

#### Adding Line Items

5. In the line item section, click **Add Item**.
6. Select an **Item** from the searchable dropdown.
7. When you select an item, the system auto-fills:
   - **Buying Price** from the item's Price field (you can edit this)
   - **UOM** from the item master
   - **Default Warehouse** name and **Current Stock** (for reference)
   - **Pending BSR Qty** (total demand from branch requests — for reference)
8. Enter the **Required Qty** (must be greater than zero).
9. Optionally enter per-line **Remarks**.
10. Click **Add** to save the line to the grid.
11. Repeat for additional items.

| Field | Required? | Description | Sample |
|-------|-----------|-------------|--------|
| Item | Yes | Select from dropdown | ITM0042 — Uniform Shirt (L) |
| Required Qty | Yes | Quantity needed | 100 |
| Buying Price | No | Auto-filled from item; editable | 45.00 |
| UOM | Auto | From item master | Nos |

#### Saving and Submitting

12. Click **Save Draft** to save the PR in Draft status (you can return to edit it later).
13. To send the PR for approval, click **Submit for Approval**.
14. The PR status changes to **Submitted** and the configured approver receives a notification.

#### Tracking Approval Status

| Status | Meaning |
|--------|---------|
| Draft | Saved but not submitted — you can still edit |
| Submitted | Waiting for approver to review |
| Approved | Approved — ready for PO creation |
| Rejected | Rejected — check rejection remarks, correct, and resubmit |

> **Note:** If no approval routing is configured for your user, the PR auto-approves on submit. If approval is required, only the configured approver (or SuperAdmin) can approve.

#### PR Processing Task (Purchase Department)

When a PR is approved, a **PR Processing Task** is created. Purchase department staff can:
1. View the task in the PR list (shows "Approved — Pending PO" status).
2. Click **Claim** to take ownership of the task.
3. Create a Purchase Order from the approved PR.

> **Note:** Only one person can claim a task at a time. The first person to click Claim wins.

#### Editing a PR

- **Draft PRs** can be fully edited (all header and line fields).
- **Submitted/Approved/Rejected PRs** cannot be edited (form is disabled).
- For non-Draft PRs, an **Edit Reference** button allows updating only Supplier Invoice No, Delivery Note Ref, and Remarks.

#### Deleting a PR

- Only **Draft** PRs can be deleted.
- If a Purchase Order has already been created from the PR, deletion is blocked.

---

### 4.2 Purchase Order (PO)

**How to open:** Sidebar → Inventory → Purchase Order → Click **+ New**

**Purpose:** A Purchase Order is the official order sent to a supplier. It can be created from an approved PR or directly (without a PR).

#### Creating a PO from an Approved PR

1. On the approved PR, click **Create PO** (or navigate to Purchase Order → New and select the PR from the dropdown).
2. The system pre-fills the PR link (PRID) and loads the PR's line items.
3. When a PO is created from a PR, the item list is locked — you can only edit quantities, unit prices, and SST percentages per line.

#### Creating a PO Directly

1. Click **+ New** on the Purchase Order list. Leave the PR field empty.
2. All fields and line items are fully editable.

#### PO Header Fields

| Field | Required? | Description | Sample |
|-------|-----------|-------------|--------|
| PO Number | Auto | System-generated (read-only) | PO0010 |
| PR Link | No | Select approved PR (optional) | PR0012 |
| Supplier | Yes | Select supplier from dropdown | ABC Uniform Sdn Bhd |
| Warehouse | Yes | Select receiving warehouse | Main Store |
| PO Date | Yes | Order date | 10-Nov-2026 |
| Expected Date | Yes | Expected delivery date | 20-Nov-2026 |
| Remarks | No | Additional notes | — |

#### PO Line Items

| Field | Required? | Description | Sample |
|-------|-----------|-------------|--------|
| Item | Yes | Select from dropdown | ITM0042 — Uniform Shirt (L) |
| Ordered Qty | Yes | Quantity to order | 100 |
| Unit Price | Yes | Price per unit | 45.00 |
| SST Percent | No | SST rate (0, 6, or 8) | 0 |
| Line Total | Auto | Qty × Unit Price | 4,500.00 |

The **Total Amount** at the bottom auto-calculates as the sum of all line totals.

#### Saving and Submitting

1. Click **Save Draft** to save without submitting.
2. Click **Submit for Approval** to send to the approver.

#### PO Status Lifecycle

| Status | Can Edit? | Meaning |
|--------|-----------|---------|
| Draft | Yes — all fields | Saved, not submitted |
| Submitted | No | Waiting for approval |
| Approved | Limited (reference edit) | Approved, ready for GRN |
| Partially Received | No | Some goods received via GRN |
| Fully Received | No | All goods received |
| Cancelled | No | Cancelled (blocked if GRN exists) |
| Rejected | No | Rejected by approver |

#### PO Editability Rules

- **Draft with no PR link:** All fields editable.
- **Draft with PR link:** Item list locked — only quantities, prices, and SST can be edited.
- **Approved with no GRN:** Can edit Remarks only (via Edit Reference button).
- **Approved with GRN:** Fully locked — use GRN delete + recreate for corrections.

> **Guard:** If a PO already exists for a PR, the system redirects you to the existing PO instead of creating a duplicate.

---

### 4.3 Goods Receipt Note (GRN)

**How to open:** Sidebar → Inventory → Goods Receipt → Click **+ New**

**Purpose:** A GRN records the actual receipt of goods against an approved Purchase Order. Saving a GRN increases stock in the warehouse and creates a Finance CreditorInvoice (payable record).

> **Important:** GRN is the only inventory transaction that creates a Finance record. The CreditorInvoice appears in the Finance → Payments screen as a payable. Deleting a GRN reverses this and is blocked if a payment has already been made.

#### Creating a GRN — Step by Step

1. The **GRN Number** is auto-generated (e.g., GRN0008).
2. Select an **Approved Purchase Order** from the dropdown (only approved POs appear).
3. When you select a PO, the system loads all PO line items with their ordered and already-received quantities.
4. The **Warehouse** auto-fills from the PO.
5. Enter the **GRN Date** (defaults to today).
6. Enter the **Supplier Invoice No** (the invoice number from the supplier).
7. Enter the **Delivery Note Ref** (the delivery note number from the supplier).
8. Enter **Remarks** if needed.

#### Entering Received Quantities

9. For each PO line item, enter the quantity you are receiving now.
10. The system shows: Ordered Qty, Already Received, and Pending Qty.
11. You cannot receive more than the pending quantity (over-receive is blocked).
12. The **Unit Cost** carries forward from the PO Unit Price (read-only).
13. Line amounts auto-calculate as Received Qty × Unit Cost.

**Sample GRN with partial receipt:**

| # | Item | Ordered | Already Recv | Pending | Receiving Now | Unit Cost | Amount |
|---|------|---------|-------------|---------|---------------|-----------|--------|
| 1 | Uniform Shirt (L) | 100 | 0 | 100 | 50 | 45.00 | 2,250.00 |
| 2 | Uniform Pants (L) | 100 | 0 | 100 | 50 | 55.00 | 2,750.00 |
| 3 | Safety Boots (42) | 50 | 0 | 50 | 50 | 120.00 | 6,000.00 |

In this example, only 50 of 100 shirts and pants are received (partial GRN). The PO status changes to "Partially Received." When the remaining 50 are received in a second GRN, the PO status changes to "Fully Received."

#### Saving the GRN

14. Click **Save GRN**.
15. On save, the system:
    - Creates STOCK_IN ledger entries (stock increases for each item)
    - Updates the PO's received quantities and status
    - Creates a CreditorInvoice in Finance (appears as a payable)
    - Checks for low-stock alerts on received items

#### After Saving

- Stock is immediately available in the warehouse.
- The GRN appears in the Goods Receipt list.
- The Finance department sees the supplier invoice as a payable in the Payments screen.
- You can print a GRN voucher (PDF) from the list or view screen.

#### GRN Reference Edit

For posted GRNs, you can edit only the Supplier Invoice No, Delivery Note Ref, and Remarks using the **Edit Reference** button. Stock quantities and finance records cannot be changed. To correct a quantity error, delete the GRN and recreate it.

#### Deleting a GRN

- The system checks for dependencies:
  - **Blocked** if a Finance payment has been made against this GRN's CreditorInvoice.
  - **Blocked** if a Purchase Return has been created against this GRN.
- If no dependencies, the system reverses: stock ledger entries, PO received quantities, PO status, and CreditorInvoice are all reversed.

---

### 4.4 Purchase Return

**How to open:** Sidebar → Inventory → Purchase Return → Click **+ New**

**Purpose:** Record the return of goods to a supplier. A purchase return links to a specific GRN and reduces stock.

**Step by step:**

1. Select a **GRN** from the dropdown (only GRNs without existing returns appear).
2. The system loads all GRN line items with returnable quantities.
3. Enter the **Return Date**.
4. The **Warehouse** auto-fills from the GRN.
5. For each line item, enter the **Return Qty** (cannot exceed the received quantity).
6. Enter the **Reason** for the return.
7. Click **Save**.

> **Important:** Once saved, a Purchase Return cannot be deleted or reversed. The stock reduction is permanent. Verify all quantities carefully before saving.

> **Known Limitation:** There is currently no delete function for Purchase Returns. If you make an error, contact your administrator.

---

## 5. Stock Management

### 5.1 Stock Issue

**How to open:** Sidebar → Inventory → Stock Issue → Click **+ New**

**Purpose:** Issue (give out) stock from a warehouse to a staff member, guard, branch, or other recipient.

#### Step by Step

1. The **Issue No** is auto-generated (e.g., SI0015).
2. Select the **Issue Date** (defaults to today).
3. Select the **Warehouse** (required) — stock will be deducted from this warehouse.
4. Select the **Issue To Type**:

| Type | Description |
|------|-------------|
| Staff | Issue to an employee |
| Guard | Issue to a security guard |
| Other Guards | Issue to guards (multiple) |
| Branch | Issue to a branch location |
| Other | Issue to a non-listed recipient |

5. Based on the type:
   - For Staff/Guard/Other Guards: select the employee from the dropdown (filtered by the warehouse's branch).
   - For Branch: select the branch from the dropdown.
   - For Other: enter the recipient name manually.
6. **Salary Deduction toggle** (appears only for Guard/Staff/Other Guards):
   - Check this if the value of issued items should be deducted from the recipient's salary.
   - This links to the Payroll module's Uniform Loan feature.
7. Enter **Remarks** if needed.

#### Adding Items

8. Click **Add Row** to add a line item.
9. Select an **Item** from the dropdown.
10. When you select an item:
    - **Unit Cost** auto-fills from the item's Price.
    - **Available Stock** displays for the selected warehouse.
    - If the warehouse is not yet selected, the item's default warehouse auto-fills.
11. Select a **Client** (required — the client/branch the issue is for).
12. Enter the **Issue Qty** (must be greater than zero and cannot exceed available stock).
13. Click **Add** to save the line.
14. Repeat for additional items.

| Field | Required? | Description |
|-------|-----------|-------------|
| Item | Yes | Select from dropdown |
| UOM | Auto | From item master |
| Issue Qty | Yes | Quantity to issue (must be > 0 and ≤ available stock) |
| Unit Cost | Auto | From item Price (editable) |
| Client | Yes | Select client from dropdown |
| Remarks | No | Line-specific notes |

#### Saving and Printing

15. Click **Save** to record the issue.
16. The system creates ISSUE ledger entries (OutQty), reducing stock.
17. A success message appears.
18. Click **Print PDF** to generate a printable voucher.

> **Delete:** A stock issue can be deleted only if no Stock Issue Return has been recorded against it and it is not linked to a Uniform Loan.

---

### 5.2 Stock Issue Return

**How to open:** Sidebar → Inventory → Issue Return → Click **+ New**

**Purpose:** Record the return of previously issued stock. Returns increase stock back in the warehouse.

**Step by step:**

1. The **Return No** is auto-generated (e.g., SIR0001).
2. Select the **Return Date**.
3. Select the **Original Issue** from the dropdown (lists all active issue documents).
4. When you select an issue, the system loads its line items with:
   - **Issued Qty** — the original issued quantity per line
   - **Already Returned** — cumulative returns per line
   - **Returnable Qty** — Issued Qty minus Already Returned
5. For each line, enter the **Return Qty** (cannot exceed the returnable quantity).
6. Enter **Remarks** if needed.
7. Click **Save**.

> **Important:** Once saved, an Issue Return cannot be edited or deleted. The stock increase is permanent. Verify quantities carefully before saving.

> **Known Limitation:** There is currently no delete function for Issue Returns. If you make an error, contact your administrator.

---

### 5.3 Stock Transfer

**How to open:** Sidebar → Inventory → Stock Transfer → Click **+ New**

**Purpose:** Move stock from one warehouse to another. This creates paired ledger entries: TRANSFER_OUT (source warehouse) and TRANSFER_IN (destination warehouse).

**Step by step:**

1. The **Transfer No** is auto-generated (e.g., ST0007).
2. Select the **Transfer Date**.
3. Select the **Source Warehouse** (where stock comes from).
4. Select the **Destination Warehouse** (where stock goes to).
5. Source and destination must be different warehouses.

#### Adding Items

6. Click **Add Row**.
7. Select an **Item**.
8. The system displays **Available Stock** in the source warehouse.
9. Enter the **Transfer Qty** (must be > 0 and ≤ available stock in source).
10. The **Unit Cost** auto-fills from the item's Price.
11. Click **Add** to save the line.
12. Repeat for additional items.

13. Click **Save** to record the transfer.
14. The system creates two ledger entries per line:
    - TRANSFER_OUT (OutQty) in the source warehouse
    - TRANSFER_IN (InQty) in the destination warehouse

> **Delete:** A stock transfer can be deleted. This reverses both TRANSFER_OUT and TRANSFER_IN ledger entries.

#### Transfer from a BSR

When you create a transfer from an approved Branch Stock Request (via the "Create Transfer" button on the BSR), the system:
- Pre-fills the source warehouse from the BSR
- Auto-selects the destination warehouse based on the requesting branch
- Pre-fills all BSR line items with requested quantities
- After saving, links the transfer back to the BSR

---

### 5.4 Stock Adjustment

**How to open:** Sidebar → Inventory → Stock Adjustment → Click **+ New**

**Purpose:** Adjust stock quantities for physical count discrepancies. Adjustments can be positive (add stock) or negative (remove stock).

> **Key Rule:** Stock Ledger entries for adjustments are created ONLY when the adjustment is **Approved**, not when it is saved as Draft.

**Step by step:**

1. The **Adjustment No** is auto-generated (e.g., SA0003).
2. Select the **Adjustment Date**.
3. Select the **Warehouse** (required).
4. Select the **Item** (required) — the system displays current stock for reference.
5. Select the **Adjustment Type**:

| Type | Description |
|------|-------------|
| PLUS | Increase stock (creates ADJ_PLUS ledger entry on approval) |
| MINUS | Decrease stock (creates ADJ_MINUS ledger entry on approval) |

6. Enter the **Adjustment Qty** (must be greater than zero).
7. The **Unit Cost** auto-fills from the item's Price.
8. Enter the **Reason** (required — explain why the adjustment is needed).
9. Enter **Remarks** if needed.
10. Click **Save Draft** to save without applying.

> **Note:** Stock Adjustment is a single-item-per-document screen. You can only adjust one item per adjustment.

#### Submitting for Approval

11. On the saved Draft, click **Submit for Approval**.
12. If approval is configured for Stock Adjustment, the configured approver receives a notification.
13. If approval is NOT configured (ApprovalRequired = false), the adjustment auto-approves on submit.

#### Approval

14. The approver sees **Approve** and **Reject** buttons.
15. On **Approve**: the system creates the ADJ_PLUS or ADJ_MINUS ledger entry — stock changes immediately.
16. On **Reject**: the adjustment returns to Draft with rejection remarks.

#### Status Lifecycle

| Status | Ledger Applied? | Can Edit? | Can Delete? |
|--------|-----------------|-----------|-------------|
| Draft | No | Yes | Yes |
| Submitted | No | No | No |
| Approved | Yes (on approval) | No | No |
| Rejected | No | Yes (resubmit) | Yes |

---

### 5.5 Stock Damage / Loss

**How to open:** Sidebar → Inventory → Damage / Loss → Click **+ New**

**Purpose:** Record items that have been damaged or lost. This reduces stock immediately on save.

**Step by step:**

1. The **Record No** is auto-generated (e.g., DL0001).
2. Select the **Record Date**.
3. Select the **Warehouse** (required).
4. Select the **Item** (required) — the system displays current stock for reference.
5. Select the **Loss Type**:

| Type | Description |
|------|-------------|
| Damage | Item is damaged but may be repairable |
| Loss | Item is lost or permanently unusable |

6. Enter the **Quantity** (must be > 0 and cannot exceed available stock).
7. The **Unit Cost** auto-fills from the item's Price.
8. Enter the **Reason** (required — explain what happened).
9. Enter **Remarks** if needed.
10. Click **Save**.

On save, the system creates a DAMAGE_LOSS ledger entry (OutQty), reducing stock immediately.

> **Delete:** A damage/loss record can be deleted. This reverses the DAMAGE_LOSS ledger entry and restores stock.

---

### 5.6 Branch Stock Request (BSR)

**How to open:** Sidebar → Inventory → Branch Stock Request → Click **+ New**

**Purpose:** A branch requests stock from the HQ warehouse. The request goes through approval, then is fulfilled via a Stock Transfer.

**Step by step:**

1. The **Request No** is auto-generated (e.g., SR0008).
2. Select the **Request Date**.
3. Select the **Requesting Branch** (your branch).
4. Select the **Source Warehouse** (the HQ warehouse that will fulfill the request).
5. Enter **Remarks** if needed.

#### Adding Items

6. Click **Add Row**.
7. Select an **Item**.
8. Enter the **Requested Qty**.
9. Select a **Client** (the client/branch the items are for).
10. The **Buying Price** auto-fills from the item's Price.
11. Click **Add** to save the line.
12. Repeat for additional items.

#### Saving and Submitting

13. Click **Save Draft** to save without submitting.
14. Click **Submit for Approval** to send to the approver.

#### BSR Status Lifecycle

| Status | Meaning | Action Available |
|--------|---------|-----------------|
| Draft | Saved, not submitted | Edit, Delete, Submit |
| Submitted | Waiting for approval | (wait) |
| Approved | Approved — ready for transfer | Create Transfer |
| Rejected | Rejected | View rejection reason |

#### Fulfilling an Approved BSR

15. On an Approved BSR, click **Create Transfer**.
16. The system navigates to the New Stock Transfer screen with:
    - Source warehouse pre-filled from the BSR
    - Destination warehouse auto-selected based on the requesting branch
    - All BSR line items pre-filled with requested quantities
17. Review and adjust quantities if needed.
18. Save the transfer.
19. The system links the transfer back to the BSR.

---

## 6. Asset Management User Guide

### Asset Lifecycle Overview

```
Register → Allocate → [Transfer ↔ Return] → Maintenance → Damage → Disposal
                                                              ↓
                                                        Location Tracking
```

Assets follow a lifecycle from registration through allocation, potential transfers and returns, maintenance, damage, and finally disposal. Each step is recorded as an event in the Asset Movement History.

---

### 6.1 Asset Master

**How to open:** Sidebar → Inventory → Asset Master → Click **+ New**

**Purpose:** Register fixed assets (vehicles, equipment, etc.) in the system.

| Field | Required? | Description | Sample |
|-------|-----------|-------------|--------|
| Code | Auto | System-generated (read-only) | AST001 |
| Name | Yes | Asset name | Patrol Vehicle 01 |
| Asset Type | Yes | Type of asset | Vehicle |
| Branch | Yes | Branch where asset is assigned | Seremban HQ |
| Purchase Amount | No | Original purchase price | 85,000.00 |
| Purchase Date | No | Date of purchase | 15-Jan-2026 |
| Warranty Expiry | No | Warranty expiration date | 15-Jan-2028 |
| Status | Auto | Current status (see lifecycle) | Available |

**Asset Statuses:**

| Status | Meaning |
|--------|---------|
| Available | In stock and ready for allocation |
| Allocated | Currently assigned to a person or branch |
| In_Transit | Being transferred |
| Under_Maintenance | Undergoing maintenance/repair |
| Damaged | Reported as damaged |
| Disposed | Disposed (permanent — cannot be changed) |
| Lost | Reported as lost |

**How to edit:** Click the edit icon. All fields except Code can be modified.

---

### 6.2 Asset Allocation

**How to open:** Sidebar → Inventory → Asset Allocation

**Purpose:** Allocate (assign) an asset to a person or branch. Only Available assets can be allocated.

**Step by step:**

1. Click **+ New** on the Asset Allocation screen.
2. The **Allocation No** is auto-generated (e.g., AAL0001).
3. Select the **Asset** (only Available and non-Disposed assets appear).
4. Select the **Allocation Date**.
5. Select the **Allocated To Type**: Branch, Staff, or Guard.
6. Based on the type:
   - For Branch: select the branch from the dropdown.
   - For Staff/Guard: search and select the employee.
7. Enter **Remarks** if needed.
8. Click **Save**.

On save: the asset's status changes to **Allocated** and an entry is recorded in Asset Movement History.

**Cancelling an Allocation:** Click the cancel icon on the allocation list. The asset returns to **Available** status.

**Printing:** Click the print icon to generate an Asset Allocation PDF voucher.

---

### 6.3 Asset Transfer

**How to open:** Sidebar → Inventory → Asset Transfer

**Purpose:** Transfer an asset from one holder to another. This is an event record (cannot be edited after saving).

**Step by step:**

1. Click **+ New** (inline form on the same screen).
2. The **Transfer No** is auto-generated.
3. Select the **Asset**.
4. Select the **Transfer Date**.
5. Enter the **From** person/branch and **To** person/branch.
6. Enter the **Transfer Reason**.
7. Click **Save**.

> **Note:** Asset transfers are immutable event records. To correct a wrong transfer, create a new transfer back to the original holder.

---

### 6.4 Asset Return

**How to open:** Sidebar → Inventory → Asset Return

**Purpose:** Record the return of an allocated asset. The asset returns to Available status.

**Step by step:**

1. Click **+ New** (inline form).
2. The **Return No** is auto-generated.
3. Select the **Asset** (only Allocated assets appear).
4. Select the **Allocation** (the original allocation record).
5. Select the **Return Date**.
6. Enter who returned the asset.
7. Record the **Return Condition** (e.g., Good, Damaged).
8. Enter who received the return.
9. Click **Save**.

On save: the asset's status changes to **Available**, the original allocation is marked inactive, and an entry is recorded in Asset Movement History.

---

### 6.5 Asset Maintenance

**How to open:** Sidebar → Inventory → Asset Maintenance

**Purpose:** Schedule and track maintenance/repair work for assets.

**Step by step:**

1. Click **+ New** (inline form).
2. The **Maintenance No** is auto-generated.
3. Select the **Asset**.
4. Select the **Maintenance Type** (e.g., Routine, Repair, Service).
5. Enter the **Problem Description**.
6. Enter the **Vendor Name** (who will do the maintenance).
7. Select the **Start Date**.
8. Click **Save** — the asset status changes to **Under_Maintenance**.

**Completing Maintenance:**

9. When maintenance is finished, click the **Complete** action on the list.
10. Enter the **Completion Date**, **Cost**, and **Completion Remarks**.
11. Click **Save** — the asset status reverts to its previous status.

> **Known Limitation:** You cannot edit or delete a maintenance record after saving. To correct, complete it and create a new one.

---

### 6.6 Asset Damage

**How to open:** Sidebar → Inventory → Asset Damage

**Purpose:** Record damage to an asset.

**Step by step:**

1. Click **+ New** (inline form).
2. The **Damage No** is auto-generated.
3. Select the **Asset**.
4. Select the **Damage Date**.
5. Enter the **Damage Type** (e.g., Accident, Wear and Tear).
6. Enter the **Damage Description** (required).
7. Enter the **Estimated Repair Cost**.
8. Enter who reported the damage.
9. Click **Save** — the asset status changes to **Damaged**.

> **Note:** Damage records are event logs and cannot be edited after saving.

---

### 6.7 Asset Disposal

**How to open:** Sidebar → Inventory → Asset Disposal

**Purpose:** Dispose of an asset that is no longer needed. This requires approval. Once disposed, the asset status is permanently set to **Disposed**.

**Step by step:**

1. On the Asset Disposal screen, click the inline **+ New** form.
2. The **Disposal No** is auto-generated.
3. Select the **Asset** to dispose.
4. Select the **Disposal Date**.
5. Select the **Disposal Type** (e.g., Sold, Scrapped, Donated).
6. Enter the **Disposal Reason** (required).
7. Enter the **Disposal Value** (amount received from sale, if any).
8. Enter **Remarks** if needed.
9. Click **Save** — the disposal record is created in **Draft** status.

#### Disposal Approval Workflow

```
Draft → Submitted → Approved / Rejected
```

10. Click **Submit** to send for approval.
11. The configured approver (or SuperAdmin) sees **Approve** and **Reject** buttons.
12. On **Approve**:
    - Asset status changes to **Disposed** (permanent).
    - Any active allocations are cancelled.
    - Movement history records the disposal.
13. On **Reject**:
    - Disposal returns to Draft with rejection remarks.
    - Asset status is unchanged.

> **Important:** Disposal is irreversible. Once approved, the asset cannot be reactivated. Verify all details before submitting.

---

### 6.8 Asset Movement History

**How to open:** Navigate to an asset's Movement History (via icon link from Asset Master)

**Purpose:** View the complete lifecycle history of a single asset.

**What it shows:**

| Column | Description |
|--------|-------------|
| Movement Date | When the event occurred |
| Movement Type | Allocation, Transfer, Return, Maintenance, Damage, Disposal |
| From | Previous holder/location |
| To | New holder/location |
| Reference No | Document number |
| Previous Status | Asset status before this event |
| New Status | Asset status after this event |

This is a read-only screen — no editing or actions available.

---

### 6.9 Asset Location Tracking

**How to open:** Sidebar → Inventory → Asset Location

**Purpose:** Track the physical location of assets and log location changes.

**Location Statuses:**

| Status | Meaning |
|--------|---------|
| Normal | Asset is at its expected location |
| Defect_Location | Asset is at an unexpected/wrong location |
| Parked_Away | Asset is parked away from its duty point |
| Unknown | Location not confirmed |

**Step by step:**

1. Select an asset from the list.
2. View its current location and status.
3. To update, enter the **New Location** and select the **Location Status**.
4. Enter who is reporting the update.
5. Click **Save**.
6. The system logs the change in AssetLocationLog (append-only — full history preserved).

---

## 7. Reports User Guide

All inventory reports are accessible from the sidebar under **Inventory → Reports**. Reports open as PDF or Excel files in a new browser tab.

### How Reports Work

1. Open the report screen from the sidebar.
2. Fill in the filter fields (warehouse, date range, item, etc.).
3. Click **Search** (or Generate) to preview the report as a PDF in an embedded viewer.
4. Click **Export Excel** to download an Excel version (where supported).

> **Note:** Reports run on a separate report server. If a report does not open, the report server may be down or a required database migration may not have been run. Contact your administrator.

---

### 7.1 Stock Ledger Report

**Navigation:** Inventory → Reports → Stock Ledger Report

**Purpose:** Detailed stock movement history with running balance per item per warehouse.

**Filters:**

| Filter | Required? | Description |
|--------|-----------|-------------|
| Warehouse | Yes | Select warehouse |
| Item | No | Select specific item or All Items |
| Start Date | Yes | From date |
| End Date | Yes | To date |
| Transaction Type | No | All Types, or filter by specific type |
| Category | No | Filter by item category |
| Size | No | Filter by item size |

**Output:** PDF (inline viewer) + Excel export

**Columns:** S.No, Ref No, Date, Type, Category, Item Code, Item Name, Size, UOM, In Qty, Out Qty, Balance (running), Unit Cost, Remarks

**Special features:**
- Running balance column shows cumulative stock after each transaction.
- Ref No is a clickable hyperlink that navigates to the source document.
- Totals row sums In Qty and Out Qty.
- Negative balances shown in red.

---

### 7.2 Current Stock Report

**Navigation:** Inventory → Reports → Current Stock Report

**Purpose:** Show current stock levels for all items across warehouses.

**Filters:** Warehouse, Item, Category, Size

**Output:** PDF + Excel

**Columns:** S.No, Warehouse, Item Code, Item Name, Category, Brand, Size, UOM, Current Stock, Reorder Level, Unit Cost, Stock Value, Status

**Special features:**
- Items at or below reorder level are highlighted in amber with "Low Stock" status.
- Items above reorder level show "OK" status in green.
- Totals row sums Current Stock and Stock Value.

---

### 7.3 Stock Movement Report

**Navigation:** Inventory → Reports → Stock Movement Report

**Purpose:** Summary of opening balance, total inward, total outward, and closing balance per item.

**Filters:** Warehouse, Item, Date Range, Client Name

**Output:** PDF + Excel

**Columns:** S.No, Warehouse, Item Code, Item Name, Category, Client Name, UOM, Opening Balance, Total In, Total Out, Closing Balance, Unit Cost, Txn Count

---

### 7.4 Low Stock Report

**Navigation:** Inventory → Reports → Low Stock Report

**Purpose:** Show items at or below their reorder level — a shopping list for replenishment.

**Filters:** Warehouse, Category

**Output:** PDF + Excel

**Columns:** S.No, Item Code, Item Name, Category, UOM, Warehouse, Current Stock, Reorder Level, Reorder Qty, Shortage Qty, Status

**Special features:**
- No date filter — snapshot of current state.
- "Out of Stock" items shown with red background.
- "Low Stock" items shown with amber background.
- Shortage Qty = Reorder Quantity - Current Stock.

---

### 7.5 Inventory Valuation Report

**Navigation:** Inventory → Reports → Inventory Valuation

**Purpose:** Calculate stock value using average cost method.

**Filters:** Warehouse, Category, As Of Date

**Output:** PDF + Excel

**Columns:** Category, Item Code, Item Name, UOM, Warehouse, Total In, Total Out, Current Stock, Avg Unit Cost, Valuation Amount

---

### 7.6 Voucher Reports (Single Document)

These reports generate a PDF voucher for a single document. They are accessed from the "Print PDF" button on the respective transaction screen.

| Report | Trigger Screen | Purpose |
|--------|---------------|---------|
| GRN Report | Goods Receipt (view) | Print goods receipt voucher |
| PR Report | Purchase Requisition (view) | Print purchase requisition |
| PO Report | Purchase Order (view) | Print purchase order |
| BSR Report | Branch Stock Request (view) | Print branch stock request |
| Stock Issue Report | Stock Issue (view) | Print stock issue voucher |
| Issue Return Report | Issue Return (view) | Print issue return voucher |
| Stock Transfer Report | Stock Transfer (view) | Print transfer voucher |
| Stock Adjustment Report | Stock Adjustment (view) | Print adjustment voucher |
| Asset Allocation Report | Asset Allocation (print icon) | Print allocation voucher |
| Asset Master Report | Asset Master | Print asset details |
| Asset Transfer Report | Asset Transfer | Print transfer voucher |
| Asset Return Report | Asset Return | Print return voucher |
| Asset Maintenance Report | Asset Maintenance | Print maintenance record |
| Asset Damage Report | Asset Damage | Print damage record |
| Asset Disposal Report | Asset Disposal | Print disposal record |

---

### 7.7 List Reports

| Report | Purpose | Key Filters |
|--------|---------|-------------|
| Stock In (GRN) Report | List of goods received | Warehouse, Date Range |
| Stock Issue Report | List of all stock issues | Warehouse, Date Range |
| Item & Category Stock | Stock by item and category | Warehouse, Category, Item, Date |
| Warehouse Stock Report | Stock by warehouse | Warehouse, Date |
| Supplier Purchase Report | Purchases by supplier | Date Range |
| Stock Transfer Report | Transfer history | Date Range |
| Stock Adj & Damage Loss | Adjustments and damage/loss summary | Date Range |
| Purchase & Issue Return | Purchase and issue return summary | Warehouse, Date Range, Return Type |
| Branch Asset Report | Assets by branch | Branch, Asset Status, Asset Type |
| Asset Movement Report | Asset allocation/transfer history | Asset, Movement Type, Date Range |

---

## 8. Approval and Notifications

### 8.1 Which Documents Require Approval?

| Document Type | Approval Required? | Default |
|---------------|-------------------|---------|
| Purchase Requisition | Yes | Required |
| Purchase Order | Yes | Required |
| Branch Stock Request | Yes | Required |
| Stock Adjustment | Configurable | Optional (off by default) |
| Stock Transfer | Configurable | Optional (off by default) |
| Damage / Loss | Configurable | Optional (off by default) |
| Asset Disposal | Configurable | Optional (off by default) |

> **Note:** Your administrator can toggle approval requirements via the Approval Setup screen (Administration → Approval Setup).

### 8.2 How to Submit a Document

1. Create the document (PR, PO, BSR, etc.) and save it as Draft.
2. Click the **Submit for Approval** button.
3. The document status changes to **Submitted**.
4. If approval routing is configured for your user, the designated approver receives a notification.
5. If no routing is configured, the document auto-approves immediately.

### 8.3 How to Check Pending Approvals

1. Go to **Administration → Pending Approvals**.
2. The screen shows all documents submitted to you for approval.
3. Each row shows the document number, type, submitted by, and date.

### 8.4 How to Approve or Reject

**To Approve:**
1. Open the submitted document.
2. Review the details.
3. Click **Approve**.
4. Optionally enter approval remarks.
5. The document status changes to **Approved** and a notification is sent to the requester.

**To Reject:**
1. Open the submitted document.
2. Click **Reject**.
3. Enter the **rejection reason** (mandatory).
4. The document status changes to **Rejected** and a notification is sent to the requester.

> **Security:** The system uses your logged-in identity for all approval actions. You cannot approve on behalf of another user. SuperAdmin users can self-approve any document.

### 8.5 How to Check Document Status

Each document list shows the current status:
- **Purchase Requisition:** Draft, Submitted, Approved, PO Posted, GRN Posted
- **Purchase Order:** Draft, Submitted, Approved, Partially Received, Fully Received, Cancelled, Rejected
- **Branch Stock Request:** Draft, Submitted, Approved, Rejected (with Linked Transfer No if fulfilled)
- **Stock Adjustment:** Draft, Submitted, Approved, Rejected
- **Asset Disposal:** Draft, Submitted, Approved, Rejected

### 8.6 How Notifications Work

- When you submit a document, the approver receives an in-app notification (real-time push via SignalR).
- When a document is approved or rejected, the requester receives a notification.
- Low-stock alerts are sent automatically when stock falls below the reorder level.
- View notifications in the notification panel (top right of the screen).

---

## 9. Daily Inventory User Workflow

Here is a practical end-to-end scenario showing how the screens work together. Each step notes which previous steps must be completed first.

### Step 1: Create a New Inventory Item

**Prerequisites:** Category, UOM, and Warehouse must already exist.

1. Go to Inventory → Item Master → + New.
2. Select Category "Uniform", UOM "Nos", default Warehouse "Main Store".
3. Enter name "Security Uniform Shirt (XL)", Price 48.00, Reorder Level 50.
4. Save. Item code ITM0043 is generated.

### Step 2: Record Opening Stock

**Prerequisites:** Step 1 (item exists).

1. Go to Inventory → Opening Stock → + New.
2. Select Warehouse "Main Store", Item "ITM0043 — Security Uniform Shirt (XL)".
3. Enter Opening Qty 300, Unit Cost 48.00.
4. Save. Stock is now 300 units in Main Store.

### Step 3: Raise a Purchase Requisition

**Prerequisites:** Step 1 (item exists), Supplier exists.

1. Go to Inventory → Purchase Requisition → + New.
2. Enter PR Date and Required By Date.
3. Add line: Item "ITM0043", Required Qty 200, Buying Price auto-fills as 48.00.
4. Save Draft, then Submit for Approval.

### Step 4: Create and Approve a Purchase Order

**Prerequisites:** Step 3 (PR approved).

1. The approver opens the PR and clicks **Approve**.
2. Purchase department claims the PR Processing Task.
3. Go to Inventory → Purchase Order → + New.
4. Select the approved PR (items auto-fill).
5. Select Supplier "ABC Uniform Sdn Bhd".
6. Adjust quantities/prices if needed.
7. Save Draft, then Submit for Approval.
8. The approver approves the PO.

### Step 5: Receive Goods through GRN

**Prerequisites:** Step 4 (PO approved).

1. Go to Inventory → Goods Receipt → + New.
2. Select the approved PO. Line items load with pending quantities.
3. Enter received quantities (e.g., 200 units received).
4. Enter Supplier Invoice No and Delivery Note Ref.
5. Save GRN. Stock increases by 200. Finance CreditorInvoice created.

### Step 6: Check Warehouse Stock

**Prerequisites:** Steps 2 and 5 (opening stock + GRN saved).

1. Go to Inventory → Stock Ledger.
2. Select Warehouse "Main Store", date range covering opening and GRN dates.
3. Click Search. You see:
   - OPENING entry: InQty 300, Balance 300.
   - STOCK_IN entry: InQty 200, Balance 500.

### Step 7: Issue Items to Employees or Guards

**Prerequisites:** Step 6 (stock available).

1. Go to Inventory → Stock Issue → + New.
2. Select Warehouse "Main Store", Issue To Type "Guard".
3. Select the guard from the dropdown.
4. Add line: Item "ITM0043", Issue Qty 2, Client "FWG HQ".
5. Save. Stock decreases by 2 (balance = 498).
6. Print PDF voucher for the guard's signature.

### Step 8: Process Returns

**Prerequisites:** Step 7 (issue exists).

1. Go to Inventory → Issue Return → + New.
2. Select the original issue document.
3. Enter return quantity (e.g., 1 unit returned — wrong size).
4. Save. Stock increases by 1 (balance = 499).

### Step 9: Transfer Stock Between Warehouses

**Prerequisites:** Step 6 (stock available), Destination warehouse exists.

1. Go to Inventory → Stock Transfer → + New.
2. Select Source "Main Store", Destination "Branch A Store".
3. Add line: Item "ITM0043", Transfer Qty 50.
4. Save. Main Store decreases by 50 (449), Branch A increases by 50.

### Step 10: Record Damaged or Lost Stock

**Prerequisites:** Step 6 (stock available).

1. Go to Inventory → Damage / Loss → + New.
2. Select Warehouse "Main Store", Item "ITM0043".
3. Select Loss Type "Damage", Quantity 5.
4. Enter Reason "5 shirts torn during storage".
5. Save. Stock decreases by 5 (balance = 444).

### Step 11: Generate Inventory Reports

1. Go to Inventory → Reports → Current Stock Report.
2. Select Warehouse "Main Store" or "All Warehouses".
3. Click Search to view the PDF.
4. Click Export Excel to download the spreadsheet.

### Dependency Summary

```
Category → Item Master → Opening Stock → Stock available
                                    ↓
Item Master → PR → Approve → PO → Approve → GRN → Stock increases
                                                    ↓
                                              Stock Issue → Issue Return
                                                    ↓
                                              Stock Transfer
                                                    ↓
                                              Damage / Loss
                                                    ↓
                                              Reports (any time)
```

---

## 10. Troubleshooting Guide

| Problem | Possible Reason | User Solution |
|---------|----------------|---------------|
| Cannot save a record | A required field is empty | Look for red-highlighted fields. Fill all mandatory fields (marked with *) and try again. |
| Item not appearing in dropdown | Item is inactive or belongs to a different category type | Verify the item exists in Item Master. Check that the category type matches (P for products, A for assets). |
| Warehouse not appearing in dropdown | Warehouse is inactive or you don't have branch access | Contact your administrator to verify warehouse IsActive status and your branch access rights. |
| "Insufficient stock for [item]" | You are trying to issue/transfer more than available | Check the Available Stock column. Reduce the quantity or receive more stock via GRN. |
| Cannot submit a PR/PO/BSR | The document is already submitted or approved | Only Draft documents can be submitted. Check the current status. |
| Approval pending for too long | The approver has not reviewed the document | Check Pending Approvals screen. Send a reminder to the approver. Verify approval routing is configured. |
| "Received quantity is more than remaining" on GRN | You entered more than the pending quantity | Check the Pending Qty column. Enter a quantity equal to or less than the pending amount. |
| Report not opening (blank page or error) | Report server is down or stored procedure is missing | Contact your administrator. Check ReportParamLog.txt on the report server for error details. |
| PDF download fails | BouncyCastle.Crypto.dll missing from report server bin/ | Contact your administrator to verify itextsharp.dll and BouncyCastle.Crypto.dll are in the report server bin/ folder. |
| Excel export fails | Same as PDF issue | Same solution as PDF download failure. |
| "Opening stock for this item already exists" | You are trying to create a duplicate opening stock | Edit the existing opening stock entry instead of creating a new one. |
| Cannot delete a GRN | Finance payment or Purchase Return exists against it | Reverse the Finance payment or delete the Purchase Return first, then delete the GRN. |
| Cannot delete a Stock Issue | A Stock Issue Return exists against it | The issue has been returned. You cannot delete it. |
| Cannot delete a Purchase Order | A GRN has been created against it | Delete the GRN first (if no payment exists), then delete the PO. |
| "Only Draft requisitions can be edited" | You are trying to edit a submitted/approved PR | Only Draft PRs can be fully edited. Use Edit Reference for non-Draft PRs. |
| Asset not appearing in allocation dropdown | Asset is already allocated, disposed, or under maintenance | Only Available assets can be allocated. Check the asset's current status in Asset Master. |
| Cannot dispose an asset | Asset disposal requires approval | Submit the disposal for approval. The approver must approve it before the asset status changes. |
| Stock Adjustment did not change stock | Adjustment is in Draft or Submitted status | Stock changes only on Approval. Submit and have the approver approve it. |
| Dashboard numbers seem wrong | Dashboard data may need refresh | Click the refresh button. If still wrong, contact your administrator. |
| Menu item is missing from sidebar | Your user account lacks access rights | Contact your administrator to verify your screen permissions. |
| "You do not have permission to view this page" | Your user account lacks Read access | Contact your administrator to grant Read access via User Access Rights. |

---

## 11. Glossary

| Term | Definition |
|------|-----------|
| **Approval Routing** | Configuration that maps a document type + requester to a specific approver user. |
| **BSR** | Branch Stock Request — a formal request from a branch to HQ for stock. |
| **CreditorInvoice** | A Finance record representing money owed to a supplier. Created automatically when a GRN is saved. |
| **Draft** | A document that has been saved but not yet submitted for approval. Can still be edited. |
| **GRN** | Goods Receipt Note — confirms that goods have been received against a Purchase Order. |
| **IsActive** | A flag that marks a record as active (visible in dropdowns) or inactive (hidden). |
| **Issue Return** | Return of previously issued stock back to the warehouse. |
| **Ledger Entry** | A single row in the Stock Ledger recording a stock movement (in or out) for a specific item and warehouse. |
| **Opening Stock** | The initial quantity of an item recorded in a warehouse before regular transactions begin. |
| **PR** | Purchase Requisition — a formal request to purchase items. |
| **PO** | Purchase Order — the official order sent to a supplier. |
| **Reorder Level** | The stock level at which a low-stock alert is triggered. |
| **Running Balance** | The cumulative stock quantity after each transaction, calculated as SUM(InQty) - SUM(OutQty). |
| **SignalR** | Real-time notification technology used to push approval and low-stock alerts to users. |
| **Soft Delete** | Marking a record as inactive rather than physically removing it from the database. |
| **Stock Ledger** | The master record of all stock movements. Every stock-in, stock-out, transfer, and adjustment creates a ledger entry. |
| **Stock Transfer** | Movement of stock from one warehouse to another. |
| **SuperAdmin** | A user with IsAdmin = 2. Can access all screens, approve any document, and bypass all permission checks. |
| **UOM** | Unit of Measure — the unit in which an item is counted (e.g., Nos, Pairs, Boxes). |
| **VWStockLedger** | A database view that joins the Stock Ledger with warehouse, item, category, brand, and UOM tables, and calculates the running balance. |

---

*This manual is based on the actual OBMS Inventory Module implementation as of November 2026. All screens, fields, and workflows described herein are verified from source code. Features marked as "Known Limitation" are documented gaps that may be addressed in future updates.*
