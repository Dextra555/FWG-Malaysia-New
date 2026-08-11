using BoldReports.Processing.ObjectModels;
using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.BusinessObjects;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using SkiaSharp;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;

namespace OBMS.WebAPI.Repositories.Implementation
{
    public class FinanceRepository : IFinanceRepository
    {
        private readonly OBMSDbContext _oBMSDbContext;
        private readonly IRegisterRepository _registerRepository;

        public FinanceRepository(OBMSDbContext oBMSDbContext, IRegisterRepository registerRepository)
        {
            _oBMSDbContext = oBMSDbContext;
            _registerRepository = registerRepository;
        }
        public async Task<Dictionary<string, Object>> GetInvoiceMaster(string userID)
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


            results.Add("branchList", branchs);

            return results;
        }
        public async Task<Dictionary<string, Object>> GetPaymentMaster(string userID)
        {
            bool isSuperAdmin = userID.Equals("superadmin", StringComparison.OrdinalIgnoreCase);
            var results = new Dictionary<string, Object>();

            var suppliers = await _oBMSDbContext.Suppliers.Where(s => s.Status == "A").OrderBy(s => s.Name).ToListAsync();
            results.Add("suppliers", suppliers);

            // 🔹 Bank List (Superadmin sees all)
            var banks = await (
                from bankMaster in _oBMSDbContext.BankMasters
                join bankList in _oBMSDbContext.BankLists
                    on bankMaster.BankCode equals bankList.BankCode
                join obmsBanks in _oBMSDbContext.OBMSBanks
                    on bankMaster.BankId equals obmsBanks.BankID
                where isSuperAdmin || (obmsBanks.IsAllowed == true && obmsBanks.Name == userID)
                orderby bankMaster.Accname
                select new
                {
                    bankMaster.BankId,
                    bankMaster.BankCode,
                    bankMaster.Accname,
                    bankMaster.Accno,
                    bankMaster.AccShortName,
                    bankMaster.PREFIX,
                    bankMaster.LASTUPDATE
                }
            )
            .Distinct()
            .ToListAsync();

            results.Add("banks", banks);

            var category = await _oBMSDbContext.InventoryCategories.Where(s => s.Cat != "P").OrderBy(c => c.Name).ToListAsync();
            results.Add("categories", category);

            var obj2 = await _oBMSDbContext.BranchPaymentForBranchs.Where(x => x.BName == "superuser").OrderBy(x => x.Name).ToListAsync();

            results.Add("other", obj2);




            //   results.Add("resultN", GetList(userID,0,3));

            //          var sqlQuery = @"
            //  SELECT 
            //      BranchPaymentDetails.ID,
            //      BranchPaymentDetails.PaymentID,
            //      BranchMaster.Code,
            //      BranchMaster.Name,
            //      CreditorInvoice.ID AS InvoiceID,
            //      CreditorInvoice.InvoiceNo,
            //      CreditorInvoice.Total,
            //      ISNULL(BranchPaymentDetails.Amount, 0) AS Amount,
            //      ISNULL(Payments.PaidAmount, 0) AS PaidAmount
            //  FROM 
            //      CreditorInvoice
            //  INNER JOIN 
            //      OBMSBranches ON OBMSBranches.BranchCode = CreditorInvoice.Branch
            //  INNER JOIN 
            //      BranchMaster ON BranchMaster.Code = OBMSBranches.BranchCode
            //  LEFT OUTER JOIN 
            //      BranchPaymentDetails ON BranchPaymentDetails.InvoiceID = CreditorInvoice.ID 
            //                            AND BranchPaymentDetails.PaymentID = 0 
            //                            AND BranchPaymentDetails.IsDeleted = 0
            //  LEFT OUTER JOIN 
            //      (SELECT InvoiceID, SUM(Amount) AS PaidAmount 
            //       FROM BranchPaymentDetails 
            //       WHERE InvoiceID <> 0 AND IsDeleted = 0 
            //       GROUP BY InvoiceID) Payments ON Payments.InvoiceID = CreditorInvoice.ID
            //  WHERE 
            //      OBMSBranches.Name = @UserID AND Supplier = @Supplier";

            //          var parameters = new object[]
            //          {
            //  new Microsoft.Data.SqlClient.SqlParameter("@UserID", userID),
            //  new Microsoft.Data.SqlClient.SqlParameter("@Supplier", 3),
            ////  new Microsoft.Data.SqlClient.SqlParameter("@PaymentID", 0M)
            //          };

            //          var result = _oBMSDbContext.BranchPaymentRows
            //                              .FromSqlRaw(sqlQuery, parameters)
            //                              .ToList();

            //          results.Add("resultNew", result);

            //var result = _oBMSDbContext.BranchPaymentRows.FromSqlRaw(
            //    @"SELECT BranchPaymentDetails.ID, BranchPaymentDetails.PaymentID, BranchMaster.Code, BranchMaster.Name, 
            //    CreditorInvoice.ID AS InvoiceID, CreditorInvoice.InvoiceNo, CreditorInvoice.Total, ISNULL(BranchPaymentDetails.Amount, 0) AS Amount, 
            //    ISNULL(Payments.PaidAmount, 0) AS PaidAmount, CreditorInvoice.PaymentDate AS PaymentDate
            //    FROM CreditorInvoice 
            //    INNER JOIN OBMSBranches ON OBMSBranches.BranchCode = CreditorInvoice.Branch 
            //    INNER JOIN BranchMaster ON BranchMaster.Code = OBMSBranches.BranchCode 
            //    LEFT OUTER JOIN BranchPaymentDetails ON BranchPaymentDetails.InvoiceID = CreditorInvoice.ID
            //    AND BranchPaymentDetails.PaymentID = 0 AND BranchPaymentDetails.IsDeleted = 0 
            //    LEFT OUTER JOIN (SELECT InvoiceID, SUM(Amount) AS PaidAmount FROM BranchPaymentDetails WHERE InvoiceID <> 0 AND IsDeleted = 0 GROUP BY InvoiceID) Payments 
            //    ON Payments.InvoiceID = CreditorInvoice.ID 
            //    WHERE OBMSBranches.Name = @UserID AND Supplier = @Supplier 
            //    AND CreditorInvoice.Total - ISNULL(Payments.PaidAmount, 0) > 0",
            //    new Microsoft.Data.SqlClient.SqlParameter("@UserID", userID),
            //    new Microsoft.Data.SqlClient.SqlParameter("@Supplier", 3)
            //).Select(r => new BranchPaymentRow
            //{
            //    ID = r.ID,
            //    PaymentID = r.PaymentID,
            //    Code = r.Code,
            //    Name = r.Name,
            //    InvoiceID = r.InvoiceID,
            //    InvoiceNo = r.InvoiceNo,
            //    Total = r.Total,
            //    Amount = r.Amount != null ? r.Amount : 0m, // Use 0m for decimal
            //    PaidAmount = r.PaidAmount != null ? r.PaidAmount : 0m, // Use 0m for decimal
            //    PaymentDate = r.PaymentDate // Assuming PaymentDate is a DateTime property in BranchPaymentRow
            //}).ToList();


            //            results.Add("resultNew", result);

            return results;
        }
        public List<BranchPaymentRow> GetList(string userID, decimal paymentID, decimal supplier)
        {
            try
            {
                var result = (from creditorInvoice in _oBMSDbContext.CreditorInvoices
                              join branch in _oBMSDbContext.OBMSBranches on creditorInvoice.Branch equals branch.BranchCode
                              join branchMaster in _oBMSDbContext.BranchMasters on branch.BranchCode equals branchMaster.Code
                              join paymentDetails in _oBMSDbContext.BranchPaymentDetails
                                    on creditorInvoice.ID equals paymentDetails.InvoiceID into paymentDetailsGroup
                              from pd in paymentDetailsGroup.Where(pd => pd.PaymentID == paymentID).DefaultIfEmpty()
                              join payments in (from pd2 in _oBMSDbContext.BranchPaymentDetails
                                                where pd2.InvoiceID != 0 && pd2.IsDeleted == false
                                                group pd2 by pd2.InvoiceID into g
                                                select new { InvoiceID = g.Key, PaidAmount = g.Sum(pd2 => pd2.Amount) })
                                  on (decimal?)creditorInvoice.ID equals payments.InvoiceID into paymentsGroup
                              from pg in paymentsGroup.DefaultIfEmpty()
                              where branch.Name == userID && creditorInvoice.Supplier == supplier
                              orderby creditorInvoice.PaymentDate
                              select new BranchPaymentRow
                              {
                                  ID = pd != null ? pd.ID : 0,
                                  PaymentID = pd != null ? pd.PaymentID : 0,
                                  Code = branchMaster.Code,
                                  Name = branchMaster.Name,
                                  InvoiceID = creditorInvoice.ID,
                                  InvoiceNo = creditorInvoice.InvoiceNo,
                                  Total = creditorInvoice.Total,
                                  Amount = pd != null ? pd.Amount : 0,
                                  PaidAmount = pg != null ? pg.PaidAmount : 0
                              }).ToList();

                return result;
            }
            catch
            {
                throw;
            }
        }
        public async Task<Dictionary<string, Object>> GetInvoiceClientByBranchAndInvoiceDate(string branchId1, DateTime invoiceDate)
        {
            var results = new Dictionary<string, Object>();
            //1 Client List
            //2 Invoice No

            var invoiceMonth = invoiceDate.Month;
            var invoiceYear = invoiceDate.Year;


            // Assuming _oBMSDbContext is an instance of your DbContext
            var branchId = "HQ";
            var startDate = new DateTime(2016, 4, 30);
            var endDate = startDate.AddMonths(1).AddSeconds(-1);
            var terminationDate = new DateTime(2023, 1, 12);

            var sqlQuery = $@"
    SELECT cm.Code, cm.Branch, cm.Name, ISNULL(inv.ID, 0) AS ID, ISNULL(inv.InvoiceNo, '') AS InvoiceNo
    FROM ClientMaster cm
    LEFT JOIN (
        SELECT ci.Branch, ci.Client, ci.ID, ci.InvoiceNo
        FROM ClientInvoice ci
        WHERE ci.IsDeleted = 'N' AND MONTH(ci.InvoiceDate) = 12 AND YEAR(ci.InvoiceDate) = 2023
    ) inv ON cm.Branch = inv.Branch AND cm.Code = inv.Client
    WHERE cm.Branch = @BranchId AND cm.Code IN (
        SELECT a.Client
        FROM Agreement a
        WHERE a.Branch = @BranchId AND (
            a.AgreementEndDate >= @StartDate OR 
            DATEADD(s, -1, DATEADD(mm, DATEDIFF(m, 0, @StartDate) + 1, 0)) >= a.AgreementEndDate
        ) AND a.Client NOT IN (
            SELECT ta.Client
            FROM TerminatedAgreements ta
            WHERE ta.Branch = @BranchId AND ta.TerminationDate < @TerminationDate
        )
    )
";

            var parameters = new object[]
            {
    new SqlParameter("@BranchId", branchId),
    new SqlParameter("@StartDate", startDate),
    new SqlParameter("@TerminationDate", terminationDate)
            };


            var result = _oBMSDbContext.Database.SqlQueryRaw<QueryResult>(sqlQuery, parameters).ToList();


            var branchs = _oBMSDbContext.BranchMasters.ToList();

            results.Add("branchList", branchs);

            results.Add("result", result);

            return results;
        }
        public async Task<Dictionary<string, object>> GetReceiptMaster(string userID)
        {
            bool isSuperAdmin = userID.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

            var results = new Dictionary<string, object>();

            // 🔹 Branch List (Superadmin sees all)
            var branchs = await (
                from branchMaster in _oBMSDbContext.BranchMasters
                join obmsBranches in _oBMSDbContext.OBMSBranches
                    on branchMaster.Code equals obmsBranches.BranchCode
                where isSuperAdmin || obmsBranches.Name == userID
                orderby branchMaster.Name
                select new
                {
                    branchMaster.Code,
                    branchMaster.Name
                }
            )
            .Distinct()
            .ToListAsync();

            results.Add("branchList", branchs);

            // 🔹 Bank List (Superadmin sees all)
            var banks = await (
                from bankMaster in _oBMSDbContext.BankMasters
                join bankList in _oBMSDbContext.BankLists
                    on bankMaster.BankCode equals bankList.BankCode
                join obmsBanks in _oBMSDbContext.OBMSBanks
                    on bankMaster.BankId equals obmsBanks.BankID
                where isSuperAdmin || (obmsBanks.IsAllowed == true && obmsBanks.Name == userID)
                orderby bankMaster.Accname
                select new
                {
                    bankMaster.BankId,
                    bankMaster.BankCode,
                    bankMaster.Accname,
                    bankMaster.Accno,
                    bankMaster.AccShortName,
                    bankMaster.PREFIX,
                    bankMaster.LASTUPDATE
                }
            )
            .Distinct()
            .ToListAsync();

            results.Add("banks", banks);

            // 🔹 All Bank List (Superadmin sees all, others only mapped banks)
            var bankAllList = await (
                from bankList in _oBMSDbContext.BankLists
                join bankMaster in _oBMSDbContext.BankMasters
                    on bankList.BankCode equals bankMaster.BankCode
                join obmsBanks in _oBMSDbContext.OBMSBanks
                    on bankMaster.BankId equals obmsBanks.BankID
                where isSuperAdmin || (obmsBanks.IsAllowed == true && obmsBanks.Name == userID)
                orderby bankList.BankName
                select bankList
            )
            .Distinct()
            .ToListAsync();

            results.Add("bankList", bankAllList);

            return results;
        }

        public async Task<List<ClientInvoiceList>> GetMonthlyInvoiceList(DateTime invoiceStartPeriod, DateTime invoiceEndPeriod)
        {
            return await (from c in _oBMSDbContext.InvoiceDetails
                          join b in _oBMSDbContext.ClientMasters on c.Client equals b.Code
                          join a in _oBMSDbContext.BranchMasters on c.Branch equals a.Code
                          where c.Branch != null && c.InvoiceDate >= invoiceStartPeriod && c.InvoiceDate <= invoiceEndPeriod
                          select new ClientInvoiceList
                          {
                              Row = 0,
                              InvoiceDate = c.InvoiceDate.ToString("dd-MMM-yyyy"),
                              BranchName = a.Name,
                              ClientName = b.Name,
                              InvoiceNumber = c.Branch + " " + c.InvoiceNo,
                              ServiceCharges = c.ServiceCharges,
                              Discount = c.Discount,
                              TaxAmount = c.TaxAmount,
                              InvoiceAmount = c.InvoiceAmount,
                              Payment = c.Payment == null ? "NO" : c.Payment == c.InvoiceAmount ? "YES" : "Partial"
                          }).ToListAsync();
        }
        public async Task<List<Receipts>> GetReceiptsByDateAndBranchAsync(DateTime receiptDate, string branch)
        {
            return await _oBMSDbContext.Receipts
                .Where(r => r.Branch == branch && r.IsDeleted == false &&
                            r.ReceiptDate.Month == receiptDate.Month &&
                            r.ReceiptDate.Year == receiptDate.Year)
                .ToListAsync();
        }
        public async Task<List<Receipts>> GetReceiptsByBankAndChequeAsync(string bankCode, string chequeNo, string branch)
        {
            return await _oBMSDbContext.Receipts
                .Where(r => r.Branch == branch && r.ChequeNo == chequeNo && r.IsDeleted == false &&
                            (string.IsNullOrEmpty(bankCode) || r.BankCode == bankCode))
                .ToListAsync();
        }
        public async Task<ReceiptsRequestDto?> GetReceiptAsync(int id)
        {
            var receipt = await _oBMSDbContext.Receipts
                .Where(r => !r.IsDeleted && r.ID == id)
                .FirstOrDefaultAsync();

            if (receipt == null)
                return null;

            var details = await (from d in _oBMSDbContext.ReceiptDetails
                                 join c in _oBMSDbContext.ClientInvoices
                                     on d.InvoiceID equals c.ID
                                 where d.ReceiptID == id
                                 select new ReceiptDetailRequestDto
                                 {
                                     ID = d.ID,
                                     ReceiptID = d.ReceiptID,
                                     InvoiceID = d.InvoiceID,
                                     PaidAmount = d.Amount,
                                     InvoiceNo = c.InvoiceNo,
                                     InvoiceAmount = c.ServiceCharges - c.Discount + c.TaxAmount,
                                     InvoiceDate = c.InvoiceDate
                                 }).ToArrayAsync();


            var receiptDto = new ReceiptsRequestDto
            {
                ID = receipt.ID,
                VoucherNo = receipt.VoucherNo,
                ReceiptDate = receipt.ReceiptDate,
                Branch = receipt.Branch,
                PaymentFrom = receipt.PaymentFrom,
                Particulars = receipt.Particulars,
                ReceiptType = receipt.ReceiptType,
                IsInvoiceAdjustment = receipt.IsInvoiceAdjustment,
                BankCode = receipt.BankCode,
                BankBranch = receipt.BankBranch,
                ChequeNo = receipt.ChequeNo,
                InvoiceNumbers = receipt.InvoiceNumbers,
                ReceiptAmount = receipt.ReceiptAmount,
                TaxPercentage = receipt.TaxPercentage,
                TaxAmount = receipt.TaxAmount,
                HQPercentage = receipt.HQPercentage,
                HQAmount = receipt.HQAmount,
                BranchCollection = receipt.BranchCollection,
                CreditNoteAmount = receipt.CreditNoteAmount,
                DebitNoteAmount = receipt.DebitNoteAmount,
                SuspendAmount = receipt.SuspendAmount,
                BankID = receipt.BankID,
                ChequeStatus = receipt.ChequeStatus,
                IsDeleted = receipt.IsDeleted,
                LastUpdate = receipt.LastUpdate,
                LastUpdatedBy = receipt.LastUpdatedBy,
                details = details
            };

            return receiptDto;
        }
        public async Task<List<BranchPayment>> GetListByDateAsync(DateTime paymentDate)
        {
            var payments = await _oBMSDbContext.BranchPayments
                .Where(p => !p.IsDeleted && p.PaymentDate.Month == paymentDate.Month && p.PaymentDate.Year == paymentDate.Year)
                .Select(p => new BranchPayment
                {
                    ID = p.ID,
                    PaymentDate = p.PaymentDate,
                    Supplier = p.Supplier ?? 0,
                    ChequeNo = p.ChequeNo, // Leave it as-is for now
                    LastUpdatedBy = p.LastUpdatedBy ?? "System",
                    ChqClearencedate = p.ChqClearencedate,
                    Amount = p.Amount,
                    Particulars = p.Particulars,
                    PaymentTo = p.PaymentTo,
                    VoucherNo = p.VoucherNo,
                    BankID = p.BankID
                })
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();

            // Replace null ChequeNo with bank short name
            foreach (var payment in payments)
            {
                if (string.IsNullOrEmpty(payment.ChequeNo) && payment.BankID.HasValue)
                {
                    payment.ChequeNo = await _registerRepository.GetBankAccShortNameByBankIDAsync(payment.BankID.Value);
                }
            }

            return payments;
        }
        public async Task<List<BranchPayment>> GetListByBankAndChequeAsync(decimal bankId, string chequeNo)
        {
            return await _oBMSDbContext.BranchPayments
                .Where(p => !p.IsDeleted && p.BankID == bankId && p.ChequeNo == chequeNo)
                .ToListAsync();
        }
        public async Task<BranchPayment> GetBranchPaymentAsync(int id)
        {
            return await _oBMSDbContext.BranchPayments
                .Where(bp => bp.IsDeleted == false && bp.ID == id)
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
                })
                .FirstOrDefaultAsync();
        }
        public async Task<bool> DeleteAsync(int id, string currentUser)
        {
            try
            {
                var payment = await _oBMSDbContext.BranchPayments
                    .Where(p => p.ID == id && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (payment == null)
                    return false;

                // Update main payment
                payment.IsDeleted = true;
                payment.LastUpdate = DateTime.Now;
                payment.LastUpdatedBy = currentUser;

                // Update related details
                var details = await _oBMSDbContext.BranchPaymentDetails
                    .Where(d => d.PaymentID == id && d.IsDeleted == false)
                    .ToListAsync();

                foreach (var detail in details)
                {
                    detail.IsDeleted = true;
                    detail.LastUpdate = DateTime.Now;
                    detail.LastUpdatedBy = currentUser;
                }

                await _oBMSDbContext.SaveChangesAsync();


                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task<bool> ReceiptDeleteAsync(int id, string currentUser)
        {
            try
            {
                var receipt = await _oBMSDbContext.Receipts
                    .Where(p => p.ID == id && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (receipt == null)
                    return false;

                // Update main payment
                receipt.IsDeleted = true;
                receipt.LastUpdate = DateTime.Now;
                receipt.LastUpdatedBy = currentUser;

                await _oBMSDbContext.SaveChangesAsync();


                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task<List<InventoryCategoryDto>> GetListAsync()
        {
            var data = await _oBMSDbContext.InventoryCategories
                .OrderBy(x => x.Name)
                .ToListAsync();

            var result = data.Select(x => new InventoryCategoryDto(
                id: x.ID,
                accountCategoryID: 0,           // Default int
                accountNo: string.Empty,        // Empty string
                accountTypeID: 0,               // Default int
                name: x.Cat == "P" ? $"{x.Name} (Purchase)" : $"{x.Name} (Expenses)",
                cat: x.Cat,
                assetType: x.AssetType ?? string.Empty
            )).ToList();

            return result;
        }
        public List<BranchPaymentRequestDto> GetDeletedPaymentsByMonthYear(DateTime paymentDate)
        {
            int month = paymentDate.Month;
            int year = paymentDate.Year;

            var results = _oBMSDbContext.BranchPayments
                .Where(p => p.IsDeleted && p.PaymentDate.Month == month && p.PaymentDate.Year == year)
                .Select(p => new BranchPaymentRequestDto
                {
                    ID = p.ID,
                    PaymentDate = p.PaymentDate,
                    CreditorType = p.CreditorType,
                    Supplier = p.Supplier,
                    PaymentType = p.PaymentType,
                    PaymentPurpose = p.PaymentPurpose,
                    BankID = p.BankID,
                    ChequeNo = string.IsNullOrEmpty(p.ChequeNo) ? "" : UtilityMain.GetBankCodeByBankID((int)p.BankID) + "-" + p.ChequeNo,
                    PaymentTo = p.PaymentTo,
                    Particulars = p.Particulars,
                    Amount = p.Amount,
                    IsDeleted = p.IsDeleted,
                    LastUpdate = p.LastUpdate,
                    LastUpdatedBy = p.LastUpdatedBy
                }).ToList();

            return results;
        }
        public bool Restore(int id, string currentUser)
        {
            var payment = _oBMSDbContext.BranchPayments.FirstOrDefault(bp => bp.ID == id);
            if (payment == null) return false;

            // Restore the payment header
            payment.IsDeleted = false;
            payment.LastUpdate = DateTime.Now;
            payment.LastUpdatedBy = currentUser;

            // Also restore all related detail lines so amounts re-count in vwPaymentDue
            var details = _oBMSDbContext.BranchPaymentDetails
                .Where(d => d.PaymentID == id && d.IsDeleted == true)
                .ToList();

            foreach (var detail in details)
            {
                detail.IsDeleted = false;
                detail.LastUpdate = DateTime.Now;
                detail.LastUpdatedBy = currentUser;
            }

            _oBMSDbContext.SaveChanges();
            return true;
        }
        public async Task<List<ChequeStatusDto>> GetChequeStatusesAsync(DateTime startDate, DateTime endDate, string chequeStatus, string bankCode, string transType)
        {
            if (transType != "R" && transType != "P")
                return new List<ChequeStatusDto>();

            var query = transType == "R"
                ? from a in _oBMSDbContext.ChequeStatus
                  join b in _oBMSDbContext.Receipts on a.ID equals b.ID
                  where a.ChequeDate >= startDate && a.ChequeDate <= endDate
                        && (string.IsNullOrEmpty(chequeStatus) || a.Status == chequeStatus)
                        && b.ReceiptType != 3
                        //&& !a.IsDeleted
                        && (string.IsNullOrEmpty(bankCode) || a.BankCode == bankCode)
                        && a.TransType == transType
                  orderby a.ChequeDate, a.ChequeNo
                  select new ChequeStatusDto
                  {
                      ID = a.ID,
                      TransType = a.TransType,
                      ChequeDate = a.ChequeDate,
                      BankCode = a.BankCode,
                      ChequeNo = a.ChequeNo,
                      ChequeAmount = a.ChequeAmount,
                      ChequeStatus = a.Status ?? string.Empty,
                      Particulars = b.Particulars ?? string.Empty
                  }

                : from a in _oBMSDbContext.ChequeStatus
                  join b in _oBMSDbContext.BranchPayments on a.ID equals b.ID
                  where a.ChequeDate >= startDate && a.ChequeDate <= endDate
                        && (string.IsNullOrEmpty(chequeStatus) || a.Status == chequeStatus)
                        && b.PaymentType != 3
                        //&& !a.IsDeleted
                        && (string.IsNullOrEmpty(bankCode) || a.BankCode == bankCode)
                        && a.TransType == transType
                  orderby a.ChequeDate, a.ChequeNo
                  select new ChequeStatusDto
                  {
                      ID = a.ID,
                      TransType = a.TransType,
                      ChequeDate = a.ChequeDate,
                      BankCode = a.BankCode,
                      ChequeNo = a.ChequeNo,
                      ChequeAmount = a.ChequeAmount,
                      ChequeStatus = a.Status ?? string.Empty,
                      Particulars = b.Particulars
                  };

            return await query.ToListAsync();
        }
        public async Task<List<ClientLegalDemandActionDto>> GetActionsAsync(string branch = null, string client = null, string actionTaken = null)
        {
            var query = from action in _oBMSDbContext.ClientLegalDemandActions
                        join clientMaster in _oBMSDbContext.ClientMasters
                            on new { action.Client, action.Branch } equals new { Client = clientMaster.Code, clientMaster.Branch }
                        join branchMaster in _oBMSDbContext.BranchMasters
                            on action.Branch equals branchMaster.Code
                        where !action.IsDeleted
                        select new ClientLegalDemandActionDto
                        {
                            ID = action.ID,
                            BranchCode = branchMaster.Code,
                            BranchName = branchMaster.Name ?? string.Empty,
                            ClientCode = clientMaster.Code,
                            ClientName = clientMaster.Name ?? string.Empty,
                            ActionTaken = action.ActionTaken,
                            DateIssue = action.DateIssue,
                            Remarks = action.Remarks ?? string.Empty,
                            DeletionRemarks = action.DeletionRemarks ?? string.Empty,
                            IsDeleted = action.IsDeleted,
                            CreatedBy = action.CreatedBy ?? string.Empty,
                            CreatedDate = action.CreatedDate,
                            UpdatedBy = action.UpdatedBy ?? string.Empty,
                            UpdatedDate = action.UpdatedDate
                        };
            if (!string.IsNullOrEmpty(branch))
            {
                query = query.Where(x => x.BranchCode == branch);
            }

            if (!string.IsNullOrEmpty(client))
            {
                query = query.Where(x => x.ClientCode == client);
            }

            if (!string.IsNullOrEmpty(actionTaken))
            {
                if (actionTaken == "Legal")
                    query = query.Where(x => x.ActionTaken == "L");
                else if (actionTaken == "Demand")
                    query = query.Where(x => x.ActionTaken == "D"); // or "N" depending on your data
            }

            return await query.OrderBy(x => x.ClientName).ThenBy(x => x.DateIssue).ToListAsync();

        }
        public async Task<bool> DeleteLegalDemandAsync(int id, string currentUser, string deleteRemarks)
        {
            var item = await _oBMSDbContext.ClientLegalDemandActions.FindAsync(id);
            if (item == null) return false;

            item.IsDeleted = true;
            item.DeletionRemarks = deleteRemarks;
            item.UpdatedDate = DateTime.Now;
            item.UpdatedBy = currentUser;

            await _oBMSDbContext.SaveChangesAsync();
            return true;
        }
        public async Task<ClientLegalDemandActionDto> GetLegalDemandByID(int ID)
        {
            var action = await _oBMSDbContext.ClientLegalDemandActions
                        .FirstOrDefaultAsync(x => x.ID == ID);

            if (action == null)
            {
                return null; // or handle accordingly
            }

            var result = new ClientLegalDemandActionDto
            {
                ID = action.ID,
                BranchCode = action.Branch,
                ClientCode = action.Client,
                ActionTaken = action.ActionTaken,
                DateIssue = action.DateIssue,
                Remarks = action.Remarks ?? string.Empty,
                DeletionRemarks = action.DeletionRemarks ?? string.Empty,
                IsDeleted = action.IsDeleted,
                CreatedBy = action.CreatedBy ?? string.Empty,
                CreatedDate = action.CreatedDate,
                UpdatedBy = action.UpdatedBy ?? string.Empty,
                UpdatedDate = action.UpdatedDate
            };

            return result;
        }

        /// <summary>
        /// Returns payment transactions grouped by VoucherNo with accumulated totals.
        /// Each row = one unique voucher with SUM of amounts across all branch detail lines.
        /// </summary>
        public async Task<List<BranchPaymentVoucherSummaryDto>> GetBranchPaymentVoucherSummary(
            DateTime? startDate, DateTime? endDate, string branch)
        {
            var query = from bp in _oBMSDbContext.BranchPayments
                        join bpd in _oBMSDbContext.BranchPaymentDetails
                            on bp.ID equals (int)bpd.PaymentID
                        where bp.IsDeleted == false
                           && (bpd.IsDeleted == null || bpd.IsDeleted == false)
                           && (string.IsNullOrEmpty(branch) || bpd.Branch == branch)
                           && (!startDate.HasValue || bp.PaymentDate >= startDate.Value)
                           && (!endDate.HasValue || bp.PaymentDate <= endDate.Value)
                        group new { bp, bpd } by new
                        {
                            bp.VoucherNo,
                            bpd.Branch,
                            bp.PaymentDate,
                            bp.CreditorType,
                            bp.PaymentType,
                            bp.PaymentPurpose,
                            bp.PaymentTo,
                            bp.Particulars,
                            bp.ItemCategory
                        } into g
                        select new BranchPaymentVoucherSummaryDto
                        {
                            VoucherNo       = g.Key.VoucherNo,
                            Branch          = g.Key.Branch,
                            PaymentDate     = g.Key.PaymentDate,
                            CreditorType    = g.Key.CreditorType,
                            PaymentType     = g.Key.PaymentType,
                            PaymentPurpose  = g.Key.PaymentPurpose,
                            PaymentTo       = g.Key.PaymentTo,
                            Particulars     = g.Key.Particulars,
                            ItemCategory    = g.Key.ItemCategory,
                            TransactionCount = g.Count(),
                            TotalAmount     = g.Sum(x => x.bpd.Amount),
                            FirstPaymentDate = g.Min(x => x.bp.PaymentDate),
                            LastPaymentDate  = g.Max(x => x.bp.PaymentDate)
                        };

            return await query
                .OrderBy(x => x.PaymentDate)
                .ThenBy(x => x.VoucherNo)
                .ToListAsync();
        }
    }
}

public class QueryResult
{
    public string Code { get; set; }
    public string Branch { get; set; }
    public string Name { get; set; }
    public int? ID { get; set; }
    public string? InvoiceNo { get; set; }
    public string? Note { get; set; }
    public int? AgreementID { get; set; }
    public string? WorkPlace { get; set; }
}

[Keyless]
public class OtherPayment
{
    public decimal ID { get; set; }
    public decimal PaymentID { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public decimal? Amount { get; set; }
}

[Keyless]

public class BranchPaymentRow
{
    public decimal ID { get; set; }
    public decimal PaymentID { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public decimal InvoiceID { get; set; }
    public string InvoiceNo { get; set; }
    public decimal Total { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

}

[Table("BranchPaymentForBranch")]
[Keyless]
public class BranchPaymentForBranch
{

    public int? ID { get; set; }
    public decimal? PaymentID { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? BName { get; set; }
    public decimal? Amount { get; set; }
}
