import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDatepickerInputEvent } from '@angular/material/datepicker';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { debounceTime, forkJoin, Observable, Subject } from 'rxjs';
import { BranchModel } from 'src/app/model/branchModel';
import { EmployeeAdvanceListModel } from 'src/app/model/empolyeeAdvanceListModel';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { PayrollModuleService } from 'src/app/service/payrollmodule.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-payslip-report',
  templateUrl: './payslip-report.component.html',
  styleUrls: ['./payslip-report.component.css']
})
export class PayslipReportComponent implements OnInit {
  payslipForm!: FormGroup;
  showLoadingSpinner: boolean = false;
  branchModel!: BranchModel[];
  currentUser: string = '';
  advanceType: string = '';
  paymentType: string = '';
  employeeListModel!: EmployeeAdvanceListModel[];
  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  errorMessage: string = '';
  warningMessage: string = '';
  userAccessModel!: UserAccessModel;
  dtAdvanceDate!: string;
  StartPeriod!: string;
  EndPeriod!: string;
  nameList: string[] = [];
  salaryProcessStatus: boolean = false;
  temporaryEmployeeList: string[] = [];
  employeeSearchSubject = new Subject<string>();
  branchSearchSubject = new Subject<string>();
  employeeSearchString: string = '';
  branchSearchString: string = '';
  filteredEmployeeList: any[] = [];
  filteredBranchList: any[] = [];

  private formatDate(date: any) {
    const d = new Date(date);
    let month = '' + (d.getMonth() + 1);
    let day = '' + d.getDate();
    const year = d.getFullYear();
    if (month.length < 2) month = '0' + month;
    if (day.length < 2) day = '0' + day;
    return [year, month, day].join('-');
  }
  constructor(public sanitizer: DomSanitizer, private fb: FormBuilder, private _dataService: DatasharingService,
    private _masterService: MastermoduleService, private _payrollService: PayrollModuleService, private router: Router) {
    this.payslipForm = this.fb.group({
      AdvanceDate: [this.formatDate(new Date)],
      BranchCode: [''],
      EmployeeCode: [''],
      EmployeeType: ['Guard'],
      LanguageType: ['E'],
    });
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }
  }

  ngOnInit(): void {
    // Employee search debounce
    this.employeeSearchSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.employeeSearchString = '';
      this.employeeListModel = [...this.filteredEmployeeList]; // reset list
    });

    // Branch search debounce
    this.branchSearchSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.branchSearchString = '';
      this.branchModel = [...this.filteredBranchList];
    });
    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this._dataService.scrollToTop(); // Scroll to top on route change
      }
    });
    this.currentUser = sessionStorage.getItem('username')!;
    if (this.currentUser == null || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Pay Slip Report');
  }
  getUserAccessRights(userName: string, screenName: string) {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess = data.Read
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
          if (this.userAccessModel.readAccess === true || this.currentUser == 'superadmin') {
            this.warningMessage = '';
            this.getBranchMasterListByUser(this.currentUser);
          } else {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
                      You do not have permissions to view this page. <br>
                      If you feel you should have access to this page, Please contact administrator. <br>
                      Thank you`;

          }
        }
        this.hideSpinner();
      },
      (error) => {
        this.handleErrors(error);
      }
    );
  }
  changeAdvanceDate(type: string, event: MatDatepickerInputEvent<Date>) {
    this.payslipForm.value.AdvanceDate = this.formatDate(`${type}: ${event.value}`);
    let dtAdvanceDate = new Date(this.payslipForm.value.AdvanceDate);
    this.dtAdvanceDate = this.formatDate(
      new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
    );
  }
  onBranchSelectionChange(event: any) {
    if (event.value != '' && event.value != undefined) {
      let dtAdvanceDate = new Date(this.payslipForm.value.AdvanceDate);
      this.dtAdvanceDate = this.formatDate(
        new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
      );
      const advanceDate = this.formatDate(this.payslipForm.get('AdvanceDate')?.value);
      this.StartPeriod = this.formatDate(this.firstOfMonth(new Date(advanceDate)));
      this.EndPeriod = this.formatDate(this.lastOfMonth(new Date(advanceDate)));
      const branchCode = this.payslipForm.get('BranchCode')?.value;
      if (advanceDate != null && advanceDate != 'NaN-NaN-NaN' && branchCode != '') {
        this.errorMessage = '';
        this.getEmployeeListByEmployeeType(branchCode, this.payslipForm.value.EmployeeType, this.StartPeriod, this.EndPeriod, 'Active');
      } else {
        this.errorMessage = 'Please select advance date selection.';
        this.payslipForm.patchValue({
          EmployeeType: 'Guard',
        })
      }
    }
  }
  radioButtonTypeSelectionChange(event: any) {
    const attendancePeriod = this.formatDate(this.payslipForm.get('AdvanceDate')?.value);
    const branchCode = this.payslipForm.get('BranchCode')?.value;
    this.StartPeriod = this.formatDate(this.firstOfMonth(new Date(attendancePeriod)));
    this.EndPeriod = this.formatDate(this.lastOfMonth(new Date(attendancePeriod)));
    if (branchCode != undefined && branchCode != 'NaN-NaN-NaN' && branchCode != '') {
      this.errorMessage = '';
      this.getEmployeeListByEmployeeType(branchCode, event.value, this.StartPeriod, this.EndPeriod, 'Active');
    } else {
      this.errorMessage = 'Please select advance date and branch selection.';
      this.payslipForm.patchValue({
        EmployeeType: 'FGuard',
      })
    }
  }
  getEmployeeListByEmployeeType(branchCode: string, employeeType: string, startPeriod: string, endPeriod: string, status: string): void {
    this.showLoadingSpinner = true;
    forkJoin({
      employeeList: this._payrollService.getListByEmployee(branchCode, employeeType, startPeriod, endPeriod, status),
      salaryProcessStatus: this._payrollService.getIsSalaryProcessDoneForCurrentPeriod(branchCode, employeeType, this.dtAdvanceDate),
      nameList: this._payrollService.getEmployeeAttendanceList(this.dtAdvanceDate, branchCode),
      temporaryList: this._payrollService.getTemporaryEmployeeList(branchCode)
    }).subscribe(
      ({ employeeList, salaryProcessStatus, nameList, temporaryList }) => {
        this.payslipForm.patchValue({ EmployeeCode: '' });
        this.employeeListModel = employeeList;
        this.filteredEmployeeList = [...this.employeeListModel];
        this.salaryProcessStatus = salaryProcessStatus;
        this.nameList = nameList;
        this.temporaryEmployeeList = temporaryList;
        this.showLoadingSpinner = false;
      },
      (error) => this.handleErrors(error)
    );
  }
  // Function to check if EMP_CODE is in nameList
  isEmployeeInNameList(empCode: string): boolean {
    return this.nameList && this.nameList.includes(empCode);
  }

  isEmployeeProcessList(empCode: string): boolean {
    if (!this.salaryProcessStatus) {
      return false;
    }
    return this.temporaryEmployeeList.includes(empCode);
  }
  getBranchMasterListByUser(userName: string) {
    this.showLoadingSpinner = true;
    this._masterService.GetBranchListByUserName(userName).subscribe(
      (data) => {
        this.branchModel = data
        this.filteredBranchList = [...this.branchModel];
        this.showLoadingSpinner = false;
      },
      (error) => {
        this.handleErrors(error);
      }
    );
  }

  searchDropdown(searchString: string, list: any[], key: string): any[] {
    if (!searchString) return [...list]; // if empty, return full list
    return list.filter(item => item[key].toLowerCase().includes(searchString.toLowerCase()));
  }

  onKeyDropdown(
    event: KeyboardEvent,
    searchStringProp: 'employeeSearchString' | 'branchSearchString',
    listProp: 'employeeListModel' | 'branchModel',
    filteredListProp: 'filteredEmployeeList' | 'filteredBranchList',
    keyName: string,
    subject: Subject<string>
  ) {
    const key = event.key;

    this[searchStringProp] = this[searchStringProp] || '';

    if (key.length === 1) {
      this[searchStringProp] += key.toLowerCase();
    } else if (key === 'Backspace') {
      this[searchStringProp] = this[searchStringProp].slice(0, -1);
    } else if (key === 'Escape') {
      this[searchStringProp] = '';
    }

    // Apply filter immediately
    this[listProp] = this.searchDropdown(this[searchStringProp], this[filteredListProp], keyName);

    // Trigger debounce to reset after 2s of inactivity
    subject.next(this[searchStringProp]);
  }
  public firstOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
  }
  public lastOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth() + 1, 0);
  }
  // Cache to store ongoing requests for de-duplication
  private inProgressRequests: Map<string, Observable<boolean>> = new Map();

  generateExcelFileClick() {

  }
  getPayslipReportClick(): void {
    this.url = environment.baseReportUrl;
    this.url += 'Payroll/PaySlipReport.aspx?';
    this.url += "LoginID=" + this.currentUser;
    this.url += "&Branch=" + (this.payslipForm.get("BranchCode")?.value ?? '')
    this.url += "&Period=" + this.dtAdvanceDate
    this.url += "&EmployeeType=" + this.payslipForm.get("EmployeeType")?.value
    this.url += "&Employee=" + (this.payslipForm.get("EmployeeCode")?.value || '0')
    this.url += "&Lang=" + this.payslipForm.get("LanguageType")?.value
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url);
  }
  getPayslipReport2Click(): void {
    this.url = environment.baseReportUrl;
    this.url += 'Payroll/PaySlip2Report.aspx?';
    this.url += "LoginID=" + this.currentUser;
    this.url += "&Branch=" + this.payslipForm.get("BranchCode")?.value
    this.url += "&Period=" + this.dtAdvanceDate
    this.url += "&EmployeeType=" + this.payslipForm.get("EmployeeType")?.value
    this.url += "&Employee=" + (this.payslipForm.get("EmployeeCode")?.value || '0')
    this.url += "&Lang=" + this.payslipForm.get("LanguageType")?.value
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url);
  }
  getNewPayslipReportClick(): void {
    this.url = environment.baseReportUrl;
    this.url += 'Payroll/PaySlipNewReport.aspx?';
    this.url += "LoginID=" + this.currentUser;
    this.url += "&Branch=" + this.payslipForm.get("BranchCode")?.value
    this.url += "&Period=" + this.dtAdvanceDate
    this.url += "&EmployeeType=" + this.payslipForm.get("EmployeeType")?.value
    this.url += "&Employee=" + (this.payslipForm.get("EmployeeCode")?.value || '0')
    this.url += "&Lang=" + this.payslipForm.get("LanguageType")?.value
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url);
  }
  getTimeSheetReportClick(): void {
    this.url = environment.baseReportUrl;
    this.url += 'Payroll/TimeSheetReport.aspx?';
    this.url += "LoginID=" + this.currentUser;
    this.url += "&Branch=" + this.payslipForm.get("BranchCode")?.value
    this.url += "&Period=" + this.dtAdvanceDate
    this.url += "&EmployeeType=" + this.payslipForm.get("EmployeeType")?.value
    this.url += "&Employee=" + (this.payslipForm.get("EmployeeCode")?.value || '0')
    this.url += "&Lang=" + this.payslipForm.get("LanguageType")?.value
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url);
  }
  getPaySlipForGuard1ReportClick(): void {
    this.url = environment.baseReportUrl;
    this.url += 'Payroll/PaySlipForGuard1Report.aspx?';
    this.url += "LoginID=" + this.currentUser;
    this.url += "&Branch=" + this.payslipForm.get("BranchCode")?.value
    this.url += "&Period=" + this.dtAdvanceDate
    this.url += "&EmployeeType=" + this.payslipForm.get("EmployeeType")?.value
    this.url += "&Employee=" + (this.payslipForm.get("EmployeeCode")?.value || '0')
    this.url += "&Lang=" + this.payslipForm.get("LanguageType")?.value
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url);
  }
  getPaySlipForGuard2ReportClick(): void {
    this.url = environment.baseReportUrl;
    this.url += 'Payroll/PaySlipForGuard2Report.aspx?';
    this.url += "LoginID=" + this.currentUser;
    this.url += "&Branch=" + this.payslipForm.get("BranchCode")?.value
    this.url += "&Period=" + this.dtAdvanceDate
    this.url += "&EmployeeType=" + this.payslipForm.get("EmployeeType")?.value
    this.url += "&Employee=" + (this.payslipForm.get("EmployeeCode")?.value || '0')
    this.url += "&Lang=" + this.payslipForm.get("LanguageType")?.value
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url);
  }
  getPaySlipRBAReportClick(): void {
    this.url = environment.baseReportUrl;
    this.url += 'Payroll/PaySlipRBAReport.aspx?';
    this.url += "LoginID=" + this.currentUser;
    this.url += "&Branch=" + this.payslipForm.get("BranchCode")?.value
    this.url += "&Period=" + this.dtAdvanceDate
    this.url += "&EmployeeType=" + this.payslipForm.get("EmployeeType")?.value
    this.url += "&Employee=" + (this.payslipForm.get("EmployeeCode")?.value || '0')
    this.url += "&Lang=" + this.payslipForm.get("LanguageType")?.value
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url);
  }
  handleErrors(error: string) {
    if (error != null && error != '') {
      this.hideSpinner();
    }
  };
  hideSpinner() {
    this.showLoadingSpinner = false;
  }
}
