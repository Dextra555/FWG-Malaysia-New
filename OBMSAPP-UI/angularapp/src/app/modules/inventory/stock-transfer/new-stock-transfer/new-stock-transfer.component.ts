import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';
import { forkJoin } from 'rxjs';

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
  selector: 'app-new-stock-transfer',
  templateUrl: './new-stock-transfer.component.html',
  styleUrls: ['./new-stock-transfer.component.css']
})
export class NewStockTransferComponent implements OnInit {

  frm!: FormGroup;
  isViewMode: boolean = false;
  ID: any;

  warehouses: any[] = [];
  items: any[] = [];
  availableStockMap: { [key: number]: number } = {};
  // Search filter state
  srcWarehouseSearch = '';  filteredSrcWarehouses: any[] = [];
  destWarehouseSearch = ''; filteredDestWarehouses: any[] = [];
  itemSearch = '';           filteredItems: any[] = [];

  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  /** BSR ID that originated this transfer (null if not BSR-originated). */
  fromBSR: number | null = null;

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
      ID:                [0],
      TransferNo:        [''],
      TransferDate:      [new Date(), Validators.required],
      SourceWarehouseID: [0, [Validators.required, mustSelect()]],
      DestWarehouseID:   [0, [Validators.required, mustSelect()]],
      Remarks:           [''],
      Details:           this.fb.array([])
    });
  }

  get details(): FormArray { return this.frm.get('Details') as FormArray; }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.fromBSR = this.activatedRoute.snapshot.queryParams['fromBSR']
      ? +this.activatedRoute.snapshot.queryParams['fromBSR']
      : null;
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Stock Transfer');

    if (this.ID && this.ID !== '0') {
      // View / edit an existing stock transfer
      this.isViewMode = true;
      this.loadWarehousesAndItems(() => this.loadExisting());
    } else {
      // New transfer — check if we arrived from a BSR "Create Stock Transfer" action
      if (this.fromBSR) {
        this.service.getStockTransferCode().subscribe({ next: (d: any) => this.frm.get('TransferNo')?.setValue(d.Code), error: () => {} });
        this.loadWarehousesAndItems(() => this.prefillFromBSR(this.fromBSR!));
      } else {
        this.service.getStockTransferCode().subscribe({ next: (d: any) => this.frm.get('TransferNo')?.setValue(d.Code), error: () => {} });
        this.loadWarehousesAndItems(() => this.addRow());
      }
    }
  }

  /** Loads warehouses + items in parallel, then calls the given callback once both are ready. */
  private loadWarehousesAndItems(onReady: () => void): void {
    forkJoin({
      warehouses: this.service.getWarehouseMasters(),
      items: this.service.getItemsForStock()
    }).subscribe({
      next: ({ warehouses, items }: any) => {
        this.warehouses = (warehouses || []).filter((w: any) => w.IsActive);
        this.filteredSrcWarehouses  = [...this.warehouses];
        this.filteredDestWarehouses = [...this.warehouses];
        this.items = items || [];
        this.filteredItems = [...this.items];
        onReady();
      },
      error: () => { this.errorMessage = 'Unable to load master data. Please refresh the page.'; }
    });
  }

  /** Pre-fills the form using the BSR record identified by bsrID. */
  private prefillFromBSR(bsrID: number): void {
    this.showLoadingSpinner = true;
    this.service.getBSRByID(bsrID).subscribe({
      next: (bsr: any) => {
        if (!bsr) { this.addRow(); this.hideLoadingSpinner(); return; }

        // Set source warehouse from BSR
        this.frm.get('SourceWarehouseID')?.setValue(bsr.SourceWarehouseID ?? 0);

        // Auto-fill destination warehouse: find the warehouse that belongs to the requesting branch
        const destWH = this.warehouses.find(w => w.BranchID === bsr.RequestingBranchID);
        if (destWH) {
          this.frm.get('DestWarehouseID')?.setValue(destWH.ID);
        }

        this.details.clear();
        const lines: any[] = bsr.Details || [];
        lines.forEach((line: any, idx: number) => {
          this.details.push(this.fb.group({
            ID:          [0],
            ItemID:      [line.ItemID,      [Validators.required, mustSelect()]],
            UOMID:       [line.UOMID ?? null],
            TransferQty: [line.RequestedQty ?? line.TransferQty ?? '', [Validators.required, greaterThanZero()]],
            UnitCost:    [line.UnitCost ?? 0],
            Remarks:     [line.Remarks ?? ''],
            _ItemName:   [line.ItemName ?? ''],
            _UOMName:    [line.UOMName  ?? ''],
            _Available:  [0]
          }));
          // Load available stock for each pre-filled item
          this.loadAvailableStock(idx);
        });

        if (lines.length === 0) { this.addRow(); }
        this.hideLoadingSpinner();
      },
      error: () => {
        this.errorMessage = 'Unable to load Branch Stock Request data. Please try again.';
        this.addRow();
        this.hideLoadingSpinner();
      }
    });
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

  loadExisting(): void {
    this.showLoadingSpinner = true;
    this.service.getStockTransferByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) { this.errorMessage = 'Transfer not found.'; this.hideLoadingSpinner(); return; }
        this.frm.patchValue({
          ID: d.ID, TransferNo: d.TransferNo, TransferDate: new Date(d.TransferDate),
          SourceWarehouseID: d.SourceWarehouseID, DestWarehouseID: d.DestWarehouseID, Remarks: d.Remarks
        });
        this.errorMessage = '';   // clear any stale default-warehouse warning
        this.details.clear();
        (d.Details || []).forEach((line: any) => {
          this.details.push(this.fb.group({
            ID: [line.ID], ItemID: [line.ItemID], UOMID: [line.UOMID],
            TransferQty: [line.TransferQty], UnitCost: [line.UnitCost],
            Remarks: [line.Remarks], _ItemName: [line.ItemName], _UOMName: [line.UOMName], _Available: [0]
          }));
        });
        this.frm.disable();
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load transfer. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  addRow(): void {
    this.details.push(this.fb.group({
      ID: [0], ItemID: [0, [Validators.required, mustSelect()]], UOMID: [null],
      TransferQty: ['', [Validators.required, greaterThanZero()]], UnitCost: [0],
      Remarks: [''], _ItemName: [''], _UOMName: [''], _Available: [0]
    }));
  }

  removeRow(i: number): void { this.details.removeAt(i); delete this.availableStockMap[i]; }

  onSourceWarehouseChange(): void {
    this.details.controls.forEach((_, i) => {
      const itemID = this.details.at(i).get('ItemID')?.value;
      if (itemID && itemID > 0) this.loadAvailableStock(i);
    });
  }

  onItemChange(i: number): void {
    const line = this.details.at(i);
    const selected = this.items.find(x => x.ID === +line.get('ItemID')?.value);
    if (selected) {
      line.get('_ItemName')?.setValue(selected.Name);
      line.get('_UOMName')?.setValue(selected.UOMName || '');
      line.get('UOMID')?.setValue(selected.UOMID || null);
      line.get('UnitCost')?.setValue(selected.Price || 0);

      // ── Default Warehouse auto-fill (Source Warehouse only) ──────────────
      // Auto-fill SourceWarehouseID from item's default warehouse when:
      //   • Not in view mode (existing transfer is read-only)
      //   • SourceWarehouseID is not yet selected (0)
      // BSR-originated transfers already have SourceWarehouseID set from
      // prefillFromBSR(), so the currentWH===0 guard naturally protects them.
      // Destination warehouse is NEVER auto-filled — user always picks it.
      if (!this.isViewMode) {
        const currentWH = +this.frm.get('SourceWarehouseID')?.value;
        if (currentWH === 0 && selected.WarehouseID) {
          const whExists = this.warehouses.find((w: any) => w.ID === selected.WarehouseID);
          if (whExists) {
            this.frm.get('SourceWarehouseID')?.setValue(selected.WarehouseID);
          } else {
            // Default warehouse is inactive — warn user, leave field empty
            this.errorMessage =
              'The default warehouse for this item is inactive. Please select another warehouse.';
          }
        }
      }
    }
    this.loadAvailableStock(i);
  }

  loadAvailableStock(i: number): void {
    const whID   = +this.frm.get('SourceWarehouseID')?.value;
    const itemID = +this.details.at(i).get('ItemID')?.value;
    if (whID > 0 && itemID > 0) {
      this.service.getCurrentStock(whID, itemID).subscribe({
        next: (res: any) => { this.availableStockMap[i] = res?.currentStock ?? 0; this.details.at(i).get('_Available')?.setValue(res?.currentStock ?? 0); },
        error: () => { this.availableStockMap[i] = 0; }
      });
    }
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  onSubmit(): void {
    if (this.isViewMode) { this.route.navigate(['/inventory/stock-transfer']); return; }
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }

    const raw = this.frm.getRawValue();
    if (+raw.SourceWarehouseID === +raw.DestWarehouseID) {
      this.showMessage('Source and destination warehouses must be different.', 'warning', 'Warning Message');
      return;
    }
    if (this.details.length === 0) {
      this.showMessage('Please add at least one item to the transfer.', 'warning', 'Warning Message');
      return;
    }

    for (let i = 0; i < this.details.length; i++) {
      const line  = this.details.at(i);
      const qty   = +line.get('TransferQty')?.value;
      const avail = this.availableStockMap[i] ?? 0;
      if (qty > avail) {
        this.showMessage(`Insufficient stock for '${line.get('_ItemName')?.value}'. Available quantity is ${this.formatNumber(avail)}.`, 'warning', 'Warning Message');
        return;
      }
    }

    this.showLoadingSpinner = true;
    const payload = {
      ID: raw.ID, TransferNo: raw.TransferNo, TransferDate: raw.TransferDate,
      SourceWarehouseID: +raw.SourceWarehouseID, DestWarehouseID: +raw.DestWarehouseID,
      Remarks: raw.Remarks, LastUpdatedBy: this.currentUser,
      Details: raw.Details.map((d: any) => ({
        ID: d.ID, ItemID: +d.ItemID, UOMID: d.UOMID ? +d.UOMID : null,
        TransferQty: +d.TransferQty, UnitCost: +d.UnitCost || 0,
        Remarks: d.Remarks, LastUpdatedBy: this.currentUser
      }))
    };

    this.service.saveStockTransfer(payload).subscribe({
      next: (res: any) => {
        // BUG-BSR-01 FIX: If this transfer was created from a BSR "Create Transfer" action,
        // link it back to the originating BSR so the BSR shows "Transfer Created" and the
        // LinkedTransferNo is populated. The backend LinkBSRTransfer endpoint already exists
        // but was never called from here.
        if (this.fromBSR && res?.id) {
          this.service.linkBSRTransfer(this.fromBSR, res.id).subscribe({
            next: () => {
              this.showMessage(res?.message || 'Stock transfer saved and linked to stock request.', 'success', 'Success Message');
              this.route.navigateByUrl('/dummy', { skipLocationChange: true })
                .then(() => this.route.navigate(['/inventory/branch-stock-request/new-bsr', this.fromBSR]));
            },
            error: () => {
              // Transfer saved successfully — link failed silently.
              // User can still see the transfer; they can re-link manually if needed.
              this.showMessage(
                (res?.message || 'Stock transfer saved.') +
                ' Note: Unable to link the transfer to the stock request automatically. Please contact your administrator.',
                'warning', 'Warning Message'
              );
              this.route.navigateByUrl('/dummy', { skipLocationChange: true })
                .then(() => this.route.navigate(['/inventory/stock-transfer']));
            }
          });
        } else {
          // Normal (non-BSR) transfer — navigate to list as before
          this.showMessage(res?.message || 'Stock transfer saved successfully.', 'success', 'Success Message');
          this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => this.route.navigate(['/inventory/stock-transfer']));
        }
      },
      error: (err: any) => {
        this.showMessage(this.extractErrorMessage(err, 'Unable to save the transfer right now. Please try again.'), 'error', 'Error Message');
        this.hideLoadingSpinner();
      }
    });
  }

  onCancelClick(): void { this.route.navigate(['/inventory/stock-transfer']); }

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
  onSrcWarehouseOpen(opened: boolean)  { if (opened) { this.srcWarehouseSearch  = ''; this.filteredSrcWarehouses  = [...this.warehouses]; } }
  onDestWarehouseOpen(opened: boolean) { if (opened) { this.destWarehouseSearch = ''; this.filteredDestWarehouses = [...this.warehouses]; } }
  onItemOpen(opened: boolean)          { if (opened) { this.itemSearch = ''; this.filteredItems = [...this.items]; } }
  onSrcWarehouseSearch(q: string)  { this.srcWarehouseSearch  = q; const s = q.toLowerCase(); this.filteredSrcWarehouses  = this.warehouses.filter(w => w.Name?.toLowerCase().includes(s)); }
  onDestWarehouseSearch(q: string) { this.destWarehouseSearch = q; const s = q.toLowerCase(); this.filteredDestWarehouses = this.warehouses.filter(w => w.Name?.toLowerCase().includes(s)); }
  onItemSearch(q: string)          { this.itemSearch = q;          const s = q.toLowerCase(); this.filteredItems          = this.items.filter(i => i.Name?.toLowerCase().includes(s) || i.Code?.toLowerCase().includes(s)); }
}
