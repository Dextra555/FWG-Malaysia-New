import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Router } from '@angular/router';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { InventoryService } from '../../../service/inventory.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import Swal from 'sweetalert2';

export interface ITimesheetRow {
  Day: number;
  Weekday: string;
  ItemName: string;
  CategoryName: string;
  NoOfUnits: number;
  CostPerUnit: number;
  Amount: number;
  InvoiceNo: string;
  InvoiceDate: string;
}

@Component({
  selector: 'app-inventory-timesheet',
  templateUrl: './inventory-timesheet.component.html',
  styleUrls: ['./inventory-timesheet.component.css']
})
export class InventoryTimesheetComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  frm!: FormGroup;
  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  branchList: any[] = [];
  timesheetData: ITimesheetRow[] = [];
  summaryByItem: any[] = [];
  totalAmount: number = 0;
  totalUnits: number = 0;

  selectedBranch: string = '';
  selectedYear: number = new Date().getFullYear();
  selectedMonth: number = new Date().getMonth() + 1;

  displayedColumns: string[] = [
    'Day', 'Weekday', 'InvoiceNo', 'CategoryName', 'ItemName', 'NoOfUnits', 'CostPerUnit', 'Amount'
  ];
  dataSource = new MatTableDataSource<ITimesheetRow>();
  pageSizeOptions: number[] = [10, 20, 30, 50];

  monthList = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' },
  ];

  yearList: number[] = [];

  constructor(
    private fb: FormBuilder,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService,
    public dialog: MatDialog,
    private _liveAnnouncer: LiveAnnouncer,
    private route: Router
  ) {
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    };

    const currentYear = new Date().getFullYear();
    for (let y = currentYear - 3; y <= currentYear + 1; y++) {
      this.yearList.push(y);
    }
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (this.currentUser == null || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
        this.init();
      });
    } else {
      this.init();
    }
  }

  init(): void {
    this.frm = this.fb.group({
      Branch: ['', Validators.required],
      Year: [new Date().getFullYear(), Validators.required],
      Month: [new Date().getMonth() + 1, Validators.required],
    });
    this.getUserAccessRights(this.currentUser, 'Inventory Timesheet');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;

          if (this.userAccessModel.readAccess === true || this.currentUser === 'superadmin') {
            this.warningMessage = '';
            this.loadBranches();
          } else {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
              You do not have permissions to view this page. <br>
              If you feel you should have access to this page, Please contact administrator. <br>
              Thank you`;
            this.hideLoadingSpinner();
          }
        }
      },
      (error) => this.handleErrors(error)
    );
  }

  loadBranches(): void {
    this.service.getBranchList().subscribe(
      (data: any) => {
        this.branchList = data;
        this.hideLoadingSpinner();
      },
      (error) => this.handleErrors(error)
    );
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  onSearch(): void {
    if (this.frm.invalid) {
      this.showMessage('Please select Branch, Year and Month.', 'warning', 'Warning Message');
      return;
    }

    const { Branch, Year, Month } = this.frm.value;
    this.selectedBranch = Branch;
    this.selectedYear = Year;
    this.selectedMonth = Month;
    this.showLoadingSpinner = true;
    this.errorMessage = '';

    this.service.getInventoryTimesheet(Branch, Year, Month).subscribe(
      (data: any) => {
        this.timesheetData = data || [];
        this.dataSource = new MatTableDataSource<ITimesheetRow>(this.timesheetData);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;

        this.totalAmount = this.timesheetData.reduce((sum, row) => sum + (row.Amount || 0), 0);
        this.totalUnits = this.timesheetData.reduce((sum, row) => sum + (row.NoOfUnits || 0), 0);

        // Build per-item summary
        const summaryMap: { [key: string]: any } = {};
        this.timesheetData.forEach(row => {
          const key = `${row.CategoryName}||${row.ItemName}`;
          if (!summaryMap[key]) {
            summaryMap[key] = { CategoryName: row.CategoryName, ItemName: row.ItemName, TotalUnits: 0, TotalAmount: 0 };
          }
          summaryMap[key].TotalUnits += row.NoOfUnits || 0;
          summaryMap[key].TotalAmount += row.Amount || 0;
        });
        this.summaryByItem = Object.values(summaryMap);

        if (this.timesheetData.length === 0) {
          this.errorMessage = `No inventory issues found for <b>${this.getBranchName(Branch)}</b> in ${this.getMonthLabel(Month)} ${Year}.`;
        }

        this.hideLoadingSpinner();
      },
      (error) => this.handleErrors(error)
    );
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  getBranchName(code: string): string {
    const branch = this.branchList.find(b => b.Code === code);
    return branch ? branch.Name : code;
  }

  getMonthLabel(month: number): string {
    const m = this.monthList.find(x => x.value === month);
    return m ? m.label : '';
  }

  getWeekdayClass(weekday: string): string {
    if (weekday === 'Sunday') return 'text-danger fw-bold';
    if (weekday === 'Saturday') return 'text-warning fw-bold';
    return '';
  }

  private showMessage(
    message: string,
    icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message' = 'Warning Message'
  ): void {
    Swal.fire({
      toast: true,
      position: 'top',
      showConfirmButton: false,
      title: title,
      text: message,
      icon: icon,
      showCloseButton: false,
      timer: 4000,
      width: '600px',
      customClass: { popup: 'swal-top-offset' }
    });
  }

  handleErrors(error: string): void {
    if (error != null && error !== '') {
      this.errorMessage = error;
    }
    this.hideLoadingSpinner();
  }

  hideLoadingSpinner(): void {
    this.showLoadingSpinner = false;
  }
}
