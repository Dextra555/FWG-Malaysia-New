import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({
  selector: 'app-stock-balance-summary',
  templateUrl: './stock-balance-summary.component.html',
  styleUrls: ['./stock-balance-summary.component.css']
})
export class StockBalanceSummaryComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; }
  }

  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  // Filters
  warehouses: any[] = [];
  selectedWarehouseID: number = 0;
  showReorderOnly: boolean = false;
  filterValue: string = '';

  displayedColumns: string[] = [
    'WarehouseName', 'ItemCode', 'ItemName', 'CategoryName',
    'UOMName', 'CurrentStock', 'ReorderLevel', 'Status',
    'LastUnitCost', 'StockValue', 'LastMovementDate', 'LastTransactionType'
  ];
  dataSource: MatTableDataSource<any> = new MatTableDataSource<any>([]);
  pageSizeOptions: number[] = [10, 20, 30, 50, 100];

  // Summary totals
  totalStockValue: number = 0;
  belowReorderCount: number = 0;

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Stock Balance Summary');
    this.loadWarehouses();
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
            this.loadSummary();
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

  loadWarehouses(): void {
    this.service.getWarehouseMasters().subscribe({
      next: (d: any[]) => { this.warehouses = (d || []).filter((w: any) => w.IsActive); },
      error: () => {}
    });
  }

  loadSummary(): void {
    this.showLoadingSpinner = true;
    this.service.getStockBalanceSummary(this.selectedWarehouseID, 0).subscribe({
      next: (d: any[]) => {
        let data = d || [];
        if (this.showReorderOnly) {
          data = data.filter((x: any) => x.BelowReorderLevel);
        }
        this.dataSource = new MatTableDataSource(data);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.totalStockValue  = data.reduce((s: number, x: any) => s + (x.StockValue || 0), 0);
        this.belowReorderCount = data.filter((x: any) => x.BelowReorderLevel).length;
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load stock balance data right now. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  onFilterChange(): void { this.loadSummary(); }

  applyFilter(event: Event): void {
    this.filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort): void {
    if (sortState.direction) this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    else this._liveAnnouncer.announce('Sorting cleared');
  }

  formatDate(val: any): string {
    if (!val) return '—';
    return new Date(val).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatInt(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
  }

  txTypeLabel(type: string): string {
    const map: { [k: string]: string } = {
      'OPENING': 'Opening', 'STOCK_IN': 'Stock In', 'ISSUE': 'Issue',
      'ISSUE_RETURN': 'Issue Return', 'TRANSFER_IN': 'Transfer In',
      'TRANSFER_OUT': 'Transfer Out', 'ADJ_PLUS': 'Adj +', 'ADJ_MINUS': 'Adj −',
      'PURCHASE_RTN': 'Purchase Return', 'DAMAGE_LOSS': 'Damage/Loss'
    };
    return map[type] || type;
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }
}
