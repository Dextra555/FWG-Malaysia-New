import { Component, OnInit } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { Router, NavigationEnd } from '@angular/router';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';

@Component({
  selector: 'app-sales-invoice-collection-report',
  templateUrl: './sales-invoice-collection-report.component.html',
  styleUrls: ['./sales-invoice-collection-report.component.css']
})
export class SalesInvoiceCollectionReportComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Finance/';
  frm!: FormGroup;
  branchList: any = [];   // kept to avoid template errors if referenced elsewhere
  currentUser: string = '';
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
      StartDate: ['', Validators.required],
      EndDate: ['', Validators.required],
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
    this.getUserAccessRights(this.currentUser, 'Sales Report');
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

          const hasAccess =
            this.currentUser === 'superadmin' ||
            this.userAccessModel.readAccess === true ||
            data.Read === null ||
            data.Read === undefined;

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
    const year  = currentDate.getFullYear();
    const month = String(currentDate.getMonth() + 1).padStart(2, '0');
    const day   = String(currentDate.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  /** Builds the common query-string shared by both report pages */
  private buildQueryParams(): string {
    const startDate = this.returnDate(this.frm.get('StartDate')?.value);
    const endDate   = this.returnDate(this.frm.get('EndDate')?.value);
    return `LoginID=${this.currentUser}&StartDate=${startDate}&EndDate=${endDate}&Branch=0`;
  }

  /** Show button — opens the existing detail Crystal Report */
  onShowDetail() {
    if (this.frm.invalid) {
      return;
    }
    const baseUrl = environment.baseReportUrl + this.currentUrl;
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(
      baseUrl + 'SalesInvoiceCollectionReport.aspx?' + this.buildQueryParams()
    );
  }

  /** Summary by Month button — opens the new pivot summary Crystal Report */
  onShowSummary() {
    if (this.frm.invalid) {
      return;
    }
    const baseUrl = environment.baseReportUrl + this.currentUrl;
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(
      baseUrl + 'SalesInvoiceCollectionSummaryReport.aspx?' + this.buildQueryParams()
    );
  }

  // Keep onSubmit as an alias for detail (used if form ngSubmit is ever re-wired)
  onSubmit() {
    this.onShowDetail();
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
