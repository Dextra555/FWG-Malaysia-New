import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { UserAccessModel } from '../../../../model/userAccesModel';

/** Custom validator: rejects null, undefined, empty string, and 0 — for required dropdowns */
function mustSelect(): ValidatorFn {
  return (control: AbstractControl) => {
    const v = control.value;
    if (v === null || v === undefined || v === '' || +v === 0) {
      return { mustSelect: true };
    }
    return null;
  };
}

/** Custom validator: value must be > 0 */
function greaterThanZero(): ValidatorFn {
  return (control: AbstractControl) => {
    const v = parseFloat(control.value);
    return isNaN(v) || v <= 0 ? { greaterThanZero: true } : null;
  };
}

/** Custom validator: value must be >= 0 */
function nonNegative(): ValidatorFn {
  return (control: AbstractControl) => {
    const v = parseFloat(control.value);
    return isNaN(v) || v < 0 ? { nonNegative: true } : null;
  };
}

@Component({
  selector: 'app-new-opening-stock',
  templateUrl: './new-opening-stock.component.html',
  styleUrls: ['./new-opening-stock.component.css']
})
export class NewOpeningStockComponent implements OnInit {

  frm!: FormGroup;
  isEdit: boolean = false;
  ID: any;

  warehouses: any[] = [];
  items: any[] = [];
  // Search filter state
  warehouseSearch = ''; filteredWarehouses: any[] = [];
  itemSearch = '';      filteredItems: any[] = [];

  // Auto-populated item detail panel (read-only display)
  selectedItemCode: string = '';
  selectedItemCategory: string = '';
  selectedItemBrand: string = '';
  selectedItemUOM: string = '';

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

    this.frm = this.fb.group({
      ID:           [0],
      ReferenceNo:  [''],                                              // Auto-generated — read-only
      WarehouseID:  [0, [Validators.required, mustSelect()]],
      ItemID:       [0, [Validators.required, mustSelect()]],
      OpeningDate:  [new Date(), Validators.required],
      OpeningQty:   ['', [Validators.required, greaterThanZero()]],
      UnitCost:     ['', [Validators.required, nonNegative()]],
      TotalValue:   [{ value: '0.00', disabled: true }],              // Calculated — read-only
      Remarks:      ['']
    });
  }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }

    this.getUserAccessRights(this.currentUser, 'Opening Stock');
    this.loadWarehouses();
    this.loadItems();

    // Subscribe to qty/cost changes to auto-calculate total value
    this.frm.get('OpeningQty')!.valueChanges.subscribe(() => this.recalculateTotalValue());
    this.frm.get('UnitCost')!.valueChanges.subscribe(() => this.recalculateTotalValue());

    if (this.ID && this.ID !== '0') {
      this.isEdit = true;
      this.loadExistingRecord();
    } else {
      this.service.getOpeningStockCode().subscribe({
        next: (d: any) => this.frm.get('ReferenceNo')?.setValue(d.Code),
        error: () => { /* non-critical — code will be generated server-side */ }
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

  loadWarehouses(): void {
    this.service.getWarehouseMasters().subscribe({
      next: (d: any[]) => { this.warehouses = (d || []).filter(w => w.IsActive); this.filteredWarehouses = [...this.warehouses]; },
      error: () => { this.errorMessage = 'Unable to load warehouse list. Please refresh the page.'; }
    });
  }

  loadItems(): void {
    this.service.getItemsForStock().subscribe({
      next: (d: any[]) => { this.items = d || []; this.filteredItems = [...this.items]; },
      error: () => { this.errorMessage = 'Unable to load item list. Please refresh the page.'; }
    });
  }

  loadExistingRecord(): void {
    this.showLoadingSpinner = true;
    this.service.getOpeningStockByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) {
          this.errorMessage = 'Opening stock entry not found. It may have been removed.';
          this.hideLoadingSpinner();
          return;
        }
        // Patch all saved values FIRST (including WarehouseID) so that
        // onItemChange() sees the warehouse already set and does NOT overwrite it.
        this.frm.patchValue({
          ID:          d.ID,
          ReferenceNo: d.ReferenceNo,
          WarehouseID: d.WarehouseID,
          ItemID:      d.ItemID,
          OpeningDate: new Date(d.OpeningDate),
          OpeningQty:  d.OpeningQty,
          UnitCost:    d.UnitCost,
          Remarks:     d.Remarks
        });
        this.warningMessage = '';   // clear any stale default-warehouse warning
        this.onItemChange(d.ItemID);
        this.recalculateTotalValue();
        this.hideLoadingSpinner();
      },
      error: () => {
        this.errorMessage = 'Unable to load the opening stock entry. Please try again.';
        this.hideLoadingSpinner();
      }
    });
  }

  onItemChange(itemID: number): void {
    const selected = this.items.find(i => i.ID === +itemID);
    if (selected) {
      this.selectedItemCode     = selected.Code         || '';
      this.selectedItemCategory = selected.CategoryName || '';
      this.selectedItemBrand    = selected.BrandName    || 'N/A';
      this.selectedItemUOM      = selected.UOMName      || '';

      // ── Auto-fill UnitCost from ItemMaster buying price ──────────────────
      // Only fill when UnitCost is empty (new doc) — never overwrite user entry
      // or a value loaded from an existing record.
      const currentCost = this.frm.get('UnitCost')?.value;
      if ((!currentCost || currentCost === '') && selected.Price != null) {
        this.frm.get('UnitCost')?.setValue(selected.Price);
      }

      // ── Default Warehouse auto-fill ──────────────────────────────────────
      // Only populate when warehouse is not yet selected (new document only).
      // Never overwrite a warehouse the user has already chosen.
      const currentWH = this.frm.get('WarehouseID')?.value;
      if ((!currentWH || +currentWH === 0) && selected.WarehouseID) {
        // Confirm the default warehouse is still active in the loaded list
        const whExists = this.warehouses.find((w: any) => w.ID === selected.WarehouseID);
        if (whExists) {
          this.frm.get('WarehouseID')?.setValue(selected.WarehouseID);
        } else if (selected.WarehouseID) {
          // Default warehouse is inactive — warn user, leave field empty
          this.warningMessage =
            'The default warehouse for this item is inactive. Please select another warehouse.';
        }
      }
    } else {
      this.selectedItemCode = this.selectedItemCategory = this.selectedItemBrand = this.selectedItemUOM = '';
    }
  }

  recalculateTotalValue(): void {
    const qty  = parseFloat(this.frm.get('OpeningQty')?.value) || 0;
    const cost = parseFloat(this.frm.get('UnitCost')?.value)   || 0;
    const total = qty * cost;
    this.frm.get('TotalValue')?.setValue(total.toFixed(2), { emitEvent: false });
  }

  onSubmit(): void {
    if (this.frm.invalid) {
      this.frm.markAllAsTouched();
      return;
    }

    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();

    const payload = {
      ID:          raw.ID,
      ReferenceNo: raw.ReferenceNo,
      WarehouseID: +raw.WarehouseID,
      ItemID:      +raw.ItemID,
      OpeningDate: raw.OpeningDate,
      OpeningQty:  +raw.OpeningQty,
      UnitCost:    +raw.UnitCost,
      Remarks:     raw.Remarks || '',
      LastUpdatedBy: this.currentUser
    };

    this.service.saveOpeningStock(payload).subscribe({
      next: (res: any) => {
        const msg = res?.message || (this.isEdit
          ? 'Opening stock updated successfully.'
          : 'Opening stock saved successfully.');
        this.showMessage(msg, 'success', 'Success Message');
        this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => {
          this.route.navigate(['/inventory/opening-stock']);
        });
      },
      error: (err: any) => {
        const msg = this.extractErrorMessage(err,
          'Unable to save the opening stock right now. Please try again. ' +
          'If the problem continues, contact your administrator.');
        this.showMessage(msg, 'error', 'Error Message');
        this.hideLoadingSpinner();
      }
    });
  }

  onCancelClick(): void {
    this.route.navigate(['/inventory/opening-stock']);
  }

  private extractErrorMessage(err: any, fallback: string): string {
    if (err && typeof err === 'string') return err;
    if (err?.error?.message) return err.error.message;
    if (err?.message)        return err.message;
    return fallback;
  }

  private showMessage(
    message: string,
    icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message' = 'Success Message'
  ): void {
    Swal.fire({
      toast: true, position: 'top', showConfirmButton: false,
      title, text: message, icon,
      showCloseButton: false, timer: 5000, width: '600px',
      customClass: { popup: 'swal-top-offset' }
    });
    this.hideLoadingSpinner();
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers ───────────────────────────────────────────
  onWarehouseOpen(opened: boolean) { if (opened) { this.warehouseSearch = ''; this.filteredWarehouses = [...this.warehouses]; } }
  onWarehouseSearch(q: string) { this.warehouseSearch = q; const s = q.toLowerCase(); this.filteredWarehouses = this.warehouses.filter(w => w.Name?.toLowerCase().includes(s)); }
  onItemOpen(opened: boolean) { if (opened) { this.itemSearch = ''; this.filteredItems = [...this.items]; } }
  onItemSearch(q: string) { this.itemSearch = q; const s = q.toLowerCase(); this.filteredItems = this.items.filter(i => i.Name?.toLowerCase().includes(s) || i.Code?.toLowerCase().includes(s)); }
}
