import { Component, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-leave-application-form',
  templateUrl: './leave-application-form.component.html',
  styleUrls: ['./leave-application-form.component.css']
})
export class LeaveApplicationFormComponent implements OnInit {
  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  showLoadingSpinner: boolean = false;
  warningMessage: string = '';
  currentUser: string = '';
  userRole: string = '';
  errorMessage: string = '';
  userAccessModel!: UserAccessModel;
  staffAdminAccessModel!: UserAccessModel;
  constructor(public sanitizer: DomSanitizer, private _dataService: DatasharingService, 
    private _masterService: MastermoduleService,private router: Router) {
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }
    this.staffAdminAccessModel = {
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
    const rawRole = sessionStorage.getItem('userrole');
    if (rawRole == '1') { this.userRole = 'admin'; }
    else if (rawRole == '2') { this.userRole = 'superadmin'; }
    else { this.userRole = 'user'; }
    if (this.currentUser == null || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Pay Slip Report');
  }
  getUserAccessRights(userName: string, screenName: string) {
    this.showLoadingSpinner = true;
    forkJoin({
      mainAccess: this._masterService.getUserAccessRights(userName, screenName),
      staffAdminAccess: this._masterService.getUserAccessRights(userName, 'Payroll Staff Admin')
    }).subscribe({
      next: (res: any) => {
        if (res.mainAccess) {
          this.userAccessModel = {
            readAccess: res.mainAccess.Read,
            updateAccess: res.mainAccess.Update,
            deleteAccess: res.mainAccess.Delete,
            createAccess: res.mainAccess.Create
          };
        }
        if (res.staffAdminAccess) {
          this.staffAdminAccessModel = {
            readAccess: res.staffAdminAccess.Read,
            updateAccess: res.staffAdminAccess.Update,
            deleteAccess: res.staffAdminAccess.Delete,
            createAccess: res.staffAdminAccess.Create
          };
        }
        if (this.userAccessModel.readAccess === true || this.userRole == 'superadmin') {
          this.warningMessage = '';
          this.url = environment.baseReportUrl;
          this.url += 'Payroll/LeaveApplicationForms.aspx';
          this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.url);
        } else {
          this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
                    You do not have permissions to view this page. <br>
                    If you feel you should have access to this page, Please contact administrator. <br>
                    Thank you`;
        }
        this.hideSpinner();
      },
      error: (error) => { this.handleErrors(error); }
    });
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
