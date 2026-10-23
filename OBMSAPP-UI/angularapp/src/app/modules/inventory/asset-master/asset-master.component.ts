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
  selector: 'app-asset-master',
  templateUrl: './asset-master.component.html',
  styleUrls: ['./asset-master.component.css']
})
export class AssetMasterComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; }
  }

  userAccessModel!: UserAccessModel;
  currentUser:    string  = '';
  userRole:       string  = '';
  warningMessage: string  = '';
  errorMessage:   string  = '';
  showLoadingSpinner: boolean = false;
  filterValue:    string  = '';
  pageSizeOptions: number[] = [10, 20, 30, 50];

  displayedColumns: string[] = [
    'AssetCode', 'Name', 'AssetType', 'SerialNumber', 'Branch',
    'PurchaseAmount', 'AssetStatus', 'CurrentAllocatedTo', 'action'
  ];
  dataSource = new MatTableDataSource<any>([]);

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
    const rawRole = sessionStorage.getItem('userrole');
    if (rawRole == '1') { this.userRole = 'admin'; }
    else if (rawRole == '2') { this.userRole = 'superadmin'; }
    else { this.userRole = 'user'; }
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Asset Master');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.showLoadingSpinner = true;
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
          if (this.userAccessModel.readAccess || this.userRole == 'superadmin') {
            this.loadList();
          } else {
            this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>
              You do not have permission to view this page.<br>Please contact your administrator.`;
            this.hideLoadingSpinner();
          }
        }
      },
      () => { this.errorMessage = 'Unable to load this page right now. Please try again.'; this.hideLoadingSpinner(); }
    );
  }

  loadList(): void {
    this.service.getAssetList().subscribe({
      next: (d: any[]) => {
        this.dataSource = new MatTableDataSource(d || []);
        this.dataSource.sort = this.sort;
        this.dataSource.paginator = this.paginator;
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load the asset list right now. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  applyFilter(event: Event): void {
    this.filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort): void {
    if (sortState.direction) this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    else this._liveAnnouncer.announce('Sorting cleared');
  }

  deleteClickButton(row: any): void {
    this.dialog.open(DialogConfirmationComponent, {
      data: `Are you sure you want to delete asset <b>${row.AssetCode} — ${row.Name}</b>?`
    }).afterClosed().subscribe((result: { confirmDialog: boolean }) => {
      if (result?.confirmDialog) {
        this.showLoadingSpinner = true;
        this.service.deleteAssetRecord(row.ID, this.currentUser).subscribe({
          next: (res: any) => {
            this.showMessage(res?.message || 'Asset deleted successfully.', 'success', 'Success Message');
            this.loadList();
          },
          error: (err: any) => {
            this.showMessage(this.extractErrorMessage(err, 'Unable to delete the asset. Please try again.'), 'error', 'Error Message');
            this.hideLoadingSpinner();
          }
        });
      }
    });
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Available':         return 'badge-success';
      case 'Allocated':         return 'badge-primary';
      case 'Under_Maintenance': return 'badge-warning';
      case 'Damaged':           return 'badge-danger';
      case 'Disposed':          return 'badge-secondary';
      case 'In_Transit':        return 'badge-info';
      case 'Lost':              return 'badge-dark';
      default:                  return 'badge-secondary';
    }
  }

  statusLabel(status: string): string {
    return status?.replace(/_/g, ' ') || 'Unknown';
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  private extractErrorMessage(err: any, fallback: string): string {
    if (err && typeof err === 'string') return err;
    if (err?.error?.message) return err.error.message;
    if (err?.message) return err.message;
    return fallback;
  }

  private showMessage(message: string, icon: 'success' | 'warning' | 'info' | 'error',
    title: 'Success Message' | 'Warning Message' | 'Error Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon,
      showCloseButton: false, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }
}
