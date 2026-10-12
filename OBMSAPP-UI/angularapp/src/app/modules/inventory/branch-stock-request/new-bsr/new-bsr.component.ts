import { Component, OnInit } from '@angular/core';
import {
  AbstractControl, FormArray, FormBuilder,
  FormGroup, ValidatorFn, Validators
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { UserAccessModel } from '../../../../model/userAccesModel';
import { DialogConfirmationComponent } from '../../../../components/dialog-confirmation/dialog-confirmation.component';
import { environment } from '../../../../../environments/environment';

// â”€â”€ Custom validators (same pattern as new-stock-transfer) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
function mustSelect(): ValidatorFn {
  return (c: AbstractControl) => (+c.value === 0 ? { mustSelect: true } : null);
}
function greaterThanZero(): ValidatorFn {
  return (c: AbstractControl) => {
    const v = parseFloat(c.value);
    return isNaN(v) || v <= 0 ? { greaterThanZero: true } : null;
  };
}

@Component({
  selector: 'app-new-bsr',
  templateUrl: './new-bsr.component.html',
  styleUrls: ['./new-bsr.component.css']
})
export class NewBSRComponent implements OnInit {

  frm!: FormGroup;
  ID: any;
  isViewMode   = false;
  isEditMode   = false;
  currentStatus  = '';
  submittedBy    = '';
  approvedBy     = '';
  approvalRemarks = '';
  linkedTransferNo = '';
  linkedTransferID: number | null = null;

  // Approver check
  isApprover       = false;
  approverCheckDone = false;
  approverName      = '';      // configured approver username — shown in the pending banner

  // Role (from sessionStorage 'userrole', set at login from OBMSUsers.IsAdmin)
  // '0'→'user', '1'→'admin', '2'→'superadmin'
  userRole = '';

  // Dropdowns
  branches:          any[] = [];
  warehouses:        any[] = [];
  items:             any[] = [];
  clients:           any[] = [];          // clients for the selected requesting branch
  filteredBranches:  any[] = [];
  filteredWarehouses: any[] = [];
  filteredItems:     any[] = [];

  // Per-row client search state (keyed by row index)
  clientSearchMap:    { [i: number]: string }   = {};
  filteredClientsMap: { [i: number]: any[] }    = {};

  // Searchable-dropdown state
  branchSearch    = '';
  warehouseSearch = '';
  itemSearch      = '';

  userAccessModel!: UserAccessModel;
  currentUser        = '';
  warningMessage     = '';
  errorMessage       = '';
  showLoadingSpinner = false;

  constructor(
    private fb:              FormBuilder,
    private activatedRoute:  ActivatedRoute,
    private route:           Router,
    public  dialog:          MatDialog,
    private service:         InventoryService,
    private _dataService:    DatasharingService,
    private _masterService:  MastermoduleService
  ) {
    this.userAccessModel = {
      readAccess: false, updateAccess: false,
      deleteAccess: false, createAccess: false
    };
    this.frm = this.fb.group({
      ID:                   [0],
      RequestNo:            [''],
      RequestDate:          [new Date(), Validators.required],
      RequestingBranchID:   [0, [Validators.required, mustSelect()]],
      SourceWarehouseID:    [0, [Validators.required, mustSelect()]],
      Remarks:              [''],
      Details:              this.fb.array([])
    });
  }

  get details(): FormArray { return this.frm.get('Details') as FormArray; }

  // â”€â”€ Lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    // Load role from sessionStorage 'userrole' (set at login from OBMSUsers.IsAdmin)
    const rawRole = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = rawRole === '2' ? 'superadmin' : rawRole === '1' ? 'admin' : 'user';
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => {
        this.currentUser = u;
        // Load branches only after currentUser is resolved so the user-branch
        // access filter is applied correctly
        this.getUserAccessRights(this.currentUser, 'Branch Stock Request');
        this.loadBranches();
      });
    } else {
      this.getUserAccessRights(this.currentUser, 'Branch Stock Request');
      this.loadBranches();
    }
    this.loadWarehouses();
    this.loadItems();

    if (this.ID && this.ID !== '0') {
      this.loadExisting();
    } else {
      this.service.getBSRCode().subscribe({
        next: (d: any) => this.frm.get('RequestNo')?.setValue(d.Code),
        error: () => {}
      });
      this.addRow();
    }
  }

  // â”€â”€ Permissions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      data => {
        if (data) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.createAccess = data.Create;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.deleteAccess = data.Delete;
        }
      }, () => {}
    );
  }

  // â”€â”€ Data Loading â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  loadBranches(): void {
    this.service.getBranchListByUser(this.currentUser).subscribe({
      next: (d: any[]) => {
        this.branches          = (d || []).filter((b: any) => b.IsActive !== false);
        this.filteredBranches  = [...this.branches];
      },
      error: () => { this.errorMessage = 'Unable to load branch list. Please refresh.'; }
    });
  }

  loadWarehouses(): void {
    this.service.getWarehouseMasters().subscribe({
      next: (d: any[]) => {
        this.warehouses          = (d || []).filter((w: any) => w.IsActive);
        this.filteredWarehouses  = [...this.warehouses];
      },
      error: () => { this.errorMessage = 'Unable to load warehouse list. Please refresh.'; }
    });
  }

  loadItems(): void {
    this.service.getItemsForStock().subscribe({
      next: (d: any[]) => { this.items = d || []; this.filteredItems = [...this.items]; },
      error: () => { this.errorMessage = 'Unable to load item list. Please refresh.'; }
    });
  }

  /** Load clients for the selected requesting branch (by branch Code). */
  loadClientsByBranch(branchID: number): void {
    const branch = this.branches.find((b: any) => b.ID === branchID);
    const branchCode = branch?.Code ?? '';
    if (!branchCode) { this.clients = []; this.resetAllClientFilters(); return; }
    this._masterService.getClientMsterListByBranch(branchCode).subscribe({
      next: (d: any[]) => {
        this.clients = (d || []).filter((c: any) => c.Status !== 'Inactive');
        this.resetAllClientFilters();
      },
      error: () => { this.clients = []; this.resetAllClientFilters(); }
    });
  }

  private resetAllClientFilters(): void {
    for (let i = 0; i < this.details.length; i++) {
      this.clientSearchMap[i]    = '';
      this.filteredClientsMap[i] = [...this.clients];
    }
  }

  loadExisting(): void {
    this.showLoadingSpinner = true;
    this.service.getBSRByID(+this.ID).subscribe({
      next: (d: any) => {
        if (!d) {
          this.errorMessage = 'Stock request not found.';
          this.hideSpinner();
          return;
        }

        this.currentStatus   = d.Status;
        this.submittedBy     = d.SubmittedBy || '';
        this.approvedBy      = d.ApprovedBy  || '';
        this.approvalRemarks = d.ApprovalRemarks || '';
        this.linkedTransferNo  = d.LinkedTransferNo || '';
        this.linkedTransferID  = d.LinkedTransferID || null;

        this.frm.patchValue({
          ID:                 d.ID,
          RequestNo:          d.RequestNo,
          RequestDate:        new Date(d.RequestDate),
          RequestingBranchID: d.RequestingBranchID,
          SourceWarehouseID:  d.SourceWarehouseID,
          Remarks:            d.Remarks || ''
        });
        this.warningMessage = '';   // clear any stale default-warehouse warning

        // Load clients for this branch, then populate detail rows
        const branch     = this.branches.find((b: any) => b.ID === d.RequestingBranchID);
        const branchCode = branch?.Code ?? '';
        const buildDetails = (clientList: any[]) => {
          this.clients = clientList;
          this.details.clear();
          (d.Details || []).forEach((line: any) => {
            this.details.push(this.fb.group({
              ID:           [line.ID],
              ItemID:       [line.ItemID],
              UOMID:        [line.UOMID],
              RequestedQty: [line.RequestedQty],
              BuyingPrice:  [line.BuyingPrice ?? null],
              ClientID:     [line.ClientID ?? 0],
              Remarks:      [line.Remarks || ''],
              _ItemName:    [line.ItemName],
              _UOMName:     [line.UOMName],
              _ClientName:  [line.ClientName || '']
            }));
          });
          this.resetAllClientFilters();
        };

        if (branchCode) {
          this._masterService.getClientMsterListByBranch(branchCode).subscribe({
            next: (cl: any[]) => buildDetails((cl || []).filter((c: any) => c.Status !== 'Inactive')),
            error: ()         => buildDetails([])
          });
        } else {
          buildDetails([]);
        }

        // Determine view vs edit mode
        if (d.Status === 'Draft') {
          this.isEditMode = true;
        } else {
          this.isViewMode = true;
          this.frm.disable();
        }

        // BUG-BSR-02 FIX: always call checkIsApprover for Submitted status regardless
        // of whether submittedBy is populated — backend provides fallback via
        // sr.SubmittedBy ?? sr.LastUpdatedBy, so the value should always be non-empty.
        // Even if empty, the API returns IsApprover=false gracefully.
        if (d.Status === 'Submitted') {
          this.checkIsApprover();
        }

        this.hideSpinner();
      },
      error: () => {
        this.errorMessage = 'Unable to load stock request. Please try again.';
        this.hideSpinner();
      }
    });
  }

  checkIsApprover(): void {
    // Guard: if already confirmed as approver, don't let a second call override it.
    if (this.isApprover && this.approverCheckDone) return;
    this.service
      .isCurrentUserApprover('BRANCH_STOCK_REQUEST', this.submittedBy || '')
      .subscribe({
        next: (res: any) => {
          if (!this.isApprover) {
            this.isApprover   = res?.IsApprover === true;
            this.approverName = res?.ApproverUser || '';
          }
          this.approverCheckDone = true;
        },
        error: () => {
          if (!this.approverCheckDone) {
            this.isApprover   = false;
            this.approverName = '';
          }
          this.approverCheckDone = true;
        }
      });
  }

  /** Returns true when the current user can approve/reject this BSR.
   *  SuperAdmin (IsAdmin='2') has global authority including their own submissions.
   *  Configured approvers (via ApprovalRouting) can act on their assigned requests. */
  canApproveOrReject(): boolean {
    // Must be an existing Submitted document to show approve/reject
    if (!this.isViewMode && !this.isEditMode) return false;
    if (this.currentStatus !== 'Submitted') return false;
    if (this.userRole === 'superadmin') return true;
    return this.isApprover && this.approverCheckDone;
  }

  // â”€â”€ FormArray helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  addRow(): void {
    const i = this.details.length;
    this.details.push(this.fb.group({
      ID:           [0],
      ItemID:       [0, [Validators.required, mustSelect()]],
      UOMID:        [null],
      RequestedQty: ['', [Validators.required, greaterThanZero()]],
      BuyingPrice:  [null],   // auto-filled from ItemMaster; user-editable
      ClientID:     [0],
      Remarks:      [''],
      _ItemName:    [''],
      _UOMName:     [''],
      _ClientName:  ['']
    }));
    this.clientSearchMap[i]    = '';
    this.filteredClientsMap[i] = [...this.clients];
  }

  removeRow(i: number): void {
    this.details.removeAt(i);
    // Re-index client search/filter maps
    const newSearch:  { [k: number]: string }   = {};
    const newFiltered:{ [k: number]: any[] }    = {};
    for (let k = 0; k < this.details.length; k++) {
      const src = k >= i ? k + 1 : k;
      newSearch[k]   = this.clientSearchMap[src]    ?? '';
      newFiltered[k] = this.filteredClientsMap[src] ?? [...this.clients];
    }
    this.clientSearchMap    = newSearch;
    this.filteredClientsMap = newFiltered;
  }

  // â”€â”€ Dropdown search helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  onBranchOpen(opened: boolean): void    { if (opened) { this.branchSearch    = ''; this.filteredBranches    = [...this.branches];    } }
  onWarehouseOpen(opened: boolean): void { if (opened) { this.warehouseSearch = ''; this.filteredWarehouses = [...this.warehouses]; } }
  onItemOpen(opened: boolean): void      { if (opened) { this.itemSearch      = ''; this.filteredItems      = [...this.items];      } }
  onClientOpen(i: number, opened: boolean): void { if (opened) { this.clientSearchMap[i] = ''; this.filteredClientsMap[i] = [...this.clients]; } }

  onBranchSearch(q: string): void    { this.branchSearch    = q; const s = q.toLowerCase(); this.filteredBranches    = this.branches.filter((b: any)   => b.Name?.toLowerCase().includes(s)); }
  onWarehouseSearch(q: string): void { this.warehouseSearch = q; const s = q.toLowerCase(); this.filteredWarehouses = this.warehouses.filter((w: any) => w.Name?.toLowerCase().includes(s)); }
  onItemSearch(q: string): void      { this.itemSearch      = q; const s = q.toLowerCase(); this.filteredItems      = this.items.filter((i: any)      => i.Name?.toLowerCase().includes(s) || i.Code?.toLowerCase().includes(s)); }
  onClientSearch(i: number, q: string): void {
    this.clientSearchMap[i] = q;
    const s = q.toLowerCase();
    this.filteredClientsMap[i] = this.clients.filter((c: any) => c.Name?.toLowerCase().includes(s) || c.Code?.toLowerCase().includes(s));
  }

  // â”€â”€ Branch change â†’ reload clients â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  onBranchChange(): void {
    const branchID = +this.frm.get('RequestingBranchID')?.value;
    // Clear client selection on all rows when branch changes
    this.details.controls.forEach(ctrl => {
      ctrl.get('ClientID')?.setValue(0);
      ctrl.get('_ClientName')?.setValue('');
    });
    this.loadClientsByBranch(branchID);
  }

  onWarehouseChange(): void {
    // Source warehouse changed — reset any warehouse-dependent state on detail rows.
    // (Stock availability is checked on submit; no per-row reload needed here.)
    this.details.controls.forEach(ctrl => {
      ctrl.get('AvailableQty')?.setValue(null);
    });
  }

  // â”€â”€ Item change â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  onItemChange(i: number): void {
    const line     = this.details.at(i);
    const selected = this.items.find(x => x.ID === +line.get('ItemID')?.value);
    if (selected) {
      line.get('_ItemName')?.setValue(selected.Name);
      line.get('_UOMName')?.setValue(selected.UOMName || '');
      line.get('UOMID')?.setValue(selected.UOMID || null);
      line.get('BuyingPrice')?.setValue(selected.Price ?? null);

      // ── Default Warehouse auto-fill ─────────────────────────────────────
      // Auto-fill SourceWarehouseID from item's default warehouse only when
      // SourceWarehouseID is not yet selected (new BSR or first item on a
      // Draft where warehouse was not manually set).
      // If SourceWarehouseID is already set, the currentWarehouse===0 guard
      // prevents any overwrite — preserving user's or saved selection.
      const currentWarehouse = this.frm.get('SourceWarehouseID')?.value;
      if ((!currentWarehouse || +currentWarehouse === 0) && selected.WarehouseID) {
        const whExists = this.warehouses.find((w: any) => w.ID === selected.WarehouseID);
        if (whExists) {
          this.frm.get('SourceWarehouseID')?.setValue(selected.WarehouseID);
        } else {
          // Default warehouse is inactive — warn user, leave field empty
          this.warningMessage =
            'The default warehouse for this item is inactive. Please select another warehouse.';
        }
      }
    }
  }

  // â”€â”€ Client change â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  onClientChange(i: number): void {
    const clientID = +this.details.at(i).get('ClientID')?.value;
    const selected = this.clients.find((c: any) => c.ID === clientID);
    this.details.at(i).get('_ClientName')?.setValue(selected?.Name || '');
  }

  // â”€â”€ Save â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  // ── State helpers ──────────────────────────────────────────────────────────

  /** Submit button visible: new form with items added, OR existing Draft opened */
  canSubmit(): boolean {
    // New form (not yet saved): show if at least one item added
    if (!this.isEditMode && !this.isViewMode) {
      return this.details.length > 0;
    }
    // Existing record: only show for Draft status
    return this.currentStatus === 'Draft' && this.details.length > 0;
  }

  get viewBranchName(): string {
    const id = this.frm.get('RequestingBranchID')?.value;
    return this.branches.find((b: any) => b.ID === id)?.Name || '—';
  }
  get viewWarehouseName(): string {
    const id = this.frm.get('SourceWarehouseID')?.value;
    return this.warehouses.find((w: any) => w.ID === id)?.Name || '—';
  }

  /** Save Draft button visible: not in view mode */
  canSave(): boolean {
    return !this.isViewMode;
  }

  onSave(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    if (this.details.length === 0) {
      this.showMsg('Please add at least one item to the request.', 'warning', 'Warning Message');
      return;
    }

    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const payload = {
      ID:                  raw.ID,
      RequestNo:           raw.RequestNo,
      RequestDate:         raw.RequestDate,
      RequestingBranchID:  +raw.RequestingBranchID,
      SourceWarehouseID:   +raw.SourceWarehouseID,
      Remarks:             raw.Remarks,
      Details:             raw.Details.map((d: any) => ({
        ID:           d.ID,
        ItemID:       +d.ItemID,
        UOMID:        d.UOMID ? +d.UOMID : null,
        RequestedQty: +d.RequestedQty,
        BuyingPrice:  d.BuyingPrice != null && d.BuyingPrice !== '' ? +d.BuyingPrice : null,
        ClientID:     d.ClientID && +d.ClientID > 0 ? +d.ClientID : null,
        Remarks:      d.Remarks
      }))
    };

    this.service.saveBSR(payload).subscribe({
      next: (res: any) => {
        this.showMsg(res?.message || 'Stock request saved.', 'success', 'Success Message');
        // Navigate to edit form so Submit button stays visible
        if (res?.id && !this.isEditMode) {
          this.route.navigateByUrl('/dummy', { skipLocationChange: true })
              .then(() => this.route.navigate(['/inventory/branch-stock-request/new-bsr', res.id]));
        } else {
          this.route.navigateByUrl('/dummy', { skipLocationChange: true })
              .then(() => this.route.navigate(['/inventory/branch-stock-request/new-bsr', raw.ID]));
        }
      },
      error: (err: any) => {
        this.showMsg(
          this.extractMsg(err, 'Unable to save the request right now. Please try again.'),
          'error', 'Error Message');
        this.hideSpinner();
      }
    });
  }

  // â”€â”€ Submit â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    if (this.details.length === 0) {
      this.showMsg('Please add at least one item before submitting.', 'warning', 'Warning Message');
      return;
    }

    const label = this.isEditMode
      ? this.frm.get('RequestNo')?.value
      : 'this stock request';

    this.dialog.open(DialogConfirmationComponent, {
      data: { message: `Submit <b>${label}</b> for approval?` }
    }).afterClosed().subscribe((result: any) => {
      if (!result?.confirmDialog) return;
      this.showLoadingSpinner = true;

      if (this.isEditMode) {
        // Existing Draft — directly submit
        const id = this.frm.get('ID')?.value;
        this.service.submitBSR(+id).subscribe({
          next: (res: any) => {
            this.showMsg(res?.message || 'Stock request submitted for approval.', 'success', 'Success Message');
            this.route.navigateByUrl('/dummy', { skipLocationChange: true })
                .then(() => this.route.navigate(['/inventory/branch-stock-request/new-bsr', id]));
          },
          error: (err: any) => {
            this.showMsg(this.extractMsg(err, 'Unable to submit right now. Please try again.'), 'error', 'Error Message');
            this.hideSpinner();
          }
        });
      } else {
        // New unsaved form — save as Draft first, then submit
        const raw = this.frm.getRawValue();
        const payload = {
          ID: 0,
          RequestNo:           raw.RequestNo,
          RequestDate:         raw.RequestDate,
          RequestingBranchID:  +raw.RequestingBranchID,
          SourceWarehouseID:   +raw.SourceWarehouseID,
          Remarks:             raw.Remarks,
          Details:             raw.Details.map((d: any) => ({
            ID: 0,
            ItemID:       +d.ItemID,
            UOMID:        d.UOMID ? +d.UOMID : null,
            RequestedQty: +d.RequestedQty,
            BuyingPrice:  d.BuyingPrice != null && d.BuyingPrice !== '' ? +d.BuyingPrice : null,
            ClientID:     d.ClientID && +d.ClientID > 0 ? +d.ClientID : null,
            Remarks:      d.Remarks
          }))
        };
        this.service.saveBSR(payload).subscribe({
          next: (saveRes: any) => {
            const newID = saveRes?.id;
            if (!newID) {
              this.showMsg('Unable to save the request. Please try again.', 'error', 'Error Message');
              this.hideSpinner();
              return;
            }
            this.service.submitBSR(newID).subscribe({
              next: (submitRes: any) => {
                this.showMsg(submitRes?.message || 'Stock request submitted for approval.', 'success', 'Success Message');
                this.route.navigateByUrl('/dummy', { skipLocationChange: true })
                    .then(() => this.route.navigate(['/inventory/branch-stock-request/new-bsr', newID]));
              },
              error: (err: any) => {
                this.showMsg(this.extractMsg(err, 'Saved as Draft but unable to submit. Please open and submit manually.'), 'error', 'Error Message');
                this.route.navigateByUrl('/dummy', { skipLocationChange: true })
                    .then(() => this.route.navigate(['/inventory/branch-stock-request/new-bsr', newID]));
              }
            });
          },
          error: (err: any) => {
            this.showMsg(this.extractMsg(err, 'Unable to save the request right now. Please try again.'), 'error', 'Error Message');
            this.hideSpinner();
          }
        });
      }
    });
  }

  // â”€â”€ Approve â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  onApprove(): void {
    const id = this.frm.get('ID')?.value;
    this.showLoadingSpinner = true;
    this.service.approveBSR(+id, { ApprovalRemarks: '' }).subscribe({
      next: (res: any) => {
        this.showMsg(res?.message || 'Stock request approved.', 'success', 'Success Message');
        this.route.navigateByUrl('/dummy', { skipLocationChange: true })
            .then(() => this.route.navigate(['/inventory/branch-stock-request']));
      },
      error: (err: any) => {
        this.showMsg(
          this.extractMsg(err, 'Unable to approve right now. Please try again.'),
          'error', 'Error Message');
        this.hideSpinner();
      }
    });
  }

  // â”€â”€ Reject â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  onReject(): void {
    const id = this.frm.get('ID')?.value;
    Swal.fire({
      title: 'Reject Stock Request',
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
      this.service.rejectBSR(+id, { ApprovalRemarks: result.value.trim() }).subscribe({
        next: (res: any) => {
          this.showMsg(res?.message || 'Stock request rejected.', 'warning', 'Warning Message');
          this.route.navigateByUrl('/dummy', { skipLocationChange: true })
              .then(() => this.route.navigate(['/inventory/branch-stock-request']));
        },
        error: (err: any) => {
          this.showMsg(this.extractMsg(err, 'Unable to reject right now. Please try again.'), 'error', 'Error Message');
          this.hideSpinner();
        }
      });
    });
  }

  // â”€â”€ Create Stock Transfer (Approved requests only) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  onCreateTransfer(): void {
    const raw = this.frm.getRawValue();
    this.route.navigate(
      ['/inventory/stock-transfer/new-stock-transfer'],
      {
        queryParams: {
          fromBSR:           raw.ID,
          sourceWarehouseID: raw.SourceWarehouseID
        }
      }
    );
  }

  openLinkedTransfer(): void {
    if (this.linkedTransferID) {
      this.route.navigate([
        '/inventory/stock-transfer/new-stock-transfer', this.linkedTransferID
      ]);
    }
  }

  // â”€â”€ Navigation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  onCancel(): void {
    this.route.navigate(['/inventory/branch-stock-request']);
  }

  onPrintPDF(): void {
    const docID = this.ID || this.frm.get('ID')?.value;
    if (!docID || +docID === 0) return;
    const url = environment.baseReportUrl
      + 'Inventory/BSRReport.aspx'
      + '?LoginID=' + encodeURIComponent(this.currentUser)
      + '&BSRID=' + encodeURIComponent(docID);
    window.open(url, '_blank');
  }

  // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', {
      minimumFractionDigits: 2, maximumFractionDigits: 2
    });
  }

  getStatusClass(s: string): string {
    switch (s) {
      case 'Draft':     return 'badge-draft';
      case 'Submitted': return 'badge-submitted';
      case 'Approved':  return 'badge-approved';
      case 'Rejected':  return 'badge-rejected';
      default:          return '';
    }
  }

  private extractMsg(err: any, fallback: string): string {
    if (typeof err === 'string') return err;
    return err?.error?.message || err?.message || fallback;
  }

  private showMsg(
    message: string,
    icon: 'success' | 'warning' | 'info' | 'error',
    title: 'Success Message' | 'Warning Message' | 'Error Message'
  ): void {
    Swal.fire({
      toast: true, position: 'top', showConfirmButton: false,
      title, text: message, icon,
      showCloseButton: false, timer: 6000, width: '650px',
      customClass: { popup: 'swal-top-offset' }
    });
    this.hideSpinner();
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }
}
