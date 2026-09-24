import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from "@angular/router";
import { InventoryService } from "../../../../service/inventory.service";
import Swal from "sweetalert2";
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';

@Component({
  selector: 'app-new-recepient',
  templateUrl: './new-recepient.component.html',
  styleUrls: ['./new-recepient.component.css']
})
export class NewRecepientComponent implements OnInit {

  frm!: FormGroup;
  stateList: any;
  categories: any;
  isEdit: boolean = false;
  ID: any;
  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  constructor(private fb: FormBuilder, public dialog: MatDialog, private route: Router, private activatedRoute: ActivatedRoute, private service: InventoryService,
    private _dataService: DatasharingService, private _masterService: MastermoduleService
  ) {
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }
    this.ID = this.activatedRoute.snapshot.params['ID'];

    service.getRecipientMaster().subscribe((d: any) => {
      this.categories = d['categories'];
      this.stateList = d['stateList'];
    })

    // Auto-generate Recepient Code on new form
    if (this.ID == 0 || this.ID == undefined) {
      service.getRecipientCode().subscribe((d: any) => {
        this.frm.get('Code')?.setValue(d.Code);
        this.frm.get('Code')?.disable({ onlySelf: true });
      });
    }
    this.frm = this.fb.group({
      Id: [0],
      Code: ['', Validators.required],
      Name: ['', Validators.required],
      ICNO: ['', Validators.required],
      Address1: ['', Validators.required],
      Address2: ['', Validators.required],
      PostCode: ['', Validators.required],
      City: ['', Validators.required],
      State: [''],
      Phone: ['', Validators.required],
      Fax: [''],
      Supervisor: ['', Validators.required],
      CreditLimit: [''],
      Category: [''],
      Status: ['A'],
      LASTUPDATE: [''],
      LastUpdatedBy: [''],
    });

    if (this.ID != 0 && this.ID != undefined) {
      this.isEdit = true;
      service.getRecipientByID(this.ID).subscribe((d: any) => {
        this.frm.patchValue(d);
        this.frm.get('Category')?.setValue(d['Category'].toString());
      });
    }
  }

  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (this.currentUser == 'null' || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Recepient Master');
  }
  getUserAccessRights(userName: string, screenName: string) {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess = data.Read
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;
        }

      },
      (error) => {
        this.handleErrors(error);
      }
    );
  }
  onSubmit() {
    if (this.frm.invalid) {
      this.frm.markAllAsTouched();
      return;
    }

    let data = this.frm.getRawValue();
    data['LASTUPDATE'] = this.returnDate();
    // Convert empty Category to null — backend expects int? not empty string
    if (data['Category'] === '' || data['Category'] === undefined) {
      data['Category'] = null;
    } else {
      data['Category'] = parseInt(data['Category'], 10) || null;
    }
    // Convert empty CreditLimit to 0 — backend expects decimal not empty string
    if (data['CreditLimit'] === '' || data['CreditLimit'] === null || data['CreditLimit'] === undefined) {
      data['CreditLimit'] = 0;
    } else {
      data['CreditLimit'] = parseFloat(data['CreditLimit']) || 0;
    }
    this.service.saveRecipient(data).subscribe({
      next: (d: any) => {
        this.showMessage("Recepient Saved/Updated Successfully", 'success', 'Success Message');
        this.frm.reset();
        this.route.navigate(['/inventory/recepient-master']);
      },
      error: (err: any) => {
        let errMsg = 'Please check your input and try again.';
        if (err?.error?.errors) {
          // ASP.NET model validation errors — extract field names and messages
          const validationErrors = err.error.errors;
          const messages = Object.keys(validationErrors)
            .map(field => `${field}: ${validationErrors[field].join(', ')}`)
            .join(' | ');
          errMsg = messages || errMsg;
        } else if (err?.error?.Message) {
          errMsg = err.error.Message;
        } else if (err?.error?.message) {
          errMsg = err.error.message;
        } else if (err?.error?.title) {
          errMsg = err.error.title;
        }
        this.showMessage(`Failed to save Recepient: ${errMsg}`, 'error', 'Error Message');
      }
    });
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

  onCancelClick() {
    this.ID = 0;
    this.isEdit = false;
    this.frm.reset();
    this.route.navigate(['/inventory/recepient-master']);
  }

  private showMessage(message: string, icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message'): void {
    Swal.fire({
      toast: true,
      position: 'top',
      showConfirmButton: false,
      title: title,
      text: message,
      icon: icon, // Dynamically set the icon based on the parameter
      showCloseButton: false,
      timer: 5000,
      width: '600px', 
      customClass: {
        popup: 'swal-top-offset'
      }
    });
    this.hideLoadingSpinner();
    return;
  }
  handleErrors(error: string) {
    if (error != null && error != '') {
      this.errorMessage = error;
      this.hideLoadingSpinner();
    }
  }
  hideLoadingSpinner() {
    this.showLoadingSpinner = false
  }
}
