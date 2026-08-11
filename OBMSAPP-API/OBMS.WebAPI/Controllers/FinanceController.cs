using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.BusinessObjects;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using SkiaSharp;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Web.Services.Description;


namespace OBMS.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinanceController : Controller
    {

        HttpResponseMessage response = new HttpResponseMessage();
        private readonly OBMSDbContext _oBMSDbContext;
        private readonly IFinanceRepository _financeRepository;

        public FinanceController(IFinanceRepository financeRepository, OBMSDbContext oBMSDbContext)
        {
            _financeRepository = financeRepository;
            _oBMSDbContext = oBMSDbContext;
        }

        [HttpGet]
        [Route("GetInvoiceMaster")]
        public async Task<ActionResult<Object>> GetInvoiceMaster(string userID)
        {
            try
            {
                var obj = await _financeRepository.GetInvoiceMaster(userID);

                return obj;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetInvoiceClientByBranchAndInvoiceDate")]
        public async Task<ActionResult<List<QueryResult>>> GetInvoiceClientByBranchAndInvoiceDate(string branchId, DateTime invoicePeriod)
        {
            try
            {
                // One row per active agreement per client per WorkPlace (site).
                // For clients with multiple sites: each site appears as a separate row.
                // For each client+WorkPlace combo, only the LATEST agreement <= invoicePeriod is shown.
                var sqlQuery = @"
                        SELECT  
                            cm.Code, 
                            cm.Branch, 
                            cm.Name, 
                            ISNULL(ci.ID, 0)          AS ID, 
                            ISNULL(ci.InvoiceNo, '')   AS InvoiceNo,
                            ISNULL(ci.Note, '')        AS Note,
                            ag.ID                      AS AgreementID,
                            ag.WorkPlace
                        FROM Agreement ag
                        JOIN ClientMaster cm
                            ON cm.Code    = ag.Client
                            AND cm.Branch = ag.Branch
                        LEFT JOIN ClientInvoice ci
                            ON ci.AgreementID  = ag.ID
                            AND ci.IsDeleted   = 'N'
                            AND MONTH(ci.InvoiceDate) = @InvoiceMonth
                            AND YEAR(ci.InvoiceDate)  = @InvoiceYear
                        WHERE
                            ag.Branch   = @Branch
                            AND ag.IsValid = 1
                            AND ag.AgreementDate <= @InvoicePeriod
                            -- Only the latest agreement per client+WorkPlace combo
                            AND ag.ID = (
                                SELECT TOP 1 ag2.ID
                                FROM Agreement ag2
                                WHERE ag2.Branch       = ag.Branch
                                  AND ag2.Client       = ag.Client
                                  AND ag2.WorkPlace    = ag.WorkPlace
                                  AND ag2.IsValid      = 1
                                  AND ag2.AgreementDate <= @InvoicePeriod
                                ORDER BY ag2.AgreementDate DESC, ag2.LASTUPDATE DESC
                            )
                            AND ag.Client NOT IN
                            (
                                SELECT ta.Client
                                FROM TerminatedAgreements ta
                                WHERE ta.Branch = @Branch
                                  AND ta.TerminationDate < DATEADD(month, DATEDIFF(month, 0, @InvoicePeriod), 0)
                            )
                        ORDER BY cm.Name, ag.WorkPlace";

                var parameters = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@Branch", branchId),
                    new Microsoft.Data.SqlClient.SqlParameter("@InvoiceMonth", invoicePeriod.Month),
                    new Microsoft.Data.SqlClient.SqlParameter("@InvoiceYear", invoicePeriod.Year),
                    new Microsoft.Data.SqlClient.SqlParameter("@InvoicePeriod", invoicePeriod),
                };

                var result = await _oBMSDbContext.QueryResults.FromSqlRaw(sqlQuery, parameters).ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }


        [HttpGet]
        [Route("GetBatchInvoiceClientByBranchAndInvoiceDate")]
        public async Task<ActionResult<Object>> GetBatchInvoiceClientByBranchAndInvoiceDate(string branchId, DateTime invoicePeriod)
        {
            try
            {
                // Same logic as GetInvoiceClientByBranchAndInvoiceDate:
                // One row per client+WorkPlace (latest agreement per site <= invoicePeriod)
                var sqlQuery = @"
                            SELECT
                                cm.Code,
                                cm.Branch,
                                cm.Name,
                                ISNULL(ci.ID, 0)          AS ID,
                                ISNULL(ci.InvoiceNo, '')   AS InvoiceNo,
                                ISNULL(ci.Note, '')        AS Note,
                                ag.ID                      AS AgreementID,
                                ag.WorkPlace
                            FROM Agreement ag
                            JOIN ClientMaster cm
                                ON cm.Code    = ag.Client
                                AND cm.Branch = ag.Branch
                            LEFT JOIN ClientInvoice ci
                                ON ci.AgreementID  = ag.ID
                                AND ci.IsDeleted   = 'N'
                                AND MONTH(ci.InvoiceDate) = @InvoiceMonth
                                AND YEAR(ci.InvoiceDate)  = @InvoiceYear
                            WHERE
                                ag.Branch   = @Branch
                                AND ag.IsValid = 1
                                AND ag.AgreementDate <= @InvoicePeriod
                                AND ag.ID = (
                                    SELECT TOP 1 ag2.ID
                                    FROM Agreement ag2
                                    WHERE ag2.Branch       = ag.Branch
                                      AND ag2.Client       = ag.Client
                                      AND ag2.WorkPlace    = ag.WorkPlace
                                      AND ag2.IsValid      = 1
                                      AND ag2.AgreementDate <= @InvoicePeriod
                                    ORDER BY ag2.AgreementDate DESC, ag2.LASTUPDATE DESC
                                )
                                AND ag.Client NOT IN
                                (
                                    SELECT ta.Client
                                    FROM TerminatedAgreements ta
                                    WHERE ta.Branch = @Branch
                                      AND ta.TerminationDate < DATEADD(month, DATEDIFF(month, 0, @InvoicePeriod), 0)
                                )
                            ORDER BY cm.Name, ag.WorkPlace";

                var parameters = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@Branch", branchId),
                    new Microsoft.Data.SqlClient.SqlParameter("@InvoiceMonth", invoicePeriod.Month),
                    new Microsoft.Data.SqlClient.SqlParameter("@InvoiceYear", invoicePeriod.Year),
                    new Microsoft.Data.SqlClient.SqlParameter("@InvoicePeriod", invoicePeriod),
                };

                var result = await _oBMSDbContext.QueryResults.FromSqlRaw(sqlQuery, parameters).ToListAsync();

                List<BatchInvoice> batchInvoices = new List<BatchInvoice>();
                foreach (var item in result)
                {
                    BatchInvoice bi = new BatchInvoice();
                    bi.InvoiceNo = item.InvoiceNo;
                    bi.Branch = item.Branch;
                    bi.Code = item.Code;
                    bi.ID = item.ID ?? 0;
                    bi.Name = item.Name;
                    bi.WorkPlace = item.WorkPlace;
                    bi.AgreementID = item.AgreementID;

                    if (item.ID == 0)
                    {
                        // Use agreementID directly if available, otherwise fall back to client lookup
                        if (item.AgreementID.HasValue && item.AgreementID > 0)
                        {
                            var ag = GetAgreementDetailsByAgreementId(item.AgreementID.Value, item.Branch);
                            bi.data = ag.Result.Value;
                        }
                        else
                        {
                            var ag = GetAgreementAndDetailsByBranchInvoicePeriodAndClient(item.Branch, invoicePeriod, item.Code);
                            bi.data = ag.Result.Value;
                        }
                    }
                    else
                    {
                        var ag = GetInvoiceAndDetailsByInvoiceId(item.ID ?? 0);
                        bi.data = ag.Result.Value;
                    }
                    batchInvoices.Add(bi);
                }

                var results = new Dictionary<string, Object>();
                results.Add("batchInvoice", batchInvoices);

                return results;
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        [Route("GetAgreementAndDetailsByBranchInvoicePeriodAndClient")]
        public async Task<ActionResult<Object>> GetAgreementAndDetailsByBranchInvoicePeriodAndClient(string branchId, DateTime invoicePeriod, string clientId)

        {

            // oAgreement 

            var sqlQuery = @"SELECT TOP 1 ID,Branch,Client,WorkPlace,AgreementDate,Note,IsValid,LASTUPDATE,AgreementEndDate,LastUpdatedBy FROM Agreement WHERE  Branch=@Branch AND Client=@Client AND AgreementDate <=@AgreementPeriod ORDER BY AgreementDate DESC,LASTUPDATE DESC ";


            var parameters = new[]
                {
    new Microsoft.Data.SqlClient.SqlParameter("@Branch", branchId),
    new Microsoft.Data.SqlClient.SqlParameter("@Client", clientId),
    new Microsoft.Data.SqlClient.SqlParameter("@AgreementPeriod", invoicePeriod),
};
            var result = await _oBMSDbContext.Agreements.FromSqlRaw(sqlQuery, parameters).FirstOrDefaultAsync();

            //AgreementDetails
            var results = new Dictionary<string, Object>();
            results.Add("agreement", result);


            if (result != null)
            {
                var ad = _oBMSDbContext.AgreementDetails.Where(x => x.AgreementID == result.ID).ToList();
                results.Add("agreementDetails", ad);
            }



            if (invoicePeriod >= Convert.ToDateTime("2015-04-01 00:00:00.000"))
            {
                var sqlQuery1 = @"SELECT ISNULL(MAX(CAST(INVOICENO AS INT))+1, 1) AS NEWCLIENTINVOICENO FROM CLIENTINVOICE WHERE BRANCH=@Branch AND InvoiceDate >= '2015-04-01T00:00:00.000'";

                var parameters1 = new[]
                {
    new Microsoft.Data.SqlClient.SqlParameter("@Branch", branchId)
};

                var result1 = await _oBMSDbContext.ClientInvoiceNoResult
                    .FromSqlRaw(sqlQuery1, parameters1)
                    .FirstOrDefaultAsync();


                results.Add("invoiceNo", result1.NEWCLIENTINVOICENO);
            }
            else
            {
                var sqlQuery1 = @" SELECT ISNULL(MAX(CAST(INVOICENO AS INT))+1,1) AS NEWCLIENTINVOICENO FROM CLIENTINVOICE WHERE BRANCH=@BranchCode AND InvoiceDate < '2015-04-01 00:00:00.000'";

                var parameters1 = new[]
                 {
                    new Microsoft.Data.SqlClient.SqlParameter("@Branch", branchId)
                 };

                var result1 = await _oBMSDbContext.ClientInvoiceNoResult
                   .FromSqlRaw(sqlQuery1, parameters1)
                   .FirstOrDefaultAsync();


                results.Add("invoiceNo", result1.NEWCLIENTINVOICENO);
            }

            return results;

        }

        [HttpGet]
        [Route("GetInvoiceAndDetailsByInvoiceId")]
        public async Task<ActionResult<Object>> GetInvoiceAndDetailsByInvoiceId(int invoiceId)
        {
            var results = new Dictionary<string, Object>();

            var obj1 = await _oBMSDbContext.ClientInvoices.Where(x => x.ID == invoiceId).FirstOrDefaultAsync();
            results.Add("invoice", obj1);

            var obj2 = await _oBMSDbContext.ClientInvoiceDetails.Where(x => x.ClientInvoiceID == invoiceId).ToListAsync();
            results.Add("details", obj2);

            return results;
        }

        /// <summary>
        /// Fetches agreement and its details by agreementID directly.
        /// Used when a client has multiple agreements in the same month (different sites/WorkPlace).
        /// </summary>
        [HttpGet]
        [Route("GetAgreementDetailsByAgreementId")]
        public async Task<ActionResult<Object>> GetAgreementDetailsByAgreementId(int agreementId, string branchId)
        {
            var results = new Dictionary<string, Object>();

            var agreement = await _oBMSDbContext.Agreements
                .Where(x => x.ID == agreementId)
                .FirstOrDefaultAsync();

            results.Add("agreement", agreement);

            if (agreement != null)
            {
                var details = _oBMSDbContext.AgreementDetails
                    .Where(x => x.AgreementID == agreementId)
                    .ToList();
                results.Add("agreementDetails", details);
            }

            // Invoice number (same logic as GetAgreementAndDetailsByBranchInvoicePeriodAndClient)
            var invoicePeriod = agreement?.AgreementDate ?? DateTime.Now;
            if (invoicePeriod >= Convert.ToDateTime("2015-04-01"))
            {
                var sqlQ = @"SELECT ISNULL(MAX(CAST(INVOICENO AS INT))+1, 1) AS NEWCLIENTINVOICENO FROM CLIENTINVOICE WHERE BRANCH=@Branch AND InvoiceDate >= '2015-04-01T00:00:00.000'";
                var result1 = await _oBMSDbContext.ClientInvoiceNoResult
                    .FromSqlRaw(sqlQ, new Microsoft.Data.SqlClient.SqlParameter("@Branch", branchId))
                    .FirstOrDefaultAsync();
                results.Add("invoiceNo", result1?.NEWCLIENTINVOICENO ?? 1);
            }
            else
            {
                var sqlQ = @"SELECT ISNULL(MAX(CAST(INVOICENO AS INT))+1,1) AS NEWCLIENTINVOICENO FROM CLIENTINVOICE WHERE BRANCH=@Branch AND InvoiceDate < '2015-04-01 00:00:00.000'";
                var result1 = await _oBMSDbContext.ClientInvoiceNoResult
                    .FromSqlRaw(sqlQ, new Microsoft.Data.SqlClient.SqlParameter("@Branch", branchId))
                    .FirstOrDefaultAsync();
                results.Add("invoiceNo", result1?.NEWCLIENTINVOICENO ?? 1);
            }

            return results;
        }

        //[HttpGet]
        //[Route("GetInvoiceAndDetailsByAgreementId")]
        //public async Task<ActionResult<Object>> GetInvoiceAndDetailsByAgreementId(int agreementID)
        //{
        //    var results = new Dictionary<string, Object>();

        //    var obj1 = await _oBMSDbContext.ClientInvoices.Where(x => x.AgreementID == agreementID).FirstOrDefaultAsync();
        //    results.Add("invoice", obj1);

        //    var obj2 = await _oBMSDbContext.ClientInvoiceDetails.Where(x => x.ClientInvoiceID == obj1.ID).ToListAsync();
        //    results.Add("details", obj2);

        //    return results;
        //}

        [HttpPost]
        [Route("SaveAndUpdateBatchInvoice")]
        public async Task<Object> SaveAndUpdateBatchInvoice(ClientBatchInvoiceRequestDto clientBatchInvoiceRequestDto)
        {
            try
            {
                foreach (ClientInvoiceRequestDto clientInvoiceRequestDto in clientBatchInvoiceRequestDto.data)
                {
                    try
                    {
                        // Clear EF change tracker between each invoice to avoid identity conflicts
                        _oBMSDbContext.ChangeTracker.Clear();
                        var clientInvoice = new ClientInvoice();
                        if (clientInvoiceRequestDto.ID != 0)
                        {
                            clientInvoice = _oBMSDbContext.ClientInvoices.Where(x => x.ID == clientInvoiceRequestDto.ID).FirstOrDefault();
                        }

                        if (clientInvoiceRequestDto.ID == 0)
                        {
                            // New invoice — don't set ID, let DB auto-generate
                            clientInvoice.ServiceCharges = clientInvoiceRequestDto.ServiceCharges;
                            clientInvoice.Subject = clientInvoiceRequestDto.Subject ?? "-";
                            clientInvoice.Note = clientInvoiceRequestDto.Note ?? "-";
                            clientInvoice.Branch = clientInvoiceRequestDto.Branch;
                            clientInvoice.Client = clientInvoiceRequestDto.Client;
                            clientInvoice.AgreementID = clientInvoiceRequestDto.AgreementID;
                            clientInvoice.Discount = clientInvoiceRequestDto.Discount;
                            clientInvoice.InvoiceDate = clientInvoiceRequestDto.InvoiceDate;
                            clientInvoice.InvoiceNo = clientInvoiceRequestDto.InvoiceNo;
                            clientInvoice.TaxAmount = clientInvoiceRequestDto.TaxAmount;
                            clientInvoice.IsDeleted = "N";
                            clientInvoice.LASTUPDATE = DateTime.Now;
                            _oBMSDbContext.ClientInvoices.Add(clientInvoice);
                        }
                        else
                        {
                            clientInvoice.ID = clientInvoiceRequestDto.ID;
                            clientInvoice.ServiceCharges = clientInvoiceRequestDto.ServiceCharges;
                            clientInvoice.Subject = clientInvoiceRequestDto.Subject ?? "-";
                            clientInvoice.Note = clientInvoiceRequestDto.Note ?? "-";
                            clientInvoice.Branch = clientInvoiceRequestDto.Branch;
                            clientInvoice.Client = clientInvoiceRequestDto.Client;
                            clientInvoice.AgreementID = clientInvoiceRequestDto.AgreementID;
                            clientInvoice.Discount = clientInvoiceRequestDto.Discount;
                            clientInvoice.InvoiceDate = clientInvoiceRequestDto.InvoiceDate;
                            clientInvoice.InvoiceNo = clientInvoiceRequestDto.InvoiceNo;
                            clientInvoice.TaxAmount = clientInvoiceRequestDto.TaxAmount;
                            clientInvoice.IsDeleted = "N";
                            clientInvoice.LASTUPDATE = DateTime.Now;
                            _oBMSDbContext.ClientInvoices.Update(clientInvoice);
                        }

                        await _oBMSDbContext.SaveChangesAsync();

                        if (clientInvoiceRequestDto.details != null)
                        {
                            foreach (ClientInvoiceDetailRequestDto detail in clientInvoiceRequestDto.details)
                            {
                                var clientInvoiceDetail = new ClientInvoiceDetail();

                                if (detail.ID != 0)
                                {
                                    clientInvoiceDetail = _oBMSDbContext.ClientInvoiceDetails.Where(x => x.ID == detail.ID).FirstOrDefault();
                                    clientInvoiceDetail.ID = detail.ID;
                                    clientInvoiceDetail.LASTUPDATE = DateTime.Now;
                                }
                                else
                                {
                                    // New detail — don't set ID
                                    clientInvoiceDetail.LASTUPDATE = DateTime.Now;
                                }

                                clientInvoiceDetail.NoOfGuards = detail.NoOfGuards;
                                clientInvoiceDetail.ClientInvoiceID = clientInvoice.ID;
                                clientInvoiceDetail.AgreementDetailID = detail.AgreementDetailID;
                                clientInvoiceDetail.AgreementID = detail.AgreementID;
                                clientInvoiceDetail.AgreementDate = detail.AgreementDate;
                                clientInvoiceDetail.Rate = detail.Rate;
                                clientInvoiceDetail.NoOfHours = detail.NoOfHours;
                                clientInvoiceDetail.NoOfDays = detail.NoOfDays;
                                clientInvoiceDetail.FollowCalender = detail.FollowCalender;
                                clientInvoiceDetail.HasDiscount = detail.HasDiscount;
                                clientInvoiceDetail.DiscountAmount = detail.DiscountAmount;
                                clientInvoiceDetail.IsTaxable = detail.IsTaxable;
                                clientInvoiceDetail.TaxAmount = detail.TaxAmount;
                                clientInvoiceDetail.MonthTotal = detail.MonthTotal;

                                if (detail.ID != 0)
                                    _oBMSDbContext.ClientInvoiceDetails.Update(clientInvoiceDetail);
                                else
                                    _oBMSDbContext.ClientInvoiceDetails.Add(clientInvoiceDetail);

                                await _oBMSDbContext.SaveChangesAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = ex.Message, 
                    inner = ex.InnerException?.Message,
                    detail = ex.InnerException?.InnerException?.Message
                });
            }
            return null;
        }


        [HttpPost]
        [Route("SaveAndUpdateInvoice")]
        public async Task<Object> SaveAndUpdateInvoice(ClientInvoiceRequestDto clientInvoiceRequestDto)
        {
            try
            {

                var clientInvoice = new ClientInvoice();
                if (clientInvoiceRequestDto.ID != 0)
                {
                    clientInvoice = _oBMSDbContext.ClientInvoices.Where(x => x.ID == clientInvoiceRequestDto.ID).FirstOrDefault();
                }

                clientInvoice.ID = clientInvoiceRequestDto.ID;
                clientInvoice.ServiceCharges = clientInvoiceRequestDto.ServiceCharges;
                clientInvoice.Subject = clientInvoiceRequestDto.Subject;
                clientInvoice.Note = clientInvoiceRequestDto.Note;
                clientInvoice.Branch = clientInvoiceRequestDto.Branch;
                clientInvoice.Client = clientInvoiceRequestDto.Client;
                clientInvoice.AgreementID = clientInvoiceRequestDto.AgreementID;
                clientInvoice.Discount = clientInvoiceRequestDto.Discount;
                clientInvoice.InvoiceDate = clientInvoiceRequestDto.InvoiceDate;
                clientInvoice.InvoiceNo = clientInvoiceRequestDto.InvoiceNo;
                clientInvoice.TaxAmount = clientInvoiceRequestDto.TaxAmount;
                clientInvoice.IsDeleted = "N";
                clientInvoice.LASTUPDATE = DateTime.Now;

                if (clientInvoiceRequestDto.ID != 0)
                {
                    _oBMSDbContext.ClientInvoices.Update(clientInvoice);

                }
                else
                {
                    _oBMSDbContext.ClientInvoices.Add(clientInvoice);

                }

                await _oBMSDbContext.SaveChangesAsync();


                if (clientInvoiceRequestDto.details != null)
                {
                    foreach (ClientInvoiceDetailRequestDto detail in clientInvoiceRequestDto.details)
                    {


                        var clientInvoiceDetail = new ClientInvoiceDetail();

                        if (detail.ID != 0)
                        {
                            clientInvoiceDetail = _oBMSDbContext.ClientInvoiceDetails.Where(x => x.ID == detail.ID).FirstOrDefault();
                        }

                        clientInvoiceDetail.ID = detail.ID;

                        clientInvoiceDetail.NoOfGuards = detail.NoOfGuards;
                        clientInvoiceDetail.ClientInvoiceID = clientInvoice.ID;
                        clientInvoiceDetail.AgreementDetailID = detail.AgreementDetailID;
                        clientInvoiceDetail.AgreementID = detail.AgreementID;
                        clientInvoiceDetail.AgreementDate = detail.AgreementDate;
                        clientInvoiceDetail.NoOfGuards = detail.NoOfGuards;
                        clientInvoiceDetail.Rate = detail.Rate;
                        clientInvoiceDetail.NoOfHours = detail.NoOfHours;
                        clientInvoiceDetail.NoOfDays = detail.NoOfDays;
                        clientInvoiceDetail.FollowCalender = detail.FollowCalender;
                        clientInvoiceDetail.HasDiscount = detail.HasDiscount;
                        clientInvoiceDetail.DiscountAmount = detail.DiscountAmount;
                        clientInvoiceDetail.IsTaxable = detail.IsTaxable;
                        clientInvoiceDetail.TaxAmount = detail.TaxAmount;
                        clientInvoiceDetail.MonthTotal
                         = detail.MonthTotal;


                        clientInvoiceDetail.LASTUPDATE = DateTime.Now;

                        if (detail.ID != 0)
                        {
                            _oBMSDbContext.ClientInvoiceDetails.Update(clientInvoiceDetail);
                        }
                        else
                        {
                            _oBMSDbContext.ClientInvoiceDetails.Add(clientInvoiceDetail);
                        }


                        await _oBMSDbContext.SaveChangesAsync();
                    }
                }

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("agreement", clientInvoice);
                response.Headers.Add("Success", "Successfully save & update Invoice details.");

                return Ok(dictResult);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        //[HttpPost]
        //[Route("SaveAndUpdateInvoiceDetail")]
        //public async Task<Object> SaveAndUpdateInvoiceDetail(ClientInvoiceRequestDto clientInvoiceRequestDto)
        //{
        //    try
        //    {
        //        var clientInvoice = new ClientInvoice();
        //        if (clientInvoiceRequestDto.ID != 0)
        //        {
        //            clientInvoice = _oBMSDbContext.ClientInvoices.Where(x => x.ID == clientInvoiceRequestDto.ID).FirstOrDefault();
        //        }

        //        clientInvoice.ID = clientInvoiceRequestDto.ID;
        //        clientInvoice.ServiceCharges = clientInvoiceRequestDto.ServiceCharges;
        //        clientInvoice.LASTUPDATE = DateTime.Now;

        //        if (clientInvoiceRequestDto.ID != 0)
        //        {
        //            _oBMSDbContext.ClientInvoices.Update(clientInvoice);

        //        }


        //        await _oBMSDbContext.SaveChangesAsync();

        //        if (clientInvoiceRequestDto.details != null)
        //        {
        //            foreach (ClientInvoiceDetailRequestDto detail in clientInvoiceRequestDto.details)
        //            {


        //                var clientInvoiceDetail = new ClientInvoiceDetail();

        //                if (detail.ID != 0)
        //                {
        //                    clientInvoiceDetail = _oBMSDbContext.ClientInvoiceDetails.Where(x => x.ID == detail.ID).FirstOrDefault();
        //                }

        //                clientInvoiceDetail.ID = detail.ID;

        //                clientInvoiceDetail.NoOfGuards = detail.NoOfGuards;
        //                clientInvoiceDetail.ClientInvoiceID = clientInvoice.ID;
        //                clientInvoiceDetail.AgreementDetailID = detail.AgreementDetailID;
        //                clientInvoiceDetail.AgreementID = detail.AgreementID;
        //                clientInvoiceDetail.AgreementDate = detail.AgreementDate;
        //                clientInvoiceDetail.NoOfGuards = detail.NoOfGuards;
        //                clientInvoiceDetail.Rate = detail.Rate;
        //                clientInvoiceDetail.NoOfHours = detail.NoOfHours;
        //                clientInvoiceDetail.NoOfDays = detail.NoOfDays;
        //                clientInvoiceDetail.FollowCalender = detail.FollowCalender;
        //                clientInvoiceDetail.HasDiscount = detail.HasDiscount;
        //                clientInvoiceDetail.DiscountAmount = detail.DiscountAmount;
        //                clientInvoiceDetail.IsTaxable = detail.IsTaxable;
        //                clientInvoiceDetail.TaxAmount = detail.TaxAmount;
        //                clientInvoiceDetail.MonthTotal
        //                 = detail.MonthTotal;


        //                clientInvoiceDetail.LASTUPDATE = DateTime.Now;

        //                if (detail.ID != 0)
        //                {
        //                    _oBMSDbContext.ClientInvoiceDetails.Update(clientInvoiceDetail);
        //                }
        //                else
        //                {
        //                    _oBMSDbContext.ClientInvoiceDetails.Add(clientInvoiceDetail);
        //                }


        //                await _oBMSDbContext.SaveChangesAsync();
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //    return null;
        //}

        [HttpPost]
        [Route("SaveAndUpdateInvoiceDetail")]
        public async Task<IActionResult> SaveAndUpdateInvoiceDetail(ClientInvoiceRequestDto clientInvoiceRequestDto)
        {
            try
            {
                ClientInvoice? clientInvoice;

                // 1️⃣ Get existing invoice or create new
                if (clientInvoiceRequestDto.ID != 0)
                {
                    clientInvoice = await _oBMSDbContext.ClientInvoices
                        .FirstOrDefaultAsync(x => x.ID == clientInvoiceRequestDto.ID);

                    if (clientInvoice == null)
                        return NotFound("ClientInvoice not found");

                    // Update existing invoice
                    clientInvoice.ServiceCharges = clientInvoiceRequestDto.ServiceCharges;
                    clientInvoice.Discount = clientInvoiceRequestDto.Discount;
                    clientInvoice.TaxAmount = clientInvoiceRequestDto.TaxAmount;
                    clientInvoice.LASTUPDATE = DateTime.Now;

                    _oBMSDbContext.ClientInvoices.Update(clientInvoice);
                }
                else
                {
                    // Create new invoice
                    clientInvoice = new ClientInvoice
                    {
                        ServiceCharges = clientInvoiceRequestDto.ServiceCharges,
                        Discount = clientInvoiceRequestDto.Discount,
                        TaxAmount = clientInvoiceRequestDto.TaxAmount,
                        LASTUPDATE = DateTime.Now
                    };
                    _oBMSDbContext.ClientInvoices.Add(clientInvoice);
                }

                await _oBMSDbContext.SaveChangesAsync();

                // 2️⃣ Handle details
                if (clientInvoiceRequestDto.details != null && clientInvoiceRequestDto.details.Any())
                {
                    foreach (var detail in clientInvoiceRequestDto.details)
                    {
                        ClientInvoiceDetail? clientInvoiceDetail;

                        if (detail.ID != 0)
                        {
                            clientInvoiceDetail = await _oBMSDbContext.ClientInvoiceDetails
                                .FirstOrDefaultAsync(x => x.ID == detail.ID);

                            if (clientInvoiceDetail == null)
                                continue; // skip or handle error
                        }
                        else
                        {
                            clientInvoiceDetail = new ClientInvoiceDetail
                            {
                                ClientInvoiceID = clientInvoice.ID
                            };
                            _oBMSDbContext.ClientInvoiceDetails.Add(clientInvoiceDetail);
                        }

                        // Map fields
                        clientInvoiceDetail.AgreementID = detail.AgreementID;
                        clientInvoiceDetail.AgreementDetailID = detail.AgreementDetailID;
                        clientInvoiceDetail.AgreementDate = detail.AgreementDate;
                        clientInvoiceDetail.NoOfGuards = detail.NoOfGuards;
                        clientInvoiceDetail.Rate = detail.Rate;
                        clientInvoiceDetail.NoOfHours = detail.NoOfHours;
                        clientInvoiceDetail.NoOfDays = detail.NoOfDays;
                        clientInvoiceDetail.FollowCalender = detail.FollowCalender;
                        clientInvoiceDetail.HasDiscount = detail.HasDiscount;
                        clientInvoiceDetail.DiscountAmount = detail.DiscountAmount;
                        clientInvoiceDetail.IsTaxable = detail.IsTaxable;
                        clientInvoiceDetail.TaxAmount = detail.TaxAmount;
                        clientInvoiceDetail.MonthTotal = detail.MonthTotal;
                        clientInvoiceDetail.LASTUPDATE = DateTime.Now;
                    }

                    await _oBMSDbContext.SaveChangesAsync();
                }

                return Ok(new { Message = "Invoice and details saved successfully", InvoiceID = clientInvoice.ID });
            }
            catch (Exception ex)
            {
                // Optionally log error
                return StatusCode(500, new { Message = ex.Message });
            }
        }


        [HttpPost]
        [Route("DeleteInvoiceDetailById")]
        public async Task<ActionResult<HttpResponseMessage>> DeleteInvoiceDetailById(ClientInvoiceRequestDto clientInvoiceRequestDto)
        {
            try
            {
                var data = _oBMSDbContext.ClientInvoiceDetails.Where(x => x.ID == clientInvoiceRequestDto.DeletedDetailID).FirstOrDefault();


                if (data != null)
                {
                    _oBMSDbContext.ClientInvoiceDetails.Remove(data);
                    await _oBMSDbContext.SaveChangesAsync();
                }

                var clientInvoice = new ClientInvoice();
                if (clientInvoiceRequestDto.ID != 0)
                {
                    clientInvoice = _oBMSDbContext.ClientInvoices.Where(x => x.ID == clientInvoiceRequestDto.ID).FirstOrDefault();
                }

                clientInvoice.ID = clientInvoiceRequestDto.ID;
                clientInvoice.ServiceCharges = clientInvoiceRequestDto.ServiceCharges;
                clientInvoice.LASTUPDATE = DateTime.Now;

                if (clientInvoiceRequestDto.ID != 0)
                {
                    _oBMSDbContext.ClientInvoices.Update(clientInvoice);

                }


                await _oBMSDbContext.SaveChangesAsync();


                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");


                return Ok(dictResult);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("DeleteInvoiceById")]
        public async Task<ActionResult<HttpResponseMessage>> DeleteInvoiceById(int Id)
        {
            try
            {
                var clientInvoice = new ClientInvoice();
                clientInvoice = _oBMSDbContext.ClientInvoices.Where(x => x.ID == Id).FirstOrDefault();

                clientInvoice.LASTUPDATE = DateTime.Now;
                clientInvoice.IsDeleted = "Y";


                _oBMSDbContext.ClientInvoices.Update(clientInvoice);




                await _oBMSDbContext.SaveChangesAsync();


                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");


                return Ok(dictResult);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("DeletedInvoiceListByBranchId")]
        public async Task<ActionResult<HttpResponseMessage>> DeletedInvoiceListByBranchId(string Id)
        {
            try
            {
                var result = from clientInvoice in _oBMSDbContext.ClientInvoices
                             join clientMaster in _oBMSDbContext.ClientMasters on new { clientInvoice.Client, clientInvoice.Branch } equals new { Client = clientMaster.Code, clientMaster.Branch }
                             where clientInvoice.IsDeleted == "Y" && clientInvoice.Branch == Id
                             orderby clientInvoice.InvoiceNo
                             select new
                             {
                                 clientInvoice.ID,
                                 clientInvoice.InvoiceNo,
                                 clientInvoice.InvoiceDate,
                                 clientInvoice.ServiceCharges,
                                 clientInvoice.Discount,
                                 clientInvoice.TaxAmount,
                                 ClientName = clientMaster.Name,
                                 clientInvoice.Client,
                                 clientInvoice.LASTUPDATE
                             };


                return Ok(result);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("PrintInvoice")]
        public async Task<ActionResult<HttpResponseMessage>> PrintInvoice(int invoiceId)
        {
            try
            {
                Dictionary<string, object> results = new Dictionary<string, object>();
                var obj1 = await _oBMSDbContext.ClientInvoices.Where(x => x.ID == invoiceId).FirstOrDefaultAsync();
                results.Add("invoice", obj1);

                var obj2 = await _oBMSDbContext.ClientInvoiceDetails.Where(x => x.ClientInvoiceID == invoiceId).ToListAsync();
                results.Add("details", obj2);

                var client = await _oBMSDbContext.ClientMasters.Where(x => x.Code == obj1.Client).FirstOrDefaultAsync();

                results.Add("client", client);

                results.Add("Success", "");


                return Ok(results);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("RestoreInvoices")]
        public async Task<ActionResult<HttpResponseMessage>> RestoreInvoices(int[] ID)
        {
            try
            {
                if (ID != null)
                {
                    foreach (int id in ID)
                    {
                        var clientInvoice = new ClientInvoice();
                        clientInvoice = _oBMSDbContext.ClientInvoices.Where(x => x.ID == id).FirstOrDefault();

                        clientInvoice.LASTUPDATE = DateTime.Now;
                        clientInvoice.IsDeleted = "N";


                        _oBMSDbContext.ClientInvoices.Update(clientInvoice);

                        await _oBMSDbContext.SaveChangesAsync();
                    }

                }

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", ID);


                return Ok(dictResult);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("GetPaymentMaster")]
        public async Task<ActionResult<Object>> GetPaymentMaster(string userID = "")
        {
            try
            {
                var obj = await _financeRepository.GetPaymentMaster(userID);


                return obj;


            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetPaymentMasterCategoryType")]
        public async Task<ActionResult<Object>> GetPaymentMasterCategoryType(string cat = "")
        {
            try
            {
                IQueryable<InventoryCategory> query = _oBMSDbContext.InventoryCategories;

                if (cat == "U")
                {
                    query = query.Where(x => x.Cat != "P");
                }
                else
                {
                    query = query.Where(x => x.Cat == cat);
                }

                var obj = await query.OrderBy(x => x.Name).ToListAsync();
                return obj;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        [HttpGet]
        [Route("GetPaymentMasterCategoryTypeChangePayTo")]
        public async Task<ActionResult<Object>> GetPaymentMasterCategoryTypeChangePayTo(int cat)
        {
            try
            {

                var result = await _oBMSDbContext.PayToViews.Where(x => x.Category == cat).ToListAsync();

                return result;


            }
            catch (Exception ex)
            {
                throw;
            }

        }

        //[HttpGet]
        //[Route("GetPaymentSupplierInvoices")]
        //public async Task<ActionResult<Object>> GetPaymentSupplierInvoices(int supplier, string userId)
        //{
        //    try
        //    {
        //        var result = await _oBMSDbContext.SupplierInvoiceViews.Where(x => x.BranchUserName == userId).Where(x => x.Supplier == supplier).ToListAsync();

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }

        //}

        [HttpGet]
        [Route("GetPaymentSupplierInvoices")]
        public async Task<ActionResult> GetPaymentSupplierInvoices(int supplier, string userId)
        {
            try
            {
                // 1️⃣ Calculate PaidAmount
                decimal paidAmount = await _oBMSDbContext.BranchPayments
                    .Where(x => x.Supplier == supplier && x.IsDeleted == false)
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;

                // 2️⃣ Return anonymous object
                var result = await _oBMSDbContext.SupplierInvoiceViews
                    .Where(x => x.BranchUserName == userId && x.Supplier == supplier)
                    .Select(x => new
                    {
                        x.ID,
                        x.PaymentID,
                        x.Code,
                        x.Name,
                        x.InvoiceID,
                        x.InvoiceNo,
                        x.Total,
                        x.Amount,

                        PaidAmount = paidAmount,
                        Balance = x.Total - paidAmount,

                        x.BranchUserName,
                        x.Supplier
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch
            {
                throw;
            }
        }

        [HttpGet]
        [Route("creditor-invoice-payment-details")]
        public async Task<List<CreditorInvoicePaymentRow>> GetCreditorInvoicePaymentList(string userId, decimal paymentId)
        {
            var sql = @"
                        SELECT
                            CAST(ISNULL(BPD.ID,0) AS INT) AS ID,
                            CAST(ISNULL(BPD.PaymentID,0) AS INT) AS PaymentID,
                            BM.Code,
                            BM.Name,
                            CAST(CI.ID AS INT) AS InvoiceID,
                            CI.InvoiceNo,
                            CI.Total,
                            ISNULL(P.PaidAmount,0) AS PaidAmount,
                            ISNULL(BPD.Amount,0) AS Amount
                        FROM CreditorInvoice CI
                        INNER JOIN OBMSBranches OB
                            ON OB.BranchCode = CI.Branch
                        INNER JOIN BranchMaster BM
                            ON BM.Code = OB.BranchCode
                        LEFT JOIN BranchPaymentDetails BPD
                            ON BPD.InvoiceID = CI.ID
                           AND BPD.PaymentID = @PaymentID
                           AND BPD.IsDeleted = 0
                        LEFT JOIN
                        (
                            SELECT InvoiceID, SUM(Amount) AS PaidAmount
                            FROM BranchPaymentDetails
                            WHERE InvoiceID <> 0 AND IsDeleted = 0
                            GROUP BY InvoiceID
                        ) P ON P.InvoiceID = CI.ID
                        WHERE OB.Name = @UserID
                            AND CI.IsDeleted = 0
                          AND (
                                @PaymentID <> 0
                                OR CI.Total - ISNULL(P.PaidAmount,0) > 0
                              )
                          AND (
                                @PaymentID = 0
                                OR BPD.ID IS NOT NULL
                              )
                        ORDER BY CI.PaymentDate";

            var parameters = new[]
            {
                new Microsoft.Data.SqlClient.SqlParameter("@UserID", userId),
                new Microsoft.Data.SqlClient.SqlParameter("@PaymentID", paymentId)
            };

            return await _oBMSDbContext.CreditorInvoicePaymentRows
                .FromSqlRaw(sql, parameters)
                .AsNoTracking()
                .ToListAsync();
        }

        [HttpGet]
        [Route("creditor-invoice-payment-details-by-supplier")]
        public async Task<List<CreditorInvoicePaymentRow>> getCreditorInvoicePaymentListBySupplier(string userId, decimal paymentId,decimal supplier)
        {
            try
            {
                var sql = @"
                    SELECT 
                        CAST(ISNULL(BPD.ID,0) AS INT) AS ID,
                        CAST(ISNULL(BPD.PaymentID,0) AS INT) AS PaymentID,
                        BM.Code,
                        BM.Name,
                        CAST(CI.ID AS INT) AS InvoiceID,
                        CI.InvoiceNo,
                        CI.Total,
                        ISNULL(BPD.Amount,0) AS Amount,
                        ISNULL(P.PaidAmount,0) AS PaidAmount
                    FROM CreditorInvoice CI
                    INNER JOIN OBMSBranches OB 
                        ON OB.BranchCode = CI.Branch
                    INNER JOIN BranchMaster BM 
                        ON BM.Code = OB.BranchCode
                    LEFT JOIN BranchPaymentDetails BPD 
                        ON BPD.InvoiceID = CI.ID 
                       AND BPD.PaymentID = @PaymentID
                       AND BPD.IsDeleted = 0
                    LEFT JOIN (
                        SELECT InvoiceID, SUM(Amount) AS PaidAmount
                        FROM BranchPaymentDetails
                        WHERE InvoiceID <> 0 AND IsDeleted = 0
                        GROUP BY InvoiceID
                    ) P 
                        ON P.InvoiceID = CI.ID
                    WHERE OB.Name = @UserID             
                      AND CI.IsDeleted = 0
                      AND CI.Supplier = @Supplier
                      AND (
                            @PaymentID <> 0               
                            OR CI.Total - ISNULL(P.PaidAmount,0) > 0 
                          )
                    ORDER BY CI.PaymentDate";

                var parameters = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@UserID", userId),
                    new Microsoft.Data.SqlClient.SqlParameter("@PaymentID", paymentId),
                    new Microsoft.Data.SqlClient.SqlParameter("@Supplier", supplier)
                };

                return await _oBMSDbContext.CreditorInvoicePaymentRows
                    .FromSqlRaw(sql, parameters)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return new List<CreditorInvoicePaymentRow>();
            }
            
        }



        [HttpGet]
        [Route("branch-payment-details")]
        public async Task<List<BranchPaymentRow>> GetBranchPaymentList(string userId, decimal paymentId)
        {
            var sql = @"
                        SELECT
                            CAST(ISNULL(BPD.ID,0) AS INT) AS ID,
                            CAST(ISNULL(BPD.PaymentID,0) AS INT) AS PaymentID,
                            BM.Code,
                            BM.Name,
                            BPD.Amount
                        FROM OBMSBranches OB
                        INNER JOIN BranchMaster BM
                            ON BM.Code = OB.BranchCode
                        LEFT JOIN BranchPaymentDetails BPD
                            ON BPD.Branch = BM.Code
                           AND BPD.PaymentID = @PaymentID
                           AND BPD.IsDeleted = 0
                        WHERE OB.Name = @UserID
                        ORDER BY BM.Code";

            var parameters = new[]
            {
                new Microsoft.Data.SqlClient.SqlParameter("@UserID", userId),
                new Microsoft.Data.SqlClient.SqlParameter("@PaymentID", paymentId)
            };

            return await _oBMSDbContext.BranchPaymentRows
                .FromSqlRaw(sql, parameters)
                .AsNoTracking()
                .ToListAsync();
        }

        [HttpPost]
        [Route("SaveAndUpdatePayment")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdatePayment(BranchPaymentRequestDto branchPaymentRequestDto)
        {
            try
            {
                var branchPayment = new BranchPayment();
                if (branchPaymentRequestDto.ID != 0)
                {
                    branchPayment = _oBMSDbContext.BranchPayments.Where(x => x.ID == branchPaymentRequestDto.ID)
                        .Select(bp => new BranchPayment
                        {
                            ID = bp.ID,
                            PaymentDate = bp.PaymentDate,
                            CreditorType = bp.CreditorType,
                            Supplier = bp.Supplier ?? 0, // Default value for nullable decimal
                            PaymentType = bp.PaymentType,
                            PaymentPurpose = bp.PaymentPurpose,
                            BankID = bp.BankID ?? 0, // Default value for nullable decimal
                            ChequeNo = bp.ChequeNo ?? string.Empty, // Default value for nullable string
                            PaymentTo = bp.PaymentTo ?? string.Empty, // Default value for nullable string
                            ItemCategory = bp.ItemCategory ?? string.Empty, // Default value for nullable string
                            Particulars = bp.Particulars ?? string.Empty, // Default value for nullable string
                            Amount = bp.Amount,
                            IsDeleted = bp.IsDeleted,
                            ChequeStatus = bp.ChequeStatus == '\0' ? ' ' : bp.ChequeStatus, // Default to space if empty char
                            LastUpdate = bp.LastUpdate,
                            VoucherNo = bp.VoucherNo ?? string.Empty // Default value for nullable string
                        }).FirstOrDefault();
                }
                bool shouldGenerateVoucher =
                     branchPaymentRequestDto.ID == 0 ||
                     string.IsNullOrWhiteSpace(branchPayment?.VoucherNo) ||
                     branchPayment.VoucherNo == "0" ||
                     branchPayment.BankID != branchPaymentRequestDto.BankID || // Bank changed
                     branchPayment.PaymentType != branchPaymentRequestDto.PaymentType; // Payment Type changed

                //if (string.IsNullOrWhiteSpace(branchPayment.VoucherNo) || branchPayment.VoucherNo == "0")
                //{
                //    var newVoucherNo = _oBMSDbContext.BranchPayments
                //        .Where(bp =>
                //            bp.PaymentType == branchPaymentRequestDto.PaymentType &&
                //            (bp.BankID ?? 0) == branchPaymentRequestDto.BankID &&
                //            bp.PaymentDate.Year > 2012)
                //        .Select(bp => bp.VoucherNo)
                //        .ToList()
                //        .Select(v => int.TryParse(v, out var n) ? n : 0)
                //        .DefaultIfEmpty(0)
                //        .Max() + 1;

                //    branchPayment.VoucherNo = newVoucherNo.ToString();
                //}
                if (shouldGenerateVoucher)
                {
                    var newVoucherNo = _oBMSDbContext.BranchPayments
                        .Where(bp =>
                            bp.PaymentType == branchPaymentRequestDto.PaymentType &&
                            (bp.BankID ?? 0) == branchPaymentRequestDto.BankID &&
                            bp.PaymentDate.Year > 2012)
                        .Select(bp => bp.VoucherNo)
                        .AsEnumerable()
                        .Select(v => int.TryParse(v, out var n) ? n : 0)
                        .DefaultIfEmpty(0)
                        .Max() + 1;

                    branchPayment.VoucherNo = newVoucherNo.ToString();
                }
                else
                {
                    branchPayment.VoucherNo = branchPayment.VoucherNo;
                }

                branchPayment.ID = branchPaymentRequestDto.ID;
                branchPayment.PaymentDate = branchPaymentRequestDto.PaymentDate;
                branchPayment.CreditorType = branchPaymentRequestDto.CreditorType;
                branchPayment.Supplier = branchPaymentRequestDto.Supplier;
                branchPayment.PaymentType = branchPaymentRequestDto.PaymentType;
                branchPayment.PaymentPurpose = branchPaymentRequestDto.PaymentPurpose;
                branchPayment.BankID = branchPaymentRequestDto.BankID;
                branchPayment.ChequeNo = branchPaymentRequestDto.ChequeNo;
                branchPayment.PaymentTo = branchPaymentRequestDto.PaymentTo;
                branchPayment.ItemCategory = branchPaymentRequestDto.ItemCategory;
                branchPayment.Particulars = branchPaymentRequestDto.Particulars;
                branchPayment.Amount = branchPaymentRequestDto.Amount;
                branchPayment.IsDeleted = false;
                branchPayment.ChequeStatus = branchPayment.ChequeStatus == '\0' ? ' ' : branchPayment.ChequeStatus;
                branchPayment.LastUpdate = DateTime.Now;
                branchPayment.LastUpdatedBy = branchPaymentRequestDto.userId;

                if (branchPayment.ID == 0)
                {
                    _oBMSDbContext.BranchPayments.Add(branchPayment);
                }
                else
                {
                    _oBMSDbContext.BranchPayments.Update(branchPayment);
                }


                await _oBMSDbContext.SaveChangesAsync();
                var savedPaymentId = branchPayment.ID; // ✅ This now contains the new or updated ID

                if (branchPaymentRequestDto.details != null)
                {
                    foreach (BranchPaymentDetailsRequestDto detail in branchPaymentRequestDto.details)
                    {
                        if (detail.Amount != 0)
                        {
                            bool isExistingDetail = detail.ID != 0;
                            BranchPaymentDetails branchPaymentDetails;

                            if (isExistingDetail)
                            {
                                // Detach any tracked instance to avoid EF conflicts
                                var tracked = _oBMSDbContext.ChangeTracker.Entries<BranchPaymentDetails>()
                                    .FirstOrDefault(e => e.Entity.ID == detail.ID);
                                if (tracked != null)
                                    tracked.State = Microsoft.EntityFrameworkCore.EntityState.Detached;

                                branchPaymentDetails = _oBMSDbContext.BranchPaymentDetails
                                    .Where(x => x.ID == detail.ID).FirstOrDefault()
                                    ?? new BranchPaymentDetails();
                            }
                            else
                            {
                                // New row (ID == 0): check if a soft-deleted record already exists
                                // for the same (PaymentID, Branch, InvoiceID) to avoid unique key violation
                                var existingSoftDeleted = await _oBMSDbContext.BranchPaymentDetails
                                    .FirstOrDefaultAsync(x =>
                                        x.PaymentID == branchPayment.ID &&
                                        x.Branch == detail.Branch &&
                                        x.InvoiceID == detail.InvoiceID &&
                                        x.IsDeleted == true);

                                if (existingSoftDeleted != null)
                                {
                                    // Reactivate the soft-deleted row instead of inserting a duplicate
                                    branchPaymentDetails = existingSoftDeleted;
                                    isExistingDetail = true; // treat as update
                                }
                                else
                                {
                                    branchPaymentDetails = new BranchPaymentDetails();
                                    // Do NOT set ID — let the DB auto-generate it
                                }
                            }

                            branchPaymentDetails.PaymentID = branchPayment.ID;
                            branchPaymentDetails.Branch = detail.Branch;
                            branchPaymentDetails.InvoiceID = detail.InvoiceID;
                            branchPaymentDetails.Amount = detail.Amount;
                            branchPaymentDetails.LastUpdate = DateTime.Now;
                            branchPaymentDetails.LastUpdatedBy = branchPaymentRequestDto.userId;
                            branchPaymentDetails.IsDeleted = false;

                            if (isExistingDetail)
                            {
                                _oBMSDbContext.BranchPaymentDetails.Update(branchPaymentDetails);
                            }
                            else
                            {
                                _oBMSDbContext.BranchPaymentDetails.Add(branchPaymentDetails);
                            }

                            await _oBMSDbContext.SaveChangesAsync();
                            _oBMSDbContext.ChangeTracker.Clear(); // Prevent stale tracked entities on next iteration
                        }
                    }
                }

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("PaymentID", savedPaymentId); // ✅ Return the ID

                response.Headers.Add("Success", "Successfully save & update  details.");

                return Ok(dictResult);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    detail = ex.InnerException?.InnerException?.Message
                });
            }
        }

        //public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdatePayment(BranchPaymentRequestDto branchPaymentRequestDto)
        //{
        //    try
        //    {
        //        var branchPayment = new BranchPayment();
        //        bool isNewPayment = branchPaymentRequestDto.ID == 0;

        //        if (!isNewPayment)
        //        {
        //            branchPayment = _oBMSDbContext.BranchPayments
        //                .FirstOrDefault(x => x.ID == branchPaymentRequestDto.ID) ?? new BranchPayment
        //                {
        //                    LastUpdate = DateTime.Now,
        //                    ChequeStatus = 'N', // example default
        //                    IsDeleted = false,
        //                    PaymentDate = DateTime.Now
        //                };
        //        }

        //        // Only generate new voucher number if creating new record or voucher is missing
        //        if (isNewPayment || string.IsNullOrWhiteSpace(branchPayment.VoucherNo))
        //        {
        //            var newVoucherNo = _oBMSDbContext.BranchPayments
        //                .Where(bp =>
        //                    bp.PaymentType == branchPaymentRequestDto.PaymentType &&
        //                    (bp.BankID ?? 0) == branchPaymentRequestDto.BankID &&
        //                    bp.PaymentDate.Year > 2012)
        //                .Select(bp => bp.VoucherNo)
        //                .ToList()
        //                .Select(v => int.TryParse(v, out var n) ? n : 0)
        //                .DefaultIfEmpty(0)
        //                .Max() + 1;

        //            branchPayment.VoucherNo = newVoucherNo.ToString();
        //        }

        //        // Assign payment fields
        //        branchPayment.PaymentDate = branchPaymentRequestDto.PaymentDate;
        //        branchPayment.CreditorType = branchPaymentRequestDto.CreditorType;
        //        branchPayment.Supplier = branchPaymentRequestDto.Supplier;
        //        branchPayment.PaymentType = branchPaymentRequestDto.PaymentType;
        //        branchPayment.Supplier = branchPaymentRequestDto.Supplier;
        //        branchPayment.PaymentPurpose = branchPaymentRequestDto.PaymentPurpose;
        //        branchPayment.BankID = branchPaymentRequestDto.BankID;
        //        branchPayment.ChequeNo = branchPaymentRequestDto.ChequeNo ?? string.Empty;
        //        branchPayment.PaymentTo = branchPaymentRequestDto.PaymentTo ?? string.Empty;
        //        branchPayment.ItemCategory = branchPaymentRequestDto.ItemCategory ?? string.Empty;
        //        branchPayment.Particulars = branchPaymentRequestDto.Particulars ?? string.Empty;
        //        branchPayment.Amount = branchPaymentRequestDto.Amount;
        //        branchPayment.IsDeleted = false;
        //        branchPayment.ChequeStatus = branchPayment.ChequeStatus == '\0' ? ' ' : branchPayment.ChequeStatus;
        //        branchPayment.LastUpdate = DateTime.Now;
        //        branchPayment.LastUpdatedBy = branchPaymentRequestDto.userId;

        //        if (isNewPayment)
        //        {
        //            _oBMSDbContext.BranchPayments.Add(branchPayment);
        //        }
        //        else
        //        {
        //            _oBMSDbContext.BranchPayments.Update(branchPayment);
        //        }

        //        // Save header first to get the new ID (if new)
        //        await _oBMSDbContext.SaveChangesAsync();

        //        // Process payment details
        //        if (branchPaymentRequestDto.details != null)
        //        {
        //            foreach (var detail in branchPaymentRequestDto.details.Where(d => d.Amount != 0))
        //            {
        //                var branchPaymentDetails = detail.ID != 0
        //                    ? _oBMSDbContext.BranchPaymentDetails.FirstOrDefault(x => x.ID == detail.ID)
        //                    : new BranchPaymentDetails();

        //                if (branchPaymentDetails == null)
        //                    continue;

        //                branchPaymentDetails.PaymentID = branchPayment.ID;
        //                branchPaymentDetails.Branch = detail.Branch;
        //                branchPaymentDetails.InvoiceID = detail.InvoiceID;
        //                branchPaymentDetails.Amount = detail.Amount;
        //                branchPaymentDetails.LastUpdate = DateTime.Now;
        //                branchPaymentDetails.LastUpdatedBy = branchPaymentRequestDto.userId;
        //                branchPaymentDetails.IsDeleted = false;

        //                if (detail.ID == 0)
        //                    _oBMSDbContext.BranchPaymentDetails.Add(branchPaymentDetails);
        //                else
        //                    _oBMSDbContext.BranchPaymentDetails.Update(branchPaymentDetails);
        //            }

        //            await _oBMSDbContext.SaveChangesAsync();
        //        }

        //        response.Headers.Add("Success", "Successfully saved & updated details.");

        //        return Ok(new Dictionary<string, object> { { "Success", "Success" } });
        //    }
        //    catch (Exception)
        //    {
        //        // Add logging here for better debugging
        //        throw;
        //    }
        //}


        //public int GetNewVoucherNumber(int paymentType, decimal bankId)
        //{
        //    int currentYear = DateTime.Now.Year;

        //    var newVoucherNo = _oBMSDbContext.BranchPayments
        //        .Where(bp => bp.PaymentType == paymentType &&
        //                     (bp.BankID.HasValue ? bp.BankID.Value : 0) == bankId &&
        //                     bp.PaymentDate.Year > 2012)
        //        .Select(bp => bp.VoucherNo)
        //        .Max();

        //    int newVoucherNumber = int.TryParse(newVoucherNo, out int result) ? result + 1 : 1;

        //    return newVoucherNumber;
        //}


        [HttpGet]
        [Route("GetReceiptMaster")]
        public async Task<ActionResult<Object>> GetReceiptMaster(string userID = "")
        {
            try
            {
                var obj = await _financeRepository.GetReceiptMaster(userID);
                return obj;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost]
        [Route("SaveAndUpdateReceipt")]
        //public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateReceipt(ReceiptsRequestDto receiptsRequestDto)
        //{
        //    try
        //    {
        //        var sInvoiceNo = "";
        //        foreach (ReceiptDetailRequestDto detail in receiptsRequestDto.details)
        //        {
        //            if (detail.InvoiceID > 0)
        //            {
        //                var clientInvoice = await _oBMSDbContext.ClientInvoices.Where(x => x.ID == detail.InvoiceID).FirstAsync();
        //                sInvoiceNo = sInvoiceNo + clientInvoice.InvoiceNo + ",";
        //            }
        //        }

        //        var receipt = new Receipts();
        //        receipt.ID = receiptsRequestDto.ID;
        //        var existingReceipt = _oBMSDbContext.Receipts.Where(x => x.ID == receiptsRequestDto.ID && x.IsDeleted == false).FirstOrDefault();
        //        if (receipt.ID == 0)
        //        {
        //            receipt.VoucherNo = UtilityMain.NewReceiptVoucherNoByYear(receiptsRequestDto.BankID, receiptsRequestDto.ReceiptDate.Year);
        //        }
        //        else
        //        {
        //            if (existingReceipt != null)
        //            {
        //                receipt.VoucherNo = existingReceipt.VoucherNo;
        //            }
        //        }
        //        receipt.ReceiptDate = receiptsRequestDto.ReceiptDate;
        //        receipt.Branch = receiptsRequestDto.Branch;
        //        receipt.PaymentFrom = receiptsRequestDto.PaymentFrom;
        //        receipt.Particulars = receiptsRequestDto.Particulars;
        //        receipt.ReceiptType = receiptsRequestDto.ReceiptType;
        //        receipt.IsInvoiceAdjustment = receiptsRequestDto.IsInvoiceAdjustment;
        //        receipt.BankCode = receiptsRequestDto.BankCode;
        //        receipt.BankBranch = receiptsRequestDto.BankBranch;
        //        receipt.ChequeNo = receiptsRequestDto.ChequeNo;
        //        receipt.InvoiceNumbers = sInvoiceNo;
        //        receipt.ReceiptAmount = receiptsRequestDto.ReceiptAmount;
        //        receipt.TaxPercentage = receiptsRequestDto.TaxPercentage;
        //        receipt.TaxAmount = receiptsRequestDto.TaxAmount;
        //        receipt.HQPercentage = receiptsRequestDto.HQPercentage;
        //        receipt.HQAmount = receiptsRequestDto.HQAmount;
        //        receipt.BranchCollection = receiptsRequestDto.BranchCollection;
        //        receipt.CreditNoteAmount = receiptsRequestDto.CreditNoteAmount;
        //        receipt.DebitNoteAmount = receiptsRequestDto.DebitNoteAmount;
        //        receipt.SuspendAmount = receiptsRequestDto.SuspendAmount;
        //        receipt.BankID = receiptsRequestDto.BankID;
        //        receipt.ChequeStatus = receiptsRequestDto.ChequeStatus;
        //        receipt.IsDeleted = false;
        //        receipt.LastUpdate = DateTime.Now;
        //        receipt.LastUpdatedBy = receiptsRequestDto.LastUpdatedBy;

        //        if (receiptsRequestDto.ID == 0)
        //        {
        //            _oBMSDbContext.Receipts.Add(receipt);
        //        }
        //        else
        //        {
        //            _oBMSDbContext.Receipts.Update(receipt);
        //        }

        //        await _oBMSDbContext.SaveChangesAsync();
        //        var savedReceiptId = receipt.ID;

        //        foreach (ReceiptDetailRequestDto detail in receiptsRequestDto.details)
        //        {
        //            var receiptDetail = new ReceiptDetail();
        //            receiptDetail.ID = detail.ID;
        //            receiptDetail.ReceiptID = receipt.ID;
        //            receiptDetail.InvoiceID = detail.InvoiceID;
        //            receiptDetail.Amount = detail.Amount;
        //            receiptDetail.BalanceStatus = detail.BalanceStatus;
        //            receiptDetail.BalanceAmount = detail.BalanceAmount;
        //            receiptDetail.LastUpdate = DateTime.Now;

        //            if (receiptDetail.ID == 0)
        //            {
        //                _oBMSDbContext.ReceiptDetails.Add(receiptDetail);
        //            }
        //            else
        //            {
        //                _oBMSDbContext.ReceiptDetails.Update(receiptDetail);
        //            }

        //            await _oBMSDbContext.SaveChangesAsync();

        //        }
        //        Dictionary<string, object> dictResult = new Dictionary<string, object>();
        //        dictResult.Add("Success", "Success");
        //        dictResult.Add("ReceiptID", savedReceiptId);

        //        response.Headers.Add("Success", "Successfully save & update  details.");

        //        return Ok(dictResult);

        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //}

        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateReceipt(ReceiptsRequestDto receiptsRequestDto)
        {
            try
            {
                var sInvoiceNo = "";
                foreach (ReceiptDetailRequestDto detail in receiptsRequestDto.details)
                {
                    if (detail.InvoiceID > 0)
                    {
                        var clientInvoice = await _oBMSDbContext.ClientInvoices
                            .Where(x => x.ID == detail.InvoiceID)
                            .FirstAsync();

                        sInvoiceNo = sInvoiceNo + clientInvoice.InvoiceNo + ",";
                    }
                }

                Receipts receipt;

                // 🔥 LOAD WITHOUT TRACKING to avoid duplicate tracking
                var existingReceipt = await _oBMSDbContext.Receipts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ID == receiptsRequestDto.ID && x.IsDeleted == false);

                receipt = new Receipts();
                receipt.ID = receiptsRequestDto.ID;

                if (receipt.ID == 0)
                {
                    receipt.VoucherNo = UtilityMain.NewReceiptVoucherNoByYear(
                        receiptsRequestDto.BankID,
                        receiptsRequestDto.ReceiptDate.Year);
                }
                else
                {
                    if (existingReceipt != null)
                    {
                        if (existingReceipt.BankID == receiptsRequestDto.BankID)
                        {
                            receipt.VoucherNo = existingReceipt.VoucherNo;
                        }
                        else
                        {
                            receipt.VoucherNo = UtilityMain.NewReceiptVoucherNoByYear(
                            receiptsRequestDto.BankID,
                            receiptsRequestDto.ReceiptDate.Year);
                        }

                    }
                }

                receipt.ReceiptDate = receiptsRequestDto.ReceiptDate;
                receipt.Branch = receiptsRequestDto.Branch;
                receipt.PaymentFrom = receiptsRequestDto.PaymentFrom;
                receipt.Particulars = receiptsRequestDto.Particulars;
                receipt.ReceiptType = receiptsRequestDto.ReceiptType;
                receipt.IsInvoiceAdjustment = receiptsRequestDto.IsInvoiceAdjustment;
                receipt.BankCode = receiptsRequestDto.BankCode;
                receipt.BankBranch = receiptsRequestDto.BankBranch;
                receipt.ChequeNo = receiptsRequestDto.ChequeNo;
                receipt.InvoiceNumbers = sInvoiceNo;
                receipt.ReceiptAmount = receiptsRequestDto.ReceiptAmount;
                receipt.TaxPercentage = receiptsRequestDto.TaxPercentage;
                receipt.TaxAmount = receiptsRequestDto.TaxAmount;
                receipt.HQPercentage = receiptsRequestDto.HQPercentage;
                receipt.HQAmount = receiptsRequestDto.HQAmount;
                receipt.BranchCollection = receiptsRequestDto.BranchCollection;
                receipt.CreditNoteAmount = receiptsRequestDto.CreditNoteAmount;
                receipt.DebitNoteAmount = receiptsRequestDto.DebitNoteAmount;
                receipt.SuspendAmount = receiptsRequestDto.SuspendAmount;
                receipt.BankID = receiptsRequestDto.BankID;
                receipt.ChequeStatus = receiptsRequestDto.ChequeStatus;
                receipt.IsDeleted = false;
                receipt.LastUpdate = DateTime.Now;
                receipt.LastUpdatedBy = receiptsRequestDto.LastUpdatedBy;

                if (receiptsRequestDto.ID == 0)
                {
                    _oBMSDbContext.Receipts.Add(receipt);
                }
                else
                {
                    _oBMSDbContext.Entry(receipt).State = EntityState.Modified; // ✅ safer than Update()
                }

                await _oBMSDbContext.SaveChangesAsync();
                var savedReceiptId = receipt.ID;

                foreach (ReceiptDetailRequestDto detail in receiptsRequestDto.details)
                {
                    ReceiptDetail receiptDetail;

                    // 🔥 Load without tracking to avoid duplicate tracking
                    var existingDetail = await _oBMSDbContext.ReceiptDetails
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.ID == detail.ID);

                    receiptDetail = new ReceiptDetail();
                    receiptDetail.ID = detail.ID;
                    receiptDetail.ReceiptID = savedReceiptId;
                    receiptDetail.InvoiceID = detail.InvoiceID;
                    receiptDetail.Amount = detail.Amount;
                    receiptDetail.BalanceStatus = detail.BalanceStatus;
                    receiptDetail.BalanceAmount = detail.BalanceAmount;
                    receiptDetail.LastUpdate = DateTime.Now;

                    if (receiptDetail.ID == 0)
                    {
                        _oBMSDbContext.ReceiptDetails.Add(receiptDetail);
                    }
                    else
                    {
                        _oBMSDbContext.Entry(receiptDetail).State = EntityState.Modified; // ✅ avoids tracking clash
                    }

                    await _oBMSDbContext.SaveChangesAsync();
                }

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("ReceiptID", savedReceiptId);

                response.Headers.Add("Success", "Successfully save & update  details.");

                return Ok(dictResult);
            }
            catch (Exception)
            {
                throw;
            }
        }


        [HttpGet]
        [Route("GetReceiptInvoiceByClient")]
        public async Task<ActionResult<Object>> GetReceiptInvoiceByClient(string client, string branch)
        {
            try
            {
                //return await _oBMSDbContext.ReceiptClientInvoiceViews.Where(x => x.Branch == branch).Where(x => x.Client == client).ToListAsync();

                var receiptIdParam = new SqlParameter("@ReceiptID", -1);
                var branchParam = new SqlParameter("@Branch", branch);
                var clientParam = new SqlParameter("@Client", client);

                var parameters = new[]
                 {
                        new Microsoft.Data.SqlClient.SqlParameter("@ReceiptID", -1),
                        new Microsoft.Data.SqlClient.SqlParameter("@Branch", branch),
                        new Microsoft.Data.SqlClient.SqlParameter("@Client", client)
                 };

                return await _oBMSDbContext.ClientReceiptDetailsResults.FromSqlRaw("EXEC GetClientReceiptDetails @ReceiptID, @Branch, @Client", parameters).ToListAsync();



            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("invoice-details")]
        public async Task<List<InvoiceDetailRow>> GetInvoiceDetailList(string branch, string client, int receiptId)
        {
            try
            {
                var sql = @"
                        SELECT 
                            CAST(ReceiptDetails.ID AS INT) AS ID,
                            CAST(ReceiptDetails.ReceiptID AS INT) AS ReceiptID,
                            CAST(ClientInvoice.ID AS INT) AS InvoiceID,
                            ClientInvoice.InvoiceNo,
                            ClientInvoice.ServiceCharges - ClientInvoice.Discount + ClientInvoice.TaxAmount AS InvoiceAmount,
                            InvoicePayments.PaidAmount,
                            ClientInvoice.InvoiceDate,
                            (ClientInvoice.ServiceCharges - ClientInvoice.Discount + ClientInvoice.TaxAmount 
                             - InvoicePayments.PaidAmount) AS BalanceAmount
                        FROM ClientInvoice
                        INNER JOIN ReceiptDetails ON ReceiptDetails.InvoiceID = ClientInvoice.ID
                        INNER JOIN
                        (
                            SELECT InvoiceID, ISNULL(SUM(PaidAmount),0) AS PaidAmount
                            FROM
                            (
                                SELECT InvoiceID,
                                CASE 
                                    WHEN BalanceStatus = 3 THEN Amount
                                    WHEN BalanceStatus = 2 THEN Amount + BalanceAmount
                                    WHEN BalanceStatus = 1 THEN Amount
                                END AS PaidAmount
                                FROM ReceiptDetails RD1
                                INNER JOIN Receipts R 
                                    ON RD1.ReceiptId = R.ID AND R.IsDeleted = 0
                            ) AS ClientPayments
                            GROUP BY InvoiceID
                        ) AS InvoicePayments 
                            ON InvoicePayments.InvoiceID = ClientInvoice.ID
                        WHERE ReceiptDetails.ReceiptID = @ReceiptID
                          AND ClientInvoice.IsDeleted = 'N'

                        UNION

                        SELECT 
                            0 AS ID,
                            0 AS ReceiptID,
                            ClientInvoice.ID AS InvoiceID,
                            ClientInvoice.InvoiceNo,
                            ClientInvoice.ServiceCharges - ClientInvoice.Discount + ClientInvoice.TaxAmount AS InvoiceAmount,
                            ISNULL(InvoicePayments.PaidAmount,0) AS PaidAmount,
                            ClientInvoice.InvoiceDate,
                            (ClientInvoice.ServiceCharges - ClientInvoice.Discount + ClientInvoice.TaxAmount 
                             - ISNULL(InvoicePayments.PaidAmount,0)) AS BalanceAmount
                        FROM ClientInvoice
                        LEFT JOIN
                        (
                            SELECT InvoiceID, ISNULL(SUM(PaidAmount),0) AS PaidAmount
                            FROM
                            (
                                SELECT InvoiceID,
                                CASE 
                                    WHEN BalanceStatus = 3 THEN Amount
                                    WHEN BalanceStatus = 2 THEN Amount + BalanceAmount
                                    WHEN BalanceStatus = 1 THEN Amount
                                END AS PaidAmount
                                FROM ReceiptDetails RD1
                                INNER JOIN Receipts R 
                                    ON RD1.ReceiptId = R.ID AND R.IsDeleted = 0
                                WHERE RD1.InvoiceID NOT IN
                                (
                                    SELECT InvoiceID 
                                    FROM ReceiptDetails 
                                    WHERE ReceiptID = @ReceiptID
                                )
                            ) AS ClientPayments
                            GROUP BY InvoiceID
                        ) AS InvoicePayments 
                            ON InvoicePayments.InvoiceID = ClientInvoice.ID
                        WHERE ClientInvoice.ID NOT IN
                              (SELECT InvoiceID FROM ReceiptDetails WHERE ReceiptID = @ReceiptID)
                          AND ClientInvoice.ServiceCharges - ClientInvoice.Discount + ClientInvoice.TaxAmount
                              - ISNULL(InvoicePayments.PaidAmount,0) > 0
                          AND ClientInvoice.Branch = @Branch
                          AND ClientInvoice.Client = @Client
                          AND ClientInvoice.IsDeleted = 'N'
                        ORDER BY InvoiceNo";

                var parameters = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@ReceiptID", receiptId),
                    new Microsoft.Data.SqlClient.SqlParameter("@Branch", branch),
                    new Microsoft.Data.SqlClient.SqlParameter("@Client", client)
                };

                return await _oBMSDbContext.InvoiceDetailRows
                        .FromSqlRaw(sql, parameters)
                        .ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("GetClientByBranch")]
        public async Task<ActionResult<Object>> GetClientByBranch(string branch)
        {
            try
            {
                return await _oBMSDbContext.ClientMasters.Where(x => x.Status != "Inactive" && x.Branch == branch).OrderBy(x=> x.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("CheckUniqueChequeNoWithReceiptsNotDeleted")]
        public async Task<ActionResult<Object>> CheckUniqueChequeNoWithReceiptsNotDeleted(string Bank, string BankBranch, string ChequeNo)
        {
            try
            {

                return await _oBMSDbContext.Receipts.Where(x => x.BankCode == Bank).Where(x => x.BankBranch == BankBranch).Where(x => x.ChequeNo == ChequeNo).Where(x => x.IsDeleted == false).ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("GetReceiptDetailRowAmount")]
        public async Task<ActionResult<Object>> GetReceiptDetailRowAmount(decimal ReceiptDetailsID, decimal ReceiptID, decimal InvoiceID)
        {
            try
            {
                return await _oBMSDbContext.ReceiptDetails.Where(x => x.ID == ReceiptDetailsID).Where(x => x.ReceiptID == ReceiptID).Where(x => x.InvoiceID == InvoiceID).ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpGet("GetSuppliers")]
        public async Task<IActionResult> GetSuppliers([FromQuery] string? category)
        {
            try
            {
                var suppliers = await _oBMSDbContext.Suppliers
                    .Where(s => string.IsNullOrEmpty(category) || (s.Category == category && s.Status == "A"))
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Code
                    })
                    .ToListAsync();

                var supplierList = new List<object>
            {
                new { Id = 0, Name = "", Code = "" }
            };

                supplierList.AddRange(suppliers);

                return Ok(supplierList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetInventoryCategories")]
        public async Task<IActionResult> GetInventoryCategories([FromQuery] string? cat)
        {
            try
            {
                var query = _oBMSDbContext.InventoryCategories.AsQueryable();

                if (!string.IsNullOrEmpty(cat))
                {
                    query = query.Where(ic => ic.Cat == cat);
                }

                var inventoryCategoryFactoryList = await query
                    .OrderBy(ic => ic.Name)
                    .Select(ic => new InventoryCategory
                    {
                        ID = ic.ID,
                        Name = ic.Cat == "P" ? ic.Name + " (Purchase)" : ic.Name + " (Expenses)",
                        Cat = ic.Cat,
                        AssetType = ic.AssetType
                    })
                    .ToListAsync();

                var inventoryCategoryList = new List<InventoryCategoryDto>
            {
                new InventoryCategoryDto(0, 0, "", 0, "", "", "")
            };

                inventoryCategoryList.AddRange(inventoryCategoryFactoryList.Select(icf => new InventoryCategoryDto(
                    icf.ID,
                    0, // AccountCategoryID placeholder
                    "", // AccountNo placeholder
                    0, // AccountTypeID placeholder
                    icf.Name,
                    icf.Cat,
                    icf.AssetType
                )));

                return Ok(inventoryCategoryList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("monthly-invoices")]
        public async Task<IActionResult> GetMonthlyInvoices(DateTime invoiceStartPeriod, DateTime invoiceEndPeriod)
        {
            try
            {
                var invoices = await _financeRepository.GetMonthlyInvoiceList(invoiceStartPeriod, invoiceEndPeriod);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving invoices.", details = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetMonthlyInvoiseList")]
        public IActionResult GetMonthlyInvoiseList(DateTime Start, DateTime End)
        {
            try
            {
                var result = UtilityMain.GetMonthlyInvoiseList(Start, End);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetList")]
        public IActionResult GetList(DateTime dtSalaryPeriod, DateTime dtEndPeriod)
        {
            try
            {
                var result = SumProfitLoss.GetList(dtSalaryPeriod, dtEndPeriod);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetListWithBranch")]
        public IActionResult GetListWithBranch(DateTime dtSalaryPeriod, DateTime dtEndPeriod, string Branch)
        {
            try
            {
                var result = SumProfitLoss.GetList(dtSalaryPeriod, dtEndPeriod, Branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetByDateAndBranch")]
        public async Task<IActionResult> GetByDateAndBranch(DateTime receiptDate, string branch)
        {
            var receipts = await _financeRepository.GetReceiptsByDateAndBranchAsync(receiptDate, branch);
            return Ok(receipts);
        }

        [HttpGet("GetByBankAndCheque")]
        public async Task<IActionResult> GetByBankAndCheque(string bankCode, string chequeNo, string branch)
        {
            var receipts = await _financeRepository.GetReceiptsByBankAndChequeAsync(bankCode, chequeNo, branch);
            return Ok(receipts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReceipt(int id)
        {
            var receipt = await _financeRepository.GetReceiptAsync(id);

            if (receipt == null)
                return NotFound();

            return Ok(receipt);
        }

        [HttpGet("byDate")]
        public async Task<IActionResult> GetListByDate(DateTime paymentDate)
        {
            try
            {
                var result = await _financeRepository.GetListByDateAsync(paymentDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("byBankAndCheque")]
        public async Task<IActionResult> GetListByBankAndCheque(decimal bankId, string chequeNo)
        {
            try
            {
                var result = await _financeRepository.GetListByBankAndChequeAsync(bankId, chequeNo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("byPaymentID/{id}")]
        public async Task<IActionResult> GetBranchPayment(int id)
        {
            var branchPayment = await _financeRepository.GetBranchPaymentAsync(id);
            if (branchPayment == null)
            {
                return NotFound();
            }
            return Ok(branchPayment);
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] DeletePaymentRequest request)
        {
            var result = await _financeRepository.DeleteAsync(request.Id, request.CurrentUser);

            if (!result)
                return NotFound(new { message = "Payment not found." });

            return Ok(result);
        }

        [HttpPost]
        [Route("DeletePaymentLine")]
        public async Task<IActionResult> DeletePaymentLine([FromBody] DeletePaymentRequest request)
        {
            try
            {
                var detail = await _oBMSDbContext.BranchPaymentDetails
                    .FirstOrDefaultAsync(x => x.ID == request.Id);

                if (detail == null)
                    return NotFound(new { message = "Payment line not found." });

                detail.IsDeleted = true;
                detail.LastUpdate = DateTime.Now;
                detail.LastUpdatedBy = request.CurrentUser;

                _oBMSDbContext.BranchPaymentDetails.Update(detail);
                await _oBMSDbContext.SaveChangesAsync();

                return Ok(new { message = "Payment line deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("receipt/delete")]
        public async Task<IActionResult> ReceiptDelete([FromBody] DeletePaymentRequest request)
        {
            var result = await _financeRepository.ReceiptDeleteAsync(request.Id, request.CurrentUser);

            if (!result)
                return NotFound(new { message = "Payment not found." });

            return Ok(result);
        }

        [HttpGet]
        [Route("GetNextChequeNumber")]
        public IActionResult GetNextChequeNumber(decimal account)
        {
            try
            {
                var result = UtilityMain.GetNextChequeNumber(account);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetNoOfCheques")]
        public IActionResult GetNoOfCheques(decimal account)
        {
            try
            {
                var result = UtilityMain.GetNoOfCheques(account);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("InventoryCategoryList")]
        public async Task<ActionResult<List<InventoryCategoryDto>>> GetList()
        {
            try
            {
                var result = await _financeRepository.GetListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Optionally log the exception here
                return StatusCode(500, new { Message = "An error occurred while retrieving the inventory categories.", Details = ex.Message });
            }
        }

        [HttpGet("PaymentRecycleBin")]
        public IActionResult GetDeletedPayments(DateTime paymentDate)
        {
            try
            {
                var results = _financeRepository.GetDeletedPaymentsByMonthYear(paymentDate);
                return Ok(results);
            }
            catch (Exception ex)
            {
                // You can log the exception here using a logging framework if needed
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while retrieving deleted payments.",
                    Details = ex.Message
                });
            }
        }

        [HttpPost("restore-multiple")]
        public IActionResult RestoreMultiple([FromBody] RestoreMultipleRequest request)
        {
            if (request == null || request.Ids == null || !request.Ids.Any() || string.IsNullOrEmpty(request.CurrentUser))
                return BadRequest("Invalid request");

            var results = new Dictionary<int, bool>();

            try
            {
                foreach (var id in request.Ids)
                {
                    bool success = _financeRepository.Restore(id, request.CurrentUser);
                    results.Add(id, success);
                }

                return Ok(new { message = "Restore operation completed.", results });
            }
            catch (Exception ex)
            {
                // Optionally log the exception here
                return StatusCode(500, new { message = "An error occurred while restoring payments.", error = ex.Message });
            }
        }

        [HttpGet("cheque-status")]
        public async Task<IActionResult> Get(DateTime startDate, DateTime endDate, string chequeStatus, string bankCode, string transType)
        {
            try
            {
                var data = await _financeRepository.GetChequeStatusesAsync(startDate, endDate, chequeStatus, bankCode, transType);
                return Ok(data);
            }
            catch (Exception ex)
            {
                // Optionally log the exception here
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while fetching cheque statuses.",
                    Details = ex.Message
                });
            }
        }

        [HttpPost("restore-chequeStatus")]
        public async Task<IActionResult> RestoreChequeStatus([FromBody] RestoreChequeStatusRequest request)
        {
            if (request == null || request.Ids == null || !request.Ids.Any() || string.IsNullOrEmpty(request.CurrentUser))
                return BadRequest("Invalid request");

            var results = new Dictionary<int, bool>();

            try
            {
                foreach (var id in request.Ids)
                {
                    if (request.transType == "R")
                    {
                        var receipt = await _oBMSDbContext.Receipts
                            .Where(p => p.ID == id && !p.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (receipt != null)
                        {
                            receipt.ChequeStatus = request.chequeStatus;
                            receipt.LastUpdate = DateTime.Now;
                            receipt.LastUpdatedBy = request.CurrentUser;
                            results[id] = true;
                        }
                        else
                        {
                            results[id] = false;
                        }
                    }
                    else // Assume transType == "P"
                    {
                        var branchPayment = await _oBMSDbContext.BranchPayments
                            .Where(p => p.ID == id && !p.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (branchPayment != null)
                        {
                            branchPayment.ChequeStatus = request.chequeStatus;
                            branchPayment.ChqClearencedate = request.clearence_date;
                            branchPayment.LastUpdate = DateTime.Now;
                            branchPayment.LastUpdatedBy = request.CurrentUser;
                            results[id] = true;
                        }
                        else
                        {
                            results[id] = false;
                        }
                    }
                }

                await _oBMSDbContext.SaveChangesAsync();

                return Ok(new { message = "Restore operation completed.", results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while restoring payments.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetLegalDemandList")]
        public async Task<IActionResult> GetList([FromQuery] string branch = null, [FromQuery] string client = null, [FromQuery] string actionTaken = null)
        {

            try
            {
                var result = await _financeRepository.GetActionsAsync(branch, client, actionTaken);
                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("saveOrUpdateLegalDemand")]
        public async Task<IActionResult> SaveOrUpdateLegalDemand([FromBody] ClientLegalDemandAction request)
        {
            try
            {
                var legalDemand = new ClientLegalDemandAction();

                if (request.ID != 0)
                {
                    legalDemand = _oBMSDbContext.ClientLegalDemandActions
                        .Where(x => x.ID == request.ID)
                        .Select(x => new ClientLegalDemandAction
                        {
                            ID = x.ID,
                            Branch = x.Branch ?? string.Empty,
                            Client = x.Client ?? string.Empty,
                            ActionTaken = x.ActionTaken ?? string.Empty,
                            DateIssue = x.DateIssue,
                            Remarks = x.Remarks ?? string.Empty,
                            DeletionRemarks = x.DeletionRemarks ?? string.Empty,
                            IsDeleted = x.IsDeleted,
                            CreatedDate = x.CreatedDate,
                            CreatedBy = x.CreatedBy ?? string.Empty,
                            UpdatedDate = x.UpdatedDate,
                            UpdatedBy = x.UpdatedBy ?? string.Empty
                        })
                        .FirstOrDefault();
                }

                // Assign values from request to entity
                legalDemand.Branch = request.Branch;
                legalDemand.Client = request.Client;
                legalDemand.ActionTaken = request.ActionTaken;
                legalDemand.DateIssue = request.DateIssue;
                legalDemand.Remarks = request.Remarks;
                legalDemand.DeletionRemarks = request.DeletionRemarks ?? string.Empty;
                legalDemand.IsDeleted = false;

                if (legalDemand.ID == 0)
                {
                    legalDemand.CreatedDate = DateTime.Now;
                    legalDemand.CreatedBy = request.CreatedBy;
                }

                legalDemand.UpdatedDate = DateTime.Now;
                legalDemand.UpdatedBy = request.UpdatedBy;


                if (legalDemand.ID == 0)
                {
                    _oBMSDbContext.ClientLegalDemandActions.Add(legalDemand);
                }
                else
                {
                    _oBMSDbContext.ClientLegalDemandActions.Update(legalDemand);
                }

                await _oBMSDbContext.SaveChangesAsync();

                Dictionary<string, object> result = new Dictionary<string, object>();
                result.Add("Success", "Success");

                Response.Headers.Add("Success", "Successfully saved or updated legal demand action.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                throw; // You can log ex here or return a 500 error response
            }
        }

        [HttpPost("DeleteLegalDemand/{id}")]
        public async Task<IActionResult> DeleteItem(int id, string currentUser, string deleteRemarks)
        {
            try
            {
                var result = await _financeRepository.DeleteLegalDemandAsync(id, currentUser, deleteRemarks);

                if (result)
                    return Ok(new { success = true, message = "Item deleted (soft delete)" });

                return NotFound(new { success = false, message = "Item not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting the item.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetLegalDemandByID/{id}")]
        public async Task<IActionResult> GetLegalDemandByID(int id)
        {
            try
            {
                var result = await _financeRepository.GetLegalDemandByID(id);

                if (result != null)
                    return Ok(result);

                return NotFound(new { success = false, message = "Item not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while getting the item.",
                    error = ex.Message
                });
            }
        }


        [HttpGet("GetPaymentListByCategory")]
        public IActionResult GetPaymentListByCategory(string categoryId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Call your static method (assuming it’s inside some Repository/Service)
                var result = UtilityMain.GetPaymentListByCategory(categoryId, startDate, endDate);

                if (result == null || !result.Any())
                {
                    return Ok(new { message = "No records found for the given criteria.", data = new List<BranchPaymentsDto>() });
                }

                return Ok(new { message = "Success", data = result });
            }
            catch (SqlException sqlEx)
            {
                // Handle DB-specific exceptions
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                // Handle general exceptions
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("GetBranchPaymentsTotalAmountByCategory")]
        public IActionResult GetBranchPaymentsTotalAmountByCategory(string categoryId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var totalAmount = UtilityMain.GetBranchPaymentsTotalAmountByCategory(categoryId, startDate, endDate);

                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                // Log exception here if needed
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching total amount.", error = ex.Message });
            }
        }

        [HttpGet("NewClientInvoiceNo")]
        public IActionResult NewClientInvoiceNo(string branchCode, DateTime invoiceDate)
        {
            try
            {
                string newInvoiceNo = UtilityMain.NewClientInvoiceNo(branchCode, invoiceDate);
                return Ok(new { InvoiceNo = newInvoiceNo });
            }
            catch (Exception ex)
            {
                // Optionally log the exception here
                return StatusCode(500, new { Message = "Error generating invoice number", Details = ex.Message });
            }
        }

        [HttpGet("GetListByBranchAndInvoice")]
        public async Task<ActionResult<List<ClientInvoiceDetailsDto>>> GetListByBranchAndInvoice(string branch, string invoiceNo)
        {
            try
            {
                var sqlQuery = @"
                            SELECT  
                                A.ID AS InvoiceID,
                                B.*
                            FROM ClientInvoice A
                            INNER JOIN ClientInvoiceDetails B ON A.ID = B.ClientInvoiceID
                            WHERE A.InvoiceNo = @InvoiceNo 
                              AND A.Branch = @Branch";

                var parameters = new[]
                {
                            new Microsoft.Data.SqlClient.SqlParameter("@Branch", branch),
                            new Microsoft.Data.SqlClient.SqlParameter("@InvoiceNo", invoiceNo)
                        };

                List<ClientInvoiceDetailsDto> list = new();

                // ⭐ Get database connection
                var connection = _oBMSDbContext.Database.GetDbConnection();

                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;

                    foreach (var p in parameters)
                        command.Parameters.Add(p);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new ClientInvoiceDetailsDto
                            {
                                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                ClientInvoiceID = reader.GetDecimal(reader.GetOrdinal("ClientInvoiceID")),
                                AgreementDetailID = reader.GetDecimal(reader.GetOrdinal("AgreementDetailID")),
                                AgreementID = reader.GetDecimal(reader.GetOrdinal("AgreementID")),
                                AgreementDate = reader.GetDateTime(reader.GetOrdinal("AgreementDate")),
                                NoOfGuards = reader.GetInt32(reader.GetOrdinal("NoOfGuards")),
                                Rate = reader.GetDecimal(reader.GetOrdinal("Rate")),
                                NoOfHours = reader.GetDecimal(reader.GetOrdinal("NoOfHours")),
                                NoOfDays = reader.GetDecimal(reader.GetOrdinal("NoOfDays")),
                                FollowCalender = reader.GetBoolean(reader.GetOrdinal("FollowCalender")),
                                MonthTotal = reader.GetDecimal(reader.GetOrdinal("MonthTotal")),
                                HasDiscount = reader.GetBoolean(reader.GetOrdinal("HasDiscount")),
                                DiscountAmount = reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
                                IsTaxable = reader.GetBoolean(reader.GetOrdinal("IsTaxable")),
                                TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                                LASTUPDATE = reader.GetDateTime(reader.GetOrdinal("LASTUPDATE"))
                            });
                        }
                    }
                }

                await connection.CloseAsync();

                return list;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("client-statement")]
        public IActionResult GenerateClientStatement([FromBody] ClientStatementRequest request)
        {
            try
            {
                UtilityMain.GenerateClientStatement(request.StartDate, request.EndDate, request.Branch, request.Client);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Failed to generate client statement."
                });
            }
        }
        [HttpPost("supplier-report")]
        public IActionResult ExecuteSupplierReport([FromBody] ClientActivityReportRequest request)
        {
            if (string.IsNullOrEmpty(request.Branch) || request.StartDate == default || request.EndDate == default)
            {
                return BadRequest("Branch, Start Date, and End Date are mandatory.");
            }

            try
            {
                UtilityMain.ExecuteSupplierReport(request.StartDate, request.EndDate, request.Branch, request.PayTo, request.Supplier, request.Status, request.Category);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Returns payment transactions grouped and accumulated by Voucher Number.
        /// GET api/finance/GetBranchPaymentVoucherSummary?startDate=2024-01-01&endDate=2024-12-31&branch=HQ
        /// branch is optional — omit or pass empty string for all branches.
        /// </summary>
        [HttpGet]
        [Route("GetBranchPaymentVoucherSummary")]
        public async Task<ActionResult<List<BranchPaymentVoucherSummaryDto>>> GetBranchPaymentVoucherSummary(
            DateTime? startDate, DateTime? endDate, string branch = "")
        {
            try
            {
                var result = await _financeRepository.GetBranchPaymentVoucherSummary(startDate, endDate, branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// Returns the Separated Profit &amp; Loss report split into two sections:
        ///   Section 1 - Operational P&amp;L (Sales/Income and Expenses only).
        ///   Section 2 - Non-Operational Transactions (Contra, BU transfers, Internal, Adjustments).
        /// </summary>
        [HttpGet]
        [Route("GetSeparatedProfitLoss")]
        public ActionResult<SeparatedProfitLossResponseDto> GetSeparatedProfitLoss(
            DateTime startDate, DateTime endDate, string branch = "")
        {
            try
            {
                var operationalRows = string.IsNullOrWhiteSpace(branch)
                    ? SeparatedProfitLoss.GetList(startDate, endDate)
                    : SeparatedProfitLoss.GetList(startDate, endDate, branch);

                var operationalDtos = operationalRows.Select(x => new SeparatedProfitLossDto
                {
                    Month = x.Month,
                    OperationalIncome = x.OperationalIncome,
                    OperationalCN = x.OperationalCN,
                    OperationalDiscount = x.OperationalDiscount,
                    OperationalExpenses = x.OperationalExpenses,
                    OperationalProfit = x.OperationalProfit
                }).ToList();

                var nonOperationalRows = string.IsNullOrWhiteSpace(branch)
                    ? NonOperationalTransaction.GetList(startDate, endDate)
                    : NonOperationalTransaction.GetList(startDate, endDate, branch);

                var nonOperationalDtos = nonOperationalRows.Select(t => new NonOperationalTransactionDto
                {
                    Month = t.Month,
                    Branch = t.Branch,
                    ItemCategory = t.ItemCategory,
                    PaymentPurpose = t.PaymentPurpose,
                    Amount = t.Amount,
                    TransactionNature = t.TransactionNature,
                    TransactionDate = t.TransactionDate
                }).ToList();

                var result = new SeparatedProfitLossResponseDto
                {
                    OperationalPnL = operationalDtos,
                    NonOperationalTransactions = nonOperationalDtos
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

    }
}

[Keyless]
public class ClientInvoiceNoResult
{
    public int NEWCLIENTINVOICENO { get; set; }
}

public class ClientMasterResult
{
    [Key]
    public string Code { get; set; }
    public string Branch { get; set; }
    public string Name { get; set; }

    public int ID { get; set; }
    public string InvoiceNo { get; set; }
}

public class RestoreInvoiceListRequest
{
    public int[]? ID { get; set; }
}

public class BatchInvoice
{
    public string Code { get; set; }
    public string Branch { get; set; }
    public string Name { get; set; }
    public int ID { get; set; }
    public string InvoiceNo { get; set; }
    public string? WorkPlace { get; set; }
    public int? AgreementID { get; set; }

    public Object data { get; set; }
}

[Table("PayToView")]
[Keyless]
public class PayToView
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int Category { get; set; }
}

[Table("SupplierInvoiceView")]
[Keyless]
public class SupplierInvoiceView
{
    public int? ID { get; set; }
    public decimal? PaymentID { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? InvoiceID { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal? Total { get; set; }
    public decimal? Amount { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? Balance { get; set; }
    public string? BranchUserName { get; set; }
    public int? Supplier { get; set; }
}

[Keyless]
public class ClientReceiptDetailsResult
{
    public int ID { get; set; }
    public decimal ReceiptID { get; set; }
    public int InvoiceID { get; set; }
    public string InvoiceNo { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime InvoiceDate { get; set; }
}

public class DeletePaymentRequest
{
    public int Id { get; set; }
    public string CurrentUser { get; set; }
}

public class RestoreMultipleRequest
{
    public List<int> Ids { get; set; }
    public string CurrentUser { get; set; }
}

public class RestoreChequeStatusRequest
{
    public List<int> Ids { get; set; }
    public string CurrentUser { get; set; }
    public string transType { get; set; }
    public char chequeStatus { get; set; }
    public DateTime clearence_date { get; set; }
}

public class ClientStatementRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Branch { get; set; }
    public string Client { get; set; }
}

public class ClientActivityReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Branch { get; set; } = string.Empty;
    public decimal Supplier { get; set; }
    public decimal PayTo { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
