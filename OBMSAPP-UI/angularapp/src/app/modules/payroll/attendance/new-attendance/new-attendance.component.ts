import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDatepickerInputEvent } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatSelectChange } from '@angular/material/select';
import { Router } from '@angular/router';
import { catchError, debounceTime, forkJoin, Observable, of, Subject, switchMap } from 'rxjs';
import { DialogConfirmationComponent } from 'src/app/components/dialog-confirmation/dialog-confirmation.component';
import { AttendanceModel } from 'src/app/model/attendanceModel';
import { BranchModel } from 'src/app/model/branchModel';
import { ClientModel } from 'src/app/model/clientModel';
import { EmployeeMonthlyAdvance } from 'src/app/model/employeeMonthlyAdvance';
import { EmployeeAdvanceListModel } from 'src/app/model/empolyeeAdvanceListModel';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { AgreementService } from 'src/app/modules/quotation-and-agreement/agreement.service';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { PayrollModuleService } from 'src/app/service/payrollmodule.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-new-attendance',
  templateUrl: './new-attendance.component.html',
  styleUrls: ['./new-attendance.component.css']
})
export class NewAttendanceComponent implements OnInit {
  attendanceForm!: FormGroup;
  showLoadingSpinner: boolean = false;
  attendanceModel: AttendanceModel = new AttendanceModel();
  employeeModel!: ClientModel[];
  employeeListModel!: EmployeeAdvanceListModel[];
  // normalValues1: (string | number)[] = ['', ...Array.from({ length: 24 }, (_, i) => i + 1)];
  // normalValues2: (string | number)[] = ['', ...Array.from({ length: 24 }, (_, i) => i + 1)];
  // shift2Values1: (string | number)[] = ['', ...Array.from({ length: 24 }, (_, i) => i + 1)];
  // shift2Values2: (string | number)[] = ['', ...Array.from({ length: 24 }, (_, i) => i + 1)];

  normalValues1: number[] = Array.from({ length: 24 }, (_, i) => i + 1);
  normalValues2: number[] = Array.from({ length: 24 }, (_, i) => i + 1);
  shift2Values1: number[] = Array.from({ length: 24 }, (_, i) => i + 1);
  shift2Values2: number[] = Array.from({ length: 24 }, (_, i) => i + 1);


  dynamicForm!: FormGroup;
  minDate = new Date();
  advanceDateError!: string;
  dynamicEditable: string = '';
  selectedWorkType: string = '1';
  finalHours: number = 0;
  finalHoursshift2: number = 0;
  branchModel!: BranchModel[];
  clientList: any;
  attendanceID: number = 0;
  workType: string = '';
  workTypeId: number = 1;
  errorMessage: string = '';
  currentUser: string | null = '';
  warningMessage: string = '';
  userAccessModel!: UserAccessModel;
  shift2StartTime: number = 0;
  shift2EndTime: number = 0;
  startTime: number = 0;
  endTime: number = 0;
  dtAdvanceDate!: string;
  nameList: string[] = [];
  employeeSelectedType: string = 'Guard';
  StartPeriod!: string;
  EndPeriod!: string;
  attendancePeriod!: string;
  branchCode!: string;
  normalValue1Change!: string;
  normalValue2Change!: string;
  shiftValues1Change!: string;
  shiftValues2Change!: string;
  ClientName!: string;
  Shift2Client!: string;
  shift2StartTimeValidation!: number | null;;
  shift2EndTimeValidation!: number | null;;
  shift2HoursValidation!: number | null;;
  salaryProcessStatus: boolean = false; // Tracks if salary processing is done.
  temporaryEmployeeList: string[] = []; // Caches temporary employee codes for the current branch.
  dtAttendanceDate = new Date();
  userRole!: string;
  iAbsent: number = 0;
  iAnnualLeave: number = 0;
  iMedicalLeave: number = 0;
  iMaternityLeave: number = 0;
  iPaternityLeave: number = 0;
  iHospitalizationLeave: number = 0;
  attendanceDetails: any[] = [];
  showAllowance = false;
  showSpecialAllowance = false;

  employeeSearchSubject = new Subject<string>();
  branchSearchSubject = new Subject<string>();
  clientSearchSubject = new Subject<string>();
  employeeClientSearchSubject = new Subject<string>();

  employeeSearchString: string = '';
  branchSearchString: string = '';
  clientSearchString: string = '';
  employeeClientSearchString: string = '';

  filteredEmployeeList: any[] = [];
  filteredBranchList: any[] = [];
  filteredClientList: any[] = [];
  filteredEmployeeClientList: any[] = [];

  [key: string]: any;
  displayFullName = false;

  // Row-specific filtered lists for FormArray dropdowns
  filteredDropdownRows: { [key: string]: any[][] } = {};
  attendanceDetailsData: any[] = [];

  private formatDate(date: any) {
    const d = new Date(date);
    const year = d.getFullYear();
    let month = ('0' + (d.getMonth() + 1)).slice(-2);
    let day = ('0' + d.getDate()).slice(-2);
    let hours = ('0' + d.getHours()).slice(-2);
    let minutes = ('0' + d.getMinutes()).slice(-2);
    let seconds = ('0' + d.getSeconds()).slice(-2);
    //let milliseconds = ('00' + d.getMilliseconds()).slice(-3);

    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
  }

  private formatDisplayDate(date: any) {
    const d = new Date(date);
    let month = '' + (d.getMonth() + 1);
    let day = '' + d.getDate();
    const year = d.getFullYear();
    if (month.length < 2) month = '0' + month;
    if (day.length < 2) day = '0' + day;
    return [year, month, day].join('-');
  }

  workTypeList: any[] = [
    { id: 1, name: 'General Working' },
    { id: 2, name: 'Off Day' },
    { id: 3, name: 'Off Day Working' },
    { id: 4, name: 'Holiday' },
    { id: 5, name: 'Holiday Working' },
    { id: 6, name: 'Unpaid Leave' },
    { id: 7, name: 'Absent' },
    { id: 8, name: 'Annual Leave' },
    { id: 9, name: 'Medical Leave' },
    { id: 10, name: 'Maternity Leave' },
    { id: 11, name: 'Paternity Leave' },
    { id: 12, name: 'Hospitalization Leave' },
    { id: 13, name: 'Socso' },
    { id: 14, name: 'Non Schedule Off' },
    { id: 15, name: 'Replacement Leave' },
    { id: 16, name: 'Compensanate Leave' },
    { id: 17, name: 'Marriage Leave' },

  ]

  constructor(private fb: FormBuilder, private _payrollService: PayrollModuleService, public dialog: MatDialog,
    private _masterService: MastermoduleService, private _dataService: DatasharingService, public service: AgreementService,
    private _router: Router) {
    this.attendanceForm = this.fb.group({
      ID: [0],
      EmployeeID: [''],
      AdvanceDate: [new Date, [Validators.required]],
      ClientName: [''],
      BranchCode: ['', [Validators.required]],
      EmployeeNo: ['0', [Validators.required]],
      EmployeeType: ['Guard'],
      StartTime: [''],
      EndTime: [''],
      Hours: [''],
      Shif2Client: [''],
      Shift2StartTime: [''],
      Shift2EndTime: [''],
      Shift2Hours: [''],
      Passport: '',
      Age: '',
      JoinDate: '',
      ResignedDate: '',
      IncomeTax: '',
      EPF: '',
      EpfNo: '',
      Socso: '',
      PaymentMode: '',
      SalaryStructure: '',
      SalarySlab: '',
      BasicPay: '',
      Annual: '',
      Medical: '',
      Maternity: '',
      Paternity: '',
      Hospitalization: '',
      BonusAmount: 0.00,
      Shift2Type: '3',
      Shift2Rate: 0.00,
      Allowance: '',
      SpecialAllowance: '',
      AllowanceDeduction: 0.00,
      SpecialAllowanceDeduction: 0.00,
      AllowanceDeductionStaff: 0.00,
      SpecialAllowanceDeductionStaff: 0.00,
      LastUpdate: [this.formatDate(new Date)],
      LastUpdatedBy: [''],
      KPIDeduction: [0.00],
      Client: ['0']
    });
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }
    this.userRole = sessionStorage.getItem('userrole')!
    if (this.userRole == '1') {
      this.userRole = 'admin'
    } else if (this.userRole == '2') {
      this.userRole = 'superadmin'
    } else {
      this.userRole = 'user'
    }
  }

  ngOnInit(): void {
    this.createForm();

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

    // Client search debounce
    this.clientSearchSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.clientSearchString = '';
      this.clientList = [...this.filteredClientList];
    });

    // employee Client search debounce
    this.employeeClientSearchSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.employeeClientSearchString = '';
      this.employeeModel = [...this.filteredEmployeeClientList];
    });

    const formArray = this.dynamicForm.get('formArray') as FormArray;
    const formArrayLength = formArray?.length || 0;

    this.filteredDropdownRows['filteredEmployeeClientList'] = [];

    for (let i = 0; i < formArrayLength; i++) {
      // Pre-populate each row's filtered list with all employees
      this.filteredDropdownRows['filteredEmployeeClientList'][i] = [...this.employeeModel];
    }

    // Debounce for standalone dropdowns
    this.employeeClientSearchSubject.pipe(debounceTime(2000)).subscribe(() => {
      this.employeeClientSearchString = '';
      // reset row-based dropdowns
      for (let i = 0; i < formArrayLength; i++) {
        this.filteredDropdownRows['filteredEmployeeClientList'][i] = [...this.employeeModel];
      }
    });

    this.currentUser = sessionStorage.getItem('username')!;

    if (this.currentUser == null) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Employee Attendance');
    // ✅ BUG FIX: Removed duplicate createForm() call here.
    // It was called at the top of ngOnInit already. Calling it again wipes
    // the dynamicForm reference, orphaning the filteredDropdownRows init above.
  }

  initializeRowDropdown(filteredListProp: string) {

    const formArray = this.dynamicForm.get('formArray') as FormArray;

    if (!formArray || formArray.length === 0) return;

    if (!this.filteredDropdownRows[filteredListProp]) {
      this.filteredDropdownRows[filteredListProp] = [];
    }

    for (let i = 0; i < formArray.length; i++) {
      this.filteredDropdownRows[filteredListProp][i] = [...this.employeeModel];
    }
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
            this.hideloadingSpinner();
            this.getBranchMasterListByUser(this.currentUser!);
          } else {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
                      You do not have permissions to view this page. <br>
                      If you feel you should have access to this page, Please contact administrator. <br>
                      Thank you`;
            this.hideloadingSpinner();
          }
        }

      },
      (error) => {
        this.handleErrors(error);
      }
    );
  }

  changeAdvanceDate(type: string, event: MatDatepickerInputEvent<Date>) {
    this.clearFormFields();
    this.errorMessage = '';
    this.employeeSelectedType = this.attendanceForm.value.EmployeeType;
    this.attendancePeriod = this.attendanceForm.value.AdvanceDate;
    this.branchCode = this.attendanceForm.value.BranchCode;

    const dtAdvanceDate = new Date(this.attendanceForm.value.AdvanceDate);

    this.dtAdvanceDate = this.formatDate(
      new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
    );
    this.StartPeriod = this.formatDate(this.firstOfMonth(new Date(this.attendancePeriod)));
    this.EndPeriod = this.formatDate(this.lastOfMonth(new Date(this.attendancePeriod)));


    if (this.branchCode != '' && this.branchCode != undefined) {
      this.getEmployeeListByEmployeeType(this.branchCode, this.attendanceForm.value.EmployeeType, this.StartPeriod, this.EndPeriod, 'Active');
      this.getClients(this.dtAdvanceDate, this.branchCode);
    } else {
      this.getEmployeeListByEmployeeType("0", this.attendanceForm.value.EmployeeType, this.StartPeriod, this.EndPeriod, 'Active');
    }
  }
  onBranchSelectionChange(event: any) {
    if (event.value != 0) {
      this.showLoadingSpinner = true;
      this.clearFormFields();
      this.employeeSelectedType = this.attendanceForm.value.EmployeeType;

      let dtAdvanceDate = new Date(this.attendanceForm.value.AdvanceDate);
      this.dtAdvanceDate = this.formatDate(
        new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
      );

      this.attendancePeriod = this.formatDate(this.attendanceForm.get('AdvanceDate')?.value);
      this.branchCode = event.value;

      this.service.getClientsByBranchID(this.branchCode, this.currentUser!).subscribe({
        next: (clientData: any) => {
          // Assign clients
          this.clientList = clientData['clients'];
          this.filteredClientList = [...this.clientList]
        },
        error: (err) => {
          console.error('Error loading data:', err);
        }
      });

      this.StartPeriod = this.formatDate(this.firstOfMonth(new Date(this.attendancePeriod)));
      this.EndPeriod = this.formatDate(this.lastOfMonth(new Date(this.attendancePeriod)));

      if (this.attendancePeriod != null && this.attendancePeriod != 'NaN-NaN-NaN' && this.branchCode != '') {
        this.errorMessage = '';
        this.getEmployeeListByEmployeeType(this.branchCode, this.attendanceForm.value.EmployeeType, this.StartPeriod, this.EndPeriod, 'Active');
        this.getClients(this.attendancePeriod, this.branchCode);
      } else {
        this.errorMessage = 'Please select advance date selection.';
        this.attendanceForm.patchValue({
          EmployeeType: 'None',
        })
        this.hideloadingSpinner();
      }
    } else {
      this.clearFormFields();
      this.clientList = [];
      this.employeeListModel = [];
    }


  }
  radioButtonTypeSelectionChange(event: any) {
    //this.dynamicEditable = 'emptype';
    this.clearFormFields();
    this.employeeSelectedType = event.value;
    this.attendancePeriod = this.formatDate(this.attendanceForm.get('AdvanceDate')?.value);
    this.branchCode = this.attendanceForm.get('BranchCode')?.value;
    this.StartPeriod = this.formatDate(this.firstOfMonth(new Date(this.attendancePeriod)));
    this.EndPeriod = this.formatDate(this.lastOfMonth(new Date(this.attendancePeriod)));
    if (this.branchCode != null && this.branchCode != 'NaN-NaN-NaN' && this.branchCode != '') {
      const empClientCode = this.attendanceForm.get('Client')?.value;
      if (empClientCode != null && empClientCode != 'NaN-NaN-NaN' && empClientCode != ''
        && empClientCode != 0 && this.employeeSelectedType != 'Staff') {
        this.onClientChange(empClientCode);
      } else {
        this.errorMessage = '';
        // this.getEmployeeListByEmployeeType(branchCode, event.value, dtStartPeriod,dtEndPeriod, 'All');
        this.getEmployeeListByEmployeeType(this.branchCode, this.attendanceForm.value.EmployeeType, this.StartPeriod, this.EndPeriod, 'Active');
      }

    } else {
      this.errorMessage = 'Please select advance date and branch selection.';
      // this.attendanceForm.patchValue({
      //   EmployeeType: 'None',
      // })
    }
  }
  onClientChange(clientCode: any) {
    if (clientCode != 0) {
      this.employeeSelectedType = this.attendanceForm.value.EmployeeType;
      this.attendancePeriod = this.formatDate(this.attendanceForm.get('AdvanceDate')?.value);
      this.branchCode = this.attendanceForm.get('BranchCode')?.value;
      this.StartPeriod = this.formatDate(this.firstOfMonth(new Date(this.attendancePeriod)));
      this.EndPeriod = this.formatDate(this.lastOfMonth(new Date(this.attendancePeriod)));

      forkJoin({
        employeeList: this._payrollService.getListEmployeeByClient(this.branchCode, this.employeeSelectedType, this.StartPeriod, this.EndPeriod, 'Active', clientCode),
        salaryProcessStatus: this._payrollService.getIsSalaryProcessDoneForCurrentPeriod(this.branchCode, this.employeeSelectedType, this.dtAdvanceDate),
        nameList: this._payrollService.getEmployeeAttendanceList(this.dtAdvanceDate, this.branchCode),
        temporaryList: this._payrollService.getTemporaryEmployeeList(this.branchCode)
      }).subscribe(
        ({ employeeList, salaryProcessStatus, nameList, temporaryList }) => {
          // Handle successful response
          this.employeeListModel = employeeList;
          this.filteredEmployeeList = [...this.employeeListModel];
          this.salaryProcessStatus = salaryProcessStatus;
          this.nameList = nameList;
          this.temporaryEmployeeList = temporaryList;
          this.hideloadingSpinner();
        },
        (error) => this.handleErrors(error) // Handle errors
      );
    } else {
      this.getEmployeeListByEmployeeType(this.branchCode, this.attendanceForm.value.EmployeeType, this.StartPeriod, this.EndPeriod, 'Active');
    }

  }
  onEmployeeChange(empcode: any) {
    if (empcode != '0') {
      this.showLoadingSpinner = true;
      this.errorMessage = '';
      this.dynamicEditable = 'set';
      // Reset attendanceID immediately so the Edit-mode button shows 'Create'
      // for a new employee, even before the async API call completes.
      this.attendanceID = 0;
      this.attendanceForm.patchValue({ ID: 0 });
      this.employeeSelectedType = this.attendanceForm.value.EmployeeType;

      // ✅ BUG FIX: Always recalculate dtAdvanceDate here.
      // If the user navigates back or reloads, dtAdvanceDate may be stale or unset.
      // This ensures attendanceByEmployeeID always queries with the correct Period.
      const advDate = new Date(this.attendanceForm.value.AdvanceDate);
      this.dtAdvanceDate = this.formatDate(
        new Date(advDate.getFullYear(), advDate.getMonth() + 1, 0)
      );

      if (this.attendancePeriod != null && this.attendancePeriod != '') {
        this._payrollService.getEmployeeDetails(this.attendanceForm.value.BranchCode, this.attendanceForm.value.EmployeeNo).subscribe(
          (data) => {
            const employeeDetails = data[0];

            const allowance = employeeDetails.ATTENDANCEALLOWANCE ?? 0;
            const specialAllowance = employeeDetails.SpecialAllowance ?? 0;

            this.showAllowance = allowance > 0;
            this.showSpecialAllowance = specialAllowance > 0;

            // Patch employee details
            this.attendanceForm.patchValue({
              EmployeeID: employeeDetails.EMP_ID,
              Passport: employeeDetails.EMP_IC_NEW + employeeDetails.EMP_PASSPORT_NO,
              JoinDate: this.formatDisplayDate(employeeDetails.EMPPAY_DATE_JOINED) === '1970-01-01' ? '' : this.formatDisplayDate(employeeDetails.EMPPAY_DATE_JOINED),
              ResignedDate: this.formatDisplayDate(employeeDetails.EMPPAY_DATE_RESIGNED) === '1970-01-01' ? '' : this.formatDisplayDate(employeeDetails.EMPPAY_DATE_RESIGNED),
              IncomeTax: employeeDetails.INCOMETAXDETECT ? 'YES' : 'NO',
              EPF: employeeDetails.EPFDETECT ? 'YES' : 'NO',
              EpfNo: employeeDetails.EMPFL_EPFNO,
              Socso: employeeDetails.SOCSODETECT ? 'YES' : 'NO',
              PaymentMode: employeeDetails.PAYMODE,
              BasicPay: employeeDetails.EMPPAY_BASIC_RATE,
              Allowance: employeeDetails.ATTENDANCEALLOWANCE,
              SpecialAllowance: employeeDetails.SpecialAllowance,
              SalaryStructure: employeeDetails.SalaryStructure === 'Y' ? 'YES' : 'NO',
              SalarySlab: employeeDetails.Name,
              Age: this.calculateAge(employeeDetails.EMP_DATE_OF_BIRTH),
            });

            // Fetch leave details and attendance details in parallel
            forkJoin({
              annualLeave: this._payrollService.getAnnualLeave(this.attendanceForm.value.EmployeeID, this.attendanceForm.value.AdvanceDate),
              medicalLeave: this._payrollService.getMedicalLeave(this.attendanceForm.value.EmployeeID, this.attendanceForm.value.AdvanceDate),
              maternityLeave: this._payrollService.getMaternityLeave(this.attendanceForm.value.EmployeeID, this.attendanceForm.value.AdvanceDate),
              paternityLeave: this._payrollService.getPaternityLeave(this.attendanceForm.value.EmployeeID, this.attendanceForm.value.AdvanceDate),
              hospitalizationLeave: this._payrollService.getHospitalizationLeave(this.attendanceForm.value.EmployeeID, this.attendanceForm.value.AdvanceDate),
              attendanceData: this._payrollService.attendanceByEmployeeID(this.dtAdvanceDate, this.attendanceForm.value.EmployeeID),
            }).subscribe(
              ({ annualLeave, medicalLeave, maternityLeave, paternityLeave, hospitalizationLeave, attendanceData }) => {
                // Patch leave details
                this.attendanceForm.patchValue({
                  Annual: annualLeave.LeaveRemaining,
                  Medical: medicalLeave.LeaveRemaining,
                  Maternity: maternityLeave.LeaveRemaining,
                  Paternity: paternityLeave.LeaveRemaining,
                  Hospitalization: hospitalizationLeave.LeaveRemaining,
                });

                let iNoOfDays = 0;
                let iStartDay = 1;

                const advanceDate = new Date(this.attendanceForm.value.AdvanceDate);
                this.dtAttendanceDate = new Date(this.attendanceForm.value.AdvanceDate);

                const today = new Date();
                const joinDate = new Date(this.attendanceForm.value.JoinDate);
                const resignDate = this.attendanceForm.value.ResignedDate
                  ? new Date(this.attendanceForm.value.ResignedDate)
                  : null;

                // Check if the AdvanceDate is in the current month and year
                if (advanceDate.getMonth() === today.getMonth() && advanceDate.getFullYear() === today.getFullYear()) {
                  iNoOfDays = advanceDate.getDate();
                } else {
                  // If joinDate and resignDate are in the same month, calculate difference
                  if (joinDate && resignDate
                    && joinDate.getMonth() === resignDate.getMonth()
                    && joinDate.getFullYear() === resignDate.getFullYear()) {
                    iNoOfDays = resignDate.getDate() - joinDate.getDate() + 1; // include both days
                  } else {
                    iNoOfDays = this.getDaysInMonth(advanceDate.toString()); // Use utility function to get days in month
                  }
                }
                const attendanceDate = new Date(advanceDate.getFullYear(), advanceDate.getMonth(), 1);
                // Check if the employee has a resignation date
                if (resignDate) {
                  if (attendanceDate > resignDate) {
                    this.showMessage(
                      `Employee has resigned on ${resignDate.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}`,
                      'warning',
                      'Warning Message'
                    );
                    this.getEmployeeListByEmployeeType(this.branchCode, this.attendanceForm.value.EmployeeType, this.StartPeriod, this.EndPeriod, 'Active');
                    this.clearFormFields();
                    this.hideloadingSpinner();
                    return;
                  }
                  else if (this.dtAttendanceDate.getMonth() === resignDate.getMonth() && this.dtAttendanceDate.getFullYear() === resignDate.getFullYear()) {
                    this.dtAttendanceDate = resignDate;

                    // Handle join + resign in same month
                    if (joinDate && joinDate.getMonth() === resignDate.getMonth() && joinDate.getFullYear() === resignDate.getFullYear()) {
                      iNoOfDays = resignDate.getDate() - joinDate.getDate() + 1;
                    } else {
                      iNoOfDays = resignDate.getDate();
                    }
                  }
                }

                // Check if the AdvanceDate is in the employee's joining month and year
                if (this.dtAttendanceDate.getMonth() === joinDate.getMonth() && this.dtAttendanceDate.getFullYear() === joinDate.getFullYear()) {
                  iStartDay = joinDate.getDate();
                }

                // Handle attendance data
                if (attendanceData) {
                  this.attendanceForm.patchValue({
                    ID: attendanceData.ID,
                    EmployeeID: attendanceData.EmployeeID,
                    BonusAmount: attendanceData.Bonus,
                    Shift2Type: attendanceData.Shift2Type === 1 ? '1' : attendanceData.Shift2Type === 2 ? '2' : '3',
                    Shift2Rate: attendanceData.Shift2Rate,
                    KPIDeduction: attendanceData.KPIDeduction,
                    LastUpdatedBy: attendanceData.LastUpdatedBy,
                  });
                  if (this.employeeSelectedType === 'Guard' || this.employeeSelectedType === 'Others') {
                    this.attendanceForm.patchValue({
                      AllowanceDeduction: attendanceData.AllowanceDeduction,
                      SpecialAllowanceDeduction: attendanceData.SpecialAllowanceDeduction
                    });
                  } else {
                    this.attendanceForm.patchValue({
                      AllowanceDeductionStaff: attendanceData.AllowanceDeduction,
                      SpecialAllowanceDeductionStaff: attendanceData.SpecialAllowanceDeduction
                    });
                  }
                  this.attendanceID = attendanceData.ID;
                  this.iAbsent = 0;
                  this.iAnnualLeave = 0;
                  this.iMedicalLeave = 0;
                  this.iMaternityLeave = 0;
                  this.iPaternityLeave = 0;
                  this.iHospitalizationLeave = 0;
                  if (this.attendanceID > 0) {
                    forkJoin({
                      attendanceDetails: this._payrollService.attendanceDetailsByID(this.attendanceID),
                    }).subscribe(({ attendanceDetails }) => {
                      if (attendanceDetails) {
                        this.attendanceDetails = attendanceDetails;
                        attendanceDetails.forEach((oAttendanceDetails: any) => {
                          switch (oAttendanceDetails.Type) {
                            case 7:
                              this.iAbsent++;
                              break;
                            case 8:
                              this.iAnnualLeave++;
                              break;
                            case 9:
                              this.iMedicalLeave++;
                              break;
                            case 10:
                              this.iMaternityLeave++;
                              break;
                            case 11:
                              this.iPaternityLeave++;
                              break;
                            case 12:
                              this.iHospitalizationLeave++;
                              break;
                            default:
                              // Handle other types if needed
                              break;
                          }
                        });
                        this.updateFormFields(attendanceDetails, iNoOfDays, iStartDay);

                      } else {
                        this.advanceDateError = '';
                        this.addFormFields(iNoOfDays, iStartDay);
                      }
                    })
                  } else {
                    // attendanceData returned an object but with ID=0 (no saved record yet) — treat as new
                    this.advanceDateError = '';
                    this.addFormFields(iNoOfDays, iStartDay);
                  }
                } else {
                  this.advanceDateError = '';
                  this.addFormFields(iNoOfDays, iStartDay);
                }

                // Hide loading spinner after completion
                this.hideloadingSpinner();
              },
              (error) => this.handleErrors(error)
            );
          },
          (error) => this.handleErrors(error)
        );

      } else {
        this.advanceDateError = 'AdvanceDate';
        this.attendanceForm.patchValue({
          EmployeeNo: ''
        });
        this.hideloadingSpinner();
      }
    } else {
      this.clearFormFields();
    }
  }

  addFormFields(count: number, startDay: number = 1): void {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    formArray.clear();

    const baseDate = new Date(this.attendanceForm.value.AdvanceDate);
    const year = baseDate.getFullYear();
    const month = baseDate.getMonth();

    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const remainingDays = daysInMonth - startDay + 1;

    const safeCount = Math.min(count, remainingDays);

    for (let i = 0; i < safeCount; i++) {

      const day = startDay + i;
      const currentDate = new Date(year, month, day);

      formArray.push(this.fb.group({
        weekDay: this.getWeekday(currentDate.getDay()),
        dayField: [day],
        ID: [0],
        AttendanceID: [0],
        AttendanceDate: [this.formatDate(currentDate)],
        Client: [''],
        ClientCode: [''],
        Type: ['General Working'],
        TimeStart: [null],
        TimeEnd: [null],
        StartTime: [''],
        EndTime: [''],
        Hours: [''],
        OTClientCode: [''],
        OTClient: [''],
        OTTimeStart: [null],
        OTTimeEnd: [null],
        StartTimeOT: [''],
        EndTimeOT: [''],
        Shift2Hours: [''],
        LastUpdate: [this.formatDate(new Date())],
        LastUpdatedBy: [this.currentUser],
      }));

      this.attendanceForm.patchValue({
        Shift2Type: '3'
      })
    }
  }

  updateFormFields(data: any, iNoOfDays: number, iStartDay: number): void {
    this.showLoadingSpinner = true;

    const formArray = this.dynamicForm.get('formArray') as FormArray;
    formArray.clear();

    const baseDate = new Date(this.attendanceForm.value.AdvanceDate);
    const year = baseDate.getFullYear();
    const month = baseDate.getMonth();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const remainingDays = daysInMonth - iStartDay + 1;
    const safeCount = Math.min(iNoOfDays, remainingDays);

    for (let idx = 0; idx < safeCount; idx++) {

      const day = iStartDay + idx;
      const currentDate = new Date(year, month, day);

      // Match by date instead of by array index to prevent data shifting
      // when only some days have saved records (e.g. employee joined mid-month)
      const row = data.find((d: any) => {
        const rowDate = new Date(d.AttendanceDate);
        return rowDate.getFullYear() === year
          && rowDate.getMonth() === month
          && rowDate.getDate() === day;
      }) ?? null;

      formArray.push(this.fb.group({
        weekDay: this.getWeekday(currentDate.getDay()),
        dayField: [day],
        ID: [row?.ID || 0],
        AttendanceID: [row?.AttendanceID || 0],
        AttendanceDate: [this.formatDate(currentDate)],
        Client: [(row?.Client || '')],
        ClientCode: [row?.Client || ''],
        Type: [this.getWorkType(row?.Type || '')],
        TimeStart: [row?.TimeStart || null],
        TimeEnd: [row?.TimeEnd || null],
        StartTime: this.getDayOfWeek(row?.TimeStart) || '',
        EndTime: this.getDayOfWeek(row?.TimeEnd) || '',
        Hours: this.getDayOfHoursEdited(row?.TimeStart, row?.TimeEnd) || '0',
        OTClient: [(row?.OTClient || '')],
        OTClientCode: [row?.OTClient || ''],
        OTTimeStart: [row?.OTTimeStart || null],
        OTTimeEnd: [row?.OTTimeEnd || null],
        StartTimeOT: this.getDayOfWeek(row?.OTTimeStart) || '',
        EndTimeOT: this.getDayOfWeek(row?.OTTimeEnd) || '',
        Shift2Hours: this.getDayOfHoursEdited(row?.OTTimeStart, row?.OTTimeEnd) || '',
        LastUpdate: [this.formatDate(new Date(row?.LastUpdate)) || ''],
        LastUpdatedBy: [row?.LastUpdatedBy || this.currentUser],
      }));
    }

    // Hide spinner immediately after form is built — no artificial delay
    this.hideloadingSpinner();
  }

  addStaffFormFields(count: number, startDay: number = 1): void {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    formArray.clear();

    const baseDate = new Date(this.attendanceForm.value.AdvanceDate);
    const year = baseDate.getFullYear();
    const month = baseDate.getMonth();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const remainingDays = daysInMonth - startDay + 1;
    const safeCount = Math.min(count, remainingDays);

    for (let idx = 0; idx < safeCount; idx++) {

      const day = startDay + idx;
      const currentDate = new Date(year, month, day);
      const weekDayIndex = currentDate.getDay();

      let type = 'General Working';
      let hours = '8';

      if (weekDayIndex === 0) {
        type = 'Off Day';
        hours = '0';
      } else if (weekDayIndex === 6) {
        hours = '4';
      }

      formArray.push(this.fb.group({
        weekDay: this.getWeekday(weekDayIndex),
        dayField: [day],
        ID: [0],
        AttendanceID: [0],
        AttendanceDate: [this.formatDate(currentDate)],
        Client: [''],
        ClientCode: [''],
        Type: [type],
        TimeStart: [null],
        TimeEnd: [null],
        StartTime: [''],
        EndTime: [''],
        Hours: [hours],
        OTClientCode: [''],
        OTClient: [''],
        OTTimeStart: [null],
        OTTimeEnd: [null],
        StartTimeOT: [''],
        EndTimeOT: [''],
        Shift2Hours: [''],
        LastUpdate: [this.formatDate(new Date())],
        LastUpdatedBy: [this.currentUser],
      }));

      this.hoursTimeChange(idx, hours);
    }
  }
  updateStaffFormFields(data: any, iNoOfDays: number, iStartDay: number): void {
    this.showLoadingSpinner = true;

    const formArray = this.dynamicForm.get('formArray') as FormArray;
    formArray.clear();

    const baseDate = new Date(this.attendanceForm.value.AdvanceDate);
    const year = baseDate.getFullYear();
    const month = baseDate.getMonth();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const remainingDays = daysInMonth - iStartDay + 1;
    const safeCount = Math.min(iNoOfDays, remainingDays);

    for (let idx = 0; idx < safeCount; idx++) {

      const day = iStartDay + idx;
      const currentDate = new Date(year, month, day);
      const weekDayIndex = currentDate.getDay();

      // Match by date instead of by array index
      const row = data.find((d: any) => {
        const rowDate = new Date(d.AttendanceDate);
        return rowDate.getFullYear() === year
          && rowDate.getMonth() === month
          && rowDate.getDate() === day;
      }) ?? null;

      let type = 'General Working';
      let hours = '8';

      if (weekDayIndex === 0) {
        type = 'Off Day';
        hours = '0';
      } else if (weekDayIndex === 6) {
        hours = '4';
      }

      // ✅ BUG FIX: If the DB has a saved Type for this row (e.g. Medical Leave),
      // use it. Only fall back to the calendar-default when there is no saved record.
      // Previously this always used the calendar type, wiping all leave entries on reload.
      const savedType = row?.Type ? this.getWorkType(row.Type) : null;
      const displayType = savedType || type;

      formArray.push(this.fb.group({
        weekDay: this.getWeekday(weekDayIndex),
        dayField: [day],
        ID: [row?.ID || 0],
        AttendanceID: [row?.AttendanceID || 0],
        AttendanceDate: [this.formatDate(currentDate)],
        Client: [row?.Client || ''],
        ClientCode: [row?.Client || ''],
        Type: [displayType],
        TimeStart: [row?.TimeStart || null],
        TimeEnd: [row?.TimeEnd || null],
        StartTime: this.getDayOfWeek(row?.TimeStart) || '',
        EndTime: this.getDayOfWeek(row?.TimeEnd) || '',
        Hours: [hours],
        OTClient: [''],
        OTClientCode: [''],
        OTTimeStart: [null],
        OTTimeEnd: [null],
        StartTimeOT: [''],
        EndTimeOT: [''],
        Shift2Hours: [''],
        LastUpdate: [this.formatDate(new Date(row?.LastUpdate)) || ''],
        LastUpdatedBy: [row?.LastUpdatedBy || this.currentUser],
      }));

      this.hoursTimeChange(idx, hours);
    }

    // Hide spinner immediately after form is built — no artificial delay
    this.hideloadingSpinner();
  }

  normalValues1Change(event: any) {
    this.normalValue1Change = event.value == '' ? '0' : event.value;
  }
  normalValues2Change(event: any) {
    this.normalValue2Change = event.value == '' ? '0' : event.value;
  }
  shift2Values1Change(event: any) {
    this.shiftValues1Change = event.value == '' ? '0' : event.value;
  }
  shift2Values2Change(event: any) {
    this.shiftValues2Change = event.value == '' ? '0' : event.value;
  }
  clientNameChange(event: any) {
    this.ClientName = event.value == '' ? '0' : event.value;
  }
  Shif2ClientChange(event: any) {
    this.Shift2Client = event.value == '' ? '0' : event.value;
  }
  getClients(advanceDate: string, branchCode: string): void {
    this._payrollService.getClients(advanceDate, branchCode).subscribe(
      (data) => {
        this.employeeModel = [...data.Value];
        this.filteredEmployeeClientList = [... this.employeeModel];
        this.hideloadingSpinner();
      },
      (error) => this.handleErrors(error)
    );
  }

  getEmployeeName(code: any): string {
    if (code === null || code === undefined || code === '') return '';
    const codeStr = String(code);
    const emp = (this.employeeModel || []).find(e => String(e.Code) === codeStr);
    return emp ? emp.Shortname : ''; // important: return empty string, not the raw code
  }

  getEmployeeFullName(code: any): string {
    if (!code) return '';

    const employee = this.employeeModel.find(x => x.Code === code);
    return employee ? employee.Name : '';
  }
  getEmployeeShortName(code: any): string {
    if (!code) return '';

    const employee = this.employeeModel.find(x => x.Code === code);
    return employee ? employee.Shortname : '';
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
        this.employeeListModel = employeeList;
        this.filteredEmployeeList = [...this.employeeListModel];
        this.salaryProcessStatus = salaryProcessStatus;
        this.nameList = nameList;
        this.temporaryEmployeeList = temporaryList;
        this.hideloadingSpinner();
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

  getEmployeeMasterList(): void {
    this._payrollService.getEmployeeList().subscribe(
      (data) => {
        this.employeeModel = data;
        this.filteredEmployeeClientList = [... this.employeeModel];
      },
      (error) => this.handleErrors(error)
    );
  }
  createForm() {
    this.dynamicForm = this.fb.group({
      formArray: this.fb.array([])
    });
  }
  get formArray() {
    return (this.dynamicForm.get('formArray') as FormArray).controls;
  }
  getBranchMasterList() {
    this._masterService.getBranchMaster('null').subscribe((responseData) => {
      if (responseData != null) {
        this.branchModel = responseData
        this.filteredBranchList = [...this.branchModel]
      }
    },
      (error) => this.handleErrors(error)
    );
  }
  getBranchMasterListByUser(userName: string) {
    forkJoin({
      branchList: this._masterService.GetBranchListByUserName(userName)
    }).subscribe(
      ({ branchList }) => {
        this.branchModel = branchList;
        this.filteredBranchList = [...this.branchModel]
      },
      (error) => {
        this.handleErrors(error);
      }
    );
  }
  getDayOfWeek(dateTime: any): any {
    if (dateTime != null) {
      const dateTime1 = new Date(dateTime);
      // Check if the conversion was successful
      if (!isNaN(dateTime1.getTime())) {
        return dateTime1.getHours();
      }
    }
    return '';
  }
  getWorkType(type: number): string {
    // ✅ BUG FIX: Reset workType before each call.
    // Previously, if type didn't match any case, the last-set value was returned (stale state).
    this.workType = 'General Working'; // safe default
    if (type == 1) {
      this.workType = 'General Working';
    }
    if (type == 2) {
      this.workType = 'Off Day';
    }
    if (type == 3) {
      this.workType = 'Off Day Working';
    }
    if (type == 4) {
      this.workType = 'Holiday';
    }
    if (type == 5) {
      this.workType = 'Holiday Working';
    }
    if (type == 6) {
      this.workType = 'Unpaid Leave';
    }
    if (type == 7) {
      this.workType = 'Absent';
    }
    if (type == 8) {
      this.workType = 'Annual Leave';
      const type = 'Annual Leave';
    }
    if (type == 9) {
      this.workType = 'Medical Leave';
    }
    if (type == 10) {
      this.workType = 'Maternity Leave';
    }
    if (type == 11) {
      this.workType = 'Paternity Leave';
    }
    if (type == 12) {
      this.workType = 'Hospitalization Leave';
    }
    if (type == 13) {
      this.workType = 'Socso';
    }
    if (type == 14) {
      this.workType = 'Non Schedule Off';
    }
    if (type == 15) {
      this.workType = 'Replacement Leave';
    }
    if (type == 16) {
      this.workType = 'Compensanate Leave';
    }
    if (type == 17) {
      this.workType = 'Marriage Leave';
    }
    return this.workType;
  }
  getWorkTypeByName(typeName: string): number {
    if (typeName == 'General Working') {
      this.workTypeId = 1;
    }
    if (typeName == 'Off Day') {
      this.workTypeId = 2;
    }
    if (typeName == 'Off Day Working') {
      this.workTypeId = 3;
    }
    if (typeName == 'Holiday') {
      this.workTypeId = 4;
    }
    if (typeName == 'Holiday Working') {
      this.workTypeId = 5;
    }
    if (typeName == 'Unpaid Leave') {
      this.workTypeId = 6;
    }
    if (typeName == 'Absent') {
      this.workTypeId = 7;
    }
    if (typeName == 'Annual Leave') {
      this.workTypeId = 8;
    }
    if (typeName == 'Medical Leave') {
      this.workTypeId = 9;
    }
    if (typeName == 'Maternity Leave') {
      this.workTypeId = 10;
    }
    if (typeName == 'Paternity Leave') {
      this.workTypeId = 11;
    }
    if (typeName == 'Hospitalization Leave') {
      this.workTypeId = 12;
    }
    if (typeName == 'Socso') {
      this.workTypeId = 13;
    }
    if (typeName == 'Non Schedule Off') {
      this.workTypeId = 14;
    }
    if (typeName == 'Replacement Leave') {
      this.workTypeId = 15;
    }
    if (typeName == 'Compensanate Leave') {
      this.workTypeId = 16;
    }
    if (typeName == 'Marriage Leave') {
      this.workTypeId = 17;
    }
    return this.workTypeId;
  }

  applyIncrementByType(type: number): void {
    switch (type) {
      case 7:
        this.iAbsent++;
        break;
      case 8:
        this.iAnnualLeave++;
        break;
      case 9:
        this.iMedicalLeave++;
        break;
      case 10:
        this.iMaternityLeave++;
        break;
      case 11:
        this.iPaternityLeave++;
        break;
      case 12:
        this.iHospitalizationLeave++;
        break;
      default:
        // No action needed
        break;
    }
  }

  getWeekday(dayIndex: number): string {
    const weekdays = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    return weekdays[dayIndex];
  }
  calculateAge(birthdate: Date) {
    this._payrollService.calculateAge(birthdate).subscribe(
      (age) => {
        this.attendanceForm.patchValue({
          Age: age,
        });
      },
      (error) => this.handleErrors(error)
    );
  }
  setButtonClick(): void {
    const formValues = this.attendanceForm.value;
    const shift2Client = formValues.Shif2Client;
    const shift2Start = formValues.Shift2StartTime;
    const shift2End = formValues.Shift2EndTime;

    const isShift2ClientEmpty = !shift2Client || shift2Client === '' || shift2Client === '0' || shift2Client === 0;
    const isShift2StartEmpty = !shift2Start || shift2Start === '' || shift2Start === '0' || shift2Start === 0;
    const isShift2EndEmpty = !shift2End || shift2End === '' || shift2End === '0' || shift2End === 0;

    const anyShift2Filled = !isShift2ClientEmpty || !isShift2StartEmpty || !isShift2EndEmpty;

    if (anyShift2Filled) {
      // At least one Shift II field is filled — validate all three are present
      const missingFields: string[] = [];
      if (isShift2ClientEmpty) missingFields.push('Shift II Client');
      if (isShift2StartEmpty) missingFields.push('Shift II Start Time');
      if (isShift2EndEmpty) missingFields.push('Shift II End Time');

      if (missingFields.length > 0) {
        this.showMessage(
          `Please set the following Shift II field(s): ${missingFields.join(', ')}`,
          'warning',
          'Warning Message'
        );
        return;
      }
    }

    this.dynamicEditable = 'set';
    this.assignSetButtonValues();
  }
  setStaffButtonClick(): void {
    this.dynamicEditable = 'set';
    let iNoOfDays = 0;
    let iStartDay = 1;

    const advanceDate = new Date(this.attendanceForm.value.AdvanceDate);
    this.dtAttendanceDate = new Date(this.attendanceForm.value.AdvanceDate);

    const today = new Date();
    const joinDate = new Date(this.attendanceForm.value.JoinDate);
    const resignDate = this.attendanceForm.value.ResignedDate
      ? new Date(this.attendanceForm.value.ResignedDate)
      : null;

    // Check if the AdvanceDate is in the current month and year
    if (advanceDate.getMonth() === today.getMonth() && advanceDate.getFullYear() === today.getFullYear()) {
      iNoOfDays = advanceDate.getDate();
    } else {
      iNoOfDays = this.getDaysInMonth(advanceDate.toString()); // Use utility function to get days in month
    }
    const attendanceDate = new Date(advanceDate.getFullYear(), advanceDate.getMonth(), 1);
    // Check if the employee has a resignation date
    if (resignDate) {
      if (attendanceDate > resignDate) {
        this.showMessage(`Employee has resigned on ${resignDate.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}`, 'warning', 'Warning Message');
        this.getEmployeeListByEmployeeType(this.branchCode, this.attendanceForm.value.EmployeeType, this.StartPeriod, this.EndPeriod, 'Active');
        this.clearFormFields()
        this.hideloadingSpinner();
        return;
      } else if (this.dtAttendanceDate.getMonth() === resignDate.getMonth() && this.dtAttendanceDate.getFullYear() === resignDate.getFullYear()) {
        this.dtAttendanceDate = resignDate
        iNoOfDays = resignDate.getDate();
      }
    }

    // Check if the AdvanceDate is in the employee's joining month and year
    if (this.dtAttendanceDate.getMonth() === joinDate.getMonth() && this.dtAttendanceDate.getFullYear() === joinDate.getFullYear()) {
      iStartDay = joinDate.getDate();
    }

    //let iNoOfDays = new Date(this.attendanceForm.value.AdvanceDate).getDate();
    if (this.attendanceDetails.length > 0) {
      this.updateStaffFormFields(this.attendanceDetails, iNoOfDays, iStartDay);
    } else {
      this.addStaffFormFields(iNoOfDays, iStartDay);
    }
  }
  editButtonClick(): void {
    this.displayFullName = false;
    this.dynamicEditable = 'edit';
    this.selectedWorkType = '1';
    if (this.employeeSelectedType == 'Guard' || this.employeeSelectedType == 'Others') {
      this.assignEditedButtonValues();
    } else {
      this.assignEditedStaffButtonValues()
    }
    setTimeout(() => {
      this.initializeRowDropdown('filteredEmployeeClientList');
    });
  }
  updateButtonClick(): void {
    this.dynamicEditable = 'set';
    this.assignUpdatedButtonValues();
  }
  updateStaffButtonClick(): void {
    this.dynamicEditable = 'set';
    this.assignStaffUpdatedButtonValues();
  }
  clearButtonClick(): void {
    this.dynamicEditable = 'set';
    this.clearFormFields();
  }
  clearEditButtonClick(): void {
    this.dynamicEditable = 'set';

    const formArray = this.dynamicForm.get('formArray') as FormArray;
    formArray.clear();

    this.shift2StartTimeValidation = null
    this.shift2EndTimeValidation = null
    this.shift2HoursValidation = null

    this.onEmployeeChange(0);
  }
  assignSetButtonValues() {
    let formArray = this.dynamicForm.get('formArray') as FormArray;

    for (let i = 0; i < formArray.length; i++) {
      const control = formArray.at(i) as FormGroup;
      const formArrayItem = this.dynamicForm.value.formArray[i];
      const attendanceForm = this.attendanceForm.value;

      // Extract common values
      const { StartTime, EndTime, Shift2StartTime, Shift2EndTime, Shif2Client, ClientName } = attendanceForm;

      // Compute dynamic values
      const dynamicStartTime = this.normalValue1Change === '0' ? '' : formArrayItem.StartTime;
      const dynamicEndTime = this.normalValue2Change === '0' ? '' : formArrayItem.EndTime;
      const dynamicShift2StartTime = this.shiftValues1Change === '0' ? '' : formArrayItem.Shift2StartTime;
      const dynamicShift2EndTime = this.shiftValues2Change === '0' ? '' : formArrayItem.Shift2EndTime;
      // Patch common values
      control.get('Type')?.patchValue(formArrayItem.Type, { emitEvent: false });
      control.get('ClientCode')?.patchValue(ClientName, { emitEvent: false });
      // control.get('Client')?.patchValue(this.getEmployeeFullName(ClientName), { emitEvent: false });
      control.get('Client')?.patchValue(ClientName), { emitEvent: false };
      control.get('StartTime')?.patchValue(this.getEffectiveValue(StartTime, dynamicStartTime), { emitEvent: false });
      control.get('EndTime')?.patchValue(this.getEffectiveValue(EndTime, dynamicEndTime), { emitEvent: false });
      control.get('Hours')?.patchValue(this.getComputedHours(StartTime, EndTime, dynamicStartTime, dynamicEndTime), { emitEvent: false });
      control.get('OTClientCode')?.patchValue(Shif2Client, { emitEvent: false });
      // control.get('OTClient')?.patchValue(this.getEmployeeFullName(Shif2Client), { emitEvent: false });
      control.get('OTClient')?.patchValue(Shif2Client), { emitEvent: false };
      control.get('StartTimeOT')?.patchValue(this.getEffectiveValue(Shift2StartTime, dynamicShift2StartTime), { emitEvent: false });
      control.get('EndTimeOT')?.patchValue(this.getEffectiveValue(Shift2EndTime, dynamicShift2EndTime), { emitEvent: false });
      control.get('Shift2Hours')?.patchValue(this.getComputedHours(Shift2StartTime, Shift2EndTime, dynamicShift2StartTime, dynamicShift2EndTime), { emitEvent: false });

      // Patch formatted time values
      control.get('TimeStart')?.patchValue(this.formatDate(this.assignTimeStartValue(StartTime, i + 1)), { emitEvent: false });
      control.get('TimeEnd')?.patchValue(this.formatDate(this.assignTimeEndValue(EndTime, i + 1)), { emitEvent: false });
      control.get('OTTimeStart')?.patchValue(this.formatDate(this.assignOTTimeStartValue(Shift2StartTime, i + 1)), { emitEvent: false });
      control.get('OTTimeEnd')?.patchValue(this.formatDate(this.assignOTTimeEndValue(Shift2EndTime, i + 1)), { emitEvent: false });
    }
  }
  assignEditedButtonValues() {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    for (let i = 0; i < formArray.length; i++) {
      const control = formArray.at(i) as FormGroup
      control.get('Type')?.patchValue(this.getWorkTypeByName(this.dynamicForm.value.formArray[i].Type), { emitEvent: false });
      control.get('ClientCode')?.patchValue(this.dynamicForm.value.formArray[i].ClientCode || '', { emitEvent: false });
      control.get('Client')?.patchValue(this.dynamicForm.value.formArray[i].ClientCode || '', { emitEvent: false });
      control.get('StartTime')?.patchValue(this.dynamicForm.value.formArray[i].StartTime == 0 ? '' : this.dynamicForm.value.formArray[i].StartTime, { emitEvent: false });
      control.get('EndTime')?.patchValue(this.dynamicForm.value.formArray[i].EndTime == 0 ? '' : this.dynamicForm.value.formArray[i].EndTime, { emitEvent: false });
      control.get('Hours')?.patchValue(this.getDayOfHours(this.dynamicForm.value.formArray[i].StartTime, this.dynamicForm.value.formArray[i].EndTime), { emitEvent: false });
      control.get('OTClientCode')?.patchValue(this.dynamicForm.value.formArray[i].OTClientCode || '', { emitEvent: false });
      control.get('OTClient')?.patchValue(this.dynamicForm.value.formArray[i].OTClientCode || '', { emitEvent: false });
      control.get('StartTimeOT')?.patchValue(this.dynamicForm.value.formArray[i].StartTimeOT == 0 ? '' : this.dynamicForm.value.formArray[i].StartTimeOT, { emitEvent: false });
      control.get('EndTimeOT')?.patchValue(this.dynamicForm.value.formArray[i].EndTimeOT == 0 ? '' : this.dynamicForm.value.formArray[i].EndTimeOT, { emitEvent: false });
      control.get('Shift2Hours')?.patchValue(this.getDayOfHours(this.dynamicForm.value.formArray[i].StartTimeOT, this.dynamicForm.value.formArray[i].EndTimeOT), { emitEvent: false });
      control.get('TimeStart')?.patchValue(this.formatDate(this.assignTimeStartValue(this.dynamicForm.value.formArray[i].StartTime, i + 1)), { emitEvent: false });
      control.get('TimeEnd')?.patchValue(this.formatDate(this.assignTimeEndValue(this.dynamicForm.value.formArray[i].EndTime, i + 1)), { emitEvent: false });
      control.get('OTTimeStart')?.patchValue(this.formatDate(this.assignOTTimeStartValue(this.dynamicForm.value.formArray[i].StartTimeOT, i + 1)), { emitEvent: false });
      control.get('OTTimeEnd')?.patchValue(this.formatDate(this.assignOTTimeEndValue(this.dynamicForm.value.formArray[i].EndTimeOT, i + 1)), { emitEvent: false });
    }
  }
  assignEditedStaffButtonValues() {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    for (let i = 0; i < formArray.length; i++) {
      const control = formArray.at(i) as FormGroup
      control.get('Type')?.patchValue(this.getWorkTypeByName(this.dynamicForm.value.formArray[i].Type), { emitEvent: false });
      control.get('ClientCode')?.patchValue(this.dynamicForm.value.formArray[i].ClientCode || '', { emitEvent: false });
      control.get('Client')?.patchValue(this.dynamicForm.value.formArray[i].ClientCode || '', { emitEvent: false });
      control.get('StartTime')?.patchValue(this.dynamicForm.value.formArray[i].StartTime == 0 ? '' : this.dynamicForm.value.formArray[i].StartTime, { emitEvent: false });
      control.get('EndTime')?.patchValue(this.dynamicForm.value.formArray[i].EndTime == 0 ? '' : this.dynamicForm.value.formArray[i].EndTime, { emitEvent: false });
      control.get('Hours')?.patchValue(this.getDayOfHours(this.dynamicForm.value.formArray[i].StartTime, this.dynamicForm.value.formArray[i].EndTime), { emitEvent: false });
      control.get('OTClientCode')?.patchValue('', { emitEvent: false });
      control.get('OTClient')?.patchValue('', { emitEvent: false });
      control.get('StartTimeOT')?.patchValue('', { emitEvent: false });
      control.get('EndTimeOT')?.patchValue('', { emitEvent: false });
      control.get('Shift2Hours')?.patchValue('', { emitEvent: false });
      control.get('TimeStart')?.patchValue(this.formatDate(this.assignTimeStartValue(this.dynamicForm.value.formArray[i].StartTime, i + 1)), { emitEvent: false });
      control.get('TimeEnd')?.patchValue(this.formatDate(this.assignTimeEndValue(this.dynamicForm.value.formArray[i].EndTime, i + 1)), { emitEvent: false });
      control.get('OTTimeStart')?.patchValue(null, { emitEvent: false });
      control.get('OTTimeEnd')?.patchValue(null, { emitEvent: false });
      control.get('LastUpdate')?.patchValue(this.formatDate(new Date()), { emitEvent: false });
    }
  }
  assignStaffUpdatedButtonValues() {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    for (let i = 0; i < formArray.length; i++) {
      const control = formArray.at(i) as FormGroup
      control.get('Type')?.patchValue(this.getWorkType(this.dynamicForm.value.formArray[i].Type), { emitEvent: false });
      control.get('ClientCode')?.patchValue(this.dynamicForm.value.formArray[i].Client || '', { emitEvent: false });
      control.get('Client')?.patchValue(this.getEmployeeFullName(this.dynamicForm.value.formArray[i].Client || ''), { emitEvent: false });
      control.get('StartTime')?.patchValue(this.dynamicForm.value.formArray[i].StartTime == 0 ? '' : this.dynamicForm.value.formArray[i].StartTime, { emitEvent: false });
      control.get('EndTime')?.patchValue(this.dynamicForm.value.formArray[i].EndTime == 0 ? '' : this.dynamicForm.value.formArray[i].EndTime, { emitEvent: false });
      control.get('Hours')?.patchValue(this.dynamicForm.value.formArray[i].Hours, { emitEvent: false });
      control.get('OTClientCode')?.patchValue('', { emitEvent: false });
      control.get('OTClient')?.patchValue('', { emitEvent: false });
      control.get('StartTimeOT')?.patchValue('', { emitEvent: false });
      control.get('EndTimeOT')?.patchValue('', { emitEvent: false });
      control.get('Shift2Hours')?.patchValue('', { emitEvent: false });
      control.get('TimeStart')?.patchValue(this.formatDate(this.dynamicForm.value.formArray[i].TimeStart), { emitEvent: false });
      control.get('TimeEnd')?.patchValue(this.formatDate(this.dynamicForm.value.formArray[i].TimeEnd), { emitEvent: false });
      control.get('OTTimeStart')?.patchValue(null, { emitEvent: false });
      control.get('OTTimeEnd')?.patchValue(null, { emitEvent: false });
    }
  }
  assignUpdatedButtonValues() {
    this.displayFullName = true;
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    for (let i = 0; i < formArray.length; i++) {
      const control = formArray.at(i) as FormGroup
      control.get('Type')?.patchValue(this.getWorkType(this.dynamicForm.value.formArray[i].Type), { emitEvent: false });
      control.get('ClientCode')?.patchValue(this.dynamicForm.value.formArray[i].Client || '', { emitEvent: false });
      // control.get('Client')?.patchValue(this.getEmployeeFullName(this.dynamicForm.value.formArray[i].Client || ''), { emitEvent: false });
      control.get('Client')?.patchValue(this.dynamicForm.value.formArray[i].Client || '', { emitEvent: false });
      control.get('StartTime')?.patchValue(this.dynamicForm.value.formArray[i].StartTime == 0 ? '' : this.dynamicForm.value.formArray[i].StartTime, { emitEvent: false });
      control.get('EndTime')?.patchValue(this.dynamicForm.value.formArray[i].EndTime == 0 ? '' : this.dynamicForm.value.formArray[i].EndTime, { emitEvent: false });
      control.get('Hours')?.patchValue(this.getDayOfHours(this.dynamicForm.value.formArray[i].StartTime, this.dynamicForm.value.formArray[i].EndTime,), { emitEvent: false });
      control.get('OTClientCode')?.patchValue(this.dynamicForm.value.formArray[i].OTClient || '', { emitEvent: false });
      // control.get('OTClient')?.patchValue(this.getEmployeeFullName(this.dynamicForm.value.formArray[i].OTClient || ''), { emitEvent: false });
      control.get('OTClient')?.patchValue(this.dynamicForm.value.formArray[i].OTClient || ''), { emitEvent: false };
      control.get('StartTimeOT')?.patchValue(this.dynamicForm.value.formArray[i].StartTimeOT == 0 ? '' : this.dynamicForm.value.formArray[i].StartTimeOT, { emitEvent: false });
      control.get('EndTimeOT')?.patchValue(this.dynamicForm.value.formArray[i].EndTimeOT == 0 ? '' : this.dynamicForm.value.formArray[i].EndTimeOT, { emitEvent: false });
      control.get('Shift2Hours')?.patchValue(this.getDayOfHours(this.dynamicForm.value.formArray[i].StartTimeOT, this.dynamicForm.value.formArray[i].EndTimeOT), { emitEvent: false });
      control.get('TimeStart')?.patchValue(this.formatDate(this.assignTimeStartValue(this.dynamicForm.value.formArray[i].StartTime, i + 1)), { emitEvent: false });
      control.get('TimeEnd')?.patchValue(this.formatDate(this.assignTimeEndValue(this.dynamicForm.value.formArray[i].EndTime, i + 1)), { emitEvent: false });
      control.get('OTTimeStart')?.patchValue(this.formatDate(this.assignOTTimeStartValue(this.dynamicForm.value.formArray[i].StartTimeOT, i + 1)), { emitEvent: false });
      control.get('OTTimeEnd')?.patchValue(this.formatDate(this.assignOTTimeEndValue(this.dynamicForm.value.formArray[i].EndTimeOT, i + 1)), { emitEvent: false });
    }
  }
  // normalTimeChange(indexValue: number) {
  //   this.subscribeToStartEndTimeChanges(indexValue);
  // }

  normalTimeChange(indexValue: number) {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    const row = formArray.at(indexValue);

    const value = row.value;

    if (
      value.StartTime != undefined && value.StartTime != '' && value.StartTime != null &&
      value.EndTime != undefined && value.EndTime != '' && value.EndTime != null
    ) {

      this.startTime = this.addLeadingZero(value.StartTime);
      this.endTime = this.addLeadingZero(value.EndTime);

      if (this.startTime != null) {
        const newDate = new Date(
          this.attendanceForm.value.AdvanceDate.getFullYear(),
          this.attendanceForm.value.AdvanceDate.getMonth(),
          value.dayField
        );
        newDate.setHours(newDate.getHours() + this.startTime);

        row.get('TimeStart')?.patchValue(newDate, { emitEvent: false });
      }

      if (this.endTime != null) {
        const newDate = new Date(
          this.attendanceForm.value.AdvanceDate.getFullYear(),
          this.attendanceForm.value.AdvanceDate.getMonth(),
          value.dayField
        );
        newDate.setHours(newDate.getHours() + this.endTime);

        row.get('TimeEnd')?.patchValue(newDate, { emitEvent: false });
      }

      if (
        this.startTime != null && this.endTime != null &&
        this.startTime != 0 && this.endTime != 0
      ) {
        if (this.startTime < this.endTime) {
          this.finalHours = this.endTime - this.startTime;
        } else if (this.startTime == this.endTime) {
          this.finalHours = 24;
        } else {
          const hour = this.startTime - this.endTime;
          this.finalHours = 24 - hour;
        }

        row.get('Hours')?.patchValue(this.finalHours, { emitEvent: false });
      } else {
        row.get('Hours')?.patchValue('', { emitEvent: false });
      }

    } else {
      this.startTime = value.StartTime;
      this.endTime = value.EndTime;
      row.get('Hours')?.patchValue('', { emitEvent: false });
    }
  }
  // shift2STimeChange(indexValue: number) {
  //   this.subscribeToShift2StartEndTimeChanges(indexValue);
  // }
  shift2STimeChange(indexValue: number) {

    const formArray = this.dynamicForm.get('formArray') as FormArray;
    const row = formArray.at(indexValue);
    const value = row.value;

    if (
      value.StartTimeOT != undefined && value.StartTimeOT != '' && value.StartTimeOT != null &&
      value.EndTimeOT != undefined && value.EndTimeOT != '' && value.EndTimeOT != null
    ) {

      this.shift2StartTime = this.addLeadingZero(value.StartTimeOT);
      this.shift2EndTime = this.addLeadingZero(value.EndTimeOT);

      if (this.shift2StartTime != null) {
        const newDate = new Date(
          this.attendanceForm.value.AdvanceDate.getFullYear(),
          this.attendanceForm.value.AdvanceDate.getMonth(),
          value.dayField
        );

        newDate.setHours(newDate.getHours() + this.shift2StartTime);

        const formattedDate = this.formatDate(newDate);

        row.get('OTTimeStart')?.patchValue(formattedDate, { emitEvent: false });
      }

      if (this.shift2EndTime != null) {
        const newDate = new Date(
          this.attendanceForm.value.AdvanceDate.getFullYear(),
          this.attendanceForm.value.AdvanceDate.getMonth(),
          value.dayField
        );

        newDate.setHours(newDate.getHours() + this.shift2EndTime);

        const formattedDate = this.formatDate(newDate);

        row.get('OTTimeEnd')?.patchValue(formattedDate, { emitEvent: false });
      }

      if (
        this.shift2StartTime != null && this.shift2EndTime != null &&
        this.shift2StartTime != 0 && this.shift2EndTime != 0
      ) {

        if (this.shift2StartTime < this.shift2EndTime) {
          this.finalHoursshift2 = this.shift2EndTime - this.shift2StartTime;
        }
        else if (this.shift2StartTime == this.shift2EndTime) {
          this.finalHoursshift2 = 24;
        }
        else {
          const hour = this.shift2StartTime - this.shift2EndTime;
          this.finalHoursshift2 = 24 - hour;
        }

        row.get('Shift2Hours')?.patchValue(this.finalHoursshift2, { emitEvent: false });

      } else {
        row.get('Shift2Hours')?.patchValue('', { emitEvent: false });
      }

    } else {
      this.shift2StartTime = value.StartTimeOT;
      this.shift2EndTime = value.EndTimeOT;
      row.get('Shift2Hours')?.patchValue('', { emitEvent: false });
    }
  }
  hoursTimeChange(indexValue: number, eventOrValue: Event | string | number) {
    let numericValue: number;

    if (eventOrValue instanceof Event) {
      const inputElement = eventOrValue.target as HTMLInputElement;
      numericValue = parseFloat(inputElement.value);
    } else if (typeof eventOrValue === 'string') {
      numericValue = parseFloat(eventOrValue);
    } else {
      numericValue = eventOrValue;
    }
    this.subscribeToStartEndTimeHoursChanges(indexValue, numericValue);
  }
  subscribeToStartEndTimeChanges(indexValue: number) {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    const subscription = formArray.valueChanges.subscribe((values) => {
      values.forEach((value: any, index: any) => {
        if (indexValue == index) {
          if (value.StartTime != undefined && value.StartTime != '' && value.StartTime != null &&
            value.EndTime != undefined && value.EndTime != '' && value.EndTime != null
          ) {
            this.startTime = this.addLeadingZero(value.StartTime);
            this.endTime = this.addLeadingZero(value.EndTime);
            if (this.startTime != null) {
              const newDate = new Date(
                this.attendanceForm.value.AdvanceDate.getFullYear(),
                this.attendanceForm.value.AdvanceDate.getMonth(),
                value.dayField
              );
              newDate.setHours(newDate.getHours() + this.startTime);
              const formattedDate = newDate;
              formArray.at(index).get('TimeStart')?.patchValue(formattedDate, { emitEvent: false });
            }
            if (this.endTime != null) {
              const newDate = new Date(
                this.attendanceForm.value.AdvanceDate.getFullYear(),
                this.attendanceForm.value.AdvanceDate.getMonth(),
                value.dayField
              );
              newDate.setHours(newDate.getHours() + this.endTime);
              const formattedDate = newDate;
              formArray.at(index).get('TimeEnd')?.patchValue(formattedDate, { emitEvent: false });
            }
            if (this.startTime != null && this.endTime != null &&
              this.startTime != 0 && this.endTime != 0) {
              if (this.startTime < this.endTime) {
                this.finalHours = this.endTime - this.startTime;
              } else if (this.startTime == this.endTime && this.startTime != 0 && this.endTime != 0) {
                this.finalHours = 24;
              } else if (this.startTime > this.endTime) {
                const hour = this.startTime - this.endTime;
                this.finalHours = 24 - hour;
              }
              formArray.at(index).get('Hours')?.patchValue(this.finalHours, { emitEvent: false });
            } else {
              formArray.at(index).get('Hours')?.patchValue('', { emitEvent: false });
            }
          } else {
            this.startTime = value.StartTime;
            this.endTime = value.EndTime;
            formArray.at(index).get('Hours')?.patchValue('', { emitEvent: false });
          }
        }

      });

      // Unsubscribe to avoid recursive calls
      subscription.unsubscribe();
    });
  }
  subscribeToShift2StartEndTimeChanges(indexValue: number) {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    const subscription = formArray.valueChanges.subscribe((values) => {
      values.forEach((value: any, index: any) => {
        if (indexValue == index) {
          if (value.StartTimeOT != undefined && value.StartTimeOT != '' && value.StartTimeOT != null &&
            value.EndTimeOT != undefined && value.EndTimeOT != '' && value.EndTimeOT != null
          ) {
            this.shift2StartTime = this.addLeadingZero(value.StartTimeOT);
            this.shift2EndTime = this.addLeadingZero(value.EndTimeOT);
            if (this.shift2StartTime != null) {
              const newDate = new Date(
                this.attendanceForm.value.AdvanceDate.getFullYear(),
                this.attendanceForm.value.AdvanceDate.getMonth(),
                value.dayField
              );
              newDate.setHours(newDate.getHours() + this.shift2StartTime);
              const formattedDate = this.formatDate(newDate);
              formArray.at(index).get('OTTimeStart')?.patchValue(formattedDate, { emitEvent: false });
            }
            if (this.shift2EndTime != null) {
              const newDate = new Date(
                this.attendanceForm.value.AdvanceDate.getFullYear(),
                this.attendanceForm.value.AdvanceDate.getMonth(),
                value.dayField
              );
              newDate.setHours(newDate.getHours() + this.shift2EndTime);
              const formattedDate = this.formatDate(newDate);
              formArray.at(index).get('OTTimeEnd')?.patchValue(formattedDate, { emitEvent: false });
            }
            if (this.shift2StartTime != null && this.shift2EndTime != null &&
              this.shift2StartTime != 0 && this.shift2EndTime != 0) {
              if (this.shift2StartTime < this.shift2EndTime) {
                this.finalHoursshift2 = this.shift2EndTime - this.shift2StartTime;
              } else if (this.shift2StartTime == this.shift2EndTime && this.shift2StartTime != 0 && this.shift2EndTime != 0) {
                this.finalHoursshift2 = 24;
              } else if (this.shift2StartTime > this.shift2EndTime) {
                const hour = this.shift2StartTime - this.shift2EndTime;
                this.finalHoursshift2 = 24 - hour;
              }
              formArray.at(index).get('Shift2Hours')?.patchValue(this.finalHoursshift2, { emitEvent: false });

            } else {
              formArray.at(index).get('Shift2Hours')?.patchValue('', { emitEvent: false });
            }
          } else {
            this.shift2StartTime = value.StartTimeOT;
            this.shift2EndTime = value.EndTimeOT;
            formArray.at(index).get('Shift2Hours')?.patchValue('', { emitEvent: false });
          }
        }
      });

      // Unsubscribe to avoid recursive calls
      subscription.unsubscribe();
    });
  }
  subscribeToStartEndTimeHoursChanges(indexValue: number, numericValue: number) {
    const formArray = this.dynamicForm.get('formArray') as FormArray;

    let startHour = 1; // base hour is 0 (00:00)
    let endHour = 0;

    if (!Number.isNaN(numericValue) && numericValue > 0) {
      endHour = startHour + numericValue;
      if (endHour > 24) {
        endHour = 0; // cap at 24
      }
    } else {
      endHour = startHour
    }

    // Update component-level startTime and endTime (as string with leading zero)
    this.startTime = this.addLeadingZero(startHour);
    this.endTime = this.addLeadingZero(endHour);

    const newDateStart = new Date(
      this.attendanceForm.value.AdvanceDate.getFullYear(),
      this.attendanceForm.value.AdvanceDate.getMonth(),
      indexValue + 1
    );
    newDateStart.setHours(startHour, 0, 0, 0);

    const newDateEnd = new Date(
      this.attendanceForm.value.AdvanceDate.getFullYear(),
      this.attendanceForm.value.AdvanceDate.getMonth(),
      indexValue + 1
    );
    newDateEnd.setHours(endHour, 0, 0, 0);

    formArray.at(indexValue).get('StartTime')?.patchValue(this.startTime)
    formArray.at(indexValue).get('EndTime')?.patchValue(this.endTime)
    formArray.at(indexValue).get('TimeStart')?.patchValue(this.formatDate(newDateStart), { emitEvent: false });
    formArray.at(indexValue).get('TimeEnd')?.patchValue(this.formatDate(newDateEnd), { emitEvent: false });

    if (Number.isNaN(numericValue) || numericValue === 0) {
      formArray.at(indexValue).get('Hours')?.patchValue('0', { emitEvent: false });
    } else {
      formArray.at(indexValue).get('Hours')?.patchValue(numericValue, { emitEvent: false });
    }
  }

  addLeadingZero(value: any) {
    return value < 10 ? '0' + value : value;
  }
  getDayOfHours(startTime: any, endTime: any): any {

    this.startTime = this.addLeadingZero(startTime);
    this.endTime = this.addLeadingZero(endTime);

    if (this.startTime != null && this.endTime != null &&
      this.startTime != 0 && this.endTime != 0) {
      if (this.startTime < this.endTime) {
        return this.endTime - this.startTime;
      } else if (this.startTime == this.endTime && this.startTime != 0 && this.endTime != 0) {
        // ✅ BUG FIX: Same start and end time means a full 24-hour shift, not 0 hours.
        return 24;
      } else if (this.startTime > this.endTime) {
        const hour = this.startTime - this.endTime;
        return 24 - hour;
      }
    }

    return '';
  }
  getDayOfHoursEdited(startTime: any, endTime: any): any {
    if (startTime != null && startTime != '' && endTime != '' && endTime != null) {
      const startTime1 = new Date(startTime);
      const endTime1 = new Date(endTime);

      const time1 = startTime1.getHours();
      const time2 = endTime1.getHours();

      // Check if the conversion was successful
      if (!isNaN(startTime1.getTime())) {
        const time1 = startTime1.getHours();
      }
      if (!isNaN(endTime1.getTime())) {
        const time2 = endTime1.getHours();
      }

      if (time1 < time2) {
        return time2 - time1
      }
      else if (time1 == time2 && time1 != 0 && time2 != 0) {
        // ✅ BUG FIX: Same start and end time means a full 24-hour shift, not 0 hours.
        return 24;
      }
      else if (time1 > time2) {
        const hour = time1 - time2
        return 24 - hour
      }
    }
    return '';
  }
  assignTimeStartValue(startTime: number, dayField: number): any {
    if (startTime != null && startTime != undefined) {
      this.startTime = this.addLeadingZero(startTime);
      if (this.startTime != null) {
        const newDate = new Date(
          this.attendanceForm.value.AdvanceDate.getFullYear(),
          this.attendanceForm.value.AdvanceDate.getMonth(),
          dayField
        );
        newDate.setHours(newDate.getHours() + this.startTime);
        const formattedDate = newDate;
        return formattedDate
      }
    } else {
      const newDate = new Date(
        this.attendanceForm.value.AdvanceDate.getFullYear(),
        this.attendanceForm.value.AdvanceDate.getMonth(),
        dayField
      );
      return newDate;
    }
    return '';
  }
  assignTimeEndValue(endTime: number, dayField: number): any {
    if (endTime != null && endTime != undefined) {
      this.endTime = this.addLeadingZero(endTime);
      if (this.endTime != null) {
        const newDate = new Date(
          this.attendanceForm.value.AdvanceDate.getFullYear(),
          this.attendanceForm.value.AdvanceDate.getMonth(),
          dayField
        );
        newDate.setHours(newDate.getHours() + this.endTime);
        const formattedDate = newDate;
        return formattedDate;
      }
    } else {
      const newDate = new Date(
        this.attendanceForm.value.AdvanceDate.getFullYear(),
        this.attendanceForm.value.AdvanceDate.getMonth(),
        dayField
      );
      return newDate;
    }
    return '';
  }
  assignOTTimeStartValue(startTimeOT: number, dayField: number): any {
    if (startTimeOT != null && startTimeOT != undefined) {
      this.shift2StartTime = this.addLeadingZero(startTimeOT);
      if (this.shift2StartTime != null) {
        const newDate = new Date(
          this.attendanceForm.value.AdvanceDate.getFullYear(),
          this.attendanceForm.value.AdvanceDate.getMonth(),
          dayField
        );
        newDate.setHours(newDate.getHours() + this.shift2StartTime);
        const formattedDate = newDate;
        return formattedDate
      }
    } else {
      const newDate = new Date(
        this.attendanceForm.value.AdvanceDate.getFullYear(),
        this.attendanceForm.value.AdvanceDate.getMonth(),
        dayField
      );
      return newDate;
    }
    return '';
  }
  assignOTTimeEndValue(endTimeOT: number, dayField: number): any {
    if (endTimeOT != null && endTimeOT != undefined) {
      this.shift2EndTime = this.addLeadingZero(endTimeOT);
      if (this.shift2EndTime != null) {
        const newDate = new Date(
          this.attendanceForm.value.AdvanceDate.getFullYear(),
          this.attendanceForm.value.AdvanceDate.getMonth(),
          dayField
        );
        newDate.setHours(newDate.getHours() + this.shift2EndTime);
        const formattedDate = newDate;
        return formattedDate;
      }
    } else {
      const newDate = new Date(
        this.attendanceForm.value.AdvanceDate.getFullYear(),
        this.attendanceForm.value.AdvanceDate.getMonth(),
        dayField
      );
      return newDate;
    }
    return '';
  }
  addingHours(value: any): any {
    if (value != '' && value != null) {
      return value + ':00:00.000';
    }
    return;
  }
  onWorkTypeChange(event: MatSelectChange, index: number) {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    const group = formArray.at(index) as FormGroup;

    if (this.employeeSelectedType == 'Guard' || this.employeeSelectedType == 'Others') {
      // Working types that require Client, Start Time, End Time:
      // 1 = General Working, 3 = Off Day Working, 5 = Holiday Working
      const workingTypeIds = [1, 3, 5];
      if (!workingTypeIds.includes(Number(event.value))) {
        // Reset fields for non-working types
        group.get('Client')?.setValue('');
        group.get('ClientCode')?.setValue('');
        group.get('StartTime')?.setValue('');
        group.get('EndTime')?.setValue('');
        group.get('Hours')?.setValue('');
      }
    }
  }

  onOTTClientChange(event: MatSelectChange, index: number) {
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    const group = formArray.at(index) as FormGroup;

    if (this.employeeSelectedType == 'Guard' || this.employeeSelectedType == 'Others') {
      if (event.value == '' || event.value == undefined) {
        // Perform any necessary actions like resetting related fields
        group.get('OTClient')?.setValue('');
        group.get('StartTimeOT')?.setValue('');
        group.get('EndTimeOT')?.setValue('');
        group.get('Shift2Hours')?.setValue('');
      }

    }
  }

  getDaysInMonth(date: string): number {
    const currentDate = new Date(date);
    return new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0).getDate();
  }

  async saveAttendance(): Promise<void> {
    const allowProceed = await this.checkAllowance();
    if (!allowProceed) {
      return; // stop if user cancels the confirm dialog
    }
    // Get allowed leave values from form.
    // When editing an existing record, the "remaining" value already has the current record's
    // leaves deducted. Add them back so the comparison reflects the true available quota.
    const annualAllowed = (this.attendanceForm.get('Annual')?.value ?? 0) + (this.attendanceID > 0 ? this.iAnnualLeave : 0);
    const medicalAllowed = (this.attendanceForm.get('Medical')?.value ?? 0) + (this.attendanceID > 0 ? this.iMedicalLeave : 0);
    const maternityAllowed = (this.attendanceForm.get('Maternity')?.value ?? 0) + (this.attendanceID > 0 ? this.iMaternityLeave : 0);
    const paternityAllowed = (this.attendanceForm.get('Paternity')?.value ?? 0) + (this.attendanceID > 0 ? this.iPaternityLeave : 0);
    const hospitalizationAllowed = (this.attendanceForm.get('Hospitalization')?.value ?? 0) + (this.attendanceID > 0 ? this.iHospitalizationLeave : 0);

    let dtAdvanceDate = this.attendanceForm.value.AdvanceDate;
    this.dtAdvanceDate = this.formatDate(
      new Date(dtAdvanceDate.getFullYear(), dtAdvanceDate.getMonth() + 1, 0)
    );

    this.showLoadingSpinner = true;
    this.attendanceModel.ID = this.attendanceForm.value.ID;
    this.attendanceModel.EmployeeID = this.attendanceForm.value.EmployeeID;
    this.attendanceModel.Branch = this.attendanceForm.value.BranchCode;
    this.attendanceModel.Period = this.dtAdvanceDate;
    this.attendanceModel.Shift2Type = this.attendanceForm.value.Shift2Type;
    this.attendanceModel.Shift2Rate = Number(this.attendanceForm.value.Shift2Rate) || 0;
    this.attendanceModel.Bonus = Number(this.attendanceForm.value.BonusAmount) || 0;

    this.attendanceModel.AllowanceDeduction = Number(
      (this.employeeSelectedType === 'Guard' || this.employeeSelectedType === 'Others')
        ? this.attendanceForm.value.AllowanceDeduction
        : this.attendanceForm.value.AllowanceDeductionStaff
    ) || 0;

    this.attendanceModel.SpecialAllowanceDeduction = Number(
      (this.employeeSelectedType === 'Guard' || this.employeeSelectedType === 'Others')
        ? this.attendanceForm.value.SpecialAllowanceDeduction
        : this.attendanceForm.value.SpecialAllowanceDeductionStaff
    ) || 0;

    this.attendanceModel.KPIDeduction = Number(this.attendanceForm.value.KPIDeduction) || 0;
    this.attendanceModel.LastUpdatedBy = this.currentUser!;
    this.attendanceModel.LastUpdate = new Date();

    this.attendanceDetailsData = [];
    const formArray = this.dynamicForm.get('formArray') as FormArray;
    this.attendanceDetailsData = formArray.value.map((formGroupValue: any) => {

      const {
        weekDay, dayField, Hours, Shift2Hours,
        StartTime, EndTime, StartTimeOT, EndTimeOT,
        ...dataWithoutUnwantedFields
      } = formGroupValue;

      // Convert work type
      dataWithoutUnwantedFields.Type = this.getWorkTypeByName(dataWithoutUnwantedFields.Type);
      dataWithoutUnwantedFields.Client = dataWithoutUnwantedFields.ClientCode;
      dataWithoutUnwantedFields.OTClient = dataWithoutUnwantedFields.OTClientCode;

      // ✅ FIX LastUpdate if invalid
      const parsedDate = new Date(dataWithoutUnwantedFields.LastUpdate);
      if (!dataWithoutUnwantedFields.LastUpdate || isNaN(parsedDate.getTime())) {
        dataWithoutUnwantedFields.LastUpdate = this.formatDate(new Date());
      } else {
        dataWithoutUnwantedFields.LastUpdate = this.formatDate(parsedDate);
      }

      return dataWithoutUnwantedFields;
    });

    // 1️⃣ Reset counters first
    this.iAbsent = 0;
    this.iAnnualLeave = 0;
    this.iMedicalLeave = 0;
    this.iMaternityLeave = 0;
    this.iPaternityLeave = 0;
    this.iHospitalizationLeave = 0;

    // 2️⃣ Loop through the attendanceData array
    this.attendanceDetailsData.forEach((row: any) => {
      // row.Type is already a number
      this.applyIncrementByType(row.Type);
    });
    // ✅ Row-level validation for Guard/Others: Client, Start Time, End Time must all be set together
    if (this.employeeSelectedType === 'Guard' || this.employeeSelectedType === 'Others') {
      const workingTypeNames = ['General Working', 'Off Day Working', 'Holiday Working'];
      const invalidRows: number[] = [];

      formArray.controls.forEach((group, index) => {
        const typeVal = group.get('Type')?.value;
        // typeVal can be a string name (set mode) or a number (edit mode numeric id)
        const typeName = typeof typeVal === 'string' ? typeVal : this.getWorkType(Number(typeVal));

        if (workingTypeNames.includes(typeName)) {
          const client = group.get('Client')?.value;
          const startTime = group.get('StartTime')?.value;
          const endTime = group.get('EndTime')?.value;

          const clientEmpty = !client || client === '' || client === '0' || client === 0;
          const startEmpty = !startTime || startTime === '' || startTime === '0' || startTime === 0;
          const endEmpty = !endTime || endTime === '' || endTime === '0' || endTime === 0;

          // Only block save when at least one field is partially filled:
          // - Client filled but Start or End Time missing → invalid
          // - Start or End Time filled but Client missing → invalid
          // - ALL three empty → allow (blank row, no working data entered)
          const anyFilled = !clientEmpty || !startEmpty || !endEmpty;
          if (anyFilled && (clientEmpty || startEmpty || endEmpty)) {
            const dayNum = group.get('dayField')?.value ?? (index + 1);
            invalidRows.push(dayNum);
          }
        }
      });

      if (invalidRows.length > 0) {
        this.showLoadingSpinner = false;
        this.showMessage(
          `Day(s) ${invalidRows.join(', ')}: Client, Start Time and End Time must all be set for working days.`,
          'warning',
          'Warning Message'
        );
        return;
      }

      // ✅ Row-level validation for Shift II: OTClient, StartTimeOT, EndTimeOT must all be set together
      const invalidShift2Rows: number[] = [];

      formArray.controls.forEach((group, index) => {
        const otClient = group.get('OTClient')?.value;
        const startTimeOT = group.get('StartTimeOT')?.value;
        const endTimeOT = group.get('EndTimeOT')?.value;

        const otClientEmpty = !otClient || otClient === '' || otClient === '0' || otClient === 0;
        const startOTEmpty = !startTimeOT || startTimeOT === '' || startTimeOT === '0' || startTimeOT === 0;
        const endOTEmpty = !endTimeOT || endTimeOT === '' || endTimeOT === '0' || endTimeOT === 0;

        // Same rule as main row: only block if partially filled
        // - Shift II Client filled but Start or End Time missing → invalid
        // - Shift II Start or End Time filled but Client missing → invalid
        // - ALL three empty → allow
        const anyShift2RowFilled = !otClientEmpty || !startOTEmpty || !endOTEmpty;
        if (anyShift2RowFilled && (otClientEmpty || startOTEmpty || endOTEmpty)) {
          const dayNum = group.get('dayField')?.value ?? (index + 1);
          invalidShift2Rows.push(dayNum);
        }
      });

      if (invalidShift2Rows.length > 0) {
        this.showLoadingSpinner = false;
        this.showMessage(
          `Day(s) ${invalidShift2Rows.join(', ')}: Shift II Client, Start Time and End Time must all be set together.`,
          'warning',
          'Warning Message'
        );
        return;
      }
    }

    // Check if taken leaves exceed allowed limits
    if (this.iAnnualLeave > annualAllowed) {
      this.showMessage('Annual Leave exceeds allowed limit.', 'warning', 'Warning Message');
      return;
    }
    if (this.iMedicalLeave > medicalAllowed) {
      this.showMessage('Medical Leave exceeds allowed limit.', 'warning', 'Warning Message');
      return;
    }
    if (this.iMaternityLeave > maternityAllowed) {
      this.showMessage('Maternity Leave exceeds allowed limit.', 'warning', 'Warning Message');
      return;
    }
    if (this.iPaternityLeave > paternityAllowed) {
      this.showMessage('Paternity Leave exceeds allowed limit.', 'warning', 'Warning Message');
      return;
    }
    if (this.iHospitalizationLeave > hospitalizationAllowed) {
      this.showMessage('Hospitalization Leave exceeds allowed limit.', 'warning', 'Warning Message');
      return;
    }
    formArray.controls.forEach((group) => {
      const startTime = group.get('StartTimeOT')?.value ?? '';
      const endTime = group.get('EndTimeOT')?.value ?? '';
      const shift2Hours = group.get('Shift2Hours')?.value ?? '';

      if (startTime && endTime && shift2Hours) {
        this.shift2StartTimeValidation = startTime
        this.shift2EndTimeValidation = endTime
        this.shift2HoursValidation = shift2Hours
      }
    });

    if (this.shift2StartTimeValidation && this.shift2EndTimeValidation && this.shift2HoursValidation
      && this.attendanceModel.Shift2Type == 3) {
      this.showMessage(`Please select Shift II type from radio button.`, 'warning', 'Warning Message');
    }
    else if (this.shift2StartTimeValidation && this.shift2EndTimeValidation && this.shift2HoursValidation
      && (this.attendanceForm.value.Shift2Rate === '' || this.attendanceForm.value.Shift2Rate == 0)) {
      this.showMessage(`Please enter Shift II rate details.`, 'warning', 'Warning Message');
    } else {
      this._payrollService.saveAndUpdateAttendance(this.attendanceModel, this.attendanceDetailsData)
        .subscribe(response => {
          if (response.Success == 'Success') {
            // Refresh the employee list and name list together via forkJoin in getEmployeeListByEmployeeType
            this.getEmployeeListByEmployeeType(this.branchCode, this.attendanceForm.value.EmployeeType, this.StartPeriod, this.EndPeriod, 'Active');
            this.clearFormFields();
            this.hideloadingSpinner();
            this.showMessage(`${response.Message}`, 'success', 'Success Message');
          }
        });
    }
  }

  onDeleteClick(): void {
    this.showLoadingSpinner = true;

    const year = new Date(this.attendancePeriod).getFullYear();
    const month = new Date(this.attendancePeriod).getMonth() + 1; // JS months are zero-based

    this._payrollService.getLatestAttendancePeriod(this.attendanceForm.value.EmployeeID, year, month)
      .subscribe({
        next: (period) => {
          if (period === null || period === '') {
            this.confirmAndDelete();
          } else {
            const formattedPeriod = this.formatDisplayDate(period);
            Swal.fire({
              title: 'Warning Message',
              html: `You cannot delete this attendance, please first delete the next month <b style="font-size: 1.2em;">(${formattedPeriod})</b> attendance.`,
              icon: 'warning',
              confirmButtonText: 'Ok'
            });
            this.hideloadingSpinner();
          }
        },
        error: (err) => {
          this.handleErrors(err);
          this.hideloadingSpinner();
        }
      });
  }

  confirmAndDelete(): void {
    this.dialog
      .open(DialogConfirmationComponent, {
        data: `Are you sure want to delete this attendance details?`
      })
      .afterClosed()
      .subscribe((result: { confirmDialog: boolean; remarks: any }) => {
        if (result.confirmDialog) {
          this._payrollService.deleteAttendance(this.attendanceID, this.currentUser!).subscribe(
            (response) => {
              if (response.Success == 'Success') {
                this.showMessage(`${response.Message}`, 'success', 'Success Message');
                this.getEmployeeListByEmployeeType(
                  this.branchCode,
                  this.attendanceForm.value.EmployeeType,
                  this.StartPeriod,
                  this.EndPeriod,
                  'Active'
                );
                this.clearFormFields();
              }
              this.hideloadingSpinner();
            },
            (error) => {
              this.handleErrors(error);
              this.hideloadingSpinner();
            }
          );
        } else {
          this.hideloadingSpinner();
        }
      });
  }

  clearFormFields() {
    this.attendanceForm.patchValue({
      ID: 0,
      EmployeeID: '',
      ClientName: '',
      EmployeeNo: '',
      StartTime: '',
      EndTime: '',
      Hours: '',
      Shif2Client: '',
      Shift2StartTime: '',
      Shift2EndTime: '',
      Shift2Hours: '',
      Passport: '',
      Age: '',
      JoinDate: '',
      ResignedDate: '',
      IncomeTax: '',
      EPF: '',
      EpfNo: '',
      Socso: '',
      PaymentMode: '',
      SalaryStructure: '',
      SalarySlab: '',
      BasicPay: '',
      Annual: '',
      Medical: '',
      Maternity: '',
      Paternity: '',
      Hospitalization: '',
      BonusAmount: 0.00,
      Shift2Type: ['3'],  // Default value
      Shift2Rate: 0.00,
      Allowance: '',
      SpecialAllowance: '',
      AllowanceDeduction: 0.00,
      SpecialAllowanceDeduction: 0.00,
      LastUpdate: this.formatDate(new Date),
      LastUpdatedBy: '',
      AllowanceDeductionStaff: 0.00,
      SpecialAllowanceDeductionStaff: 0.00,
      KPIDeduction: [0.00],
    });

    //this.employeeListModel = [];

    const formArray = this.dynamicForm.get('formArray') as FormArray;
    formArray.clear();

    // ✅ BUG FIX: Reset all component-level state that is NOT in the form.
    // Without these resets, stale values from the previously loaded employee
    // can corrupt saves for the next employee (e.g. attendanceID still pointing
    // to Employee A when saving Employee B).
    this.attendanceID = 0;
    this.attendanceDetails = [];
    this.attendanceDetailsData = [];
    this.iAbsent = 0;
    this.iAnnualLeave = 0;
    this.iMedicalLeave = 0;
    this.iMaternityLeave = 0;
    this.iPaternityLeave = 0;
    this.iHospitalizationLeave = 0;
    this.showAllowance = false;
    this.showSpecialAllowance = false;

    this.shift2StartTimeValidation = null;
    this.shift2EndTimeValidation = null;
    this.shift2HoursValidation = null;
  }

  async checkAllowance(): Promise<boolean> {
    const form = this.attendanceForm;

    const allowance = parseFloat(form.get('Allowance')?.value) || 0;
    const specialAllowance = parseFloat(form.get('SpecialAllowance')?.value) || 0;

    const isGuard = this.employeeSelectedType === 'Guard' || this.employeeSelectedType === 'Others';
    const hasPositiveAllowance = allowance > 0 || specialAllowance > 0;

    if (isGuard) {
      const allowanceDeduction = parseFloat(form.get('AllowanceDeduction')?.value) || 0;
      const specialAllowanceDeduction = parseFloat(form.get('SpecialAllowanceDeduction')?.value) || 0;

      const hasBothDeductions = allowanceDeduction > 0 && specialAllowanceDeduction > 0;

      // Warning if allowance exists but deductions are missing
      if (hasPositiveAllowance && !hasBothDeductions) {
        const result = await Swal.fire({
          title: 'Warning Message',
          text: 'Do you want to deduct from Allowance/Special Allowance?',
          icon: 'warning',
          showCancelButton: true,
          confirmButtonText: 'OK',      // Continue
          cancelButtonText: 'Cancel'    // Stay
        });
        return result.isConfirmed;
      }

      // Confirmation if both deductions exist
      if (hasBothDeductions) {
        const result = await Swal.fire({
          title: 'Confirmation',
          text:
            `RM ${allowanceDeduction.toFixed(2)} will be deducted from Allowance.\n` +
            `RM ${specialAllowanceDeduction.toFixed(2)} will be deducted from Special Allowance.\n` +
            `Do you want to continue?`,
          icon: 'warning',
          showCancelButton: true,
          confirmButtonText: 'OK',      // Continue
          cancelButtonText: 'Cancel'    // Stay
        });
        return result.isConfirmed;
      }

    } else {
      // Staff
      const allowanceDeductionStaff = parseFloat(form.get('AllowanceDeductionStaff')?.value) || 0;
      const specialAllowanceDeductionStaff = parseFloat(form.get('SpecialAllowanceDeductionStaff')?.value) || 0;

      const hasBothDeductions = allowanceDeductionStaff > 0 && specialAllowanceDeductionStaff > 0;

      // Warning if allowance exists but deductions are missing
      if (hasPositiveAllowance && !hasBothDeductions) {
        const result = await Swal.fire({
          title: 'Warning Message',
          text: 'Do you want to deduct from Allowance/Special Allowance?',
          icon: 'warning',
          showCancelButton: true,
          confirmButtonText: 'OK',      // Continue
          cancelButtonText: 'Cancel'    // Stay
        });
        return result.isConfirmed;
      }

      // Confirmation if both deductions exist
      if (hasBothDeductions) {
        const result = await Swal.fire({
          title: 'Confirmation',
          text:
            `RM ${allowanceDeductionStaff.toFixed(2)} will be deducted from Allowance.\n` +
            `RM ${specialAllowanceDeductionStaff.toFixed(2)} will be deducted from Special Allowance.\n` +
            `Do you want to continue?`,
          icon: 'warning',
          showCancelButton: true,
          confirmButtonText: 'OK',      // Continue
          cancelButtonText: 'Cancel'    // Stay
        });
        return result.isConfirmed;
      }
    }

    // Default: no deduction or no allowance involved — allow proceed
    return true;
  }
  searchDropdown(searchString: string, list: any[], key: string): any[] {
    if (!searchString) return [...list]; // if empty, return full list
    return list.filter(item => item[key].toLowerCase().includes(searchString.toLowerCase()));
  }

  // onKeyDropdown(
  //   event: KeyboardEvent,
  //   searchStringProp: 'employeeSearchString' | 'branchSearchString' | 'clientSearchString' | 'employeeClientSearchString',
  //   listProp: 'employeeListModel' | 'branchModel' | 'clientList' | 'employeeModel',
  //   filteredListProp: 'filteredEmployeeList' | 'filteredBranchList' | 'filteredClientList' | 'filteredEmployeeClientList',
  //   keyName: string,
  //   subject: Subject<string>
  // ) {
  //   const key = event.key;

  //   this[searchStringProp] = this[searchStringProp] || '';

  //   if (key.length === 1) {
  //     this[searchStringProp] += key.toLowerCase();
  //   } else if (key === 'Backspace') {
  //     this[searchStringProp] = this[searchStringProp].slice(0, -1);
  //   } else if (key === 'Escape') {
  //     this[searchStringProp] = '';
  //   }

  //   // Apply filter immediately
  //   this[listProp] = this.searchDropdown(this[searchStringProp], this[filteredListProp], keyName);

  //   // Trigger debounce to reset after 2s of inactivity
  //   subject.next(this[searchStringProp]);
  // }

  onKeyDropdown(
    event: KeyboardEvent,
    searchStringProp: string,               // e.g. 'employeeClientSearchString'
    listProp: string,                       // e.g. 'employeeModel'
    filteredListProp: string,               // e.g. 'filteredEmployeeClientList'
    keyName: string,                        // e.g. 'Shortname'
    subject: Subject<string>,
    rowIndex?: number                        // optional for FormArray rows
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

    const sourceList = this[filteredListProp] || [];

    if (rowIndex !== undefined) {
      // --- FORM ARRAY / ROW-BASED DROPDOWN ---
      if (!this.filteredDropdownRows[filteredListProp]) {
        this.filteredDropdownRows[filteredListProp] = [];
      }

      if (!this.filteredDropdownRows[filteredListProp][rowIndex]) {
        this.filteredDropdownRows[filteredListProp][rowIndex] = [...sourceList];
      }

      this.filteredDropdownRows[filteredListProp][rowIndex] = sourceList.filter((item: any) =>
        item[keyName]?.toString().toLowerCase().includes(this[searchStringProp].toLowerCase())
      );
    } else {
      // --- STANDALONE DROPDOWN ---
      this[listProp] = sourceList.filter((item: any) =>
        item[keyName]?.toString().toLowerCase().includes(this[searchStringProp].toLowerCase())
      );
    }

    // Trigger debounce reset
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
  // Helper function to get effective value (dynamic or default)
  private getEffectiveValue(value: string, dynamicValue: string): string {
    return (!value || value === '') ? dynamicValue : value;
  }
  // Helper function to compute hours
  private getComputedHours(startTime: string, endTime: string, dynamicStartTime: string, dynamicEndTime: string): number {
    return (!startTime || startTime === '') && (!endTime || endTime === '')
      ? this.getDayOfHours(dynamicStartTime, dynamicEndTime)
      : this.getDayOfHours(startTime, endTime);
  }
  private showMessage(message: string, icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message'): void {
    Swal.fire({
      toast: true,
      position: 'top',
      showConfirmButton: false,
      title: title,
      text: message,
      icon: icon, // Dynamically set the icon based on the parameter
      showCloseButton: false,
      timer: 5000,
      width: '600px',
      customClass: {
        popup: 'swal-top-offset'
      }
    });
    this.hideloadingSpinner();
    return;
  }
  hideloadingSpinner() {
    this.showLoadingSpinner = false;
  }
  handleErrors(error: string) {
    if (error != null && error != '') {
      this.errorMessage = error;
      this.hideloadingSpinner();
    }
  };
}
