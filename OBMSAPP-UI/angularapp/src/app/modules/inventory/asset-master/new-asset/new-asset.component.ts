import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';

import { InventoryService } from '../../../../service/inventory.service';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { DatasharingService } from '../../../../service/datasharing.service';
import { UserAccessModel } from '../../../../model/userAccesModel';

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
  // Search filter state
  supplierSearch = ''; filteredSuppliers: any[] = [];
  branchSearch = '';   filteredBranches:  any[] = [];

  assetTypes:     string[] = ['Fixed Asset', 'Liquid Assets', 'Current Liabilities', 'Long Term Liabilities'];
  conditionTypes: string[] = ['Good', 'Fair', 'Poor', 'Damaged'];

  userAccessModel!: UserAccessModel;
  currentUser:    string  = '';
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
      ID:                 [0],
      AssetCode:          [''],           // auto-generated, read-only
      Name:               ['', Validators.required],
      AssetType:          ['', Validators.required],
      SerialNumber:       [''],
      Description:        [''],
      ModelNo:            [''],
      BrandName:          [''],
      SupplierID:         [null],
      Branch:             [''],
      PurchaseAmount:     [''],
      PurchaseDate:       [null],
      WarrantyExpiryDate: [null],
      AssetCondition:     ['Good'],
      Remarks:            ['']
    });
  }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
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
    this.service.getSuppliers().subscribe({
      next: (d: any[]) => { this.supplierList = d || []; this.filteredSuppliers = [...this.supplierList]; },
      error: () => {}
    });
  }

  loadExisting(): void {
    this.showLoadingSpinner = true;
    this.service.getAssetByIDEnriched(this.ID).subscribe({
      next: (d: any) => {
        if (!d) { this.errorMessage = 'Asset not found.'; this.hideLoadingSpinner(); return; }
        this.frm.patchValue({
          ID:                 d.ID,
          AssetCode:          d.AssetCode,
          Name:               d.Name,
          AssetType:          d.AssetType,
          SerialNumber:       d.SerialNumber,
          Description:        d.Description,
          ModelNo:            d.ModelNo,
          BrandName:          d.BrandName,
          SupplierID:         d.SupplierID,
          Branch:             d.Branch,
          PurchaseAmount:     d.PurchaseAmount,
          PurchaseDate:       d.PurchaseDate ? new Date(d.PurchaseDate) : null,
          WarrantyExpiryDate: d.WarrantyExpiryDate ? new Date(d.WarrantyExpiryDate) : null,
          AssetCondition:     d.AssetCondition,
          Remarks:            d.Remarks
        });
        this.hideLoadingSpinner();
      },
      error: () => { this.errorMessage = 'Unable to load asset. Please try again.'; this.hideLoadingSpinner(); }
    });
  }

  onSubmit(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }
    this.showLoadingSpinner = true;
    const raw = this.frm.getRawValue();
    const payload = {
      ...raw,
      SupplierID:    raw.SupplierID ? +raw.SupplierID : null,
      PurchaseAmount: raw.PurchaseAmount ? +raw.PurchaseAmount : null,
      LastUpdatedBy: this.currentUser,
      LastUpdatedDate: new Date()
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

  // ── Searchable dropdown helpers ──────────────────────────────────────────
  onSupplierOpen(opened: boolean) { if (opened) { this.supplierSearch = ''; this.filteredSuppliers = [...this.supplierList]; } }
  onBranchOpen(opened: boolean)   { if (opened) { this.branchSearch   = ''; this.filteredBranches  = [...this.branchList];   } }
  onSupplierSearch(q: string) { this.supplierSearch = q; const s = q.toLowerCase(); this.filteredSuppliers = this.supplierList.filter(x => x.Name?.toLowerCase().includes(s) || x.Code?.toLowerCase().includes(s)); }
  onBranchSearch(q: string)   { this.branchSearch   = q; const s = q.toLowerCase(); this.filteredBranches  = this.branchList.filter(x  => x.Name?.toLowerCase().includes(s) || x.Code?.toLowerCase().includes(s)); }
}
