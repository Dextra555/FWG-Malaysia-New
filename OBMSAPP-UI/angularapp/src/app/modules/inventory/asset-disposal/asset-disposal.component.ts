import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { DialogConfirmationComponent } from '../../../components/dialog-confirmation/dialog-confirmation.component';
import { UserAccessModel } from '../../../model/userAccesModel';

function mustSelect(): ValidatorFn {
  return (c: AbstractControl) => (+c.value === 0 || !c.value) ? { mustSelect: true } : null;
}

@Component({
  selector: 'app-asset-disposal',
  templateUrl: './asset-disposal.component.html',
  styleUrls: ['./asset-disposal.component.css']
})
export class AssetDisposalComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void {
    if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; }
  }

  // ── List mode ─────────────────────────────────────────────────────────────
  userAccessModel!: UserAccessModel;
  currentUser = ''; userRole = '';
  warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;
  filterValue = ''; pageSizeOptions = [10, 20, 30, 50];
  displayedColumns = ['DisposalNo', 'AssetCode', 'AssetName', 'DisposalDate', 'DisposalType', 'DisposalReason', 'DisposalValue', 'Status', 'action'];
  dataSource = new MatTableDataSource<any>([]);

  // ── Form mode (new or view existing) ──────────────────────────────────────
  showForm    = false;   // inline new form
  showDetail  = false;   // detail panel for existing record (view/approve)
  frm!: FormGroup;
  assetList: any[] = [];
  disposalTypes = ['Sold', 'Scrapped', 'Donated', 'Written_Off'];
  disposalNo = '';
  assetSearch = ''; filteredAssets: any[] = [];

  // ── Detail / approval state ───────────────────────────────────────────────
  selectedRecord: any = null;   // currently viewed disposal record
  currentStatus   = '';
  submittedBy     = '';
  approvedBy      = '';
  approvalRemarks = '';

  isApprover        = false;
  approverCheckDone = false;
  approverName      = '';

  constructor(
    private fb: FormBuilder,
    private _liveAnnouncer: LiveAnnouncer,
    public dialog: MatDialog,
    private activatedRoute: ActivatedRoute,
    private route: Router,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForm();
  }

  buildForm(): void {
    this.frm = this.fb.group({
      ID:             [0],
      DisposalNo:     [''],
      AssetID:        [0, [Validators.required, mustSelect()]],
      DisposalDate:   [new Date(), Validators.required],
      DisposalType:   ['', Validators.required],
      DisposalReason: ['', Validators.required],
      DisposalValue:  [0],
      Remarks:        ['']
    });
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username') || '';
    const rawRole = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = rawRole === '2' ? 'superadmin' : rawRole === '1' ? 'admin' : 'user';
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Asset Disposal');
    this.service.getAssetList().subscribe({
      next: d => {
        this.assetList       = (d || []).filter((a: any) => a.AssetStatus !== 'Disposed');
        this.filteredAssets  = [...this.assetList];
      },
      error: () => {}
    });

    // Check if opened with an ID param (from Pending Approvals viewDocument)
    const idParam = this.activatedRoute.snapshot.params['ID'];
    if (idParam && idParam !== '0') {
      this.openDetailByID(+idParam);
    }
  }

  getUserAccessRights(user: string, screen: string): void {
    this._masterService.getUserAccessRights(user, screen).subscribe(d => {
      if (d) {
        this.userAccessModel.readAccess   = d.Read;
        this.userAccessModel.createAccess = d.Create;
        this.userAccessModel.updateAccess = d.Update;
        this.userAccessModel.deleteAccess = d.Delete;
      }
      if (this.userAccessModel.readAccess || this.userRole === 'superadmin') {
        this.showLoadingSpinner = true;
        this.loadList();
      } else {
        this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>No permission to view this page.`;
        this.hideLoadingSpinner();
      }
    }, () => {
      this.showLoadingSpinner = true;
      this.loadList();
    });
  }

  loadList(): void {
    this.service.getAssetDisposalList().subscribe({
      next: d => {
        this.dataSource = new MatTableDataSource(d || []);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load disposal list.'; this.hideLoadingSpinner(); }
    });
  }

  // ── New disposal form ─────────────────────────────────────────────────────
  openNewForm(): void {
    this.buildForm();
    this.service.getDisposalCode().subscribe({ next: d => { this.disposalNo = d.Code; this.frm.get('DisposalNo')?.setValue(d.Code); }, error: () => {} });
    this.showDetail  = false;
    this.showForm    = true;
  }

  cancelForm(): void { this.showForm = false; this.showDetail = false; this.selectedRecord = null; }

  onSave(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const payload = { ...raw, AssetID: +raw.AssetID, LastUpdatedBy: this.currentUser };
    this.service.saveAssetDisposal(payload).subscribe({
      next: (res: any) => {
        this.showMsg(res?.message || 'Disposal saved as Draft.', 'success', 'Success Message');
        const savedID = res?.id;
        this.showForm = false;
        this.loadList();
        if (savedID) { this.openDetailByID(savedID); }
      },
      error: err => { this.showMsg(this.extractMsg(err, 'Unable to save.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    const raw   = this.frm.getRawValue();
    const isNew = !raw.ID || raw.ID === 0;
    const label = isNew ? 'this disposal' : raw.DisposalNo;

    this.dialog.open(DialogConfirmationComponent, {
      data: { message: `Submit Asset Disposal <b>${label}</b> for approval? The asset will be disposed only after approval.` }
    }).afterClosed().subscribe((result: any) => {
      if (!result?.confirmDialog) return;
      this.showLoadingSpinner = true;

      if (!isNew) {
        // Already saved Draft — just submit
        this.service.submitAD(+raw.ID).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Submitted.', 'success', 'Success Message'); this.showForm = false; this.loadList(); },
          error: err => { this.showMsg(this.extractMsg(err, 'Unable to submit.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
        });
      } else {
        // New form — save then submit
        const payload = { ...raw, AssetID: +raw.AssetID, LastUpdatedBy: this.currentUser };
        this.service.saveAssetDisposal(payload).subscribe({
          next: (saveRes: any) => {
            const newID = saveRes?.id;
            if (!newID) { this.showMsg('Unable to save. Please try again.', 'error', 'Error Message'); this.hideLoadingSpinner(); return; }
            this.service.submitAD(+newID).subscribe({
              next: (submitRes: any) => {
                this.showMsg(submitRes?.message || 'Submitted for approval.', 'success', 'Success Message');
                this.showForm = false;
                this.loadList();
                this.openDetailByID(newID);
              },
              error: err => { this.showMsg(this.extractMsg(err, 'Saved as Draft but submit failed.'), 'error', 'Error Message'); this.loadList(); this.hideLoadingSpinner(); }
            });
          },
          error: err => { this.showMsg(this.extractMsg(err, 'Unable to save.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
        });
      }
    });
  }

  // ── Detail / approve panel ────────────────────────────────────────────────
  openDetailByID(id: number): void {
    this.showLoadingSpinner = true;
    this.service.getAssetDisposalByID(id).subscribe({
      next: (d: any) => {
        if (!d) { this.hideLoadingSpinner(); return; }
        this.selectedRecord  = d;
        this.currentStatus   = d.Status || 'Draft';
        this.submittedBy     = d.SubmittedBy || d.LastUpdatedBy || '';
        this.approvedBy      = d.ApprovedBy || '';
        this.approvalRemarks = d.ApprovalRemarks || '';
        this.showDetail      = true;
        this.showForm        = false;
        this.hideLoadingSpinner();

        this.isApprover = false; this.approverCheckDone = false; this.approverName = '';
        if (this.currentStatus === 'Submitted') {
          this.checkIsApprover();
        } else {
          this.approverCheckDone = true;
        }
      },
      error: () => { this.errorMessage = 'Unable to load disposal record.'; this.hideLoadingSpinner(); }
    });
  }

  viewRecord(row: any): void { this.openDetailByID(row.ID); }

  // ── Approver check (PR pattern) ───────────────────────────────────────────
  private checkIsApprover(): void {
    if (this.isApprover && this.approverCheckDone) return;
    this.service.isCurrentUserApprover('ASSET_DISPOSAL', this.submittedBy || '').subscribe({
      next: (res: any) => {
        if (!this.isApprover) { this.isApprover = res?.IsApprover === true; this.approverName = res?.ApproverUser || ''; }
        this.approverCheckDone = true;
      },
      error: () => { if (!this.approverCheckDone) { this.isApprover = false; this.approverName = ''; } this.approverCheckDone = true; }
    });
  }

  canApproveAction(): boolean {
    if (!this.selectedRecord || this.currentStatus !== 'Submitted') return false;
    if (this.userRole === 'superadmin') return true;
    return this.isApprover && this.approverCheckDone;
  }

  onApprove(): void {
    if (!this.selectedRecord) return;
    this.showLoadingSpinner = true;
    this.service.approveAD(+this.selectedRecord.ID, { ApprovalRemarks: '' }).subscribe({
      next: (res: any) => { this.showMsg(res?.message || 'Approved.', 'success', 'Success Message'); this.loadList(); this.openDetailByID(this.selectedRecord.ID); },
      error: err => { this.showMsg(this.extractMsg(err, 'Unable to approve.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  onReject(): void {
    if (!this.selectedRecord) return;
    Swal.fire({
      title: 'Reject Asset Disposal',
      text: 'Please enter the reason for rejection:',
      input: 'textarea',
      inputPlaceholder: 'Enter rejection reason here…',
      inputAttributes: { rows: '3' },
      showCancelButton: true,
      confirmButtonText: 'Reject',
      confirmButtonColor: '#d33',
      cancelButtonText: 'Cancel',
      inputValidator: (value) => { if (!value || value.trim() === '') return 'Please enter a reason for rejection.'; return null; }
    }).then(result => {
      if (!result.isConfirmed || !result.value?.trim()) return;
      this.showLoadingSpinner = true;
      this.service.rejectAD(+this.selectedRecord.ID, { ApprovalRemarks: result.value.trim() }).subscribe({
        next: (res: any) => { this.showMsg(res?.message || 'Rejected.', 'warning', 'Warning Message'); this.loadList(); this.openDetailByID(this.selectedRecord.ID); },
        error: err => { this.showMsg(this.extractMsg(err, 'Unable to reject.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
      });
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  getStatusLabel(row: any): string {
    const s = row.Status || 'Approved';
    if (s === 'Submitted') return 'Waiting Approval';
    return s;
  }

  getStatusClass(row: any): string {
    switch (row.Status || 'Approved') {
      case 'Draft':     return 'badge-draft';
      case 'Submitted': return 'badge-submitted';
      case 'Approved':  return 'badge-approved';
      case 'Rejected':  return 'badge-rejected';
      default:          return 'badge-approved';
    }
  }

  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }
  formatDate(v: any): string { if (!v) return '—'; return new Date(v).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }
  formatNumber(v: number): string { return (v ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
  private extractMsg(e: any, fb: string): string { return (e && typeof e === 'string') ? e : e?.error?.message || e?.message || fb; }
  private showMsg(msg: string, icon: 'success'|'error'|'warning'|'info', title: 'Success Message'|'Error Message'|'Warning Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: msg, icon, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }
  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }
  onAssetOpen(opened: boolean) { if (opened) { this.assetSearch = ''; this.filteredAssets = [...this.assetList]; } }
  onAssetSearch(q: string) { this.assetSearch = q; const s = q.toLowerCase(); this.filteredAssets = this.assetList.filter(a => a.Name?.toLowerCase().includes(s) || a.AssetCode?.toLowerCase().includes(s)); }
}
