import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { DialogConfirmationComponent } from '../../../components/dialog-confirmation/dialog-confirmation.component';
import { ApprovalHistoryDialogComponent } from '../approval-history-dialog/approval-history-dialog.component';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({
  selector: 'app-pending-approvals',
  templateUrl: './pending-approvals.component.html',
  styleUrls: ['./pending-approvals.component.css']
})
export class PendingApprovalsComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) {
      this.dataSource.paginator = this.paginator;
      this.dataSource.sort = this.sort;
    }
  }

  userAccessModel!: UserAccessModel;
  currentUser    = '';
  userRole       = '';   // BUG-07/08 FIX: derived from sessionStorage 'userrole' ('0'→'user','1'→'admin','2'→'superadmin')
  warningMessage = '';
  errorMessage   = '';
  showLoadingSpinner = false;
  filterValue    = '';

  displayedColumns = [
    'DocumentTypeDisplay', 'DocumentNumber', 'DocumentDate',
    'RequestedBy', 'SubmittedDate', 'PendingSinceDays',
    'WarehouseName', 'TotalAmount', 'CurrentStatus', 'action'
  ];
  dataSource = new MatTableDataSource<any>([]);
  pageSizeOptions: number[] = [10, 20, 30, 50];

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    public dialog: MatDialog,
    private service: InventoryService,
    private route: Router,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => this.currentUser = u);
    }
    // BUG-07 FIX: load role from sessionStorage 'userrole' (set at login from OBMSUsers.IsAdmin)
    // '0' = user, '1' = admin, '2' = superadmin
    const raw = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = raw === '2' ? 'superadmin' : raw === '1' ? 'admin' : 'user';
    // Always load the list immediately — the backend (GetPendingApprovals) is the
    // authoritative gate: it only returns documents the JWT user is authorised to
    // see (routing-filtered for normal users, all docs for SuperAdmin).
    // getUserAccessRights is still called so canActOnRow() has the updateAccess
    // flag for the Approve/Reject button guard, but we don't block the list load
    // on its result to avoid approvers being locked out by missing permission rows.
    this.showLoadingSpinner = true;
    this.loadList();
    this.getUserAccessRights(this.currentUser, 'Pending Approvals');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      data => {
        if (data) {
          this.userAccessModel.readAccess   = data.Read   ?? data.read   ?? false;
          this.userAccessModel.updateAccess = data.Update ?? data.update ?? false;
          this.userAccessModel.createAccess = data.Create ?? data.create ?? false;
          this.userAccessModel.deleteAccess = data.Delete ?? data.delete ?? false;
        }
        // List is already loading (started in ngOnInit).
        // warningMessage is NOT shown — backend returns empty list for unauthorized users.
      },
      () => { /* permission check failed — list still loads, backend is the gate */ }
    );
  }

  loadList(): void {
    this.service.getPendingApprovals().subscribe({
      next: (d: any[]) => {
        this.dataSource = new MatTableDataSource<any>(d || []);
        this.dataSource.sort = this.sort;
        this.dataSource.paginator = this.paginator;
        this.hideSpinner();
      },
      error: (err: any) => {
        this.errorMessage = this.extractMsg(err, 'Unable to load pending approvals right now. Please try again.');
        this.hideSpinner();
      }
    });
  }

  applyFilter(e: Event): void {
    this.filterValue = (e.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  announceSortChange(s: Sort): void {
    this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared');
  }

  viewDocument(row: any): void {
    if (row.DocumentType === 'PURCHASE_REQUISITION') {
      this.route.navigate(['/inventory/purchase-requisition/new-pr', row.DocumentID]);
    } else if (row.DocumentType === 'PURCHASE_ORDER') {
      this.route.navigate(['/inventory/purchase-order/new-po', row.DocumentID]);
    } else if (row.DocumentType === 'BRANCH_STOCK_REQUEST') {
      this.route.navigate(['/inventory/branch-stock-request/new-bsr', row.DocumentID]);
    } else if (row.DocumentType === 'STOCK_ADJUSTMENT') {
      this.route.navigate(['/inventory/stock-adjustment/new-stock-adjustment', row.DocumentID]);
    } else if (row.DocumentType === 'ASSET_DISPOSAL') {
      this.route.navigate(['/inventory/asset-disposal', row.DocumentID]);
    }
  }

  // ── Per-row authorization helper ─────────────────────────────────────────
  // Returns false (non-actionable) when:
  //   • The current user is NOT SuperAdmin (IsAdmin='2')
  //   AND
  //   • row.RequestedBy matches the current user (self-submission block)
  // SuperAdmin can act on any row, including documents they submitted themselves.
  // Also returns false when basic screen permission (updateAccess) is missing and
  // the user is not SuperAdmin.
  canActOnRow(row: any): boolean {
    // SuperAdmin override — always actionable
    if (this.userRole === 'superadmin') return true;

    // Screen-level permission gate
    if (!this.userAccessModel.updateAccess) return false;

    // Self-submission block: requester must not act on their own document
    const requestedBy = (row?.RequestedBy ?? '').toLowerCase().trim();
    const me          = (this.currentUser ?? '').toLowerCase().trim();
    if (requestedBy && me && requestedBy === me) return false;

    return true;
  }

  approveClick(row: any): void {
    // Guard: self-submission block + screen permission (mirrors button [disabled] logic)
    if (!this.canActOnRow(row)) {
      const isSelf = (row?.RequestedBy ?? '').toLowerCase().trim() ===
                     (this.currentUser ?? '').toLowerCase().trim();
      if (isSelf) {
        this.showMsg(
          'You cannot approve a document that you submitted yourself.',
          'warning', 'Warning Message');
      } else {
        this.showMsg('You do not have permission to approve documents.', 'warning', 'Warning Message');
      }
      return;
    }
    this.dialog.open(DialogConfirmationComponent, {
      data: `Are you sure you want to approve <b>${row.DocumentTypeDisplay} ${row.DocumentNumber}</b>?`
    }).afterClosed().subscribe((result: any) => {
      if (!result?.confirmDialog) return;
      this.showLoadingSpinner = true;

      if (row.DocumentType === 'PURCHASE_REQUISITION') {
        this.service.approvePR(row.DocumentID, { ApprovedBy: this.currentUser, ApprovalRemarks: '' }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Approved.', 'success', 'Success Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to approve. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      } else if (row.DocumentType === 'PURCHASE_ORDER') {
        this.service.approvePOWithHistory(row.DocumentID, { ApprovedBy: this.currentUser, Comments: '' }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Approved.', 'success', 'Success Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to approve. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      } else if (row.DocumentType === 'BRANCH_STOCK_REQUEST') {
        this.service.approveBSR(row.DocumentID, { ApprovalRemarks: '' }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Approved.', 'success', 'Success Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to approve. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      } else if (row.DocumentType === 'STOCK_ADJUSTMENT') {
        this.service.approveSA(row.DocumentID, { ApprovalRemarks: '' }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Approved.', 'success', 'Success Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to approve. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      } else if (row.DocumentType === 'ASSET_DISPOSAL') {
        this.service.approveAD(row.DocumentID, { ApprovalRemarks: '' }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Approved.', 'success', 'Success Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to approve. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      }
    });
  }

  rejectClick(row: any): void {
    // Guard: self-submission block + screen permission (mirrors button [disabled] logic)
    if (!this.canActOnRow(row)) {
      const isSelf = (row?.RequestedBy ?? '').toLowerCase().trim() ===
                     (this.currentUser ?? '').toLowerCase().trim();
      if (isSelf) {
        this.showMsg(
          'You cannot reject a document that you submitted yourself.',
          'warning', 'Warning Message');
      } else {
        this.showMsg('You do not have permission to reject documents.', 'warning', 'Warning Message');
      }
      return;
    }
    Swal.fire({
      title: `Reject ${row.DocumentTypeDisplay}`,
      html: `<b>${row.DocumentNumber}</b> — Please enter the reason for rejection:`,
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
      const reason = result.value.trim();
      this.showLoadingSpinner = true;

      if (row.DocumentType === 'PURCHASE_REQUISITION') {
        this.service.rejectPR(row.DocumentID, { ApprovedBy: this.currentUser, ApprovalRemarks: reason }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Rejected.', 'warning', 'Warning Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to reject. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      } else if (row.DocumentType === 'PURCHASE_ORDER') {
        this.service.rejectPO(row.DocumentID, { RejectedBy: this.currentUser, RejectReason: reason }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Rejected.', 'warning', 'Warning Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to reject. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      } else if (row.DocumentType === 'BRANCH_STOCK_REQUEST') {
        this.service.rejectBSR(row.DocumentID, { ApprovalRemarks: reason }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Rejected.', 'warning', 'Warning Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to reject. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      } else if (row.DocumentType === 'STOCK_ADJUSTMENT') {
        this.service.rejectSA(row.DocumentID, { ApprovalRemarks: reason }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Rejected.', 'warning', 'Warning Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to reject. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      } else if (row.DocumentType === 'ASSET_DISPOSAL') {
        this.service.rejectAD(row.DocumentID, { ApprovalRemarks: reason }).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Rejected.', 'warning', 'Warning Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to reject. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
        });
      }
    });
  }

  viewHistory(row: any): void {
    this.dialog.open(ApprovalHistoryDialogComponent, {
      width: '800px',
      data: { documentType: row.DocumentType, documentID: row.DocumentID, documentNumber: row.DocumentNumber }
    });
  }

  getStatusClass(s: string): string {
    switch (s) {
      case 'Submitted': return 'badge-submitted';
      case 'Approved':  return 'badge-approved';
      case 'Rejected':  return 'badge-rejected';
      case 'Draft':     return 'badge-draft';
      default:          return '';
    }
  }

  getStatusLabel(s: string): string {
    switch (s) {
      case 'Submitted': return 'Submitted — Pending';
      case 'Approved':  return 'Approved';
      case 'Rejected':  return 'Rejected';
      case 'Draft':     return 'Draft';
      default:          return s || '—';
    }
  }

  getDocTypeClass(t: string): string {
    switch (t) {
      case 'PURCHASE_REQUISITION': return 'badge-pr';
      case 'PURCHASE_ORDER':       return 'badge-po';
      case 'BRANCH_STOCK_REQUEST': return 'badge-bsr';
      case 'STOCK_ADJUSTMENT':     return 'badge-sa';
      case 'ASSET_DISPOSAL':       return 'badge-disposal';
      default:                     return 'badge-draft';
    }
  }

  formatDate(d: any): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatAmount(v: number | null): string {
    if (v == null) return '—';
    return v.toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  getPendingLabel(days: number): string {
    if (days === 0) return 'Today';
    if (days === 1) return '1 day';
    return `${days} days`;
  }

  getPendingClass(days: number): string {
    if (days >= 7) return 'text-danger fw-bold';
    if (days >= 3) return 'text-warning fw-bold';
    return 'text-success';
  }

  private extractMsg(err: any, fallback: string): string {
    if (typeof err === 'string') return err;
    return err?.error?.message || err?.message || fallback;
  }

  private showMsg(message: string, icon: 'success' | 'warning' | 'info' | 'error',
    title: 'Success Message' | 'Warning Message' | 'Error Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon, showCloseButton: false, timer: 7000, width: '700px', customClass: { popup: 'swal-top-offset' } });
    this.hideSpinner();
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }
}
