import { Component, OnInit } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { InventoryService } from '../../../../service/inventory.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';

@Component({
  selector: 'app-current-stock-report',
  templateUrl: './current-stock-report.component.html',
  styleUrls: ['./current-stock-report.component.css']
})
export class CurrentStockReportComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Inventory/CurrentStockReport.aspx?';

  frm!: FormGroup;
  warehouseList: any[] = [];
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

    this.frm = this.fb.group({
      WarehouseID: ['0'],   // 0 = All Warehouses
      ItemID:      ['0'],   // 0 = All Items
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
    this.getUserAccessRights(this.currentUser, 'Current Stock Report');
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
            this.service.getWarehouseMasters().subscribe((d: any[]) => {
              this.warehouseList = (d || []).filter((w: any) => w.IsActive);
            });
            this.service.GetReportHQMaster().subscribe((d: any) => {
              this.itemList = d;
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
    const raw = this.frm.value;

    let localURL = '';
    localURL += '&WarehouseID=' + (raw.WarehouseID || '0');
    localURL += '&ItemID='      + (raw.ItemID      || '0');

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + localURL);
  }

  onExportExcel(): void {
    this.errorMessage = '';
    const raw = this.frm.value;

    const params = new URLSearchParams();
    params.set('LoginID',     this.currentUser      || '');
    params.set('WarehouseID', String(raw.WarehouseID || 0));
    params.set('ItemID',      String(raw.ItemID      || 0));
    params.set('format',      'excel');

    const baseUrl = environment.baseReportUrl + 'Inventory/CurrentStockReport.aspx';
    window.open(baseUrl + '?' + params.toString(), '_blank');
  }

  onClear(): void {
    this.frm.patchValue({ WarehouseID: '0', ItemID: '0' });
    this.urlSafe      = undefined;
    this.errorMessage = '';
  }
}
