import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { UserAccessModel } from '../../../../model/userAccesModel';
import { environment } from '../../../../../environments/environment';

@Component({ selector: 'app-new-grn', templateUrl: './new-grn.component.html', styleUrls: ['./new-grn.component.css'] })
export class NewGRNComponent implements OnInit {
  frm!: FormGroup;
  isView = false;   // true when viewing an existing GRN (read-only)
  ID: any;
  poID: any;        // from query param when coming from PO

  approvedPOs: any[] = [];
  lines: any[] = []; // POLineForGRNDto enriched with receivedInput field
  existingDetails: any[] = [];

  grnNumber = '';
  poInfo: any = null;
  /** SST total and grand total for the saved GRN (view mode) */
  viewSSTTotalAmt  = 0;
  viewGrandTotalAmt = 0;

  // ── Reference-field inline edit (view mode only) ─────────────────────────
  isEditingRef    = false;   // toggles the edit inputs in view mode
  refSaving       = false;   // disables Save button while API call is in flight
  refSupplierInvoiceNo = '';
  refDeliveryNoteRef   = '';
  refRemarks           = '';

  // Search filter state
  poSearch = ''; filteredPOs: any[] = [];

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;

  constructor(private fb: FormBuilder, private activatedRoute: ActivatedRoute, private route: Router,
    private service: InventoryService, private _dataService: DatasharingService, private _masterService: MastermoduleService) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.frm = this.fb.group({
      POID:              [0, [Validators.required, Validators.min(1)]],
      WarehouseID:       [0],
      GRNDate:           [new Date(), Validators.required],
      DeliveryNoteRef:   [''],
      SupplierInvoiceNo: [''],
      Remarks:           ['']
    });
  }

  ngOnInit(): void {
    this.ID    = this.activatedRoute.snapshot.params['ID'];
    this.poID  = this.activatedRoute.snapshot.queryParams['poID'];
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => this.currentUser = u);
    this.getUserAccessRights(this.currentUser, 'Goods Receipt');
    this.loadApprovedPOs();

    if (this.ID && this.ID !== '0') {
      this.isView = true;
      this.loadExisting();
    } else {
      this.service.getGRNCode().subscribe({ next: (d: any) => this.grnNumber = d.Code, error: () => {} });
      if (this.poID) {
        this.frm.get('POID')?.setValue(+this.poID);
        this.loadPOLines(+this.poID);
      }
    }
  }

  getUserAccessRights(u: string, s: string): void {
    this._masterService.getUserAccessRights(u, s).subscribe(
      data => { if (data) { this.userAccessModel.readAccess = data.Read; this.userAccessModel.deleteAccess = data.Delete; this.userAccessModel.updateAccess = data.Update; this.userAccessModel.createAccess = data.Create; } },
      () => { this.errorMessage = 'Unable to load page permissions. Please try again.'; }
    );
  }

  loadApprovedPOs(): void {
    this.service.getApprovedPOsForGRN().subscribe({
      next: (d: any[]) => {
        this.approvedPOs = d || [];
        this.filteredPOs = [...this.approvedPOs];
        // If poID was provided (coming from PO page), now that list is loaded we can set PO info
        if (this.poID && !this.isView) {
          const po = this.approvedPOs.find(p => p.ID === +this.poID);
          if (po) { this.poInfo = po; this.frm.get('WarehouseID')?.setValue(po.WarehouseID); }
        }
      },
      error: () => { this.errorMessage = 'Unable to load purchase orders right now. Please try again.'; }
    });
  }

  onPOChange(poID: number): void {
    if (!poID || poID === 0) { this.lines = []; this.poInfo = null; return; }
    this.loadPOLines(poID);
  }

  loadPOLines(poID: number): void {
    this.showLoadingSpinner = true;
    // Set warehouse from PO — try from already-loaded list first, then from API
    const po = this.approvedPOs.find(p => p.ID === +poID);
    if (po) {
      this.poInfo = po;
      this.frm.get('WarehouseID')?.setValue(po.WarehouseID);
    } else {
      // Fallback: fetch PO details directly from API to get warehouse
      this.service.getPOByID(+poID).subscribe({
        next: (poData: any) => {
          if (poData) {
            this.poInfo = poData;
            this.frm.get('WarehouseID')?.setValue(poData.WarehouseID);
          }
        },
        error: () => {}
      });
    }
    this.service.getPOLinesForGRN(poID).subscribe({
      next: (d: any[]) => {
        if (!d || d.length === 0) { this.lines = []; this.errorMessage = 'There are no pending items to receive for this purchase order. All items may have already been fully received.'; }
        else {
          this.errorMessage = '';
          this.lines = d.map((l: any) => ({ ...l, receivedInput: l.PendingQty, inputError: '' }));
        }
        this.hideSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load purchase order items right now. Please try again.'; this.hideSpinner(); }
    });
  }

  loadExisting(): void {
    this.showLoadingSpinner = true;
    this.service.getGRNByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) { this.errorMessage = 'Goods receipt not found.'; this.hideSpinner(); return; }
        this.grnNumber = d.GRNNumber;
        this.frm.patchValue({ POID: d.POID, WarehouseID: d.WarehouseID, GRNDate: new Date(d.GRNDate), DeliveryNoteRef: d.DeliveryNoteRef, SupplierInvoiceNo: d.SupplierInvoiceNo, Remarks: d.Remarks });
        this.existingDetails = d.Details || [];
        // Store SST totals from the GRN header for the footer display
        this.viewSSTTotalAmt   = d.SSTTotal   || 0;
        this.viewGrandTotalAmt = d.GrandTotal  || 0;

        // Pre-fill the editable ref fields so the inputs are ready when the
        // user clicks Edit Reference without having to retype the existing values
        this.refSupplierInvoiceNo = d.SupplierInvoiceNo || '';
        this.refDeliveryNoteRef   = d.DeliveryNoteRef   || '';
        this.refRemarks           = d.Remarks           || '';

        // Populate poInfo for the view-mode PO summary panel.
        // Try the already-loaded approvedPOs list first; fall back to a direct API call.
        const matchedPO = this.approvedPOs.find((p: any) => p.ID === +d.POID);
        if (matchedPO) {
          this.poInfo = matchedPO;
        } else {
          this.service.getPOByID(+d.POID).subscribe({
            next: (poData: any) => { if (poData) { this.poInfo = poData; } },
            error: () => {}
          });
        }

        this.frm.disable();
        this.hideSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load the goods receipt. Please try again.'; this.hideSpinner(); }
    });
  }

  validateQty(line: any): void {
    const entered = parseFloat(line.receivedInput) || 0;
    if (entered < 0) { line.inputError = 'Quantity cannot be negative.'; return; }
    if (entered > line.PendingQty) {
      line.inputError = `Received quantity is more than the remaining quantity.\nOrdered: ${line.OrderedQty}   Already Received: ${line.ReceivedQty}   Remaining: ${line.PendingQty}\nPlease enter ${line.PendingQty} or less.`;
      return;
    }
    line.inputError = '';
  }

  hasAnyQty(): boolean { return this.lines.some(l => parseFloat(l.receivedInput) > 0); }
  hasErrors(): boolean { return this.lines.some(l => l.inputError && l.inputError !== ''); }

  /** Total received quantity across all lines in view mode */
  get viewTotalQty(): number {
    return this.existingDetails.reduce((sum: number, d: any) => sum + (parseFloat(d.ReceivedQty) || 0), 0);
  }

  /** Sub-total received value (ex-SST) across all lines in view mode */
  get viewTotalValue(): number {
    return this.existingDetails.reduce((sum: number, d: any) => sum + ((parseFloat(d.ReceivedQty) || 0) * (parseFloat(d.UnitCost) || 0)), 0);
  }

  /** SST total across all lines in view mode */
  get viewSSTTotal(): number {
    // Prefer the stored header value; fall back to summing lines
    if (this.viewSSTTotalAmt > 0) return this.viewSSTTotalAmt;
    return this.existingDetails.reduce((sum: number, d: any) => sum + (parseFloat(d.SSTAmount) || 0), 0);
  }

  /** Grand total in view mode: sub-total + SST */
  get viewGrandTotal(): number {
    if (this.viewGrandTotalAmt > 0) return this.viewGrandTotalAmt;
    return this.viewTotalValue + this.viewSSTTotal;
  }

  /** True when at least one received line has a non-zero SST rate */
  get hasSSTLines(): boolean {
    return this.existingDetails.some((d: any) => (parseFloat(d.SSTPercent) || 0) > 0);
  }

  /** True when any pending receiving line has a non-zero SST rate (new-mode) */
  get newModeHasSST(): boolean {
    return this.lines.some((l: any) => (parseFloat(l.SSTPercent) || 0) > 0);
  }

  /** Computed new-mode SST total across lines × receivedInput */
  get newModeSSTTotal(): number {
    return this.lines.reduce((sum: number, l: any) => {
      const qty  = parseFloat(l.receivedInput) || 0;
      const cost = parseFloat(l.UnitCost)      || 0;
      const sst  = parseFloat(l.SSTPercent)    || 0;
      return sum + qty * cost * sst / 100;
    }, 0);
  }

  /** Computed new-mode sub-total (ex-SST) */
  get newModeSubTotal(): number {
    return this.lines.reduce((sum: number, l: any) => {
      const qty  = parseFloat(l.receivedInput) || 0;
      const cost = parseFloat(l.UnitCost)      || 0;
      return sum + qty * cost;
    }, 0);
  }

  /** Computed new-mode grand total (sub-total + SST) */
  get newModeGrandTotal(): number {
    return this.newModeSubTotal + this.newModeSSTTotal;
  }

  onSave(): void {
    if (this.frm.get('POID')?.value <= 0) { this.showMsg('Please select a purchase order to receive against.', 'warning', 'Warning Message'); return; }
    if (!this.hasAnyQty()) { this.showMsg('Please enter the received quantity for at least one item.', 'warning', 'Warning Message'); return; }
    if (this.hasErrors()) { this.showMsg('Please correct the quantity errors before saving.', 'warning', 'Warning Message'); return; }

    // Block GRN save if any receiving line has UnitCost = 0
    // (0 cost means the GRN total = 0 and it will never appear in Finance Payments)
    const zeroCost = this.lines.find(l => parseFloat(l.receivedInput) > 0 && (!l.UnitCost || +l.UnitCost <= 0));
    if (zeroCost) {
      this.showMsg(`Unit Cost is 0 for "${zeroCost.ItemName}". Please update the Unit Price in the Purchase Order before receiving.`, 'warning', 'Warning Message');
      return;
    }

    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const details = this.lines
      .filter(l => parseFloat(l.receivedInput) > 0)
      .map(l => ({
        ID:          0,
        PODetailID:  l.PODetailID,
        ItemID:      l.ItemID,
        UOMID:       l.UOMID || null,
        ReceivedQty: parseFloat(l.receivedInput),
        UnitCost:    l.UnitCost,
        SSTPercent:  parseFloat(l.SSTPercent) || 0,
        LastUpdatedBy: this.currentUser
      }));

    const payload = { ID: 0, GRNNumber: this.grnNumber, POID: +raw.POID, WarehouseID: +raw.WarehouseID, GRNDate: raw.GRNDate, DeliveryNoteRef: raw.DeliveryNoteRef, SupplierInvoiceNo: raw.SupplierInvoiceNo, Remarks: raw.Remarks, LastUpdatedBy: this.currentUser, Details: details };

    this.service.saveGRN(payload).subscribe({
      next: (res: any) => { this.showMsg(res?.message || 'Goods receipt saved. Stock has been updated.', 'success', 'Success Message'); this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => this.route.navigate(['/inventory/goods-receipt'])); },
      error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to save the goods receipt right now. Please try again. If the problem continues, contact your administrator.'), 'error', 'Error Message'); this.hideSpinner(); }
    });
  }

  // ── Reference-field inline edit (view mode) ─────────────────────────────

  /** Enter edit mode for the three reference fields. */
  startEditRef(): void {
    // Refresh from current form values in case the user navigated back without saving
    this.refSupplierInvoiceNo = this.frm.get('SupplierInvoiceNo')?.value || '';
    this.refDeliveryNoteRef   = this.frm.get('DeliveryNoteRef')?.value   || '';
    this.refRemarks           = this.frm.get('Remarks')?.value           || '';
    this.isEditingRef = true;
  }

  /** Cancel reference edit without saving. */
  cancelEditRef(): void {
    this.isEditingRef = false;
  }

  /** POST only the three reference fields to the API. No stock/ledger/PO changes. */
  saveRef(): void {
    if (this.refSaving) return;
    this.refSaving = true;
    const payload = {
      ID:               +this.ID,
      SupplierInvoiceNo: this.refSupplierInvoiceNo.trim(),
      DeliveryNoteRef:   this.refDeliveryNoteRef.trim(),
      Remarks:           this.refRemarks.trim()
    };
    this.service.updateGRNReference(payload).subscribe({
      next: (res: any) => {
        // Reflect the saved values back into the read-only display
        this.frm.patchValue({
          SupplierInvoiceNo: payload.SupplierInvoiceNo,
          DeliveryNoteRef:   payload.DeliveryNoteRef,
          Remarks:           payload.Remarks
        });
        this.isEditingRef = false;
        this.refSaving    = false;
        this.showMsg(res?.message || 'Reference updated successfully.', 'success', 'Success Message');
      },
      error: (err: any) => {
        this.refSaving = false;
        this.showMsg(this.extractMsg(err, 'Unable to update right now. Please try again.'), 'error', 'Error Message');
      }
    });
  }

  onCancel(): void { this.route.navigate(['/inventory/goods-receipt']); }

  onPrintPDF(): void {
    if (!this.isView || !this.ID) return;
    const url = environment.baseReportUrl
      + 'Inventory/GRNReport.aspx'
      + '?LoginID=' + encodeURIComponent(this.currentUser)
      + '&GRNID=' + encodeURIComponent(this.ID);
    window.open(url, '_blank');
  }

  formatDate(d: any): string { if (!d) return ''; return new Date(d).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }

  private extractMsg(err: any, fallback: string): string { if (typeof err === 'string') return err; return err?.error?.message || err?.message || fallback; }
  private showMsg(message: string, icon: 'success'|'warning'|'info'|'error', title: 'Success Message'|'Warning Message'|'Error Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon, showCloseButton: false, timer: 8000, width: '750px', customClass: { popup: 'swal-top-offset' } });
    this.hideSpinner();
  }
  hideSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onPOOpen(opened: boolean) { if (opened) { this.poSearch = ''; this.filteredPOs = [...this.approvedPOs]; } }
  onPOSearch(q: string) { this.poSearch = q; const s = q.toLowerCase(); this.filteredPOs = this.approvedPOs.filter(p => p.PONumber?.toLowerCase().includes(s) || p.SupplierName?.toLowerCase().includes(s)); }
}
