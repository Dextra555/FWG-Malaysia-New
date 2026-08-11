import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { EmployeeVisaPassportListView } from 'src/app/model/EmployeeVisaPassportListView';
import { debounceTime, Subject } from 'rxjs';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-employee-visa-passport-list',
  templateUrl: './employee-visa-passport-list.component.html',
  styleUrls: ['./employee-visa-passport-list.component.css']
})
export class EmployeeVisaPassportListComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  frm!: FormGroup;
  branchList: any[] = [];
  filteredBranchList: any[] = [];
  branchSearchString: string = '';
  branchSearchSubject = new Subject<string>();

  dataSource = new MatTableDataSource<EmployeeVisaPassportListView>([]);
  displayedColumns: string[] = [
    'branch', 'empCode', 'name', 'icNo', 'passportNo',
    'category', 'dateJoined', 'visaExpiry', 'passportExpiry', 'daysLeft'
  ];
  dynamicPageSizeOptions: number[] = [];

  currentUser: string = '';
  errorMessage: string = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  searched: boolean = false;
  userAccessModel!: UserAccessModel;

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    private _masterService: MastermoduleService,
    private _dataService: DatasharingService,
    private fb: FormBuilder
  ) {
    this.userAccessModel = {
      readAccess: false, updateAccess: false,
      deleteAccess: false, createAccess: false
    };

    this.frm = this.fb.group({
      branch:       ['All', Validators.required],
      expiryType:   ['',   Validators.required],
      expiryStatus: ['',   Validators.required],
      exportOption: ['0']
    });
  }

  ngOnInit(): void {
    this.branchSearchSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.branchSearchString = '';
      this.branchList = [...this.filteredBranchList];
    });

    // Reset status + clear table when Type changes
    this.frm.get('expiryType')!.valueChanges.subscribe(() => {
      this.frm.get('expiryStatus')!.reset('');
      this.dataSource.data = [];
      this.searched = false;
    });

    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => {
        this.currentUser = u;
        this.getUserAccessRights(this.currentUser);
      });
    } else {
      this.getUserAccessRights(this.currentUser);
    }
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  getUserAccessRights(userName: string) {
    this.showLoadingSpinner = true;

    if (userName === 'superadmin' || userName === 'admin') {
      this.userAccessModel = { readAccess: true, createAccess: true, updateAccess: true, deleteAccess: true };
      this.warningMessage = '';
      this.loadBranchList();
      this.hideSpinner();
      return;
    }

    this._masterService.getUserAccessRights(userName, 'Employee Master').subscribe(
      data => {
        if (data) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.createAccess = data.Create;
          if (data.Read) {
            this.warningMessage = '';
            this.loadBranchList();
          } else {
            this.warningMessage = `Dear <B>${userName}</B>, <br>
              You do not have permissions to view this page. <br>
              Please contact administrator. Thank you`;
          }
        }
        this.hideSpinner();
      },
      error => this.handleErrors(error)
    );
  }

  loadBranchList() {
    this._masterService.GetBranchListByUserName(this.currentUser).subscribe((d: any) => {
      this.branchList = d;
      this.filteredBranchList = [...this.branchList];
    });
  }

  onSubmit() {
    this.frm.markAllAsTouched();
    if (this.frm.invalid) return;

    const branch       = this.frm.get('branch')?.value       ?? 'All';
    const expiryType   = this.frm.get('expiryType')?.value;
    const expiryStatus = this.frm.get('expiryStatus')?.value;

    this.showLoadingSpinner = true;
    this.searched = true;

    this._masterService.getEmployeeVisaPassportList(branch, expiryType, expiryStatus).subscribe(
      (data: any[]) => {
        const mapped = data.map(item => new EmployeeVisaPassportListView(item));
        this.dataSource.data = mapped;
        this.generatePageSizeOptions(mapped);
        // Defer paginator/sort binding — *ngIf renders them after data is set
        setTimeout(() => {
          this.dataSource.paginator = this.paginator;
          this.dataSource.sort = this.sort;
          if (this.paginator) this.paginator.firstPage();
        });
        this.hideSpinner();
      },
      error => this.handleErrors(error)
    );
  }

  /** Returns the relevant days-left number based on selected type */
  getDaysLeft(row: EmployeeVisaPassportListView): number | null {
    const type = this.frm.get('expiryType')?.value;
    return type === 'Visa' ? row.DaysToVisaExpiry : row.DaysToPassportExpiry;
  }

  /** Colours: red = expired/≤30d, orange = ≤90d, green = safe */
  getDaysLeftColor(row: EmployeeVisaPassportListView): string {
    const days = this.getDaysLeft(row);
    if (days === null) return '';
    if (days < 0)   return 'red';
    if (days <= 30) return 'red';
    if (days <= 90) return 'orange';
    return 'green';
  }

  announceSortChange(sortState: Sort) {
    if (sortState.direction) {
      this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this._liveAnnouncer.announce('Sorting cleared');
    }
  }

  generatePageSizeOptions(data: any[]) {
    const total = data.length;
    const base = [10, 25, 50, 100];
    // Keep only options that are <= total, always include 10 as minimum
    const opts = base.filter(n => n <= total);
    if (opts.length === 0) opts.push(10);
    this.dynamicPageSizeOptions = opts;
  }

  exportToExcel() {
    const opt = this.frm.get('exportOption')?.value;
    let rows: any[] = [];

    if (opt === '0') {
      rows = this.dataSource.filteredData.slice(
        this.paginator.pageIndex * this.paginator.pageSize,
        (this.paginator.pageIndex + 1) * this.paginator.pageSize
      );
    } else if (opt === '1') {
      rows = this.dataSource.filteredData;
    } else if (opt === '2') {
      rows = this.dataSource.filteredData.slice(0, 100);
    }

    if (rows.length === 0) { alert('No data to export.'); return; }

    const ws: XLSX.WorkSheet = XLSX.utils.json_to_sheet(rows);
    const wb: XLSX.WorkBook  = { Sheets: { data: ws }, SheetNames: ['data'] };
    const type = this.frm.get('expiryType')?.value ?? 'List';
    XLSX.writeFile(wb, `Employee_${type}_List.xlsx`);
  }

  searchDropdown(searchString: string, list: any[], key: string): any[] {
    if (!searchString) return [...list];
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
    if (key.length === 1)        this[searchStringProp] += key.toLowerCase();
    else if (key === 'Backspace') this[searchStringProp] = this[searchStringProp].slice(0, -1);
    else if (key === 'Escape')    this[searchStringProp] = '';
    this[listProp] = this.searchDropdown(this[searchStringProp], this[filteredListProp], keyName);
    subject.next(this[searchStringProp]);
  }

  handleErrors(error: string) {
    if (error) this.hideSpinner();
  }

  hideSpinner() {
    this.showLoadingSpinner = false;
  }
}
