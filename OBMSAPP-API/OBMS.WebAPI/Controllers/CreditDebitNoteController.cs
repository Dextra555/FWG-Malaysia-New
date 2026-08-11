using Microsoft.AspNetCore.Mvc;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;

namespace OBMS.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreditDebitNoteController : Controller
    {
        private readonly ICreditDebitNoteRepository _repository;

        public CreditDebitNoteController(ICreditDebitNoteRepository repository)
        {
            _repository = repository;
        }

        // ─── Credit Note Endpoints ───────────────────────────────────────────────

        [HttpGet]
        [Route("GetCreditNotes")]
        public async Task<ActionResult<List<CreditNote>>> GetCreditNotes(
            string? branch, string? client, int? agreementID)
        {
            try
            {
                var result = await _repository.GetCreditNotes(branch, client, agreementID);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetCreditNoteById")]
        public async Task<ActionResult<CreditNote>> GetCreditNoteById(int id)
        {
            try
            {
                var result = await _repository.GetCreditNoteById(id);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GenerateCreditNoteNo")]
        public async Task<ActionResult<string>> GenerateCreditNoteNo(string branch, DateTime date)
        {
            try
            {
                var no = await _repository.GenerateCreditNoteNo(branch, date);
                return Ok(no);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Route("SaveCreditNote")]
        public async Task<ActionResult<CreditNote>> SaveCreditNote([FromBody] CreditNoteRequestDto dto)
        {
            try
            {
                var creditNote = MapCreditNoteFromDto(dto);
                // Auto-generate number for new records if not provided
                if (creditNote.ID == 0 && string.IsNullOrWhiteSpace(creditNote.CreditNoteNo))
                    creditNote.CreditNoteNo = await _repository.GenerateCreditNoteNo(creditNote.Branch, creditNote.CreditNoteDate);

                var result = await _repository.SaveAndUpdateCreditNote(creditNote);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Route("UpdateCreditNote")]
        public IActionResult UpdateCreditNote([FromBody] CreditNoteRequestDto dto)
        {
            try
            {
                var creditNote = MapCreditNoteFromDto(dto);
                var result = _repository.SaveAndUpdateCreditNote(creditNote).Result;
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Route("DeleteCreditNote")]
        public async Task<ActionResult<bool>> DeleteCreditNote(int id, string currentUser)
        {
            try
            {
                var result = await _repository.DeleteCreditNote(id, currentUser);
                if (!result) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ─── Debit Note Endpoints ────────────────────────────────────────────────

        [HttpGet]
        [Route("GetDebitNotes")]
        public async Task<ActionResult<List<DebitNote>>> GetDebitNotes(
            string? branch, string? client, int? agreementID)
        {
            try
            {
                var result = await _repository.GetDebitNotes(branch, client, agreementID);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetDebitNoteById")]
        public async Task<ActionResult<DebitNote>> GetDebitNoteById(int id)
        {
            try
            {
                var result = await _repository.GetDebitNoteById(id);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GenerateDebitNoteNo")]
        public async Task<ActionResult<string>> GenerateDebitNoteNo(string branch, DateTime date)
        {
            try
            {
                var no = await _repository.GenerateDebitNoteNo(branch, date);
                return Ok(no);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Route("SaveDebitNote")]
        public async Task<ActionResult<DebitNote>> SaveDebitNote([FromBody] DebitNoteRequestDto dto)
        {
            try
            {
                var debitNote = MapDebitNoteFromDto(dto);
                if (debitNote.ID == 0 && string.IsNullOrWhiteSpace(debitNote.DebitNoteNo))
                    debitNote.DebitNoteNo = await _repository.GenerateDebitNoteNo(debitNote.Branch, debitNote.DebitNoteDate);

                var result = await _repository.SaveAndUpdateDebitNote(debitNote);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Route("UpdateDebitNote")]
        public async Task<ActionResult<DebitNote>> UpdateDebitNote([FromBody] DebitNoteRequestDto dto)
        {
            try
            {
                var debitNote = MapDebitNoteFromDto(dto);
                var result = await _repository.SaveAndUpdateDebitNote(debitNote);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Route("DeleteDebitNote")]
        public async Task<ActionResult<bool>> DeleteDebitNote(int id, string currentUser)
        {
            try
            {
                var result = await _repository.DeleteDebitNote(id, currentUser);
                if (!result) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ─── Summary Endpoint (used by receipt form) ─────────────────────────────

        [HttpGet]
        [Route("GetApprovedCreditNotesByClient")]
        public async Task<ActionResult<List<CreditNote>>> GetApprovedCreditNotesByClient(string branch, string client)
        {
            try
            {
                var result = await _repository.GetApprovedCreditNotesByClient(branch, client);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetApprovedDebitNotesByClient")]
        public async Task<ActionResult<List<DebitNote>>> GetApprovedDebitNotesByClient(string branch, string client)
        {
            try
            {
                var result = await _repository.GetApprovedDebitNotesByClient(branch, client);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ─── Mappers ────────────────────────────────────────────────────────────

        private static CreditNote MapCreditNoteFromDto(CreditNoteRequestDto dto)
        {
            return new CreditNote
            {
                ID = dto.ID,
                CreditNoteNo = dto.CreditNoteNo ?? string.Empty,
                Branch = dto.Branch,
                Client = dto.Client,
                ClientInvoiceID = dto.ClientInvoiceID,
                CreditNoteDate = dto.CreditNoteDate,
                CreditNoteAmount = dto.CreditNoteAmount,
                TaxPercentage = dto.TaxPercentage,
                TaxAmount = dto.TaxAmount,
                TotalAmount = dto.TotalAmount,
                Reason = dto.Reason,
                ReferenceInvoiceNo = dto.ReferenceInvoiceNo,
                LastUpdatedBy = dto.LastUpdatedBy
            };
        }

        private static DebitNote MapDebitNoteFromDto(DebitNoteRequestDto dto)
        {
            return new DebitNote
            {
                ID = dto.ID,
                DebitNoteNo = dto.DebitNoteNo ?? string.Empty,
                Branch = dto.Branch,
                Client = dto.Client,
                ClientInvoiceID = dto.ClientInvoiceID,
                DebitNoteDate = dto.DebitNoteDate,
                DebitNoteAmount = dto.DebitNoteAmount,
                TaxPercentage = dto.TaxPercentage,
                TaxAmount = dto.TaxAmount,
                TotalAmount = dto.TotalAmount,
                Reason = dto.Reason,
                ReferenceInvoiceNo = dto.ReferenceInvoiceNo,
                DueDate = dto.DueDate,
                LastUpdatedBy = dto.LastUpdatedBy
            };
        }
    }
}
