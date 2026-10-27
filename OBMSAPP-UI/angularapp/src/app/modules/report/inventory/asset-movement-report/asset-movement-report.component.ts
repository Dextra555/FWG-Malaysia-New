import { Component, OnInit } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { InventoryService } from '../../../../service/inventory.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';

@Component({
  selector: 'app-asset-movement-report',
  templateUrl: './asset-movement-report.component.html',
  styleUrls: ['./asset-movement-report.component.css']
})
export class AssetMovementReportComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Inventory/AssetMovementReport.aspx?';

  frm!: FormGroup;
  assetList: any[]   = [];
  currentUser: string  = '';
  errorMessage: string   = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;

  movementTypeOptions = [
    { value: 'ALL',        label: 'All Movements' },
    { value: 'ALLOCATION', label: 'Allocation'    },
    { value: 'TRANSFER',   label: 'Transfer'      },
    { value: 'RETURN',     label: 'Return'        },
  ];

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
      AssetID:      ['0'],                               // optional — 0 = All Assets
      MovementType: ['ALL'],                             // default = All Movements
      StartDate:    [firstOfMonth, Validators.required],
      EndDate:      [today,        Validators.required]
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
    this.getUserAccessRights(this.currentUser, 'Asset Movement Report');
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

            // Load asset dropdown using getAssetList() — returns enriched asset objects
            this.service.getAssetList().subscribe((d: any[]) => {
              // Filter only active assets
              this.assetList = (d || []).filter((a: any) => a.isActive !== false);
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
    localURL += '&AssetID='      + (raw.AssetID      || '0');
    localURL += '&MovementType=' + (raw.MovementType || 'ALL');
    localURL += '&StartDate='    + this.returnDate(raw.StartDate);
    localURL += '&EndDate='      + this.returnDate(raw.EndDate);

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
    params.set('LoginID',      this.currentUser        || '');
    params.set('AssetID',      String(raw.AssetID      || 0));
    params.set('MovementType', raw.MovementType        || 'ALL');
    params.set('StartDate',    this.returnDate(raw.StartDate));
    params.set('EndDate',      this.returnDate(raw.EndDate));
    params.set('format',       'excel');

    const baseUrl = environment.baseReportUrl + 'Inventory/AssetMovementReport.aspx';
    window.open(baseUrl + '?' + params.toString(), '_blank');
  }

  onClear(): void {
    const today        = new Date();
    const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    this.frm.patchValue({
      AssetID: '0', MovementType: 'ALL',
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
