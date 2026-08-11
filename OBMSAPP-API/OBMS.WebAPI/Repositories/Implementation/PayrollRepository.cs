using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using SkiaSharp;
using Syncfusion.XlsIO.Implementation.Security;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace OBMS.WebAPI.Repositories.Implementation
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly OBMSDbContext _oBMSDbContext;
        public PayrollRepository(OBMSDbContext oBMSDbContext)
        {
            _oBMSDbContext = oBMSDbContext;
        }

        #region Monthly Salary Advance
        public async Task<List<SalaryAdvance>> GetSalaryAdvanceById(int employeeId,int id)
        {
            var result = await _oBMSDbContext.SalaryAdvances.Where(sa => sa.EmployeeID == employeeId && sa.ID == id).ToListAsync();
            return new List<SalaryAdvance>(result);
        }
        public async Task<List<Employee>> GetEmployeeById(int employeeId)
        {
            var result = await _oBMSDbContext.Employees.Where(sa => sa.EMP_ID == employeeId).ToListAsync();
            return new List<Employee>(result);
        }
        public async Task<List<SalaryAdvance>> GetSalaryAdvanceByDateAndEmployee(SalaryAdvance salaryAdvance)
        {
            var result = await _oBMSDbContext.SalaryAdvances
                .Where(sa => sa.AdvanceDate == salaryAdvance.AdvanceDate &&
                              sa.EmployeeID == salaryAdvance.EmployeeID &&
                              sa.TransType == salaryAdvance.TransType &&
                              !sa.IsDeleted) // Assuming IsDeleted is a boolean property
                .ToListAsync();

            return result;
        }
        public async Task<List<SalaryAdvanceDto>> GetEmployeeList()
        {
            var result = (from employee in _oBMSDbContext.Employees
                          join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                          join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
                          select new
                          {
                              employee.EMP_ID,
                              employee.EMP_NAME,
                              employee.EMP_IC_NEW,
                              employee.EMP_IC_OLD,
                              employee.EMP_PASSPORT_NO,
                              salaryDetails.EMPFL_BANK,
                              salaryDetails.EMPFL_BK_ACCNO,
                              salaryDetails.PAYMODE
                          }).ToList();

            var salaryAdvanceList = result.Select(x => new SalaryAdvanceDto
            {
                EMP_ID = x.EMP_ID,
                EMP_NAME = string.IsNullOrEmpty(x.EMP_NAME)
                            ? x.EMP_NAME
                            : x.EMP_NAME.Replace("''", "'"),
                EMP_IC_NEW = x.EMP_IC_NEW,
                EMP_IC_OLD = x.EMP_IC_OLD,
                EMP_PASSPORT_NO = x.EMP_PASSPORT_NO,
                EMPFL_BANK = x.EMPFL_BANK,
                EMPFL_BK_ACCNO = x.EMPFL_BK_ACCNO,
                PAYMODE = x.PAYMODE,
            }).ToList();

            return new List<SalaryAdvanceDto>(salaryAdvanceList);
        }

        //public async Task<List<SalaryAdvanceDto>> GetEmployeeListBySalaryAdvance(int TransType,string currentUser)
        //{
        //    bool isSuperAdmin = currentUser.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

        //    var result = await (from employee in _oBMSDbContext.Employees
        //                        join ob in _oBMSDbContext.OBMSBranches on employee.EMP_BRANCH_CODE equals ob.BranchCode
        //                        join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
        //                        join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
        //                        ////join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID into salaryAdvanceGroup
        //                        ////  from salaryAdvances in salaryAdvanceGroup.DefaultIfEmpty()
        //                        join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID
        //                        where salaryAdvance.TransType == TransType && salaryAdvance.IsDeleted == false 
        //                        && (isSuperAdmin || ob.Name == currentUser)
        //                        select new
        //                        {
        //                            salaryAdvance.ID,
        //                            employee.EMP_ID,
        //                            employee.EMP_NAME,
        //                            employee.EMP_IC_NEW,
        //                            employee.EMP_IC_OLD,
        //                            employee.EMP_PASSPORT_NO,
        //                            salaryDetails.EMPFL_BANK,
        //                            salaryDetails.EMPFL_BK_ACCNO,
        //                            salaryDetails.PAYMODE,
        //                            salaryAdvance.Amount,
        //                            salaryAdvance.Particulars
        //                        }).ToListAsync();

        //    var salaryAdvanceList = result.Select(x => new SalaryAdvanceDto
        //    {
        //        ID = x.ID,
        //        EMP_ID = x.EMP_ID,
        //        EMP_NAME = x.EMP_NAME,
        //        EMP_IC_NEW = x.EMP_IC_NEW,
        //        EMP_IC_OLD = x.EMP_IC_OLD,
        //        EMP_PASSPORT_NO = x.EMP_PASSPORT_NO,
        //        EMPFL_BANK = x.EMPFL_BANK,
        //        EMPFL_BK_ACCNO = x.EMPFL_BK_ACCNO,
        //        PAYMODE = x.PAYMODE,
        //        Amount = x.Amount,
        //        Particulars = x.Particulars,
        //    }).ToList();

        //    return new List<SalaryAdvanceDto>(salaryAdvanceList);
        //}

        public async Task<List<SalaryAdvanceDto>> GetEmployeeListBySalaryAdvance(int TransType, string currentUser)
        {
            bool isSuperAdmin = currentUser.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

            var query = from salaryAdvance in _oBMSDbContext.SalaryAdvances
                        join employee in _oBMSDbContext.Employees
                            on salaryAdvance.EmployeeID equals employee.EMP_ID
                        join branch in _oBMSDbContext.OBMSBranches
                            on employee.EMP_BRANCH_CODE equals branch.BranchCode
                        join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails
                            on employee.EMP_CODE equals salaryDetails.EMPFL_CODE into salaryDetailsGroup
                        from sd in salaryDetailsGroup.DefaultIfEmpty()
                        join employment in _oBMSDbContext.EmploymentDetails
                            on employee.EMP_CODE equals employment.EMPPAY_CODE into employmentGroup
                        from emp in employmentGroup.DefaultIfEmpty()
                        where salaryAdvance.TransType == TransType
                              && salaryAdvance.IsDeleted == false
                              && (isSuperAdmin || branch.Name == currentUser)
                        select new
                        {
                            salaryAdvance.ID,
                            employee.EMP_ID,
                            employee.EMP_NAME,
                            employee.EMP_IC_NEW,
                            employee.EMP_IC_OLD,
                            employee.EMP_PASSPORT_NO,
                            EMPFL_BANK = sd != null ? sd.EMPFL_BANK : null,
                            EMPFL_BK_ACCNO = sd != null ? sd.EMPFL_BK_ACCNO : null,
                            PAYMODE = sd != null ? sd.PAYMODE : null,
                            salaryAdvance.Amount,
                            salaryAdvance.Particulars
                        };

            // Ensure distinct SalaryAdvance IDs to prevent duplicates
            var result = await query
                .GroupBy(x => x.ID)
                .Select(g => g.First())
                .ToListAsync();

            var salaryAdvanceList = result.Select(x => new SalaryAdvanceDto
            {
                ID = x.ID,
                EMP_ID = x.EMP_ID,
                EMP_NAME = string.IsNullOrEmpty(x.EMP_NAME)
                            ? x.EMP_NAME
                            : x.EMP_NAME.Replace("''", "'"),
                EMP_IC_NEW = x.EMP_IC_NEW,
                EMP_IC_OLD = x.EMP_IC_OLD,
                EMP_PASSPORT_NO = x.EMP_PASSPORT_NO,
                EMPFL_BANK = x.EMPFL_BANK,
                EMPFL_BK_ACCNO = x.EMPFL_BK_ACCNO,
                PAYMODE = x.PAYMODE,
                Amount = x.Amount,
                Particulars = x.Particulars
            }).ToList();

            return salaryAdvanceList;
        }

        public async Task<List<SalaryAdvanceDto>> GetEmployeeListByBranchCode(string branchCode)
        {
            var result = await (from employee in _oBMSDbContext.Employees
                                join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                                join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
                                join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID into advancesGroup
                                from salaryAdvance in advancesGroup.DefaultIfEmpty()
                                where employee.EMP_BRANCH_CODE == branchCode
                                select new SalaryAdvanceDto
                                {
                                    ID = (salaryAdvance != null) ? salaryAdvance.ID : 0,
                                    EMP_ID = employee.EMP_ID,
                                    EMP_NAME = employee.EMP_NAME,
                                    EMP_CODE = employee.EMP_CODE,
                                    EMP_IC_NEW = employee.EMP_IC_NEW,
                                    EMP_IC_OLD = employee.EMP_IC_OLD,
                                    EMP_PASSPORT_NO = employee.EMP_PASSPORT_NO,
                                    EMPFL_BANK = salaryDetails.EMPFL_BANK,
                                    EMPFL_BK_ACCNO = salaryDetails.EMPFL_BK_ACCNO,
                                    PAYMODE = salaryDetails.PAYMODE,
                                    Amount = (salaryAdvance != null) ? salaryAdvance.Amount : 0,
                                    Particulars = (salaryAdvance != null) ? salaryAdvance.Particulars : ""
                                })
                    .GroupBy(dto => dto.EMP_ID) // Group by employee ID
                    .Select(group => group.First()) // Select the first element of each group
                    .ToListAsync();


            return new List<SalaryAdvanceDto>(result);
        }
        public async Task<List<SalaryAdvanceDto>> GetEmployeeListByAdvanceID(int Id)
        {
            var result = await (from employee in _oBMSDbContext.Employees
                                join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                                join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
                                join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID into advancesGroup
                                from salaryAdvance in advancesGroup.DefaultIfEmpty()
                                where salaryAdvance.ID == Id
                                select new SalaryAdvanceDto
                                {
                                    ID = (salaryAdvance != null) ? salaryAdvance.ID : 0,
                                    EMP_ID = employee.EMP_ID,
                                    EMP_NAME = employee.EMP_NAME,
                                    EMP_IC_NEW = employee.EMP_IC_NEW,
                                    EMP_IC_OLD = employee.EMP_IC_OLD,
                                    EMP_PASSPORT_NO = employee.EMP_PASSPORT_NO,
                                    EMPFL_BANK = salaryDetails.EMPFL_BANK,
                                    EMPFL_BK_ACCNO = salaryDetails.EMPFL_BK_ACCNO,
                                    PAYMODE = salaryDetails.PAYMODE,
                                    Amount = (salaryAdvance != null) ? salaryAdvance.Amount : 0,
                                    Particulars = (salaryAdvance != null) ? salaryAdvance.Particulars : ""
                                }).ToListAsync();

            return new List<SalaryAdvanceDto>(result);
        }
        public async Task<SalaryAdvance> SaveAndUpdateSalaryMonthlyAdvance(SalaryAdvance salaryAdvance)
        {
            var existingRecord = await _oBMSDbContext.SalaryAdvances
                .FirstOrDefaultAsync(s => s.ID == salaryAdvance.ID);

            if (existingRecord != null)
            {
                // Update properties
                existingRecord.EmployeeID = salaryAdvance.EmployeeID;
                existingRecord.AdvanceTakenDate = salaryAdvance.AdvanceTakenDate;
                existingRecord.AdvanceDate = salaryAdvance.AdvanceDate;
                existingRecord.VoucherNo = salaryAdvance.VoucherNo;
                existingRecord.Amount = salaryAdvance.Amount;
                existingRecord.NoOfInstallments = salaryAdvance.NoOfInstallments;
                existingRecord.PaymentType = salaryAdvance.PaymentType;
                existingRecord.Particulars = salaryAdvance.Particulars;
                existingRecord.TransType = salaryAdvance.TransType;
                existingRecord.IsDeleted = salaryAdvance.IsDeleted;
                existingRecord.LastUpdate = DateTime.Now;
                existingRecord.LastUpdatedBy = salaryAdvance.LastUpdatedBy;
                _oBMSDbContext.Update(existingRecord);
                await _oBMSDbContext.SaveChangesAsync();
                return existingRecord;
            }
            else
            {
                _oBMSDbContext.Add(salaryAdvance);
                await _oBMSDbContext.SaveChangesAsync();
                return salaryAdvance;
            }


        }

        public async Task SaveEmployeeItemIssuesAsync(List<EmployeeItemIssue> items)
        {
            foreach (var item in items)
            {
                // Never save items with invalid AdvanceID
                if (item.AdvanceID <= 0) continue;

                // Use AsNoTracking so the check doesn't conflict with the entity we're about to add/update
                var existing = await _oBMSDbContext.EmployeeItemIssues
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.AdvanceID == item.AdvanceID && e.ItemID == item.ItemID);

                decimal safePrice = item.Price;

                if (existing != null)
                {
                    // Update � attach a new entity with the known ID
                    var updated = new EmployeeItemIssue
                    {
                        ID = existing.ID,
                        AdvanceID = item.AdvanceID,
                        ItemID = item.ItemID,
                        Quantity = item.Quantity,
                        Price = safePrice,
                        LastUpdate = DateTime.Now,
                        LastUpdatedBy = item.LastUpdatedBy
                    };
                    _oBMSDbContext.EmployeeItemIssues.Update(updated);
                }
                else if (item.Quantity > 0)
                {
                    // Insert � create a fresh entity so EF uses identity for ID
                    var newItem = new EmployeeItemIssue
                    {
                        AdvanceID = item.AdvanceID,
                        ItemID = item.ItemID,
                        Quantity = item.Quantity,
                        Price = safePrice,
                        LastUpdate = DateTime.Now,
                        LastUpdatedBy = item.LastUpdatedBy
                    };
                    _oBMSDbContext.EmployeeItemIssues.Add(newItem);
                }

                // Save after each item to avoid duplicate tracking conflicts
                await _oBMSDbContext.SaveChangesAsync();
            }
        }
        #endregion
        public async Task<List<InventoryCategory>> GetInventoryCategories()
        {
            var query = _oBMSDbContext.InventoryCategories
                .OrderBy(ic => ic.Name)
                .Select(ic => new InventoryCategory
                {
                    ID = ic.ID,
                    Name = $"{ic.Name}({(ic.Cat == "P" ? "Purchase" : "Expenses")})",
                    Cat = ic.Cat,
                    AssetType = ic.AssetType
                });

            return await query.ToListAsync();
        }
        public string GetNewVoucherNumberAsync(int transType)
        {
            var result = _oBMSDbContext.SalaryAdvances
                .Where(s => s.TransType == transType)
                .OrderByDescending(s => s.LastUpdate) // Ensure latest record comes first
                .AsEnumerable()
                .Select(s =>
                    int.TryParse(s.VoucherNo, out var voucherNo) ? voucherNo : (int?)null) // Parse VoucherNo
                .Where(v => v.HasValue) // Exclude null values
                .Max() ?? 0; // Get the highest valid VoucherNo or default to 0

            var newVoucherNo = result + 1;

            return newVoucherNo.ToString("000000");
        }
        public List<ItemMasterDto> GetUniformItemRows(int AdvanceID, int Category)
        {
            // When AdvanceID = 0 (new record), skip the join � return all items with Quantity 0
            if (AdvanceID == 0)
            {
                return _oBMSDbContext.ItemMasters
                    .Where(im => im.CategoryID == Category)
                    .OrderBy(im => im.Name)
                    .Select(im => new ItemMasterDto
                    {
                        ID = 0,
                        AdvanceID = 0,
                        ItemID = im.ID,
                        Name = im.Name,
                        Price = im.SellPrice ?? 0,
                        Quantity = 0
                    })
                    .ToList();
            }

            var query = from itemMaster in _oBMSDbContext.ItemMasters
                        join employeeItemIssue in _oBMSDbContext.EmployeeItemIssues
                            on new { ItemID = itemMaster.ID, AdvanceID }
                            equals new { ItemID = employeeItemIssue.ItemID, AdvanceID = employeeItemIssue.AdvanceID }
                            into itemIssueGroup
                        from employeeItemIssue in itemIssueGroup.DefaultIfEmpty()
                        where itemMaster.CategoryID == Category
                        select new ItemMasterDto
                        {
                            ID = employeeItemIssue != null ? employeeItemIssue.ID : 0,
                            AdvanceID = employeeItemIssue != null ? employeeItemIssue.AdvanceID : 0,
                            ItemID = itemMaster.ID,
                            Name = itemMaster.Name,
                            Price = itemMaster.SellPrice ?? 0,
                            Quantity = employeeItemIssue != null ? employeeItemIssue.Quantity : 0
                        };

            return query.OrderBy(x => x.Name).ToList();
        }
        public bool GetSalaryProcessDateByEmployeeID(int employeeID, int year, int month)
        {
            var payslipExists = _oBMSDbContext.PaySlips
                .Where(p => p.EmployeeID == employeeID && p.Period.Year == year && p.Period.Month == month)
                .OrderByDescending(p => p.Period)
                .Take(1)
                .Any();

            return payslipExists;

        }
        public DateTime GetResignDateByEmployeeID(int employeeID)
        {
            var resignDate = _oBMSDbContext.Employees
                .Join(
                    _oBMSDbContext.EmploymentDetails,
                    employee => employee.EMP_CODE,
                    employmentDetails => employmentDetails.EMPPAY_CODE,
                    (employee, employmentDetails) => new { employee, employmentDetails }
                )
                .Where(joinResult => !joinResult.employee.HasTransfered && joinResult.employee.EMP_ID == employeeID)
                .Select(joinResult => joinResult.employmentDetails.EMPPAY_DATE_RESIGNED)
                .FirstOrDefault();
            return resignDate ?? new DateTime(1900, 1, 1);

            //return (DateTime)(resignDate != default(DateTime) ? resignDate : new DateTime(1900, 1, 1));

        }
        #region MiscTransaction Region
        public async Task<IEnumerable<MiscTrans>> GetMiscTrans(string currentUser)
        {
            bool isSuperAdmin = currentUser.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

            var query = from m in _oBMSDbContext.MiscTrans
                        join e in _oBMSDbContext.Employees on m.EmployeeID equals e.EMP_ID
                        join ob in _oBMSDbContext.OBMSBranches on e.EMP_BRANCH_CODE equals ob.BranchCode
                        where isSuperAdmin || m.LastUpdatedBy == currentUser
                        select m;

            return await query.Distinct().ToListAsync();
        }
        public async Task<List<MiscTrans>> GetMiscTransById(int id)
        {
            var result = await _oBMSDbContext.MiscTrans.Where(m => m.ID == id).ToListAsync();
            return new List<MiscTrans>(result);
        }
        public async Task<MiscTrans> SaveAndUpdateMiscTrans(MiscTrans miscTrans)
        {
            var existingMiscTrans = await _oBMSDbContext.MiscTrans.FirstOrDefaultAsync(mt => mt.ID == miscTrans.ID);
            if (existingMiscTrans != null)
            {
                existingMiscTrans.TransDate = miscTrans.TransDate;
                existingMiscTrans.EmployeeID = miscTrans.EmployeeID;
                existingMiscTrans.TransType = miscTrans.TransType;
                existingMiscTrans.Amount = miscTrans.Amount;
                existingMiscTrans.Particulars = miscTrans.Particulars;
                existingMiscTrans.LastUpdate = DateTime.Now;
                existingMiscTrans.LastUpdatedBy = miscTrans.LastUpdatedBy;
                _oBMSDbContext.Update(existingMiscTrans);
                await _oBMSDbContext.SaveChangesAsync();
                return existingMiscTrans;
            }
            else
            {
                miscTrans.LastUpdate = DateTime.Now;
                _oBMSDbContext.MiscTrans.Add(miscTrans);
                await _oBMSDbContext.SaveChangesAsync();
                return miscTrans;
            }

        }

        public async Task DeleteMiscTransById(int id)
        {
            var miscTrans = await _oBMSDbContext.MiscTrans.FindAsync(id);
            _oBMSDbContext.MiscTrans.Remove(miscTrans);
            await _oBMSDbContext.SaveChangesAsync();
        }

        #endregion
        public async Task<List<SalaryAdvance>> SaveAndUpdateSalaryDailyAdvances(List<SalaryAdvance> salaryAdvances)
        {
            List<SalaryAdvance> updatedRecords = new List<SalaryAdvance>();

            foreach (var salaryAdvance in salaryAdvances)
            {
                var existingRecord = await _oBMSDbContext.SalaryAdvances
                    .FirstOrDefaultAsync(s => s.ID == salaryAdvance.ID);

                if (existingRecord != null)
                {
                    // Update properties
                    existingRecord.EmployeeID = salaryAdvance.EmployeeID;
                    existingRecord.AdvanceTakenDate = salaryAdvance.AdvanceTakenDate;
                    existingRecord.AdvanceDate = salaryAdvance.AdvanceDate;
                    existingRecord.VoucherNo = salaryAdvance.VoucherNo;
                    existingRecord.Amount = salaryAdvance.Amount;
                    existingRecord.NoOfInstallments = salaryAdvance.NoOfInstallments;
                    existingRecord.PaymentType = salaryAdvance.PaymentType;
                    existingRecord.Particulars = salaryAdvance.Particulars;
                    existingRecord.TransType = salaryAdvance.TransType;
                    existingRecord.IsDeleted = salaryAdvance.IsDeleted;
                    existingRecord.LastUpdate = DateTime.Now;
                    existingRecord.LastUpdatedBy = salaryAdvance.LastUpdatedBy;

                    _oBMSDbContext.Update(existingRecord);
                    updatedRecords.Add(existingRecord);
                }
                else
                {
                    _oBMSDbContext.Add(salaryAdvance);
                    updatedRecords.Add(salaryAdvance);
                }
            }

            await _oBMSDbContext.SaveChangesAsync();
            return updatedRecords;
        }

        public List<EmployeeDailyAdvanceRow> GetDailyAdvanceList(DateTime advanceDate, int employeeID, int advanceType)
        {
            if (employeeID > 0)
            {

                int noOfDays = DateTime.DaysInMonth(advanceDate.Year, advanceDate.Month);
                List<EmployeeDailyAdvanceRow> employeeDailyAdvanceRowList = new List<EmployeeDailyAdvanceRow>();
                int startDay = 1;

                var employee = _oBMSDbContext.Employees.Where(e => e.EMP_ID == employeeID).SingleOrDefault().EMP_CODE;

                if (employee != null)
                {
                    var employment = Get(employee);
                    if (employment.EMPPAY_DATE_RESIGNED?.Year != 1)
                    {
                        if (advanceDate > employment.EMPPAY_DATE_RESIGNED)
                        {
                            startDay = 0;
                            //noOfDays = 0;
                        }
                        else if ((advanceDate.Month == employment.EMPPAY_DATE_RESIGNED?.Month) && (advanceDate.Year == employment.EMPPAY_DATE_RESIGNED?.Year))
                        {
                            noOfDays = (int)(employment.EMPPAY_DATE_RESIGNED?.Day);
                        }
                    }

                    if ((advanceDate.Month == employment.EMPPAY_DATE_JOINED.Month) && (advanceDate.Year == employment.EMPPAY_DATE_JOINED.Year))
                    {
                        startDay = employment.EMPPAY_DATE_JOINED.Day;
                    }

                    for (int i = startDay; i <= noOfDays; i++)
                    {
                        employeeDailyAdvanceRowList.Add(new EmployeeDailyAdvanceRow(0, 0, i, 0, "", 0, "", "", 0, false, "", DateTime.MinValue, DateTime.MinValue));
                    }

                    var salaryAdvances = _oBMSDbContext.SalaryAdvances
                        .Where(sa => sa.TransType == advanceType &&
                                     sa.AdvanceDate.Month == advanceDate.Month &&
                                     sa.AdvanceDate.Year == advanceDate.Year &&
                                     sa.EmployeeID == employeeID &&
                                     !sa.IsDeleted)
                        .ToList();

                    foreach (var salaryAdvance in salaryAdvances)
                    {
                        int dayIndex = salaryAdvance.AdvanceDate.Day - 1; // Subtract 1 to convert day number to zero-based index
                        employeeDailyAdvanceRowList[dayIndex].ID = salaryAdvance.ID;
                        employeeDailyAdvanceRowList[dayIndex].Day = salaryAdvance.AdvanceDate.Day;
                        employeeDailyAdvanceRowList[dayIndex].Amount = salaryAdvance.Amount;
                        employeeDailyAdvanceRowList[dayIndex].EmployeeID = salaryAdvance.EmployeeID;
                        employeeDailyAdvanceRowList[dayIndex].TransType = salaryAdvance.TransType;
                        employeeDailyAdvanceRowList[dayIndex].Particulars = salaryAdvance.Particulars;
                        employeeDailyAdvanceRowList[dayIndex].IsDeleted = salaryAdvance.IsDeleted;
                        employeeDailyAdvanceRowList[dayIndex].LastUpdatedBy = salaryAdvance.LastUpdatedBy;
                        employeeDailyAdvanceRowList[dayIndex].VoucherNo = salaryAdvance.VoucherNo;
                        employeeDailyAdvanceRowList[dayIndex].PaymentType = salaryAdvance.PaymentType;
                        employeeDailyAdvanceRowList[dayIndex].NoOfInstallments = salaryAdvance.NoOfInstallments;
                        employeeDailyAdvanceRowList[dayIndex].AdvanceDate = salaryAdvance.AdvanceDate;
                        employeeDailyAdvanceRowList[dayIndex].AdvanceTakenDate = salaryAdvance.AdvanceTakenDate;
                    }
                    return employeeDailyAdvanceRowList;
                }
            }

            return new List<EmployeeDailyAdvanceRow>(); // Return empty list if employee is not found

        }

        public EmploymentDetails Get(string employeeNo)
        {
            var employmentDetails = _oBMSDbContext.EmploymentDetails.FirstOrDefault(ed => ed.EMPPAY_CODE == employeeNo);

            if (employmentDetails != null)
            {
                return employmentDetails;
            }
            return employmentDetails;
        }
        public async Task<string?> GetEmployeeNoAsync(int employeeId)
        {
            var employee = await _oBMSDbContext.Employees
                .Where(e => e.EMP_ID == employeeId)
                .Select(e => e.EMP_CODE)
                .FirstOrDefaultAsync();

            return employee;
        }

        public async Task<IEnumerable<SalaryAdvance>> GetSalaryAdvancesAsync(DateTime advanceDate, int employeeId, int transType)
        {
            return await _oBMSDbContext.SalaryAdvances
                .Where(sa => sa.AdvanceDate == advanceDate &&
                             sa.EmployeeID == employeeId &&
                             sa.TransType == transType &&
                             !sa.IsDeleted)
                .ToListAsync();
        }
        public async Task<bool> DeleteSalaryAdvanceAsync(int salaryAdvanceID, string currentUser)
        {
            try
            {
                var salaryAdvance = await _oBMSDbContext.SalaryAdvances.FirstOrDefaultAsync(x => x.ID == salaryAdvanceID && !x.IsDeleted);
                if (salaryAdvance == null)
                    return false;

                salaryAdvance.IsDeleted = true;
                salaryAdvance.LastUpdatedBy = currentUser;
                salaryAdvance.LastUpdate = DateTime.Now;

                await _oBMSDbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                throw;
            }
        }       

        public async Task<SalaryAdvance?> GetEmployeeLoanIdAsync(int id, int transType)
        {
            return await _oBMSDbContext.SalaryAdvances
                             .Where(sa => sa.ID == id && sa.TransType == transType && sa.IsDeleted == false)
                             .FirstOrDefaultAsync();
        }
        #region Attendance

        public ActionResult<IEnumerable<ClientMaster>> GetClients(DateTime period, string branchCode)
        {
            IQueryable<ClientMaster> query = _oBMSDbContext.ClientMasters;

            if (!string.IsNullOrEmpty(branchCode))
            {
                query = query.Where(c => c.Branch == branchCode);
            }

            query = query.Where(c => c.SuperClientCode != null);
            query = query.Where(c => c.Status == "Active");

            //if (period != null)
            //{
            //    query = query.Where(c => _oBMSDbContext.Agreements.Any(a => a.Client == c.Code && a.AgreementDate <= period) &&
            //                              !_oBMSDbContext.TerminatedAgreements.Any(ta => ta.Client == c.Code && ta.TerminationDate <= period));
            //}
            query = query.OrderBy(c => c.Shortname);
            List<ClientMaster> clients = query.ToList();
            return clients;
        }
        public Attendance AttendanceByEmployeeID(DateTime Period, int employeeID)
        {
            try
            {
                // ✅ BUG FIX: Use Date-only comparison instead of exact DateTime match.
                // The frontend sends "2025-07-31T00:00:00" but the DB may have stored
                // a different time component. Comparing only the Date part prevents
                // a mismatch that causes attendanceData to return null, which triggers
                // addFormFields() (blank form) even though data exists in the DB.
                var attendance = _oBMSDbContext.Attendances
                    .Where(e => e.EmployeeID == employeeID
                             && e.Period.Year == Period.Year
                             && e.Period.Month == Period.Month
                             && e.Period.Day == Period.Day)
                    .FirstOrDefault();

                return attendance;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<AttendanceDetails>> AttendanceDetailsByID(int Id)
        {
            var result = _oBMSDbContext.AttendanceDetails
                .Where(e => e.AttendanceID == Id).ToList();

            return new List<AttendanceDetails>(result);


        }
        public async Task<List<AttendanceDetails>> GetAttendanceDetailsList(int AttendanceID)
        {
            try
            {
                var attendancedetailsfactoryList = (
                    from ad in _oBMSDbContext.AttendanceDetails
                    join a in _oBMSDbContext.Attendances on ad.AttendanceID equals a.ID
                    join c1 in _oBMSDbContext.ClientMasters on new { a.Branch, Code = ad.Client } equals new { c1.Branch, c1.Code } into clientJoin
                    from c1 in clientJoin.DefaultIfEmpty()
                    join c2 in _oBMSDbContext.ClientMasters on new { a.Branch, Code = ad.OTClient } equals new { c2.Branch, c2.Code } into otClientJoin
                    from c2 in otClientJoin.DefaultIfEmpty()
                    where ad.AttendanceID == AttendanceID
                    select new AttendanceDetails
                    {
                        ID = ad.ID,
                        AttendanceID = ad.AttendanceID,
                        AttendanceDate = ad.AttendanceDate,
                        Client = ad.Client ?? "",
                        TimeStart = ad.TimeStart ?? DateTime.MinValue,
                        TimeEnd = ad.TimeEnd ?? DateTime.MinValue,
                        OTClient = ad.OTClient ?? "",
                        OTTimeStart = ad.OTTimeStart ?? DateTime.MinValue,
                        OTTimeEnd = ad.OTTimeEnd ?? DateTime.MinValue,
                        Type = ad.Type,
                        LastUpdate = ad.LastUpdate
                    }).ToList();

                return attendancedetailsfactoryList;

            }
            catch
            {
                throw;
            }
        }

        public async Task<List<SalaryAttendenceDto>> GetEmployeeDetails(string branchCode, string employeeNo)
        {
            // Note: we intentionally do NOT filter by EMP_BRANCH_CODE here.
            // After the employee-transfer feature, an employee viewed on a historical
            // attendance month may have a CURRENT branch that differs from the selected
            // (historical) branch. Filtering by branchCode would return no rows and cause
            // a null-reference in the frontend. The employee is uniquely identified by
            // EMP_CODE, which is sufficient.
            var result = await (from employee in _oBMSDbContext.Employees
                                join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                                join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
                                join salarystructure in _oBMSDbContext.SalaryStructures on employeement.SALARYLAB equals salarystructure.SalaryId
                                join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID into advancesGroup
                                from salaryAdvance in advancesGroup.DefaultIfEmpty()
                                where employee.EMP_CODE == employeeNo
                                select new SalaryAttendenceDto
                                {
                                    ID = (salaryAdvance != null) ? salaryAdvance.ID : 0,
                                    EMP_ID = employee.EMP_ID,
                                    EMP_NAME = string.IsNullOrEmpty(employee.EMP_NAME)
                                                ? employee.EMP_NAME
                                                : employee.EMP_NAME.Replace("''", "'"),
                                    EMP_CODE = employee.EMP_CODE,
                                    EMP_IC_NEW = employee.EMP_IC_NEW,
                                    EMP_IC_OLD = employee.EMP_IC_OLD,
                                    EMP_PASSPORT_NO = employee.EMP_PASSPORT_NO,
                                    EMP_DATE_OF_BIRTH = employee.EMP_DATE_OF_BIRTH,
                                    SalaryStructure = employee.NewSalaryStructure.ToString(),
                                    EMPFL_BANK = salaryDetails.EMPFL_BANK,
                                    EMPFL_BK_ACCNO = salaryDetails.EMPFL_BK_ACCNO,
                                    EPFDETECT = salaryDetails.EPFDETECT,
                                    SOCSODETECT = salaryDetails.SOCSODETECT,
                                    INCOMETAXDETECT = salaryDetails.INCOMETAXDETECT,
                                    EMPFL_EPFNO = salaryDetails.EMPFL_EPFNO,
                                    PAYMODE = salaryDetails.PAYMODE,
                                    Amount = (salaryAdvance != null) ? salaryAdvance.Amount : 0,
                                    Particulars = (salaryAdvance != null) ? salaryAdvance.Particulars : "",
                                    EMPPAY_DATE_JOINED = employeement.EMPPAY_DATE_JOINED,
                                    EMPPAY_DATE_RESIGNED = employeement.EMPPAY_DATE_RESIGNED,
                                    ATTENDANCEALLOWANCE = employeement.ATTENDANCEALLOWANCE,
                                    SpecialAllowance = employeement.SpecialAllowance,
                                    EMPPAY_BASIC_RATE = employeement.EMPPAY_BASIC_RATE,
                                    Name = salarystructure.Name,
                                }).ToListAsync();

            return new List<SalaryAttendenceDto>(result);
        }
        public int CalculateAge(DateTime birthDate)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            int num;

            if (DateTime.Now.Month >= birthDate.Month)
            {
                DateTime now = DateTime.Now;

                if (now.Month == birthDate.Month)
                {
                    now = DateTime.Now;
                    num = now.Day >= birthDate.Day ? 1 : 0;
                }
                else
                {
                    num = 1;
                }
            }
            else
            {
                num = 0;
            }

            if (num == 0)
            {
                --age;
            }

            return age;
        }
        public int GetAnnualLeave(int employeeID, DateTime period)
        {
            try
            {
                // Count annual leave taken (Type=8) within the same year, before the period date
                // Matches SQL: WHERE AttendanceDate < period AND Year(AttendanceDate) = Year(period) AND Type = 8
                var leaveTaken = (from attendanceDetail in _oBMSDbContext.AttendanceDetails
                                  join attendance in _oBMSDbContext.Attendances on attendanceDetail.AttendanceID equals attendance.ID
                                  join employee in _oBMSDbContext.Employees on attendance.EmployeeID equals employee.EMP_ID
                                  join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
                                  where attendanceDetail.AttendanceDate < period
                                     && attendanceDetail.AttendanceDate.Year == period.Year
                                     && attendanceDetail.Type == 8
                                     && employee.EMP_ID == employeeID
                                  select attendanceDetail.ID).Count();

                // Fetch leave system config and employment details for the employee in memory
                // LeaveSystem is a single config table (cross join), not per-employee — cannot use LS_ID = EMP_ID
                var leaveData = (from employee in _oBMSDbContext.Employees
                                 join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
                                 where employee.EMP_ID == employeeID
                                 select employmentDetail).FirstOrDefault();

                var leaveSystemConfig = _oBMSDbContext.LeaveSystems.FirstOrDefault();

                int leaveAvailable = 0;

                if (leaveData != null && leaveSystemConfig != null)
                {
                    DateTime dateJoined = leaveData.EMPPAY_DATE_JOINED;
                    int monthsDiff = DateDiffInMonths(dateJoined, period);

                    // Calculate years diff the same way as SQL DATEDIFF(YEAR, ...)
                    int yearsDiff = period.Year - dateJoined.Year;

                    if (yearsDiff == 0)
                    {
                        // CAST(AL0To1 * DateDiff(Month, dateJoined, period) / 12 as int)
                        leaveAvailable = (int)((double)leaveSystemConfig.al0to1 * monthsDiff / 12);
                    }
                    else if (yearsDiff == 1)
                    {
                        if (monthsDiff <= 12)
                            leaveAvailable = (int)((double)leaveSystemConfig.al0to1 * monthsDiff / 12);
                        else
                            leaveAvailable = (int)leaveSystemConfig.AL1to2;
                    }
                    else if (yearsDiff == 2)
                    {
                        if (monthsDiff >= 13 && monthsDiff <= 24)
                            leaveAvailable = (int)leaveSystemConfig.AL1to2;
                        else
                            leaveAvailable = (int)((double)leaveSystemConfig.AL2to5 * monthsDiff / 12 - 24 + (double)leaveSystemConfig.AL1to2);
                    }
                    else if (yearsDiff == 3)
                    {
                        if (monthsDiff >= 25 && monthsDiff <= 28)
                            leaveAvailable = (int)((double)leaveSystemConfig.AL2to5 * monthsDiff / 12 - 24 + (double)leaveSystemConfig.AL1to2);
                        else
                            leaveAvailable = (int)leaveSystemConfig.AL2to5;
                    }
                    else if (yearsDiff >= 4 && yearsDiff <= 5)
                    {
                        if (monthsDiff >= 25 && monthsDiff <= 60)
                            leaveAvailable = (int)leaveSystemConfig.AL2to5;
                        else
                            leaveAvailable = (int)(Math.Round(((monthsDiff - 61) * 1.0 / 12) * 4, 0) + (double)leaveSystemConfig.AL2to5);
                    }
                    else if (yearsDiff == 6)
                    {
                        if (monthsDiff >= 61 && monthsDiff <= 63)
                            leaveAvailable = (int)((double)leaveSystemConfig.AL6 * monthsDiff / 12 - 68);
                        else
                            leaveAvailable = (int)leaveSystemConfig.AL6;
                    }
                    else
                    {
                        // 7+ years
                        leaveAvailable = (int)leaveSystemConfig.AL6;
                    }
                }

                return leaveAvailable - leaveTaken;
            }
            catch
            {
                throw;
            }
        }

        public int DateDiffInMonths(DateTime startDate, DateTime endDate)
        {
            return (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;
        }

        //public async Task<ActionResult> SaveAndUpdateAttendance(Attendance attendanceModel, List<AttendanceDetails> attendanceDetails)
        //{
        //    try
        //    {
        //        if (attendanceModel != null)
        //        {
        //            var existingattendance = _oBMSDbContext.Attendances.Where(a => a.ID == attendanceModel.ID).SingleOrDefault();
        //            if (existingattendance != null)
        //            {
        //                existingattendance.EmployeeID = attendanceModel.EmployeeID;
        //                existingattendance.Period = attendanceModel.Period;
        //                existingattendance.Branch = attendanceModel.Branch;
        //                existingattendance.Shift2Type = attendanceModel.Shift2Type;
        //                existingattendance.Shift2Rate = attendanceModel.Shift2Rate;
        //                existingattendance.Bonus = attendanceModel.Bonus;
        //                existingattendance.KPIDeduction = attendanceModel.KPIDeduction;
        //                existingattendance.AllowanceDeduction = attendanceModel.AllowanceDeduction;
        //                existingattendance.SpecialAllowanceDeduction = attendanceModel.SpecialAllowanceDeduction;
        //                existingattendance.LastUpdate = attendanceModel.LastUpdate;
        //                existingattendance.LastUpdatedBy = attendanceModel.LastUpdatedBy;

        //                _oBMSDbContext.Update(existingattendance);
        //                await _oBMSDbContext.SaveChangesAsync();

        //                List<AttendanceDetails> updatedRecords = new List<AttendanceDetails>();
        //                foreach (var attendanceDetail in attendanceDetails)
        //                {
        //                    var existingRecord = await _oBMSDbContext.AttendanceDetails
        //                        .FirstOrDefaultAsync(s => s.ID == attendanceDetail.ID);

        //                    if (existingRecord != null)
        //                    {
        //                        // Update properties
        //                        existingRecord.AttendanceID = attendanceDetail.AttendanceID;
        //                        existingRecord.AttendanceDate = attendanceDetail.AttendanceDate;
        //                        existingRecord.Client = attendanceDetail.Client;
        //                        existingRecord.TimeStart = attendanceDetail.TimeStart;
        //                        existingRecord.TimeEnd = attendanceDetail.TimeEnd;
        //                        existingRecord.OTClient = attendanceDetail.OTClient;
        //                        existingRecord.OTTimeStart = attendanceDetail.OTTimeStart;
        //                        existingRecord.OTTimeEnd = attendanceDetail.OTTimeEnd;
        //                        existingRecord.Type = attendanceDetail.Type;
        //                        existingRecord.LastUpdate = DateTime.Now;
        //                        existingRecord.LastUpdatedBy = attendanceDetail.LastUpdatedBy;

        //                        _oBMSDbContext.Update(existingRecord);
        //                        updatedRecords.Add(existingRecord);
        //                    }
        //                    else
        //                    {
        //                        attendanceDetail.AttendanceID = existingattendance.ID;
        //                        _oBMSDbContext.Add(attendanceDetail);
        //                        updatedRecords.Add(attendanceDetail);
        //                    }
        //                }
        //                await _oBMSDbContext.SaveChangesAsync();
        //            }
        //            else
        //            {
        //                _oBMSDbContext.Add(attendanceModel);
        //                await _oBMSDbContext.SaveChangesAsync();

        //                List<AttendanceDetails> updatedRecords = new List<AttendanceDetails>();
        //                foreach (var attendanceDetail in attendanceDetails)
        //                {
        //                    var existingRecord = await _oBMSDbContext.AttendanceDetails
        //                        .FirstOrDefaultAsync(s => s.ID == attendanceDetail.ID);

        //                    if (existingRecord != null)
        //                    {
        //                        // Update properties
        //                        existingRecord.AttendanceID = attendanceDetail.AttendanceID;
        //                        existingRecord.AttendanceDate = attendanceDetail.AttendanceDate;
        //                        existingRecord.Client = attendanceDetail.Client;
        //                        existingRecord.TimeStart = attendanceDetail.TimeStart;
        //                        existingRecord.TimeEnd = attendanceDetail.TimeEnd;
        //                        existingRecord.OTClient = attendanceDetail.OTClient;
        //                        existingRecord.OTTimeStart = attendanceDetail.OTTimeStart;
        //                        existingRecord.OTTimeEnd = attendanceDetail.OTTimeEnd;
        //                        existingRecord.Type = attendanceDetail.Type;
        //                        existingRecord.LastUpdate = DateTime.Now;
        //                        existingRecord.LastUpdatedBy = attendanceDetail.LastUpdatedBy;

        //                        _oBMSDbContext.Update(existingRecord);
        //                        updatedRecords.Add(existingRecord);
        //                    }
        //                    else
        //                    {
        //                        attendanceDetail.AttendanceID = attendanceModel.ID;
        //                        _oBMSDbContext.Add(attendanceDetail);
        //                        updatedRecords.Add(attendanceDetail);
        //                    }
        //                }
        //                await _oBMSDbContext.SaveChangesAsync();
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }

        //    return null;
        //}

        public async Task<ActionResult> SaveAndUpdateAttendance(Attendance attendanceModel, List<AttendanceDetails> attendanceDetails)
        {
            if (attendanceModel == null) return null;

            // EnableRetryOnFailure is configured, so user-initiated transactions must be
            // wrapped inside CreateExecutionStrategy().ExecuteAsync() to be retriable.
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var auditTimestamp = DateTime.Now;
                    var baseUser      = attendanceModel.LastUpdatedBy;
                    var auditUserInsert = $"INSERT {baseUser}";
                    var auditUserUpdate = $"UPDATE {baseUser}";

                    // -----------------------------------------------------------------
                    // Locate existing Attendance record
                    // Priority: find by ID first (edit path).
                    // Fallback: EmployeeID + Period date match (prevents duplicates when
                    // the frontend sends ID=0 due to a Period DateTime mismatch on load).
                    // -----------------------------------------------------------------
                    Attendance? existingAttendance = null;

                    if (attendanceModel.ID > 0)
                    {
                        existingAttendance = await _oBMSDbContext.Attendances
                            .Where(a => a.ID == attendanceModel.ID)
                            .SingleOrDefaultAsync();
                    }

                    if (existingAttendance == null)
                    {
                        existingAttendance = await _oBMSDbContext.Attendances
                            .Where(a => a.EmployeeID == attendanceModel.EmployeeID
                                     && a.Period.Year  == attendanceModel.Period.Year
                                     && a.Period.Month == attendanceModel.Period.Month
                                     && a.Period.Day   == attendanceModel.Period.Day)
                            .FirstOrDefaultAsync();
                    }

                    // -----------------------------------------------------------------
                    // INSERT path — new Attendance record
                    // -----------------------------------------------------------------
                    if (existingAttendance == null)
                    {
                        // 1. Insert Attendance
                        attendanceModel.LastUpdatedBy = auditUserInsert;
                        _oBMSDbContext.Add(attendanceModel);
                        await _oBMSDbContext.SaveChangesAsync();

                        int newAttendanceID = attendanceModel.ID; // populated by EF after save

                        // 2. Insert Attendance_AuditTrail
                        _oBMSDbContext.Add(new AttendanceAuditTrail
                        {
                            AttendanceID              = newAttendanceID,
                            Period                    = attendanceModel.Period,
                            Branch                    = attendanceModel.Branch,
                            EmployeeID                = attendanceModel.EmployeeID,
                            Shift2Type                = attendanceModel.Shift2Type,
                            Shift2Rate                = attendanceModel.Shift2Rate,
                            AllowanceDeduction        = attendanceModel.AllowanceDeduction,
                            SpecialAllowanceDeduction = attendanceModel.SpecialAllowanceDeduction,
                            Bonus                     = attendanceModel.Bonus,
                            KPIDeduction              = attendanceModel.KPIDeduction,
                            LastUpdate                = auditTimestamp,
                            LastUpdatedBy             = auditUserInsert
                        });
                        await _oBMSDbContext.SaveChangesAsync();

                        // 3. Insert AttendanceDetails + 4. Insert AttendanceDetails_AuditTrail
                        foreach (var detail in attendanceDetails)
                        {
                            var newDetail = new AttendanceDetails
                            {
                                AttendanceID   = newAttendanceID,
                                AttendanceDate = detail.AttendanceDate,
                                Client         = detail.Client,
                                TimeStart      = detail.TimeStart,
                                TimeEnd        = detail.TimeEnd,
                                OTClient       = detail.OTClient,
                                OTTimeStart    = detail.OTTimeStart,
                                OTTimeEnd      = detail.OTTimeEnd,
                                Type           = detail.Type,
                                LastUpdate     = auditTimestamp,
                                LastUpdatedBy  = auditUserInsert
                            };

                            _oBMSDbContext.Add(newDetail);
                            await _oBMSDbContext.SaveChangesAsync(); // flush to get ID

                            _oBMSDbContext.Add(new AttendanceDetailsAuditTrail
                            {
                                AttendanceDetailsID = newDetail.ID,
                                AttendanceID        = newAttendanceID,
                                AttendanceDate      = newDetail.AttendanceDate,
                                Client              = newDetail.Client,
                                TimeStart           = newDetail.TimeStart,
                                TimeEnd             = newDetail.TimeEnd,
                                OTClient            = newDetail.OTClient,
                                OTTimeStart         = newDetail.OTTimeStart,
                                OTTimeEnd           = newDetail.OTTimeEnd,
                                Type                = newDetail.Type,
                                LastUpdate          = auditTimestamp,
                                LastUpdatedBy       = auditUserInsert
                            });
                        }

                        await _oBMSDbContext.SaveChangesAsync();
                    }
                    // -----------------------------------------------------------------
                    // UPDATE path — existing Attendance record
                    // -----------------------------------------------------------------
                    else
                    {
                        // 1. Update Attendance
                        existingAttendance.EmployeeID                = attendanceModel.EmployeeID;
                        existingAttendance.Period                    = attendanceModel.Period;
                        existingAttendance.Branch                    = attendanceModel.Branch;
                        existingAttendance.Shift2Type                = attendanceModel.Shift2Type;
                        existingAttendance.Shift2Rate                = attendanceModel.Shift2Rate;
                        existingAttendance.Bonus                     = attendanceModel.Bonus;
                        existingAttendance.KPIDeduction              = attendanceModel.KPIDeduction;
                        existingAttendance.AllowanceDeduction        = attendanceModel.AllowanceDeduction;
                        existingAttendance.SpecialAllowanceDeduction = attendanceModel.SpecialAllowanceDeduction;
                        existingAttendance.LastUpdate                = auditTimestamp;
                        existingAttendance.LastUpdatedBy             = auditUserUpdate;

                        _oBMSDbContext.Update(existingAttendance);
                        await _oBMSDbContext.SaveChangesAsync();

                        // 2. Insert Attendance_AuditTrail (latest state after update)
                        _oBMSDbContext.Add(new AttendanceAuditTrail
                        {
                            AttendanceID              = existingAttendance.ID,
                            Period                    = existingAttendance.Period,
                            Branch                    = existingAttendance.Branch,
                            EmployeeID                = existingAttendance.EmployeeID,
                            Shift2Type                = existingAttendance.Shift2Type,
                            Shift2Rate                = existingAttendance.Shift2Rate,
                            AllowanceDeduction        = existingAttendance.AllowanceDeduction,
                            SpecialAllowanceDeduction = existingAttendance.SpecialAllowanceDeduction,
                            Bonus                     = existingAttendance.Bonus,
                            KPIDeduction              = existingAttendance.KPIDeduction,
                            LastUpdate                = auditTimestamp,
                            LastUpdatedBy             = auditUserUpdate
                        });
                        await _oBMSDbContext.SaveChangesAsync();

                        // 3. Update AttendanceDetails + 4. Insert AttendanceDetails_AuditTrail
                        var existingDetailRecords = await _oBMSDbContext.AttendanceDetails
                            .Where(ad => ad.AttendanceID == existingAttendance.ID)
                            .ToListAsync();

                        foreach (var detail in attendanceDetails)
                        {
                            var existingDetail = existingDetailRecords
                                .FirstOrDefault(ad => ad.AttendanceDate.Date == detail.AttendanceDate.Date);

                            if (existingDetail != null)
                            {
                                // Update existing detail row
                                existingDetail.Client        = detail.Client;
                                existingDetail.TimeStart     = detail.TimeStart;
                                existingDetail.TimeEnd       = detail.TimeEnd;
                                existingDetail.OTClient      = detail.OTClient;
                                existingDetail.OTTimeStart   = detail.OTTimeStart;
                                existingDetail.OTTimeEnd     = detail.OTTimeEnd;
                                existingDetail.Type          = detail.Type;
                                existingDetail.LastUpdate    = auditTimestamp;
                                existingDetail.LastUpdatedBy = auditUserUpdate;

                                _oBMSDbContext.Update(existingDetail);
                                await _oBMSDbContext.SaveChangesAsync();

                                // Audit trail for updated detail (latest state)
                                _oBMSDbContext.Add(new AttendanceDetailsAuditTrail
                                {
                                    AttendanceDetailsID = existingDetail.ID,
                                    AttendanceID        = existingDetail.AttendanceID,
                                    AttendanceDate      = existingDetail.AttendanceDate,
                                    Client              = existingDetail.Client,
                                    TimeStart           = existingDetail.TimeStart,
                                    TimeEnd             = existingDetail.TimeEnd,
                                    OTClient            = existingDetail.OTClient,
                                    OTTimeStart         = existingDetail.OTTimeStart,
                                    OTTimeEnd           = existingDetail.OTTimeEnd,
                                    Type                = existingDetail.Type,
                                    LastUpdate          = auditTimestamp,
                                    LastUpdatedBy       = auditUserUpdate
                                });
                            }
                            else
                            {
                                // New detail row added during an update — treat as INSERT
                                var newDetail = new AttendanceDetails
                                {
                                    AttendanceID   = existingAttendance.ID,
                                    AttendanceDate = detail.AttendanceDate,
                                    Client         = detail.Client,
                                    TimeStart      = detail.TimeStart,
                                    TimeEnd        = detail.TimeEnd,
                                    OTClient       = detail.OTClient,
                                    OTTimeStart    = detail.OTTimeStart,
                                    OTTimeEnd      = detail.OTTimeEnd,
                                    Type           = detail.Type,
                                    LastUpdate     = auditTimestamp,
                                    LastUpdatedBy  = auditUserInsert
                                };

                                _oBMSDbContext.Add(newDetail);
                                await _oBMSDbContext.SaveChangesAsync(); // flush to get ID

                                _oBMSDbContext.Add(new AttendanceDetailsAuditTrail
                                {
                                    AttendanceDetailsID = newDetail.ID,
                                    AttendanceID        = newDetail.AttendanceID,
                                    AttendanceDate      = newDetail.AttendanceDate,
                                    Client              = newDetail.Client,
                                    TimeStart           = newDetail.TimeStart,
                                    TimeEnd             = newDetail.TimeEnd,
                                    OTClient            = newDetail.OTClient,
                                    OTTimeStart         = newDetail.OTTimeStart,
                                    OTTimeEnd           = newDetail.OTTimeEnd,
                                    Type                = newDetail.Type,
                                    LastUpdate          = auditTimestamp,
                                    LastUpdatedBy       = auditUserInsert
                                });
                            }
                        }

                        await _oBMSDbContext.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return null;
        }



        public async Task<bool> DeleteAttendanceAsync(int dID, string username)
        {
            var strategy = _oBMSDbContext.Database.CreateExecutionStrategy();
            bool deleted = false;

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _oBMSDbContext.Database.BeginTransactionAsync();
                try
                {
                    var auditTimestamp = DateTime.Now;
                    var auditUser = $"DELETE {username}";

                    // Load attendance header before deleting (needed for audit trail)
                    var attendance = await _oBMSDbContext.Attendances
                        .FirstOrDefaultAsync(a => a.ID == dID);

                    if (attendance == null)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    // Load detail rows before deleting
                    var attendanceDetails = await _oBMSDbContext.AttendanceDetails
                        .Where(ad => ad.AttendanceID == dID)
                        .ToListAsync();

                    // 1. Insert Attendance_AuditTrail (DELETE record)
                    _oBMSDbContext.Add(new AttendanceAuditTrail
                    {
                        AttendanceID              = attendance.ID,
                        Period                    = attendance.Period,
                        Branch                    = attendance.Branch,
                        EmployeeID                = attendance.EmployeeID,
                        Shift2Type                = attendance.Shift2Type,
                        Shift2Rate                = attendance.Shift2Rate,
                        AllowanceDeduction        = attendance.AllowanceDeduction,
                        SpecialAllowanceDeduction = attendance.SpecialAllowanceDeduction,
                        Bonus                     = attendance.Bonus,
                        KPIDeduction              = attendance.KPIDeduction,
                        LastUpdate                = auditTimestamp,
                        LastUpdatedBy             = auditUser
                    });

                    // 2. Insert AttendanceDetails_AuditTrail for each detail row (DELETE record)
                    foreach (var detail in attendanceDetails)
                    {
                        _oBMSDbContext.Add(new AttendanceDetailsAuditTrail
                        {
                            AttendanceDetailsID = detail.ID,
                            AttendanceID        = detail.AttendanceID,
                            AttendanceDate      = detail.AttendanceDate,
                            Client              = detail.Client,
                            TimeStart           = detail.TimeStart,
                            TimeEnd             = detail.TimeEnd,
                            OTClient            = detail.OTClient,
                            OTTimeStart         = detail.OTTimeStart,
                            OTTimeEnd           = detail.OTTimeEnd,
                            Type                = detail.Type,
                            LastUpdate          = auditTimestamp,
                            LastUpdatedBy       = auditUser
                        });
                    }

                    await _oBMSDbContext.SaveChangesAsync();

                    // 3. Delete AttendanceDetails
                    if (attendanceDetails.Any())
                    {
                        _oBMSDbContext.AttendanceDetails.RemoveRange(attendanceDetails);
                    }

                    // 4. Delete Attendance
                    _oBMSDbContext.Attendances.Remove(attendance);

                    // 5. Delete associated AdvanceRepayment entries via PaySlip
                    var paySlipId = await _oBMSDbContext.PaySlips
                        .Where(p => p.EmployeeID == attendance.EmployeeID && p.Period == attendance.Period)
                        .Select(p => p.ID)
                        .FirstOrDefaultAsync();

                    if (paySlipId != 0)
                    {
                        _oBMSDbContext.AdvanceRepayments.RemoveRange(
                            _oBMSDbContext.AdvanceRepayments.Where(ar => ar.PaySlipID == paySlipId)
                        );
                    }

                    // 6. Delete PaySlip
                    _oBMSDbContext.PaySlips.RemoveRange(
                        _oBMSDbContext.PaySlips.Where(p => p.EmployeeID == attendance.EmployeeID && p.Period == attendance.Period)
                    );

                    await _oBMSDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    deleted = true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return deleted;
        }

        public List<EmployeeDto> GetList(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, DateTime attendancePeriod, string status)
        {
            var query = (from emp in _oBMSDbContext.Employees
                         join empDetails in _oBMSDbContext.EmploymentDetails on emp.EMP_CODE equals empDetails.EMPPAY_CODE
                         join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on emp.EMP_CODE equals salaryDetails.EMPFL_CODE
                         join attendance in _oBMSDbContext.Attendances on emp.EMP_ID equals attendance.EmployeeID
                         join attendanceDetails in _oBMSDbContext.AttendanceDetails on attendance.ID equals attendanceDetails.AttendanceID
                         where emp.HasTransfered == false
                         select new EmployeeDto
                         {
                             EMP_ID = emp.EMP_ID,
                             EMP_ROLE = emp.EMP_ROLE,
                             EMP_CODE = emp.EMP_CODE,
                             EMP_NAME = string.IsNullOrEmpty(emp.EMP_NAME)
                                        ? emp.EMP_NAME
                                        : emp.EMP_NAME.Replace("''", "'"),
                             EMP_CLIENT = emp.EMP_CLIENT,
                             EMP_ADDRESS1 = emp.EMP_ADDRESS1,
                             EMP_ADDRESS2 = emp.EMP_ADDRESS2,
                             EMP_POST_CODE = emp.EMP_POST_CODE,
                             EMP_TOWN = emp.EMP_TOWN,
                             EMP_STATE = emp.EMP_STATE,
                             EMP_CITIZEN = emp.EMP_CITIZEN,
                             EMP_CHECKLIST = emp.EMP_CHECKLIST,
                             EMP_NATIONAL = emp.EMP_NATIONAL,
                             EMP_PHONE = emp.EMP_PHONE,
                             EMP_MOBILEPHONE = emp.EMP_MOBILEPHONE,
                             EMP_HGH_EDU = emp.EMP_HGH_EDU,
                             EMP_DATE_OF_BIRTH = emp.EMP_DATE_OF_BIRTH,
                             EMP_IC_OLD = emp.EMP_IC_OLD,
                             EMP_IC_NEW = emp.EMP_IC_NEW,
                             EMP_IC_COLOR = emp.EMP_IC_COLOR,
                             EMP_PASSPORT_NO = emp.EMP_PASSPORT_NO,
                             EMP_SEX = emp.EMP_SEX,
                             EMP_RACE = emp.EMP_RACE,
                             EMP_MARTIAL_STATUS = emp.EMP_MARTIAL_STATUS,
                             EMP_SPOUSE_NAME = emp.EMP_SPOUSE_NAME,
                             EMP_SP_IC = emp.EMP_SP_IC,
                             EMP_NO_CHILD = emp.EMP_NO_CHILD,
                             EMP_SP_WORK = emp.EMP_SP_WORK,
                             EMP_PER_NAME_CONTACT = emp.EMP_PER_NAME_CONTACT,
                             EMP_CONTACT_ADDRESS1 = emp.EMP_CONTACT_ADDRESS1,
                             EMP_CONTACT_ADDRESS2 = emp.EMP_CONTACT_ADDRESS2,
                             EMP_CONTACT_POST_CODE = emp.EMP_CONTACT_POST_CODE,
                             EMP_CONTACT_TOWN = emp.EMP_CONTACT_TOWN,
                             EMP_CONTACT_STATE = emp.EMP_CONTACT_STATE,
                             EMP_CONTACT_TELEPHONE = emp.EMP_CONTACT_TELEPHONE,
                             EMP_BRANCH_CODE = emp.EMP_BRANCH_CODE,
                             OldBranch = emp.OldBranch,
                             TransferDate = emp.TransferDate,
                             LASTUPDATE = emp.LASTUPDATE,
                             NewSalaryStructure = emp.NewSalaryStructure,
                             SalaryStructure1000_3h = emp.SalaryStructure1000_3h,
                             EMPPAY_DATE_RESIGNED = empDetails.EMPPAY_DATE_RESIGNED,
                             EMPPAY_DATE_JOINED = empDetails.EMPPAY_DATE_JOINED,
                             TMPGUARD = salaryDetails.TMPGUARD,
                             Period = attendance.Period

                         }).Distinct();

            // Apply filters based on parameters
            if (!string.IsNullOrEmpty(branch))
                query = query.Where(emp => emp.EMP_BRANCH_CODE == branch);

            if (!string.IsNullOrEmpty(employeeType))
            {
                if (employeeType.Contains("TEMPORARY"))
                    query = query.Where(emp => emp.TMPGUARD == false);
                if (employeeType == "TEMPORARYSTAFF")
                    query = query.Where(emp => emp.EMP_ROLE.Contains("STAFF"));
                else if (employeeType == "TEMPORARYGUARD")
                    query = query.Where(emp => emp.EMP_ROLE.Contains("GUARD"));
                else if (employeeType == "Others")
                    query = query.Where(emp => !emp.EMP_ROLE.Contains("Guard") && !emp.EMP_ROLE.Contains("Staff") && !emp.EMP_ROLE.Contains("FGuard"));
                else
                    query = query.Where(emp => emp.EMP_ROLE.Contains(employeeType));
            }

            // ✅ BUG FIX: Previously the resigned-date filter was commented out and the
            // joinDate filter was incorrectly nested inside the resignedDate block (no braces).
            // This caused resigned employees to always appear in the attendance list.
            // Now: filter out employees whose resign date is before the attendance period.
            if (resignedDate.Year != 1)
            {
                query = query.Where(emp =>
                    emp.EMPPAY_DATE_RESIGNED == null ||
                    emp.EMPPAY_DATE_RESIGNED.Value.Year == 1 ||
                    emp.EMPPAY_DATE_RESIGNED >= resignedDate);
            }
            if (joinDate.Year != 1)
                query = query.Where(emp => emp.EMPPAY_DATE_JOINED <= joinDate);
            if (attendancePeriod.Year != 1)
                query = query.Where(emp => emp.Period.Year == attendancePeriod.Year && emp.Period.Month == attendancePeriod.Month);

            query = query.OrderBy(emp => emp.EMP_NAME);

            return query.ToList();
        }
        //public List<EmployeeDto> getListByEmployee(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status)
        //{
        //    var defaultResignedDate = new DateTime(2100, 1, 1);

        //    var query = from e in _oBMSDbContext.Employees
        //                     join ed in _oBMSDbContext.EmploymentDetails on e.EMP_CODE equals ed.EMPPAY_CODE
        //                     join esd in _oBMSDbContext.EmployeeSalaryDetails on e.EMP_CODE equals esd.EMPFL_CODE
        //                     where
        //                           (e.HasTransfered == false ||
        //                            (e.HasTransfered == true && e.TransferDate >= resignedDate))
        //                     select new EmployeeDto
        //                     {
        //                         EMP_ID = e.EMP_ID,
        //                         EMP_ROLE = e.EMP_ROLE,
        //                         EMP_CODE = e.EMP_CODE,
        //                         EMP_NAME = e.EMP_NAME,
        //                         EMP_CLIENT = e.EMP_CLIENT,
        //                         EMP_ADDRESS1 = e.EMP_ADDRESS1,
        //                         EMP_ADDRESS2 = e.EMP_ADDRESS2,
        //                         EMP_POST_CODE = e.EMP_POST_CODE,
        //                         EMP_TOWN = e.EMP_TOWN,
        //                         EMP_STATE = e.EMP_STATE,
        //                         EMP_NATIONAL = e.EMP_NATIONAL,
        //                         EMP_PHONE = e.EMP_PHONE,
        //                         EMP_MOBILEPHONE = e.EMP_MOBILEPHONE,
        //                         EMP_CITIZEN = e.EMP_CITIZEN,
        //                         EMP_CHECKLIST = e.EMP_CHECKLIST,
        //                         EMP_HGH_EDU = e.EMP_HGH_EDU,
        //                         EM_WORK_EXP = e.EM_WORK_EXP,
        //                         EMP_DATE_OF_BIRTH = e.EMP_DATE_OF_BIRTH,
        //                         EMP_IC_OLD = e.EMP_IC_OLD,
        //                         EMP_IC_NEW = e.EMP_IC_NEW,
        //                         EMP_IC_COLOR = e.EMP_IC_COLOR,
        //                         EMP_PASSPORT_NO = e.EMP_PASSPORT_NO,
        //                         EMP_SEX = e.EMP_SEX,
        //                         EMP_RACE = e.EMP_RACE,
        //                         EMP_MARTIAL_STATUS = e.EMP_MARTIAL_STATUS,
        //                         EMP_SPOUSE_NAME = e.EMP_SPOUSE_NAME,
        //                         EMP_SP_IC = e.EMP_SP_IC,
        //                         EMP_NO_CHILD = e.EMP_NO_CHILD,
        //                         EMP_SP_WORK = e.EMP_SP_WORK,
        //                         EMP_PER_NAME_CONTACT = e.EMP_PER_NAME_CONTACT,
        //                         EMP_CONTACT_ADDRESS1 = e.EMP_CONTACT_ADDRESS1,
        //                         EMP_CONTACT_ADDRESS2 = e.EMP_CONTACT_ADDRESS2,
        //                         EMP_CONTACT_POST_CODE = e.EMP_CONTACT_POST_CODE,
        //                         EMP_CONTACT_TOWN = e.EMP_CONTACT_TOWN,
        //                         EMP_CONTACT_STATE = e.EMP_CONTACT_STATE,
        //                         EMP_CONTACT_TELEPHONE = e.EMP_CONTACT_TELEPHONE,
        //                         EMP_BRANCH_CODE = e.EMP_BRANCH_CODE,
        //                         OldBranch = e.OldBranch,
        //                         TransferDate = e.TransferDate,
        //                         LASTUPDATE = e.LASTUPDATE,
        //                         NewSalaryStructure = e.NewSalaryStructure,
        //                         SalaryStructure1000_3h = e.SalaryStructure1000_3h
        //                     };
        //    // Apply filters based on parameters
        //    //if (!string.IsNullOrEmpty(status))
        //    //{
        //    //    if (status == "Active")
        //    //    {
        //    //        if (resignedDate.Month == 12)
        //    //        {
        //    //            query = query.Where(ed =>
        //    //                // Case for Active status with ResignedDate in December
        //    //                ed.EMPPAY_DATE_RESIGNED == null ||
        //    //                (ed.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year - 1 &&
        //    //                 ed.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1)
        //    //            );
        //    //        }
        //    //        else
        //    //        {
        //    //            query = query.Where(ed =>
        //    //                // Case for Active status with ResignedDate not in December
        //    //                ed.EMPPAY_DATE_RESIGNED == null ||
        //    //                (ed.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year &&
        //    //                 ed.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1)
        //    //            );
        //    //        }
        //    //    }
        //    //    else if (status == "Inactive")
        //    //    {
        //    //        // Case for Inactive status where EMPPAY_DATE_RESIGNED is not null
        //    //        query = query.Where(ed => ed.EMPPAY_DATE_RESIGNED != null);
        //    //    }
        //    //}

        //    if (!string.IsNullOrEmpty(employeeType))
        //        query = query.Where(e => e.EMP_ROLE.Contains(employeeType));

        //    if (!string.IsNullOrEmpty(branch))
        //        query = query.Where(e => e.EMP_BRANCH_CODE == branch);

        //    if (resignedDate.Year != 1)
        //        query = query.Where(ed =>
        //                (ed.EMPPAY_DATE_RESIGNED != null && ed.EMPPAY_DATE_RESIGNED >= resignedDate) ||
        //                defaultResignedDate >= resignedDate);

        //    if (joinDate.Year != 1)
        //        query = query.Where(ed => ed.EMPPAY_DATE_JOINED <= joinDate);

        //    query = query.OrderBy(emp => emp.EMP_NAME);

        //    return query.ToList();
        //}

        public List<EmployeeDto> getListByEmployee(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status)
        {
            // resignedDate = first day of selected attendance month (StartPeriod)
            // joinDate     = last day  of selected attendance month (EndPeriod)
            // When a period is supplied we resolve each employee's branch from
            // EmploymentDetailsHistory so that transferred employees appear under
            // the branch they were actually assigned to during that month.

            var result = new List<EmployeeDto>();

            bool hasPeriod = joinDate.Year != 1 && resignedDate.Year != 1;

            // firstDay / lastDay of the selected month used for history look-up
            DateTime firstDayOfMonth = hasPeriod ? resignedDate.Date : DateTime.MinValue;
            DateTime lastDayOfMonth  = hasPeriod ? joinDate.Date    : DateTime.MaxValue;

            // ---------------------------------------------------------------
            // Base query: join Employee → EmploymentDetails → EmployeeSalaryDetails
            // Also join EmploymentDetailsHistory to resolve the branch for the
            // selected period.  We use a LEFT JOIN so employees who have no
            // history row (pre-backfill edge case) still appear, falling back
            // to their current EMP_BRANCH_CODE.
            // ---------------------------------------------------------------
            var baseQuery = from e in _oBMSDbContext.Employees
                            join ed  in _oBMSDbContext.EmploymentDetails          on e.EMP_CODE equals ed.EMPPAY_CODE
                            join esd in _oBMSDbContext.EmployeeSalaryDetails       on e.EMP_CODE equals esd.EMPFL_CODE
                            // History row that was active during the selected month
                            join edh in _oBMSDbContext.EmploymentDetailsHistories
                                on e.EMP_ID equals edh.EMP_ID into edhGroup
                            from edh in edhGroup
                                .Where(h => h.Emp_StartDate <= lastDayOfMonth &&
                                            (h.Emp_EndDate == null || h.Emp_EndDate >= firstDayOfMonth))
                                .DefaultIfEmpty()
                            select new
                            {
                                Employee         = e,
                                EmploymentDetail = ed,
                                // Effective branch for this period:
                                // use history branch if available, otherwise current branch
                                EffectiveBranch  = edh != null ? edh.EMPPAY_BRANCHCODE : e.EMP_BRANCH_CODE
                            };

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                {
                    if (resignedDate.Year != 1 && resignedDate.Month == 12)
                    {
                        baseQuery = baseQuery.Where(q =>
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                            (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year  >= resignedDate.Year - 1 &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                    }
                    else if (resignedDate.Year != 1)
                    {
                        baseQuery = baseQuery.Where(q =>
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                            (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year  >= resignedDate.Year &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                    }
                }
                else if (status == "Inactive")
                {
                    baseQuery = baseQuery.Where(q => q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null);
                }
            }

            // Employee type filter
            if (!string.IsNullOrEmpty(employeeType))
            {
                if (employeeType == "Others")
                    baseQuery = baseQuery.Where(q =>
                        q.Employee.EMP_ROLE != "Guard" &&
                        q.Employee.EMP_ROLE != "Staff" &&
                        q.Employee.EMP_ROLE != "Foreign Guard" &&
                        q.Employee.EMP_ROLE != "FGuard");
                else if (employeeType == "FGuard" || employeeType == "Foreign Guard")
                    baseQuery = baseQuery.Where(q => q.Employee.EMP_ROLE == "Guard" && q.Employee.EMP_CITIZEN == 1);
                else
                    baseQuery = baseQuery.Where(q => q.Employee.EMP_ROLE == employeeType);
            }

            // Branch filter — use effective (history-aware) branch
            if (!string.IsNullOrEmpty(branch))
                baseQuery = baseQuery.Where(q => q.EffectiveBranch == branch);

            // Resigned / joined date range filters
            if (resignedDate.Year != 1)
                baseQuery = baseQuery.Where(q =>
                    q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                    q.EmploymentDetail.EMPPAY_DATE_RESIGNED >= resignedDate);

            if (joinDate.Year != 1)
                baseQuery = baseQuery.Where(q => q.EmploymentDetail.EMPPAY_DATE_JOINED <= joinDate);

            // Project and deduplicate (a transferred employee can have multiple history rows
            // but should appear only once in the list)
            result = baseQuery
                .OrderBy(q => q.Employee.EMP_NAME)
                .Select(q => new EmployeeDto
                {
                    EMP_ID           = q.Employee.EMP_ID,
                    EMP_ROLE         = q.Employee.EMP_ROLE,
                    EMP_CODE         = q.Employee.EMP_CODE,
                    EMP_NAME         = string.IsNullOrEmpty(q.Employee.EMP_NAME)
                                           ? q.Employee.EMP_NAME
                                           : q.Employee.EMP_NAME.Replace("''", "'"),
                    EMP_CLIENT       = q.Employee.EMP_CLIENT,
                    EMP_ADDRESS1     = q.Employee.EMP_ADDRESS1,
                    EMP_ADDRESS2     = q.Employee.EMP_ADDRESS2,
                    EMP_POST_CODE    = q.Employee.EMP_POST_CODE,
                    EMP_TOWN         = q.Employee.EMP_TOWN,
                    EMP_STATE        = q.Employee.EMP_STATE,
                    EMP_NATIONAL     = q.Employee.EMP_NATIONAL,
                    EMP_PHONE        = q.Employee.EMP_PHONE,
                    EMP_MOBILEPHONE  = q.Employee.EMP_MOBILEPHONE,
                    EMP_CITIZEN      = q.Employee.EMP_CITIZEN,
                    EMP_CHECKLIST    = q.Employee.EMP_CHECKLIST,
                    EMP_HGH_EDU      = q.Employee.EMP_HGH_EDU,
                    EM_WORK_EXP      = q.Employee.EM_WORK_EXP,
                    EMP_DATE_OF_BIRTH = q.Employee.EMP_DATE_OF_BIRTH,
                    EMP_IC_OLD       = q.Employee.EMP_IC_OLD,
                    EMP_IC_NEW       = q.Employee.EMP_IC_NEW,
                    EMP_IC_COLOR     = q.Employee.EMP_IC_COLOR,
                    EMP_PASSPORT_NO  = q.Employee.EMP_PASSPORT_NO,
                    EMP_SEX          = q.Employee.EMP_SEX,
                    EMP_RACE         = q.Employee.EMP_RACE,
                    EMP_MARTIAL_STATUS = q.Employee.EMP_MARTIAL_STATUS,
                    EMP_SPOUSE_NAME  = q.Employee.EMP_SPOUSE_NAME,
                    EMP_SP_IC        = q.Employee.EMP_SP_IC,
                    EMP_NO_CHILD     = q.Employee.EMP_NO_CHILD,
                    EMP_SP_WORK      = q.Employee.EMP_SP_WORK,
                    EMP_PER_NAME_CONTACT    = q.Employee.EMP_PER_NAME_CONTACT,
                    EMP_CONTACT_ADDRESS1    = q.Employee.EMP_CONTACT_ADDRESS1,
                    EMP_CONTACT_ADDRESS2    = q.Employee.EMP_CONTACT_ADDRESS2,
                    EMP_CONTACT_POST_CODE   = q.Employee.EMP_CONTACT_POST_CODE,
                    EMP_CONTACT_TOWN        = q.Employee.EMP_CONTACT_TOWN,
                    EMP_CONTACT_STATE       = q.Employee.EMP_CONTACT_STATE,
                    EMP_CONTACT_TELEPHONE   = q.Employee.EMP_CONTACT_TELEPHONE,
                    // Return the current branch code for display; the list is already
                    // filtered by effective (history) branch above.
                    EMP_BRANCH_CODE  = q.Employee.EMP_BRANCH_CODE,
                    OldBranch        = q.Employee.OldBranch,
                    TransferDate     = q.Employee.TransferDate,
                    LASTUPDATE       = q.Employee.LASTUPDATE,
                    NewSalaryStructure   = q.Employee.NewSalaryStructure,
                    SalaryStructure1000_3h = q.Employee.SalaryStructure1000_3h
                })
                .Distinct()
                .ToList();

            return result;
        }

        public List<EmployeeDto> GetListEmployeeByClient(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status, string empClient)
        {
            // resignedDate = first day of selected attendance month (StartPeriod)
            // joinDate     = last day  of selected attendance month (EndPeriod)
            // Branch is resolved from EmploymentDetailsHistory for the selected period
            // so that transferred employees appear under the branch they were in that month.

            var result = new List<EmployeeDto>();

            bool hasPeriod = joinDate.Year != 1 && resignedDate.Year != 1;

            DateTime firstDayOfMonth = hasPeriod ? resignedDate.Date : DateTime.MinValue;
            DateTime lastDayOfMonth  = hasPeriod ? joinDate.Date    : DateTime.MaxValue;

            // Base query with history-aware branch join
            var baseQuery = from e in _oBMSDbContext.Employees
                            join ed  in _oBMSDbContext.EmploymentDetails          on e.EMP_CODE equals ed.EMPPAY_CODE
                            join esd in _oBMSDbContext.EmployeeSalaryDetails       on e.EMP_CODE equals esd.EMPFL_CODE
                            join edh in _oBMSDbContext.EmploymentDetailsHistories
                                on e.EMP_ID equals edh.EMP_ID into edhGroup
                            from edh in edhGroup
                                .Where(h => h.Emp_StartDate <= lastDayOfMonth &&
                                            (h.Emp_EndDate == null || h.Emp_EndDate >= firstDayOfMonth))
                                .DefaultIfEmpty()
                            select new
                            {
                                Employee         = e,
                                EmploymentDetail = ed,
                                EffectiveBranch  = edh != null ? edh.EMPPAY_BRANCHCODE : e.EMP_BRANCH_CODE
                            };

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                {
                    if (resignedDate.Year != 1 && resignedDate.Month == 12)
                    {
                        baseQuery = baseQuery.Where(q =>
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                            (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year  >= resignedDate.Year - 1 &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                    }
                    else if (resignedDate.Year != 1)
                    {
                        baseQuery = baseQuery.Where(q =>
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                            (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year  >= resignedDate.Year &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                    }
                }
                else if (status == "Inactive")
                {
                    baseQuery = baseQuery.Where(q => q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null);
                }
            }

            // Employee type filter
            if (!string.IsNullOrEmpty(employeeType))
            {
                if (employeeType == "Others")
                    baseQuery = baseQuery.Where(q =>
                        q.Employee.EMP_ROLE != "Guard" &&
                        q.Employee.EMP_ROLE != "Staff" &&
                        q.Employee.EMP_ROLE != "Foreign Guard" &&
                        q.Employee.EMP_ROLE != "FGuard");
                else
                    baseQuery = baseQuery.Where(q => q.Employee.EMP_ROLE == employeeType);
            }

            // Client filter
            if (!string.IsNullOrEmpty(empClient))
                baseQuery = baseQuery.Where(q => q.Employee.EMP_CLIENT == empClient);

            // Branch filter — use effective (history-aware) branch
            if (!string.IsNullOrEmpty(branch))
                baseQuery = baseQuery.Where(q => q.EffectiveBranch == branch);

            // Date range filters
            if (resignedDate.Year != 1)
                baseQuery = baseQuery.Where(q =>
                    q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                    q.EmploymentDetail.EMPPAY_DATE_RESIGNED >= resignedDate);

            if (joinDate.Year != 1)
                baseQuery = baseQuery.Where(q => q.EmploymentDetail.EMPPAY_DATE_JOINED <= joinDate);

            result = baseQuery
                .OrderBy(q => q.Employee.EMP_NAME)
                .Select(q => new EmployeeDto
                {
                    EMP_ID           = q.Employee.EMP_ID,
                    EMP_ROLE         = q.Employee.EMP_ROLE,
                    EMP_CODE         = q.Employee.EMP_CODE,
                    EMP_NAME         = string.IsNullOrEmpty(q.Employee.EMP_NAME)
                                           ? q.Employee.EMP_NAME
                                           : q.Employee.EMP_NAME.Replace("''", "'"),
                    EMP_CLIENT       = q.Employee.EMP_CLIENT,
                    EMP_ADDRESS1     = q.Employee.EMP_ADDRESS1,
                    EMP_ADDRESS2     = q.Employee.EMP_ADDRESS2,
                    EMP_POST_CODE    = q.Employee.EMP_POST_CODE,
                    EMP_TOWN         = q.Employee.EMP_TOWN,
                    EMP_STATE        = q.Employee.EMP_STATE,
                    EMP_NATIONAL     = q.Employee.EMP_NATIONAL,
                    EMP_PHONE        = q.Employee.EMP_PHONE,
                    EMP_MOBILEPHONE  = q.Employee.EMP_MOBILEPHONE,
                    EMP_CITIZEN      = q.Employee.EMP_CITIZEN,
                    EMP_CHECKLIST    = q.Employee.EMP_CHECKLIST,
                    EMP_HGH_EDU      = q.Employee.EMP_HGH_EDU,
                    EM_WORK_EXP      = q.Employee.EM_WORK_EXP,
                    EMP_DATE_OF_BIRTH = q.Employee.EMP_DATE_OF_BIRTH,
                    EMP_IC_OLD       = q.Employee.EMP_IC_OLD,
                    EMP_IC_NEW       = q.Employee.EMP_IC_NEW,
                    EMP_IC_COLOR     = q.Employee.EMP_IC_COLOR,
                    EMP_PASSPORT_NO  = q.Employee.EMP_PASSPORT_NO,
                    EMP_SEX          = q.Employee.EMP_SEX,
                    EMP_RACE         = q.Employee.EMP_RACE,
                    EMP_MARTIAL_STATUS = q.Employee.EMP_MARTIAL_STATUS,
                    EMP_SPOUSE_NAME  = q.Employee.EMP_SPOUSE_NAME,
                    EMP_SP_IC        = q.Employee.EMP_SP_IC,
                    EMP_NO_CHILD     = q.Employee.EMP_NO_CHILD,
                    EMP_SP_WORK      = q.Employee.EMP_SP_WORK,
                    EMP_PER_NAME_CONTACT  = q.Employee.EMP_PER_NAME_CONTACT,
                    EMP_CONTACT_ADDRESS1  = q.Employee.EMP_CONTACT_ADDRESS1,
                    EMP_CONTACT_ADDRESS2  = q.Employee.EMP_CONTACT_ADDRESS2,
                    EMP_CONTACT_POST_CODE = q.Employee.EMP_CONTACT_POST_CODE,
                    EMP_CONTACT_TOWN      = q.Employee.EMP_CONTACT_TOWN,
                    EMP_CONTACT_STATE     = q.Employee.EMP_CONTACT_STATE,
                    EMP_CONTACT_TELEPHONE = q.Employee.EMP_CONTACT_TELEPHONE,
                    EMP_BRANCH_CODE  = q.Employee.EMP_BRANCH_CODE,
                    OldBranch        = q.Employee.OldBranch,
                    TransferDate     = q.Employee.TransferDate,
                    LASTUPDATE       = q.Employee.LASTUPDATE,
                    NewSalaryStructure   = q.Employee.NewSalaryStructure,
                    SalaryStructure1000_3h = q.Employee.SalaryStructure1000_3h
                })
                .Distinct()
                .ToList();

            return result;
        }

        public async Task<List<SalaryAdvanceDto>> GetListByEmplyeeType(DateTime advanceDate, string branch, string employeeType, int transType, decimal advanceAmount, string race)
        {
            if (race == "All")
            {
                string[] raceArray = { "Chinese", "Indian", "Malay", "Others" };
                race = "('" + string.Join("','", raceArray) + "')";

            }

            var query = from employee in _oBMSDbContext.Employees
                         join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails
                             on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                         join employmentDetails in _oBMSDbContext.EmploymentDetails
                             on employee.EMP_CODE equals employmentDetails.EMPPAY_CODE
                         join salaryAdvanceTemp in _oBMSDbContext.SalaryAdvances
                             .Where(sa => sa.TransType == transType
                                          && !sa.IsDeleted
                                          && sa.AdvanceDate.Month == advanceDate.Month
                                          && sa.AdvanceDate.Year == advanceDate.Year)
                             on employee.EMP_ID equals salaryAdvanceTemp.EmployeeID into salaryAdvanceGroup
                         from salaryAdvance in salaryAdvanceGroup.DefaultIfEmpty()
                         where employee.EMP_BRANCH_CODE == branch
                              && employee.HasTransfered == false
                         //&& !salaryAdvance.IsDeleted
                          && race.Contains(employee.EMP_RACE)

                         orderby employee.EMP_NAME
                         select new
                         {
                             employee,
                             salaryDetails,
                             employmentDetails,
                             salaryAdvance
                         };

            if (!string.IsNullOrEmpty(employeeType))
            {
                if (employeeType == "Others")
                    query = query.Where(x => x.employee.EMP_ROLE != "Guard" && x.employee.EMP_ROLE != "Staff" && x.employee.EMP_ROLE != "Foreign Guard" && x.employee.EMP_ROLE != "FGuard");
                else
                    query = query.Where(x => x.employee.EMP_ROLE == employeeType);
            }

            var result = query.AsEnumerable() // <-- Forces execution in memory
             .Where(x =>
             {
                 DateTime joinedDate = Convert.ToDateTime(x.employmentDetails.EMPPAY_DATE_JOINED);
                 DateTime resignedDate = string.IsNullOrEmpty(x.employmentDetails.EMPPAY_DATE_RESIGNED.ToString())
                                         ? new DateTime(2100, 1, 1)
                                         : Convert.ToDateTime(x.employmentDetails.EMPPAY_DATE_RESIGNED);
                 DateTime compareDate = x.salaryAdvance?.AdvanceDate ?? advanceDate;

                 return joinedDate <= compareDate && resignedDate >= compareDate;
             })
             .Select(x => new SalaryAdvanceDto
             {
                 ID = x.salaryAdvance?.ID ?? 0,
                 EMP_ID = x.employee.EMP_ID,
                 EMP_CODE = x.employee.EMP_CODE,
                 EMP_NAME = string.IsNullOrEmpty(x.employee.EMP_NAME)
                            ? x.employee.EMP_NAME
                            : x.employee.EMP_NAME.Replace("''", "'"),
                 EMP_RACE = x.employee.EMP_RACE,
                 EMP_ROLE = x.employee.EMP_ROLE,
                 EMP_IC_NEW = x.employee.EMP_IC_NEW ?? "",
                 EMP_IC_OLD = x.employee.EMP_IC_OLD ?? "",
                 EMP_PASSPORT_NO = x.employee.EMP_PASSPORT_NO ?? "",
                 EMPFL_BANK = x.salaryDetails.EMPFL_BANK ?? "",
                 EMPFL_BK_ACCNO = x.salaryDetails.EMPFL_BK_ACCNO ?? "",
                 PAYMODE = x.salaryDetails.PAYMODE ?? "",
                 Amount = x.salaryAdvance?.Amount ?? 0,
                 Particulars = x.salaryAdvance?.Particulars ?? ""
             })
             .Distinct()
             .ToList();        


            return result;

        }

        public async Task<List<SalaryAdvanceDto>> GetEmployeeAdvanceList(DateTime advanceDate, string branch, string employeeType, string client, int transType, decimal advanceAmount, string race)
        {
            // Handle race filter properly
            List<string> raceList = null;
            if (race == "All")
            {
                raceList = new List<string> { "Chinese", "Indian", "Malay", "Others" };
            }
            else if (!string.IsNullOrEmpty(race))
            {
                raceList = new List<string> { race };
            }

            var query = from employee in _oBMSDbContext.Employees
                         join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails
                             on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                         join employmentDetails in _oBMSDbContext.EmploymentDetails
                             on employee.EMP_CODE equals employmentDetails.EMPPAY_CODE
                         join salaryAdvanceTemp in _oBMSDbContext.SalaryAdvances
                             .Where(sa => sa.TransType == transType
                                          && !sa.IsDeleted
                                          && sa.AdvanceDate.Month == advanceDate.Month
                                          && sa.AdvanceDate.Year == advanceDate.Year)
                             on employee.EMP_ID equals salaryAdvanceTemp.EmployeeID into salaryAdvanceGroup
                         from salaryAdvance in salaryAdvanceGroup.DefaultIfEmpty()
                         where employee.EMP_BRANCH_CODE == branch
                               && employee.HasTransfered == false
                               && employee.EMP_CLIENT == client
                               && (raceList == null || raceList.Contains(employee.EMP_RACE))
                         orderby employee.EMP_NAME
                         select new
                         {
                             employee,
                             salaryDetails,
                             employmentDetails,
                             salaryAdvance
                         };

            if (!string.IsNullOrEmpty(employeeType))
            {
                if (employeeType == "Others")
                    query = query.Where(x => x.employee.EMP_ROLE != "Guard" && x.employee.EMP_ROLE != "Staff" && x.employee.EMP_ROLE != "Foreign Guard" && x.employee.EMP_ROLE != "FGuard");
                else
                    query = query.Where(x => x.employee.EMP_ROLE == employeeType);
            }

            var data =  query
                .AsEnumerable() // keep date comparison logic same as old SQL
                .Where(x =>
                {
                    DateTime joinedDate = x.employmentDetails.EMPPAY_DATE_JOINED;
                    DateTime resignedDate = x.employmentDetails.EMPPAY_DATE_RESIGNED ?? new DateTime(2100, 1, 1);
                    DateTime compareDate = x.salaryAdvance?.AdvanceDate ?? advanceDate;

                    return joinedDate <= compareDate && resignedDate >= compareDate;
                })
                .Select(x => new SalaryAdvanceDto
                {
                    ID = x.salaryAdvance?.ID ?? 0,
                    EMP_ID = x.employee.EMP_ID,
                    EMP_CODE = x.employee.EMP_CODE,
                    EMP_NAME = string.IsNullOrEmpty(x.employee.EMP_NAME)
                            ? x.employee.EMP_NAME
                            : x.employee.EMP_NAME.Replace("''", "'"),
                    EMP_RACE = x.employee.EMP_RACE,
                    EMP_ROLE = x.employee.EMP_ROLE,
                    EMP_IC_NEW = x.employee.EMP_IC_NEW ?? "",
                    EMP_IC_OLD = x.employee.EMP_IC_OLD ?? "",
                    EMP_PASSPORT_NO = x.employee.EMP_PASSPORT_NO ?? "",
                    EMPFL_BANK = x.salaryDetails.EMPFL_BANK ?? "",
                    EMPFL_BK_ACCNO = x.salaryDetails.EMPFL_BK_ACCNO ?? "",
                    PAYMODE = x.salaryDetails.PAYMODE ?? "",
                    Amount = x.salaryAdvance?.Amount ?? advanceAmount,
                    Particulars = x.salaryAdvance?.Particulars ?? ""
                })
                .Distinct()
                .ToList();

            return data;
        }


        #endregion

        #region Salary Processing
        public string LastSalaryProcessRemarks(DateTime period, string branchCode, string employeeType)
        {
            var remarks = _oBMSDbContext.SalaryProcess
                 .Where(sp => sp.Period.Year == period.Year
                           && sp.Period.Month == period.Month
                           && sp.Branch == branchCode
                           && sp.EmployeeType == employeeType)
                 .OrderByDescending(sp => sp.LastUpdate)
                 .Select(sp => sp.Remarks)
                 .FirstOrDefault();

            return remarks;
        }
        public bool IsSalaryProcessDoneForCurrentPeriod(string branch, string employeeType, DateTime dtPeriod)
        {
            var isProcessed = _oBMSDbContext.SalaryProcess
                   .Any(sp =>
                       sp.EmployeeType == employeeType &&
                       sp.Branch == branch &&
                       sp.Period.Year == dtPeriod.Year &&
                       sp.Period.Month == dtPeriod.Month);

            return isProcessed;
        }
        public List<string> GetEmployeeAttendanceList(DateTime period, string branch)
{
    var nameList = _oBMSDbContext.Attendances
        .Where(a => a.Period == period && a.Branch == branch)
        .Join(
            _oBMSDbContext.EmployeeHistories,   // Changed here
            attendance => attendance.EmployeeID,
            history => history.EMP_ID,
            (attendance, history) => history.EMP_CODE
        )
        .ToList();

    return nameList;
}
        public bool IsTemporaryEmployee(string employeeCode)
        {
            try
            {
                var employee = _oBMSDbContext.EmployeeSalaryDetails
                    .Where(e => e.EMPFL_CODE == employeeCode)
                    .FirstOrDefault();

                return employee?.TMPGUARD ?? false;
            }
            catch
            {
                throw;
            }
        }

        public List<string> GetTemporaryEmployeeList(string branch)
        {
            try
            {
                var tempList = _oBMSDbContext.EmployeeSalaryDetails
                    .Where(e => e.TMPGUARD == true && e.EMPFL_BRANCHCODE == branch)
                    .Select(e => e.EMPFL_CODE)
                    .ToList();

                return tempList;
            }
            catch
            {
                throw;
            }
        }
        public string Process(string branch, string employeeType, string remarks, DateTime period, bool lockProcess, string currentUser, string companyCode)
        {
            string status = string.Empty;
            var attendanceQuery = (from attendance in _oBMSDbContext.Attendances
                                   join employee in _oBMSDbContext.Employees on attendance.EmployeeID equals employee.EMP_ID
                                   where attendance.Branch == branch && attendance.Period == period
                                   orderby employee.EMP_NAME
                                   select new { attendance.Branch, attendance.EmployeeID, EMP_ROLE = employee.EMP_ROLE });

            if (!string.IsNullOrEmpty(employeeType))
            {
                if (employeeType == "Others")
                    attendanceQuery = attendanceQuery.Where(x => !x.EMP_ROLE.Contains("Guard") && !x.EMP_ROLE.Contains("Staff") && !x.EMP_ROLE.Contains("FGuard"));
                else
                    attendanceQuery = attendanceQuery.Where(x => x.EMP_ROLE.Contains(employeeType));
            }

            List<int> employeeIDs = attendanceQuery.Select(a => a.EmployeeID).ToList();
            var salaryProcess = _oBMSDbContext.SalaryProcess
                    .FirstOrDefault(sp => sp.Period == period && sp.Branch == branch && sp.EmployeeType.Contains(employeeType));

            if (salaryProcess != null)
            {
                if (salaryProcess.IsLocked)
                    return "Salary Processing for the month is locked. It cannot be recomputed again.";

                salaryProcess.ID = salaryProcess.ID;
                salaryProcess.LastUpdate = DateTime.Now;
                salaryProcess.LastUpdatedBy = currentUser;
                salaryProcess.IsLocked = lockProcess;
                salaryProcess.Remarks = remarks;
                _oBMSDbContext.SalaryProcess.Update(salaryProcess);
                status = "updated";
            }
            else
            {
                _oBMSDbContext.SalaryProcess.Add(new SalaryProcess
                {
                    Branch = branch,
                    Period = period,
                    EmployeeType = employeeType,
                    IsLocked = lockProcess,
                    Remarks = remarks,
                    LastUpdate = DateTime.Now,
                    LastUpdatedBy = currentUser
                });
                status = "inserted";
            }

            //  _oBMSDbContext.SaveChanges();
            if (attendanceQuery != null)
            {
                decimal dAttendanceAllowance = 0;
                decimal dSpecialAllowance = 0;
                decimal dAttendanceAllowanceDays = 0;
                string sAttendanceAllowanceFollowCalendar = string.Empty;
                decimal dReAllowance = 0;
                decimal dReAllowanceRate = 0;
                var query = from employmentDetails in _oBMSDbContext.EmploymentDetails
                            join employee in _oBMSDbContext.Employees on employmentDetails.EMPPAY_CODE equals employee.EMP_CODE
                            join employeeSalaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals employeeSalaryDetails.EMPFL_CODE
                            join salaryStructure in _oBMSDbContext.SalaryStructure on employmentDetails.SALARYLAB equals salaryStructure.SalaryId
                            where employeeIDs.Contains(employee.EMP_ID)
                            select new
                            {
                                employeeSalaryDetails.TMPGUARD,
                                employee.EMP_DATE_OF_BIRTH,
                                employee.EMP_CITIZEN,
                                employmentDetails.EMPPAY_DATE_JOINED,
                                employmentDetails.EMPPAY_DATE_RESIGNED,
                                employmentDetails.EMPPAY_BASIC_RATE,
                                employmentDetails.ATTENDANCEALLOWANCE,
                                employmentDetails.SpecialAllowance,
                                employmentDetails.AttendanceAllowanceWorkingDays,
                                employmentDetails.AttendanceAllowanceFollowCalendar,
                                salaryStructure.EmployeeNationality,
                                salaryStructure.WorkingHours,
                                salaryStructure.Name,
                                salaryStructure.TravelAllowance,
                                salaryStructure.GeneralDayRate,
                                salaryStructure.GeneralDayHours,
                                salaryStructure.WorkingDays,
                                salaryStructure.GeneralDayOTRate,
                                salaryStructure.OffDayRate,
                                salaryStructure.OffDayOTRate,
                                salaryStructure.HolidayRate,
                                salaryStructure.HolidayOTRate,
                                salaryStructure.SalaryBand,
                                salaryStructure.EICC,
                                salaryStructure.NonStructure,
                                employee.NewSalaryStructure,
                                employee.SalaryStructure1000_3h,
                                employee.EMP_PASSPORT_NO
                            };

                var drEmployee = query.FirstOrDefault();

                bool DeductEPF8Pa = false;
                bool DeductEPF = false;
                bool DeductSOCSO = false;
                bool DeductEPFBeyond55 = false;
                bool DeductIncomeTax = false; //IncomeTax
                decimal WorkingHours = 0;
                decimal GeneralDayHours = 0;
                int EmployeeAge = 0;
                int strCitizen = 0;
                decimal NoOfWorkingDays = 0;
                string SalaryPayMode = string.Empty;
                string EmployeeNationality = string.Empty;
                decimal dAttendanceDeduction = 0;
                decimal dSpecialAttendanceDeduction = 0;
                double dEmployeeBasicRate = 0;
                decimal dBonus = 0;
                int sSalaryStructure1000_3h = 0;
                string sEmpPassport = string.Empty;
                bool bNonStructure = false;
                bool bTmpGuard = false;

                if (drEmployee != null)
                {
                    //EmployeeAge = period.Year - drEmployee.EMP_DATE_OF_BIRTH.Year;
                    //if (period.DayOfYear < drEmployee.EMP_DATE_OF_BIRTH.DayOfYear)
                    //{
                    //    EmployeeAge = EmployeeAge - 1;
                    //}
                    WorkingHours = drEmployee.WorkingHours;
                    GeneralDayHours = drEmployee.GeneralDayHours;
                    NoOfWorkingDays = drEmployee.WorkingDays;
                    dAttendanceAllowance = (decimal)drEmployee.ATTENDANCEALLOWANCE;
                    dSpecialAllowance = drEmployee.SpecialAllowance;
                    EmployeeNationality = drEmployee.EmployeeNationality;
                    sAttendanceAllowanceFollowCalendar = drEmployee.AttendanceAllowanceFollowCalendar;
                    dAttendanceAllowanceDays = (decimal)drEmployee.AttendanceAllowanceWorkingDays;
                    dEmployeeBasicRate = drEmployee.EMPPAY_BASIC_RATE;
                    strCitizen = drEmployee.EMP_CITIZEN;
                    sEmpPassport = drEmployee.EMP_PASSPORT_NO;
                    bNonStructure = drEmployee.NonStructure;
                    bTmpGuard = drEmployee.TMPGUARD;

                }

                var drAllowanceDeduct = _oBMSDbContext.Attendances
                           .Where(a => employeeIDs.Contains(a.EmployeeID) &&
                                       a.Period.Month == period.Month &&
                                       a.Period.Year == period.Year)
                           .Select(a => new
                           {
                               a.AllowanceDeduction,
                               a.SpecialAllowanceDeduction
                           }).FirstOrDefault();

                if (drAllowanceDeduct != null)
                {
                    if (employeeType == "Staff")
                    {
                        //Normal Allowance
                        dAttendanceAllowance -= drAllowanceDeduct.AllowanceDeduction;
                        dAttendanceDeduction = drAllowanceDeduct.AllowanceDeduction;

                        //Special Allowance deduction
                        dSpecialAttendanceDeduction = drAllowanceDeduct.SpecialAllowanceDeduction;
                        dSpecialAllowance = dSpecialAllowance - dSpecialAttendanceDeduction;

                    }
                    else
                    {
                        //Normal Allowance
                        dAttendanceDeduction = drAllowanceDeduct.AllowanceDeduction;
                        dSpecialAttendanceDeduction = drAllowanceDeduct.SpecialAllowanceDeduction;

                        //Special Allowance deduction
                        dSpecialAllowance = dSpecialAllowance - dSpecialAttendanceDeduction;

                    }
                }

                decimal OTGeneralDayHours = GeneralDayHours;
                OTGeneralDayHours = OTGeneralDayHours + Convert.ToDecimal(sSalaryStructure1000_3h);
            }
            return status;
        }
        public async Task<DateTime?> GetLatestAttendancePeriodAsync(int employeeId, int year, int month)
        {
            var record = await _oBMSDbContext.Attendances
                .Where(a => a.EmployeeID == employeeId &&
                            a.Period.Year == year &&
                            a.Period.Month > month)
                .OrderBy(a => a.Period)
                .FirstOrDefaultAsync();

            return record?.Period;
        }

        public async Task<List<MiscTransDto>> GetList(DateTime transDate, decimal employeeId)
        {
            try
            {
                return await _oBMSDbContext.MiscTrans
                    .Where(x => x.EmployeeID == employeeId &&
                                x.TransDate.Month == transDate.Month &&
                                x.TransDate.Year == transDate.Year)
                    .Select(x => new MiscTransDto
                    {
                        ID = x.ID,
                        EmployeeID = x.EmployeeID,
                        TransDate = x.TransDate,
                        Amount = x.Amount,
                        TransType = x.TransType,
                        Particulars = x.Particulars ?? "",
                        LastUpdate = x.LastUpdate
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving Misc Transactions", ex);
            }
        }
        #endregion
    }
}
