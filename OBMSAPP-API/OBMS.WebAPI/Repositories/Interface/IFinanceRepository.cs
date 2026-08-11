using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IFinanceRepository
    {
        Task<Dictionary<string, Object>> GetInvoiceMaster(string userID);
        Task<Dictionary<string, Object>> GetPaymentMaster(string userID);
        Task<Dictionary<string, Object>> GetReceiptMaster(string userID);
        Task<List<ClientInvoiceList>> GetMonthlyInvoiceList(DateTime invoiceStartPeriod, DateTime invoiceEndPeriod);
        Task<List<Receipts>> GetReceiptsByDateAndBranchAsync(DateTime receiptDate, string branch);
        Task<List<Receipts>> GetReceiptsByBankAndChequeAsync(string bankCode, string chequeNo, string branch);
        Task<ReceiptsRequestDto> GetReceiptAsync(int id);
        Task<List<BranchPayment>> GetListByDateAsync(DateTime paymentDate);
        Task<List<BranchPayment>> GetListByBankAndChequeAsync(decimal bankId, string chequeNo);
        Task<BranchPayment> GetBranchPaymentAsync(int id);
        Task<bool> DeleteAsync(int id, string currentUser);
        Task<bool> ReceiptDeleteAsync(int id, string currentUser);
        Task<List<InventoryCategoryDto>> GetListAsync();
        List<BranchPaymentRequestDto> GetDeletedPaymentsByMonthYear(DateTime paymentDate);
        bool Restore(int id, string currentUser);
        Task<List<ChequeStatusDto>> GetChequeStatusesAsync(DateTime startDate, DateTime endDate, string chequeStatus, string bankCode, string transType);
        Task<List<ClientLegalDemandActionDto>> GetActionsAsync(string branch = null, string client = null, string actionTaken = null);
        Task<bool> DeleteLegalDemandAsync(int id, string currentUser, string deleteRemarks);
        Task<ClientLegalDemandActionDto> GetLegalDemandByID(int ID);
        Task<List<BranchPaymentVoucherSummaryDto>> GetBranchPaymentVoucherSummary(DateTime? startDate, DateTime? endDate, string branch);
    }
}
