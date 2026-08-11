namespace OBMS.WebAPI.Models.DTO
{
    public class InventoryCategoryDto
    {
        public int ID { get; set; }
        public int AccountCategoryID { get; set; }
        public string AccountNo { get; set; }
        public int AccountTypeID { get; set; }
        public string Name { get; set; }
        public string Cat { get; set; }
        public string AssetType { get; set; }

        public InventoryCategoryDto(int id, int accountCategoryID, string accountNo, int accountTypeID,string name, string cat, string assetType)
        {
            ID = id;
            AccountCategoryID = accountCategoryID;
            AccountNo = accountNo;
            AccountTypeID = accountTypeID;
            Name = name;
            Cat = cat;
            AssetType = assetType;
            
        }
    }
}
