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

export interface IOpeningStock {
  ID: number;
  ReferenceNo: string;
  WarehouseName: string;
  ItemCode: string;
  ItemName: string;
  CategoryName: string;
  UOMName: string;
  OpeningDate: Date;
  OpeningQty: number;
  UnitCost: number;
  TotalValue: number;
  Remarks: string;
}

@Component({
  selector: 'app-opening-stock',
  templateUrl: './opening-stock.component.html',
  styleUrls: ['./opening-stock.component.css']
})
export class OpeningStockComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) {
      this.dataSource.paginator = this.paginator;
      this.dataSource.sort = this.sort;
    }
  }

  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  displayedColumns: string[] = [
    'ReferenceNo', 'OpeningDate', 'WarehouseName', 'ItemCode', 'ItemName',
    'CategoryName', 'UOMName', 'OpeningQty', 'UnitCost', 'TotalValue', 'action'
  ];
  dataSource: MatTableDataSource<IOpeningStock> = new MatTableDataSource<IOpeningStock>([]);
  pageSizeOptions: number[] = [];

  filterValue: string = '';

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
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Opening Stock');
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

          if (this.userAccessModel.readAccess || this.currentUser === 'superadmin') {
            this.loadList();
          } else {
            this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>
              You do not have permission to view this page.<br>
              Please contact your administrator.`;
            this.hideLoadingSpinner();
          }
        }
      },
      () => {
        this.errorMessage = 'Unable to load this page right now. Please try again.';
        this.hideLoadingSpinner();
      }
    );
  }

  loadList(): void {
    this.service.getOpeningStockList().subscribe({
      next: (d: any[]) => {
        if (d && d.length > 0) {
          this.dataSource = new MatTableDataSource(d);
          this.pageSizeOptions = [];
          const total = d.length;
          for (let i = 10; i <= total && i <= 1000; i += 10) this.pageSizeOptions.push(i);
          if (total > 0 && total < 10) this.pageSizeOptions.push(total);
          this.dataSource.sort = this.sort;
          this.dataSource.paginator = this.paginator;
        } else {
          this.dataSource = new MatTableDataSource<IOpeningStock>([]);
          this.errorMessage = '';  // empty state shown via template
        }
        this.hideLoadingSpinner();
      },
      error: () => {
        this.errorMessage = 'Unable to load the opening stock list right now. Please try again.';
        this.hideLoadingSpinner();
      }
    });
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.filterValue = filterValue;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort): void {
    if (sortState.direction) {
      this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this._liveAnnouncer.announce('Sorting cleared');
    }
  }

  deleteClickButton(row: IOpeningStock): void {
    this.dialog
      .open(DialogConfirmationComponent, {
        data: `Are you sure you want to remove the opening stock for <b>${row.ItemName}</b> in <b>${row.WarehouseName}</b>?<br>This will also remove the corresponding stock ledger entry.`
      })
      .afterClosed()
      .subscribe((result: { confirmDialog: boolean }) => {
        if (result?.confirmDialog) {
          this.showLoadingSpinner = true;
          this.service.deleteOpeningStock(row.ID, this.currentUser).subscribe({
            next: (res: any) => {
              this.showMessage(res?.message || 'Opening stock entry removed successfully.', 'success', 'Success Message');
              this.loadList();
            },
            error: (err: any) => {
              const msg = this.extractErrorMessage(err,
                'Unable to remove the opening stock entry right now. Please try again.');
              this.showMessage(msg, 'error', 'Error Message');
              this.hideLoadingSpinner();
            }
          });
        }
      });
  }

  formatDate(dateVal: any): string {
    if (!dateVal) return '';
    const d = new Date(dateVal);
    return d.toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' });
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

  private showMessage(
    message: string,
    icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message' = 'Success Message'
  ): void {
    Swal.fire({
      toast: true, position: 'top', showConfirmButton: false,
      title, text: message, icon,
      showCloseButton: false, timer: 5000, width: '600px',
      customClass: { popup: 'swal-top-offset' }
    });
    this.hideLoadingSpinner();
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }
}
