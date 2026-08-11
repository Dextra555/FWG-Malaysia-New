import { Component, OnInit } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MastermoduleService } from '../../../../service/mastermodule.service';
import { Router, NavigationEnd } from '@angular/router';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';

@Component({
  selector: 'app-separated-profit-loss',
  templateUrl: './separated-profit-loss.component.html',
  styleUrls: ['./separated-profit-loss.component.css']
})
export class SeparatedProfitLossComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Finance/';
  frm!: FormGroup;
  branchList: any[] = [];
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
      Branch: ['0'],
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
    if (!this.currentUser) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Separated Profit Loss Report');
  }

  getUserAccessRights(userName: string, screenName: string): void {
    this.showLoadingSpinner = true;
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess   = data.Read   ?? false;
          this.userAccessModel.deleteAccess  = data.Delete ?? false;
          this.userAccessModel.updateAccess  = data.Update ?? false;
          this.userAccessModel.createAccess  = data.Create ?? false;

          const hasAccess = this.currentUser === 'superadmin'
            || this.userAccessModel.readAccess === true
            || data.Read === null
            || data.Read === undefined;

          if (hasAccess) {
            this.warningMessage = '';
            this._masterService.GetBranchListByUserName(this.currentUser).subscribe((d: any) => {
              this.branchList = d;
            });
          } else {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
              You do not have permissions to view this page. <br>
              If you feel you should have access to this page, please contact administrator. <br>
              Thank you`;
          }
        }
        this.hideSpinner();
      },
      (error) => { this.handleErrors(error); }
    );
  }

  returnDate(date?: any): string {
    const d = date ? new Date(date) : new Date();
    const year  = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day   = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  onSubmit(): void {
    if (this.frm.invalid) { return; }

    const baseUrl  = environment.baseReportUrl + this.currentUrl;
    const branch   = this.frm.get('Branch')?.value ?? '0';
    const start    = this.returnDate(this.frm.get('StartDate')?.value);
    const end      = this.returnDate(this.frm.get('EndDate')?.value);

    let localURL = 'SeparatedProfitLossReport.aspx?';
    localURL += 'LoginID='   + this.currentUser;
    localURL += '&StartDate=' + start;
    localURL += '&EndDate='   + end;
    localURL += '&Branch='    + branch;

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(baseUrl + localURL);
  }

  handleErrors(error: string): void {
    if (error) { this.hideSpinner(); }
  }

  hideSpinner(): void {
    this.showLoadingSpinner = false;
  }
}
