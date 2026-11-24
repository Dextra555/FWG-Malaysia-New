import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { UserAccessModel } from '../../../../model/userAccesModel';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-new-asset',
  templateUrl: './new-asset.component.html',
  styleUrls: ['./new-asset.component.css']
})
export class NewAssetComponent implements OnInit {

  frm!: FormGroup;
  isEdit: boolean = false;
  ID: any;

  branchList:   any[] = [];
  supplierList: any[] = [];
  categoryList: any[] = [];
  // Search filter state
  supplierSearch = ''; filteredSuppliers: any[] = [];
  branchSearch = '';   filteredBranches:  any[] = [];
  categorySearch = ''; filteredCategories: any[] = [];

  assetTypes:     string[] = ['Fixed Asset', 'Liquid Assets', 'Current Liabilities', 'Long Term Liabilities', 'Vehicle'];
  conditionTypes: string[] = ['Good', 'Fair', 'Poor', 'Damaged'];

  // Installment schedule preview rows
  installmentPreview: { no: number; dueDate: Date; amount: number; paidAmount: number; status: string }[] = [];

  userAccessModel!: UserAccessModel;
  currentUser:    string  = '';
  userRole:       string  = '';
  warningMessage: string  = '';
  errorMessage:   string  = '';
  showLoadingSpinner: boolean = false;

  constructor(
    private fb: FormBuilder,
    public dialog: MatDialog,
    private activatedRoute: ActivatedRoute,
    private service: InventoryService,
    private route: Router,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };

    this.frm = this.fb.group({
      ID:                       [0],
      AssetCode:                [''],           // auto-generated, read-only
      Name:                     ['', Validators.required],
      AssetType:                ['', Validators.required],
      SerialNumber:             [''],
      Description:              [''],
      ModelNo:                  [''],
      BrandName:                [''],
      SupplierID:               [null],
      CategoryID:               [null],
      Branch:                   [''],
      PurchaseAmount:           [''],
      PurchaseDate:             [null],
      WarrantyExpiryDate:       [null],
      AssetCondition:           ['Good'],
      Remarks:                  [''],
      SupplierInvoiceNo:        [''],
      // ── Deposit & Installment ──────────────────────────────────────────
      DepositAmount:            [null],
      PaymentTermType:          ['FULL'],
      InstallmentStartDate:     [null],
      MonthlyInstallmentAmount: [null],
      InstallmentMonths:        [{ value: null, disabled: true }],  // auto-calculated
      // ── Preclosing ────────────────────────────────────────────────────
      IsPreClosing:             [false],
      PreClosingDate:           [null],
      PreClosingAmount:         [{ value: null, disabled: true }],   // auto-calculated
      // ── Vehicle-specific ──────────────────────────────────────────────
      RoadTaxRenewalDate:       [null],
      InsuranceRenewalDate:     [null],
      // ── Office-specific ───────────────────────────────────────────────
      ServiceLastDate:          [null]
    });

    // Recalculate installments whenever relevant fields change
    ['PurchaseAmount', 'DepositAmount', 'MonthlyInstallmentAmount',
     'InstallmentStartDate', 'PaymentTermType'].forEach(f => {
      this.frm.get(f)?.valueChanges.subscribe(() => this.recalcInstallments());
    });

    // Recalc preclosing amount when toggle changes
    this.frm.get('IsPreClosing')?.valueChanges.subscribe(() => this.recalcPreClosing());
    this.frm.get('PreClosingDate')?.valueChanges.subscribe(() => this.recalcPreClosing());
  }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    const rawRole = sessionStorage.getItem('userrole');
    if (rawRole == '1') { this.userRole = 'admin'; }
    else if (rawRole == '2') { this.userRole = 'superadmin'; }
    else { this.userRole = 'user'; }
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => { this.currentUser = u; });
    }
    this.getUserAccessRights(this.currentUser, 'Asset Master');
    this.loadDropdowns();

    if (this.ID && this.ID !== '0') {
      this.isEdit = true;
      this.loadExisting();
    } else {
      this.service.getAssetCode().subscribe({
        next: (d: any) => this.frm.get('AssetCode')?.setValue(d.Code),
        error: () => {}
      });
    }
  }

  /** True when MONTHLY payment term is selected */
  get isMonthly(): boolean {
    return this.frm.get('PaymentTermType')?.value === 'MONTHLY';
  }

  /** True when the selected Category Master has AssetType = 'Vehicle' */
  get isVehicle(): boolean {
    const catID = this.frm.get('CategoryID')?.value;
    if (!catID) return false;
    const cat = this.categoryList.find((c: any) => c.ID === catID);
    return cat?.AssetType === 'Vehicle';
  }

  /** True when the selected Category Master has AssetType = 'Office' */
  get isOffice(): boolean {
    const catID = this.frm.get('CategoryID')?.value;
    if (!catID) return false;
    const cat = this.categoryList.find((c: any) => c.ID === catID);
    return cat?.AssetType === 'Office';
  }

  /** Sum of all installment preview amounts */
  get installmentTotal(): number {
    return this.installmentPreview.reduce((sum, r) => sum + r.amount, 0);
  }

  /** Sum of all paid amounts in preview */
  get installmentPaidTotal(): number {
    return this.installmentPreview.reduce((sum, r) => sum + (r.paidAmount || 0), 0);
  }

  /** True when preclosing toggle is ON */
  get isPreClosingOn(): boolean {
    return this.frm.get('IsPreClosing')?.value === true;
  }

  /** Count of pending (unpaid) installments */
  get preClosingPendingCount(): number {
    return this.installmentPreview.filter(r => r.paidAmount < r.amount).length;
  }

  /**
   * Recalculate preclosing amount = sum of all unpaid installment balances.
   * Called when IsPreClosing toggle or PreClosingDate changes.
   */
  recalcPreClosing(): void {    if (!this.isPreClosingOn) {
      this.frm.get('PreClosingAmount')?.setValue(null, { emitEvent: false });
      return;
    }
    // Sum all pending balances from installmentPreview
    const remaining = this.installmentPreview
      .filter(r => r.paidAmount < r.amount)
      .reduce((sum, r) => sum + (r.amount - r.paidAmount), 0);
    this.frm.get('PreClosingAmount')?.setValue(+remaining.toFixed(2), { emitEvent: false });
  }

  /**
   * Recalculate InstallmentMonths and rebuild the preview table
   * whenever any of the four driver fields change.
   */
  recalcInstallments(): void {
    const termType   = this.frm.get('PaymentTermType')?.value;
    const purchase   = +(this.frm.get('PurchaseAmount')?.value || 0);
    const deposit    = +(this.frm.get('DepositAmount')?.value  || 0);
    const monthly    = +(this.frm.get('MonthlyInstallmentAmount')?.value || 0);
    const startRaw   = this.frm.get('InstallmentStartDate')?.value;

    if (termType !== 'MONTHLY' || purchase <= 0 || monthly <= 0) {
      this.frm.get('InstallmentMonths')?.setValue(null, { emitEvent: false });
      this.installmentPreview = [];
      return;
    }

    const balance = Math.max(purchase - deposit, 0);
    const months  = Math.ceil(balance / monthly);
    this.frm.get('InstallmentMonths')?.setValue(months, { emitEvent: false });

    // Build preview rows
    this.installmentPreview = [];
    if (startRaw && months > 0) {
      const start = new Date(startRaw);
      let remaining = balance;
      for (let i = 0; i < months; i++) {
        const dueDate = new Date(start.getFullYear(), start.getMonth() + i, 1);
        const amt     = (i === months - 1) ? remaining : monthly;
        this.installmentPreview.push({ no: i + 1, dueDate, amount: +amt.toFixed(2), paidAmount: 0, status: 'Pending' });
        remaining = +(remaining - monthly).toFixed(2);
        if (remaining < 0) remaining = 0;
      }
    }
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
        }
      },
      () => { this.errorMessage = 'Unable to load page permissions. Please try again.'; }
    );
  }

  loadDropdowns(): void {
    this.service.getBranchListByUser(this.currentUser).subscribe({
      next: (d: any[]) => { this.branchList = d || []; this.filteredBranches = [...this.branchList]; },
      error: () => {}
    });
    this.service.getSuppliers('P').subscribe({
      next: (d: any[]) => { this.supplierList = d || []; this.filteredSuppliers = [...this.supplierList]; },
      error: () => {}
    });
    this.service.getAssetCategories().subscribe({
      next: (d: any[]) => { this.categoryList = d || []; this.filteredCategories = [...this.categoryList]; },
      error: () => {}
    });
  }

  loadExisting(): void {
    this.showLoadingSpinner = true;
    this.service.getAssetByIDEnriched(this.ID).subscribe({
      next: (d: any) => {
        if (!d) { this.errorMessage = 'Asset not found.'; this.hideLoadingSpinner(); return; }
        this.frm.patchValue({
          ID:                       d.ID,
          AssetCode:                d.AssetCode,
          Name:                     d.Name,
          AssetType:                d.AssetType,
          SerialNumber:             d.SerialNumber,
          Description:              d.Description,
          ModelNo:                  d.ModelNo,
          BrandName:                d.BrandName,
          SupplierID:               d.SupplierID,
          CategoryID:               d.CategoryID ?? null,
          Branch:                   d.Branch,
          PurchaseAmount:           d.PurchaseAmount,
          PurchaseDate:             d.PurchaseDate ? new Date(d.PurchaseDate) : null,
          WarrantyExpiryDate:       d.WarrantyExpiryDate ? new Date(d.WarrantyExpiryDate) : null,
          AssetCondition:           d.AssetCondition,
          Remarks:                  d.Remarks,
          SupplierInvoiceNo:        d.SupplierInvoiceNo ?? '',
          DepositAmount:            d.DepositAmount ?? null,
          PaymentTermType:          d.PaymentTermType ?? 'FULL',
          InstallmentStartDate:     d.InstallmentStartDate ? new Date(d.InstallmentStartDate) : null,
          MonthlyInstallmentAmount: d.MonthlyInstallmentAmount ?? null,
          InstallmentMonths:        d.InstallmentMonths ?? null,
          IsPreClosing:             d.IsPreClosing ?? false,
          PreClosingDate:           d.PreClosingDate ? new Date(d.PreClosingDate) : null,
          PreClosingAmount:         d.PreClosingAmount ?? null,
          RoadTaxRenewalDate:       d.RoadTaxRenewalDate   ? new Date(d.RoadTaxRenewalDate)   : null,
          InsuranceRenewalDate:     d.InsuranceRenewalDate ? new Date(d.InsuranceRenewalDate) : null,
          ServiceLastDate:          d.ServiceLastDate       ? new Date(d.ServiceLastDate)       : null
        });

        // Load existing installment schedule for display
        if (d.PaymentTermType === 'MONTHLY') {
          this.service.getAssetInstallments(d.ID).subscribe({
            next: (rows: any[]) => {
              this.installmentPreview = (rows || []).map((r: any) => ({
                no:         r.InstallmentNo,
                dueDate:    new Date(r.DueDate),
                amount:     r.TotalAmount,
                paidAmount: r.PaidAmount ?? 0,
                status:     r.Status ?? 'Pending'
              }));
              // Recalc preclosing amount after preview loads
              this.recalcPreClosing();
            },
            error: () => {}
          });
        }
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load asset. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }

    const raw = this.frm.getRawValue();
    const isMonthlySelected = raw.PaymentTermType === 'MONTHLY';

    // Validate monthly fields
    if (isMonthlySelected) {
      if (!raw.PurchaseAmount || +raw.PurchaseAmount <= 0) {
        this.showMessage('Please enter the Purchase Amount before setting up installments.', 'warning', 'Warning Message'); return;
      }
      if (!raw.MonthlyInstallmentAmount || +raw.MonthlyInstallmentAmount <= 0) {
        this.showMessage('Please enter the Monthly Installment Amount.', 'warning', 'Warning Message'); return;
      }
      if (!raw.InstallmentStartDate) {
        this.showMessage('Please select the Installment Start Date.', 'warning', 'Warning Message'); return;
      }
      if (!raw.SupplierID) {
        this.showMessage('Please select a Supplier for monthly installment payment.', 'warning', 'Warning Message'); return;
      }
    }

    this.showLoadingSpinner = true;
    const payload = {
      ...raw,
      SupplierID:               raw.SupplierID ? +raw.SupplierID : null,
      PurchaseAmount:           raw.PurchaseAmount ? +raw.PurchaseAmount : null,
      DepositAmount:            raw.DepositAmount ? +raw.DepositAmount : null,
      MonthlyInstallmentAmount: raw.MonthlyInstallmentAmount ? +raw.MonthlyInstallmentAmount : null,
      InstallmentMonths:        raw.InstallmentMonths ? +raw.InstallmentMonths : null,
      PaymentTermType:          raw.PaymentTermType || 'FULL',
      IsPreClosing:             raw.IsPreClosing || false,
      PreClosingDate:           raw.IsPreClosing ? raw.PreClosingDate : null,
      PreClosingAmount:         raw.IsPreClosing ? (raw.PreClosingAmount ? +raw.PreClosingAmount : null) : null,
      LastUpdatedBy:            this.currentUser,
      LastUpdatedDate:          new Date()
    };

    this.service.saveAssetRecord(payload).subscribe({
      next: (res: any) => {
        this.showMessage(res?.message || (this.isEdit ? 'Asset updated successfully.' : 'Asset saved successfully.'), 'success', 'Success Message');
        this.route.navigateByUrl('/dummy', { skipLocationChange: true })
          .then(() => this.route.navigate(['/inventory/asset-master']));
      },
      error: (err: any) => {
        this.showMessage(this.extractErrorMessage(err, 'Unable to save the asset right now. Please try again.'), 'error', 'Error Message');
        this.hideLoadingSpinner();
      }
    });
  }

  onCancelClick(): void { this.route.navigate(['/inventory/asset-master']); }

  /** Open the Asset Master PDF in a new tab. Read-only — does not modify any record. */
  onPrintPDF(): void {
    const docID = this.ID || this.frm.get('ID')?.value;
    if (!docID || +docID === 0) return;
    window.open(
      environment.baseReportUrl + 'Inventory/AssetMasterReport.aspx'
      + '?LoginID=' + encodeURIComponent(this.currentUser)
      + '&AssetID=' + encodeURIComponent(docID),
      '_blank');
  }

  private extractErrorMessage(err: any, fallback: string): string {
    if (err && typeof err === 'string') return err;
    if (err?.error?.message) return err.error.message;
    if (err?.message) return err.message;
    return fallback;
  }

  private showMessage(message: string, icon: 'success' | 'warning' | 'info' | 'error',
    title: 'Success Message' | 'Warning Message' | 'Error Message'): void {
    Swal.fire({ toast: true, position: 'top', showConfirmButton: false, title, text: message, icon,
      showCloseButton: false, timer: 5000, width: '600px', customClass: { popup: 'swal-top-offset' } });
    this.hideLoadingSpinner();
  }

  hideLoadingSpinner(): void { this.showLoadingSpinner = false; }

  /**
   * User edited an installment row's Amount directly in the table.
   * Updates that row's amount in-place; recalcs preclosing if ON.
   */
  onInstallmentAmountChange(row: any, value: string): void {
    const newAmt = parseFloat(value);
    if (isNaN(newAmt) || newAmt < 0) return;
    row.amount = +newAmt.toFixed(2);
    // Recalc preclosing if toggled ON
    this.recalcPreClosing();
  }

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onSupplierOpen(opened: boolean) { if (opened) { this.supplierSearch = ''; this.filteredSuppliers = [...this.supplierList]; } }
  onBranchOpen(opened: boolean)   { if (opened) { this.branchSearch   = ''; this.filteredBranches  = [...this.branchList];   } }
  onCategoryOpen(opened: boolean) { if (opened) { this.categorySearch = ''; this.filteredCategories = [...this.categoryList]; } }
  onSupplierSearch(q: string) { this.supplierSearch = q; const s = q.toLowerCase(); this.filteredSuppliers = this.supplierList.filter(x => x.Name?.toLowerCase().includes(s) || x.Code?.toLowerCase().includes(s)); }
  onBranchSearch(q: string)   { this.branchSearch   = q; const s = q.toLowerCase(); this.filteredBranches  = this.branchList.filter(x  => x.Name?.toLowerCase().includes(s) || x.Code?.toLowerCase().includes(s)); }
  onCategorySearch(q: string) { this.categorySearch = q; const s = q.toLowerCase(); this.filteredCategories = this.categoryList.filter(x => x.Name?.toLowerCase().includes(s)); }
}
