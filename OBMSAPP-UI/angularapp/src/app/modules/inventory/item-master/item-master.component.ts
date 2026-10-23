import { AfterViewInit, Component, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatDialog } from '@angular/material/dialog';
import { InventoryService } from "../../../service/inventory.service";
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { DialogConfirmationComponent } from 'src/app/components/dialog-confirmation/dialog-confirmation.component';
import Swal from "sweetalert2";
import { Router } from '@angular/router';

export interface IItem {
  ID: number;
  Code: string;
  ItemName: string;
  CategoryName: string;
  BrandName: string;
  UOMName: string;
  WarehouseID: number | null;
  WarehouseName: string;
  Price: number;
  ReorderLevel: number;
  ReorderQuantity: number;
  Remarks: string;
}

@Component({
  selector: 'app-item-master',
  templateUrl: './item-master.component.html',
  styleUrls: ['./item-master.component.css']
})
export class ItemMasterComponent implements AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngAfterViewInit(): void {
    if (this.dataSource) {
      this.dataSource.paginator = this.paginator;
      this.dataSource.sort = this.sort;
    }
  }

  userAccessModel!: UserAccessModel;
  currentUser: string = '';
  userRole: string = '';
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;
  data: any = [];
  displayedColumns: string[] = ['Code', 'ItemName', 'CategoryName', 'BrandName', 'UOMName', 'WarehouseName', 'Price', 'ReorderLevel', 'Remarks', 'action'];
  dataSource: MatTableDataSource<IItem> = new MatTableDataSource<IItem>([]);
  pageSizeOptions: number[] = [];

  constructor(
    private _liveAnnouncer: LiveAnnouncer,
    public dialog: MatDialog,
    private service: InventoryService,
    private route: Router,
    private _dataService: DatasharingService,
    private _masterService: MastermoduleService
  ) {
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    };
    for (let i = 10; i <= 50; i += 10) {
      this.pageSizeOptions.push(i);
    }
  }

  ngOnInit(): void {
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
    this.getUserAccessRights(this.currentUser, 'Item Master');
  }

  getUserAccessRights(userName: string, screenName: string) {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.showLoadingSpinner = true;
          this.userAccessModel.readAccess = data.Read;
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;

          if (this.userAccessModel.readAccess === true || this.userRole == 'superadmin') {
            this.warningMessage = '';
            this.service.getItems().subscribe((d: any) => {
              this.data = d;
              if (d.length > 0) {
                this.dataSource = new MatTableDataSource(d);
                this.pageSizeOptions = [];
                const totalRows = d.length;
                for (let i = 10; i <= totalRows && i <= 1000; i += 10) {
                  this.pageSizeOptions.push(i);
                }
                if (totalRows > 0 && totalRows < 10) {
                  this.pageSizeOptions.push(totalRows);
                }
                this.dataSource.sort = this.sort;
                this.dataSource.paginator = this.paginator;
              } else {
                this.errorMessage = `No items found. Click <b>+ New</b> to add your first item.`;
              }
              this.hideLoadingSpinner();
            });
          } else {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
              You do not have permissions to view this page. <br>
              Please contact your administrator.`;
            this.hideLoadingSpinner();
          }
        }
      },
      (error) => { this.handleErrors(error); }
    );
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  announceSortChange(sortState: Sort) {
    if (sortState.direction) {
      this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this._liveAnnouncer.announce('Sorting cleared');
    }
  }

  deleteClickButton(id: number): void {
    this.showLoadingSpinner = true;
    this.dialog
      .open(DialogConfirmationComponent, {
        data: `Are you sure you want to delete this Item?`
      })
      .afterClosed()
      .subscribe((result: { confirmDialog: boolean; remarks: any }) => {
        if (result.confirmDialog) {
          this.service.deleteItem(id, this.currentUser).subscribe({
            next: () => {
              this.showMessage('Item deleted successfully.', 'success', 'Success Message');
              this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => {
                this.route.navigate(['/inventory/item-master']);
              });
            },
            error: (err: any) => {
              this.showMessage(`Failed to delete item: ${err}`, 'error', 'Error Message');
            }
          });
        } else {
          this.hideLoadingSpinner();
        }
      });
  }

  private showMessage(
    message: string,
    icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message' = 'Success Message'
  ): void {
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
      customClass: { popup: 'swal-top-offset' }
    });
    this.hideLoadingSpinner();
  }

  handleErrors(error: string) {
    if (error != null && error != '') {
      this.errorMessage = error;
      this.hideLoadingSpinner();
    }
  }

  hideLoadingSpinner() {
    this.showLoadingSpinner = false;
  }
}
