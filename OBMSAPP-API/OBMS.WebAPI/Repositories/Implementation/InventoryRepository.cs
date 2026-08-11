using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Repositories.Interface;
using OBMS.WebAPI.Utility;
using SkiaSharp;

namespace OBMS.WebAPI.Repositories.Implementation
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly OBMSDbContext _oBMSDbContext;

        public InventoryRepository(OBMSDbContext oBMSDbContext)
        {
            _oBMSDbContext = oBMSDbContext;

        }

        public async Task<InventoryCategory> SaveAndUpdateCategory(InventoryCategory category)
        {

            if (category.ID == 0)
            {
                _oBMSDbContext.InventoryCategories.Add(category);
            }
            else
            {
                _oBMSDbContext.InventoryCategories.Update(category);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return category;

            throw new NotImplementedException();
        }

        public async Task<List<Object>> GetCategories()
        {
            var ret = _oBMSDbContext.InventoryCategories.ToList().OrderBy(x => x.Name);
            return new List<Object>(ret);
        }

        public async Task<ActionResult<InventoryCategory>> GetCategoryByID(int ID)
        {
            var ret = _oBMSDbContext.InventoryCategories.Where(x => x.ID == ID).FirstOrDefault();
            return new ActionResult<InventoryCategory>(ret);

            throw new NotImplementedException();
        }


        public async Task<List<Object>> GetCategoriesByCat(string cat)
        {
            IEnumerable<InventoryCategory> ret;
            if (cat == "d")
            {
                ret = _oBMSDbContext.InventoryCategories.ToList().OrderBy(x => x.Name);                
            }
            else
            {
                ret = _oBMSDbContext.InventoryCategories.Where(x => x.Cat == cat).ToList().OrderBy(x => x.Name);
            }
            
            return new List<Object>(ret);
        }


        public async Task<ItemMaster> SaveAndUpdateItem(ItemMaster obj)
        {

            if (obj.ID == 0)
            {
                _oBMSDbContext.ItemMasters.Add(obj);
            }
            else
            {
                _oBMSDbContext.ItemMasters.Update(obj);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return obj;

            throw new NotImplementedException();
        }

        public async Task<List<Object>> GetItems()
        {
            var ret = _oBMSDbContext.ItemMasters.ToList().OrderBy(x => x.Name);
            var query = from item in _oBMSDbContext.ItemMasters
                        join category in _oBMSDbContext.InventoryCategories on item.CategoryID equals category.ID
                        select new
                        {
                            ID = item.ID,
                            ItemName = item.Name,
                            CategoryName = category.Name,
                            Quantity = item.Quantity,
                            Price = item.Price,
                            SellPrice = item.SellPrice,
                            Remarks = item.Remarks
                        };
            return new List<Object>(query);
        }

        public async Task<Dictionary<string, Object>> GetItemByID(int ID)
        {
            var category = new InventoryCategory();


            var results = new Dictionary<string, Object>();


            var item = _oBMSDbContext.ItemMasters.Where(x => x.ID == ID).FirstOrDefault();

            if(ID  > 0)
            {
                category = _oBMSDbContext.InventoryCategories.Where(x => x.ID == ID).FirstOrDefault();
            }
            else
            {
                category = _oBMSDbContext.InventoryCategories.FirstOrDefault();
            }
            

            results.Add("item", item);
            results.Add("type", category.Cat);
            results.Add("categories", GetCategoriesByCat(category.Cat));


            return results;
        }

        public async Task<AssetMaster> SaveAndUpdateAssetMaster(AssetMaster obj)
        {

            if (obj.ID == 0)
            {
                _oBMSDbContext.AssetMasters.Add(obj);
            }
            else
            {
                _oBMSDbContext.AssetMasters.Update(obj);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return obj;

            throw new NotImplementedException();
        }

        public async Task<List<Object>> GetAssetMasters()
        {
            var ret = _oBMSDbContext.AssetMasters.ToList();
            return new List<Object>(ret);
        }

        public async Task<AssetMaster> GetAssetByID(int ID)
        {


            var obj = _oBMSDbContext.AssetMasters.Where(x => x.ID == ID).FirstOrDefault();

            return obj;
        }


        public async Task<Dictionary<string, Object>> GetSupplierMaster()
        {

            var results = new Dictionary<string, Object>();

            results.Add("stateList", Utility.Utility.GetStateList());


            return results;
        }

        public Task<Dictionary<string, object>> GetSupplierMaster(int ID)
        {
            throw new NotImplementedException();
        }

        public async Task<Dictionary<string, Object>> GetSupplierCode()
        {
            var results = new Dictionary<string, Object>();

            var maxRow = _oBMSDbContext.Suppliers
                            .OrderByDescending(x => x.Code)
                            .FirstOrDefault();

            if (maxRow == null)
            {
                results.Add("Code", "S0001");
            }
            else
            {
                int nextCode = Convert.ToInt32(maxRow.Code.Substring(1, 4)) + 1;
                results.Add("Code", "S" + nextCode.ToString("D4"));
            }

            results.Add("stateList", Utility.Utility.GetStateList());
            return results;
        }

        public async Task<Supplier> SaveAndUpdateSupplier(Supplier obj)
        {
            if (obj.Id == 0)
            {
                _oBMSDbContext.Suppliers.Add(obj);
            }
            else
            {
                _oBMSDbContext.Suppliers.Update(obj);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return obj;

            throw new NotImplementedException();
        }

        public async Task<List<object>> GetSuppliers()
        {
            var ret = _oBMSDbContext.Suppliers.ToList();
            return new List<Object>(ret);
            throw new NotImplementedException();
        }

        public async Task<Supplier> GetSupplierByID(int ID)
        {
            var obj = _oBMSDbContext.Suppliers.Where(x => x.Id == ID).FirstOrDefault();

            return obj;
        }

        public async Task<Dictionary<string, Object>> GetRecipientMaster()
        {
            var results = new Dictionary<string, Object>();

            var categories = _oBMSDbContext.InventoryCategories.Where(x => x.Cat == "U")
                            .OrderBy(x => x.Name).ToList();

            results.Add("categories", categories);


            results.Add("stateList", Utility.Utility.GetStateList());
            return results;
        }

        public async Task<Recipient> SaveAndUpdateRecipient(Recipient obj)
        {
            if (obj.Id == 0)
            {
                _oBMSDbContext.Recipients.Add(obj);
            }
            else
            {
                _oBMSDbContext.Recipients.Update(obj);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return obj;
        }

        public async Task<List<object>> GetRecipients()
        {
            var ret = _oBMSDbContext.Recipients.ToList();
            return new List<Object>(ret);
            throw new NotImplementedException();
        }

        public async Task<Recipient> GetRecipientByID(int ID)
        {
            var obj = _oBMSDbContext.Recipients.Where(x => x.Id == ID).FirstOrDefault();

            return obj;
        }

        public async Task<Dictionary<string, Object>> GetUtilityBillsMasterList()
        {

            var results = new Dictionary<string, Object>();


            var branchs = _oBMSDbContext.BranchMasters.ToList();

            results.Add("branchList", branchs);

            var suppliers = _oBMSDbContext.Suppliers.OrderBy(x => x.Name).ToList();

            results.Add("supplierList", suppliers);

            var categories = _oBMSDbContext.InventoryCategories.Where(x => x.Cat == "U").OrderBy(x => x.Name).ToList();

            results.Add("categoryList", categories);



            var _recipient = from recipient in _oBMSDbContext.Recipients
                             where recipient.Status == "A"
                             orderby recipient.Name
                             select new
                             {
                                 recipient.Id,
                                 FullName = recipient.Name + " (Recipient)"
                             };

            results.Add("payList", _recipient);

            return results;


        }

        public async Task<Dictionary<string, Object>> GetPaytoByCategory(int categoryId)
        {
            var results = new Dictionary<string, Object>();
            var query1 = from recipient in _oBMSDbContext.Recipients
                         where recipient.Category == categoryId && recipient.Status == "A"
                         select new
                         {
                             ID = recipient.Id,
                             Name = recipient.Name + " (Recipient)"
                         };


            results.Add("List1", query1.OrderBy(x => x.Name).ToList());

            var query2 = from supplier in _oBMSDbContext.Suppliers
                         join category in _oBMSDbContext.InventoryCategories
                         on supplier.Category equals category.Cat
                         where category.ID == categoryId && supplier.Status == "A"
                         select new
                         {
                             ID = supplier.Id,
                             Name = supplier.Name + " (Supplier)"
                         };

           


            results.Add("List2", query2.OrderBy(x => x.Name).ToList());

            return results;
            throw new NotImplementedException();
        }

        public async Task<Dictionary<string, Object>> GetUtilityMasterList(string invCat, string supplierCat, string userID )
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

            var categoryList = _oBMSDbContext.InventoryCategories
    .Where(category => category.Cat == invCat || category.Cat == "O")
    .OrderBy(category => category.Name)
    .Select(category => new
    {
        category.ID,
        category.Name,
        category.Cat,
        category.AssetType
    })
    .ToList();


            if (supplierCat == "")
            {
                var suppliers = _oBMSDbContext.Suppliers
    .OrderBy(supplier => supplier.Name)
    .Select(supplier => new
    {
        supplier.Id,
        supplier.Name
    })
    .ToList();
                results.Add("supplierList", suppliers);
            }
            else
            {
                var result = _oBMSDbContext.Suppliers
    .Where(supplier => supplier.Category == supplierCat && supplier.Status == "A")
    .OrderBy(supplier => supplier.Name)
    .Select(supplier => new
    {
        supplier.Id,
        supplier.Name
    })
    .ToList();

                results.Add("supplierList", result);


            }

            results.Add("branchList", branchs);
            results.Add("categoryList", categoryList);



            return results;
        }

        public async Task<CreditorInvoice> SaveAndUpdateUtility(CreditorInvoice creditorInvoice)
        {
            if (creditorInvoice.ID == 0)
            {
                _oBMSDbContext.CreditorInvoices.Add(creditorInvoice);
            }
            else
            {
                _oBMSDbContext.CreditorInvoices.Update(creditorInvoice);
            }

            await _oBMSDbContext.SaveChangesAsync();


            //throw new NotImplementedException();
            return creditorInvoice;
        }

        public async Task<CreditorInvoiceDetails> SaveAndUpdateUtilityDetails(CreditorInvoiceDetails creditorInvoiceDetails)
        {
            if (creditorInvoiceDetails.ID == 0)
            {
                _oBMSDbContext.CreditorInvoiceDetails.Add(creditorInvoiceDetails);
            }
            else
            {
                _oBMSDbContext.CreditorInvoiceDetails.Update(creditorInvoiceDetails);
            }

            await _oBMSDbContext.SaveChangesAsync();


            //throw new NotImplementedException();
            return creditorInvoiceDetails;
        }


        public async Task<Dictionary<string, Object>> GetMaterialMasterList(string userID)
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

            var categoryList = _oBMSDbContext.InventoryCategories
    .OrderBy(category => category.Name)
    .Select(category => new
    {
        category.ID,
        category.Name,
        category.Cat,
        category.AssetType
    })
    .ToList();



            results.Add("branchList", branchs);
            results.Add("categoryList", categoryList);



            return results;
        }



        public async Task<StockIssues> SaveAndUpdateMaterial(StockIssues stockIssues)
        {
            if (stockIssues.ID == 0)
            {
                _oBMSDbContext.StockIssues.Add(stockIssues);
            }
            else
            {
                _oBMSDbContext.StockIssues.Update(stockIssues);
            }

            await _oBMSDbContext.SaveChangesAsync();


            //throw new NotImplementedException();
            return stockIssues;
        }

        public async Task<StockIssueDetail> SaveAndUpdateMaterialDetails(StockIssueDetail stockIssueDetail)
        {
            if (stockIssueDetail.ID == 0)
            {
                _oBMSDbContext.StockIssueDetails.Add(stockIssueDetail);
            }
            else
            {
                _oBMSDbContext.StockIssueDetails.Update(stockIssueDetail);
            }

            await _oBMSDbContext.SaveChangesAsync();


            //throw new NotImplementedException();
            return stockIssueDetail;
        }

        public async Task<bool> DeleteItemAsync(int id, string currentUser)
        {
            var item = await _oBMSDbContext.ItemMasters.FindAsync(id);
            if (item == null) return false;

            item.LASTUPDATE = DateTime.Now;
            item.LastUpdatedBy = currentUser;

            await _oBMSDbContext.SaveChangesAsync();
            return true;
        }

        public bool DeleteAssetType(int id)
        {
            var asset = _oBMSDbContext.AssetMasters.FirstOrDefault(a => a.ID == id);
            if (asset == null)
                return false;

            _oBMSDbContext.AssetMasters.Remove(asset);
            _oBMSDbContext.SaveChanges();
            return true;
        }

        public bool DeleteRecipientAsync(int id)
        {
            var recipient = _oBMSDbContext.Recipients.FirstOrDefault(a => a.Id == id);
            if (recipient == null)
                return false;

            _oBMSDbContext.Recipients.Remove(recipient);
            _oBMSDbContext.SaveChanges();
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id, string currentUser)
        {
            var category = await _oBMSDbContext.InventoryCategories.FindAsync(id);
            if (category == null) return false;

            category.LastUpdatedBy = currentUser;

            await _oBMSDbContext.SaveChangesAsync();
            return true;
        }

        public bool DeleteSupplierAsync(int id)
        {
            var supplier = _oBMSDbContext.suppliers.FirstOrDefault(a => a.Id == id);
            if (supplier == null)
                return false;

            _oBMSDbContext.suppliers.Remove(supplier);
            _oBMSDbContext.SaveChanges();
            return true;
        }
    }
}
