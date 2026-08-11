import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { SafeResourceUrl, DomSanitizer } from '@angular/platform-browser';
import { Router, NavigationEnd } from '@angular/router';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-profit-and-loss-without-contra',
  templateUrl: './profit-and-loss-without-contra.component.html',
  styleUrls: ['./profit-and-loss-without-contra.component.css']
})
export class ProfitAndLossWithoutContraComponent implements OnInit {

  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Accounting/ProfitAndLossWithoutContraReport.aspx?';
  frm!: FormGroup;
  currentUser: string = '';
  errorMessage: string = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;
  reportUrl: string = '';   // holds the raw URL for Excel export

  constructor(
    public sanitizer: DomSanitizer,
    private _masterService: MastermoduleService,
    private fb: FormBuilder,
    private router: Router,
    private _dataService: DatasharingService
  ) {
    this.frm = fb.group({
      FromDate: ['', Validators.required],
      ToDate:   ['', Validators.required]
    });

    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false
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
    this.getUserAccessRights(this.currentUser, 'Profit And Lost');
  }

  getUserAccessRights(userName: string, screenName: string) {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess   = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;

          if (this.userAccessModel.readAccess !== true && this.currentUser !== 'superadmin') {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
              You do not have permissions to view this page. <br>
              If you feel you should have access to this page, Please contact administrator. <br>
              Thank you`;
          }
        }
        this.hideSpinner();
      },
      (error) => { this.handleErrors(error); }
    );
  }

  onSubmit() {
    if (this.frm.invalid) {
      this.errorMessage = 'Please select both From Date and To Date.';
      return;
    }
    this.errorMessage = '';

    const fromDate = this.toIsoDate(new Date(this.frm.get('FromDate')?.value));
    const toDate   = this.toIsoDate(new Date(this.frm.get('ToDate')?.value));

    // Store raw URL for Excel export reuse
    this.reportUrl = `${environment.baseReportUrl}${this.currentUrl}FromDate=${fromDate}&ToDate=${toDate}`;

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.reportUrl);
  }

  exportExcel() {
    if (!this.reportUrl) return;
    // Append format=excel — browser will download the file
    window.open(this.reportUrl + '&format=excel', '_blank');
  }

  toIsoDate(date: Date): string {
    const year  = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day   = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  handleErrors(error: string) {
    if (error != null && error !== '') { this.hideSpinner(); }
  }

  hideSpinner() {
    this.showLoadingSpinner = false;
  }
}
