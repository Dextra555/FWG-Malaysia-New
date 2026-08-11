using OBMS.WebAPI.Models.Domain;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface ICreditDebitNoteRepository
    {
        // Credit Note
        Task<List<CreditNote>> GetCreditNotes(string? branch, string? client, int? agreementID);
        Task<CreditNote?> GetCreditNoteById(int id);
        Task<CreditNote> SaveAndUpdateCreditNote(CreditNote creditNote);
        Task<bool> DeleteCreditNote(int id, string currentUser);
        Task<string> GenerateCreditNoteNo(string branch, DateTime date);

        // Debit Note
        Task<List<DebitNote>> GetDebitNotes(string? branch, string? client, int? agreementID);
        Task<DebitNote?> GetDebitNoteById(int id);
        Task<DebitNote> SaveAndUpdateDebitNote(DebitNote debitNote);
        Task<bool> DeleteDebitNote(int id, string currentUser);
        Task<string> GenerateDebitNoteNo(string branch, DateTime date);

        // Summary helpers used by receipts
        Task<List<CreditNote>> GetApprovedCreditNotesByClient(string branch, string client);
        Task<List<DebitNote>> GetApprovedDebitNotesByClient(string branch, string client);
    }
}
