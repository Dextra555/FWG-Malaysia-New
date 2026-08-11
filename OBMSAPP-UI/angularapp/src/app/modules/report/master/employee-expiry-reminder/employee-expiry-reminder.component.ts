import { Component, OnInit } from '@angular/core';
import { environment } from 'src/environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';

@Component({
  selector: 'app-employee-expiry-reminder',
  templateUrl: './employee-expiry-reminder.component.html',
  styleUrls: ['./employee-expiry-reminder.component.css']
})
export class EmployeeExpiryReminderComponent implements OnInit {

  currentUrl: string = 'Master/EmployeeExpiryReminder.aspx?';

  urlSafe: SafeResourceUrl | undefined;
  activeType: 'Visa' | 'Passport' = 'Visa';  // default active button

  currentUser: string = '';
  warningMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;

  constructor(
    public sanitizer: DomSanitizer,
    private _masterService: MastermoduleService,
    private _dataService: DatasharingService
  ) {
    this.userAccessModel = {
      readAccess: false, updateAccess: false,
      deleteAccess: false, createAccess: false
    };
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => {
        this.currentUser = u;
        this.getUserAccessRights(this.currentUser);
      });
    } else {
      this.getUserAccessRights(this.currentUser);
    }
  }

  getUserAccessRights(userName: string) {
    if (userName === 'superadmin' || userName === 'admin') {
      this.userAccessModel = {
        readAccess: true, createAccess: true,
        updateAccess: true, deleteAccess: true
      };
      this.warningMessage = '';
      // Auto-load Visa report on open
      this.loadReport('Visa');
      return;
    }

    this._masterService.getUserAccessRights(userName, 'Employee Master').subscribe(data => {
      if (data) {
        this.userAccessModel.readAccess = data.Read;
        if (data.Read) {
          this.warningMessage = '';
          // Auto-load Visa report on open
          this.loadReport('Visa');
        } else {
          this.warningMessage = `Dear <B>${userName}</B>, <br>
            You do not have permissions to view this page. <br>
            Please contact administrator. Thank you`;
        }
      }
    });
  }

  loadReport(type: 'Visa' | 'Passport') {
    this.activeType = type;

    let reportUrl = environment.baseReportUrl + this.currentUrl;
    reportUrl += `LoginID=${encodeURIComponent(this.currentUser)}`;
    reportUrl += `&Branch=All`;
    reportUrl += `&ExpiryType=${encodeURIComponent(type)}`;

    this.showLoadingSpinner = true;
    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(reportUrl);
  }

  onReportLoad() {
    setTimeout(() => (this.showLoadingSpinner = false), 2000);
  }

  handleErrors(error: string) {
    if (error) this.showLoadingSpinner = false;
  }
}
