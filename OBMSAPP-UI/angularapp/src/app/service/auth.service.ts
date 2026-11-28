import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { CommonService } from './common.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private tokenKey = 'auth_token';
  private inactivityTimer: any;
  private readonly timeoutDuration = 1 * 60 * 1000; // 1 minute idle → auto logout
  private isIdle = false;

  constructor(private router: Router, private commonService: CommonService) {
    this.initializeActivityTracking();
  }

  getToken(): string | null {
    // sessionStorage = per-tab storage → each tab has its own independent token.
    // Falls back to localStorage for any token set by an older build.
    return sessionStorage.getItem(this.tokenKey) || localStorage.getItem(this.tokenKey);
  }

  setToken(token: string): void {
    // Store in sessionStorage only — tab-isolated, no cross-user bleed.
    sessionStorage.setItem(this.tokenKey, token);
    // Remove any stale localStorage token from older builds.
    localStorage.removeItem(this.tokenKey);
    this.startInactivityTimer();
  }

  clearToken(): void {
    sessionStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.tokenKey); // clean up legacy localStorage token
  }

  getManulLogout() {
  localStorage.setItem('manualLogout', 'true');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  logout(): void {
    // Record logout in DB before clearing the session (user name comes from sessionStorage).
    const userName = sessionStorage.getItem('username');
    if (userName) {
      try {
        this.commonService.recordLogout(userName).subscribe({
          complete: () => {},
          error: () => {}
        });
      } catch { /* fire-and-forget — never block logout */ }
    }
    this.clearToken();               // remove auth_token from localStorage
    sessionStorage.clear();          // remove username, userrole, currentUser
    this.stopInactivityTimer();
    this.router.navigate(['/login']);
  }

  // === Inactivity Timer Management ===

  private initializeActivityTracking(): void {
    const events = ['mousedown', 'keydown', 'click', 'scroll', 'touchstart'];

    events.forEach(event => {
      window.addEventListener(event, () => {
        if (!this.isAuthenticated()) return;
        // User is active → reset the idle timer
        this.startInactivityTimer();
      });
    });

    // If user already logged in on refresh
    if (this.isAuthenticated()) {
      this.startInactivityTimer();
    }
  }

  private resetInactivityTimer(): void {
    this.stopInactivityTimer();
    this.inactivityTimer = setTimeout(() => {
      this.logout();
    }, this.timeoutDuration);
  }

  private startInactivityTimer(): void {
    this.stopInactivityTimer();
    this.isIdle = false;
    console.log('[IdleTimer] Timer started — will logout in 1 minute if no activity');

    this.inactivityTimer = setTimeout(() => {
      this.isIdle = true;
      console.log('[IdleTimer] 1 minute idle — logging out now');
      this.logout(); // 1 minute idle → immediately logout
    }, this.timeoutDuration);
  }

  private stopInactivityTimer(): void {
    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
    }
  }
}
