using BoldReports.Processing.ObjectModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using SkiaSharp;
using Syncfusion.XlsIO.Implementation.Security;

namespace OBMS.WebAPI.Repositories.Implementation
{
    public class RegisterRepository : IRegisterRepository
    {
        private readonly OBMSDbContext _dbContext;

        public RegisterRepository(OBMSDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Obmsuser?> LoginUser(Obmsuser user)
        {
          var userAvailable =  _dbContext.Obmsusers.Where(u=>u.Name == user.Name && u.Password == user.Password).SingleOrDefault();
            return Task.FromResult(userAvailable);
                     
        }

        public async Task<Obmsuser> RegisterAsync(Obmsuser obmsuser)
        {
            var existingUser = await _dbContext.Obmsusers.Where(u => u.Name == obmsuser.Name).SingleOrDefaultAsync();
            if(existingUser != null)
            {
                // Update each property individually
                existingUser.Password = obmsuser.Password;
                existingUser.Name = obmsuser.Name;
                existingUser.Designation = obmsuser.Designation;
                existingUser.Description = obmsuser.Description;
                existingUser.CreatedBy = obmsuser.CreatedBy;
                existingUser.CreatedDate = obmsuser.CreatedDate;
                existingUser.LastUpdatedDate = obmsuser.LastUpdatedDate;
                existingUser.IsDeleted = obmsuser.IsDeleted;
                existingUser.LastUpdatedBy = obmsuser.LastUpdatedBy;
                existingUser.IsView = obmsuser.IsView;
                existingUser.IsAdmin = obmsuser.IsAdmin;
                existingUser.Email = obmsuser.Email;
                existingUser.ContactNo = obmsuser.ContactNo;
                _dbContext.Obmsusers.Update(existingUser);
                await _dbContext.SaveChangesAsync();
                return existingUser;
            }
            else
            {
                _dbContext.Obmsusers.Add(obmsuser);
                await _dbContext.SaveChangesAsync();
                return obmsuser;
            }
           
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequestDto changePasswordRequest)
        {
            var password = _dbContext.Obmsusers.Where(u => u.Name == changePasswordRequest.CurrentUser && u.Password == changePasswordRequest.CurrentPassword).SingleOrDefault();

            if(password != null)
            {
                var userAvailable = _dbContext.Obmsusers.Where(u => u.Name == changePasswordRequest.CurrentUser).SingleOrDefault();
                if (userAvailable != null)
                {
                    userAvailable.Password = changePasswordRequest.NewPassword;
                    userAvailable.LastUpdatedDate = DateTime.Now;
                    _dbContext.Obmsusers.Update(userAvailable);
                    await _dbContext.SaveChangesAsync();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }            
        }
        // Get all Obmsusers
        public List<Obmsuser> GetObmsusers()
        {
            return _dbContext.Obmsusers.Where(u=>u.IsDeleted != true).ToList();
        }
        // Get a specific Obmsuser by Id
        public Obmsuser GetObmsuserById(int userId)
        {
            var user = _dbContext.Obmsusers
                .Where(u => u.UserId == userId && u.IsDeleted != true)
                .SingleOrDefault();
            return user ?? throw new Exception($"User with ID {userId} not found.");        
           
        }
        public Obmsuser GetObmsuserByName(string userName)
        {
            var user = _dbContext.Obmsusers
                .Where(u => u.Name == userName && !u.IsDeleted)
                .SingleOrDefault();
            return user ?? throw new Exception($"User with ID {userName} not found.");

        }
        // Delete an Obmsuser by Id
        public async Task<ActionResult<Obmsuser>> DeleteObmsuser(int userId)
        {
            var obmsuser =  _dbContext.Obmsusers.Find(userId);
            if (obmsuser != null)
            {
                obmsuser.IsDeleted = true;
                _dbContext.Obmsusers.Update(obmsuser);
                 _dbContext.SaveChanges();
            }
            return new ActionResult<Obmsuser>(obmsuser);
        }

        
        public List<BankList> GetBankList()
        {
            return _dbContext.BankLists.OrderBy(x=> x.BankName).ToList();
        }
        public async Task<List<BankList>> GetUserBankList(string user)
        {
            bool isSuperAdmin = user.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

            var query = from obms in _dbContext.OBMSBanks
                        join bm in _dbContext.BankMasters on obms.BankID equals bm.BankId
                        join bl in _dbContext.BankLists on bm.BankCode equals bl.BankCode
                        where isSuperAdmin || (obms.IsAllowed == true && obms.Name == user)
                        select new BankList
                        {
                            ID = bl.ID,
                            BankCode = bl.BankCode,
                            BankName = bl.BankName
                        };

            return await query
                .Distinct()
                .OrderBy(x => x.BankName)
                .ToListAsync();
        }

        public async Task<List<BankMaster>> GetUserBankMaster(string user)
        {
            var bankMaster = await (from bm in _dbContext.BankMasters
                                   join obms in _dbContext.OBMSBanks on bm.BankId equals obms.BankID
                                   join bl in _dbContext.BankLists on bm.BankCode equals bl.BankCode
                                   where obms.Name == user
                                   select bm).ToListAsync();

            return bankMaster;
        }
        public BankList GetBankListById(int Id)
        {
            var user = _dbContext.BankLists.Where(u => u.ID == Id).SingleOrDefault();
            return user ?? throw new Exception($"User with ID {Id} not found.");

        }
        public async Task<BankList> saveAndUpdateBankDetails(BankList bankList)
        {
            var existingBank = await _dbContext.BankLists.Where(u => u.ID == bankList.ID).SingleOrDefaultAsync();           
            if (existingBank != null)
            {
                string oldBankCode = existingBank.BankCode!;
                // Update each property individually
                existingBank.BankCode = bankList.BankCode;
                existingBank.BankName = bankList.BankName;
                existingBank.LASTUPDATE = bankList.LASTUPDATE;
                existingBank.LastUpdatedBy = bankList.LastUpdatedBy;
                _dbContext.BankLists.Update(existingBank);

                // Update BankMaster(s) with old BankCode
                var bankMasters = await _dbContext.BankMasters
                    .Where(x => x.BankCode == oldBankCode)
                    .ToListAsync();

                foreach (var bm in bankMasters)
                {
                    bm.BankCode = bankList.BankCode!;
                    bm.LASTUPDATE = bankList.LASTUPDATE;
                    bm.LastUpdatedBy = bankList.LastUpdatedBy!;
                }
                await _dbContext.SaveChangesAsync();
                return existingBank;
            }
            else
            {
                _dbContext.BankLists.Add(bankList);
                await _dbContext.SaveChangesAsync();
                return bankList;
            }

        }        
        public async Task<ActionResult<BankList>> DeleteBank(int Id)
        {
            var bank = _dbContext.BankLists.Find(Id);
            if (bank != null)
            {
                _dbContext.BankLists.Remove(bank);
                _dbContext.SaveChanges();
            }
            return new ActionResult<BankList>(bank);
        }

        public List<BankMaster> GetBankMasterList()
        {
            return _dbContext.BankMasters.ToList();
        }
        public BankMaster GetBankMasterById(int BankId)
        {
            var user = _dbContext.BankMasters.Where(u => u.BankId == BankId).SingleOrDefault();
            return user ?? throw new Exception($"User with ID {BankId} not found.");

        }
        public async Task<BankMaster> saveAndUpdateBankMasterDetails(BankMaster bankMaster)
        {
            var existingBank = await _dbContext.BankMasters.Where(u => u.BankId == bankMaster.BankId).SingleOrDefaultAsync();
            if (existingBank != null)
            {
                // Update each property individually
                existingBank.BankCode = bankMaster.BankCode;
                existingBank.Accname = bankMaster.Accname;
                existingBank.AccShortName = bankMaster.AccShortName;
                existingBank.Accno = bankMaster.Accno;
                existingBank.PREFIX = bankMaster.PREFIX;
                existingBank.LASTUPDATE = bankMaster.LASTUPDATE;
                existingBank.LastUpdatedBy = bankMaster.LastUpdatedBy;
                _dbContext.BankMasters.Update(existingBank);
                await _dbContext.SaveChangesAsync();
                return existingBank;
            }
            else
            {
                _dbContext.BankMasters.Add(bankMaster);
                await _dbContext.SaveChangesAsync();
                return bankMaster;
            }

        }
        public async Task<ActionResult<BankMaster>> DeleteBankMaster(int BankId)
        {
            var bankMaster = _dbContext.BankMasters.Find(BankId);
            if (bankMaster != null)
            {
                _dbContext.BankMasters.Remove(bankMaster);
                _dbContext.SaveChanges();
            }
            return new ActionResult<BankMaster>(bankMaster);
        }

        public string GetBankPrefixNoByBankID(string BankCode)
        {
            var maxPrefix = _dbContext.BankMasters
                .Where(b => b.BankCode == BankCode && b.PREFIX != null)
                .Max(b => b.PREFIX) + 1;

            return maxPrefix;

        }

        public ChequeMaster GetChequeListById(int Id)
        {
            try
            {
                var user = _dbContext.ChequeMasters.Where(u => u.ID == Id).SingleOrDefault();
                return user ?? throw new Exception($"User with ID {Id} not found.");

            }
            catch
            {
                throw;
            }
        }

        public List<ChequeMasterDto> GetChequeList()
        {
            try
            {
                var query = from chequeMaster in _dbContext.ChequeMasters
                            join bankMaster in _dbContext.BankMasters on chequeMaster.BankID equals bankMaster.BankId
                            where chequeMaster.IsActive == true
                            select new ChequeMasterDto
                            {
                                ID = chequeMaster.ID,
                                AccountName = bankMaster.Accname,
                                ChequeStart = chequeMaster.ChequeStart,
                                ChequeEnd = chequeMaster.ChequeEnd,
                                IsActive = chequeMaster.IsActive
                            };

                return query.ToList();

            }
            catch
            {
                throw;
            }
        }

        public async Task<List<ChequeMasterDto>> GetUserChequeList(string user)
        {
            try
            {
                var userChequeMaster = await(from chequeMaster in _dbContext.ChequeMasters
                            join bankMaster in _dbContext.BankMasters on chequeMaster.BankID equals bankMaster.BankId
                            join obmsbank in _dbContext.OBMSBanks on chequeMaster.BankID equals obmsbank.BankID
                            where chequeMaster.IsActive == true
                                  && obmsbank.Name == user    // Another example condition
                            select new ChequeMasterDto
                            {
                                ID = chequeMaster.ID,
                                AccountName = bankMaster.Accname,
                                ChequeStart = chequeMaster.ChequeStart,
                                ChequeEnd = chequeMaster.ChequeEnd,
                                IsActive = chequeMaster.IsActive
                            }).ToListAsync();

                return userChequeMaster;


            }
            catch
            {
                throw;
            }
        }
        public async Task<ChequeMaster> saveAndUpdateChequeMasterDetails(ChequeMaster chequeMaster)
        {
            var existingCheque = await _dbContext.ChequeMasters.Where(u => u.ID == chequeMaster.ID).SingleOrDefaultAsync();
            if (existingCheque != null)
            {
                // Update each property individually
                existingCheque.ChequeStart = chequeMaster.ChequeStart;
                existingCheque.ChequeEnd = chequeMaster.ChequeEnd;
                existingCheque.IsActive = chequeMaster.IsActive;
                existingCheque.LastUpdate = chequeMaster.LastUpdate;                
                existingCheque.LastUpdatedBy = chequeMaster.LastUpdatedBy;
                _dbContext.ChequeMasters.Update(existingCheque);
                await _dbContext.SaveChangesAsync();
                return existingCheque;
            }
            else
            {
                _dbContext.ChequeMasters.Add(chequeMaster);
                await _dbContext.SaveChangesAsync();
                return chequeMaster;
            }

        }

        public async Task<ActionResult<ChequeMaster>> DeleteChequeMaster(int Id)
        {
            var chequeMaster = _dbContext.ChequeMasters.Find(Id);
            if (chequeMaster != null)
            {
                chequeMaster.IsActive = true;
                _dbContext.ChequeMasters.Update(chequeMaster);
                await _dbContext.SaveChangesAsync();
            }
            return new ActionResult<ChequeMaster>(chequeMaster);
        }

        public List<Obmsuser> GetUsers(string currentUser)
        {
            try
            {
                if (string.Compare(currentUser.ToUpper(), "SUPERADMIN", true) == 0)
                {
                    return _dbContext.Obmsusers
                        .Where(u => !u.IsDeleted).ToList();
                }
                else if (string.Compare(currentUser.ToUpper(), "ADMIN", true) == 0)
                {
                    return _dbContext.Obmsusers
                        .Where(u => !u.IsDeleted && u.IsView == false).ToList();
                }
                else
                {
                    return _dbContext.Obmsusers
                        .Where(u => (u.CreatedBy == currentUser) && !u.IsDeleted && u.IsView == false).ToList();
                }
            }
            catch
            {
                throw;
            }
        }
      
        public async Task<List<OBMSPermissions>> SaveAndUpdateObmsPermissions(List<OBMSPermissions> oBMSPermissions)
        {
            List<OBMSPermissions> updatedRecords = new List<OBMSPermissions>();

            foreach (var oBMSPermission in oBMSPermissions)
            {
                var existingRecord = await _dbContext.OBMSPermissions
                    .Where(s => s.Name == oBMSPermission.Name && s.ScreenName == oBMSPermission.ScreenName).SingleOrDefaultAsync();

                if (existingRecord != null)
                {
                    // Update properties
                    existingRecord.Name = oBMSPermission.Name;
                    existingRecord.ScreenName = oBMSPermission.ScreenName;
                    existingRecord.Read = oBMSPermission.Read;
                    existingRecord.Create = oBMSPermission.Create;
                    existingRecord.Update = oBMSPermission.Update;
                    existingRecord.Delete = oBMSPermission.Delete;
                    existingRecord.LastUpdatedBy = oBMSPermission.LastUpdatedBy;

                    _dbContext.Update(existingRecord);
                    updatedRecords.Add(existingRecord);
                }
                else
                {
                    _dbContext.Add(oBMSPermission);
                    updatedRecords.Add(oBMSPermission);
                }
            }

            await _dbContext.SaveChangesAsync();
            return updatedRecords;
        }
        
        public async Task<List<OBMSBranches>> SaveAndUpdateObmsBranches(List<OBMSBranches> oBMSBranches)
        {
            List<OBMSBranches> updatedRecords = new List<OBMSBranches>();

            foreach (var oBMSBranche in oBMSBranches)
            {
                var existingRecord = await _dbContext.OBMSBranches
                    .Where(s => s.Name == oBMSBranche.Name && s.BranchCode == oBMSBranche.BranchCode).SingleOrDefaultAsync();

                if (existingRecord != null)
                {
                    // Update properties
                    existingRecord.Name = oBMSBranche.Name;
                    existingRecord.BranchCode = oBMSBranche.BranchCode;
                    existingRecord.IsAllowed = oBMSBranche.IsAllowed;
                    existingRecord.LastUpdatedDate = oBMSBranche.LastUpdatedDate;
                    existingRecord.LastUpdatedBy = oBMSBranche.LastUpdatedBy;

                    _dbContext.Update(existingRecord);
                    updatedRecords.Add(existingRecord);
                }
                else
                {
                    oBMSBranche.ID = 0;
                    updatedRecords.Add(oBMSBranche);
                    _dbContext.Add(oBMSBranche);                    
                }
            }

            await _dbContext.SaveChangesAsync();
            return updatedRecords;
        }

        public List<OBMSBanksDto> GetObmsBanksPermission(string userName)
        {
            try
            {

                var query = from obmsbanks in _dbContext.OBMSBanks
                            join banklist in _dbContext.BankLists on obmsbanks.BankID equals banklist.ID
                            where obmsbanks.Name == userName
                            select new OBMSBanksDto
                            {
                                ID = obmsbanks.ID,
                                Name = obmsbanks.Name,
                                BankID = obmsbanks.BankID,
                                IsAllowed = obmsbanks.IsAllowed,
                                LastUpdatedBy = obmsbanks.LastUpdatedBy,
                                BankCode = banklist.BankCode
                            };

                return query.ToList();
            }
            catch
            {
                throw;
            }
        }

        public List<OBMSBanks> GetObmsBankMasterPwrmission(string userName)
        {
            try
            {
                return _dbContext.OBMSBanks
                    .Where(u => u.Name == userName).ToList();

            }
            catch
            {
                throw;
            }
        }
        public async Task<List<OBMSBanks>> SaveAndUpdateObmsBanks(List<OBMSBanks> oBMSBanks)
        {
            List<OBMSBanks> updatedRecords = new List<OBMSBanks>();

            foreach (var oBMSBank in oBMSBanks)
            {
                var existingRecord = await _dbContext.OBMSBanks
                    .Where(s => s.Name == oBMSBank.Name && s.BankID == oBMSBank.BankID).SingleOrDefaultAsync();

                if (existingRecord != null)
                {
                    // Update properties
                    existingRecord.Name = oBMSBank.Name;
                    existingRecord.BankID = oBMSBank.BankID;
                    existingRecord.IsAllowed = oBMSBank.IsAllowed;
                    existingRecord.LastUpdatedDate = oBMSBank.LastUpdatedDate;
                    existingRecord.LastUpdatedBy = oBMSBank.LastUpdatedBy;

                    _dbContext.Update(existingRecord);
                    updatedRecords.Add(existingRecord);
                }
                else
                {
                    oBMSBank.ID = 0;
                    updatedRecords.Add(oBMSBank);
                    _dbContext.Add(oBMSBank);
                }
            }

            await _dbContext.SaveChangesAsync();
            return updatedRecords;
        }
        public async Task<string> GetBankAccShortNameByBankIDAsync(decimal bankID)
        {
            var bank = await _dbContext.BankMasters
                                     .Where(b => b.BankId == bankID)
                                     .Select(b => b.AccShortName)
                                     .FirstOrDefaultAsync();

            return bank ?? string.Empty;
        }
        #region UserAccess Rights
        public async Task<List<ScreenList>> GetScreensByCategory(string categoryName)
        {
            var screens = await _dbContext.ScreenLists
                .Where(s => s.Category.Contains(categoryName))
                .ToListAsync();

            return new List<ScreenList>(screens);
        }
        //public List<OBMSPermissionDto> GetPermissionsWithScreens(string categoryName, string userName)
        //{
        //    var query = from permission in _dbContext.OBMSPermissions
        //                join screen in _dbContext.ScreenLists on permission.ScreenName equals screen.ScreenName
        //                where permission.Name == userName && screen.Category == categoryName
        //                select new OBMSPermissionDto
        //                {
        //                    ID = permission.ID,
        //                    Name = permission.Name,
        //                    ScreenName = permission.ScreenName,
        //                    Create = permission.Create,
        //                    Read = permission.Read,
        //                    Update = permission.Update,
        //                    Delete = permission.Delete,
        //                    LastUpdatedBy = permission.LastUpdatedBy,
        //                    LastUpdate = screen.LastUpdate,
        //                    Category = screen.Category
        //                };

        //    return query.ToList();
        //}

        public List<OBMSPermissionDto> GetPermissionsWithScreens(string categoryName, string userName)
        {
            var query = from screen in _dbContext.ScreenLists
                        where screen.Category == categoryName
                        join permission in _dbContext.OBMSPermissions
                            .Where(p => p.Name == userName)
                            on screen.ScreenName equals permission.ScreenName into permGroup
                        from permission in permGroup.DefaultIfEmpty()
                        select new OBMSPermissionDto
                        {
                            ID = permission != null ? permission.ID : 0,
                            Name = permission != null ? permission.Name : userName,
                            ScreenName = screen.ScreenName,

                            Create = permission != null ? (permission.Create ?? false) : false,
                            Read = permission != null ? (permission.Read ?? false) : false,
                            Update = permission != null ? (permission.Update ?? false) : false,
                            Delete = permission != null ? (permission.Delete ?? false) : false,

                            LastUpdatedBy = permission != null ? permission.LastUpdatedBy : null,
                            LastUpdate = screen.LastUpdate,
                            Category = screen.Category
                        };

            return query.ToList();
        }


        public OBMSPermissions GetUserAccessRights(string userName, string screenName)
        {
            if(userName != null)
            {
                var result = _dbContext.OBMSPermissions.Where(x => x.Name == userName && x.ScreenName == screenName).SingleOrDefault();
                return result ?? new OBMSPermissions();
            }
            return new OBMSPermissions();

        }

        public List<OBMSBranches> GetObmsBranchesPermission()
        {
            try
            {
                return _dbContext.OBMSBranches
                    .ToList();

            }
            catch
            {
                throw;
            }
        }
        public List<OBMSBranches> GetObmsBranchesPermissionByUser(string userName)
        {
            try
            {
                return _dbContext.OBMSBranches
                    .Where(u => u.Name == userName).ToList();

            }
            catch
            {
                throw;
            }
        }

        #endregion
    }
}
