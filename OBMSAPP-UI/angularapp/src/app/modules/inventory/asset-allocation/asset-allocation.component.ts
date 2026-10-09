import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatDialog } from '@angular/material/dialog';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { PayrollModuleService } from '../../../service/payrollmodule.service';
import { DialogConfirmationComponent } from '../../../components/dialog-confirmation/dialog-confirmation.component';
import { UserAccessModel } from '../../../model/userAccesModel';

function mustSelect(): ValidatorFn {
  return (c: AbstractControl) => (+c.value === 0 || !c.value) ? { mustSelect: true } : null;
}

@Component({
  selector: 'app-asset-allocation',
  templateUrl: './asset-allocation.component.html',
  styleUrls: ['./asset-allocation.component.css']
})
export class AssetAllocationComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void { if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; } }

  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;
  filterValue = '';
  pageSizeOptions = [10, 20, 30, 50];
  showForm = false;

  displayedColumns = ['AllocationNo', 'AssetCode', 'AssetName', 'AllocatedToType', 'AllocatedToName', 'AllocationDate', 'IsActive', 'action'];
  dataSource = new MatTableDataSource<any>([]);

  frm!: FormGroup;
  assetList: any[] = [];
  branchList: any[] = [];
  employeeList: any[] = [];
  filteredEmployeeList: any[] = [];
  employeeSearch = '';
  // Search filter state for Asset and Branch
  assetSearch = '';  filteredAssets:   any[] = [];
  branchSearch = ''; filteredBranches: any[] = [];
  allocationTypes = ['Branch', 'Staff', 'Guard'];
  allocationNo = '';

  constructor(
    private fb: FormBuilder,
    private _liveAnnouncer: LiveAnnouncer,
    public dialog: MatDialog,
    private service: InventoryService,
    private _payroll: PayrollModuleService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForm();
  }

  buildForm(): void {
    this.frm = this.fb.group({
      AssetID:          [0, [Validators.required, mustSelect()]],
      AllocationDate:   [new Date(), Validators.required],
      AllocatedToType:  ['', Validators.required],
      AllocatedToID:    [null],
      AllocatedToName:  ['', Validators.required],
      BranchCode:       [''],
      Remarks:          ['']
    });
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    this.getUserAccessRights(this.currentUser, 'Asset Allocation');
    this.loadDropdowns();
  }

  getUserAccessRights(user: string, screen: string): void {
    this._masterService.getUserAccessRights(user, screen).subscribe(d => {
      if (d) {
        this.showLoadingSpinner = true;
        this.userAccessModel.readAccess   = d.Read;
        this.userAccessModel.deleteAccess = d.Delete;
        this.userAccessModel.updateAccess = d.Update;
        this.userAccessModel.createAccess = d.Create;
        if (d.Read || this.currentUser === 'superadmin') this.loadList();
        else { this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>You do not have permission to view this page.`; this.hideLoadingSpinner(); }
      }
    }, () => { this.errorMessage = 'Unable to load page right now.'; this.hideLoadingSpinner(); });
  }

  loadList(): void {
    this.service.getAssetAllocationList(false).subscribe({
      next: d => { this.dataSource = new MatTableDataSource(d || []); this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; this.hideLoadingSpinner(); },
      error: () => { this.errorMessage = 'Unable to load allocation list.'; this.hideLoadingSpinner(); }
    });
  }

  loadDropdowns(): void {
    this.service.getAssetList().subscribe({ next: d => { this.assetList = (d || []).filter((a: any) => a.AssetStatus !== 'Disposed'); this.filteredAssets = [...this.assetList]; }, error: () => {} });
    this.service.getBranchListByUser(this.currentUser).subscribe({ next: d => { this.branchList = d || []; this.filteredBranches = [...this.branchList]; }, error: () => {} });
    this._payroll.getEmployeeList().subscribe({ next: d => { this.employeeList = d || []; this.filteredEmployeeList = this.employeeList; }, error: () => {} });
  }

  onAllocTypeChange(): void {
    this.frm.get('AllocatedToID')?.setValue(null);
    this.frm.get('AllocatedToName')?.setValue('');
    this.frm.get('BranchCode')?.setValue('');
    this.employeeSearch = '';
    this.filteredEmployeeList = this.employeeList;
  }

  onEmployeeSearch(val: string): void {
    this.employeeSearch = val;
    const l = val.toLowerCase();
    this.filteredEmployeeList = l ? this.employeeList.filter((e: any) => (e.EMP_NAME || '').toLowerCase().includes(l) || String(e.EMP_ID).includes(l)) : this.employeeList;
  }

  onEmployeeSelect(emp: any): void {
    this.frm.get('AllocatedToID')?.setValue(emp.EMP_ID);
    this.frm.get('AllocatedToName')?.setValue(emp.EMP_NAME);
  }

  onBranchSelect(branch: any): void {
    this.frm.get('AllocatedToName')?.setValue(`${branch.Code} — ${branch.Name}`);
    this.frm.get('BranchCode')?.setValue(branch.Code);
  }

  openNewForm(): void {
    this.buildForm();
    this.service.getAllocationCode().subscribe({ next: d => { this.allocationNo = d.Code; }, error: () => {} });
    this.showForm = true;
  }

  cancelForm(): void { this.showForm = false; }

  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }

  get allocType(): string { return this.frm.get('AllocatedToType')?.value || ''; }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const payload = { ...raw, AssetID: +raw.AssetID, LastUpdatedBy: this.currentUser };
    this.service.saveAssetAllocation(payload).subscribe({
      next: res => { this.showMessage(res?.message || 'Allocation saved.', 'success', 'Success Message'); this.showForm = false; this.loadList(); },
      error: err => { this.showMessage(this.extractMsg(err, 'Unable to save allocation.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  cancelAllocation(row: any): void {
    this.dialog.open(DialogConfirmationComponent, { data: { message: `Cancel allocation <b>${row.AllocationNo}</b>? The asset will be returned to Available status.` } })
      .afterClosed().subscribe((r: any) => {
        if (r?.confirmDialog) {
          this.showLoadingSpinner = true;
          this.service.cancelAssetAllocation(row.ID, this.currentUser).subscribe({
            next: res => { this.showMessage(res?.message || 'Allocation cancelled.', 'success', 'Success Message'); this.loadList(); },
            error: err => { this.showMessage(this.extractMsg(err, 'Unable to cancel allocation.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
          });
        }
      });
  }

  formatDate(v: any): string { if (!v) return '—'; return new Date(v).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }
  private extractMsg(err: any, fb: string): string { return (err && typeof err === 'string') ? err : err?.error?.message || err?.message || fb; }
  private showMessage(msg: string, icon: 'success'|'error'|'warning'|'info', title: 'Success Message'|'Error Message'|'Warning Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: msg, icon, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }
  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers (Asset + Branch; Staff/Guard already have inline search) ──
  onAssetOpen(opened: boolean)  { if (opened) { this.assetSearch  = ''; this.filteredAssets   = [...this.assetList];  } }
  onBranchOpen(opened: boolean) { if (opened) { this.branchSearch = ''; this.filteredBranches = [...this.branchList]; } }
  onAssetSearch(q: string)  { this.assetSearch  = q; const s = q.toLowerCase(); this.filteredAssets   = this.assetList.filter(a  => a.Name?.toLowerCase().includes(s) || a.AssetCode?.toLowerCase().includes(s)); }
  onBranchSearch(q: string) { this.branchSearch = q; const s = q.toLowerCase(); this.filteredBranches = this.branchList.filter(b => b.Name?.toLowerCase().includes(s) || b.Code?.toLowerCase().includes(s)); }
}
