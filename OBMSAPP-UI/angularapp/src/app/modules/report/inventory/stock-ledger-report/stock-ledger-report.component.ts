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

  // ASPX report base URL  (LoginID appended in constructor)
  baseUrl: string = environment.baseReportUrl + 'Inventory/StockLedgerReport.aspx?';
  urlSafe: SafeResourceUrl | undefined;

  frm!: FormGroup;
  warehouseList: any[] = [];
  itemList: any[]      = [];
  categoryList: any[]  = [];
  sizeList: string[]   = [];

  currentUser: string    = '';
  errorMessage: string   = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  loadingSizes: boolean = false;
  userAccessModel!: UserAccessModel;

<<<<<<< HEAD
  /** Parent categories that have sub-categories — shown as optgroups */
  categoryGroups: { parent: any; children: any[] }[] = [];
  /** Standalone categories (no parent, no children) — shown flat */
  categoryFlat: any[] = [];

=======
>>>>>>> 754e8b5 (Updated payslip changes)
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

    const today        = new Date();
    const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);

    this.frm = this.fb.group({
      WarehouseID:     ['', Validators.required],
      ItemID:          ['0'],
      TransactionType: ['ALL'],
      StartDate:       [firstOfMonth, Validators.required],
      EndDate:         [today,        Validators.required],
      CategoryID:      ['0'],
      Size:            ['']
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

    // When Category changes → clear selected Size and reload size list
    this.frm.get('CategoryID')!.valueChanges.subscribe((catId: any) => {
      this.frm.get('Size')!.setValue('', { emitEvent: false });
      this.sizeList = [];
      this.loadSizes(parseInt(catId, 10) || 0);
    });
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

            this.service.getWarehouseMasters(this.currentUser).subscribe((d: any[]) => {
              this.warehouseList = (d || []).filter((w: any) => w.IsActive);
            });

            this.service.GetReportHQMaster().subscribe((d: any) => {
              this.itemList = d || [];
            });

            this.service.getCategories().subscribe((d: any) => {
              const all = (d || []).filter((c: any) => c.ID > 0);
              this.categoryList = all;
              this.buildCategoryGroups(all);
            });

            this.loadSizes(0);   // pre-load all sizes
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

  /** Group categories into parent/children for the optgroup dropdown. */
  buildCategoryGroups(all: any[]): void {
    const parentIDs = new Set(
      all.filter(c => c.ParentCategoryID).map(c => c.ParentCategoryID)
    );
    const groups: { parent: any; children: any[] }[] = [];
    const usedIDs = new Set<number>();

    all.filter(c => parentIDs.has(c.ID)).forEach(parent => {
      const children = all.filter(c => c.ParentCategoryID === parent.ID);
      groups.push({ parent, children });
      usedIDs.add(parent.ID);
      children.forEach(ch => usedIDs.add(ch.ID));
    });

    this.categoryGroups = groups;
    this.categoryFlat   = all.filter(c => !usedIDs.has(c.ID));
  }

  /** Display label for the category badge. */
  getCategoryLabel(id: any): string {
    const found = this.categoryList.find(c => c.ID === parseInt(id, 10));
    return found ? found.Name : String(id);
  }

  /** Load size list for the given categoryID (0 = all). */
  loadSizes(categoryID: number): void {
    this.loadingSizes = true;
    this.service.getItemSizesByCategory(categoryID).subscribe({
      next: (data: any[]) => {
        this.sizeList    = (data || []).map((s: any) => s.Size || s.size || '').filter(Boolean);
        this.loadingSizes = false;
      },
      error: () => { this.sizeList = []; this.loadingSizes = false; }
    });
  }

  /** Build the ASPX URL and set it on the iframe. */
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

<<<<<<< HEAD
    const categoryID = parseInt(raw.CategoryID, 10) || 0;
    const size       = (raw.Size || '').trim();

=======
    let localURL = '';
    localURL += '&StartDate='       + this.returnDate(raw.StartDate);
    localURL += '&EndDate='         + this.returnDate(raw.EndDate);
    localURL += '&WarehouseID='     + (raw.WarehouseID     || '0');
    localURL += '&ItemID='          + (raw.ItemID          || '0');
    localURL += '&TransactionType=' + (raw.TransactionType || 'ALL');
    localURL += '&format=html';

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
>>>>>>> 754e8b5 (Updated payslip changes)
    const params = new URLSearchParams();
    params.set('LoginID',         this.currentUser             || '');
    params.set('StartDate',       this.returnDate(raw.StartDate));
    params.set('EndDate',         this.returnDate(raw.EndDate));
    params.set('WarehouseID',     String(raw.WarehouseID       || 0));
    params.set('ItemID',          String(raw.ItemID            || 0));
    params.set('TransactionType', raw.TransactionType          || 'ALL');
    params.set('CategoryID',      String(categoryID));
    if (size) params.set('Size',  size);

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(
      environment.baseReportUrl + 'Inventory/StockLedgerReport.aspx?' + params.toString()
    );
  }

  /** Open the Excel export in a new tab. */
  onExportExcel(): void {
    if (this.frm.invalid) { this.frm.markAllAsTouched(); return; }

    const raw = this.frm.value;
    const from = new Date(raw.StartDate);
    const to   = new Date(raw.EndDate);
    if (from > to) { this.errorMessage = "The 'From' date must be before the 'To' date."; return; }
    this.errorMessage = '';

    const categoryID = parseInt(raw.CategoryID, 10) || 0;
    const size       = (raw.Size || '').trim();

    const params = new URLSearchParams();
    params.set('LoginID',         this.currentUser             || '');
    params.set('StartDate',       this.returnDate(raw.StartDate));
    params.set('EndDate',         this.returnDate(raw.EndDate));
    params.set('WarehouseID',     String(raw.WarehouseID       || 0));
    params.set('ItemID',          String(raw.ItemID            || 0));
    params.set('TransactionType', raw.TransactionType          || 'ALL');
    params.set('CategoryID',      String(categoryID));
    if (size) params.set('Size',  size);
    params.set('format',          'excel');

    window.open(
      environment.baseReportUrl + 'Inventory/StockLedgerReport.aspx?' + params.toString(),
      '_blank'
    );
  }

  onClear(): void {
    const today        = new Date();
    const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    this.frm.patchValue({
      WarehouseID: '', ItemID: '0', TransactionType: 'ALL',
      StartDate: firstOfMonth, EndDate: today,
      CategoryID: '0', Size: ''
    });
    this.urlSafe      = undefined;
    this.errorMessage = '';
<<<<<<< HEAD
    this.loadSizes(0);
=======
>>>>>>> 754e8b5 (Updated payslip changes)
  }

  returnDate(date?: any): string {
    const d = date ? new Date(date) : new Date();
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
