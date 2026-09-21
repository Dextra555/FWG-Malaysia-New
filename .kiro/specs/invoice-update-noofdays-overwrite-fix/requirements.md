# Bug Report: Finance / Invoice — NoOfDays Overwritten on Invoice Update

## Summary

When a user opens a previously saved invoice on the Finance/Invoice page and clicks **Update**, the `NoOfDays` column in the `ClientInvoiceDetails` table is recalculated and overwritten with a freshly computed value instead of retaining the originally saved value.

---

## Requirements

### 1. Bug Condition

#### 1.1 Trigger Conditions

When all of the following are true, the bug is triggered:

- The user loads an existing invoice (`invoiceId > 0`) via `getClientInvoiceById()`
- `calculation()` is called (happens automatically after load)
- The user clicks **Update** (triggering `onSubmit()`)

**Root cause chain:**

1. `getClientInvoiceById()` assigns the API response array directly to `this.agreementDetails` — no defensive copy is made.
2. `calculation()` mutates `d.NoOfDays` **in-place** on every item in `this.agreementDetails` (e.g., `d.NoOfDays = daysInMonth` for `FollowCalendar` lines, or a recomputed partial-month value otherwise).
3. `onSubmit()` later iterates `sourceDetails` (which is `this.agreementDetails` for non-edit mode), reads the already-mutated `d.NoOfDays`, and saves those recalculated values to the database — overwriting the originally persisted `NoOfDays`.

#### 1.2 Observable Defective Behavior

- The `NoOfDays` value submitted to the API during an update differs from the value that was originally saved and returned by the API.
- For a `FollowCalendar` line loaded mid-month, `NoOfDays` is replaced with the full days-in-month count.
- For a partial-month agreement-date line, `NoOfDays` may be recomputed based on the current `invoice_period`, discarding the user-intended original value.

---

### 2. Expected Behavior

#### 2.1 NoOfDays Must Not Change on Update

When `invoiceId > 0` (i.e., updating an existing invoice), `NoOfDays` submitted to the API for each detail row **must equal** the `NoOfDays` value that was returned by the API when the invoice was loaded.

#### 2.2 Display Calculation Uses Computed Days, Not Saved Days

The `calculation()` function is responsible for displaying totals (ServiceCharges, Tax, Total) in the UI. It may use a locally computed days value for display purposes, but **must not mutate** `d.NoOfDays` on the shared `agreementDetails` objects.

#### 2.3 New Invoice Behavior Is Unchanged

For new invoices (`invoiceId === 0`), `NoOfDays` recalculation in `onSubmit()` continues to work exactly as today.

---

### 3. Preservation Requirements

The following existing behaviors must be preserved after the fix:

#### 3.1 New Invoice Creation

For `invoiceId === 0`, `onSubmit()` must still compute `noOfDays` using the same `FollowCalender` / partial-month logic as before and submit the correct value to the API.

#### 3.2 Totals Display on Load

When an existing invoice is loaded, the UI totals (ServiceCharges, Discount, Tax, Total) must still render correctly. `calculation()` must continue to produce accurate display values.

#### 3.3 Edit-Mode Behavior

When `isEditMode === true`, `onSubmit()` uses `this.agreementEditDetails` as `sourceDetails`. This path must remain unaffected by the fix.

#### 3.4 FollowCalendar Lines in New Invoices

For new invoices, lines where `d.FollowCalender === true` must still have `noOfDays` set to the full days-in-month count when submitted.

#### 3.5 Partial-Month Agreement Date Lines in New Invoices

For new invoices, lines where the agreement date falls in the same month/year as the invoice period must still apply the partial-month `NoOfDays` recalculation logic when submitted.
