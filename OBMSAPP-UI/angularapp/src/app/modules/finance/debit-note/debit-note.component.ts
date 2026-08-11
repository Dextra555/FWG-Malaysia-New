import { Component, AfterViewInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatDialog } from '@angular/material/dialog';
import Swal from 'sweetalert2';
import { FinanceService } from 'src/app/service/finance.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DialogConfirmationComponent } from 'src/app/components/dialog-confirmation/dialog-confirmation.component';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-debit-note',
  templateUrl: './debit-note.component.html',
  styleUrls: ['./debit-note.component.css']
})
export class DebitNoteComponent implements AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns: string[] = [
    'DebitNoteNo', 'DebitNoteDate', 'Client',
    'ReferenceInvoiceNo', 'DebitNoteAmount', 'TaxAmount', 'TotalAmount', 'actions'
  ];
  dataSource = new MatTableDataSource<any>();

  frm!: FormGroup;
  branchList: any[] = [];
  clientList: any[] = [];

  currentUser: string = '';
  userAccessModel!: UserAccessModel;
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  selectedID: number = 0;

  constructor(
    private fb: FormBuilder,
    private _financeService: FinanceService,
    private _masterService: MastermoduleService,
    private _dataService: DatasharingService,
    public dialog: MatDialog,
    private _liveAnnouncer: LiveAnnouncer
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => this.currentUser = u);
    }

    this.frm = this.fb.group({
      ID: [0],
      DebitNoteNo: [''],
      Branch: ['', Validators.required],
      Client: ['', Validators.required],
      ClientInvoiceID: [null],
      DebitNoteDate: [new Date(), Validators.required],
      DebitNoteAmount: [0, [Validators.required, Validators.min(0.01)]],
      TaxPercentage: [0],
      TaxAmount: [{ value: 0, disabled: true }],
      TotalAmount: [{ value: 0, disabled: true }],
      Reason: [''],
      ReferenceInvoiceNo: [''],
    });

    this.getUserAccessRights(this.currentUser, 'DebitNote');
  }

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  applyFilter(event: Event) {
    this.dataSource.filter = (event.target as HTMLInputElement).value.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort) {
    if (sortState.direction) this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    else this._liveAnnouncer.announce('Sorting cleared');
  }

  getUserAccessRights(userName: string, screenName: string) {
    if (userName === 'superadmin' || userName === 'admin') {
      this.userAccessModel = { readAccess: true, createAccess: true, updateAccess: true, deleteAccess: true };
      this.warningMessage = '';
      this.loadBranches();
      return;
    }
    this._masterService.getUserAccessRights(userName, screenName).subscribe(data => {
      if (data) {
        this.userAccessModel.readAccess = data.Read;
        this.userAccessModel.deleteAccess = data.Delete;
        this.userAccessModel.updateAccess = data.Update;
        this.userAccessModel.createAccess = data.Create;
        if (data.Read) {
          this.warningMessage = '';
          this.loadBranches();
        } else {
          this.warningMessage = `Dear <B>${userName}</B>, you do not have permission to view this page.`;
        }
      }
    });
  }

  loadBranches() {
    this._masterService.GetBranchListByUserName(this.currentUser).subscribe((data: any) => {
      this.branchList = data;
      // Load all records on page open
      this.loadDebitNotes();
    });
  }

  onBranchChange(branchCode: string) {
    this.frm.patchValue({ Client: '', DebitNoteNo: '' });
    this.clientList = [];
    if (branchCode) {
      this._masterService.getClientMsterListByBranch(branchCode).subscribe((data: any) => {
        this.clientList = data;
      });
    }
    // Reload list filtered by selected branch
    this.loadDebitNotes();
  }

  onClientChange(clientCode: string) {
    const branch = this.frm.get('Branch')?.value;
    const date = this.formatDate(this.frm.get('DebitNoteDate')?.value);
    if (branch && clientCode && this.selectedID === 0) {
      this._financeService.generateDebitNoteNo(branch, date).subscribe((no: any) => {
        this.frm.patchValue({ DebitNoteNo: no });
      });
    }
    this.loadDebitNotes();
  }

  // Regenerate note number when date is changed (month/year in number must update)
  onDateChange() {
    const branch = this.frm.get('Branch')?.value;
    const client = this.frm.get('Client')?.value;
    const date = this.formatDate(this.frm.get('DebitNoteDate')?.value);
    if (branch && client && this.selectedID === 0) {
      this._financeService.generateDebitNoteNo(branch, date).subscribe((no: any) => {
        this.frm.patchValue({ DebitNoteNo: no });
      });
    }
  }

  calculateTax() {
    const amount = parseFloat(this.frm.get('DebitNoteAmount')?.value) || 0;
    const taxPct = parseFloat(this.frm.get('TaxPercentage')?.value) || 0;
    const taxAmt = parseFloat((amount * taxPct / 100).toFixed(2));
    const total = parseFloat((amount + taxAmt).toFixed(2));
    this.frm.patchValue({ TaxAmount: taxAmt, TotalAmount: total });
  }

  loadDebitNotes() {
    const branch = this.frm.get('Branch')?.value || '';
    const client = this.frm.get('Client')?.value || '';

    this.showLoadingSpinner = true;
    this._financeService.getDebitNotes(branch, client).subscribe({
      next: (data: any[]) => {
        this.dataSource.data = data;
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        this.hideSpinner();
      },
      error: () => this.hideSpinner()
    });
  }

  onRowEdit(row: any) {
    this.selectedID = row.ID;
    if (row.Branch) {
      this._masterService.getClientMsterListByBranch(row.Branch).subscribe((data: any) => {
        this.clientList = data;
      });
    }
    this.frm.patchValue({
      ID: row.ID,
      DebitNoteNo: row.DebitNoteNo,
      Branch: row.Branch,
      Client: row.Client,
      ClientInvoiceID: row.ClientInvoiceID,
      DebitNoteDate: new Date(row.DebitNoteDate),
      DebitNoteAmount: row.DebitNoteAmount,
      TaxPercentage: row.TaxPercentage,
      TaxAmount: row.TaxAmount,
      TotalAmount: row.TotalAmount,
      Reason: row.Reason,
      ReferenceInvoiceNo: row.ReferenceInvoiceNo
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  onRowDelete(row: any) {
    this.dialog.open(DialogConfirmationComponent, {
      data: `Are you sure you want to delete Debit Note: ${row.DebitNoteNo}?`
    }).afterClosed().subscribe((result: any) => {
      if (result?.confirmDialog) {
        this._financeService.deleteDebitNote(row.ID, this.currentUser).subscribe({
          next: () => {
            this.showMessage('Debit Note deleted successfully', 'warning', 'Warning Message');
            this.loadDebitNotes();
            if (this.selectedID === row.ID) this.onNewClick();
          },
          error: () => this.showMessage('Delete failed', 'error', 'Error Message')
        });
      }
    });
  }

  onNewClick() {
    this.selectedID = 0;
    this.frm.reset({
      ID: 0, DebitNoteDate: new Date(),
      DebitNoteAmount: 0, TaxPercentage: 0, TaxAmount: 0, TotalAmount: 0
    });
  }

  onSubmit() {
    if (this.frm.invalid) return;
    const data = this.frm.getRawValue();
    data.LastUpdatedBy = this.currentUser;
    data.DebitNoteDate = this.formatDate(data.DebitNoteDate);

    this.showLoadingSpinner = true;
    const req$ = this.selectedID > 0
      ? this._financeService.updateDebitNote(data)
      : this._financeService.saveDebitNote(data);

    req$.subscribe({
      next: (result: any) => {
        this.showMessage(
          this.selectedID > 0 ? 'Debit Note updated successfully' : 'Debit Note saved successfully',
          'success', 'Success Message'
        );
        this.loadDebitNotes();
        this.onNewClick();

        // Open Crystal Report after successful save/update
        const savedId     = result?.ID     ?? result?.id     ?? data.ID;
        const savedBranch = result?.Branch ?? result?.branch ?? data.Branch;
        const savedClient = result?.Client ?? result?.client ?? data.Client;
        const savedDate   = result?.DebitNoteDate ?? result?.debitNoteDate ?? data.DebitNoteDate;
        if (savedId > 0) {
          this.openPrintWindow(savedId, savedBranch, savedClient, savedDate);
        }
      },
      error: () => {
        this.showMessage('Failed to save Debit Note', 'error', 'Error Message');
        this.hideSpinner();
      }
    });
  }

  openPrintWindow(id: number, branch: string, client: string, noteDate: string) {
    const loginID = sessionStorage.getItem('username') || '';

    // Use the document's own month as date range so the report always finds it
    const d = noteDate ? new Date(noteDate) : new Date();
    const year = d.getFullYear();
    const month = d.getMonth(); // 0-based
    const startDate = `${year}-${String(month + 1).padStart(2, '0')}-01`;
    const lastDay = new Date(year, month + 1, 0).getDate();
    const endDate = `${year}-${String(month + 1).padStart(2, '0')}-${String(lastDay).padStart(2, '0')}`;

    window.open(
      `${environment.baseReportUrl}Finance/DebitNoteReport.aspx?ID=${id}&Branch=${encodeURIComponent(branch)}&Client=${encodeURIComponent(client)}&StartDate=${startDate}&EndDate=${endDate}&LoginID=${encodeURIComponent(loginID)}`,
      '_blank'
    );
  }

  onPrint(row: any) {
    this.openPrintWindow(row.ID, row.Branch, row.Client, row.DebitNoteDate);
  }

  formatDate(date: any): string {
    const d = date ? new Date(date) : new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  private showMessage(
    message: string,
    icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message'
  ): void {
    Swal.fire({
      toast: true, position: 'top', showConfirmButton: false,
      title, text: message, icon, showCloseButton: false, timer: 5000,
      width: '600px', customClass: { popup: 'swal-top-offset' }
    });
    this.hideSpinner();
  }

  hideSpinner() { this.showLoadingSpinner = false; }
}
