import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({
  selector: 'app-asset-location',
  templateUrl: './asset-location.component.html',
  styleUrls: ['./asset-location.component.css']
})
export class AssetLocationComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void {
    if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; }
  }

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = '';
  showLoadingSpinner = false;

  // â”€â”€ Mode control â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  // mode: 'defect-list' | 'update-form' | 'location-log'
  mode: 'defect-list' | 'update-form' | 'location-log' = 'defect-list';

  // â”€â”€ Defect location list â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  displayedColumns = ['AssetCode', 'Name', 'LocationStatus', 'CurrentLocation', 'LocationUpdatedBy', 'LocationUpdatedDate', 'action'];
  dataSource = new MatTableDataSource<any>([]);
  pageSizeOptions = [10, 20, 30, 50];
  filterValue = '';

  // â”€â”€ Update location form â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  frm!: FormGroup;
  assetList: any[] = [];
  selectedAsset: any = null;
  locationStatuses = ['Normal', 'Defect_Location', 'Parked_Away', 'Unknown'];

  // â”€â”€ Location log â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  logAsset: any = null;
  locationLog: any[] = [];
  logColumns = ['LogDate', 'NewLocation', 'LocationStatus', 'PreviousLocation', 'UpdatedBy', 'Remarks'];
  logSource = new MatTableDataSource<any>([]);

  constructor(
    private fb: FormBuilder,
    private activatedRoute: ActivatedRoute,
    private route: Router,
    private _liveAnnouncer: LiveAnnouncer,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ,
    private titleService: Title) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.buildForm();
  }

  buildForm(): void {
    this.frm = this.fb.group({
      AssetID:        [0, [Validators.required, (c: any) => +c.value === 0 ? { mustSelect: true } : null]],
      NewLocation:    ['', Validators.required],
      LocationStatus: ['Normal', Validators.required],
      UpdatedBy:      ['', Validators.required],
      Remarks:        ['']
    });
  }

  ngOnInit(): void {
    this.titleService.setTitle('Asset Location');
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    this.getUserAccessRights(this.currentUser, 'Asset Location');
    this.loadAssetList();

    // Check if opened with assetID param (from movement history page)
    const assetID = this.activatedRoute.snapshot.params['assetID'];
    if (assetID && +assetID > 0) {
      this.frm.get('AssetID')?.setValue(+assetID);
      this.onAssetChange(+assetID);
      this.mode = 'update-form';
    }
  }

  getUserAccessRights(user: string, screen: string): void {
    this._masterService.getUserAccessRights(user, screen).subscribe(d => {
      if (d) {
        this.showLoadingSpinner = true;
        this.userAccessModel.readAccess   = d.Read;
        this.userAccessModel.updateAccess = d.Update;
        this.userAccessModel.createAccess = d.Create;
        if (d.Read || this.currentUser === 'superadmin') this.loadDefectList();
        else { this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>You do not have permission to view this page.`; this.hideLoadingSpinner(); }
      }
    }, () => { this.errorMessage = 'Unable to load page.'; this.hideLoadingSpinner(); });
  }

  loadAssetList(): void {
    this.service.getAssetList().subscribe({
      next: d => { this.assetList = (d || []).filter((a: any) => a.AssetStatus !== 'Disposed'); },
      error: () => {}
    });
  }

  loadDefectList(): void {
    this.showLoadingSpinner = true;
    this.service.getDefectLocationAssets().subscribe({
      next: d => {
        const data = d || [];
        this.dataSource = new MatTableDataSource(data);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load location list.'; this.hideLoadingSpinner(); }
    });
  }

  // â”€â”€ Form actions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  openUpdateForm(asset?: any): void {
    this.buildForm();
    if (asset) {
      this.selectedAsset = asset;
      this.frm.patchValue({
        AssetID:        asset.ID,
        NewLocation:    asset.CurrentLocation || '',
        LocationStatus: asset.LocationStatus || 'Normal',
        UpdatedBy:      this.currentUser
      });
    } else {
      this.frm.get('UpdatedBy')?.setValue(this.currentUser);
      this.selectedAsset = null;
    }
    this.mode = 'update-form';
  }

  onAssetChange(assetID: number): void {
    const found = this.assetList.find((a: any) => a.ID === +assetID);
    this.selectedAsset = found || null;
    if (found) {
      this.frm.patchValue({
        NewLocation:    found.CurrentLocation || '',
        LocationStatus: found.LocationStatus  || 'Normal'
      });
    }
  }

  openLocationLog(asset: any): void {
    this.logAsset = asset;
    this.showLoadingSpinner = true;
    this.service.getAssetLocationLog(asset.ID).subscribe({
      next: d => {
        this.logSource = new MatTableDataSource(d || []);
        this.mode = 'location-log';
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load location log.'; this.hideLoadingSpinner(); }
    });
  }

  backToDefectList(): void {
    this.mode = 'defect-list';
    this.selectedAsset = null;
    this.loadDefectList();
  }

  applyFilter(e: Event): void {
    this.filterValue = (e.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  announceSortChange(s: Sort): void {
    this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared');
  }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const payload = { ...raw, AssetID: +raw.AssetID, LastUpdatedBy: this.currentUser };
    this.service.updateAssetLocation(payload).subscribe({
      next: res => {
        this.showMsg(res?.message || 'Location updated successfully.', 'success', 'Success Message');
        this.mode = 'defect-list';
        this.loadDefectList();
        // Also refresh assetList so the dropdown reflects latest data
        this.loadAssetList();
      },
      error: err => {
        this.showMsg(this.extractMsg(err, 'Unable to update location. Please try again.'), 'error', 'Error Message');
        this.hideLoadingSpinner();
      }
    });
  }

  locationStatusLabel(s: string): string { return s?.replace(/_/g, ' ') || 'â€”'; }

  locationStatusBadge(s: string): string {
    switch (s) {
      case 'Normal':          return 'badge-success';
      case 'Defect_Location': return 'badge-danger';
      case 'Parked_Away':     return 'badge-warning';
      case 'Unknown':         return 'badge-secondary';
      default:                return 'badge-secondary';
    }
  }

  formatDate(v: any): string {
    if (!v) return 'â€”';
    return new Date(v).toLocaleString('en-MY', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  private extractMsg(e: any, fb: string): string { return (e && typeof e === 'string') ? e : e?.error?.message || e?.message || fb; }
  private showMsg(msg: string, icon: 'success'|'error'|'warning'|'info', title: 'Success Message'|'Error Message'|'Warning Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: msg, icon, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }
  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }
}

