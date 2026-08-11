import { Component, OnInit } from '@angular/core';
import {environment} from "../../../../../environments/environment";
import {DomSanitizer, SafeResourceUrl} from "@angular/platform-browser";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import {MastermoduleService} from "../../../../service/mastermodule.service";
import {InventoryService} from "../../../../service/inventory.service";
import {EmployeeService} from "../../../../service/employee.service";
import { Router, NavigationEnd } from '@angular/router';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';

@Component({
  selector: 'app-invoice-ageing',
  templateUrl: './invoice-ageing.component.html',
  styleUrls: ['./invoice-ageing.component.css']
})
export class InvoiceAgeingComponent implements OnInit {
  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = "Finance/"
  frm!: FormGroup;
  branchList: any = [];
  itemList: any = [];
  allMasterClientList: any = [];   // full list loaded on init
  masterClientList: any = [];
  allClientList: any = [];         // full client list loaded on init
  clientList: any = [];
  currentUser: string = "";
  errorMessage: string = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;
  reportPageName: string = "";

  constructor(public sanitizer: DomSanitizer, private _masterService: MastermoduleService, private service: InventoryService, private empService: EmployeeService, 
    private fb: FormBuilder,private router: Router, private _dataService: DatasharingService) {    
    this.frm = fb.group({
      Branch: [""],
      MasterClient: [""],
      Client: [""],
      StartDate: ["", Validators.required],
      EndDate: ["", Validators.required],
    })

    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }

  }
  ngOnInit(): void {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this._dataService.scrollToTop(); // Scroll to top on route change
      }
    });
    this.currentUser = sessionStorage.getItem('username')!;
    if (this.currentUser == null || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Invoice Ageing Report');
  }

  getUserAccessRights(userName: string, screenName: string) {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess = data.Read
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
          if (this.userAccessModel.readAccess === true || this.currentUser == 'superadmin') {
            this.warningMessage = '';
            this._masterService.GetBranchListByUserName(this.currentUser).subscribe((d: any) => {
              this.branchList = d;
            });
            // Load all HQ master clients independent of branch selection
            this._masterService.getClientMsterListByStatus('Active').subscribe((d: any) => {
              this.allMasterClientList = d.filter((c: any) => c.IsClientHeadQuarters === true);
              this.masterClientList = [...this.allMasterClientList];
              this.allClientList = d;
            });

          } else {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
                      You do not have permissions to view this page. <br>
                      If you feel you should have access to this page, Please contact administrator. <br>
                      Thank you`;
          }
        }
        this.hideSpinner();
      },
      (error) => {
        this.handleErrors(error);
      }
    );
  }
  returnDate(date?: any) {
    let currentDate = new Date();
    if (date) {
      currentDate = new Date(date);
    }

    const year = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0'); // Month is zero-based
    const day = String(currentDate.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  clkBtn(number: number) {
    const branchValue = this.frm.get('Branch')?.value;
    if (number == 1) {
      // Use all-branches report when 'ALL' is selected, otherwise single-branch report
      this.reportPageName = (branchValue === 'ALL')
        ? 'InvoiceAgeingAllBranchesReport.aspx?'
        : 'InvoiceAgeingReport.aspx?';
    } else if (number == 2) {
      this.reportPageName = 'InvoiceCollectionTotalReportNoTax.aspx?';
    } else {
      this.reportPageName = 'InvoiceAgeingAllBranchesReport.aspx?';
    }
    this.reportPageName += "LoginID=" + this.currentUser;
    this.onSubmit();
  }

  branchChange(data: any) {
    this.errorMessage = '';
    this.clientList = [];
    this.frm.patchValue({ MasterClient: '', Client: '' });

    // Always show all HQ clients regardless of branch selection.
    // HQ clients are corporate entities and may not share the same branch code
    // as their sub-clients — filtering HQ by branch here would hide valid options.
    this.masterClientList = [...this.allMasterClientList];
  }

  masterClientChange(data: any) {
    this.errorMessage = '';
    this.clientList = [];
    this.frm.patchValue({ Client: '' });

    const hqCode = data.value;
    const branchCode = this.frm.get('Branch')?.value;

    if (hqCode) {
      // Show clients that belong to this HQ (sub-clients via SuperClientCode, or the HQ itself)
      let filtered = this.allClientList.filter(
        (c: any) => c.SuperClientCode === hqCode || c.Code === hqCode
      );
      // Further narrow by branch if one is selected
      if (branchCode && branchCode !== '0') {
        filtered = filtered.filter((c: any) => c.Branch === branchCode);
      }
      this.clientList = filtered;
    }
  }

  onSubmit() {
    this.url = environment.baseReportUrl;
    this.url += this.currentUrl;
    let localURL = "";
    if (this.frm.invalid) {
      return;
    }

    localURL += "&StartDate=" + this.returnDate(this.frm.get("StartDate")?.value)
    localURL += "&EndDate=" + this.returnDate(this.frm.get("EndDate")?.value)
    localURL += "&Branch=" + (this.frm.get("Branch")?.value === 'ALL' ? '' : (this.frm.get("Branch")?.value || ''))
    localURL += "&MasterClient=" + (this.frm.get("MasterClient")?.value ?? '')
    localURL += "&Client=" + (this.frm.get("Client")?.value ?? '')

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url +  this.reportPageName + localURL);

  }

  handleErrors(error: string) {
    if (error != null && error != '') {
      this.hideSpinner();
    }
  };
  hideSpinner() {
    this.showLoadingSpinner = false;
  }

}
