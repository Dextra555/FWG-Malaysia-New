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
import { UserAccessModel } from '../../../model/userAccesModel';

function mustSelect(): ValidatorFn { return (c: AbstractControl) => (+c.value === 0 || !c.value) ? { mustSelect: true } : null; }

@Component({ selector: 'app-asset-disposal', templateUrl: './asset-disposal.component.html', styleUrls: ['./asset-disposal.component.css'] })
export class AssetDisposalComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void { if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; } }

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;
  filterValue = ''; pageSizeOptions = [10, 20, 30, 50]; showForm = false;
  displayedColumns = ['DisposalNo', 'AssetCode', 'AssetName', 'DisposalDate', 'DisposalType', 'DisposalReason', 'DisposalValue', 'ApprovedBy', 'action'];
  dataSource = new MatTableDataSource<any>([]);
  frm!: FormGroup;
  assetList: any[] = [];
  disposalTypes = ['Sold', 'Scrapped', 'Donated', 'Written_Off'];
  disposalNo = '';
  // Search filter state
  assetSearch = ''; filteredAssets: any[] = [];

  constructor(private fb: FormBuilder, private _liveAnnouncer: LiveAnnouncer,
    private service: InventoryService, private _dataService: DatasharingService, private _masterService: MastermoduleService) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForm();
  }

  buildForm(): void {
    this.frm = this.fb.group({
      AssetID: [0, [Validators.required, mustSelect()]],
      DisposalDate: [new Date(), Validators.required],
      DisposalType: ['', Validators.required],
      DisposalReason: ['', Validators.required],
      DisposalValue: [0],
      ApprovedBy: [''],
      Remarks: ['']
    });
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    this.getUserAccessRights(this.currentUser, 'Asset Disposal');
    // Only show assets that are NOT already disposed
    this.service.getAssetList().subscribe({ next: d => { this.assetList = (d || []).filter((a: any) => a.AssetStatus !== 'Disposed'); this.filteredAssets = [...this.assetList]; }, error: () => {} });
  }

  getUserAccessRights(user: string, screen: string): void {
    this._masterService.getUserAccessRights(user, screen).subscribe(d => {
      if (d) { this.showLoadingSpinner = true; this.userAccessModel.readAccess = d.Read; this.userAccessModel.createAccess = d.Create;
        if (d.Read || this.currentUser === 'superadmin') this.loadList();
        else { this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>No permission.`; this.hideLoadingSpinner(); }
      }
    }, () => { this.errorMessage = 'Unable to load page.'; this.hideLoadingSpinner(); });
  }

  loadList(): void {
    this.service.getAssetDisposalList().subscribe({
      next: d => { this.dataSource = new MatTableDataSource(d || []); this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; this.hideLoadingSpinner(); },
      error: () => { this.errorMessage = 'Unable to load disposal list.'; this.hideLoadingSpinner(); }
    });
  }

  openNewForm(): void {
    this.buildForm();
    this.service.getDisposalCode().subscribe({ next: d => { this.disposalNo = d.Code; }, error: () => {} });
    this.showForm = true;
  }

  cancelForm(): void { this.showForm = false; }
  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    this.service.saveAssetDisposal({ ...raw, AssetID: +raw.AssetID, LastUpdatedBy: this.currentUser }).subscribe({
      next: res => { this.showMsg(res?.message || 'Asset disposed.', 'success', 'Success Message'); this.showForm = false; this.loadList(); },
      error: err => { this.showMsg(this.extractMsg(err, 'Unable to save disposal record.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  formatDate(v: any): string { if (!v) return '—'; return new Date(v).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }
  formatNumber(v: number): string { return (v ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
  private extractMsg(e: any, fb: string): string { return (e && typeof e === 'string') ? e : e?.error?.message || e?.message || fb; }
  private showMsg(msg: string, icon: 'success'|'error'|'warning'|'info', title: 'Success Message'|'Error Message'|'Warning Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: msg, icon, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }
  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onAssetOpen(opened: boolean) { if (opened) { this.assetSearch = ''; this.filteredAssets = [...this.assetList]; } }
  onAssetSearch(q: string) { this.assetSearch = q; const s = q.toLowerCase(); this.filteredAssets = this.assetList.filter(a => a.Name?.toLowerCase().includes(s) || a.AssetCode?.toLowerCase().includes(s)); }
}
