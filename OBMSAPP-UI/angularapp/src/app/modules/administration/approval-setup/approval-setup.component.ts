import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import Swal from 'sweetalert2';

import { MastermoduleService } from '../../../service/mastermodule.service';
import { InventoryService } from '../../../service/inventory.service';
import { DatasharingService } from '../../../service/datasharing.service';
import { DialogConfirmationComponent } from '../../../components/dialog-confirmation/dialog-confirmation.component';
import { UserAccessModel } from '../../../model/userAccesModel';

@Component({
  selector: 'app-approval-setup',
  templateUrl: './approval-setup.component.html',
  styleUrls: ['./approval-setup.component.css']
})
export class ApprovalSetupComponent implements OnInit, AfterViewInit {

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  // ── Access ────────────────────────────────────────────────────────────────
  userAccessModel!: UserAccessModel;
  currentUser = '';
  userRole    = '';   // BUG-09/10 FIX: '0'→'user', '1'→'admin', '2'→'superadmin'
  warningMessage = '';
  errorMessage   = '';

  // ── State ─────────────────────────────────────────────────────────────────
  showLoadingSpinner = false;
  showForm           = false;
  isEditMode         = false;
  editingId          = 0;
  filterValue        = '';

  // ── Table ─────────────────────────────────────────────────────────────────
  displayedColumns = [
    'DocumentTypeDisplay', 'RequesterUser', 'ApproverUser', 'IsActive', 'action'
  ];
  dataSource = new MatTableDataSource<any>([]);
  pageSizeOptions = [10, 20, 30, 50];

  // ── Form ──────────────────────────────────────────────────────────────────
  form!: FormGroup;

  // ── Dropdowns ─────────────────────────────────────────────────────────────
  documentTypes = [
    { value: 'PURCHASE_REQUISITION', label: 'Purchase Requisition'  },
    { value: 'PURCHASE_ORDER',       label: 'Purchase Order'        },
    { value: 'STOCK_ADJUSTMENT',     label: 'Stock Adjustment'      },
    { value: 'STOCK_TRANSFER',       label: 'Stock Transfer'        },
    { value: 'DAMAGE_LOSS',          label: 'Damage / Loss'         },
    { value: 'ASSET_DISPOSAL',       label: 'Asset Disposal'        },
    { value: 'BRANCH_STOCK_REQUEST', label: 'Branch Stock Request'  }
  ];
  users: any[] = [];

  constructor(
    private fb: FormBuilder,
    private dialog: MatDialog,
    private _liveAnnouncer: LiveAnnouncer,
    private _masterService: MastermoduleService,
    private _inventoryService: InventoryService,
    private _dataService: DatasharingService
  ) {
    this.userAccessModel = {
      readAccess: false, updateAccess: false,
      deleteAccess: false, createAccess: false
    };
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username') ?? '';
    if (!this.currentUser) {
      this._dataService.getUsername().subscribe(u => {
        this.currentUser = u;
        this.init();
      });
    } else {
      this.init();
    }
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort      = this.sort;
  }

  private init(): void {
    // BUG-09 FIX: load role from sessionStorage 'userrole' (set at login from OBMSUsers.IsAdmin)
    const raw = sessionStorage.getItem('userrole') ?? '0';
    this.userRole = raw === '2' ? 'superadmin' : raw === '1' ? 'admin' : 'user';
    this.buildForm();
    this.getUserAccessRights(this.currentUser, 'Approval Setup');
  }

  private buildForm(): void {
    this.form = this.fb.group({
      DocumentType:  ['', Validators.required],
      RequesterUser: ['', Validators.required],
      ApproverUser:  ['', Validators.required],
      IsActive:      [true]
    });
  }

  // ── Permissions ───────────────────────────────────────────────────────────

  getUserAccessRights(userName: string, screenName: string): void {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      data => {
        if (data) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.createAccess = data.Create;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.deleteAccess = data.Delete;
        }
        // BUG-09 FIX: use userRole (from sessionStorage 'userrole'), not username string
        if (this.userAccessModel.readAccess || this.userRole === 'superadmin') {
          this.loadUsers();
          this.loadList();
        } else {
          this.warningMessage =
            `Dear <b>${this.currentUser}</b>,<br>` +
            `You do not have permission to view Approval Setup.<br>` +
            `Please contact your administrator.`;
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

  loadUsers(): void {
    this._masterService.getObmsusers().subscribe({
      next: (users: any[]) => {
        this.users = (users || [])
          .filter((u: any) => !u.IsDeleted)
          .sort((a: any, b: any) => (a.Name ?? '').localeCompare(b.Name ?? ''));
      },
      error: () => { /* non-critical — form dropdowns will be empty */ }
    });
  }

  loadList(): void {
    this.showLoadingSpinner = true;
    this._inventoryService.getApprovalRoutings().subscribe({
      next: (data: any[]) => {
        this.dataSource = new MatTableDataSource<any>(data || []);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort      = this.sort;
        this.hideSpinner();
      },
      error: () => {
        this.errorMessage = 'Unable to load approval routing configurations. Please try again.';
        this.hideSpinner();
      }
    });
  }

  // ── Table Helpers ─────────────────────────────────────────────────────────

  applyFilter(e: Event): void {
    this.filterValue = (e.target as HTMLInputElement).value;
    this.dataSource.filter = this.filterValue.trim().toLowerCase();
  }

  announceSortChange(s: Sort): void {
    this._liveAnnouncer.announce(
      s.direction ? `Sorted ${s.direction}ending` : 'Sorting cleared'
    );
  }

  formatDate(d: any): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('en-MY', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
  }

  // ── Form Show / Hide ──────────────────────────────────────────────────────

  openNewForm(): void {
    this.isEditMode = false;
    this.editingId  = 0;
    this.form.reset({ IsActive: true });
    this.showForm   = true;
    this.errorMessage = '';
  }

  editRow(row: any): void {
    this.isEditMode = true;
    this.editingId  = row.ID;
    this.form.patchValue({
      DocumentType:  row.DocumentType,
      RequesterUser: row.RequesterUser,
      ApproverUser:  row.ApproverUser,
      IsActive:      row.IsActive === true || row.IsActive === 1  // guard: cast to boolean
    });
    this.showForm     = true;
    this.errorMessage = '';
  }

  cancelForm(): void {
    this.showForm = false;
    this.form.reset({ IsActive: true });
    this.errorMessage = '';
  }

  // ── Save ──────────────────────────────────────────────────────────────────

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.value;

    // Client-side validation: requester ≠ approver
    if (v.RequesterUser === v.ApproverUser) {
      this.showMsg('Requester and approver cannot be the same user.', 'warning', 'Warning');
      return;
    }

    const payload = {
      ID:            this.editingId,
      DocumentType:  v.DocumentType,
      RequesterUser: v.RequesterUser,
      ApproverUser:  v.ApproverUser,
      IsActive:      v.IsActive === true || v.IsActive === 1,   // always a real boolean
      LastUpdatedBy: this.currentUser
    };

    this.showLoadingSpinner = true;
    this._inventoryService.saveApprovalRouting(payload).subscribe({
      next: (res: any) => {
        this.showMsg(
          res?.message || 'Approval configuration saved successfully.',
          'success', 'Success'
        );
        this.cancelForm();
        this.loadList();
      },
      error: (err: any) => {
        this.showMsg(
          this.extractMsg(err, 'Unable to save the configuration. Please try again.'),
          'error', 'Error'
        );
        this.hideSpinner();
      }
    });
  }

  // ── Deactivate ────────────────────────────────────────────────────────────

  deactivateRow(row: any): void {
    // BUG-10 FIX: use userRole (from sessionStorage 'userrole'), not username string
    if (!this.userAccessModel.deleteAccess && this.userRole !== 'superadmin') {
      this.showMsg('You do not have permission to deactivate configurations.', 'warning', 'Warning');
      return;
    }
    this.dialog.open(DialogConfirmationComponent, {
      data: `Are you sure you want to deactivate the approval routing for <b>${row.RequesterUser}</b> → <b>${row.ApproverUser}</b> (${row.DocumentTypeDisplay})?`
    }).afterClosed().subscribe((result: any) => {
      if (!result?.confirmDialog) return;
      this.showLoadingSpinner = true;
      this._inventoryService.deactivateApprovalRouting(row.ID).subscribe({
        next: (res: any) => {
          this.showMsg(
            res?.message || 'Configuration deactivated.',
            'warning', 'Warning'
          );
          this.loadList();
        },
        error: (err: any) => {
          this.showMsg(
            this.extractMsg(err, 'Unable to deactivate. Please try again.'),
            'error', 'Error'
          );
          this.hideSpinner();
        }
      });
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private extractMsg(err: any, fallback: string): string {
    if (typeof err === 'string') return err;
    return err?.error?.message || err?.message || fallback;
  }

  private showMsg(
    message: string,
    icon: 'success' | 'warning' | 'info' | 'error',
    title: string
  ): void {
    Swal.fire({
      toast: true, position: 'top', showConfirmButton: false,
      title, text: message, icon,
      showCloseButton: false, timer: 7000, width: '700px',
      customClass: { popup: 'swal-top-offset' }
    });
    this.hideSpinner();
  }

  hideSpinner(): void { this.showLoadingSpinner = false; }

  // ── Form field error helpers ──────────────────────────────────────────────

  hasError(field: string, error: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl && ctrl.hasError(error) && ctrl.touched);
  }
}
