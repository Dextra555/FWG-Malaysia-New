import { AfterViewInit, Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import Swal from 'sweetalert2';

import { NotificationService } from '../../../service/notification.service';
import { DatasharingService } from '../../../service/datasharing.service';

@Component({
  selector: 'app-notification-list',
  templateUrl: './notification-list.component.html',
  styleUrls: ['./notification-list.component.css']
})
export class NotificationListComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort)      sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) { this.dataSource.paginator = this.paginator; this.dataSource.sort = this.sort; }
  }

  currentUser    = '';
  showLoadingSpinner = false;
  errorMessage   = '';
  filterValue    = '';

  displayedColumns = ['NotificationType', 'Title', 'Message', 'CreatedDate', 'TimeAgo', 'IsRead', 'action'];
  dataSource = new MatTableDataSource<any>([]);
  maxRows = 100;

  // Low-stock alerts tab
  lowStockAlerts: any[] = [];
  showLowStock = false;
  lowStockLoading = false;

  private notifSub?: Subscription;

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    private notifService: NotificationService,
    private _dataService: DatasharingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username') || '';
    if (!this.currentUser) {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; this.loadNotifications(); });
    } else {
      this.loadNotifications();
    }

    // Subscribe to real-time notification updates from SignalR.
    // When a new notification arrives while this page is open, the table
    // updates immediately without a page refresh.
    this.notifSub = this.notifService.notifications$.subscribe((list: any[]) => {
      if (list && list.length > 0 && this.currentUser) {
        // Filter to only this user's notifications (safety check)
        const mine = list.filter(n => !n.RecipientUser || n.RecipientUser === this.currentUser);
        if (mine.length !== this.dataSource.data.length) {
          this.dataSource.data = mine;
          if (this.paginator) { this.dataSource.paginator = this.paginator; }
          if (this.sort)      { this.dataSource.sort      = this.sort; }
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.notifSub?.unsubscribe();
  }

  loadNotifications(): void {
    if (!this.currentUser) return;
    this.showLoadingSpinner = true;
    this.notifService.getNotifications(this.currentUser, this.maxRows).subscribe({
      next: (d: any[]) => {
        const list = d || [];
        // Push into shared state — SignalR prepends will keep this fresh
        this.notifService.setNotifications(list);
        this.dataSource = new MatTableDataSource<any>(list);
        this.dataSource.sort = this.sort;
        this.dataSource.paginator = this.paginator;
        this.notifService.refreshUnreadCount(this.currentUser);
        this.hideSpinner();
      },
      error: (err: any) => { this.errorMessage = typeof err === 'string' ? err : 'Unable to load notifications.'; this.hideSpinner(); }
    });
  }

  loadLowStockAlerts(): void {
    this.lowStockLoading = true;
    this.notifService.getLowStockAlerts().subscribe({
      next: (d: any[]) => { this.lowStockAlerts = d || []; this.lowStockLoading = false; },
      error: () => { this.lowStockLoading = false; }
    });
  }

  switchTab(tab: 'notifications' | 'lowstock'): void {
    this.showLowStock = tab === 'lowstock';
    if (this.showLowStock && this.lowStockAlerts.length === 0) this.loadLowStockAlerts();
  }

  applyFilter(e: Event): void {
    this.filterValue = (e.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  announceSortChange(s: Sort): void {
    this._liveAnnouncer.announce(s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared');
  }

  markRead(row: any): void {
    if (row.IsRead) return;
    this.notifService.markAsRead(row.ID, this.currentUser).subscribe({
      next: () => {
        row.IsRead = true;
        this.notifService.decrementUnreadCount();
      },
      error: () => this.showMsg('Unable to mark notification as read.', 'error')
    });
  }

  markAllRead(): void {
    this.notifService.markAllAsRead(this.currentUser).subscribe({
      next: () => {
        this.dataSource.data.forEach(n => n.IsRead = true);
        this.notifService.setUnreadCount(0);
        this.showMsg('All notifications marked as read.', 'success');
      },
      error: () => this.showMsg('Unable to mark all as read.', 'error')
    });
  }

  dismiss(row: any): void {
    this.notifService.dismissNotification(row.ID, this.currentUser).subscribe({
      next: () => {
        const data = this.dataSource.data.filter(n => n.ID !== row.ID);
        this.dataSource.data = data;
        // Keep shared state in sync so bell dropdown also reflects the dismissal
        this.notifService.setNotifications(data);
        if (!row.IsRead) { this.notifService.decrementUnreadCount(); }
      },
      error: () => {}
    });
  }

  navigateTo(notif: any): void {
    this.markRead(notif);
    const refType = notif.ReferenceType;
    const refID   = notif.ReferenceID;
    if (!refType || !refID) return;
    switch (refType) {
      case 'PURCHASE_REQUISITION':
        this.router.navigate(['/inventory/purchase-requisition/new-pr', refID]); break;
      case 'PURCHASE_ORDER':
        this.router.navigate(['/inventory/purchase-order/new-po', refID]); break;
      case 'BRANCH_STOCK_REQUEST':
        this.router.navigate(['/inventory/branch-stock-request/new-bsr', refID]); break;
      case 'ITEM':
        this.router.navigate(['/inventory/item-master/new-item', refID]); break;
    }
  }

  getTypeIcon(type: string): string {
    switch (type) {
      case 'PENDING_APPROVAL': return 'pending_actions';
      case 'APPROVED':         return 'check_circle';
      case 'REJECTED':         return 'cancel';
      case 'LOW_STOCK':        return 'warning';
      case 'OUT_OF_STOCK':     return 'inventory_2';
      default:                 return 'notifications';
    }
  }

  getTypeColor(type: string): string {
    switch (type) {
      case 'PENDING_APPROVAL': return '#f57c00';
      case 'APPROVED':         return '#388e3c';
      case 'REJECTED':         return '#d32f2f';
      case 'LOW_STOCK':        return '#f9a825';
      case 'OUT_OF_STOCK':     return '#d32f2f';
      default:                 return '#1976d2';
    }
  }

  formatDate(d: any): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  private showMsg(msg: string, icon: 'success' | 'error'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, timer: 4000, icon, text: msg, width: '500px', customClass: { popup: 'swal-top-offset' } });
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }
}
