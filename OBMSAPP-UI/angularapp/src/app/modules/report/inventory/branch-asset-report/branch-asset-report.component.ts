import { Component, OnInit } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { InventoryService } from '../../../../service/inventory.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';

@Component({
  selector: 'app-branch-asset-report',
  templateUrl: './branch-asset-report.component.html',
  styleUrls: ['./branch-asset-report.component.css']
})
export class BranchAssetReportComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Inventory/BranchAssetReport.aspx?';

  frm!: FormGroup;
  branchList: any[]  = [];
  currentUser: string  = '';
  errorMessage: string   = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;

  assetStatusOptions = [
    { value: 'ALL',               label: 'All Statuses'       },
    { value: 'Available',         label: 'Available'          },
    { value: 'Allocated',         label: 'Allocated'          },
    { value: 'Under_Maintenance', label: 'Under Maintenance'  },
    { value: 'Damaged',           label: 'Damaged'            },
    { value: 'Disposed',          label: 'Disposed'           },
    { value: 'Lost',              label: 'Lost'               },
  ];

  assetTypeOptions = [
    { value: 'ALL',            label: 'All Types'          },
    { value: 'Fixed Asset',    label: 'Fixed Asset'        },
    { value: 'Liquid Assets',  label: 'Liquid Assets'      },
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

    // No date filter — current snapshot only
    this.frm = this.fb.group({
      BranchID:    ['0'],    // optional — 0 = All Branches
      AssetStatus: ['ALL'],  // default = All Statuses
      AssetType:   ['ALL'],  // default = All Types
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
    this.getUserAccessRights(this.currentUser, 'Branch Asset Report');
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

            // Load branch dropdown using getBranchMasterList
            this._masterService.getBranchMasterList().subscribe((d: any[]) => {
              this.branchList = d || [];
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
    localURL += '&BranchID='    + (raw.BranchID    || '0');
    localURL += '&AssetStatus=' + (raw.AssetStatus || 'ALL');
    localURL += '&AssetType='   + encodeURIComponent(raw.AssetType || 'ALL');

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + localURL);
  }

  onExportExcel(): void {
    this.errorMessage = '';
    const raw = this.frm.value;

    const params = new URLSearchParams();
    params.set('LoginID',     this.currentUser       || '');
    params.set('BranchID',    String(raw.BranchID    || 0));
    params.set('AssetStatus', raw.AssetStatus        || 'ALL');
    params.set('AssetType',   raw.AssetType          || 'ALL');
    params.set('format',      'excel');

    const baseUrl = environment.baseReportUrl + 'Inventory/BranchAssetReport.aspx';
    window.open(baseUrl + '?' + params.toString(), '_blank');
  }

  onClear(): void {
    this.frm.patchValue({ BranchID: '0', AssetStatus: 'ALL', AssetType: 'ALL' });
    this.urlSafe      = undefined;
    this.errorMessage = '';
  }
}
