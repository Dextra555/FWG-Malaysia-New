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

  displayedColumns = ['MaintenanceNo', 'AssetCode', 'AssetName', 'MaintenanceType', 'StartDate', 'VendorName', 'Status', 'MaintenanceCost', 'action'];
  dataSource = new MatTableDataSource<any>([]);

  frm!: FormGroup;
  completeForm!: FormGroup;
  assetList: any[] = [];
  maintTypes = ['Preventive', 'Corrective', 'Emergency'];
  maintNo = '';
  // Search filter state
  assetSearch = ''; filteredAssets: any[] = [];

  constructor(
    private fb: FormBuilder, private _liveAnnouncer: LiveAnnouncer,
    public dialog: MatDialog,
    private service: InventoryService,
    private _dataService: DatasharingService, private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForms();
  }

  buildForms(): void {
    this.frm = this.fb.group({
      AssetID:           [0, [Validators.required, mustSelect()]],
      MaintenanceType:   ['', Validators.required],
      ProblemDescription:[''],
      VendorName:        [''],
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
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    this.getUserAccessRights(this.currentUser, 'Asset Maintenance');
    this.service.getAssetList().subscribe({ next: d => { this.assetList = (d || []).filter((a: any) => a.AssetStatus !== 'Disposed'); this.filteredAssets = [...this.assetList]; }, error: () => {} });
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
    this.buildForms(); this.selectedMaint = null;
    this.service.getMaintenanceCode().subscribe({ next: d => { this.maintNo = d.Code; }, error: () => {} });
    this.showForm = true; this.showCompleteForm = false;
  }

  openCompleteForm(row: any): void {
    this.selectedMaint = row;
    this.completeForm.patchValue({ CompletionDate: new Date(), Cost: row.MaintenanceCost || 0, Remarks: '' });
    this.showCompleteForm = true; this.showForm = false;
  }

  cancelForm(): void { this.showForm = false; this.showCompleteForm = false; }

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
    this.service.saveAssetMaintenance({ ...raw, AssetID: +raw.AssetID, LastUpdatedBy: this.currentUser }).subscribe({
      next: res => { this.showMessage(res?.message || 'Maintenance record created.', 'success', 'Success Message'); this.showForm = false; this.loadList(); },
      error: err => { this.showMessage(this.extractMsg(err, 'Unable to save maintenance record.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  onComplete(): void {
    if (this.completeForm.invalid) { this.completeForm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.completeForm.getRawValue();
    this.service.completeAssetMaintenance(this.selectedMaint.ID, { ...raw, CurrentUser: this.currentUser }).subscribe({
      next: res => { this.showMessage(res?.message || 'Maintenance completed.', 'success', 'Success Message'); this.showCompleteForm = false; this.loadList(); },
      error: err => { this.showMessage(this.extractMsg(err, 'Unable to complete maintenance.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  formatDate(v: any): string { if (!v) return '—'; return new Date(v).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }
  formatNumber(v: number): string { return (v ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
  private extractMsg(e: any, fb: string): string { return (e && typeof e === 'string') ? e : e?.error?.message || e?.message || fb; }
  private showMessage(msg: string, icon: 'success'|'error'|'warning'|'info', title: 'Success Message'|'Error Message'|'Warning Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: msg, icon, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }
  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onAssetOpen(opened: boolean) { if (opened) { this.assetSearch = ''; this.filteredAssets = [...this.assetList]; } }
  onAssetSearch(q: string) { this.assetSearch = q; const s = q.toLowerCase(); this.filteredAssets = this.assetList.filter(a => a.Name?.toLowerCase().includes(s) || a.AssetCode?.toLowerCase().includes(s)); }
}
