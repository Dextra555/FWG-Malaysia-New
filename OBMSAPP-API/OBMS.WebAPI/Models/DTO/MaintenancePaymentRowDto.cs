namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Shape returned by GetMaintenanceForPayment endpoint —
    /// mirrors IAssetInstallmentPaymentRow on the Angular side.
    /// </summary>
    public class MaintenancePaymentRowDto
    {
        public int    MaintenanceID    { get; set; }
        public int    PaymentDetailID  { get; set; }   // 0 when not yet linked to a payment
        public int    PaymentID        { get; set; }
        public string MaintenanceRef   { get; set; } = string.Empty;  // e.g. AM0001
        public int    AssetID          { get; set; }
        public string AssetCode        { get; set; } = string.Empty;
        public string AssetName        { get; set; } = string.Empty;
        public string VendorName       { get; set; } = string.Empty;
        public DateTime StartDate      { get; set; }
        public decimal TotalAmount     { get; set; }
        public decimal PaidAmount      { get; set; }
        public decimal Balance         => TotalAmount - PaidAmount;
        public decimal Amount          { get; set; }   // current payment amount
    }
}
