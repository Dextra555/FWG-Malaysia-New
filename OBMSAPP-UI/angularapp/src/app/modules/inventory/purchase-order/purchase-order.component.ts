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

@Component({
  selector: 'app-purchase-order',
  templateUrl: './purchase-order.component.html',
  styleUrls: ['./purchase-order.component.css']
})
export class PurchaseOrderComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  ngAfterViewInit(): void { if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; } }

  userAccessModel!: UserAccessModel;
  currentUser = ''; userRole = ''; warningMessage = ''; errorMessage = ''; showLoadingSpinner = false; filterValue = '';
  displayedColumns = ['PONumber', 'PODate', 'ExpectedDate', 'SupplierName', 'WarehouseName', 'TotalAmount', 'Status', 'action'];
  dataSource = new MatTableDataSource<any>([]);
  pageSizeOptions: number[] = [];

  constructor(private _liveAnnouncer: LiveAnnouncer, public dialog: MatDialog, private service: InventoryService,
    private route: Router, private _dataService: DatasharingService, private _masterService: MastermoduleService) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    for (let i = 10; i <= 50; i += 10) this.pageSizeOptions.push(i);
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    const raw = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = raw === '2' ? 'superadmin' : raw === '1' ? 'admin' : 'user';
    if (!this.currentUser || this.currentUser === 'null') this._dataService.getUsername().subscribe(u => this.currentUser = u);
    this.getUserAccessRights(this.currentUser, 'Purchase Order');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      data => {
        if (data) {
          this.showLoadingSpinner = true;
          this.userAccessModel.readAccess = data.Read; this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update; this.userAccessModel.createAccess = data.Create;
          if (this.userAccessModel.readAccess || this.userRole === 'superadmin') { this.loadList(); }
          else { this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>You do not have permission to view this page.`; this.hideSpinner(); }
        }
      },
      () => { this.errorMessage = 'Unable to load page permissions. Please try again.'; this.hideSpinner(); }
    );
  }

  loadList(): void {
    this.service.getPOList().subscribe({
      next: (d: any[]) => {
        this.dataSource = new MatTableDataSource<any>(d || []);
        this.dataSource.sort = this.sort; this.dataSource.paginator = this.paginator;
        this.hideSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load purchase orders right now. Please try again.'; this.hideSpinner(); }
    });
  }

  applyFilter(e: Event): void { this.filterValue = (e.target as HTMLInputElement).value; this.dataSource.filter = this.filterValue.trim().toLowerCase(); }
  announceSortChange(s: Sort): void { this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'); }

  getStatusClass(s: string): string {
    switch (s) {
      case 'Draft':             return 'badge-draft';
      case 'Submitted':         return 'badge-submitted';
      case 'Approved':          return 'badge-approved';
      case 'Rejected':          return 'badge-rejected';
      case 'Partially Received': return 'badge-partial';
      case 'Fully Received':    return 'badge-received';
      case 'Cancelled':         return 'badge-rejected';
      default:                  return '';
    }
  }
  formatDate(d: any): string { if (!d) return ''; return new Date(d).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' }); }
  formatAmount(v: number): string { return (v || 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }

  cancelClick(row: any): void {
    if (row.Status === 'Fully Received') { this.showMsg(`Purchase Order ${row.PONumber} cannot be cancelled because all goods have already been received.`, 'warning', 'Warning Message'); return; }
    if (row.Status === 'Partially Received') { this.showMsg(`Purchase Order ${row.PONumber} cannot be cancelled because goods have already been partially received against it.`, 'warning', 'Warning Message'); return; }
    this.dialog.open(DialogConfirmationComponent, { data: { message: `Are you sure you want to cancel Purchase Order <b>${row.PONumber}</b>?` } })
      .afterClosed().subscribe((r: any) => {
        if (r?.confirmDialog) {
          this.showLoadingSpinner = true;
          this.service.cancelPO(row.ID, this.currentUser).subscribe({
            next: (res: any) => { this.showMsg(res?.message || 'Purchase order cancelled.', 'warning', 'Warning Message'); this.loadList(); },
            error: (err: any) => { this.showMsg(this.extractMsg(err, 'Unable to cancel right now. Please try again.'), 'error', 'Error Message'); this.hideSpinner(); }
          });
        }
      });
  }

  private extractMsg(err: any, fallback: string): string { if (typeof err === 'string') return err; return err?.error?.message || err?.message || fallback; }
  private showMsg(message: string, icon: 'success'|'warning'|'info'|'error', title: 'Success Message'|'Warning Message'|'Error Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon, showCloseButton: false, timer: 7000, width: '700px', customClass: { popup: 'swal-top-offset' } });
    this.hideSpinner();
  }
  hideSpinner(): void { this.showLoadingSpinner = false; }
}
