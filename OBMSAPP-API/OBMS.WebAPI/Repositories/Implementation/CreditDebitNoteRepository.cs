using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Repositories.Interface;

namespace OBMS.WebAPI.Repositories.Implementation
{
    public class CreditDebitNoteRepository : ICreditDebitNoteRepository
    {
        private readonly OBMSDbContext _context;

        public CreditDebitNoteRepository(OBMSDbContext context)
        {
            _context = context;
        }

        // ─── Credit Note ────────────────────────────────────────────────────────

        public async Task<List<CreditNote>> GetCreditNotes(string? branch, string? client, int? agreementID)
        {
            var query = _context.CreditNotes
                .Where(cn => cn.IsDeleted == false);

            if (!string.IsNullOrEmpty(branch) && branch != "0")
                query = query.Where(cn => cn.Branch == branch);

            if (!string.IsNullOrEmpty(client))
                query = query.Where(cn => cn.Client == client);

            var list = await query.OrderByDescending(cn => cn.CreditNoteNo).ToListAsync();

            // Attach ClientName from ClientMaster
            var clientCodes = list.Select(x => x.Client).Distinct().ToList();
            var clientNames = await _context.ClientMasters
                .Where(cm => clientCodes.Contains(cm.Code))
                .ToDictionaryAsync(cm => cm.Code, cm => cm.Name);

            foreach (var cn in list)
                cn.ClientName = clientNames.TryGetValue(cn.Client, out var name) ? name : cn.Client;

            return list;
        }

        public async Task<CreditNote?> GetCreditNoteById(int id)
        {
            return await _context.CreditNotes
                .FirstOrDefaultAsync(cn => cn.ID == id && cn.IsDeleted == false);
        }

        public async Task<CreditNote> SaveAndUpdateCreditNote(CreditNote creditNote)
        {
            if (creditNote.ID == 0)
            {
                // New record
                creditNote.CreatedDate = DateTime.Now;
                creditNote.LastUpdatedDate = DateTime.Now;
                await _context.CreditNotes.AddAsync(creditNote);
            }
            else
            {
                // Update existing
                var existing = await _context.CreditNotes.FindAsync(creditNote.ID);
                if (existing == null)
                    throw new KeyNotFoundException($"CreditNote ID {creditNote.ID} not found.");

                existing.Branch = creditNote.Branch;
                existing.Client = creditNote.Client;
                existing.ClientInvoiceID = creditNote.ClientInvoiceID;
                existing.CreditNoteDate = creditNote.CreditNoteDate;
                existing.CreditNoteAmount = creditNote.CreditNoteAmount;
                existing.TaxPercentage = creditNote.TaxPercentage;
                existing.TaxAmount = creditNote.TaxAmount;
                existing.TotalAmount = creditNote.TotalAmount;
                existing.Reason = creditNote.Reason;
                existing.ReferenceInvoiceNo = creditNote.ReferenceInvoiceNo;
                existing.LastUpdatedDate = DateTime.Now;
                existing.LastUpdatedBy = creditNote.LastUpdatedBy;

                _context.CreditNotes.Update(existing);
            }

            await _context.SaveChangesAsync();
            return creditNote;
        }

        public async Task<bool> DeleteCreditNote(int id, string currentUser)
        {
            var creditNote = await _context.CreditNotes.FindAsync(id);
            if (creditNote == null) return false;

            creditNote.IsDeleted = true;
            creditNote.LastUpdatedDate = DateTime.Now;
            creditNote.LastUpdatedBy = currentUser;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateCreditNoteNo(string branch, DateTime date)
        {
            // Format: CN001/MM/YY
            // Sequential number resets each YEAR — continues across months within same year
            // e.g. CN001/01/26 → CN002/02/26 → CN003/06/26 → CN001/01/27 (new year resets)
            string month = date.ToString("MM");
            string year  = date.ToString("yy");
            string suffix = $"/{month}/{year}";

            // Find max sequence number used THIS YEAR only
            string yearSuffix = $"/{year}"; // matches any "CN###/MM/yy"

            var allNosThisYear = await _context.CreditNotes
                .Where(cn => cn.CreditNoteNo.StartsWith("CN")
                          && cn.CreditNoteNo.EndsWith(yearSuffix.Substring(0)) // any month same year
                          && cn.IsDeleted == false)
                .Select(cn => cn.CreditNoteNo)
                .ToListAsync();

            // Filter to only records where last 3 chars match the year (e.g. "/26")
            int maxSeq = 0;
            foreach (var no in allNosThisYear)
            {
                // e.g. CN003/06/26 — last 3 chars = "/26", extract "003"
                if (no.Length >= 8 && no.Substring(no.Length - 3) == $"/{year}")
                {
                    var numPart = no.Substring(2, no.IndexOf('/') - 2);
                    if (int.TryParse(numPart, out int seq) && seq > maxSeq)
                        maxSeq = seq;
                }
            }

            return $"CN{(maxSeq + 1):D3}{suffix}";
        }

        // ─── Debit Note ─────────────────────────────────────────────────────────

        public async Task<List<DebitNote>> GetDebitNotes(string? branch, string? client, int? agreementID)
        {
            var query = _context.DebitNotes
                .Where(dn => dn.IsDeleted == false);

            if (!string.IsNullOrEmpty(branch) && branch != "0")
                query = query.Where(dn => dn.Branch == branch);

            if (!string.IsNullOrEmpty(client))
                query = query.Where(dn => dn.Client == client);

            var list = await query.OrderByDescending(dn => dn.DebitNoteDate).ToListAsync();

            // Attach ClientName from ClientMaster
            var clientCodes = list.Select(x => x.Client).Distinct().ToList();
            var clientNames = await _context.ClientMasters
                .Where(cm => clientCodes.Contains(cm.Code))
                .ToDictionaryAsync(cm => cm.Code, cm => cm.Name);

            foreach (var dn in list)
                dn.ClientName = clientNames.TryGetValue(dn.Client, out var name) ? name : dn.Client;

            return list;
        }

        public async Task<DebitNote?> GetDebitNoteById(int id)
        {
            return await _context.DebitNotes
                .FirstOrDefaultAsync(dn => dn.ID == id && dn.IsDeleted == false);
        }

        public async Task<DebitNote> SaveAndUpdateDebitNote(DebitNote debitNote)
        {
            if (debitNote.ID == 0)
            {
                debitNote.CreatedDate = DateTime.Now;
                debitNote.LastUpdatedDate = DateTime.Now;
                await _context.DebitNotes.AddAsync(debitNote);
            }
            else
            {
                var existing = await _context.DebitNotes.FindAsync(debitNote.ID);
                if (existing == null)
                    throw new KeyNotFoundException($"DebitNote ID {debitNote.ID} not found.");

                existing.Branch = debitNote.Branch;
                existing.Client = debitNote.Client;
                existing.ClientInvoiceID = debitNote.ClientInvoiceID;
                existing.DebitNoteDate = debitNote.DebitNoteDate;
                existing.DebitNoteAmount = debitNote.DebitNoteAmount;
                existing.TaxPercentage = debitNote.TaxPercentage;
                existing.TaxAmount = debitNote.TaxAmount;
                existing.TotalAmount = debitNote.TotalAmount;
                existing.Reason = debitNote.Reason;
                existing.ReferenceInvoiceNo = debitNote.ReferenceInvoiceNo;
                existing.DueDate = debitNote.DueDate;
                existing.LastUpdatedDate = DateTime.Now;
                existing.LastUpdatedBy = debitNote.LastUpdatedBy;

                _context.DebitNotes.Update(existing);
            }

            await _context.SaveChangesAsync();
            return debitNote;
        }

        public async Task<bool> DeleteDebitNote(int id, string currentUser)
        {
            var debitNote = await _context.DebitNotes.FindAsync(id);
            if (debitNote == null) return false;

            debitNote.IsDeleted = true;
            debitNote.LastUpdatedDate = DateTime.Now;
            debitNote.LastUpdatedBy = currentUser;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateDebitNoteNo(string branch, DateTime date)
        {
            // Format: DN001/MM/YY
            // Sequential number resets each YEAR — continues across months within same year
            // e.g. DN001/01/26 → DN002/02/26 → DN003/06/26 → DN001/01/27 (new year resets)
            string month = date.ToString("MM");
            string year  = date.ToString("yy");
            string suffix = $"/{month}/{year}";

            var allNosThisYear = await _context.DebitNotes
                .Where(dn => dn.DebitNoteNo.StartsWith("DN")
                          && dn.IsDeleted == false)
                .Select(dn => dn.DebitNoteNo)
                .ToListAsync();

            int maxSeq = 0;
            foreach (var no in allNosThisYear)
            {
                // e.g. DN003/06/26 — last 3 chars = "/26", extract "003"
                if (no.Length >= 8 && no.Substring(no.Length - 3) == $"/{year}")
                {
                    var numPart = no.Substring(2, no.IndexOf('/') - 2);
                    if (int.TryParse(numPart, out int seq) && seq > maxSeq)
                        maxSeq = seq;
                }
            }

            return $"DN{(maxSeq + 1):D3}{suffix}";
        }

        // ─── Summary helpers ────────────────────────────────────────────────────

        public async Task<List<CreditNote>> GetApprovedCreditNotesByClient(string branch, string client)
        {
            return await _context.CreditNotes
                .Where(cn => cn.Branch == branch
                          && cn.Client == client
                          && cn.IsDeleted == false)
                .OrderByDescending(cn => cn.CreditNoteDate)
                .ToListAsync();
        }

        public async Task<List<DebitNote>> GetApprovedDebitNotesByClient(string branch, string client)
        {
            return await _context.DebitNotes
                .Where(dn => dn.Branch == branch
                          && dn.Client == client
                          && dn.IsDeleted == false)
                .OrderByDescending(dn => dn.DebitNoteDate)
                .ToListAsync();
        }
    }
}
