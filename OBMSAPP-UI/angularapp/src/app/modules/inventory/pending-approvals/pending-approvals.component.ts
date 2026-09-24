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
    this.getUserAccessRights(this.currentUser, 'Pending Approvals');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      data => {
        if (data) {
          this.showLoadingSpinner = true;
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
          this.userAccessModel.deleteAccess = data.Delete;
          if (this.userAccessModel.readAccess || this.currentUser === 'superadmin') {
            this.loadList();
          } else {
            this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>You do not have permission to view pending approvals.<br>Please contact your administrator.`;
            this.hideSpinner();
          }
        }
      },
      () => { this.errorMessage = 'Unable to load page permissions. Please try again.'; this.hideSpinner(); }
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
      // new-pr route uses route param :ID
      this.route.navigate(['/inventory/purchase-requisition/new-pr', row.DocumentID]);
    } else if (row.DocumentType === 'PURCHASE_ORDER') {
      // new-po route uses route param :ID
      this.route.navigate(['/inventory/purchase-order/new-po', row.DocumentID]);
    }
  }

  approveClick(row: any): void {
    if (!this.userAccessModel.updateAccess && this.currentUser !== 'superadmin') {
      this.showMsg('You do not have permission to approve documents.', 'warning', 'Warning Message');
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
      }
    });
  }

  rejectClick(row: any): void {
    if (!this.userAccessModel.updateAccess && this.currentUser !== 'superadmin') {
      this.showMsg('You do not have permission to reject documents.', 'warning', 'Warning Message');
      return;
    }
    const reason = prompt(`Please enter the reason for rejecting ${row.DocumentTypeDisplay} ${row.DocumentNumber}:`);
    if (!reason || reason.trim() === '') {
      this.showMsg('Please enter a reason for rejection.', 'warning', 'Warning Message');
      return;
    }
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
    }
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

  getDocTypeClass(t: string): string {
    switch (t) {
      case 'PURCHASE_REQUISITION': return 'badge-pr';
      case 'PURCHASE_ORDER':       return 'badge-po';
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
