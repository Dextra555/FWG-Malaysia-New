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
  categoryList: any[]  = [];
  sizeList: string[]   = [];

  currentUser: string    = '';
  errorMessage: string   = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  loadingSizes: boolean  = false;
  userAccessModel!: UserAccessModel;

  /** Parent categories with sub-categories — rendered as mat-optgroup */
  categoryGroups: { parent: any; children: any[] }[] = [];
  /** Standalone categories (no children) — rendered flat */
  categoryFlat: any[] = [];

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
      WarehouseID: ['0'],
      ItemID:      ['0'],
      CategoryID:  ['0'],
      Size:        ['']
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

    // When Category changes → clear Size and reload size list
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

            this.loadSizes(0);
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

  getCategoryLabel(id: any): string {
    const found = this.categoryList.find(c => c.ID === parseInt(id, 10));
    return found ? found.Name : String(id);
  }

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

  onSubmit(): void {
    this.errorMessage = '';
    const raw        = this.frm.value;
    const categoryID = parseInt(raw.CategoryID, 10) || 0;
    const size       = (raw.Size || '').trim();

    const params = new URLSearchParams();
    params.set('LoginID',     this.currentUser       || '');
    params.set('WarehouseID', String(raw.WarehouseID || 0));
    params.set('ItemID',      String(raw.ItemID      || 0));
    params.set('CategoryID',  String(categoryID));
    if (size) params.set('Size', size);

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(
      environment.baseReportUrl + 'Inventory/CurrentStockReport.aspx?' + params.toString()
    );
  }

  onExportExcel(): void {
    this.errorMessage = '';
    const raw        = this.frm.value;
    const categoryID = parseInt(raw.CategoryID, 10) || 0;
    const size       = (raw.Size || '').trim();

    const params = new URLSearchParams();
    params.set('LoginID',     this.currentUser       || '');
    params.set('WarehouseID', String(raw.WarehouseID || 0));
    params.set('ItemID',      String(raw.ItemID      || 0));
    params.set('CategoryID',  String(categoryID));
    if (size) params.set('Size', size);
    params.set('format', 'excel');

    window.open(
      environment.baseReportUrl + 'Inventory/CurrentStockReport.aspx?' + params.toString(),
      '_blank'
    );
  }

  onClear(): void {
    this.frm.patchValue({ WarehouseID: '0', ItemID: '0', CategoryID: '0', Size: '' });
    this.urlSafe      = undefined;
    this.errorMessage = '';
    this.loadSizes(0);
  }
}
