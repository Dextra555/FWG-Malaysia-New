using OBMS.WebAPI.Models.DTO;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface ISalaryProcess
    {
        List<KKDNExcelListDto> GetListWithBlankRow(string Branch, string EmployeeType, DateTime dtDateJoinFrom, DateTime dtDateJoinTo, string KDNVetting);
        string Process(string branch, string employeeType, string remarks, DateTime period, bool lockProcess, string currentUser, string companyCode);
    }
}
