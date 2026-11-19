import { Injectable, NgZone, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { NotificationService } from './notification.service';
import { AuthService } from './auth.service';

/**
 * SignalR real-time notification service.
 *
 * Responsibilities:
 *  1. Connect to /hubs/notifications using the stored JWT.
 *  2. Listen for "ReceiveNotification" events from the server.
 *  3. On each event:
 *     a. Prepend the notification to the shared list in NotificationService.
 *     b. Increment the unread badge count in NotificationService.
 *     c. Show a browser desktop notification (if permitted).
 *     d. Show an in-app toast.
 *  4. Reconnect automatically on disconnection.
 *  5. Never crash the application on any error.
 *
 * Desktop notification permission is requested once when the user logs in,
 * not on every notification arrival.
 */
@Injectable({ providedIn: 'root' })
export class SignalrNotificationService implements OnDestroy {

  private connection: signalR.HubConnection | null = null;
  private _connected = false;

  /** Derive hub URL from existing baseUrl: strip '/api/' and add '/hubs/notifications' */
  private get hubUrl(): string {
    const base = environment.baseUrl;          // e.g. http://localhost:5002/api/
    const origin = base.replace(/\/api\/?$/, ''); // → http://localhost:5002
    return `${origin}/hubs/notifications`;
  }

  constructor(
    private notifService: NotificationService,
    private authService: AuthService,
    private router: Router,
    private ngZone: NgZone
  ) {}

  // ── Connection management ─────────────────────────────────────────────────

  /**
   * Start the SignalR connection. Called from NotificationBellComponent.ngOnInit()
   * after the user is authenticated.
   * Safe to call multiple times — returns immediately if already connected.
   */
  startConnection(): void {
    if (this._connected || this.connection) { return; }

    const token = this.authService.getToken();
    if (!token) { return; }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        // JWT is passed as query-string because WebSockets can't send headers
        accessTokenFactory: () => this.authService.getToken() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])  // retry intervals ms
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Register the server-push handler before connecting
    this.connection.on('ReceiveNotification', (payload: any) => {
      // Always run Angular change detection inside ngZone
      this.ngZone.run(() => this.onNotificationReceived(payload));
    });

    this.connection.onreconnecting(() => {
      this._connected = false;
      console.info('[SignalR] Reconnecting to notification hub…');
    });

    this.connection.onreconnected(() => {
      this._connected = true;
      console.info('[SignalR] Reconnected to notification hub.');
    });

    this.connection.onclose((err) => {
      this._connected = false;
      if (err) {
        console.warn('[SignalR] Connection closed with error:', err.message);
      }
    });

    this.connection.start()
      .then(() => {
        this._connected = true;
        console.info('[SignalR] Connected to notification hub.');
      })
      .catch((err) => {
        this._connected = false;
        console.warn('[SignalR] Could not connect to notification hub:', err?.message ?? err);
        // Polling fallback is still active — app continues to work normally
      });
  }

  /** Stop the connection. Called on logout. */
  stopConnection(): void {
    if (this.connection) {
      this.connection.stop().catch(() => {});
      this.connection = null;
      this._connected = false;
    }
  }

  get isConnected(): boolean { return this._connected; }

  ngOnDestroy(): void {
    this.stopConnection();
  }

  // ── Notification arrival handler ─────────────────────────────────────────

  private onNotificationReceived(payload: any): void {
    if (!payload) { return; }

    // 1. Push into the shared notification list so bell dropdown updates live
    this.notifService.prependNotification(payload);

    // 2. Increment the badge
    this.notifService.incrementUnreadCount();

    // 3. Browser desktop notification
    this.showDesktopNotification(payload);

    // 4. In-app toast (bottom-right, non-blocking)
    this.showInAppToast(payload);
  }

  // ── Desktop / browser notification ───────────────────────────────────────

  /**
   * Request notification permission once (idempotent — browser returns the
   * current state immediately if permission was already granted/denied).
   * Should be called after login, not on every notification.
   */
  requestDesktopPermission(): void {
    if (!('Notification' in window)) { return; }          // API not supported
    if (Notification.permission === 'default') {
      Notification.requestPermission().catch(() => {});   // ignore rejection
    }
  }

  private showDesktopNotification(payload: any): void {
    try {
      if (!('Notification' in window)) { return; }        // unsupported browser

      if (Notification.permission !== 'granted') { return; } // not permitted

      const icon = '/assets/images/logo.png';             // OBMS app icon (graceful 404)
      const notif = new Notification('OBMS – ' + (payload.Title ?? 'New Notification'), {
        body: payload.Message ?? '',
        icon,
        tag: `obms-notif-${payload.ID}`,                  // deduplicates same notification
        requireInteraction: false                          // auto-dismiss
      });

      // Clicking the desktop notification navigates to the referenced document
      notif.onclick = () => {
        window.focus();
        notif.close();
        this.ngZone.run(() => this.navigateToRef(payload));
      };

      // Auto-close after 8 seconds (some browsers ignore requireInteraction=false)
      setTimeout(() => notif.close(), 8000);

    } catch (e) {
      // Silent — desktop notification failure must never break the application
    }
  }

  // ── In-app toast ──────────────────────────────────────────────────────────

  private showInAppToast(payload: any): void {
    try {
      const color = this.getTypeColor(payload.NotificationType);

      // Create toast element
      const toast = document.createElement('div');
      toast.className = 'obms-realtime-toast';
      toast.setAttribute('role', 'alert');
      toast.setAttribute('aria-live', 'polite');
      toast.innerHTML = `
        <div class="obms-toast-icon" style="color:${color}">
          <span class="material-icons" style="font-size:22px;vertical-align:middle">${this.getTypeMaterialIcon(payload.NotificationType)}</span>
        </div>
        <div class="obms-toast-body">
          <div class="obms-toast-title">${this.escHtml(payload.Title ?? 'New Notification')}</div>
          <div class="obms-toast-message">${this.escHtml(payload.Message ?? '')}</div>
          <div class="obms-toast-time">just now</div>
        </div>
        <button class="obms-toast-close" aria-label="Close notification">&times;</button>
      `;

      // Close button
      const closeBtn = toast.querySelector('.obms-toast-close') as HTMLElement;
      closeBtn?.addEventListener('click', (e) => {
        e.stopPropagation();
        this.removeToast(toast);
      });

      // Clicking the toast body navigates to the referenced document
      toast.addEventListener('click', () => {
        this.removeToast(toast);
        this.ngZone.run(() => this.navigateToRef(payload));
      });

      document.body.appendChild(toast);

      // Animate in
      requestAnimationFrame(() => toast.classList.add('obms-toast-visible'));

      // Auto-dismiss after 6 seconds
      setTimeout(() => this.removeToast(toast), 6000);

    } catch (e) {
      // Silent — toast failure must never break the application
    }
  }

  private removeToast(toast: HTMLElement): void {
    try {
      toast.classList.remove('obms-toast-visible');
      toast.classList.add('obms-toast-hiding');
      setTimeout(() => { if (toast.parentNode) { toast.parentNode.removeChild(toast); } }, 350);
    } catch (e) {}
  }

  // ── Navigation helper ─────────────────────────────────────────────────────

  private navigateToRef(payload: any): void {
    const refType: string = payload.ReferenceType;
    const refID: number   = payload.ReferenceID;

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
      case 'ITEM':
        this.router.navigate(['/inventory/item-master/new-item', refID]);
        break;
      default:
        this.router.navigate(['/administration/notifications']);
    }
  }

  // ── Icon/colour helpers ───────────────────────────────────────────────────

  private getTypeMaterialIcon(type: string): string {
    switch (type) {
      case 'PENDING_APPROVAL': return 'pending_actions';
      case 'APPROVED':         return 'check_circle';
      case 'REJECTED':         return 'cancel';
      case 'LOW_STOCK':        return 'warning';
      case 'OUT_OF_STOCK':     return 'inventory_2';
      default:                 return 'notifications';
    }
  }

  private getTypeColor(type: string): string {
    switch (type) {
      case 'PENDING_APPROVAL': return '#f57c00';
      case 'APPROVED':         return '#388e3c';
      case 'REJECTED':         return '#d32f2f';
      case 'LOW_STOCK':        return '#f9a825';
      case 'OUT_OF_STOCK':     return '#d32f2f';
      default:                 return '#1976d2';
    }
  }

  private escHtml(s: string): string {
    return s
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }
}
