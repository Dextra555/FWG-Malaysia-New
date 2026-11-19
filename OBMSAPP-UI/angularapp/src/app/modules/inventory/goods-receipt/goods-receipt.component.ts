import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { DialogConfirmationComponent } from '../../../components/dialog-confirmation/dialog-confirmation.component';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({ selector: 'app-goods-receipt', templateUrl: './goods-receipt.component.html', styleUrls: ['./goods-receipt.component.css'] })
export class GoodsReceiptComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void { if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; } }

  userAccessModel!: UserAccessModel;
  currentUser = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false; filterValue = '';
  displayedColumns = ['GRNNumber', 'GRNDate', 'PONumber', 'SupplierName', 'WarehouseName', 'DeliveryNoteRef', 'action'];
  dataSource = new MatTableDataSource<any>([]);
  pageSizeOptions: number[] = [];

  constructor(private _liveAnnouncer: LiveAnnouncer, public dialog: MatDialog, private service: InventoryService,
    private route: Router, private _dataService: DatasharingService, private _masterService: MastermoduleService) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    for (let i = 10; i <= 50; i += 10) this.pageSizeOptions.push(i);
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => this.currentUser = u);
    this.getUserAccessRights(this.currentUser, 'Goods Receipt');
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
    this.service.getGRNList().subscribe({
      next: (d: any[]) => { this.dataSource = new MatTableDataSource<any>(d || []); this.dataSource.sort = this.sort; this.dataSource.paginator = this.paginator; this.hideSpinner(); },
      error: () => { this.errorMessage = 'Unable to load the goods receipt list right now. Please try again.'; this.hideSpinner(); }
    });
  }

  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }
  formatDate(d: any): string { if (!d) return ''; return new Date(d).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }

  deleteClick(row: any): void {
    this.showLoadingSpinner = true;
    // Always check backend dependency first — do NOT rely solely on frontend
    this.service.checkGRNDependency(row.ID).subscribe({
      next: (dep: any) => {
        this.hideSpinner();
        if (!dep.canDelete) {
          this.showMsg(dep.blockingMessage || 'This goods receipt cannot be deleted due to a downstream dependency.', 'warning', 'Warning Message');
          return;
        }
        // Safe to delete — ask for confirmation
        this.dialog.open(DialogConfirmationComponent, {
          data: `Are you sure you want to delete Goods Receipt <b>${row.GRNNumber}</b>?<br><br>
                 This will reverse the stock update and revert the purchase order status.`
        }).afterClosed().subscribe((result: any) => {
          if (result?.confirmDialog) {
            this.showLoadingSpinner = true;
            this.service.deleteGRN(row.ID, this.currentUser).subscribe({
              next: (res: any) => { this.showMsg(res?.message || 'Goods receipt deleted. Stock has been reversed.', 'success', 'Success Message'); this.loadList(); },
              error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to delete this goods receipt right now. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
            });
          }
        });
      },
      error: (err: any) => {
        this.hideSpinner();
        this.showMsg(this.extractMsg(err, 'Unable to check goods receipt dependencies right now. Please try again.'), 'error', 'Error Message');
      }
    });
  }

  private extractMsg(err: any, fallback: string): string { if (typeof err === 'string') return err; return err?.error?.message || err?.message || fallback; }
  private showMsg(message: string, icon: 'success'|'warning'|'info'|'error', title: 'Success Message'|'Warning Message'|'Error Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon, showCloseButton: false, timer: 8000, width: '780px', customClass: { popup: 'swal-top-offset' } });
    this.hideSpinner();
  }
  hideSpinner(): void { this.showLoadingSpinner = false; }
}
