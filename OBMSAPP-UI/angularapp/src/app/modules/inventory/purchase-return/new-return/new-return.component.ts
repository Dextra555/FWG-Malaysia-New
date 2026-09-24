import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { UserAccessModel } from '../../../../model/userAccesModel';

@Component({ selector: 'app-new-return', templateUrl: './new-return.component.html', styleUrls: ['./new-return.component.css'] })
export class NewReturnComponent implements OnInit {
  frm!: FormGroup;
  isView = false;
  ID: any;

  grns: any[] = [];
  lines: any[] = []; // GRNLineForReturnDto enriched with returnInput
  existingDetails: any[] = [];
  returnNumber = '';
  grnInfo: any = null;
  // Search filter state
  grnSearch = ''; filteredGRNs: any[] = [];

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;

  constructor(private fb: FormBuilder, private activatedRoute: ActivatedRoute, private route: Router,
    private service: InventoryService, private _dataService: DatasharingService, private _masterService: MastermoduleService) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.frm = this.fb.group({
      GRNID:      [0, [Validators.required, Validators.min(1)]],
      WarehouseID:[0],
      ReturnDate: [new Date(), Validators.required],
      Reason:     ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => this.currentUser = u);
    this.getUserAccessRights(this.currentUser, 'Purchase Return');
    this.loadGRNs();

    if (this.ID && this.ID !== '0') { this.isView = true; this.loadExisting(); }
    else { this.service.getReturnCode().subscribe({ next: (d: any) => this.returnNumber = d.Code, error: () => {} }); }
  }

  getUserAccessRights(u: string, s: string): void {
    this._masterService.getUserAccessRights(u, s).subscribe(
      data => { if (data) { this.userAccessModel.readAccess = data.Read; this.userAccessModel.deleteAccess = data.Delete; this.userAccessModel.updateAccess = data.Update; this.userAccessModel.createAccess = data.Create; } },
      () => { this.errorMessage = 'Unable to load page permissions. Please try again.'; }
    );
  }

  loadGRNs(): void {
    this.service.getGRNsForReturn().subscribe({
      next: (d: any[]) => { this.grns = d || []; this.filteredGRNs = [...this.grns]; },
      error: () => { this.errorMessage = 'Unable to load goods receipts right now. Please try again.'; }
    });
  }

  onGRNChange(grnID: number): void {
    if (!grnID || grnID === 0) { this.lines = []; this.grnInfo = null; return; }
    const grn = this.grns.find(g => g.ID === +grnID);
    if (grn) { this.grnInfo = grn; this.frm.get('WarehouseID')?.setValue(grn.WarehouseID); }
    this.showLoadingSpinner = true;
    this.service.getGRNLinesForReturn(grnID).subscribe({
      next: (d: any[]) => {
        if (!d || d.length === 0) { this.lines = []; this.errorMessage = 'There are no items available to return from this goods receipt. All received items may have already been fully returned.'; }
        else { this.errorMessage = ''; this.lines = d.map((l: any) => ({ ...l, returnInput: 0, inputError: '' })); }
        this.hideSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load goods receipt items. Please try again.'; this.hideSpinner(); }
    });
  }

  loadExisting(): void {
    this.showLoadingSpinner = true;
    this.service.getPurchaseReturnByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) { this.errorMessage = 'Purchase return not found.'; this.hideSpinner(); return; }
        this.returnNumber = d.ReturnNumber;
        this.grnInfo = { SupplierName: d.SupplierName, WarehouseName: d.WarehouseName, GRNNumber: d.GRNNumber, PONumber: d.PONumber };
        this.frm.patchValue({ GRNID: d.GRNID, WarehouseID: d.WarehouseID, ReturnDate: new Date(d.ReturnDate), Reason: d.Reason });
        this.existingDetails = d.Details || [];
        this.frm.disable();
        this.hideSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load the purchase return. Please try again.'; this.hideSpinner(); }
    });
  }

  validateQty(line: any): void {
    const entered = parseFloat(line.returnInput) || 0;
    if (entered < 0) { line.inputError = 'Quantity cannot be negative.'; return; }
    if (entered > line.ReturnableQty) {
      line.inputError = `Return quantity is more than the available return quantity for "${line.ItemName}".\nReceived: ${line.ReceivedQty}   Already Returned: ${line.AlreadyReturnedQty}   Returnable: ${line.ReturnableQty}\nPlease enter ${line.ReturnableQty} or less.`;
      return;
    }
    line.inputError = '';
  }

  hasAnyQty(): boolean { return this.lines.some(l => parseFloat(l.returnInput) > 0); }
  hasErrors(): boolean { return this.lines.some(l => l.inputError); }

  onSave(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    if (this.lines.length === 0) { this.showMsg('No items are available to return from this goods receipt.', 'warning', 'Warning Message'); return; }
    if (!this.hasAnyQty()) { this.showMsg('Please enter a return quantity for at least one item.', 'warning', 'Warning Message'); return; }
    if (this.hasErrors()) { this.showMsg('Please correct the quantity errors before saving.', 'warning', 'Warning Message'); return; }

    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const details = this.lines
      .filter(l => parseFloat(l.returnInput) > 0)
      .map(l => ({ ID: 0, GRNDetailID: l.GRNDetailID, ItemID: l.ItemID, UOMID: l.UOMID || null, ReturnQty: parseFloat(l.returnInput), UnitCost: l.UnitCost, LastUpdatedBy: this.currentUser }));

    const payload = { ID: 0, ReturnNumber: this.returnNumber, GRNID: +raw.GRNID, WarehouseID: +raw.WarehouseID, ReturnDate: raw.ReturnDate, Reason: raw.Reason, LastUpdatedBy: this.currentUser, Details: details };

    this.service.savePurchaseReturn(payload).subscribe({
      next: (res: any) => { this.showMsg(res?.message || 'Purchase return saved. Stock has been updated.', 'success', 'Success Message'); this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => this.route.navigate(['/inventory/purchase-return'])); },
      error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to save the purchase return right now. Please try again. If the problem continues, contact your administrator.'), 'error', 'Error Message'); this.hideSpinner(); }
    });
  }

  onCancel(): void { this.route.navigate(['/inventory/purchase-return']); }

  formatDate(d: any): string { if (!d) return ''; return new Date(d).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }

  private extractMsg(err: any, fallback: string): string { if (typeof err === 'string') return err; return err?.error?.message || err?.message || fallback; }
  private showMsg(message: string, icon: 'success'|'warning'|'info'|'error', title: 'Success Message'|'Warning Message'|'Error Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon, showCloseButton: false, timer: 8000, width: '750px', customClass: { popup: 'swal-top-offset' } });
    this.hideSpinner();
  }
  hideSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onGRNOpen(opened: boolean) { if (opened) { this.grnSearch = ''; this.filteredGRNs = [...this.grns]; } }
  onGRNSearch(q: string) { this.grnSearch = q; const s = q.toLowerCase(); this.filteredGRNs = this.grns.filter(g => g.GRNNumber?.toLowerCase().includes(s) || g.SupplierName?.toLowerCase().includes(s)); }
}
