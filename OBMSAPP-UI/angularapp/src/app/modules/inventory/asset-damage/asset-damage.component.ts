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

@Component({ selector: 'app-asset-damage', templateUrl: './asset-damage.component.html', styleUrls: ['./asset-damage.component.css'] })
export class AssetDamageComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void { if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; } }

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;
  filterValue = ''; pageSizeOptions = [10, 20, 30, 50]; showForm = false;
  displayedColumns = ['DamageNo', 'AssetCode', 'AssetName', 'DamageDate', 'DamageType', 'DamageCondition', 'ReportedBy', 'EstimatedRepairCost', 'action'];
  dataSource = new MatTableDataSource<any>([]);
  frm!: FormGroup;
  assetList: any[] = [];
  damageTypes = ['Physical', 'Water', 'Electrical', 'Fire', 'Other'];
  damageConditions = ['Repairable', 'Beyond_Repair'];
  damageNo = '';
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
      DamageDate: [new Date(), Validators.required],
      DamageType: ['Physical'],
      DamageDescription: ['', Validators.required],
      DamageCondition: ['Repairable'],
      ReportedBy: [''],
      EstimatedRepairCost: [0],
      Remarks: ['']
    });
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    this.getUserAccessRights(this.currentUser, 'Asset Damage');
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
    this.service.getAssetDamageList(0).subscribe({
      next: d => { this.dataSource = new MatTableDataSource(d || []); this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; this.hideLoadingSpinner(); },
      error: () => { this.errorMessage = 'Unable to load damage list.'; this.hideLoadingSpinner(); }
    });
  }

  openNewForm(): void {
    this.buildForm();
    this.service.getDamageCode().subscribe({ next: d => { this.damageNo = d.Code; }, error: () => {} });
    this.showForm = true;
  }

  cancelForm(): void { this.showForm = false; }
  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    this.service.saveAssetDamage({ ...raw, AssetID: +raw.AssetID, LastUpdatedBy: this.currentUser }).subscribe({
      next: res => { this.showMsg(res?.message || 'Damage recorded.', 'success', 'Success Message'); this.showForm = false; this.loadList(); },
      error: err => { this.showMsg(this.extractMsg(err, 'Unable to save damage record.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
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
