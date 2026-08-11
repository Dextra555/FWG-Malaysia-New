import { Component, OnInit } from '@angular/core';
import { environment } from "../../../../../environments/environment";
import { DomSanitizer, SafeResourceUrl } from "@angular/platform-browser";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { MastermoduleService } from "../../../../service/mastermodule.service";
import { Router, NavigationEnd } from '@angular/router';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';

@Component({
  selector: 'app-branch-payment-summary',
  templateUrl: './branch-payment-summary.component.html',
  styleUrls: ['./branch-payment-summary.component.css']
})
export class BranchPaymentSummaryComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = "Finance/";
  frm!: FormGroup;
  branchList: any = [];
  bankList: any = [];
  currentUser: string = "";
  errorMessage: string = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;

  constructor(
    public sanitizer: DomSanitizer,
    private _masterService: MastermoduleService,
    private fb: FormBuilder,
    private router: Router,
    private _dataService: DatasharingService
  ) {
    this.frm = fb.group({
      StartDate: ["", Validators.required],
      EndDate: ["", Validators.required],
      BankId: [""],
    });

    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    };
  }

  ngOnInit(): void {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this._dataService.scrollToTop();
      }
    });

    this.currentUser = sessionStorage.getItem('username')!;
    if (this.currentUser == null || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Branch Payment Summary Report');
    this.loadBankList();
  }

  loadBankList() {
    this._masterService.GetBankListByUserName(this.currentUser).subscribe(
      (data) => { this.bankList = data ?? []; },
      (error) => { this.handleErrors(error); }
    );
  }

  getUserAccessRights(userName: string, screenName: string) {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess   = data.Read   ?? false;
          this.userAccessModel.deleteAccess  = data.Delete ?? false;
          this.userAccessModel.updateAccess  = data.Update ?? false;
          this.userAccessModel.createAccess  = data.Create ?? false;

          // Grant access when: superadmin, Read=true, or no permission record exists yet (Read=null)
          const hasAccess = this.currentUser === 'superadmin'
            || this.userAccessModel.readAccess === true
            || data.Read === null
            || data.Read === undefined;

          if (hasAccess) {
            this.warningMessage = '';
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

  returnDate(date?: any): string {
    let currentDate = new Date();
    if (date) {
      currentDate = new Date(date);
    }
    const year = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0');
    const day = String(currentDate.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  onSubmit() {
    if (this.frm.invalid) {
      return;
    }

    this.url = environment.baseReportUrl + this.currentUrl;

    const bankId = this.frm.get("BankId")?.value ?? "0";

    let localURL = "BranchPaymentSummaryReport.aspx?";
    localURL += "LoginID=" + this.currentUser;
    localURL += "&Branch=0";
    localURL += "&BankId=" + (bankId || "0");
    localURL += "&StartDate=" + this.returnDate(this.frm.get("StartDate")?.value);
    localURL += "&EndDate=" + this.returnDate(this.frm.get("EndDate")?.value);

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url + localURL);
  }

  handleErrors(error: string) {
    if (error != null && error !== '') {
      this.hideSpinner();
    }
  }

  hideSpinner() {
    this.showLoadingSpinner = false;
  }
}
