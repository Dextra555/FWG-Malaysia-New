import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { BehaviorSubject, catchError, Observable, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private apiUrl = environment.baseUrl;

  // Reactive unread count — bell badge subscribes to this
  private _unreadCount = new BehaviorSubject<number>(0);
  unreadCount$ = this._unreadCount.asObservable();

  // Reactive notification list — bell dropdown and list page subscribe to this.
  // SignalrNotificationService prepends new arrivals here so the UI updates
  // immediately without a round-trip to the server.
  private _notifications = new BehaviorSubject<any[]>([]);
  notifications$ = this._notifications.asObservable();

  constructor(private http: HttpClient) {}

  // ── Retrieve ───────────────────────────────────────────────────────────────

  getNotifications(username: string, maxRows: number = 50): Observable<any[]> {
    const params = new HttpParams()
      .set('username', username)
      .set('maxRows', maxRows);
    return this.http.get<any[]>(this.apiUrl + 'Notification/GetNotifications', { params })
      .pipe(catchError(this.errorHandle));
  }

  getUnreadCount(username: string): Observable<any> {
    const params = new HttpParams().set('username', username);
    return this.http.get<any>(this.apiUrl + 'Notification/GetUnreadCount', { params })
      .pipe(catchError(this.errorHandle));
  }

  /** Refresh the reactive unread count used by the bell badge. */
  refreshUnreadCount(username: string): void {
    if (!username) return;
    this.getUnreadCount(username).subscribe({
      next: (res: any) => this.setUnreadCount(res?.UnreadCount ?? 0),
      error: () => {} // silent – bell should never crash the app
    });
  }

  // ── Mark read ──────────────────────────────────────────────────────────────

  markAsRead(id: number, currentUser: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}Notification/MarkAsRead/${id}`,
      { CurrentUser: currentUser })
      .pipe(catchError(this.errorHandle));
  }

  markAllAsRead(currentUser: string): Observable<any> {
    return this.http.post<any>(this.apiUrl + 'Notification/MarkAllAsRead',
      { CurrentUser: currentUser })
      .pipe(catchError(this.errorHandle));
  }

  dismissNotification(id: number, currentUser: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}Notification/DismissNotification/${id}`,
      { CurrentUser: currentUser })
      .pipe(catchError(this.errorHandle));
  }

  // ── Low-stock alerts ───────────────────────────────────────────────────────

  getLowStockAlerts(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl + 'Notification/GetLowStockAlerts')
      .pipe(catchError(this.errorHandle));
  }

  // ── Real-time helpers (called by SignalrNotificationService) ───────────────

  /**
   * Prepend a newly-arrived SignalR notification to the shared list.
   * Bell dropdown and notification-list page subscribe to notifications$
   * and will update without any HTTP call.
   */
  prependNotification(notif: any): void {
    const current = this._notifications.getValue();
    this._notifications.next([notif, ...current]);
  }

  /**
   * Replace the entire notification list (called by bell on dropdown open
   * and by notification-list on page load).
   */
  setNotifications(list: any[]): void {
    this._notifications.next(list ?? []);
  }

  /** Increment the badge counter by 1 when a real-time push arrives. */
  incrementUnreadCount(): void {
    this._unreadCount.next(this._unreadCount.getValue() + 1);
  }

  /** Decrement the badge counter by 1 when the user reads a notification. */
  decrementUnreadCount(): void {
    const cur = this._unreadCount.getValue();
    this._unreadCount.next(Math.max(0, cur - 1));
  }

  /** Directly set the badge counter (used by polling reconciliation). */
  setUnreadCount(count: number): void {
    this._unreadCount.next(count);
  }

  // ── Helper ─────────────────────────────────────────────────────────────────

  private errorHandle(error: HttpErrorResponse) {
    let msg = 'An error occurred.';
    if (error.error?.message) msg = error.error.message;
    else if (typeof error.error === 'string') msg = error.error;
    return throwError(() => msg);
  }
}
