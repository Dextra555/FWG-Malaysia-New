import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../service/inventory.service';
import { MastermoduleService } from '../../../service/mastermodule.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { DialogConfirmationComponent } from '../../../components/dialog-confirmation/dialog-confirmation.component';
import { UserAccessModel } from '../../../model/userAccesModel';

export interface IOpeningStock {
  ID: number;
  ReferenceNo: string;
  WarehouseName: string;
  ItemCode: string;
  ItemName: string;
  CategoryName: string;
  UOMName: string;
  OpeningDate: Date;
  OpeningQty: number;
  UnitCost: number;
  TotalValue: number;
  Remarks: string;
}

// Excel import row
interface ImportRow {
  rowNumber:     number;
  itemCode:      string;
  warehouseCode: string;
  openingDate:   string;
  openingQty:    number;
  unitCost:      number;
  remarks:       string;
  // UI state
  status:  'pending' | 'success' | 'error' | 'skipped';
  message: string;
  refNo:   string;
}

@Component({
  selector: 'app-opening-stock',
  templateUrl: './opening-stock.component.html',
  styleUrls: ['./opening-stock.component.css']
})
export class OpeningStockComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild('fileInput') fileInputRef!: ElementRef<HTMLInputElement>;

  ngAfterViewInit(): void {
    if (this.dataSource) {
      this.dataSource.paginator = this.paginator;
      this.dataSource.sort = this.sort;
    }
  }

  userAccessModel!: UserAccessModel;
  currentUser    = '';
  userRole       = '';   // 'superadmin' | 'admin' | 'user'
  warningMessage = '';
  errorMessage   = '';
  showLoadingSpinner = false;

  displayedColumns: string[] = [
    'ReferenceNo', 'OpeningDate', 'WarehouseName', 'ItemCode', 'ItemName',
    'CategoryName', 'UOMName', 'OpeningQty', 'UnitCost', 'TotalValue', 'action'
  ];
  dataSource: MatTableDataSource<IOpeningStock> = new MatTableDataSource<IOpeningStock>([]);
  pageSizeOptions: number[] = [];
  filterValue = '';

  // â”€â”€ Import state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  showImportPanel   = false;
  importRows:       ImportRow[] = [];
  importFileName    = '';
  importFileError   = '';   // error shown below file input
  importProcessing  = false;
  importDone        = false;
  importSuccessCount = 0;
  importFailCount    = 0;

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    public dialog: MatDialog,
    private service: InventoryService,
    private route: Router,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ,
    private titleService: Title) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    for (let i = 10; i <= 50; i += 10) this.pageSizeOptions.push(i);
  }

  ngOnInit(): void {
    this.titleService.setTitle('Opening Stock');
    this.currentUser = sessionStorage.getItem('username')!;
    const rawRole = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = rawRole === '2' ? 'superadmin' : rawRole === '1' ? 'admin' : 'user';
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Opening Stock');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.showLoadingSpinner = true;
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
          if (this.userAccessModel.readAccess || this.currentUser === 'superadmin') {
            this.loadList();
          } else {
            this.warningMessage = `Dear <b>${this.currentUser}</b>,<br>You do not have permission to view this page.`;
            this.hideLoadingSpinner();
          }
        }
      },
      () => { this.errorMessage = 'Unable to load this page right now. Please try again.'; this.hideLoadingSpinner(); }
    );
  }

  loadList(): void {
    this.service.getOpeningStockList().subscribe({
      next: (d: any[]) => {
        this.dataSource = new MatTableDataSource(d || []);
        this.pageSizeOptions = [];
        const total = (d || []).length;
        for (let i = 10; i <= total && i <= 1000; i += 10) this.pageSizeOptions.push(i);
        if (total > 0 && total < 10) this.pageSizeOptions.push(total);
        this.dataSource.sort      = this.sort;
        this.dataSource.paginator = this.paginator;
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load the opening stock list right now. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  applyFilter(event: Event): void {
    const v = (event.target as HTMLInputElement).value;
    this.filterValue = v;
    this.dataSource.filter = v.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort): void {
    if (sortState.direction) this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    else this._liveAnnouncer.announce('Sorting cleared');
  }

  deleteClickButton(row: IOpeningStock): void {
    this.dialog.open(DialogConfirmationComponent, {
      data: `Are you sure you want to remove the opening stock for <b>${row.ItemName}</b> in <b>${row.WarehouseName}</b>?<br>This will also remove the corresponding stock ledger entry.`
    }).afterClosed().subscribe((result: { confirmDialog: boolean }) => {
      if (result?.confirmDialog) {
        this.showLoadingSpinner = true;
        this.service.deleteOpeningStock(row.ID, this.currentUser).subscribe({
          next: (res: any) => { this.showMsg(res?.message || 'Opening stock removed.', 'success', 'Success Message'); this.loadList(); },
          error: (err: any) => { this.showMsg(this.extractErr(err, 'Unable to remove the entry.'), 'error', 'Error Message'); this.hideLoadingSpinner(); }
        });
      }
    });
  }

  // â”€â”€ Excel Import â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  downloadTemplate(): void {
    // Build CSV template
    const header  = 'ItemCode,WarehouseCode,OpeningDate,OpeningQty,UnitCost,Remarks';
    const sample1 = 'ITM0001,WH01,2026-01-01,100,25.50,Initial stock';
    const sample2 = 'ITM0002,WH01,2026-01-01,50,10.00,';
    const csvContent = [header, sample1, sample2].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href = url; a.download = 'OpeningStock_ImportTemplate.csv';
    a.click(); URL.revokeObjectURL(url);
  }

  onImportClick(): void {
    this.showImportPanel  = true;
    this.importRows       = [];
    this.importFileName   = '';
    this.importFileError  = '';
    this.importDone       = false;
    this.importSuccessCount = 0;
    this.importFailCount    = 0;
    if (this.fileInputRef?.nativeElement) {
      this.fileInputRef.nativeElement.value = '';
    }
  }

  cancelImport(): void {
    this.showImportPanel = false;
    this.importRows      = [];
    this.importFileName  = '';
    this.importFileError = '';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    this.importFileError = '';
    this.importRows      = [];
    this.importDone      = false;

    if (!file) return;

    const allowed = ['.csv', '.xlsx', '.xls'];
    const ext     = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    if (!allowed.includes(ext)) {
      this.importFileError = `Invalid file type "${ext}". Please upload a .csv file.`;
      this.importFileName  = '';
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.importFileError = 'File size exceeds 5 MB. Please upload a smaller file.';
      this.importFileName  = '';
      return;
    }

    this.importFileName = file.name;
    const reader = new FileReader();
    reader.onload = (e) => {
      const text = e.target?.result as string;
      if (!text || text.trim().length === 0) {
        this.importFileError = 'The file is empty. Please upload a file with data.';
        this.importFileName  = '';
        return;
      }
      this.parseCSV(text);
      if (this.importRows.length === 0) {
        this.importFileError = 'No valid data rows found. Check that the file matches the template format.';
      }
    };
    reader.onerror = () => {
      this.importFileError = 'Failed to read the file. Please try again.';
      this.importFileName  = '';
    };
    reader.readAsText(file);
  }

  private parseCSV(text: string): void {
    const lines = text.split(/\r?\n/).filter(l => l.trim());
    if (lines.length < 2) {
      this.showMsg('File is empty or has no data rows.', 'warning', 'Warning Message');
      return;
    }

    // Skip header row (row 0)
    this.importRows = [];
    for (let i = 1; i < lines.length; i++) {
      const cols = lines[i].split(',');
      if (cols.length < 5) continue; // skip malformed rows

      const row: ImportRow = {
        rowNumber:     i,
        itemCode:      (cols[0] || '').trim(),
        warehouseCode: (cols[1] || '').trim(),
        openingDate:   (cols[2] || '').trim(),
        openingQty:    parseFloat(cols[3]) || 0,
        unitCost:      parseFloat(cols[4]) || 0,
        remarks:       (cols[5] || '').trim(),
        status:        'pending',
        message:       '',
        refNo:         ''
      };

      // Client-side basic validation
      if (!row.itemCode)      { row.status = 'error'; row.message = 'Item code is required.'; }
      else if (!row.warehouseCode) { row.status = 'error'; row.message = 'Warehouse code is required.'; }
      else if (!row.openingDate)   { row.status = 'error'; row.message = 'Opening date is required.'; }
      else if (row.openingQty <= 0) { row.status = 'error'; row.message = 'Quantity must be > 0.'; }
      else if (row.unitCost < 0)    { row.status = 'error'; row.message = 'Unit cost cannot be negative.'; }

      this.importRows.push(row);
    }
  }

  onImportSubmit(): void {
    const validRows = this.importRows.filter(r => r.status !== 'error');
    if (validRows.length === 0) {
      this.showMsg('No valid rows to import. Please fix the errors and re-upload.', 'warning', 'Warning Message');
      return;
    }

    this.importProcessing = true;
    const payload = {
      Rows: validRows.map(r => ({
        RowNumber:     r.rowNumber,
        ItemCode:      r.itemCode,
        WarehouseCode: r.warehouseCode,
        OpeningDate:   r.openingDate,
        OpeningQty:    r.openingQty,
        UnitCost:      r.unitCost,
        Remarks:       r.remarks
      })),
      LastUpdatedBy: this.currentUser
    };

    this.service.bulkImportOpeningStock(payload).subscribe({
      next: (res: any) => {
        this.importProcessing   = false;
        this.importDone         = true;
        this.importSuccessCount = res.successCount || 0;
        this.importFailCount    = res.failCount    || 0;

        // Update row statuses from API response
        const apiResults: any[] = res.results || [];
        for (const apiRow of apiResults) {
          const local = this.importRows.find(r => r.rowNumber === apiRow.RowNumber);
          if (local) {
            local.status  = apiRow.Success ? 'success' : 'error';
            local.message = apiRow.Message || '';
            local.refNo   = apiRow.ReferenceNo || '';
          }
        }

        if (this.importSuccessCount > 0) {
          this.loadList(); // refresh table
          this.showMsg(res.message, 'success', 'Success Message');
        } else {
          this.showMsg(res.message, 'warning', 'Warning Message');
        }
      },
      error: (err: any) => {
        this.importProcessing = false;
        this.showMsg(this.extractErr(err, 'Import failed. Please try again.'), 'error', 'Error Message');
      }
    });
  }

  getRowStatusClass(row: ImportRow): string {
    if (row.status === 'success') return 'import-row-success';
    if (row.status === 'error')   return 'import-row-error';
    return '';
  }

  get pendingRowCount(): number  { return this.importRows.filter(r => r.status === 'pending').length; }
  get errorRowCount(): number    { return this.importRows.filter(r => r.status === 'error').length; }

  // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

  formatDate(dateVal: any): string {
    if (!dateVal) return '';
    return new Date(dateVal).toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatNumber(val: number): string {
    return (val ?? 0).toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  private extractErr(err: any, fallback: string): string {
    if (err && typeof err === 'string') return err;
    if (err?.error?.message) return err.error.message;
    if (err?.message) return err.message;
    return fallback;
  }

  private showMsg(message: string, icon: 'success' | 'warning' | 'info' | 'error',
    title: 'Success Message' | 'Warning Message' | 'Error Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon,
      showCloseButton: false, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }
}

