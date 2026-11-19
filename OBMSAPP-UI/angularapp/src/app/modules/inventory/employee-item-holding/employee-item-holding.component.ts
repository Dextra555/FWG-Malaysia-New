import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({
  selector: 'app-employee-item-holding',
  templateUrl: './employee-item-holding.component.html',
  styleUrls: ['./employee-item-holding.component.css']
})
export class EmployeeItemHoldingComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; }
    if (this.detailSource) { this.detailSource.paginator = this.detailPaginator; this.detailSource.sort = this.detailSort; }
  }

  @ViewChild('detailPaginator') detailPaginator!: MatPaginator;
  @ViewChild('detailSort') detailSort!: MatSort;

  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  // Filters
  issueToTypeFilter: string = 'ALL';
  filterValue: string = '';

  // Summary list (one row per employee)
  summaryColumns: string[] = ['EmployeeName', 'IssueToType', 'ItemCount', 'TotalValue', 'LastIssueDate', 'action'];
  dataSource: MatTableDataSource<any> = new MatTableDataSource<any>([]);
  pageSizeOptions: number[] = [10, 20, 30, 50];

  // Detail panel (items for selected employee)
  selectedEmployee: any = null;
  detailColumns: string[] = ['ItemCode', 'ItemName', 'CategoryName', 'UOMName', 'TotalIssued', 'TotalReturned', 'NetHolding', 'UnitCost', 'TotalValue', 'LastIssueNo', 'LastIssueDate'];
  detailSource: MatTableDataSource<any> = new MatTableDataSource<any>([]);
  showDetail: boolean = false;
  detailFilterValue: string = '';

  // Totals
  totalItems: number = 0;
  totalValue: number = 0;

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
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
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Employee Item Holding');
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

  loadSummary(): void {
    this.showLoadingSpinner = true;
    this.showDetail = false;
    this.selectedEmployee = null;
    this.service.getEmployeeHoldingSummaryList().subscribe({
      next: (d: any[]) => {
        let filtered = d || [];
        if (this.issueToTypeFilter !== 'ALL') {
          filtered = filtered.filter((x: any) => x.IssueToType === this.issueToTypeFilter);
        }
        this.dataSource = new MatTableDataSource(filtered);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load employee holding data right now. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  onFilterChange(): void { this.loadSummary(); }

  applyFilter(event: Event): void {
    this.filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  applyDetailFilter(event: Event): void {
    this.detailFilterValue = (event.target as HTMLInputElement).value;
    this.detailSource.filter = this.detailFilterValue.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort): void {
    if (sortState.direction) this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    else this._liveAnnouncer.announce('Sorting cleared');
  }

  viewDetail(row: any): void {
    this.showLoadingSpinner = true;
    this.selectedEmployee = row;
    this.service.getEmployeeHoldingDetail(row.EmployeeID, row.IssueToType).subscribe({
      next: (d: any[]) => {
        this.detailSource = new MatTableDataSource(d || []);
        this.detailSource.paginator = this.detailPaginator;
        this.detailSource.sort = this.detailSort;
        this.totalItems = d?.length || 0;
        this.totalValue = d?.reduce((s: number, x: any) => s + (x.TotalValue || 0), 0) || 0;
        this.showDetail = true;
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load item detail. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  backToList(): void {
    this.showDetail = false;
    this.selectedEmployee = null;
    this.detailFilterValue = '';
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

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }
}
