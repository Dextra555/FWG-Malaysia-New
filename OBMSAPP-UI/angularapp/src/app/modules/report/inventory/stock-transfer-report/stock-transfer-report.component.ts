import { Component, OnInit } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { InventoryService } from '../../../../service/inventory.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';

@Component({
  selector: 'app-stock-transfer-report',
  templateUrl: './stock-transfer-report.component.html',
  styleUrls: ['./stock-transfer-report.component.css']
})
export class StockTransferReportComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Inventory/StockTransferReport.aspx?';

  frm!: FormGroup;
  warehouseList: any[] = [];   // shared for both From and To dropdowns
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

    const today        = new Date();
    const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);

    this.frm = this.fb.group({
      SourceWarehouseID: ['0'],                               // optional — 0 = All
      DestWarehouseID:   ['0'],                               // optional — 0 = All
      ItemID:            ['0'],                               // optional — 0 = All
      StartDate:         [firstOfMonth, Validators.required],
      EndDate:           [today,        Validators.required]
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
    this.getUserAccessRights(this.currentUser, 'Stock Transfer Report');
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

            // One warehouse list shared for both From and To dropdowns
            this.service.getWarehouseMasters().subscribe((d: any[]) => {
              this.warehouseList = (d || []).filter((w: any) => w.IsActive);
            });

            // Item dropdown
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

    const raw  = this.frm.value;
    const from = new Date(raw.StartDate);
    const to   = new Date(raw.EndDate);

    if (from > to) {
      this.errorMessage = "The 'From' date must be before the 'To' date.";
      return;
    }

    let localURL = '';
    localURL += '&SourceWarehouseID=' + (raw.SourceWarehouseID || '0');
    localURL += '&DestWarehouseID='   + (raw.DestWarehouseID   || '0');
    localURL += '&ItemID='            + (raw.ItemID            || '0');
    localURL += '&StartDate='         + this.returnDate(raw.StartDate);
    localURL += '&EndDate='           + this.returnDate(raw.EndDate);

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + localURL);
  }

  onExportExcel(): void {
    if (this.frm.invalid) {
      this.frm.markAllAsTouched();
      return;
    }

    const raw  = this.frm.value;
    const from = new Date(raw.StartDate);
    const to   = new Date(raw.EndDate);

    if (from > to) {
      this.errorMessage = "The 'From' date must be before the 'To' date.";
      return;
    }

    this.errorMessage = '';

    const params = new URLSearchParams();
    params.set('LoginID',           this.currentUser              || '');
    params.set('SourceWarehouseID', String(raw.SourceWarehouseID  || 0));
    params.set('DestWarehouseID',   String(raw.DestWarehouseID    || 0));
    params.set('ItemID',            String(raw.ItemID             || 0));
    params.set('StartDate',         this.returnDate(raw.StartDate));
    params.set('EndDate',           this.returnDate(raw.EndDate));
    params.set('format',            'excel');

    const baseUrl = environment.baseReportUrl + 'Inventory/StockTransferReport.aspx';
    window.open(baseUrl + '?' + params.toString(), '_blank');
  }

  onClear(): void {
    const today        = new Date();
    const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    this.frm.patchValue({
      SourceWarehouseID: '0', DestWarehouseID: '0', ItemID: '0',
      StartDate: firstOfMonth, EndDate: today
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
