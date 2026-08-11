import { Component, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { forkJoin } from 'rxjs';
import { BranchModel } from 'src/app/model/branchModel';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { PayrollModuleService } from 'src/app/service/payrollmodule.service';
import { DialogConfirmationComponent } from 'src/app/components/dialog-confirmation/dialog-confirmation.component';
import Swal from 'sweetalert2';
import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';
import { AgreementService } from 'src/app/modules/quotation-and-agreement/agreement.service';

export interface AttendanceDisplayRow {
  ID: number;
  EmployeeCode: string;
  EmployeeName: string;
  Client: string;
  BranchName: string;
  AttendanceDate: Date;
  Punch: string;
  AttendanceID: number;
}

@Component({
  selector: 'app-attendance-display',
  templateUrl: './attendance-display.component.html',
  styleUrls: ['./attendance-display.component.css']
})
export class AttendanceDisplayComponent implements OnInit {

  filterForm!: FormGroup;
  displayedColumns: string[] = ['sno', 'EmployeeCode', 'EmployeeName', 'Client', 'BranchName', 'AttendanceDate', 'Punch', 'action'];
  dataSource = new MatTableDataSource<AttendanceDisplayRow>([]);
  showLoadingSpinner = false;
  warningMessage = '';
  errorMessage = '';
  currentUser: string | null = '';
  userRole: string = '';
  userAccessModel!: UserAccessModel;
  branchModel: BranchModel[] = [];
  clientList: any[] = [];
  allRows: AttendanceDisplayRow[] = [];

  statusList = ['All Status', 'Present', 'Absent'];

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private fb: FormBuilder,
    private _payrollService: PayrollModuleService,
    private _masterService: MastermoduleService,
    private _dataService: DatasharingService,
    private _agreementService: AgreementService,
    public dialog: MatDialog
  ) {
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    };
    this.userRole = sessionStorage.getItem('userrole')!;
    if (this.userRole == '1') this.userRole = 'admin';
    else if (this.userRole == '2') this.userRole = 'superadmin';
    else this.userRole = 'user';
  }

  ngOnInit(): void {
    this.buildForm();

    this.currentUser = sessionStorage.getItem('username');
    if (!this.currentUser) {
      this._dataService.getUsername().subscribe(u => this.currentUser = u);
    }

    this.getUserAccessRights(this.currentUser!, 'Attendance Display');
  }

  buildForm(): void {
    this.filterForm = this.fb.group({
      AttendanceDate: [new Date()],
      Branch: [''],
      Client: [''],
      Status: ['All Status'],
      EmployeeCode: ['']
    });
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this.showLoadingSpinner = true;

    // admin and superadmin always get full access — no DB permission check needed
    if (this.userRole === 'admin' || this.userRole === 'superadmin') {
      this.userAccessModel = {
        readAccess: true,
        createAccess: true,
        updateAccess: true,
        deleteAccess: true,
      };
      this.warningMessage = '';
      this.showLoadingSpinner = false;
      this.loadDropdowns();
      return;
    }

    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;

          if (this.userAccessModel.readAccess === true) {
            this.warningMessage = '';
            this.loadDropdowns();
          } else {
            this.warningMessage = `Dear <B>${userName}</B>, <br>
              You do not have permissions to view this page. <br>
              If you feel you should have access to this page, Please contact administrator. <br>
              Thank you`;
          }
        }
        this.showLoadingSpinner = false;
      },
      (err) => { this.errorMessage = err; this.showLoadingSpinner = false; }
    );
  }

  loadDropdowns(): void {
    this.showLoadingSpinner = true;
    this._masterService.GetBranchListByUserName(this.currentUser!).subscribe(
      (branches) => {
        this.branchModel = branches;
        this.showLoadingSpinner = false;
        // Auto-load today's attendance
        this.onRefresh();
      },
      (err) => { this.errorMessage = err; this.showLoadingSpinner = false; }
    );
  }

  onBranchChange(branchCode: string): void {
    this.clientList = [];
    this.filterForm.patchValue({ Client: '' });
    if (branchCode) {
      this._agreementService.getClientsByBranchID(branchCode, this.currentUser!).subscribe({
        next: (data: any) => { this.clientList = data['clients'] || []; },
        error: () => {}
      });
    }
  }

  onRefresh(): void {
    this.showLoadingSpinner = true;
    this.errorMessage = '';

    const formVal = this.filterForm.value;
    const attendanceDate = formVal.AttendanceDate ? this.formatDate(new Date(formVal.AttendanceDate)) : this.formatDate(new Date());
    const branch = formVal.Branch || '';
    const client = formVal.Client || '';
    const status = formVal.Status || 'All Status';
    const employeeCode = (formVal.EmployeeCode || '').trim();

    this._payrollService.getAttendanceDisplayList(attendanceDate, branch, client, status, employeeCode).subscribe(
      (data: AttendanceDisplayRow[]) => {
        this.allRows = data || [];
        this.dataSource = new MatTableDataSource<AttendanceDisplayRow>(this.allRows);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.showLoadingSpinner = false;
      },
      (err) => { this.errorMessage = err; this.showLoadingSpinner = false; }
    );
  }

  onClear(): void {
    this.filterForm.patchValue({
      AttendanceDate: new Date(),
      Branch: '',
      Client: '',
      Status: 'All Status',
      EmployeeCode: ''
    });
    this.clientList = [];
    this.dataSource = new MatTableDataSource<AttendanceDisplayRow>([]);
    this.allRows = [];
    this.errorMessage = '';
  }

  onExport(): void {
    if (!this.allRows || this.allRows.length === 0) {
      Swal.fire({ title: 'No Data', text: 'No records to export.', icon: 'info' });
      return;
    }
    const exportData = this.allRows.map((row, i) => ({
      'No': i + 1,
      'Employee Code': row.EmployeeCode,
      'Employee Name': row.EmployeeName,
      'Client': row.Client,
      'Branch Name': row.BranchName,
      'Date': this.formatDisplayDate(row.AttendanceDate),
      'Punch': row.Punch
    }));

    const ws = XLSX.utils.json_to_sheet(exportData);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Attendance');
    const excelBuffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
    const blob = new Blob([excelBuffer], { type: 'application/octet-stream' });
    saveAs(blob, `Attendance_Display_${this.formatDisplayDate(new Date())}.xlsx`);
  }

  onViewDetail(row: AttendanceDisplayRow): void {
    // Navigate to existing attendance entry page for this record
    // Open a dialog or navigate
    Swal.fire({
      title: row.EmployeeName,
      html: `
        <table class="table table-sm text-left" style="font-size:14px">
          <tr><td><b>Employee Code</b></td><td>${row.EmployeeCode}</td></tr>
          <tr><td><b>Client</b></td><td>${row.Client || 'Not Assigned'}</td></tr>
          <tr><td><b>Branch</b></td><td>${row.BranchName}</td></tr>
          <tr><td><b>Date</b></td><td>${this.formatDisplayDate(row.AttendanceDate)}</td></tr>
          <tr><td><b>Punch</b></td><td>${row.Punch}</td></tr>
        </table>`,
      icon: 'info',
      confirmButtonText: 'Close',
      width: '500px'
    });
  }

  onDelete(row: AttendanceDisplayRow): void {
    this.dialog.open(DialogConfirmationComponent, {
      data: `Are you sure you want to delete the attendance for ${row.EmployeeName}?`
    }).afterClosed().subscribe((result: { confirmDialog: boolean }) => {
      if (result?.confirmDialog) {
        this.showLoadingSpinner = true;
        this._payrollService.deleteAttendance(row.AttendanceID, this.currentUser!).subscribe(
          (response) => {
            if (response?.Success === 'Success') {
              Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title: 'Deleted', text: response.Message, icon: 'success', timer: 3000 });
              this.onRefresh();
            }
            this.showLoadingSpinner = false;
          },
          (err) => { this.errorMessage = err; this.showLoadingSpinner = false; }
        );
      }
    });
  }

  private formatDate(date: Date): string {
    const y = date.getFullYear();
    const m = ('0' + (date.getMonth() + 1)).slice(-2);
    const d = ('0' + date.getDate()).slice(-2);
    return `${y}-${m}-${d}T00:00:00`;
  }

  private formatDisplayDate(date: any): string {
    if (!date) return '';
    const d = new Date(date);
    if (isNaN(d.getTime())) return '';
    const day = ('0' + d.getDate()).slice(-2);
    const month = ('0' + (d.getMonth() + 1)).slice(-2);
    const year = d.getFullYear();
    return `${day} ${this.getMonthAbbr(d.getMonth())} ${year}`;
  }

  private getMonthAbbr(month: number): string {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return months[month];
  }
}
