import { Component, OnInit, Renderer2 } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { MatDatepickerInputEvent } from '@angular/material/datepicker';
import { SafeResourceUrl, DomSanitizer } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { debounceTime, forkJoin, Subject } from 'rxjs';
import { BankListModel } from 'src/app/model/bankListModel';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { CommonService } from 'src/app/service/common.service';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { PayrollModuleService } from 'src/app/service/payrollmodule.service';
import { environment } from 'src/environments/environment';
import Swal from 'sweetalert2';
import * as XLSX from 'xlsx';
import * as FileSaver from 'file-saver';

@Component({
  selector: 'app-bank-salary-statement-report',
  templateUrl: './bank-salary-statement-report.component.html',
  styleUrls: ['./bank-salary-statement-report.component.css']
})
export class BankSalaryStatementReportComponent implements OnInit {
  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = "PayRoll/"
  reportPageName: string = "";
  frm!: FormGroup;
  branchList: any = [];
  bankList!: BankListModel[];
  currentUser: string = "";
  userRole: string = '';
  errorMessage: string = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;
  staffAdminAccessModel!: UserAccessModel;
  dtAdvanceDate!: string;
  reportType!: any;
  dtPeriod!: string;
  salaryAdvanceList: string[] = [];
  salaryAdvanceTotalList: string[] = [];
  salaryAdvanceHashTotalList: string[] = [];
  salaryList: any[] = [];
  branchSearchSubject = new Subject<string>();
  branchSearchString: string = '';
  filteredBranchList: any[] = [];
  bankSearchSubject = new Subject<string>();
  bankSearchString: string = '';
  filteredBankList: any[] = [];

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
    private _dataService: DatasharingService, private _commonService: CommonService, private router: Router,
    private _payrollService: PayrollModuleService
  ) {
    this.currentUser = sessionStorage.getItem('username')!;
    const rawRole = sessionStorage.getItem('userrole');
    if (rawRole == '1') { this.userRole = 'admin'; }
    else if (rawRole == '2') { this.userRole = 'superadmin'; }
    else { this.userRole = 'user'; }
    this.url += this.currentUrl;

    this.frm = fb.group({
      Branch: ["0"],
      BankCode: ['0'],
      Period: ["", Validators.required],
      EmployeeType: ["Guard", Validators.required],
      EmpTempType: ['8'],
      PaymentType: ['Salary'],
      PaymentMode: ['Bank']
    })
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }
    this.staffAdminAccessModel = {
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

    // Bank search debounce
    this.bankSearchSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.bankSearchString = '';
      this.bankList = [...this.filteredBankList];
    });
    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this._dataService.scrollToTop(); // Scroll to top on route change
      }
    });
    this.currentUser = sessionStorage.getItem('username')!;
    const rawRole = sessionStorage.getItem('userrole');
    if (rawRole == '1') { this.userRole = 'admin'; }
    else if (rawRole == '2') { this.userRole = 'superadmin'; }
    else { this.userRole = 'user'; }
    if (this.currentUser == null || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Pay Slip Report');
  }
  getUserAccessRights(userName: string, screenName: string) {
    this.showLoadingSpinner = true;
    forkJoin({
      mainAccess: this._masterService.getUserAccessRights(userName, screenName),
      staffAdminAccess: this._masterService.getUserAccessRights(userName, 'Payroll Staff Admin')
    }).subscribe({
      next: (res: any) => {
        if (res.mainAccess) {
          this.userAccessModel = {
            readAccess: res.mainAccess.Read,
            updateAccess: res.mainAccess.Update,
            deleteAccess: res.mainAccess.Delete,
            createAccess: res.mainAccess.Create
          };
        }
        if (res.staffAdminAccess) {
          this.staffAdminAccessModel = {
            readAccess: res.staffAdminAccess.Read,
            updateAccess: res.staffAdminAccess.Update,
            deleteAccess: res.staffAdminAccess.Delete,
            createAccess: res.staffAdminAccess.Create
          };
        }
        if (this.userAccessModel.readAccess === true || this.userRole == 'superadmin') {
          this.warningMessage = '';
          this._masterService.GetBranchListByUserName(this.currentUser).subscribe((d: any) => {
            this.branchList = d;
            this.filteredBranchList = [...this.branchList];
          });
          this._commonService.getUserBankList(this.currentUser).subscribe(bankList => {
            this.bankList = bankList;
            this.filteredBankList = [...this.bankList];
          });
        } else {
          this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
                    You do not have permissions to view this page. <br>
                    If you feel you should have access to this page, Please contact administrator. <br>
                    Thank you`;
        }
        this.hideSpinner();
      },
      error: (error) => { this.handleErrors(error); }
    });
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
  onPaymentModeChange() {
    if (this.frm.get('PaymentMode')?.value === 'Cash') {
      this.frm.patchValue({ BankCode: '0' });
    }
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
  onSubmit() {
    let localURL = "";
    if (this.frm.invalid) {
      return;
    }
    if (this.reportType == 1) {
      localURL += "Branch=" + this.frm.get("Branch")?.value
      localURL += "&LoginID=" + this.currentUser
      localURL += "&Period=" + this.dtAdvanceDate
      localURL += "&EmployeeType=" + this.frm.get("EmployeeType")?.value
      localURL += "&Bank=" + this.frm.get("BankCode")?.value
      localURL += "&PaymentMode=" + this.frm.get("PaymentMode")?.value
      this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + this.reportPageName + localURL);
    } else if (this.reportType == 2) {
      localURL += "Branch=" + this.frm.get("Branch")?.value
      localURL += "&LoginID=" + this.currentUser
      localURL += "&Period=" + this.dtAdvanceDate
      localURL += "&EmployeeType=" + this.frm.get("EmployeeType")?.value
      localURL += "&Bank=" + this.frm.get("BankCode")?.value
      localURL += "&PaymentMode=" + this.frm.get("PaymentMode")?.value
      this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + this.reportPageName + localURL);
    } else if (this.reportType == 3) {
      localURL += "Branch=" + this.frm.get("Branch")?.value
      localURL += "&LoginID=" + this.currentUser
      localURL += "&Period=" + this.dtAdvanceDate
      localURL += "&EmployeeType=" + this.frm.get("EmployeeType")?.value
      localURL += "&Bank=" + this.frm.get("BankCode")?.value
      localURL += "&PaymentMode=" + this.frm.get("PaymentMode")?.value
<<<<<<< HEAD
      this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + this.reportPageName + localURL);
    } else if (this.reportType == 7) {
      localURL += "Branch=" + this.frm.get("Branch")?.value
      localURL += "&LoginID=" + this.currentUser
      localURL += "&Period=" + this.dtAdvanceDate
      localURL += "&EmployeeType=" + this.frm.get("EmployeeType")?.value
      localURL += "&Bank=" + this.frm.get("BankCode")?.value
      localURL += "&PaymentMode=" + this.frm.get("PaymentMode")?.value
      this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + this.reportPageName + localURL);
    } else if (this.reportType == 8) {
      localURL += "Branch=" + this.frm.get("Branch")?.value
      localURL += "&LoginID=" + this.currentUser
      localURL += "&Period=" + this.dtAdvanceDate
      localURL += "&EmployeeType=" + this.frm.get("EmployeeType")?.value
      localURL += "&Bank=" + this.frm.get("BankCode")?.value
      localURL += "&PaymentMode=" + this.frm.get("PaymentMode")?.value
=======
>>>>>>> dadc643 (Updated payslip changes)
      this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + this.reportPageName + localURL);
    } else if (this.reportType == undefined) {

    }
    else {
      this.generateDataToTextFile(this.reportType);
    }
  }

  clkBtn(number: number) {
    this.reportType = number === 1 ? 1 : number === 2 ? 2 : number === 3 ? 3 : number === 7 ? 7 : number === 8 ? 8 :
      number === 4 ? 'StandardSalary' : number === 5 ? 'SalaryGuard1' : 'SalaryGuard2';
    this.reportPageName = number == 1 ? "BankSalaryReport.aspx?" : number == 2 ? 'BankSalaryReportGuard1.aspx?' :
      number == 3 ? 'BankSalaryReportGuard2.aspx?' : number == 7 ? 'Bank1SalaryReport.aspx?' : number == 8 ? 'Bank2SalaryReport.aspx?' :  '';
  }

  generateDataToTextFile(source: string): void {
    try {
      const paymentMode = this.frm.get('PaymentMode')?.value || 'Bank';

      // Text file (bank upload format) is only applicable for Bank payment mode
      if (paymentMode === 'Cash') {
        this.showMessage('Text file generation is only available for Bank payment mode. Please use Generate Excel File for Cash employees.', 'warning');
        return;
      }

      let dtAdvanceDate = new Date(this.frm.value.Period);
      this.dtPeriod = this.formatDate(
        new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
      );
      const branchCode = this.frm.get('Branch')?.value == '' ? '0' : this.frm.get('Branch')?.value;
      const employeeType = this.frm.get('EmployeeType')?.value;
      const bank = this.frm.get('BankCode')?.value == '' ? '0' : this.frm.get('BankCode')?.value;
      const payType = this.frm.get('PaymentType')?.value || 'Salary';
      const companyCode = environment.CompanyCode.toUpperCase().trim(); // Replace with actual environment variable service
      const currentDateTime = new Date();

      // Construct the file name
      const fileName = `${companyCode}_${payType}_${this.formatdDate(dtAdvanceDate, 'ddMMyyyy')}_${this.formatdDate(currentDateTime, 'ddMMyyyyhhMMss')}_${this.currentUser}`; // Replace `userService` with actual user identity service
      const filePath = `C:/temp/${fileName}.txt`;

      // Fetch data based on branch selection
      // if (branchCode !== '0') {
      //   this._payrollService
      //     .getEmployeeSalaryAdvanceListWithBranch(branchCode, this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
      //     .subscribe((data) => {
      //       setTimeout(() => {
      //         this.salaryAdvanceList = data;
      //       });

      //       this._payrollService
      //         .getEmployeeSalaryAdvanceTotalListWithBranch(branchCode, this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
      //         .subscribe((totalData) => {
      //           setTimeout(() => {
      //             this.salaryAdvanceTotalList = totalData;
      //           });

      //           this._payrollService
      //             .getEmployeeSalaryAdvanceHashTotalListWithBranch(branchCode, this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
      //             .subscribe((hashData) => {
      //               setTimeout(() => {
      //                 this.salaryAdvanceHashTotalList = hashData;
      //               });
      //             });
      //         });
      //     });
      // } else {
      //   this._payrollService
      //     .getEmployeeSalaryAdvanceList(this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
      //     .subscribe((data) => {
      //       setTimeout(() => {
      //         this.salaryAdvanceList = data;
      //       });
      //       this._payrollService
      //         .getEmployeeSalaryAdvanceTotalList(this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
      //         .subscribe((totalData) => {
      //           setTimeout(() => {
      //             this.salaryAdvanceTotalList = totalData;
      //           });
      //           this._payrollService
      //             .getEmployeeSalaryAdvanceHashTotalList(this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
      //             .subscribe((hashData) => {
      //               setTimeout(() => {
      //                 this.salaryAdvanceHashTotalList = hashData;
      //               });
      //             });
      //         });
      //     });
      // }      

      // if (this.salaryAdvanceList.length > 0) {
      //   let fileContent = '';

      //   // Header record
      //   if (bank === 'CIMB') {
      //     fileContent += this.generateHeaderRecordCIMB() + '\n';
      //   } else if (bank.toUpperCase() === 'RHB') {
      //     fileContent += 'header0000\n';
      //   } else if (bank.toUpperCase() === 'BSN') {
      //     fileContent += this.generateHeaderRecordBSN() + '\n';
      //   } else {
      //     fileContent += this.generateHeaderRecordCIMB() + '\n';
      //   }

      //   // Detail records
      //   this.salaryAdvanceList.forEach((detail) => {
      //     if (bank.toUpperCase() === 'CIMB') {
      //       fileContent += this.generateDetailRecordCIMB(detail) + '\n';
      //     } else {
      //       fileContent += detail + '\n';
      //     }
      //   });

      //   // Trailer records
      //   this.salaryAdvanceTotalList.forEach((total) => {
      //     if (bank.toUpperCase() === 'CIMB') {
      //       fileContent += this.generateTrailerRecordCIMB(total) + '\n';
      //     } else if (bank.toUpperCase() === 'RHB') {
      //       fileContent += 'Footer000000000\n';
      //     } else if (bank.toUpperCase() === 'BSN') {
      //       fileContent += this.generateTrailerRecordBSN(total, this.salaryAdvanceHashTotalList) + '\n';
      //     } else {
      //       fileContent += this.generateTrailerRecordCIMB(total) + '\n';
      //     }
      //   });

      //   // Write content to file and download
      //   this.downloadFile(fileContent, fileName);

      //   this.showMessage(`Text File Successfully Generated to ${filePath.toUpperCase().trim()}`, 'success');
      // } else {
      //   this.showMessage('NO RECORD FOUND!', 'warning');
      // }

      forkJoin({
        salaryAdvanceList: branchCode !== '0'
          ? this._payrollService.getEmployeeSalaryAdvanceListWithBranch(branchCode, this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
          : this._payrollService.getEmployeeSalaryAdvanceList(this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source),
        salaryAdvanceTotalList: branchCode !== '0'
          ? this._payrollService.getEmployeeSalaryAdvanceTotalListWithBranch(branchCode, this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
          : this._payrollService.getEmployeeSalaryAdvanceTotalList(this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source),
        salaryAdvanceHashTotalList: branchCode !== '0'
          ? this._payrollService.getEmployeeSalaryAdvanceHashTotalListWithBranch(branchCode, this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
          : this._payrollService.getEmployeeSalaryAdvanceHashTotalList(this.dtPeriod, employeeType, bank.toUpperCase(), payType, companyCode, source)
      }).subscribe(
        ({ salaryAdvanceList, salaryAdvanceTotalList, salaryAdvanceHashTotalList }) => {
          // Assigning values
          this.salaryAdvanceList = salaryAdvanceList || [];
          this.salaryAdvanceTotalList = salaryAdvanceTotalList || [];
          this.salaryAdvanceHashTotalList = salaryAdvanceHashTotalList || [];

          // Check for data availability
          if (this.salaryAdvanceList.length > 0) {
            this.generateAndDownloadFile(bank, fileName, filePath);
          } else {
            this.showMessage('NO RECORD FOUND!', 'warning');
          }
        },
        (error) => {
          this.showMessage(`Error retrieving data: ${error.message}`, 'error');
        }
      );
    } catch (error) {
      this.showMessage(`Error Found: ${error}`);
    }
  }
  generateExcelFileClick() {
    let dtAdvanceDate = new Date(this.frm.value.Period);
    this.dtPeriod = this.formatDate(
      new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
    );
    const branch = this.frm.get('Branch')?.value == '' ? '0' : this.frm.get('Branch')?.value;
    const client = ''
    const employeeType = this.frm.get('EmployeeType')?.value;
    const empTempType = this.frm.get('EmpTempType')?.value;
    const bank = this.frm.get('BankCode')?.value == '' ? '0' : this.frm.get('BankCode')?.value;
    const paymentType = this.frm.get('PaymentType')?.value;
    const paymentMode = this.frm.get('PaymentMode')?.value || 'Bank';
    const source = '';
    forkJoin({
      salaryList: this._payrollService.getSalaryList(this.dtPeriod, branch, client, bank, paymentType, employeeType, empTempType, source, paymentMode),

    }).subscribe({
      next: (results) => {
        this.salaryList = results.salaryList;
        this.downloadExcel();
      },
      error: (err) => {
        console.error('Error loading data', err);
      }
    });

  }

  // ─── Cash employees: RHB Smart Payroll Excel format ───────────────────────
  generateCashExcelFileClick() {
    let dtAdvanceDate = new Date(this.frm.value.Period);
    this.dtPeriod = this.formatDate(
      new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
    );
    const branch = this.frm.get('Branch')?.value == '' ? '0' : this.frm.get('Branch')?.value;
    const employeeType = this.frm.get('EmployeeType')?.value;
    const empTempType = this.frm.get('EmpTempType')?.value;
    const paymentType = this.frm.get('PaymentType')?.value;
    const source = '';

    forkJoin({
      salaryList: this._payrollService.getSalaryList(
        this.dtPeriod, branch, '', '0', paymentType, employeeType, empTempType, source, 'Cash'
      )
    }).subscribe({
      next: (results) => {
        this.salaryList = results.salaryList;
        this.downloadCashExcel();
      },
      error: (err) => {
        console.error('Error loading cash salary data', err);
        this.showMessage('Error loading cash salary data: ' + err.message, 'error');
      }
    });
  }

  downloadCashExcel(): void {
    if (!this.salaryList || this.salaryList.length === 0) {
      this.showMessage('No cash salary data found.', 'warning');
      return;
    }

    // RHB Smart Payroll Format — columns A to M, No Header & Footer
    // Col A: Record Type       — 20 (Rentas)
    // Col B: Trans Record No   — sequential 1-based
    // Col C: Crediting Amount  — net salary (2 decimal)
    // Col D: Receiving Member Bank (BIC) — blank for Rentas same-bank
    // Col E: RENTAS Trx Code   — SALA
    // Col F: BOP Indicator     — 1 (Resident to Resident)
    // Col G: Beneficiary Name
    // Col H: Beneficiary Acct No (no dashes, no apostrophe)
    // Col I: I/D Number (no dashes, no apostrophe)
    // Col J: Address 1 (blank — not in DB)
    // Col K: Address 2 (blank)
    // Col L: Address 3 (blank)
    // Col M: ID Indicator      — 01=New IC, 02=Old IC, 03=Passport

    let rowNum = 1;
    const rows = this.salaryList.map((item: any) => {
      const acctNo = item.AccountNo
        ? item.AccountNo.replace(/'/g, '').replace(/-/g, '').trim()
        : '';
      const idNo = item.SecurityNo
        ? item.SecurityNo.replace(/'/g, '').replace(/-/g, '').trim()
        : '';
      const amount = parseFloat(item.Salary) || 0;
      // IcType: "01"=New IC, "02"=Old IC, "03"=Passport — from API
      const idIndicator = item.IcType || '01';

      return {
        'A': 20,              // Record Type: Rentas
        'B': rowNum++,        // Transaction Record Number (sequential)
        'C': amount,          // Crediting Amount
        'D': '',              // Receiving Member Bank (BIC) — blank for RENTAS
        'E': 'SALA',          // RENTAS Transaction Code
        'F': 1,               // BOP Indicator: Resident to Resident
        'G': item.Name || '', // Beneficiary Name
        'H': acctNo,          // Beneficiary Account Number
        'I': idNo,            // I/D Number
        'J': '',              // Address 1
        'K': '',              // Address 2
        'L': '',              // Address 3
        'M': idIndicator      // ID Indicator
      };
    });

    // Build worksheet — skipHeader:true (no header row per spec: "No Header & Footer")
    const worksheet: XLSX.WorkSheet = XLSX.utils.json_to_sheet(rows, { skipHeader: true });

    // Column widths
    worksheet['!cols'] = [
      { wch: 6 },   // A: Record Type
      { wch: 8 },   // B: Trans Record No
      { wch: 14 },  // C: Amount
      { wch: 12 },  // D: BIC
      { wch: 8 },   // E: RENTAS code
      { wch: 5 },   // F: BOP
      { wch: 40 },  // G: Name
      { wch: 20 },  // H: Account No
      { wch: 16 },  // I: IC/Passport
      { wch: 30 },  // J: Address 1
      { wch: 30 },  // K: Address 2
      { wch: 30 },  // L: Address 3
      { wch: 5 },   // M: ID Indicator
    ];

    // Force text format on key columns (account no, IC, ID indicator)
    const textCols = ['H', 'I', 'M'];
    for (let r = 0; r < rows.length; r++) {
      textCols.forEach(col => {
        const ref = `${col}${r + 1}`;
        if (worksheet[ref]) {
          worksheet[ref].t = 's';
          worksheet[ref].z = '@';
        }
      });
    }

    const workbook: XLSX.WorkBook = { Sheets: { Sheet1: worksheet }, SheetNames: ['Sheet1'] };
    const excelBuffer: any = XLSX.write(workbook, { bookType: 'xlsx', type: 'array', cellStyles: true });

    // Filename
    const safePeriod = this.dtPeriod.replace(/[\/,:]/g, '_');
    const now = new Date();
    let hours = now.getHours();
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const seconds = String(now.getSeconds()).padStart(2, '0');
    const ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12 || 12;
    const currentTime = `${String(hours).padStart(2, '0')}_${minutes}_${seconds}_${ampm}`;
    const fileName = `Cash_SmartPayroll_${safePeriod} ${currentTime}.xlsx`;

    const blob = new Blob([excelBuffer], { type: 'application/vnd.ms-excel' });
    FileSaver.saveAs(blob, fileName);
    this.showMessage(`Cash Excel (Smart Payroll Format) generated: ${fileName}`, 'success');
  }
  generateAndDownloadFile(bank: string, fileName: string, filePath: string): void {
    let fileContent = '';
    const CRLF = '\r\n';
    // Normalise: treat '0' / empty as CIMB (default bank for FWG)
    const bankUpper = (bank === '0' || bank === '') ? 'CIMB' : bank.toUpperCase();

    // Header record
    if (bankUpper === 'CIMB') {
      fileContent += this.generateHeaderRecordCIMB() + CRLF;
    } else if (bankUpper === 'RHB') {
      fileContent += 'header0000' + CRLF;
    } else if (bankUpper === 'BSN') {
      fileContent += this.generateHeaderRecordBSN() + CRLF;
    } else {
      fileContent += this.generateHeaderRecordCIMB() + CRLF;
    }

    // Detail records
    this.salaryAdvanceList.forEach((detail) => {
      if (bankUpper === 'CIMB') {
        fileContent += this.generateDetailRecordCIMB(detail) + CRLF;
      } else {
        fileContent += detail + CRLF;
      }
    });

    // Trailer records
    this.salaryAdvanceTotalList.forEach((total) => {
      if (bankUpper === 'CIMB') {
        fileContent += this.generateTrailerRecordCIMB(total) + CRLF;
      } else if (bankUpper === 'RHB') {
        fileContent += 'Footer000000000' + CRLF;
      } else if (bankUpper === 'BSN') {
        fileContent += this.generateTrailerRecordBSN(total, this.salaryAdvanceHashTotalList) + CRLF;
      } else {
        fileContent += this.generateTrailerRecordCIMB(total) + CRLF;
      }
    });

    // Write content to file and download
    this.downloadFile(fileContent, fileName);

    // Notify success
    this.showMessage(`Text File Successfully Generated to ${filePath.toUpperCase().trim()}`, 'success');
  }
  downloadExcel(): void {
    if (!this.salaryList || this.salaryList.length === 0) {
      console.warn('No data to export');
      this.showMessage(`No data to export`, 'warning')
      return;
    }

    // Re-map & sanitize data
    const orderedData = this.salaryList.map((item: any) => ({
      Name: item.Name,
      Extra1: "",
      AccountNo: item.AccountNo ? item.AccountNo.replace(/'/g, '') : '',
      Amount: parseFloat(item.Salary) || 0,   // ensure numeric so SUM formula works
      Extra2: "",
      SecurityNo: item.SecurityNo ? item.SecurityNo.replace(/'/g, '').trim() : ''
    }));

    // Convert JSON to worksheet
    const worksheet: XLSX.WorkSheet = XLSX.utils.json_to_sheet(orderedData, {
      skipHeader: true
    });

    // Amount column is column D (index 3, 0-based)
    // Data rows: 1..orderedData.length (1-based Excel rows, since skipHeader:true means row 1 = first data row)
    const dataRowCount = orderedData.length;
    const amountColLetter = 'D'; // Amount is 4th column
    const sumFirstRow = 1;       // first data row in Excel (1-based)
    const sumLastRow = dataRowCount;
    const sumRowIndex = dataRowCount; // 0-based row index for the SUM row (appended after last data row)

    // Add SUM formula cell for Amount column (D) at the row after last data row
    const sumCellRef = `${amountColLetter}${sumLastRow + 1}`;
    worksheet[sumCellRef] = {
      t: 'n',
      f: `SUM(${amountColLetter}${sumFirstRow}:${amountColLetter}${sumLastRow})`,
      s: {
        font: { bold: true },
        border: {
          top: { style: 'thin', color: { rgb: '000000' } },
          bottom: { style: 'double', color: { rgb: '000000' } },
          left: { style: 'thin', color: { rgb: '000000' } },
          right: { style: 'thin', color: { rgb: '000000' } }
        }
      }
    };

    // Extend the worksheet range to include the SUM row
    const existingRange = XLSX.utils.decode_range(worksheet['!ref']!);
    existingRange.e.r = sumRowIndex; // extend to SUM row (0-based)
    worksheet['!ref'] = XLSX.utils.encode_range(existingRange);

    // Add borders to all data cells
    const range = XLSX.utils.decode_range(worksheet['!ref']!); // get range of cells
    for (let R = range.s.r; R <= range.e.r; ++R) {
      for (let C = range.s.c; C <= range.e.c; ++C) {
        const cellAddress = { r: R, c: C };
        const cellRef = XLSX.utils.encode_cell(cellAddress);
        if (!worksheet[cellRef]) continue;

        // Don't overwrite the SUM cell style
        if (cellRef === sumCellRef) continue;

        worksheet[cellRef].s = {
          border: {
            top: { style: 'thin', color: { rgb: '000000' } },
            bottom: { style: 'thin', color: { rgb: '000000' } },
            left: { style: 'thin', color: { rgb: '000000' } },
            right: { style: 'thin', color: { rgb: '000000' } }
          }
        };
      }
    }

    // Auto width for all columns
    const colWidths = Object.keys(orderedData[0]).map((key) => {
      const maxLength = Math.max(
        key.length,
        ...orderedData.map((d) => ((d as any)[key] ? (d as any)[key].toString().length : 0))
      );
      return { wch: maxLength + 2 }; // add some padding
    });
    worksheet['!cols'] = colWidths;

    // Workbook
    const workbook: XLSX.WorkBook = { Sheets: { data: worksheet }, SheetNames: ['data'] };

    // Generate buffer
    const excelBuffer: any = XLSX.write(workbook, { bookType: 'xlsx', type: 'array', cellStyles: true });

    // Filename
    const safePeriod = this.dtPeriod.replace(/[\/,:]/g, '_');
    const now = new Date();
    let hours = now.getHours();
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const seconds = String(now.getSeconds()).padStart(2, '0');
    const ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12 || 12;
    const currentTime = `${String(hours).padStart(2, '0')}_${minutes}_${seconds}_${ampm}`;

    const fileName = `${this.frm.get('PaymentType')?.value}_${this.frm.get('PaymentMode')?.value === 'Cash' ? 'Cash' : this.frm.get('BankCode')?.value}_${safePeriod} ${currentTime}.xlsx`;

    // Save file
    const data: Blob = new Blob([excelBuffer], { type: 'application/vnd.ms-excel' });
    FileSaver.saveAs(data, fileName);
  }

  searchDropdown(searchString: string, list: any[], key: string): any[] {
    if (!searchString) return [...list]; // if empty, return full list
    return list.filter(item => item[key].toLowerCase().includes(searchString.toLowerCase()));
  }

  onKeyDropdown(
    event: KeyboardEvent,
    searchStringProp: 'branchSearchString' | 'bankSearchString',
    listProp: 'branchList' | 'bankList',
    filteredListProp: 'filteredBranchList' | 'filteredBankList',
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

  private generateHeaderRecordCIMB(): string {
    // Fixed-width header: "01" + orgCode(5) + orgName(40) + date(8) + securityCode(16) + 2 trailing spaces = 73 chars
    const header = `01${environment.CIMB_OrganizationCode}${environment.CIMB_OrganizationName}${this.formatdDate(new Date(), 'ddMMyyyy')}${environment.CIMB_SecurityCode}`;
    return header.padEnd(73, ' ');
  }

  private generateHeaderRecordBSN(): string {
    return `SGB${environment.BSN_OrganizationCode}${environment.BSN_OrganizationName}${this.formatdDate(new Date(), 'ddMMyyyy')}0000000000000000000000000000000000000000000000000000`;
  }

  private generateDetailRecordCIMB(detail: string): string {
    // Fixed-width detail line: "02" + BNMCode(7) + detail fields = 147 chars total
    // detail from backend (trimmed): accNo(16) + name(40) + amount(11) + spaces(30) + secNo(20) + "2"(1) = 118 chars
    // Full line: 2 + 7 + 118 + 20 trailing spaces = 147
    const line = `02${environment.CIMB_BNMCode}${detail}`;
    return line.padEnd(147, ' ');
  }

  private generateTrailerRecordCIMB(total: string): string {
    // Trailer: "03" + totalRecord(6) + totalAmount(13) = 21 chars exactly
    return `03${total}`;
  }

  private generateTrailerRecordBSN(total: string, hashTotals: string[]): string {
    let record = `END${total}`;
    hashTotals.forEach((hash) => {
      record += hash;
    });
    record += `0000000000000000000000000000000000000000000000000000000000`;
    return record;
  }
  private formatdDate(date: Date, format: string): string {
    const options: any = {
      ddMMyyyy: `${date.getDate().toString().padStart(2, '0')}${(date.getMonth() + 1).toString().padStart(2, '0')}${date.getFullYear()}`,
      ddMMyyyyhhMMss: `${date.getDate().toString().padStart(2, '0')}${(date.getMonth() + 1).toString().padStart(2, '0')}${date.getFullYear()}${date.getHours().toString().padStart(2, '0')}${date.getMinutes().toString().padStart(2, '0')}${date.getSeconds().toString().padStart(2, '0')}`,
    };
    return options[format];
  }
  private showMessage(message: string, icon: 'success' | 'warning' | 'info' | 'error' = 'info'): void {
    Swal.fire({
      toast: true,
      position: 'top',
      showConfirmButton: false,
      title: icon,
      text: message,
      icon: icon, // Dynamically set the icon based on the parameter
      showCloseButton: false,
      timer: 5000,
      width: '600px',
      customClass: {
        popup: 'swal-top-offset'
      }
    });
  }

  private downloadFile(content: string, fileName: string): void {
    // Use CRLF line endings as required by CIMB bank portal
    const blob = new Blob([content], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `${fileName}.txt`;
    anchor.click();
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
