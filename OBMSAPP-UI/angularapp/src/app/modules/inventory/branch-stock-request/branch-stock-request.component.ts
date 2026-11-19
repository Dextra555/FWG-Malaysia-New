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
  selector: 'app-branch-stock-request',
  templateUrl: './branch-stock-request.component.html',
  styleUrls: ['./branch-stock-request.component.css']
})
export class BranchStockRequestComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) {
      this.dataSource.paginator = this.paginator;
      this.dataSource.sort      = this.sort;
    }
  }

  userAccessModel!: UserAccessModel;
  currentUser        = '';
  userRole           = '';   // '0'→'user', '1'→'admin', '2'→'superadmin'
  warningMessage     = '';
  errorMessage       = '';
  showLoadingSpinner = false;
  filterValue        = '';
  pageSizeOptions    = [10, 20, 30, 50];

  displayedColumns = [
    'RequestNo', 'RequestDate', 'RequestingBranchName',
    'SourceWarehouseName', 'RequestedBy', 'Status', 'action'
  ];
  dataSource = new MatTableDataSource<any>([]);

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    public  dialog: MatDialog,
    private service: InventoryService,
    private route: Router,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = {
      readAccess: false, updateAccess: false,
      deleteAccess: false, createAccess: false
    };
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    const raw = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = raw === '2' ? 'superadmin' : raw === '1' ? 'admin' : 'user';
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Branch Stock Request');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      data => {
        if (data != null) {
          this.showLoadingSpinner            = true;
          this.userAccessModel.readAccess    = data.Read;
          this.userAccessModel.deleteAccess  = data.Delete;
          this.userAccessModel.updateAccess  = data.Update;
          this.userAccessModel.createAccess  = data.Create;
          if (this.userAccessModel.readAccess || this.userRole === 'superadmin') {
            this.loadList();
          } else {
            this.warningMessage =
              `Dear <b>${this.currentUser}</b>,<br>` +
              `You do not have permission to view Branch Stock Requests.<br>` +
              `Please contact your administrator.`;
            this.hideSpinner();
          }
        }
      },
      () => {
        this.errorMessage = 'Unable to load this page right now. Please try again.';
        this.hideSpinner();
      }
    );
  }

  loadList(): void {
    this.service.getBSRList().subscribe({
      next: (d: any[]) => {
        this.dataSource = new MatTableDataSource(d || []);
        this.dataSource.sort      = this.sort;
        this.dataSource.paginator = this.paginator;
        this.hideSpinner();
      },
      error: () => {
        this.errorMessage = 'Unable to load the stock request list right now. Please try again.';
        this.hideSpinner();
      }
    });
  }

  applyFilter(event: Event): void {
    this.filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort): void {
    if (sortState.direction) this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    else                     this._liveAnnouncer.announce('Sorting cleared');
  }

  submitClick(row: any): void {
    this.dialog.open(DialogConfirmationComponent, {
      data: `Are you sure you want to submit stock request <b>${row.RequestNo}</b> for approval?`
    }).afterClosed().subscribe((result: { confirmDialog: boolean }) => {
      if (!result?.confirmDialog) return;
      this.showLoadingSpinner = true;
      this.service.submitBSR(row.ID).subscribe({
        next: (res: any) => {
          this.showMsg(res?.message || 'Stock request submitted for approval.', 'success', 'Success Message');
          this.loadList();
        },
        error: (err: any) => {
          this.showMsg(
            this.extractMsg(err, 'Unable to submit the request right now. Please try again.'),
            'error', 'Error Message');
          this.hideSpinner();
        }
      });
    });
  }

  deleteClick(row: any): void {
    if (row.Status !== 'Draft') {
      this.showMsg(
        `This stock request cannot be deleted because it has already been ${row.Status.toLowerCase()}. ` +
        'Only Draft requests can be deleted.',
        'warning', 'Warning Message');
      return;
    }
    this.dialog.open(DialogConfirmationComponent, {
      data: `Are you sure you want to delete stock request <b>${row.RequestNo}</b>?`
    }).afterClosed().subscribe((result: { confirmDialog: boolean }) => {
      if (!result?.confirmDialog) return;
      this.showLoadingSpinner = true;
      this.service.deleteBSR(row.ID).subscribe({
        next: (res: any) => {
          this.showMsg(res?.message || 'Stock request deleted.', 'success', 'Success Message');
          this.loadList();
        },
        error: (err: any) => {
          this.showMsg(
            this.extractMsg(err, 'Unable to delete the request right now. Please try again.'),
            'error', 'Error Message');
          this.hideSpinner();
        }
      });
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

  formatDate(val: any): string {
    if (!val) return '';
    return new Date(val).toLocaleDateString('en-MY', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
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
      showCloseButton: false, timer: 5000, width: '600px',
      customClass: { popup: 'swal-top-offset' }
    });
    this.hideSpinner();
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }
}
