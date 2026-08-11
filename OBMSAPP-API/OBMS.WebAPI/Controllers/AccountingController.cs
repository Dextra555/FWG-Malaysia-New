using BoldReports.ServerProcessor;
using Microsoft.AspNetCore.Mvc;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;

namespace OBMS.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountingController : Controller
    {
        HttpResponseMessage response = new HttpResponseMessage();
        private readonly OBMSDbContext _oBMSDbContext;
        private readonly IAccountingRepository _accountingRepository;

        public AccountingController(IAccountingRepository accountingRepository, OBMSDbContext oBMSDbContext)
        {
            _accountingRepository = accountingRepository;
            _oBMSDbContext = oBMSDbContext;
        }

        [HttpGet("GetList")]
        public async Task<IActionResult> GetList(int processYear, string branch)
        {
            var result = await _accountingRepository.GetDataList(processYear, branch);
            return Ok(result);
        }

        [HttpGet("GetListWithType")]
        public async Task<IActionResult> GetListWithType(int processYear, string branch, string type)
        {
            var result = await _accountingRepository.GetDataList(processYear, branch, type);
            return Ok(result);
        }

        [HttpGet("exists")]
        public async Task<IActionResult> CheckRecordExists(int year, string branch)
        {
            var exists = await _accountingRepository.GetRecord(year, branch);
            return Ok(exists);
        }

        [HttpGet("exists-with-type")]
        public async Task<IActionResult> CheckRecordExistsWithType(int year, string branch, string type)
        {
            var exists = await _accountingRepository.GetRecordWithType(year, branch, type);
            return Ok(exists);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddGLRecord(string currentUser, List<AccountGLReportDto> accountGLReportDto)
        {
            try
            {
                var result = await _accountingRepository.AddGLRecordAsync(currentUser,accountGLReportDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteGLRecord(int processYear, string branch, string currentUser)
        {
            try
            {
                var result = await _accountingRepository.DeleteGLRecordAsync(processYear, branch, currentUser);
                return Ok(result);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
