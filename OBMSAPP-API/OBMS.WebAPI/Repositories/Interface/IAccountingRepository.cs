using OBMS.WebAPI.Models.DTO;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IAccountingRepository
    {
        Task<List<AccountGLReportDto>> GetDataList(int processYear, string branch);
        Task<List<AccountGLReportDto>> GetDataList(int processYear, string branch, string type);
        Task<bool> GetRecord(int processYear, string branch);
        Task<bool> GetRecordWithType(int processYear, string branch, string type);
        Task<bool> AddGLRecordAsync(string currentUser, List<AccountGLReportDto> accountGLReportDto);
        Task<bool> DeleteGLRecordAsync(int processYear, string branch, string currentUser);
    }
}
