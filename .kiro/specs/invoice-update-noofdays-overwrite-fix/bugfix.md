# Bugfix Requirements Document

## Introduction

When a user opens a previously saved invoice on the Finance/Invoice page and clicks **Update**, the `NoOfDays` column in the `ClientInvoiceDetails` table is recalculated and overwritten with a fresh value derived from the current agreement or the current calendar month — instead of preserving the value that was saved at invoice creation time.

This causes silent data corruption: an invoice originally saved with `NoOfDays = 2` (e.g., a mid-month agreement start) silently becomes `NoOfDays = 3` after a no-change update, altering the service charges and tax amounts recorded for that billing period.

The affected fields in `ClientInvoiceDetails` are: `NoOfDays`, `NoOfGuards`, `Rate`, `NoOfHours`, `MonthTotal`, and `TaxAmount`.

---

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN an existing saved invoice is loaded and the user clicks Update (with no changes made) AND the invoice detail has `FollowCalender = true`, THEN the system overwrites `NoOfDays` with the number of days in the current invoice period month, discarding the originally saved value.

1.2 WHEN an existing saved invoice is loaded and the user clicks Update AND the invoice period month/year matches the agreement date's month/year, THEN the system re-evaluates the partial-month `NoOfDays` calculation against the current date offsets, potentially producing a different value than what was originally saved.

1.3 WHEN `NoOfDays` is recalculated and differs from the saved value, THEN the system also recalculates `MonthTotal` and `TaxAmount` from the new `NoOfDays`, further corrupting the stored billing amounts.

### Expected Behavior (Correct)

2.1 WHEN an existing saved invoice is loaded and the user clicks Update (with no changes made) AND the invoice detail has `FollowCalender = true`, THEN the system SHALL preserve the `NoOfDays` value as stored in `ClientInvoiceDetails` and SHALL NOT overwrite it with a freshly calculated month-days value.

2.2 WHEN an existing saved invoice is loaded and the user clicks Update AND the invoice period matches the agreement start month, THEN the system SHALL preserve the `NoOfDays` value already stored in `ClientInvoiceDetails` and SHALL NOT re-run the partial-month day offset calculation.

2.3 WHEN an existing saved invoice detail is submitted for update, THEN the system SHALL send the persisted `NoOfDays`, `MonthTotal`, and `TaxAmount` values to the API unchanged, so that `ClientInvoiceDetails` retains the values from the original creation.

### Unchanged Behavior (Regression Prevention)

3.1 WHEN a brand-new invoice is being created (invoice ID = 0) AND the invoice detail has `FollowCalender = true`, THEN the system SHALL CONTINUE TO calculate `NoOfDays` as the number of days in the selected invoice period month.

3.2 WHEN a brand-new invoice is being created (invoice ID = 0) AND the invoice period matches the agreement start month, THEN the system SHALL CONTINUE TO apply the partial-month `NoOfDays` adjustment based on the agreement start date offset.

3.3 WHEN a brand-new invoice is being created, THEN the system SHALL CONTINUE TO calculate `MonthTotal` and `TaxAmount` from the computed `NoOfDays` before saving.

3.4 WHEN the user explicitly edits invoice detail fields (guards, rate, hours, days) through the Edit Invoice dialog on an existing invoice, THEN the system SHALL CONTINUE TO accept the user-modified values and save them correctly.

3.5 WHEN an existing invoice is updated and the invoice header fields (ServiceCharges, Discount, TaxAmount, InvoiceDate, InvoiceNo, Subject, Note) are changed, THEN the system SHALL CONTINUE TO save those header-level changes to `ClientInvoices`.

---

## Bug Condition (Pseudocode)

### Bug Condition Function

```pascal
FUNCTION isBugCondition(X)
  INPUT: X of type InvoiceUpdateRequest
  OUTPUT: boolean

  // Bug is triggered when updating an existing saved invoice
  // that has at least one detail row with FollowCalender = true,
  // OR with an invoice period matching the agreement start month
  RETURN X.invoiceID != 0
    AND EXISTS detail IN X.details WHERE (
      detail.FollowCalender = true
      OR (detail.AgreementDate.Month = X.invoicePeriod.Month
          AND detail.AgreementDate.Year = X.invoicePeriod.Year)
    )
END FUNCTION
```

### Fix Checking Property

```pascal
// Property: Fix Checking — NoOfDays must not be overwritten on update
FOR ALL X WHERE isBugCondition(X) DO
  result ← SaveAndUpdateInvoice'(X)
  FOR ALL detail IN result.savedDetails DO
    ASSERT detail.NoOfDays = X.originalSavedDetails[detail.ID].NoOfDays
    ASSERT detail.MonthTotal = X.originalSavedDetails[detail.ID].MonthTotal
  END FOR
END FOR
```

### Preservation Checking Property

```pascal
// Property: Preservation Checking — new invoice creation unaffected
FOR ALL X WHERE NOT isBugCondition(X) DO
  ASSERT SaveAndUpdateInvoice(X) = SaveAndUpdateInvoice'(X)
END FOR
```
