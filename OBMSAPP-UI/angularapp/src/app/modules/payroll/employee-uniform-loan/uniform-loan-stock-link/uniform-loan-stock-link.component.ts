import { LiveAnnouncer } from '@angular/cdk/a11y';
import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Router } from '@angular/router';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { InventoryService } from 'src/app/service/inventory.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';

@Component({
  selector: 'app-uniform-loan-stock-link',
  templateUrl: './uniform-loan-stock-link.component.html',
  styleUrls: ['./uniform-loan-stock-link.component.css']
})
export class UniformLoanStockLinkComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  currentUser        = '';
  showLoadingSpinner = false;
  warningMessage     = '';
  errorMessage       = '';
  userAccessModel!: UserAccessModel;

  // ── filter controls ──────────────────────────────────────────────────────
  branchList:          any[] = [];
  employeeList:        any[] = [];
  selectedBranchCode   = '';
  selectedIssueToType  = '';    // '' = All, 'Guard', 'Staff'
  selectedEmployeeID   = 0;
  loadingBranches      = false;
  loadingEmployees     = false;

  // ── master data ───────────────────────────────────────────────────────────
  allRows: any[] = [];

  // ── table ─────────────────────────────────────────────────────────────────
  displayedColumns: string[] = [
    'IssueNo', 'IssueDate', 'EmployeeName', 'ItemSummary', 'TotalValue', 'action'
  ];
  dataSource       = new MatTableDataSource<any>([]);
  pageSizeOptions  = [10, 20, 30, 50];
  filterValue      = '';
  expandedRow: any = null;

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    private _inventoryService: InventoryService,
    private _masterService: MastermoduleService,
    private _dataService: DatasharingService,
    private _router: Router
  ) {
    this.userAccessModel = {
      readAccess: false, updateAccess: false,
      deleteAccess: false, createAccess: false
    };
  }

  // ── lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username') ?? '';
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => this.currentUser = u);
    }
    this.getUserAccessRights(this.currentUser, 'Uniform Loan Stock Issues');
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort      = this.sort;
  }

  // ── access rights ─────────────────────────────────────────────────────────
  getUserAccessRights(userName: string, screenName: string): void {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe({
      next: (data) => {
        if (data) {
          this.userAccessModel.readAccess   = data.Read   ?? data.read   ?? false;
          this.userAccessModel.updateAccess = data.Update ?? data.update ?? false;
          this.userAccessModel.createAccess = data.Create ?? data.create ?? false;
          this.userAccessModel.deleteAccess = data.Delete ?? data.delete ?? false;
        }
        if (this.userAccessModel.readAccess || this.currentUser === 'superadmin') {
          this.warningMessage = '';
          this.loadBranches();
          this.loadAllPendingIssues();
        } else {
          this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>
            You do not have permissions to view this page.<br>
            Please contact your administrator.`;
          this.hideSpinner();
        }
      },
      error: () => {
        if (this.currentUser === 'superadmin') {
          this.loadBranches();
          this.loadAllPendingIssues();
        } else {
          this.warningMessage = 'Unable to verify access rights. Please try again.';
          this.hideSpinner();
        }
      }
    });
  }

  // ── load all pending issues on page open ──────────────────────────────────
  loadAllPendingIssues(): void {
    this.showLoadingSpinner = true;
    this._inventoryService.getAllPendingStockIssues().subscribe({
      next: (data: any[]) => {
        this.allRows = data ?? [];
        this.applyFilters();
        this.hideSpinner();
      },
      error: () => {
        this.errorMessage = 'Unable to load pending stock issues. Please try again.';
        this.hideSpinner();
      }
    });
  }

  // ── load branch list ──────────────────────────────────────────────────────
  loadBranches(): void {
    this.loadingBranches = true;
    this._masterService.getBranchMasterList().subscribe({
      next: (data: any) => {
        this.branchList    = Array.isArray(data) ? data : (data?.Result ?? []);
        this.loadingBranches = false;
      },
      error: () => { this.loadingBranches = false; }
    });
  }

  // ── branch or type changed → reload employee list ─────────────────────────
  onBranchOrTypeChange(): void {
    this.selectedEmployeeID = 0;
    this.employeeList       = [];

    // Apply branch filter immediately — uses EmployeeBranchCode from allRows directly
    this.applyFilters();

    // Also load employee dropdown for the employee filter (if branch selected)
    if (!this.selectedBranchCode) return;

    this.loadingEmployees = true;
    this._inventoryService.getEmployeesByBranch(
      this.selectedBranchCode, this.selectedIssueToType
    ).subscribe({
      next: (data: any[]) => {
        this.employeeList     = data ?? [];
        this.loadingEmployees = false;
      },
      error: () => { this.loadingEmployees = false; }
    });
  }

  // ── employee changed ──────────────────────────────────────────────────────
  onEmployeeChange(): void {
    this.applyFilters();
  }

  // ── apply all filters client-side ────────────────────────────────────────
  applyFilters(): void {
    let filtered = [...this.allRows];

    // Branch filter — use EmployeeBranchCode directly from each row (no async dependency)
    if (this.selectedBranchCode) {
      filtered = filtered.filter(r =>
        (r.EmployeeBranchCode ?? '').toLowerCase() === this.selectedBranchCode.toLowerCase()
      );
    }

    // Employee filter
    if (this.selectedEmployeeID > 0) {
      filtered = filtered.filter(r => r.EmployeeID === +this.selectedEmployeeID);
    }

    this.dataSource = new MatTableDataSource<any>(filtered);
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort      = this.sort;
    this.dataSource.filterPredicate = (row: any, filter: string) => {
      const fields = [row.IssueNo, row.EmployeeName]
        .map(v => (v ?? '').toLowerCase()).join(' ');
      const items = (row.Lines ?? [])
        .map((l: any) => (l.ItemName ?? '') + ' ' + (l.ItemCode ?? ''))
        .join(' ').toLowerCase();
      return fields.includes(filter) || items.includes(filter);
    };
    if (this.filterValue) {
      this.dataSource.filter = this.filterValue.trim().toLowerCase();
    }
    this.expandedRow = null;
  }

  // ── text search ───────────────────────────────────────────────────────────
  applyFilter(e: Event): void {
    this.filterValue       = (e.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  announceSortChange(s: Sort): void {
    this._liveAnnouncer.announce(
      s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'
    );
  }

  toggleExpand(row: any): void {
    this.expandedRow = this.expandedRow === row ? null : row;
  }

  getItemSummary(row: any): string {
    const lines: any[] = row.Lines ?? [];
    if (!lines.length) return '—';
    const suffix = lines.length > 1 ? ` +${lines.length - 1} more` : '';
    return `${lines[0].ItemName} (${lines[0].IssueQty})${suffix}`;
  }

  formatDate(val: any): string {
    if (!val) return '';
    const d = new Date(val);
    if (isNaN(d.getTime())) return val;
    return [
      String(d.getDate()).padStart(2, '0'),
      String(d.getMonth() + 1).padStart(2, '0'),
      d.getFullYear()
    ].join('/');
  }

  // ── actions ───────────────────────────────────────────────────────────────
  onLinkToNewLoan(row: any): void {
    this._router.navigate(['/payroll/new-employee-uniform-loan'], {
      queryParams: { stockIssueDocID: row.IssueDocID, employeeID: row.EmployeeID }
    });
  }

  onViewUniformLoans(): void {
    this._router.navigate(['/payroll/search-employee-uniform-loan']);
  }

  refresh(): void { this.loadAllPendingIssues(); }

  clearFilters(): void {
    this.selectedBranchCode  = '';
    this.selectedIssueToType = '';
    this.selectedEmployeeID  = 0;
    this.employeeList        = [];
    this.filterValue         = '';
    this.applyFilters();
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }

  get selectedBranchName(): string {
    const b = this.branchList.find(x => x.Code === this.selectedBranchCode);
    return b ? (b.Name ?? b.Code) : '';
  }
}
