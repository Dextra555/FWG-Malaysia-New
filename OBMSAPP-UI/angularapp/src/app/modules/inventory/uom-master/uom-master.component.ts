import { AfterViewInit, Component, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { MatDialog } from '@angular/material/dialog';
import { InventoryService } from "../../../service/inventory.service";
import { UserAccessModel } from 'src/app/model/userAccesModel';
import { MastermoduleService } from 'src/app/service/mastermodule.service';
import { DatasharingService } from 'src/app/service/datasharing.service';
import { DialogConfirmationComponent } from 'src/app/components/dialog-confirmation/dialog-confirmation.component';
import Swal from 'sweetalert2';
import { Router } from '@angular/router';

export interface UOM {
  ID: number;
  Code: string;
  Name: string;
  IsPrimary: boolean;
}

@Component({
  selector: 'app-uom-master',
  templateUrl: './uom-master.component.html',
  styleUrls: ['./uom-master.component.css']
})
export class UOMMasterComponent implements AfterViewInit {
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
  warningMessage: string = '';
  errorMessage: string = '';
  showLoadingSpinner: boolean = false;
  data: any = [];
  displayedColumns: string[] = ['Code', 'Name', 'IsPrimary', 'action'];
  dataSource: MatTableDataSource<UOM> = new MatTableDataSource<UOM>([]);
  pageSizeOptions: number[] = [];

  constructor(private _liveAnnouncer: LiveAnnouncer, public dialog: MatDialog, private service: InventoryService, private route: Router,
    private _dataService: DatasharingService, private _masterService: MastermoduleService) {
    this.userAccessModel = {
      readAccess: false,
      updateAccess: false,
      deleteAccess: false,
      createAccess: false,
    }
    for (let i = 10; i <= 50; i += 10) {
      this.pageSizeOptions.push(i);
    }

  }
  ngOnInit(): void {
    this.currentUser = sessionStorage.getItem('username')!;
    if (this.currentUser == 'null' || this.currentUser == undefined) {
      this._dataService.getUsername().subscribe((username) => {
        this.currentUser = username;
      });
    }
    this.getUserAccessRights(this.currentUser, 'UOM Master');
  }
  getUserAccessRights(userName: string, screenName: string) {
    this._masterService.getUserAccessRights(userName, screenName).subscribe(
      (data) => {
        if (data != null) {
          this.showLoadingSpinner = true;
          this.userAccessModel.readAccess = data.Read
          this.userAccessModel.deleteAccess = data.Delete;
          this.userAccessModel.updateAccess = data.Update;
          this.userAccessModel.createAccess = data.Create;

          if (this.userAccessModel.readAccess === true || this.currentUser == 'superadmin') {
            this.warningMessage = '';
            this.service.getUOMMasters().subscribe((d: any) => {
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
                this.errorMessage = `No data available for <span style="color: black;">${this.currentUser}</span>. Please try again later.`;
              }
              this.hideLoadingSpinner();
            })
          } else {
            this.warningMessage = `Dear <B>${this.currentUser}</B>, <br>
                      You do not have permissions to view this page. <br>
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
  announceSortChange(sortState: Sort) {
    if (sortState.direction) {
      this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this._liveAnnouncer.announce('Sorting cleared');
    }
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  deleteClickButton(id: number): void {
    this.showLoadingSpinner = true;

    this.dialog
      .open(DialogConfirmationComponent, {
        data: `Are you sure you want to delete this UOM?`
      })
      .afterClosed()
      .subscribe((result: { confirmDialog: boolean; remarks: any }) => {
        if (result.confirmDialog) {

          this.service.deleteUOM(id, this.currentUser).subscribe({
            next: res => {
              this.showMessage(`UOM deleted successfully.`, 'success', 'Success Message');
              this.route.navigateByUrl('/dummy', { skipLocationChange: true }).then(() => {
                this.route.navigate(['/inventory/uom-master']);
              });
            },
            error: err => {
              this.showMessage(`UOM Failed to delete records due to ${err}`, 'error', 'Error Message');
            }
          });

        } else {
          this.hideLoadingSpinner();
        }
      });

  }
  private showMessage(message: string, icon: 'success' | 'warning' | 'info' | 'error' = 'info',
    title: 'Success Message' | 'Warning Message' | 'Error Message' = 'Success Message'): void {
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