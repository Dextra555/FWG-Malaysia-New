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
  return (c: AbstractControl) => { const v = parseFloat(c.value); return isNaN(v) || v <= 0 ? { greaterThanZero: true } : null; };
}

@Component({
  selector: 'app-new-pr',
  templateUrl: './new-pr.component.html',
  styleUrls: ['./new-pr.component.css']
})
export class NewPRComponent implements OnInit {

  frm!: FormGroup;
  lineForm!: FormGroup;
  isEdit = false;
  ID: any;

  items: any[] = [];
  lines: any[] = [];
  editLineIndex = -1;
  // Search filter state
  itemSearch = ''; filteredItems: any[] = [];

  currentStatus   = 'Draft';
  approvalRemarks = '';
  approvedBy      = '';
  submittedBy     = '';   // who submitted — used to check approver routing

  // ── Approver visibility flag ─────────────────────────────────────────────
  // true  = current user is the configured approver → show Approve / Reject
  // false = current user is the requester / other   → hide Approve / Reject
  isApprover        = false;
  approverCheckDone = false;   // prevents flicker before API responds
  approverName      = '';      // configured approver username — shown in the pending banner

  // ── Role (from sessionStorage 'userrole', set at login from OBMSUsers.IsAdmin) ──
  // '0'→'user', '1'→'admin', '2'→'superadmin'
  // Used so SuperAdmin (any username) sees Approve/Reject without routing config.
  userRole = '';

  // ── Linked PO (set after loadExisting when status = Approved) ─────────────
  linkedPO: any = null;   // holds PO info if a PO has already been created for this PR

  userAccessModel!: UserAccessModel;
  currentUser      = '';
  warningMessage   = '';
  errorMessage     = '';
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
      ID:             [0],
      PRNumber:       [''],
      PRDate:         [new Date(), Validators.required],
      RequiredByDate: [new Date(), Validators.required],
      Remarks:        ['']
    });

    this.lineForm = this.fb.group({
      ItemID:      [0, [Validators.required, mustSelect()]],
      RequiredQty: ['', [Validators.required, greaterThanZero()]],
      BuyingPrice: [''],   // auto-filled from ItemMaster; user-editable
      UOMID:       [0],
      Remarks:     [''],
      _index:      [-1]
    });
  }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username') || '';
    // Load role from sessionStorage 'userrole' (set at login from OBMSUsers.IsAdmin)
    // '0'→'user', '1'→'admin', '2'→'superadmin'
    const rawRole = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = rawRole === '2' ? 'superadmin' : rawRole === '1' ? 'admin' : 'user';
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => {
        this.currentUser = u;
        this.getUserAccessRights(u, 'Purchase Requisition');
      });
    } else {
      this.getUserAccessRights(this.currentUser, 'Purchase Requisition');
    }
    this.loadItems();

    if (this.ID && this.ID !== '0') {
      this.isEdit = true;
      this.loadExisting();
    } else {
      this.service.getPRCode().subscribe({
        next: (d: any) => this.frm.get('PRNumber')?.setValue(d.Code),
        error: () => {}
      });
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

  loadItems(): void {
    this.service.getItemsForStock().subscribe({
      next: (d: any[]) => { this.items = d || []; this.filteredItems = [...this.items]; },
      error: () => { this.errorMessage = 'Unable to load the item list. Please try again.'; }
    });
  }

  loadExisting(): void {
    this.showLoadingSpinner = true;
    this.service.getPRByID(this.ID).subscribe({
      next: (d: any) => {
        if (!d) {
          this.errorMessage = 'Purchase requisition not found.';
          this.hideSpinner();
          return;
        }
        this.currentStatus   = d.Status;
        this.approvedBy      = d.ApprovedBy || '';
        this.approvalRemarks = d.ApprovalRemarks || '';
        // Part 7 fix: use SubmittedBy (now returned by backend) so the approver
        // lookup uses the person who actually submitted, not whoever last edited.
        // Fall back to LastUpdatedBy only for legacy PRs that pre-date Part 7.
        this.submittedBy     = d.SubmittedBy || d.LastUpdatedBy || '';

        this.frm.patchValue({
          ID:             d.ID,
          PRNumber:       d.PRNumber,
          PRDate:         new Date(d.PRDate),
          RequiredByDate: new Date(d.RequiredByDate),
          Remarks:        d.Remarks
        });
        this.lines = (d.Details || []).map((l: any) => ({ ...l, _index: -1 }));
        if (d.Status !== 'Draft') { this.frm.disable(); }
        this.hideSpinner();

        // Once we know the status and submitter, check whether current user is the approver
        if (d.Status === 'Submitted') {
          // Part 7 fix: use this.submittedBy (already set above) — not d.LastUpdatedBy directly
          this.checkIsApprover(this.submittedBy);
        } else {
          this.approverCheckDone = true;
          this.isApprover = false;
        }

        // If Approved, check whether a PO has already been created for this PR
        if (d.Status === 'Approved') {
          this.service.getPOByPRID(+this.ID).subscribe({
            next: (po: any) => { this.linkedPO = (po && po.ID) ? po : null; },
            error: () => { this.linkedPO = null; }
          });
        }
      },
      error: () => {
        this.errorMessage = 'Unable to load the purchase requisition. Please try again.';
        this.hideSpinner();
      }
    });
  }

  // ── Ask backend: "Is the currently logged-in user the configured approver?" ──
  private checkIsApprover(requesterUser: string): void {
    // Guard: if already confirmed as approver, don't let a second call override it.
    // Double-call can happen when loadExisting() fires twice (route + access rights).
    if (this.isApprover && this.approverCheckDone) return;

    console.log('[checkIsApprover] requesterUser=', requesterUser, 'currentUser=', this.currentUser);
    this.service.isCurrentUserApprover('PURCHASE_REQUISITION', requesterUser || '').subscribe({
      next: (res: any) => {
        console.log('[checkIsApprover] response=', res);
        // Only update if not already confirmed — prevents false override from
        // a racing second call that arrives after the first true result.
        if (!this.isApprover) {
          this.isApprover   = res?.IsApprover === true;
          this.approverName = res?.ApproverUser || '';
        }
        this.approverCheckDone = true;
      },
      error: (err: any) => {
        console.error('[checkIsApprover] error=', err);
        if (!this.approverCheckDone) {
          this.isApprover        = false;
          this.approverName      = '';
        }
        this.approverCheckDone = true;
      }
    });
  }

  // ── State helpers ────────────────────────────────────────────────────────
  canEdit():   boolean { return !this.isEdit || this.currentStatus === 'Draft'; }
  // Submit is available on a new (unsaved) form with at least one line,
  // OR on an existing Draft — Draft is no longer a mandatory pre-step.
  canSubmit(): boolean { return this.lines.length > 0 && (!this.isEdit || this.currentStatus === 'Draft'); }

  // Approve/Reject: configured approver OR SuperAdmin
  canApproveAction(): boolean {
    if (!this.isEdit || this.currentStatus !== 'Submitted') return false;

    // SuperAdmin (IsAdmin='2') has global approval authority — including their own submissions.
    if (this.userRole === 'superadmin') return true;

    // Normal users: configured approver only, self-approval blocked
    return this.isApprover && this.approverCheckDone;
  }

  canCreatePO(): boolean { return this.isEdit && this.currentStatus === 'Approved'; }

  // ── Line management ──────────────────────────────────────────────────────
  onItemChange(itemID: number): void {
    const item = this.items.find(i => i.ID === +itemID);
    if (item) {
      if (item.UOMID) this.lineForm.get('UOMID')?.setValue(item.UOMID);
      this.lineForm.get('BuyingPrice')?.setValue(item.Price ?? '');
    }
  }

  addLine(): void {
    if (this.lineForm.invalid) { this.lineForm.markAllAsTouched(); return; }
    const raw  = this.lineForm.getRawValue();
    const item = this.items.find(i => i.ID === +raw.ItemID);
    const line = {
      ...raw,
      ItemName:     item?.Name         || '',
      ItemCode:     item?.Code         || '',
      UOMName:      item?.UOMName      || '',
      CategoryName: item?.CategoryName || ''
    };
    if (raw._index >= 0) { this.lines[raw._index] = line; }
    else { this.lines.push(line); }
    this.lineForm.reset({ ItemID: 0, RequiredQty: '', BuyingPrice: '', UOMID: 0, Remarks: '', _index: -1 });
    this.editLineIndex = -1;
  }

  editLine(i: number): void {
    this.editLineIndex = i;
    const l = this.lines[i];
    this.lineForm.patchValue({
      ItemID: l.ItemID, RequiredQty: l.RequiredQty,
      BuyingPrice: l.BuyingPrice ?? '',
      UOMID: l.UOMID || 0, Remarks: l.Remarks, _index: i
    });
  }

  removeLine(i: number): void { this.lines.splice(i, 1); this.editLineIndex = -1; }

  // ── Actions ──────────────────────────────────────────────────────────────
  onSave(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    if (this.lines.length === 0) {
      this.showMsg('Please add at least one item to the requisition before saving.', 'warning', 'Warning Message');
      return;
    }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const payload = {
      ID: raw.ID, PRNumber: raw.PRNumber,
      PRDate: raw.PRDate, RequiredByDate: raw.RequiredByDate,
      Remarks: raw.Remarks, LastUpdatedBy: this.currentUser,
      Details: this.lines.map(l => ({
        ID: l.ID || 0, ItemID: l.ItemID,
        RequiredQty: l.RequiredQty,
        BuyingPrice: l.BuyingPrice != null && l.BuyingPrice !== '' ? +l.BuyingPrice : null,
        UOMID: l.UOMID || null,
        Remarks: l.Remarks || '',
        LastUpdatedBy: this.currentUser
      }))
    };
    this.service.savePR(payload).subscribe({
      next: (res: any) => {
        this.showMsg(res?.message || 'Purchase requisition saved as draft.', 'success', 'Success Message');
        // Navigate to the edit page so both [Save Draft] and [Submit] remain visible
        if (res?.id && !this.isEdit) {
          this.route.navigateByUrl('/dummy', { skipLocationChange: true })
            .then(() => this.route.navigate(['/inventory/purchase-requisition/new-pr', res.id]));
        } else {
          this.loadExisting();
        }
      },
      error: (err: any) => {
        this.showMsg(this.extractMsg(err, 'Unable to save right now. Please try again.'), 'error', 'Error Message');
        this.hideSpinner();
      }
    });
  }

  onSubmitForApproval(): void {
    this.dialog.open(DialogConfirmationComponent, {
      data: { message: `Submit Purchase Requisition <b>${this.frm.get('PRNumber')?.value}</b> for approval?` }
    }).afterClosed().subscribe((result: any) => {
      if (result?.confirmDialog) {
        this.showLoadingSpinner = true;
        // The controller reads identity from JWT — currentUser param is redundant but kept for compat
        this.service.submitPR(this.ID, this.currentUser).subscribe({
          next: (res: any) => {
            this.showMsg(res?.message || 'Submitted for approval.', 'success', 'Success Message');
            this.loadExisting();
          },
          error: (err: any) => {
            this.showMsg(this.extractMsg(err, 'Unable to submit right now.'), 'error', 'Error Message');
            this.hideSpinner();
          }
        });
      }
    });
  }

  // ── Direct Submit (New form → save as Draft → immediately submit) ────────
  // Allows the user to click [Submit] on a brand-new form without first saving a Draft.
  // If the PR is already a saved Draft (isEdit=true), this skips the save step.
  onSubmitDirect(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    if (this.lines.length === 0) {
      this.showMsg('Please add at least one item to the requisition before submitting.', 'warning', 'Warning Message');
      return;
    }

    const prLabel = this.isEdit ? this.frm.get('PRNumber')?.value : 'this requisition';
    this.dialog.open(DialogConfirmationComponent, {
      data: { message: `Submit Purchase Requisition <b>${prLabel}</b> for approval?` }
    }).afterClosed().subscribe((result: any) => {
      if (!result?.confirmDialog) return;

      this.showLoadingSpinner = true;

      if (this.isEdit) {
        // Already a saved Draft — just submit
        this.service.submitPR(this.ID, this.currentUser).subscribe({
          next: (res: any) => {
            this.showMsg(res?.message || 'Submitted for approval.', 'success', 'Success Message');
            this.loadExisting();
          },
          error: (err: any) => {
            this.showMsg(this.extractMsg(err, 'Unable to submit right now.'), 'error', 'Error Message');
            this.hideSpinner();
          }
        });
      } else {
        // New form — save as Draft first, then immediately submit
        const raw = this.frm.getRawValue();
        const payload = {
          ID: 0, PRNumber: raw.PRNumber,
          PRDate: raw.PRDate, RequiredByDate: raw.RequiredByDate,
          Remarks: raw.Remarks, LastUpdatedBy: this.currentUser,
          Details: this.lines.map(l => ({
            ID: 0, ItemID: l.ItemID,
            RequiredQty: l.RequiredQty,
            BuyingPrice: l.BuyingPrice != null && l.BuyingPrice !== '' ? +l.BuyingPrice : null,
            UOMID: l.UOMID || null,
            Remarks: l.Remarks || '',
            LastUpdatedBy: this.currentUser
          }))
        };
        this.service.savePR(payload).subscribe({
          next: (saveRes: any) => {
            const newID = saveRes?.id;
            if (!newID) {
              this.showMsg('Unable to save the requisition. Please try again.', 'error', 'Error Message');
              this.hideSpinner();
              return;
            }
            // Draft saved — now submit
            this.service.submitPR(newID, this.currentUser).subscribe({
              next: (submitRes: any) => {
                this.showMsg(submitRes?.message || 'Submitted for approval.', 'success', 'Success Message');
                // Navigate to the saved/submitted record so user can see the status
                this.route.navigateByUrl('/dummy', { skipLocationChange: true })
                  .then(() => this.route.navigate(['/inventory/purchase-requisition/new-pr', newID]));
              },
              error: (err: any) => {
                // Save succeeded but submit failed — navigate to draft so user can retry submit
                this.showMsg(this.extractMsg(err, 'Saved as Draft, but submit failed. Please open the draft and try submitting again.'), 'error', 'Error Message');
                this.route.navigateByUrl('/dummy', { skipLocationChange: true })
                  .then(() => this.route.navigate(['/inventory/purchase-requisition/new-pr', newID]));
              }
            });
          },
          error: (err: any) => {
            this.showMsg(this.extractMsg(err, 'Unable to save right now. Please try again.'), 'error', 'Error Message');
            this.hideSpinner();
          }
        });
      }
    });
  }

  onApprove(): void {
    this.dialog.open(DialogConfirmationComponent, {
      data: { message: `Are you sure you want to approve Purchase Requisition <b>${this.frm.get('PRNumber')?.value}</b>?` }
    }).afterClosed().subscribe((result: any) => {
      if (result?.confirmDialog) {
        this.showLoadingSpinner = true;
        // Backend reads approver identity from JWT — ApprovedBy body is overridden server-side
        this.service.approvePR(this.ID, { ApprovedBy: this.currentUser, ApprovalRemarks: '' }).subscribe({
          next: (res: any) => {
            this.showMsg(res?.message || 'Approved successfully.', 'success', 'Success Message');
            this.loadExisting();
          },
          error: (err: any) => {
            this.showMsg(this.extractMsg(err, 'Unable to approve right now.'), 'error', 'Error Message');
            this.hideSpinner();
          }
        });
      }
    });
  }

  onReject(): void {
    Swal.fire({
      title: 'Reject Purchase Requisition',
      text: 'Please enter the reason for rejection:',
      input: 'textarea',
      inputPlaceholder: 'Enter rejection reason here…',
      inputAttributes: { rows: '3' },
      showCancelButton: true,
      confirmButtonText: 'Reject',
      confirmButtonColor: '#d33',
      cancelButtonText: 'Cancel',
      inputValidator: (value) => {
        if (!value || value.trim() === '') {
          return 'Please enter a reason for rejection so the requester knows what to correct.';
        }
        return null;
      }
    }).then(result => {
      if (!result.isConfirmed || !result.value?.trim()) return;
      const reason = result.value.trim();
      this.showLoadingSpinner = true;
      // Backend reads rejector identity from JWT
      this.service.rejectPR(this.ID, { ApprovedBy: this.currentUser, ApprovalRemarks: reason }).subscribe({
        next: (res: any) => {
          this.showMsg(res?.message || 'Rejected.', 'warning', 'Warning Message');
          this.loadExisting();
        },
        error: (err: any) => {
          this.showMsg(this.extractMsg(err, 'Unable to reject right now.'), 'error', 'Error Message');
          this.hideSpinner();
        }
      });
    });
  }

  onCreatePO(): void {
    this.route.navigate(['/inventory/purchase-order/new-po'], { queryParams: { prID: this.ID } });
  }

  openLinkedPO(): void {
    if (this.linkedPO?.ID) {
      this.route.navigate(['/inventory/purchase-order/new-po', this.linkedPO.ID]);
    }
  }

  onCancel(): void { this.route.navigate(['/inventory/purchase-requisition']); }

  // ── Helpers ──────────────────────────────────────────────────────────────
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
      title, text: message, icon, showCloseButton: false,
      timer: 7000, width: '700px',
      customClass: { popup: 'swal-top-offset' }
    });
    this.hideSpinner();
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onItemOpen(opened: boolean) { if (opened) { this.itemSearch = ''; this.filteredItems = [...this.items]; } }
  onItemSearch(q: string) { this.itemSearch = q; const s = q.toLowerCase(); this.filteredItems = this.items.filter(i => i.Name?.toLowerCase().includes(s) || i.Code?.toLowerCase().includes(s)); }
}
