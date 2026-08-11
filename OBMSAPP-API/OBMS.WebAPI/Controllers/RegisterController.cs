using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using SkiaSharp;
using Syncfusion.XlsIO.Implementation.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OBMS.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : Controller
    {
        HttpResponseMessage response = new HttpResponseMessage();
        Dictionary<string, object> dictResult = new Dictionary<string, object>();
        private readonly IRegisterRepository _registerRepository;
        private readonly OBMSDbContext _dbContext;

        public RegisterController(IRegisterRepository registerRepository, OBMSDbContext dbContext)
        {
            _dbContext = dbContext;
            _registerRepository = registerRepository;
        }

        // POST: api/Register
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("UserRegistration")]
        public async Task<ActionResult<Obmsuser>> UserRegistration(CreateRegisterRequestDto requestDto)
        {
            try
            {
                //Map DTO to Domain Model
                var userRegister = new Obmsuser
                {
                    UserId = requestDto.UserId,
                    Name = requestDto.Name,
                    Email = requestDto.Email,
                    ContactNo = requestDto.ContactNo,
                    CreatedBy = requestDto.CreatedBy,
                    CreatedDate = requestDto.CreatedDate,
                    Description = requestDto.Description,
                    Designation = requestDto.Designation,
                    IsAdmin = requestDto.IsAdmin,
                    IsDeleted = requestDto.IsDeleted,
                    IsView = requestDto.IsView,
                    LastUpdatedBy = requestDto?.LastUpdatedBy,
                    LastUpdatedDate = DateTime.Now,
                    Password = requestDto.Password
                };
                await _registerRepository.RegisterAsync(userRegister);

                var responseUser = new RegisterResponseDto
                {
                    UserId = userRegister.UserId,
                    Name = userRegister.Name,

                };
                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("User", responseUser);
                dictResult.Add("Message", "Successfully Registerd");
                //response.Headers.Add("Success", "Successfully Registerd.");

                return Ok(dictResult);


            }
            catch (DbUpdateException)
            {

            }

            return Ok(response);
        }

        [HttpPost]
        [Route("LoginUser")]
        public async Task<ActionResult<HttpResponseMessage>> LoginUser(LoginRequestDto loginUser)
        {
            try
            {
                var obmsUser = new Obmsuser
                {
                    Name = loginUser.userName,
                    Password = loginUser.password

                };

                var userAvailable = await _registerRepository.LoginUser(obmsUser);

                if (userAvailable != null)
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes("obms-authentication");
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new[] {
                                  new Claim(ClaimTypes.Name, loginUser.userName)
                        }),
                        Expires = DateTime.UtcNow.AddMinutes(60),  // Token expires in 15 mins
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var tokenString = tokenHandler.WriteToken(token);

                    dictResult.Add("Success", "Success");
                    dictResult.Add("Users", userAvailable);
                    dictResult.Add("Message", "Successfully login.");
                    dictResult.Add("token", tokenString);
                    return Ok(dictResult);
                }
                else
                {
                    dictResult.Add("Failure", "Failure");
                    dictResult.Add("Users", obmsUser);
                    dictResult.Add("Message", "Login failed please check your login details....");
                    return Ok(dictResult);
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        [HttpPost("changepassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto changePasswordRequest)
        {
            if (string.IsNullOrEmpty(changePasswordRequest.CurrentPassword) || string.IsNullOrEmpty(changePasswordRequest.NewPassword))
            {
                return BadRequest("Invalid request");
            }

            var result = await _registerRepository.ChangePasswordAsync(changePasswordRequest);

            if (result)
            {
                dictResult.Add("Success", "Success");
                dictResult.Add("Message", "Password changed successfully");
                return Ok(dictResult);
            }
            else
            {
                dictResult.Add("Fail", "Fail");
                dictResult.Add("Message", "Failed to change password, the current password is not matched...");
                return Ok(dictResult);
            }            
        }

        [HttpGet]
        [Route("GetObmsusers")]
        public IActionResult GetObmsusers()
        {
            var obmsusers = _registerRepository.GetObmsusers();
            return Ok(obmsusers);
        }

        [HttpGet]
        [Route("GetObmsuserById")]
        public IActionResult GetObmsuserById(int userId)
        {
            var obmsuser = _registerRepository.GetObmsuserById(userId);
            return Ok(obmsuser);
        }

        [HttpGet]
        [Route("GetObmsuserByName")]
        public IActionResult GetObmsuserByName(string userName)
        {
            var obmsuser = _registerRepository.GetObmsuserByName(userName);
            return Ok(obmsuser);
        }

        [HttpPost]
        [Route("DeleteObmsuser")]
        public async Task<ActionResult<HttpResponseMessage>> DeleteObmsuser(int userId)
        {
            try
            {
                await _registerRepository.DeleteObmsuser(userId);
                response.Headers.Add("Success", "Successfully deleted user detals");
            }
            catch (Exception ex)
            {

                throw;
            }

            return response;
        }

        [HttpGet]
        [Route("GetBankList")]
        public IActionResult GetBankList()
        {
            var bankLists = _registerRepository.GetBankList();
            return Ok(bankLists);
        }

        [HttpGet]
        [Route("GetUserBankList")]
        public async Task<ActionResult<List<BankList>>> GetUserBankList(string user)
        {
            try
            {
                var bankList = await _registerRepository.GetUserBankList(user);
                if (bankList == null || bankList.Count == 0)
                {
                    return NotFound($"No banks found with the name '{user}'.");
                }
                return Ok(bankList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, message = "An error occurred while processing your request." });
            }

        }

        [HttpGet]
        [Route("GetBankListById")]
        public IActionResult GetBankListById(int Id)
        {
            var bankList = _registerRepository.GetBankListById(Id);
            return Ok(bankList);
        }

        [HttpGet]
        [Route("GetBankListByUserId")]
        public IActionResult GetBankListByUsreId(string name)
        {

            var result = from bm in _dbContext.BankMasters
                         join bl in _dbContext.BankLists on bm.BankCode equals bl.BankCode
                         join ob in _dbContext.OBMSBanks on bm.BankId equals ob.BankID
                         where ob.Name == name
                         select new
                         {
                             bm.BankId,
                             bm.BankCode,
                             bm.Accname,
                             bm.Accno,
                             bm.AccShortName,
                             bm.PREFIX,
                             bm.LASTUPDATE
                         };

            return Ok(result);
        }

        [HttpPost]
        [Route("DeleteBank")]
        public async Task<ActionResult<HttpResponseMessage>> DeleteBank(int Id)
        {
            try
            {
                await _registerRepository.DeleteBank(Id);
                response.Headers.Add("Success", "Successfully deleted bank detals");
            }
            catch (Exception ex)
            {

                throw;
            }

            return response;
        }

        [HttpPost]
        [Route("saveAndUpdateBankDetails")]
        public async Task<ActionResult<BankList>> saveAndUpdateBankDetails(BankList requestDto)
        {
            try
            {
                //Map DTO to Domain Model
                var bankRegister = new BankList
                {
                    ID = requestDto.ID,
                    BankCode = requestDto.BankCode,
                    BankName = requestDto.BankName,
                    LastUpdatedBy = requestDto.LastUpdatedBy,
                    LASTUPDATE = requestDto.LASTUPDATE,
                };
                await _registerRepository.saveAndUpdateBankDetails(bankRegister);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("User", bankRegister);
                dictResult.Add("Message", "Successfully Save and Update bank details");

                return Ok(dictResult);


            }
            catch (DbUpdateException)
            {

            }

            return Ok(response);
        }


        [HttpGet]
        [Route("GetBankMasterList")]
        public IActionResult GetBankMasterList()
        {
            var bankLists = _registerRepository.GetBankMasterList();
            return Ok(bankLists);
        }

        [HttpGet]
        [Route("GetUserBankMaster")]
        public async Task<ActionResult<List<BankMasterDto>>> GetUserBankMaster(string user)
        {
            try
            {
                var bankMaster = await _registerRepository.GetUserBankMaster(user);
                if (bankMaster == null || bankMaster.Count == 0)
                {
                    return NotFound($"No banks found with the name '{user}'.");
                }
                return Ok(bankMaster);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, message = "An error occurred while processing your request." });
            }

        }

        [HttpGet]
        [Route("GetBankMasterById")]
        public IActionResult GetBankMasterById(int Id)
        {
            var bankList = _registerRepository.GetBankMasterById(Id);
            return Ok(bankList);
        }
        [HttpPost]
        [Route("DeleteBankMaster")]
        public async Task<ActionResult<HttpResponseMessage>> DeleteBankMaster(int Id)
        {
            try
            {
                await _registerRepository.DeleteBankMaster(Id);
                response.Headers.Add("Success", "Successfully deleted bank master detals");
            }
            catch (Exception ex)
            {

                throw;
            }

            return response;
        }

        [HttpPost]
        [Route("saveAndUpdateBankMasterDetails")]
        public async Task<ActionResult<BankMasterDto>> saveAndUpdateBankMasterDetails(BankMasterDto bankMasterDto)
        {
            try
            {
                //Map DTO to Domain Model
                var bankRegister = new BankMaster
                {
                    BankId = bankMasterDto.BankId,
                    BankCode = bankMasterDto.BankCode,
                    Accname = bankMasterDto.Accname,
                    Accno = bankMasterDto.Accno,
                    PREFIX = bankMasterDto.PREFIX,
                    AccShortName = bankMasterDto.AccShortName,
                    LastUpdatedBy = bankMasterDto.LastUpdatedBy,
                    LASTUPDATE = bankMasterDto.LASTUPDATE,
                };
                await _registerRepository.saveAndUpdateBankMasterDetails(bankRegister);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("User", bankRegister);
                dictResult.Add("Message", "Successfully Save and Update bank master details");

                return Ok(dictResult);


            }
            catch (DbUpdateException)
            {

            }

            return Ok(response);
        }

        [HttpGet]
        [Route("GetBankPrefixNoByBankID")]
        public ActionResult<string> GetBankPrefixNoByBankID(string BankCode)
        {
            try
            {
                string prefix = _registerRepository.GetBankPrefixNoByBankID(BankCode);
                if (prefix != null)
                {
                    Dictionary<string, object> dictResult = new Dictionary<string, object>();
                    dictResult.Add("Success", "Success");
                    dictResult.Add("Prefix", prefix);
                    return Ok(dictResult);
                }
                return Ok(prefix);
            }
            catch (Exception ex)
            {
                throw;
            }


        }

        [HttpGet]
        [Route("GetChequeList")]
        public IActionResult GetChequeList()
        {
            var chequeLists = _registerRepository.GetChequeList();
            return Ok(chequeLists);
        }
        [HttpGet]
        [Route("GetUserChequeList")]
        public async Task<ActionResult<List<ChequeMasterDto>>> GetUserChequeList(string user)
        {
            try
            {
                var chequeMaster = await _registerRepository.GetUserChequeList(user);
                if (chequeMaster == null || chequeMaster.Count == 0)
                {
                    return NotFound($"No cheques found with the name '{user}'.");
                }
                return Ok(chequeMaster);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, message = "An error occurred while processing your request." });
            }

        }

        [HttpGet]
        [Route("GetChequeListById")]
        public IActionResult GetChequeListById(int Id)
        {
            var chequeLists = _registerRepository.GetChequeListById(Id);
            return Ok(chequeLists);
        }
        [HttpPost]
        [Route("DeleteChequeMaster")]
        public async Task<ActionResult<HttpResponseMessage>> DeleteChequeMaster(int Id)
        {
            try
            {
                await _registerRepository.DeleteChequeMaster(Id);
                response.Headers.Add("Success", "Successfully deleted cheque detals");
            }
            catch (Exception ex)
            {

                throw;
            }

            return response;
        }

        [HttpPost]
        [Route("saveAndUpdateChequeMasterDetails")]
        public async Task<ActionResult<ChequeMasterDto>> saveAndUpdateChequeMasterDetails(ChequeMasterDto chequeMasterDto)
        {
            try
            {
                //Map DTO to Domain Model
                var chequeRegister = new ChequeMaster
                {
                    ID = chequeMasterDto.ID,
                    BankID = chequeMasterDto.BankID,
                    ChequeStart = chequeMasterDto.ChequeStart,
                    ChequeEnd = chequeMasterDto.ChequeEnd,
                    IsActive = chequeMasterDto.IsActive,
                    LastUpdatedBy = chequeMasterDto.LastUpdatedBy,
                    LastUpdate = chequeMasterDto.LastUpdate,
                };
                await _registerRepository.saveAndUpdateChequeMasterDetails(chequeRegister);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("Cheque", chequeRegister);
                dictResult.Add("Message", "Successfully Save and Update cheque master details");

                return Ok(dictResult);


            }
            catch (DbUpdateException)
            {

            }

            return Ok(response);
        }

        [HttpGet]
        [Route("GetUsers")]
        public ActionResult<List<Obmsuser>> GetUsers(string currentUser)
        {
            try
            {
                var users = _registerRepository.GetUsers(currentUser);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetScreensByCategory")]
        public async Task<ActionResult<List<ScreenListDto>>> GetScreensByCategory(string categoryName)
        {
            var screens = await _registerRepository.GetScreensByCategory(categoryName);
            return Ok(screens);
        }
        [HttpGet]
        [Route("GetPermissionsWithScreens")]
        public ActionResult<List<OBMSPermissionDto>> GetPermissionsWithScreens(string categoryName, string userName)
        {
            var result = _registerRepository.GetPermissionsWithScreens(categoryName, userName);
            return Ok(result);
        }

        [HttpPost]
        [Route("SaveAndUpdateObmsPermissions")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateObmsPermissions(List<OBMSPermissionDto> oBMSPermissionDtos)
        {
            try
            {
                List<OBMSPermissions> updatedRecords = new List<OBMSPermissions>();

                foreach (var obmspermissionRequest in oBMSPermissionDtos)
                {
                    var obmsDetails = new OBMSPermissions()
                    {
                        ID = obmspermissionRequest.ID,
                        Name = obmspermissionRequest.Name,
                        ScreenName = obmspermissionRequest.ScreenName,
                        Read = obmspermissionRequest.Read,
                        Create = obmspermissionRequest.Create,
                        Update = obmspermissionRequest.Update,
                        Delete = obmspermissionRequest.Delete,
                        LastUpdatedBy = obmspermissionRequest.LastUpdatedBy,
                    };

                    // Add the current salaryAdvanceDetails to the updatedRecords list
                    updatedRecords.Add(obmsDetails);
                }

                // Pass the updatedRecords list to the repository method
                await _registerRepository.SaveAndUpdateObmsPermissions(updatedRecords);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("ObmsPermissions", updatedRecords);

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpGet]
        [Route("GetObmsBranchesPermission")]
        public ActionResult<List<Obmsuser>> GetObmsBranchesPermission()
        {
            try
            {
                var obmsBranches = _registerRepository.GetObmsBranchesPermission();
                return Ok(obmsBranches);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetObmsBranchesPermissionByUser")]
        public ActionResult<List<OBMSBranches>> GetObmsBranchesPermissionByUser(string userName)
        {
            try
            {
                var obmsBranches = _registerRepository.GetObmsBranchesPermissionByUser(userName);
                return Ok(obmsBranches);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost]
        [Route("SaveAndUpdateObmsBranches")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateObmsBranches(List<OBMSBranchesDto> oBMSBranchesDtos)
        {
            try
            {
                List<OBMSBranches> updatedRecords = new List<OBMSBranches>();

                foreach (var oBMSBranchesRequest in oBMSBranchesDtos)
                {
                    var obmsDetails = new OBMSBranches()
                    {
                        ID = oBMSBranchesRequest.ID,
                        Name = oBMSBranchesRequest.Name,
                        BranchCode = oBMSBranchesRequest.BranchCode,
                        IsAllowed = oBMSBranchesRequest.IsAllowed,
                        LastUpdatedDate = DateTime.Now,
                        LastUpdatedBy = oBMSBranchesRequest.LastUpdatedBy,
                    };

                    // Add the current salaryAdvanceDetails to the updatedRecords list
                    updatedRecords.Add(obmsDetails);
                }

                // Pass the updatedRecords list to the repository method
                await _registerRepository.SaveAndUpdateObmsBranches(updatedRecords);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("ObmsBranches", updatedRecords);

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpGet]
        [Route("GetObmsBanksPermission")]
        public ActionResult<List<ObmsuserDto>> GetObmsBanksPermission(string userName)
        {
            try
            {
                var obmsBanks = _registerRepository.GetObmsBanksPermission(userName);
                return Ok(obmsBanks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetObmsBankMasterPwrmission")]
        public ActionResult<List<ObmsuserDto>> GetObmsBankMasterPwrmission(string userName)
        {
            try
            {
                var obmsBanks = _registerRepository.GetObmsBankMasterPwrmission(userName);
                return Ok(obmsBanks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost]
        [Route("SaveAndUpdateObmsBanks")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateObmsBanks(List<OBMSBanksDto> oBMSBanksDtos)
        {
            try
            {
                List<OBMSBanks> updatedRecords = new List<OBMSBanks>();

                foreach (var oBMSBanksRequest in oBMSBanksDtos)
                {
                    var obmsBanksDetails = new OBMSBanks()
                    {
                        ID = oBMSBanksRequest.ID,
                        Name = oBMSBanksRequest.Name,
                        BankID = oBMSBanksRequest.BankID,
                        IsAllowed = oBMSBanksRequest.IsAllowed,
                        LastUpdatedDate = DateTime.Now,
                        LastUpdatedBy = oBMSBanksRequest.LastUpdatedBy,
                    };

                    // Add the current salaryAdvanceDetails to the updatedRecords list
                    updatedRecords.Add(obmsBanksDetails);
                }

                // Pass the updatedRecords list to the repository method
                await _registerRepository.SaveAndUpdateObmsBanks(updatedRecords);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("ObmsBanks", updatedRecords);

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpGet("GetBankShortName/{bankID}")]
        public async Task<IActionResult> GetBankShortName(decimal bankID)
        {
            var shortName = await _registerRepository.GetBankAccShortNameByBankIDAsync(bankID);
            return Ok(shortName);
        }

        #region UserAccessRights
        [HttpGet]
        [Route("GetUserAccessRights")]
        public ActionResult<OBMSPermissionDto> GetUserAccessRights(string userName, string screenName)
        {
            try
            {
                var results = _registerRepository.GetUserAccessRights(userName, screenName);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }


        }
        #endregion
    }
}
