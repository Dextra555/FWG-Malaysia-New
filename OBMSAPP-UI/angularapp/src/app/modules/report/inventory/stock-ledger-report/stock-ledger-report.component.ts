import { Component, OnInit } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { InventoryService } from '../../../../service/inventory.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';

@Component({
  selector: 'app-stock-ledger-report',
  templateUrl: './stock-ledger-report.component.html',
  styleUrls: ['./stock-ledger-report.component.css']
})
export class StockLedgerReportComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Inventory/StockLedgerReport.aspx?';

  frm!: FormGroup;
  warehouseList: any[] = [];
  itemList: any[]      = [];
  currentUser: string  = '';
  errorMessage: string   = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;

  transactionTypeOptions = [
    { value: 'ALL',          label: 'All Transaction Types' },
    { value: 'OPENING',      label: 'Opening Stock'         },
    { value: 'STOCK_IN',     label: 'Stock In'              },
    { value: 'ISSUE',        label: 'Stock Issue'           },
    { value: 'ISSUE_RETURN', label: 'Issue Return'          },
    { value: 'TRANSFER_IN',  label: 'Transfer In'           },
    { value: 'TRANSFER_OUT', label: 'Transfer Out'          },
    { value: 'ADJ_PLUS',     label: 'Adjustment (In)'       },
    { value: 'ADJ_MINUS',    label: 'Adjustment (Out)'      },
    { value: 'PURCHASE_RTN', label: 'Purchase Return'       },
    { value: 'DAMAGE_LOSS',  label: 'Damage / Loss'         },
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
      WarehouseID:     ['', Validators.required],
      ItemID:          ['0'],
      TransactionType: ['ALL'],
      StartDate:       [firstOfMonth, Validators.required],
      EndDate:         [today,        Validators.required]
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
    this.getUserAccessRights(this.currentUser, 'Stock Ledger Report');
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

    if (this.frm.invalid) {
      this.frm.markAllAsTouched();
      return;
    }

    const raw = this.frm.value;

    const from = new Date(raw.StartDate);
    const to   = new Date(raw.EndDate);
    if (from > to) {
      this.errorMessage = "The 'From' date must be before the 'To' date.";
      return;
    }

    let localURL = '';
    localURL += '&StartDate='       + this.returnDate(raw.StartDate);
    localURL += '&EndDate='         + this.returnDate(raw.EndDate);
    localURL += '&WarehouseID='     + (raw.WarehouseID     || '0');
    localURL += '&ItemID='          + (raw.ItemID          || '0');
    localURL += '&TransactionType=' + (raw.TransactionType || 'ALL');

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + localURL);
  }

  onExportExcel(): void {
    if (this.frm.invalid) {
      this.frm.markAllAsTouched();
      return;
    }

    const raw = this.frm.value;

    const from = new Date(raw.StartDate);
    const to   = new Date(raw.EndDate);
    if (from > to) {
      this.errorMessage = "The 'From' date must be before the 'To' date.";
      return;
    }

    this.errorMessage = '';

    // Build same URL as PDF but with &format=excel — triggers download
    const params = new URLSearchParams();
    params.set('LoginID',         this.currentUser           || '');
    params.set('StartDate',       this.returnDate(raw.StartDate));
    params.set('EndDate',         this.returnDate(raw.EndDate));
    params.set('WarehouseID',     String(raw.WarehouseID     || 0));
    params.set('ItemID',          String(raw.ItemID          || 0));
    params.set('TransactionType', raw.TransactionType        || 'ALL');
    params.set('format',          'excel');

    const baseUrl = environment.baseReportUrl + 'Inventory/StockLedgerReport.aspx';
    window.open(baseUrl + '?' + params.toString(), '_blank');
  }

  onClear(): void {
    const today        = new Date();
    const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    this.frm.patchValue({
      WarehouseID: '', ItemID: '0',
      TransactionType: 'ALL',
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
