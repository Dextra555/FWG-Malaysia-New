import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { UserAccessModel } from '../../../../model/userAccesModel';

function mustSelect(): ValidatorFn {
  return (c: AbstractControl) => (+c.value === 0 ? { mustSelect: true } : null);
}
function greaterThanZero(): ValidatorFn {
  return (c: AbstractControl) => {
    const v = parseFloat(c.value); return isNaN(v) || v <= 0 ? { greaterThanZero: true } : null;
  };
}

@Component({
  selector: 'app-new-stock-adjustment',
  templateUrl: './new-stock-adjustment.component.html',
  styleUrls: ['./new-stock-adjustment.component.css']
})
export class NewStockAdjustmentComponent implements OnInit {

  frm!: FormGroup;
  isViewMode: boolean = false;
  ID: any;

  warehouses: any[] = [];
  items: any[] = [];
  // Search filter state
  warehouseSearch = ''; filteredWarehouses: any[] = [];
  itemSearch = '';      filteredItems: any[] = [];

  currentStock: number = 0;
  selectedItemName: string = '';
  selectedItemCode: string = '';
  selectedUOMName: string = '';

  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  constructor(
    private fb: FormBuilder,
    private activatedRoute: ActivatedRoute,
    private route: Router,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.frm = this.fb.group({
      ID:             [0],
      AdjustmentNo:   [''],
      AdjustmentDate: [new Date(), Validators.required],
      WarehouseID:    [0, [Validators.required, mustSelect()]],
      ItemID:         [0, [Validators.required, mustSelect()]],
      AdjustmentType: ['', Validators.required],
      AdjustmentQty:  ['', [Validators.required, greaterThanZero()]],
      UnitCost:       [0],
      Reason:         ['', Validators.required],
      Remarks:        ['']
    });
  }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Stock Adjustment');
    this.loadWarehouses();
    this.loadItems();

    if (this.ID && this.ID !== '0') {
      this.isViewMode = true;
      this.loadExisting();
    } else {
      this.service.getStockAdjustmentCode().subscribe({ next: (d: any) => this.frm.get('AdjustmentNo')?.setValue(d.Code), error: () => {} });
    }

    // Watch warehouse + item changes to refresh current stock
    this.frm.get('WarehouseID')!.valueChanges.subscribe(() => this.refreshCurrentStock());
    this.frm.get('ItemID')!.valueChanges.subscribe(() => this.onItemChange());
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
        }
      }, () => {}
    );
  }

  loadWarehouses(): void {
    this.service.getWarehouseMasters().subscribe({
      next: (d: any[]) => { this.warehouses = (d || []).filter((w: any) => w.IsActive); this.filteredWarehouses = [...this.warehouses]; },
      error: () => { this.errorMessage = 'Unable to load warehouse list. Please refresh the page.'; }
    });
  }

  loadItems(): void {
    this.service.getItemsForStock().subscribe({
      next: (d: any[]) => { this.items = d || []; this.filteredItems = [...this.items]; },
      error: () => { this.errorMessage = 'Unable to load item list. Please refresh the page.'; }
    });
  }

  onItemChange(): void {
    const itemID   = +this.frm.get('ItemID')?.value;
    const selected = this.items.find(x => x.ID === itemID);
    if (selected) {
      this.selectedItemCode = selected.Code || '';
      this.selectedItemName = selected.Name || '';
      this.selectedUOMName  = selected.UOMName || '';
      this.frm.get('UnitCost')?.setValue(selected.Price || 0);
    } else {
      this.selectedItemCode = this.selectedItemName = this.selectedUOMName = '';
    }
    this.refreshCurrentStock();
  }

  refreshCurrentStock(): void {
    const whID   = +this.frm.get('WarehouseID')?.value;
    const itemID = +this.frm.get('ItemID')?.value;
    if (whID > 0 && itemID > 0) {
      this.service.getCurrentStock(whID, itemID).subscribe({
        next: (res: any) => { this.currentStock = res?.currentStock ?? 0; },
        error: () => { this.currentStock = 0; }
      });
    } else {
      this.currentStock = 0;
    }
  }

  loadExisting(): void {
    this.showLoadingSpinner = true;
    this.service.getStockAdjustmentByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) { this.errorMessage = 'Adjustment not found.'; this.hideLoadingSpinner(); return; }
        this.frm.patchValue({
          ID: d.ID, AdjustmentNo: d.AdjustmentNo, AdjustmentDate: new Date(d.AdjustmentDate),
          WarehouseID: d.WarehouseID, ItemID: d.ItemID, AdjustmentType: d.AdjustmentType,
          AdjustmentQty: d.AdjustmentQty, UnitCost: d.UnitCost, Reason: d.Reason, Remarks: d.Remarks
        });
        this.selectedItemName = d.ItemName || '';
        this.selectedItemCode = d.ItemCode || '';
        this.selectedUOMName  = d.UOMName  || '';
        this.frm.disable();
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load the adjustment. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  onSubmit(): void {
    if (this.isViewMode) { this.route.navigate(['/inventory/stock-adjustment']); return; }
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }

    const raw = this.frm.getRawValue();

    // Client-side stock check for MINUS
    if (raw.AdjustmentType === 'MINUS' && +raw.AdjustmentQty > this.currentStock) {
      this.showMessage(
        `Insufficient stock. Available quantity is ${this.formatNumber(this.currentStock)}.`,
        'warning', 'Warning Message');
      return;
    }

    this.showLoadingSpinner = true;
    const payload = {
      ID:             raw.ID,
      AdjustmentNo:   raw.AdjustmentNo,
      AdjustmentDate: raw.AdjustmentDate,
      WarehouseID:    +raw.WarehouseID,
      ItemID:         +raw.ItemID,
      AdjustmentType: raw.AdjustmentType,
      AdjustmentQty:  +raw.AdjustmentQty,
      UnitCost:       +raw.UnitCost || 0,
      Reason:         raw.Reason,
      Remarks:        raw.Remarks,
      LastUpdatedBy:  this.currentUser
    };

    this.service.saveStockAdjustment(payload).subscribe({
      next: (res: any) => {
        this.showMessage(res?.message || 'Adjustment saved successfully.', 'success', 'Success Message');
        this.route.navigateByUrl('/dummy', { skipLocationChange: true })
          .then(() => this.route.navigate(['/inventory/stock-adjustment']));
      },
      error: (err: any) => {
        this.showMessage(this.extractErrorMessage(err, 'Unable to save the adjustment right now. Please try again.'), 'error', 'Error Message');
        this.hideLoadingSpinner();
      }
    });
  }

  onCancelClick(): void { this.route.navigate(['/inventory/stock-adjustment']); }

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
  onWarehouseOpen(opened: boolean) { if (opened) { this.warehouseSearch = ''; this.filteredWarehouses = [...this.warehouses]; } }
  onItemOpen(opened: boolean)      { if (opened) { this.itemSearch = '';      this.filteredItems      = [...this.items];      } }
  onWarehouseSearch(q: string) { this.warehouseSearch = q; const s = q.toLowerCase(); this.filteredWarehouses = this.warehouses.filter(w => w.Name?.toLowerCase().includes(s)); }
  onItemSearch(q: string)      { this.itemSearch = q;      const s = q.toLowerCase(); this.filteredItems      = this.items.filter(i => i.Name?.toLowerCase().includes(s) || i.Code?.toLowerCase().includes(s)); }
}
