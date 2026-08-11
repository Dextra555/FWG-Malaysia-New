using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.BusinessObjects;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using System.Web.Services.Description;

namespace OBMS.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollController : ControllerBase
    {
        HttpResponseMessage response = new HttpResponseMessage();
        private readonly IPayrollRepository _payrollRepository;
        private readonly OBMSDbContext _oBMSDbContext;
        private readonly ISalaryProcess _salaryProcess;

        public PayrollController(IPayrollRepository payrollRepository, OBMSDbContext oBMSDbContext, ISalaryProcess salaryProcess)
        {
            _payrollRepository = payrollRepository;
            _oBMSDbContext = oBMSDbContext;
            _salaryProcess = salaryProcess;
        }

        [HttpGet]
        [Route("GetEmployeeList")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetEmployeeList()
        {
            try
            {
                var employeeList = await _payrollRepository.GetEmployeeList();
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetEmployeeListBySalaryAdvance")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetEmployeeListBySalaryAdvance(int TransType, string currentUser)
        {
            try
            {
                var employeeList = await _payrollRepository.GetEmployeeListBySalaryAdvance(TransType, currentUser);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetEmployeeListByBranchCode")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetEmployeeListByBranchCode(string branchCode)
        {
            try
            {
                var employeeList = await _payrollRepository.GetEmployeeListByBranchCode(branchCode);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetEmployeeListByAdvanceID")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetEmployeeListByAdvanceID(int Id)
        {
            try
            {
                var employeeList = await _payrollRepository.GetEmployeeListByAdvanceID(Id);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetListByEmplyeeType")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetListByEmplyeeType(DateTime advanceDate, string branch, string employeeType, int transType, decimal advanceAmount, string race)
        {
            try
            {
                var employeeList = await _payrollRepository.GetListByEmplyeeType(advanceDate, branch, employeeType, transType, advanceAmount, race);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetEmployeeAdvanceList")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetEmployeeAdvanceList(DateTime advanceDate, string branch, string employeeType, string client, int transType, decimal advanceAmount, string race)
        {
            try
            {
                var employeeList = await _payrollRepository.GetEmployeeAdvanceList(advanceDate, branch,employeeType, client, transType, advanceAmount, race);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetInventoryCategories")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetInventoryCategories()
        {
            try
            {
                var inventoryCategories = await _payrollRepository.GetInventoryCategories();
                return Ok(inventoryCategories);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost]
        [Route("SaveAndUpdateSalaryMonthlyAdvance")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateSalaryMonthlyAdvance(SalaryAdvanceRequestDto salaryAdvanceRequestDto)
        {
            try
            {
                var salaryAdvanceDetails = new SalaryAdvance()
                {
                    ID = salaryAdvanceRequestDto.ID,
                    EmployeeID = salaryAdvanceRequestDto.EmployeeID,
                    AdvanceTakenDate = salaryAdvanceRequestDto.AdvanceTakenDate,
                    AdvanceDate = salaryAdvanceRequestDto.AdvanceDate,
                    VoucherNo = salaryAdvanceRequestDto.VoucherNo,
                    Amount = salaryAdvanceRequestDto.Amount,
                    NoOfInstallments = salaryAdvanceRequestDto.NoOfInstallments,
                    PaymentType = salaryAdvanceRequestDto.PaymentType,
                    Particulars = salaryAdvanceRequestDto.Particulars,
                    TransType = salaryAdvanceRequestDto.TransType,
                    IsDeleted = salaryAdvanceRequestDto.IsDeleted,
                    LastUpdate = DateTime.Now,
                    LastUpdatedBy = salaryAdvanceRequestDto.LastUpdatedBy,
                };
                await _payrollRepository.SaveAndUpdateSalaryMonthlyAdvance(salaryAdvanceDetails);

                if (salaryAdvanceDetails != null)
                {
                    Dictionary<string, object> dictResult = new Dictionary<string, object>();
                    dictResult.Add("Success", "Success");
                    dictResult.Add("SalaryAdvance", salaryAdvanceDetails);
                    response.Headers.Add("Message", "Successfully save & update salary advance details.");

                    return Ok(dictResult);
                }
                else
                {
                    response.Headers.Add("Failure", "Branch save & update failed please check your key-in details....");
                }
                return response;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("SaveEmployeeItemIssues")]
        public async Task<IActionResult> SaveEmployeeItemIssues([FromBody] List<EmployeeItemIssueDto> itemDtos)
        {
            try
            {
                var items = itemDtos.Select(dto => new EmployeeItemIssue
                {
                    ID = dto.ID,
                    AdvanceID = dto.AdvanceID,
                    ItemID = dto.ItemID,
                    Price = dto.Price,
                    Quantity = dto.Quantity,
                    LastUpdate = DateTime.Now,
                    LastUpdatedBy = dto.LastUpdatedBy
                }).ToList();

                await _payrollRepository.SaveEmployeeItemIssuesAsync(items);

                return Ok(new { Success = "Success", Message = "Item issues saved successfully." });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.InnerException?.Message
                            ?? ex.InnerException?.Message
                            ?? ex.Message;
                return BadRequest(new { Message = innerMsg });
            }
        }

        [HttpGet]
        [Route("GetSalaryAdvanceById")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetSalaryAdvanceById(int employeeId,int id)
        {
            try
            {
                var employeeList = await _payrollRepository.GetSalaryAdvanceById(employeeId,id);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }
        [HttpGet]
        [Route("GetEmployeeById")]
        public async Task<ActionResult<EmployeeRequestDto>> GetEmployeeById(int employeeId)
        {
            try
            {
                var employeeList = await _payrollRepository.GetEmployeeById(employeeId);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpPost]
        [Route("GetSalaryAdvanceByDateAndEmployee")]
        public async Task<ActionResult<HttpResponseMessage>> GetSalaryAdvanceByDateAndEmployee(SalaryAdvance salaryAdvance)
        {
            try
            {
                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                if (salaryAdvance != null)
                {
                    var result = _payrollRepository.GetSalaryProcessDateByEmployeeID(salaryAdvance.EmployeeID, salaryAdvance.AdvanceDate.Year, salaryAdvance.AdvanceDate.Month + 1);
                    if (result == true)
                    {

                        dictResult.Add("True", "True");
                        dictResult.Add("SalaryAdvance", salaryAdvance);
                        dictResult.Add("Message", "Salary already Process for this Guard/Satff.You Not have Right To Update or Save. Please Contact HQ for More Information..");
                        return Ok(dictResult);
                    }
                    else
                    {
                        var resgindate = _payrollRepository.GetResignDateByEmployeeID(salaryAdvance.EmployeeID);
                        if (resgindate != new DateTime(1900, 1, 1))
                        {
                            if (salaryAdvance.AdvanceDate > resgindate)
                            {
                                dictResult.Add("ResignDate", "ResignDate");
                                dictResult.Add("SalaryAdvance", salaryAdvance);
                                dictResult.Add("Message", "Advance Date Cannot Over than Guard/Staff Resign Date. Please Contact HQ for More Information.");
                                return Ok(dictResult);
                            }
                            else
                            {
                                await _payrollRepository.SaveAndUpdateSalaryMonthlyAdvance(salaryAdvance);
                                dictResult.Add("Success", "Success");
                                dictResult.Add("SalaryAdvance", salaryAdvance);
                                dictResult.Add("Message", "Successfully save & update salary advance details.");
                                return Ok(dictResult);
                            }
                        }
                        else
                        {
                            await _payrollRepository.SaveAndUpdateSalaryMonthlyAdvance(salaryAdvance);
                            dictResult.Add("Success", "Success");
                            dictResult.Add("SalaryAdvance", salaryAdvance);
                            dictResult.Add("Message", "Successfully save & update salary advance details.");
                            //response.Headers.Add("Success", "Successfully save & update salary advance details.");
                            return Ok(dictResult);
                        }

                    }

                }

                //var employeeList = await _payrollRepository.GetSalaryAdvanceByDateAndEmployee(salaryAdvance);
                //if (employeeList != null && employeeList.Count > 0)
                //{
                //    Dictionary<string, object> dictResult = new Dictionary<string, object>();
                //    dictResult.Add("Exists", "Exists");
                //    dictResult.Add("SalaryAdvance", employeeList);
                //    dictResult.Add("Message", "Record Exists.Please check");
                //    //response.Headers.Add("Message", "Record Exists.Please check");

                //    return Ok(dictResult);
                //}
                //else
                //{
                //    await _payrollRepository.SaveAndUpdateSalaryMonthlyAdvance(salaryAdvance);
                //    if (salaryAdvance != null)
                //    {
                //        Dictionary<string, object> dictResult = new Dictionary<string, object>();
                //        dictResult.Add("Success", "Success");
                //        dictResult.Add("SalaryAdvance", salaryAdvance);
                //        dictResult.Add("Message", "Successfully save & update salary advance details.");
                //        //response.Headers.Add("Success", "Successfully save & update salary advance details.");

                //        return Ok(dictResult);
                //    }
                //}
                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetNewVoucherNumberAsync")]
        public IActionResult GetNewVoucherNumberAsync(int transType)
        {
            try
            {
                string voucherNumber = _payrollRepository.GetNewVoucherNumberAsync(transType);
                if (voucherNumber != null)
                {
                    Dictionary<string, object> dictResult = new Dictionary<string, object>();
                    dictResult.Add("Success", "Success");
                    dictResult.Add("VoucherNumber", voucherNumber);
                    return Ok(dictResult);
                }
                return Ok(voucherNumber);

            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetUniformItemRows")]
        public async Task<ActionResult<List<ItemMasterDto>>> GetUniformItemRows(int AdvanceID, int Category)
        {
            try
            {
                var uniformItemRows = _payrollRepository.GetUniformItemRows(AdvanceID, Category);
                return Ok(uniformItemRows);

            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetSalaryProcessDateByEmployeeID")]
        public bool GetSalaryProcessDateByEmployeeID(int employeeID, int year, int month)
        {
            try
            {
                return _payrollRepository.GetSalaryProcessDateByEmployeeID(employeeID, year, month);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("GetResignDate")]
        public IActionResult GetResignDate(int employeeID)
        {
            try
            {
                var resignDate = _payrollRepository.GetResignDateByEmployeeID(employeeID);
                return Ok(resignDate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("misc-trans")]
        public async Task<IActionResult> GetMiscTransList(DateTime transDate, decimal employeeId)
        {
            try
            {
                var result = await _payrollRepository.GetList(transDate, employeeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving Misc Transactions.",
                    Error = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("GetMiscTrans")]
        public async Task<ActionResult<IEnumerable<MiscTransDto>>> GetMiscTrans(string currentUser)
        {
            try
            {
                var miscTrans = await _payrollRepository.GetMiscTrans(currentUser);
                return Ok(miscTrans);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetMiscTransById")]
        public async Task<ActionResult<MiscTransDto>> GetMiscTransById(int id)
        {
            try
            {
                var miscTrans = await _payrollRepository.GetMiscTransById(id);
                if (miscTrans == null)
                {
                    return NotFound();
                }

                return Ok(miscTrans);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        [HttpPost]
        [Route("SaveAndUpdateMiscTrans")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateMiscTrans(MiscTransDto miscTrans)
        {
            try
            {
                var miscTransdetails = new MiscTrans()
                {
                    ID = miscTrans.ID,
                    TransDate = miscTrans.TransDate,
                    EmployeeID = miscTrans.EmployeeID,
                    TransType = miscTrans.TransType,
                    Amount = miscTrans.Amount,
                    Particulars = miscTrans.Particulars,
                    LastUpdate = DateTime.Now,
                    LastUpdatedBy = miscTrans.LastUpdatedBy,
                };
                await _payrollRepository.SaveAndUpdateMiscTrans(miscTransdetails);
                if (miscTransdetails != null)
                {
                    Dictionary<string, object> dictResult = new Dictionary<string, object>();
                    dictResult.Add("Success", "Success");
                    dictResult.Add("MiscTrans", miscTransdetails);
                    dictResult.Add("Message", "Successfully save & update misc trans details.");
                    return Ok(dictResult);
                }
                else
                {
                    response.Headers.Add("Failure", "Misc Trans save & update failed please check your key-in deatais....");
                }
                return response;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        //not yet  implemented
        [HttpPost]
        [Route("DeleteMiscTransById")]
        public async Task<ActionResult> DeleteMiscTransById(int id)
        {
            try
            {
                await _payrollRepository.DeleteMiscTransById(id);
                return NoContent();
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        [HttpPost]
        [Route("SaveAndUpdateSalaryDailyAdvances")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateSalaryDailyAdvances(List<SalaryAdvanceRequestDto> salaryAdvanceRequestDto)
        {
            try
            {
                List<SalaryAdvance> updatedRecords = new List<SalaryAdvance>();

                foreach (var salaryAdvanceReques in salaryAdvanceRequestDto)
                {
                    var salaryAdvanceDetails = new SalaryAdvance()
                    {
                        ID = salaryAdvanceReques.ID,
                        EmployeeID = salaryAdvanceReques.EmployeeID,
                        AdvanceTakenDate = salaryAdvanceReques.AdvanceTakenDate,
                        AdvanceDate = salaryAdvanceReques.AdvanceDate,
                        VoucherNo = salaryAdvanceReques.VoucherNo,
                        Amount = salaryAdvanceReques.Amount,
                        NoOfInstallments = salaryAdvanceReques.NoOfInstallments,
                        PaymentType = salaryAdvanceReques.PaymentType,
                        Particulars = salaryAdvanceReques.Particulars,
                        TransType = salaryAdvanceReques.TransType,
                        IsDeleted = salaryAdvanceReques.IsDeleted,
                        LastUpdate = DateTime.Now,
                        LastUpdatedBy = salaryAdvanceReques.LastUpdatedBy,
                    };

                    // Add the current salaryAdvanceDetails to the updatedRecords list
                    updatedRecords.Add(salaryAdvanceDetails);
                }

                // Pass the updatedRecords list to the repository method
                await _payrollRepository.SaveAndUpdateSalaryDailyAdvances(updatedRecords);

                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("SalaryAdvance", updatedRecords);
                dictResult.Add("Message", "Successfully save & update daily advance details");
                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpGet]
        [Route("GetDailyAdvanceList")]
        public List<EmployeeDailyAdvanceRow> GetDailyAdvanceList(DateTime advanceDate, int employeeID, int advanceType)
        {
            try
            {
                return _payrollRepository.GetDailyAdvanceList(advanceDate, employeeID, advanceType);

            }
            catch (Exception ex)
            {

                throw;
            }
        }
        [HttpGet("GetEmployeeNo/{employeeId}")]
        public async Task<IActionResult> GetEmployeeNo(int employeeId)
        {
            var employeeNo = await _payrollRepository.GetEmployeeNoAsync(employeeId);
            if (employeeNo == null)
            {
                return NotFound(new { Message = "Employee not found" });
            }
            return Ok(new { EmployeeNo = employeeNo });
        }       

        [HttpGet("GetEmployeeLoanById")]
        public async Task<IActionResult> GetEmployeeLoanById(int id, int transType)
        {
            try
            {
                var result = await _payrollRepository.GetEmployeeLoanIdAsync(id, transType);
                if (result == null)
                    return NotFound(new { message = $"Salary advance with ID {id} not found" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching salary advance by ID", error = ex.Message });
            }
        }

        #region Attendance

        [HttpGet]
        [Route("GetClients")]
        public async Task<ActionResult<AttendanceDto>> GetClients(DateTime period, string branchCode)
        {
            try
            {
                var clients = _payrollRepository.GetClients(period, branchCode);
                return Ok(clients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        [HttpGet]
        [Route("AttendanceByEmployeeID")]
        public async Task<ActionResult<AttendanceDto>> AttendanceByEmployeeID(DateTime Period, int employeeID)
        {
            try
            {
                var attendance = _payrollRepository.AttendanceByEmployeeID(Period, employeeID);
                return Ok(attendance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        [HttpGet]
        [Route("AttendanceDetailsByID")]
        public async Task<ActionResult<List<AttendanceDetailsDto>>> AttendanceDetailsByID(int Id)
        {
            try
            {
                var attendanceDetails = await _payrollRepository.AttendanceDetailsByID(Id);
                return Ok(attendanceDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        [HttpGet]
        [Route("GetAttendanceDetailsList")]
        public async Task<ActionResult<List<AttendanceDetailsDto>>> GetAttendanceDetailsList(int attendanceID)
        {
            try
            {
                var attendanceDetails = await _payrollRepository.GetAttendanceDetailsList(attendanceID);
                return Ok(attendanceDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}"); ;
            }

        }

        [HttpGet]
        [Route("GetEmployeeDetails")]
        public async Task<ActionResult<List<SalaryAttendenceDto>>> GetEmployeeDetails(string branchCode, string employeeNo)
        {
            try
            {
                var employeeList = await _payrollRepository.GetEmployeeDetails(branchCode, employeeNo);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}"); ;
            }

        }

        [HttpGet]
        [Route("IsSalaryProcessDoneForCurrentPeriod")]
        public IActionResult IsSalaryProcessDoneForCurrentPeriod(string branch, string employeeType, DateTime dtPeriod)
        {
            try
            {
                var isSalaryProcess = _payrollRepository.IsSalaryProcessDoneForCurrentPeriod(branch, employeeType, dtPeriod);
                return Ok(isSalaryProcess);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetEmployeeAttendanceList")]
        public ActionResult<List<string>> GetEmployeeAttendanceList(DateTime period, string branch)
        {
            try
            {
                var result = _payrollRepository.GetEmployeeAttendanceList(period, branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("IsTemporaryEmployee")]
        public IActionResult IsTemporaryEmployee(string employeeCode)
        {
            try
            {
                var isTemporary = _payrollRepository.IsTemporaryEmployee(employeeCode);
                return Ok(isTemporary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetTemporaryEmployeeList")]
        public ActionResult<List<string>> GetTemporaryEmployeeList(string branch)
        {
            try
            {
                var result = _payrollRepository.GetTemporaryEmployeeList(branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetAnnualLeave")]
        public async Task<Dictionary<string, Object>> GetAnnualLeave(int employeeID, DateTime Period)
        {
            var results = new Dictionary<string, Object>();
            try
            {
                var sqlQuery = @"
                            SELECT CAST(SUM(LeaveTaken) AS INT) as LeaveTaken, 
                                   CAST(SUM(LeaveAvailable) AS INT) as LeaveAvailable
                            FROM (
                            SELECT COUNT(TYPE) as LeaveTaken, 0 as LeaveAvailable FROM AttendanceDetails
                            INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID
                            INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID
                            INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                            WHERE AttendanceDate < @Period
                              AND YEAR(AttendanceDate) = YEAR(@Period)
                              AND Type = 8 
                              AND Attendance.EmployeeID = @employeeID
                            UNION
                            SELECT 0 as LeaveTaken,
                                CASE
                                WHEN DateDiff(YEAR,EMPPAY_DATE_JOINED,@Period) = 0
                                    THEN CAST(AL0To1 * DateDiff(Month,EMPPAY_DATE_JOINED,@Period)/12 as int)
                                WHEN DateDiff(YEAR,EMPPAY_DATE_JOINED,@Period) = 1
                                    THEN CASE WHEN DateDiff(month,EMPPAY_DATE_JOINED,@Period) <= 12
                                        THEN CAST(AL0To1 * DateDiff(Month,EMPPAY_DATE_JOINED,@Period)/12 as int)
                                        ELSE CAST(AL1To2 as int) END
                                WHEN DateDiff(YEAR,EMPPAY_DATE_JOINED,@Period) = 2
                                    THEN CASE WHEN DateDiff(month,EMPPAY_DATE_JOINED,@Period) BETWEEN 13 AND 24
                                        THEN CAST(AL1To2 as int)
                                        ELSE CAST((AL2To5 * DateDiff(Month,EMPPAY_DATE_JOINED,@Period)/12) - 24 + AL1To2 as int) END
                                WHEN DateDiff(YEAR,EMPPAY_DATE_JOINED,@Period) = 3
                                    THEN CASE WHEN DateDiff(month,EMPPAY_DATE_JOINED,@Period) BETWEEN 25 AND 28
                                        THEN CAST((AL2To5 * DateDiff(Month,EMPPAY_DATE_JOINED,@Period)/12) - 24 + AL1To2 as int)
                                        ELSE CAST(AL2To5 as int) END
                                WHEN DateDiff(YEAR,EMPPAY_DATE_JOINED,@Period) BETWEEN 4 AND 5
                                    THEN CASE WHEN DateDiff(month,EMPPAY_DATE_JOINED,@Period) BETWEEN 25 AND 60
                                        THEN CAST(AL2To5 as int)
                                        ELSE CAST(ROUND((((DateDiff(Month,EMPPAY_DATE_JOINED,@Period)-61)*1.0/12) * 4),0) + AL2To5 as int) END
                                WHEN DateDiff(YEAR,EMPPAY_DATE_JOINED,@Period) = 6
                                    THEN CASE WHEN DateDiff(month,EMPPAY_DATE_JOINED,@Period) BETWEEN 61 AND 63
                                        THEN CAST(Al6 * DateDiff(Month,EMPPAY_DATE_JOINED,@Period)/12 - 68 as int)
                                        ELSE CAST(Al6 as int) END
                                ELSE CAST(Al6 as int)
                                END as LeaveAvailable
                            FROM leaveSystem, Employee
                            INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                            WHERE Employee.EMP_ID = @employeeID
                            ) ANNUALLEAVE";

                var parameters = new[]
                                {
                                 new Microsoft.Data.SqlClient.SqlParameter("@employeeID", employeeID),
                                 new Microsoft.Data.SqlClient.SqlParameter("@Period", Period),
                                };


                var result = await _oBMSDbContext.LeaveClassResults
                       .FromSqlRaw(sqlQuery, parameters)
                .FirstOrDefaultAsync();
                results.Add("LeaveTaken", result.LeaveTaken);
                results.Add("LeaveAvailable", result.LeaveAvailable);
                results.Add("LeaveRemaining", result.LeaveAvailable - result.LeaveTaken);
            }
            catch (Exception)
            {

                throw;
            }

            return results;
        }

        [HttpGet]
        [Route("GetMedicalLeave")]
        public async Task<Dictionary<string, Object>> GetMedicalLeave(int employeeID, DateTime Period)
        {
            try
            {
                var results = new Dictionary<string, Object>();
                var sqlQuery = @"
                    SELECT CAST(SUM(LeaveTaken) AS INT) as LeaveTaken, 
                           CAST(SUM(LeaveAvailable) AS INT) as LeaveAvailable
                    FROM (
                        SELECT COUNT(TYPE) as LeaveTaken, 0 as LeaveAvailable
                        FROM AttendanceDetails
                        INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID
                        INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID
                        INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                        WHERE YEAR(AttendanceDate) = YEAR(@Period)
                            AND MONTH(AttendanceDate) <= MONTH(@Period)
                            AND Type = 9
                            AND Attendance.EmployeeID = @employeeID
                        UNION
                        SELECT 0 as LeaveTaken,
                            CASE
                                WHEN DateDiff(day, EMPPAY_DATE_JOINED, @Period) / 365 < 2 THEN ML0To2
                                WHEN DateDiff(day, EMPPAY_DATE_JOINED, @Period) / 365 BETWEEN 2 AND 5 THEN ML2To5
                                ELSE Ml6
                            END as LeaveAvailable
                        FROM leaveSystem
                        INNER JOIN Employee ON Employee.EMP_ID = @employeeID
                        INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                    ) ANNUALLEAVE";


                var parameters = new[]
                {
                                 new Microsoft.Data.SqlClient.SqlParameter("@employeeID", employeeID),
                                new Microsoft.Data.SqlClient.SqlParameter("@Period", Period),
                    };


                var result = await _oBMSDbContext.LeaveClassResults
                       .FromSqlRaw(sqlQuery, parameters)
                .FirstOrDefaultAsync();
                results.Add("LeaveTaken", result.LeaveTaken);
                results.Add("LeaveAvailable", result.LeaveAvailable);
                results.Add("LeaveRemaining", result.LeaveAvailable - result.LeaveTaken);

                return results;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        [HttpGet]
        [Route("GetMaternityLeave")]
        public async Task<Dictionary<string, Object>> GetMaternityLeave(int employeeID, DateTime Period)
        {
            try
            {
                var results = new Dictionary<string, Object>();
                var sqlQuery = @"
                            SELECT CAST(SUM(LeaveTaken) AS INT) as LeaveTaken, 
                                   CAST(SUM(LeaveAvailable) AS INT) as LeaveAvailable
                            FROM (
                            SELECT COUNT(TYPE) as LeaveTaken, 0 as LeaveAvailable
                            FROM AttendanceDetails
                            INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID
                            INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID
                            INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                            WHERE YEAR(AttendanceDate) = YEAR(@Period) 
                              AND MONTH(AttendanceDate) <= MONTH(@Period) 
                              AND Type = 10 
                              AND Attendance.EmployeeID = @employeeID
                            UNION
                            SELECT 0 as LeaveTaken, MtnyL as LeaveAvailable
                            FROM leaveSystem, Employee
                            INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                            WHERE Employee.EMP_ID = @employeeID
                            ) ANNUALLEAVE";

                var parameters = new[]
                {
                                 new Microsoft.Data.SqlClient.SqlParameter("@employeeID", employeeID),
                                new Microsoft.Data.SqlClient.SqlParameter("@Period", Period),
                    };


                var result = await _oBMSDbContext.LeaveClassResults
                       .FromSqlRaw(sqlQuery, parameters)
                .FirstOrDefaultAsync();
                results.Add("LeaveTaken", result.LeaveTaken);
                results.Add("LeaveAvailable", result.LeaveAvailable);
                results.Add("LeaveRemaining", result.LeaveAvailable - result.LeaveTaken);

                return results;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        [HttpGet]
        [Route("GetPaternityLeave")]
        public async Task<Dictionary<string, Object>> GetPaternityLeave(int employeeID, DateTime Period)
        {
            try
            {
                var results = new Dictionary<string, Object>();
                var sqlQuery = @"
                             SELECT CAST(SUM(LeaveTaken) AS INT) as LeaveTaken, 
                                    CAST(SUM(LeaveAvailable) AS INT) as LeaveAvailable
                            FROM (
                            SELECT COUNT(TYPE) as LeaveTaken, 0 as LeaveAvailable FROM AttendanceDetails
                            INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID
                            INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID
                            INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                            WHERE YEAR(AttendanceDate) = YEAR(@Period) 
                              AND MONTH(AttendanceDate) <= MONTH(@Period) 
                              AND Type = 11 
                              AND Attendance.EmployeeID = @employeeID
                            UNION
                            SELECT 0 as LeaveTaken, PtnyL as LeaveAvailable
                            FROM leaveSystem, Employee
                            INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                            WHERE Employee.EMP_ID = @employeeID
                            ) ANNUALLEAVE";

                var parameters = new[]
                {
                                 new Microsoft.Data.SqlClient.SqlParameter("@employeeID", employeeID),
                                new Microsoft.Data.SqlClient.SqlParameter("@Period", Period),
                    };


                var result = await _oBMSDbContext.LeaveClassResults
                       .FromSqlRaw(sqlQuery, parameters)
                .FirstOrDefaultAsync();
                results.Add("LeaveTaken", result.LeaveTaken);
                results.Add("LeaveAvailable", result.LeaveAvailable);
                results.Add("LeaveRemaining", result.LeaveAvailable - result.LeaveTaken);

                return results;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        [HttpGet]
        [Route("GetHospitalizationLeave")]
        public async Task<Dictionary<string, Object>> GetHospitalizationLeave(int employeeID, DateTime Period)
        {
            try
            {
                var results = new Dictionary<string, Object>();
                var sqlQuery = @"
                             SELECT CAST(SUM(LeaveTaken) AS INT) as LeaveTaken, 
                                    CAST(SUM(LeaveAvailable) AS INT) as LeaveAvailable
                             FROM (
                            SELECT COUNT(TYPE) as LeaveTaken, 0 as LeaveAvailable FROM AttendanceDetails
                                INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID
                                INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID
                                INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                                WHERE YEAR(AttendanceDate) = YEAR(@Period) 
                                  AND MONTH(AttendanceDate) <= MONTH(@Period) 
                                  AND Type = 12 
                                  AND Attendance.EmployeeID = @employeeID
                            UNION
                            SELECT 0 as LeaveTaken, HL as LeaveAvailable
                                FROM leaveSystem, Employee
                                INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE
                                WHERE Employee.EMP_ID = @employeeID
                            ) ANNUALLEAVE";

                var parameters = new[]
                {
                                 new Microsoft.Data.SqlClient.SqlParameter("@employeeID", employeeID),
                                new Microsoft.Data.SqlClient.SqlParameter("@Period", Period),
                    };


                var result = await _oBMSDbContext.LeaveClassResults
                       .FromSqlRaw(sqlQuery, parameters)
                .FirstOrDefaultAsync();
                results.Add("LeaveTaken", result.LeaveTaken);
                results.Add("LeaveAvailable", result.LeaveAvailable);
                results.Add("LeaveRemaining", result.LeaveAvailable - result.LeaveTaken);

                return results;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("CalculateAge")]
        public int CalculateAge(DateTime birthDate)
        {
            int age = _payrollRepository.CalculateAge(birthDate);

            return age;
        }

        [HttpGet]
        [Route("GetAttendanceDisplayList")]
        public async Task<IActionResult> GetAttendanceDisplayList(
            DateTime attendanceDate,
            string branch = "",
            string client = "",
            string status = "All Status",
            string employeeCode = "")
        {
            try
            {
                var sqlQuery = @"
                    SELECT
                        CAST(ROW_NUMBER() OVER (ORDER BY AD.AttendanceDate, E.EMP_CODE) AS INT) AS ID,
                        E.EMP_CODE AS EmployeeCode,
                        E.EMP_NAME AS EmployeeName,
                        ISNULL(C.Name, 'Not Assigned') AS Client,
                        ISNULL(B.Name, A.Branch) AS BranchName,
                        AD.AttendanceDate,
                        CASE
                            WHEN AD.TimeStart IS NOT NULL
                            THEN CAST(DATEPART(HOUR, AD.TimeStart) AS VARCHAR) + ':00'
                            ELSE '00:00'
                        END AS Punch,
                        A.ID AS AttendanceID
                    FROM AttendanceDetails AD
                    INNER JOIN Attendance A ON A.ID = AD.AttendanceID
                    INNER JOIN Employee E ON E.EMP_ID = A.EmployeeID
                    LEFT JOIN ClientMaster C ON C.Code = AD.Client
                    LEFT JOIN BranchMaster B ON B.Code = A.Branch
                    WHERE CAST(AD.AttendanceDate AS DATE) = CAST(@attendanceDate AS DATE)
                      AND (@branch = '' OR A.Branch = @branch)
                      AND (@client = '' OR AD.Client = @client)
                      AND (@employeeCode = '' OR E.EMP_CODE LIKE '%' + @employeeCode + '%')
                      AND (
                           @status = 'All Status'
                        OR (@status = 'Absent'  AND AD.Type = 7)
                        OR (@status = 'Present' AND AD.Type <> 7)
                      )
                    ORDER BY AD.AttendanceDate, E.EMP_CODE";

                var parameters = new[]
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@attendanceDate", attendanceDate),
                    new Microsoft.Data.SqlClient.SqlParameter("@branch", string.IsNullOrEmpty(branch) ? "" : branch),
                    new Microsoft.Data.SqlClient.SqlParameter("@client", string.IsNullOrEmpty(client) ? "" : client),
                    new Microsoft.Data.SqlClient.SqlParameter("@status", string.IsNullOrEmpty(status) ? "All Status" : status),
                    new Microsoft.Data.SqlClient.SqlParameter("@employeeCode", string.IsNullOrEmpty(employeeCode) ? "" : employeeCode),
                };

                var result = await _oBMSDbContext.AttendanceDisplayResults
                    .FromSqlRaw(sqlQuery, parameters)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("SaveAndUpdateAttendance")]
        public async Task<ActionResult> SaveAndUpdateAttendance(AttendanceModel attendance)
        {
            try
            {
                var attendanceModel = new Attendance()
                {
                    ID = attendance.attendanceModel.ID,
                    Period = attendance.attendanceModel.Period,
                    EmployeeID = attendance.attendanceModel.EmployeeID,
                    Branch = attendance.attendanceModel.Branch,
                    Shift2Type = attendance.attendanceModel.Shift2Type,
                    Shift2Rate = attendance.attendanceModel.Shift2Rate,
                    AllowanceDeduction = attendance.attendanceModel.AllowanceDeduction,
                    SpecialAllowanceDeduction = attendance.attendanceModel.SpecialAllowanceDeduction,
                    Bonus = attendance.attendanceModel.Bonus,
                    KPIDeduction = attendance.attendanceModel.KPIDeduction,
                    LastUpdate = DateTime.Now,
                    LastUpdatedBy = attendance.attendanceModel.LastUpdatedBy,
                };
                await _payrollRepository.SaveAndUpdateAttendance(attendanceModel, attendance.attendanceDetails);
                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                dictResult.Add("Success", "Success");
                dictResult.Add("Message", "Attendance saved / updated successfully");
                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("DeleteAttendance")]
        public async Task<IActionResult> DeleteAttendance(int id, [FromQuery] string username)
        {
            Dictionary<string, object> dictResult = new Dictionary<string, object>();
            try
            {
                var result = await _payrollRepository.DeleteAttendanceAsync(id, username);

                if (result)
                {
                   
                    dictResult.Add("Success", "Success");
                    dictResult.Add("Message", "Attendance deleted successfully");
                    return Ok(dictResult);
                }

                dictResult.Add("NotFound", "NotFound");
                dictResult.Add("Message", "Record Not Found");
                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                // Log the exception here                
                dictResult.Add("Error", "Error");
                dictResult.Add("Message", "An error occurred: " + ex.Message);
                return Ok(dictResult);
            }
        }

        [HttpGet]
        [Route("GetList")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetList(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, DateTime attendancePeriod, string status)
        {
            try
            {
                var employeeList = _payrollRepository.GetList(branch, employeeType, resignedDate, joinDate, attendancePeriod, status);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("getListByEmployee")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> getListByEmployee(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status)
        {
            try
            {
                var employeeList = _payrollRepository.getListByEmployee(branch, employeeType, resignedDate, joinDate, status);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("getListEmployeeByClient")]
        public async Task<ActionResult<SalaryAdvanceRequestDto>> GetListEmployeeByClient(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status, string empClient)
        {
            try
            {
                var employeeList = _payrollRepository.GetListEmployeeByClient(branch, employeeType, resignedDate, joinDate, status, empClient);
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        //[HttpGet]
        //[Route("GetAnnualLeave")]
        //public int GetAnnualLeave(int employeeID, DateTime period)
        //{
        //    var leaveQuery = from attendanceDetail in _oBMSDbContext.AttendanceDetails
        //                     join attendance in _oBMSDbContext.Attendances on attendanceDetail.AttendanceID equals attendance.ID
        //                     join employee in _oBMSDbContext.Employees on attendance.EmployeeID equals employee.EMP_ID
        //                     join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                     where attendanceDetail.AttendanceDate < period && attendanceDetail.Type == 8 && employee.EMP_ID == employeeID
        //                     select new
        //                     {
        //                         LeaveTaken = 0, // Assuming 1 for LeaveTaken
        //                         LeaveAvailable = 0
        //                     };

        //    var leaveAvailableQuery = from employee in _oBMSDbContext.Employees
        //                              from leaveSystem in _oBMSDbContext.LeaveSystems
        //                              join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                              where employee.EMP_ID == employeeID
        //                              select new
        //                              {
        //                                  LeaveTaken = 0,
        //                                  //LeaveAvailable = leaveSystem.AL6 * ((period.Year - employmentDetail.EMPPAY_DATE_JOINED.Year == 0) ? DateDiffInMonths(employmentDetail.EMPPAY_DATE_JOINED, period) / 12 : 0)
        //                                  LeaveAvailable = leaveSystem.AL6
        //                                  //LeaveAvailable = leaveSystem.AL6 * (decimal)((period - employmentDetail.EMPPAY_DATE_JOINED).TotalDays / 365) / 12
        //                              };

        //    return (int)leaveAvailableQuery.AsEnumerable().Sum(x => x.LeaveAvailable);

        //}

        //[HttpGet]
        //[Route("GetMedicalLeave")]
        //public int GetMedicalLeave(int employeeID, DateTime period)
        //{
        //    var leaveQuery = from attendanceDetail in _oBMSDbContext.AttendanceDetails
        //                     join attendance in _oBMSDbContext.Attendances on attendanceDetail.AttendanceID equals attendance.ID
        //                     join employee in _oBMSDbContext.Employees on attendance.EmployeeID equals employee.EMP_ID
        //                     join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                     where attendanceDetail.AttendanceDate < period && attendanceDetail.Type == 9 && employee.EMP_ID == employeeID
        //                     select new
        //                     {
        //                         LeaveTaken = 0, // Assuming 1 for LeaveTaken
        //                         LeaveAvailable = 0
        //                     };

        //    var leaveAvailableQuery = from employee in _oBMSDbContext.Employees
        //                              from leaveSystem in _oBMSDbContext.LeaveSystems
        //                              join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                              where employee.EMP_ID == employeeID
        //                              select new
        //                              {
        //                                  LeaveTaken = 0,
        //                                  //LeaveAvailable = leaveSystem.AL6 * ((period.Year - employmentDetail.EMPPAY_DATE_JOINED.Year == 0) ? DateDiffInMonths(employmentDetail.EMPPAY_DATE_JOINED, period) / 12 : 0)
        //                                  LeaveAvailable = leaveSystem.ML6
        //                                  //LeaveAvailable = leaveSystem.AL6 * (decimal)((period - employmentDetail.EMPPAY_DATE_JOINED).TotalDays / 365) / 12
        //                              };

        //    return (int)leaveAvailableQuery.AsEnumerable().Sum(x => x.LeaveAvailable);

        //}

        //[HttpGet]
        //[Route("GetPaternityLeave")]
        //public int GetPaternityLeave(int employeeID, DateTime period)
        //{
        //    var leaveQuery = from attendanceDetail in _oBMSDbContext.AttendanceDetails
        //                     join attendance in _oBMSDbContext.Attendances on attendanceDetail.AttendanceID equals attendance.ID
        //                     join employee in _oBMSDbContext.Employees on attendance.EmployeeID equals employee.EMP_ID
        //                     join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                     where attendanceDetail.AttendanceDate < period && attendanceDetail.Type == 9 && employee.EMP_ID == employeeID
        //                     select new
        //                     {
        //                         LeaveTaken = 0, // Assuming 1 for LeaveTaken
        //                         LeaveAvailable = 0
        //                     };

        //    var leaveAvailableQuery = from employee in _oBMSDbContext.Employees
        //                              from leaveSystem in _oBMSDbContext.LeaveSystems
        //                              join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                              where employee.EMP_ID == employeeID
        //                              select new
        //                              {
        //                                  LeaveTaken = 0,
        //                                  //LeaveAvailable = leaveSystem.AL6 * ((period.Year - employmentDetail.EMPPAY_DATE_JOINED.Year == 0) ? DateDiffInMonths(employmentDetail.EMPPAY_DATE_JOINED, period) / 12 : 0)
        //                                  LeaveAvailable = leaveSystem.PtnyL
        //                                  //LeaveAvailable = leaveSystem.AL6 * (decimal)((period - employmentDetail.EMPPAY_DATE_JOINED).TotalDays / 365) / 12
        //                              };

        //    return (int)leaveAvailableQuery.AsEnumerable().Sum(x => x.LeaveAvailable);

        //}

        //[HttpGet]
        //[Route("GetMaternityLeave")]
        //public int GetMaternityLeave(int employeeID, DateTime period)
        //{
        //    var leaveQuery = from attendanceDetail in _oBMSDbContext.AttendanceDetails
        //                     join attendance in _oBMSDbContext.Attendances on attendanceDetail.AttendanceID equals attendance.ID
        //                     join employee in _oBMSDbContext.Employees on attendance.EmployeeID equals employee.EMP_ID
        //                     join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                     where attendanceDetail.AttendanceDate < period && attendanceDetail.Type == 10 && employee.EMP_ID == employeeID
        //                     select new
        //                     {
        //                         LeaveTaken = 0, // Assuming 1 for LeaveTaken
        //                         LeaveAvailable = 0
        //                     };

        //    var leaveAvailableQuery = from employee in _oBMSDbContext.Employees
        //                              from leaveSystem in _oBMSDbContext.LeaveSystems
        //                              join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                              where employee.EMP_ID == employeeID
        //                              select new
        //                              {
        //                                  LeaveTaken = 0,
        //                                  //LeaveAvailable = leaveSystem.AL6 * ((period.Year - employmentDetail.EMPPAY_DATE_JOINED.Year == 0) ? DateDiffInMonths(employmentDetail.EMPPAY_DATE_JOINED, period) / 12 : 0)
        //                                  LeaveAvailable = leaveSystem.MtnyL
        //                                  //LeaveAvailable = leaveSystem.AL6 * (decimal)((period - employmentDetail.EMPPAY_DATE_JOINED).TotalDays / 365) / 12
        //                              };

        //    return (int)leaveAvailableQuery.AsEnumerable().Sum(x => x.LeaveAvailable);

        //}

        //[HttpGet]
        //[Route("GetHospitalizationLeave")]
        //public int GetHospitalizationLeave(int employeeID, DateTime period)
        //{
        //    var leaveQuery = from attendanceDetail in _oBMSDbContext.AttendanceDetails
        //                     join attendance in _oBMSDbContext.Attendances on attendanceDetail.AttendanceID equals attendance.ID
        //                     join employee in _oBMSDbContext.Employees on attendance.EmployeeID equals employee.EMP_ID
        //                     join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                     where attendanceDetail.AttendanceDate < period && attendanceDetail.Type == 9 && employee.EMP_ID == employeeID
        //                     select new
        //                     {
        //                         LeaveTaken = 0, // Assuming 1 for LeaveTaken
        //                         LeaveAvailable = 0
        //                     };

        //    var leaveAvailableQuery = from employee in _oBMSDbContext.Employees
        //                              from leaveSystem in _oBMSDbContext.LeaveSystems
        //                              join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
        //                              where employee.EMP_ID == employeeID
        //                              select new
        //                              {
        //                                  LeaveTaken = 0,
        //                                  //LeaveAvailable = leaveSystem.AL6 * ((period.Year - employmentDetail.EMPPAY_DATE_JOINED.Year == 0) ? DateDiffInMonths(employmentDetail.EMPPAY_DATE_JOINED, period) / 12 : 0)
        //                                  LeaveAvailable = leaveSystem.HL
        //                                  //LeaveAvailable = leaveSystem.AL6 * (decimal)((period - employmentDetail.EMPPAY_DATE_JOINED).TotalDays / 365) / 12
        //                              };

        //    return (int)leaveAvailableQuery.AsEnumerable().Sum(x => x.LeaveAvailable);

        //}

        //[HttpGet]
        //[Route("GetAnnualLeaveAPI1")]
        //public int GetAnnualLeaveAPI1(decimal employeeID, DateTime period)
        //{
        //    try
        //    {
        //        var leaveTakenQuery = from ad in _oBMSDbContext.AttendanceDetails
        //                              join a in _oBMSDbContext.Attendances on ad.AttendanceID equals a.ID
        //                              where ad.AttendanceDate < period && ad.Type == 8 && a.EmployeeID == employeeID
        //                              select ad;

        //        int leaveTaken = leaveTakenQuery.Count();

        //        var employmentDetails = _oBMSDbContext.EmploymentDetails.FirstOrDefault(ed => ed.EMPPAY_ID == employeeID);
        //        var leaveSystem = _oBMSDbContext.LeaveSystems.SingleOrDefault();
        //        if (employmentDetails == null || leaveSystem == null)
        //            return 0;

        //        int yearsWorked = (int)((period - employmentDetails.EMPPAY_DATE_JOINED).TotalDays / 365);

        //        int leaveAvailable = yearsWorked switch
        //        {
        //            int n when n < 1 => (int)(leaveSystem.al0to1 * (decimal)((period - employmentDetails.EMPPAY_DATE_JOINED).TotalDays / 365) / 12),
        //            int n when n >= 1 && n < 2 => (int)leaveSystem.AL1to2,
        //            int n when n >= 3 && n <= 5 => (int)leaveSystem.AL2to5,
        //            _ => (int)leaveSystem.AL6
        //        };

        //        return leaveAvailable - leaveTaken;
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}



        #endregion

        #region Salary Processing

        [HttpGet]
        [Route("LastprocessedDate")]
        public async Task<Dictionary<string, Object>> LastprocessedDate(string branchCode, string employeeType)
        {
            try
            {
                var results = new Dictionary<string, Object>();
                var sqlQuery = @"
                            SELECT COALESCE(MAX(Period), GETDATE()) AS ProcessedDate
                            FROM SalaryProcess
                            WHERE Branch = @branchCode
                            AND EmployeeType = @employeeType";

                var parameters = new[]
                {
                     new Microsoft.Data.SqlClient.SqlParameter("@branchCode", branchCode),
                     new Microsoft.Data.SqlClient.SqlParameter("@employeeType", employeeType),
                };


                var result = await _oBMSDbContext.LastprocessedDates
                .FromSqlRaw(sqlQuery, parameters)
                .FirstOrDefaultAsync();

                if (result != null)
                {
                    if (result.ProcessedDate != null)
                    {
                        results.Add("LastProcessedDate", (DateTime)result.ProcessedDate);
                    }
                    else
                    {
                        results.Add("LastProcessedDate", DateTime.Now);
                    }
                }
                else
                {
                    results.Add("LastProcessedDate", DateTime.Now);
                }

                return results;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        [HttpGet]
        [Route("LastSalaryProcessRemarks")]
        public IActionResult LastSalaryProcessRemarks(DateTime period, string branchCode, string employeeType)
        {
            try
            {
                var lastProcessedRemarks = _payrollRepository.LastSalaryProcessRemarks(period, branchCode, employeeType);

                return Ok(new { remarks = lastProcessedRemarks });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("Process")]
        public async Task<IActionResult> Process(string branch, string employeeType, string remarks, DateTime period, bool lockProcess, string currentUser, string companyCode)
        {
            var results = new Dictionary<string, object>();

            try
            {
                //var resultMessage = _payrollRepository.Process(branch, employeeType, remarks, period, lockProcess, currentUser, companyCode);
                var resultMessage = _salaryProcess.Process(branch, employeeType, remarks, period, lockProcess, currentUser, companyCode);
                var result = new ProcessResult
                {
                    Message = resultMessage
                };
                results.Add("result", result);

                // Return the result wrapped in a Task as HTTP 200 OK
                return Ok(await Task.FromResult(results));
            }
            catch (Exception ex)
            {
                // Handle exceptions and return an HTTP 500 error with exception details
                return StatusCode(500, new { message = "An error occurred while processing payroll", error = ex.Message });
            }
        }
        #endregion

        #region Payroll report sections
        [HttpGet]
        [Route("WithBlankRow")]
        public IActionResult GetListWithBlankRow(string dtSalaryPeriod)
        {
            try
            {
                var result = BankStatementExcel.GetListWithBlankRow(dtSalaryPeriod);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("WithBlankRowByBranch")]
        public IActionResult GetListWithBlankRowByBranch(string dtSalaryPeriod, string branch)
        {
            try
            {
                var result = BankStatementExcel.GetListWithBlankRow(dtSalaryPeriod, branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("WithBlankRowByBranchAndEmployeeType")]
        public IActionResult GetListWithBlankRowByBranchAndEmployeeType(string dtSalaryPeriod, string branch, string employeeType)
        {
            try
            {
                var result = BankStatementExcel.GetListWithBlankRow(dtSalaryPeriod, branch, employeeType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("WithBlankRowByBranchEmployeeTypeAndBank")]
        public IActionResult GetListWithBlankRowByBranchEmployeeTypeAndBank(string dtSalaryPeriod, string branch, string employeeType, string bank)
        {
            try
            {
                var result = BankStatementExcel.GetListWithBlankRow(dtSalaryPeriod, branch, employeeType, bank);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetEPFToExcel")]
        public IActionResult GetEPFToExcel(string branch, DateTime dtSalaryPeriod,  string employeeType)
        {
            try
            {
                var result = BankStatementExcel.GetEPFToExcel( branch,dtSalaryPeriod, employeeType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetEmployeeSocsoList")]
        public IActionResult GetEmployeeSocsoList(DateTime dtSalaryPeriod, string branch)
        {
            try
            {
                var result = BankStatementExcel.GetEmployeeSocsoList(dtSalaryPeriod, branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetEmployeeSIPList")]
        public IActionResult GetEmployeeSIPList(string CompanyCode, string SSM, DateTime dtSalaryPeriod, string branch)
        {
            try
            {
                var result = BankStatementExcel.GetEmployeeSIPList(CompanyCode, SSM,dtSalaryPeriod, branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetConfig")]
        public IActionResult GetConfig(string KeyValue, string Branch)
        {
            try
            {
                var result = UtilityMain.GetConfig(KeyValue, Branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetEPFToCIMBList")]
        public IActionResult GetEPFToCIMBList(string Branch,DateTime Period,string EmployeeType,string CompanyEPF,string CompanyPIC,string CompanyPICContact)
        {
            try
            {
                var result = UtilityMain.GetEPFToCIMBList(Branch, Period, EmployeeType, CompanyEPF, CompanyPIC, CompanyPICContact);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Endpoint for GetSocsoToCIMBList
        [HttpGet]
        [Route("GetSocsoToCIMBList")]
        public IActionResult GetSocsoToCIMBList(string CompanyRegNumber,string SocsoCompanyCode,string Branch,DateTime Period,string EmployeeType, string EmpTempType)
        {
            try
            {
                var result = UtilityMain.GetSocsoToCIMBList(CompanyRegNumber, SocsoCompanyCode, Branch, Period, EmployeeType, EmpTempType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Endpoint for GetSIPToCIMBList
        [HttpGet]
        [Route("GetSIPToCIMBList")]
        public IActionResult GetSIPToCIMBList(string CompanyRegNumber,string SIPCompanyCode,string Branch, DateTime Period, string EmployeeType)
        {
            try
            {
                var result = UtilityMain.GetSIPToCIMBList(CompanyRegNumber, SIPCompanyCode, Branch, Period, EmployeeType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetEmployeeSalarynAdvanceList")]
        public IActionResult GetEmployeeSalarynAdvanceList(DateTime period,string employeeType,string bankCode,string type,string company,string source)
        {
            try
            {
                List<string> result = UtilityMain.GetEmployeeSalarynAdvanceList(period, employeeType, bankCode, type, company, source);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message }); 
            }
            
        }

        [HttpGet]
        [Route("GetEmployeeSalarynAdvanceListWithBranch")]
        public IActionResult GetEmployeeSalarynAdvanceListWithBranch(string branch,DateTime period,string employeeType,string bankCode,string type,string company,string source)
        {
            try
            {
                List<string> result = UtilityMain.GetEmployeeSalarynAdvanceList(branch, period, employeeType, bankCode, type, company, source);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
           
        }

        [HttpGet]
        [Route("GetEmployeeSalarynAdvanceTotalList")]
        public IActionResult GetEmployeeSalarynAdvanceTotalList(DateTime period,string employeeType,string bankCode,string type,string company,string source)
        {
            try
            {
                List<string> result = UtilityMain.GetEmployeeSalarynAdvanceTotalList(period, employeeType, bankCode, type, company, source);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetEmployeeSalarynAdvanceTotalListWithBranch")]
        public IActionResult GetEmployeeSalarynAdvanceTotalListWithBranch(string branch,DateTime period,string employeeType,string bankCode,string type,string company,string source)
        {
            try
            {
                List<string> result = UtilityMain.GetEmployeeSalarynAdvanceTotalList(branch, period, employeeType, bankCode, type, company, source);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetEmployeeSalarynAdvanceHashTotalList")]
        public IActionResult GetEmployeeSalarynAdvanceHashTotalList(DateTime period,string employeeType,string bankCode,string type,string company,string source)
        {
            try
            {
                List<string> result = UtilityMain.GetEmployeeSalarynAdvanceHashTotalList(period, employeeType, bankCode, type, company, source);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetEmployeeSalarynAdvanceHashTotalListWithBranch")]
        public IActionResult GetEmployeeSalarynAdvanceHashTotalListWithBranch(string branch,DateTime period,string employeeType,string bankCode,string type,string company,string source)
        {
            try
            {
                List<string> result = UtilityMain.GetEmployeeSalarynAdvanceHashTotalList(branch, period, employeeType, bankCode, type, company, source);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }

        }

        [HttpGet("ClientInvoiceCalculation")]
        public IActionResult ClientInvoiceCalculation(string branch, string client, DateTime agreementPeriod)
        {
            try
            {
                // Create an instance of ClientInvoiceCalculation
                var invoiceCalculation = new ClientInvoiceCalculation(branch, client, agreementPeriod);

                // Prepare the response
                var result = new
                {
                    ServiceCharges = invoiceCalculation.ServiceCharges,
                    Discount = invoiceCalculation.Discount,
                    TaxAmount = invoiceCalculation.TaxAmount,
                    NoOfDays = invoiceCalculation.NoOfDays,
                    NoOfHours = invoiceCalculation.NoOfHours,
                    Total = invoiceCalculation.Total
                };

                // Return the result
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                return StatusCode(500, new { Message = "An error occurred while calculating the invoice.", Error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate invoice amounts by specific AgreementID using EF directly.
        /// Used when a client has multiple agreements (different sites) in the same month.
        /// </summary>
        [HttpGet("ClientInvoiceCalculationByAgreementId")]
        public IActionResult ClientInvoiceCalculationByAgreementId(int agreementId, DateTime agreementPeriod)
        {
            try
            {
                var agreement = _oBMSDbContext.Agreements
                    .Where(a => a.ID == agreementId)
                    .FirstOrDefault();

                if (agreement == null)
                    return Ok(new { ServiceCharges = 0, Discount = 0, TaxAmount = 0, NoOfDays = 0, NoOfHours = 0, Total = 0 });

                var details = _oBMSDbContext.AgreementDetails
                    .Where(d => d.AgreementID == agreementId)
                    .ToList();

                decimal serviceCharges = 0;
                decimal discount = 0;
                decimal taxAmount = 0;
                decimal noOfHours = 0;
                int daysInMonth = DateTime.DaysInMonth(agreementPeriod.Year, agreementPeriod.Month);

                // Tax rate constants (same logic as ClientInvoiceCalculation)
                DateTime agreementDate = agreementPeriod.Date;
                decimal pa;
                if (agreementDate.Year <= 2010) pa = 0.05M;
                else if (agreementDate >= new DateTime(2015, 4, 1) && agreementDate <= new DateTime(2018, 5, 31)) pa = 0.06M;
                else if (agreementDate >= new DateTime(2018, 6, 1)) pa = 0.08M;
                else pa = 0.00M;

                foreach (var d in details)
                {
                    decimal noOfDays = d.NoOfDays;
                    if (d.FollowCalender)
                    {
                        noOfDays = daysInMonth;
                    }

                    decimal monthTotal;
                    if (d.NoOfGuards != 0 && d.Rate != 0 && d.NoOfHours != 0 && noOfDays != 0)
                        monthTotal = noOfDays * d.NoOfGuards * d.NoOfHours * d.Rate;
                    else
                        monthTotal = d.MonthTotal;

                    serviceCharges += monthTotal;
                    noOfHours += d.NoOfHours * d.NoOfGuards * noOfDays;

                    if (d.HasDiscount)
                        discount += d.DiscountAmount;

                    if (d.IsTaxable)
                        taxAmount += (d.MonthTotal - d.DiscountAmount) * pa;
                }

                return Ok(new
                {
                    ServiceCharges = serviceCharges,
                    Discount = discount,
                    TaxAmount = taxAmount,
                    NoOfDays = 0,
                    NoOfHours = noOfHours,
                    Total = serviceCharges - discount + taxAmount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while calculating the invoice.", Error = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetMonthlyInvoiceStatusList")]
        public IActionResult GetMonthlyInvoiceStatusList(string Start, string End, string Branch)
        {
            try
            {
                var result = BankStatementExcel.GetMonthlyInvoiceStatusList(Start, End, Branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("CheckExistAdvance")]
        public IActionResult CheckExistAdvance(int EmployeeID, DateTime AdvanceDate, int LoanType)
        {
            try
            {
                var result = UtilityMain.CheckExistAdvance(EmployeeID, AdvanceDate, LoanType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("NewAdvanceVoucherNo")]
        public IActionResult NewAdvanceVoucherNo(string Branch, int TransType)
        {
            try
            {
                var result = UtilityMain.NewAdvanceVoucherNo(Branch, TransType);
                return Ok(new { VoucherNo = result }); // Return as JSON object
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("GetSalaryAdvances")]
        public async Task<IActionResult> GetSalaryAdvances(DateTime advanceDate, int employeeId, int transType)
        {
            try
            {
                var result = await _payrollRepository.GetSalaryAdvancesAsync(advanceDate, employeeId, transType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string currentUser)
        {
            try
            {
                var result = await _payrollRepository.DeleteSalaryAdvanceAsync(id, currentUser);
                if (result)
                    return Ok(new { success = true, message = "Salary advance deleted successfully." });

                return NotFound(new { success = false, message = "Salary advance not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpGet("latest-period")]
        public async Task<IActionResult> GetLatestPeriod(int employeeId, int year, int month)
        {
            try
            {
                var period = await _payrollRepository.GetLatestAttendancePeriodAsync(employeeId, year, month);
                return Ok(period);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetEmployeeLoanList")]
        public IActionResult GetEmployeeLoanList(DateTime Period, string Branch, string EmployeeType, int TransType)
        {
            try
            {
                var result = UtilityMain.GetEmployeeLoanList(Period, Branch, EmployeeType, TransType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        [HttpGet]
        [Route("GetSalaryList")]
        public IActionResult GetSalaryList(DateTime Period, string Branch, string Client, string Bank, string PaymentType, string EmployeeType, string EmpTempType, string Source)
        {
            try
            {
                List<BankStatementExcelDto> result = UtilityMain.GetSalaryList(Period, Branch, Client, Bank, PaymentType, EmployeeType, EmpTempType, Source);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }

        }
        #endregion
    }
}

[Keyless]
public class LeaveClassResults
{
    public int LeaveTaken { get; set; }
    public int LeaveAvailable { get; set; }
}
[Keyless]
public class LastprocessedDates
{
    public DateTime ProcessedDate { get; set; }
}

public class ProcessResult
{
    public string Message { get; set; }
}

