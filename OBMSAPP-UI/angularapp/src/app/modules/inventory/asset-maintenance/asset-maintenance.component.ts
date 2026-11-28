import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatDialog } from '@angular/material/dialog';
import Swal from 'sweetalert2';
import { Router } from '@angular/router';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { DialogConfirmationComponent } from '../../../components/dialog-confirmation/dialog-confirmation.component';
import { UserAccessModel } from '../../../model/userAccesModel';
import { environment } from '../../../../environments/environment';

function mustSelect(): ValidatorFn { return (c: AbstractControl) => (+c.value === 0 || !c.value) ? { mustSelect: true } : null; }

@Component({
  selector: 'app-asset-maintenance',
  templateUrl: './asset-maintenance.component.html',
  styleUrls: ['./asset-maintenance.component.css']
})
export class AssetMaintenanceComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void { if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; } }

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;
  filterValue = ''; pageSizeOptions = [10, 20, 30, 50]; showForm = false; showCompleteForm = false;
  selectedMaint: any = null;
  editMaintID = 0;  // 0 = new, >0 = edit mode
  editIsPaid = false; // true = Completed + already paid → amount field locked

  displayedColumns = ['MaintenanceNo', 'AssetCode', 'AssetName', 'MaintenanceType', 'StartDate', 'VendorName', 'Status', 'MaintenanceCost', 'action'];
  dataSource = new MatTableDataSource<any>([]);

  frm!: FormGroup;
  completeForm!: FormGroup;
  assetList: any[] = [];
  supplierList: any[] = [];
  branchList: any[] = [];
  maintTypes = ['Preventive', 'Corrective', 'Emergency'];
  maintNo = '';
  // Search filter state
  assetSearch = ''; filteredAssets: any[] = [];

  constructor(
    private fb: FormBuilder, private _liveAnnouncer: LiveAnnouncer,
    public dialog: MatDialog,
    private service: InventoryService,
    private _dataService: DatasharingService, private _masterService: MastermoduleService,
    private router: Router,
    private titleService: Title) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForms();
  }

  buildForms(): void {
    this.frm = this.fb.group({
      ID:                [0],
      AssetID:           [0, [Validators.required, mustSelect()]],
      MaintenanceType:   ['', Validators.required],
      ProblemDescription:[''],
      VendorName:        ['', Validators.required],
      BranchName:        ['', Validators.required],
      StartDate:         [new Date(), Validators.required],
      MaintenanceCost:   [0],
      Remarks:           ['']
    });
    this.completeForm = this.fb.group({
      CompletionDate: [new Date(), Validators.required],
      Cost:           [0],
      Remarks:        ['']
    });
  }

  ngOnInit(): void {
    this.titleService.setTitle('Asset Maintenance');
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    this.getUserAccessRights(this.currentUser, 'Asset Maintenance');
    this.service.getAssetList().subscribe({ next: d => { this.assetList = (d || []).filter((a: any) => a.AssetStatus !== 'Disposed'); this.filteredAssets = [...this.assetList]; }, error: () => {} });
    this.service.getSuppliers().subscribe({ next: d => { this.supplierList = d || []; }, error: () => {} });
    this.service.getBranchListByUser(this.currentUser).subscribe({ next: d => { this.branchList = d || []; }, error: () => {} });
  }

  getUserAccessRights(user: string, screen: string): void {
    this._masterService.getUserAccessRights(user, screen).subscribe(d => {
      if (d) { this.showLoadingSpinner = true; this.userAccessModel.readAccess = d.Read; this.userAccessModel.createAccess = d.Create; this.userAccessModel.updateAccess = d.Update;
        if (d.Read || this.currentUser === 'superadmin') this.loadList();
        else { this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>No permission.`; this.hideLoadingSpinner(); }
      }
    }, () => { this.errorMessage = 'Unable to load page.'; this.hideLoadingSpinner(); });
  }

  loadList(): void {
    this.service.getAssetMaintenanceList(0).subscribe({
      next: d => { this.dataSource = new MatTableDataSource(d || []); this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; this.hideLoadingSpinner(); },
      error: () => { this.errorMessage = 'Unable to load maintenance list.'; this.hideLoadingSpinner(); }
    });
  }

  openNewForm(): void {
    this.buildForms(); this.selectedMaint = null; this.editMaintID = 0;
    this.service.getMaintenanceCode().subscribe({ next: d => { this.maintNo = d.Code; }, error: () => {} });
    this.showForm = true; this.showCompleteForm = false;
  }

  openEditForm(row: any): void {
    this.buildForms();
    this.editMaintID = row.ID;
    this.editIsPaid  = row.IsPaid === true;
    this.maintNo = row.MaintenanceNo;
    this.selectedMaint = row;

    this.frm.patchValue({
      ID:                row.ID,
      AssetID:           row.AssetID,
      MaintenanceType:   row.MaintenanceType,
      ProblemDescription:row.ProblemDescription || '',
      VendorName:        row.VendorName || '',
      BranchName:        row.BranchName || '',
      StartDate:         row.StartDate ? new Date(row.StartDate) : new Date(),
      MaintenanceCost:   row.MaintenanceCost || 0,
      Remarks:           row.Remarks || ''
    });

    // If already paid → lock amount field
    if (this.editIsPaid) {
      this.frm.get('MaintenanceCost')?.disable();
    }

    // Sync asset search filtered list so selected asset shows
    this.filteredAssets = [...this.assetList];

    this.showForm = true; this.showCompleteForm = false;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  openCompleteForm(row: any): void {
    this.selectedMaint = row;
    this.completeForm.patchValue({ CompletionDate: new Date(), Cost: row.MaintenanceCost || 0, Remarks: '' });
    this.showCompleteForm = true; this.showForm = false;
  }

  cancelForm(): void { this.showForm = false; this.showCompleteForm = false; this.editMaintID = 0; this.editIsPaid = false; }

  /** Open Asset Maintenance PDF for the given row. Read-only. */
  onPrintPDF(row: any): void {
    window.open(environment.baseReportUrl + 'Inventory/AssetMaintenanceReport.aspx'
      + '?LoginID=' + encodeURIComponent(this.currentUser)
      + '&MaintenanceID=' + encodeURIComponent(row.ID), '_blank');
  }
  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }

  statusBadge(s: string): string {
    if (s === 'Completed') return 'badge-success';
    if (s === 'In_Progress') return 'badge-warning';
    if (s === 'Scheduled') return 'badge-info';
    return 'badge-secondary';
  }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const payload = { ...raw, AssetID: +raw.AssetID, ID: this.editMaintID, LastUpdatedBy: this.currentUser };
    this.service.saveAssetMaintenance(payload).subscribe({
      next: res => {
        const msg = this.editMaintID > 0
          ? (res?.message || 'Maintenance record updated.')
          : (res?.message || 'Maintenance record created.');
        this.showMessage(msg, 'success', 'Success Message');
        this.showForm = false; this.editMaintID = 0; this.editIsPaid = false; this.loadList();
      },
      error: err => { this.showMessage(this.extractMsg(err, 'Unable to save maintenance record.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  onComplete(): void {
    if (this.completeForm.invalid) { this.completeForm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.completeForm.getRawValue();
    const maintID = this.selectedMaint.ID;
    this.service.completeAssetMaintenance(maintID, { ...raw, CurrentUser: this.currentUser }).subscribe({
      next: res => {
        this.showMessage(res?.message || 'Maintenance completed.', 'success', 'Success Message');
        this.showCompleteForm = false;
        this.loadList();
      },
      error: err => { this.showMessage(this.extractMsg(err, 'Unable to complete maintenance.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  formatDate(v: any): string { if (!v) return '-'; return new Date(v).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }
  formatNumber(v: number): string { return (v ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
  private extractMsg(e: any, fb: string): string { return (e && typeof e === 'string') ? e : e?.error?.message || e?.message || fb; }
  private showMessage(msg: string, icon: 'success'|'error'|'warning'|'info', title: 'Success Message'|'Error Message'|'Warning Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: msg, icon, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }
  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  // â”€â”€ Searchable dropdown helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  onAssetOpen(opened: boolean) { if (opened) { this.assetSearch = ''; this.filteredAssets = [...this.assetList]; } }
  onAssetSearch(q: string) { this.assetSearch = q; const s = q.toLowerCase(); this.filteredAssets = this.assetList.filter(a => a.Name?.toLowerCase().includes(s) || a.AssetCode?.toLowerCase().includes(s)); }
}

