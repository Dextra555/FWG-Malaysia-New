import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';
import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({
  selector: 'app-asset-movement-history',
  templateUrl: './asset-movement-history.component.html',
  styleUrls: ['./asset-movement-history.component.css']
})
export class AssetMovementHistoryComponent implements OnInit {

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false;

  assetID: number = 0;
  asset: any = null;
  history: any[] = [];

  constructor(
    private activatedRoute: ActivatedRoute,
    private route: Router,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
  }

  ngOnInit(): void {
    this.assetID = +this.activatedRoute.snapshot.params['assetID'];
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    this.getUserAccessRights(this.currentUser, 'Asset Movement History');
  }

  getUserAccessRights(user: string, screen: string): void {
    this._masterService.getUserAccessRights(user, screen).subscribe(d => {
      if (d) {
        this.showLoadingSpinner = true;
        this.userAccessModel.readAccess = d.Read;
        if (d.Read || this.currentUser === 'superadmin') {
          this.loadData();
        } else {
          this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>You do not have permission to view this page.`;
          this.hideLoadingSpinner();
        }
      }
    }, () => { this.errorMessage = 'Unable to load page.'; this.hideLoadingSpinner(); });
  }

  loadData(): void {
    if (!this.assetID || this.assetID === 0) { this.errorMessage = 'Invalid asset ID.'; this.hideLoadingSpinner(); return; }

    // Load asset info
    this.service.getAssetByIDEnriched(this.assetID).subscribe({
      next: d => { this.asset = d; },
      error: () => {}
    });

    // Load movement history
    this.service.getAssetMovementHistory(this.assetID).subscribe({
      next: d => { this.history = d || []; this.hideLoadingSpinner(); },
      error: () => { this.errorMessage = 'Unable to load asset history. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  getMovementIcon(type: string): string {
    const icons: { [k: string]: string } = {
      'Allocation': 'assignment_ind', 'Allocation_Cancelled': 'assignment_return',
      'Transfer': 'swap_horiz', 'Return': 'keyboard_return',
      'Maintenance': 'build', 'Maintenance_Complete': 'check_circle',
      'Damage': 'broken_image', 'Disposal': 'delete_forever', 'Status_Change': 'sync'
    };
    return icons[type] || 'radio_button_unchecked';
  }

  getMovementColor(type: string): string {
    const colors: { [k: string]: string } = {
      'Allocation': '#1976d2', 'Allocation_Cancelled': '#ef6c00',
      'Transfer': '#7b1fa2', 'Return': '#388e3c',
      'Maintenance': '#f9a825', 'Maintenance_Complete': '#43a047',
      'Damage': '#d32f2f', 'Disposal': '#424242', 'Status_Change': '#00838f'
    };
    return colors[type] || '#9e9e9e';
  }

  statusBadgeClass(status: string): string {
    const map: { [k: string]: string } = {
      'Available': 'badge-success', 'Allocated': 'badge-primary',
      'Under_Maintenance': 'badge-warning', 'Damaged': 'badge-danger',
      'Disposed': 'badge-secondary', 'In_Transit': 'badge-info'
    };
    return map[status] || 'badge-secondary';
  }

  formatDate(v: any): string {
    if (!v) return '—';
    return new Date(v).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  goBack(): void { this.route.navigate(['/inventory/asset-master']); }
  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }
}
