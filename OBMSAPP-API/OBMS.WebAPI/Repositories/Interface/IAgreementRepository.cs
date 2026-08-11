using OBMS.WebAPI.Models.Domain;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IAgreementRepository
    {

        Task<Dictionary<string, Object>> GetAgreementMasterList(string userID);
        Task<Agreement> saveAndUpdateAgreement(Agreement agreement);

        Task<AgreementDetails> saveAndUpdateAgreementDetails(AgreementDetails agreementDetails);

        Task<Dictionary<string, Object>> GetAgreementByID(int id);

        Task<List<Object>> GetAgreements(string branchId, string userID);

        Task<Object> CheckClientStatus(string branchId,string clientId,string status);

        Task<Object> GetFinalInvoiceDate(int agreementId);

        Task<List<Object>> GetAgreementsDiscountReport(string branchId, string clientId,DateTime startdate ,DateTime endDate);

        Task<Dictionary<string, Object>> GetAgreementListByBranchId(string branchId, string clientId, DateTime terminationDate);

        Task<TerminatedAgreement> SaveAndUpdateAgreementTermination(TerminatedAgreement terminatedAgreement);

        Task<Dictionary<string, Object>> GetAgreementTerminationByID(int id);
    }
}
