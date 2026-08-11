import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { MatDatepickerInputEvent } from '@angular/material/datepicker';
import { SafeResourceUrl, DomSanitizer } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { debounceTime, forkJoin, Observable, Subject } from 'rxjs';
import { EmployeeSosco } from 'src/app/model/EmployeeSosco';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { PayrollModuleService } from 'src/app/service/payrollmodule.service';
import { environment } from 'src/environments/environment';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-socso-statement-report',
  templateUrl: './socso-statement-report.component.html',
  styleUrls: ['./socso-statement-report.component.css']
})
export class SocsoStatementReportComponent implements OnInit {
  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = "PayRoll/"
  reportPageName: string = "";
  frm!: FormGroup;
  branchList: any = [];
  currentUser: string = "";
  errorMessage: string = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;
  dtAdvanceDate!: string;
  employeeSOCSOArray: EmployeeSosco[] = [];
  reportType!: number;
  branchSearchSubject = new Subject<string>();
  branchSearchString: string = '';
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

  constructor(public sanitizer: DomSanitizer, private _masterService: MastermoduleService, private fb: FormBuilder,
    private _dataService: DatasharingService, private router: Router, private _payrollService: PayrollModuleService
  ) {
    this.currentUser = sessionStorage.getItem('username')!;
    this.url += this.currentUrl;

    this.frm = fb.group({
      Branch: [""],
      Period: ["", Validators.required],
      EmployeeType: ["All"],
      EmployeeCode: [''],
      EmpTempType: ['8']
    })
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }
  }

  ngOnInit(): void {
    // Branch search debounce
    this.branchSearchSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.branchSearchString = '';
      this.branchList = [...this.filteredBranchList];
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
            this._masterService.GetBranchListByUserName(this.currentUser).subscribe((d: any) => {
              this.branchList = d;
              this.filteredBranchList = [...this.branchList];
            });
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
    this.frm.value.Period = this.formatDate(`${type}: ${event.value}`);
    let dtAdvanceDate = new Date(this.frm.value.Period);
    this.dtAdvanceDate = this.formatDate(
      new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
    );
  }
  onBranchSelectionChange(event: any) {
    let dtAdvanceDate = new Date(this.frm.value.Period);
    this.dtAdvanceDate = this.formatDate(
      new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
    );
  }
  returnDate(date?: any) {
    let currentDate = new Date();
    if (date) {
      currentDate = new Date(date);
    }

    const year = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0'); // Month is zero-based
    const day = String(currentDate.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
  generateExcelFileClick() {
    let dtAdvanceDate = new Date(this.frm.value.Period);
    this.dtAdvanceDate = this.formatDate(
      new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
    );
    if (this.dtAdvanceDate != null && this.dtAdvanceDate != 'NaN-NaN-NaN') {
      const branchCode = this.frm.get('Branch')?.value ?? '';
      this.getEPFToExcelGeneration(this.dtAdvanceDate, branchCode);
    }
  }

  private mapSocsoItem(item: any): EmployeeSosco {
    const employee = new EmployeeSosco();
    employee.EmployeeName   = item.EmployeeName  || '';
    employee.EMPICNO        = item.EMPICNO        || '';
    employee.EMPJoinDate    = new Date(item.EMPJoinDate) || new Date();
    employee.SOCSONO        = item.SOCSONO        || '';
    employee.Salary         = item.Salary         || 0;
    employee.SOCSOEmployee  = item.SOCSOEmployee  || 0;
    employee.SOCSOEmployer  = item.SOCSOEmployer  || 0;
    employee.SKBBK          = item.SKBBK          || 0;
    return employee;
  }

  getEPFToExcelGeneration(dtSalaryPeriod: string, branch: string): void {
    // Reset array before populating
    this.employeeSOCSOArray = [];

    if (branch && branch !== '' && branch !== '0') {
      // Single branch
      this._payrollService.getSOCSOToExcel(dtSalaryPeriod, branch)
        .subscribe({
          next: (data: any[]) => {
            this.employeeSOCSOArray = data.map(item => this.mapSocsoItem(item));
            this.exportToExcel();
          },
          error: (err) => { this.errorMessage = 'Error fetching data: ' + err.message; }
        });
    } else {
      // No branch selected — fetch all branches and merge results
      const branches: string[] = this.filteredBranchList.map((b: any) => b.Code);
      if (branches.length === 0) {
        console.warn('No branches available to generate Excel file.');
        return;
      }

      const requests: Observable<any[]>[] = branches.map((branchCode: string) =>
        this._payrollService.getSOCSOToExcel(dtSalaryPeriod, branchCode)
      );

      forkJoin(requests).subscribe({
        next: (results: any[][]) => {
          this.employeeSOCSOArray = ([] as any[])
            .concat(...results)
            .map(item => this.mapSocsoItem(item));
          this.exportToExcel();
        },
        error: (err) => { this.errorMessage = 'Error fetching data for all branches: ' + err.message; }
      });
    }
  }
  exportToExcel() {
    // Format date for filename
    const formattedDate = new Date(this.dtAdvanceDate).toISOString().split('T')[0];
    const fileName = `${environment.PayTypeSOCSO.toUpperCase()}_${formattedDate}.xlsx`;
    const worksheet: XLSX.WorkSheet = XLSX.utils.json_to_sheet(this.employeeSOCSOArray);
    const workbook: XLSX.WorkBook = { Sheets: { data: worksheet }, SheetNames: ['data'] };
    XLSX.writeFile(workbook, fileName);
  }
  onSubmit() {
    let localURL = "";
    if (this.frm.invalid) {
      return;
    }
    const branchCode = this.frm.get("Branch")?.value != undefined ? this.frm.get("Branch")?.value : ''
    if (this.reportType != 2 && this.reportType != 3) {
      // For SSRS report viewer, map "All" back to "Guard" as the report doesn't support "All"
      const empTypeForReport = this.frm.get("EmployeeType")?.value === 'All' ? 'Guard' : this.frm.get("EmployeeType")?.value;
      localURL += "Branch=" + branchCode
      localURL += "&Period=" + this.dtAdvanceDate
      localURL += "&EmployeeType=" + empTypeForReport
      localURL += "&LoginID=" + this.currentUser
      this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + this.reportPageName + localURL);
    } else if (this.reportType == 2) {
      let dtAdvanceDate = new Date(this.frm.value.Period);
      this.dtAdvanceDate = this.formatDate(
        new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
      );
      const branchCode = this.frm.get('Branch')?.value;
      const employeeType = this.frm.get('EmployeeType')?.value;
      const empTempType = this.frm.get('EmpTempType')?.value;
      this.generateSocsoToTextFile(branchCode, this.dtAdvanceDate, employeeType, empTempType);
    } else if (this.reportType == 3) {
      this.generateExcelFileClick();
    }
  }

  clkBtn(number: number) {
    this.reportType = number === 1 ? 1 : number === 2 ? 2 : 3;
    this.reportPageName = "SOCSOReport.aspx?"
  }
  // Generate Socso to Text File
  // Returns SOCSO employer code based on branch
  private getSocsoCompanyCodeByBranch(branch: string): string {
    const branchSocsoMap: { [key: string]: string } = {
      'FWG002-PCH': 'B3502056993B',
      'FWG003-JB':  'E1102071173P',
      'FWG012-KDH': 'C5202057292M',
    };
    return branchSocsoMap[branch] ?? environment.SocsoCompanyCode; // D4100019020Z for all others
  }

  generateSocsoToTextFile(branch: string, period: string, employeeType: string, empTempType: string): void {
    const companyRegNumber = environment.CompanyRegNumber;
    const fileName = `${environment.PayTypeSOCSO}_${environment.PayTypeSOCSO}_${period.replace(/-/g, '')}_${Date.now()}_${this.currentUser}.txt`;

    if (branch && branch !== '' && branch !== '0') {
      // Single branch — existing behaviour
      const socsoCompanyCode = this.getSocsoCompanyCodeByBranch(branch);
      this._payrollService.getSocsoToCIMBList(companyRegNumber, socsoCompanyCode, branch, period, employeeType, empTempType)
        .subscribe({
          next: (data) => this.downloadFile(data, fileName),
          error: (error) => console.error('Error generating Socso text file:', error)
        });
    } else {
      // No branch selected — call API for every branch using its own SOCSO code, then merge
      const branches: string[] = this.filteredBranchList.map((b: any) => b.Code);
      if (branches.length === 0) {
        console.warn('No branches available to generate file.');
        return;
      }

      const requests: Observable<string[]>[] = branches.map((branchCode: string) => {
        const socsoCompanyCode = this.getSocsoCompanyCodeByBranch(branchCode);
        return this._payrollService.getSocsoToCIMBList(companyRegNumber, socsoCompanyCode, branchCode, period, employeeType, empTempType);
      });

      forkJoin(requests).subscribe({
        next: (results: string[][]) => {
          // Flatten all branch results into one list
          const allLines: string[] = ([] as string[]).concat(...results);
          this.downloadFile(allLines, fileName);
        },
        error: (error) => console.error('Error generating Socso text file for all branches:', error)
      });
    }
  }
  searchDropdown(searchString: string, list: any[], key: string): any[] {
    if (!searchString) return [...list]; // if empty, return full list
    return list.filter(item => item[key].toLowerCase().includes(searchString.toLowerCase()));
  }

  onKeyDropdown(
    event: KeyboardEvent,
    searchStringProp: 'branchSearchString',
    listProp: 'branchList',
    filteredListProp: 'filteredBranchList',
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
  // Utility method to download a text file (Windows CRLF line endings)
  private downloadFile(data: string[], fileName: string): void {
    const blob = new Blob([data.join('\r\n')], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    window.URL.revokeObjectURL(url);
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
