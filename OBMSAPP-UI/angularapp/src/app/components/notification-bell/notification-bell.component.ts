import { Component, OnInit, OnDestroy, HostListener, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { startWith, switchMap, filter } from 'rxjs/operators';
import { NotificationService } from '../../service/notification.service';
import { SignalrNotificationService } from '../../service/signalr-notification.service';
import { DatasharingService } from '../../service/datasharing.service';
import { AuthService } from '../../service/auth.service';

@Component({
  selector: 'app-notification-bell',
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.css']
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  currentUser   = '';
  unreadCount   = 0;
  notifications: any[] = [];
  dropdownOpen  = false;

  private pollSub?:      Subscription;
  private countSub?:     Subscription;
  private notifListSub?: Subscription;

  constructor(
    private notifService:    NotificationService,
    private signalrService:  SignalrNotificationService,
    private dataService:     DatasharingService,
    private authService:     AuthService,
    private router:          Router,
    private elRef:           ElementRef
  ) {}

  // ── Close dropdown when clicking outside ───────────────────────────────────
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elRef.nativeElement.contains(event.target)) {
      this.dropdownOpen = false;
    }
  }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) { return; }

    this.currentUser = sessionStorage.getItem('username') || '';

    if (!this.currentUser) {
      this.dataService.getUsername().subscribe((u: string) => {
        this.currentUser = u;
        if (this.currentUser) { this.initRealTime(); }
      });
    } else {
      this.initRealTime();
    }

    // Badge stays in sync with any push (from SignalR or poll reconciliation)
    this.countSub = this.notifService.unreadCount$.subscribe(
      (c: number) => { this.unreadCount = c; }
    );

    // When SignalR pushes a new notification, update dropdown list immediately
    // if it is currently open — no extra HTTP call needed.
    this.notifListSub = this.notifService.notifications$.subscribe((list: any[]) => {
      if (this.dropdownOpen) {
        // Show only unread items in the bell dropdown
        this.notifications = list.filter((n: any) => !n.IsRead);
      }
    });
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
    this.countSub?.unsubscribe();
    this.notifListSub?.unsubscribe();
    // Do NOT stop SignalR here — the service is singleton and must stay alive
  }

  // ── Initialise real-time + fallback polling ────────────────────────────────
  private initRealTime(): void {
    if (!this.currentUser || !this.authService.isAuthenticated()) { return; }

    // 1. Start SignalR (primary delivery channel)
    this.signalrService.startConnection();

    // 2. Request browser desktop notification permission (once per session)
    this.signalrService.requestDesktopPermission();

    // 3. Start polling as fallback / reconciliation
    //    - When SignalR is connected, this only reconciles the count every 30 s
    //      so any edge-case missed pushes are caught.
    //    - When SignalR is disconnected, this keeps the badge alive at 15 s.
    this.startPolling();

    // 4. Load initial unread count immediately
    this.notifService.refreshUnreadCount(this.currentUser);
  }

  // ── Polling (fallback + reconciliation) ───────────────────────────────────
  private startPolling(): void {
    if (!this.currentUser || !this.authService.isAuthenticated()) { return; }

    // Poll every 15 s when SignalR is disconnected, 30 s when connected
    // Use a fixed 15 s interval; the reconciliation is lightweight (count only)
    this.pollSub = interval(15000).pipe(
      startWith(0),
      filter(() => this.authService.isAuthenticated()),
      switchMap(() => this.notifService.getUnreadCount(this.currentUser))
    ).subscribe({
      next: (res: any) => {
        const serverCount: number = res?.UnreadCount ?? 0;
        // Only update if SignalR is disconnected OR the server count is
        // higher than what we're showing (safety net for missed pushes).
        // Never decrease the count from polling — the user may have read
        // items locally that the poll hasn't seen yet.
        if (!this.signalrService.isConnected) {
          // Fallback mode: trust the server count directly
          this.notifService.setUnreadCount(serverCount);
        } else {
          // Reconciliation: raise badge if server is ahead (missed push)
          if (serverCount > this.unreadCount) {
            this.notifService.setUnreadCount(serverCount);
          }
        }
      },
      error: () => { /* silent — never crash the UI */ }
    });
  }

  // ── Dropdown toggle ────────────────────────────────────────────────────────
  toggleDropdown(): void {
    if (!this.authService.isAuthenticated()) { return; }
    this.dropdownOpen = !this.dropdownOpen;
    if (this.dropdownOpen) { this.loadNotifications(); }
  }

  // ── Load preview list — only unread shown in bell dropdown ────────────────
  loadNotifications(): void {
    if (!this.currentUser || !this.authService.isAuthenticated()) { return; }
    this.notifService.getNotifications(this.currentUser, 50).subscribe({
      next: (d: any[]) => {
        const all = d || [];
        // Push full list into shared state so SignalR prepends work correctly
        this.notifService.setNotifications(all);
        // Show only unread in bell dropdown
        this.notifications = all.filter((n: any) => !n.IsRead);
      },
      error: () => {}
    });
  }

  // ── Mark single notification read & remove from dropdown ─────────────────
  markRead(notif: any, event: Event): void {
    event.stopPropagation();
    if (notif.IsRead) { return; }
    this.notifService.markAsRead(notif.ID, this.currentUser).subscribe({
      next: () => {
        notif.IsRead = true;
        this.notifications = this.notifications.filter((n: any) => !n.IsRead);
        this.notifService.decrementUnreadCount();
      },
      error: () => {}
    });
  }

  // ── Mark all read & clear dropdown ────────────────────────────────────────
  markAllRead(): void {
    this.notifService.markAllAsRead(this.currentUser).subscribe({
      next: () => {
        this.notifications = [];
        this.notifService.setUnreadCount(0);
      },
      error: () => {}
    });
  }

  // ── Navigate to referenced document ───────────────────────────────────────
  navigateTo(notif: any): void {
    if (!notif.IsRead) {
      this.notifService.markAsRead(notif.ID, this.currentUser).subscribe({
        next: () => {
          notif.IsRead = true;
          this.notifications = this.notifications.filter((n: any) => !n.IsRead);
          this.notifService.decrementUnreadCount();
        },
        error: () => {}
      });
    }
    this.dropdownOpen = false;

    const refType: string = notif.ReferenceType;
    const refID:   number = notif.ReferenceID;

    if (!refType || !refID) {
      this.router.navigate(['/administration/notifications']);
      return;
    }

    switch (refType) {
      case 'PURCHASE_REQUISITION':
        this.router.navigate(['/inventory/purchase-requisition/new-pr', refID]);
        break;
      case 'PURCHASE_ORDER':
        this.router.navigate(['/inventory/purchase-order/new-po', refID]);
        break;
      case 'BRANCH_STOCK_REQUEST':
        this.router.navigate(['/inventory/branch-stock-request/new-bsr', refID]);
        break;
      case 'ITEM':
        this.router.navigate(['/inventory/item-master/new-item', refID]);
        break;
      default:
        this.router.navigate(['/administration/notifications']);
    }
  }

  // ── View all notifications page ────────────────────────────────────────────
  viewAll(): void {
    this.dropdownOpen = false;
    this.router.navigate(['/administration/notifications']);
  }

  // ── Icon helpers ───────────────────────────────────────────────────────────
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
}
