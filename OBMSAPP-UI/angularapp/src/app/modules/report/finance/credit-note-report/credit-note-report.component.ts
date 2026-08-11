import { Component, OnInit } from '@angular/core';
import { environment } from 'src/environments/environment';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { Router, NavigationEnd } from '@angular/router';

@Component({
  selector: 'app-credit-note-report',
  templateUrl: './credit-note-report.component.html',
  styleUrls: ['./credit-note-report.component.css']
})
export class CreditNoteReportComponent implements OnInit {

  url: string = environment.baseReportUrl;
  urlSafe: SafeResourceUrl | undefined;
  currentUrl: string = 'Finance/';

  frm!: FormGroup;
  branchList: any[] = [];
  clientList: any[] = [];

  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;
  userAccessModel!: UserAccessModel;

  constructor(
    public sanitizer: DomSanitizer,
    private fb: FormBuilder,
    private _masterService: MastermoduleService,
    private _dataService: DatasharingService,
    private router: Router
  ) {
    this.userAccessModel = { readAccess: false, updateAccess: false, deleteAccess: false, createAccess: false };
    this.frm = this.fb.group({
      StartDate: ['', Validators.required],
      EndDate: ['', Validators.required],
      Branch: [''],
      Client: ['']
    });
  }

  ngOnInit(): void {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) this._dataService.scrollToTop();
    });

    this.currentUser = sessionStorage.getItem('username')!;
    if (!this.currentUser || this.currentUser === 'null') {
      this._dataService.getUsername().subscribe(u => this.currentUser = u);
    }
    this.getUserAccessRights(this.currentUser, 'CreditNote');
  }

  getUserAccessRights(userName: string, screenName: string) {
    if (userName === 'superadmin' || userName === 'admin') {
      this.userAccessModel = { readAccess: true, createAccess: true, updateAccess: true, deleteAccess: true };
      this.warningMessage = '';
      this.loadBranches();
      return;
    }
    this._masterService.getUserAccessRights(userName, screenName).subscribe(data => {
      if (data) {
        this.userAccessModel.readAccess = data.Read;
        if (data.Read || data.Read === null) {
          this.warningMessage = '';
          this.loadBranches();
        } else {
          this.warningMessage = `Dear <B>${userName}</B>, you do not have permissions to view this page.`;
        }
      }
    });
  }

  loadBranches() {
    this._masterService.GetBranchListByUserName(this.currentUser).subscribe((data: any) => {
      this.branchList = data;
    });
  }

  onBranchChange(branchCode: string) {
    this.clientList = [];
    this.frm.patchValue({ Client: '' });
    if (branchCode) {
      this._masterService.getClientMsterListByBranch(branchCode).subscribe((data: any) => {
        this.clientList = data;
      });
    }
  }

  returnDate(date?: any): string {
    const d = date ? new Date(date) : new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  onSubmit() {
    if (this.frm.invalid) return;

    const branch = this.frm.get('Branch')?.value || '0';
    const client = this.frm.get('Client')?.value || '';
    const startDate = this.returnDate(this.frm.get('StartDate')?.value);
    const endDate   = this.returnDate(this.frm.get('EndDate')?.value);

    let localURL = 'CreditNoteList.aspx?';
    localURL += `LoginID=${encodeURIComponent(this.currentUser)}`;
    localURL += `&Branch=${encodeURIComponent(branch)}`;
    localURL += `&Client=${encodeURIComponent(client)}`;
    localURL += `&StartDate=${startDate}`;
    localURL += `&EndDate=${endDate}`;

    this.urlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(
      environment.baseReportUrl + this.currentUrl + localURL
    );
  }

  hideSpinner() { this.showLoadingSpinner = false; }
}
