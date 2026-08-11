using Microsoft.AspNetCore.Mvc;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IRegisterRepository
    {
        Task<bool> ChangePasswordAsync(ChangePasswordRequestDto changePasswordRequest);
        Task<Obmsuser> RegisterAsync(Obmsuser user);
        Task<Obmsuser?> LoginUser(Obmsuser user);
        List<Obmsuser> GetObmsusers();
        Obmsuser GetObmsuserById(int userId);
        Obmsuser GetObmsuserByName(string userName);
        Task<ActionResult<Obmsuser>> DeleteObmsuser(int userId);
        List<BankList> GetBankList();
        Task<List<BankList>> GetUserBankList(string user);
        BankList GetBankListById(int Id);
        Task<ActionResult<BankList>> DeleteBank(int Id);
        Task<BankList> saveAndUpdateBankDetails(BankList bankList);
        List<BankMaster> GetBankMasterList();
        Task<List<BankMaster>> GetUserBankMaster(string user);
        BankMaster GetBankMasterById(int Id);
        Task<ActionResult<BankMaster>> DeleteBankMaster(int Id);
        Task<BankMaster> saveAndUpdateBankMasterDetails(BankMaster bankList);
        string GetBankPrefixNoByBankID(string BankCode);
        ChequeMaster GetChequeListById(int BankID);
        List<ChequeMasterDto> GetChequeList();
        Task<List<ChequeMasterDto>> GetUserChequeList(string user);
        Task<ActionResult<ChequeMaster>> DeleteChequeMaster(int Id);
        Task<ChequeMaster> saveAndUpdateChequeMasterDetails(ChequeMaster chequeMaster);
        List<Obmsuser> GetUsers(string currentUser);
        Task<List<ScreenList>> GetScreensByCategory(string categoryName);
        List<OBMSPermissionDto> GetPermissionsWithScreens(string categoryName, string userName);
        Task<List<OBMSPermissions>> SaveAndUpdateObmsPermissions(List<OBMSPermissions> oBMSPermissions);
        List<OBMSBranches> GetObmsBranchesPermission();
        List<OBMSBranches> GetObmsBranchesPermissionByUser(string UserName);
        Task<List<OBMSBranches>> SaveAndUpdateObmsBranches(List<OBMSBranches> oBMSBranches);
        List<OBMSBanksDto> GetObmsBanksPermission(string userName);
        List<OBMSBanks> GetObmsBankMasterPwrmission(string userName);
        Task<List<OBMSBanks>> SaveAndUpdateObmsBanks(List<OBMSBanks> oBMSBanks);
        Task<string> GetBankAccShortNameByBankIDAsync(decimal bankID);

        #region UserAccess Rights
        OBMSPermissions GetUserAccessRights(string userName, string screenName);

        #endregion
    }
}
