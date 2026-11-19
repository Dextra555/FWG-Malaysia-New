import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import * as XLSX from 'xlsx';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({
  selector: 'app-approval-history',
  templateUrl: './approval-history.component.html',
  styleUrls: ['./approval-history.component.css']
})
export class ApprovalHistoryComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort)      sort!: MatSort;

  // ── Access ────────────────────────────────────────────────────────────────
  userAccessModel!: UserAccessModel;
  currentUser    = '';
  userRole       = '';   // '0'→'user', '1'→'admin', '2'→'superadmin'
  warningMessage = '';
  errorMessage   = '';

  // ── State ─────────────────────────────────────────────────────────────────
  showLoadingSpinner = false;

  // ── Filter ────────────────────────────────────────────────────────────────
  filterDocType  = '';       // document type dropdown
  filterDateFrom: Date | null = null;   // From Date — mat-datepicker
  filterDateTo:   Date | null = null;   // To Date   — mat-datepicker

  // ── Table ─────────────────────────────────────────────────────────────────
  displayedColumns = [
    'ActionDate', 'DocumentTypeDisplay', 'DocumentNumber',
    'Action', 'ActionBy', 'PreviousStatus', 'NewStatus', 'Comments'
  ];
  dataSource = new MatTableDataSource<any>([]);
  pageSizeOptions: number[] = [10, 20, 30, 50];

  // ── Dropdown options ───────────────────────────────────────────────────────
  docTypeOptions = [
    { value: '',                      label: 'All'                   },
    { value: 'PURCHASE_REQUISITION',  label: 'Purchase Requisition'  },
    { value: 'PURCHASE_ORDER',        label: 'Purchase Order'        },
    { value: 'STOCK_ADJUSTMENT',      label: 'Stock Adjustment'      },
    { value: 'STOCK_TRANSFER',        label: 'Stock Transfer'        },
    { value: 'DAMAGE_LOSS',           label: 'Damage / Loss'         },
    { value: 'ASSET_DISPOSAL',        label: 'Asset Disposal'        },
    { value: 'BRANCH_STOCK_REQUEST',  label: 'Branch Stock Request'  }
  ];

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = {
      readAccess: false, updateAccess: false,
      deleteAccess: false, createAccess: false
    };
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username') ?? '';
    const raw = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = raw === '2' ? 'superadmin' : raw === '1' ? 'admin' : 'user';
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => {
        this.currentUser = u;
        this.getUserAccessRights(this.currentUser, 'Approval History');
      });
    } else {
      this.getUserAccessRights(this.currentUser, 'Approval History');
    }
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort      = this.sort;
  }

  // ── Permissions ───────────────────────────────────────────────────────────

  getUserAccessRights(userName: string, screenName: string): void {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      data => {
        if (data) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.createAccess = data.Create;
        }
        if (this.userAccessModel.readAccess || this.userRole === 'superadmin') {
          this.warningMessage = '';
          this.loadList();
        } else {
          this.warningMessage =
            `Dear <b>${this.currentUser}</b>,<br>` +
            `You do not have permissions to view this page.<br>` +
            `If you feel you should have access, please contact your administrator.`;
          this.hideSpinner();
        }
      },
      () => {
        this.errorMessage = 'Unable to load page permissions. Please try again.';
        this.hideSpinner();
      }
    );
  }

  // ── Data Loading ──────────────────────────────────────────────────────────

  /**
   * Loads approval history from the API.
   * From Date / To Date are optional — data loads automatically on page open.
   * Selecting dates and clicking Search narrows results by date range.
   */
  loadList(): void {
    this.showLoadingSpinner = true;
    this.errorMessage       = '';

    // Convert Date objects → ISO string (yyyy-MM-dd) for the API
    const fromStr = this.filterDateFrom
      ? this.filterDateFrom.toISOString().split('T')[0]
      : undefined;
    const toStr = this.filterDateTo
      ? this.filterDateTo.toISOString().split('T')[0]
      : undefined;

    this.service.getAllApprovalHistory(
      this.filterDocType || undefined,
      undefined,     // no documentID filter on this screen
      fromStr,       // optional From Date
      toStr          // optional To Date
    ).subscribe({
      next: (d: any[]) => {
        const enriched = (d || []).map(r => ({
          ...r,
          DocumentTypeDisplay: this.friendlyDocType(r.DocumentType)
        }));
        this.dataSource           = new MatTableDataSource<any>(enriched);
        this.dataSource.sort      = this.sort;
        this.dataSource.paginator = this.paginator;
        this.buildPageSizeOptions(enriched.length);
        this.hideSpinner();
      },
      error: (err: any) => {
        this.errorMessage = this.extractMsg(err, 'Unable to load approval history. Please try again.');
        this.hideSpinner();
      }
    });
  }

  // ── Table Helpers ─────────────────────────────────────────────────────────

  /** Live search within current results — same as Branch Master / Approval Setup. */
  applyFilter(e: Event): void {
    const val = (e.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
  }

  announceSortChange(s: Sort): void {
    this._liveAnnouncer.announce(
      s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'
    );
  }

  /** Triggered by the Search button — re-fetches with current filter values. */
  onSearch(): void {
    this.loadList();
  }

  // ── Badge ─────────────────────────────────────────────────────────────────

  getActionClass(action: string): string {
    switch ((action || '').trim()) {
      case 'Approved':  return 'ah-badge-approved';
      case 'Rejected':  return 'ah-badge-rejected';
      case 'Submitted': return 'ah-badge-submitted';
      case 'Cancelled': return 'ah-badge-cancelled';
      default:          return 'ah-badge-draft';
    }
  }

  // ── Excel Export ──────────────────────────────────────────────────────────

  /**
   * Exports the currently visible (filtered) data to Excel.
   * Uses xlsx library already present in the project (xlsx@0.18.5).
   * File naming follows the existing OBMS pattern: Module_YYYY-MM-DD.xlsx
   */
  exportToExcel(): void {
    const rows = this.dataSource.filteredData;

    if (!rows || rows.length === 0) {
      alert('No data to export.');
      return;
    }

    // Map to export-friendly column headers (only real fields, no internal IDs)
    const exportData = rows.map(r => ({
      'Date / Time':     this.formatDate(r.ActionDate),
      'Document Type':   r.DocumentTypeDisplay || r.DocumentType || '',
      'Document No.':    r.DocumentNumber      || '',
      'Action':          r.Action              || '',
      'Action By':       r.ActionBy            || '',
      'From Status':     r.PreviousStatus      || '',
      'To Status':       r.NewStatus           || '',
      'Remarks / Reason': r.Comments           || ''
    }));

    const worksheet: XLSX.WorkSheet = XLSX.utils.json_to_sheet(exportData);
    const workbook:  XLSX.WorkBook  = { Sheets: { 'Approval History': worksheet }, SheetNames: ['Approval History'] };

    // Date-stamped filename following the OBMS convention
    const today    = new Date().toISOString().split('T')[0];   // YYYY-MM-DD
    const fileName = `Approval_History_${today}.xlsx`;

    XLSX.writeFile(workbook, fileName);
  }

  // ── Formatters ────────────────────────────────────────────────────────────

  formatDate(d: any): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('en-MY', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  private friendlyDocType(t: string): string {
    const map: Record<string, string> = {
      'PURCHASE_REQUISITION':  'Purchase Requisition',
      'PURCHASE_ORDER':        'Purchase Order',
      'STOCK_ADJUSTMENT':      'Stock Adjustment',
      'STOCK_TRANSFER':        'Stock Transfer',
      'DAMAGE_LOSS':           'Damage / Loss',
      'ASSET_DISPOSAL':        'Asset Disposal',
      'BRANCH_STOCK_REQUEST':  'Branch Stock Request'
    };
    return map[t] || t;
  }

  private buildPageSizeOptions(total: number): void {
    const base = [10, 20, 30, 50];
    this.pageSizeOptions = base.filter(n => n <= total);
    if (this.pageSizeOptions.length === 0) this.pageSizeOptions = [10];
  }

  private extractMsg(err: any, fallback: string): string {
    if (typeof err === 'string') return err;
    return err?.error?.message || err?.message || fallback;
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }
}
