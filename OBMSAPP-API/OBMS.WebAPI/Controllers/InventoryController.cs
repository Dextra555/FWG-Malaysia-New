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
                category.ParentCategoryID = inventoryCategoryDto.ParentCategoryID == 0 ? null : inventoryCategoryDto.ParentCategoryID;

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
                    // Edit: load existing record to preserve Code and fields not in DTO
                    item = _oBMSDbContext.ItemMasters.Where(x => x.ID == dto.ID).FirstOrDefault() ?? new ItemMaster();
                }

                // Generate Code server-side on new record only
                if (dto.ID == 0)
                {
                    var maxRow = _oBMSDbContext.ItemMasters
                        .Where(x => x.Code != null && x.Code.StartsWith("ITM"))
                        .OrderByDescending(x => x.Code)
                        .FirstOrDefault();
                    if (maxRow == null || string.IsNullOrEmpty(maxRow.Code))
                        item.Code = "ITM0001";
                    else
                    {
                        if (int.TryParse(maxRow.Code.Substring(3), out int num))
                            item.Code = "ITM" + (num + 1).ToString("D4");
                        else
                            item.Code = "ITM0001";
                    }
                }
                // else: Code preserved from the loaded existing record

                item.ID = dto.ID;
                item.CategoryID = dto.CategoryID;
                item.BrandID = dto.BrandID == 0 ? null : dto.BrandID;
                item.UOMID = dto.UOMID == 0 ? null : dto.UOMID;
                item.Name = dto.Name ?? "";
                item.Price = dto.Price;
                item.LASTUPDATE = DateTime.Now;
                item.LastUpdatedBy = dto.LastUpdatedBy;
                item.Remarks = dto.Remarks;
                item.ReorderLevel = dto.ReorderLevel;
                item.ReorderQuantity = dto.ReorderQuantity;
                item.WarehouseID = dto.WarehouseID == 0 ? null : dto.WarehouseID;
                // SellPrice and Quantity: not mapped from DTO — preserved as-is on edit, null on new

                await _inventoryRepository.SaveAndUpdateItem(item);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", item);
                response.Headers.Add("Success", "Successfully save & update Item.");

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving item", details = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetItemCode")]
        public async Task<ActionResult<Object>> GetItemCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.ItemMasters
                    .Where(x => x.Code != null && x.Code.StartsWith("ITM"))
                    .OrderByDescending(x => x.Code)
                    .FirstOrDefault();

                string nextCode;
                if (maxRow == null || string.IsNullOrEmpty(maxRow.Code))
                    nextCode = "ITM0001";
                else if (int.TryParse(maxRow.Code.Substring(3), out int num))
                    nextCode = "ITM" + (num + 1).ToString("D4");
                else
                    nextCode = "ITM0001";

                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating item code", details = ex.Message });
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
        public async Task<ActionResult<Object>> GetSuppliers([FromQuery] string? category = null)
        {
            try
            {
                var obj = await _inventoryRepository.GetSuppliers(category);
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

        [HttpGet]
        [Route("GetRecipientCode")]
        public IActionResult GetRecipientCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.Recipients
                    .Where(x => x.Code != null && x.Code.StartsWith("RC"))
                    .OrderByDescending(x => x.Code)
                    .FirstOrDefault();

                string nextCode;
                if (maxRow == null || string.IsNullOrEmpty(maxRow.Code))
                    nextCode = "RC0001";
                else if (int.TryParse(maxRow.Code.Substring(2), out int num))
                    nextCode = "RC" + (num + 1).ToString("D4");
                else
                    nextCode = "RC0001";

                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating recipient code", details = ex.Message });
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
        public async Task<ActionResult<Object>> GetUtilityBillsMasterList(string userID)
        {
            try
            {
                var obj = await _inventoryRepository.GetUtilityBillsMasterList(userID);
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
            item.WarehouseID,
            CategoryName = category.Name
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
                                IM.Code,
                                IM.CategoryID,
                                IM.Name,
                                IM.Price
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


        // Part 1: Inventory Masters - Brand
        [HttpPost("SaveAndUpdateBrandMaster")]
        public async Task<ActionResult<BrandMaster>> SaveAndUpdateBrandMaster(BrandMasterRequestDto dto)
        {
            try
            {
                var brand = new BrandMaster();
                if (dto.ID != 0)
                {
                    brand = _oBMSDbContext.BrandMasters.Where(x => x.ID == dto.ID).FirstOrDefault() ?? new BrandMaster();
                }

                // Generate code server-side on new record; preserve on edit
                if (dto.ID == 0)
                {
                    var maxRow = _oBMSDbContext.BrandMasters
                        .OrderByDescending(x => x.Code)
                        .FirstOrDefault();
                    brand.Code = (maxRow == null || string.IsNullOrEmpty(maxRow.Code))
                        ? "BR0001"
                        : "BR" + (Convert.ToInt32(maxRow.Code.Substring(2)) + 1).ToString("D4");
                }
                // else: Code preserved from DB record loaded above

                brand.ID = dto.ID;
                brand.Name = dto.Name ?? "";
                brand.Description = dto.Description;
                brand.IsActive = dto.IsActive;
                brand.LASTUPDATE = DateTime.Now;
                brand.LastUpdatedBy = dto.LastUpdatedBy;

                await _inventoryRepository.SaveAndUpdateBrandMaster(brand);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", brand);

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving brand", details = ex.Message });
            }
        }

        [HttpGet("GetBrandMasters")]
        public async Task<ActionResult<List<BrandMaster>>> GetBrandMasters()
        {
            try
            {
                var brands = await _inventoryRepository.GetBrandMasters();
                return Ok(brands);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving brands", details = ex.Message });
            }
        }

        [HttpGet("GetBrandByID")]
        public async Task<ActionResult<BrandMaster>> GetBrandByID(int ID)
        {
            try
            {
                var brand = await _inventoryRepository.GetBrandByID(ID);
                if (brand == null)
                {
                    return NotFound();
                }
                return Ok(brand);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving brand", details = ex.Message });
            }
        }

        // Part 1: Inventory Masters - UOM
        [HttpPost("SaveAndUpdateUOMMaster")]
        public async Task<ActionResult<UOMMaster>> SaveAndUpdateUOMMaster(UOMMasterRequestDto dto)
        {
            try
            {
                var uom = new UOMMaster();
                if (dto.ID != 0)
                {
                    uom = _oBMSDbContext.UOMMasters.Where(x => x.ID == dto.ID).FirstOrDefault() ?? new UOMMaster();
                }

                if (dto.ID == 0)
                {
                    var maxRow = _oBMSDbContext.UOMMasters
                        .OrderByDescending(x => x.Code)
                        .FirstOrDefault();
                    uom.Code = (maxRow == null || string.IsNullOrEmpty(maxRow.Code))
                        ? "UM0001"
                        : "UM" + (Convert.ToInt32(maxRow.Code.Substring(2)) + 1).ToString("D4");
                }

                uom.ID = dto.ID;
                uom.Name = dto.Name ?? "";
                uom.ShortName = dto.ShortName;
                uom.IsPrimary = dto.IsPrimary;
                uom.LASTUPDATE = DateTime.Now;
                uom.LastUpdatedBy = dto.LastUpdatedBy;

                await _inventoryRepository.SaveAndUpdateUOMMaster(uom);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", uom);

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving UOM", details = ex.Message });
            }
        }

        [HttpGet("GetUOMMasters")]
        public async Task<ActionResult<List<UOMMaster>>> GetUOMMasters()
        {
            try
            {
                var uoms = await _inventoryRepository.GetUOMMasters();
                return Ok(uoms);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving UOMs", details = ex.Message });
            }
        }

        [HttpGet("GetUOMByID")]
        public async Task<ActionResult<UOMMaster>> GetUOMByID(int ID)
        {
            try
            {
                var uom = await _inventoryRepository.GetUOMByID(ID);
                if (uom == null)
                {
                    return NotFound();
                }
                return Ok(uom);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving UOM", details = ex.Message });
            }
        }

        // Part 1: Inventory Masters - Warehouse
        [HttpPost("SaveAndUpdateWarehouseMaster")]
        public async Task<ActionResult<WarehouseMaster>> SaveAndUpdateWarehouseMaster(WarehouseMasterRequestDto dto)
        {
            try
            {
                var warehouse = new WarehouseMaster();
                if (dto.ID != 0)
                {
                    warehouse = _oBMSDbContext.WarehouseMasters.Where(x => x.ID == dto.ID).FirstOrDefault() ?? new WarehouseMaster();
                }

                if (dto.ID == 0)
                {
                    var maxRow = _oBMSDbContext.WarehouseMasters
                        .OrderByDescending(x => x.Code)
                        .FirstOrDefault();
                    warehouse.Code = (maxRow == null || string.IsNullOrEmpty(maxRow.Code))
                        ? "WH0001"
                        : "WH" + (Convert.ToInt32(maxRow.Code.Substring(2)) + 1).ToString("D4");
                }

                warehouse.ID = dto.ID;
                warehouse.Name = dto.Name ?? "";
                warehouse.Address = dto.Address ?? "";
                warehouse.BranchID = dto.BranchID;
                warehouse.IsActive = dto.IsActive;
                warehouse.LASTUPDATE = DateTime.Now;
                warehouse.LastUpdatedBy = dto.LastUpdatedBy;

                await _inventoryRepository.SaveAndUpdateWarehouseMaster(warehouse);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("data", warehouse);

                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving warehouse", details = ex.Message });
            }
        }

        [HttpGet("GetWarehouseMasters")]
        public async Task<ActionResult<List<WarehouseMaster>>> GetWarehouseMasters()
        {
            try
            {
                var warehouses = await _inventoryRepository.GetWarehouseMasters();
                return Ok(warehouses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving warehouses", details = ex.Message });
            }
        }

        [HttpGet("GetWarehouseByID")]
        public async Task<ActionResult<WarehouseMaster>> GetWarehouseByID(int ID)
        {
            try
            {
                var warehouse = await _inventoryRepository.GetWarehouseByID(ID);
                if (warehouse == null)
                {
                    return NotFound();
                }
                return Ok(warehouse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving warehouse", details = ex.Message });
            }
        }

        // Part 1: Delete endpoints
        [HttpPost("DeleteBrand/{id}")]
        public IActionResult DeleteBrand(int id)
        {
            try
            {
                // Check if any Item is using this Brand before deleting
                bool isInUse = _oBMSDbContext.ItemMasters.Any(x => x.BrandID == id);
                if (isInUse)
                    return BadRequest(new { Message = "Cannot delete. This Brand is assigned to one or more Items. Please reassign those items first." });

                var result = _inventoryRepository.DeleteBrandAsync(id);
                if (!result)
                    return NotFound(new { Message = "Brand not found." });
                return Ok(new { Success = "Success", Message = "Brand deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error deleting brand.", details = ex.Message });
            }
        }

        [HttpPost("DeleteUOM/{id}")]
        public IActionResult DeleteUOM(int id)
        {
            try
            {
                var result = _inventoryRepository.DeleteUOMAsync(id);
                if (!result)
                    return NotFound(new { Message = "UOM not found." });
                return Ok(new { Success = "Success", Message = "UOM deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error deleting UOM.", details = ex.Message });
            }
        }

        [HttpPost("DeleteWarehouse/{id}")]
        public IActionResult DeleteWarehouse(int id)
        {
            try
            {
                var result = _inventoryRepository.DeleteWarehouseAsync(id);
                if (!result)
                    return NotFound(new { Message = "Warehouse not found." });
                return Ok(new { Success = "Success", Message = "Warehouse deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error deleting warehouse.", details = ex.Message });
            }
        }

        // Part 1: Code generation endpoints
        [HttpGet("GetBrandCode")]
        public async Task<ActionResult<Object>> GetBrandCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.BrandMasters
                    .OrderByDescending(x => x.Code)
                    .FirstOrDefault();

                string nextCode;
                if (maxRow == null || string.IsNullOrEmpty(maxRow.Code))
                {
                    nextCode = "BR0001";
                }
                else
                {
                    int numericPart = Convert.ToInt32(maxRow.Code.Substring(2)) + 1;
                    nextCode = "BR" + numericPart.ToString("D4");
                }

                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating brand code", details = ex.Message });
            }
        }

        [HttpGet("GetUOMCode")]
        public async Task<ActionResult<Object>> GetUOMCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.UOMMasters
                    .OrderByDescending(x => x.Code)
                    .FirstOrDefault();

                string nextCode;
                if (maxRow == null || string.IsNullOrEmpty(maxRow.Code))
                {
                    nextCode = "UM0001";
                }
                else
                {
                    int numericPart = Convert.ToInt32(maxRow.Code.Substring(2)) + 1;
                    nextCode = "UM" + numericPart.ToString("D4");
                }

                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating UOM code", details = ex.Message });
            }
        }

        [HttpGet("GetWarehouseCode")]
        public async Task<ActionResult<Object>> GetWarehouseCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.WarehouseMasters
                    .OrderByDescending(x => x.Code)
                    .FirstOrDefault();

                string nextCode;
                if (maxRow == null || string.IsNullOrEmpty(maxRow.Code))
                {
                    nextCode = "WH0001";
                }
                else
                {
                    int numericPart = Convert.ToInt32(maxRow.Code.Substring(2)) + 1;
                    nextCode = "WH" + numericPart.ToString("D4");
                }

                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating warehouse code", details = ex.Message });
            }
        }


        // ── Part 2: Opening Stock endpoints ─────────────────────────────────────

        /// <summary>Returns all active opening stock entries, enriched with names.</summary>
        [HttpGet("GetOpeningStockList")]
        public async Task<IActionResult> GetOpeningStockList()
        {
            try
            {
                var list = await _inventoryRepository.GetOpeningStockList();
                return Ok(list);
            }
            catch (Exception ex)
            {
                // Log internally — do not expose exception to client
                Console.Error.WriteLine($"[GetOpeningStockList] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load the opening stock list right now. Please try again. If the problem continues, contact your administrator."
                });
            }
        }

        /// <summary>Returns a single opening stock entry by ID.</summary>
        [HttpGet("GetOpeningStockByID")]
        public async Task<IActionResult> GetOpeningStockByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetOpeningStockByID(ID);
                if (record == null)
                    return NotFound(new { success = false, message = "Opening stock entry not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetOpeningStockByID] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load the opening stock entry right now. Please try again."
                });
            }
        }

        /// <summary>Returns the next auto-generated opening stock reference number.</summary>
        [HttpGet("GetOpeningStockCode")]
        public IActionResult GetOpeningStockCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.OpeningStocks
                    .Where(x => x.ReferenceNo.StartsWith("OS"))
                    .OrderByDescending(x => x.ReferenceNo)
                    .FirstOrDefault();

                string nextCode;
                if (maxRow == null || string.IsNullOrEmpty(maxRow.ReferenceNo))
                    nextCode = "OS0001";
                else if (int.TryParse(maxRow.ReferenceNo.Substring(2), out int num))
                    nextCode = "OS" + (num + 1).ToString("D4");
                else
                    nextCode = "OS0001";

                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetOpeningStockCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        /// <summary>
        /// Saves a new opening stock entry or updates an existing one.
        /// Transactionally maintains a corresponding OPENING ledger row.
        /// </summary>
        [HttpPost("SaveOpeningStock")]
        public async Task<IActionResult> SaveOpeningStock([FromBody] OpeningStockRequestDto dto)
        {
            // ── Server-side validation ──────────────────────────────────────────
            if (dto.WarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select a warehouse." });

            if (dto.ItemID <= 0)
                return BadRequest(new { success = false, message = "Please select an item." });

            if (dto.OpeningQty <= 0)
                return BadRequest(new { success = false, message = "Opening quantity must be greater than zero." });

            if (dto.UnitCost < 0)
                return BadRequest(new { success = false, message = "Unit cost cannot be negative." });

            if (dto.OpeningDate == default)
                return BadRequest(new { success = false, message = "Please enter the opening date." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SaveOpeningStock(dto);

                if (!success)
                    return BadRequest(new { success = false, message });

                return Ok(new { success = true, message, data = record });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveOpeningStock] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to save the opening stock right now. Please try again. If the problem continues, contact your administrator."
                });
            }
        }

        /// <summary>Soft-deletes an opening stock entry and its ledger row.</summary>
        [HttpPost("DeleteOpeningStock/{id}")]
        public async Task<IActionResult> DeleteOpeningStock(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.DeleteOpeningStock(id, currentUser ?? "");

                if (!success)
                    return BadRequest(new { success = false, message });

                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DeleteOpeningStock] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to remove the opening stock entry right now. Please try again."
                });
            }
        }

        // ── Part 2: Stock Ledger endpoints ───────────────────────────────────────

        /// <summary>
        /// Returns stock ledger rows for the given filters with running balance per row.
        /// Required: warehouseID, dateFrom, dateTo.
        /// Optional: itemID (0 = all items), transactionType ("ALL" or specific code).
        /// </summary>
        [HttpGet("GetStockLedger")]
        public async Task<IActionResult> GetStockLedger(
            int warehouseID,
            int? itemID,
            DateTime dateFrom,
            DateTime dateTo,
            string? transactionType = "ALL")
        {
            if (warehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select a warehouse." });

            if (dateFrom == default || dateTo == default)
                return BadRequest(new { success = false, message = "Please enter a valid date range." });

            if (dateFrom.Date > dateTo.Date)
                return BadRequest(new { success = false, message = "The 'From' date must be before the 'To' date." });

            try
            {
                var rows = await _inventoryRepository.GetStockLedger(
                    warehouseID, itemID, dateFrom, dateTo, transactionType);

                return Ok(rows);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockLedger] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load the stock ledger right now. Please try again."
                });
            }
        }

        /// <summary>Returns the current stock balance for a given item in a warehouse.</summary>
        [HttpGet("GetCurrentStock")]
        public async Task<IActionResult> GetCurrentStock(int warehouseID, int itemID)
        {
            if (warehouseID <= 0 || itemID <= 0)
                return BadRequest(new { success = false, message = "Please provide a valid warehouse and item." });

            try
            {
                var balance = await _inventoryRepository.GetCurrentStock(warehouseID, itemID);

                // Enrich the response with item and warehouse names for display
                var item      = await _oBMSDbContext.ItemMasters.FindAsync(itemID);
                var warehouse = await _oBMSDbContext.WarehouseMasters.FindAsync(warehouseID);
                var uom       = item?.UOMID != null
                    ? await _oBMSDbContext.UOMMasters.FindAsync(item.UOMID)
                    : null;

                return Ok(new
                {
                    success       = true,
                    warehouseID,
                    warehouseName = warehouse?.Name ?? "",
                    itemID,
                    itemName      = item?.Name ?? "",
                    uomName       = uom?.Name ?? "",
                    currentStock  = balance
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetCurrentStock] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to retrieve current stock. Please try again."
                });
            }
        }

        /// <summary>
        /// Returns the list of active items enriched with Category, Brand and UOM names.
        /// Used to populate the item dropdown on the Opening Stock form.
        /// </summary>
        [HttpGet("GetItemsForStock")]
        public async Task<IActionResult> GetItemsForStock()
        {
            try
            {
                var result = await (
                    from im in _oBMSDbContext.ItemMasters
                    join ic in _oBMSDbContext.InventoryCategories on im.CategoryID equals ic.ID into icJoin
                    from ic in icJoin.DefaultIfEmpty()
                    join br in _oBMSDbContext.BrandMasters on im.BrandID equals br.ID into brJoin
                    from br in brJoin.DefaultIfEmpty()
                    join um in _oBMSDbContext.UOMMasters   on im.UOMID   equals um.ID into umJoin
                    from um in umJoin.DefaultIfEmpty()
                    join wh in _oBMSDbContext.WarehouseMasters on im.WarehouseID equals wh.ID into whJoin
                    from wh in whJoin.DefaultIfEmpty()
                    orderby im.Name
                    select new
                    {
                        im.ID,
                        im.Code,
                        im.Name,
                        im.UOMID,
                        im.Price,           // Buying / cost price — auto-filled into UnitPrice / UnitCost on item select
                        CategoryName  = ic != null ? ic.Name : "",
                        BrandName     = br != null ? br.Name : "",
                        UOMName       = um != null ? um.Name : "",
                        UOMShort      = um != null ? um.ShortName : "",
                        WarehouseID   = im.WarehouseID,
                        WarehouseName = wh != null ? wh.Name : ""
                    }
                ).ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetItemsForStock] {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load item list. Please try again."
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 3 — PURCHASE REQUISITION ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("GetPRList")]
        public async Task<IActionResult> GetPRList()
        {
            try { return Ok(await _inventoryRepository.GetPRList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPRList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the purchase requisition list right now. Please try again." });
            }
        }

        [HttpGet("GetPRByID")]
        public async Task<IActionResult> GetPRByID(int ID)
        {
            try
            {
                var pr = await _inventoryRepository.GetPRByID(ID);
                if (pr == null) return NotFound(new { success = false, message = "Purchase requisition not found." });
                return Ok(pr);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPRByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the purchase requisition right now. Please try again." });
            }
        }

        [HttpGet("GetPRCode")]
        public IActionResult GetPRCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.PurchaseRequisitions
                    .Where(x => x.PRNumber != null && x.PRNumber.StartsWith("PR"))
                    .OrderByDescending(x => x.PRNumber)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.PRNumber))
                    ? "PR0001"
                    : int.TryParse(maxRow.PRNumber.Substring(2), out int n)
                        ? "PR" + (n + 1).ToString("D4") : "PR0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPRCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        [HttpPost("SavePR")]
        public async Task<IActionResult> SavePR([FromBody] PRRequestDto dto)
        {
            if (dto.PRDate == default)
                return BadRequest(new { success = false, message = "Please enter the requisition date." });
            if (dto.RequiredByDate == default)
                return BadRequest(new { success = false, message = "Please enter the required by date." });
            if (!dto.Details.Any())
                return BadRequest(new { success = false, message = "Please add at least one item to the requisition." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SavePR(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, prNumber = record?.PRNumber });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SavePR] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the purchase requisition right now. Please try again. If the problem continues, contact your administrator." });
            }
        }

        [HttpPost("SubmitPR/{id}")]
        public async Task<IActionResult> SubmitPR(int id, [FromQuery] string currentUser)
        {
            // Use the JWT-authenticated username; ignore any client-supplied value
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });

            try
            {
                var (success, message) = await _inventoryRepository.SubmitPR(id, jwtUser);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SubmitPR] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to submit the purchase requisition right now. Please try again." });
            }
        }

        [HttpPost("ApprovePR/{id}")]
        public async Task<IActionResult> ApprovePR(int id, [FromBody] PRApprovalDto dto)
        {
            // Use the JWT-authenticated username; do NOT trust ApprovedBy from the request body
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });

            // Override ApprovedBy with the authenticated identity
            dto.ApprovedBy = jwtUser;
            // BUG-05 FIX: stamp IsSuperAdmin from JWT "IsAdmin" claim; never trust client value
            dto.IsSuperAdmin = User.FindFirst("IsAdmin")?.Value == "2";

            try
            {
                var (success, message) = await _inventoryRepository.ApprovePR(id, dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ApprovePR] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to approve the purchase requisition right now. Please try again." });
            }
        }

        [HttpPost("RejectPR/{id}")]
        public async Task<IActionResult> RejectPR(int id, [FromBody] PRApprovalDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ApprovalRemarks))
                return BadRequest(new { success = false, message = "Please enter a reason for rejection so the requester knows what to correct." });

            // Use the JWT-authenticated username; do NOT trust ApprovedBy from the request body
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });

            // Override ApprovedBy with the authenticated identity
            dto.ApprovedBy = jwtUser;
            // BUG-05 FIX: stamp IsSuperAdmin from JWT "IsAdmin" claim; never trust client value
            dto.IsSuperAdmin = User.FindFirst("IsAdmin")?.Value == "2";

            try
            {
                var (success, message) = await _inventoryRepository.RejectPR(id, dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RejectPR] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to reject the purchase requisition right now. Please try again." });
            }
        }

        [HttpPost("DeletePR/{id}")]
        public async Task<IActionResult> DeletePR(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.DeletePR(id, currentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DeletePR] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to delete the purchase requisition right now. Please try again." });
            }
        }

        [HttpGet("GetApprovedPRsForPO")]
        public async Task<IActionResult> GetApprovedPRsForPO()
        {
            try { return Ok(await _inventoryRepository.GetApprovedPRsForPO()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetApprovedPRsForPO] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load approved requisitions right now. Please try again." });            }
        }

        // ── PR Processing Tasks (Purchase department claim / inbox) ──────────────

        /// <summary>
        /// Returns all active (Pending / Claimed) PR processing tasks.
        /// Used by the Purchase department inbox to see the work queue.
        /// </summary>
        [HttpGet("GetPRProcessingTasks")]
        public async Task<IActionResult> GetPRProcessingTasks()
        {
            try { return Ok(await _inventoryRepository.GetPRProcessingTasks()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPRProcessingTasks] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the processing task list right now. Please try again." });
            }
        }

        /// <summary>
        /// Returns the processing task for a single PR by its PRID.
        /// Called by the PR form on load to display claim state (claimed/unclaimed/completed).
        /// Returns 404 when no task exists (PR not yet approved).
        /// </summary>
        [HttpGet("GetPRProcessingTaskByPR")]
        public async Task<IActionResult> GetPRProcessingTaskByPR([FromQuery] int prid)
        {
            if (prid <= 0)
                return BadRequest(new { success = false, message = "Please provide a valid PR ID." });
            try
            {
                var task = await _inventoryRepository.GetPRProcessingTaskByPR(prid);
                if (task == null) return NotFound(new { success = false, message = "No processing task found for this PR." });
                return Ok(task);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPRProcessingTaskByPR] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the processing task right now. Please try again." });
            }
        }

        /// <summary>
        /// Atomically claims an unclaimed PR processing task for the authenticated user.
        /// Only the first concurrent caller succeeds — a conditional UPDATE enforces this.
        /// Returns 409 Conflict when the task has already been claimed by someone else.
        /// </summary>
        [HttpPost("ClaimPR/{prid}")]
        public async Task<IActionResult> ClaimPR(int prid)
        {
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });
            try
            {
                var (success, message) = await _inventoryRepository.ClaimPR(prid, jwtUser);
                if (!success) return Conflict(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ClaimPR] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to claim the purchase requisition right now. Please try again." });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 3 — PURCHASE ORDER ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("GetPOList")]
        public async Task<IActionResult> GetPOList()
        {
            try { return Ok(await _inventoryRepository.GetPOList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPOList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the purchase order list right now. Please try again." });
            }
        }

        [HttpGet("GetPOByID")]
        public async Task<IActionResult> GetPOByID(int ID)
        {
            try
            {
                var po = await _inventoryRepository.GetPOByID(ID);
                if (po == null) return NotFound(new { success = false, message = "Purchase order not found." });
                return Ok(po);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPOByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the purchase order right now. Please try again." });
            }
        }

        /// <summary>
        /// Returns the existing active PO linked to a given PR ID, or null if none exists.
        /// Used by the Angular new-po component to prevent duplicate PO creation from
        /// the same approved PR when a notification is clicked multiple times.
        /// </summary>
        [HttpGet("GetPOByPRID")]
        public async Task<IActionResult> GetPOByPRID(int prID)
        {
            try
            {
                var po = await _inventoryRepository.GetPOByPRID(prID);
                if (po == null) return Ok(null);
                return Ok(po);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPOByPRID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to check existing purchase order. Please try again." });
            }
        }

        [HttpGet("GetPOCode")]
        public IActionResult GetPOCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.PurchaseOrders
                    .Where(x => x.PONumber != null && x.PONumber.StartsWith("PO"))
                    .OrderByDescending(x => x.PONumber)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.PONumber))
                    ? "PO0001"
                    : int.TryParse(maxRow.PONumber.Substring(2), out int n)
                        ? "PO" + (n + 1).ToString("D4") : "PO0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPOCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        [HttpPost("SavePO")]
        public async Task<IActionResult> SavePO([FromBody] PORequestDto dto)
        {
            if (dto.SupplierID <= 0)
                return BadRequest(new { success = false, message = "Please select a supplier." });
            if (dto.WarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select the warehouse where the goods will be received." });
            if (dto.PODate == default)
                return BadRequest(new { success = false, message = "Please enter the purchase order date." });
            if (dto.ExpectedDate == default)
                return BadRequest(new { success = false, message = "Please select a valid expected delivery date." });
            if (!dto.Details.Any())
                return BadRequest(new { success = false, message = "Please add at least one item to the purchase order." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SavePO(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, poNumber = record?.PONumber });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SavePO] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the purchase order right now. Please try again. If the problem continues, contact your administrator." });
            }
        }

        /// <summary>
        /// Legacy PO approval endpoint — kept for backward compatibility but now secured.
        /// Delegates to ApprovalController.ApprovePOWithHistory via the repository,
        /// which enforces JWT identity and routing authorization.
        /// New callers should use POST /api/Approval/ApprovePOWithHistory/{id}.
        /// </summary>
        [HttpPost("ApprovePO/{id}")]
        public async Task<IActionResult> ApprovePO(int id, [FromBody] POApprovalDto dto)
        {
            // Use JWT identity — do NOT trust ApprovedBy from the request body
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });

            try
            {
                // Route through ApprovalRepository which enforces routing authorization
                var (success, message) = await _inventoryRepository.ApprovePO(
                    id, new POApprovalDto {
                        ApprovedBy   = jwtUser,
                        IsSuperAdmin = User.FindFirst("IsAdmin")?.Value == "2"  // BUG-13 FIX
                    });
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ApprovePO] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to approve the purchase order right now. Please try again." });
            }
        }

        [HttpPost("CancelPO/{id}")]
        public async Task<IActionResult> CancelPO(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.CancelPO(id, currentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CancelPO] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to cancel the purchase order right now. Please try again." });
            }
        }

        /// <summary>
        /// Returns the editability state of a PO: can it be edited, is the item list
        /// locked (PR-linked), and why it is blocked if not editable.
        /// Called by the PO form on load so the frontend can configure field locks.
        /// </summary>
        [HttpGet("GetPOEditability")]
        public async Task<IActionResult> GetPOEditability(int id)
        {
            try
            {
                var result = await _inventoryRepository.GetPOEditability(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPOEditability] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to check purchase order editability right now. Please try again." });
            }
        }


        // ════════════════════════════════════════════════════════════════════════
        // PART 3 — GRN ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("GetGRNList")]
        public async Task<IActionResult> GetGRNList()
        {
            try { return Ok(await _inventoryRepository.GetGRNList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetGRNList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the goods receipt list right now. Please try again." });
            }
        }

        [HttpGet("GetGRNByID")]
        public async Task<IActionResult> GetGRNByID(int ID)
        {
            try
            {
                var grn = await _inventoryRepository.GetGRNByID(ID);
                if (grn == null) return NotFound(new { success = false, message = "Goods receipt not found." });
                return Ok(grn);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetGRNByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the goods receipt right now. Please try again." });
            }
        }

        /// <summary>
        /// Returns the first active GRN for a given PO ID, or null if none.
        /// Used by the PO screen to show a "GRN already posted" status banner.
        /// </summary>
        [HttpGet("GetGRNByPOID")]
        public async Task<IActionResult> GetGRNByPOID(int poID)
        {
            try
            {
                var grn = await _inventoryRepository.GetGRNByPOID(poID);
                if (grn == null) return Ok(null);
                return Ok(grn);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetGRNByPOID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to check goods receipt. Please try again." });
            }
        }

        [HttpGet("GetGRNCode")]
        public IActionResult GetGRNCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.GoodsReceiptNotes
                    .Where(x => x.GRNNumber != null && x.GRNNumber.StartsWith("GRN"))
                    .OrderByDescending(x => x.GRNNumber)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.GRNNumber))
                    ? "GRN0001"
                    : int.TryParse(maxRow.GRNNumber.Substring(3), out int n)
                        ? "GRN" + (n + 1).ToString("D4") : "GRN0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetGRNCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        [HttpGet("GetApprovedPOsForGRN")]
        public async Task<IActionResult> GetApprovedPOsForGRN()
        {
            try { return Ok(await _inventoryRepository.GetApprovedPOsForGRN()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetApprovedPOsForGRN] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load purchase orders right now. Please try again." });
            }
        }

        [HttpGet("GetPOLinesForGRN")]
        public async Task<IActionResult> GetPOLinesForGRN(int POID)
        {
            if (POID <= 0)
                return BadRequest(new { success = false, message = "Please select a purchase order." });
            try { return Ok(await _inventoryRepository.GetPOLinesForGRN(POID)); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPOLinesForGRN] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load purchase order items right now. Please try again." });
            }
        }

        [HttpPost("SaveGRN")]
        public async Task<IActionResult> SaveGRN([FromBody] GRNRequestDto dto)
        {
            if (dto.POID <= 0)
                return BadRequest(new { success = false, message = "Please select a purchase order to receive against." });
            if (dto.WarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select the warehouse where the goods will be received." });
            if (dto.GRNDate == default)
                return BadRequest(new { success = false, message = "Please enter the goods receipt date." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SaveGRN(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, grnNumber = record?.GRNNumber });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveGRN] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the goods receipt right now. Please try again. If the problem continues, contact your administrator." });
            }
        }

        /// <summary>
        /// Returns the dependency status for a GRN — used by the frontend to decide
        /// whether to show a delete button. Returns canDelete:true/false and a reason.
        /// </summary>
        [HttpGet("CheckGRNDependency")]
        public async Task<IActionResult> CheckGRNDependency(int id)
        {
            try
            {
                var (canDelete, blockingMessage, blockingDocNumber) = await _inventoryRepository.CheckGRNDependency(id);
                return Ok(new { canDelete, blockingMessage, blockingDocNumber });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CheckGRNDependency] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to check goods receipt dependencies right now. Please try again." });
            }
        }

        [HttpPost("DeleteGRN/{id}")]
        public async Task<IActionResult> DeleteGRN(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.DeleteGRN(id, currentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DeleteGRN] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to delete the goods receipt right now. Please try again. If the problem continues, contact your administrator." });
            }
        }


        // ════════════════════════════════════════════════════════════════════════
        // PART 3 — PURCHASE RETURN ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("GetPurchaseReturnList")]
        public async Task<IActionResult> GetPurchaseReturnList()
        {
            try { return Ok(await _inventoryRepository.GetPurchaseReturnList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPurchaseReturnList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the purchase return list right now. Please try again." });
            }
        }

        [HttpGet("GetPurchaseReturnByID")]
        public async Task<IActionResult> GetPurchaseReturnByID(int ID)
        {
            try
            {
                var ret = await _inventoryRepository.GetPurchaseReturnByID(ID);
                if (ret == null) return NotFound(new { success = false, message = "Purchase return not found." });
                return Ok(ret);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPurchaseReturnByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the purchase return right now. Please try again." });
            }
        }

        [HttpGet("GetReturnCode")]
        public IActionResult GetReturnCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.PurchaseReturns
                    .Where(x => x.ReturnNumber != null && x.ReturnNumber.StartsWith("RTN"))
                    .OrderByDescending(x => x.ReturnNumber)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.ReturnNumber))
                    ? "RTN0001"
                    : int.TryParse(maxRow.ReturnNumber.Substring(3), out int n)
                        ? "RTN" + (n + 1).ToString("D4") : "RTN0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetReturnCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        [HttpGet("GetGRNsForReturn")]
        public async Task<IActionResult> GetGRNsForReturn()
        {
            try { return Ok(await _inventoryRepository.GetGRNsForReturn()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetGRNsForReturn] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load goods receipts right now. Please try again." });
            }
        }

        [HttpGet("GetGRNLinesForReturn")]
        public async Task<IActionResult> GetGRNLinesForReturn(int GRNID)
        {
            if (GRNID <= 0)
                return BadRequest(new { success = false, message = "Please select a goods receipt." });
            try { return Ok(await _inventoryRepository.GetGRNLinesForReturn(GRNID)); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetGRNLinesForReturn] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load goods receipt items right now. Please try again." });
            }
        }

        [HttpPost("SavePurchaseReturn")]
        public async Task<IActionResult> SavePurchaseReturn([FromBody] PurchaseReturnRequestDto dto)
        {
            if (dto.GRNID <= 0)
                return BadRequest(new { success = false, message = "Please select the goods receipt to return against." });
            if (dto.WarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select the warehouse for this return." });
            if (dto.ReturnDate == default)
                return BadRequest(new { success = false, message = "Please enter the return date." });
            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { success = false, message = "Please enter a reason for the return." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SavePurchaseReturn(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, returnNumber = record?.ReturnNumber });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SavePurchaseReturn] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the purchase return right now. Please try again. If the problem continues, contact your administrator." });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 4 — STOCK ISSUE ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("GetStockIssueList")]
        public async Task<IActionResult> GetStockIssueList()
        {
            try { return Ok(await _inventoryRepository.GetStockIssueList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockIssueList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the stock issue list right now. Please try again." });
            }
        }

        [HttpGet("GetStockIssueByID")]
        public async Task<IActionResult> GetStockIssueByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetStockIssueByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Stock issue not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockIssueByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the stock issue right now. Please try again." });
            }
        }

        [HttpGet("GetStockIssueCode")]
        public IActionResult GetStockIssueCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.StockIssueDocs
                    .Where(x => x.IssueNo.StartsWith("SI") && x.IsActive)
                    .OrderByDescending(x => x.IssueNo)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.IssueNo))
                    ? "SI0001"
                    : int.TryParse(maxRow.IssueNo.Substring(2), out int n)
                        ? "SI" + (n + 1).ToString("D4") : "SI0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockIssueCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        [HttpPost("SaveStockIssue")]
        public async Task<IActionResult> SaveStockIssue([FromBody] StockIssueDocRequestDto dto)
        {
            if (dto.WarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select a warehouse." });
            if (dto.IssueDate == default)
                return BadRequest(new { success = false, message = "Please enter the issue date." });
            if (string.IsNullOrWhiteSpace(dto.IssueToType))
                return BadRequest(new { success = false, message = "Please select who the stock is being issued to." });
            if (!dto.Details.Any())
                return BadRequest(new { success = false, message = "Please add at least one item to the stock issue." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SaveStockIssue(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, issueNo = record?.IssueNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveStockIssue] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the stock issue right now. Please try again. If the problem continues, contact your administrator." });
            }
        }

        [HttpPost("DeleteStockIssue/{id}")]
        public async Task<IActionResult> DeleteStockIssue(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.DeleteStockIssue(id, currentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DeleteStockIssue] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to delete the stock issue right now. Please try again." });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 4 — STOCK ISSUE RETURN ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("GetIssuableDocsForReturn")]
        public async Task<IActionResult> GetIssuableDocsForReturn()
        {
            try { return Ok(await _inventoryRepository.GetIssuableDocsForReturn()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetIssuableDocsForReturn] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the stock issue list right now. Please try again." });
            }
        }

        [HttpGet("GetIssueDetailsForReturn")]
        public async Task<IActionResult> GetIssueDetailsForReturn(int issueDocID)
        {
            if (issueDocID <= 0)
                return BadRequest(new { success = false, message = "Please select a stock issue document." });
            try { return Ok(await _inventoryRepository.GetIssueDetailsForReturn(issueDocID)); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetIssueDetailsForReturn] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the issue lines right now. Please try again." });
            }
        }

        [HttpGet("GetStockIssueReturnList")]
        public async Task<IActionResult> GetStockIssueReturnList()
        {
            try { return Ok(await _inventoryRepository.GetStockIssueReturnList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockIssueReturnList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the stock issue return list right now. Please try again." });
            }
        }

        [HttpGet("GetStockIssueReturnByID")]
        public async Task<IActionResult> GetStockIssueReturnByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetStockIssueReturnByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Stock issue return not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockIssueReturnByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the stock issue return right now. Please try again." });
            }
        }

        [HttpGet("GetStockIssueReturnCode")]
        public IActionResult GetStockIssueReturnCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.StockIssueReturns
                    .Where(x => x.ReturnNo.StartsWith("SIR") && x.IsActive)
                    .OrderByDescending(x => x.ReturnNo)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.ReturnNo))
                    ? "SIR0001"
                    : int.TryParse(maxRow.ReturnNo.Substring(3), out int n)
                        ? "SIR" + (n + 1).ToString("D4") : "SIR0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockIssueReturnCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        [HttpPost("SaveStockIssueReturn")]
        public async Task<IActionResult> SaveStockIssueReturn([FromBody] StockIssueReturnRequestDto dto)
        {
            if (dto.OriginalIssueDocID <= 0)
                return BadRequest(new { success = false, message = "Please select the original stock issue." });
            if (dto.WarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select the warehouse." });
            if (dto.ReturnDate == default)
                return BadRequest(new { success = false, message = "Please enter the return date." });
            if (!dto.Details.Any())
                return BadRequest(new { success = false, message = "Please add at least one item to the return." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SaveStockIssueReturn(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, returnNo = record?.ReturnNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveStockIssueReturn] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the stock issue return right now. Please try again. If the problem continues, contact your administrator." });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 4 — STOCK TRANSFER ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("GetStockTransferList")]
        public async Task<IActionResult> GetStockTransferList()
        {
            try { return Ok(await _inventoryRepository.GetStockTransferList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockTransferList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the stock transfer list right now. Please try again." });
            }
        }

        [HttpGet("GetStockTransferByID")]
        public async Task<IActionResult> GetStockTransferByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetStockTransferByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Stock transfer not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockTransferByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the stock transfer right now. Please try again." });
            }
        }

        [HttpGet("GetStockTransferCode")]
        public IActionResult GetStockTransferCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.StockTransfers
                    .Where(x => x.TransferNo.StartsWith("TRF") && x.IsActive)
                    .OrderByDescending(x => x.TransferNo)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.TransferNo))
                    ? "TRF0001"
                    : int.TryParse(maxRow.TransferNo.Substring(3), out int n)
                        ? "TRF" + (n + 1).ToString("D4") : "TRF0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockTransferCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        [HttpPost("SaveStockTransfer")]
        public async Task<IActionResult> SaveStockTransfer([FromBody] StockTransferRequestDto dto)
        {
            if (dto.SourceWarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select the source warehouse." });
            if (dto.DestWarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select the destination warehouse." });
            if (dto.SourceWarehouseID == dto.DestWarehouseID)
                return BadRequest(new { success = false, message = "Source and destination warehouses must be different." });
            if (dto.TransferDate == default)
                return BadRequest(new { success = false, message = "Please enter the transfer date." });
            if (!dto.Details.Any())
                return BadRequest(new { success = false, message = "Please add at least one item to the transfer." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SaveStockTransfer(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, transferNo = record?.TransferNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveStockTransfer] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the stock transfer right now. Please try again. If the problem continues, contact your administrator." });
            }
        }

        [HttpPost("DeleteStockTransfer/{id}")]
        public async Task<IActionResult> DeleteStockTransfer(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.DeleteStockTransfer(id, currentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DeleteStockTransfer] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to cancel the stock transfer right now. Please try again." });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 4 — STOCK ADJUSTMENT ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("GetStockAdjustmentList")]
        public async Task<IActionResult> GetStockAdjustmentList()
        {
            try { return Ok(await _inventoryRepository.GetStockAdjustmentList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockAdjustmentList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the stock adjustment list right now. Please try again." });
            }
        }

        [HttpGet("GetStockAdjustmentByID")]
        public async Task<IActionResult> GetStockAdjustmentByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetStockAdjustmentByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Stock adjustment not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockAdjustmentByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the stock adjustment right now. Please try again." });
            }
        }

        [HttpGet("GetStockAdjustmentCode")]
        public IActionResult GetStockAdjustmentCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.StockAdjustments
                    .Where(x => x.AdjustmentNo.StartsWith("ADJ") && x.IsActive)
                    .OrderByDescending(x => x.AdjustmentNo)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.AdjustmentNo))
                    ? "ADJ0001"
                    : int.TryParse(maxRow.AdjustmentNo.Substring(3), out int n)
                        ? "ADJ" + (n + 1).ToString("D4") : "ADJ0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockAdjustmentCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        [HttpPost("SaveStockAdjustment")]
        public async Task<IActionResult> SaveStockAdjustment([FromBody] StockAdjustmentRequestDto dto)
        {
            if (dto.WarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select a warehouse." });
            if (dto.ItemID <= 0)
                return BadRequest(new { success = false, message = "Please select an item." });
            if (dto.AdjustmentDate == default)
                return BadRequest(new { success = false, message = "Please enter the adjustment date." });
            if (string.IsNullOrWhiteSpace(dto.AdjustmentType) || (dto.AdjustmentType != "PLUS" && dto.AdjustmentType != "MINUS"))
                return BadRequest(new { success = false, message = "Please select an adjustment type (Plus or Minus)." });
            if (dto.AdjustmentQty <= 0)
                return BadRequest(new { success = false, message = "Adjustment quantity must be greater than zero." });
            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { success = false, message = "Please enter a reason for the adjustment." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SaveStockAdjustment(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, adjustmentNo = record?.AdjustmentNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveStockAdjustment] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the stock adjustment right now. Please try again. If the problem continues, contact your administrator." });
            }
        }

        [HttpPost("DeleteStockAdjustment/{id}")]
        public async Task<IActionResult> DeleteStockAdjustment(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.DeleteStockAdjustment(id, currentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DeleteStockAdjustment] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to delete the stock adjustment right now. Please try again." });
            }
        }

        [HttpPost("SubmitSA/{id}")]
        public async Task<IActionResult> SubmitSA(int id)
        {
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });
            try
            {
                var (success, message) = await _inventoryRepository.SubmitSA(id, jwtUser);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SubmitSA] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to submit the stock adjustment right now. Please try again." });
            }
        }

        [HttpPost("ApproveSA/{id}")]
        public async Task<IActionResult> ApproveSA(int id, [FromBody] SAApprovalDto dto)
        {
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });
            if (dto == null) dto = new SAApprovalDto();
            dto.ApprovedBy   = jwtUser;
            dto.IsSuperAdmin = User.FindFirst("IsAdmin")?.Value == "2";
            try
            {
                var (success, message) = await _inventoryRepository.ApproveSA(id, dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ApproveSA] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to approve the stock adjustment right now. Please try again." });
            }
        }

        [HttpPost("RejectSA/{id}")]
        public async Task<IActionResult> RejectSA(int id, [FromBody] SAApprovalDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.ApprovalRemarks))
                return BadRequest(new { success = false, message = "Please enter a reason for rejection so the requester knows what to correct." });
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });
            dto.ApprovedBy   = jwtUser;
            dto.IsSuperAdmin = User.FindFirst("IsAdmin")?.Value == "2";
            try
            {
                var (success, message) = await _inventoryRepository.RejectSA(id, dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RejectSA] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to reject the stock adjustment right now. Please try again." });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 4 — DAMAGE / LOSS ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("GetStockDamageLossList")]
        public async Task<IActionResult> GetStockDamageLossList()
        {
            try { return Ok(await _inventoryRepository.GetStockDamageLossList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockDamageLossList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the damage/loss list right now. Please try again." });
            }
        }

        [HttpGet("GetStockDamageLossByID")]
        public async Task<IActionResult> GetStockDamageLossByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetStockDamageLossByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Damage/loss record not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockDamageLossByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the damage/loss record right now. Please try again." });
            }
        }

        [HttpGet("GetStockDamageLossCode")]
        public IActionResult GetStockDamageLossCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.StockDamageLosses
                    .Where(x => x.RecordNo.StartsWith("DL") && x.IsActive)
                    .OrderByDescending(x => x.RecordNo)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.RecordNo))
                    ? "DL0001"
                    : int.TryParse(maxRow.RecordNo.Substring(2), out int n)
                        ? "DL" + (n + 1).ToString("D4") : "DL0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockDamageLossCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number. Please try again." });
            }
        }

        [HttpPost("SaveStockDamageLoss")]
        public async Task<IActionResult> SaveStockDamageLoss([FromBody] StockDamageLossRequestDto dto)
        {
            if (dto.WarehouseID <= 0)
                return BadRequest(new { success = false, message = "Please select a warehouse." });
            if (dto.ItemID <= 0)
                return BadRequest(new { success = false, message = "Please select an item." });
            if (dto.RecordDate == default)
                return BadRequest(new { success = false, message = "Please enter the record date." });
            if (dto.Quantity <= 0)
                return BadRequest(new { success = false, message = "Quantity must be greater than zero." });
            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { success = false, message = "Please enter a reason for the damage/loss." });

            try
            {
                var (success, message, record) = await _inventoryRepository.SaveStockDamageLoss(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, recordNo = record?.RecordNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveStockDamageLoss] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the damage/loss record right now. Please try again. If the problem continues, contact your administrator." });
            }
        }

        [HttpPost("DeleteStockDamageLoss/{id}")]
        public async Task<IActionResult> DeleteStockDamageLoss(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.DeleteStockDamageLoss(id, currentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DeleteStockDamageLoss] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to delete the damage/loss record right now. Please try again." });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 5 — EMPLOYEE/GUARD ITEM TRACKING + REPORTS
        // ════════════════════════════════════════════════════════════════════════

        // ── Employee / Guard Item Holding ─────────────────────────────────────

        /// <summary>
        /// Returns one summary row per employee/guard showing
        /// total items held, total value, and last issue date.
        /// </summary>
        [HttpGet("GetEmployeeHoldingSummaryList")]
        public async Task<IActionResult> GetEmployeeHoldingSummaryList()
        {
            try
            {
                var list = await _inventoryRepository.GetEmployeeHoldingSummaryList();
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetEmployeeHoldingSummaryList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load staff holding summary right now. Please try again." });
            }
        }

        /// <summary>
        /// Returns per-item net holding for a single employee/guard.
        /// issueToType: "Employee" | "Guard" | blank for both.
        /// </summary>
        [HttpGet("GetEmployeeHoldingDetail")]
        public async Task<IActionResult> GetEmployeeHoldingDetail(int employeeID, string? issueToType)
        {
            if (employeeID <= 0)
                return BadRequest(new { success = false, message = "Please provide a valid staff/guard ID." });
            try
            {
                var list = await _inventoryRepository.GetEmployeeHoldingDetail(employeeID, issueToType);
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetEmployeeHoldingDetail] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load staff item details right now. Please try again." });
            }
        }

        /// <summary>
        /// Returns full item-level holdings for ALL employees/guards with net holding > 0.
        /// issueToType: "Employee" | "Guard" | blank for both.
        /// </summary>
        [HttpGet("GetAllEmployeeHoldings")]
        public async Task<IActionResult> GetAllEmployeeHoldings(string? issueToType)
        {
            try
            {
                var list = await _inventoryRepository.GetAllEmployeeHoldings(issueToType);
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAllEmployeeHoldings] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load staff holdings right now. Please try again." });
            }
        }

        // ── Stock Balance Summary ─────────────────────────────────────────────

        /// <summary>
        /// Returns current stock balance per item per warehouse.
        /// warehouseID = 0 → all warehouses.
        /// itemID = 0 → all items.
        /// Flags items at or below reorder level.
        /// </summary>
        [HttpGet("GetStockBalanceSummary")]
        public async Task<IActionResult> GetStockBalanceSummary(int? warehouseID, int? itemID)
        {
            try
            {
                int? wh   = (warehouseID.HasValue && warehouseID > 0) ? warehouseID : null;
                int? item = (itemID.HasValue && itemID > 0) ? itemID : null;
                var list = await _inventoryRepository.GetStockBalanceSummary(wh, item);
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetStockBalanceSummary] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load stock balance summary right now. Please try again." });
            }
        }

        // ── Employee Issue Register ───────────────────────────────────────────

        /// <summary>
        /// Returns all Employee/Guard issue lines in a date range.
        /// employeeID = 0 → all employees.
        /// issueToType: "Employee" | "Guard" | "ALL".
        /// </summary>
        [HttpGet("GetEmployeeIssueRegister")]
        public async Task<IActionResult> GetEmployeeIssueRegister(
            DateTime dateFrom,
            DateTime dateTo,
            int? employeeID,
            string? issueToType = "ALL")
        {
            if (dateFrom == default || dateTo == default)
                return BadRequest(new { success = false, message = "Please enter a valid date range." });

            if (dateFrom.Date > dateTo.Date)
                return BadRequest(new { success = false, message = "The 'From' date must be before the 'To' date." });

            try
            {
                int? empID = (employeeID.HasValue && employeeID > 0) ? employeeID : null;
                var list = await _inventoryRepository.GetEmployeeIssueRegister(
                    dateFrom, dateTo, empID, issueToType);
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetEmployeeIssueRegister] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the staff issue register right now. Please try again." });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 6 — ASSET MANAGEMENT ENDPOINTS
        // ════════════════════════════════════════════════════════════════════════

        // ── Asset Master ──────────────────────────────────────────────────────

        [HttpGet("GetAssetList")]
        public async Task<IActionResult> GetAssetList()
        {
            try { return Ok(await _inventoryRepository.GetAssetList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the asset list right now. Please try again." });
            }
        }

        [HttpGet("GetAssetByIDEnriched")]
        public async Task<IActionResult> GetAssetByIDEnriched(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetAssetByIDEnriched(ID);
                if (record == null) return NotFound(new { success = false, message = "Asset not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetByIDEnriched] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the asset right now. Please try again." });
            }
        }

        [HttpGet("GetAssetCode")]
        public IActionResult GetAssetCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.AssetMasters
                    .Where(x => x.AssetCode != null && x.AssetCode.StartsWith("AST") && x.IsActive)
                    .OrderByDescending(x => x.AssetCode)
                    .FirstOrDefault();
                string nextCode = (maxRow == null || string.IsNullOrEmpty(maxRow.AssetCode))
                    ? "AST0001"
                    : int.TryParse(maxRow.AssetCode.Substring(3), out int n)
                        ? "AST" + (n + 1).ToString("D4") : "AST0001";
                return Ok(new { Code = nextCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate asset code. Please try again." });
            }
        }

        [HttpPost("SaveAsset")]
        public async Task<IActionResult> SaveAsset([FromBody] AssetMasterRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "Please enter the asset name." });
            if (string.IsNullOrWhiteSpace(dto.AssetType))
                return BadRequest(new { success = false, message = "Please select the asset type." });
            try
            {
                var (success, message, record) = await _inventoryRepository.SaveAsset(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, assetCode = record?.AssetCode });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveAsset] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the asset right now. Please try again." });
            }
        }

        [HttpPost("DeleteAssetRecord/{id}")]
        public async Task<IActionResult> DeleteAssetRecord(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.DeleteAsset(id, currentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DeleteAssetRecord] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to delete the asset right now. Please try again." });
            }
        }

        // ── Asset Allocation ──────────────────────────────────────────────────

        [HttpGet("GetAssetAllocationList")]
        public async Task<IActionResult> GetAssetAllocationList([FromQuery] bool activeOnly = false)
        {
            try { return Ok(await _inventoryRepository.GetAssetAllocationList(activeOnly)); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetAllocationList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the allocation list right now. Please try again." });
            }
        }

        [HttpGet("GetAssetAllocationByID")]
        public async Task<IActionResult> GetAssetAllocationByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetAssetAllocationByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Allocation record not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetAllocationByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the allocation right now. Please try again." });
            }
        }

        [HttpGet("GetActiveAllocationsByAsset")]
        public async Task<IActionResult> GetActiveAllocationsByAsset(int assetID)
        {
            try { return Ok(await _inventoryRepository.GetActiveAllocationsByAsset(assetID)); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetActiveAllocationsByAsset] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load allocations for this asset. Please try again." });
            }
        }

        [HttpGet("GetAllocationCode")]
        public IActionResult GetAllocationCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.AssetAllocations
                    .Where(x => x.AllocationNo.StartsWith("AA"))
                    .OrderByDescending(x => x.AllocationNo)
                    .FirstOrDefault();
                string next = (maxRow == null) ? "AA0001"
                    : int.TryParse(maxRow.AllocationNo.Substring(2), out int n)
                        ? "AA" + (n + 1).ToString("D4") : "AA0001";
                return Ok(new { Code = next });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAllocationCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number." });
            }
        }

        [HttpPost("SaveAssetAllocation")]
        public async Task<IActionResult> SaveAssetAllocation([FromBody] AssetAllocationRequestDto dto)
        {
            if (dto.AssetID <= 0)
                return BadRequest(new { success = false, message = "Please select an asset." });
            if (string.IsNullOrWhiteSpace(dto.AllocatedToType))
                return BadRequest(new { success = false, message = "Please select the allocation type (Branch / Staff / Guard)." });
            if (string.IsNullOrWhiteSpace(dto.AllocatedToName))
                return BadRequest(new { success = false, message = "Please select who the asset is being allocated to." });
            if (dto.AllocationDate == default)
                return BadRequest(new { success = false, message = "Please enter the allocation date." });
            try
            {
                var (success, message, record) = await _inventoryRepository.SaveAssetAllocation(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, allocationNo = record?.AllocationNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveAssetAllocation] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the allocation right now. Please try again." });
            }
        }

        [HttpPost("CancelAssetAllocation/{id}")]
        public async Task<IActionResult> CancelAssetAllocation(int id, [FromQuery] string currentUser)
        {
            try
            {
                var (success, message) = await _inventoryRepository.CancelAssetAllocation(id, currentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CancelAssetAllocation] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to cancel the allocation right now. Please try again." });
            }
        }

        // ── Asset Transfer ────────────────────────────────────────────────────

        [HttpGet("GetAssetTransferList")]
        public async Task<IActionResult> GetAssetTransferList()
        {
            try { return Ok(await _inventoryRepository.GetAssetTransferList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetTransferList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the transfer list right now. Please try again." });
            }
        }

        [HttpGet("GetAssetTransferByID")]
        public async Task<IActionResult> GetAssetTransferByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetAssetTransferByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Transfer record not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetTransferByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the transfer right now. Please try again." });
            }
        }

        [HttpGet("GetTransferCode")]
        public IActionResult GetTransferCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.AssetTransfers
                    .Where(x => x.TransferNo.StartsWith("AT"))
                    .OrderByDescending(x => x.TransferNo)
                    .FirstOrDefault();
                string next = (maxRow == null) ? "AT0001"
                    : int.TryParse(maxRow.TransferNo.Substring(2), out int n)
                        ? "AT" + (n + 1).ToString("D4") : "AT0001";
                return Ok(new { Code = next });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetTransferCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number." });
            }
        }

        [HttpPost("SaveAssetTransfer")]
        public async Task<IActionResult> SaveAssetTransfer([FromBody] AssetTransferRequestDto dto)
        {
            if (dto.AssetID <= 0)
                return BadRequest(new { success = false, message = "Please select an asset." });
            if (string.IsNullOrWhiteSpace(dto.ToType) || string.IsNullOrWhiteSpace(dto.ToName))
                return BadRequest(new { success = false, message = "Please select the destination for the transfer." });
            if (dto.TransferDate == default)
                return BadRequest(new { success = false, message = "Please enter the transfer date." });
            try
            {
                var (success, message, record) = await _inventoryRepository.SaveAssetTransfer(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, transferNo = record?.TransferNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveAssetTransfer] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the transfer right now. Please try again." });
            }
        }

        // ── Asset Return ──────────────────────────────────────────────────────

        [HttpGet("GetAssetReturnList")]
        public async Task<IActionResult> GetAssetReturnList()
        {
            try { return Ok(await _inventoryRepository.GetAssetReturnList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetReturnList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the return list right now. Please try again." });
            }
        }

        [HttpGet("GetAssetReturnByID")]
        public async Task<IActionResult> GetAssetReturnByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetAssetReturnByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Return record not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetReturnByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the return record right now. Please try again." });
            }
        }

        [HttpGet("GetReturnCodeAsset")]
        public IActionResult GetReturnCodeAsset()
        {
            try
            {
                var maxRow = _oBMSDbContext.AssetReturns
                    .Where(x => x.ReturnNo.StartsWith("AR"))
                    .OrderByDescending(x => x.ReturnNo)
                    .FirstOrDefault();
                string next = (maxRow == null) ? "AR0001"
                    : int.TryParse(maxRow.ReturnNo.Substring(2), out int n)
                        ? "AR" + (n + 1).ToString("D4") : "AR0001";
                return Ok(new { Code = next });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetReturnCodeAsset] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number." });
            }
        }

        [HttpPost("SaveAssetReturn")]
        public async Task<IActionResult> SaveAssetReturn([FromBody] AssetReturnRequestDto dto)
        {
            if (dto.AssetID <= 0)
                return BadRequest(new { success = false, message = "Please select an asset." });
            if (dto.AllocationID <= 0)
                return BadRequest(new { success = false, message = "Please select the allocation to return against." });
            if (dto.ReturnDate == default)
                return BadRequest(new { success = false, message = "Please enter the return date." });
            try
            {
                var (success, message, record) = await _inventoryRepository.SaveAssetReturn(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, returnNo = record?.ReturnNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveAssetReturn] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the return right now. Please try again." });
            }
        }

        // ── Asset Maintenance ─────────────────────────────────────────────────

        [HttpGet("GetAssetMaintenanceList")]
        public async Task<IActionResult> GetAssetMaintenanceList([FromQuery] int? assetID)
        {
            try { return Ok(await _inventoryRepository.GetAssetMaintenanceList(assetID > 0 ? assetID : null)); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetMaintenanceList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the maintenance list right now. Please try again." });
            }
        }

        [HttpGet("GetAssetMaintenanceByID")]
        public async Task<IActionResult> GetAssetMaintenanceByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetAssetMaintenanceByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Maintenance record not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetMaintenanceByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the maintenance record right now. Please try again." });
            }
        }

        [HttpGet("GetMaintenanceCode")]
        public IActionResult GetMaintenanceCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.AssetMaintenances
                    .Where(x => x.MaintenanceNo.StartsWith("AM"))
                    .OrderByDescending(x => x.MaintenanceNo)
                    .FirstOrDefault();
                string next = (maxRow == null) ? "AM0001"
                    : int.TryParse(maxRow.MaintenanceNo.Substring(2), out int n)
                        ? "AM" + (n + 1).ToString("D4") : "AM0001";
                return Ok(new { Code = next });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetMaintenanceCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number." });
            }
        }

        [HttpPost("SaveAssetMaintenance")]
        public async Task<IActionResult> SaveAssetMaintenance([FromBody] AssetMaintenanceRequestDto dto)
        {
            if (dto.AssetID <= 0)
                return BadRequest(new { success = false, message = "Please select an asset." });
            if (string.IsNullOrWhiteSpace(dto.MaintenanceType))
                return BadRequest(new { success = false, message = "Please select the maintenance type." });
            if (dto.StartDate == default)
                return BadRequest(new { success = false, message = "Please enter the start date." });
            try
            {
                var (success, message, record) = await _inventoryRepository.SaveAssetMaintenance(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, maintenanceNo = record?.MaintenanceNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveAssetMaintenance] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the maintenance record right now. Please try again." });
            }
        }

        [HttpPost("CompleteAssetMaintenance/{id}")]
        public async Task<IActionResult> CompleteAssetMaintenance(
            int id,
            [FromBody] CompleteMaintenanceDto dto)
        {
            if (dto.CompletionDate == default)
                return BadRequest(new { success = false, message = "Please enter the completion date." });
            try
            {
                var (success, message) = await _inventoryRepository.CompleteAssetMaintenance(
                    id, dto.CompletionDate, dto.Cost, dto.Remarks ?? "", dto.CurrentUser ?? "");
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CompleteAssetMaintenance] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to complete the maintenance record right now. Please try again." });
            }
        }

        // ── Asset Damage ──────────────────────────────────────────────────────

        [HttpGet("GetAssetDamageList")]
        public async Task<IActionResult> GetAssetDamageList([FromQuery] int? assetID)
        {
            try { return Ok(await _inventoryRepository.GetAssetDamageList(assetID > 0 ? assetID : null)); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetDamageList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the damage list right now. Please try again." });
            }
        }

        [HttpGet("GetAssetDamageByID")]
        public async Task<IActionResult> GetAssetDamageByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetAssetDamageByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Damage record not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetDamageByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the damage record right now. Please try again." });
            }
        }

        [HttpGet("GetDamageCode")]
        public IActionResult GetDamageCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.AssetDamages
                    .Where(x => x.DamageNo.StartsWith("AD"))
                    .OrderByDescending(x => x.DamageNo)
                    .FirstOrDefault();
                string next = (maxRow == null) ? "AD0001"
                    : int.TryParse(maxRow.DamageNo.Substring(2), out int n)
                        ? "AD" + (n + 1).ToString("D4") : "AD0001";
                return Ok(new { Code = next });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetDamageCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number." });
            }
        }

        [HttpPost("SaveAssetDamage")]
        public async Task<IActionResult> SaveAssetDamage([FromBody] AssetDamageRequestDto dto)
        {
            if (dto.AssetID <= 0)
                return BadRequest(new { success = false, message = "Please select an asset." });
            if (string.IsNullOrWhiteSpace(dto.DamageDescription))
                return BadRequest(new { success = false, message = "Please enter a description of the damage." });
            if (dto.DamageDate == default)
                return BadRequest(new { success = false, message = "Please enter the damage date." });
            try
            {
                var (success, message, record) = await _inventoryRepository.SaveAssetDamage(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, damageNo = record?.DamageNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveAssetDamage] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the damage record right now. Please try again." });
            }
        }

        // ── Asset Disposal ────────────────────────────────────────────────────

        [HttpGet("GetAssetDisposalList")]
        public async Task<IActionResult> GetAssetDisposalList()
        {
            try { return Ok(await _inventoryRepository.GetAssetDisposalList()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetDisposalList] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the disposal list right now. Please try again." });
            }
        }

        [HttpGet("GetAssetDisposalByID")]
        public async Task<IActionResult> GetAssetDisposalByID(int ID)
        {
            try
            {
                var record = await _inventoryRepository.GetAssetDisposalByID(ID);
                if (record == null) return NotFound(new { success = false, message = "Disposal record not found." });
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetDisposalByID] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the disposal record right now. Please try again." });
            }
        }

        [HttpGet("GetDisposalCode")]
        public IActionResult GetDisposalCode()
        {
            try
            {
                var maxRow = _oBMSDbContext.AssetDisposals
                    .Where(x => x.DisposalNo.StartsWith("DIS"))
                    .OrderByDescending(x => x.DisposalNo)
                    .FirstOrDefault();
                string next = (maxRow == null) ? "DIS0001"
                    : int.TryParse(maxRow.DisposalNo.Substring(3), out int n)
                        ? "DIS" + (n + 1).ToString("D4") : "DIS0001";
                return Ok(new { Code = next });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetDisposalCode] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to generate reference number." });
            }
        }

        [HttpPost("SaveAssetDisposal")]
        public async Task<IActionResult> SaveAssetDisposal([FromBody] AssetDisposalRequestDto dto)
        {
            if (dto.AssetID <= 0)
                return BadRequest(new { success = false, message = "Please select an asset." });
            if (string.IsNullOrWhiteSpace(dto.DisposalType))
                return BadRequest(new { success = false, message = "Please select the disposal type." });
            if (string.IsNullOrWhiteSpace(dto.DisposalReason))
                return BadRequest(new { success = false, message = "Please enter a reason for the disposal." });
            if (dto.DisposalDate == default)
                return BadRequest(new { success = false, message = "Please enter the disposal date." });
            try
            {
                var (success, message, record) = await _inventoryRepository.SaveAssetDisposal(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message, id = record?.ID, disposalNo = record?.DisposalNo });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveAssetDisposal] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to save the disposal record right now. Please try again." });
            }
        }

        [HttpPost("SubmitAD/{id}")]
        public async Task<IActionResult> SubmitAD(int id)
        {
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });
            try
            {
                var (success, message) = await _inventoryRepository.SubmitAD(id, jwtUser);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SubmitAD] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to submit the disposal record right now. Please try again." });
            }
        }

        [HttpPost("ApproveAD/{id}")]
        public async Task<IActionResult> ApproveAD(int id, [FromBody] ADApprovalDto dto)
        {
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });
            if (dto == null) dto = new ADApprovalDto();
            dto.ApprovedBy   = jwtUser;
            dto.IsSuperAdmin = User.FindFirst("IsAdmin")?.Value == "2";
            try
            {
                var (success, message) = await _inventoryRepository.ApproveAD(id, dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ApproveAD] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to approve the disposal right now. Please try again." });
            }
        }

        [HttpPost("RejectAD/{id}")]
        public async Task<IActionResult> RejectAD(int id, [FromBody] ADApprovalDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.ApprovalRemarks))
                return BadRequest(new { success = false, message = "Please enter a reason for rejection." });
            var jwtUser = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(jwtUser))
                return Unauthorized(new { success = false, message = "Unable to identify the current user. Please log in again." });
            dto.ApprovedBy   = jwtUser;
            dto.IsSuperAdmin = User.FindFirst("IsAdmin")?.Value == "2";
            try
            {
                var (success, message) = await _inventoryRepository.RejectAD(id, dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RejectAD] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to reject the disposal right now. Please try again." });
            }
        }

        // ── Asset Movement History ────────────────────────────────────────────

        [HttpGet("GetAssetMovementHistory")]
        public async Task<IActionResult> GetAssetMovementHistory(int assetID)
        {
            if (assetID <= 0)
                return BadRequest(new { success = false, message = "Please provide a valid asset ID." });
            try { return Ok(await _inventoryRepository.GetAssetMovementHistory(assetID)); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetMovementHistory] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the asset movement history right now. Please try again." });
            }
        }
        // ── Asset Location Tracking ───────────────────────────────────────────

        [HttpPost("UpdateAssetLocation")]
        public async Task<IActionResult> UpdateAssetLocation([FromBody] AssetLocationUpdateDto dto)
        {
            if (dto.AssetID <= 0)
                return BadRequest(new { success = false, message = "Please select an asset." });
            if (string.IsNullOrWhiteSpace(dto.NewLocation))
                return BadRequest(new { success = false, message = "Please enter the current location." });
            if (string.IsNullOrWhiteSpace(dto.LocationStatus))
                return BadRequest(new { success = false, message = "Please select the location status." });
            try
            {
                var (success, message) = await _inventoryRepository.UpdateAssetLocation(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[UpdateAssetLocation] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to update location right now. Please try again." });
            }
        }

        [HttpGet("GetAssetLocationLog")]
        public async Task<IActionResult> GetAssetLocationLog(int assetID)
        {
            if (assetID <= 0)
                return BadRequest(new { success = false, message = "Please provide a valid asset ID." });
            try { return Ok(await _inventoryRepository.GetAssetLocationLog(assetID)); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetAssetLocationLog] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load location history right now. Please try again." });
            }
        }

        [HttpGet("GetDefectLocationAssets")]
        public async Task<IActionResult> GetDefectLocationAssets()
        {
            try { return Ok(await _inventoryRepository.GetDefectLocationAssets()); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetDefectLocationAssets] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load defect location assets right now. Please try again." });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // Stock Issue → Uniform Loan integration endpoints
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns active employees for the warehouse's branch, filtered by IssueToType.
        /// Guard → Guard / FGuard / Foreign Guard.  Staff → Staff.
        /// </summary>
        [HttpGet("GetEmployeesByWarehouse")]
        public async Task<IActionResult> GetEmployeesByWarehouse(
            [FromQuery] int warehouseID,
            [FromQuery] string issueToType)
        {
            if (warehouseID <= 0)
                return BadRequest(new { success = false, message = "Please provide a valid warehouse ID." });
            if (string.IsNullOrWhiteSpace(issueToType))
                return BadRequest(new { success = false, message = "Please provide the issue-to type (Guard or Staff)." });
            try
            {
                var list = await _inventoryRepository.GetEmployeesByWarehouse(warehouseID, issueToType);
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetEmployeesByWarehouse] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load the employee list right now. Please try again." });
            }
        }

        /// <summary>
        /// Returns all Stock Issue documents issued to the given employee
        /// that are NOT already linked to an active Uniform Loan.
        /// </summary>
        [HttpGet("GetPendingStockIssuesForEmployee")]
        public async Task<IActionResult> GetPendingStockIssuesForEmployee(
            [FromQuery] int employeeID)
        {
            if (employeeID <= 0)
                return BadRequest(new { success = false, message = "Please provide a valid employee ID." });
            try
            {
                var list = await _inventoryRepository.GetPendingStockIssuesForEmployee(employeeID);
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetPendingStockIssuesForEmployee] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to load pending stock issues right now. Please try again." });
            }
        }

        /// <summary>
        /// Links a StockIssueDoc to a Uniform Loan (SalaryAdvance TransType=4).
        /// Called AFTER the Uniform Loan has been saved successfully.
        /// </summary>
        [HttpPost("LinkStockIssueToUniformLoan")]
        public async Task<IActionResult> LinkStockIssueToUniformLoan(
            [FromBody] LinkStockIssueToLoanRequestDto dto)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Invalid request payload." });
            try
            {
                var (success, message) = await _inventoryRepository.LinkStockIssueToUniformLoan(dto);
                if (!success) return BadRequest(new { success = false, message });
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[LinkStockIssueToUniformLoan] {ex}");
                return StatusCode(500, new { success = false, message = "Unable to link the stock issue to the uniform loan right now. Please try again." });
            }
        }
    }
}

// ── CompleteMaintenanceDto — inline DTO for the complete-maintenance endpoint ──
namespace OBMS.WebAPI.Models.DTO
{
    public class CompleteMaintenanceDto
    {
        public DateTime CompletionDate { get; set; }
        public decimal Cost { get; set; }
        public string? Remarks { get; set; }
        public string? CurrentUser { get; set; }
    }
}
