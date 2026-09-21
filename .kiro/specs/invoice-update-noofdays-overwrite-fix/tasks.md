# Implementation Plan

- [ ] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** - NoOfDays Overwrite on Existing Invoice Update
  - **CRITICAL**: This test MUST FAIL on unfixed code — failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior — it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate `NoOfDays` gets overwritten during an update
  - **Scoped PBT Approach**: Scope the property to the concrete failing case — a `FollowCalendar` detail row loaded for an existing invoice (`invoiceId > 0`) where the saved `NoOfDays` is NOT equal to `daysInMonth` for the current invoice period
  - Create a test harness (Jasmine/Jest or plain TS) that:
    - Constructs an `InvoiceComponent` instance with `invoiceId = 42`
    - Sets `this.agreementDetails = [{ NoOfDays: 15, FollowCalendar: true, NoOfGuards: 1, Rate: 100, NoOfHours: 8, ... }]`
    - Sets `invoice_period` form control to a month with 31 days (e.g., January 2025)
    - Calls `calculation()` to simulate the on-load mutation
    - Records the `NoOfDays` value that will be submitted by `onSubmit()` (inspect `this.agreementDetails[0].NoOfDays`)
  - Assert: `this.agreementDetails[0].NoOfDays === 15` (original saved value)
  - **EXPECTED OUTCOME on UNFIXED code**: Test FAILS — `agreementDetails[0].NoOfDays` is 31, not 15 (mutation happened)
  - Document the counterexample: `"After calculation(), agreementDetails[0].NoOfDays changed from 15 to 31 (daysInMonth). onSubmit() will submit 31 instead of 15."`
  - Mark task complete when test is written, run, and the failure is documented
  - _Requirements: 1.1, 1.2_

- [ ] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** - New Invoice NoOfDays Computation and Totals Display
  - **IMPORTANT**: Follow observation-first methodology — observe UNFIXED code behavior first
  - **Observation step 1 (New Invoice — FollowCalendar)**:
    - Set `invoiceId = 0`, detail: `{ NoOfDays: 0, FollowCalender: true, NoOfGuards: 2, Rate: 50, NoOfHours: 8 }`, invoice period = January 2025
    - Observe that `onSubmit()` sets `noOfDays = 31` (full month) — record this
  - **Observation step 2 (New Invoice — partial month)**:
    - Set `invoiceId = 0`, detail: `{ NoOfDays: 10, FollowCalender: false, AgreementDate: "2025-01-10" }`, invoice period = January 2025
    - Observe the partial-month recalculation result — record exact `noOfDays` value
  - **Observation step 3 (Totals display)**:
    - Load any detail, call `calculation()`, observe ServiceCharges, Tax, Total are computed correctly
  - Write property-based tests:
    - **Preservation P2a**: For all new invoices (`invoiceId === 0`) with `FollowCalender = true` and any invoice period, `noOfDays` in submitted payload equals `daysInMonth` for that period
    - **Preservation P2b**: For all new invoices (`invoiceId === 0`) with `FollowCalender = false` and a non-same-month agreement date, `noOfDays` in submitted payload equals `d.NoOfDays` unchanged
    - **Preservation P2c**: For any set of detail rows, `calculation()` produces `ServiceCharges = sum(computedDays * NoOfGuards * NoOfHours * Rate)` for rows where all fields are non-zero
  - Run tests on UNFIXED code
  - **EXPECTED OUTCOME**: Tests PASS (confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 3. Fix: NoOfDays overwritten on existing invoice update

  - [ ] 3.1 Fix `calculation()` — replace `d.NoOfDays` mutation with local `computedDays` variable
    - In `calculation()`, before the `if (d.FollowCalendar)` block, declare: `let computedDays = d.NoOfDays;`
    - Replace all `d.NoOfDays =` assignments with `computedDays =`
    - Replace all subsequent usages of `d.NoOfDays` in the arithmetic expressions within the loop with `computedDays`
    - Specifically update: `this.ServiceCharges += computedDays * d.NoOfGuards * d.NoOfHours * d.Rate;`
    - Specifically update: `this.NoOfHours += d.NoOfHours * d.NoOfGuards * computedDays;`
    - `d.NoOfDays` must remain untouched throughout the method
    - _Bug_Condition: isBugCondition(component) where invoiceId > 0 AND calculation() mutates agreementDetails[*].NoOfDays_
    - _Expected_Behavior: calculation() uses computedDays locally; d.NoOfDays is never mutated_
    - _Preservation: ServiceCharges, Tax, Total display values remain arithmetically identical (PR-3)_
    - _Requirements: 1.1, 2.2, 3.2_

  - [ ] 3.2 Fix `onSubmit()` — guard NoOfDays recalculation behind `invoiceId === 0`
    - In the `sourceDetails.forEach(...)` loop in `onSubmit()`, wrap the `FollowCalender` / partial-month recalculation block inside `if (this.invoiceId === 0) { ... }`
    - When `this.invoiceId > 0`, `noOfDays` stays as `d.NoOfDays` (the loaded, persisted value)
    - When `this.invoiceId === 0`, the existing recalculation logic runs unchanged
    - _Bug_Condition: isBugCondition(component) where invoiceId > 0 AND onSubmit() recalculates noOfDays unconditionally_
    - _Expected_Behavior: For invoiceId > 0, submitted NoOfDays equals the value loaded from the API (expectedBehavior in design section 2)_
    - _Preservation: New invoice creation still computes noOfDays correctly (PR-1, PR-4, PR-5)_
    - _Requirements: 2.1, 3.1, 3.4, 3.5_

  - [ ] 3.3 (Optional) Fix `getClientInvoiceById()` — deep-copy `agreementDetails`
    - Replace `this.agreementDetails = d['details'];` with `this.agreementDetails = d['details'].map((item: any) => ({ ...item }));`
    - This is a defensive measure; the bug is already prevented by changes 3.1 and 3.2
    - Ensures future changes to `calculation()` cannot accidentally corrupt the loaded data
    - _Preservation: No behavioral change (PR-1 through PR-5 unaffected)_
    - _Requirements: 1.1_

  - [ ] 3.4 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - NoOfDays Preserved on Existing Invoice Update
    - **IMPORTANT**: Re-run the SAME test from task 1 — do NOT write a new test
    - The test from task 1 asserts `agreementDetails[0].NoOfDays === 15` after `calculation()` is called
    - With the fix applied, `calculation()` no longer mutates `d.NoOfDays`
    - **EXPECTED OUTCOME**: Test PASSES — confirms the bug is fixed
    - _Requirements: 2.1, 2.2_

  - [ ] 3.5 Verify preservation tests still pass
    - **Property 2: Preservation** - New Invoice NoOfDays Computation and Totals Display
    - **IMPORTANT**: Re-run the SAME tests from task 2 — do NOT write new tests
    - Run all three preservation properties (P2a, P2b, P2c) against the fixed code
    - **EXPECTED OUTCOME**: All tests PASS — confirms no regressions in new invoice creation or display totals
    - Confirm `FollowCalender` lines in new invoices still receive `daysInMonth` value
    - Confirm `calculation()` totals remain arithmetically correct

- [ ] 4. Checkpoint — Ensure all tests pass
  - Run the full test suite for the invoice component
  - Confirm Property 1 (Bug Condition → now Expected Behavior) passes
  - Confirm Property 2 (Preservation) passes
  - Manually verify on the Finance/Invoice page:
    - Load an existing invoice, click Update — confirm `NoOfDays` in the database remains unchanged
    - Create a new invoice — confirm `NoOfDays` is still computed correctly for `FollowCalender` and partial-month lines
  - Ensure all tests pass; ask the user if any questions arise
