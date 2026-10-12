import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { UserAccessModel } from '../../../../model/userAccesModel';
import { environment } from '../../../../../environments/environment';

/** Validator: value must be > 0 */
function greaterThanZero(): ValidatorFn {
  return (control: AbstractControl) => {
    const v = parseFloat(control.value);
    return (isNaN(v) || v <= 0) ? { greaterThanZero: true } : null;
  };
}

/** Validator: mat-select must not be 0 / null / '' */
function mustSelect(): ValidatorFn {
  return (control: AbstractControl) => {
    const v = control.value;
    return (v === null || v === undefined || v === 0 || v === '') ? { mustSelect: true } : null;
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
  /** Full list loaded from API (filtered by warehouse + role) */
  employeeList:      any[] = [];
  branchList:        any[] = [];
  /** Currently displayed in the searchable dropdown */
  filteredRecipients: any[] = [];
  recipientSearch:   string = '';

  /** true while loading employee list after warehouse/type change */
  loadingEmployees: boolean = false;

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
    private _masterService: MastermoduleService
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

  loadBranchList(): void {
    this.service.getBranchListByUser(this.currentUser).subscribe({
      next: (d: any[]) => { this.branchList = d || []; },
      error: () => {}
    });
  }

  // ── Called when Warehouse selection changes ────────────────────────────────
  onWarehouseChange(): void {
    // Reset item stock map
    this.availableStockMap = {};
    // Reload available stock for any existing rows
    for (let i = 0; i < this.details.length; i++) {
      const itemID = this.details.at(i).get('ItemID')?.value;
      if (itemID && itemID !== 0) this.loadAvailableStock(i);
    }
    // Reload employee list for current type (if Guard/Staff already selected)
    this.reloadEmployeesByWarehouseAndType();
  }

  // ── Called when Issue To Type changes ─────────────────────────────────────
  onIssueToTypeChange(): void {
    this.frm.get('IssueToID')?.setValue(null);
    this.frm.get('IssueToName')?.setValue('');
    this.recipientSearch   = '';
    this.filteredRecipients = [];
    this.reloadEmployeesByWarehouseAndType();
  }

  /**
   * Core logic: load employees from the API filtered by warehouse branch + role.
   * Only fires when IssueToType is Guard or Staff AND a warehouse is selected.
   * For Branch/Other, uses branchList (already loaded) or clears the list.
   */
  private reloadEmployeesByWarehouseAndType(): void {
    const type        = this.frm.get('IssueToType')?.value;
    const warehouseID = +(this.frm.get('WarehouseID')?.value || 0);

    if (type === 'Staff' || type === 'Guard') {
      if (warehouseID <= 0) {
        this.employeeList       = [];
        this.filteredRecipients = [];
        return;
      }
      this.loadingEmployees = true;
      this.service.getEmployeesByWarehouse(warehouseID, type).subscribe({
        next: (d: any[]) => {
          this.employeeList       = d || [];
          this.filteredRecipients = [...this.employeeList];
          this.loadingEmployees   = false;
        },
        error: () => {
          this.employeeList       = [];
          this.filteredRecipients = [];
          this.loadingEmployees   = false;
        }
      });
    } else if (type === 'Branch') {
      this.filteredRecipients = [...this.branchList];
    } else {
      this.filteredRecipients = [];
    }
  }

  // Returns the right source list based on current type
  getSourceList(): any[] {
    const type = this.frm.get('IssueToType')?.value;
    if (type === 'Staff' || type === 'Guard') return this.employeeList;
    if (type === 'Branch')                    return this.branchList;
    return [];
  }

  // Returns display label depending on list type
  getRecipientLabel(item: any): string {
    const type = this.frm.get('IssueToType')?.value;
    if (type === 'Branch') {
      return `${item.Code || ''} — ${item.Name || ''}`.trim().replace(/^—\s*/, '');
    }
    // Staff / Guard
    return `${item.EMP_NAME || ''} (${item.EMP_ROLE || ''} — ID: ${item.EMP_ID || ''})`;
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
      // Employee / Guard
      this.frm.get('IssueToID')?.setValue(item.EMP_ID);
      this.frm.get('IssueToName')?.setValue(item.EMP_NAME || '');
    }
  }

  // ── Load existing record (view mode) ──────────────────────────────────────
  loadExistingRecord(): void {
    this.showLoadingSpinner = true;
    this.service.getStockIssueByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) { this.errorMessage = 'Stock issue not found.'; this.hideLoadingSpinner(); return; }
        this.frm.patchValue({
          ID:          d.ID,
          IssueNo:     d.IssueNo,
          IssueDate:   d.IssueDate ? new Date(d.IssueDate) : new Date(),
          WarehouseID: d.WarehouseID,
          IssueToType: d.IssueToType,
          IssueToID:   d.IssueToID,
          IssueToName: d.IssueToName,
          Remarks:     d.Remarks
        });
        this.warningMessage = '';   // clear any stale default-warehouse warning
        this.frm.disable();

        const detailsArray = this.frm.get('Details') as FormArray;
        detailsArray.clear();
        (d.Details || []).forEach((line: any) => {
          const row = this.fb.group({
            ID:       [line.ID],
            ItemID:   [line.ItemID,  [Validators.required, mustSelect()]],
            _ItemName:[line.ItemName || ''],
            UOMID:    [line.UOMID],
            IssueQty: [line.IssueQty, [Validators.required, greaterThanZero()]],
            UnitCost: [line.UnitCost || 0],
            Remarks:  [line.Remarks]
          });
          detailsArray.push(row);
        });
        this.hideLoadingSpinner();
      },
      error: () => {
        this.errorMessage = 'Unable to load this stock issue. Please try again.';
        this.hideLoadingSpinner();
      }
    });
  }

  // ── Detail rows ────────────────────────────────────────────────────────────
  addDetailRow(): void {
    this.details.push(this.fb.group({
      ID:       [0],
      ItemID:   [0,   [Validators.required, mustSelect()]],
      _ItemName:[''],
      UOMID:    [null],
      IssueQty: [null, [Validators.required, greaterThanZero()]],
      UnitCost: [0],
      Remarks:  ['']
    }));
  }

  removeDetailRow(i: number): void {
    this.details.removeAt(i);
    delete this.availableStockMap[i];
    // Re-key the map
    const newMap: { [key: number]: number } = {};
    Object.keys(this.availableStockMap).forEach(k => {
      const ki = +k;
      if (ki > i) newMap[ki - 1] = this.availableStockMap[ki];
      else if (ki < i) newMap[ki] = this.availableStockMap[ki];
    });
    this.availableStockMap = newMap;
  }

  onItemChange(i: number): void {
    const row  = this.details.at(i);
    const item = this.items.find((it: any) => it.ID === row.get('ItemID')?.value);
    row.get('_ItemName')?.setValue(item?.Name || '');
    row.get('UnitCost')?.setValue(item?.Price ?? 0);

    // ── Default Warehouse auto-fill ──────────────────────────────────────────
    // Only populate when header warehouse is not yet selected (new document).
    // Never overwrite a warehouse the user has already chosen.
    // View mode (isViewMode=true) cannot reach here — form is disabled.
    const currentWarehouse = this.frm.get('WarehouseID')?.value;
    if ((!currentWarehouse || +currentWarehouse === 0) && item?.WarehouseID) {
      const whExists = this.warehouses.find((w: any) => w.ID === item.WarehouseID);
      if (whExists) {
        this.frm.get('WarehouseID')?.setValue(item.WarehouseID);
        // Re-trigger employee list reload now that warehouse is known
        this.reloadEmployeesByWarehouseAndType();
      } else {
        // Default warehouse is inactive — warn user, leave field empty
        this.warningMessage =
          'The default warehouse for this item is inactive. Please select another warehouse.';
      }
    }

    this.loadAvailableStock(i);
  }

  loadAvailableStock(i: number): void {
    const warehouseID = +(this.frm.get('WarehouseID')?.value || 0);
    const itemID      = +(this.details.at(i).get('ItemID')?.value || 0);
    if (warehouseID <= 0 || itemID <= 0) return;
    this.service.getCurrentStock(warehouseID, itemID).subscribe({
      next: (d: any) => {
        // API returns { currentStock: number } — fall back through all known shapes
        this.availableStockMap[i] =
          typeof d === 'number'
            ? d
            : (d?.currentStock ?? d?.CurrentStock ?? d?.quantity ?? d?.Quantity ?? 0);
      },
      error: () => { this.availableStockMap[i] = 0; }
    });
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  get issueToType(): string  { return this.frm.get('IssueToType')?.value || ''; }
  get issueToName(): string  { return this.frm.get('IssueToName')?.value || ''; }
  get viewWarehouseName(): string {
    const id = this.frm.get('WarehouseID')?.value;
    return this.warehouses.find((w: any) => w.ID === id)?.Name || '—';
  }
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

  /** Open the Stock Issue PDF in a new browser tab for printing or downloading.
   *  Only callable in view mode (existing saved document). Read-only — does not
   *  modify any record, stock ledger, or salary deduction. */
  onPrintPDF(): void {
    // Guard: this.ID is the route param; frm.get('ID') holds the DB id after patchValue.
    // Both are the same value when isViewMode is true — use this.ID as primary.
    const docID = this.ID || this.frm.get('ID')?.value;
    if (!docID || +docID === 0) return;
    const url = environment.baseReportUrl
      + 'Inventory/StockIssueReport.aspx'
      + '?LoginID='    + encodeURIComponent(this.currentUser)
      + '&IssueDocID=' + encodeURIComponent(docID);
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
}
