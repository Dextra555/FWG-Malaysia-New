# Design: Invoice Update — NoOfDays Overwrite Fix

## Bug Condition Methodology

### Notation

| Symbol | Meaning |
|--------|---------|
| `C(X)` | Bug Condition — inputs that trigger the bug |
| `P(result)` | Property — desired behavior for buggy inputs |
| `¬C(X)` | Non-buggy inputs that must be preserved |
| `F` | Original (unfixed) function |
| `F'` | Fixed function |

---

## 1. Bug Condition — `C(X)`

### Formal Definition

```
C(input) ≡
  input.invoiceId > 0
  AND calculation() has been called (mutating agreementDetails[*].NoOfDays)
  AND onSubmit() reads sourceDetails (= agreementDetails) with already-mutated NoOfDays
```

### `isBugCondition` Pseudocode

```typescript
function isBugCondition(component: InvoiceComponent): boolean {
  return (
    component.invoiceId > 0 &&                         // existing invoice loaded
    component.agreementDetails != null &&
    component.agreementDetails.length > 0 &&
    component.isEditMode === false                      // non-edit mode uses agreementDetails as sourceDetails
  );
}
```

### Concrete Counterexample

1. User selects an existing invoice with `ID = 42`.
2. `getClientInvoiceById(42)` assigns `this.agreementDetails` = API response (e.g., `[{ NoOfDays: 15, FollowCalendar: true, ... }]`).
3. `calculation()` runs — mutates: `d.NoOfDays = daysInMonth` (e.g., 31 for January).
4. User clicks **Update** → `onSubmit()` → `sourceDetails = this.agreementDetails` → `noOfDays = d.NoOfDays` = **31** (mutated value).
5. API call saves `NoOfDays = 31`, overwriting the original `NoOfDays = 15`.

---

## 2. Expected Behavior — `P(result)`

### Formal Definition

```
P(result) ≡
  ∀ detail ∈ submittedDetails:
    detail.NoOfDays === originalNoOfDaysFromAPI[detail.AgreementDetailID]
  WHEN invoiceId > 0
```

### `expectedBehavior` Pseudocode

```typescript
function expectedBehavior(submitted: InvoiceDetail[], loaded: InvoiceDetail[]): boolean {
  // For an existing invoice, every submitted detail must have
  // the same NoOfDays as the originally loaded API value.
  return submitted.every((s, i) =>
    s.NoOfDays === loaded[i].NoOfDays
  );
}
```

### Expected Behavior Properties

- **EB-1**: `NoOfDays` in the `onSubmit()` payload equals the value returned by `getClientInvoiceById()` for each detail row when `invoiceId > 0`.
- **EB-2**: `calculation()` uses a **local variable** (`computedDays`) for arithmetic and **never mutates** `d.NoOfDays` on the shared array.
- **EB-3**: The UI totals (ServiceCharges, Tax, Total) remain accurate — they use `computedDays` for display, not the persisted `NoOfDays`.

---

## 3. Preservation Requirements — `¬C(X)`

Inputs/scenarios where the bug is **not** triggered and existing behavior must be unchanged:

### PR-1: New Invoice (invoiceId === 0)

```typescript
¬C(X) where X.invoiceId === 0
```

- `onSubmit()` must still compute `noOfDays` using the full `FollowCalender` / partial-month logic.
- Submitted `NoOfDays` may differ from `d.NoOfDays` for new invoices (this is correct and intended).

### PR-2: Edit Mode (isEditMode === true)

```typescript
¬C(X) where X.isEditMode === true
```

- `sourceDetails = agreementEditDetails` — this path is unaffected by the fix.
- No changes to edit-mode behavior.

### PR-3: Totals Display

- `calculation()` must still correctly compute `ServiceCharges`, `DiscountAmount`, `TaxAmount`, and `Total` for display.
- Using a `computedDays` local variable instead of mutating `d.NoOfDays` must not change the arithmetic result.

### PR-4: FollowCalendar Lines — New Invoices

- For `invoiceId === 0`, lines with `d.FollowCalender === true` continue to submit `noOfDays = daysInMonth`.

### PR-5: Partial-Month Lines — New Invoices

- For `invoiceId === 0`, lines where `ad.getMonth() === dt.getMonth()` and same year apply the partial-month recalculation.

---

## 4. Fix Design

### Change 1 — `calculation()`: Use local variable instead of mutating `d.NoOfDays`

**File**: `invoice.component.ts`, method `calculation()`, lines ~305-318

**Before (buggy)**:
```typescript
if (d.FollowCalendar) {
  d.NoOfDays = daysInMonth;              // ← mutates shared object
} else {
  if (...) {
    ...
    d.NoOfDays = (agreementPeriod.getDay() - agreementPeriodDate.getDay() + 1);  // ← mutates
  }
}
// later uses d.NoOfDays for arithmetic
this.ServiceCharges += d.NoOfDays * d.NoOfGuards * ...;
```

**After (fixed)**:
```typescript
let computedDays = d.NoOfDays;           // ← local copy, never mutates d
if (d.FollowCalendar) {
  computedDays = daysInMonth;
} else {
  if (...) {
    ...
    computedDays = (agreementPeriod.getDay() - agreementPeriodDate.getDay() + 1);
  }
}
// use computedDays for arithmetic, d.NoOfDays is untouched
this.ServiceCharges += computedDays * d.NoOfGuards * ...;
this.NoOfHours += d.NoOfHours * d.NoOfGuards * computedDays;
```

### Change 2 — `onSubmit()`: Guard NoOfDays recalculation behind `invoiceId === 0`

**File**: `invoice.component.ts`, method `onSubmit()`, lines ~514-543

**Before (buggy)**:
```typescript
sourceDetails.forEach((d: any) => {
  let noOfDays = d.NoOfDays;
  if (d.FollowCalender) {
    noOfDays = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
  } else { ... }
  ...
  NoOfDays: noOfDays,   // ← always recalculated, even for existing invoices
});
```

**After (fixed)**:
```typescript
sourceDetails.forEach((d: any) => {
  // For existing invoices, preserve the loaded NoOfDays exactly as-is.
  // Recalculation only applies to new invoice creation.
  let noOfDays = d.NoOfDays;
  if (this.invoiceId === 0) {
    if (d.FollowCalender) {
      noOfDays = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
    } else {
      const ad = new Date(d.AgreementDate);
      if (ad.getMonth() === dt.getMonth() && ad.getFullYear() === dt.getFullYear()) {
        const lastDayOfMonth = new Date(ad.getFullYear(), ad.getMonth() + 1, 0).getDate();
        if (!(dt.getDate() === lastDayOfMonth && ad.getDate() === 1)) {
          if (!(d.NoOfDays > (dt.getDate() - ad.getDate() + 1))) {
            // keep original NoOfDays
          }
        }
      }
    }
  }
  ...
  NoOfDays: noOfDays,   // ← unchanged for existing invoices
});
```

### Change 3 (Optional) — `getClientInvoiceById()`: Deep-copy `agreementDetails`

**File**: `invoice.component.ts`, method `getClientInvoiceById()`, line ~267

**Before**:
```typescript
this.agreementDetails = d['details'];   // ← direct reference
```

**After**:
```typescript
this.agreementDetails = d['details'].map((item: any) => ({ ...item }));  // ← shallow clone per item
```

This is a defensive measure. With Changes 1 and 2 in place, the bug is already prevented, but deep-copying ensures `calculation()` can never inadvertently corrupt the original data even if future code is added.

---

## 5. Impact Analysis

| Area | Impact |
|------|--------|
| New invoice creation (`invoiceId === 0`) | No change — recalculation guard only activates for `invoiceId > 0` |
| Existing invoice update | **Fixed** — `NoOfDays` submitted = loaded value |
| Edit mode (`isEditMode === true`) | No change — uses `agreementEditDetails`, unaffected |
| UI totals display | No change — `computedDays` produces identical arithmetic result |
| API contract | No change — same payload shape; only `NoOfDays` values corrected |
