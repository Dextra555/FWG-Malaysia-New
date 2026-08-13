export class EmployeeSip {
  CompanyCode: string = '';
  SSM: string = '';
  EMPICNO: string = '';
  EmployeeName: string = '';
  Period:  Date = new Date();
  SIPTotal: number = 0;
  EMPJoinDate:  Date = new Date();
  EmpStatus: string = '';
 

     // Method to allow partial initialization
  setEmployeeData(data: Partial<EmployeeSip>): void {
    Object.assign(this, data);
  }
}