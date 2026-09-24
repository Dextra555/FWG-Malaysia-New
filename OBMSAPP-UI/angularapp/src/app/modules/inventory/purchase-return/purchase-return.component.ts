import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Router } from '@angular/router';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({ selector: 'app-purchase-return', templateUrl: './purchase-return.component.html', styleUrls: ['./purchase-return.component.css'] })
export class PurchaseReturnComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void { if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; } }

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false; filterValue = '';
  displayedColumns = ['ReturnNumber', 'ReturnDate', 'GRNNumber', 'PONumber', 'SupplierName', 'WarehouseName', 'Reason', 'action'];
  dataSource = new MatTableDataSource<any>([]);
  pageSizeOptions: number[] = [];

  constructor(private _liveAnnouncer: LiveAnnouncer, private service: InventoryService, private route: Router, private _dataService: DatasharingService, private _masterService: MastermoduleService) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    for (let i = 10; i <= 50; i += 10) this.pageSizeOptions.push(i);
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => this.currentUser = u);
    this.getUserAccessRights(this.currentUser, 'Purchase Return');
  }

  getUserAccessRights(u: string, s: string): void {
    this._masterService.getUserAccessRights(u, s).subscribe(
      data => {
        if (data) {
          this.showLoadingSpinner = true;
          this.userAccessModel.readAccess = data.Read; this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update; this.userAccessModel.createAccess = data.Create;
          if (this.userAccessModel.readAccess || this.currentUser === 'superadmin') { this.loadList(); }
          else { this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>You do not have permission to view this page.`; this.hideSpinner(); }
        }
      },
      () => { this.errorMessage = 'Unable to load page permissions. Please try again.'; this.hideSpinner(); }
    );
  }

  loadList(): void {
    this.service.getPurchaseReturnList().subscribe({
      next: (d: any[]) => { this.dataSource = new MatTableDataSource<any>(d || []); this.dataSource.sort = this.sort; this.dataSource.paginator = this.paginator; this.hideSpinner(); },
      error: () => { this.errorMessage = 'Unable to load the purchase return list right now. Please try again.'; this.hideSpinner(); }
    });
  }

  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }
  formatDate(d: any): string { if (!d) return ''; return new Date(d).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }
  hideSpinner(): void { this.showLoadingSpinner = false; }
}
