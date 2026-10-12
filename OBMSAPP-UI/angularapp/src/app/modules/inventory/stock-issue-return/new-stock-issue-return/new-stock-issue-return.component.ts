import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { UserAccessModel } from '../../../../model/userAccesModel';
import { environment } from '../../../../../environments/environment';

function greaterThanZero(): ValidatorFn {
  return (c: AbstractControl) => {
    const v = parseFloat(c.value);
    return isNaN(v) || v <= 0 ? { greaterThanZero: true } : null;
  };
}

@Component({
  selector: 'app-new-stock-issue-return',
  templateUrl: './new-stock-issue-return.component.html',
  styleUrls: ['./new-stock-issue-return.component.css']
})
export class NewStockIssueReturnComponent implements OnInit {

  frm!: FormGroup;
  isViewMode: boolean = false;
  ID: any;

  issueDocs: any[] = [];
  issueLines: any[] = [];
  warehouses: any[] = [];
  // Search filter state
  issueDocSearch = '';  filteredIssueDocs:  any[] = [];
  warehouseSearch = ''; filteredWarehouses: any[] = [];

  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  constructor(
    private fb: FormBuilder,
    private activatedRoute: ActivatedRoute,
    private route: Router,
    public dialog: MatDialog,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForm();
  }

  buildForm(): void {
    this.frm = this.fb.group({
      ID:                 [0],
      ReturnNo:           [''],
      ReturnDate:         [new Date(), Validators.required],
      OriginalIssueDocID: [0, [Validators.required, (c: AbstractControl) => +c.value === 0 ? { mustSelect: true } : null]],
      WarehouseID:        [0, [Validators.required, (c: AbstractControl) => +c.value === 0 ? { mustSelect: true } : null]],
      Remarks:            [''],
      Details:            this.fb.array([])
    });
  }

  get details(): FormArray { return this.frm.get('Details') as FormArray; }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Stock Issue Return');
    this.loadIssueDocs();
    this.loadWarehouses();

    if (this.ID && this.ID !== '0') {
      this.isViewMode = true;
      this.loadExistingRecord();
    } else {
      this.service.getStockIssueReturnCode().subscribe({
        next: (d: any) => this.frm.get('ReturnNo')?.setValue(d.Code),
        error: () => {}
      });
    }
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
        }
      },
      () => { this.errorMessage = 'Unable to load page permissions. Please try again.'; }
    );
  }

  loadIssueDocs(): void {
    this.service.getIssuableDocsForReturn().subscribe({
      next: (d: any[]) => { this.issueDocs = d || []; this.filteredIssueDocs = [...this.issueDocs]; },
      error: () => { this.errorMessage = 'Unable to load stock issue list. Please refresh the page.'; }
    });
  }

  loadWarehouses(): void {
    this.service.getWarehouseMasters().subscribe({
      next: (d: any[]) => { this.warehouses = (d || []).filter((w: any) => w.IsActive); this.filteredWarehouses = [...this.warehouses]; },
      error: () => {}
    });
  }

  onIssueDocChange(issueDocID: number): void {
    if (!issueDocID || issueDocID === 0) { this.issueLines = []; this.details.clear(); return; }

    // Auto-fill warehouse from selected issue doc
    const doc = this.issueDocs.find(d => d.ID === +issueDocID);
    if (doc) this.frm.get('WarehouseID')?.setValue(doc.WarehouseID);

    this.service.getIssueDetailsForReturn(+issueDocID).subscribe({
      next: (lines: any[]) => {
        this.issueLines = lines || [];
        this.details.clear();
        this.issueLines.forEach(line => {
          if (line.ReturnableQty > 0) {
            this.details.push(this.fb.group({
              ID:                    [0],
              OriginalIssueDetailID: [line.ID],
              ItemID:                [line.ItemID],
              UOMID:                 [line.UOMID],
              ReturnQty:             ['', [Validators.required, greaterThanZero()]],
              UnitCost:              [line.UnitCost],
              Remarks:               [''],
              _ItemName:             [line.ItemName],
              _UOMName:              [line.UOMName],
              _IssueQty:             [line.IssueQty],
              _AlreadyReturned:      [line.AlreadyReturnedQty],
              _ReturnableQty:        [line.ReturnableQty],
              _Include:              [true]
            }));
          }
        });
      },
      error: () => { this.errorMessage = 'Unable to load issue lines. Please try again.'; }
    });
  }

  loadExistingRecord(): void {
    this.showLoadingSpinner = true;
    this.service.getStockIssueReturnByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) { this.errorMessage = 'Return not found.'; this.hideLoadingSpinner(); return; }
        this.frm.patchValue({
          ID:                 d.ID,
          ReturnNo:           d.ReturnNo,
          ReturnDate:         new Date(d.ReturnDate),
          OriginalIssueDocID: d.OriginalIssueDocID,
          WarehouseID:        d.WarehouseID,
          Remarks:            d.Remarks
        });
        this.details.clear();
        (d.Details || []).forEach((line: any) => {
          this.details.push(this.fb.group({
            ID:                    [line.ID],
            OriginalIssueDetailID: [line.OriginalIssueDetailID],
            ItemID:                [line.ItemID],
            UOMID:                 [line.UOMID],
            ReturnQty:             [line.ReturnQty],
            UnitCost:              [line.UnitCost],
            Remarks:               [line.Remarks],
            _ItemName:             [line.ItemName],
            _UOMName:              [line.UOMName],
            _IssueQty:             [0],
            _AlreadyReturned:      [0],
            _ReturnableQty:        [line.ReturnQty],
            _Include:              [true]
          }));
        });
        this.frm.disable();
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load the return. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  get viewWarehouseName(): string {
    const id = this.frm.get('WarehouseID')?.value;
    return this.warehouses.find((w: any) => w.ID === id)?.Name || '—';
  }
  get viewIssueDocLabel(): string {
    const id = this.frm.get('OriginalIssueDocID')?.value;
    const doc = this.issueDocs.find((d: any) => d.ID === id);
    if (!doc) return '—';
    return `${doc.IssueNo} — ${doc.IssueToName || doc.IssueToType}`;
  }

  onSubmit(): void {
    if (this.isViewMode) { this.route.navigate(['/inventory/stock-issue-return']); return; }
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }

    const raw = this.frm.getRawValue();
    const activeLines = raw.Details.filter((d: any) => d._Include);
    if (activeLines.length === 0) {
      this.showMessage('Please include at least one item to return.', 'warning', 'Warning Message');
      return;
    }

    // Client-side returnable qty check
    for (const line of activeLines) {
      if (+line.ReturnQty > +line._ReturnableQty) {
        this.showMessage(
          `Return quantity for '${line._ItemName}' (${this.formatNumber(+line.ReturnQty)}) exceeds the returnable quantity (${this.formatNumber(+line._ReturnableQty)}).`,
          'warning', 'Warning Message');
        return;
      }
    }

    this.showLoadingSpinner = true;
    const payload = {
      ID:                 raw.ID,
      ReturnNo:           raw.ReturnNo,
      ReturnDate:         raw.ReturnDate,
      OriginalIssueDocID: +raw.OriginalIssueDocID,
      WarehouseID:        +raw.WarehouseID,
      Remarks:            raw.Remarks,
      LastUpdatedBy:      this.currentUser,
      Details: activeLines.map((d: any) => ({
        ID:                    d.ID,
        OriginalIssueDetailID: d.OriginalIssueDetailID,
        ItemID:                d.ItemID,
        UOMID:                 d.UOMID,
        ReturnQty:             +d.ReturnQty,
        UnitCost:              +d.UnitCost || 0,
        Remarks:               d.Remarks,
        LastUpdatedBy:         this.currentUser
      }))
    };

    this.service.saveStockIssueReturn(payload).subscribe({
      next: (res: any) => {
        this.showMessage(res?.message || 'Stock issue return saved successfully.', 'success', 'Success Message');
        this.route.navigateByUrl('/dummy', { skipLocationChange: true })
          .then(() => this.route.navigate(['/inventory/stock-issue-return']));
      },
      error: (err: any) => {
        this.showMessage(this.extractErrorMessage(err, 'Unable to save the return right now. Please try again.'), 'error', 'Error Message');
        this.hideLoadingSpinner();
      }
    });
  }

  onCancelClick(): void { this.route.navigate(['/inventory/stock-issue-return']); }

  /** Open the Issue Return PDF in a new browser tab for printing or downloading.
   *  Only callable in view mode (existing saved document). Read-only — does not
   *  modify any record, stock ledger, balance, or salary deduction. */
  onPrintPDF(): void {
    const docID = this.ID || this.frm.get('ID')?.value;
    if (!docID || +docID === 0) return;
    const url = environment.baseReportUrl
      + 'Inventory/IssueReturnReport.aspx'
      + '?LoginID='       + encodeURIComponent(this.currentUser)
      + '&IssueReturnID=' + encodeURIComponent(docID);
    window.open(url, '_blank');
  }

  private extractErrorMessage(err: any, fallback: string): string {
    if (err && typeof err === 'string') return err;
    if (err?.error?.message) return err.error.message;
    if (err?.message) return err.message;
    return fallback;
  }

  private showMessage(message: string, icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message' = 'Success Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon,
      showCloseButton: false, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onIssueDocOpen(opened: boolean)  { if (opened) { this.issueDocSearch  = ''; this.filteredIssueDocs  = [...this.issueDocs];  } }
  onWarehouseOpen(opened: boolean) { if (opened) { this.warehouseSearch = ''; this.filteredWarehouses = [...this.warehouses]; } }
  onIssueDocSearch(q: string)  { this.issueDocSearch  = q; const s = q.toLowerCase(); this.filteredIssueDocs  = this.issueDocs.filter(d  => d.IssueNo?.toLowerCase().includes(s) || d.IssueToName?.toLowerCase().includes(s) || d.IssueToType?.toLowerCase().includes(s)); }
  onWarehouseSearch(q: string) { this.warehouseSearch = q; const s = q.toLowerCase(); this.filteredWarehouses = this.warehouses.filter(w => w.Name?.toLowerCase().includes(s)); }
}
