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
  selector: 'app-new-uom',
  templateUrl: './new-uom.component.html',
  styleUrls: ['./new-uom.component.css']
})
export class NewUOMComponent implements OnInit {

  frm!: FormGroup;
  isEdit: boolean = false;
  ID: any;
  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  constructor(private fb: FormBuilder, public dialog: MatDialog, private activatedRoute: ActivatedRoute, private service: InventoryService, private route: Router,
    private _dataService: DatasharingService, private _masterService: MastermoduleService) {
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }

    this.frm = this.fb.group({
      ID: [0],
      Code: [''],
      Name: ['', Validators.required],
      ShortName: [''],
      IsPrimary: [false]
    })
  }

  ngOnInit(): void {
    this.ID = this.activatedRoute.snapshot.params['ID'];
    this.currentUser = sessionStorage.getItem('username')!;
    if (this.currentUser == 'null' || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'UOM Master');

    this.service.getUOMCode().subscribe((d: any) => {
      if (this.ID == 0 || this.ID == undefined) {
        this.frm.get('Code')?.setValue(d['Code']);
      }
    });

    if (this.ID != 0 && this.ID != undefined) {
      this.isEdit = true;
      this.service.getUOMByID(this.ID).subscribe((d: any) => {
        this.frm.patchValue(d);
      })
    }
    this.frm.get('Code')?.disable({ onlySelf: true });
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
      return;
    }
    this.showLoadingSpinner = true;
    let data = this.frm.getRawValue();
    this.service.saveUOM(data).subscribe({
      next: res => {
        this.showMessage("UOM Saved/Updated Successfully", 'success', 'Success Message');
        this.frm.reset();
        this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => {
          this.route.navigate(['/inventory/uom-master']);
        });
      },
      error: err => {
        this.showMessage(`UOM failed to save: ${err}`, 'error', 'Error Message');
      }
    });
  }
  onCancelClick() {
    this.ID = 0;
    this.isEdit = false;
    this.frm.reset();
    this.route.navigate(['/inventory/uom-master']);
  }

  private showMessage(message: string, icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message' = 'Success Message'): void {
    Swal.fire({
      toast: true,
      position: 'top',
      showConfirmButton: false,
      title: title,
      text: message,
      icon: icon,
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