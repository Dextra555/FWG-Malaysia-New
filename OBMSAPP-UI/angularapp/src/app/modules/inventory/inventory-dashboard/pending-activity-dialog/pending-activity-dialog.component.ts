import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { InventoryService } from '../../../../service/inventory.service';

export type PendingActivityType = 'PO' | 'GRN' | 'TRANSFER' | 'ISSUE' | 'ADJUSTMENT' | 'APPROVAL';

export interface PendingActivityDialogData {
  activityType: PendingActivityType;
  title: string;
  count: number;
}

@Component({
  selector: 'app-pending-activity-dialog',
  templateUrl: './pending-activity-dialog.component.html',
  styleUrls: ['./pending-activity-dialog.component.css']
})
export class PendingActivityDialogComponent implements OnInit {

  loading      = true;
  errorMessage = '';
  filterText   = '';

  allRows:      any[] = [];
  filteredRows: any[] = [];

  columns: { key: string; label: string; align?: 'right' | 'center' }[] = [];

  constructor(
    public  dialogRef: MatDialogRef<PendingActivityDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: PendingActivityDialogData,
    private _service: InventoryService,
    private _router:  Router
  ) {}

  ngOnInit(): void {
    this.setupColumns();
    this.loadData();
  }

  // ── Column definitions ────────────────────────────────────────────────────

  private setupColumns(): void {
    switch (this.data.activityType) {
      case 'PO':
        this.columns = [
          { key: 'PRNumber',       label: 'PR Number'   },
          { key: 'PRDate',         label: 'PR Date'     },
          { key: 'RequiredByDate', label: 'Required By' },
          { key: 'SubmittedBy',    label: 'Requested By'},
          { key: 'Status',         label: 'Status'      }
        ];
        break;
      case 'GRN':
        this.columns = [
          { key: 'PONumber',      label: 'PO Number'       },
          { key: 'PODate',        label: 'PO Date'         },
          { key: 'SupplierName',  label: 'Supplier'        },
          { key: 'WarehouseName', label: 'Warehouse'       },
          { key: 'TotalAmount',   label: 'Amount (RM)', align: 'right' },
          { key: 'Status',        label: 'Status'          }
        ];
        break;
      case 'TRANSFER':
        this.columns = [
          { key: 'RequestNo',            label: 'BSR No'           },
          { key: 'RequestDate',          label: 'Date'             },
          { key: 'RequestingBranchName', label: 'Branch'           },
          { key: 'SourceWarehouseName',  label: 'Source Warehouse' },
          { key: 'RequestedBy',          label: 'Requested By'     },
          { key: 'Status',               label: 'Status'           }
        ];
        break;
      case 'ISSUE':
        this.columns = [
          { key: 'IssueNo',       label: 'Issue No'  },
          { key: 'IssueDate',     label: 'Date'      },
          { key: 'WarehouseName', label: 'Warehouse' },
          { key: 'IssueToName',   label: 'Issued To' },
          { key: 'IssueToType',   label: 'Type'      }
        ];
        break;
      case 'ADJUSTMENT':
        this.columns = [
          { key: 'AdjustmentNo',   label: 'Adj. No'   },
          { key: 'AdjustmentDate', label: 'Date'      },
          { key: 'WarehouseName',  label: 'Warehouse' },
          { key: 'ItemName',       label: 'Item'      },
          { key: 'AdjustmentType', label: 'Type'      },
          { key: 'Reason',         label: 'Reason'    },
          { key: 'Status',         label: 'Status'    }
        ];
        break;
      case 'APPROVAL':
        // Uses getPendingApprovals() — fields from VW_PendingApprovals
        this.columns = [
          { key: 'DocumentTypeDisplay', label: 'Document Type' },
          { key: 'DocumentNumber',      label: 'Doc No'        },
          { key: 'DocumentDate',        label: 'Date'          },
          { key: 'RequestedBy',         label: 'Requested By'  },
          { key: 'BranchName',          label: 'Branch'        },
          { key: 'CurrentStatus',       label: 'Status'        }
        ];
        break;
    }
  }

  // ── Data loading ──────────────────────────────────────────────────────────

  private loadData(): void {
    this.loading = true;
    this.errorMessage = '';

    const month = new Date().getMonth();
    const year  = new Date().getFullYear();
    const isThisMonth = (v: any) => {
      if (!v) return false;
      const d = new Date(v);
      return d.getMonth() === month && d.getFullYear() === year;
    };

    let obs$: any;
    switch (this.data.activityType) {
      case 'PO':         obs$ = this._service.getPendingPOs();          break;
      case 'GRN':        obs$ = this._service.getPendingGRNs();         break;
      case 'TRANSFER':   obs$ = this._service.getPendingTransfers();    break;
      case 'ISSUE':      obs$ = this._service.getPendingIssues();       break;
      case 'ADJUSTMENT': obs$ = this._service.getPendingAdjustments();  break;
      case 'APPROVAL':   obs$ = this._service.getPendingApprovals();    break;
      default:           obs$ = this._service.getPendingApprovals();    break;
    }

    obs$.subscribe({
      next: (rows: any[]) => {
        switch (this.data.activityType) {
          case 'PO':
            // Approved PRs with no PO yet (DisplayStatus = 'Approved', not 'PO Posted')
            this.allRows = rows.filter(r =>
              r.Status === 'Approved' && r.DisplayStatus === 'Approved'
            );
            break;
          case 'GRN':
            // Approved POs with no GRN yet
            this.allRows = rows.filter(r => r.Status === 'Approved');
            break;
          case 'TRANSFER':
            // Approved BSRs with no linked Transfer
            this.allRows = rows.filter(r =>
              r.Status === 'Approved' && (!r.LinkedTransferID || r.LinkedTransferID === 0)
            );
            break;
          case 'ISSUE':
            this.allRows = rows.filter(r => isThisMonth(r.IssueDate));
            break;
          case 'ADJUSTMENT':
            this.allRows = rows.filter(r =>
              r.Status === 'Draft' || r.Status === 'Submitted'
            );
            break;
          case 'APPROVAL':
            // getPendingApprovals() already returns only Submitted documents
            this.allRows = rows;
            break;
          default:
            this.allRows = rows;
        }
        this.filteredRows = [...this.allRows];
        this.loading = false;
      },
      error: (err: any) => {
        this.errorMessage = typeof err === 'string'
          ? err : 'Unable to load documents. Please try again.';
        this.loading = false;
      }
    });
  }

  // ── Search / filter ────────────────────────────────────────────────────────

  applyFilter(event: Event): void {
    const q = (event.target as HTMLInputElement).value.trim().toLowerCase();
    this.filterText = q;
    if (!q) { this.filteredRows = [...this.allRows]; return; }
    this.filteredRows = this.allRows.filter(row =>
      this.columns.some(col => {
        const v = row[col.key];
        return v != null && String(v).toLowerCase().includes(q);
      })
    );
  }

  // ── Navigation — document number link → view page ─────────────────────────

  navigateToDoc(row: any): void {
    this.dialogRef.close();
    switch (this.data.activityType) {
      case 'PO':
        this._router.navigate(['/inventory/purchase-requisition/new-pr', row.ID]);
        break;
      case 'GRN':
        this._router.navigate(['/inventory/purchase-order/new-po', row.ID]);
        break;
      case 'TRANSFER':
        this._router.navigate(['/inventory/branch-stock-request/new-bsr', row.ID]);
        break;
      case 'ISSUE':
        this._router.navigate(['/inventory/stock-issue/new-stock-issue', row.ID]);
        break;
      case 'ADJUSTMENT':
        this._router.navigate(['/inventory/stock-adjustment/new-stock-adjustment', row.ID]);
        break;
      case 'APPROVAL':
        // Reuse pending-approvals viewDocument logic
        this.viewApprovalDoc(row);
        break;
    }
  }

  // ── Navigation — Action button → action/create page ───────────────────────

  navigateTo(row: any): void {
    this.dialogRef.close();
    switch (this.data.activityType) {
      case 'PO':
        this._router.navigate(['/inventory/purchase-order/new-po'], {
          queryParams: { prID: row.ID }
        });
        break;
      case 'GRN':
        this._router.navigate(['/inventory/goods-receipt/new-grn'], {
          queryParams: { poID: row.ID }
        });
        break;
      case 'TRANSFER':
        this._router.navigate(['/inventory/branch-stock-request/new-bsr', row.ID]);
        break;
      case 'ISSUE':
        this._router.navigate(['/inventory/stock-issue/new-stock-issue', row.ID]);
        break;
      case 'ADJUSTMENT':
        this._router.navigate(['/inventory/stock-adjustment/new-stock-adjustment', row.ID]);
        break;
      case 'APPROVAL':
        // Navigate to the full Pending Approvals page
        this._router.navigate(['/administration/pending-approvals']);
        break;
    }
  }

  // ── Approval document navigation (mirrors pending-approvals.viewDocument) ──

  private viewApprovalDoc(row: any): void {
    switch (row.DocumentType) {
      case 'PURCHASE_REQUISITION':
        this._router.navigate(['/inventory/purchase-requisition/new-pr', row.DocumentID]);
        break;
      case 'PURCHASE_ORDER':
        this._router.navigate(['/inventory/purchase-order/new-po', row.DocumentID]);
        break;
      case 'BRANCH_STOCK_REQUEST':
        this._router.navigate(['/inventory/branch-stock-request/new-bsr', row.DocumentID]);
        break;
      case 'STOCK_ADJUSTMENT':
        this._router.navigate(['/inventory/stock-adjustment/new-stock-adjustment', row.DocumentID]);
        break;
      case 'ASSET_DISPOSAL':
        this._router.navigate(['/inventory/asset-disposal', row.DocumentID]);
        break;
      default:
        this._router.navigate(['/administration/pending-approvals']);
        break;
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  docNumber(row: any): string {
    switch (this.data.activityType) {
      case 'PO':         return row.PRNumber       ?? '';
      case 'GRN':        return row.PONumber       ?? '';
      case 'TRANSFER':   return row.RequestNo      ?? '';
      case 'ISSUE':      return row.IssueNo        ?? '';
      case 'ADJUSTMENT': return row.AdjustmentNo   ?? '';
      case 'APPROVAL':   return row.DocumentNumber ?? '';
      default:           return '';
    }
  }

  docDate(row: any): string {
    let d: any = null;
    switch (this.data.activityType) {
      case 'PO':         d = row.PRDate;         break;
      case 'GRN':        d = row.PODate;         break;
      case 'TRANSFER':   d = row.RequestDate;    break;
      case 'ISSUE':      d = row.IssueDate;      break;
      case 'ADJUSTMENT': d = row.AdjustmentDate; break;
      case 'APPROVAL':   d = row.DocumentDate;   break;
      default:           d = null;               break;
    }
    if (!d) return '';
    return new Date(d).toLocaleDateString('en-MY',
      { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatDate(v: any): string {
    if (!v) return '—';
    return new Date(v).toLocaleDateString('en-MY',
      { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatAmount(v: any): string {
    const n = parseFloat(v);
    if (isNaN(n)) return '—';
    return n.toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  cellValue(row: any, col: { key: string; label: string; align?: string }): string {
    const v = row[col.key];
    if (v == null || v === '') return '—';
    if (col.key.toLowerCase().endsWith('date')) return this.formatDate(v);
    if (col.key === 'TotalAmount' || col.key === 'GrandTotal') return this.formatAmount(v);
    return String(v);
  }

  statusClass(status: string): string {
    switch ((status ?? '').toLowerCase()) {
      case 'draft':     return 'pad-badge pad-badge--draft';
      case 'submitted': return 'pad-badge pad-badge--submitted';
      case 'approved':  return 'pad-badge pad-badge--approved';
      case 'rejected':  return 'pad-badge pad-badge--rejected';
      default:          return 'pad-badge';
    }
  }

  isStatusColumn(key: string): boolean {
    // Both 'Status' and 'CurrentStatus' (used by APPROVAL) get badge styling
    return key === 'Status' || key === 'CurrentStatus';
  }

  isDocNumColumn(key: string): boolean {
    return ['PRNumber','PONumber','RequestNo','IssueNo','AdjustmentNo','DocumentNumber'].includes(key);
  }

  isAmountColumn(key: string): boolean {
    return key === 'TotalAmount' || key === 'GrandTotal';
  }

  close(): void { this.dialogRef.close(); }
}
