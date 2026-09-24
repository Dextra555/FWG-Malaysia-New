import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { PayrollModuleService } from '../../../service/payrollmodule.service';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({
  selector: 'app-employee-issue-register',
  templateUrl: './employee-issue-register.component.html',
  styleUrls: ['./employee-issue-register.component.css']
})
export class EmployeeIssueRegisterComponent implements OnInit, AfterViewInit {

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
  dateFrom: string = '';
  dateTo: string = '';
  issueToTypeFilter: string = 'ALL';
  selectedEmployeeID: number = 0;
  filterValue: string = '';

  // Employee list for filter dropdown
  employeeList: any[] = [];
  filteredEmployeeList: any[] = [];
  employeeSearch: string = '';

  displayedColumns: string[] = [
    'IssueNo', 'IssueDate', 'WarehouseName', 'IssueToType',
    'EmployeeName', 'ItemCode', 'ItemName', 'UOMName',
    'IssueQty', 'ReturnedQty', 'NetQty', 'UnitCost', 'TotalValue', 'IssuedBy'
  ];
  dataSource: MatTableDataSource<any> = new MatTableDataSource<any>([]);
  pageSizeOptions: number[] = [10, 20, 30, 50, 100];

  // Totals
  totalIssued: number = 0;
  totalReturned: number = 0;
  totalNet: number = 0;
  totalValue: number = 0;

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService,
    private _payrollService: PayrollModuleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    // Default: current month
    const now   = new Date();
    const first = new Date(now.getFullYear(), now.getMonth(), 1);
    this.dateFrom = this.toDateString(first);
    this.dateTo   = this.toDateString(now);
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Employee Issue Register');
    this.loadEmployeeList();
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
            this.loadRegister();
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

  loadEmployeeList(): void {
    this._payrollService.getEmployeeList().subscribe({
      next: (d: any[]) => {
        this.employeeList = d || [];
        this.filteredEmployeeList = this.employeeList;
      },
      error: () => {}
    });
  }

  onEmployeeSearch(val: string): void {
    this.employeeSearch = val;
    const lower = val.trim().toLowerCase();
    this.filteredEmployeeList = lower
      ? this.employeeList.filter((e: any) =>
          (e.EMP_NAME || '').toLowerCase().includes(lower) ||
          String(e.EMP_ID || '').includes(lower))
      : this.employeeList;
  }

  onEmployeeSelect(emp: any): void {
    this.selectedEmployeeID = emp?.EMP_ID ?? 0;
  }

  loadRegister(): void {
    if (!this.dateFrom || !this.dateTo) {
      this.errorMessage = 'Please select a date range.';
      return;
    }
    this.showLoadingSpinner = true;
    this.service.getEmployeeIssueRegister(
      this.dateFrom, this.dateTo, this.selectedEmployeeID, this.issueToTypeFilter
    ).subscribe({
      next: (d: any[]) => {
        const data = d || [];
        this.dataSource = new MatTableDataSource(data);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.totalIssued   = data.reduce((s: number, x: any) => s + (x.IssueQty   || 0), 0);
        this.totalReturned = data.reduce((s: number, x: any) => s + (x.ReturnedQty|| 0), 0);
        this.totalNet      = data.reduce((s: number, x: any) => s + (x.NetQty     || 0), 0);
        this.totalValue    = data.reduce((s: number, x: any) => s + (x.TotalValue || 0), 0);
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load the register right now. Please try again.'; this.hideLoadingSpinner(); }
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

  toDateString(d: Date): string {
    return d.toISOString().split('T')[0];
  }

  formatDate(val: any): string {
    if (!val) return '';
    return new Date(val).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }
}
