using Microsoft.AspNetCore.Mvc;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IPayrollRepository
    {
        Task<List<SalaryAdvanceDto>> GetEmployeeList();
        Task<List<SalaryAdvanceDto>> GetEmployeeListBySalaryAdvance(int TransType, string currentUser);
        Task<List<SalaryAdvanceDto>> GetEmployeeListByBranchCode(string branchCode);
        Task<List<SalaryAdvanceDto>> GetEmployeeListByAdvanceID(int Id);
        Task<List<SalaryAdvanceDto>> GetListByEmplyeeType(DateTime advanceDate, string branch, string employeeType, int transType, decimal advanceAmount, string race);
        Task<List<SalaryAdvanceDto>> GetEmployeeAdvanceList(DateTime advanceDate, string branch, string employeeType, string client, int transType, decimal advanceAmount, string race);
        Task<SalaryAdvance> SaveAndUpdateSalaryMonthlyAdvance(SalaryAdvance salaryAdvance);
        Task SaveEmployeeItemIssuesAsync(List<EmployeeItemIssue> items);
        Task<List<SalaryAdvance>> GetSalaryAdvanceById(int employeeId,int id);
        Task<List<Employee>> GetEmployeeById(int employeeId);
        Task<List<SalaryAdvance>> GetSalaryAdvanceByDateAndEmployee(SalaryAdvance salaryAdvance);
        Task<List<InventoryCategory>> GetInventoryCategories();
        string GetNewVoucherNumberAsync(int transType);
        List<ItemMasterDto> GetUniformItemRows(int AdvanceID, int Category);
        bool GetSalaryProcessDateByEmployeeID(int employeeID, int year, int month);
        DateTime GetResignDateByEmployeeID(int employeeID);
        Task<IEnumerable<MiscTrans>> GetMiscTrans(string currentUser);
        Task<List<MiscTrans>> GetMiscTransById(int id);
        Task<MiscTrans> SaveAndUpdateMiscTrans(MiscTrans miscTrans);
        Task DeleteMiscTransById(int id);
        Task<List<SalaryAdvance>> SaveAndUpdateSalaryDailyAdvances(List<SalaryAdvance> salaryAdvances);
        List<EmployeeDailyAdvanceRow> GetDailyAdvanceList(DateTime advanceDate, int employeeID, int advanceType);
        EmploymentDetails Get(string employeeNo);
        Task<string?> GetEmployeeNoAsync(int employeeId);
        Task<bool> DeleteSalaryAdvanceAsync(int salaryAdvanceID, string currentUser);
        Task<IEnumerable<SalaryAdvance>> GetSalaryAdvancesAsync(DateTime advanceDate, int employeeId, int transType);
       
        Task<SalaryAdvance?> GetEmployeeLoanIdAsync(int id, int transType);

        #region Attendance
        ActionResult<IEnumerable<ClientMaster>> GetClients(DateTime period, string branchCode);
        Attendance AttendanceByEmployeeID(DateTime Period,int employeeID);
        Task<List<AttendanceDetails>> AttendanceDetailsByID(int Id);
        Task<List<AttendanceDetails>> GetAttendanceDetailsList(int AttendanceID);
        Task<List<SalaryAttendenceDto>> GetEmployeeDetails(string branchCode, string employeeNo);
        bool IsSalaryProcessDoneForCurrentPeriod(string branch,string employeeType, DateTime dtPeriod);
        int CalculateAge(DateTime birthDate);
        List<string> GetEmployeeAttendanceList(DateTime period, string branch);
        bool IsTemporaryEmployee(string employeeCode);
        List<string> GetTemporaryEmployeeList(string branch);
        int GetAnnualLeave(int employeeId, DateTime period);
        int DateDiffInMonths(DateTime startDate, DateTime endDate);
        Task<ActionResult> SaveAndUpdateAttendance(Attendance attendanceModel, List<AttendanceDetails> attendanceDetails);
        Task<bool> DeleteAttendanceAsync(int dID, string username);
        List<EmployeeDto> GetList(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, DateTime attendancePeriod, string status);
        List<EmployeeDto> getListByEmployee(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status);
        Task<DateTime?> GetLatestAttendancePeriodAsync(int employeeId, int year, int month);
        List<EmployeeDto> GetListEmployeeByClient(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status, string empClient);
        Task<List<MiscTransDto>> GetList(DateTime transDate, decimal employeeId);
        #endregion

        #region Salary Processing
        string LastSalaryProcessRemarks(DateTime period, string branchCode, string employeeType);
        string Process(string branch, string employeeType, string remarks, DateTime period, bool lockProcess, string currentUser, string companyCode);
        #endregion

    }
}
