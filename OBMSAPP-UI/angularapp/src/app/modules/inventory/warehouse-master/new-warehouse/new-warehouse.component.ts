import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from "@angular/router";
import { InventoryService } from "../../../../service/inventory.service";
import Swal from "sweetalert2";
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';

@Component({
  selector: 'app-new-warehouse',
  templateUrl: './new-warehouse.component.html',
  styleUrls: ['./new-warehouse.component.css']
})
export class NewWarehouseComponent implements OnInit {

  frm!: FormGroup;
  isEdit: boolean = false;
  ID: any;
  branches: any[] = [];
  branchSearch = ''; filteredBranches: any[] = [];
  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;

  constructor(private fb: FormBuilder, private activatedRoute: ActivatedRoute, private service: InventoryService, private route: Router,
    private _dataService: DatasharingService, private _masterService: MastermoduleService) {
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }
    this.frm = this.fb.group({
      ID: [0],
      Code: ['', Validators.required],
      Name: ['', Validators.required],
      Address: [''],
      BranchID: [0, Validators.required],
      IsActive: [true]
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
    this.getUserAccessRights(this.currentUser, 'Warehouse Master');
    this.loadBranches();

    this.service.getWarehouseCode().subscribe((d: any) => {
      if (this.ID == 0 || this.ID == undefined) {
        this.frm.get('Code')?.setValue(d['Code']);
      }
    });

    if (this.ID != 0 && this.ID != undefined) {
      this.isEdit = true;
      this.service.getWarehouseByID(this.ID).subscribe((d: any) => {
        this.frm.patchValue(d);
      })
    }
    this.frm.get('Code')?.disable({ onlySelf: true });
  }

  getUserAccessRights(userName: string, screenName: string) {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.userAccessModel.readAccess = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;

          if (!(this.userAccessModel.createAccess === true || this.userAccessModel.updateAccess === true || this.currentUser == 'superadmin')) {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
                      You do not have permissions to create/edit this warehouse. <br>
                      If you feel you should have access to this page, Please contact administrator. <br>
                      Thank you`;
            this.hideLoadingSpinner();
          }
        }
      },
      (error) => {
        this.handleErrors(error);
      }
    );
  }

  loadBranches() {
    this.service.getBranchList().subscribe(
      (d: any) => {
        this.branches = d;
        this.filteredBranches = [...d];
      },
      (error) => this.handleErrors(error)
    );
  }

  onBranchOpen(opened: boolean) { if (opened) { this.branchSearch = ''; this.filteredBranches = [...this.branches]; } }
  onBranchSearch(q: string) { this.branchSearch = q; const s = q.toLowerCase(); this.filteredBranches = this.branches.filter(b => b.Name?.toLowerCase().includes(s) || b.Code?.toLowerCase().includes(s)); }

  onSubmit() {
    if (this.frm.invalid) {
      return;
    }
    this.showLoadingSpinner = true;
    let data = this.frm.getRawValue();
    this.service.saveWarehouse(data).subscribe({
      next: res => {
        this.showMessage("Warehouse Saved/Updated Successfully", 'success', 'Success Message');
        this.frm.reset();
        this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => {
          this.route.navigate(['/inventory/warehouse-master']);
        });
      },
      error: err => {
        this.showMessage(`Warehouse failed to save: ${err}`, 'error', 'Error Message');
      }
    });
  }

  onCancelClick() {
    this.ID = 0;
    this.isEdit = false;
    this.frm.reset();
    this.route.navigate(['/inventory/warehouse-master']);
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