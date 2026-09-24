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
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({
  selector: 'app-purchase-requisition',
  templateUrl: './purchase-requisition.component.html',
  styleUrls: ['./purchase-requisition.component.css']
})
export class PurchaseRequisitionComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; }
  }

  userAccessModel!: UserAccessModel;
  currentUser = '';
  warningMessage = '';
  errorMessage = '';
  showLoadingSpinner = false;
  filterValue = '';

  displayedColumns = ['PRNumber', 'PRDate', 'RequiredByDate', 'Status', 'Remarks', 'action'];
  dataSource = new MatTableDataSource<any>([]);
  pageSizeOptions: number[] = [];

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    public dialog: MatDialog,
    private service: InventoryService,
    private route: Router,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    for (let i = 10; i <= 50; i += 10) this.pageSizeOptions.push(i);
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => this.currentUser = u);
    }
    this.getUserAccessRights(this.currentUser, 'Purchase Requisition');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      data => {
        if (data) {
          this.showLoadingSpinner = true;
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
          if (this.userAccessModel.readAccess || this.currentUser === 'superadmin') {
            this.loadList();
          } else {
            this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>You do not have permission to view this page.<br>Please contact your administrator.`;
            this.hideSpinner();
          }
        }
      },
      () => { this.errorMessage = 'Unable to load page permissions. Please try again.'; this.hideSpinner(); }
    );
  }

  loadList(): void {
    this.service.getPRList().subscribe({
      next: (d: any[]) => {
        this.dataSource = new MatTableDataSource<any>(d || []);
        this.dataSource.sort = this.sort;
        this.dataSource.paginator = this.paginator;
        // Allow searching by DisplayStatus (e.g. "PO Posted", "GRN Posted")
        this.dataSource.filterPredicate = (row: any, filter: string) => {
          const search = filter.toLowerCase();
          return (row.PRNumber        || '').toLowerCase().includes(search)
              || (row.DisplayStatus   || row.Status || '').toLowerCase().includes(search)
              || (row.Remarks         || '').toLowerCase().includes(search)
              || (row.LastUpdatedBy   || '').toLowerCase().includes(search);
        };
        this.pageSizeOptions = [];
        const total = (d || []).length;
        for (let i = 10; i <= total && i <= 1000; i += 10) this.pageSizeOptions.push(i);
        if (total > 0 && total < 10) this.pageSizeOptions.push(total);
        this.hideSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load purchase requisitions right now. Please try again.'; this.hideSpinner(); }
    });
  }

  applyFilter(e: Event): void {
    this.filterValue = (e.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  announceSortChange(s: Sort): void {
    this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared');
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Draft':      return 'badge-draft';
      case 'Submitted':  return 'badge-submitted';
      case 'Approved':   return 'badge-approved';
      case 'Rejected':   return 'badge-rejected';
      case 'PO Posted':  return 'badge-po-posted';
      case 'GRN Posted': return 'badge-grn-posted';
      default:           return '';
    }
  }

  formatDate(d: any): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  deleteClick(row: any): void {
    if (row.Status !== 'Draft') {
      this.showMsg(`This purchase requisition cannot be deleted because it has already been ${row.Status.toLowerCase()}. Only Draft requisitions can be deleted.`, 'warning', 'Warning Message');
      return;
    }
    this.dialog.open(DialogConfirmationComponent, { data: `Are you sure you want to delete Purchase Requisition <b>${row.PRNumber}</b>?` })
      .afterClosed().subscribe((result: any) => {
        if (result?.confirmDialog) {
          this.showLoadingSpinner = true;
          this.service.deletePR(row.ID, this.currentUser).subscribe({
            next: (res: any) => { this.showMsg(res?.message || 'Purchase requisition deleted.', 'success', 'Success Message'); this.loadList(); },
            error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to delete right now. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
          });
        }
      });
  }

  private extractMsg(err: any, fallback: string): string {
    if (typeof err === 'string') return err;
    return err?.error?.message || err?.message || fallback;
  }

  private showMsg(message: string, icon: 'success'|'warning'|'info'|'error', title: 'Success Message'|'Warning Message'|'Error Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon, showCloseButton: false, timer: 6000, width: '650px', customClass: { popup: 'swal-top-offset' } });
    this.hideSpinner();
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }
}
