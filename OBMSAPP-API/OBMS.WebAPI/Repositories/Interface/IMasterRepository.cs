using Microsoft.AspNetCore.Mvc;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IMasterRepository
    {
        Task<List<BranchMaster>> GetBranchMasterList(string branchCode);
        Task<List<BranchMaster>> GetBranchListByUserName(string userName);
        Task<List<BranchMaster>> GetBranchMasterAllList();
        Task<BranchMaster> saveAndUpdateBranchMaster(BranchMaster branchMaster);
        Task<ActionResult<BranchMaster>> DeleteBranchMasterByCode(string code);
        Task<List<ClientMaster>> GetClientMsterList(string clientCode, string status, string currentUser);
        Task<List<ClientMaster>> GetClientMsterListByStatus(string status);
        Task<List<ClientMaster>> GetClientMsterListByBranch(string branchCode);
        Task<ClientMaster> saveAndUpdateClientMaster(ClientMaster branchMaster);
        Task<ActionResult<ClientMaster>> DeleteClientMasterByCode(string code);
        string GetNClientMasterCode();

        Task<List<ShiftTimeMaster>> GetShiftTimeMasterList();
        Task<ShiftTimeMaster> saveAndUpdateShiftTimeMaster(ShiftTimeMaster branchMaster);
        Task<ActionResult<ShiftTimeMaster>> DeleteShiftTimeMasterById(int Id);

        Task<List<SIP>> GetSIPMasterList(int Id);
        Task<SIP> saveAndUpdateSIPMaster(SIP sipMaster);
        Task<ActionResult<SIP>> DeleteSIPMasterById(int Id);

        Task<List<EPF>> GetEPFMasterList(int Id);
        Task<EPF> saveAndUpdateEPFMaster(EPF epfMaster);
        Task<ActionResult<EPF>> DeleteEPFMasterById(int Id);

        Task<List<LeaveSystem>> GetLeaveMasterList(int Id);
        Task<LeaveSystem> saveAndUpdateLeaveMaster(LeaveSystem leaveMaster);
        Task<ActionResult<LeaveSystem>> DeleteLeaveMasterById(int Id);


        Task<List<SalaryStructure>> GetSalaryMasterList(int salaryId, string status);
        Task<List<SalaryStructure>> GetSalaryListByStatus(string activeStatus);
        Task<SalaryStructure> saveAndUpdateSalaryMaster(SalaryStructure salaryStructure);
        Task<ActionResult<SalaryStructure>> DeleteSalaryMasterById(int salaryId);

        Task<List<SOCSO>> GetSOCSOMasterList(int socsoId);
        Task<SOCSO> saveAndUpdateSOCSOMaster(SOCSO salaryStructure);
        Task<ActionResult<SOCSO>> DeleteSOCSOMasterById(int socsoId);

        Task<List<IncomeTax>> GetIncomeTaxMasterList(int Id);
        Task<IncomeTax> saveAndUpdateIncomeTaxMaster(IncomeTax incomeTax);
        Task<ActionResult<IncomeTax>> DeleteIncomeTaxMasterById(int Id);
        Task<List<KKDNExcelListView>> GetListWithBlankRowAsync(string branch, string kdnVetting);
        string GetBranchMasterCode();
    }
}

