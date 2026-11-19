namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Single row from the Excel import for Opening Stock bulk upload.
    /// ItemCode and WarehouseCode are resolved server-side to IDs.
    /// </summary>
    public class BulkOpeningStockRowDto
    {
        public int RowNumber { get; set; }
        public string? ItemCode { get; set; }
        public string? WarehouseCode { get; set; }
        public string? OpeningDate { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal UnitCost { get; set; }
        public string? Remarks { get; set; }
    }

    public class BulkOpeningStockRequestDto
    {
        public List<BulkOpeningStockRowDto> Rows { get; set; } = new();
        public string? LastUpdatedBy { get; set; }
    }

    public class BulkOpeningStockResultDto
    {
        public int RowNumber { get; set; }
        public string? ItemCode { get; set; }
        public string? WarehouseCode { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ReferenceNo { get; set; }
    }
}
