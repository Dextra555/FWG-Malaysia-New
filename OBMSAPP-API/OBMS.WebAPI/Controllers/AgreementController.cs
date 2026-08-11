using BoldReports.Processing.ObjectModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.BusinessObjects;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Implementation;
using OBMS.WebAPI.Repositories.Interface;
using Syncfusion.XlsIO.Implementation.Security;
using System.Data.SqlClient;

namespace OBMS.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgreementController : Controller
    {
        HttpResponseMessage response = new HttpResponseMessage();
        private readonly OBMSDbContext _oBMSDbContext;
        private readonly IAgreementRepository _agreementRepository;


        public AgreementController(IAgreementRepository agreementRepository, OBMSDbContext oBMSDbContext)
        {
            _agreementRepository = agreementRepository;
            _oBMSDbContext = oBMSDbContext;
        }

        [HttpGet]
        [Route("GetAgreementMaster")]
        public async Task<ActionResult<Object>> GetAgreementMasterList(string userID)
        {
            try
            {
                var objList = await _agreementRepository.GetAgreementMasterList(userID);
                return Ok(objList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetClientsAgreementByBranchID")]
        public async Task<ActionResult<object>> GetClientsByBranchID(string branchID, string userID)
        {
            try
            {
                bool isSuperAdmin = userID.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

                var agreements = await _agreementRepository.GetAgreements(branchID, userID);

                var clients = await (
                    from c in _oBMSDbContext.ClientMasters
                    join ob in _oBMSDbContext.OBMSBranches
                        on c.Branch equals ob.BranchCode
                    where
                        c.Status == "Active"
                        && c.Branch == branchID
                        && (isSuperAdmin || ob.Name == userID)                   
                    select c
                )
                //.GroupBy(c => c.ID)   // ✅ USE ACTUAL CLIENT PK
                //.Select(g => g.First())
                .Distinct()              // removes duplicates from join
                .OrderBy(c => c.Name)
                .ToListAsync();

                var result = new Dictionary<string, object>
                {
                    { "agreements", agreements },
                    { "clients", clients }
                };

                return Ok(result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("GetClientsOnlyByBranchID")]
        public async Task<ActionResult<Object>> GetClientsOnlyByBranchID(string branchID)
        {



            try
            {
                var objList = _oBMSDbContext.ClientMasters.Where(x => x.Branch == branchID && (x.Status == "Active" || x.Status == "A"))
                    .OrderBy(x => x.Name)
                    .ToList();
                return Ok(objList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetClientsAllStatusByBranchID")]
        public async Task<ActionResult<Object>> GetClientsAllStatusByBranchID(string branchID)
        {
            try
            {
                var objList = _oBMSDbContext.ClientMasters.Where(x => x.Branch == branchID && (x.Status == "Active" || x.Status == "A")).OrderBy(x => x.Name).ToList();
                return Ok(objList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpPost]
        [Route("SaveAndUpdateAgreement")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateAgreement(AgreementRequestDto agreementRequestDto)
        {
            try
            {
                var agreement = new OBMS.WebAPI.Models.Domain.Agreement();
                if (agreementRequestDto.ID != 0)
                {
                    agreement = _oBMSDbContext.Agreements.Where(x => x.ID == agreementRequestDto.ID).FirstOrDefault();
                }
                agreement.AgreementDate = agreementRequestDto.AgreementDate;
                agreement.Branch = agreementRequestDto.Branch;
                agreement.Client = agreementRequestDto.Client;
                agreement.WorkPlace = agreementRequestDto.WorkPlace;
                agreement.IsValid = true;
                agreement.Note = agreementRequestDto.Note;
                agreement.LastUpdatedBy = agreementRequestDto.LastUpdatedBy;
                agreement.LASTUPDATE = DateTime.Now;
                agreement.AgreementEndDate = DateTime.Now;

                await _agreementRepository.saveAndUpdateAgreement(agreement);

                if (agreementRequestDto.details != null)
                {
                    foreach (AgreementDetailsRequestDto detail in agreementRequestDto.details)
                    {
                        var agreementDetail = new AgreementDetails();
                        if (detail.ID != 0)
                        {
                            agreementDetail = _oBMSDbContext.AgreementDetails.Where(x => x.ID == detail.ID).FirstOrDefault();
                        }

                        agreementDetail.ID = detail.ID;
                        agreementDetail.AgreementID = agreement.ID;
                        agreementDetail.AgreementDate = agreement.AgreementDate;
                        agreementDetail.Client = agreement.Client;
                        agreementDetail.Branch = agreement.Branch;
                        agreementDetail.Description = detail.Description;
                        agreementDetail.NoOfGuards = detail.NoOfGuards;
                        agreementDetail.Rate = detail.Rate;
                        agreementDetail.NoOfHours = detail.NoOfHours;
                        agreementDetail.NoOfDays = detail.NoOfDays;
                        agreementDetail.FollowCalender = detail.FollowCalender;
                        agreementDetail.MonthTotal = detail.MonthTotal;
                        agreementDetail.HasDiscount = detail.HasDiscount;
                        agreementDetail.DiscountAmount = detail.DiscountAmount;
                        agreementDetail.DiscountHour = detail.DiscountHour;
                        agreementDetail.IsTaxable = detail.IsTaxable;
                        agreementDetail.TaxAmount = detail.TaxAmount;
                        agreementDetail.Category = detail.Category;
                        agreementDetail.Reason = detail.Reason;
                        agreementDetail.LastUpdatedBy = detail.LastUpdatedBy;
                        agreementDetail.LASTUPDATE = DateTime.Now;

                        await _agreementRepository.saveAndUpdateAgreementDetails(agreementDetail);
                    }
                }

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("agreement", agreement);
                response.Headers.Add("Success", "Successfully save & update Agreement details.");

                return Ok(dictResult);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("DeleteAgreementDetailById")]
        public async Task<ActionResult<HttpResponseMessage>> DeleteAgreementDetailById(int Id)
        {
            try
            {
                var data = _oBMSDbContext.AgreementDetails.Where(x => x.ID == Id).FirstOrDefault();


                if (data != null)
                {
                    _oBMSDbContext.AgreementDetails.Remove(data);
                    await _oBMSDbContext.SaveChangesAsync();
                }

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");


                return Ok(dictResult);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("DeleteAgreementById")]
        public async Task<IActionResult> DeleteAgreementById(int Id,string currentUser)
        {
            try
            {
                var agreement = await _oBMSDbContext.Agreements
                    .FirstOrDefaultAsync(x => x.ID == Id);

                if (agreement != null)
                {
                    agreement.IsValid = false;
                    agreement.LASTUPDATE = DateTime.Now;
                    agreement.LastUpdatedBy = currentUser;
                }

                // 2️⃣ Update ClientInvoice (Soft Delete)
                var invoices = await _oBMSDbContext.ClientInvoices
                    .Where(x => x.AgreementID == Id)
                    .ToListAsync();

                foreach (var invoice in invoices)
                {
                    invoice.IsDeleted = "Y";
                    invoice.LASTUPDATE = DateTime.Now;
                    invoice.LastUpdatedBy = currentUser;
                }

                await _oBMSDbContext.SaveChangesAsync();

                return Ok(new { Success = "Success" });
            }
            catch (Exception)
            {
                throw;
            }
        }


        [HttpGet]
        [Route("GetAgreementByID")]
        public async Task<ActionResult<Object>> GetAgreementByID(int agreementID)
        {
            try
            {
                var objList = _agreementRepository.GetAgreementByID(agreementID);
                return Ok(objList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetAgreements")]
        public async Task<ActionResult<Object>> GetAgreements(string userID, string branchId, bool load)
        {
            try
            {
                Dictionary<string, object> dictResult = new Dictionary<string, object>();

                var objList = _agreementRepository.GetAgreements(branchId, userID);

                dictResult.Add("agreements", objList);

                if (load)
                {
                    var branchs = await _oBMSDbContext.BranchMasters
                .Join(_oBMSDbContext.OBMSBranches,
                    branchmaster => branchmaster.Code,
                    branches => branches.BranchCode,
                    (branchmaster, branches) => new { branchmaster, branches })
                .Where(joinResult => joinResult.branches.Name == userID && joinResult.branches.IsAllowed == true)
                .Select(joinResult => new BranchMaster
                {
                    ID = joinResult.branchmaster.ID,
                    Code = joinResult.branchmaster.Code,
                    Name = joinResult.branchmaster.Name,
                    Address1 = joinResult.branchmaster.Address1,
                    Address2 = joinResult.branchmaster.Address2,
                    PostCode = joinResult.branchmaster.PostCode,
                    City = joinResult.branchmaster.City,
                    State = joinResult.branchmaster.State,
                    Phone = joinResult.branchmaster.Phone,
                    Fax = joinResult.branchmaster.Fax,
                    BankName = joinResult.branchmaster.BankName,
                    BankBranch = joinResult.branchmaster.BankBranch,
                    BankAccount = joinResult.branchmaster.BankAccount,
                    PersonIncharge = joinResult.branchmaster.PersonIncharge,
                    Email = joinResult.branchmaster.Email,
                    Description = joinResult.branchmaster.Description,
                    ShortName = joinResult.branchmaster.ShortName,
                    IsHeadQuarters = joinResult.branchmaster.IsHeadQuarters,
                    UbsCode = joinResult.branchmaster.UbsCode,
                    LastUpdate = joinResult.branchmaster.LastUpdate,
                    LastUpdatedBy = joinResult.branchmaster.LastUpdatedBy,
                    ParentBranch = joinResult.branchmaster.ParentBranch
                })
                .ToListAsync();

                    dictResult.Add("branches", branchs);
                }


                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("CheckClientStatus")]
        public async Task<ActionResult<Object>> CheckClientStatus(string branchId, string clientId, string status)
        {
            try
            {

                var dictResult = _agreementRepository.CheckClientStatus(branchId, clientId, status);


                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetFinalInvoiceDate")]
        public async Task<ActionResult<Object>> GetFinalInvoiceDate(int agreementId)
        {
            try
            {

                var dictResult = _agreementRepository.GetFinalInvoiceDate(agreementId);


                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpGet]
        [Route("GetAgreementsDiscountReportMaster")]
        public async Task<ActionResult<Object>> GetAgreementsDiscountReportMaster(string userID)
        {
            try
            {
                var branches = await _oBMSDbContext.BranchMasters
                .Join(_oBMSDbContext.OBMSBranches,
                    branchmaster => branchmaster.Code,
                    branches => branches.BranchCode,
                    (branchmaster, branches) => new { branchmaster, branches })
                .Where(joinResult => joinResult.branches.Name == userID && joinResult.branches.IsAllowed == true)
                .Select(joinResult => new BranchMaster
                {
                    ID = joinResult.branchmaster.ID,
                    Code = joinResult.branchmaster.Code,
                    Name = joinResult.branchmaster.Name,
                    Address1 = joinResult.branchmaster.Address1,
                    Address2 = joinResult.branchmaster.Address2,
                    PostCode = joinResult.branchmaster.PostCode,
                    City = joinResult.branchmaster.City,
                    State = joinResult.branchmaster.State,
                    Phone = joinResult.branchmaster.Phone,
                    Fax = joinResult.branchmaster.Fax,
                    BankName = joinResult.branchmaster.BankName,
                    BankBranch = joinResult.branchmaster.BankBranch,
                    BankAccount = joinResult.branchmaster.BankAccount,
                    PersonIncharge = joinResult.branchmaster.PersonIncharge,
                    Email = joinResult.branchmaster.Email,
                    Description = joinResult.branchmaster.Description,
                    ShortName = joinResult.branchmaster.ShortName,
                    IsHeadQuarters = joinResult.branchmaster.IsHeadQuarters,
                    UbsCode = joinResult.branchmaster.UbsCode,
                    LastUpdate = joinResult.branchmaster.LastUpdate,
                    LastUpdatedBy = joinResult.branchmaster.LastUpdatedBy,
                    ParentBranch = joinResult.branchmaster.ParentBranch
                })
                .ToListAsync();

                return Ok(branches);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetAgreementsDiscountReport")]
        public async Task<ActionResult<Object>> GetAgreementsDiscountReport(string branchId, string clientId, DateTime startDate, DateTime endDate)
        {
            try
            {

                Dictionary<string, object> dictResult = new Dictionary<string, object>();

                var objList = _agreementRepository.GetAgreementsDiscountReport(branchId, clientId, startDate, endDate);

                dictResult.Add("agreements", objList);



                dictResult.Add("branches", _oBMSDbContext.BranchMasters.ToList());



                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetAgreementTerminationList")]
        public async Task<ActionResult<Object>> GeAgreementTerminationList(string branchId,string userID)
        {

            var results = new Dictionary<string, Object>();


            var branchs = await _oBMSDbContext.BranchMasters
                .Join(_oBMSDbContext.OBMSBranches,
                    branchmaster => branchmaster.Code,
                    branches => branches.BranchCode,
                    (branchmaster, branches) => new { branchmaster, branches })
                .Where(joinResult => joinResult.branches.Name == userID && joinResult.branches.IsAllowed == true)
                .Select(joinResult => new BranchMaster
                {
                    ID = joinResult.branchmaster.ID,
                    Code = joinResult.branchmaster.Code,
                    Name = joinResult.branchmaster.Name,
                    Address1 = joinResult.branchmaster.Address1,
                    Address2 = joinResult.branchmaster.Address2,
                    PostCode = joinResult.branchmaster.PostCode,
                    City = joinResult.branchmaster.City,
                    State = joinResult.branchmaster.State,
                    Phone = joinResult.branchmaster.Phone,
                    Fax = joinResult.branchmaster.Fax,
                    BankName = joinResult.branchmaster.BankName,
                    BankBranch = joinResult.branchmaster.BankBranch,
                    BankAccount = joinResult.branchmaster.BankAccount,
                    PersonIncharge = joinResult.branchmaster.PersonIncharge,
                    Email = joinResult.branchmaster.Email,
                    Description = joinResult.branchmaster.Description,
                    ShortName = joinResult.branchmaster.ShortName,
                    IsHeadQuarters = joinResult.branchmaster.IsHeadQuarters,
                    UbsCode = joinResult.branchmaster.UbsCode,
                    LastUpdate = joinResult.branchmaster.LastUpdate,
                    LastUpdatedBy = joinResult.branchmaster.LastUpdatedBy,
                    ParentBranch = joinResult.branchmaster.ParentBranch
                })
                .ToListAsync();



            results.Add("branches", branchs);



            if (branchId == "0")
            {


                var query = from terminatedAgreement in _oBMSDbContext.TerminatedAgreements
                            join clientMaster in _oBMSDbContext.ClientMasters
                            on new { terminatedAgreement.Client, terminatedAgreement.Branch } equals new { Client = clientMaster.Code, clientMaster.Branch }
                            select new
                            {
                                terminatedAgreement.ID,
                                terminatedAgreement.Branch,
                                terminatedAgreement.Client,
                                ClientName = clientMaster.Name,
                                terminatedAgreement.Reason,
                                terminatedAgreement.TerminationDate,
                                terminatedAgreement.Note,
                                terminatedAgreement.LASTUPDATE
                            };

                results.Add("agreementTermination", query);
            }
            else
            {
                var query = from terminatedAgreement in _oBMSDbContext.TerminatedAgreements
                            join clientMaster in _oBMSDbContext.ClientMasters
                            on new { terminatedAgreement.Client, terminatedAgreement.Branch } equals new { Client = clientMaster.Code, clientMaster.Branch }
                            where terminatedAgreement.Branch == branchId
                            select new
                            {
                                terminatedAgreement.ID,
                                terminatedAgreement.Branch,
                                terminatedAgreement.Client,
                                ClientName = clientMaster.Name,
                                terminatedAgreement.Reason,
                                terminatedAgreement.TerminationDate,
                                terminatedAgreement.Note,
                                terminatedAgreement.LASTUPDATE
                            };

                results.Add("agreementTermination", query);
            }

            //return new List<object>(query);
            //throw new NotImplementedException();

            return Ok(results); ;
        }

        [HttpGet]
        [Route("GetAgreementListByBranchId")]
        public async Task<ActionResult<Object>> GetAgreementListByBranchId(string branchId, string clientId, DateTime terminationDate)
        {
            try
            {

                Dictionary<string, object> dictResult = new Dictionary<string, object>();

                var objList = _agreementRepository.GetAgreementListByBranchId(branchId, clientId, terminationDate);

                //dictResult.Add("agreements", objList);

                return Ok(objList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost]
        [Route("SaveAndUpdateAgreementTermination")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateAgreementTermination(TerminatedAgreementRequestDto terminatedAgreementRequestDto)
        {
            try
            {
                var terminatedAgreement = new TerminatedAgreement();
                if (terminatedAgreementRequestDto.ID != 0)
                {
                    terminatedAgreement = _oBMSDbContext.TerminatedAgreements.Where(x => x.ID == terminatedAgreementRequestDto.ID).FirstOrDefault();
                }
                terminatedAgreement.ID = terminatedAgreementRequestDto.ID;
                terminatedAgreement.Branch = terminatedAgreementRequestDto.Branch;
                terminatedAgreement.Client = terminatedAgreementRequestDto.Client;
                terminatedAgreement.Reason = terminatedAgreementRequestDto.Reason;
                terminatedAgreement.TerminationDate = terminatedAgreementRequestDto.TerminationDate;
                terminatedAgreement.Note = terminatedAgreementRequestDto.Note;
                terminatedAgreement.LASTUPDATE = DateTime.Now;
                terminatedAgreement.LastUpdatedBy = terminatedAgreementRequestDto.LastUpdatedBy;



                await _agreementRepository.SaveAndUpdateAgreementTermination(terminatedAgreement);



                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", terminatedAgreement);
                response.Headers.Add("Success", "Successfully save & update Agreement Termination details.");

                return Ok(dictResult);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("GetAgreementTerminationByID")]
        public async Task<ActionResult<Object>> GetAgreementTerminationByID(int id)
        {
            try
            {
                var objList = _agreementRepository.GetAgreementTerminationByID(id);
                return Ok(objList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet("GetInvoiceDate/{agreementId}")]
        public IActionResult GetInvoiceDate(decimal agreementId)
        {
            try
            {
                var result = UtilityMain.GetInvoiceDate(agreementId);

                if (result == null)
                    return NotFound($"No final invoice date found for Agreement ID {agreementId}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                // You can also log the error here (e.g., using ILogger)
                return StatusCode(500, new { message = "An error occurred while retrieving the final invoice date.", error = ex.Message });
            }
        }

        [HttpGet("IsInvoiceAvailable/{agreementId}")]
        public IActionResult IsInvoiceAvailable(decimal agreementId)
        {
            try
            {
                // Call your static method (replace with your actual logic)
                bool result = UtilityMain.IsInvoiceAvailable(agreementId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log exception if needed
                return StatusCode(500, new { message = "Error checking invoice availability", details = ex.Message });
            }
        }        

    }
}
