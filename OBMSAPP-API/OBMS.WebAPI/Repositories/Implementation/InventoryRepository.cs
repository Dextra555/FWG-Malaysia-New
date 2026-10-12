using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using OBMS.WebAPI.Utility;
using SkiaSharp;

namespace OBMS.WebAPI.Repositories.Implementation
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly OBMSDbContext _oBMSDbContext;
        // Part 7: injected for history recording + notifications
        private readonly INotificationRepository? _notif;
        private readonly IApprovalRepository?     _approval;

        /// <summary>Legacy constructor — keeps existing DI registrations working.</summary>
        public InventoryRepository(OBMSDbContext oBMSDbContext)
        {
            _oBMSDbContext = oBMSDbContext;
        }

        /// <summary>Full constructor with Part 7 services.</summary>
        public InventoryRepository(
            OBMSDbContext oBMSDbContext,
            INotificationRepository notif,
            IApprovalRepository approval)
        {
            _oBMSDbContext = oBMSDbContext;
            _notif         = notif;
            _approval      = approval;
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
            var query = from cat in _oBMSDbContext.InventoryCategories
                        join parent in _oBMSDbContext.InventoryCategories
                            on cat.ParentCategoryID equals parent.ID into parentJoin
                        from parent in parentJoin.DefaultIfEmpty()
                        orderby cat.Name
                        select new
                        {
                            cat.ID,
                            cat.Name,
                            cat.Cat,
                            cat.AssetType,
                            cat.ParentCategoryID,
                            cat.LastUpdatedBy,
                            ParentCategoryName = parent != null ? parent.Name : ""
                        };
            return new List<Object>(query.ToList());
        }

        public async Task<ActionResult<InventoryCategory>> GetCategoryByID(int ID)
        {
            var ret = _oBMSDbContext.InventoryCategories.Where(x => x.ID == ID).FirstOrDefault();
            return new ActionResult<InventoryCategory>(ret);

            throw new NotImplementedException();
        }


        public async Task<List<Object>> GetCategoriesByCat(string cat)
        {
            IQueryable<InventoryCategory> query = _oBMSDbContext.InventoryCategories;

            if (cat != "d")
            {
                query = query.Where(x => x.Cat == cat);
            }

            var ret = query
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.ID,
                    x.Name,
                    x.Cat,
                    x.AssetType,
                    x.ParentCategoryID,
                    x.LastUpdatedBy
                })
                .ToList<Object>();

            return ret;
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
            var query = from item in _oBMSDbContext.ItemMasters
                        join category in _oBMSDbContext.InventoryCategories on item.CategoryID equals category.ID
                        join brand in _oBMSDbContext.BrandMasters on item.BrandID equals brand.ID into brandJoin
                        from brand in brandJoin.DefaultIfEmpty()
                        join uom in _oBMSDbContext.UOMMasters on item.UOMID equals uom.ID into uomJoin
                        from uom in uomJoin.DefaultIfEmpty()
                        join wh in _oBMSDbContext.WarehouseMasters on item.WarehouseID equals wh.ID into whJoin
                        from wh in whJoin.DefaultIfEmpty()
                        select new
                        {
                            ID = item.ID,
                            Code = item.Code,
                            ItemName = item.Name,
                            CategoryName = category.Name,
                            BrandName = brand != null ? brand.Name : "",
                            UOMName = uom != null ? uom.Name : "",
                            WarehouseID = item.WarehouseID,
                            WarehouseName = wh != null ? wh.Name : "",
                            Price = item.Price,
                            ReorderLevel = item.ReorderLevel,
                            ReorderQuantity = item.ReorderQuantity,
                            Remarks = item.Remarks
                        };
            return new List<Object>(query.OrderBy(x => x.ItemName).ToList());
        }

        public async Task<Dictionary<string, Object>> GetItemByID(int ID)
        {
            var results = new Dictionary<string, Object>();

            var item = _oBMSDbContext.ItemMasters.Where(x => x.ID == ID).FirstOrDefault();
            if (item == null) return results;

            // Fix: look up category by item's CategoryID, not by item's ID
            var category = _oBMSDbContext.InventoryCategories
                .Where(x => x.ID == item.CategoryID)
                .FirstOrDefault();

            if (category == null) category = _oBMSDbContext.InventoryCategories.FirstOrDefault();

            results.Add("item", item);
            results.Add("type", category?.Cat ?? "P");
            results.Add("categories", GetCategoriesByCat(category?.Cat ?? "P"));

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

        public async Task<List<object>> GetSuppliers(string? category = null)
        {
            var query = _oBMSDbContext.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                // Filter by category AND only active suppliers
                query = query.Where(s => s.Category == category && s.Status == "A");
            }

            var ret = await query.OrderBy(s => s.Name).ToListAsync();
            return new List<object>(ret);
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

        public async Task<Dictionary<string, Object>> GetUtilityBillsMasterList(string userID)
        {
            bool isSuperAdmin = string.Equals(userID?.Trim(), "superadmin", StringComparison.OrdinalIgnoreCase);

            var results = new Dictionary<string, Object>();

            List<BranchMaster> branchs;
            if (isSuperAdmin)
            {
                branchs = await _oBMSDbContext.BranchMasters
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            }
            else
            {
                branchs = await _oBMSDbContext.BranchMasters
                    .Join(_oBMSDbContext.OBMSBranches,
                        branchmaster => branchmaster.Code,
                        branches => branches.BranchCode,
                        (branchmaster, branches) => new { branchmaster, branches })
                    .Where(x => x.branches.Name == userID && x.branches.IsAllowed == true)
                    .Select(x => x.branchmaster)
                    .Distinct()
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            }

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

        // Part 1: New Inventory Masters - Brand
        public async Task<BrandMaster> SaveAndUpdateBrandMaster(BrandMaster brand)
        {
            if (brand.ID == 0)
            {
                _oBMSDbContext.BrandMasters.Add(brand);
            }
            else
            {
                _oBMSDbContext.BrandMasters.Update(brand);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return brand;

            throw new NotImplementedException();
        }

        public async Task<List<Object>> GetBrandMasters()
        {
            var ret = await _oBMSDbContext.BrandMasters.ToListAsync();
            return new List<Object>(ret.OrderBy(x => x.Name).ToList());
        }

        public async Task<BrandMaster> GetBrandByID(int ID)
        {
            var ret = await _oBMSDbContext.BrandMasters.FindAsync(ID);
            return ret;
        }

        // Part 1: New Inventory Masters - UOM
        public async Task<UOMMaster> SaveAndUpdateUOMMaster(UOMMaster uom)
        {
            if (uom.ID == 0)
            {
                _oBMSDbContext.UOMMasters.Add(uom);
            }
            else
            {
                _oBMSDbContext.UOMMasters.Update(uom);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return uom;
        }

        public async Task<List<Object>> GetUOMMasters()
        {
            var ret = await _oBMSDbContext.UOMMasters.ToListAsync();
            return new List<Object>(ret.OrderBy(x => x.Code).ToList());
        }

        public async Task<UOMMaster> GetUOMByID(int ID)
        {
            var ret = await _oBMSDbContext.UOMMasters.FindAsync(ID);
            return ret;
        }

        // Part 1: New Inventory Masters - Warehouse
        public async Task<WarehouseMaster> SaveAndUpdateWarehouseMaster(WarehouseMaster warehouse)
        {
            if (warehouse.ID == 0)
            {
                _oBMSDbContext.WarehouseMasters.Add(warehouse);
            }
            else
            {
                _oBMSDbContext.WarehouseMasters.Update(warehouse);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return warehouse;
        }

        public async Task<List<Object>> GetWarehouseMasters()
        {
            var ret = await _oBMSDbContext.WarehouseMasters.ToListAsync();
            return new List<Object>(ret.OrderBy(x => x.Name).ToList());
        }

        public async Task<WarehouseMaster> GetWarehouseByID(int ID)
        {
            var ret = await _oBMSDbContext.WarehouseMasters.FindAsync(ID);
            return ret;
        }

        public bool DeleteBrandAsync(int id)
        {
            var brand = _oBMSDbContext.BrandMasters.FirstOrDefault(a => a.ID == id);
            if (brand == null)
                return false;

            _oBMSDbContext.BrandMasters.Remove(brand);
            _oBMSDbContext.SaveChanges();
            return true;
        }

        public bool DeleteUOMAsync(int id)
        {
            var uom = _oBMSDbContext.UOMMasters.FirstOrDefault(a => a.ID == id);
            if (uom == null)
                return false;

            _oBMSDbContext.UOMMasters.Remove(uom);
            _oBMSDbContext.SaveChanges();
            return true;
        }

        public bool DeleteWarehouseAsync(int id)
        {
            var warehouse = _oBMSDbContext.WarehouseMasters.FirstOrDefault(a => a.ID == id);
            if (warehouse == null)
                return false;

            _oBMSDbContext.WarehouseMasters.Remove(warehouse);
            _oBMSDbContext.SaveChanges();
            return true;
        }

        // ── Part 2: Opening Stock ────────────────────────────────────────────────

        public async Task<List<OpeningStockListDto>> GetOpeningStockList()
        {
            var result = await (
                from os in _oBMSDbContext.OpeningStocks
                join wh in _oBMSDbContext.WarehouseMasters   on os.WarehouseID equals wh.ID
                join im in _oBMSDbContext.ItemMasters         on os.ItemID       equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID   equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join br in _oBMSDbContext.BrandMasters on im.BrandID equals br.ID into brJoin
                from br in brJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters   on im.UOMID   equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where os.IsActive == true
                orderby os.OpeningDate descending, os.ID descending
                select new OpeningStockListDto
                {
                    ID            = os.ID,
                    ReferenceNo   = os.ReferenceNo,
                    WarehouseID   = os.WarehouseID,
                    WarehouseName = wh.Name,
                    ItemID        = os.ItemID,
                    ItemCode      = im.Code ?? "",
                    ItemName      = im.Name,
                    CategoryName  = ic != null ? ic.Name : "",
                    BrandName     = br != null ? br.Name : "",
                    UOMName       = um != null ? um.Name : "",
                    OpeningDate   = os.OpeningDate,
                    OpeningQty    = os.OpeningQty,
                    UnitCost      = os.UnitCost,
                    TotalValue    = os.TotalValue,
                    Remarks       = os.Remarks,
                    LASTUPDATE    = os.LASTUPDATE,
                    LastUpdatedBy = os.LastUpdatedBy
                }
            ).ToListAsync();
            return result;
        }

        public async Task<OpeningStockListDto?> GetOpeningStockByID(int id)
        {
            var result = await (
                from os in _oBMSDbContext.OpeningStocks
                join wh in _oBMSDbContext.WarehouseMasters   on os.WarehouseID equals wh.ID
                join im in _oBMSDbContext.ItemMasters         on os.ItemID       equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID   equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join br in _oBMSDbContext.BrandMasters on im.BrandID equals br.ID into brJoin
                from br in brJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters   on im.UOMID   equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where os.ID == id
                select new OpeningStockListDto
                {
                    ID            = os.ID,
                    ReferenceNo   = os.ReferenceNo,
                    WarehouseID   = os.WarehouseID,
                    WarehouseName = wh.Name,
                    ItemID        = os.ItemID,
                    ItemCode      = im.Code ?? "",
                    ItemName      = im.Name,
                    CategoryName  = ic != null ? ic.Name : "",
                    BrandName     = br != null ? br.Name : "",
                    UOMName       = um != null ? um.Name : "",
                    OpeningDate   = os.OpeningDate,
                    OpeningQty    = os.OpeningQty,
                    UnitCost      = os.UnitCost,
                    TotalValue    = os.TotalValue,
                    Remarks       = os.Remarks,
                    LASTUPDATE    = os.LASTUPDATE,
                    LastUpdatedBy = os.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();
            return result;
        }

        public async Task<(bool Success, string Message, OpeningStock? Record)> SaveOpeningStock(
            OpeningStockRequestDto dto)
        {
            // ── Application-layer duplicate check ──────────────────────────────
            if (dto.ID == 0)
            {
                bool duplicate = await _oBMSDbContext.OpeningStocks
                    .AnyAsync(x => x.WarehouseID == dto.WarehouseID
                                && x.ItemID      == dto.ItemID
                                && x.IsActive    == true);
                if (duplicate)
                    return (false,
                        "Opening stock for this item already exists in the selected warehouse. " +
                        "Please edit the existing entry instead.",
                        null);
            }

            // ── Validate item and warehouse still exist ─────────────────────────
            var item      = await _oBMSDbContext.ItemMasters.FindAsync(dto.ItemID);
            var warehouse = await _oBMSDbContext.WarehouseMasters.FindAsync(dto.WarehouseID);

            if (item == null)
                return (false, "The selected item is no longer available. Please choose a different item.", null);
            if (warehouse == null)
                return (false, "The selected warehouse is no longer available. Please choose a different warehouse.", null);

            // ── Generate reference code ─────────────────────────────────────────
            string referenceNo;
            if (dto.ID == 0)
            {
                var maxRow = await _oBMSDbContext.OpeningStocks
                    .Where(x => x.ReferenceNo != null && x.ReferenceNo.StartsWith("OS"))
                    .OrderByDescending(x => x.ReferenceNo)
                    .FirstOrDefaultAsync();

                if (maxRow == null || string.IsNullOrEmpty(maxRow.ReferenceNo))
                    referenceNo = "OS0001";
                else if (int.TryParse(maxRow.ReferenceNo.Substring(2), out int num))
                    referenceNo = "OS" + (num + 1).ToString("D4");
                else
                    referenceNo = "OS0001";
            }
            else
            {
                var existing = await _oBMSDbContext.OpeningStocks.FindAsync(dto.ID);
                if (existing == null)
                    return (false, "The opening stock entry could not be found. It may have been deleted.", null);
                referenceNo = existing.ReferenceNo;
            }

            decimal totalValue = dto.OpeningQty * dto.UnitCost;

            // ── Transactional save using execution strategy (required when EnableRetryOnFailure is configured) ──
            OpeningStock? savedRecord = null;
            string savedMessage = "";

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var dbTransaction = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    OpeningStock record;

                    if (dto.ID == 0)
                    {
                        record = new OpeningStock
                        {
                            ReferenceNo   = referenceNo,
                            WarehouseID   = dto.WarehouseID,
                            ItemID        = dto.ItemID,
                            OpeningDate   = dto.OpeningDate,
                            OpeningQty    = dto.OpeningQty,
                            UnitCost      = dto.UnitCost,
                            TotalValue    = totalValue,
                            Remarks       = dto.Remarks,
                            IsActive      = true,
                            LASTUPDATE    = DateTime.Now,
                            LastUpdatedBy = dto.LastUpdatedBy
                        };
                        _oBMSDbContext.OpeningStocks.Add(record);
                        await _oBMSDbContext.SaveChangesAsync(); // get new ID before creating ledger row

                        _oBMSDbContext.StockLedgers.Add(new StockLedger
                        {
                            TransactionNo   = await GenerateStockLedgerNo(),
                            TransactionDate = dto.OpeningDate,
                            TransactionType = "OPENING",
                            WarehouseID     = dto.WarehouseID,
                            ItemID          = dto.ItemID,
                            InQty           = dto.OpeningQty,
                            OutQty          = 0,
                            UnitCost        = dto.UnitCost,
                            ReferenceType   = "Opening Stock",
                            ReferenceID     = record.ID,
                            ReferenceNo     = referenceNo,
                            Remarks         = dto.Remarks,
                            LASTUPDATE      = DateTime.Now,
                            LastUpdatedBy   = dto.LastUpdatedBy
                        });
                        await _oBMSDbContext.SaveChangesAsync();
                        savedRecord  = record;
                        savedMessage = "Opening stock saved successfully.";
                    }
                    else
                    {
                        record = await _oBMSDbContext.OpeningStocks.FindAsync(dto.ID)
                            ?? throw new InvalidOperationException("Opening stock record not found.");

                        record.WarehouseID   = dto.WarehouseID;
                        record.ItemID        = dto.ItemID;
                        record.OpeningDate   = dto.OpeningDate;
                        record.OpeningQty    = dto.OpeningQty;
                        record.UnitCost      = dto.UnitCost;
                        record.TotalValue    = totalValue;
                        record.Remarks       = dto.Remarks;
                        record.LASTUPDATE    = DateTime.Now;
                        record.LastUpdatedBy = dto.LastUpdatedBy;

                        _oBMSDbContext.OpeningStocks.Update(record);
                        await _oBMSDbContext.SaveChangesAsync();

                        // Update corresponding OPENING ledger row in-place (no duplicate)
                        var ledgerEntry = await _oBMSDbContext.StockLedgers
                            .FirstOrDefaultAsync(x => x.ReferenceID    == dto.ID
                                                   && x.TransactionType == "OPENING");

                        if (ledgerEntry != null)
                        {
                            ledgerEntry.TransactionDate = dto.OpeningDate;
                            ledgerEntry.WarehouseID     = dto.WarehouseID;
                            ledgerEntry.ItemID          = dto.ItemID;
                            ledgerEntry.InQty           = dto.OpeningQty;
                            ledgerEntry.UnitCost        = dto.UnitCost;
                            ledgerEntry.Remarks         = dto.Remarks;
                            ledgerEntry.LASTUPDATE      = DateTime.Now;
                            ledgerEntry.LastUpdatedBy   = dto.LastUpdatedBy;
                            _oBMSDbContext.StockLedgers.Update(ledgerEntry);
                        }
                        else
                        {
                            // Safety net: missing ledger row — create it
                            _oBMSDbContext.StockLedgers.Add(new StockLedger
                            {
                                TransactionNo   = await GenerateStockLedgerNo(),
                                TransactionDate = dto.OpeningDate,
                                TransactionType = "OPENING",
                                WarehouseID     = dto.WarehouseID,
                                ItemID          = dto.ItemID,
                                InQty           = dto.OpeningQty,
                                OutQty          = 0,
                                UnitCost        = dto.UnitCost,
                                ReferenceType   = "Opening Stock",
                                ReferenceID     = dto.ID,
                                ReferenceNo     = referenceNo,
                                Remarks         = dto.Remarks,
                                LASTUPDATE      = DateTime.Now,
                                LastUpdatedBy   = dto.LastUpdatedBy
                            });
                        }

                        await _oBMSDbContext.SaveChangesAsync();
                        savedRecord  = record;
                        savedMessage = "Opening stock updated successfully.";
                    }

                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });

            return (true, savedMessage, savedRecord);
        }

        public async Task<(bool Success, string Message)> DeleteOpeningStock(int id, string currentUser)
        {
            var record = await _oBMSDbContext.OpeningStocks.FindAsync(id);
            if (record == null)
                return (false, "The opening stock entry could not be found. It may have already been removed.");

            var strategy2 = _oBMSDbContext.Database.CreateExecutionStrategy();
            bool deleteSuccess = false;
            string deleteMessage = "";

            await strategy2.ExecuteAsync(async () =>
            {
                using var dbTransaction = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    record.IsActive      = false;
                    record.LASTUPDATE    = DateTime.Now;
                    record.LastUpdatedBy = currentUser;
                    _oBMSDbContext.OpeningStocks.Update(record);
                    await _oBMSDbContext.SaveChangesAsync();

                    var ledgerEntry = await _oBMSDbContext.StockLedgers
                        .FirstOrDefaultAsync(x => x.ReferenceID    == id
                                               && x.TransactionType == "OPENING");
                    if (ledgerEntry != null)
                    {
                        _oBMSDbContext.StockLedgers.Remove(ledgerEntry);
                        await _oBMSDbContext.SaveChangesAsync();
                    }

                    await dbTransaction.CommitAsync();
                    deleteSuccess = true;
                    deleteMessage = "Opening stock entry removed successfully.";
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });

            return (deleteSuccess, deleteMessage);
        }

        // ── Part 2: Stock Ledger ─────────────────────────────────────────────────

        public async Task<List<StockLedgerRowDto>> GetStockLedger(
            int warehouseID,
            int? itemID,
            DateTime dateFrom,
            DateTime dateTo,
            string? transactionType)
        {
            var typeLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "OPENING",      "Opening Stock"    },
                { "STOCK_IN",     "Stock In"         },
                { "ISSUE",        "Stock Issue"      },
                { "ISSUE_RETURN", "Issue Return"     },
                { "TRANSFER_IN",  "Transfer In"      },
                { "TRANSFER_OUT", "Transfer Out"     },
                { "ADJ_PLUS",     "Adjustment (In)"  },
                { "ADJ_MINUS",    "Adjustment (Out)" },
                { "PURCHASE_RTN", "Purchase Return"  },
                { "DAMAGE_LOSS",  "Damage / Loss"    },
            };

            var dateToEnd = dateTo.Date.AddDays(1).AddSeconds(-1);

            var query = _oBMSDbContext.StockLedgers
                .Where(sl => sl.WarehouseID     == warehouseID
                          && sl.TransactionDate >= dateFrom.Date
                          && sl.TransactionDate <= dateToEnd);

            if (itemID.HasValue && itemID.Value > 0)
                query = query.Where(sl => sl.ItemID == itemID.Value);

            if (!string.IsNullOrWhiteSpace(transactionType) && transactionType != "ALL")
                query = query.Where(sl => sl.TransactionType == transactionType);

            var rows = await (
                from sl in query
                join wh in _oBMSDbContext.WarehouseMasters on sl.WarehouseID equals wh.ID
                join im in _oBMSDbContext.ItemMasters       on sl.ItemID       equals im.ID
                join um in _oBMSDbContext.UOMMasters   on im.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                orderby sl.ItemID, sl.TransactionDate, sl.ID
                select new
                {
                    sl.ID, sl.TransactionNo, sl.TransactionDate, sl.TransactionType,
                    sl.WarehouseID, WarehouseName = wh.Name,
                    sl.ItemID, ItemCode = im.Code ?? "", ItemName = im.Name,
                    UOMName = um != null ? um.Name : "",
                    sl.InQty, sl.OutQty, sl.UnitCost,
                    sl.ReferenceNo, sl.ReferenceType, sl.Remarks,
                    sl.LASTUPDATE, sl.LastUpdatedBy
                }
            ).ToListAsync();

            var result = new List<StockLedgerRowDto>();
            var runningBalances = new Dictionary<(int, int), decimal>();

            foreach (var row in rows)
            {
                var key = (row.WarehouseID, row.ItemID);
                if (!runningBalances.ContainsKey(key))
                    runningBalances[key] = 0;
                runningBalances[key] += row.InQty - row.OutQty;

                result.Add(new StockLedgerRowDto
                {
                    ID                   = row.ID,
                    TransactionNo        = row.TransactionNo,
                    TransactionDate      = row.TransactionDate,
                    TransactionType      = row.TransactionType,
                    TransactionTypeLabel = typeLabels.TryGetValue(row.TransactionType, out var lbl) ? lbl : row.TransactionType,
                    WarehouseID          = row.WarehouseID,
                    WarehouseName        = row.WarehouseName,
                    ItemID               = row.ItemID,
                    ItemCode             = row.ItemCode,
                    ItemName             = row.ItemName,
                    UOMName              = row.UOMName,
                    InQty                = row.InQty,
                    OutQty               = row.OutQty,
                    RunningBalance       = runningBalances[key],
                    UnitCost             = row.UnitCost,
                    ReferenceNo          = row.ReferenceNo,
                    ReferenceType        = row.ReferenceType,
                    Remarks              = row.Remarks,
                    LASTUPDATE           = row.LASTUPDATE,
                    LastUpdatedBy        = row.LastUpdatedBy
                });
            }
            return result;
        }

        public async Task<decimal> GetCurrentStock(int warehouseID, int itemID)
        {
            var balance = await _oBMSDbContext.StockLedgers
                .Where(sl => sl.WarehouseID == warehouseID && sl.ItemID == itemID)
                .SumAsync(sl => (decimal?)(sl.InQty - sl.OutQty));
            return balance ?? 0;
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private async Task<string> GenerateStockLedgerNo()
        {
            var maxRow = await _oBMSDbContext.StockLedgers
                .Where(x => x.TransactionNo != null && x.TransactionNo.StartsWith("SL"))
                .OrderByDescending(x => x.TransactionNo)
                .FirstOrDefaultAsync();

            if (maxRow == null || string.IsNullOrEmpty(maxRow.TransactionNo))
                return "SL00001";
            if (int.TryParse(maxRow.TransactionNo.Substring(2), out int num))
                return "SL" + (num + 1).ToString("D5");
            return "SL00001";
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 3 — PURCHASE REQUISITION
        // ════════════════════════════════════════════════════════════════════════

        // ── Shared enrichment query ───────────────────────────────────────────

        private IQueryable<PRListDto> PRListQuery() =>
            from pr in _oBMSDbContext.PurchaseRequisitions
            where pr.IsActive == true
            orderby pr.PRDate descending, pr.ID descending
            select new PRListDto
            {
                ID              = pr.ID,
                PRNumber        = pr.PRNumber,
                PRDate          = pr.PRDate,
                RequiredByDate  = pr.RequiredByDate,
                Status          = pr.Status,
                Remarks         = pr.Remarks,
                ApprovedBy      = pr.ApprovedBy,
                ApprovedDate    = pr.ApprovedDate,
                ApprovalRemarks = pr.ApprovalRemarks,
                // Part 7: SubmittedBy/SubmittedDate must be in the DTO so the
                // frontend can pass the correct requesterUser to IsCurrentUserApprover.
                // Fallback to LastUpdatedBy so the value is never null for old records.
                SubmittedBy     = pr.SubmittedBy ?? pr.LastUpdatedBy,
                SubmittedDate   = pr.SubmittedDate,
                LASTUPDATE      = pr.LASTUPDATE,
                LastUpdatedBy   = pr.LastUpdatedBy
            };

        public async Task<List<PRListDto>> GetPRList()
        {
            var list = await PRListQuery().ToListAsync();
            foreach (var pr in list)
            {
                pr.Details = await GetPRDetails(pr.ID);

                // ── Compute DisplayStatus ──────────────────────────────────
                // Only Approved PRs can have further progression
                if (pr.Status == "Approved")
                {
                    // Check if a PO exists for this PR
                    var po = await _oBMSDbContext.PurchaseOrders
                        .Where(p => p.PRID == pr.ID && p.IsActive)
                        .Select(p => new { p.ID, p.Status })
                        .FirstOrDefaultAsync();

                    if (po != null)
                    {
                        // Check if a GRN exists for that PO
                        bool grnExists = await _oBMSDbContext.GoodsReceiptNotes
                            .AnyAsync(g => g.POID == po.ID && g.IsActive);

                        pr.DisplayStatus = grnExists ? "GRN Posted" : "PO Posted";
                    }
                    else
                    {
                        pr.DisplayStatus = pr.Status; // "Approved" — no PO yet
                    }
                }
                else
                {
                    pr.DisplayStatus = pr.Status;
                }
            }
            return list;
        }

        public async Task<PRListDto?> GetPRByID(int id)
        {
            var pr = await PRListQuery().FirstOrDefaultAsync(x => x.ID == id);
            if (pr != null)
                pr.Details = await GetPRDetails(id);
            return pr;
        }

        private async Task<List<PRDetailListDto>> GetPRDetails(int prID)
        {
            return await (
                from d in _oBMSDbContext.PurchaseRequisitionDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on d.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where d.PRID == prID
                select new PRDetailListDto
                {
                    ID           = d.ID,
                    PRID         = d.PRID,
                    ItemID       = d.ItemID,
                    ItemCode     = im.Code ?? "",
                    ItemName     = im.Name,
                    CategoryName = ic != null ? ic.Name : "",
                    RequiredQty  = d.RequiredQty,
                    BuyingPrice  = d.BuyingPrice,
                    UOMID        = d.UOMID,
                    UOMName      = um != null ? um.Name : "",
                    Remarks      = d.Remarks
                }
            ).ToListAsync();
        }

        public async Task<(bool Success, string Message, PurchaseRequisition? Record)> SavePR(PRRequestDto dto)
        {
            if (!dto.Details.Any())
                return (false, "Please add at least one item to the requisition.", null);

            if (dto.Details.Any(d => d.RequiredQty <= 0))
            {
                var bad = dto.Details.First(d => d.RequiredQty <= 0);
                var badItem = await _oBMSDbContext.ItemMasters.FindAsync(bad.ItemID);
                return (false, $"Quantity for \"{badItem?.Name ?? "the item"}\" must be greater than zero.", null);
            }

            // On edit: confirm it is still Draft
            if (dto.ID != 0)
            {
                var existing = await _oBMSDbContext.PurchaseRequisitions.FindAsync(dto.ID);
                if (existing == null)
                    return (false, "This requisition could not be found. It may have been removed.", null);
                if (existing.Status != "Draft")
                    return (false, $"This purchase requisition has already been {existing.Status.ToLower()} and cannot be edited.", null);
            }

            // Generate code
            string prNumber = dto.PRNumber ?? "";
            if (dto.ID == 0)
            {
                var maxRow = await _oBMSDbContext.PurchaseRequisitions
                    .Where(x => x.PRNumber != null && x.PRNumber.StartsWith("PR"))
                    .OrderByDescending(x => x.PRNumber)
                    .FirstOrDefaultAsync();
                prNumber = (maxRow == null || string.IsNullOrEmpty(maxRow.PRNumber))
                    ? "PR0001"
                    : int.TryParse(maxRow.PRNumber.Substring(2), out int n)
                        ? "PR" + (n + 1).ToString("D4")
                        : "PR0001";
            }

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            PurchaseRequisition? savedRecord = null;

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    PurchaseRequisition pr;
                    if (dto.ID == 0)
                    {
                        pr = new PurchaseRequisition
                        {
                            PRNumber       = prNumber,
                            PRDate         = dto.PRDate,
                            RequiredByDate = dto.RequiredByDate,
                            Status         = "Draft",
                            Remarks        = dto.Remarks,
                            IsActive       = true,
                            LASTUPDATE     = DateTime.Now,
                            LastUpdatedBy  = dto.LastUpdatedBy
                        };
                        _oBMSDbContext.PurchaseRequisitions.Add(pr);
                        await _oBMSDbContext.SaveChangesAsync();
                    }
                    else
                    {
                        pr = await _oBMSDbContext.PurchaseRequisitions.FindAsync(dto.ID)!;
                        pr.PRDate         = dto.PRDate;
                        pr.RequiredByDate = dto.RequiredByDate;
                        pr.Remarks        = dto.Remarks;
                        pr.LASTUPDATE     = DateTime.Now;
                        pr.LastUpdatedBy  = dto.LastUpdatedBy;
                        _oBMSDbContext.PurchaseRequisitions.Update(pr);
                        await _oBMSDbContext.SaveChangesAsync();

                        // Remove existing detail lines before re-adding
                        var oldDetails = _oBMSDbContext.PurchaseRequisitionDetails.Where(x => x.PRID == pr.ID);
                        _oBMSDbContext.PurchaseRequisitionDetails.RemoveRange(oldDetails);
                        await _oBMSDbContext.SaveChangesAsync();
                    }

                    foreach (var d in dto.Details)
                    {
                        _oBMSDbContext.PurchaseRequisitionDetails.Add(new PurchaseRequisitionDetail
                        {
                            PRID          = pr.ID,
                            ItemID        = d.ItemID,
                            RequiredQty   = d.RequiredQty,
                            BuyingPrice   = d.BuyingPrice,
                            UOMID         = d.UOMID == 0 ? null : d.UOMID,
                            Remarks       = d.Remarks,
                            LASTUPDATE    = DateTime.Now,
                            LastUpdatedBy = d.LastUpdatedBy ?? dto.LastUpdatedBy
                        });
                    }
                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedRecord = pr;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, dto.ID == 0 ? "Purchase requisition saved successfully." : "Purchase requisition updated successfully.", savedRecord);
        }

        public async Task<(bool Success, string Message)> SubmitPR(int id, string currentUser)
        {
            var pr = await _oBMSDbContext.PurchaseRequisitions.FindAsync(id);
            if (pr == null) return (false, "This purchase requisition could not be found.");
            if (pr.Status != "Draft")
                return (false, $"Only a Draft requisition can be submitted. This requisition is currently {pr.Status}.");

            var hasDetails = await _oBMSDbContext.PurchaseRequisitionDetails.AnyAsync(x => x.PRID == id);
            if (!hasDetails)
                return (false, "Please add at least one item to the requisition before submitting.");

            // ── BUG-06 FIX: ApprovalConfiguration is now the master on/off switch. ──
            // If ApprovalRequired = false for PURCHASE_REQUISITION → direct-approve flow
            // regardless of any ApprovalRouting configuration.
            if (_approval != null)
            {
                bool approvalRequired = await _approval.IsApprovalRequired("PURCHASE_REQUISITION");
                if (!approvalRequired)
                {
                    // Approval is turned off for PRs → auto-approve immediately.
                    string prevDirect2 = pr.Status;
                    pr.Status        = "Approved";
                    pr.ApprovedBy    = currentUser;
                    pr.ApprovedDate  = DateTime.Now;
                    pr.SubmittedBy   = currentUser;
                    pr.SubmittedDate = DateTime.Now;
                    pr.LASTUPDATE    = DateTime.Now;
                    pr.LastUpdatedBy = currentUser;
                    await _oBMSDbContext.SaveChangesAsync();
                    return (true, $"Purchase Requisition {pr.PRNumber} has been approved. You can now create a Purchase Order.");
                }

                var approverUser = await _approval.GetApproverForUser(
                    currentUser, "PURCHASE_REQUISITION");

                if (string.IsNullOrEmpty(approverUser))
                {
                    // ── No approval routing configured → direct approved flow ──
                    // PR goes straight to Approved so it can be used for PO immediately.
                    // No Pending Approval, no notification, no ApprovalHistory.
                    string prevDirect = pr.Status;
                    pr.Status        = "Approved";
                    pr.ApprovedBy    = currentUser;
                    pr.ApprovedDate  = DateTime.Now;
                    pr.SubmittedBy   = currentUser;
                    pr.SubmittedDate = DateTime.Now;
                    pr.LASTUPDATE    = DateTime.Now;
                    pr.LastUpdatedBy = currentUser;
                    await _oBMSDbContext.SaveChangesAsync();
                    return (true, $"Purchase Requisition {pr.PRNumber} has been approved. You can now create a Purchase Order.");
                }

                string prev = pr.Status;
                pr.Status        = "Submitted";
                pr.SubmittedBy   = currentUser;
                pr.SubmittedDate = DateTime.Now;
                pr.LASTUPDATE    = DateTime.Now;
                pr.LastUpdatedBy = currentUser;
                await _oBMSDbContext.SaveChangesAsync();

                // Record history (awaited — must succeed)
                await _approval.RecordHistory("PURCHASE_REQUISITION", pr.ID, pr.PRNumber,
                    "Submitted", currentUser, prev, "Submitted", null);

                // Notify the actual configured approver — awaited so we know it succeeded
                if (_notif != null)
                    await _notif.SendApprovalNotification(
                        "PENDING_APPROVAL", "PURCHASE_REQUISITION", pr.PRNumber, pr.ID,
                        approverUser, currentUser, null);
            }
            else
            {
                // Fallback if approval service not injected (should not happen in production)
                string prev = pr.Status;
                pr.Status        = "Submitted";
                pr.SubmittedBy   = currentUser;
                pr.SubmittedDate = DateTime.Now;
                pr.LASTUPDATE    = DateTime.Now;
                pr.LastUpdatedBy = currentUser;
                await _oBMSDbContext.SaveChangesAsync();
            }

            return (true, $"Purchase Requisition {pr.PRNumber} has been submitted for approval.");
        }

        public async Task<(bool Success, string Message)> ApprovePR(int id, PRApprovalDto dto)
        {
            var pr = await _oBMSDbContext.PurchaseRequisitions.FindAsync(id);
            if (pr == null) return (false, "This purchase requisition could not be found.");
            if (pr.Status != "Submitted")
                return (false, "Only a Submitted requisition can be approved. Please ensure the requisition has been submitted first.");

            // ── Security: verify the authenticated user is the configured approver ──
            var submitter = pr.SubmittedBy ?? pr.LastUpdatedBy ?? string.Empty;
            // BUG-05 FIX: isSuperAdmin is derived from the JWT "IsAdmin" claim value "2",
            // passed in via PRApprovalDto.IsSuperAdmin. No more hard-coded "superadmin" string.
            bool isSuperAdmin   = dto.IsSuperAdmin;

            // SuperAdmin has global approval authority — they can approve ANY document
            // including documents they submitted themselves (global override).
            // Normal users: self-approval is always blocked.
            bool isSelfApproval = !isSuperAdmin &&
                                  !string.IsNullOrWhiteSpace(submitter) &&
                                  string.Equals(dto.ApprovedBy, submitter,
                                                StringComparison.OrdinalIgnoreCase);
            if (isSelfApproval)
                return (false, "You cannot approve a document that you submitted yourself.");

            if (!isSuperAdmin && _approval != null)
            {
                var authorized = await _approval.IsAuthorizedApprover(
                    dto.ApprovedBy, submitter, "PURCHASE_REQUISITION");
                if (!authorized)
                    return (false, "You are not authorized to approve this document.");
            }

            string prev        = pr.Status;
            pr.Status          = "Approved";
            pr.ApprovedBy      = dto.ApprovedBy;
            pr.ApprovedDate    = DateTime.Now;
            pr.ApprovalRemarks = dto.ApprovalRemarks;
            pr.LASTUPDATE      = DateTime.Now;
            pr.LastUpdatedBy   = dto.ApprovedBy;
            await _oBMSDbContext.SaveChangesAsync();

            // Part 7: record history + notify requester
            if (_approval != null)
                await _approval.RecordHistory("PURCHASE_REQUISITION", pr.ID, pr.PRNumber,
                    "Approved", dto.ApprovedBy, prev, "Approved", dto.ApprovalRemarks);
            if (_notif != null && !string.IsNullOrEmpty(pr.SubmittedBy))
                await _notif.SendApprovalNotification(
                    "APPROVED", "PURCHASE_REQUISITION", pr.PRNumber, pr.ID,
                    pr.SubmittedBy, dto.ApprovedBy, dto.ApprovalRemarks);

            // Notify all active Purchase-department users about this approval.
            // The PR submitter (already notified above) is excluded to prevent
            // a duplicate notification if they also belong to the Purchase department.
            if (_notif != null)
                await _notif.SendApprovalNotificationToDepartment(
                    "APPROVED", "PURCHASE_REQUISITION", pr.PRNumber, pr.ID,
                    "Purchase", dto.ApprovedBy, dto.ApprovalRemarks,
                    excludeUsers: new[] { pr.SubmittedBy ?? string.Empty });

            // Create a Pending processing task so Purchase dept users can claim and action this PR.
            // Non-throwing — failure here must never roll back the approval.
            await CreatePRProcessingTask(pr.ID, pr.PRNumber, dto.ApprovedBy);

            return (true, $"Purchase Requisition {pr.PRNumber} has been approved. You can now create a Purchase Order.");
        }

        public async Task<(bool Success, string Message)> RejectPR(int id, PRApprovalDto dto)
        {
            var pr = await _oBMSDbContext.PurchaseRequisitions.FindAsync(id);
            if (pr == null) return (false, "This purchase requisition could not be found.");
            if (pr.Status != "Submitted")
                return (false, "Only a Submitted requisition can be rejected.");
            if (string.IsNullOrWhiteSpace(dto.ApprovalRemarks))
                return (false, "Please enter a reason for rejection so the requester knows what to correct.");

            // ── Security: verify the authenticated user is the configured approver ──
            var submitter = pr.SubmittedBy ?? pr.LastUpdatedBy ?? string.Empty;
            // BUG-05 FIX: isSuperAdmin is derived from the JWT "IsAdmin" claim value "2",
            // passed in via PRApprovalDto.IsSuperAdmin. No more hard-coded "superadmin" string.
            bool isSuperAdmin    = dto.IsSuperAdmin;

            // SuperAdmin has global authority — self-rejection block does not apply.
            // Normal users: cannot reject their own submission.
            bool isSelfRejection = !isSuperAdmin &&
                                   !string.IsNullOrWhiteSpace(submitter) &&
                                   string.Equals(dto.ApprovedBy, submitter,
                                                 StringComparison.OrdinalIgnoreCase);
            if (isSelfRejection)
                return (false, "You cannot reject a document that you submitted yourself.");

            if (!isSuperAdmin && _approval != null)
            {
                var authorized = await _approval.IsAuthorizedApprover(
                    dto.ApprovedBy, submitter, "PURCHASE_REQUISITION");
                if (!authorized)
                    return (false, "You are not authorized to reject this document.");
            }

            string prev        = pr.Status;
            pr.Status          = "Rejected";
            pr.ApprovedBy      = dto.ApprovedBy;
            pr.ApprovedDate    = DateTime.Now;
            pr.ApprovalRemarks = dto.ApprovalRemarks;
            pr.LASTUPDATE      = DateTime.Now;
            pr.LastUpdatedBy   = dto.ApprovedBy;
            await _oBMSDbContext.SaveChangesAsync();

            // Part 7: record history + notify requester
            if (_approval != null)
                await _approval.RecordHistory("PURCHASE_REQUISITION", pr.ID, pr.PRNumber,
                    "Rejected", dto.ApprovedBy, prev, "Rejected", dto.ApprovalRemarks);
            if (_notif != null && !string.IsNullOrEmpty(pr.SubmittedBy))
                await _notif.SendApprovalNotification(
                    "REJECTED", "PURCHASE_REQUISITION", pr.PRNumber, pr.ID,
                    pr.SubmittedBy, dto.ApprovedBy, dto.ApprovalRemarks);

            return (true, $"Purchase Requisition {pr.PRNumber} has been rejected.");
        }

        public async Task<(bool Success, string Message)> DeletePR(int id, string currentUser)
        {
            var pr = await _oBMSDbContext.PurchaseRequisitions.FindAsync(id);
            if (pr == null) return (false, "This purchase requisition could not be found.");
            if (pr.Status != "Draft")
                return (false, $"This purchase requisition cannot be deleted because it has already been {pr.Status.ToLower()}. Only Draft requisitions can be deleted.");

            // ── Dependency check: block if a PO has been raised from this PR ──
            var linkedPO = await _oBMSDbContext.PurchaseOrders
                .Where(po => po.PRID == id && po.IsActive)
                .Select(po => new { po.PONumber })
                .FirstOrDefaultAsync();
            if (linkedPO != null)
                return (false, $"Purchase Requisition {pr.PRNumber} cannot be deleted because Purchase Order {linkedPO.PONumber} has already been created from it. Please cancel Purchase Order {linkedPO.PONumber} first.");

            pr.IsActive      = false;
            pr.LASTUPDATE    = DateTime.Now;
            pr.LastUpdatedBy = currentUser;
            await _oBMSDbContext.SaveChangesAsync();
            return (true, "Purchase requisition removed successfully.");
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 3 — PR PROCESSING TASKS (Purchase department claim / inbox)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all active processing tasks (Pending or Claimed), newest first.
        /// Used by the Purchase department inbox to show the work queue.
        /// </summary>
        public async Task<List<PRProcessingTask>> GetPRProcessingTasks()
        {
            return await _oBMSDbContext.PRProcessingTasks
                .Where(t => t.Status == "Pending" || t.Status == "Claimed")
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Returns the processing task for a single PR, or null if none exists.
        /// Called by the PR form on load to display claim state.
        /// </summary>
        public async Task<PRProcessingTask?> GetPRProcessingTaskByPR(int prid)
        {
            return await _oBMSDbContext.PRProcessingTasks
                .FirstOrDefaultAsync(t => t.PRID == prid);
        }

        /// <summary>
        /// Atomically claims an unclaimed (Pending) task for the given user.
        ///
        /// Uses a conditional UPDATE WHERE Status='Pending' so that two concurrent
        /// callers can never both succeed — the second UPDATE lands 0 rows and returns
        /// an error explaining who claimed it first.
        /// </summary>
        public async Task<(bool Success, string Message)> ClaimPR(int prid, string claimedBy)
        {
            if (string.IsNullOrWhiteSpace(claimedBy))
                return (false, "Unable to identify the current user. Please log in again.");

            // Conditional UPDATE — only succeeds if the task is still Pending
            int rowsUpdated = await _oBMSDbContext.PRProcessingTasks
                .Where(t => t.PRID == prid && t.Status == "Pending")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Status,        "Claimed")
                    .SetProperty(t => t.ClaimedBy,     claimedBy)
                    .SetProperty(t => t.ClaimedDate,   DateTime.Now)
                    .SetProperty(t => t.LASTUPDATE,    DateTime.Now)
                    .SetProperty(t => t.LastUpdatedBy, claimedBy));

            if (rowsUpdated > 0)
                return (true, "PR claimed successfully. You are now responsible for creating the Purchase Order.");

            // Update landed 0 rows — check why so we can return a useful message
            var task = await _oBMSDbContext.PRProcessingTasks
                .FirstOrDefaultAsync(t => t.PRID == prid);

            if (task == null)
                return (false, "No processing task found for this purchase requisition. It may not have been approved yet.");

            return task.Status switch
            {
                "Claimed"   => (false, $"This PR has already been claimed by {task.ClaimedBy}."),
                "Completed" => (false, "This PR has already been processed and a Purchase Order has been created."),
                _           => (false, "This PR cannot be claimed in its current state.")
            };
        }

        /// <summary>
        /// Creates a Pending processing task when a PR is approved.
        /// Non-throwing — a failure here must never roll back the approval transaction.
        /// Idempotent: if a task already exists for this PRID, the method exits silently.
        /// </summary>
        private async Task CreatePRProcessingTask(int prid, string prNumber, string createdBy)
        {
            try
            {
                // Idempotency guard: don't create a second task if one already exists
                bool exists = await _oBMSDbContext.PRProcessingTasks
                    .AnyAsync(t => t.PRID == prid);
                if (exists) return;

                _oBMSDbContext.PRProcessingTasks.Add(new PRProcessingTask
                {
                    PRID        = prid,
                    PRNumber    = prNumber,
                    Status      = "Pending",
                    CreatedDate = DateTime.Now,
                    CreatedBy   = createdBy,
                    LASTUPDATE  = DateTime.Now,
                    LastUpdatedBy = createdBy
                });
                await _oBMSDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[CreatePRProcessingTask] PR {prNumber} (PRID={prid}) — " +
                    $"The approval was successful; the task row should be recreated manually " +
                    $"or will appear on the next retry. Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Marks a processing task Completed when a PO is successfully created.
        /// Non-throwing — a failure here must never roll back the PO save transaction.
        /// </summary>
        private async Task CompletePRProcessingTask(int prid, string completedBy, string poNumber)
        {
            try
            {
                int rows = await _oBMSDbContext.PRProcessingTasks
                    .Where(t => t.PRID == prid && t.Status != "Completed")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.Status,        "Completed")
                        .SetProperty(t => t.CompletedBy,   completedBy)
                        .SetProperty(t => t.CompletedDate, DateTime.Now)
                        .SetProperty(t => t.PONumber,      poNumber)
                        .SetProperty(t => t.LASTUPDATE,    DateTime.Now)
                        .SetProperty(t => t.LastUpdatedBy, completedBy));

                if (rows == 0)
                    Console.Error.WriteLine(
                        $"[CompletePRProcessingTask] No Pending/Claimed task found for " +
                        $"PRID={prid} (PO={poNumber}). The task may not exist or was already Completed.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[CompletePRProcessingTask] PRID={prid} PO={poNumber} — " +
                    $"The PO was saved successfully; the task can be marked complete manually. " +
                    $"Error: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 3 — PURCHASE ORDER
        // ════════════════════════════════════════════════════════════════════════

        private IQueryable<POListDto> POListQuery() =>
            from po in _oBMSDbContext.PurchaseOrders
            join sup in _oBMSDbContext.Suppliers    on po.SupplierID  equals sup.Id
            join wh  in _oBMSDbContext.WarehouseMasters on po.WarehouseID equals wh.ID
            join pr  in _oBMSDbContext.PurchaseRequisitions on po.PRID equals pr.ID into prJoin
            from pr in prJoin.DefaultIfEmpty()
            where po.IsActive == true
            orderby po.PODate descending, po.ID descending
            select new POListDto
            {
                ID            = po.ID,
                PONumber      = po.PONumber,
                PRID          = po.PRID,
                PRNumber      = pr != null ? pr.PRNumber : "",
                SupplierID    = po.SupplierID,
                SupplierName  = sup.Name,
                WarehouseID   = po.WarehouseID,
                WarehouseName = wh.Name,
                PODate        = po.PODate,
                ExpectedDate  = po.ExpectedDate,
                Status        = po.Status,
                TotalAmount   = po.TotalAmount,
                Remarks       = po.Remarks,
                ApprovedBy    = po.ApprovedBy,
                ApprovedDate  = po.ApprovedDate,
                SubmittedBy   = po.SubmittedBy ?? po.LastUpdatedBy,   // fallback so Angular always gets a non-null value
                SubmittedDate = po.SubmittedDate,
                RejectReason  = po.RejectReason,
                LASTUPDATE    = po.LASTUPDATE,
                LastUpdatedBy = po.LastUpdatedBy
            };

        public async Task<List<POListDto>> GetPOList()
        {
            var list = await POListQuery().ToListAsync();
            foreach (var po in list)
                po.Details = await GetPODetails(po.ID);
            return list;
        }

        public async Task<POListDto?> GetPOByID(int id)
        {
            var po = await POListQuery().FirstOrDefaultAsync(x => x.ID == id);
            if (po != null)
                po.Details = await GetPODetails(id);
            return po;
        }

        /// <summary>
        /// Returns the existing active PO for a given PRID, or null if none exists.
        /// Used to prevent duplicate POs when an approved-PR notification is clicked
        /// more than once.
        /// </summary>
        public async Task<POListDto?> GetPOByPRID(int prID)
        {
            // POListQuery already filters po.IsActive == true
            var po = await POListQuery()
                .FirstOrDefaultAsync(x => x.PRID == prID);
            if (po != null)
                po.Details = await GetPODetails(po.ID);
            return po;
        }

        private async Task<List<PODetailListDto>> GetPODetails(int poID)
        {
            return await (
                from d  in _oBMSDbContext.PurchaseOrderDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on d.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where d.POID == poID
                select new PODetailListDto
                {
                    ID           = d.ID,
                    POID         = d.POID,
                    ItemID       = d.ItemID,
                    ItemCode     = im.Code ?? "",
                    ItemName     = im.Name,
                    CategoryName = ic != null ? ic.Name : "",
                    UOMID        = d.UOMID,
                    UOMName      = um != null ? um.Name : "",
                    OrderedQty   = d.OrderedQty,
                    ReceivedQty  = d.ReceivedQty,
                    PendingQty   = d.OrderedQty - d.ReceivedQty,
                    UnitPrice    = d.UnitPrice,
                    TotalPrice   = d.TotalPrice,
                    Remarks      = d.Remarks
                }
            ).ToListAsync();
        }

        public async Task<List<PRListDto>> GetApprovedPRsForPO()
        {
            // Return approved PRs that have NOT yet been converted to a PO
            var prIdsWithPO = _oBMSDbContext.PurchaseOrders
                .Where(po => po.PRID != null && po.IsActive)
                .Select(po => po.PRID!.Value);

            var list = await PRListQuery()
                .Where(pr => pr.Status == "Approved" && !prIdsWithPO.Contains(pr.ID))
                .ToListAsync();
            return list;
        }

        public async Task<(bool Success, string Message, PurchaseOrder? Record)> SavePO(PORequestDto dto)
        {
            if (!dto.Details.Any())
                return (false, "Please add at least one item to the purchase order.", null);

            if (dto.Details.Any(d => d.OrderedQty <= 0))
            {
                var bad = dto.Details.First(d => d.OrderedQty <= 0);
                var badItem = await _oBMSDbContext.ItemMasters.FindAsync(bad.ItemID);
                return (false, $"Quantity for \"{badItem?.Name ?? "the item"}\" must be greater than zero.", null);
            }

            if (dto.Details.Any(d => d.UnitPrice < 0))
                return (false, "Unit price cannot be negative. Please enter a valid price for all items.", null);

            // Validate status on edit + dependency check
            if (dto.ID != 0)
            {
                var existing = await _oBMSDbContext.PurchaseOrders.FindAsync(dto.ID);
                if (existing == null) return (false, "This purchase order could not be found.", null);

                // ── Editability rules ───────────────────────────────────────
                // Draft      → always editable (normal path)
                // Approved   → editable ONLY if no active GRN exists (GRN was deleted / never created)
                // Any other status → blocked
                if (existing.Status != "Draft" && existing.Status != "Approved")
                    return (false, $"This purchase order cannot be edited because it has already been {existing.Status.ToLower()}.", null);

                // Check for active GRN regardless of status
                var linkedGRN = await _oBMSDbContext.GoodsReceiptNotes
                    .Where(g => g.POID == dto.ID && g.IsActive)
                    .Select(g => new { g.GRNNumber })
                    .FirstOrDefaultAsync();
                if (linkedGRN != null)
                    return (false, $"Purchase Order {existing.PONumber} cannot be edited because Goods Receipt {linkedGRN.GRNNumber} has already been created for this PO. Please delete Goods Receipt {linkedGRN.GRNNumber} first.", null);

                // ── PR-linked item protection ───────────────────────────────
                // If this PO was created from a PR, the item list (which items are ordered)
                // is frozen — only Qty and UnitPrice per line may change.
                // Detect if the user is trying to add/remove/swap items.
                if (existing.PRID.HasValue && existing.PRID > 0)
                {
                    var existingItems = await _oBMSDbContext.PurchaseOrderDetails
                        .Where(x => x.POID == dto.ID)
                        .Select(x => x.ItemID)
                        .OrderBy(x => x)
                        .ToListAsync();
                    var incomingItems = dto.Details
                        .Select(d => d.ItemID)
                        .OrderBy(x => x)
                        .ToList();
                    if (!existingItems.SequenceEqual(incomingItems))
                        return (false,
                            $"Purchase Order {existing.PONumber} was created from a Purchase Requisition. " +
                            "The item list cannot be changed. " +
                            "You may only update quantities and unit prices. " +
                            "To change the items, please create a new Purchase Order without linking to a PR.", null);
                }
            }

            // Validate linked PR if provided
            if (dto.PRID.HasValue && dto.PRID > 0)
            {
                var pr = await _oBMSDbContext.PurchaseRequisitions.FindAsync(dto.PRID.Value);
                if (pr == null)
                    return (false, "The selected purchase requisition could not be found.", null);
                if (pr.Status != "Approved")
                    return (false, $"Purchase Requisition {pr.PRNumber} has not been approved yet. It must be approved before a Purchase Order can be created.", null);

                // ── Duplicate PO guard ─────────────────────────────────────
                // A new PO (dto.ID == 0) must not be created if an active PO
                // already references the same PR. This prevents duplicate POs
                // when the user clicks the same approval notification twice.
                if (dto.ID == 0)
                {
                    var existingPO = await _oBMSDbContext.PurchaseOrders
                        .Where(po => po.PRID == dto.PRID.Value && po.IsActive)
                        .Select(po => new { po.PONumber })
                        .FirstOrDefaultAsync();

                    if (existingPO != null)
                        return (false,
                            $"A Purchase Order ({existingPO.PONumber}) already exists for Purchase Requisition {pr.PRNumber}. " +
                            "Please open the existing Purchase Order instead of creating a new one.", null);
                }
            }

            // Generate PO number
            string poNumber = dto.PONumber ?? "";
            if (dto.ID == 0)
            {
                var maxRow = await _oBMSDbContext.PurchaseOrders
                    .Where(x => x.PONumber != null && x.PONumber.StartsWith("PO"))
                    .OrderByDescending(x => x.PONumber)
                    .FirstOrDefaultAsync();
                poNumber = (maxRow == null || string.IsNullOrEmpty(maxRow.PONumber))
                    ? "PO0001"
                    : int.TryParse(maxRow.PONumber.Substring(2), out int n)
                        ? "PO" + (n + 1).ToString("D4")
                        : "PO0001";
            }

            decimal totalAmount = dto.Details.Sum(d => d.OrderedQty * d.UnitPrice);

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            PurchaseOrder? savedRecord = null;

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    PurchaseOrder po;
                    if (dto.ID == 0)
                    {
                        po = new PurchaseOrder
                        {
                            PONumber      = poNumber,
                            PRID          = dto.PRID.HasValue && dto.PRID > 0 ? dto.PRID : null,
                            SupplierID    = dto.SupplierID,
                            WarehouseID   = dto.WarehouseID,
                            PODate        = dto.PODate,
                            ExpectedDate  = dto.ExpectedDate,
                            Status        = "Draft",
                            TotalAmount   = totalAmount,
                            Remarks       = dto.Remarks,
                            IsActive      = true,
                            LASTUPDATE    = DateTime.Now,
                            LastUpdatedBy = dto.LastUpdatedBy
                        };
                        _oBMSDbContext.PurchaseOrders.Add(po);
                        await _oBMSDbContext.SaveChangesAsync();
                    }
                    else
                    {
                        po = await _oBMSDbContext.PurchaseOrders.FindAsync(dto.ID)!;
                        // If PO was Approved (GRN deleted and re-editing), reset back to Draft
                        if (po.Status == "Approved")
                            po.Status = "Draft";
                        // PRID is immutable on edit — never allow changing the PR link
                        po.SupplierID    = dto.SupplierID;
                        po.WarehouseID   = dto.WarehouseID;
                        po.PODate        = dto.PODate;
                        po.ExpectedDate  = dto.ExpectedDate;
                        po.TotalAmount   = totalAmount;
                        po.Remarks       = dto.Remarks;
                        po.LASTUPDATE    = DateTime.Now;
                        po.LastUpdatedBy = dto.LastUpdatedBy;
                        _oBMSDbContext.PurchaseOrders.Update(po);
                        await _oBMSDbContext.SaveChangesAsync();

                        var oldDetails = _oBMSDbContext.PurchaseOrderDetails.Where(x => x.POID == po.ID);
                        _oBMSDbContext.PurchaseOrderDetails.RemoveRange(oldDetails);
                        await _oBMSDbContext.SaveChangesAsync();
                    }

                    foreach (var d in dto.Details)
                    {
                        _oBMSDbContext.PurchaseOrderDetails.Add(new PurchaseOrderDetail
                        {
                            POID          = po.ID,
                            ItemID        = d.ItemID,
                            UOMID         = d.UOMID == 0 ? null : d.UOMID,
                            OrderedQty    = d.OrderedQty,
                            ReceivedQty   = 0,
                            UnitPrice     = d.UnitPrice,
                            TotalPrice    = d.OrderedQty * d.UnitPrice,
                            Remarks       = d.Remarks,
                            LASTUPDATE    = DateTime.Now,
                            LastUpdatedBy = d.LastUpdatedBy ?? dto.LastUpdatedBy
                        });
                    }
                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedRecord = po;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            // When a new PO is created for an existing PR, mark the processing task Completed.
            // Runs after the transaction commits so savedRecord is guaranteed populated.
            // Non-throwing — a failure here must never undo the PO save.
            if (dto.ID == 0 && dto.PRID.HasValue && dto.PRID > 0 && savedRecord != null)
                await CompletePRProcessingTask(dto.PRID.Value, dto.LastUpdatedBy ?? "", poNumber);

            return (true, dto.ID == 0 ? $"Purchase Order {poNumber} saved successfully." : "Purchase order updated successfully.", savedRecord);
        }

        public async Task<(bool Success, string Message)> ApprovePO(int id, POApprovalDto dto)
        {
            var po = await _oBMSDbContext.PurchaseOrders.FindAsync(id);
            if (po == null) return (false, "This purchase order could not be found.");
            // Accept Draft (legacy flow) or Submitted (new Part 7 flow)
            if (po.Status != "Draft" && po.Status != "Submitted")
                return (false, $"Only a Draft or Submitted purchase order can be approved. This order is currently {po.Status}.");

            // ── Security: verify the authenticated user is the configured approver ──
            var submitter = po.SubmittedBy ?? po.LastUpdatedBy ?? string.Empty;
            // BUG-13 FIX: use IsSuperAdmin from dto (stamped by controller from JWT claim).
            // No more hard-coded "superadmin" username string check.
            bool isSuperAdmin = dto.IsSuperAdmin;

            // BUG-B FIX: add self-approval guard that was missing from this legacy method.
            // ApprovePOWithHistory (ApprovalRepository) already has this guard; this legacy
            // path was inconsistent.  SuperAdmin is exempt — they can approve any document
            // including their own submissions.
            bool isSelfApproval = !isSuperAdmin &&
                                  !string.IsNullOrWhiteSpace(submitter) &&
                                  string.Equals(dto.ApprovedBy, submitter,
                                                StringComparison.OrdinalIgnoreCase);
            if (isSelfApproval)
                return (false, "You cannot approve a document that you submitted yourself.");

            if (!isSuperAdmin && _approval != null)
            {
                var authorized = await _approval.IsAuthorizedApprover(
                    dto.ApprovedBy, submitter, "PURCHASE_ORDER");
                if (!authorized)
                    return (false, "You are not authorized to approve this document.");
            }

            string prev      = po.Status;
            po.Status        = "Approved";
            po.ApprovedBy    = dto.ApprovedBy;
            po.ApprovedDate  = DateTime.Now;
            po.LASTUPDATE    = DateTime.Now;
            po.LastUpdatedBy = dto.ApprovedBy;
            await _oBMSDbContext.SaveChangesAsync();

            // Part 7: record history + notify requester (awaited — not fire-and-forget)
            if (_approval != null)
                await _approval.RecordHistory("PURCHASE_ORDER", po.ID, po.PONumber,
                    "Approved", dto.ApprovedBy, prev, "Approved", null);
            if (_notif != null && !string.IsNullOrEmpty(po.SubmittedBy))
                await _notif.SendApprovalNotification(
                    "APPROVED", "PURCHASE_ORDER", po.PONumber, po.ID,
                    po.SubmittedBy, dto.ApprovedBy, null);

            // Notify Purchase department — approved PO enables GRN; Purchase team should be aware
            if (_notif != null)
                await _notif.SendApprovalNotificationToDepartment(
                    "APPROVED", "PURCHASE_ORDER", po.PONumber, po.ID,
                    "Purchase", dto.ApprovedBy, null,
                    excludeUsers: new[] { po.SubmittedBy ?? string.Empty });

            return (true, $"Purchase Order {po.PONumber} has been approved. Goods can now be received against this order.");
        }

        public async Task<(bool Success, string Message)> CancelPO(int id, string currentUser)
        {
            var po = await _oBMSDbContext.PurchaseOrders.FindAsync(id);
            if (po == null) return (false, "This purchase order could not be found.");
            if (po.Status == "Fully Received")
                return (false, $"Purchase Order {po.PONumber} cannot be cancelled because all goods have already been received.");
            if (po.Status == "Partially Received")
                return (false, $"Purchase Order {po.PONumber} cannot be cancelled because goods have already been partially received against it.");
            if (po.Status == "Cancelled")
                return (false, "This purchase order has already been cancelled.");

            // ── Dependency check: explicit GRN message even if status wasn't Partially/Fully Received ──
            var linkedGRN = await _oBMSDbContext.GoodsReceiptNotes
                .Where(g => g.POID == id && g.IsActive)
                .Select(g => new { g.GRNNumber })
                .FirstOrDefaultAsync();
            if (linkedGRN != null)
                return (false, $"Purchase Order {po.PONumber} cannot be cancelled because Goods Receipt {linkedGRN.GRNNumber} has already been created for this PO. Please delete Goods Receipt {linkedGRN.GRNNumber} first.");

            po.Status        = "Cancelled";
            po.IsActive      = false;
            po.LASTUPDATE    = DateTime.Now;
            po.LastUpdatedBy = currentUser;
            await _oBMSDbContext.SaveChangesAsync();
            return (true, $"Purchase Order {po.PONumber} has been cancelled.");
        }

        /// <summary>
        /// Returns the editability state of a PO for the frontend:
        /// - CanEdit: is the form currently saveable?
        /// - IsPRLinked: is the item list locked (only Qty/Price changeable)?
        /// - Status: current PO status for banner display
        /// - BlockReason: why editing is blocked (when CanEdit = false)
        /// </summary>
        public async Task<POEditabilityDto> GetPOEditability(int id)
        {
            var po = await _oBMSDbContext.PurchaseOrders.FindAsync(id);
            if (po == null)
                return new POEditabilityDto { CanEdit = false, Status = "Unknown", BlockReason = "This purchase order could not be found." };

            // Check active GRN
            var activeGRN = await _oBMSDbContext.GoodsReceiptNotes
                .Where(g => g.POID == id && g.IsActive)
                .Select(g => new { g.GRNNumber })
                .FirstOrDefaultAsync();

            // PO is editable only if: Draft, OR Approved with no active GRN
            bool canEdit = po.Status == "Draft" ||
                           (po.Status == "Approved" && activeGRN == null);

            string? blockReason = null;
            if (!canEdit)
            {
                if (activeGRN != null)
                    blockReason = $"Goods Receipt {activeGRN.GRNNumber} has been posted for this PO. Delete the GRN first to re-enable editing.";
                else
                    blockReason = $"This purchase order cannot be edited because it is currently {po.Status}.";
            }

            // PR link info
            string? linkedPRNumber = null;
            if (po.PRID.HasValue && po.PRID > 0)
            {
                var pr = await _oBMSDbContext.PurchaseRequisitions
                    .Where(r => r.ID == po.PRID.Value)
                    .Select(r => new { r.PRNumber })
                    .FirstOrDefaultAsync();
                linkedPRNumber = pr?.PRNumber;
            }

            return new POEditabilityDto
            {
                CanEdit        = canEdit,
                IsPRLinked     = po.PRID.HasValue && po.PRID > 0,
                LinkedPRNumber = linkedPRNumber,
                Status         = po.Status,
                BlockReason    = blockReason
            };
        }


        // ════════════════════════════════════════════════════════════════════════
        // PART 3 — GOODS RECEIPT NOTE
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<GRNListDto>> GetGRNList()
        {
            var list = await (
                from grn in _oBMSDbContext.GoodsReceiptNotes
                join po  in _oBMSDbContext.PurchaseOrders      on grn.POID        equals po.ID
                join sup in _oBMSDbContext.Suppliers           on po.SupplierID   equals sup.Id
                join wh  in _oBMSDbContext.WarehouseMasters    on grn.WarehouseID equals wh.ID
                where grn.IsActive == true
                orderby grn.GRNDate descending, grn.ID descending
                select new GRNListDto
                {
                    ID              = grn.ID,
                    GRNNumber       = grn.GRNNumber,
                    POID            = grn.POID,
                    PONumber        = po.PONumber,
                    SupplierName    = sup.Name,
                    WarehouseID     = grn.WarehouseID,
                    WarehouseName   = wh.Name,
                    GRNDate         = grn.GRNDate,
                    DeliveryNoteRef = grn.DeliveryNoteRef,
                    SupplierInvoiceNo = grn.SupplierInvoiceNo,
                    Remarks         = grn.Remarks,
                    LASTUPDATE      = grn.LASTUPDATE,
                    LastUpdatedBy   = grn.LastUpdatedBy
                }
            ).ToListAsync();

            foreach (var g in list)
                g.Details = await GetGRNDetails(g.ID);
            return list;
        }

        public async Task<GRNListDto?> GetGRNByID(int id)
        {
            var grn = await (
                from g   in _oBMSDbContext.GoodsReceiptNotes
                join po  in _oBMSDbContext.PurchaseOrders   on g.POID        equals po.ID
                join sup in _oBMSDbContext.Suppliers        on po.SupplierID equals sup.Id
                join wh  in _oBMSDbContext.WarehouseMasters on g.WarehouseID equals wh.ID
                where g.ID == id
                select new GRNListDto
                {
                    ID                = g.ID,
                    GRNNumber         = g.GRNNumber,
                    POID              = g.POID,
                    PONumber          = po.PONumber,
                    SupplierName      = sup.Name,
                    WarehouseID       = g.WarehouseID,
                    WarehouseName     = wh.Name,
                    GRNDate           = g.GRNDate,
                    DeliveryNoteRef   = g.DeliveryNoteRef,
                    SupplierInvoiceNo = g.SupplierInvoiceNo,
                    Remarks           = g.Remarks,
                    LASTUPDATE        = g.LASTUPDATE,
                    LastUpdatedBy     = g.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();

            if (grn != null)
                grn.Details = await GetGRNDetails(id);
            return grn;
        }

        /// <summary>
        /// Returns the first active GRN linked to a given PO ID, or null if none exists.
        /// Used by the PO screen to show a "GRN already posted" banner.
        /// </summary>
        public async Task<GRNListDto?> GetGRNByPOID(int poID)
        {
            var grn = await (
                from g   in _oBMSDbContext.GoodsReceiptNotes
                join po  in _oBMSDbContext.PurchaseOrders   on g.POID        equals po.ID
                join sup in _oBMSDbContext.Suppliers        on po.SupplierID equals sup.Id
                join wh  in _oBMSDbContext.WarehouseMasters on g.WarehouseID equals wh.ID
                where g.POID == poID && g.IsActive == true
                orderby g.GRNDate descending
                select new GRNListDto
                {
                    ID                = g.ID,
                    GRNNumber         = g.GRNNumber,
                    POID              = g.POID,
                    PONumber          = po.PONumber,
                    SupplierName      = sup.Name,
                    WarehouseID       = g.WarehouseID,
                    WarehouseName     = wh.Name,
                    GRNDate           = g.GRNDate,
                    DeliveryNoteRef   = g.DeliveryNoteRef,
                    SupplierInvoiceNo = g.SupplierInvoiceNo,
                    Remarks           = g.Remarks,
                    LASTUPDATE        = g.LASTUPDATE,
                    LastUpdatedBy     = g.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();

            if (grn != null)
                grn.Details = await GetGRNDetails(grn.ID);
            return grn;
        }

        private async Task<List<GRNDetailListDto>> GetGRNDetails(int grnID)
        {
            return await (
                from d  in _oBMSDbContext.GoodsReceiptNoteDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on d.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where d.GRNID == grnID
                select new GRNDetailListDto
                {
                    ID           = d.ID,
                    GRNID        = d.GRNID,
                    PODetailID   = d.PODetailID,
                    ItemID       = d.ItemID,
                    ItemCode     = im.Code ?? "",
                    ItemName     = im.Name,
                    CategoryName = ic != null ? ic.Name : "",
                    UOMID        = d.UOMID,
                    UOMName      = um != null ? um.Name : "",
                    ReceivedQty  = d.ReceivedQty,
                    UnitCost     = d.UnitCost
                }
            ).ToListAsync();
        }

        public async Task<List<POListDto>> GetApprovedPOsForGRN()
        {
            var list = await POListQuery()
                .Where(po => po.Status == "Approved" || po.Status == "Partially Received")
                .ToListAsync();
            return list;
        }

        public async Task<List<POLineForGRNDto>> GetPOLinesForGRN(int poID)
        {
            return await (
                from d  in _oBMSDbContext.PurchaseOrderDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on d.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where d.POID == poID && (d.OrderedQty - d.ReceivedQty) > 0
                select new POLineForGRNDto
                {
                    PODetailID   = d.ID,
                    ItemID       = d.ItemID,
                    ItemCode     = im.Code ?? "",
                    ItemName     = im.Name,
                    CategoryName = ic != null ? ic.Name : "",
                    UOMID        = d.UOMID,
                    UOMName      = um != null ? um.Name : "",
                    OrderedQty   = d.OrderedQty,
                    ReceivedQty  = d.ReceivedQty,
                    PendingQty   = d.OrderedQty - d.ReceivedQty,
                    UnitCost     = d.UnitPrice
                }
            ).ToListAsync();
        }

        public async Task<(bool Success, string Message, GoodsReceiptNote? Record)> SaveGRN(GRNRequestDto dto)
        {
            // Validate PO
            var po = await _oBMSDbContext.PurchaseOrders.FindAsync(dto.POID);
            if (po == null)
                return (false, "The selected purchase order could not be found.", null);
            if (po.Status != "Approved" && po.Status != "Partially Received")
                return (false, $"Goods can only be received against an Approved purchase order. Purchase Order {po.PONumber} is currently {po.Status}.", null);

            // Filter to lines with actual qty entered
            var linesWithQty = dto.Details.Where(d => d.ReceivedQty > 0).ToList();
            if (!linesWithQty.Any())
                return (false, "Please enter the received quantity for at least one item.", null);

            // Per-line over-receive validation
            foreach (var line in linesWithQty)
            {
                var poDetail = await _oBMSDbContext.PurchaseOrderDetails.FindAsync(line.PODetailID);
                if (poDetail == null) continue;
                var pending = poDetail.OrderedQty - poDetail.ReceivedQty;
                if (line.ReceivedQty > pending)
                {
                    var item = await _oBMSDbContext.ItemMasters.FindAsync(line.ItemID);
                    return (false,
                        $"Received quantity is more than the remaining quantity for \"{item?.Name ?? "the item"}\".\n" +
                        $"Ordered: {poDetail.OrderedQty}   Already Received: {poDetail.ReceivedQty}   Remaining: {pending}\n" +
                        $"Please enter {pending} or less.", null);
                }
            }

            // Generate GRN number
            var maxGRN = await _oBMSDbContext.GoodsReceiptNotes
                .Where(x => x.GRNNumber != null && x.GRNNumber.StartsWith("GRN"))
                .OrderByDescending(x => x.GRNNumber)
                .FirstOrDefaultAsync();
            string grnNumber = (maxGRN == null || string.IsNullOrEmpty(maxGRN.GRNNumber))
                ? "GRN0001"
                : int.TryParse(maxGRN.GRNNumber.Substring(3), out int n)
                    ? "GRN" + (n + 1).ToString("D4")
                    : "GRN0001";

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            GoodsReceiptNote? savedRecord = null;

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    // Save GRN header
                    var grn = new GoodsReceiptNote
                    {
                        GRNNumber         = grnNumber,
                        POID              = dto.POID,
                        WarehouseID       = dto.WarehouseID,
                        GRNDate           = dto.GRNDate,
                        DeliveryNoteRef   = dto.DeliveryNoteRef,
                        SupplierInvoiceNo = dto.SupplierInvoiceNo,
                        Remarks           = dto.Remarks,
                        IsActive          = true,
                        LASTUPDATE        = DateTime.Now,
                        LastUpdatedBy     = dto.LastUpdatedBy
                    };
                    _oBMSDbContext.GoodsReceiptNotes.Add(grn);
                    await _oBMSDbContext.SaveChangesAsync();

                    // Save GRN detail lines + update PO received qty + create StockLedger entries
                    foreach (var line in linesWithQty)
                    {
                        _oBMSDbContext.GoodsReceiptNoteDetails.Add(new GoodsReceiptNoteDetail
                        {
                            GRNID         = grn.ID,
                            PODetailID    = line.PODetailID,
                            ItemID        = line.ItemID,
                            UOMID         = line.UOMID == 0 ? null : line.UOMID,
                            ReceivedQty   = line.ReceivedQty,
                            UnitCost      = line.UnitCost,
                            LASTUPDATE    = DateTime.Now,
                            LastUpdatedBy = line.LastUpdatedBy ?? dto.LastUpdatedBy
                        });

                        // Update PO detail received qty
                        var poDetail = await _oBMSDbContext.PurchaseOrderDetails.FindAsync(line.PODetailID);
                        if (poDetail != null)
                        {
                            poDetail.ReceivedQty  += line.ReceivedQty;
                            poDetail.LASTUPDATE    = DateTime.Now;
                            poDetail.LastUpdatedBy = dto.LastUpdatedBy;
                            _oBMSDbContext.PurchaseOrderDetails.Update(poDetail);
                        }

                        // StockLedger STOCK_IN entry
                        _oBMSDbContext.StockLedgers.Add(new StockLedger
                        {
                            TransactionNo   = await GenerateStockLedgerNo(),
                            TransactionDate = dto.GRNDate,
                            TransactionType = "STOCK_IN",
                            WarehouseID     = dto.WarehouseID,
                            ItemID          = line.ItemID,
                            InQty           = line.ReceivedQty,
                            OutQty          = 0,
                            UnitCost        = line.UnitCost,
                            ReferenceType   = "Goods Receipt",
                            ReferenceID     = grn.ID,
                            ReferenceNo     = grnNumber,
                            Remarks         = dto.Remarks,
                            LASTUPDATE      = DateTime.Now,
                            LastUpdatedBy   = dto.LastUpdatedBy
                        });
                    }
                    await _oBMSDbContext.SaveChangesAsync();

                    // Recalculate PO status based on all lines
                    var allPODetails = await _oBMSDbContext.PurchaseOrderDetails
                        .Where(x => x.POID == dto.POID).ToListAsync();
                    bool allReceived = allPODetails.All(x => x.ReceivedQty >= x.OrderedQty);
                    po.Status        = allReceived ? "Fully Received" : "Partially Received";
                    po.LASTUPDATE    = DateTime.Now;
                    po.LastUpdatedBy = dto.LastUpdatedBy;
                    _oBMSDbContext.PurchaseOrders.Update(po);
                    await _oBMSDbContext.SaveChangesAsync();

                    // ── Create CreditorInvoice row so this GRN appears in vwPaymentDue ──
                    // Get branch code via Warehouse → BranchMaster
                    var warehouse = await _oBMSDbContext.WarehouseMasters
                        .Include(w => w.Branch)
                        .FirstOrDefaultAsync(w => w.ID == dto.WarehouseID);
                    string branchCode = warehouse?.Branch?.Code ?? "";

                    // GRN total = sum of (ReceivedQty × UnitCost) across all received lines
                    decimal grnTotal = linesWithQty.Sum(l => l.ReceivedQty * l.UnitCost);

                    // Payment due date: use SupplierInvoiceNo as InvoiceNo; due date = GRNDate
                    _oBMSDbContext.CreditorInvoices.Add(new CreditorInvoice
                    {
                        Branch        = branchCode,
                        Supplier      = po.SupplierID,
                        RecID         = grn.ID,                         // link back to GRN
                        InvoiceNo     = string.IsNullOrWhiteSpace(dto.SupplierInvoiceNo)
                                            ? grnNumber
                                            : dto.SupplierInvoiceNo,
                        InvoiceType   = "G",                            // G = GRN (already handled by view)
                        InvoiceDate   = dto.GRNDate,
                        Total         = grnTotal,
                        PaymentDate   = dto.GRNDate,                    // due date = GRN date
                        LASTUPDATE    = DateTime.Now,
                        LastUpdatedBy = dto.LastUpdatedBy ?? "",
                        ItemCategory  = null,
                        IsDeleted     = false
                    });
                    await _oBMSDbContext.SaveChangesAsync();

                    await tx.CommitAsync();
                    savedRecord = grn;

                    // Part 7: low-stock check for each received item
                    if (_notif != null)
                    {
                        foreach (var line in linesWithQty)
                            _ = _notif.CheckAndNotifyLowStock(dto.WarehouseID, line.ItemID, dto.LastUpdatedBy ?? "system");
                    }
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Goods Receipt {grnNumber} saved successfully. Stock has been updated.", savedRecord);
        }

        /// <summary>
        /// Checks downstream dependencies for a GRN before allowing delete.
        /// Returns (canDelete, blockingReason) where blockingReason is null when safe to delete.
        /// </summary>
        public async Task<(bool CanDelete, string? BlockingMessage, string? BlockingDocNumber)> CheckGRNDependency(int id)
        {
            var grn = await _oBMSDbContext.GoodsReceiptNotes
                .Where(g => g.ID == id && g.IsActive)
                .Select(g => new { g.GRNNumber })
                .FirstOrDefaultAsync();
            if (grn == null)
                return (false, "This goods receipt could not be found.", null);

            // 1. Check Finance Payment dependency
            var linkedPayment = await _oBMSDbContext.BranchPaymentDetails
                .Where(p => p.GRNID == id && p.IsDeleted != true)
                .Select(p => new { p.PaymentID })
                .FirstOrDefaultAsync();
            if (linkedPayment != null)
                return (false,
                    $"Goods Receipt {grn.GRNNumber} cannot be deleted because it has already been used in Finance Payment (Payment ID: {linkedPayment.PaymentID}). Please reverse or cancel the related payment first.",
                    grn.GRNNumber);

            // 2. Check Purchase Return dependency
            var linkedReturn = await _oBMSDbContext.PurchaseReturns
                .Where(r => r.GRNID == id && r.IsActive)
                .Select(r => new { r.ReturnNumber })
                .FirstOrDefaultAsync();
            if (linkedReturn != null)
                return (false,
                    $"Goods Receipt {grn.GRNNumber} cannot be deleted because Purchase Return {linkedReturn.ReturnNumber} has been created against it. Please delete Purchase Return {linkedReturn.ReturnNumber} first.",
                    grn.GRNNumber);

            return (true, null, grn.GRNNumber);
        }

        /// <summary>
        /// Deletes a GRN transactionally:
        /// - Blocks if linked to a Finance Payment or Purchase Return.
        /// - Reverses StockLedger STOCK_IN rows.
        /// - Decrements PurchaseOrderDetail.ReceivedQty.
        /// - Recalculates PurchaseOrder status.
        /// - Soft-deletes the GRN and its detail lines.
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteGRN(int id, string currentUser)
        {
            var grn = await _oBMSDbContext.GoodsReceiptNotes.FindAsync(id);
            if (grn == null || !grn.IsActive)
                return (false, "This goods receipt could not be found.");

            // ── Dependency checks (re-use helper logic inline for atomicity) ──
            var paymentBlock = await _oBMSDbContext.BranchPaymentDetails
                .Where(p => p.GRNID == id && p.IsDeleted != true)
                .Select(p => new { p.PaymentID })
                .FirstOrDefaultAsync();
            if (paymentBlock != null)
                return (false, $"Goods Receipt {grn.GRNNumber} cannot be deleted because it has already been used in Finance Payment (Payment ID: {paymentBlock.PaymentID}). Please reverse or cancel the related payment first.");

            var returnBlock = await _oBMSDbContext.PurchaseReturns
                .Where(r => r.GRNID == id && r.IsActive)
                .Select(r => new { r.ReturnNumber })
                .FirstOrDefaultAsync();
            if (returnBlock != null)
                return (false, $"Goods Receipt {grn.GRNNumber} cannot be deleted because Purchase Return {returnBlock.ReturnNumber} has been created against it. Please delete Purchase Return {returnBlock.ReturnNumber} first.");

            // ── Transactional reversal ──────────────────────────────────────
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            string resultMessage = "";
            bool resultSuccess = false;

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    // Load GRN detail lines before soft-delete
                    var grnDetails = await _oBMSDbContext.GoodsReceiptNoteDetails
                        .Where(d => d.GRNID == id)
                        .ToListAsync();

                    // 1. Remove StockLedger STOCK_IN entries created by this GRN
                    var ledgerRows = await _oBMSDbContext.StockLedgers
                        .Where(sl => sl.ReferenceID == id && sl.ReferenceType == "Goods Receipt" && sl.TransactionType == "STOCK_IN")
                        .ToListAsync();
                    if (ledgerRows.Any())
                        _oBMSDbContext.StockLedgers.RemoveRange(ledgerRows);

                    // 2. Reverse PurchaseOrderDetail.ReceivedQty for each line
                    foreach (var detail in grnDetails)
                    {
                        var poDetail = await _oBMSDbContext.PurchaseOrderDetails.FindAsync(detail.PODetailID);
                        if (poDetail != null)
                        {
                            poDetail.ReceivedQty  = Math.Max(0, poDetail.ReceivedQty - detail.ReceivedQty);
                            poDetail.LASTUPDATE    = DateTime.Now;
                            poDetail.LastUpdatedBy = currentUser;
                            _oBMSDbContext.PurchaseOrderDetails.Update(poDetail);
                        }
                    }

                    await _oBMSDbContext.SaveChangesAsync();

                    // 3. Recalculate PurchaseOrder status
                    var po = await _oBMSDbContext.PurchaseOrders.FindAsync(grn.POID);
                    if (po != null)
                    {
                        // Check if any other active GRNs exist for this PO
                        var otherGRNs = await _oBMSDbContext.GoodsReceiptNotes
                            .AnyAsync(g => g.POID == grn.POID && g.IsActive && g.ID != id);

                        if (otherGRNs)
                        {
                            // Recalculate from remaining GRNs
                            var allPODetails = await _oBMSDbContext.PurchaseOrderDetails
                                .Where(x => x.POID == grn.POID).ToListAsync();
                            bool allReceived = allPODetails.All(x => x.ReceivedQty >= x.OrderedQty);
                            po.Status = allReceived ? "Fully Received" : "Partially Received";
                        }
                        else
                        {
                            // No more GRNs — revert to Approved
                            po.Status = "Approved";
                            po.IsActive = true; // ensure PO remains active
                        }
                        po.LASTUPDATE    = DateTime.Now;
                        po.LastUpdatedBy = currentUser;
                        _oBMSDbContext.PurchaseOrders.Update(po);
                    }

                    // 4. Soft-delete linked CreditorInvoice row (InvoiceType='G', RecID=GRN.ID)
                    //    so it disappears from vwPaymentDue
                    var linkedCI = await _oBMSDbContext.CreditorInvoices
                        .FirstOrDefaultAsync(ci => ci.InvoiceType == "G"
                                                && ci.RecID       == id
                                                && ci.IsDeleted   == false);
                    if (linkedCI != null)
                    {
                        linkedCI.IsDeleted     = true;
                        linkedCI.LASTUPDATE    = DateTime.Now;
                        linkedCI.LastUpdatedBy = currentUser;
                        _oBMSDbContext.CreditorInvoices.Update(linkedCI);
                    }

                    // 5. Soft-delete GRN
                    grn.IsActive      = false;
                    grn.LASTUPDATE    = DateTime.Now;
                    grn.LastUpdatedBy = currentUser;
                    _oBMSDbContext.GoodsReceiptNotes.Update(grn);

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();

                    resultSuccess = true;
                    resultMessage = $"Goods Receipt {grn.GRNNumber} has been deleted. Stock and purchase order status have been reversed.";
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (resultSuccess, resultMessage);
        }


        // ════════════════════════════════════════════════════════════════════════
        // PART 3 — PURCHASE RETURN
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<PurchaseReturnListDto>> GetPurchaseReturnList()
        {
            var list = await (
                from ret in _oBMSDbContext.PurchaseReturns
                join grn in _oBMSDbContext.GoodsReceiptNotes on ret.GRNID       equals grn.ID
                join po  in _oBMSDbContext.PurchaseOrders    on grn.POID        equals po.ID
                join sup in _oBMSDbContext.Suppliers         on po.SupplierID   equals sup.Id
                join wh  in _oBMSDbContext.WarehouseMasters  on ret.WarehouseID equals wh.ID
                where ret.IsActive == true
                orderby ret.ReturnDate descending, ret.ID descending
                select new PurchaseReturnListDto
                {
                    ID            = ret.ID,
                    ReturnNumber  = ret.ReturnNumber,
                    GRNID         = ret.GRNID,
                    GRNNumber     = grn.GRNNumber,
                    PONumber      = po.PONumber,
                    SupplierName  = sup.Name,
                    WarehouseID   = ret.WarehouseID,
                    WarehouseName = wh.Name,
                    ReturnDate    = ret.ReturnDate,
                    Reason        = ret.Reason,
                    LASTUPDATE    = ret.LASTUPDATE,
                    LastUpdatedBy = ret.LastUpdatedBy
                }
            ).ToListAsync();

            foreach (var r in list)
                r.Details = await GetReturnDetails(r.ID);
            return list;
        }

        public async Task<PurchaseReturnListDto?> GetPurchaseReturnByID(int id)
        {
            var ret = await (
                from r   in _oBMSDbContext.PurchaseReturns
                join grn in _oBMSDbContext.GoodsReceiptNotes on r.GRNID        equals grn.ID
                join po  in _oBMSDbContext.PurchaseOrders    on grn.POID       equals po.ID
                join sup in _oBMSDbContext.Suppliers         on po.SupplierID  equals sup.Id
                join wh  in _oBMSDbContext.WarehouseMasters  on r.WarehouseID  equals wh.ID
                where r.ID == id
                select new PurchaseReturnListDto
                {
                    ID            = r.ID,
                    ReturnNumber  = r.ReturnNumber,
                    GRNID         = r.GRNID,
                    GRNNumber     = grn.GRNNumber,
                    PONumber      = po.PONumber,
                    SupplierName  = sup.Name,
                    WarehouseID   = r.WarehouseID,
                    WarehouseName = wh.Name,
                    ReturnDate    = r.ReturnDate,
                    Reason        = r.Reason,
                    LASTUPDATE    = r.LASTUPDATE,
                    LastUpdatedBy = r.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();

            if (ret != null)
                ret.Details = await GetReturnDetails(id);
            return ret;
        }

        private async Task<List<PurchaseReturnDetailListDto>> GetReturnDetails(int returnID)
        {
            return await (
                from d  in _oBMSDbContext.PurchaseReturnDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on d.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where d.ReturnID == returnID
                select new PurchaseReturnDetailListDto
                {
                    ID           = d.ID,
                    ReturnID     = d.ReturnID,
                    GRNDetailID  = d.GRNDetailID,
                    ItemID       = d.ItemID,
                    ItemCode     = im.Code ?? "",
                    ItemName     = im.Name,
                    CategoryName = ic != null ? ic.Name : "",
                    UOMID        = d.UOMID,
                    UOMName      = um != null ? um.Name : "",
                    ReturnQty    = d.ReturnQty,
                    UnitCost     = d.UnitCost
                }
            ).ToListAsync();
        }

        public async Task<List<GRNListDto>> GetGRNsForReturn()
        {
            var list = await (
                from grn in _oBMSDbContext.GoodsReceiptNotes
                join po  in _oBMSDbContext.PurchaseOrders   on grn.POID        equals po.ID
                join sup in _oBMSDbContext.Suppliers        on po.SupplierID   equals sup.Id
                join wh  in _oBMSDbContext.WarehouseMasters on grn.WarehouseID equals wh.ID
                where grn.IsActive == true
                orderby grn.GRNDate descending
                select new GRNListDto
                {
                    ID            = grn.ID,
                    GRNNumber     = grn.GRNNumber,
                    POID          = grn.POID,
                    PONumber      = po.PONumber,
                    SupplierName  = sup.Name,
                    WarehouseID   = grn.WarehouseID,
                    WarehouseName = wh.Name,
                    GRNDate       = grn.GRNDate,
                    DeliveryNoteRef = grn.DeliveryNoteRef,
                    Remarks       = grn.Remarks,
                    LASTUPDATE    = grn.LASTUPDATE,
                    LastUpdatedBy = grn.LastUpdatedBy
                }
            ).ToListAsync();
            return list;
        }

        public async Task<List<GRNLineForReturnDto>> GetGRNLinesForReturn(int grnID)
        {
            var grnDetails = await (
                from d  in _oBMSDbContext.GoodsReceiptNoteDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on d.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where d.GRNID == grnID
                select new { d.ID, d.ItemID, im.Code, im.Name, Cat = ic != null ? ic.Name : "", d.UOMID, UOM = um != null ? um.Name : "", d.ReceivedQty, d.UnitCost }
            ).ToListAsync();

            var result = new List<GRNLineForReturnDto>();
            foreach (var row in grnDetails)
            {
                decimal alreadyReturned = await _oBMSDbContext.PurchaseReturnDetails
                    .Where(x => x.GRNDetailID == row.ID)
                    .SumAsync(x => (decimal?)x.ReturnQty) ?? 0;

                decimal returnable = row.ReceivedQty - alreadyReturned;
                if (returnable > 0)
                {
                    result.Add(new GRNLineForReturnDto
                    {
                        GRNDetailID        = row.ID,
                        ItemID             = row.ItemID,
                        ItemCode           = row.Code ?? "",
                        ItemName           = row.Name,
                        CategoryName       = row.Cat,
                        UOMID              = row.UOMID,
                        UOMName            = row.UOM,
                        ReceivedQty        = row.ReceivedQty,
                        AlreadyReturnedQty = alreadyReturned,
                        ReturnableQty      = returnable,
                        UnitCost           = row.UnitCost
                    });
                }
            }
            return result;
        }

        public async Task<(bool Success, string Message, PurchaseReturn? Record)> SavePurchaseReturn(PurchaseReturnRequestDto dto)
        {
            // Validate GRN exists
            var grn = await _oBMSDbContext.GoodsReceiptNotes.FindAsync(dto.GRNID);
            if (grn == null)
                return (false, "The selected goods receipt could not be found.", null);

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return (false, "Please enter a reason for the return.", null);

            var linesWithQty = dto.Details.Where(d => d.ReturnQty > 0).ToList();
            if (!linesWithQty.Any())
                return (false, "Please enter a return quantity for at least one item.", null);

            // Per-line over-return validation
            foreach (var line in linesWithQty)
            {
                var grnDetail = await _oBMSDbContext.GoodsReceiptNoteDetails.FindAsync(line.GRNDetailID);
                if (grnDetail == null) continue;

                decimal alreadyReturned = await _oBMSDbContext.PurchaseReturnDetails
                    .Where(x => x.GRNDetailID == line.GRNDetailID)
                    .SumAsync(x => (decimal?)x.ReturnQty) ?? 0;

                decimal returnable = grnDetail.ReceivedQty - alreadyReturned;
                if (line.ReturnQty > returnable)
                {
                    var item = await _oBMSDbContext.ItemMasters.FindAsync(line.ItemID);
                    return (false,
                        $"Return quantity is more than the available return quantity for \"{item?.Name ?? "the item"}\".\n" +
                        $"Received: {grnDetail.ReceivedQty}   Already Returned: {alreadyReturned}   Returnable: {returnable}\n" +
                        $"Please enter {returnable} or less.", null);
                }
            }

            // Generate return number
            var maxRtn = await _oBMSDbContext.PurchaseReturns
                .Where(x => x.ReturnNumber != null && x.ReturnNumber.StartsWith("RTN"))
                .OrderByDescending(x => x.ReturnNumber)
                .FirstOrDefaultAsync();
            string rtnNumber = (maxRtn == null || string.IsNullOrEmpty(maxRtn.ReturnNumber))
                ? "RTN0001"
                : int.TryParse(maxRtn.ReturnNumber.Substring(3), out int n)
                    ? "RTN" + (n + 1).ToString("D4")
                    : "RTN0001";

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            PurchaseReturn? savedRecord = null;

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var ret = new PurchaseReturn
                    {
                        ReturnNumber  = rtnNumber,
                        GRNID         = dto.GRNID,
                        WarehouseID   = dto.WarehouseID,
                        ReturnDate    = dto.ReturnDate,
                        Reason        = dto.Reason,
                        IsActive      = true,
                        LASTUPDATE    = DateTime.Now,
                        LastUpdatedBy = dto.LastUpdatedBy
                    };
                    _oBMSDbContext.PurchaseReturns.Add(ret);
                    await _oBMSDbContext.SaveChangesAsync();

                    foreach (var line in linesWithQty)
                    {
                        var grnDetail = await _oBMSDbContext.GoodsReceiptNoteDetails.FindAsync(line.GRNDetailID);

                        _oBMSDbContext.PurchaseReturnDetails.Add(new PurchaseReturnDetail
                        {
                            ReturnID      = ret.ID,
                            GRNDetailID   = line.GRNDetailID,
                            ItemID        = line.ItemID,
                            UOMID         = line.UOMID == 0 ? null : line.UOMID,
                            ReturnQty     = line.ReturnQty,
                            UnitCost      = line.UnitCost,
                            LASTUPDATE    = DateTime.Now,
                            LastUpdatedBy = line.LastUpdatedBy ?? dto.LastUpdatedBy
                        });

                        // StockLedger PURCHASE_RTN (Out) entry
                        _oBMSDbContext.StockLedgers.Add(new StockLedger
                        {
                            TransactionNo   = await GenerateStockLedgerNo(),
                            TransactionDate = dto.ReturnDate,
                            TransactionType = "PURCHASE_RTN",
                            WarehouseID     = dto.WarehouseID,
                            ItemID          = line.ItemID,
                            InQty           = 0,
                            OutQty          = line.ReturnQty,
                            UnitCost        = line.UnitCost,
                            ReferenceType   = "Purchase Return",
                            ReferenceID     = ret.ID,
                            ReferenceNo     = rtnNumber,
                            Remarks         = dto.Reason,
                            LASTUPDATE      = DateTime.Now,
                            LastUpdatedBy   = dto.LastUpdatedBy
                        });
                    }
                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedRecord = ret;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Purchase Return {rtnNumber} saved successfully. Stock has been updated.", savedRecord);
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 4 — STOCK OPERATIONS
        // ════════════════════════════════════════════════════════════════════════

        // ── Shared helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the current available stock (InQty - OutQty) for an item in a warehouse.
        /// This is the single source of truth — never read from ItemMaster.Quantity.
        /// </summary>
        private async Task<decimal> GetCurrentStockInternal(int warehouseID, int itemID)
        {
            var inQty  = await _oBMSDbContext.StockLedgers
                .Where(l => l.WarehouseID == warehouseID && l.ItemID == itemID)
                .SumAsync(l => (decimal?)l.InQty) ?? 0;
            var outQty = await _oBMSDbContext.StockLedgers
                .Where(l => l.WarehouseID == warehouseID && l.ItemID == itemID)
                .SumAsync(l => (decimal?)l.OutQty) ?? 0;
            return inQty - outQty;
        }

        // ────────────────────────────────────────────────────────────────────────
        // A. STOCK ISSUE
        // ────────────────────────────────────────────────────────────────────────

        public async Task<List<StockIssueDocListDto>> GetStockIssueList()
        {
            var docs = await (
                from sid in _oBMSDbContext.StockIssueDocs
                join wh in _oBMSDbContext.WarehouseMasters on sid.WarehouseID equals wh.ID
                where sid.IsActive == true
                orderby sid.IssueDate descending, sid.ID descending
                select new StockIssueDocListDto
                {
                    ID            = sid.ID,
                    IssueNo       = sid.IssueNo,
                    IssueDate     = sid.IssueDate,
                    WarehouseID   = sid.WarehouseID,
                    WarehouseName = wh.Name,
                    IssueToType   = sid.IssueToType,
                    IssueToID     = sid.IssueToID,
                    IssueToName   = sid.IssueToName,
                    Remarks       = sid.Remarks,
                    LASTUPDATE    = sid.LASTUPDATE,
                    LastUpdatedBy = sid.LastUpdatedBy,
                    Details       = new List<StockIssueDocDetailListDto>()
                }
            ).ToListAsync();
            return docs;
        }

        public async Task<StockIssueDocListDto?> GetStockIssueByID(int id)
        {
            var doc = await (
                from sid in _oBMSDbContext.StockIssueDocs
                join wh in _oBMSDbContext.WarehouseMasters on sid.WarehouseID equals wh.ID
                where sid.ID == id && sid.IsActive == true
                select new StockIssueDocListDto
                {
                    ID            = sid.ID,
                    IssueNo       = sid.IssueNo,
                    IssueDate     = sid.IssueDate,
                    WarehouseID   = sid.WarehouseID,
                    WarehouseName = wh.Name,
                    IssueToType   = sid.IssueToType,
                    IssueToID     = sid.IssueToID,
                    IssueToName   = sid.IssueToName,
                    Remarks       = sid.Remarks,
                    LASTUPDATE    = sid.LASTUPDATE,
                    LastUpdatedBy = sid.LastUpdatedBy,
                    Details       = new List<StockIssueDocDetailListDto>()
                }
            ).FirstOrDefaultAsync();

            if (doc == null) return null;

            doc.Details = await BuildIssueDetailList(id);
            return doc;
        }

        /// <summary>Builds enriched detail lines for a given issue doc ID.</summary>
        private async Task<List<StockIssueDocDetailListDto>> BuildIssueDetailList(int issueDocID)
        {
            var details = await (
                from d in _oBMSDbContext.StockIssueDocDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on d.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where d.IssueDocID == issueDocID
                select new StockIssueDocDetailListDto
                {
                    ID           = d.ID,
                    IssueDocID   = d.IssueDocID,
                    ItemID       = d.ItemID,
                    ItemCode     = im.Code ?? "",
                    ItemName     = im.Name,
                    CategoryName = ic != null ? ic.Name : "",
                    UOMID        = d.UOMID,
                    UOMName      = um != null ? um.Name : "",
                    IssueQty     = d.IssueQty,
                    UnitCost     = d.UnitCost,
                    TotalValue   = d.IssueQty * d.UnitCost,
                    Remarks      = d.Remarks
                }
            ).ToListAsync();

            // Compute already-returned quantities per detail line
            foreach (var line in details)
            {
                line.AlreadyReturnedQty = await _oBMSDbContext.StockIssueReturnDetails
                    .Where(r => r.OriginalIssueDetailID == line.ID)
                    .SumAsync(r => (decimal?)r.ReturnQty) ?? 0;
                line.ReturnableQty = line.IssueQty - line.AlreadyReturnedQty;
            }
            return details;
        }

        public async Task<(bool Success, string Message, StockIssueDoc? Record)> SaveStockIssue(
            StockIssueDocRequestDto dto)
        {
            // ── Validate warehouse ──────────────────────────────────────────────
            var warehouse = await _oBMSDbContext.WarehouseMasters.FindAsync(dto.WarehouseID);
            if (warehouse == null)
                return (false, "The selected warehouse is no longer available.", null);

            if (!dto.Details.Any())
                return (false, "Please add at least one item to the stock issue.", null);

            // ── Per-item stock availability check (backend) ─────────────────────
            foreach (var line in dto.Details)
            {
                if (line.IssueQty <= 0)
                    return (false, "Issue quantity must be greater than zero.", null);

                var available = await GetCurrentStockInternal(dto.WarehouseID, line.ItemID);
                if (line.IssueQty > available)
                {
                    var item = await _oBMSDbContext.ItemMasters.FindAsync(line.ItemID);
                    var itemName = item?.Name ?? $"Item #{line.ItemID}";
                    return (false,
                        $"Insufficient stock for '{itemName}'. Available quantity is {available:N2}.",
                        null);
                }
            }

            // ── Generate issue number ───────────────────────────────────────────
            string issueNo = dto.IssueNo ?? "";
            if (string.IsNullOrEmpty(issueNo))
            {
                var maxRow = await _oBMSDbContext.StockIssueDocs
                    .Where(x => x.IssueNo.StartsWith("SI"))
                    .OrderByDescending(x => x.IssueNo)
                    .FirstOrDefaultAsync();
                issueNo = (maxRow == null || string.IsNullOrEmpty(maxRow.IssueNo))
                    ? "SI0001"
                    : int.TryParse(maxRow.IssueNo.Substring(2), out int n)
                        ? "SI" + (n + 1).ToString("D4") : "SI0001";
            }

            StockIssueDoc? savedRecord = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    StockIssueDoc doc;
                    if (dto.ID > 0)
                    {
                        doc = await _oBMSDbContext.StockIssueDocs.FindAsync(dto.ID)
                              ?? throw new InvalidOperationException("Stock issue not found.");
                        // Remove old detail lines and their ledger rows
                        var oldDetails = await _oBMSDbContext.StockIssueDocDetails
                            .Where(d => d.IssueDocID == dto.ID).ToListAsync();
                        var oldLedgers = await _oBMSDbContext.StockLedgers
                            .Where(l => l.ReferenceType == "Stock Issue" && l.ReferenceID == dto.ID)
                            .ToListAsync();
                        _oBMSDbContext.StockIssueDocDetails.RemoveRange(oldDetails);
                        _oBMSDbContext.StockLedgers.RemoveRange(oldLedgers);
                        await _oBMSDbContext.SaveChangesAsync();
                    }
                    else
                    {
                        doc = new StockIssueDoc();
                        _oBMSDbContext.StockIssueDocs.Add(doc);
                    }

                    doc.IssueNo       = issueNo;
                    doc.IssueDate     = dto.IssueDate;
                    doc.WarehouseID   = dto.WarehouseID;
                    doc.IssueToType   = dto.IssueToType;
                    doc.IssueToID     = dto.IssueToID;
                    doc.IssueToName   = dto.IssueToName;
                    doc.Remarks       = dto.Remarks;
                    doc.IsActive      = true;
                    doc.LASTUPDATE    = DateTime.Now;
                    doc.LastUpdatedBy = dto.LastUpdatedBy;

                    await _oBMSDbContext.SaveChangesAsync(); // get doc.ID

                    foreach (var line in dto.Details)
                    {
                        var detail = new StockIssueDocDetail
                        {
                            IssueDocID    = doc.ID,
                            ItemID        = line.ItemID,
                            UOMID         = line.UOMID,
                            IssueQty      = line.IssueQty,
                            UnitCost      = line.UnitCost,
                            Remarks       = line.Remarks,
                            LASTUPDATE    = DateTime.Now,
                            LastUpdatedBy = line.LastUpdatedBy
                        };
                        _oBMSDbContext.StockIssueDocDetails.Add(detail);

                        // StockLedger ISSUE (Out)
                        _oBMSDbContext.StockLedgers.Add(new StockLedger
                        {
                            TransactionNo   = await GenerateStockLedgerNo(),
                            TransactionDate = dto.IssueDate,
                            TransactionType = "ISSUE",
                            WarehouseID     = dto.WarehouseID,
                            ItemID          = line.ItemID,
                            InQty           = 0,
                            OutQty          = line.IssueQty,
                            UnitCost        = line.UnitCost,
                            ReferenceType   = "Stock Issue",
                            ReferenceID     = doc.ID,
                            ReferenceNo     = issueNo,
                            Remarks         = dto.Remarks,
                            LASTUPDATE      = DateTime.Now,
                            LastUpdatedBy   = dto.LastUpdatedBy
                        });
                    }

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedRecord = doc;

                    // Part 7: low-stock check after issue reduces stock
                    if (_notif != null)
                    {
                        foreach (var line in dto.Details)
                            _ = _notif.CheckAndNotifyLowStock(dto.WarehouseID, line.ItemID, dto.LastUpdatedBy ?? "system");
                    }
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Stock Issue {issueNo} saved successfully.", savedRecord);
        }

        public async Task<(bool Success, string Message)> DeleteStockIssue(int id, string currentUser)
        {
            var doc = await _oBMSDbContext.StockIssueDocs.FindAsync(id);
            if (doc == null) return (false, "Stock issue not found.");

            // Block delete if any returns exist against this issue
            bool hasReturns = await _oBMSDbContext.StockIssueReturns
                .AnyAsync(r => r.OriginalIssueDocID == id && r.IsActive == true);
            if (hasReturns)
                return (false, "This stock issue cannot be deleted because one or more returns have been recorded against it.");

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var ledgers = await _oBMSDbContext.StockLedgers
                        .Where(l => l.ReferenceType == "Stock Issue" && l.ReferenceID == id)
                        .ToListAsync();
                    _oBMSDbContext.StockLedgers.RemoveRange(ledgers);

                    doc.IsActive      = false;
                    doc.LASTUPDATE    = DateTime.Now;
                    doc.LastUpdatedBy = currentUser;

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, "Stock issue deleted successfully. Stock has been restored.");
        }

        // ────────────────────────────────────────────────────────────────────────
        // B. STOCK ISSUE RETURN
        // ────────────────────────────────────────────────────────────────────────

        public async Task<List<StockIssueDocListDto>> GetIssuableDocsForReturn()
        {
            // Return issue docs that have at least one line with remaining returnable qty
            var docs = await (
                from sid in _oBMSDbContext.StockIssueDocs
                join wh in _oBMSDbContext.WarehouseMasters on sid.WarehouseID equals wh.ID
                where sid.IsActive == true
                orderby sid.IssueDate descending, sid.IssueNo descending
                select new StockIssueDocListDto
                {
                    ID            = sid.ID,
                    IssueNo       = sid.IssueNo,
                    IssueDate     = sid.IssueDate,
                    WarehouseID   = sid.WarehouseID,
                    WarehouseName = wh.Name,
                    IssueToType   = sid.IssueToType,
                    IssueToID     = sid.IssueToID,
                    IssueToName   = sid.IssueToName,
                    Remarks       = sid.Remarks,
                    LASTUPDATE    = sid.LASTUPDATE,
                    LastUpdatedBy = sid.LastUpdatedBy,
                    Details       = new List<StockIssueDocDetailListDto>()
                }
            ).ToListAsync();
            return docs;
        }

        public async Task<List<StockIssueDocDetailListDto>> GetIssueDetailsForReturn(int issueDocID)
        {
            return await BuildIssueDetailList(issueDocID);
        }

        public async Task<List<StockIssueReturnListDto>> GetStockIssueReturnList()
        {
            var returns = await (
                from sir in _oBMSDbContext.StockIssueReturns
                join sid in _oBMSDbContext.StockIssueDocs on sir.OriginalIssueDocID equals sid.ID
                join wh  in _oBMSDbContext.WarehouseMasters on sir.WarehouseID equals wh.ID
                where sir.IsActive == true
                orderby sir.ReturnDate descending, sir.ID descending
                select new StockIssueReturnListDto
                {
                    ID                 = sir.ID,
                    ReturnNo           = sir.ReturnNo,
                    ReturnDate         = sir.ReturnDate,
                    OriginalIssueDocID = sir.OriginalIssueDocID,
                    OriginalIssueNo    = sid.IssueNo,
                    WarehouseID        = sir.WarehouseID,
                    WarehouseName      = wh.Name,
                    IssueToName        = sid.IssueToName,
                    Remarks            = sir.Remarks,
                    LASTUPDATE         = sir.LASTUPDATE,
                    LastUpdatedBy      = sir.LastUpdatedBy,
                    Details            = new List<StockIssueReturnDetailListDto>()
                }
            ).ToListAsync();
            return returns;
        }

        public async Task<StockIssueReturnListDto?> GetStockIssueReturnByID(int id)
        {
            var ret = await (
                from sir in _oBMSDbContext.StockIssueReturns
                join sid in _oBMSDbContext.StockIssueDocs on sir.OriginalIssueDocID equals sid.ID
                join wh  in _oBMSDbContext.WarehouseMasters on sir.WarehouseID equals wh.ID
                where sir.ID == id && sir.IsActive == true
                select new StockIssueReturnListDto
                {
                    ID                 = sir.ID,
                    ReturnNo           = sir.ReturnNo,
                    ReturnDate         = sir.ReturnDate,
                    OriginalIssueDocID = sir.OriginalIssueDocID,
                    OriginalIssueNo    = sid.IssueNo,
                    WarehouseID        = sir.WarehouseID,
                    WarehouseName      = wh.Name,
                    IssueToName        = sid.IssueToName,
                    Remarks            = sir.Remarks,
                    LASTUPDATE         = sir.LASTUPDATE,
                    LastUpdatedBy      = sir.LastUpdatedBy,
                    Details            = new List<StockIssueReturnDetailListDto>()
                }
            ).FirstOrDefaultAsync();

            if (ret == null) return null;

            ret.Details = await (
                from d  in _oBMSDbContext.StockIssueReturnDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join um in _oBMSDbContext.UOMMasters on d.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where d.ReturnID == id
                select new StockIssueReturnDetailListDto
                {
                    ID                    = d.ID,
                    ReturnID              = d.ReturnID,
                    OriginalIssueDetailID = d.OriginalIssueDetailID,
                    ItemID                = d.ItemID,
                    ItemCode              = im.Code ?? "",
                    ItemName              = im.Name,
                    UOMName               = um != null ? um.Name : "",
                    ReturnQty             = d.ReturnQty,
                    UnitCost              = d.UnitCost,
                    TotalValue            = d.ReturnQty * d.UnitCost,
                    Remarks               = d.Remarks
                }
            ).ToListAsync();

            return ret;
        }

        public async Task<(bool Success, string Message, StockIssueReturn? Record)> SaveStockIssueReturn(
            StockIssueReturnRequestDto dto)
        {
            var issueDoc = await _oBMSDbContext.StockIssueDocs.FindAsync(dto.OriginalIssueDocID);
            if (issueDoc == null)
                return (false, "The original stock issue document was not found.", null);

            var warehouse = await _oBMSDbContext.WarehouseMasters.FindAsync(dto.WarehouseID);
            if (warehouse == null)
                return (false, "The selected warehouse is no longer available.", null);

            if (!dto.Details.Any())
                return (false, "Please add at least one item to the return.", null);

            // ── Validate each return line against remaining returnable qty ───────
            foreach (var line in dto.Details)
            {
                if (line.ReturnQty <= 0)
                    return (false, "Return quantity must be greater than zero.", null);

                var issueDetail = await _oBMSDbContext.StockIssueDocDetails.FindAsync(line.OriginalIssueDetailID);
                if (issueDetail == null)
                    return (false, "One or more referenced issue lines are no longer valid.", null);

                var alreadyReturned = await _oBMSDbContext.StockIssueReturnDetails
                    .Where(r => r.OriginalIssueDetailID == line.OriginalIssueDetailID)
                    .SumAsync(r => (decimal?)r.ReturnQty) ?? 0;

                var remainingQty = issueDetail.IssueQty - alreadyReturned;

                if (line.ReturnQty > remainingQty)
                {
                    var item = await _oBMSDbContext.ItemMasters.FindAsync(line.ItemID);
                    return (false,
                        $"Return quantity for '{item?.Name ?? "item"}' ({line.ReturnQty:N2}) exceeds the returnable quantity ({remainingQty:N2}).",
                        null);
                }
            }

            // ── Generate return number ──────────────────────────────────────────
            string returnNo = dto.ReturnNo ?? "";
            if (string.IsNullOrEmpty(returnNo))
            {
                var maxRow = await _oBMSDbContext.StockIssueReturns
                    .Where(x => x.ReturnNo.StartsWith("SIR"))
                    .OrderByDescending(x => x.ReturnNo)
                    .FirstOrDefaultAsync();
                returnNo = (maxRow == null || string.IsNullOrEmpty(maxRow.ReturnNo))
                    ? "SIR0001"
                    : int.TryParse(maxRow.ReturnNo.Substring(3), out int n)
                        ? "SIR" + (n + 1).ToString("D4") : "SIR0001";
            }

            StockIssueReturn? savedRecord = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var ret = new StockIssueReturn
                    {
                        ReturnNo           = returnNo,
                        ReturnDate         = dto.ReturnDate,
                        OriginalIssueDocID = dto.OriginalIssueDocID,
                        WarehouseID        = dto.WarehouseID,
                        Remarks            = dto.Remarks,
                        IsActive           = true,
                        LASTUPDATE         = DateTime.Now,
                        LastUpdatedBy      = dto.LastUpdatedBy
                    };
                    _oBMSDbContext.StockIssueReturns.Add(ret);
                    await _oBMSDbContext.SaveChangesAsync(); // get ret.ID

                    foreach (var line in dto.Details)
                    {
                        _oBMSDbContext.StockIssueReturnDetails.Add(new StockIssueReturnDetail
                        {
                            ReturnID              = ret.ID,
                            OriginalIssueDetailID = line.OriginalIssueDetailID,
                            ItemID                = line.ItemID,
                            UOMID                 = line.UOMID,
                            ReturnQty             = line.ReturnQty,
                            UnitCost              = line.UnitCost,
                            Remarks               = line.Remarks,
                            LASTUPDATE            = DateTime.Now,
                            LastUpdatedBy         = line.LastUpdatedBy
                        });

                        // StockLedger ISSUE_RETURN (In — restores stock)
                        _oBMSDbContext.StockLedgers.Add(new StockLedger
                        {
                            TransactionNo   = await GenerateStockLedgerNo(),
                            TransactionDate = dto.ReturnDate,
                            TransactionType = "ISSUE_RETURN",
                            WarehouseID     = dto.WarehouseID,
                            ItemID          = line.ItemID,
                            InQty           = line.ReturnQty,
                            OutQty          = 0,
                            UnitCost        = line.UnitCost,
                            ReferenceType   = "Stock Issue Return",
                            ReferenceID     = ret.ID,
                            ReferenceNo     = returnNo,
                            Remarks         = dto.Remarks,
                            LASTUPDATE      = DateTime.Now,
                            LastUpdatedBy   = dto.LastUpdatedBy
                        });
                    }

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedRecord = ret;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Stock Issue Return {returnNo} saved successfully. Stock has been updated.", savedRecord);
        }

        // ────────────────────────────────────────────────────────────────────────
        // C. STOCK TRANSFER
        // ────────────────────────────────────────────────────────────────────────

        public async Task<List<StockTransferListDto>> GetStockTransferList()
        {
            var list = await (
                from st in _oBMSDbContext.StockTransfers
                join sw in _oBMSDbContext.WarehouseMasters on st.SourceWarehouseID equals sw.ID
                join dw in _oBMSDbContext.WarehouseMasters on st.DestWarehouseID   equals dw.ID
                where st.IsActive == true
                orderby st.TransferDate descending, st.ID descending
                select new StockTransferListDto
                {
                    ID                   = st.ID,
                    TransferNo           = st.TransferNo,
                    TransferDate         = st.TransferDate,
                    SourceWarehouseID    = st.SourceWarehouseID,
                    SourceWarehouseName  = sw.Name,
                    DestWarehouseID      = st.DestWarehouseID,
                    DestWarehouseName    = dw.Name,
                    Remarks              = st.Remarks,
                    LASTUPDATE           = st.LASTUPDATE,
                    LastUpdatedBy        = st.LastUpdatedBy,
                    Details              = new List<StockTransferDetailListDto>()
                }
            ).ToListAsync();
            return list;
        }

        public async Task<StockTransferListDto?> GetStockTransferByID(int id)
        {
            var transfer = await (
                from st in _oBMSDbContext.StockTransfers
                join sw in _oBMSDbContext.WarehouseMasters on st.SourceWarehouseID equals sw.ID
                join dw in _oBMSDbContext.WarehouseMasters on st.DestWarehouseID   equals dw.ID
                where st.ID == id && st.IsActive == true
                select new StockTransferListDto
                {
                    ID                  = st.ID,
                    TransferNo          = st.TransferNo,
                    TransferDate        = st.TransferDate,
                    SourceWarehouseID   = st.SourceWarehouseID,
                    SourceWarehouseName = sw.Name,
                    DestWarehouseID     = st.DestWarehouseID,
                    DestWarehouseName   = dw.Name,
                    Remarks             = st.Remarks,
                    LASTUPDATE          = st.LASTUPDATE,
                    LastUpdatedBy       = st.LastUpdatedBy,
                    Details             = new List<StockTransferDetailListDto>()
                }
            ).FirstOrDefaultAsync();

            if (transfer == null) return null;

            transfer.Details = await (
                from d  in _oBMSDbContext.StockTransferDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join um in _oBMSDbContext.UOMMasters on d.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where d.TransferID == id
                select new StockTransferDetailListDto
                {
                    ID          = d.ID,
                    TransferID  = d.TransferID,
                    ItemID      = d.ItemID,
                    ItemCode    = im.Code ?? "",
                    ItemName    = im.Name,
                    UOMName     = um != null ? um.Name : "",
                    TransferQty = d.TransferQty,
                    UnitCost    = d.UnitCost,
                    TotalValue  = d.TransferQty * d.UnitCost,
                    Remarks     = d.Remarks
                }
            ).ToListAsync();

            return transfer;
        }

        public async Task<(bool Success, string Message, StockTransfer? Record)> SaveStockTransfer(
            StockTransferRequestDto dto)
        {
            if (dto.SourceWarehouseID == dto.DestWarehouseID)
                return (false, "Source and destination warehouses must be different.", null);

            var srcWh = await _oBMSDbContext.WarehouseMasters.FindAsync(dto.SourceWarehouseID);
            var dstWh = await _oBMSDbContext.WarehouseMasters.FindAsync(dto.DestWarehouseID);
            if (srcWh == null) return (false, "Source warehouse not found.", null);
            if (dstWh == null) return (false, "Destination warehouse not found.", null);

            if (!dto.Details.Any())
                return (false, "Please add at least one item to the transfer.", null);

            // ── Stock availability check per line ───────────────────────────────
            foreach (var line in dto.Details)
            {
                if (line.TransferQty <= 0)
                    return (false, "Transfer quantity must be greater than zero.", null);

                var available = await GetCurrentStockInternal(dto.SourceWarehouseID, line.ItemID);
                if (line.TransferQty > available)
                {
                    var item = await _oBMSDbContext.ItemMasters.FindAsync(line.ItemID);
                    return (false,
                        $"Insufficient stock for '{item?.Name ?? $"Item #{line.ItemID}"}' in {srcWh.Name}. Available quantity is {available:N2}.",
                        null);
                }
            }

            // ── Generate transfer number ────────────────────────────────────────
            string transferNo = dto.TransferNo ?? "";
            if (string.IsNullOrEmpty(transferNo))
            {
                var maxRow = await _oBMSDbContext.StockTransfers
                    .Where(x => x.TransferNo.StartsWith("TRF"))
                    .OrderByDescending(x => x.TransferNo)
                    .FirstOrDefaultAsync();
                transferNo = (maxRow == null || string.IsNullOrEmpty(maxRow.TransferNo))
                    ? "TRF0001"
                    : int.TryParse(maxRow.TransferNo.Substring(3), out int n)
                        ? "TRF" + (n + 1).ToString("D4") : "TRF0001";
            }

            StockTransfer? savedRecord = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    StockTransfer transfer;
                    if (dto.ID > 0)
                    {
                        transfer = await _oBMSDbContext.StockTransfers.FindAsync(dto.ID)
                                   ?? throw new InvalidOperationException("Stock transfer not found.");
                        var oldDetails = await _oBMSDbContext.StockTransferDetails
                            .Where(d => d.TransferID == dto.ID).ToListAsync();
                        var oldLedgers = await _oBMSDbContext.StockLedgers
                            .Where(l => (l.ReferenceType == "Stock Transfer OUT" || l.ReferenceType == "Stock Transfer IN")
                                     && l.ReferenceID == dto.ID)
                            .ToListAsync();
                        _oBMSDbContext.StockTransferDetails.RemoveRange(oldDetails);
                        _oBMSDbContext.StockLedgers.RemoveRange(oldLedgers);
                        await _oBMSDbContext.SaveChangesAsync();
                    }
                    else
                    {
                        transfer = new StockTransfer();
                        _oBMSDbContext.StockTransfers.Add(transfer);
                    }

                    transfer.TransferNo        = transferNo;
                    transfer.TransferDate      = dto.TransferDate;
                    transfer.SourceWarehouseID = dto.SourceWarehouseID;
                    transfer.DestWarehouseID   = dto.DestWarehouseID;
                    transfer.Remarks           = dto.Remarks;
                    transfer.IsActive          = true;
                    transfer.LASTUPDATE        = DateTime.Now;
                    transfer.LastUpdatedBy     = dto.LastUpdatedBy;

                    await _oBMSDbContext.SaveChangesAsync(); // get transfer.ID

                    foreach (var line in dto.Details)
                    {
                        _oBMSDbContext.StockTransferDetails.Add(new StockTransferDetail
                        {
                            TransferID    = transfer.ID,
                            ItemID        = line.ItemID,
                            UOMID         = line.UOMID,
                            TransferQty   = line.TransferQty,
                            UnitCost      = line.UnitCost,
                            Remarks       = line.Remarks,
                            LASTUPDATE    = DateTime.Now,
                            LastUpdatedBy = line.LastUpdatedBy
                        });

                        // StockLedger TRANSFER_OUT (from source)
                        _oBMSDbContext.StockLedgers.Add(new StockLedger
                        {
                            TransactionNo   = await GenerateStockLedgerNo(),
                            TransactionDate = dto.TransferDate,
                            TransactionType = "TRANSFER_OUT",
                            WarehouseID     = dto.SourceWarehouseID,
                            ItemID          = line.ItemID,
                            InQty           = 0,
                            OutQty          = line.TransferQty,
                            UnitCost        = line.UnitCost,
                            ReferenceType   = "Stock Transfer OUT",
                            ReferenceID     = transfer.ID,
                            ReferenceNo     = transferNo,
                            Remarks         = $"Transfer to {dstWh.Name}. {dto.Remarks}".Trim('.', ' '),
                            LASTUPDATE      = DateTime.Now,
                            LastUpdatedBy   = dto.LastUpdatedBy
                        });

                        // StockLedger TRANSFER_IN (to destination)
                        _oBMSDbContext.StockLedgers.Add(new StockLedger
                        {
                            TransactionNo   = await GenerateStockLedgerNo(),
                            TransactionDate = dto.TransferDate,
                            TransactionType = "TRANSFER_IN",
                            WarehouseID     = dto.DestWarehouseID,
                            ItemID          = line.ItemID,
                            InQty           = line.TransferQty,
                            OutQty          = 0,
                            UnitCost        = line.UnitCost,
                            ReferenceType   = "Stock Transfer IN",
                            ReferenceID     = transfer.ID,
                            ReferenceNo     = transferNo,
                            Remarks         = $"Transfer from {srcWh.Name}. {dto.Remarks}".Trim('.', ' '),
                            LASTUPDATE      = DateTime.Now,
                            LastUpdatedBy   = dto.LastUpdatedBy
                        });
                    }

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedRecord = transfer;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Stock Transfer {transferNo} saved successfully. Stock has been updated in both warehouses.", savedRecord);
        }

        public async Task<(bool Success, string Message)> DeleteStockTransfer(int id, string currentUser)
        {
            var transfer = await _oBMSDbContext.StockTransfers.FindAsync(id);
            if (transfer == null) return (false, "Stock transfer not found.");

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var ledgers = await _oBMSDbContext.StockLedgers
                        .Where(l => (l.ReferenceType == "Stock Transfer OUT" || l.ReferenceType == "Stock Transfer IN")
                                 && l.ReferenceID == id)
                        .ToListAsync();
                    _oBMSDbContext.StockLedgers.RemoveRange(ledgers);

                    transfer.IsActive      = false;
                    transfer.LASTUPDATE    = DateTime.Now;
                    transfer.LastUpdatedBy = currentUser;

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, "Stock transfer cancelled successfully. Stock has been restored.");
        }

        // ────────────────────────────────────────────────────────────────────────
        // D. STOCK ADJUSTMENT
        // ────────────────────────────────────────────────────────────────────────

        public async Task<List<StockAdjustmentListDto>> GetStockAdjustmentList()
        {
            var list = await (
                from sa in _oBMSDbContext.StockAdjustments
                join wh in _oBMSDbContext.WarehouseMasters   on sa.WarehouseID equals wh.ID
                join im in _oBMSDbContext.ItemMasters         on sa.ItemID       equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID   equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on im.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where sa.IsActive == true
                orderby sa.AdjustmentDate descending, sa.ID descending
                select new StockAdjustmentListDto
                {
                    ID             = sa.ID,
                    AdjustmentNo   = sa.AdjustmentNo,
                    AdjustmentDate = sa.AdjustmentDate,
                    WarehouseID    = sa.WarehouseID,
                    WarehouseName  = wh.Name,
                    ItemID         = sa.ItemID,
                    ItemCode       = im.Code ?? "",
                    ItemName       = im.Name,
                    CategoryName   = ic != null ? ic.Name : "",
                    UOMName        = um != null ? um.Name : "",
                    AdjustmentType = sa.AdjustmentType,
                    AdjustmentQty  = sa.AdjustmentQty,
                    UnitCost       = sa.UnitCost,
                    TotalValue     = sa.AdjustmentQty * sa.UnitCost,
                    Reason         = sa.Reason,
                    Remarks        = sa.Remarks,
                    LASTUPDATE     = sa.LASTUPDATE,
                    LastUpdatedBy  = sa.LastUpdatedBy,
                    // Approval workflow fields
                    Status         = sa.Status,
                    SubmittedBy    = sa.SubmittedBy,
                    SubmittedDate  = sa.SubmittedDate,
                    ApprovedBy     = sa.ApprovedBy,
                    ApprovedDate   = sa.ApprovedDate,
                    ApprovalRemarks = sa.ApprovalRemarks
                }
            ).ToListAsync();
            return list;
        }

        public async Task<StockAdjustmentListDto?> GetStockAdjustmentByID(int id)
        {
            return await (
                from sa in _oBMSDbContext.StockAdjustments
                join wh in _oBMSDbContext.WarehouseMasters   on sa.WarehouseID equals wh.ID
                join im in _oBMSDbContext.ItemMasters         on sa.ItemID       equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID   equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on im.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where sa.ID == id && sa.IsActive == true
                select new StockAdjustmentListDto
                {
                    ID             = sa.ID,
                    AdjustmentNo   = sa.AdjustmentNo,
                    AdjustmentDate = sa.AdjustmentDate,
                    WarehouseID    = sa.WarehouseID,
                    WarehouseName  = wh.Name,
                    ItemID         = sa.ItemID,
                    ItemCode       = im.Code ?? "",
                    ItemName       = im.Name,
                    CategoryName   = ic != null ? ic.Name : "",
                    UOMName        = um != null ? um.Name : "",
                    AdjustmentType = sa.AdjustmentType,
                    AdjustmentQty  = sa.AdjustmentQty,
                    UnitCost       = sa.UnitCost,
                    TotalValue     = sa.AdjustmentQty * sa.UnitCost,
                    Reason         = sa.Reason,
                    Remarks        = sa.Remarks,
                    LASTUPDATE     = sa.LASTUPDATE,
                    LastUpdatedBy  = sa.LastUpdatedBy,
                    // Approval workflow fields
                    Status         = sa.Status,
                    SubmittedBy    = sa.SubmittedBy,
                    SubmittedDate  = sa.SubmittedDate,
                    ApprovedBy     = sa.ApprovedBy,
                    ApprovedDate   = sa.ApprovedDate,
                    ApprovalRemarks = sa.ApprovalRemarks
                }
            ).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Saves a Stock Adjustment as Draft (or updates an existing Draft).
        /// Stock ledger is NOT updated at save time — it is applied only when the
        /// adjustment is Approved (via ApproveSA).
        /// Only Draft adjustments can be edited.
        /// </summary>
        public async Task<(bool Success, string Message, StockAdjustment? Record)> SaveStockAdjustment(
            StockAdjustmentRequestDto dto)
        {
            if (dto.AdjustmentQty <= 0)
                return (false, "Adjustment quantity must be greater than zero.", null);

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return (false, "Please enter a reason for the adjustment.", null);

            if (dto.AdjustmentType != "PLUS" && dto.AdjustmentType != "MINUS")
                return (false, "Adjustment type must be PLUS or MINUS.", null);

            var warehouse = await _oBMSDbContext.WarehouseMasters.FindAsync(dto.WarehouseID);
            if (warehouse == null) return (false, "Warehouse not found.", null);

            var item = await _oBMSDbContext.ItemMasters.FindAsync(dto.ItemID);
            if (item == null) return (false, "Item not found.", null);

            // For existing records, only Draft status can be edited
            if (dto.ID > 0)
            {
                var existing = await _oBMSDbContext.StockAdjustments.FindAsync(dto.ID);
                if (existing != null && existing.Status != "Draft")
                    return (false, $"Only Draft adjustments can be edited. This adjustment is currently {existing.Status}.", null);
            }

            // ── Generate adjustment number ──────────────────────────────────────
            string adjNo = dto.AdjustmentNo ?? "";
            if (string.IsNullOrEmpty(adjNo))
            {
                var maxRow = await _oBMSDbContext.StockAdjustments
                    .Where(x => x.AdjustmentNo.StartsWith("ADJ"))
                    .OrderByDescending(x => x.AdjustmentNo)
                    .FirstOrDefaultAsync();
                adjNo = (maxRow == null || string.IsNullOrEmpty(maxRow.AdjustmentNo))
                    ? "ADJ0001"
                    : int.TryParse(maxRow.AdjustmentNo.Substring(3), out int n)
                        ? "ADJ" + (n + 1).ToString("D4") : "ADJ0001";
            }

            StockAdjustment? savedRecord = null;
            StockAdjustment adj;
            if (dto.ID > 0)
            {
                adj = await _oBMSDbContext.StockAdjustments.FindAsync(dto.ID)
                      ?? throw new InvalidOperationException("Adjustment not found.");
            }
            else
            {
                adj = new StockAdjustment();
                _oBMSDbContext.StockAdjustments.Add(adj);
            }

            adj.AdjustmentNo   = adjNo;
            adj.AdjustmentDate = dto.AdjustmentDate;
            adj.WarehouseID    = dto.WarehouseID;
            adj.ItemID         = dto.ItemID;
            adj.AdjustmentType = dto.AdjustmentType;
            adj.AdjustmentQty  = dto.AdjustmentQty;
            adj.UnitCost       = dto.UnitCost;
            adj.Reason         = dto.Reason;
            adj.Remarks        = dto.Remarks;
            adj.IsActive       = true;
            adj.Status         = "Draft";
            adj.LASTUPDATE     = DateTime.Now;
            adj.LastUpdatedBy  = dto.LastUpdatedBy;

            await _oBMSDbContext.SaveChangesAsync();
            savedRecord = adj;

            return (true, $"Stock Adjustment {adjNo} saved as Draft. Submit for approval when ready.", savedRecord);
        }

        /// <summary>
        /// Submits a Draft Stock Adjustment for approval.
        /// If ApprovalRequired = false OR no routing configured → auto-approves immediately
        /// (applies the stock ledger entry).
        /// Otherwise → sets Status = Submitted, notifies the configured approver.
        /// </summary>
        public async Task<(bool Success, string Message)> SubmitSA(int id, string currentUser)
        {
            var adj = await _oBMSDbContext.StockAdjustments.FindAsync(id);
            if (adj == null) return (false, "Stock adjustment not found.");
            if (adj.Status != "Draft")
                return (false, $"Only a Draft adjustment can be submitted. This adjustment is currently {adj.Status}.");

            if (_approval != null)
            {
                bool approvalRequired = await _approval.IsApprovalRequired("STOCK_ADJUSTMENT");
                if (!approvalRequired)
                {
                    // Approval off → auto-approve
                    await ApplySALedger(adj, currentUser);
                    adj.Status        = "Approved";
                    adj.ApprovedBy    = currentUser;
                    adj.ApprovedDate  = DateTime.Now;
                    adj.SubmittedBy   = currentUser;
                    adj.SubmittedDate = DateTime.Now;
                    adj.LASTUPDATE    = DateTime.Now;
                    adj.LastUpdatedBy = currentUser;
                    await _oBMSDbContext.SaveChangesAsync();
                    return (true, $"Stock Adjustment {adj.AdjustmentNo} has been approved. Stock has been updated.");
                }

                var approverUser = await _approval.GetApproverForUser(currentUser, "STOCK_ADJUSTMENT");
                if (string.IsNullOrEmpty(approverUser))
                {
                    // No routing → auto-approve
                    await ApplySALedger(adj, currentUser);
                    adj.Status        = "Approved";
                    adj.ApprovedBy    = currentUser;
                    adj.ApprovedDate  = DateTime.Now;
                    adj.SubmittedBy   = currentUser;
                    adj.SubmittedDate = DateTime.Now;
                    adj.LASTUPDATE    = DateTime.Now;
                    adj.LastUpdatedBy = currentUser;
                    await _oBMSDbContext.SaveChangesAsync();
                    return (true, $"Stock Adjustment {adj.AdjustmentNo} has been approved. Stock has been updated.");
                }

                string prev       = adj.Status;
                adj.Status        = "Submitted";
                adj.SubmittedBy   = currentUser;
                adj.SubmittedDate = DateTime.Now;
                adj.LASTUPDATE    = DateTime.Now;
                adj.LastUpdatedBy = currentUser;
                await _oBMSDbContext.SaveChangesAsync();

                await _approval.RecordHistory("STOCK_ADJUSTMENT", adj.ID, adj.AdjustmentNo,
                    "Submitted", currentUser, prev, "Submitted", null);

                if (_notif != null)
                    await _notif.SendApprovalNotification(
                        "PENDING_APPROVAL", "STOCK_ADJUSTMENT", adj.AdjustmentNo, adj.ID,
                        approverUser, currentUser, null);
            }
            else
            {
                adj.Status        = "Submitted";
                adj.SubmittedBy   = currentUser;
                adj.SubmittedDate = DateTime.Now;
                adj.LASTUPDATE    = DateTime.Now;
                adj.LastUpdatedBy = currentUser;
                await _oBMSDbContext.SaveChangesAsync();
            }

            return (true, $"Stock Adjustment {adj.AdjustmentNo} has been submitted for approval.");
        }

        /// <summary>
        /// Approves a Submitted Stock Adjustment.
        /// Applies the stock ledger entry ONLY on approval.
        /// Enforces: JWT identity, self-approval block, IsAuthorizedApprover.
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveSA(int id, SAApprovalDto dto)
        {
            var adj = await _oBMSDbContext.StockAdjustments.FindAsync(id);
            if (adj == null) return (false, "Stock adjustment not found.");
            if (adj.Status != "Submitted")
                return (false, "Only a Submitted adjustment can be approved.");

            var submitter = adj.SubmittedBy ?? adj.LastUpdatedBy ?? string.Empty;
            bool isSuperAdmin   = dto.IsSuperAdmin;
            bool isSelfApproval = !isSuperAdmin &&
                                  !string.IsNullOrWhiteSpace(submitter) &&
                                  string.Equals(dto.ApprovedBy, submitter, StringComparison.OrdinalIgnoreCase);
            if (isSelfApproval)
                return (false, "You cannot approve a document that you submitted yourself.");

            if (!isSuperAdmin && _approval != null)
            {
                var authorized = await _approval.IsAuthorizedApprover(
                    dto.ApprovedBy, submitter, "STOCK_ADJUSTMENT");
                if (!authorized)
                    return (false, "You are not authorized to approve this adjustment.");
            }

            // Apply the stock ledger entry now that it is approved
            await ApplySALedger(adj, dto.ApprovedBy);

            string prev       = adj.Status;
            adj.Status        = "Approved";
            adj.ApprovedBy    = dto.ApprovedBy;
            adj.ApprovedDate  = DateTime.Now;
            adj.ApprovalRemarks = dto.ApprovalRemarks;
            adj.LASTUPDATE    = DateTime.Now;
            adj.LastUpdatedBy = dto.ApprovedBy;
            await _oBMSDbContext.SaveChangesAsync();

            if (_approval != null)
                await _approval.RecordHistory("STOCK_ADJUSTMENT", adj.ID, adj.AdjustmentNo,
                    "Approved", dto.ApprovedBy, prev, "Approved", dto.ApprovalRemarks);
            if (_notif != null && !string.IsNullOrEmpty(adj.SubmittedBy))
                await _notif.SendApprovalNotification(
                    "APPROVED", "STOCK_ADJUSTMENT", adj.AdjustmentNo, adj.ID,
                    adj.SubmittedBy, dto.ApprovedBy, dto.ApprovalRemarks);

            // Notify Purchase department — stock level has changed, Purchase team should be aware
            if (_notif != null)
                await _notif.SendApprovalNotificationToDepartment(
                    "APPROVED", "STOCK_ADJUSTMENT", adj.AdjustmentNo, adj.ID,
                    "Purchase", dto.ApprovedBy, dto.ApprovalRemarks,
                    excludeUsers: new[] { adj.SubmittedBy ?? string.Empty });

            return (true, $"Stock Adjustment {adj.AdjustmentNo} has been approved. Stock has been updated.");
        }

        /// <summary>
        /// Rejects a Submitted Stock Adjustment.
        /// No stock ledger entry is made. Requester is notified.
        /// </summary>
        public async Task<(bool Success, string Message)> RejectSA(int id, SAApprovalDto dto)
        {
            var adj = await _oBMSDbContext.StockAdjustments.FindAsync(id);
            if (adj == null) return (false, "Stock adjustment not found.");
            if (adj.Status != "Submitted")
                return (false, "Only a Submitted adjustment can be rejected.");
            if (string.IsNullOrWhiteSpace(dto.ApprovalRemarks))
                return (false, "Please enter a reason for rejection so the requester knows what to correct.");

            var submitter = adj.SubmittedBy ?? adj.LastUpdatedBy ?? string.Empty;
            bool isSuperAdmin    = dto.IsSuperAdmin;
            bool isSelfRejection = !isSuperAdmin &&
                                   !string.IsNullOrWhiteSpace(submitter) &&
                                   string.Equals(dto.ApprovedBy, submitter, StringComparison.OrdinalIgnoreCase);
            if (isSelfRejection)
                return (false, "You cannot reject a document that you submitted yourself.");

            if (!isSuperAdmin && _approval != null)
            {
                var authorized = await _approval.IsAuthorizedApprover(
                    dto.ApprovedBy, submitter, "STOCK_ADJUSTMENT");
                if (!authorized)
                    return (false, "You are not authorized to reject this adjustment.");
            }

            string prev       = adj.Status;
            adj.Status        = "Rejected";
            adj.ApprovedBy    = dto.ApprovedBy;
            adj.ApprovedDate  = DateTime.Now;
            adj.ApprovalRemarks = dto.ApprovalRemarks;
            adj.LASTUPDATE    = DateTime.Now;
            adj.LastUpdatedBy = dto.ApprovedBy;
            await _oBMSDbContext.SaveChangesAsync();

            if (_approval != null)
                await _approval.RecordHistory("STOCK_ADJUSTMENT", adj.ID, adj.AdjustmentNo,
                    "Rejected", dto.ApprovedBy, prev, "Rejected", dto.ApprovalRemarks);
            if (_notif != null && !string.IsNullOrEmpty(adj.SubmittedBy))
                await _notif.SendApprovalNotification(
                    "REJECTED", "STOCK_ADJUSTMENT", adj.AdjustmentNo, adj.ID,
                    adj.SubmittedBy, dto.ApprovedBy, dto.ApprovalRemarks);

            return (true, $"Stock Adjustment {adj.AdjustmentNo} has been rejected.");
        }

        /// <summary>
        /// Internal helper: write the stock ledger row for an approved adjustment.
        /// Separated so it can be called from both SubmitSA (auto-approve) and ApproveSA.
        /// </summary>
        private async Task ApplySALedger(StockAdjustment adj, string approvedBy)
        {
            bool isPlus = adj.AdjustmentType == "PLUS";
            _oBMSDbContext.StockLedgers.Add(new StockLedger
            {
                TransactionNo   = await GenerateStockLedgerNo(),
                TransactionDate = adj.AdjustmentDate,
                TransactionType = isPlus ? "ADJ_PLUS" : "ADJ_MINUS",
                WarehouseID     = adj.WarehouseID,
                ItemID          = adj.ItemID,
                InQty           = isPlus ? adj.AdjustmentQty : 0,
                OutQty          = isPlus ? 0 : adj.AdjustmentQty,
                UnitCost        = adj.UnitCost,
                ReferenceType   = "Stock Adjustment",
                ReferenceID     = adj.ID,
                ReferenceNo     = adj.AdjustmentNo,
                Remarks         = adj.Reason,
                LASTUPDATE      = DateTime.Now,
                LastUpdatedBy   = approvedBy
            });
            await _oBMSDbContext.SaveChangesAsync();
        }

        public async Task<(bool Success, string Message)> DeleteStockAdjustment(int id, string currentUser)
        {
            var adj = await _oBMSDbContext.StockAdjustments.FindAsync(id);
            if (adj == null) return (false, "Stock adjustment not found.");
            if (adj.Status != "Draft")
                return (false, $"Only Draft adjustments can be deleted. This adjustment is currently {adj.Status}.");

            adj.IsActive      = false;
            adj.LASTUPDATE    = DateTime.Now;
            adj.LastUpdatedBy = currentUser;
            await _oBMSDbContext.SaveChangesAsync();

            return (true, "Stock adjustment deleted successfully.");
        }

        // ────────────────────────────────────────────────────────────────────────
        // E. DAMAGE / LOSS
        // ────────────────────────────────────────────────────────────────────────

        public async Task<List<StockDamageLossListDto>> GetStockDamageLossList()
        {
            var list = await (
                from dl in _oBMSDbContext.StockDamageLosses
                join wh in _oBMSDbContext.WarehouseMasters   on dl.WarehouseID equals wh.ID
                join im in _oBMSDbContext.ItemMasters         on dl.ItemID       equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID   equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on im.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where dl.IsActive == true
                orderby dl.RecordDate descending, dl.ID descending
                select new StockDamageLossListDto
                {
                    ID            = dl.ID,
                    RecordNo      = dl.RecordNo,
                    RecordDate    = dl.RecordDate,
                    WarehouseID   = dl.WarehouseID,
                    WarehouseName = wh.Name,
                    ItemID        = dl.ItemID,
                    ItemCode      = im.Code ?? "",
                    ItemName      = im.Name,
                    CategoryName  = ic != null ? ic.Name : "",
                    UOMName       = um != null ? um.Name : "",
                    LossType      = dl.LossType,
                    Quantity      = dl.Quantity,
                    UnitCost      = dl.UnitCost,
                    TotalValue    = dl.Quantity * dl.UnitCost,
                    Reason        = dl.Reason,
                    Remarks       = dl.Remarks,
                    LASTUPDATE    = dl.LASTUPDATE,
                    LastUpdatedBy = dl.LastUpdatedBy
                }
            ).ToListAsync();
            return list;
        }

        public async Task<StockDamageLossListDto?> GetStockDamageLossByID(int id)
        {
            return await (
                from dl in _oBMSDbContext.StockDamageLosses
                join wh in _oBMSDbContext.WarehouseMasters   on dl.WarehouseID equals wh.ID
                join im in _oBMSDbContext.ItemMasters         on dl.ItemID       equals im.ID
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID   equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on im.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where dl.ID == id && dl.IsActive == true
                select new StockDamageLossListDto
                {
                    ID            = dl.ID,
                    RecordNo      = dl.RecordNo,
                    RecordDate    = dl.RecordDate,
                    WarehouseID   = dl.WarehouseID,
                    WarehouseName = wh.Name,
                    ItemID        = dl.ItemID,
                    ItemCode      = im.Code ?? "",
                    ItemName      = im.Name,
                    CategoryName  = ic != null ? ic.Name : "",
                    UOMName       = um != null ? um.Name : "",
                    LossType      = dl.LossType,
                    Quantity      = dl.Quantity,
                    UnitCost      = dl.UnitCost,
                    TotalValue    = dl.Quantity * dl.UnitCost,
                    Reason        = dl.Reason,
                    Remarks       = dl.Remarks,
                    LASTUPDATE    = dl.LASTUPDATE,
                    LastUpdatedBy = dl.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, StockDamageLoss? Record)> SaveStockDamageLoss(
            StockDamageLossRequestDto dto)
        {
            if (dto.Quantity <= 0)
                return (false, "Quantity must be greater than zero.", null);

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return (false, "Please enter a reason for the damage/loss record.", null);

            var warehouse = await _oBMSDbContext.WarehouseMasters.FindAsync(dto.WarehouseID);
            if (warehouse == null) return (false, "Warehouse not found.", null);

            var item = await _oBMSDbContext.ItemMasters.FindAsync(dto.ItemID);
            if (item == null) return (false, "Item not found.", null);

            // ── Stock availability check ────────────────────────────────────────
            var available = await GetCurrentStockInternal(dto.WarehouseID, dto.ItemID);
            if (dto.Quantity > available)
                return (false,
                    $"Insufficient stock. Available quantity for '{item.Name}' in {warehouse.Name} is {available:N2}.",
                    null);

            // ── Generate record number ──────────────────────────────────────────
            string recordNo = dto.RecordNo ?? "";
            if (string.IsNullOrEmpty(recordNo))
            {
                var maxRow = await _oBMSDbContext.StockDamageLosses
                    .Where(x => x.RecordNo.StartsWith("DL"))
                    .OrderByDescending(x => x.RecordNo)
                    .FirstOrDefaultAsync();
                recordNo = (maxRow == null || string.IsNullOrEmpty(maxRow.RecordNo))
                    ? "DL0001"
                    : int.TryParse(maxRow.RecordNo.Substring(2), out int n)
                        ? "DL" + (n + 1).ToString("D4") : "DL0001";
            }

            StockDamageLoss? savedRecord = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    StockDamageLoss dl;
                    if (dto.ID > 0)
                    {
                        dl = await _oBMSDbContext.StockDamageLosses.FindAsync(dto.ID)
                             ?? throw new InvalidOperationException("Record not found.");
                        var oldLedger = await _oBMSDbContext.StockLedgers
                            .Where(l => l.ReferenceType == "Damage/Loss" && l.ReferenceID == dto.ID)
                            .ToListAsync();
                        _oBMSDbContext.StockLedgers.RemoveRange(oldLedger);
                        await _oBMSDbContext.SaveChangesAsync();
                    }
                    else
                    {
                        dl = new StockDamageLoss();
                        _oBMSDbContext.StockDamageLosses.Add(dl);
                    }

                    dl.RecordNo      = recordNo;
                    dl.RecordDate    = dto.RecordDate;
                    dl.WarehouseID   = dto.WarehouseID;
                    dl.ItemID        = dto.ItemID;
                    dl.LossType      = dto.LossType;
                    dl.Quantity      = dto.Quantity;
                    dl.UnitCost      = dto.UnitCost;
                    dl.Reason        = dto.Reason;
                    dl.Remarks       = dto.Remarks;
                    dl.IsActive      = true;
                    dl.LASTUPDATE    = DateTime.Now;
                    dl.LastUpdatedBy = dto.LastUpdatedBy;

                    await _oBMSDbContext.SaveChangesAsync(); // get dl.ID

                    // StockLedger DAMAGE_LOSS (Out)
                    _oBMSDbContext.StockLedgers.Add(new StockLedger
                    {
                        TransactionNo   = await GenerateStockLedgerNo(),
                        TransactionDate = dto.RecordDate,
                        TransactionType = "DAMAGE_LOSS",
                        WarehouseID     = dto.WarehouseID,
                        ItemID          = dto.ItemID,
                        InQty           = 0,
                        OutQty          = dto.Quantity,
                        UnitCost        = dto.UnitCost,
                        ReferenceType   = "Damage/Loss",
                        ReferenceID     = dl.ID,
                        ReferenceNo     = recordNo,
                        Remarks         = $"{dto.LossType}: {dto.Reason}",
                        LASTUPDATE      = DateTime.Now,
                        LastUpdatedBy   = dto.LastUpdatedBy
                    });

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedRecord = dl;

                    // Part 7: low-stock check after damage/loss reduces stock
                    if (_notif != null)
                        _ = _notif.CheckAndNotifyLowStock(dto.WarehouseID, dto.ItemID, dto.LastUpdatedBy ?? "system");
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Damage/Loss record {recordNo} saved successfully. Stock has been updated.", savedRecord);
        }

        public async Task<(bool Success, string Message)> DeleteStockDamageLoss(int id, string currentUser)
        {
            var dl = await _oBMSDbContext.StockDamageLosses.FindAsync(id);
            if (dl == null) return (false, "Damage/loss record not found.");

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var ledgers = await _oBMSDbContext.StockLedgers
                        .Where(l => l.ReferenceType == "Damage/Loss" && l.ReferenceID == id)
                        .ToListAsync();
                    _oBMSDbContext.StockLedgers.RemoveRange(ledgers);

                    dl.IsActive      = false;
                    dl.LASTUPDATE    = DateTime.Now;
                    dl.LastUpdatedBy = currentUser;

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, "Damage/loss record deleted successfully. Stock balance has been restored.");
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 5 — EMPLOYEE/GUARD ITEM TRACKING + REPORTS
        // ════════════════════════════════════════════════════════════════════════

        // ────────────────────────────────────────────────────────────────────────
        // A. EMPLOYEE / GUARD ITEM HOLDING
        // ────────────────────────────────────────────────────────────────────────

        public async Task<List<EmployeeHoldingSummaryDto>> GetEmployeeHoldingSummaryList()
        {
            // Build per-employee, per-item net holdings from issue + return tables
            var holdings = await BuildRawHoldings(null, null);

            // Aggregate to one row per employee
            var summary = holdings
                .GroupBy(h => new { h.EmployeeID, h.EmployeeName, h.IssueToType })
                .Select(g => new EmployeeHoldingSummaryDto
                {
                    EmployeeID   = g.Key.EmployeeID,
                    EmployeeName = g.Key.EmployeeName,
                    IssueToType  = g.Key.IssueToType,
                    ItemCount    = g.Count(x => x.NetHolding > 0),
                    TotalValue   = g.Sum(x => x.TotalValue),
                    LastIssueDate = g.Max(x => x.LastIssueDate)
                })
                .Where(s => s.ItemCount > 0 || s.TotalValue > 0)
                .OrderBy(s => s.IssueToType)
                .ThenBy(s => s.EmployeeName)
                .ToList();

            return summary;
        }

        public async Task<List<EmployeeHoldingDto>> GetEmployeeHoldingDetail(
            int employeeID, string? issueToType)
        {
            var holdings = await BuildRawHoldings(employeeID, issueToType);
            return holdings.OrderBy(h => h.ItemName).ToList();
        }

        public async Task<List<EmployeeHoldingDto>> GetAllEmployeeHoldings(string? issueToType)
        {
            var holdings = await BuildRawHoldings(null, issueToType);
            return holdings
                .Where(h => h.NetHolding > 0)
                .OrderBy(h => h.IssueToType)
                .ThenBy(h => h.EmployeeName)
                .ThenBy(h => h.ItemName)
                .ToList();
        }

        /// <summary>
        /// Core helper: computes net holding (issued − returned) per
        /// employee × item. Optionally filtered by employeeID and/or issueToType.
        /// </summary>
        private async Task<List<EmployeeHoldingDto>> BuildRawHoldings(
            int? employeeID, string? issueToType)
        {
            // ── Step 1: all issue lines for Employee / Guard type docs ────────────
            var issueQuery =
                from doc    in _oBMSDbContext.StockIssueDocs
                join detail in _oBMSDbContext.StockIssueDocDetails on doc.ID equals detail.IssueDocID
                join item   in _oBMSDbContext.ItemMasters on detail.ItemID equals item.ID
                join ic     in _oBMSDbContext.InventoryCategories on item.CategoryID equals ic.ID into icJoin
                from ic     in icJoin.DefaultIfEmpty()
                join um     in _oBMSDbContext.UOMMasters on item.UOMID equals um.ID into umJoin
                from um     in umJoin.DefaultIfEmpty()
                where doc.IsActive == true
                   && (doc.IssueToType == "Staff" || doc.IssueToType == "Guard")
                select new
                {
                    doc.IssueToID,
                    doc.IssueToName,
                    doc.IssueToType,
                    doc.IssueDate,
                    doc.IssueNo,
                    detail.ID,          // StockIssueDocDetail.ID
                    detail.ItemID,
                    ItemCode     = item.Code ?? "",
                    ItemName     = item.Name,
                    CategoryName = ic != null ? ic.Name : "",
                    UOMName      = um != null ? um.Name : "",
                    UOMShort     = um != null ? um.ShortName : "",
                    detail.IssueQty,
                    detail.UnitCost
                };

            // Optional filters
            if (employeeID.HasValue && employeeID > 0)
                issueQuery = issueQuery.Where(x => x.IssueToID == employeeID);

            if (!string.IsNullOrEmpty(issueToType) && issueToType != "ALL")
                issueQuery = issueQuery.Where(x => x.IssueToType == issueToType);

            var issueLines = await issueQuery.ToListAsync();

            if (!issueLines.Any()) return new List<EmployeeHoldingDto>();

            // Collect all issue detail IDs to look up returns efficiently
            var detailIDs = issueLines.Select(x => x.ID).Distinct().ToList();

            // ── Step 2: returns against those lines ──────────────────────────────
            var returnedQtyMap = await _oBMSDbContext.StockIssueReturnDetails
                .Where(r => detailIDs.Contains(r.OriginalIssueDetailID))
                .GroupBy(r => r.OriginalIssueDetailID)
                .Select(g => new { DetailID = g.Key, ReturnedQty = g.Sum(r => r.ReturnQty) })
                .ToDictionaryAsync(x => x.DetailID, x => x.ReturnedQty);

            // ── Step 3: aggregate per employee × item ────────────────────────────
            var grouped = issueLines
                .GroupBy(x => new
                {
                    x.IssueToID,
                    x.IssueToName,
                    x.IssueToType,
                    x.ItemID,
                    x.ItemCode,
                    x.ItemName,
                    x.CategoryName,
                    x.UOMName
                })
                .Select(g =>
                {
                    decimal totalIssued   = g.Sum(x => x.IssueQty);
                    decimal totalReturned = g.Sum(x => returnedQtyMap.TryGetValue(x.ID, out var r) ? r : 0);
                    decimal netHolding    = totalIssued - totalReturned;
                    decimal unitCost      = g.OrderByDescending(x => x.IssueDate).First().UnitCost;

                    return new EmployeeHoldingDto
                    {
                        EmployeeID    = g.Key.IssueToID,
                        EmployeeName  = g.Key.IssueToName ?? "",
                        IssueToType   = g.Key.IssueToType,
                        ItemID        = g.Key.ItemID,
                        ItemCode      = g.Key.ItemCode,
                        ItemName      = g.Key.ItemName,
                        CategoryName  = g.Key.CategoryName,
                        UOMName       = g.Key.UOMName,
                        TotalIssued   = totalIssued,
                        TotalReturned = totalReturned,
                        NetHolding    = netHolding,
                        UnitCost      = unitCost,
                        TotalValue    = netHolding * unitCost,
                        LastIssueDate = g.Max(x => (DateTime?)x.IssueDate),
                        LastIssueNo   = g.OrderByDescending(x => x.IssueDate).First().IssueNo
                    };
                })
                .ToList();

            return grouped;
        }

        // ────────────────────────────────────────────────────────────────────────
        // B. STOCK BALANCE SUMMARY
        // ────────────────────────────────────────────────────────────────────────

        public async Task<List<StockBalanceSummaryDto>> GetStockBalanceSummary(
            int? warehouseID, int? itemID)
        {
            // Get all (warehouseID, itemID) pairs that have ledger entries
            var ledgerPairsQuery = _oBMSDbContext.StockLedgers
                .GroupBy(l => new { l.WarehouseID, l.ItemID })
                .Select(g => new
                {
                    g.Key.WarehouseID,
                    g.Key.ItemID,
                    InQty         = g.Sum(l => l.InQty),
                    OutQty        = g.Sum(l => l.OutQty),
                    LastMoveDate  = g.Max(l => (DateTime?)l.TransactionDate),
                    LastTxType    = g.OrderByDescending(l => l.TransactionDate)
                                     .ThenByDescending(l => l.ID)
                                     .Select(l => l.TransactionType)
                                     .FirstOrDefault(),
                    LastUnitCost  = g.OrderByDescending(l => l.TransactionDate)
                                     .ThenByDescending(l => l.ID)
                                     .Select(l => l.UnitCost)
                                     .FirstOrDefault()
                });

            if (warehouseID.HasValue && warehouseID > 0)
                ledgerPairsQuery = ledgerPairsQuery.Where(x => x.WarehouseID == warehouseID);

            if (itemID.HasValue && itemID > 0)
                ledgerPairsQuery = ledgerPairsQuery.Where(x => x.ItemID == itemID);

            var ledgerPairs = await ledgerPairsQuery.ToListAsync();

            if (!ledgerPairs.Any()) return new List<StockBalanceSummaryDto>();

            // Collect IDs for lookups
            var whIDs   = ledgerPairs.Select(x => x.WarehouseID).Distinct().ToList();
            var itemIDs = ledgerPairs.Select(x => x.ItemID).Distinct().ToList();

            var warehouses = await _oBMSDbContext.WarehouseMasters
                .Where(w => whIDs.Contains(w.ID))
                .ToDictionaryAsync(w => w.ID, w => w.Name);

            var items = await (
                from im in _oBMSDbContext.ItemMasters
                join ic in _oBMSDbContext.InventoryCategories on im.CategoryID equals ic.ID into icJoin
                from ic in icJoin.DefaultIfEmpty()
                join br in _oBMSDbContext.BrandMasters on im.BrandID equals br.ID into brJoin
                from br in brJoin.DefaultIfEmpty()
                join um in _oBMSDbContext.UOMMasters on im.UOMID equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                where itemIDs.Contains(im.ID)
                select new
                {
                    im.ID,
                    im.Code,
                    im.Name,
                    im.ReorderLevel,
                    CategoryName = ic != null ? ic.Name : "",
                    BrandName    = br != null ? br.Name : "",
                    UOMName      = um != null ? um.Name : "",
                    UOMShort     = um != null ? um.ShortName : ""
                }
            ).ToDictionaryAsync(x => x.ID);

            var result = ledgerPairs
                .Select(p =>
                {
                    var currentStock = p.InQty - p.OutQty;
                    var lastCost     = p.LastUnitCost ?? 0;
                    items.TryGetValue(p.ItemID, out var itemInfo);
                    warehouses.TryGetValue(p.WarehouseID, out var whName);

                    decimal? reorderLevel = itemInfo?.ReorderLevel;
                    bool belowReorder = reorderLevel.HasValue && reorderLevel > 0
                                        && currentStock <= reorderLevel;

                    return new StockBalanceSummaryDto
                    {
                        WarehouseID          = p.WarehouseID,
                        WarehouseName        = whName ?? "",
                        ItemID               = p.ItemID,
                        ItemCode             = itemInfo?.Code ?? "",
                        ItemName             = itemInfo?.Name ?? "",
                        CategoryName         = itemInfo?.CategoryName ?? "",
                        BrandName            = itemInfo?.BrandName ?? "",
                        UOMName              = itemInfo?.UOMName ?? "",
                        UOMShort             = itemInfo?.UOMShort ?? "",
                        CurrentStock         = currentStock,
                        ReorderLevel         = reorderLevel,
                        BelowReorderLevel    = belowReorder,
                        LastUnitCost         = p.LastUnitCost,
                        StockValue           = currentStock * lastCost,
                        LastMovementDate     = p.LastMoveDate,
                        LastTransactionType  = p.LastTxType
                    };
                })
                .OrderBy(x => x.WarehouseName)
                .ThenBy(x => x.ItemName)
                .ToList();

            return result;
        }

        // ────────────────────────────────────────────────────────────────────────
        // C. EMPLOYEE ISSUE REGISTER
        // ────────────────────────────────────────────────────────────────────────

        public async Task<List<EmployeeIssueRegisterDto>> GetEmployeeIssueRegister(
            DateTime dateFrom,
            DateTime dateTo,
            int? employeeID,
            string? issueToType)
        {
            var query =
                from doc    in _oBMSDbContext.StockIssueDocs
                join detail in _oBMSDbContext.StockIssueDocDetails on doc.ID equals detail.IssueDocID
                join wh     in _oBMSDbContext.WarehouseMasters on doc.WarehouseID equals wh.ID
                join item   in _oBMSDbContext.ItemMasters on detail.ItemID equals item.ID
                join ic     in _oBMSDbContext.InventoryCategories on item.CategoryID equals ic.ID into icJoin
                from ic     in icJoin.DefaultIfEmpty()
                join um     in _oBMSDbContext.UOMMasters on item.UOMID equals um.ID into umJoin
                from um     in umJoin.DefaultIfEmpty()
                where doc.IsActive == true
                   && (doc.IssueToType == "Staff" || doc.IssueToType == "Guard")
                   && doc.IssueDate >= dateFrom.Date
                   && doc.IssueDate <= dateTo.Date
                select new
                {
                    doc.IssueNo,
                    doc.IssueDate,
                    WarehouseName = wh.Name,
                    doc.IssueToType,
                    doc.IssueToID,
                    doc.IssueToName,
                    doc.LastUpdatedBy,
                    DetailID     = detail.ID,
                    detail.ItemID,
                    ItemCode     = item.Code ?? "",
                    ItemName     = item.Name,
                    CategoryName = ic != null ? ic.Name : "",
                    UOMName      = um != null ? um.Name : "",
                    detail.IssueQty,
                    detail.UnitCost,
                    detail.Remarks
                };

            if (employeeID.HasValue && employeeID > 0)
                query = query.Where(x => x.IssueToID == employeeID);

            if (!string.IsNullOrEmpty(issueToType) && issueToType != "ALL")
                query = query.Where(x => x.IssueToType == issueToType);

            var lines = await query
                .OrderBy(x => x.IssueToName)
                .ThenBy(x => x.IssueDate)
                .ThenBy(x => x.ItemName)
                .ToListAsync();

            if (!lines.Any()) return new List<EmployeeIssueRegisterDto>();

            // Get returned quantities for all detail IDs
            var detailIDs = lines.Select(x => x.DetailID).Distinct().ToList();
            var returnedMap = await _oBMSDbContext.StockIssueReturnDetails
                .Where(r => detailIDs.Contains(r.OriginalIssueDetailID))
                .GroupBy(r => r.OriginalIssueDetailID)
                .Select(g => new { DetailID = g.Key, ReturnedQty = g.Sum(r => r.ReturnQty) })
                .ToDictionaryAsync(x => x.DetailID, x => x.ReturnedQty);

            return lines.Select(x =>
            {
                decimal returned = returnedMap.TryGetValue(x.DetailID, out var r) ? r : 0;
                return new EmployeeIssueRegisterDto
                {
                    IssueNo      = x.IssueNo,
                    IssueDate    = x.IssueDate,
                    WarehouseName= x.WarehouseName,
                    IssueToType  = x.IssueToType,
                    EmployeeID   = x.IssueToID,
                    EmployeeName = x.IssueToName ?? "",
                    ItemID       = x.ItemID,
                    ItemCode     = x.ItemCode,
                    ItemName     = x.ItemName,
                    CategoryName = x.CategoryName,
                    UOMName      = x.UOMName,
                    IssueQty     = x.IssueQty,
                    UnitCost     = x.UnitCost,
                    TotalValue   = x.IssueQty * x.UnitCost,
                    ReturnedQty  = returned,
                    NetQty       = x.IssueQty - returned,
                    Remarks      = x.Remarks,
                    IssuedBy     = x.LastUpdatedBy
                };
            }).ToList();
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 6 — ASSET MANAGEMENT
        // All state-changing operations are transactional and append a row to
        // AssetMovementHistory for a complete, traceable lifecycle audit trail.
        // ════════════════════════════════════════════════════════════════════════

        // ── Shared helper: append movement history ────────────────────────────
        private void AppendMovementHistory(
            int assetID,
            string movementType,
            string? fromLocation,
            string? toLocation,
            string? fromPerson,
            string? toPerson,
            string? referenceNo,
            string? referenceType,
            int? referenceID,
            string? previousStatus,
            string? newStatus,
            string? remarks,
            string? createdBy)
        {
            _oBMSDbContext.AssetMovementHistories.Add(new AssetMovementHistory
            {
                AssetID        = assetID,
                MovementDate   = DateTime.Now,
                MovementType   = movementType,
                FromLocation   = fromLocation,
                ToLocation     = toLocation,
                FromPerson     = fromPerson,
                ToPerson       = toPerson,
                ReferenceNo    = referenceNo,
                ReferenceType  = referenceType,
                ReferenceID    = referenceID,
                PreviousStatus = previousStatus,
                NewStatus      = newStatus,
                Remarks        = remarks,
                CreatedBy      = createdBy,
                LASTUPDATE     = DateTime.Now
            });
        }

        // ── Generate sequential reference numbers ─────────────────────────────
        private string NextAssetCode()
        {
            var maxRow = _oBMSDbContext.AssetMasters
                .Where(x => x.AssetCode != null && x.AssetCode.StartsWith("AST") && x.IsActive)
                .OrderByDescending(x => x.AssetCode)
                .FirstOrDefault();
            return (maxRow == null || string.IsNullOrEmpty(maxRow.AssetCode))
                ? "AST0001"
                : int.TryParse(maxRow.AssetCode.Substring(3), out int n)
                    ? "AST" + (n + 1).ToString("D4") : "AST0001";
        }

        private string NextCode<T>(System.Linq.IQueryable<T> table,
            System.Linq.Expressions.Expression<Func<T, string?>> codeSelector,
            string prefix)
        {
            var all = table.Select(codeSelector)
                .Where(c => c != null && c.StartsWith(prefix))
                .OrderByDescending(c => c)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(all))
                return prefix + "0001";
            return int.TryParse(all.Substring(prefix.Length), out int n)
                ? prefix + (n + 1).ToString("D4") : prefix + "0001";
        }

        // ────────────────────────────────────────────────────────────────────
        // A. ASSET MASTER
        // ────────────────────────────────────────────────────────────────────

        public async Task<List<AssetListDto>> GetAssetList()
        {
            var list = await (
                from a in _oBMSDbContext.AssetMasters
                join sup in _oBMSDbContext.Suppliers on a.SupplierID equals sup.Id into supJoin
                from sup in supJoin.DefaultIfEmpty()
                orderby a.AssetCode
                select new AssetListDto
                {
                    ID                  = a.ID,
                    AssetCode           = a.AssetCode ?? "",
                    Name                = a.Name,
                    AssetType           = a.AssetType,
                    SerialNumber        = a.SerialNumber,
                    Description         = a.Description,
                    ModelNo             = a.ModelNo,
                    BrandName           = a.BrandName,
                    SupplierID          = a.SupplierID,
                    SupplierName        = sup != null ? sup.Name : "",
                    Branch              = a.Branch,
                    PurchaseAmount      = a.PurchaseAmount,
                    PurchaseDate        = a.PurchaseDate,
                    WarrantyExpiryDate  = a.WarrantyExpiryDate,
                    AssetStatus         = a.AssetStatus,
                    AssetCondition      = a.AssetCondition,
                    Remarks             = a.Remarks,
                    IsActive            = a.IsActive,
                    LASTUPDATE          = a.LASTUPDATE,
                    LastUpdatedBy       = a.LastUpdatedBy,
                    CurrentLocation     = a.CurrentLocation,
                    LocationStatus      = a.LocationStatus,
                    LocationUpdatedBy   = a.LocationUpdatedBy,
                    LocationUpdatedDate = a.LocationUpdatedDate
                }
            ).ToListAsync();

            // Enrich with current active allocation
            foreach (var asset in list)
            {
                var alloc = await _oBMSDbContext.AssetAllocations
                    .Where(x => x.AssetID == asset.ID && x.IsActive)
                    .OrderByDescending(x => x.AllocationDate)
                    .FirstOrDefaultAsync();
                if (alloc != null)
                {
                    asset.CurrentAllocatedTo   = alloc.AllocatedToName;
                    asset.CurrentAllocatedType = alloc.AllocatedToType;
                }
            }
            return list;
        }

        public async Task<AssetListDto?> GetAssetByIDEnriched(int id)
        {
            var asset = await (
                from a in _oBMSDbContext.AssetMasters
                join sup in _oBMSDbContext.Suppliers on a.SupplierID equals sup.Id into supJoin
                from sup in supJoin.DefaultIfEmpty()
                where a.ID == id
                select new AssetListDto
                {
                    ID                  = a.ID,
                    AssetCode           = a.AssetCode ?? "",
                    Name                = a.Name,
                    AssetType           = a.AssetType,
                    SerialNumber        = a.SerialNumber,
                    Description         = a.Description,
                    ModelNo             = a.ModelNo,
                    BrandName           = a.BrandName,
                    SupplierID          = a.SupplierID,
                    SupplierName        = sup != null ? sup.Name : "",
                    Branch              = a.Branch,
                    PurchaseAmount      = a.PurchaseAmount,
                    PurchaseDate        = a.PurchaseDate,
                    WarrantyExpiryDate  = a.WarrantyExpiryDate,
                    AssetStatus         = a.AssetStatus,
                    AssetCondition      = a.AssetCondition,
                    Remarks             = a.Remarks,
                    IsActive            = a.IsActive,
                    LASTUPDATE          = a.LASTUPDATE,
                    LastUpdatedBy       = a.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();

            if (asset == null) return null;

            var alloc = await _oBMSDbContext.AssetAllocations
                .Where(x => x.AssetID == id && x.IsActive)
                .OrderByDescending(x => x.AllocationDate)
                .FirstOrDefaultAsync();
            if (alloc != null)
            {
                asset.CurrentAllocatedTo   = alloc.AllocatedToName;
                asset.CurrentAllocatedType = alloc.AllocatedToType;
            }
            return asset;
        }

        public async Task<(bool Success, string Message, AssetMaster? Record)> SaveAsset(
            AssetMasterRequestDto dto)
        {
            // Duplicate serial number check
            if (!string.IsNullOrWhiteSpace(dto.SerialNumber))
            {
                bool dupSerial = await _oBMSDbContext.AssetMasters
                    .AnyAsync(x => x.SerialNumber == dto.SerialNumber
                               && x.ID != dto.ID
                               && x.IsActive);
                if (dupSerial)
                    return (false,
                        "An asset with this serial number already exists. Please check and try again.",
                        null);
            }

            AssetMaster? saved = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    AssetMaster asset;
                    if (dto.ID > 0)
                    {
                        asset = await _oBMSDbContext.AssetMasters.FindAsync(dto.ID)
                                ?? throw new InvalidOperationException("Asset not found.");
                    }
                    else
                    {
                        asset = new AssetMaster { AssetStatus = "Available", IsActive = true };
                        asset.AssetCode = NextAssetCode();
                        _oBMSDbContext.AssetMasters.Add(asset);
                    }

                    asset.Name               = dto.Name;
                    asset.AssetType          = dto.AssetType;
                    asset.SerialNumber       = dto.SerialNumber;
                    asset.Description        = dto.Description;
                    asset.ModelNo            = dto.ModelNo;
                    asset.BrandName          = dto.BrandName;
                    asset.SupplierID         = dto.SupplierID;
                    asset.Branch             = dto.Branch;
                    asset.PurchaseAmount     = dto.PurchaseAmount;
                    asset.PurchaseDate       = dto.PurchaseDate;
                    asset.WarrantyExpiryDate = dto.WarrantyExpiryDate;
                    asset.AssetCondition     = dto.AssetCondition;
                    asset.Remarks            = dto.Remarks;
                    asset.LASTUPDATE         = DateTime.Now;
                    asset.LastUpdatedDate    = DateTime.Now;
                    asset.LastUpdatedBy      = dto.LastUpdatedBy;

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    saved = asset;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            var action = dto.ID > 0 ? "updated" : "created";
            return (true, $"Asset {saved!.AssetCode} {action} successfully.", saved);
        }

        public async Task<(bool Success, string Message)> DeleteAsset(int id, string currentUser)
        {
            var asset = await _oBMSDbContext.AssetMasters.FindAsync(id);
            if (asset == null) return (false, "Asset not found.");

            bool hasActiveAlloc = await _oBMSDbContext.AssetAllocations
                .AnyAsync(x => x.AssetID == id && x.IsActive);
            if (hasActiveAlloc)
                return (false, "This asset cannot be deleted because it has an active allocation. Please return the asset first.");

            // Only soft-delete — never hard delete
            asset.IsActive      = false;
            asset.LASTUPDATE    = DateTime.Now;
            asset.LastUpdatedBy = currentUser;
            await _oBMSDbContext.SaveChangesAsync();

            return (true, "Asset deleted successfully.");
        }

        // ────────────────────────────────────────────────────────────────────
        // B. ASSET ALLOCATION
        // ────────────────────────────────────────────────────────────────────

        public async Task<List<AssetAllocationListDto>> GetAssetAllocationList(bool activeOnly)
        {
            var q = from aa in _oBMSDbContext.AssetAllocations
                    join a in _oBMSDbContext.AssetMasters on aa.AssetID equals a.ID
                    where !activeOnly || aa.IsActive == true
                    orderby aa.AllocationDate descending
                    select new AssetAllocationListDto
                    {
                        ID               = aa.ID,
                        AllocationNo     = aa.AllocationNo,
                        AssetID          = aa.AssetID,
                        AssetCode        = a.AssetCode ?? "",
                        AssetName        = a.Name,
                        SerialNumber     = a.SerialNumber,
                        AllocationDate   = aa.AllocationDate,
                        AllocatedToType  = aa.AllocatedToType,
                        AllocatedToID    = aa.AllocatedToID,
                        AllocatedToName  = aa.AllocatedToName,
                        BranchCode       = aa.BranchCode,
                        Remarks          = aa.Remarks,
                        IsActive         = aa.IsActive,
                        LASTUPDATE       = aa.LASTUPDATE,
                        LastUpdatedBy    = aa.LastUpdatedBy
                    };
            return await q.ToListAsync();
        }

        public async Task<AssetAllocationListDto?> GetAssetAllocationByID(int id)
        {
            return await (
                from aa in _oBMSDbContext.AssetAllocations
                join a in _oBMSDbContext.AssetMasters on aa.AssetID equals a.ID
                where aa.ID == id
                select new AssetAllocationListDto
                {
                    ID               = aa.ID,
                    AllocationNo     = aa.AllocationNo,
                    AssetID          = aa.AssetID,
                    AssetCode        = a.AssetCode ?? "",
                    AssetName        = a.Name,
                    SerialNumber     = a.SerialNumber,
                    AllocationDate   = aa.AllocationDate,
                    AllocatedToType  = aa.AllocatedToType,
                    AllocatedToID    = aa.AllocatedToID,
                    AllocatedToName  = aa.AllocatedToName,
                    BranchCode       = aa.BranchCode,
                    Remarks          = aa.Remarks,
                    IsActive         = aa.IsActive,
                    LASTUPDATE       = aa.LASTUPDATE,
                    LastUpdatedBy    = aa.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();
        }

        public async Task<List<AssetAllocationListDto>> GetActiveAllocationsByAsset(int assetID)
        {
            return await (
                from aa in _oBMSDbContext.AssetAllocations
                join a in _oBMSDbContext.AssetMasters on aa.AssetID equals a.ID
                where aa.AssetID == assetID && aa.IsActive
                select new AssetAllocationListDto
                {
                    ID = aa.ID, AllocationNo = aa.AllocationNo,
                    AssetID = aa.AssetID, AssetCode = a.AssetCode ?? "",
                    AssetName = a.Name, AllocationDate = aa.AllocationDate,
                    AllocatedToType = aa.AllocatedToType, AllocatedToID = aa.AllocatedToID,
                    AllocatedToName = aa.AllocatedToName, BranchCode = aa.BranchCode,
                    Remarks = aa.Remarks, IsActive = aa.IsActive,
                    LASTUPDATE = aa.LASTUPDATE, LastUpdatedBy = aa.LastUpdatedBy
                }
            ).ToListAsync();
        }

        public async Task<(bool Success, string Message, AssetAllocation? Record)> SaveAssetAllocation(
            AssetAllocationRequestDto dto)
        {
            var asset = await _oBMSDbContext.AssetMasters.FindAsync(dto.AssetID);
            if (asset == null) return (false, "Please select a valid asset.", null);

            if (asset.AssetStatus == "Disposed")
                return (false, "Disposed assets cannot be allocated.", null);
            if (asset.AssetStatus == "Under_Maintenance")
                return (false, "This asset is currently under maintenance and cannot be allocated.", null);

            bool alreadyAllocated = await _oBMSDbContext.AssetAllocations
                .AnyAsync(x => x.AssetID == dto.AssetID && x.IsActive);
            if (alreadyAllocated)
                return (false, "Selected asset is already allocated. Please return it before creating a new allocation.", null);

            string allocNo = NextCode(_oBMSDbContext.AssetAllocations, x => x.AllocationNo, "AA");
            string prevStatus = asset.AssetStatus;

            AssetAllocation? saved = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var alloc = new AssetAllocation
                    {
                        AllocationNo    = allocNo,
                        AssetID         = dto.AssetID,
                        AllocationDate  = dto.AllocationDate,
                        AllocatedToType = dto.AllocatedToType,
                        AllocatedToID   = dto.AllocatedToID,
                        AllocatedToName = dto.AllocatedToName,
                        BranchCode      = dto.BranchCode,
                        Remarks         = dto.Remarks,
                        IsActive        = true,
                        LASTUPDATE      = DateTime.Now,
                        LastUpdatedBy   = dto.LastUpdatedBy
                    };
                    _oBMSDbContext.AssetAllocations.Add(alloc);
                    await _oBMSDbContext.SaveChangesAsync();

                    asset.AssetStatus   = "Allocated";
                    asset.LASTUPDATE    = DateTime.Now;
                    asset.LastUpdatedBy = dto.LastUpdatedBy;

                    AppendMovementHistory(dto.AssetID, "Allocation",
                        null, dto.AllocatedToName,
                        null, dto.AllocatedToName,
                        allocNo, "Allocation", alloc.ID,
                        prevStatus, "Allocated",
                        dto.Remarks, dto.LastUpdatedBy);

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    saved = alloc;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Asset {asset.AssetCode} allocated successfully as {allocNo}.", saved);
        }

        public async Task<(bool Success, string Message)> CancelAssetAllocation(int id, string currentUser)
        {
            var alloc = await _oBMSDbContext.AssetAllocations.FindAsync(id);
            if (alloc == null) return (false, "Allocation record not found.");
            if (!alloc.IsActive) return (false, "This allocation is no longer active.");

            var asset = await _oBMSDbContext.AssetMasters.FindAsync(alloc.AssetID);
            if (asset == null) return (false, "Asset not found.");

            string prevStatus = asset.AssetStatus;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    alloc.IsActive      = false;
                    alloc.LASTUPDATE    = DateTime.Now;
                    alloc.LastUpdatedBy = currentUser;

                    asset.AssetStatus   = "Available";
                    asset.LASTUPDATE    = DateTime.Now;
                    asset.LastUpdatedBy = currentUser;

                    AppendMovementHistory(alloc.AssetID, "Allocation_Cancelled",
                        alloc.AllocatedToName, null,
                        alloc.AllocatedToName, null,
                        alloc.AllocationNo, "Allocation", alloc.ID,
                        prevStatus, "Available",
                        "Allocation cancelled.", currentUser);

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, "Allocation cancelled successfully. Asset is now available.");
        }

        // ────────────────────────────────────────────────────────────────────
        // C. ASSET TRANSFER
        // ────────────────────────────────────────────────────────────────────

        public async Task<List<AssetTransferListDto>> GetAssetTransferList()
        {
            return await (
                from at in _oBMSDbContext.AssetTransfers
                join a in _oBMSDbContext.AssetMasters on at.AssetID equals a.ID
                where at.IsActive
                orderby at.TransferDate descending
                select new AssetTransferListDto
                {
                    ID = at.ID, TransferNo = at.TransferNo,
                    AssetID = at.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                    TransferDate = at.TransferDate,
                    FromType = at.FromType, FromName = at.FromName,
                    ToType = at.ToType, ToName = at.ToName,
                    TransferReason = at.TransferReason, Remarks = at.Remarks,
                    LASTUPDATE = at.LASTUPDATE, LastUpdatedBy = at.LastUpdatedBy
                }
            ).ToListAsync();
        }

        public async Task<AssetTransferListDto?> GetAssetTransferByID(int id)
        {
            return await (
                from at in _oBMSDbContext.AssetTransfers
                join a in _oBMSDbContext.AssetMasters on at.AssetID equals a.ID
                where at.ID == id
                select new AssetTransferListDto
                {
                    ID = at.ID, TransferNo = at.TransferNo,
                    AssetID = at.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                    TransferDate = at.TransferDate,
                    FromType = at.FromType, FromName = at.FromName,
                    ToType = at.ToType, ToName = at.ToName,
                    TransferReason = at.TransferReason, Remarks = at.Remarks,
                    LASTUPDATE = at.LASTUPDATE, LastUpdatedBy = at.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, AssetTransfer? Record)> SaveAssetTransfer(
            AssetTransferRequestDto dto)
        {
            var asset = await _oBMSDbContext.AssetMasters.FindAsync(dto.AssetID);
            if (asset == null) return (false, "Please select a valid asset.", null);
            if (asset.AssetStatus == "Disposed")
                return (false, "Disposed assets cannot be transferred.", null);

            if (dto.FromType == dto.ToType && dto.FromID == dto.ToID && dto.FromName == dto.ToName)
                return (false, "Source and destination must be different.", null);

            string transferNo = NextCode(_oBMSDbContext.AssetTransfers, x => x.TransferNo, "AT");
            string prevStatus = asset.AssetStatus;

            AssetTransfer? saved = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    // Close existing active allocation
                    var existingAlloc = await _oBMSDbContext.AssetAllocations
                        .Where(x => x.AssetID == dto.AssetID && x.IsActive)
                        .FirstOrDefaultAsync();
                    if (existingAlloc != null)
                    {
                        existingAlloc.IsActive      = false;
                        existingAlloc.LASTUPDATE    = DateTime.Now;
                        existingAlloc.LastUpdatedBy = dto.LastUpdatedBy;
                    }

                    // Create new allocation for destination
                    var newAlloc = new AssetAllocation
                    {
                        AllocationNo    = NextCode(_oBMSDbContext.AssetAllocations, x => x.AllocationNo, "AA"),
                        AssetID         = dto.AssetID,
                        AllocationDate  = dto.TransferDate,
                        AllocatedToType = dto.ToType,
                        AllocatedToID   = dto.ToID,
                        AllocatedToName = dto.ToName,
                        IsActive        = true,
                        LASTUPDATE      = DateTime.Now,
                        LastUpdatedBy   = dto.LastUpdatedBy
                    };
                    _oBMSDbContext.AssetAllocations.Add(newAlloc);

                    var transfer = new AssetTransfer
                    {
                        TransferNo     = transferNo,
                        AssetID        = dto.AssetID,
                        TransferDate   = dto.TransferDate,
                        FromType       = dto.FromType,
                        FromID         = dto.FromID,
                        FromName       = dto.FromName,
                        ToType         = dto.ToType,
                        ToID           = dto.ToID,
                        ToName         = dto.ToName,
                        TransferReason = dto.TransferReason,
                        Remarks        = dto.Remarks,
                        IsActive       = true,
                        LASTUPDATE     = DateTime.Now,
                        LastUpdatedBy  = dto.LastUpdatedBy
                    };
                    _oBMSDbContext.AssetTransfers.Add(transfer);
                    await _oBMSDbContext.SaveChangesAsync();

                    asset.AssetStatus   = "Allocated";
                    asset.LASTUPDATE    = DateTime.Now;
                    asset.LastUpdatedBy = dto.LastUpdatedBy;

                    AppendMovementHistory(dto.AssetID, "Transfer",
                        dto.FromName, dto.ToName,
                        dto.FromName, dto.ToName,
                        transferNo, "Transfer", transfer.ID,
                        prevStatus, "Allocated",
                        dto.TransferReason, dto.LastUpdatedBy);

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    saved = transfer;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Asset transfer {transferNo} saved successfully.", saved);
        }

        // ────────────────────────────────────────────────────────────────────
        // D. ASSET RETURN
        // ────────────────────────────────────────────────────────────────────

        public async Task<List<AssetReturnListDto>> GetAssetReturnList()
        {
            return await (
                from ar in _oBMSDbContext.AssetReturns
                join a  in _oBMSDbContext.AssetMasters    on ar.AssetID      equals a.ID
                join aa in _oBMSDbContext.AssetAllocations on ar.AllocationID equals aa.ID into aaJoin
                from aa in aaJoin.DefaultIfEmpty()
                where ar.IsActive
                orderby ar.ReturnDate descending
                select new AssetReturnListDto
                {
                    ID = ar.ID, ReturnNo = ar.ReturnNo,
                    AssetID = ar.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                    AllocationNo = aa != null ? aa.AllocationNo : "",
                    ReturnDate = ar.ReturnDate,
                    ReturnedByType = ar.ReturnedByType, ReturnedByName = ar.ReturnedByName,
                    ReturnCondition = ar.ReturnCondition, ReceivedBy = ar.ReceivedBy,
                    Remarks = ar.Remarks, LASTUPDATE = ar.LASTUPDATE, LastUpdatedBy = ar.LastUpdatedBy
                }
            ).ToListAsync();
        }

        public async Task<AssetReturnListDto?> GetAssetReturnByID(int id)
        {
            return await (
                from ar in _oBMSDbContext.AssetReturns
                join a  in _oBMSDbContext.AssetMasters    on ar.AssetID      equals a.ID
                join aa in _oBMSDbContext.AssetAllocations on ar.AllocationID equals aa.ID into aaJoin
                from aa in aaJoin.DefaultIfEmpty()
                where ar.ID == id
                select new AssetReturnListDto
                {
                    ID = ar.ID, ReturnNo = ar.ReturnNo,
                    AssetID = ar.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                    AllocationNo = aa != null ? aa.AllocationNo : "",
                    ReturnDate = ar.ReturnDate,
                    ReturnedByType = ar.ReturnedByType, ReturnedByName = ar.ReturnedByName,
                    ReturnCondition = ar.ReturnCondition, ReceivedBy = ar.ReceivedBy,
                    Remarks = ar.Remarks, LASTUPDATE = ar.LASTUPDATE, LastUpdatedBy = ar.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, AssetReturn? Record)> SaveAssetReturn(
            AssetReturnRequestDto dto)
        {
            var asset = await _oBMSDbContext.AssetMasters.FindAsync(dto.AssetID);
            if (asset == null) return (false, "Asset not found.", null);

            var alloc = await _oBMSDbContext.AssetAllocations.FindAsync(dto.AllocationID);
            if (alloc == null || !alloc.IsActive)
                return (false, "The selected allocation is no longer active. Asset cannot be returned against it.", null);

            // Check not already returned
            bool alreadyReturned = await _oBMSDbContext.AssetReturns
                .AnyAsync(x => x.AllocationID == dto.AllocationID && x.IsActive);
            if (alreadyReturned)
                return (false, "This allocation has already been returned.", null);

            string returnNo   = NextCode(_oBMSDbContext.AssetReturns, x => x.ReturnNo, "AR");
            string prevStatus = asset.AssetStatus;

            // Post-return status: Damaged condition → Damaged, else Available
            string newStatus = (dto.ReturnCondition == "Damaged") ? "Damaged" : "Available";

            AssetReturn? saved = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var ret = new AssetReturn
                    {
                        ReturnNo        = returnNo,
                        AssetID         = dto.AssetID,
                        AllocationID    = dto.AllocationID,
                        ReturnDate      = dto.ReturnDate,
                        ReturnedByType  = dto.ReturnedByType,
                        ReturnedByID    = dto.ReturnedByID,
                        ReturnedByName  = dto.ReturnedByName,
                        ReturnCondition = dto.ReturnCondition,
                        ReceivedBy      = dto.ReceivedBy,
                        Remarks         = dto.Remarks,
                        IsActive        = true,
                        LASTUPDATE      = DateTime.Now,
                        LastUpdatedBy   = dto.LastUpdatedBy
                    };
                    _oBMSDbContext.AssetReturns.Add(ret);

                    alloc.IsActive      = false;
                    alloc.LASTUPDATE    = DateTime.Now;
                    alloc.LastUpdatedBy = dto.LastUpdatedBy;

                    asset.AssetStatus   = newStatus;
                    asset.AssetCondition= dto.ReturnCondition;
                    asset.LASTUPDATE    = DateTime.Now;
                    asset.LastUpdatedBy = dto.LastUpdatedBy;

                    await _oBMSDbContext.SaveChangesAsync();

                    AppendMovementHistory(dto.AssetID, "Return",
                        alloc.AllocatedToName, null,
                        alloc.AllocatedToName, dto.ReceivedBy,
                        returnNo, "Return", ret.ID,
                        prevStatus, newStatus,
                        dto.Remarks, dto.LastUpdatedBy);

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    saved = ret;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Asset return {returnNo} saved. Asset status updated to {newStatus}.", saved);
        }

        // ────────────────────────────────────────────────────────────────────
        // E. ASSET MAINTENANCE
        // ────────────────────────────────────────────────────────────────────

        public async Task<List<AssetMaintenanceListDto>> GetAssetMaintenanceList(int? assetID)
        {
            var q = from am in _oBMSDbContext.AssetMaintenances
                    join a in _oBMSDbContext.AssetMasters on am.AssetID equals a.ID
                    where am.IsActive && (!assetID.HasValue || am.AssetID == assetID)
                    orderby am.StartDate descending
                    select new AssetMaintenanceListDto
                    {
                        ID = am.ID, MaintenanceNo = am.MaintenanceNo,
                        AssetID = am.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                        MaintenanceType = am.MaintenanceType, ProblemDescription = am.ProblemDescription,
                        VendorName = am.VendorName, StartDate = am.StartDate,
                        CompletionDate = am.CompletionDate, MaintenanceCost = am.MaintenanceCost,
                        Status = am.Status, CompletionRemarks = am.CompletionRemarks,
                        Remarks = am.Remarks, LASTUPDATE = am.LASTUPDATE, LastUpdatedBy = am.LastUpdatedBy
                    };
            return await q.ToListAsync();
        }

        public async Task<AssetMaintenanceListDto?> GetAssetMaintenanceByID(int id)
        {
            return await (
                from am in _oBMSDbContext.AssetMaintenances
                join a in _oBMSDbContext.AssetMasters on am.AssetID equals a.ID
                where am.ID == id
                select new AssetMaintenanceListDto
                {
                    ID = am.ID, MaintenanceNo = am.MaintenanceNo,
                    AssetID = am.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                    MaintenanceType = am.MaintenanceType, ProblemDescription = am.ProblemDescription,
                    VendorName = am.VendorName, StartDate = am.StartDate,
                    CompletionDate = am.CompletionDate, MaintenanceCost = am.MaintenanceCost,
                    Status = am.Status, CompletionRemarks = am.CompletionRemarks,
                    Remarks = am.Remarks, LASTUPDATE = am.LASTUPDATE, LastUpdatedBy = am.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, AssetMaintenance? Record)> SaveAssetMaintenance(
            AssetMaintenanceRequestDto dto)
        {
            var asset = await _oBMSDbContext.AssetMasters.FindAsync(dto.AssetID);
            if (asset == null) return (false, "Please select a valid asset.", null);
            if (asset.AssetStatus == "Disposed")
                return (false, "Disposed assets cannot be sent for maintenance.", null);

            bool activeMaint = await _oBMSDbContext.AssetMaintenances
                .AnyAsync(x => x.AssetID == dto.AssetID
                            && x.IsActive
                            && (x.Status == "Scheduled" || x.Status == "In_Progress"));
            if (activeMaint)
                return (false, "Asset is already under maintenance. Please complete or cancel the existing maintenance record first.", null);

            string maintNo    = NextCode(_oBMSDbContext.AssetMaintenances, x => x.MaintenanceNo, "AM");
            string prevStatus = asset.AssetStatus;

            AssetMaintenance? saved = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var maint = new AssetMaintenance
                    {
                        MaintenanceNo      = maintNo,
                        AssetID            = dto.AssetID,
                        MaintenanceType    = dto.MaintenanceType,
                        ProblemDescription = dto.ProblemDescription,
                        VendorName         = dto.VendorName,
                        StartDate          = dto.StartDate,
                        CompletionDate     = dto.CompletionDate,
                        MaintenanceCost    = dto.MaintenanceCost ?? 0,
                        Status             = "In_Progress",
                        Remarks            = dto.Remarks,
                        IsActive           = true,
                        LASTUPDATE         = DateTime.Now,
                        LastUpdatedBy      = dto.LastUpdatedBy
                    };
                    _oBMSDbContext.AssetMaintenances.Add(maint);

                    asset.AssetStatus   = "Under_Maintenance";
                    asset.LASTUPDATE    = DateTime.Now;
                    asset.LastUpdatedBy = dto.LastUpdatedBy;

                    await _oBMSDbContext.SaveChangesAsync();

                    AppendMovementHistory(dto.AssetID, "Maintenance",
                        null, dto.VendorName,
                        null, dto.VendorName,
                        maintNo, "Maintenance", maint.ID,
                        prevStatus, "Under_Maintenance",
                        dto.ProblemDescription, dto.LastUpdatedBy);

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    saved = maint;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Maintenance record {maintNo} created. Asset status set to Under Maintenance.", saved);
        }

        public async Task<(bool Success, string Message)> CompleteAssetMaintenance(
            int id, DateTime completionDate, decimal cost, string remarks, string currentUser)
        {
            var maint = await _oBMSDbContext.AssetMaintenances.FindAsync(id);
            if (maint == null) return (false, "Maintenance record not found.");
            if (maint.Status == "Completed")
                return (false, "This maintenance record is already completed.");

            var asset = await _oBMSDbContext.AssetMasters.FindAsync(maint.AssetID);
            if (asset == null) return (false, "Asset not found.");

            string prevStatus = asset.AssetStatus;
            // Restore to previous status: if was allocated before, keep allocated else available
            string newStatus = "Available";

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    maint.Status             = "Completed";
                    maint.CompletionDate     = completionDate;
                    maint.MaintenanceCost    = cost;
                    maint.CompletionRemarks  = remarks;
                    maint.LASTUPDATE         = DateTime.Now;
                    maint.LastUpdatedBy      = currentUser;

                    asset.AssetStatus   = newStatus;
                    asset.LASTUPDATE    = DateTime.Now;
                    asset.LastUpdatedBy = currentUser;

                    AppendMovementHistory(maint.AssetID, "Maintenance_Complete",
                        maint.VendorName, null,
                        maint.VendorName, null,
                        maint.MaintenanceNo, "Maintenance", maint.ID,
                        prevStatus, newStatus,
                        remarks, currentUser);

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Maintenance {maint.MaintenanceNo} completed. Asset is now Available.");
        }

        // ────────────────────────────────────────────────────────────────────
        // F. ASSET DAMAGE
        // ────────────────────────────────────────────────────────────────────

        public async Task<List<AssetDamageListDto>> GetAssetDamageList(int? assetID)
        {
            var q = from ad in _oBMSDbContext.AssetDamages
                    join a in _oBMSDbContext.AssetMasters on ad.AssetID equals a.ID
                    where ad.IsActive && (!assetID.HasValue || ad.AssetID == assetID)
                    orderby ad.DamageDate descending
                    select new AssetDamageListDto
                    {
                        ID = ad.ID, DamageNo = ad.DamageNo,
                        AssetID = ad.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                        DamageDate = ad.DamageDate, DamageType = ad.DamageType,
                        DamageDescription = ad.DamageDescription, DamageCondition = ad.DamageCondition,
                        ReportedBy = ad.ReportedBy, EstimatedRepairCost = ad.EstimatedRepairCost,
                        Remarks = ad.Remarks, LASTUPDATE = ad.LASTUPDATE, LastUpdatedBy = ad.LastUpdatedBy
                    };
            return await q.ToListAsync();
        }

        public async Task<AssetDamageListDto?> GetAssetDamageByID(int id)
        {
            return await (
                from ad in _oBMSDbContext.AssetDamages
                join a in _oBMSDbContext.AssetMasters on ad.AssetID equals a.ID
                where ad.ID == id
                select new AssetDamageListDto
                {
                    ID = ad.ID, DamageNo = ad.DamageNo,
                    AssetID = ad.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                    DamageDate = ad.DamageDate, DamageType = ad.DamageType,
                    DamageDescription = ad.DamageDescription, DamageCondition = ad.DamageCondition,
                    ReportedBy = ad.ReportedBy, EstimatedRepairCost = ad.EstimatedRepairCost,
                    Remarks = ad.Remarks, LASTUPDATE = ad.LASTUPDATE, LastUpdatedBy = ad.LastUpdatedBy
                }
            ).FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, AssetDamage? Record)> SaveAssetDamage(
            AssetDamageRequestDto dto)
        {
            var asset = await _oBMSDbContext.AssetMasters.FindAsync(dto.AssetID);
            if (asset == null) return (false, "Please select a valid asset.", null);
            if (asset.AssetStatus == "Disposed")
                return (false, "This asset has already been disposed.", null);

            string damageNo   = NextCode(_oBMSDbContext.AssetDamages, x => x.DamageNo, "AD");
            string prevStatus = asset.AssetStatus;

            AssetDamage? saved = null;
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var dmg = new AssetDamage
                    {
                        DamageNo            = damageNo,
                        AssetID             = dto.AssetID,
                        DamageDate          = dto.DamageDate,
                        DamageType          = dto.DamageType,
                        DamageDescription   = dto.DamageDescription,
                        DamageCondition     = dto.DamageCondition,
                        ReportedBy          = dto.ReportedBy,
                        EstimatedRepairCost = dto.EstimatedRepairCost ?? 0,
                        Remarks             = dto.Remarks,
                        IsActive            = true,
                        LASTUPDATE          = DateTime.Now,
                        LastUpdatedBy       = dto.LastUpdatedBy
                    };
                    _oBMSDbContext.AssetDamages.Add(dmg);

                    asset.AssetStatus    = "Damaged";
                    asset.AssetCondition = dto.DamageCondition;
                    asset.LASTUPDATE     = DateTime.Now;
                    asset.LastUpdatedBy  = dto.LastUpdatedBy;

                    await _oBMSDbContext.SaveChangesAsync();

                    AppendMovementHistory(dto.AssetID, "Damage",
                        null, null, dto.ReportedBy, null,
                        damageNo, "Damage", dmg.ID,
                        prevStatus, "Damaged",
                        dto.DamageDescription, dto.LastUpdatedBy);

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                    saved = dmg;
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            return (true, $"Damage record {damageNo} saved. Asset status set to Damaged.", saved);
        }

        // ────────────────────────────────────────────────────────────────────
        // G. ASSET DISPOSAL
        // ────────────────────────────────────────────────────────────────────

        public async Task<List<AssetDisposalListDto>> GetAssetDisposalList()
        {
            return await (
                from disp in _oBMSDbContext.AssetDisposals
                join a in _oBMSDbContext.AssetMasters on disp.AssetID equals a.ID
                where disp.IsActive
                orderby disp.DisposalDate descending
                select new AssetDisposalListDto
                {
                    ID = disp.ID, DisposalNo = disp.DisposalNo,
                    AssetID = disp.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                    DisposalDate = disp.DisposalDate, DisposalType = disp.DisposalType,
                    DisposalReason = disp.DisposalReason, DisposalValue = disp.DisposalValue,
                    ApprovedBy = disp.ApprovedBy, Remarks = disp.Remarks,
                    LASTUPDATE = disp.LASTUPDATE, LastUpdatedBy = disp.LastUpdatedBy,
                    Status        = disp.Status,
                    SubmittedBy   = disp.SubmittedBy,
                    SubmittedDate = disp.SubmittedDate,
                    ApprovedDate  = disp.ApprovedDate,
                    ApprovalRemarks = disp.ApprovalRemarks
                }
            ).ToListAsync();
        }

        public async Task<AssetDisposalListDto?> GetAssetDisposalByID(int id)
        {
            return await (
                from disp in _oBMSDbContext.AssetDisposals
                join a in _oBMSDbContext.AssetMasters on disp.AssetID equals a.ID
                where disp.ID == id
                select new AssetDisposalListDto
                {
                    ID = disp.ID, DisposalNo = disp.DisposalNo,
                    AssetID = disp.AssetID, AssetCode = a.AssetCode ?? "", AssetName = a.Name,
                    DisposalDate = disp.DisposalDate, DisposalType = disp.DisposalType,
                    DisposalReason = disp.DisposalReason, DisposalValue = disp.DisposalValue,
                    ApprovedBy = disp.ApprovedBy, Remarks = disp.Remarks,
                    LASTUPDATE = disp.LASTUPDATE, LastUpdatedBy = disp.LastUpdatedBy,
                    Status        = disp.Status,
                    SubmittedBy   = disp.SubmittedBy,
                    SubmittedDate = disp.SubmittedDate,
                    ApprovedDate  = disp.ApprovedDate,
                    ApprovalRemarks = disp.ApprovalRemarks
                }
            ).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Saves an Asset Disposal as Draft only — does NOT change asset status yet.
        /// Asset status is only changed to Disposed when ApproveAD is called.
        /// Only Draft records can be edited.
        /// </summary>
        public async Task<(bool Success, string Message, AssetDisposal? Record)> SaveAssetDisposal(
            AssetDisposalRequestDto dto)
        {
            var asset = await _oBMSDbContext.AssetMasters.FindAsync(dto.AssetID);
            if (asset == null) return (false, "Please select a valid asset.", null);
            if (asset.AssetStatus == "Disposed")
                return (false, "Asset has already been disposed.", null);

            bool hasActiveAlloc = await _oBMSDbContext.AssetAllocations
                .AnyAsync(x => x.AssetID == dto.AssetID && x.IsActive);
            if (hasActiveAlloc)
                return (false, "Asset has an active allocation. Please return the asset before disposal.", null);

            // For existing records, only Draft can be edited
            if (dto.ID > 0)
            {
                var existing = await _oBMSDbContext.AssetDisposals.FindAsync(dto.ID);
                if (existing != null && existing.Status != "Draft")
                    return (false, $"Only Draft disposals can be edited. This record is currently {existing.Status}.", null);
            }

            string disposalNo = dto.DisposalNo ?? string.Empty;
            if (string.IsNullOrEmpty(disposalNo))
                disposalNo = NextCode(_oBMSDbContext.AssetDisposals, x => x.DisposalNo, "DIS");

            AssetDisposal disp;
            if (dto.ID > 0)
            {
                disp = await _oBMSDbContext.AssetDisposals.FindAsync(dto.ID)
                       ?? throw new InvalidOperationException("Disposal record not found.");
            }
            else
            {
                disp = new AssetDisposal();
                _oBMSDbContext.AssetDisposals.Add(disp);
            }

            disp.DisposalNo     = disposalNo;
            disp.AssetID        = dto.AssetID;
            disp.DisposalDate   = dto.DisposalDate;
            disp.DisposalType   = dto.DisposalType;
            disp.DisposalReason = dto.DisposalReason;
            disp.DisposalValue  = dto.DisposalValue ?? 0;
            disp.Remarks        = dto.Remarks;
            disp.Status         = "Draft";
            disp.IsActive       = true;
            disp.LASTUPDATE     = DateTime.Now;
            disp.LastUpdatedBy  = dto.LastUpdatedBy;

            await _oBMSDbContext.SaveChangesAsync();

            return (true, $"Asset disposal {disposalNo} saved as Draft. Submit for approval when ready.", disp);
        }

        /// <summary>
        /// Submits a Draft Asset Disposal for approval.
        /// Auto-approves if ApprovalRequired=false or no routing configured.
        /// On auto-approve: asset status → Disposed (same as old SaveAssetDisposal behaviour).
        /// </summary>
        public async Task<(bool Success, string Message)> SubmitAD(int id, string currentUser)
        {
            var disp = await _oBMSDbContext.AssetDisposals.FindAsync(id);
            if (disp == null) return (false, "Disposal record not found.");
            if (disp.Status != "Draft")
                return (false, $"Only a Draft disposal can be submitted. This record is currently {disp.Status}.");

            if (_approval != null)
            {
                bool approvalRequired = await _approval.IsApprovalRequired("ASSET_DISPOSAL");
                if (!approvalRequired)
                {
                    await ApplyADDisposal(disp, currentUser);
                    disp.Status        = "Approved";
                    disp.ApprovedBy    = currentUser;
                    disp.ApprovedDate  = DateTime.Now;
                    disp.SubmittedBy   = currentUser;
                    disp.SubmittedDate = DateTime.Now;
                    disp.LASTUPDATE    = DateTime.Now;
                    disp.LastUpdatedBy = currentUser;
                    await _oBMSDbContext.SaveChangesAsync();
                    var a = await _oBMSDbContext.AssetMasters.FindAsync(disp.AssetID);
                    return (true, $"Asset disposal {disp.DisposalNo} approved. Asset {a?.AssetCode} has been disposed.");
                }

                var approverUser = await _approval.GetApproverForUser(currentUser, "ASSET_DISPOSAL");
                if (string.IsNullOrEmpty(approverUser))
                {
                    await ApplyADDisposal(disp, currentUser);
                    disp.Status        = "Approved";
                    disp.ApprovedBy    = currentUser;
                    disp.ApprovedDate  = DateTime.Now;
                    disp.SubmittedBy   = currentUser;
                    disp.SubmittedDate = DateTime.Now;
                    disp.LASTUPDATE    = DateTime.Now;
                    disp.LastUpdatedBy = currentUser;
                    await _oBMSDbContext.SaveChangesAsync();
                    var a = await _oBMSDbContext.AssetMasters.FindAsync(disp.AssetID);
                    return (true, $"Asset disposal {disp.DisposalNo} approved. Asset {a?.AssetCode} has been disposed.");
                }

                string prev        = disp.Status;
                disp.Status        = "Submitted";
                disp.SubmittedBy   = currentUser;
                disp.SubmittedDate = DateTime.Now;
                disp.LASTUPDATE    = DateTime.Now;
                disp.LastUpdatedBy = currentUser;
                await _oBMSDbContext.SaveChangesAsync();

                await _approval.RecordHistory("ASSET_DISPOSAL", disp.ID, disp.DisposalNo,
                    "Submitted", currentUser, prev, "Submitted", null);

                if (_notif != null)
                    await _notif.SendApprovalNotification(
                        "PENDING_APPROVAL", "ASSET_DISPOSAL", disp.DisposalNo, disp.ID,
                        approverUser, currentUser, null);
            }
            else
            {
                disp.Status        = "Submitted";
                disp.SubmittedBy   = currentUser;
                disp.SubmittedDate = DateTime.Now;
                disp.LASTUPDATE    = DateTime.Now;
                disp.LastUpdatedBy = currentUser;
                await _oBMSDbContext.SaveChangesAsync();
            }

            return (true, $"Asset disposal {disp.DisposalNo} has been submitted for approval.");
        }

        /// <summary>
        /// Approves a Submitted Asset Disposal.
        /// On approval: asset status → Disposed + movement history recorded.
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveAD(int id, ADApprovalDto dto)
        {
            var disp = await _oBMSDbContext.AssetDisposals.FindAsync(id);
            if (disp == null) return (false, "Disposal record not found.");
            if (disp.Status != "Submitted")
                return (false, "Only a Submitted disposal can be approved.");

            var submitter = disp.SubmittedBy ?? disp.LastUpdatedBy ?? string.Empty;
            bool isSuperAdmin   = dto.IsSuperAdmin;
            bool isSelfApproval = !isSuperAdmin &&
                                  !string.IsNullOrWhiteSpace(submitter) &&
                                  string.Equals(dto.ApprovedBy, submitter, StringComparison.OrdinalIgnoreCase);
            if (isSelfApproval)
                return (false, "You cannot approve a document that you submitted yourself.");

            if (!isSuperAdmin && _approval != null)
            {
                var authorized = await _approval.IsAuthorizedApprover(
                    dto.ApprovedBy, submitter, "ASSET_DISPOSAL");
                if (!authorized)
                    return (false, "You are not authorized to approve this disposal.");
            }

            // Apply the actual disposal — set asset status to Disposed
            await ApplyADDisposal(disp, dto.ApprovedBy);

            string prev        = disp.Status;
            disp.Status        = "Approved";
            disp.ApprovedBy    = dto.ApprovedBy;
            disp.ApprovedDate  = DateTime.Now;
            disp.ApprovalRemarks = dto.ApprovalRemarks;
            disp.LASTUPDATE    = DateTime.Now;
            disp.LastUpdatedBy = dto.ApprovedBy;
            await _oBMSDbContext.SaveChangesAsync();

            if (_approval != null)
                await _approval.RecordHistory("ASSET_DISPOSAL", disp.ID, disp.DisposalNo,
                    "Approved", dto.ApprovedBy, prev, "Approved", dto.ApprovalRemarks);
            if (_notif != null && !string.IsNullOrEmpty(disp.SubmittedBy))
                await _notif.SendApprovalNotification(
                    "APPROVED", "ASSET_DISPOSAL", disp.DisposalNo, disp.ID,
                    disp.SubmittedBy, dto.ApprovedBy, dto.ApprovalRemarks);

            // Notify Purchase department — asset has been disposed and inventory records should be reconciled
            if (_notif != null)
                await _notif.SendApprovalNotificationToDepartment(
                    "APPROVED", "ASSET_DISPOSAL", disp.DisposalNo, disp.ID,
                    "Purchase", dto.ApprovedBy, dto.ApprovalRemarks,
                    excludeUsers: new[] { disp.SubmittedBy ?? string.Empty });

            var asset = await _oBMSDbContext.AssetMasters.FindAsync(disp.AssetID);
            return (true, $"Asset disposal {disp.DisposalNo} approved. Asset {asset?.AssetCode} has been disposed.");
        }

        /// <summary>
        /// Rejects a Submitted Asset Disposal. Asset status remains unchanged.
        /// </summary>
        public async Task<(bool Success, string Message)> RejectAD(int id, ADApprovalDto dto)
        {
            var disp = await _oBMSDbContext.AssetDisposals.FindAsync(id);
            if (disp == null) return (false, "Disposal record not found.");
            if (disp.Status != "Submitted")
                return (false, "Only a Submitted disposal can be rejected.");
            if (string.IsNullOrWhiteSpace(dto.ApprovalRemarks))
                return (false, "Please enter a reason for rejection so the requester knows what to correct.");

            var submitter = disp.SubmittedBy ?? disp.LastUpdatedBy ?? string.Empty;
            bool isSuperAdmin    = dto.IsSuperAdmin;
            bool isSelfRejection = !isSuperAdmin &&
                                   !string.IsNullOrWhiteSpace(submitter) &&
                                   string.Equals(dto.ApprovedBy, submitter, StringComparison.OrdinalIgnoreCase);
            if (isSelfRejection)
                return (false, "You cannot reject a document that you submitted yourself.");

            if (!isSuperAdmin && _approval != null)
            {
                var authorized = await _approval.IsAuthorizedApprover(
                    dto.ApprovedBy, submitter, "ASSET_DISPOSAL");
                if (!authorized)
                    return (false, "You are not authorized to reject this disposal.");
            }

            string prev        = disp.Status;
            disp.Status        = "Rejected";
            disp.ApprovedBy    = dto.ApprovedBy;
            disp.ApprovedDate  = DateTime.Now;
            disp.ApprovalRemarks = dto.ApprovalRemarks;
            disp.LASTUPDATE    = DateTime.Now;
            disp.LastUpdatedBy = dto.ApprovedBy;
            await _oBMSDbContext.SaveChangesAsync();

            if (_approval != null)
                await _approval.RecordHistory("ASSET_DISPOSAL", disp.ID, disp.DisposalNo,
                    "Rejected", dto.ApprovedBy, prev, "Rejected", dto.ApprovalRemarks);
            if (_notif != null && !string.IsNullOrEmpty(disp.SubmittedBy))
                await _notif.SendApprovalNotification(
                    "REJECTED", "ASSET_DISPOSAL", disp.DisposalNo, disp.ID,
                    disp.SubmittedBy, dto.ApprovedBy, dto.ApprovalRemarks);

            return (true, $"Asset disposal {disp.DisposalNo} has been rejected.");
        }

        /// <summary>
        /// Internal helper: set asset status → Disposed and record movement history.
        /// Called from both SubmitAD (auto-approve path) and ApproveAD.
        /// </summary>
        private async Task ApplyADDisposal(AssetDisposal disp, string actorUser)
        {
            var asset = await _oBMSDbContext.AssetMasters.FindAsync(disp.AssetID);
            if (asset == null) return;

            string prevStatus  = asset.AssetStatus;
            asset.AssetStatus  = "Disposed";
            asset.LASTUPDATE   = DateTime.Now;
            asset.LastUpdatedBy = actorUser;

            AppendMovementHistory(disp.AssetID, "Disposal",
                null, null, null, null,
                disp.DisposalNo, "Disposal", disp.ID,
                prevStatus, "Disposed",
                disp.DisposalReason, actorUser);

            await _oBMSDbContext.SaveChangesAsync();
        }

        // ────────────────────────────────────────────────────────────────────
        // H. ASSET MOVEMENT HISTORY
        // ────────────────────────────────────────────────────────────────────

        public async Task<List<AssetMovementHistoryDto>> GetAssetMovementHistory(int assetID)
        {
            return await (
                from h in _oBMSDbContext.AssetMovementHistories
                join a in _oBMSDbContext.AssetMasters on h.AssetID equals a.ID
                where h.AssetID == assetID
                orderby h.MovementDate ascending
                select new AssetMovementHistoryDto
                {
                    ID             = h.ID,
                    AssetID        = h.AssetID,
                    AssetCode      = a.AssetCode ?? "",
                    AssetName      = a.Name,
                    MovementDate   = h.MovementDate,
                    MovementType   = h.MovementType,
                    FromLocation   = h.FromLocation,
                    ToLocation     = h.ToLocation,
                    FromPerson     = h.FromPerson,
                    ToPerson       = h.ToPerson,
                    ReferenceNo    = h.ReferenceNo,
                    ReferenceType  = h.ReferenceType,
                    PreviousStatus = h.PreviousStatus,
                    NewStatus      = h.NewStatus,
                    Remarks        = h.Remarks,
                    CreatedBy      = h.CreatedBy
                }
            ).ToListAsync();
        }

        // ────────────────────────────────────────────────────────────────────────
        // ASSET LOCATION TRACKING
        // ────────────────────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> UpdateAssetLocation(
            AssetLocationUpdateDto dto)
        {
            if (dto.AssetID <= 0)
                return (false, "Please select a valid asset.");
            if (string.IsNullOrWhiteSpace(dto.NewLocation))
                return (false, "Please enter the current location.");
            if (string.IsNullOrWhiteSpace(dto.LocationStatus))
                return (false, "Please select the location status.");

            var asset = await _oBMSDbContext.AssetMasters.FindAsync(dto.AssetID);
            if (asset == null) return (false, "Asset not found.");
            if (asset.AssetStatus == "Disposed")
                return (false, "Disposed assets cannot be updated.");

            string previousLocation = asset.CurrentLocation ?? "";

            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    // Append to location log
                    _oBMSDbContext.AssetLocationLogs.Add(new AssetLocationLog
                    {
                        AssetID          = dto.AssetID,
                        LogDate          = DateTime.Now,
                        PreviousLocation = previousLocation,
                        NewLocation      = dto.NewLocation,
                        LocationStatus   = dto.LocationStatus,
                        UpdatedBy        = dto.UpdatedBy,
                        Remarks          = dto.Remarks,
                        LASTUPDATE       = DateTime.Now,
                        LastUpdatedBy    = dto.LastUpdatedBy
                    });

                    // Update AssetMaster current location
                    asset.CurrentLocation      = dto.NewLocation;
                    asset.LocationStatus       = dto.LocationStatus;
                    asset.LocationUpdatedBy    = dto.UpdatedBy;
                    asset.LocationUpdatedDate  = DateTime.Now;
                    asset.LASTUPDATE           = DateTime.Now;
                    asset.LastUpdatedBy        = dto.LastUpdatedBy;

                    // Append to movement history for full lifecycle visibility
                    AppendMovementHistory(
                        dto.AssetID,
                        "Location_Update",
                        previousLocation,
                        dto.NewLocation,
                        null, dto.UpdatedBy,
                        null, "Location", null,
                        null, null,
                        $"{dto.LocationStatus}: {dto.Remarks}",
                        dto.LastUpdatedBy);

                    await _oBMSDbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch { await tx.RollbackAsync(); throw; }
            });

            string statusLabel = dto.LocationStatus.Replace("_", " ");
            return (true, $"Location updated successfully. Status: {statusLabel}.");
        }

        public async Task<List<AssetLocationLogDto>> GetAssetLocationLog(int assetID)
        {
            return await (
                from log in _oBMSDbContext.AssetLocationLogs
                join a   in _oBMSDbContext.AssetMasters on log.AssetID equals a.ID
                where log.AssetID == assetID
                orderby log.LogDate descending
                select new AssetLocationLogDto
                {
                    ID               = log.ID,
                    AssetID          = log.AssetID,
                    AssetCode        = a.AssetCode ?? "",
                    AssetName        = a.Name,
                    LogDate          = log.LogDate,
                    PreviousLocation = log.PreviousLocation,
                    NewLocation      = log.NewLocation,
                    LocationStatus   = log.LocationStatus,
                    UpdatedBy        = log.UpdatedBy,
                    Remarks          = log.Remarks,
                    LastUpdatedBy    = log.LastUpdatedBy
                }
            ).ToListAsync();
        }

        public async Task<List<AssetListDto>> GetDefectLocationAssets()
        {
            var list = await (
                from a in _oBMSDbContext.AssetMasters
                join sup in _oBMSDbContext.Suppliers on a.SupplierID equals sup.Id into supJoin
                from sup in supJoin.DefaultIfEmpty()
                where a.IsActive == true
                   && (a.LocationStatus == "Defect_Location"
                    || a.LocationStatus == "Parked_Away"
                    || a.LocationStatus == "Unknown")
                orderby a.LocationUpdatedDate descending
                select new AssetListDto
                {
                    ID                  = a.ID,
                    AssetCode           = a.AssetCode ?? "",
                    Name                = a.Name,
                    AssetType           = a.AssetType,
                    SerialNumber        = a.SerialNumber,
                    Branch              = a.Branch,
                    PurchaseAmount      = a.PurchaseAmount,
                    AssetStatus         = a.AssetStatus,
                    AssetCondition      = a.AssetCondition,
                    IsActive            = a.IsActive,
                    LASTUPDATE          = a.LASTUPDATE,
                    LastUpdatedBy       = a.LastUpdatedBy,
                    CurrentLocation     = a.CurrentLocation,
                    LocationStatus      = a.LocationStatus,
                    LocationUpdatedBy   = a.LocationUpdatedBy,
                    LocationUpdatedDate = a.LocationUpdatedDate
                }
            ).ToListAsync();
            return list;
        }

        // ════════════════════════════════════════════════════════════════════════
        // PART 9 — BRANCH STOCK REQUEST
        // ════════════════════════════════════════════════════════════════════════

        // ── Shared read-side query ────────────────────────────────────────────

        private IQueryable<StockRequestListDto> BSRListQuery() =>
            from sr  in _oBMSDbContext.StockRequests
            join br  in _oBMSDbContext.BranchMasters    on sr.RequestingBranchID equals br.ID
            join wh  in _oBMSDbContext.WarehouseMasters  on sr.SourceWarehouseID  equals wh.ID
            join trf in _oBMSDbContext.StockTransfers    on sr.LinkedTransferID   equals trf.ID into trfJoin
            from trf in trfJoin.DefaultIfEmpty()
            where sr.IsActive == true
            orderby sr.RequestDate descending, sr.ID descending
            select new StockRequestListDto
            {
                ID                    = sr.ID,
                RequestNo             = sr.RequestNo,
                RequestDate           = sr.RequestDate,
                RequestingBranchID    = sr.RequestingBranchID,
                RequestingBranchName  = br.Name,
                SourceWarehouseID     = sr.SourceWarehouseID,
                SourceWarehouseName   = wh.Name,
                RequestedBy           = sr.RequestedBy,
                Status                = sr.Status,
                Remarks               = sr.Remarks,
                SubmittedBy           = sr.SubmittedBy ?? sr.LastUpdatedBy,   // fallback so frontend always gets a non-null requester
                SubmittedDate         = sr.SubmittedDate,
                ApprovedBy            = sr.ApprovedBy,
                ApprovedDate          = sr.ApprovedDate,
                ApprovalRemarks       = sr.ApprovalRemarks,
                LinkedTransferID      = sr.LinkedTransferID,
                LinkedTransferNo      = trf != null ? trf.TransferNo : null,
                LASTUPDATE            = sr.LASTUPDATE,
                LastUpdatedBy         = sr.LastUpdatedBy
            };

        private async Task<List<StockRequestDetailListDto>> GetBSRDetails(int requestID)
        {
            return await (
                from d  in _oBMSDbContext.StockRequestDetails
                join im in _oBMSDbContext.ItemMasters on d.ItemID equals im.ID
                join um in _oBMSDbContext.UOMMasters  on d.UOMID  equals um.ID into umJoin
                from um in umJoin.DefaultIfEmpty()
                join cl in _oBMSDbContext.ClientMasters on d.ClientID equals cl.ID into clJoin
                from cl in clJoin.DefaultIfEmpty()
                where d.RequestID == requestID
                orderby d.ID
                select new StockRequestDetailListDto
                {
                    ID           = d.ID,
                    RequestID    = d.RequestID,
                    ItemID       = d.ItemID,
                    ItemCode     = im.Code ?? "",
                    ItemName     = im.Name,
                    UOMID        = d.UOMID,
                    UOMName      = um != null ? um.Name : "",
                    RequestedQty = d.RequestedQty,
                    BuyingPrice  = d.BuyingPrice,
                    ClientID     = d.ClientID,
                    ClientName   = cl != null ? cl.Name : "",
                    Remarks      = d.Remarks
                }
            ).ToListAsync();
        }

        // ── GetBSRList ────────────────────────────────────────────────────────

        public async Task<List<StockRequestListDto>> GetBSRList()
        {
            var list = await BSRListQuery().ToListAsync();
            // Line items are not loaded for the list view (performance).
            return list;
        }

        // ── GetBSRByID ────────────────────────────────────────────────────────

        public async Task<StockRequestListDto?> GetBSRByID(int id)
        {
            var record = await BSRListQuery().FirstOrDefaultAsync(x => x.ID == id);
            if (record == null) return null;
            record.Details = await GetBSRDetails(id);
            // Populate AvailableStock per line
            foreach (var line in record.Details)
                line.AvailableStock = await GetCurrentStockInternal(record.SourceWarehouseID, line.ItemID);
            return record;
        }

        // ── SaveBSR ───────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message, StockRequest? Record)> SaveBSR(
            StockRequestSaveDto dto, string currentUser)
        {
            // ── Validate header ────────────────────────────────────────────────
            if (dto.RequestingBranchID <= 0)
                return (false, "Please select the requesting branch.", null);
            if (dto.SourceWarehouseID <= 0)
                return (false, "Please select the source warehouse.", null);
            if (dto.RequestDate == default)
                return (false, "Please enter the request date.", null);
            if (!dto.Details.Any())
                return (false, "Please add at least one item to the request.", null);

            // ── Validate line items ────────────────────────────────────────────
            var seenItems = new HashSet<int>();
            foreach (var line in dto.Details)
            {
                if (line.ItemID <= 0)
                    return (false, "Please select an item for each line.", null);
                if (line.RequestedQty <= 0)
                    return (false, "Requested quantity must be greater than zero.", null);
                if (!seenItems.Add(line.ItemID))
                {
                    var dupItem = await _oBMSDbContext.ItemMasters.FindAsync(line.ItemID);
                    return (false,
                        $"Item '{dupItem?.Name ?? $"ID {line.ItemID}"}' appears more than once. " +
                        "Please combine duplicate lines.", null);
                }

                // NOTE: Stock availability is intentionally NOT validated here.
                // A Branch Stock Request is a request TO the main warehouse asking
                // for stock. The branch does not need to know the warehouse balance.
                // The approver (warehouse manager) decides whether to fulfil the request.
                // Stock is only deducted when the actual Stock Transfer is created.
            }

            // ── Editing guard ──────────────────────────────────────────────────
            if (dto.ID > 0)
            {
                var existing = await _oBMSDbContext.StockRequests.FindAsync(dto.ID);
                if (existing != null && existing.Status != "Draft")
                    return (false,
                        $"This stock request cannot be edited because it has already been " +
                        $"{existing.Status.ToLower()}. Only Draft requests can be modified.", null);
            }

            // ── Generate request number ────────────────────────────────────────
            string requestNo = dto.RequestNo ?? "";
            if (string.IsNullOrWhiteSpace(requestNo))
            {
                var maxRow = await _oBMSDbContext.StockRequests
                    .Where(x => x.RequestNo.StartsWith("SR"))
                    .OrderByDescending(x => x.RequestNo)
                    .FirstOrDefaultAsync();
                requestNo = (maxRow == null || string.IsNullOrEmpty(maxRow.RequestNo))
                    ? "SR0001"
                    : int.TryParse(maxRow.RequestNo.Substring(2), out int n)
                        ? "SR" + (n + 1).ToString("D4") : "SR0001";
            }

            StockRequest? saved = null;
            await _oBMSDbContext.SaveChangesAsync(); // flush pending tracking state

            if (dto.ID > 0)
            {
                var record = await _oBMSDbContext.StockRequests.FindAsync(dto.ID)
                             ?? throw new InvalidOperationException("Stock request not found.");
                // Remove old details
                var oldDetails = await _oBMSDbContext.StockRequestDetails
                    .Where(d => d.RequestID == dto.ID).ToListAsync();
                _oBMSDbContext.StockRequestDetails.RemoveRange(oldDetails);
                await _oBMSDbContext.SaveChangesAsync();

                record.RequestDate         = dto.RequestDate;
                record.RequestingBranchID  = dto.RequestingBranchID;
                record.SourceWarehouseID   = dto.SourceWarehouseID;
                record.Remarks             = dto.Remarks;
                record.LASTUPDATE          = DateTime.Now;
                record.LastUpdatedBy       = currentUser;
                await _oBMSDbContext.SaveChangesAsync();
                saved = record;
            }
            else
            {
                var record = new StockRequest
                {
                    RequestNo            = requestNo,
                    RequestDate          = dto.RequestDate,
                    RequestingBranchID   = dto.RequestingBranchID,
                    SourceWarehouseID    = dto.SourceWarehouseID,
                    RequestedBy          = currentUser,
                    Status               = "Draft",
                    Remarks              = dto.Remarks,
                    IsActive             = true,
                    LASTUPDATE           = DateTime.Now,
                    LastUpdatedBy        = currentUser
                };
                _oBMSDbContext.StockRequests.Add(record);
                await _oBMSDbContext.SaveChangesAsync();
                saved = record;
            }

            // Add new detail lines
            foreach (var line in dto.Details)
            {
                _oBMSDbContext.StockRequestDetails.Add(new StockRequestDetail
                {
                    RequestID     = saved!.ID,
                    ItemID        = line.ItemID,
                    UOMID         = line.UOMID,
                    RequestedQty  = line.RequestedQty,
                    BuyingPrice   = line.BuyingPrice,
                    ClientID      = line.ClientID > 0 ? line.ClientID : null,
                    Remarks       = line.Remarks,
                    LASTUPDATE    = DateTime.Now,
                    LastUpdatedBy = currentUser
                });
            }
            await _oBMSDbContext.SaveChangesAsync();

            return (true, $"Stock Request {saved!.RequestNo} saved successfully.", saved);
        }

        // ── SubmitBSR ─────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> SubmitBSR(int id, string currentUser)
        {
            var sr = await _oBMSDbContext.StockRequests.FindAsync(id);
            if (sr == null)
                return (false, "This stock request could not be found.");
            if (sr.Status != "Draft")
                return (false,
                    $"Only a Draft stock request can be submitted. " +
                    $"This request is currently {sr.Status}.");

            var hasDetails = await _oBMSDbContext.StockRequestDetails.AnyAsync(x => x.RequestID == id);
            if (!hasDetails)
                return (false, "Please add at least one item to the request before submitting.");

            if (_approval != null)
            {
                // BUG-06 FIX: ApprovalConfiguration is now the master on/off switch.
                // If ApprovalRequired = false for BRANCH_STOCK_REQUEST → direct-approve flow
                // regardless of any ApprovalRouting configuration.
                bool approvalRequired = await _approval.IsApprovalRequired("BRANCH_STOCK_REQUEST");
                if (!approvalRequired)
                {
                    sr.Status        = "Approved";
                    sr.ApprovedBy    = currentUser;
                    sr.ApprovedDate  = DateTime.Now;
                    sr.SubmittedBy   = currentUser;
                    sr.SubmittedDate = DateTime.Now;
                    sr.LASTUPDATE    = DateTime.Now;
                    sr.LastUpdatedBy = currentUser;
                    await _oBMSDbContext.SaveChangesAsync();
                    return (true,
                        $"Stock Request {sr.RequestNo} has been approved directly. " +
                        "You can now create a Stock Transfer.");
                }

                var approverUser = await _approval.GetApproverForUser(currentUser, "BRANCH_STOCK_REQUEST");

                if (string.IsNullOrEmpty(approverUser))
                {
                    // ── No routing configured → direct-approve flow ────────────
                    sr.Status        = "Approved";
                    sr.ApprovedBy    = currentUser;
                    sr.ApprovedDate  = DateTime.Now;
                    sr.SubmittedBy   = currentUser;
                    sr.SubmittedDate = DateTime.Now;
                    sr.LASTUPDATE    = DateTime.Now;
                    sr.LastUpdatedBy = currentUser;
                    await _oBMSDbContext.SaveChangesAsync();
                    return (true,
                        $"Stock Request {sr.RequestNo} has been approved directly. " +
                        "You can now create a Stock Transfer.");
                }

                string prev  = sr.Status;
                sr.Status        = "Submitted";
                sr.SubmittedBy   = currentUser;
                sr.SubmittedDate = DateTime.Now;
                sr.LASTUPDATE    = DateTime.Now;
                sr.LastUpdatedBy = currentUser;
                await _oBMSDbContext.SaveChangesAsync();

                // Record history
                await _approval.RecordHistory("BRANCH_STOCK_REQUEST", sr.ID, sr.RequestNo,
                    "Submitted", currentUser, prev, "Submitted", null);

                // Notify approver
                if (_notif != null)
                    await _notif.SendApprovalNotification(
                        "PENDING_APPROVAL", "BRANCH_STOCK_REQUEST", sr.RequestNo, sr.ID,
                        approverUser, currentUser, null);
            }
            else
            {
                // Fallback — no approval service injected
                sr.Status        = "Submitted";
                sr.SubmittedBy   = currentUser;
                sr.SubmittedDate = DateTime.Now;
                sr.LASTUPDATE    = DateTime.Now;
                sr.LastUpdatedBy = currentUser;
                await _oBMSDbContext.SaveChangesAsync();
            }

            return (true, $"Stock Request {sr.RequestNo} has been submitted for approval.");
        }

        // ── ApproveBSR ────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> ApproveBSR(int id, StockRequestApprovalDto dto)
        {
            var sr = await _oBMSDbContext.StockRequests.FindAsync(id);
            if (sr == null)
                return (false, "This stock request could not be found.");
            if (sr.Status != "Submitted")
                return (false, "Only a Submitted stock request can be approved.");

            var submitter = sr.SubmittedBy ?? sr.LastUpdatedBy ?? string.Empty;

            // BUG-05 FIX: isSuperAdmin is derived from the JWT "IsAdmin" claim value "2",
            // passed in via StockRequestApprovalDto.IsSuperAdmin. No more hard-coded "superadmin" string.
            bool isSuperAdmin   = dto.IsSuperAdmin;

            // SuperAdmin has global approval authority — self-approval block does not apply.
            // Normal users: cannot approve their own submission.
            bool isSelfApproval = !isSuperAdmin &&
                                  !string.IsNullOrWhiteSpace(submitter) &&
                                  string.Equals(dto.ApprovedBy, submitter,
                                                StringComparison.OrdinalIgnoreCase);
            if (isSelfApproval)
                return (false, "You cannot approve a document that you submitted yourself.");

            if (!isSuperAdmin && _approval != null)
            {
                var authorized = await _approval.IsAuthorizedApprover(
                    dto.ApprovedBy, submitter, "BRANCH_STOCK_REQUEST");
                if (!authorized)
                    return (false, "You are not authorized to approve this stock request.");
            }

            string prev        = sr.Status;
            sr.Status          = "Approved";
            sr.ApprovedBy      = dto.ApprovedBy;
            sr.ApprovedDate    = DateTime.Now;
            sr.ApprovalRemarks = dto.ApprovalRemarks;
            sr.LASTUPDATE      = DateTime.Now;
            sr.LastUpdatedBy   = dto.ApprovedBy;
            await _oBMSDbContext.SaveChangesAsync();

            if (_approval != null)
                await _approval.RecordHistory("BRANCH_STOCK_REQUEST", sr.ID, sr.RequestNo,
                    "Approved", dto.ApprovedBy, prev, "Approved", dto.ApprovalRemarks);

            if (_notif != null && !string.IsNullOrEmpty(sr.SubmittedBy))
                await _notif.SendApprovalNotification(
                    "APPROVED", "BRANCH_STOCK_REQUEST", sr.RequestNo, sr.ID,
                    sr.SubmittedBy, dto.ApprovedBy, dto.ApprovalRemarks);

            // Notify Purchase department — they need to prepare and dispatch the stock transfer
            if (_notif != null)
                await _notif.SendApprovalNotificationToDepartment(
                    "APPROVED", "BRANCH_STOCK_REQUEST", sr.RequestNo, sr.ID,
                    "Purchase", dto.ApprovedBy, dto.ApprovalRemarks,
                    excludeUsers: new[] { sr.SubmittedBy ?? string.Empty });

            return (true,
                $"Stock Request {sr.RequestNo} has been approved. " +
                "The requester can now create a Stock Transfer.");
        }

        // ── RejectBSR ─────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> RejectBSR(int id, StockRequestApprovalDto dto)
        {
            var sr = await _oBMSDbContext.StockRequests.FindAsync(id);
            if (sr == null)
                return (false, "This stock request could not be found.");
            if (sr.Status != "Submitted")
                return (false, "Only a Submitted stock request can be rejected.");
            if (string.IsNullOrWhiteSpace(dto.ApprovalRemarks))
                return (false,
                    "Please enter a reason for rejection so the requester knows what to correct.");

            var submitter = sr.SubmittedBy ?? sr.LastUpdatedBy ?? string.Empty;

            // BUG-05 FIX: isSuperAdmin derived from JWT "IsAdmin" claim, not username string.
            bool isSuperAdmin    = dto.IsSuperAdmin;

            // SuperAdmin has global authority — self-rejection block does not apply.
            // Normal users: cannot reject their own submission.
            bool isSelfRejection = !isSuperAdmin &&
                                   !string.IsNullOrWhiteSpace(submitter) &&
                                   string.Equals(dto.ApprovedBy, submitter,
                                                 StringComparison.OrdinalIgnoreCase);
            if (isSelfRejection)
                return (false, "You cannot reject a document that you submitted yourself.");

            if (!isSuperAdmin && _approval != null)
            {
                var authorized = await _approval.IsAuthorizedApprover(
                    dto.ApprovedBy, submitter, "BRANCH_STOCK_REQUEST");
                if (!authorized)
                    return (false, "You are not authorized to reject this stock request.");
            }

            string prev        = sr.Status;
            sr.Status          = "Rejected";
            sr.ApprovedBy      = dto.ApprovedBy;
            sr.ApprovedDate    = DateTime.Now;
            sr.ApprovalRemarks = dto.ApprovalRemarks;
            sr.LASTUPDATE      = DateTime.Now;
            sr.LastUpdatedBy   = dto.ApprovedBy;
            await _oBMSDbContext.SaveChangesAsync();

            if (_approval != null)
                await _approval.RecordHistory("BRANCH_STOCK_REQUEST", sr.ID, sr.RequestNo,
                    "Rejected", dto.ApprovedBy, prev, "Rejected", dto.ApprovalRemarks);

            if (_notif != null && !string.IsNullOrEmpty(sr.SubmittedBy))
                await _notif.SendApprovalNotification(
                    "REJECTED", "BRANCH_STOCK_REQUEST", sr.RequestNo, sr.ID,
                    sr.SubmittedBy, dto.ApprovedBy, dto.ApprovalRemarks);

            return (true, $"Stock Request {sr.RequestNo} has been rejected.");
        }

        // ── DeleteBSR ─────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> DeleteBSR(int id, string currentUser)
        {
            var sr = await _oBMSDbContext.StockRequests.FindAsync(id);
            if (sr == null)
                return (false, "This stock request could not be found.");
            if (sr.Status != "Draft")
                return (false,
                    $"This stock request cannot be deleted because it has already been " +
                    $"{sr.Status.ToLower()}. Only Draft requests can be deleted.");

            sr.IsActive      = false;
            sr.LASTUPDATE    = DateTime.Now;
            sr.LastUpdatedBy = currentUser;
            await _oBMSDbContext.SaveChangesAsync();
            return (true, "Stock request removed successfully.");
        }

        // ── LinkBSRTransfer ───────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> LinkBSRTransfer(
            int id, int transferID, string currentUser)
        {
            var sr = await _oBMSDbContext.StockRequests.FindAsync(id);
            if (sr == null)
                return (false, "Stock request not found.");
            if (sr.Status != "Approved")
                return (false,
                    "Only an Approved stock request can be linked to a transfer.");

            var trf = await _oBMSDbContext.StockTransfers.FindAsync(transferID);
            if (trf == null)
                return (false, "The specified stock transfer could not be found.");

            sr.LinkedTransferID = transferID;
            sr.LASTUPDATE       = DateTime.Now;
            sr.LastUpdatedBy    = currentUser;
            await _oBMSDbContext.SaveChangesAsync();

            return (true,
                $"Stock Transfer {trf.TransferNo} has been linked to " +
                $"Stock Request {sr.RequestNo}.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // Stock Issue → Uniform Loan integration
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns active employees/guards for the given warehouse's branch,
        /// filtered by issueToType:
        ///   "Guard" → EMP_ROLE IN ('Guard', 'FGuard', 'Foreign Guard')
        ///   "Staff" → EMP_ROLE = 'Staff'
        /// Only employees with no resign date (or resign date in the future) are returned.
        /// </summary>
        public async Task<List<EmployeeForIssueDto>> GetEmployeesByWarehouse(
            int warehouseID, string issueToType)
        {
            // Resolve the branch code from the warehouse
            var warehouse = await _oBMSDbContext.WarehouseMasters
                .Include(w => w.Branch)
                .FirstOrDefaultAsync(w => w.ID == warehouseID && w.IsActive);

            if (warehouse?.Branch == null)
                return new List<EmployeeForIssueDto>();

            string branchCode = warehouse.Branch.Code;

            // Determine role filter
            IQueryable<Employee> query = _oBMSDbContext.Employees
                .Where(e => e.EMP_BRANCH_CODE == branchCode && !e.HasTransfered);

            if (issueToType.Equals("Guard", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e =>
                    e.EMP_ROLE == "Guard" ||
                    e.EMP_ROLE == "FGuard" ||
                    e.EMP_ROLE == "Foreign Guard");
            }
            else if (issueToType.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.EMP_ROLE == "Staff");
            }

            // Join EmploymentDetails to filter out resigned employees
            var today = DateTime.Today;
            var result = await (
                from emp in query
                join ed in _oBMSDbContext.EmploymentDetails
                    on emp.EMP_CODE equals ed.EMPPAY_CODE
                where ed.EMPPAY_DATE_RESIGNED == null ||
                      ed.EMPPAY_DATE_RESIGNED >= today
                orderby emp.EMP_NAME
                select new EmployeeForIssueDto
                {
                    EMP_ID          = emp.EMP_ID,
                    EMP_CODE        = emp.EMP_CODE,
                    EMP_NAME        = emp.EMP_NAME,
                    EMP_ROLE        = emp.EMP_ROLE,
                    EMP_BRANCH_CODE = emp.EMP_BRANCH_CODE
                }
            ).ToListAsync();

            return result;
        }

        /// <summary>
        /// Returns all StockIssueDocs issued to the given employee (IssueToID = employeeID,
        /// IssueToType IN ('Guard','Staff')) that are NOT already linked to an active
        /// Uniform Loan (StockIssueUniformLoanLink.IsActive = true).
        /// </summary>
        public async Task<List<PendingStockIssueDto>> GetPendingStockIssuesForEmployee(
            int employeeID)
        {
            // IDs of already-linked active documents
            var linkedIDs = await _oBMSDbContext.StockIssueUniformLoanLinks
                .Where(l => l.IsActive)
                .Select(l => l.StockIssueDocID)
                .ToListAsync();

            var docs = await _oBMSDbContext.StockIssueDocs
                .Where(d => d.IsActive
                    && (d.IssueToType == "Guard" || d.IssueToType == "Staff")
                    && d.IssueToID == employeeID
                    && !linkedIDs.Contains(d.ID))
                .OrderByDescending(d => d.IssueDate)
                .ToListAsync();

            if (!docs.Any())
                return new List<PendingStockIssueDto>();

            var docIDs = docs.Select(d => d.ID).ToList();

            // Fetch all detail lines + item names in one query
            var details = await (
                from det in _oBMSDbContext.StockIssueDocDetails
                join item in _oBMSDbContext.ItemMasters on det.ItemID equals item.ID
                where docIDs.Contains(det.IssueDocID)
                select new
                {
                    det.IssueDocID,
                    det.ID,
                    det.ItemID,
                    ItemName = item.Name,
                    ItemCode = item.Code,
                    det.IssueQty,
                    det.UnitCost
                }
            ).ToListAsync();

            // Fetch warehouse names
            var warehouseIDs = docs.Select(d => d.WarehouseID).Distinct().ToList();
            var warehouses = await _oBMSDbContext.WarehouseMasters
                .Where(w => warehouseIDs.Contains(w.ID))
                .ToDictionaryAsync(w => w.ID, w => w.Name);

            var result = docs.Select(doc =>
            {
                var lines = details
                    .Where(d => d.IssueDocID == doc.ID)
                    .Select(d => new PendingStockIssueLineDto
                    {
                        IssueDetailID = d.ID,
                        ItemID        = d.ItemID,
                        ItemName      = d.ItemName,
                        ItemCode      = d.ItemCode,
                        IssueQty      = d.IssueQty,
                        UnitCost      = d.UnitCost,
                        TotalCost     = d.IssueQty * d.UnitCost
                    }).ToList();

                return new PendingStockIssueDto
                {
                    IssueDocID    = doc.ID,
                    IssueNo       = doc.IssueNo,
                    IssueDate     = doc.IssueDate,
                    WarehouseName = warehouses.TryGetValue(doc.WarehouseID, out var wn) ? wn : "",
                    TotalValue    = lines.Sum(l => l.TotalCost),
                    Lines         = lines
                };
            }).ToList();

            return result;
        }

        /// <summary>
        /// Creates a StockIssueUniformLoanLink to permanently associate a Stock Issue
        /// with a Uniform Loan. Enforces that no other active link exists for the same
        /// StockIssueDoc. Safe to call on loan update — returns success silently if the
        /// exact same link already exists.
        /// </summary>
        public async Task<(bool Success, string Message)> LinkStockIssueToUniformLoan(
            LinkStockIssueToLoanRequestDto dto)
        {
            if (dto.StockIssueDocID <= 0)
                return (false, "Invalid Stock Issue reference.");
            if (dto.SalaryAdvanceID <= 0)
                return (false, "Invalid Uniform Loan reference.");

            // Check if an active link already exists for this doc
            var existing = await _oBMSDbContext.StockIssueUniformLoanLinks
                .FirstOrDefaultAsync(l => l.StockIssueDocID == dto.StockIssueDocID && l.IsActive);

            if (existing != null)
            {
                // If it points to the same loan, it's a no-op (idempotent on update)
                if (existing.SalaryAdvanceID == dto.SalaryAdvanceID)
                    return (true, "Stock Issue is already linked to this Uniform Loan.");

                return (false,
                    "This Stock Issue is already linked to another active Uniform Loan. " +
                    "Please delete the existing loan link first.");
            }

            // Validate the StockIssueDoc exists
            var doc = await _oBMSDbContext.StockIssueDocs.FindAsync(dto.StockIssueDocID);
            if (doc == null || !doc.IsActive)
                return (false, "The referenced Stock Issue document was not found or is inactive.");

            // Validate the SalaryAdvance exists and is TransType=4
            var advance = await _oBMSDbContext.SalaryAdvances.FindAsync(dto.SalaryAdvanceID);
            if (advance == null || advance.IsDeleted)
                return (false, "The referenced Uniform Loan record was not found or has been deleted.");
            if (advance.TransType != 4)
                return (false, "The referenced advance is not a Uniform Loan (TransType must be 4).");

            _oBMSDbContext.StockIssueUniformLoanLinks.Add(new StockIssueUniformLoanLink
            {
                StockIssueDocID = dto.StockIssueDocID,
                SalaryAdvanceID = dto.SalaryAdvanceID,
                LinkedDate      = DateTime.Now,
                LinkedBy        = dto.LinkedBy,
                IsActive        = true,
                LASTUPDATE      = DateTime.Now,
                LastUpdatedBy   = dto.LinkedBy
            });
            await _oBMSDbContext.SaveChangesAsync();

            return (true, "Stock Issue successfully linked to Uniform Loan.");
        }
    }
}
