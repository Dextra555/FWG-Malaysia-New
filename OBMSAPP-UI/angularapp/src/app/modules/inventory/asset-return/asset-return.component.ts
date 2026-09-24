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

@Component({
  selector: 'app-asset-return',
  templateUrl: './asset-return.component.html',
  styleUrls: ['./asset-return.component.css']
})
export class AssetReturnComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void { if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; } }

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;
  filterValue = ''; pageSizeOptions = [10, 20, 30, 50]; showForm = false;

  displayedColumns = ['ReturnNo', 'AssetCode', 'AssetName', 'ReturnDate', 'ReturnedByName', 'ReturnCondition', 'ReceivedBy', 'action'];
  dataSource = new MatTableDataSource<any>([]);

  frm!: FormGroup;
  allocatedAssets: any[] = []; // assets with active allocation
  currentAlloc: any = null;
  returnNo = '';
  conditions = ['Good', 'Fair', 'Poor', 'Damaged'];
  // Search filter state
  assetSearch = ''; filteredAssets: any[] = [];

  constructor(
    private fb: FormBuilder, private _liveAnnouncer: LiveAnnouncer,
    private service: InventoryService,
    private _dataService: DatasharingService, private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForm();
  }

  buildForm(): void {
    this.frm = this.fb.group({
      AssetID:         [0, [Validators.required, mustSelect()]],
      AllocationID:    [0, [Validators.required, mustSelect()]],
      ReturnDate:      [new Date(), Validators.required],
      ReturnedByType:  [''], ReturnedByID: [null], ReturnedByName: [''],
      ReturnCondition: ['Good', Validators.required],
      ReceivedBy:      [''],
      Remarks:         ['']
    });
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    this.getUserAccessRights(this.currentUser, 'Asset Return');
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
    this.service.getAssetReturnList().subscribe({
      next: d => { this.dataSource = new MatTableDataSource(d || []); this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; this.hideLoadingSpinner(); },
      error: () => { this.errorMessage = 'Unable to load return list.'; this.hideLoadingSpinner(); }
    });
  }

  openNewForm(): void {
    this.buildForm(); this.currentAlloc = null;
    // Load assets that have active allocations
    this.service.getAssetAllocationList(true).subscribe({ next: d => { this.allocatedAssets = d || []; this.filteredAssets = [...this.allocatedAssets]; }, error: () => {} });
    this.service.getReturnCodeAsset().subscribe({ next: d => { this.returnNo = d.Code; }, error: () => {} });
    this.showForm = true;
  }

  onAssetChange(assetID: number): void {
    this.currentAlloc = null;
    this.frm.get('AllocationID')?.setValue(0);
    if (!assetID || assetID === 0) return;
    const alloc = this.allocatedAssets.find((a: any) => a.AssetID === +assetID);
    if (alloc) {
      this.currentAlloc = alloc;
      this.frm.patchValue({ AllocationID: alloc.ID, ReturnedByType: alloc.AllocatedToType, ReturnedByID: alloc.AllocatedToID, ReturnedByName: alloc.AllocatedToName });
    }
  }

  cancelForm(): void { this.showForm = false; }
  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    this.service.saveAssetReturn({ ...raw, AssetID: +raw.AssetID, AllocationID: +raw.AllocationID, LastUpdatedBy: this.currentUser }).subscribe({
      next: res => { this.showMessage(res?.message || 'Return saved.', 'success', 'Success Message'); this.showForm = false; this.loadList(); },
      error: err => { this.showMessage(this.extractMsg(err, 'Unable to save return.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
    });
  }

  formatDate(v: any): string { if (!v) return '—'; return new Date(v).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }
  private extractMsg(e: any, fb: string): string { return (e && typeof e === 'string') ? e : e?.error?.message || e?.message || fb; }
  private showMessage(msg: string, icon: 'success'|'error'|'warning'|'info', title: 'Success Message'|'Error Message'|'Warning Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: msg, icon, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }
  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onAssetOpen(opened: boolean) { if (opened) { this.assetSearch = ''; this.filteredAssets = [...this.allocatedAssets]; } }
  onAssetSearch(q: string) { this.assetSearch = q; const s = q.toLowerCase(); this.filteredAssets = this.allocatedAssets.filter(a => a.AssetName?.toLowerCase().includes(s) || a.AssetCode?.toLowerCase().includes(s) || a.AllocatedToName?.toLowerCase().includes(s)); }
}
