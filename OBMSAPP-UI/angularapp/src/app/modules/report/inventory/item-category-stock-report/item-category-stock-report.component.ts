import { Component, OnInit } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { InventoryService } from '../../../../service/inventory.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';

@Component({
  selector: 'app-item-category-stock-report',
  templateUrl: './item-category-stock-report.component.html',
  styleUrls: ['./item-category-stock-report.component.css']
})
export class ItemCategoryStockReportComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Inventory/ItemCategoryStockReport.aspx?';

  frm!: FormGroup;
  warehouseList: any[] = [];
  categoryList: any[]  = [];
  itemList: any[]      = [];
  currentUser: string  = '';
  errorMessage: string   = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;

  constructor(
    public sanitizer: DomSanitizer,
    private _masterService: MastermoduleService,
    private service: InventoryService,
    private fb: FormBuilder,
    private _dataService: DatasharingService
  ) {
    this.currentUser = sessionStorage.getItem('username')!;
    this.url += this.currentUrl;
    this.url += 'LoginID=' + this.currentUser;

    const today = new Date();

    this.frm = this.fb.group({
      WarehouseID: ['0'],                          // optional — 0 = All
      CategoryID:  ['0'],                          // optional — 0 = All
      ItemID:      ['0'],                          // optional — 0 = All
      AsOfDate:    [today, Validators.required]    // required
    });

    this.userAccessModel = {
      readAccess: false, updateAccess: false,
      deleteAccess: false, createAccess: false
    };
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => this.currentUser = u);
    }
    this.getUserAccessRights(this.currentUser, 'Item Category Stock Report');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;

          if (this.userAccessModel.readAccess === true || this.currentUser === 'superadmin') {
            this.warningMessage = '';

            // Load warehouse dropdown
            this.service.getWarehouseMasters().subscribe((d: any[]) => {
              this.warehouseList = (d || []).filter((w: any) => w.IsActive);
            });

            // Load category dropdown (inventory categories)
            this.service.getCategories().subscribe((d: any) => {
              this.categoryList = d || [];
            });

            // Load item dropdown
            this.service.GetReportHQMaster().subscribe((d: any) => {
              this.itemList = d || [];
            });

          } else {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
              You do not have permissions to view this page. <br>
              Please contact your administrator.`;
          }
        }
        this.showLoadingSpinner = false;
      },
      () => { this.showLoadingSpinner = false; }
    );
  }

  onSubmit(): void {
    this.errorMessage = '';

    if (this.frm.invalid) {
      this.frm.markAllAsTouched();
      return;
    }

    const raw = this.frm.value;

    let localURL = '';
    localURL += '&WarehouseID=' + (raw.WarehouseID || '0');
    localURL += '&CategoryID='  + (raw.CategoryID  || '0');
    localURL += '&ItemID='      + (raw.ItemID      || '0');
    localURL += '&AsOfDate='    + this.returnDate(raw.AsOfDate);

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + localURL);
  }

  onExportExcel(): void {
    if (this.frm.invalid) {
      this.frm.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    const raw = this.frm.value;

    const params = new URLSearchParams();
    params.set('LoginID',     this.currentUser       || '');
    params.set('WarehouseID', String(raw.WarehouseID || 0));
    params.set('CategoryID',  String(raw.CategoryID  || 0));
    params.set('ItemID',      String(raw.ItemID      || 0));
    params.set('AsOfDate',    this.returnDate(raw.AsOfDate));
    params.set('format',      'excel');

    const baseUrl = environment.baseReportUrl + 'Inventory/ItemCategoryStockReport.aspx';
    window.open(baseUrl + '?' + params.toString(), '_blank');
  }

  onClear(): void {
    const today = new Date();
    this.frm.patchValue({
      WarehouseID: '0', CategoryID: '0', ItemID: '0',
      AsOfDate: today
    });
    this.urlSafe      = undefined;
    this.errorMessage = '';
  }

  returnDate(date?: any): string {
    let currentDate = new Date();
    if (date) currentDate = new Date(date);
    const year  = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0');
    const day   = String(currentDate.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
