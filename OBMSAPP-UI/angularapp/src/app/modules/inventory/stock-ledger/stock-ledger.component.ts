import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { UserAccessModel } from '../../../model/userAccesModel';

/** Custom validator: rejects null, undefined, empty string, and 0 — for required dropdowns */
function mustSelect(): ValidatorFn {
  return (control: AbstractControl) => {
    const v = control.value;
    if (v === null || v === undefined || v === '' || +v === 0) {
      return { mustSelect: true };
    }
    return null;
  };
}

export interface IStockLedgerRow {
  ID: number;
  TransactionNo: string;
  TransactionDate: Date;
  TransactionType: string;
  TransactionTypeLabel: string;
  WarehouseName: string;
  ItemCode: string;
  ItemName: string;
  UOMName: string;
  InQty: number;
  OutQty: number;
  RunningBalance: number;
  UnitCost: number | null;
  ReferenceNo: string;
  Remarks: string;
}

/** Labelled transaction types for the filter dropdown */
export const TRANSACTION_TYPE_OPTIONS = [
  { value: 'ALL',          label: 'All Transaction Types' },
  { value: 'OPENING',      label: 'Opening Stock'         },
  { value: 'STOCK_IN',     label: 'Stock In'              },
  { value: 'ISSUE',        label: 'Stock Issue'           },
  { value: 'ISSUE_RETURN', label: 'Issue Return'          },
  { value: 'TRANSFER_IN',  label: 'Transfer In'           },
  { value: 'TRANSFER_OUT', label: 'Transfer Out'          },
  { value: 'ADJ_PLUS',     label: 'Adjustment (In)'       },
  { value: 'ADJ_MINUS',    label: 'Adjustment (Out)'      },
  { value: 'PURCHASE_RTN', label: 'Purchase Return'       },
  { value: 'DAMAGE_LOSS',  label: 'Damage / Loss'         },
];

@Component({
  selector: 'app-stock-ledger',
  templateUrl: './stock-ledger.component.html',
  styleUrls: ['./stock-ledger.component.css']
})
export class StockLedgerComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) {
      this.dataSource.paginator = this.paginator;
      this.dataSource.sort = this.sort;
    }
  }

  filterForm!: FormGroup;

  warehouses: any[] = [];
  items: any[] = [];
  transactionTypeOptions = TRANSACTION_TYPE_OPTIONS;

  displayedColumns: string[] = [
    'TransactionDate', 'TransactionNo', 'TransactionTypeLabel',
    'ItemCode', 'ItemName', 'UOMName',
    'InQty', 'OutQty', 'RunningBalance',
    'ReferenceNo', 'Remarks'
  ];
  dataSource: MatTableDataSource<IStockLedgerRow> = new MatTableDataSource<IStockLedgerRow>([]);
  pageSizeOptions: number[] = [10, 20, 50, 100];

  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  noDataMessage: string = '';
  showLoadingSpinner: boolean = false;
  hasSearched: boolean = false;

  /** Summary totals shown below the table */
  totalInQty: number = 0;
  totalOutQty: number = 0;
  closingBalance: number = 0;
  tableFilterValue: string = '';

  /** Print support */
  printRows: IStockLedgerRow[] = [];
  printSummary = { warehouseName: '', itemName: '', transactionType: '', dateFrom: '', dateTo: '' };
  printGeneratedOn: string = '';

  constructor(
    private fb: FormBuilder,
    private _liveAnnouncer: LiveAnnouncer,
    private service: InventoryService,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };

    const today = new Date();
    const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);

    this.filterForm = this.fb.group({
      WarehouseID:     [0, [Validators.required, mustSelect()]],
      ItemID:          [0],           // 0 = all items
      DateFrom:        [firstOfMonth, Validators.required],
      DateTo:          [today,        Validators.required],
      TransactionType: ['ALL']
    });
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Stock Ledger');
    this.loadWarehouses();
    this.loadItems();
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;

          if (!this.userAccessModel.readAccess && this.currentUser !== 'superadmin') {
            this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>
              You do not have permission to view this page.<br>
              Please contact your administrator.`;
          }
        }
      },
      () => {
        this.errorMessage = 'Unable to load page permissions. Please try again.';
      }
    );
  }

  loadWarehouses(): void {
    this.service.getWarehouseMasters().subscribe({
      next: (d: any[]) => { this.warehouses = (d || []).filter((w: any) => w.IsActive); },
      error: () => { this.errorMessage = 'Unable to load warehouse list. Please refresh the page.'; }
    });
  }

  loadItems(): void {
    this.service.getItemsForStock().subscribe({
      next: (d: any[]) => { this.items = d || []; },
      error: () => { /* non-critical — item filter is optional */ }
    });
  }

  onViewLedger(): void {
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      return;
    }

    const raw = this.filterForm.value;

    // Client-side date validation
    if (raw.DateFrom && raw.DateTo) {
      const from = new Date(raw.DateFrom);
      const to   = new Date(raw.DateTo);
      if (from > to) {
        this.errorMessage = "The 'From' date must be before the 'To' date.";
        return;
      }
    }

    this.errorMessage    = '';
    this.noDataMessage   = '';
    this.showLoadingSpinner = true;
    this.hasSearched     = false;

    const dateFrom = this.toDateString(raw.DateFrom);
    const dateTo   = this.toDateString(raw.DateTo);

    this.service.getStockLedger(
      +raw.WarehouseID,
      raw.ItemID > 0 ? +raw.ItemID : null,
      dateFrom,
      dateTo,
      raw.TransactionType || 'ALL'
    ).subscribe({
      next: (rows: any[]) => {
        this.hasSearched = true;
        if (rows && rows.length > 0) {
          this.dataSource = new MatTableDataSource<IStockLedgerRow>(rows);
          this.dataSource.sort      = this.sort;
          this.dataSource.paginator = this.paginator;
          this.calculateSummary(rows);
          this.noDataMessage = '';
          // Capture full unfiltered/unpaginated rows for print
          this.printRows = rows;
          this.printSummary = {
            warehouseName:   this.warehouses.find(w => w.ID === +raw.WarehouseID)?.Name ?? 'All',
            itemName:        raw.ItemID > 0
                               ? (this.items.find(i => i.ID === +raw.ItemID)?.Name ?? raw.ItemID)
                               : 'All Items',
            transactionType: raw.TransactionType === 'ALL' ? 'All Types'
                               : (this.transactionTypeOptions.find(t => t.value === raw.TransactionType)?.label ?? raw.TransactionType),
            dateFrom:        this.formatDate(raw.DateFrom),
            dateTo:          this.formatDate(raw.DateTo),
          };
        } else {
          this.dataSource    = new MatTableDataSource<IStockLedgerRow>([]);
          this.noDataMessage = 'No stock transactions found for the selected filters. Try adjusting the date range or filters.';
          this.resetSummary();
        }
        this.hideLoadingSpinner();
      },
      error: (err: any) => {
        const msg = this.extractErrorMessage(err,
          'Unable to load the stock ledger right now. Please try again.');
        this.errorMessage = msg;
        this.resetSummary();
        this.hideLoadingSpinner();
      }
    });
  }

  onClearFilters(): void {
    const today = new Date();
    const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    this.filterForm.patchValue({
      WarehouseID: 0, ItemID: 0,
      DateFrom: firstOfMonth, DateTo: today,
      TransactionType: 'ALL'
    });
    this.dataSource    = new MatTableDataSource<IStockLedgerRow>([]);
    this.errorMessage  = '';
    this.noDataMessage = '';
    this.hasSearched   = false;
    this.printRows     = [];
    this.resetSummary();
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.tableFilterValue = filterValue;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort): void {
    if (sortState.direction) {
      this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this._liveAnnouncer.announce('Sorting cleared');
    }
  }

  private calculateSummary(rows: any[]): void {
    this.totalInQty    = rows.reduce((s, r) => s + (r.InQty  || 0), 0);
    this.totalOutQty   = rows.reduce((s, r) => s + (r.OutQty || 0), 0);
    // Closing balance = last row's running balance (already sorted by date asc in API)
    this.closingBalance = rows.length > 0 ? rows[rows.length - 1].RunningBalance : 0;
  }

  private resetSummary(): void {
    this.totalInQty = this.totalOutQty = this.closingBalance = 0;
  }

  formatDate(dateVal: any): string {
    if (!dateVal) return '';
    return new Date(dateVal).toLocaleDateString('en-MY', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
  }

  formatNumber(val: number | null): string {
    return ((val ?? 0)).toLocaleString('en-MY', {
      minimumFractionDigits: 2, maximumFractionDigits: 2
    });
  }

  /** Returns CSS class for the running balance cell */
  balanceClass(balance: number): string {
    if (balance < 0) return 'text-danger font-weight-bold';
    if (balance === 0) return 'text-muted';
    return 'text-dark';
  }

  private toDateString(date: Date | string): string {
    const d = new Date(date);
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private extractErrorMessage(err: any, fallback: string): string {
    if (err && typeof err === 'string') return err;
    if (err?.error?.message) return err.error.message;
    if (err?.message)        return err.message;
    return fallback;
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  /** Print / Save as PDF — uses browser's native print dialog.
   *  @media print CSS in the component stylesheet hides everything
   *  except #printSection, producing a clean A4 printout. */
  onPrint(): void {
    this.printGeneratedOn = new Date().toLocaleString('en-MY', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit', hour12: true
    });
    // Let Angular render the print section first, then open the dialog
    setTimeout(() => window.print(), 150);
  }
}
