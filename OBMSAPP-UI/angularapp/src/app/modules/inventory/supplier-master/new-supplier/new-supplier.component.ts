import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { InventoryService } from "../../../../service/inventory.service";
import { ActivatedRoute, Router } from "@angular/router";
import Swal from "sweetalert2";
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';

@Component({
  selector: 'app-new-supplier',
  templateUrl: './new-supplier.component.html',
  styleUrls: ['./new-supplier.component.css']
})
export class NewSupplierComponent implements OnInit {

  frm!: FormGroup;
  stateList: any;
  isEdit: boolean = false;
  ID: any;
  Code: any;
  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  userRole: string = '';
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
    
    this.frm = this.fb.group({
      Id: [0],
      Code: [''],
      Name: ['', Validators.required],
      Address1: ['', Validators.required],
      Address2: ['', Validators.required],
      PostCode: ['', [Validators.required, Validators.maxLength(5), Validators.pattern('^[0-9]{5}$')]],
      City: ['', Validators.required],
      State: [''],
      Phone: ['', Validators.required],
      Fax: [''],
      ContactPerson: ['', Validators.required],
      CreditLimit: [''],
      Category: [''],
      Status: ['A'],
    });
    
  }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    const rawRole = sessionStorage.getItem('userrole');
    if (rawRole == '1') { this.userRole = 'admin'; }
    else if (rawRole == '2') { this.userRole = 'superadmin'; }
    else { this.userRole = 'user'; }
    if (this.currentUser == 'null' || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'Supplier Master');

    this.service.getSupplierCode().subscribe((d: any) => {
      this.Code = d['Code'];
      this.stateList = d['stateList'];
    }, () => {
    },
      () => {
        if (this.ID == 0 || this.ID == undefined) {
          this.frm.get('Code')?.setValue(this.Code);
        }
      });

    if (this.ID != 0 && this.ID != undefined) {
      this.isEdit = true;
      this.service.getSupplierByID(this.ID).subscribe((d: any) => {
        this.frm.patchValue(d);
      });
    }

    this.frm.get('Code')?.disable({ onlySelf: true })

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
    this.service.saveSupplier(data).subscribe({
      next: (d: any) => {
        this.showMessage("Supplier Saved/Updated Successfully", 'success', 'Success Message');
        this.frm.reset();
        this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => {
          this.route.navigate(['/inventory/supplier-master']);
        });
      },
      error: (err: any) => {
        const errMsg = err?.error?.message || err?.error?.Message || err?.message || 'Unknown error occurred';
        this.showMessage(`Failed to save Supplier: ${errMsg}`, 'error', 'Error Message');
      }
    });
  }

  onCancelClick() {
    this.ID = 0;
    this.isEdit = false;
    this.frm.reset();
    this.route.navigate(['/inventory/supplier-master']);
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
