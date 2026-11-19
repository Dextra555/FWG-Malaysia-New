import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private tokenKey = 'auth_token';
  private inactivityTimer: any;
  private readonly timeoutDuration = 10 * 60 * 1000; // 3 minutes
  private isIdle = false;

  constructor(private router: Router) {
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
    this.clearToken();               // remove auth_token from localStorage
    sessionStorage.clear();          // remove username, userrole, currentUser
    this.stopInactivityTimer();
    this.router.navigate(['/login']);
  }

  // === Inactivity Timer Management ===

  private initializeActivityTracking(): void {
    const events = ['mousemove', 'keydown', 'click', 'scroll'];

    events.forEach(event => {
      window.addEventListener(event, () => {
        if (!this.isAuthenticated()) return;

        if (this.isIdle) {
          // User was idle and now interacted → logout
          this.logout();
          return;
        }

        // Not idle → Reset the timer
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

    this.inactivityTimer = setTimeout(() => {
      this.isIdle = true; // only mark idle, DO NOT logout
    }, this.timeoutDuration);
  }

  private stopInactivityTimer(): void {
    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
    }
  }
}
