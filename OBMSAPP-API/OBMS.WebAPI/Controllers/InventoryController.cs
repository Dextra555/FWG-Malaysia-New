using Azure;
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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web.Services.Description;

namespace OBMS.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : Controller
    {
        HttpResponseMessage response = new HttpResponseMessage();
        private readonly OBMSDbContext _oBMSDbContext;
        private readonly IInventoryRepository _inventoryRepository;


        public InventoryController(IInventoryRepository inventoryRepository, OBMSDbContext oBMSDbContext)
        {
            _inventoryRepository = inventoryRepository;
            _oBMSDbContext = oBMSDbContext;
        }

        [HttpPost]
        [Route("SaveAndUpdateCategory")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateCategory(InventoryCategoryDto inventoryCategoryDto)
        {
            try
            {
                var category = new InventoryCategory();
                if (inventoryCategoryDto.ID != 0)
                {
                    category = _oBMSDbContext.InventoryCategories.Where(x => x.ID == inventoryCategoryDto.ID).FirstOrDefault();
                }

                category.ID = inventoryCategoryDto.ID;
                category.Name = inventoryCategoryDto.Name;
                category.AssetType = inventoryCategoryDto.AssetType;
                category.Cat = inventoryCategoryDto.Cat;


                await _inventoryRepository.SaveAndUpdateCategory(category);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", category);
                response.Headers.Add("Success", "Successfully save & update Category.");

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("GetInventoryCategories")]
        public async Task<ActionResult<Object>> GetCategories()
        {
            try
            {
                var categories = await _inventoryRepository.GetCategories();

                return categories;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetInventoryCategoryByID")]
        public async Task<ActionResult<Object>> GetInventoryCategoryByID(int ID)
        {
            try
            {
                var category = await _inventoryRepository.GetCategoryByID(ID);

                return category;
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpGet]
        [Route("GetInventoryItemMaster")]
        public async Task<ActionResult<Object>> GetInventoryItemMaster(string cat)
        {
            try
            {
                var categories = await _inventoryRepository.GetCategoriesByCat(cat);

                var results = new Dictionary<string, Object>();

                results.Add("categories", categories);

                return results;
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpGet]
        [Route("GetCategoriesByCat")]
        public async Task<ActionResult<Object>> GetCategoriesByCat(string cat)
        {
            try
            {
                var categories = await _inventoryRepository.GetCategoriesByCat(cat);

                return categories;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost]
        [Route("SaveAndUpdateItem")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateItem(ItemMasterDto dto)
        {
            try
            {
                var item = new ItemMaster();
                if (dto.ID != 0)
                {
                    item = _oBMSDbContext.ItemMasters.Where(x => x.ID == dto.ID).FirstOrDefault();
                }


                item.ID = dto.ID;
                item.CategoryID = dto.CategoryID;
                item.Name = dto.Name;
                item.Price = dto.Price;
                item.LASTUPDATE = DateTime.Now;
                item.LastUpdatedBy = dto.LastUpdatedBy;
                item.SellPrice = dto.SellPrice;
                item.Remarks = dto.Remarks;
                item.Quantity = dto.Quantity;

                await _inventoryRepository.SaveAndUpdateItem(item);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", item);
                response.Headers.Add("Success", "Successfully save & update Item.");

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("GetInventoryItems")]
        public async Task<ActionResult<Object>> GetItems()
        {
            try
            {
                var items = await _inventoryRepository.GetItems();

                return items;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetInventoryItemByID")]
        public async Task<ActionResult<Object>> GetItemByID(int ID)
        {
            try
            {
                var data = await _inventoryRepository.GetItemByID(ID);

                return data;
            }
            catch (Exception ex)
            {
                throw;
            }

        }



        [HttpPost]
        [Route("SaveAndUpdateAssetMaster")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateAssetMaster(AssetMasterRequestDto dto)
        {
            try
            {
                var obj = new AssetMaster();
                if (dto.ID != 0)
                {
                    obj = _oBMSDbContext.AssetMasters.Where(x => x.ID == dto.ID).FirstOrDefault();
                }

                obj.ID = dto.ID;
                obj.Name = dto.Name;
                obj.AssetType = dto.AssetType;
                obj.Branch = dto.Branch;
                obj.PurchaseAmount = dto.PurchaseAmount;
                obj.PurchaseDate = dto.PurchaseDate;
                obj.LastUpdatedDate = dto.LastUpdatedDate;
                obj.LastUpdatedBy = dto.LastUpdatedBy;


                await _inventoryRepository.SaveAndUpdateAssetMaster(obj);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", obj);
                response.Headers.Add("Success", "Successfully save & update Asset Master.");

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("GetAssetMasters")]
        public async Task<ActionResult<Object>> GetAssetMasters()
        {
            try
            {
                var obj = await _inventoryRepository.GetAssetMasters();

                return obj;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetInventoryAssetByID")]
        public async Task<ActionResult<Object>> GetAssetByID(int ID)
        {
            try
            {
                var data = await _inventoryRepository.GetAssetByID(ID);

                return data;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetSupplierCode")]
        public async Task<ActionResult<Object>> GetSupplierCode()
        {
            try
            {
                return Ok(await _inventoryRepository.GetSupplierCode());
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost]
        [Route("SaveAndUpdateSupplier")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateSupplier(SupplierRequestDto dto)
        {
            try
            {
                var obj = new Supplier();
                if (dto.Id != 0)
                {
                    obj = _oBMSDbContext.Suppliers.Where(x => x.Id == dto.Id).FirstOrDefault();
                }


                obj.Id = dto.Id;
                obj.Code = dto.Code;
                obj.Name = dto.Name;
                obj.Address1 = dto.Address1;
                obj.Address2 = dto.Address2;
                obj.PostCode = dto.PostCode;
                obj.City = dto.City;
                obj.State = dto.State;
                obj.Phone = dto.Phone;
                obj.Fax = dto.Fax;
                obj.CreditLimit = dto.CreditLimit;
                obj.Status = dto.Status;
                obj.ContactPerson = dto.ContactPerson;
                obj.Category = dto.Category;
                obj.LASTUPDATE = DateTime.Now;
                obj.LastUpdatedBy = dto.LastUpdatedBy;


                await _inventoryRepository.SaveAndUpdateSupplier(obj);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", obj);
                response.Headers.Add("Success", "Successfully save & update .");

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("GetSuppliers")]
        public async Task<ActionResult<Object>> GetSuppliers()
        {
            try
            {
                var obj = await _inventoryRepository.GetSuppliers();

                return obj;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetSupplierByID")]
        public async Task<ActionResult<Object>> GetSupplierByID(int ID)
        {
            try
            {
                var data = await _inventoryRepository.GetSupplierByID(ID);

                return data;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetRecipientMaster")]
        public async Task<ActionResult<Object>> GetRecipientMaster()
        {
            try
            {
                return Ok(await _inventoryRepository.GetRecipientMaster());
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost]
        [Route("SaveAndUpdateRecipient")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateRecipient(RecipientRequestDto dto)
        {
            try
            {
                var obj = new Recipient();
                if (dto.Id != 0)
                {
                    obj = _oBMSDbContext.Recipients.Where(x => x.Id == dto.Id).FirstOrDefault();
                }

                obj.Id = dto.Id;
                obj.Code = dto.Code;
                obj.Name = dto.Name;
                obj.Address1 = dto.Address1;
                obj.Address2 = dto.Address2;
                obj.PostCode = dto.PostCode;
                obj.City = dto.City;
                obj.State = dto.State;
                obj.Phone = dto.Phone;
                obj.Fax = dto.Fax;
                obj.CreditLimit = dto.CreditLimit;
                obj.Status = dto.Status;
                obj.Supervisor = dto.Supervisor;
                obj.LASTUPDATE = DateTime.Now;
                obj.LastUpdatedBy = dto.LastUpdatedBy;
                obj.ICNO = dto.ICNO;
                obj.Category = dto.Category;

                await _inventoryRepository.SaveAndUpdateRecipient(obj);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", obj);
                response.Headers.Add("Success", "Successfully save & update .");

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("GetRecipients")]
        public async Task<ActionResult<Object>> GetRecipients()
        {
            try
            {
                var obj = await _inventoryRepository.GetRecipients();

                return obj;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetRecipientByID")]
        public async Task<ActionResult<Object>> GetRecipientByID(int ID)
        {
            try
            {
                var data = await _inventoryRepository.GetRecipientByID(ID);

                return data;
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpGet]
        [Route("GetUtilityBillsMasterList")]
        public async Task<ActionResult<Object>> GetUtilityBillsMasterList()
        {
            try
            {
                var obj = await _inventoryRepository.GetUtilityBillsMasterList();
                return Ok(obj);
            }
            catch (Exception ex)
            {
                throw;
            }

        }



        [HttpGet]
        [Route("GetPaytoByCategory")]
        public async Task<ActionResult<Object>> GetPaytoByCategory(int ID)
        {
            try
            {
                var obj = await _inventoryRepository.GetPaytoByCategory(ID);

                return Ok(obj);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetUtilityMasterList")]
        public async Task<ActionResult<Object>> GetUtilityMasterList(string invCat, string userID, string supplierCat = "")
        {
            try
            {
                var objList = await _inventoryRepository.GetUtilityMasterList(invCat, supplierCat, userID);
                return Ok(objList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetUtilitySearchList")]
        public async Task<ActionResult<Object>> GetUtilitySearchList(string branch, int supplier, string Itype)
        {
            try
            {
                var result = _oBMSDbContext.CreditorInvoices
     .Where(ci => ci.Branch == branch && ci.Supplier == supplier && ci.InvoiceType == Itype && ci.IsDeleted == false)
     .Select(ci => new
     {
         ci.ID,
         ci.Branch,
         ci.Supplier,
         ci.RecID,
         ci.ItemCategory,
         ci.InvoiceNo,
         ci.InvoiceType,
         ci.InvoiceDate,
         ci.Total,
         ci.PaymentDate,
         ci.LASTUPDATE
     })
     .OrderByDescending(x => x.ID)
     .ToList();



                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetUtilityBillSearchList")]
        public async Task<IActionResult> GetUtilityBillSearchList(string branch,int supplier,string invoiceType,string invoiceNo, DateTime? invoiceDate,DateTime? paymentDate)
        {
            try
            {
                // Base query
                var query = _oBMSDbContext.CreditorInvoices
                    .Where(ci => ci.Branch == branch &&
                                 ci.Supplier == supplier &&
                                 ci.InvoiceType == invoiceType
                                 && ci.IsDeleted == false)
                    .AsQueryable();

                // Optional filters
                if (!string.IsNullOrEmpty(invoiceNo))
                {
                    query = query.Where(ci => ci.InvoiceNo.StartsWith(invoiceNo));
                }

                if (invoiceDate.HasValue)
                {
                    var start = invoiceDate.Value.Date;
                    var end = start.AddDays(1);
                    query = query.Where(ci => ci.InvoiceDate >= start && ci.InvoiceDate <= end);
                }

                if (paymentDate.HasValue)
                {
                    var start = paymentDate.Value.Date;
                    var end = start.AddDays(1);
                    query = query.Where(ci => ci.PaymentDate >= start && ci.PaymentDate <= end);
                }

                // Execute query and select anonymous objects
                var result = await query
                    .OrderBy(x => x.PaymentDate)
                    .Select(ci => new
                    {
                        ci.ID,
                        ci.Branch,
                        ci.Supplier,
                        ci.RecID,
                        ci.ItemCategory,
                        ci.InvoiceNo,
                        ci.InvoiceType,
                        ci.InvoiceDate,
                        ci.Total,
                        ci.PaymentDate,
                        ci.LASTUPDATE
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception)
            {
                throw;
            }
        }


        [HttpGet]
        [Route("GetUtilityDetailsByID")]
        public async Task<ActionResult<Object>> GetUtilityDetailsByID(int ID)
        {
            try
            {
                var result = _oBMSDbContext.CreditorInvoiceDetails.Where(ci => ci.InvoiceID == ID).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpPost]
        [Route("SaveAndUpdateUtility")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateUtility(CreditorInvoiceRequestDto creditorInvoiceRequestDto)
        {
            try
            {
                var creditorInvoice = new CreditorInvoice();
                if (creditorInvoiceRequestDto.ID != 0)
                {
                    creditorInvoice = _oBMSDbContext.CreditorInvoices.Where(x => x.ID == creditorInvoiceRequestDto.ID).FirstOrDefault();
                }

                creditorInvoice.ID = creditorInvoiceRequestDto.ID;
                creditorInvoice.Branch = creditorInvoiceRequestDto.Branch;
                creditorInvoice.Supplier = creditorInvoiceRequestDto.Supplier;
                creditorInvoice.RecID = creditorInvoiceRequestDto.RecID;
                creditorInvoice.InvoiceNo = creditorInvoiceRequestDto.InvoiceNo;
                creditorInvoice.InvoiceType = creditorInvoiceRequestDto.InvoiceType;
                creditorInvoice.InvoiceDate = creditorInvoiceRequestDto.InvoiceDate;
                creditorInvoice.Total = creditorInvoiceRequestDto.Total;
                creditorInvoice.PaymentDate = creditorInvoiceRequestDto.PaymentDate;
                creditorInvoice.LASTUPDATE = DateTime.Now;
                creditorInvoice.LastUpdatedBy = creditorInvoiceRequestDto.LastUpdatedBy;
                creditorInvoice.ItemCategory = creditorInvoiceRequestDto.ItemCategory;
                creditorInvoice.IsDeleted = false;


                await _inventoryRepository.SaveAndUpdateUtility(creditorInvoice);

                if (creditorInvoiceRequestDto.details != null)
                {
                    foreach (CreditorInvoiceDetailRequestDto detail in creditorInvoiceRequestDto.details)
                    {
                        var creditorInvoiceDetail = new CreditorInvoiceDetails();
                        if (detail.ID != 0)
                        {
                            creditorInvoiceDetail = _oBMSDbContext.CreditorInvoiceDetails.Where(x => x.ID == detail.ID).FirstOrDefault();
                        }

                        creditorInvoiceDetail.ID = detail.ID;
                        creditorInvoiceDetail.InvoiceID = creditorInvoice.ID;
                        creditorInvoiceDetail.SerialNo = detail.SerialNo;
                        creditorInvoiceDetail.ItemID = detail.ItemID;
                        creditorInvoiceDetail.ItemCategoryID = detail.ItemCategoryID;
                        creditorInvoiceDetail.NoOfUnits = detail.NoOfUnits;
                        creditorInvoiceDetail.CostPerUnit = detail.CostPerUnit;
                        creditorInvoiceDetail.ValuePeriod = detail.ValuePeriod;
                        creditorInvoiceDetail.ValuePercentage = detail.ValuePercentage;
                        creditorInvoiceDetail.ValueStatus = detail.ValueStatus;
                        creditorInvoiceDetail.InvoicePeriodFrom = detail.InvoicePeriodFrom;
                        creditorInvoiceDetail.InvoicePeriodTo = detail.InvoicePeriodTo;
                        creditorInvoiceDetail.Amount = detail.Amount;
                        creditorInvoiceDetail.Note = detail.Note;
                        creditorInvoiceDetail.LASTUPDATE = DateTime.Now;
                        creditorInvoiceDetail.LastUpdatedBy = detail.LastUpdatedBy;



                        await _inventoryRepository.SaveAndUpdateUtilityDetails(creditorInvoiceDetail);
                    }
                }

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", creditorInvoice);
                response.Headers.Add("Success", "Successfully save & update Utility details.");

                return Ok(dictResult);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("GetUtilityDetailListByInvoiceId")]
        public async Task<ActionResult<Object>> GetUtilityDetailListByInvoiceId(int InvoiceID)
        {
            try
            {
                //var result = _oBMSDbContext.CreditorInvoiceDetails.Where(x => x.InvoiceID == InvoiceID).ToList();

                var result = from CID in _oBMSDbContext.CreditorInvoiceDetails
                             join IM in _oBMSDbContext.ItemMasters on CID.ItemID equals IM.ID
                             where CID.InvoiceID == InvoiceID
                             select new
                             {
                                 CID.ID,
                                 CID.InvoiceID,
                                 CID.SerialNo,
                                 CID.ItemID,
                                 CID.ItemCategoryID,
                                 CID.NoOfUnits,
                                 CID.CostPerUnit,
                                 CID.ValuePeriod,
                                 CID.ValuePercentage,
                                 CID.ValueStatus,
                                 CID.InvoicePeriodFrom,
                                 CID.InvoicePeriodTo,
                                 CID.Amount,
                                 CID.Note,
                                 CID.LASTUPDATE,
                                 CID.LastUpdatedBy,
                                 ItemName = IM.Name
                             };


                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost]
        [Route("DeleteUtilityDetailById")]
        public async Task<ActionResult<HttpResponseMessage>> DeleteUtilityDetailById(int Id)
        {
            try
            {
                var data = _oBMSDbContext.CreditorInvoiceDetails.Where(x => x.ID == Id).FirstOrDefault();


                if (data != null)
                {
                    _oBMSDbContext.CreditorInvoiceDetails.Remove(data);
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

        [HttpGet]
        [Route("GetItemByCategoryId")]
        public async Task<ActionResult<Object>> GetItemByCategoryId(int categoryId)
        {
            try
            {
                var result = _oBMSDbContext.ItemMasters
    .Where(item => item.CategoryID == categoryId)
    .Join(
        _oBMSDbContext.InventoryCategories,
        item => item.CategoryID,
        category => category.ID,
        (item, category) => new
        {
            item.ID,
            item.CategoryID,
            item.Name,
            item.Price,
            item.LASTUPDATE,
            CategoryName = category.Name,
            item.SellPrice
        }
    )
    .ToList();


                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetMaterialMasterList")]
        public async Task<ActionResult<Object>> GetMaterialMasterList(string userID)
        {
            try
            {
                var objList = await _inventoryRepository.GetMaterialMasterList(userID);
                return Ok(objList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetMaterialInvoiceByBranch")]
        public async Task<ActionResult<Object>> GetMaterialInvoiceByBranch(string branchID)
        {
            try
            {

                string nextEmployeeCode = _oBMSDbContext.StockIssues
                    .OrderByDescending(emp => emp.ID)
                    .Where(emp => emp.Branch == branchID)
                    .Select(emp => emp.InvoiceNo)
                    .FirstOrDefault();

                if (nextEmployeeCode != null && nextEmployeeCode != "")
                {
                    int numericPart = int.Parse(nextEmployeeCode) + 1;
                    return Ok(new { value = numericPart.ToString("D6") });
                }
                else
                {
                    return Ok(new { value = 1.ToString("D6") });
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetMaterialInvoiceItemByCategoryID")]
        public async Task<ActionResult<Object>> GetMaterialInvoiceItemByCategoryID(int ID)
        {
            try
            {

                var list = _oBMSDbContext.ItemMasters.Where(x => x.CategoryID == ID).ToList();

                return Ok(list);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost]
        [Route("SaveAndUpdateMaterial")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateMaterial(StockIssuesRequestDto stockIssuesRequestDto)
        {
            try
            {
                var stockIssues = new StockIssues();
                if (stockIssuesRequestDto.ID != 0)
                {
                    stockIssues = _oBMSDbContext.StockIssues.Where(x => x.ID == stockIssuesRequestDto.ID).FirstOrDefault();
                }

                stockIssues.ID = stockIssuesRequestDto.ID;
                stockIssues.Branch = stockIssuesRequestDto.Branch;
                stockIssues.InvoiceNo = stockIssuesRequestDto.InvoiceNo;
                stockIssues.InvoiceDate = stockIssuesRequestDto.InvoiceDate;
                stockIssues.InvoiceRemarks = stockIssuesRequestDto.InvoiceRemarks;
                stockIssues.LASTUPDATE = DateTime.Now;
                stockIssues.LastUpdatedBy = stockIssuesRequestDto.LastUpdatedBy;


                await _inventoryRepository.SaveAndUpdateMaterial(stockIssues);

                if (stockIssuesRequestDto.details != null)
                {
                    foreach (StockIssueDetailRequestDto detail in stockIssuesRequestDto.details)
                    {
                        var stockIssueDetail = new StockIssueDetail();
                        if (detail.ID != 0)
                        {
                            stockIssueDetail = _oBMSDbContext.StockIssueDetails.Where(x => x.ID == detail.ID).FirstOrDefault();
                        }

                        stockIssueDetail.ID = detail.ID;
                        stockIssueDetail.InvoiceID = stockIssues.ID;
                        stockIssueDetail.ItemID = detail.ItemID;
                        stockIssueDetail.ItemCategoryID = detail.ItemCategoryID;
                        stockIssueDetail.NoOfUnits = detail.NoOfUnits;
                        stockIssueDetail.CostPerUnit = detail.CostPerUnit;
                        stockIssueDetail.LASTUPDATE = DateTime.Now;
                        stockIssueDetail.LastUpdatedBy = detail.LastUpdatedBy;



                        await _inventoryRepository.SaveAndUpdateMaterialDetails(stockIssueDetail);
                    }
                }

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", stockIssues);
                response.Headers.Add("Success", "Successfully save & update Material details.");

                return Ok(dictResult);

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("GetMaterialSearchList")]
        public async Task<ActionResult<Object>> GetMaterialSearchList(string branch)
        {
            try
            {
                var result = _oBMSDbContext.StockIssues
     .Where(ci => ci.Branch == branch)
     .OrderByDescending(x => x.ID)
     .ToList();



                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpGet]
        [Route("GetMaterialDetailListByInvoiceId")]
        public async Task<ActionResult<Object>> GetMaterialDetailListByInvoiceId(int InvoiceID)
        {
            try
            {
              
                var result = from CID in _oBMSDbContext.StockIssueDetails
                             join IM in _oBMSDbContext.ItemMasters on CID.ItemID equals IM.ID
                             where CID.InvoiceID == InvoiceID
                             select new
                             {
                                 CID.ID,
                                 CID.InvoiceID,
                                 CID.ItemID,
                                 CID.ItemCategoryID,
                                 CID.NoOfUnits,
                                 CID.CostPerUnit,
                                 CID.LASTUPDATE,
                                 CID.LastUpdatedBy,
                                 ItemName = IM.Name
                             };


                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpGet]
        [Route("GetReportHQMaster")]
        public async Task<ActionResult<Object>> GetReportHQMaster()
        {
            try
            {

                var result = from IM in _oBMSDbContext.ItemMasters
                             join IC in _oBMSDbContext.InventoryCategories on IM.CategoryID equals IC.ID
                          
                             select new
                             {
                                IM.ID,
                                IM.CategoryID,
                                IM.Name,
                                IM.Price,
                                 IM.LASTUPDATE,
                                 IM.SellPrice
                             };


                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost("DeleteItem/{id}")]
        public async Task<IActionResult> DeleteItem(int id, string currentUser)
        {
            var result = await _inventoryRepository.DeleteItemAsync(id, currentUser);
            if (result)
                return Ok(new { success = true, message = "Item deleted (soft delete)" });
            return NotFound(new { success = false, message = "Item not found" });
        }

        [HttpPost("DeleteAsset/{id}")]
        public IActionResult DeleteAsset(int id)
        {
            var result = _inventoryRepository.DeleteAssetType(id);
            if (!result)
                return NotFound(new { Message = "Asset Type not found." });

            return Ok(new { Message = "Asset Type deleted successfully." });
        }

        [HttpPost("DeleteRecipient/{id}")]
        public IActionResult DeleteRecipient(int id)
        {
            var result = _inventoryRepository.DeleteRecipientAsync(id);
            if (!result)
                return NotFound(new { Message = "Recipient Type not found." });

            return Ok(new { Message = "Recipient Type deleted successfully." });
        }

        [HttpPost("DeleteCategory/{id}")]
        public async Task<IActionResult> DeleteCategory(int id, string currentUser)
        {
            var result = await _inventoryRepository.DeleteCategoryAsync(id, currentUser);
            if (result)
                return Ok(new { success = true, message = "Category deleted (soft delete)" });
            return NotFound(new { success = false, message = "Category not found" });
        }

        [HttpPost("DeleteSupplier/{id}")]
        public IActionResult DeleteSupplier(int id)
        {
            var result = _inventoryRepository.DeleteSupplierAsync(id);
            if (!result)
                return NotFound(new { Message = "Supplier Type not found." });

            return Ok(new { Message = "Supplier Type deleted successfully." });
        }

        [HttpGet("CreditorInvoice_CheckOnBranchPayments/{invoiceID}")]
        public IActionResult CreditorInvoice_CheckOnBranchPayments(decimal invoiceID)
        {
            try
            {
                // Call your static method (replace with your actual logic)
                bool result = UtilityMain.CreditorInvoice_CheckOnBranchPayments(invoiceID);

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log exception if needed
                return StatusCode(500, new { message = "Error checking creditor invoice availability", details = ex.Message });
            }
        }

        [HttpGet("DeleteExpensesByID/{id}")]
        public IActionResult DeleteExpensesByID(int id, [FromQuery] string currentUser)
        {
            try
            {
                bool result = UtilityMain.DeleteExpensesByID(id, currentUser);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error deleting expenses",
                    details = ex.Message
                });
            }
        }


    }
}
