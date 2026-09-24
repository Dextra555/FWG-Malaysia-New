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

function mustSelect(): ValidatorFn {
  return (c: AbstractControl) => (!c.value || +c.value === 0) ? { mustSelect: true } : null;
}
function greaterThanZero(): ValidatorFn {
  return (c: AbstractControl) => {
    const v = parseFloat(c.value);
    return isNaN(v) || v <= 0 ? { greaterThanZero: true } : null;
  };
}
function nonNegative(): ValidatorFn {
  return (c: AbstractControl) => {
    const v = parseFloat(c.value);
    return isNaN(v) || v < 0 ? { nonNegative: true } : null;
  };
}

@Component({
  selector: 'app-new-po',
  templateUrl: './new-po.component.html',
  styleUrls: ['./new-po.component.css']
})
export class NewPOComponent implements OnInit {

  frm!: FormGroup;
  lineForm!: FormGroup;
  isEdit = false;
  ID: any;
  prID: any;   // from query param when navigating from approved PR

  suppliers:   any[] = [];
  warehouses:  any[] = [];
  items:       any[] = [];
  approvedPRs: any[] = [];
  lines:       any[] = [];
  editLineIndex = -1;
  // Search filter state
  supplierSearch = '';  filteredSuppliers:  any[] = [];
  warehouseSearch = ''; filteredWarehouses: any[] = [];
  itemSearch = '';      filteredItems:      any[] = [];

  currentStatus = 'Draft';
  approvedBy    = '';
  submittedBy   = '';
  rejectReason  = '';
  totalAmount   = 0;

  // ── Approver visibility flag ─────────────────────────────────────────────
  isApprover        = false;
  approverCheckDone = false;

  // ── Linked GRN (set after loadExisting when PO is Approved or Received) ──
  linkedGRN: any = null;   // holds GRN info if a GRN has already been posted for this PO

  userAccessModel!: UserAccessModel;
  currentUser       = '';
  warningMessage    = '';
  errorMessage      = '';
  showLoadingSpinner = false;

  constructor(
    private fb: FormBuilder,
    private activatedRoute: ActivatedRoute,
    private route: Router,
    public dialog: MatDialog,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = {
      readAccess: false, updateAccess: false,
      deleteAccess: false, createAccess: false
    };

    this.frm = this.fb.group({
      ID:          [0],
      PONumber:    [''],
      PRID:        [0],
      SupplierID:  [0, [Validators.required, mustSelect()]],
      WarehouseID: [0, [Validators.required, mustSelect()]],
      PODate:       [new Date(), Validators.required],
      ExpectedDate: [new Date(), Validators.required],
      Remarks:     ['']
    });

    this.lineForm = this.fb.group({
      ItemID:    [0,  [Validators.required, mustSelect()]],
      UOMID:     [0],
      OrderedQty:['',[Validators.required, greaterThanZero()]],
      UnitPrice: ['',[Validators.required, nonNegative()]],
      LineTotal:  [{ value: '0.00', disabled: true }],
      Remarks:   [''],
      _index:    [-1]
    });

    this.lineForm.get('OrderedQty')?.valueChanges.subscribe(() => this.calcLineTotal());
    this.lineForm.get('UnitPrice')?.valueChanges.subscribe(() => this.calcLineTotal());
  }

  ngOnInit(): void {
    this.ID   = this.activatedRoute.snapshot.params['ID'];
    this.prID = this.activatedRoute.snapshot.queryParams['prID'];

    this.currentUser = sessionStorage.getItem('username') || '';
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => {
        this.currentUser = u;
        this.getUserAccessRights(u, 'Purchase Order');
      });
    } else {
      this.getUserAccessRights(this.currentUser, 'Purchase Order');
    }

    this.loadDropdowns();

    if (this.ID && this.ID !== '0') {
      this.isEdit = true;
      this.loadExisting();
    } else {
      this.service.getPOCode().subscribe({
        next: (d: any) => this.frm.get('PONumber')?.setValue(d.Code),
        error: () => {}
      });
      if (this.prID) {
        // ── Guard: if a PO already exists for this PR, open it instead ──────
        // This prevents duplicate POs when the same approved-PR notification
        // is clicked more than once.
        this.showLoadingSpinner = true;
        this.service.getPOByPRID(+this.prID).subscribe({
          next: (existingPO: any) => {
            this.hideSpinner();
            if (existingPO && existingPO.ID) {
              // A PO already exists — redirect to it (read/edit the existing one)
              this.showMsg(
                `A Purchase Order (${existingPO.PONumber}) already exists for this Purchase Requisition. Opening the existing order.`,
                'info', 'Info'
              );
              this.route.navigate(['/inventory/purchase-order/new-po', existingPO.ID]);
            } else {
              // No PO yet — safe to prefill from PR and create new
              this.frm.get('PRID')?.setValue(+this.prID);
              this.prefillFromPR(+this.prID);
            }
          },
          error: () => {
            // On error, allow normal flow (backend SavePO guard is the safety net)
            this.hideSpinner();
            this.frm.get('PRID')?.setValue(+this.prID);
            this.prefillFromPR(+this.prID);
          }
        });
      }
    }
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      data => {
        if (data) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
        }
      },
      () => { this.errorMessage = 'Unable to load page permissions. Please try again.'; }
    );
  }

  loadDropdowns(): void {
    this.service.getSuppliers().subscribe({
      next: (d: any[]) => { this.suppliers = d || []; this.filteredSuppliers = [...this.suppliers]; },
      error: () => { this.errorMessage = 'Unable to load the supplier list. Please try again.'; }
    });
    this.service.getWarehouseMasters().subscribe({
      next: (d: any[]) => { this.warehouses = (d || []).filter((w: any) => w.IsActive); this.filteredWarehouses = [...this.warehouses]; },
      error: () => { this.errorMessage = 'Unable to load the warehouse list. Please try again.'; }
    });
    this.service.getItemsForStock().subscribe({
      next: (d: any[]) => { this.items = d || []; this.filteredItems = [...this.items]; },
      error: () => { this.errorMessage = 'Unable to load the item list. Please try again.'; }
    });
    this.service.getApprovedPRsForPO().subscribe({
      next: (d: any[]) => this.approvedPRs = d || [],
      error: () => {}
    });
  }

  prefillFromPR(prID: number): void {
    this.service.getPRByID(prID).subscribe({
      next: (d: any) => {
        if (d?.Details) {
          this.lines = d.Details.map((l: any) => {
            const item = this.items.find(i => i.ID === l.ItemID);
            return {
              ItemID:       l.ItemID,
              ItemCode:     l.ItemCode  || item?.Code         || '',
              ItemName:     l.ItemName  || item?.Name         || '',
              CategoryName: l.CategoryName || item?.CategoryName || '',
              UOMID:        l.UOMID     || 0,
              UOMName:      l.UOMName   || item?.UOMName      || '',
              OrderedQty:   l.RequiredQty,
              UnitPrice:    0,
              LineTotal:    0,
              Remarks:      l.Remarks   || ''
            };
          });
          this.calcTotal();
        }
      },
      error: () => {}
    });
  }

  loadExisting(): void {
    this.showLoadingSpinner = true;
    this.service.getPOByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) {
          this.errorMessage = 'Purchase order not found.';
          this.hideSpinner();
          return;
        }
        this.currentStatus = d.Status;
        this.approvedBy    = d.ApprovedBy  || '';
        this.submittedBy   = d.SubmittedBy || d.LastUpdatedBy || '';
        this.rejectReason  = d.RejectReason || '';

        this.frm.patchValue({
          ID: d.ID, PONumber: d.PONumber, PRID: d.PRID || 0,
          SupplierID: d.SupplierID, WarehouseID: d.WarehouseID,
          PODate: new Date(d.PODate), ExpectedDate: new Date(d.ExpectedDate),
          Remarks: d.Remarks
        });
        this.lines = (d.Details || []).map((l: any) => ({
          ...l, LineTotal: l.TotalPrice, _index: -1
        }));
        this.calcTotal();
        if (d.Status !== 'Draft') { this.frm.disable(); }
        this.hideSpinner();

        // Check approver visibility
        if (d.Status === 'Submitted') {
          this.checkIsApprover(this.submittedBy);
        } else {
          this.approverCheckDone = true;
          this.isApprover = false;
        }

        // If Approved / Partially Received / Fully Received — check for a posted GRN
        const grnnableStatuses = ['Approved', 'Partially Received', 'Fully Received'];
        if (grnnableStatuses.includes(d.Status)) {
          this.service.getGRNByPOID(+this.ID).subscribe({
            next: (grn: any) => { this.linkedGRN = (grn && grn.ID) ? grn : null; },
            error: () => { this.linkedGRN = null; }
          });
        }
      },
      error: () => {
        this.errorMessage = 'Unable to load the purchase order. Please try again.';
        this.hideSpinner();
      }
    });
  }

  private checkIsApprover(requesterUser: string): void {
    if (!requesterUser) {
      this.approverCheckDone = true;
      return;
    }
    this.service.isCurrentUserApprover('PURCHASE_ORDER', requesterUser).subscribe({
      next: (res: any) => {
        this.isApprover        = res?.IsApprover === true;
        this.approverCheckDone = true;
      },
      error: () => {
        this.isApprover        = false;
        this.approverCheckDone = true;
      }
    });
  }

  // ── Line helpers ─────────────────────────────────────────────────────────
  onItemChange(itemID: number): void {
    const item = this.items.find(i => i.ID === +itemID);
    if (item) {
      this.lineForm.get('UOMID')?.setValue(item.UOMID || 0);
      this.lineForm.get('UnitPrice')?.setValue(item.Price || 0);
    }
    this.calcLineTotal();
  }

  calcLineTotal(): void {
    const qty   = parseFloat(this.lineForm.get('OrderedQty')?.value) || 0;
    const price = parseFloat(this.lineForm.get('UnitPrice')?.value)  || 0;
    this.lineForm.get('LineTotal')?.setValue((qty * price).toFixed(2), { emitEvent: false });
  }

  addLine(): void {
    if (this.lineForm.invalid) { this.lineForm.markAllAsTouched(); return; }
    const raw  = this.lineForm.getRawValue();
    const item = this.items.find(i => i.ID === +raw.ItemID);
    const line = {
      ...raw,
      ItemCode:     item?.Code         || '',
      ItemName:     item?.Name         || '',
      CategoryName: item?.CategoryName || '',
      UOMName:      item?.UOMName      || '',
      LineTotal:    parseFloat(raw.OrderedQty) * parseFloat(raw.UnitPrice)
    };
    if (raw._index >= 0) { this.lines[raw._index] = line; }
    else { this.lines.push(line); }
    this.lineForm.reset({
      ItemID: 0, UOMID: 0, OrderedQty: '',
      UnitPrice: '', LineTotal: '0.00', Remarks: '', _index: -1
    });
    this.editLineIndex = -1;
    this.calcTotal();
  }

  editLine(i: number): void {
    this.editLineIndex = i;
    const l = this.lines[i];
    this.lineForm.patchValue({
      ItemID: l.ItemID, UOMID: l.UOMID || 0,
      OrderedQty: l.OrderedQty, UnitPrice: l.UnitPrice,
      Remarks: l.Remarks, _index: i
    });
    this.calcLineTotal();
  }

  removeLine(i: number): void {
    this.lines.splice(i, 1);
    this.editLineIndex = -1;
    this.calcTotal();
  }

  calcTotal(): void {
    this.totalAmount = this.lines.reduce(
      (s, l) => s + (parseFloat(l.OrderedQty) * parseFloat(l.UnitPrice) || 0), 0
    );
  }

  // ── State helpers ─────────────────────────────────────────────────────────
  canEdit():   boolean { return this.currentStatus === 'Draft'; }
  canSubmit(): boolean {
    return this.isEdit &&
           this.currentStatus === 'Draft' &&
           this.lines.length > 0;
  }
  canApproveAction(): boolean {
    return this.isEdit &&
           this.currentStatus === 'Submitted' &&
           this.isApprover &&
           this.approverCheckDone;
  }
  canGRN(): boolean {
    return this.isEdit &&
      (this.currentStatus === 'Approved' ||
       this.currentStatus === 'Partially Received');
  }

  // ── Actions ───────────────────────────────────────────────────────────────
  onSave(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    if (this.lines.length === 0) {
      this.showMsg('Please add at least one item to the purchase order.', 'warning', 'Warning Message');
      return;
    }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const payload = {
      ID: raw.ID, PONumber: raw.PONumber,
      PRID: raw.PRID || null,
      SupplierID:  +raw.SupplierID,
      WarehouseID: +raw.WarehouseID,
      PODate:       raw.PODate,
      ExpectedDate: raw.ExpectedDate,
      Remarks:      raw.Remarks,
      LastUpdatedBy: this.currentUser,
      Details: this.lines.map(l => ({
        ID:         l.ID || 0,
        ItemID:     l.ItemID,
        UOMID:      l.UOMID || null,
        OrderedQty: +l.OrderedQty,
        UnitPrice:  +l.UnitPrice,
        Remarks:    l.Remarks || '',
        LastUpdatedBy: this.currentUser
      }))
    };
    this.service.savePO(payload).subscribe({
      next: (res: any) => {
        this.showMsg(
          res?.message || 'Purchase order saved successfully.',
          'success', 'Success Message'
        );
        // Navigate to the saved PO's edit page so the user can immediately
        // click "Send for Approval". For updates, reload in-place.
        if (res?.id) {
          this.route.navigateByUrl('/dummy', { skipLocationChange: true })
            .then(() => this.route.navigate(['/inventory/purchase-order/new-po', res.id]));
        } else {
          this.route.navigateByUrl('/dummy', { skipLocationChange: true })
            .then(() => this.route.navigate(['/inventory/purchase-order']));
        }
      },
      error: (err: any) => {
        this.showMsg(
          this.extractMsg(err, 'Unable to save the purchase order right now. Please try again.'),
          'error', 'Error Message'
        );
        this.hideSpinner();
      }
    });
  }

  onSubmitForApproval(): void {
    this.dialog.open(DialogConfirmationComponent, {
      data: `Send Purchase Order <b>${this.frm.get('PONumber')?.value}</b> for approval?`
    }).afterClosed().subscribe((r: any) => {
      if (!r?.confirmDialog) return;
      this.showLoadingSpinner = true;
      // Uses /api/Approval/SubmitPO — reads identity from JWT
      this.service.submitPO(this.ID, this.currentUser).subscribe({
        next: (res: any) => {
          this.showMsg(res?.message || 'Purchase order submitted for approval.', 'success', 'Success Message');
          this.loadExisting();
        },
        error: (err: any) => {
          this.showMsg(this.extractMsg(err, 'Unable to submit. Please try again.'), 'error', 'Error Message');
          this.hideSpinner();
        }
      });
    });
  }

  onApprove(): void {
    this.dialog.open(DialogConfirmationComponent, {
      data: `Are you sure you want to approve Purchase Order <b>${this.frm.get('PONumber')?.value}</b>?`
    }).afterClosed().subscribe((r: any) => {
      if (!r?.confirmDialog) return;
      this.showLoadingSpinner = true;
      // Uses /api/Approval/ApprovePOWithHistory — reads identity from JWT
      this.service.approvePOWithHistory(this.ID, { ApprovedBy: this.currentUser, Comments: '' }).subscribe({
        next: (res: any) => {
          this.showMsg(res?.message || 'Purchase order approved.', 'success', 'Success Message');
          this.loadExisting();
        },
        error: (err: any) => {
          this.showMsg(this.extractMsg(err, 'Unable to approve right now.'), 'error', 'Error Message');
          this.hideSpinner();
        }
      });
    });
  }

  onReject(): void {
    const reason = prompt('Please enter the reason for rejecting this purchase order:');
    if (!reason || reason.trim() === '') {
      this.showMsg('Please enter a rejection reason.', 'warning', 'Warning Message');
      return;
    }
    this.showLoadingSpinner = true;
    // Uses /api/Approval/RejectPO — reads identity from JWT
    this.service.rejectPO(this.ID, { RejectedBy: this.currentUser, RejectReason: reason }).subscribe({
      next: (res: any) => {
        this.showMsg(res?.message || 'Purchase order rejected.', 'warning', 'Warning Message');
        this.loadExisting();
      },
      error: (err: any) => {
        this.showMsg(this.extractMsg(err, 'Unable to reject right now.'), 'error', 'Error Message');
        this.hideSpinner();
      }
    });
  }

  onReceiveGoods(): void {
    this.route.navigate(['/inventory/goods-receipt/new-grn'], { queryParams: { poID: this.ID } });
  }

  openLinkedGRN(): void {
    if (this.linkedGRN?.ID) {
      this.route.navigate(['/inventory/goods-receipt/new-grn', this.linkedGRN.ID]);
    }
  }

  onCancel(): void { this.route.navigate(['/inventory/purchase-order']); }

  formatAmount(v: number): string {
    return (v || 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  private extractMsg(err: any, fallback: string): string {
    if (typeof err === 'string') return err;
    return err?.error?.message || err?.message || fallback;
  }

  private showMsg(
    message: string,
    icon: 'success' | 'warning' | 'info' | 'error',
    title: 'Success Message' | 'Warning Message' | 'Error Message' | 'Info'
  ): void {
    Swal.fire({
      toast: true, position: 'top', showConfirmButton: false,
      title, text: message, icon, showCloseButton: false,
      timer: 7000, width: '700px',
      customClass: { popup: 'swal-top-offset' }
    });
    this.hideSpinner();
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onSupplierOpen(opened: boolean)   { if (opened) { this.supplierSearch  = ''; this.filteredSuppliers  = [...this.suppliers];  } }
  onWarehouseOpen(opened: boolean)  { if (opened) { this.warehouseSearch = ''; this.filteredWarehouses = [...this.warehouses]; } }
  onItemOpen(opened: boolean)       { if (opened) { this.itemSearch      = ''; this.filteredItems      = [...this.items];      } }
  onSupplierSearch(q: string)  { this.supplierSearch  = q; const s = q.toLowerCase(); this.filteredSuppliers  = this.suppliers.filter(x  => x.Name?.toLowerCase().includes(s) || x.Code?.toLowerCase().includes(s)); }
  onWarehouseSearch(q: string) { this.warehouseSearch = q; const s = q.toLowerCase(); this.filteredWarehouses = this.warehouses.filter(x => x.Name?.toLowerCase().includes(s)); }
  onItemSearch(q: string)      { this.itemSearch      = q; const s = q.toLowerCase(); this.filteredItems      = this.items.filter(x      => x.Name?.toLowerCase().includes(s) || x.Code?.toLowerCase().includes(s)); }
}
