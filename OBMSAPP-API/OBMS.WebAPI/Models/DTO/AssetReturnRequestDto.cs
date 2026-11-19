namespace OBMS.WebAPI.Models.DTO
{
    public class AssetReturnRequestDto
    {
        public int ID { get; set; }
        public string? ReturnNo { get; set; }
        public int AssetID { get; set; }
        public int AllocationID { get; set; }
        public DateTime ReturnDate { get; set; }
        public string ReturnedByType { get; set; } = string.Empty;
        public int? ReturnedByID { get; set; }
        public string? ReturnedByName { get; set; }

        /// <summary>Good | Fair | Poor | Damaged</summary>
        public string? ReturnCondition { get; set; }

        public string? ReceivedBy { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class AssetReturnListDto
    {
        public int ID { get; set; }
        public string ReturnNo { get; set; } = string.Empty;
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public string? AllocationNo { get; set; }
        public DateTime ReturnDate { get; set; }
        public string ReturnedByType { get; set; } = string.Empty;
        public string? ReturnedByName { get; set; }
        public string? ReturnCondition { get; set; }
        public string? ReceivedBy { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
