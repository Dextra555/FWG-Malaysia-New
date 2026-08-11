using Azure;
using BoldReports.RDL.DOM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;

namespace OBMS.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : Controller
    {
        HttpResponseMessage response = new HttpResponseMessage();
        private readonly OBMSDbContext _oBMSDbContext;
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(IEmployeeRepository employeeRepository, OBMSDbContext oBMSDbContext)
        {
            _employeeRepository = employeeRepository;
            _oBMSDbContext = oBMSDbContext;
        }

        [HttpGet]
        [Route("EmployeeNoByBranchID")]
        public async Task<ActionResult<Object>> GetEmployeeNoByBranchID(string branchId)
        {
            try
            {
                return Ok(await _employeeRepository.GetEmployeeNoByBranchID(branchId));
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        //[HttpGet]
        //[Route("Employees")]
        //public async Task<ActionResult<Object>> GetEmployees(string name)
        //{
        //    var employees = _oBMSDbContext.Employees.OrderByDescending(x => x.EMP_ID).ToList();


        //    var results = new Dictionary<string, Object>();


        //    if (name != "none")
        //    {


        //        var branchList = from branchMaster in _oBMSDbContext.BranchMasters
        //                     join obmsBranches in _oBMSDbContext.OBMSBranches on branchMaster.Code equals obmsBranches.BranchCode
        //                     where obmsBranches.Name == name
        //                     orderby branchMaster.Name
        //                     select new
        //                     {
        //                         branchMaster.Code,
        //                         branchMaster.Name
        //                     };
        //        results.Add("branchList", branchList);
        //    }

        //    results.Add("employees", employees);


        //    return results;
        //}

        [HttpGet]
        [Route("Employees")]
        //public async Task<ActionResult<object>> GetEmployees(string name)
        //{
        //    var employeeData = await (
        //        from emp in _oBMSDbContext.Employees
        //        join empDetails in _oBMSDbContext.EmploymentDetails
        //            on emp.EMP_CODE equals empDetails.EMPPAY_CODE into empGroup
        //        from details in empGroup.DefaultIfEmpty()
        //        orderby emp.EMP_ID descending
        //        select new EmployeeWithEmploymentDTO
        //        {
        //            Employee = emp,
        //            EMPPAY_DATE_JOINED = details != null ? details.EMPPAY_DATE_JOINED : null,
        //            EMPPAY_CATEGORY = details != null ? details.EMPPAY_CATEGORY : null
        //        }).ToListAsync();

        //    var results = new Dictionary<string, object>
        //    {
        //        { "employees", employeeData }
        //    };            

        //    if (!string.Equals(name, "none", StringComparison.OrdinalIgnoreCase))
        //    {

        //        bool isSuperAdmin = string.Equals(
        //            name.Trim(),
        //            "superadmin",
        //            StringComparison.OrdinalIgnoreCase
        //        );                

        //        if (isSuperAdmin)
        //        {
        //            var branchList = await (
        //                from branchMaster in _oBMSDbContext.BranchMasters
        //                orderby branchMaster.Name
        //                select new
        //                {
        //                    branchMaster.Code,
        //                    branchMaster.Name
        //                }).ToListAsync();

        //            results.Add("branchList", branchList);
        //        }
        //        else
        //        {
        //            var branchList = await (
        //                from branchMaster in _oBMSDbContext.BranchMasters
        //                join obmsBranches in _oBMSDbContext.OBMSBranches
        //                    on branchMaster.Code equals obmsBranches.BranchCode
        //                where obmsBranches.Name == name && obmsBranches.IsAllowed == true
        //                orderby branchMaster.Name
        //                select new
        //                {
        //                    branchMaster.Code,
        //                    branchMaster.Name
        //                }).ToListAsync();

        //            results.Add("branchList", branchList);
        //        }

        //    }

        //    return results;
        //}

        public async Task<ActionResult<object>> GetEmployees(string name)
        {
            bool isSuperAdmin = string.Equals(name?.Trim(), "superadmin", StringComparison.OrdinalIgnoreCase);

            // ------------------------------------
            // 1. GET ALLOWED BRANCH CODES FOR USER
            // ------------------------------------
            List<string> allowedBranchCodes = null;

            if (!isSuperAdmin)
            {
                allowedBranchCodes = await (
                    from branchMaster in _oBMSDbContext.BranchMasters
                    join obmsBranches in _oBMSDbContext.OBMSBranches
                        on branchMaster.Code equals obmsBranches.BranchCode
                    where obmsBranches.Name == name && obmsBranches.IsAllowed == true
                    select branchMaster.Code
                ).ToListAsync();
            }

            // ------------------------------------
            // 2. GET EMPLOYEES (FILTER BY BRANCH IF NOT SUPERADMIN)
            // ------------------------------------
            var employeeDataQuery =
                from emp in _oBMSDbContext.Employees
                join detailsGroup in _oBMSDbContext.EmploymentDetails
                    on emp.EMP_CODE equals detailsGroup.EMPPAY_CODE into empGroup
                from details in empGroup.DefaultIfEmpty()
                orderby emp.EMP_ID descending
                select new EmployeeWithEmploymentDTO
                {
                    Employee = emp,
                    EMPPAY_DATE_JOINED = details != null ? details.EMPPAY_DATE_JOINED : null,
                    EMPPAY_CATEGORY = details != null ? details.EMPPAY_CATEGORY : null
                };

            if (!isSuperAdmin)
            {
                // filter employees by allowed branch codes
                employeeDataQuery = employeeDataQuery
                    .Where(e => allowedBranchCodes.Contains(e.Employee.EMP_BRANCH_CODE));
            }

            var employeeData = await employeeDataQuery.ToListAsync();

            var results = new Dictionary<string, object>
            {
                { "employees", employeeData }
            };

            // ------------------------------------
            // 3. BRANCH LIST (ALREADY WORKING)
            // ------------------------------------
            if (!string.Equals(name, "none", StringComparison.OrdinalIgnoreCase))
            {
                if (isSuperAdmin)
                {
                    var branchList = await (
                        from branchMaster in _oBMSDbContext.BranchMasters
                        orderby branchMaster.Name
                        select new
                        {
                            branchMaster.Code,
                            branchMaster.Name
                        }).ToListAsync();

                    results.Add("branchList", branchList);
                }
                else
                {
                    var branchList = await (
                        from branchMaster in _oBMSDbContext.BranchMasters
                        join obmsBranches in _oBMSDbContext.OBMSBranches
                            on branchMaster.Code equals obmsBranches.BranchCode
                        where obmsBranches.Name == name && obmsBranches.IsAllowed == true
                        orderby branchMaster.Name
                        select new
                        {
                            branchMaster.Code,
                            branchMaster.Name
                        }).ToListAsync();

                    results.Add("branchList", branchList);
                }
            }

            return results;
        }


        [HttpGet]
        [Route("EmployeesByBranchId")]
        public async Task<ActionResult<object>> EmployeesByBranchId(string branchId)
        {
            var employees = await (
                from emp in _oBMSDbContext.Employees
                join empDetails in _oBMSDbContext.EmploymentDetails
                    on emp.EMP_CODE equals empDetails.EMPPAY_CODE into empGroup
                from details in empGroup.DefaultIfEmpty()
                where emp.EMP_BRANCH_CODE == branchId
                orderby emp.EMP_ID descending
                select new
                {
                    emp.EMP_ID,
                    emp.EMP_CODE,
                    emp.EMP_BRANCH_CODE,
                    emp.EMP_NAME,
                    emp.EMP_SEX,
                    emp.EMP_IC_NEW,
                    emp.EMP_PASSPORT_NO,
                    emp.EMP_TOWN,
                    EMPPAY_DATE_JOINED = details.EMPPAY_DATE_JOINED,
                    EMPPAY_CATEGORY = details != null ? details.EMPPAY_CATEGORY : null
                }).ToListAsync();

            return Ok(employees);
        }



        [HttpGet]
        [Route("EmployeeById")]
        public async Task<Object> GetEmployeeById(int employeeId)
        {
            try
            {
                return _employeeRepository.GetEmployeeById(employeeId);
            }
            catch (Exception ex)
            {
                throw;
            }

        }



        [HttpGet]
        [Route("GetEmployeeMaster")]
        public async Task<ActionResult<Object>> GetEmployeeMaster(string userId)
        {
            try
            {
                var branchList = await _employeeRepository.GetEmployeeMasterList(userId);
                return Ok(branchList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpGet]
        [Route("GetClientsFromBranchId")]
        public async Task<ActionResult<Object>> GetClientsFromBranchId(string branchId, string empType)
        {
            try
            {
                var clientList = await _employeeRepository.GetClientsFromBranchId(branchId);

                var empNo = await _employeeRepository.GetEmployeeNoByBranchID(branchId);

                var salaryList = await _employeeRepository.GetSalarySlabList(empType, true);

                var results = new Dictionary<string, Object>();

                results.Add("clientList", clientList);
                results.Add("emp", empNo);
                results.Add("salarySlabList", salaryList);

                return results;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetEmployeeNo")]
        public async Task<ActionResult<Object>> GetEmployeeNo()
        {
            try
            {
                var obj = await _employeeRepository.GetEmployeeNo();
                var results = new Dictionary<string, Object>();

                results.Add("emp", obj);

                return results;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        [HttpGet]
        [Route("GetSalarySlabList")]
        public async Task<ActionResult<Object>> GetSalarySlabList(string employeeType, bool nonStructure)
        {
            try
            {
                var slabList = await _employeeRepository.GetSalarySlabList(employeeType, nonStructure);
                return Ok(slabList);
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpPost]
        [Route("SaveAndUpdateEmployee")]
        public async Task<ActionResult<HttpResponseMessage>> SaveAndUpdateEmployee(EmployeeRequestDto employeeRequestDto)
        {
            try
            {
                Dictionary<string, object> dictResult = new Dictionary<string, object>();
                var today = DateTime.Today;
                if (!string.IsNullOrWhiteSpace(employeeRequestDto.EMP_IC_NEW))
                {
                    if (employeeRequestDto.EMP_ID == 0)
                    {
                        //var existingIC = _oBMSDbContext.Employees
                        //    .Where(x => x.EMP_IC_NEW == employeeRequestDto.EMP_IC_NEW)
                        //    .Select(x => x.EMP_IC_NEW)
                        //    .FirstOrDefault();

                        //if (!string.IsNullOrEmpty(existingIC))
                        //{
                        //    dictResult.Add("Success", "Warning");
                        //    dictResult.Add("EmployeeIC", existingIC);
                        //    dictResult.Add("Message", "Employee already has this IC number. Please choose a different IC number.");

                        //    return Ok(dictResult);
                        //}
                       

                        var employeeId = _oBMSDbContext.Employees
                        .Join(_oBMSDbContext.EmployeeSalaryDetails,
                            employee => employee.EMP_CODE,
                            salaryDetails => salaryDetails.EMPFL_CODE,
                            (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails })
                        .Join(_oBMSDbContext.EmploymentDetails,
                            d => d.SalaryDetails.EMPFL_CODE,
                            employmentDetails => employmentDetails.EMPPAY_CODE,
                            (d, employmentDetails) => new { Data = d, EmploymentDetails = employmentDetails })
                        .Where(result =>
                            result.Data.Employee.EMP_IC_NEW == employeeRequestDto.EMP_IC_NEW &&
                            result.Data.Employee.HasTransfered == false &&
                            (result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null ||
                             result.EmploymentDetails.EMPPAY_DATE_RESIGNED > today)) // ✅ Updated
                        .Select(result => result.Data.Employee.EMP_ID)
                        .FirstOrDefault();

                        if (employeeId > 0)
                        {
                            dictResult.Add("Success", "Warning");
                            dictResult.Add("EmployeeIC", employeeId);
                            dictResult.Add("Message", "Employee already has this IC number. Please choose a different IC number.");

                            return Ok(dictResult);
                        }
                    }
                }

                // Continue with rest of the flow...
                // Validate branch-change date is provided when branch is being changed
                if (employeeRequestDto.EMP_ID != 0
                    && employeeRequestDto.IsBranchChanged
                    && !employeeRequestDto.BranchStartDate.HasValue)
                {
                    dictResult.Add("Success", "Warning");
                    dictResult.Add("Message", "Branch Start Date is required when changing the branch.");
                    return Ok(dictResult);
                }
                var employee = new Employee();
                if (employeeRequestDto.EMP_ID != 0)
                {
                    employee = _oBMSDbContext.Employees.Where(x => x.EMP_ID == employeeRequestDto.EMP_ID).FirstOrDefault();
                }

                employee.EMP_ID = employeeRequestDto.EMP_ID;
                employee.EMP_ROLE = employeeRequestDto.EMP_ROLE;
                employee.EMP_CODE = employeeRequestDto.EMP_CODE;
                employee.EMP_NAME = employeeRequestDto.EMP_NAME;
                employee.EMP_ADDRESS1 = employeeRequestDto.EMP_ADDRESS1;
                employee.EMP_ADDRESS2 = employeeRequestDto.EMP_ADDRESS2;
                employee.EMP_POST_CODE = employeeRequestDto.EMP_POST_CODE;
                employee.EMP_TOWN = employeeRequestDto.EMP_TOWN;
                employee.EMP_STATE = employeeRequestDto.EMP_STATE;
                employee.EMP_NATIONAL = employeeRequestDto.EMP_NATIONAL;
                employee.EMP_PHONE = employeeRequestDto.EMP_PHONE;
                employee.EMP_HGH_EDU = employeeRequestDto.EMP_HGH_EDU;
                employee.EM_WORK_EXP = employeeRequestDto.EM_WORK_EXP;
                employee.EMP_DATE_OF_BIRTH = employeeRequestDto.EMP_DATE_OF_BIRTH;
                employee.EMP_IC_OLD = employeeRequestDto.EMP_IC_OLD;
                employee.EMP_IC_NEW = employeeRequestDto.EMP_IC_NEW;
                employee.EMP_IC_COLOR = employeeRequestDto.EMP_IC_COLOR;
                employee.EMP_PASSPORT_NO = employeeRequestDto.EMP_PASSPORT_NO;
                employee.EMP_SEX = employeeRequestDto.EMP_SEX;
                employee.EMP_RACE = employeeRequestDto.EMP_RACE;
                employee.EMP_MARTIAL_STATUS = employeeRequestDto.EMP_MARTIAL_STATUS;
                employee.EMP_SPOUSE_NAME = employeeRequestDto.EMP_SPOUSE_NAME;
                employee.EMP_SP_IC = employeeRequestDto.EMP_SP_IC;
                employee.EMP_NO_CHILD = employeeRequestDto.EMP_NO_CHILD;
                employee.EMP_SP_WORK = employeeRequestDto.EMP_SP_WORK;
                employee.EMP_PER_NAME_CONTACT = employeeRequestDto.EMP_PER_NAME_CONTACT;
                employee.EMP_CONTACT_ADDRESS1 = employeeRequestDto.EMP_CONTACT_ADDRESS1;
                employee.EMP_CONTACT_ADDRESS2 = employeeRequestDto.EMP_CONTACT_ADDRESS2;
                employee.EMP_CONTACT_POST_CODE = employeeRequestDto.EMP_CONTACT_POST_CODE;
                employee.EMP_CONTACT_TOWN = employeeRequestDto.EMP_CONTACT_TOWN;
                employee.EMP_CONTACT_STATE = employeeRequestDto.EMP_CONTACT_STATE;
                employee.EMP_CONTACT_TELEPHONE = employeeRequestDto.EMP_CONTACT_TELEPHONE;
                employee.EMP_BRANCH_CODE = employeeRequestDto.EMP_BRANCH_CODE;
                employee.OldBranch = employeeRequestDto.OldBranch;
                // Do not update TransferDate on employee update, only set during transfer operation
                // For new employees, leave TransferDate as NULL (default)
                if (employeeRequestDto.EMP_ID == 0)
                {
                    employee.TransferDate = null; // New employee: explicitly set to NULL
                }
                // For updates (EMP_ID != 0), do not modify TransferDate - preserve existing value
                employee.HasTransfered = employeeRequestDto.HasTransfered;
                employee.LASTUPDATE = employeeRequestDto.LASTUPDATE;
                employee.LastUpdatedBy = employeeRequestDto.LastUpdatedBy;
                employee.EMP_MOBILEPHONE = employeeRequestDto.EMP_MOBILEPHONE;
                employee.EMP_CITIZEN = employeeRequestDto.EMP_CITIZEN;
                employee.EMP_CHECKLIST = employeeRequestDto.EMP_CHECKLIST;
                employee.EMP_CLIENT = employeeRequestDto.EMP_CLIENT;
                employee.NewSalaryStructure = employeeRequestDto.NewSalaryStructure;
                employee.KDNVetting = employeeRequestDto.KDNVetting;
                employee.SalaryStructure1000_3h = employeeRequestDto.SalaryStructure1000_3h;
                employee.VisaExpiryDate = employeeRequestDto.VisaExpiryDate;
                employee.PassportExpiryDate = employeeRequestDto.PassportExpiryDate;
                employee.FOMEMA_MedicalCheckupDate = employeeRequestDto.FOMEMA_MedicalCheckupDate;
                employee.FOMEMA_1stAppealDate = employeeRequestDto.FOMEMA_1stAppealDate;
                employee.FOMEMA_2ndAppealDate = employeeRequestDto.FOMEMA_2ndAppealDate;
                employee.FOMEMA_NCDMonitoringDate = employeeRequestDto.FOMEMA_NCDMonitoringDate;
                employee.FOMEMA_ResultDate = employeeRequestDto.FOMEMA_ResultDate;


                var employment = new EmploymentDetails();
                if (employeeRequestDto.EMPPAY_ID != 0)
                {
                    employment = _oBMSDbContext.EmploymentDetails.Where(x => x.EMPPAY_ID == employeeRequestDto.EMPPAY_ID).FirstOrDefault();
                }



                employment.EMPPAY_ID = employeeRequestDto.EMPPAY_ID;
                employment.EMPPAY_BRANCHCODE = employeeRequestDto.EMP_BRANCH_CODE;
                employment.EMPPAY_JOB_TITLE = employeeRequestDto.EMPPAY_JOB_TITLE;
                employment.EMPPAY_CATEGORY = employeeRequestDto.EMPPAY_CATEGORY;
                employment.EMPPAY_DATE_JOINED = (DateTime)employeeRequestDto.EMPPAY_DATE_JOINED;
                employment.EMPPAY_DATE_CONFIRM = employeeRequestDto.EMPPAY_DATE_CONFIRM;
                //employment.EMPPAY_DATE_RESIGNED = (DateTime)employeeRequestDto.EMPPAY_DATE_RESIGNED;

                employment.EMPPAY_DATE_RESIGNED = employeeRequestDto.EMPPAY_DATE_RESIGNED.HasValue
                                                     ? (DateTime)employeeRequestDto.EMPPAY_DATE_RESIGNED.Value
                                                     : null;


                employment.EMPPAY_BASIC_RATE = employeeRequestDto.EMPPAY_BASIC_RATE;
                employment.SALARYLAB = employeeRequestDto.SALARYLAB;
                employment.ATTENDANCEALLOWANCE = employeeRequestDto.ATTENDANCEALLOWANCE;
                employment.NewStructureATTENDANCEALLOWANCE = 0;
                employment.SpecialAllowance = employeeRequestDto.SpecialAllowance;
                employment.LASTUPDATE = employeeRequestDto.LASTUPDATE;
                employment.LastUpdatedBy = employeeRequestDto.LastUpdatedBy;
                employment.AttendanceAllowanceWorkingDays = employeeRequestDto.AttendanceAllowanceWorkingDays;
                employment.AttendanceAllowanceFollowCalendar = employeeRequestDto.AttendanceAllowanceFollowCalendar;
                employment.KPI = employeeRequestDto.KPI;

                var salaryDetails = new EmployeeSalaryDetails();
                if (employeeRequestDto.EMPFL_ID != 0)
                {
                    salaryDetails = _oBMSDbContext.EmployeeSalaryDetails.Where(x => x.EMPFL_ID == employeeRequestDto.EMPFL_ID).FirstOrDefault();
                }

                salaryDetails.EMPFL_ID = employeeRequestDto.EMPFL_ID;
                salaryDetails.EMPFL_BRANCHCODE = employeeRequestDto.EMP_BRANCH_CODE;
                salaryDetails.EMPFL_BANK = employeeRequestDto.EMPFL_BANK;
                salaryDetails.EMPFL_BK_ACCNO = employeeRequestDto.EMPFL_BK_ACCNO;
                salaryDetails.EMPFL_TAX_NO = employeeRequestDto.EMPFL_TAX_NO;
                salaryDetails.EMPFL_EPFNO = employeeRequestDto.EMPFL_EPFNO;
                salaryDetails.EMPFL_EPF8Pa = employeeRequestDto.EMPFL_EPF8Pa;
                salaryDetails.EMPFL_SOSCO_NO = employeeRequestDto.EMPFL_SOSCO_NO;
                salaryDetails.SKBBK = employeeRequestDto.SKBBK;
                salaryDetails.EPFDETECT = employeeRequestDto.EPFDETECT;
                salaryDetails.PAYMODE = employeeRequestDto.PAYMODE;
                salaryDetails.SOCSODETECT = employeeRequestDto.SOCSODETECT;
                salaryDetails.TMPGUARD = employeeRequestDto.TMPGUARD;
                salaryDetails.DETECTBYND55 = employeeRequestDto.DETECTBYND55;
                salaryDetails.INCOMETAXDETECT = employeeRequestDto.INCOMETAXDETECT;
                salaryDetails.EMP_SP_TEL_NO = employeeRequestDto.EMP_SP_TEL_NO;
                salaryDetails.PAYMODE2 = employeeRequestDto.PAYMODE2;
                salaryDetails.SplitSalaryPayment = employeeRequestDto.SplitSalaryPayment;
                salaryDetails.EMPFL_2ndBank = employeeRequestDto.EMPFL_2ndBank;
                salaryDetails.EMPFL_2ndBK_ACCNO = employeeRequestDto.EMPFL_2ndBK_ACCNO;
                salaryDetails.LASTUPDATE = employeeRequestDto.LASTUPDATE;
                salaryDetails.LastUpdatedBy = employeeRequestDto.LastUpdatedBy;



                await _employeeRepository.saveAndUpdateEmployee(
                    employee,
                    employment,
                    salaryDetails,
                    isBranchChanged: employeeRequestDto.IsBranchChanged,
                    branchStartDate: employeeRequestDto.BranchStartDate);

                if (true)
                {
                    dictResult.Add("Success", "Success");
                    dictResult.Add("Employee", employee);
                    response.Headers.Add("Success", "Successfully save & update Employee details.");

                    return Ok(dictResult);
                }
                else
                {
                    response.Headers.Add("Failure", "Employee save & update failed please check ");
                }
                return response;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        [HttpPost]
        [Route("UpdateEmployeeTransfer")]
        public async Task<ActionResult<HttpResponseMessage>> UpdateEmployeeTransfer(EmployeeTransferDto employeeTransferDto)
        {
            await _employeeRepository.UpdateEmployeeTransfer(employeeTransferDto);


            Dictionary<string, object> dictResult = new Dictionary<string, object>();
            dictResult.Add("Success", "Success");

            response.Headers.Add("Success", "Successfully save & update Employee details.");

            return Ok(dictResult);
        }

        [HttpGet]
        [Route("CheckEmployeeInfo")]
        public async Task<ActionResult<Object>> CheckEmployeeInfo(string from, string data)
        {
            try
            {

                var dictResult = _employeeRepository.CheckEmployeeInfo(from, data);


                return Ok(dictResult);
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpGet("GetAllEmployeesWithHistory")]
        public async Task<IActionResult> GetAllEmployeesWithHistory(string? branch = null)
        {
            try
            {
                var result = await _employeeRepository.GetAllEmployeesWithHistory(branch);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while retrieving employee history.",
                    Details = ex.Message
                });
            }
        }

    }
}
