import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { CommonService } from 'src/app/service/common.service';

@Component({
  selector: 'app-active-users',
  templateUrl: './active-users.component.html',
  styleUrls: ['./active-users.component.css']
})
export class ActiveUsersComponent implements OnInit {

  // ── Access control ───────────────────────────────────────────────
  accessDenied: boolean = false;
  warningMessage: string = '';
  currentUser: string = '';

  // ── Active users tab ────────────────────────────────────────────
  activeDisplayedColumns: string[] = ['UserName', 'LoginTime', 'duration'];
  activeDataSource!: MatTableDataSource<any>;
  activeRecords: any[] = [];

  // ── History tab ─────────────────────────────────────────────────
  historyDisplayedColumns: string[] = ['UserName', 'LoginTime', 'LogoutTime', 'duration'];
  historyDataSource!: MatTableDataSource<any>;
  historyRecords: any[] = [];

  // ── Date filters (mat-datepicker uses Date | null) ───────────────
  filterDateFrom: Date | null = null;
  filterDateTo:   Date | null = null;

  // ── UI state ─────────────────────────────────────────────────────
  showLoadingSpinner: boolean = false;
  activeTab: 'active' | 'history' = 'active';

  @ViewChild('activePaginator')  activePaginator!: MatPaginator;
  @ViewChild('activeSort')       activeSort!: MatSort;
  @ViewChild('historyPaginator') historyPaginator!: MatPaginator;
  @ViewChild('historySort')      historySort!: MatSort;

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    private _commonService: CommonService,
    private _router: Router
  ) {}

  ngOnInit(): void {
    // Super Admin only — IsAdmin === '2' or username === 'superadmin'
    const userRole    = sessionStorage.getItem('userrole') ?? '0';
    this.currentUser  = sessionStorage.getItem('username') ?? '';
    const isSuperAdmin = userRole === '2' || this.currentUser.toLowerCase() === 'superadmin';

    if (!isSuperAdmin) {
      this.accessDenied  = true;
      this.warningMessage = `Dear <b>${this.currentUser}</b>, <br>
        You do not have permissions to view this page. <br>
        If you feel you should have access to this page, Please contact administrator. <br>
        Thank you`;
      return;
    }

    this.loadActiveUsers();
    // Default history date range = current month
    const today    = new Date();
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
    this.filterDateFrom = firstDay;
    this.filterDateTo   = today;
  }

  // ── Currently Online ─────────────────────────────────────────────
  loadActiveUsers(): void {
    this.showLoadingSpinner = true;
    this._commonService.getActiveLoggedInUsers().subscribe({
      next: (data) => {
        this.activeRecords   = data || [];
        this.activeDataSource = new MatTableDataSource<any>(this.activeRecords);
        setTimeout(() => {
          this.activeDataSource.paginator = this.activePaginator;
          this.activeDataSource.sort      = this.activeSort;
        });
        this.showLoadingSpinner = false;
      },
      error: () => { this.showLoadingSpinner = false; }
    });
  }

  // ── Login History ─────────────────────────────────────────────────
  loadHistory(): void {
    const fromStr = this.filterDateFrom
      ? this.filterDateFrom.toISOString().split('T')[0]
      : '';
    const toStr = this.filterDateTo
      ? this.filterDateTo.toISOString().split('T')[0]
      : '';
    if (!fromStr || !toStr) return;

    this.showLoadingSpinner = true;
    this._commonService.getLoginHistory(fromStr, toStr).subscribe({
      next: (data) => {
        this.historyRecords   = data || [];
        this.historyDataSource = new MatTableDataSource<any>(this.historyRecords);
        setTimeout(() => {
          this.historyDataSource.paginator = this.historyPaginator;
          this.historyDataSource.sort      = this.historySort;
        });
        this.showLoadingSpinner = false;
      },
      error: () => { this.showLoadingSpinner = false; }
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────
  getDuration(loginTime: string, logoutTime?: string): string {
    const start = new Date(loginTime);
    const end   = logoutTime ? new Date(logoutTime) : new Date();
    const ms    = end.getTime() - start.getTime();
    if (ms < 0) return '—';
    const mins = Math.floor(ms / 60000);
    const hrs  = Math.floor(mins / 60);
    const rem  = mins % 60;
    if (hrs > 0) return `${hrs}h ${rem}m`;
    return `${rem}m`;
  }

  applyActiveFilter(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.activeDataSource.filter = val.trim().toLowerCase();
  }

  applyHistoryFilter(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.historyDataSource.filter = val.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort): void {
    if (sortState.direction) {
      this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this._liveAnnouncer.announce('Sorting cleared');
    }
  }
}
