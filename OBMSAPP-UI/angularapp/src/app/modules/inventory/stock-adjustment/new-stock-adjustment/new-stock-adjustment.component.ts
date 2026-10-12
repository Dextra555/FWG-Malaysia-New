import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { DialogConfirmationComponent } from '../../../../components/dialog-confirmation/dialog-confirmation.component';
import { UserAccessModel } from '../../../../model/userAccesModel';
import { environment } from '../../../../../environments/environment';

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
  isEdit: boolean = false;
  ID: any;

  warehouses: any[] = [];
  items: any[] = [];
  warehouseSearch = ''; filteredWarehouses: any[] = [];
  itemSearch = '';      filteredItems: any[] = [];

  currentStock: number = 0;
  selectedItemName: string = '';
  selectedItemCode: string = '';
  selectedUOMName: string = '';

  // ── Approval workflow state ──────────────────────────────────────────────
  currentStatus   = '';    // Draft | Submitted | Approved | Rejected
  submittedBy     = '';
  approvedBy      = '';
  approvalRemarks = '';

  isApprover        = false;
  approverCheckDone = false;
  approverName      = '';

  // Role from sessionStorage (set at login from OBMSUsers.IsAdmin)
  userRole = '';

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
    this.currentUser = sessionStorage.getItem('username') || '';
    const rawRole = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = rawRole === '2' ? 'superadmin' : rawRole === '1' ? 'admin' : 'user';
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Stock Adjustment');
    this.loadWarehouses();
    this.loadItems();

    if (this.ID && this.ID !== '0') {
      this.isEdit = true;
      this.loadExisting();
    } else {
      this.service.getStockAdjustmentCode().subscribe({
        next: (d: any) => this.frm.get('AdjustmentNo')?.setValue(d.Code),
        error: () => {}
      });
    }

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
      if (!this.isEdit) {
        const currentWH = +this.frm.get('WarehouseID')?.value;
        if (currentWH === 0 && selected.WarehouseID) {
          const whExists = this.warehouses.find((w: any) => w.ID === selected.WarehouseID);
          if (whExists) {
            this.frm.get('WarehouseID')?.setValue(selected.WarehouseID);
          } else {
            this.warningMessage = 'The default warehouse for this item is inactive. Please select another warehouse.';
          }
        }
      }
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
        this.currentStatus   = d.Status || 'Draft';
        this.submittedBy     = d.SubmittedBy || d.LastUpdatedBy || '';
        this.approvedBy      = d.ApprovedBy || '';
        this.approvalRemarks = d.ApprovalRemarks || '';

        this.frm.patchValue({
          ID: d.ID, AdjustmentNo: d.AdjustmentNo, AdjustmentDate: new Date(d.AdjustmentDate),
          WarehouseID: d.WarehouseID, ItemID: d.ItemID, AdjustmentType: d.AdjustmentType,
          AdjustmentQty: d.AdjustmentQty, UnitCost: d.UnitCost, Reason: d.Reason, Remarks: d.Remarks
        });
        this.selectedItemName = d.ItemName || '';
        this.selectedItemCode = d.ItemCode || '';
        this.selectedUOMName  = d.UOMName  || '';
        this.warningMessage   = '';

        if (this.currentStatus !== 'Draft') { this.frm.disable(); }
        this.hideLoadingSpinner();

        if (this.currentStatus === 'Submitted') {
          this.checkIsApprover();
        } else {
          this.approverCheckDone = true;
          this.isApprover = false;
        }
      },
      error: () => { this.errorMessage = 'Unable to load the adjustment. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  // ── Approver check (PR reference pattern) ───────────────────────────────
  private checkIsApprover(): void {
    if (this.isApprover && this.approverCheckDone) return;
    this.service.isCurrentUserApprover('STOCK_ADJUSTMENT', this.submittedBy || '').subscribe({
      next: (res: any) => {
        if (!this.isApprover) {
          this.isApprover   = res?.IsApprover === true;
          this.approverName = res?.ApproverUser || '';
        }
        this.approverCheckDone = true;
      },
      error: () => {
        if (!this.approverCheckDone) { this.isApprover = false; this.approverName = ''; }
        this.approverCheckDone = true;
      }
    });
  }

  // ── State helpers ────────────────────────────────────────────────────────
  canEdit(): boolean {
    return !this.isEdit || this.currentStatus === 'Draft';
  }

  canSubmit(): boolean {
    // Submit visible on new form (not yet saved) OR on an existing Draft
    return !this.isEdit || this.currentStatus === 'Draft';
  }

  canApproveAction(): boolean {
    if (!this.isEdit || this.currentStatus !== 'Submitted') return false;
    if (this.userRole === 'superadmin') return true;
    return this.isApprover && this.approverCheckDone;
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  // ── Actions ──────────────────────────────────────────────────────────────

  // Save Draft only — no submit
  onSave(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    const raw = this.frm.getRawValue();
    this.showLoadingSpinner = true;
    const payload = this.buildPayload(raw);

    this.service.saveStockAdjustment(payload).subscribe({
      next: (res: any) => {
        this.showMessage(res?.message || 'Adjustment saved as Draft.', 'success', 'Success Message');
        if (res?.id) {
          this.route.navigate(['/inventory/stock-adjustment/new-stock-adjustment', res.id]);
        } else {
          this.route.navigateByUrl('/dummy', { skipLocationChange: true })
            .then(() => this.route.navigate(['/inventory/stock-adjustment']));
        }
      },
      error: (err: any) => {
        this.showMessage(this.extractErrorMessage(err, 'Unable to save right now. Please try again.'), 'error', 'Error Message');
        this.hideLoadingSpinner();
      }
    });
  }

  // Submit = save (if new) then submit — mirrors PR onSubmitDirect
  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    const raw   = this.frm.getRawValue();
    const label = this.isEdit ? raw.AdjustmentNo : 'this adjustment';

    this.dialog.open(DialogConfirmationComponent, {
      data: { message: `Submit Stock Adjustment <b>${label}</b> for approval?` }
    }).afterClosed().subscribe((result: any) => {
      if (!result?.confirmDialog) return;
      this.showLoadingSpinner = true;

      if (this.isEdit) {
        // Already a saved Draft — just submit
        this.service.submitSA(+raw.ID).subscribe({
          next: (res: any) => {
            this.showMessage(res?.message || 'Submitted for approval.', 'success', 'Success Message');
            this.loadExisting();
          },
          error: (err: any) => {
            this.showMessage(this.extractErrorMessage(err, 'Unable to submit right now. Please try again.'), 'error', 'Error Message');
            this.hideLoadingSpinner();
          }
        });
      } else {
        // New form — save as Draft first, then immediately submit
        const payload = this.buildPayload(raw);
        this.service.saveStockAdjustment(payload).subscribe({
          next: (saveRes: any) => {
            const newID = saveRes?.id;
            if (!newID) {
              this.showMessage('Unable to save the adjustment. Please try again.', 'error', 'Error Message');
              this.hideLoadingSpinner();
              return;
            }
            this.service.submitSA(+newID).subscribe({
              next: (submitRes: any) => {
                this.showMessage(submitRes?.message || 'Submitted for approval.', 'success', 'Success Message');
                this.route.navigateByUrl('/dummy', { skipLocationChange: true })
                  .then(() => this.route.navigate(['/inventory/stock-adjustment/new-stock-adjustment', newID]));
              },
              error: (err: any) => {
                this.showMessage(this.extractErrorMessage(err, 'Saved as Draft but submit failed. Open the draft and retry.'), 'error', 'Error Message');
                this.route.navigateByUrl('/dummy', { skipLocationChange: true })
                  .then(() => this.route.navigate(['/inventory/stock-adjustment/new-stock-adjustment', newID]));
              }
            });
          },
          error: (err: any) => {
            this.showMessage(this.extractErrorMessage(err, 'Unable to save right now. Please try again.'), 'error', 'Error Message');
            this.hideLoadingSpinner();
          }
        });
      }
    });
  }

  onApprove(): void {
    const id = this.frm.get('ID')?.value;
    this.showLoadingSpinner = true;
    this.service.approveSA(+id, { ApprovalRemarks: '' }).subscribe({
      next: (res: any) => {
        this.showMessage(res?.message || 'Approved. Stock has been updated.', 'success', 'Success Message');
        this.loadExisting();
      },
      error: (err: any) => {
        this.showMessage(this.extractErrorMessage(err, 'Unable to approve right now. Please try again.'), 'error', 'Error Message');
        this.hideLoadingSpinner();
      }
    });
  }

  onReject(): void {
    const id = this.frm.get('ID')?.value;
    Swal.fire({
      title: 'Reject Stock Adjustment',
      text: 'Please enter the reason for rejection:',
      input: 'textarea',
      inputPlaceholder: 'Enter rejection reason here…',
      inputAttributes: { rows: '3' },
      showCancelButton: true,
      confirmButtonText: 'Reject',
      confirmButtonColor: '#d33',
      cancelButtonText: 'Cancel',
      inputValidator: (value) => {
        if (!value || value.trim() === '') return 'Please enter a reason for rejection.';
        return null;
      }
    }).then(result => {
      if (!result.isConfirmed || !result.value?.trim()) return;
      this.showLoadingSpinner = true;
      this.service.rejectSA(+id, { ApprovalRemarks: result.value.trim() }).subscribe({
        next: (res: any) => {
          this.showMessage(res?.message || 'Rejected.', 'warning', 'Warning Message');
          this.loadExisting();
        },
        error: (err: any) => {
          this.showMessage(this.extractErrorMessage(err, 'Unable to reject right now. Please try again.'), 'error', 'Error Message');
          this.hideLoadingSpinner();
        }
      });
    });
  }

  onCancelClick(): void { this.route.navigate(['/inventory/stock-adjustment']); }

  /** Open the Stock Adjustment PDF in a new browser tab for printing or downloading.
   *  Only callable when isEdit=true (existing saved document). Read-only — does not
   *  modify any record, stock ledger, ADJ_PLUS/ADJ_MINUS entries, or balances. */
  onPrintPDF(): void {
    const docID = this.ID || this.frm.get('ID')?.value;
    if (!docID || +docID === 0) return;
    const url = environment.baseReportUrl
      + 'Inventory/StockAdjustmentReport.aspx'
      + '?LoginID='      + encodeURIComponent(this.currentUser)
      + '&AdjustmentID=' + encodeURIComponent(docID);
    window.open(url, '_blank');
  }

  // ── Helpers ──────────────────────────────────────────────────────────────
  private buildPayload(raw: any) {
    return {
      ID:             raw.ID || 0,
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
  onWarehouseOpen(opened: boolean) { if (opened) { this.warehouseSearch = ''; this.filteredWarehouses = [...this.warehouses]; } }
  onItemOpen(opened: boolean)      { if (opened) { this.itemSearch = '';      this.filteredItems      = [...this.items];      } }
  onWarehouseSearch(q: string) { this.warehouseSearch = q; const s = q.toLowerCase(); this.filteredWarehouses = this.warehouses.filter(w => w.Name?.toLowerCase().includes(s)); }
  onItemSearch(q: string)      { this.itemSearch = q;      const s = q.toLowerCase(); this.filteredItems      = this.items.filter(i => i.Name?.toLowerCase().includes(s) || i.Code?.toLowerCase().includes(s)); }
}
