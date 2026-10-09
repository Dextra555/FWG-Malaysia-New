namespace OBMS.WebAPI.Models.DTO
{
    public class AssetDisposalRequestDto
    {
        public int ID { get; set; }
        public string? DisposalNo { get; set; }
        public int AssetID { get; set; }
        public DateTime DisposalDate { get; set; }

        /// <summary>Sold | Scrapped | Donated | Written_Off</summary>
        public string DisposalType { get; set; } = string.Empty;

        public string DisposalReason { get; set; } = string.Empty;
        public decimal? DisposalValue { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class AssetDisposalListDto
    {
        public int ID { get; set; }
        public string DisposalNo { get; set; } = string.Empty;
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public DateTime DisposalDate { get; set; }
        public string DisposalType { get; set; } = string.Empty;
        public string DisposalReason { get; set; } = string.Empty;
        public decimal? DisposalValue { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        // ── Approval workflow ────────────────────────────────────────────
        public string Status { get; set; } = "Draft";
        public string? SubmittedBy     { get; set; }
        public DateTime? SubmittedDate  { get; set; }
        public DateTime? ApprovedDate   { get; set; }
        public string? ApprovalRemarks { get; set; }
    }

    /// <summary>
    /// Payload for ApproveAD / RejectAD endpoints.
    /// ApprovedBy and IsSuperAdmin are ALWAYS overwritten server-side from JWT.
    /// </summary>
    public class ADApprovalDto
    {
        public string ApprovedBy   { get; set; } = string.Empty;
        public bool   IsSuperAdmin { get; set; }
        public string? ApprovalRemarks { get; set; }
    }
}
