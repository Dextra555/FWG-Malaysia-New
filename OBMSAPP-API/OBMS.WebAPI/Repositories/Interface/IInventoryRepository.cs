using OBMS.WebAPI.Models.Domain;
using Microsoft.AspNetCore.Mvc;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IInventoryRepository
    {
        Task<InventoryCategory> SaveAndUpdateCategory(InventoryCategory category);

        Task<List<Object>> GetCategories();

        Task<ActionResult<InventoryCategory>> GetCategoryByID(int ID);

        Task<List<Object>> GetCategoriesByCat(string cat);

        Task<ItemMaster> SaveAndUpdateItem(ItemMaster item);

        Task<List<Object>> GetItems();

        Task<Dictionary<string, Object>> GetItemByID(int ID);

        Task<AssetMaster> SaveAndUpdateAssetMaster(AssetMaster item);

        Task<List<Object>> GetAssetMasters();

        Task<AssetMaster> GetAssetByID(int ID);

        Task<Dictionary<string, Object>> GetSupplierMaster(int ID);

        Task<Dictionary<string, Object>> GetSupplierCode();

        Task<Supplier> SaveAndUpdateSupplier(Supplier obj);

        Task<List<Object>> GetSuppliers();

        Task<Supplier> GetSupplierByID(int ID);

        Task<Dictionary<string, Object>> GetRecipientMaster();

        Task<Recipient> SaveAndUpdateRecipient(Recipient obj);

        Task<List<Object>> GetRecipients();

        Task<Recipient> GetRecipientByID(int ID);

        Task<Dictionary<string, Object>> GetUtilityBillsMasterList();

        Task<Dictionary<string, Object>> GetPaytoByCategory(int ID);

        Task<Dictionary<string, Object>> GetUtilityMasterList(string invCat, string supplierCat, string userID);

        Task<CreditorInvoice> SaveAndUpdateUtility(CreditorInvoice creditorInvoice);

        Task<CreditorInvoiceDetails> SaveAndUpdateUtilityDetails(CreditorInvoiceDetails creditorInvoiceDetails);

        Task<Dictionary<string, Object>> GetMaterialMasterList(string userID);

        Task<StockIssues> SaveAndUpdateMaterial(StockIssues stockIssues);

        Task<StockIssueDetail> SaveAndUpdateMaterialDetails(StockIssueDetail stockIssueDetail);

        Task<bool> DeleteItemAsync(int id, string currentUser);

        bool DeleteAssetType(int id);

        bool DeleteRecipientAsync(int id);

        Task<bool> DeleteCategoryAsync(int id, string currentUser);

        bool DeleteSupplierAsync(int id);
    }
}
