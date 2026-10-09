import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { InventoryService } from "../../../../service/inventory.service";
import Swal from "sweetalert2";
import { ActivatedRoute, Router } from "@angular/router";
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';

@Component({
  selector: 'app-new-item',
  templateUrl: './new-item.component.html',
  styleUrls: ['./new-item.component.css']
})
export class NewItemComponent implements OnInit {

  frm!: FormGroup;
  categories: any[] = [];
  brands: any[] = [];
  uoms: any[] = [];
  warehouses: any[] = [];
  // Search filter state
  categorySearch = ''; filteredCategories: any[] = [];
  brandSearch = '';    filteredBrands: any[] = [];
  uomSearch = '';      filteredUoms: any[] = [];
  warehouseSearch = ''; filteredWarehouses: any[] = [];
  isEdit: boolean = false;
  ID: any;
  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
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
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    };

    this.frm = this.fb.group({
      ID:              [0],
      Code:            [''],                        // Auto-generated — read-only
      CategoryID:      ['', Validators.required],
      BrandID:         [0],                         // Optional
      UOMID:           [0, Validators.required],    // Required
      WarehouseID:     [null],                      // Optional — default warehouse
      Name:            ['', Validators.required],
      Price:           ['', Validators.required],   // Buying price
      ReorderLevel:    [null],
      ReorderQuantity: [null],
      Remarks:         [''],
      // SellPrice, Quantity, qty removed from form — not part of approved design
    });
  }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    if (this.currentUser == 'null' || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Item Master');
    this.loadBrands();
    this.loadUOMs();
    this.loadWarehouses();

    if (this.ID != 0 && this.ID != undefined) {
      // Edit mode — load existing item
      this.isEdit = true;
      this.service.getItemByID(this.ID).subscribe((d: any) => {
        const item = d['item'];
        const itemType = d['type'];
        if (item) {
          this.frm.patchValue({
            ID:              item.ID,
            Code:            item.Code,
            CategoryID:      item.CategoryID,
            BrandID:         item.BrandID ?? 0,
            UOMID:           item.UOMID ?? 0,
            WarehouseID:     item.WarehouseID ?? null,
            Name:            item.Name,
            Price:           item.Price,
            ReorderLevel:    item.ReorderLevel,
            ReorderQuantity: item.ReorderQuantity,
            Remarks:         item.Remarks,
          });
          // Load categories matching the item's type
          this.updateCategories(itemType ?? 'P');
        }
      });
    } else {
      // New mode — auto-generate code and load default categories
      this.service.getItemCode().subscribe((d: any) => {
        this.frm.get('Code')?.setValue(d['Code']);
      });
      this.updateCategories('P');
    }

    // Code is always read-only — never manually entered
    this.frm.get('Code')?.disable({ onlySelf: true });
  }

  getUserAccessRights(userName: string, screenName: string) {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
        }
      },
      (error) => { this.handleErrors(error); }
    );
  }

  updateCategories(cat: string) {
    this.service.getCategoryByCat(cat).subscribe(
      (d: any) => { this.categories = d; this.filteredCategories = d; },
      (error) => { this.handleErrors(error); }
    );
  }

  loadBrands() {
    this.service.getBrandMasters().subscribe(
      (d: any) => { this.brands = d; this.filteredBrands = d; },
      (error) => { this.handleErrors(error); }
    );
  }

  loadUOMs() {
    this.service.getUOMMasters().subscribe(
      (d: any) => { this.uoms = d; this.filteredUoms = d; },
      (error) => { this.handleErrors(error); }
    );
  }

  loadWarehouses() {
    this.service.getWarehouseMasters().subscribe(
      (d: any[]) => { this.warehouses = d || []; this.filteredWarehouses = [...this.warehouses]; },
      () => {}
    );
  }

  onCategoryTypeChange(type: string) {
    this.frm.get('CategoryID')?.setValue('');
    this.categorySearch = '';
    this.updateCategories(type);
  }

  // Search open/close handlers (reset on open)
  onCategoryOpen(opened: boolean) { if (opened) { this.categorySearch = ''; this.filteredCategories = [...this.categories]; } }
  onBrandOpen(opened: boolean)    { if (opened) { this.brandSearch = ''; this.filteredBrands = [...this.brands]; } }
  onUomOpen(opened: boolean)      { if (opened) { this.uomSearch = ''; this.filteredUoms = [...this.uoms]; } }
  onWarehouseOpen(opened: boolean){ if (opened) { this.warehouseSearch = ''; this.filteredWarehouses = [...this.warehouses]; } }

  // Filter methods
  onCategorySearch(q: string) { this.categorySearch = q; const s = q.toLowerCase(); this.filteredCategories = this.categories.filter(c => c.Name?.toLowerCase().includes(s)); }
  onBrandSearch(q: string)    { this.brandSearch = q;    const s = q.toLowerCase(); this.filteredBrands    = this.brands.filter(b => b.Name?.toLowerCase().includes(s)); }
  onUomSearch(q: string)      { this.uomSearch = q;      const s = q.toLowerCase(); this.filteredUoms      = this.uoms.filter(u => u.Name?.toLowerCase().includes(s)); }
  onWarehouseSearch(q: string){ this.warehouseSearch = q; const s = q.toLowerCase(); this.filteredWarehouses = this.warehouses.filter(w => w.Name?.toLowerCase().includes(s) || w.Code?.toLowerCase().includes(s)); }

  onSubmit() {
    if (this.frm.invalid) {
      this.frm.markAllAsTouched();
      return;
    }

    this.showLoadingSpinner = true;
    const data = this.frm.getRawValue();  // getRawValue includes disabled Code field
    data['LastUpdatedBy'] = this.currentUser;

    this.service.saveItem(data).subscribe({
      next: () => {
        this.showMessage('Item Saved/Updated Successfully', 'success', 'Success Message');
        this.frm.reset();
        this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => {
          this.route.navigate(['/inventory/item-master']);
        });
      },
      error: (err: any) => {
        this.showMessage(`Item failed to save: ${err}`, 'error', 'Error Message');
      }
    });
  }

  onCancelClick() {
    this.route.navigate(['/inventory/item-master']);
  }

  private showMessage(
    message: string,
    icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message' = 'Success Message'
  ): void {
    Swal.fire({
      toast: true,
      position: 'top',
      showConfirmButton: false,
      title: title,
      text: message,
      icon: icon,
      showCloseButton: false,
      timer: 5000,
      width: '600px',
      customClass: { popup: 'swal-top-offset' }
    });
    this.hideLoadingSpinner();
  }

  handleErrors(error: string) {
    if (error != null && error != '') {
      this.errorMessage = error;
      this.hideLoadingSpinner();
    }
  }

  hideLoadingSpinner() {
    this.showLoadingSpinner = false;
  }
}
