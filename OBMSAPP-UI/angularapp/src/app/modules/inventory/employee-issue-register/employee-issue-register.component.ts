import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
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
  dateFrom: Date | null = null;
  dateTo: Date | null = null;
  issueToTypeFilter: string = 'ALL';
  selectedEmployeeID: number = 0;
  clientNameFilter: string = '';
  isDeductionFilter: string = 'ALL';   // 'ALL' | 'true' | 'false'
  selectedClientID: number = 0;
  filterValue: string = '';

  // Raw unfiltered data â€” kept to rebuild dropdowns after type/filter changes
  private allData: any[] = [];

  // Employee list for filter dropdown
  employeeList: any[] = [];
  filteredEmployeeList: any[] = [];
  employeeSearch: string = '';

  // Client list for filter dropdown
  clientList: any[] = [];
  filteredClientList: any[] = [];
  clientSearch: string = '';

  displayedColumns: string[] = [
    'IssueNo', 'IssueDate', 'WarehouseName', 'IssueToType',
    'EmployeeName', 'ClientName', 'IsDeduction', 'ItemCode', 'ItemName', 'UOMName',
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
  ,
    private titleService: Title) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    // Default: current month
    const now   = new Date();
    const first = new Date(now.getFullYear(), now.getMonth(), 1);
    this.dateFrom = first;
    this.dateTo   = now;
  }

  ngOnInit(): void {
    this.titleService.setTitle('Employee Issue Register');
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Employee Issue Register');
    this.loadClientList();
  }

  loadClientList(): void {
    // Client list is built from the loaded register data â€” not from master
    // See buildClientListFromData() called after loadRegister()
  }

  buildClientListFromData(data: any[]): void {
    const seen = new Set<number>();
    this.clientList = [];
    for (const row of data) {
      if (row.ClientID && row.ClientID > 0 && !seen.has(row.ClientID)) {
        seen.add(row.ClientID);
        this.clientList.push({ ID: row.ClientID, Name: row.ClientName || 'â€”' });
      }
    }
    this.clientList.sort((a, b) => a.Name.localeCompare(b.Name));
    this.filteredClientList = [...this.clientList];
    this.clientSearch = '';
  }

  buildEmployeeListFromData(data: any[]): void {
    const seen = new Set<number>();
    this.employeeList = [];
    for (const row of data) {
      const empID = row.EmployeeID;
      if (empID && empID > 0 && !seen.has(empID)) {
        seen.add(empID);
        this.employeeList.push({ EMP_ID: empID, EMP_NAME: row.EmployeeName || 'â€”' });
      }
    }
    this.employeeList.sort((a, b) => a.EMP_NAME.localeCompare(b.EMP_NAME));
    this.filteredEmployeeList = [...this.employeeList];
    this.employeeSearch = '';
  }

  onClientSearch(val: string): void {
    this.clientSearch = val;
    const lower = val.trim().toLowerCase();
    this.filteredClientList = lower
      ? this.clientList.filter((c: any) => (c.Name || '').toLowerCase().includes(lower))
      : [...this.clientList];
  }

  onClientFilterChange(): void {
    if (this.selectedClientID === 0) {
      // Show all â€” re-run search with current params
      this.loadRegister();
    } else {
      this.loadRegister();
    }
  }

  onTypeFilterChange(): void {
    // Rebuild employee dropdown to only show employees matching selected type
    const filtered = this.issueToTypeFilter === 'ALL'
      ? this.allData
      : this.allData.filter((r: any) => r.IssueToType === this.issueToTypeFilter);
    this.buildEmployeeListFromData(filtered);
    this.selectedEmployeeID = 0;
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
    const fromStr = this.toDateString(this.dateFrom);
    const toStr   = this.toDateString(this.dateTo);
    this.showLoadingSpinner = true;
    this.service.getEmployeeIssueRegister(
      fromStr, toStr, this.selectedEmployeeID, this.issueToTypeFilter,
      this.clientNameFilter, this.isDeductionFilter, this.selectedClientID
    ).subscribe({
      next: (d: any[]) => {
        const data = d || [];
        this.allData = data;   // store full raw result
        this.dataSource = new MatTableDataSource(data);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.totalIssued   = data.reduce((s: number, x: any) => s + (x.IssueQty   || 0), 0);
        this.totalReturned = data.reduce((s: number, x: any) => s + (x.ReturnedQty|| 0), 0);
        this.totalNet      = data.reduce((s: number, x: any) => s + (x.NetQty     || 0), 0);
        this.totalValue    = data.reduce((s: number, x: any) => s + (x.TotalValue || 0), 0);
        // Build client dropdown from actual data in this result
        this.buildClientListFromData(data);
        this.selectedClientID = 0;
        // Build employee dropdown from actual data in this result
        this.buildEmployeeListFromData(data);
        this.selectedEmployeeID = 0;
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
    const year  = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day   = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  formatDate(val: any): string {
    if (!val) return '';
    return new Date(val).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  exportExcel(): void {
    const data = this.dataSource.data;
    if (!data || data.length === 0) {
      Swal.fire({ toast: true, position: 'top', icon: 'warning', title: 'Warning Message',
        text: 'No data to export. Please search first.', showConfirmButton: false,
        timer: 3000, width: '500px', customClass: { popup: 'swal-top-offset' } });
      return;
    }

    const headers = [
      'Issue No', 'Date', 'Warehouse', 'Type', 'Employee Name',
      'Client', 'Deduction', 'Item Code', 'Item Name', 'UOM',
      'Issued Qty', 'Returned Qty', 'Net Qty', 'Unit Cost', 'Total Value (RM)', 'Issued By'
    ];

    const rows = data.map((r: any) => [
      r.IssueNo        || '',
      this.formatDate(r.IssueDate),
      r.WarehouseName  || '',
      r.IssueToType    || '',
      r.EmployeeName   || '',
      r.ClientName     || '',
      r.IsDeduction ? 'Deduction' : 'Non-Deduction',
      r.ItemCode       || '',
      r.ItemName       || '',
      r.UOMName        || '',
      r.IssueQty       ?? 0,
      r.ReturnedQty    ?? 0,
      r.NetQty         ?? 0,
      r.UnitCost       ?? 0,
      r.TotalValue     ?? 0,
      r.IssuedBy       || ''
    ]);

    // Build HTML table Excel
    let html = '<html xmlns:o="urn:schemas-microsoft-com:office:office" '
             + 'xmlns:x="urn:schemas-microsoft-com:office:excel" '
             + 'xmlns="http://www.w3.org/TR/REC-html40">'
             + '<head><meta charset="UTF-8"/>'
             + '<style>body,td,th{font-family:Arial;font-size:10pt;}'
             + '.th{background:#1a3a5c;color:#fff;font-weight:bold;padding:5px;border:1px solid #ccc;}'
             + '.td{padding:4px;border:1px solid #ddd;}'
             + '.tn{padding:4px;border:1px solid #ddd;text-align:right;}'
             + '.tot{background:#e8f0fe;font-weight:bold;border-top:2px solid #1a3a5c;}'
             + '</style></head><body>'
             + '<table style="width:100%;border-collapse:collapse;">';

    // Title
    html += `<tr><td colspan="${headers.length}" style="font-size:13pt;font-weight:bold;color:#1a3a5c;padding:6px;">Staff / Guard Issue Register</td></tr>`;

    // Filter summary
    const fromLbl = this.dateFrom ? this.toDateString(this.dateFrom) : '';
    const toLbl   = this.dateTo   ? this.toDateString(this.dateTo)   : '';
    html += `<tr><td colspan="${headers.length}" style="font-size:9pt;padding:3px;">Period: ${fromLbl} to ${toLbl} &nbsp;|&nbsp; Type: ${this.issueToTypeFilter} &nbsp;|&nbsp; Deduction: ${this.isDeductionFilter}</td></tr>`;
    html += `<tr><td colspan="${headers.length}" style="padding:2px;"></td></tr>`;

    // Header row
    html += '<tr>' + headers.map(h => `<td class="th">${h}</td>`).join('') + '</tr>';

    // Data rows
    const numCols = new Set([10,11,12,13,14]); // 0-indexed: IssueQty onwards
    rows.forEach((row: any[], i: number) => {
      const alt = i % 2 === 1 ? 'background:#f4f7fb;' : '';
      html += '<tr>' + row.map((v: any, ci: number) =>
        `<td class="${numCols.has(ci) ? 'tn' : 'td'}" style="${alt}">${v}</td>`
      ).join('') + '</tr>';
    });

    // Totals row
    html += '<tr>'
          + `<td class="tot td" colspan="10"><b>TOTAL</b></td>`
          + `<td class="tot tn">${this.formatNumber(this.totalIssued)}</td>`
          + `<td class="tot tn">${this.formatNumber(this.totalReturned)}</td>`
          + `<td class="tot tn">${this.formatNumber(this.totalNet)}</td>`
          + `<td class="tot tn"></td>`
          + `<td class="tot tn">${this.formatNumber(this.totalValue)}</td>`
          + `<td class="tot td"></td>`
          + '</tr>';

    html += '</table></body></html>';

    const blob = new Blob([html], { type: 'application/vnd.ms-excel' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `StaffGuardIssueRegister_${this.toDateString(new Date())}.xls`;
    a.click();
    URL.revokeObjectURL(url);
  }
}

