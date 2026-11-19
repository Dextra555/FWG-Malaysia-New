import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
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
import { environment } from '../../../../environments/environment';

function mustSelect(): ValidatorFn { return (c: AbstractControl) => (!c.value || +c.value === 0) ? { mustSelect: true } : null; }

@Component({
  selector: 'app-asset-transfer',
  templateUrl: './asset-transfer.component.html',
  styleUrls: ['./asset-transfer.component.css']
})
export class AssetTransferComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void { if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; } }

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;
  filterValue = ''; pageSizeOptions = [10, 20, 30, 50]; showForm = false;

  displayedColumns = ['TransferNo', 'AssetCode', 'AssetName', 'FromName', 'ToName', 'TransferDate', 'TransferReason', 'action'];
  dataSource = new MatTableDataSource<any>([]);

  frm!: FormGroup;
  assetList: any[] = [];
  branchList: any[] = [];
  employeeList: any[] = [];
  filteredToList: any[] = [];
  toSearch = '';
  toTypes = ['Branch', 'Staff', 'Guard'];
  transferNo = '';
  // Search filter state for Asset and Branch
  assetSearch = '';  filteredAssets:   any[] = [];
  branchSearch = ''; filteredBranches: any[] = [];

  // Current allocation of selected asset
  currentAllocation: any = null;

  constructor(
    private fb: FormBuilder, private _liveAnnouncer: LiveAnnouncer,
    private service: InventoryService, private _payroll: PayrollModuleService,
    private _dataService: DatasharingService, private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForm();
  }

  buildForm(): void {
    this.frm = this.fb.group({
      AssetID:        [0, [Validators.required, mustSelect()]],
      TransferDate:   [new Date(), Validators.required],
      FromType:       [''], FromID: [null], FromName: [''],
      ToType:         ['', Validators.required],
      ToID:           [null],
      ToName:         ['', Validators.required],
      TransferReason: [''],
      Remarks:        ['']
    });
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => this.currentUser = u);
    this.getUserAccessRights(this.currentUser, 'Asset Transfer');
    this.loadDropdowns();
  }

  getUserAccessRights(user: string, screen: string): void {
    this._masterService.getUserAccessRights(user, screen).subscribe(d => {
      if (d) { this.showLoadingSpinner = true; this.userAccessModel.readAccess = d.Read; this.userAccessModel.createAccess = d.Create;
        if (d.Read || this.currentUser === 'superadmin') this.loadList();
        else { this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>You do not have permission to view this page.`; this.hideLoadingSpinner(); }
      }
    }, () => { this.errorMessage = 'Unable to load page.'; this.hideLoadingSpinner(); });
  }

  loadList(): void {
    this.service.getAssetTransferList().subscribe({
      next: d => { this.dataSource = new MatTableDataSource(d || []); this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; this.hideLoadingSpinner(); },
      error: () => { this.errorMessage = 'Unable to load transfer list.'; this.hideLoadingSpinner(); }
    });
  }

  loadDropdowns(): void {
    this.service.getAssetList().subscribe({ next: d => { this.assetList = (d || []).filter((a: any) => a.AssetStatus !== 'Disposed'); this.filteredAssets = [...this.assetList]; }, error: () => {} });
    this.service.getBranchListByUser(this.currentUser).subscribe({ next: d => { this.branchList = d || []; this.filteredBranches = [...this.branchList]; }, error: () => {} });
    this._payroll.getEmployeeList().subscribe({ next: d => { this.employeeList = d || []; this.filteredToList = d || []; }, error: () => {} });
  }

  onAssetChange(assetID: number): void {
    this.currentAllocation = null;
    if (!assetID || assetID === 0) return;
    this.service.getActiveAllocationsByAsset(+assetID).subscribe({
      next: (allocs: any[]) => {
        if (allocs && allocs.length > 0) {
          this.currentAllocation = allocs[0];
          this.frm.patchValue({ FromType: allocs[0].AllocatedToType, FromID: allocs[0].AllocatedToID, FromName: allocs[0].AllocatedToName });
        }
      }, error: () => {}
    });
  }

  onToTypeChange(): void { this.frm.get('ToID')?.setValue(null); this.frm.get('ToName')?.setValue(''); this.toSearch = ''; this.filteredToList = this.employeeList; }
  onToSearch(val: string): void { this.toSearch = val; const l = val.toLowerCase(); this.filteredToList = l ? this.employeeList.filter((e: any) => (e.EMP_NAME || '').toLowerCase().includes(l)) : this.employeeList; }
  onToBranchSelect(b: any): void { this.frm.get('ToID')?.setValue(null); this.frm.get('ToName')?.setValue(`${b.Code} — ${b.Name}`); }
  onToEmployeeSelect(e: any): void { this.frm.get('ToID')?.setValue(e.EMP_ID); this.frm.get('ToName')?.setValue(e.EMP_NAME); }

  get toType(): string { return this.frm.get('ToType')?.value || ''; }

  openNewForm(): void {
    this.buildForm(); this.currentAllocation = null;
    this.service.getTransferCodeAsset().subscribe({ next: d => this.transferNo = d.Code, error: () => {} });
    this.showForm = true;
  }
  cancelForm(): void { this.showForm = false; }

  /** Open Asset Transfer PDF for the given row. Read-only. */
  onPrintPDF(row: any): void {
    window.open(environment.baseReportUrl + 'Inventory/AssetTransferReport.aspx'
      + '?LoginID=' + encodeURIComponent(this.currentUser)
      + '&TransferID=' + encodeURIComponent(row.ID), '_blank');
  }
  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    this.service.saveAssetTransfer({ ...raw, AssetID: +raw.AssetID, LastUpdatedBy: this.currentUser }).subscribe({
      next: res => { this.showMessage(res?.message || 'Transfer saved.', 'success', 'Success Message'); this.showForm = false; this.loadList(); },
      error: err => { this.showMessage(this.extractMsg(err, 'Unable to save transfer.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  formatDate(v: any): string { if (!v) return '—'; return new Date(v).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }
  private extractMsg(e: any, fb: string): string { return typeof e === 'string' ? e : e?.error?.message || e?.message || fb; }
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
