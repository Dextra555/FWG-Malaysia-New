import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { PayrollModuleService } from '../../../../service/payrollmodule.service';
import { UserAccessModel } from '../../../../model/userAccesModel';

function mustSelect(): ValidatorFn {
  return (c: AbstractControl) => {
    const v = c.value;
    return (v === null || v === undefined || v === '' || +v === 0) ? { mustSelect: true } : null;
  };
}

function greaterThanZero(): ValidatorFn {
  return (c: AbstractControl) => {
    const v = parseFloat(c.value);
    return isNaN(v) || v <= 0 ? { greaterThanZero: true } : null;
  };
}

@Component({
  selector: 'app-new-stock-issue',
  templateUrl: './new-stock-issue.component.html',
  styleUrls: ['./new-stock-issue.component.css']
})
export class NewStockIssueComponent implements OnInit {

  frm!: FormGroup;
  isViewMode: boolean = false;
  ID: any;

  warehouses:  any[] = [];
  items:       any[] = [];

  issueToTypes: string[] = ['Staff', 'Guard', 'Branch', 'Other'];

  // Recipient lookup lists
  employeeList:      any[] = [];   // for Employee & Guard
  branchList:        any[] = [];   // for Branch
  filteredRecipients: any[] = [];  // currently shown after search filter
  recipientSearch:   string = '';  // search box value

  // Available stock display per row
  availableStockMap: { [key: number]: number } = {};

  userAccessModel!: UserAccessModel;
  currentUser:    string  = '';
  warningMessage: string  = '';
  errorMessage:   string  = '';
  showLoadingSpinner: boolean = false;

  constructor(
    private fb: FormBuilder,
    private activatedRoute: ActivatedRoute,
    private route: Router,
    public dialog: MatDialog,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService,
    private _payrollService: PayrollModuleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForm();
  }

  buildForm(): void {
    this.frm = this.fb.group({
      ID:          [0],
      IssueNo:     [''],
      IssueDate:   [new Date(), Validators.required],
      WarehouseID: [0, [Validators.required, mustSelect()]],
      IssueToType: ['', [Validators.required]],
      IssueToID:   [null],
      IssueToName: [''],
      Remarks:     [''],
      Details:     this.fb.array([])
    });
  }

  get details(): FormArray { return this.frm.get('Details') as FormArray; }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Stock Issue');
    this.loadWarehouses();
    this.loadItems();
    this.loadEmployeeList();
    this.loadBranchList();

    if (this.ID && this.ID !== '0') {
      this.isViewMode = true;
      this.loadExistingRecord();
    } else {
      this.service.getStockIssueCode().subscribe({
        next: (d: any) => this.frm.get('IssueNo')?.setValue(d.Code),
        error: () => {}
      });
      this.addDetailRow();
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

  loadWarehouses(): void {
    this.service.getWarehouseMasters().subscribe({
      next: (d: any[]) => { this.warehouses = (d || []).filter((w: any) => w.IsActive); },
      error: () => { this.errorMessage = 'Unable to load warehouse list. Please refresh the page.'; }
    });
  }

  loadItems(): void {
    this.service.getItemsForStock().subscribe({
      next: (d: any[]) => { this.items = d || []; },
      error: () => { this.errorMessage = 'Unable to load item list. Please refresh the page.'; }
    });
  }

  loadEmployeeList(): void {
    this._payrollService.getEmployeeList().subscribe({
      next: (d: any[]) => { this.employeeList = d || []; },
      error: () => {}  // non-critical — form still usable
    });
  }

  loadBranchList(): void {
    this.service.getBranchList().subscribe({
      next: (d: any[]) => { this.branchList = d || []; },
      error: () => {}
    });
  }

  // ── Called when Issue To Type changes ─────────────────────────────────────

  onIssueToTypeChange(): void {
    // Clear previous selection
    this.frm.get('IssueToID')?.setValue(null);
    this.frm.get('IssueToName')?.setValue('');
    this.recipientSearch  = '';
    this.filteredRecipients = this.getSourceList();
  }

  // Returns the right source list based on current type
  getSourceList(): any[] {
    const type = this.frm.get('IssueToType')?.value;
    if (type === 'Staff' || type === 'Guard') return this.employeeList;
    if (type === 'Branch') return this.branchList;
    return [];
  }

  // Returns display label depending on list type
  getRecipientLabel(item: any): string {
    const type = this.frm.get('IssueToType')?.value;
    if (type === 'Branch') {
      return `${item.Code || ''} — ${item.Name || ''}`.trim().replace(/^—\s*/, '');
    }
    // Staff / Guard: EMP_NAME + EMP_ID
    return `${item.EMP_NAME || ''} (${item.EMP_ID || ''})`;
  }

  // ── Search filter ──────────────────────────────────────────────────────────

  onRecipientSearch(searchVal: string): void {
    this.recipientSearch = searchVal;
    const lower  = searchVal.trim().toLowerCase();
    const source = this.getSourceList();
    if (!lower) {
      this.filteredRecipients = source;
    } else {
      this.filteredRecipients = source.filter(item => {
        const label = this.getRecipientLabel(item).toLowerCase();
        return label.includes(lower);
      });
    }
  }

  // ── Called when user selects a recipient from the dropdown ────────────────

  onRecipientSelect(item: any): void {
    const type = this.frm.get('IssueToType')?.value;
    if (type === 'Branch') {
      this.frm.get('IssueToID')?.setValue(item.ID);
      this.frm.get('IssueToName')?.setValue(item.Name || '');
    } else {
      // Employee / Guard — fields are EMP_ID, EMP_NAME
      this.frm.get('IssueToID')?.setValue(item.EMP_ID);
      this.frm.get('IssueToName')?.setValue(item.EMP_NAME || '');
    }
  }

  // ── Items table ────────────────────────────────────────────────────────────

  loadExistingRecord(): void {
    this.showLoadingSpinner = true;
    this.service.getStockIssueByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) { this.errorMessage = 'Stock issue not found.'; this.hideLoadingSpinner(); return; }
        this.frm.patchValue({
          ID:          d.ID,
          IssueNo:     d.IssueNo,
          IssueDate:   new Date(d.IssueDate),
          WarehouseID: d.WarehouseID,
          IssueToType: d.IssueToType,
          IssueToID:   d.IssueToID,
          IssueToName: d.IssueToName,
          Remarks:     d.Remarks
        });
        this.details.clear();
        (d.Details || []).forEach((line: any) => {
          this.details.push(this.fb.group({
            ID:        [line.ID],
            ItemID:    [line.ItemID, [Validators.required, mustSelect()]],
            UOMID:     [line.UOMID],
            IssueQty:  [line.IssueQty, [Validators.required, greaterThanZero()]],
            UnitCost:  [line.UnitCost],
            Remarks:   [line.Remarks || ''],
            _ItemName: [line.ItemName],
            _UOMName:  [line.UOMName],
            _Available:[0]
          }));
        });
        this.frm.disable();
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load the stock issue. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  addDetailRow(): void {
    this.details.push(this.fb.group({
      ID:        [0],
      ItemID:    [0, [Validators.required, mustSelect()]],
      UOMID:     [null],
      IssueQty:  ['', [Validators.required, greaterThanZero()]],
      UnitCost:  [0],
      Remarks:   [''],
      _ItemName: [''],
      _UOMName:  [''],
      _Available:[0]
    }));
  }

  removeDetailRow(i: number): void {
    this.details.removeAt(i);
    delete this.availableStockMap[i];
  }

  onWarehouseChange(): void {
    this.details.controls.forEach((_, i) => {
      const itemID = this.details.at(i).get('ItemID')?.value;
      if (itemID && itemID > 0) this.loadAvailableStock(i);
    });
  }

  onItemChange(i: number): void {
    const line     = this.details.at(i);
    const itemID   = +line.get('ItemID')?.value;
    const selected = this.items.find(x => x.ID === itemID);
    if (selected) {
      line.get('_ItemName')?.setValue(selected.Name);
      line.get('_UOMName')?.setValue(selected.UOMName || '');
      line.get('UOMID')?.setValue(selected.UOMID || null);
      line.get('UnitCost')?.setValue(selected.Price || 0);
    }
    this.loadAvailableStock(i);
  }

  loadAvailableStock(i: number): void {
    const warehouseID = +this.frm.get('WarehouseID')?.value;
    const itemID      = +this.details.at(i).get('ItemID')?.value;
    if (warehouseID > 0 && itemID > 0) {
      this.service.getCurrentStock(warehouseID, itemID).subscribe({
        next: (res: any) => {
          this.availableStockMap[i] = res?.currentStock ?? 0;
          this.details.at(i).get('_Available')?.setValue(res?.currentStock ?? 0);
        },
        error: () => { this.availableStockMap[i] = 0; }
      });
    }
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  // ── helpers for template ───────────────────────────────────────────────────

  get issueToType(): string { return this.frm.get('IssueToType')?.value || ''; }
  get issueToName(): string { return this.frm.get('IssueToName')?.value || ''; }
  get showRecipientDropdown(): boolean {
    return ['Staff', 'Guard', 'Branch'].includes(this.issueToType);
  }

  onSubmit(): void {
    if (this.isViewMode) { this.route.navigate(['/inventory/stock-issue']); return; }
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    if (this.details.length === 0) {
      this.showMessage('Please add at least one item.', 'warning', 'Warning Message');
      return;
    }

    for (let i = 0; i < this.details.length; i++) {
      const line     = this.details.at(i);
      const qty      = +line.get('IssueQty')?.value;
      const avail    = this.availableStockMap[i] || 0;
      const itemName = line.get('_ItemName')?.value || `Row ${i + 1}`;
      if (qty > avail) {
        this.showMessage(
          `Insufficient stock for '${itemName}'. Available quantity is ${this.formatNumber(avail)}.`,
          'warning', 'Warning Message');
        return;
      }
    }

    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const payload = {
      ID:            raw.ID,
      IssueNo:       raw.IssueNo,
      IssueDate:     raw.IssueDate,
      WarehouseID:   +raw.WarehouseID,
      IssueToType:   raw.IssueToType,
      IssueToID:     raw.IssueToID ? +raw.IssueToID : null,
      IssueToName:   raw.IssueToName,
      Remarks:       raw.Remarks,
      LastUpdatedBy: this.currentUser,
      Details: raw.Details.map((d: any) => ({
        ID:           d.ID,
        ItemID:       +d.ItemID,
        UOMID:        d.UOMID ? +d.UOMID : null,
        IssueQty:     +d.IssueQty,
        UnitCost:     +d.UnitCost || 0,
        Remarks:      d.Remarks,
        LastUpdatedBy: this.currentUser
      }))
    };

    this.service.saveStockIssue(payload).subscribe({
      next: (res: any) => {
        this.showMessage(res?.message || 'Stock issue saved successfully.', 'success', 'Success Message');
        this.route.navigateByUrl('/dummy', { skipLocationChange: true })
          .then(() => this.route.navigate(['/inventory/stock-issue']));
      },
      error: (err: any) => {
        this.showMessage(this.extractErrorMessage(err,
          'Unable to save the stock issue right now. Please try again.'), 'error', 'Error Message');
        this.hideLoadingSpinner();
      }
    });
  }

  onCancelClick(): void { this.route.navigate(['/inventory/stock-issue']); }

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
}
