using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace OBMS.WebAPI.BusinessObjects
{
    public class SalaryProcess : ISalaryProcess
    {
        private decimal EmployeeID = 0;
        private readonly IConfiguration _configuration;

        public SalaryProcess(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public List<KKDNExcelListDto> GetListWithBlankRow(string Branch, string EmployeeType, DateTime dtDateJoinFrom, DateTime dtDateJoinTo, string KDNVetting)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sQuery = string.Empty;
                    sQuery = " SELECT * FROM vwKKDN ";
                    sQuery = sQuery + " WHERE HasTransfer = 0 ";
                    int bKDNVetting = 0;
                    if (KDNVetting != "")
                    {
                        if (KDNVetting == "YES")
                        {
                            bKDNVetting = 1;
                        }
                        sQuery = sQuery + " And KDNVetting = @KDNVetting ";
                    }

                    if (Branch != "" && Branch != "0")
                    {
                        sQuery = sQuery + " And BranchCode = @Branch ";
                    }

                    //    sQuery = sQuery + " And DateJoin Between @DateJoinFrom and @DateJoinTo ";

                    sQuery = sQuery + " ORDER BY Name ";
                    cmd.CommandText = sQuery;
                    if (KDNVetting != "")
                    {
                        cmd.Parameters.AddWithValue("@KDNVetting", bKDNVetting);
                    }
                    if (Branch != "" && Branch != "0")
                    {
                        cmd.Parameters.AddWithValue("@Branch", Branch);
                    }
                    //cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    //   cmd.Parameters.AddWithValue("@DateJoinFrom", dtDateJoinFrom);
                    //   cmd.Parameters.AddWithValue("@DateJoinTo", dtDateJoinTo);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(_configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<KKDNExcelListDto> sKKDNExcelList = new List<KKDNExcelListDto>();

                            while (dr.Read())
                            {
                                string sNationlityValue;
                                if (dr.IsDBNull(dr.GetOrdinal("Nationality")))
                                {
                                    sNationlityValue = string.Empty;
                                }
                                else
                                {
                                    sNationlityValue = dr.GetString(dr.GetOrdinal("Nationality")).ToString();
                                }
                                string sCitizenValue;
                                if (dr.IsDBNull(dr.GetOrdinal("Citizen")))
                                {
                                    sCitizenValue = string.Empty;
                                }
                                else
                                {
                                    sCitizenValue = dr.GetString(dr.GetOrdinal("Citizen")).ToString();
                                }
                                string sEPFValue;
                                if (dr.IsDBNull(dr.GetOrdinal("EPF")))
                                {
                                    sEPFValue = string.Empty;
                                }
                                else
                                {
                                    sEPFValue = dr.GetString(dr.GetOrdinal("EPF")).ToString();
                                }
                                string sSOCSOValue;
                                if (dr.IsDBNull(dr.GetOrdinal("SOSCO")))
                                {
                                    sSOCSOValue = string.Empty;
                                }
                                else
                                {
                                    sSOCSOValue = dr.GetString(dr.GetOrdinal("SOSCO")).ToString();
                                }

                                string sKDNVetting;
                                if (dr.IsDBNull(dr.GetOrdinal("KDNVetting")))
                                {
                                    sKDNVetting = string.Empty;
                                }
                                else
                                {
                                    sKDNVetting = dr.GetBoolean(dr.GetOrdinal("KDNVetting")).ToString();
                                }

                                sKKDNExcelList.Add(
                                         new KKDNExcelListDto(
                                             dr.GetInt32(dr.GetOrdinal("EmpID")),
                                             dr.GetString(dr.GetOrdinal("BranchCode")),
                                             dr.GetString(dr.GetOrdinal("Name")),
                                             dr.GetString(dr.GetOrdinal("IC")),
                                             dr.GetDateTime(dr.GetOrdinal("DOB")),
                                             sNationlityValue.ToString(),
                                             sCitizenValue.ToString(),
                                             dr.GetString(dr.GetOrdinal("Race")),
                                             dr.GetString(dr.GetOrdinal("Gender")),
                                             dr.GetString(dr.GetOrdinal("Address")),
                                             dr.GetDateTime(dr.GetOrdinal("DateJoin")),
                                             dr.GetString(dr.GetOrdinal("JobTitle")),
                                             sEPFValue.ToString(),
                                             sSOCSOValue,
                                             Convert.ToBoolean(sKDNVetting),
                                             dr.GetBoolean(dr.GetOrdinal("HasTransfer"))
                                         )
                                     );
                            }
                            return sKKDNExcelList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public string Process(string Branch, string EmployeeType, string Remarks, DateTime Period, bool LockProcess, string CurrentUser, string CompanyCode)
        {
            try
            {
                //Get latest Version - amend on 4 Feb 2012
                decimal sSalaryProcessVersion = 0;
                //decimal sPaySlipVersion = 0;

                sSalaryProcessVersion = UtilityMain.GetPreviousSalaryProcessAuditVersion(Branch, EmployeeType, Period);
                //sPaySlipVersion = Utility.GetPreviousPayslipAuditVersion(Period);

                //End

                StringBuilder sbQuery = new StringBuilder();
                using (SQLDataAccess sdaAttendance = new SQLDataAccess(_configuration))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        //cmd.CommandText = " SELECT Branch,EmployeeID FROM Attendance INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID WHERE Branch=@Branch AND EMP_ROLE = @EmployeeType AND Period=@AttendancePeriod";
                        //cmd.CommandText = " SELECT Branch,EmployeeID FROM Attendance INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID WHERE Branch=@Branch AND EMP_ROLE LIKE @EmployeeType AND Period=@AttendancePeriod and EMP_ID = '17241'";
                        cmd.CommandText = " SELECT Branch,EmployeeID FROM Attendance INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID WHERE Branch=@Branch AND EMP_ROLE LIKE @EmployeeType AND Period=@AttendancePeriod order by employee.emp_name  ";
                        cmd.Parameters.AddWithValue("@Branch", Branch);
                        cmd.Parameters.AddWithValue("@EmployeeType", "%" + EmployeeType + "%");
                        cmd.Parameters.AddWithValue("@AttendancePeriod", Period);

                        using (SqlDataReader dr = sdaAttendance.RetrieveData(cmd))
                        {
                            using (SQLDataAccess sdaPaySlip = new SQLDataAccess(_configuration))
                            {
                                SqlCommand cmdSalaryProcess = new SqlCommand();
                                SqlCommand cmdSalaryProcessAudit = new SqlCommand();
                                SQLDataAccess sdaSalaryProcess = new SQLDataAccess(_configuration);
                                //cmdSalaryProcess.CommandText = "SELECT ID,IsLocked FROM SalaryProcess WHERE Period = @Period AND Branch = @Branch AND EmployeeType = @EmployeeType";
                                cmdSalaryProcess.CommandText = "SELECT ID,IsLocked FROM SalaryProcess WHERE Period = @Period AND Branch = @Branch AND EmployeeType LIKE @EmployeeType";
                                cmdSalaryProcess.Parameters.AddWithValue("@Period", Period);
                                cmdSalaryProcess.Parameters.AddWithValue("@Branch", Branch);
                                cmdSalaryProcess.Parameters.AddWithValue("@EmployeeType", "%" + EmployeeType + "%");
                                SqlDataReader drSalaryProcess = sdaSalaryProcess.RetrieveData(cmdSalaryProcess);
                                bool bFoundRecord = false;
                                int ID = 0;
                                if (drSalaryProcess.HasRows)
                                {
                                    bFoundRecord = true;
                                    drSalaryProcess.Read();
                                    ID = drSalaryProcess.GetInt32(0);
                                    if (drSalaryProcess.GetBoolean(1))
                                        return "Salary Processing for the month is locked. It cannot be recomputed again.";
                                }
                                drSalaryProcess.Dispose();
                                cmdSalaryProcess.Dispose();
                                sdaSalaryProcess.Dispose();
                                cmdSalaryProcess = new SqlCommand();
                                if (bFoundRecord)
                                {
                                    cmdSalaryProcess.CommandText = "UPDATE SalaryProcess SET LastUpdate = @LastUpdate, LastUpdatedBy = @LastUpdatedBy ,IsLocked = @IsLocked,Remarks = @Remarks WHERE ID=@ID";
                                    cmdSalaryProcess.Parameters.AddWithValue("@ID", ID);
                                    cmdSalaryProcess.Parameters.AddWithValue("@IsLocked", LockProcess);
                                    cmdSalaryProcess.Parameters.AddWithValue("@Remarks", Remarks);
                                    cmdSalaryProcess.Parameters.AddWithValue("@LastUpdate", DateTime.Now);
                                    cmdSalaryProcess.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);
                                }
                                else
                                {
                                    cmdSalaryProcess.CommandText = "INSERT INTO SalaryProcess (Branch,Period,EmployeeType,IsLocked,Remarks,LastUpdate,LastUpdatedBy) VALUES(@Branch,@Period,@EmployeeType,@IsLocked,@Remarks,@LASTUPDATE,@LastUpdatedBy)";
                                    cmdSalaryProcess.Parameters.AddWithValue("@Branch", Branch);
                                    cmdSalaryProcess.Parameters.AddWithValue("@Period", Period);
                                    cmdSalaryProcess.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                                    cmdSalaryProcess.Parameters.AddWithValue("@IsLocked", LockProcess);
                                    cmdSalaryProcess.Parameters.AddWithValue("@Remarks", Remarks);
                                    cmdSalaryProcess.Parameters.AddWithValue("@LastUpdate", DateTime.Now);
                                    cmdSalaryProcess.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);
                                }
                                //Insert into SalaryProcessAudit

                                cmdSalaryProcessAudit = new SqlCommand();
                                cmdSalaryProcessAudit.CommandText = "INSERT INTO SalaryProcessAudit (Branch,Period,EmployeeType,IsLocked,Remarks,LastUpdate,LastUpdatedBy,Version) VALUES(@Branch,@Period,@EmployeeType,@IsLocked,@Remarks,@LASTUPDATE,@LastUpdatedBy,@Version)";
                                cmdSalaryProcessAudit.Parameters.AddWithValue("@Branch", Branch);
                                cmdSalaryProcessAudit.Parameters.AddWithValue("@Period", Period);
                                cmdSalaryProcessAudit.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                                cmdSalaryProcessAudit.Parameters.AddWithValue("@IsLocked", LockProcess);
                                cmdSalaryProcessAudit.Parameters.AddWithValue("@Remarks", Remarks);
                                cmdSalaryProcessAudit.Parameters.AddWithValue("@LastUpdate", DateTime.Now);
                                cmdSalaryProcessAudit.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);
                                cmdSalaryProcessAudit.Parameters.AddWithValue("@Version", sSalaryProcessVersion);

                                //End Insert 
                                sdaPaySlip.StartTransaction();
                                sdaPaySlip.ExecuteSQL(cmdSalaryProcess);
                                sdaPaySlip.ExecuteSQL(cmdSalaryProcessAudit);

                                cmdSalaryProcess.Dispose();
                                cmdSalaryProcessAudit.Dispose();

                                while (dr.Read())
                                {
                                    // getting drEmployee query
                                    EmployeeID = Convert.ToDecimal(dr["EmployeeID"]);
                                    using (SqlCommand cmdEmployee = new SqlCommand())
                                    {
                                        decimal dAttendanceAllowance = 0;
                                        decimal dSpecialAllowance = 0;
                                        decimal dAttendanceAllowanceDays = 0;
                                        string sAttendanceAllowanceFollowCalendar = string.Empty;
                                        decimal dReAllowance = 0;
                                        decimal dReAllowanceRate = 0;
                                        sbQuery = new StringBuilder();
                                        //***New Change 22/10/2024 to add special OT Hours from clientmaster for additional OT
                                        //***New Change 13/03/2025 to add KPI Hours from clientmaster for split Overtime calculation
                                        sbQuery.Append("SELECT Client.KPIHours,employee.emp_client,EmployeeSalaryDetails.TMPGUARD ,EMP_DATE_OF_BIRTH,emp_citizen,EMPPAY_DATE_JOINED,EMPPAY_DATE_RESIGNED,EMPFL_EPF8Pa,EPFDETECT, SOCSODETECT,DETECTBYND55,ISNULL(EmployeeSalaryDetails.SKBBK,1) AS SKBBK, ");
                                        sbQuery.Append(" INCOMETAXDETECT,EMPPAY_BASIC_RATE,AttendanceAllowance,SpecialAllowance,AttendanceAllowanceWorkingDays, ");
                                        sbQuery.Append(" AttendanceAllowanceFollowCalendar,PAYMODE,SalaryStructure.EmployeeNationality,SalaryStructure.WorkingHours, ");
                                        sbQuery.Append(" SalaryStructure.Name,SalaryStructure.TravelAllowance,SalaryStructure.GeneralDayRate,SalaryStructure.GeneralDayHours, ");
                                        sbQuery.Append(" SalaryStructure.WorkingDays,SalaryStructure.GeneralDayOTRate, SalaryStructure.OffDayRate, SalaryStructure.OffDayOTRate, ");
                                         sbQuery.Append(" SalaryStructure.HolidayRate, SalaryStructure.HolidayOTRate, SalaryStructure.WorkingDays, ");
                                         sbQuery.Append(" SalaryStructure.WorkingHours, SalaryStructure.SalaryBand,SalaryStructure.EICC,salarystructure.NonStructure, Employee.NewSalaryStructure,Employee.SalaryStructure1000_3h, Employee.EMP_PASSPORT_NO, Employee.EMP_NATIONAL from EmploymentDetails ");
                                        sbQuery.Append("INNER JOIN Employee ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE ");
                                        sbQuery.Append("INNER JOIN EmployeeSalaryDetails ON EmployeeSalaryDetails.EMPFL_CODE = Employee.EMP_CODE ");
                                        sbQuery.Append("INNER JOIN SalaryStructure ON SalaryStructure.SalaryID = EmploymentDetails.Salarylab ");
                                        sbQuery.Append("Left Outer Join (SELECT TOP 1 A.Employeeid,C.Name, C.KPIHours FROM Attendance A ");
                                        sbQuery.Append("inner join AttendanceDetails B ON A.id = B.attendanceID INNER JOIN ClientMaster C on B.Client = C.Code and A.Branch=C.Branch ");
                                        sbQuery.Append("WHERE A.EmployeeID = @EmployeeID and A.period = @Period ");
                                        sbQuery.Append(") Client on Employee.EMP_ID = Client.EmployeeID ");
                                        sbQuery.Append("WHERE EMP_ID=@EmployeeID");

                                        cmdEmployee.CommandText = sbQuery.ToString();
                                        cmdEmployee.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        cmdEmployee.Parameters.AddWithValue("@Period", Period);
                                        //EmployeeID = dr.GetDecimal(dr.GetOrdinal("EmployeeID"));                                        

                                        //For Testing Purpose
                                        if (EmployeeID == 28253)
                                        {
                                            EmployeeID = dr.GetDecimal(dr.GetOrdinal("EmployeeID"));
                                        }

                                        SQLDataAccess sdaEmployee = new SQLDataAccess(_configuration);
                                        SqlDataReader drEmployee = sdaEmployee.RetrieveData(cmdEmployee);
                                        bool DeductEPF8Pa = false;
                                        bool DeductEPF = false;
                                        bool DeductSOCSO = false;
                                        bool DeductSKBBK = true; // SKBBK flag - default Yes
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
                                         string sEmpNational = string.Empty;
                                         bool bNonStructure = false;
                                         bool bTmpGuard = false;

                                        if (drEmployee.Read())
                                        {
                                            EmployeeAge = Period.Year - drEmployee.GetDateTime(drEmployee.GetOrdinal("EMP_DATE_OF_BIRTH")).Year;//DateTime.Now.Year - dateOfBirth.Year;  
                                            if (Period.DayOfYear < drEmployee.GetDateTime(drEmployee.GetOrdinal("EMP_DATE_OF_BIRTH")).DayOfYear)
                                            {
                                                EmployeeAge = EmployeeAge - 1;
                                            }
                                            //EmployeeAge = ((DateTime)(new DateTime(((TimeSpan)(Period - drEmployee.GetDateTime(drEmployee.GetOrdinal("EMP_DATE_OF_BIRTH")))).Ticks))).Year;
                                            WorkingHours = drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours"));
                                            GeneralDayHours = drEmployee["GeneralDayHours"] != DBNull.Value
                                                            ? Convert.ToDecimal(drEmployee["GeneralDayHours"])
                                                            : 0m;

                                            DeductEPF = drEmployee.GetBoolean(drEmployee.GetOrdinal("EPFDETECT")); //11%
                                            DeductEPF8Pa = drEmployee.IsDBNull(drEmployee.GetOrdinal("EMPFL_EPF8Pa"))
                                                            ? false
                                                            : drEmployee.GetBoolean(drEmployee.GetOrdinal("EMPFL_EPF8Pa"));

                                            DeductSOCSO = drEmployee.GetBoolean(drEmployee.GetOrdinal("SOCSODETECT"));
                                            DeductSKBBK = drEmployee.IsDBNull(drEmployee.GetOrdinal("SKBBK")) ? true : drEmployee.GetBoolean(drEmployee.GetOrdinal("SKBBK"));
                                            DeductEPFBeyond55 = drEmployee.GetBoolean(drEmployee.GetOrdinal("DETECTBYND55"));
                                            DeductIncomeTax = drEmployee.GetBoolean(drEmployee.GetOrdinal("INCOMETAXDETECT"));//IncomeTax
                                            NoOfWorkingDays = drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                            dAttendanceAllowance = drEmployee.GetDecimal(drEmployee.GetOrdinal("AttendanceAllowance"));
                                            dSpecialAllowance = drEmployee.GetDecimal(drEmployee.GetOrdinal("SpecialAllowance"));
                                            SalaryPayMode = drEmployee.GetString(drEmployee.GetOrdinal("PAYMODE"));
                                            EmployeeNationality = drEmployee.GetString(drEmployee.GetOrdinal("EmployeeNationality"));
                                            sAttendanceAllowanceFollowCalendar = drEmployee.GetString(drEmployee.GetOrdinal("AttendanceAllowanceFollowCalendar"));
                                            dAttendanceAllowanceDays = drEmployee.GetDecimal(drEmployee.GetOrdinal("AttendanceAllowanceWorkingDays"));
                                            dEmployeeBasicRate = drEmployee.GetDouble(drEmployee.GetOrdinal("EMPPAY_BASIC_RATE"));
                                            strCitizen = drEmployee.GetInt32(drEmployee.GetOrdinal("emp_citizen"));
                                            sEmpPassport = drEmployee.GetString(drEmployee.GetOrdinal("EMP_PASSPORT_NO"));
                                            sEmpNational = drEmployee.IsDBNull(drEmployee.GetOrdinal("EMP_NATIONAL"))
                                                            ? string.Empty
                                                            : drEmployee.GetString(drEmployee.GetOrdinal("EMP_NATIONAL"));
                                            bNonStructure = drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure"));
                                            bTmpGuard = drEmployee.GetBoolean(drEmployee.GetOrdinal("TMPGUARD"));
                                            if (drEmployee["SalaryStructure1000_3h"] == DBNull.Value)
                                            {
                                                sSalaryStructure1000_3h = 0;
                                            }
                                            else
                                            {
                                                if (drEmployee.GetString(drEmployee.GetOrdinal("SalaryStructure1000_3h")) == "Y" || drEmployee.GetString(drEmployee.GetOrdinal("SalaryStructure1000_3h")) == "N")
                                                {
                                                    sSalaryStructure1000_3h = 0;
                                                }
                                                else
                                                {
                                                    sSalaryStructure1000_3h = Convert.ToInt32(drEmployee.GetString(drEmployee.GetOrdinal("SalaryStructure1000_3h")));
                                                }
                                            }
                                            if (drEmployee["NewSalaryStructure"] == DBNull.Value)
                                            {
                                                if (dEmployeeBasicRate > 0)
                                                {
                                                    dReAllowance = Math.Round((((decimal)drEmployee.GetDouble(drEmployee.GetOrdinal("EMPPAY_BASIC_RATE")) - drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"))), 2);
                                                    dReAllowanceRate = Math.Round((((decimal)drEmployee.GetDouble(drEmployee.GetOrdinal("EMPPAY_BASIC_RATE")) - drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"))), 2);
                                                }
                                            }
                                            else
                                            {
                                                if (drEmployee.GetString(drEmployee.GetOrdinal("NewSalaryStructure")) == "N")
                                                {
                                                    if (dEmployeeBasicRate > 0)
                                                    {
                                                        dReAllowance = Math.Round((((decimal)drEmployee.GetDouble(drEmployee.GetOrdinal("EMPPAY_BASIC_RATE")) - drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"))), 2);
                                                        dReAllowanceRate = Math.Round((((decimal)drEmployee.GetDouble(drEmployee.GetOrdinal("EMPPAY_BASIC_RATE")) - drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"))), 2);
                                                    }
                                                }
                                                else
                                                {
                                                    dReAllowance = 0;
                                                    dReAllowanceRate = 0;
                                                }
                                            }

                                        }

                                        //Allowance n Special Allowance
                                        sbQuery = new StringBuilder();
                                        //sbQuery.Append("SELECT AllowanceDeduction,SpecialAllowanceDeduction FROM Attendance WHERE Attendance.EmployeeID=@EmployeeID AND  Month(Attendance.Period)=@PeriodMonth AND YEAR(Attendance.Period)=@PeriodYear ");
                                        sbQuery.Append("SELECT AllowanceDeduction,SpecialAllowanceDeduction,CONVERT(DECIMAL(10, 2), ISNULL(NULLIF(KPIDeduction, '0.00'), '0.00')) ,CONVERT(DECIMAL(10, 2), ISNULL(NULLIF(KPI, '0.00'), '0.00')) FROM Attendance A INNER JOIN Employee B on A.EmployeeID=B.EMP_ID INNER JOIN EmploymentDetails C ON B.EMP_CODE=C.EMPPAY_CODE WHERE A.EmployeeID=@EmployeeID AND  Month(A.Period)=@PeriodMonth AND YEAR(A.Period)=@PeriodYear ");
                                        SqlCommand cmdAllowanceDeduct = new SqlCommand();
                                        cmdAllowanceDeduct.CommandText = sbQuery.ToString();
                                        cmdAllowanceDeduct.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        cmdAllowanceDeduct.Parameters.AddWithValue("@PeriodMonth", Period.Month);
                                        cmdAllowanceDeduct.Parameters.AddWithValue("@PeriodYear", Period.Year);
                                        SQLDataAccess sdaAllowanceDeduct = new SQLDataAccess(_configuration);
                                        SqlDataReader drAllowanceDeduct = sdaAllowanceDeduct.RetrieveData(cmdAllowanceDeduct);
                                        while (drAllowanceDeduct.Read())
                                        {
                                            if (EmployeeType == "Staff")
                                            {
                                                //Normal Allowance
                                                dAttendanceAllowance -= drAllowanceDeduct.GetDecimal(0);
                                                dAttendanceDeduction = drAllowanceDeduct.GetDecimal(0);

                                                //Special Allowance deduction
                                                dSpecialAttendanceDeduction = drAllowanceDeduct.GetDecimal(1);
                                                dSpecialAllowance = dSpecialAllowance - dSpecialAttendanceDeduction;

                                                //Bonus
                                                //dBonus = drAllowanceDeduct.GetDecimal(2);
                                                //kean hong comment
                                            }
                                            else
                                            {
                                                //Normal Allowance
                                                dAttendanceDeduction = drAllowanceDeduct.GetDecimal(0);
                                                dSpecialAttendanceDeduction = drAllowanceDeduct.GetDecimal(1);
                                                //Special Allowance deduction
                                                dSpecialAllowance = dSpecialAllowance - dSpecialAttendanceDeduction;
                                                //if (drAllowanceDeduct.GetDecimal(2) != decimal.Parse("0.00"))
                                                //{
                                                dBonus = drAllowanceDeduct.GetDecimal(3) - drAllowanceDeduct.GetDecimal(2);
                                                //}
                                                //Bonus
                                                //dBonus = drAllowanceDeduct.GetDecimal(2);
                                                //kean hong comment
                                            }
                                        }
                                        cmdAllowanceDeduct.Dispose();
                                        drAllowanceDeduct.Dispose();
                                        sdaAllowanceDeduct.Dispose();


                                        sbQuery = new StringBuilder();

                                        decimal OTGeneralDayHours = WorkingHours;
                                        if (sSalaryStructure1000_3h != 3)
                                        {
                                            if (sSalaryStructure1000_3h == 4)
                                            {
                                                OTGeneralDayHours += 1.50m;
                                            }
                                            else
                                            {
                                                OTGeneralDayHours = OTGeneralDayHours + Convert.ToDecimal(sSalaryStructure1000_3h);
                                            }
                                        }

                                        if (EmployeeNationality == "F")
                                        {
                                            //sbQuery.Append("SELECT Type,  ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(Day,TimeStart,TimeEnd + 1)>1 THEN 1 ELSE DateDiff(Day,TimeStart,TimeEnd + 1) END),0) AS NormalDays, ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(hour,TimeStart,TimeEnd)>" + WorkingHours + " THEN " + WorkingHours + " ELSE DateDiff(hour,TimeStart,TimeEnd) END),0) AS NormalHours, ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(hour,TimeStart,TimeEnd)>" + OTGeneralDayHours + " THEN DateDiff(hour,TimeStart,TimeEnd)-" + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours ");
                                            //sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID ");
                                            //sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                            //sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                            //sbQuery.Append("AND Type in (1,3,5) GROUP BY Type ");
                                            //sbQuery.Append("UNION  ");
                                            //sbQuery.Append("SELECT Type,   ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingDays + 1 >1 THEN 1 ELSE WorkingDays + 1 END),0) AS NormalDays,    ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingHours>" + WorkingHours + " THEN " + WorkingHours + " ELSE WorkingHours END),0) AS NormalHours,    ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingHours>" + OTGeneralDayHours + " THEN WorkingHours-" + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours    ");
                                            //sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID   ");
                                            //sbQuery.Append("INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE  ");
                                            //sbQuery.Append("INNER JOIN SalaryStructure ON SalaryStructure.SalaryID = EmploymentDetails.Salarylab  ");
                                            //sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth ");
                                            //sbQuery.Append("AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                            //sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                            //sbQuery.Append("AND Type in (2,4,6,7,8,9,10,11,12,13,14,15,16,17) GROUP BY Type ");

                                            sbQuery.Append("SELECT Type,  ");
                                            sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(Day,TimeStart,TimeEnd + 1)>1 THEN 1 ELSE DateDiff(Day,TimeStart,TimeEnd + 1) END),0) AS NormalDays, ");

                                            sbQuery.Append("ISNULL(SUM(CASE WHEN (CASE WHEN TimeEnd < TimeStart ");
                                            sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                            sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END)>" + WorkingHours + " ");
                                            sbQuery.Append("THEN " + WorkingHours + " ELSE ");
                                            sbQuery.Append("(CASE WHEN TimeEnd < TimeStart ");
                                            sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                            sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) END),0) AS NormalHours, ");

                                            sbQuery.Append("ISNULL(SUM(CASE WHEN (CASE WHEN TimeEnd < TimeStart ");
                                            sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                            sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END)>" + OTGeneralDayHours + " ");
                                            sbQuery.Append("THEN (CASE WHEN TimeEnd < TimeStart ");
                                            sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                            sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END)-" + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours ");

                                            sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID ");
                                            sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                            sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                            sbQuery.Append("AND Type in (1,3,5) GROUP BY Type ");

                                            sbQuery.Append("UNION  ");

                                            sbQuery.Append("SELECT Type,   ");
                                            sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingDays + 1 >1 THEN 1 ELSE WorkingDays + 1 END),0) AS NormalDays,    ");
                                            sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingHours>" + WorkingHours + " THEN " + WorkingHours + " ELSE WorkingHours END),0) AS NormalHours,    ");
                                            sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingHours>" + OTGeneralDayHours + " THEN WorkingHours-" + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours    ");

                                            sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID   ");
                                            sbQuery.Append("INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID ");
                                            sbQuery.Append("INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE  ");
                                            sbQuery.Append("INNER JOIN SalaryStructure ON SalaryStructure.SalaryID = EmploymentDetails.Salarylab  ");
                                            sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth ");
                                            sbQuery.Append("AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                            sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                            sbQuery.Append("AND Type in (2,4,6,7,8,9,10,11,12,13,14,15,16,17) GROUP BY Type ");

                                        }
                                        else
                                        {
                                            //sbQuery.Append("SELECT Type,  ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(Day,TimeStart,TimeEnd + 1)>1 THEN 1 ELSE DateDiff(Day,TimeStart,TimeEnd + 1) END),0) AS NormalDays, ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(hour,TimeStart,TimeEnd)>" + WorkingHours + " THEN " + WorkingHours + " ELSE DateDiff(hour,TimeStart,TimeEnd) END),0) AS NormalHours, ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(hour,TimeStart,TimeEnd)>" + OTGeneralDayHours + " THEN DateDiff(hour,TimeStart,TimeEnd)-" + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours ");
                                            //sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID ");
                                            //sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                            //sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                            //sbQuery.Append("AND Type in (1,3,5) GROUP BY Type ");
                                            //sbQuery.Append("UNION  ");
                                            //sbQuery.Append("SELECT Type,   ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingDays + 1 >1 THEN 1 ELSE WorkingDays + 1 END),0) AS NormalDays,    ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingHours>" + WorkingHours + " THEN " + WorkingHours + " ELSE WorkingHours END),0) AS NormalHours,    ");
                                            //sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingHours>" + OTGeneralDayHours + " THEN WorkingHours-" + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours    ");
                                            //sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID   ");
                                            //sbQuery.Append("INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE  ");
                                            //sbQuery.Append("INNER JOIN SalaryStructure ON SalaryStructure.SalaryID = EmploymentDetails.Salarylab  ");
                                            //sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth ");
                                            //sbQuery.Append("AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                            //sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                            //sbQuery.Append("AND Type in (2,4,6,7,8,9,10,11,12,13,14,15,16,17) GROUP BY Type ");

                                            sbQuery.Append("SELECT Type,  ");
                                            sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(Day,TimeStart,TimeEnd + 1)>1 THEN 1 ELSE DateDiff(Day,TimeStart,TimeEnd + 1) END),0) AS NormalDays, ");

                                            sbQuery.Append("ISNULL(SUM(CASE WHEN (CASE WHEN TimeEnd < TimeStart ");
                                            sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                            sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END)>" + WorkingHours + " ");
                                            sbQuery.Append("THEN " + WorkingHours + " ELSE ");
                                            sbQuery.Append("(CASE WHEN TimeEnd < TimeStart ");
                                            sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                            sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) END),0) AS NormalHours, ");

                                            sbQuery.Append("ISNULL(SUM(CASE WHEN (CASE WHEN TimeEnd < TimeStart ");
                                            sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                            sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END)>" + OTGeneralDayHours + " ");
                                            sbQuery.Append("THEN (CASE WHEN TimeEnd < TimeStart ");
                                            sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                            sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END)-" + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours ");

                                            sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID ");
                                            sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                            sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                            sbQuery.Append("AND Type in (1,3,5) GROUP BY Type ");

                                            sbQuery.Append("UNION  ");

                                            sbQuery.Append("SELECT Type,   ");
                                            sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingDays + 1 >1 THEN 1 ELSE WorkingDays + 1 END),0) AS NormalDays,    ");
                                            sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingHours>" + WorkingHours + " THEN " + WorkingHours + " ELSE WorkingHours END),0) AS NormalHours,    ");
                                            sbQuery.Append("ISNULL(SUM(CASE WHEN WorkingHours>" + OTGeneralDayHours + " THEN WorkingHours-" + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours    ");

                                            sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID   ");
                                            sbQuery.Append("INNER JOIN Employee ON Employee.EMP_ID = Attendance.EmployeeID INNER JOIN EmploymentDetails ON EmploymentDetails.EMPPAY_CODE = Employee.EMP_CODE  ");
                                            sbQuery.Append("INNER JOIN SalaryStructure ON SalaryStructure.SalaryID = EmploymentDetails.Salarylab  ");
                                            sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth ");
                                            sbQuery.Append("AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                            sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                            sbQuery.Append("AND Type in (2,4,6,7,8,9,10,11,12,13,14,15,16,17) GROUP BY Type ");

                                        }

                                        SqlCommand cmdAttendance = new SqlCommand();
                                        cmdAttendance.CommandText = sbQuery.ToString();
                                        cmdAttendance.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        cmdAttendance.Parameters.AddWithValue("@PeriodMonth", Period.Month);
                                        cmdAttendance.Parameters.AddWithValue("@PeriodYear", Period.Year);
                                        SQLDataAccess sdaAttendanceList = new SQLDataAccess(_configuration);
                                        SqlDataReader drAttendance = sdaAttendanceList.RetrieveData(cmdAttendance);

                                        decimal BasicSalaryDays = 0;
                                        decimal WorkingDay = 0;
                                        decimal AllowanceDays = 0;
                                        decimal BasicSalaryRate = 0;
                                        decimal BasicSalary = 0;
                                        decimal dLumpSum_NonStructure = 0;
                                        decimal dSalaryBand = 0;
                                        decimal dTransferBasicBalance = 0;
                                        decimal OverTimeSalaryHours = 0;
                                        decimal OverTimeSalaryRate = 0;
                                        decimal OverTimeSalary = 0;
                                        decimal OffdaySalaryDays = 0;
                                        decimal OffdaySalaryRate = 0;
                                        decimal OffdaySalary = 0;
                                        decimal OffdayOTSalaryHours = 0;
                                        decimal OffdayOTSalaryRate = 0;
                                        decimal OffdayOTSalary = 0;
                                        decimal HolidayDays = 0;
                                        decimal HolidaySalaryDays = 0;
                                        decimal HolidaySalaryRate = 0;
                                        decimal HolidaySalaryRateForShift2 = 0;
                                        decimal HolidaySalary = 0;
                                        decimal HolidayOTSalaryHours = 0;
                                        decimal HolidayOTSalaryRate = 0;
                                        decimal HolidayOTSalary = 0;
                                        decimal UnPaidLeaveDay = 0;
                                        decimal AnnualLeaveDay = 0;
                                        decimal AbsentDay = 0;
                                        bool MPMbool = false;
                                        bool Normalbool = false;
                                        decimal UnPaidLeaveDayActual = 0;
                                        decimal AnnualLeaveDayActual = 0;
                                        decimal AbsentDayActual = 0;

                                        decimal MedicalLeaveDay = 0;
                                        decimal MedicalLeaveDayActual = 0;
                                        decimal MaternityLeaveDay = 0;
                                        decimal MaternityLeaveDayActual = 0;
                                        decimal PaternityLeaveDay = 0;
                                        decimal PaternityLeaveDayActual = 0;
                                        decimal HospitalizationLeaveDay = 0;
                                        decimal HospitalizationLeaveDayActual = 0;
                                        decimal SocsoDay = 0;
                                        decimal SocsoDayActual = 0;
                                        decimal HolidayDayActual = 0;
                                        decimal OnSEMIDeduction = 0;
                                        decimal AdditionalOT = 0;
                                        decimal dTotalKPIAmount = 0;

                                        sbQuery = new StringBuilder();
                                        //sbQuery.Append("SELECT TYPE, SUM(KPIHOURS) AS TOTALKPIHOURS FROM ( ");
                                        //sbQuery.Append("SELECT TYPE,CASE WHEN DateDiff(hour,TimeStart,TimeEnd)>8 THEN 8 ELSE DateDiff(hour,TimeStart,TimeEnd) END as NormalHours, ");
                                        //sbQuery.Append("CASE WHEN DateDiff(hour,TimeStart,TimeEnd)>8 THEN DateDiff(hour,TimeStart,TimeEnd)-8 ELSE 0 END as OTHours, ");
                                        //sbQuery.Append("CASE WHEN (DateDiff(hour,TimeStart,TimeEnd)>8 AND DateDiff(hour,TimeStart,TimeEnd)<=10) THEN DateDiff(hour,TimeStart,TimeEnd) - 8 -2 ");
                                        //sbQuery.Append("WHEN (DateDiff(hour,TimeStart,TimeEnd)>10 AND DateDiff(hour,TimeStart,TimeEnd)<=12) THEN DateDiff(hour,TimeStart,TimeEnd) - 8 - 2 ");
                                        //sbQuery.Append("WHEN (DateDiff(hour,TimeStart,TimeEnd)>12) THEN 2 ");
                                        //sbQuery.Append("ELSE 0 END AS KPIHOURS ");
                                        //sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID ");
                                        //sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear  ");
                                        //sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                        //sbQuery.Append("AND Type in (1,3,5)) AS KPI ");
                                        //sbQuery.Append("GROUP BY TYPE ");

                                        sbQuery.Append("SELECT TYPE, SUM(KPIHOURS) AS TOTALKPIHOURS FROM ( ");

                                        sbQuery.Append("SELECT TYPE, ");
                                        sbQuery.Append("CASE WHEN (CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) > 8 ");
                                        sbQuery.Append("THEN 8 ELSE ");
                                        sbQuery.Append("(CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) END as NormalHours, ");

                                        sbQuery.Append("CASE WHEN (CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) > 8 ");
                                        sbQuery.Append("THEN (CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) - 8 ELSE 0 END as OTHours, ");

                                        sbQuery.Append("CASE ");
                                        sbQuery.Append("WHEN ((CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) > 8 ");
                                        sbQuery.Append("AND (CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) <= 10) ");
                                        sbQuery.Append("THEN (CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) - 8 - 2 ");

                                        sbQuery.Append("WHEN ((CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) > 10 ");
                                        sbQuery.Append("AND (CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) <= 12) ");
                                        sbQuery.Append("THEN (CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) - 8 - 2 ");

                                        sbQuery.Append("WHEN ((CASE WHEN TimeEnd < TimeStart ");
                                        sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                        sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) > 12) THEN 2 ");

                                        sbQuery.Append("ELSE 0 END AS KPIHOURS ");

                                        sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID ");
                                        sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear  ");
                                        sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");
                                        sbQuery.Append("AND Type in (1,3,5)) AS KPI ");

                                        sbQuery.Append("GROUP BY TYPE ");


                                        SqlCommand cmdKPIHours = new SqlCommand();
                                        cmdKPIHours.CommandText = sbQuery.ToString();
                                        cmdKPIHours.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        cmdKPIHours.Parameters.AddWithValue("@PeriodMonth", Period.Month);
                                        cmdKPIHours.Parameters.AddWithValue("@PeriodYear", Period.Year);
                                        SQLDataAccess sdaKPIHours = new SQLDataAccess(_configuration);
                                        SqlDataReader drKPIHours = sdaKPIHours.RetrieveData(cmdKPIHours);

                                        int iTotalKPIHours = 0;
                                        int iTotalNormalWorkingKPIHours = 0;
                                        int iTotalOffdayWorkingKPIHours = 0;
                                        int iTotalHolidayWorkingKPIHours = 0;

                                        if (sSalaryStructure1000_3h == 3)
                                        {
                                            while (drKPIHours.Read())
                                            {
                                                int type = drKPIHours.GetInt32(drKPIHours.GetOrdinal("Type"));
                                                int totalKPIHours = drKPIHours.IsDBNull(drKPIHours.GetOrdinal("TOTALKPIHOURS")) ? 0 : drKPIHours.GetInt32(drKPIHours.GetOrdinal("TOTALKPIHOURS"));

                                                switch (type)
                                                {
                                                    case 1:
                                                        iTotalNormalWorkingKPIHours = totalKPIHours;
                                                        break;
                                                    case 3:
                                                        iTotalOffdayWorkingKPIHours = totalKPIHours;
                                                        break;
                                                    case 5:
                                                        iTotalHolidayWorkingKPIHours = totalKPIHours;
                                                        break;
                                                }
                                                //switch (drKPIHours.GetInt32(drKPIHours.GetOrdinal("Type")))
                                                //{
                                                //    case 1:
                                                //        iTotalNormalWorkingKPIHours = drKPIHours.GetInt32(drKPIHours.GetOrdinal("TOTALKPIHOURS"));
                                                //        break;
                                                //    case 3:
                                                //        iTotalOffdayWorkingKPIHours = drKPIHours.GetInt32(drKPIHours.GetOrdinal("TOTALKPIHOURS"));
                                                //        break;
                                                //    case 5:
                                                //        iTotalHolidayWorkingKPIHours = drKPIHours.GetInt32(drKPIHours.GetOrdinal("TOTALKPIHOURS"));
                                                //        break;

                                                //}
                                            }
                                        }
                                        cmdKPIHours.Dispose();
                                        drKPIHours.Dispose();
                                        sdaKPIHours.Dispose();

                                        while (drAttendance.Read())
                                        {
                                            if (EmployeeType == "Staff")
                                            {
                                                if (drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) > 0)
                                                {
                                                    switch (drAttendance.GetInt32(drAttendance.GetOrdinal("Type")))
                                                    {
                                                        case 1: //General Working
                                                            Normalbool = true;
                                                            if (drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) >= 26)
                                                            {
                                                                BasicSalary = (decimal)drEmployee.GetDouble(drEmployee.GetOrdinal("EMPPAY_BASIC_RATE"));
                                                                BasicSalaryDays = (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                AllowanceDays = (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                            }
                                                            else
                                                            {
                                                                if (Period.Month == 2 && drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) >= 24) //NormalHours --> NormalDays
                                                                {
                                                                    BasicSalary = (decimal)drEmployee.GetDouble(drEmployee.GetOrdinal("EMPPAY_BASIC_RATE"));
                                                                    BasicSalaryDays = (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                    AllowanceDays = (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                }
                                                                else
                                                                {
                                                                    BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")));
                                                                    BasicSalaryDays += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    AllowanceDays += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    Normalbool = false;
                                                                }
                                                            }
                                                            BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                            OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                            OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                            break;

                                                        case 3: // off day working
                                                            OffdaySalary = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"))) + (decimal)0.00000001, 2);
                                                            OffdaySalaryDays = Math.Round(OffdaySalary / Math.Round((drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")))), 2), 0);
                                                            OffdaySalaryRate = Math.Round((drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")))), 2);
                                                            OffdayOTSalary = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                            OffdayOTSalaryHours = drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                            OffdayOTSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                            break;
                                                        case 4://Holiday
                                                            HolidayDays = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")));
                                                            HolidayDayActual = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            MPMbool = true;
                                                            break;
                                                        case 5: //Holiday Working
                                                            //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                            //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            //AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            HolidaySalary = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"))) + (decimal)0.00000001, 2);
                                                            HolidaySalaryDays = Math.Round(HolidaySalary / Math.Round((drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")))), 2), 0);
                                                            HolidaySalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"))), 2);
                                                            HolidayOTSalary = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2), 2, MidpointRounding.AwayFromZero);

                                                            HolidayOTSalaryHours = drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                            HolidayOTSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                            MPMbool = true;
                                                            break;
                                                        case 6: //UnPaid Leave
                                                            UnPaidLeaveDay = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")));
                                                            UnPaidLeaveDayActual = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            MPMbool = true;
                                                            break;

                                                        case 7: //Absent
                                                            AbsentDay = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")));
                                                            AbsentDayActual = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            MPMbool = true;
                                                            break;

                                                        case 8: //Annual Leave
                                                            AnnualLeaveDay = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")));
                                                            AnnualLeaveDayActual = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            MPMbool = true;
                                                            break;
                                                        case 9: //Medical leave
                                                            MedicalLeaveDay = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")));
                                                            MedicalLeaveDayActual = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            MPMbool = true;
                                                            break;
                                                        case 10: // Maternity Leave
                                                            MaternityLeaveDay = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")));
                                                            MaternityLeaveDayActual = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            MPMbool = true;
                                                            break;
                                                        case 11: // Paternity leave
                                                            PaternityLeaveDay = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")));
                                                            PaternityLeaveDayActual = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            MPMbool = true;
                                                            break;
                                                        case 12: // Hospitalization leave
                                                            HospitalizationLeaveDay = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")));
                                                            HospitalizationLeaveDayActual = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            MPMbool = true;
                                                            break;
                                                        case 13: // Paid By Socso
                                                            SocsoDay = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")));
                                                            SocsoDayActual = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            MPMbool = true;
                                                            break;
                                                    }
                                                }
                                            }

                                            else //GUARD
                                            {
                                                Boolean bIncompleteMonth = false;
                                                int iKPIHours = 0;
                                                decimal dDailyOTHours = 0;
                                                int iDailyKPIHours = 0;
                                                //int iTotalKPIHours = 0;


                                                if (drEmployee["EMPPAY_DATE_RESIGNED"] != DBNull.Value)
                                                {
                                                    DateTime dtResignedDate = drEmployee.GetDateTime(drEmployee.GetOrdinal("EMPPAY_DATE_RESIGNED"));
                                                    if ((dtResignedDate.Month == Period.Month) && (dtResignedDate.Year == Period.Year) && (dtResignedDate.Day != Period.Day))
                                                    {
                                                        bIncompleteMonth = true;
                                                    }
                                                }

                                                if (drEmployee["EMPPAY_DATE_JOINED"] != DBNull.Value)
                                                {
                                                    DateTime dtJoinedDate = drEmployee.GetDateTime(drEmployee.GetOrdinal("EMPPAY_DATE_JOINED"));
                                                    if ((dtJoinedDate.Month == Period.Month) && (dtJoinedDate.Year == Period.Year) && dtJoinedDate.Day != 1)
                                                    {
                                                        bIncompleteMonth = true;
                                                    }
                                                }

                                                sbQuery = new StringBuilder();
                                                //sbQuery.Append("SELECT ClientMaster.SpecialOTHours,ClientMaster.Code,ClientMaster.name,Type,AttendanceDate,  ");
                                                //sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(Day,TimeStart,TimeEnd + 1)>1 THEN 1 ELSE DateDiff(Day,TimeStart,TimeEnd + 1) END),0) AS NormalDays, ");
                                                //sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(hour,TimeStart,TimeEnd)>" + GeneralDayHours + " THEN " + GeneralDayHours + " ELSE DateDiff(hour,TimeStart,TimeEnd) END),0) AS NormalHours, ");
                                                //sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(hour,TimeStart,TimeEnd)>" + OTGeneralDayHours + " THEN DateDiff(hour,TimeStart,TimeEnd)-" + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours ");
                                                //sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID ");
                                                //sbQuery.Append("INNER JOIN ClientMaster on Attendancedetails.client = ClientMaster.code and Attendance.branch=Clientmaster.branch  ");
                                                //sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                                //sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");

                                                sbQuery.Append("SELECT ClientMaster.SpecialOTHours,ClientMaster.Code,ClientMaster.name,Type,AttendanceDate,  ");

                                                sbQuery.Append("ISNULL(SUM(CASE WHEN DateDiff(Day,TimeStart,TimeEnd + 1)>1 THEN 1 ELSE DateDiff(Day,TimeStart,TimeEnd + 1) END),0) AS NormalDays, ");

                                                sbQuery.Append("ISNULL(SUM(CASE WHEN (CASE WHEN TimeEnd < TimeStart ");
                                                sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                                sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) > " + GeneralDayHours + " ");
                                                sbQuery.Append("THEN " + GeneralDayHours + " ELSE ");
                                                sbQuery.Append("(CASE WHEN TimeEnd < TimeStart ");
                                                sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                                sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) END),0) AS NormalHours, ");

                                                sbQuery.Append("ISNULL(SUM(CASE WHEN (CASE WHEN TimeEnd < TimeStart ");
                                                sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                                sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) > " + OTGeneralDayHours + " ");
                                                sbQuery.Append("THEN (CASE WHEN TimeEnd < TimeStart ");
                                                sbQuery.Append("THEN DATEDIFF(hour, TimeStart, DATEADD(day,1,TimeEnd)) ");
                                                sbQuery.Append("ELSE DATEDIFF(hour, TimeStart, TimeEnd) END) - " + OTGeneralDayHours + " ELSE 0 END),0) AS OTHours ");

                                                sbQuery.Append("FROM AttendanceDetails INNER JOIN Attendance ON Attendance.ID = AttendanceDetails.AttendanceID ");
                                                sbQuery.Append("INNER JOIN ClientMaster on Attendancedetails.client = ClientMaster.code and Attendance.branch=Clientmaster.branch  ");
                                                sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear ");
                                                sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID ");

                                                if (CompanyCode.ToString().Trim() == "EASTWEST")
                                                {
                                                    sbQuery.Append("AND Type in ('1') ");
                                                }
                                                else
                                                {
                                                    sbQuery.Append("AND Type in ('1','3','5') ");
                                                }
                                                sbQuery.Append("GROUP BY ClientMaster.SpecialOTHours,ClientMaster.Code,ClientMaster.name,Type,AttendanceDate ");

                                                SqlCommand cmdAttendanceDetails = new SqlCommand();
                                                cmdAttendanceDetails.CommandText = sbQuery.ToString();
                                                cmdAttendanceDetails.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                                cmdAttendanceDetails.Parameters.AddWithValue("@PeriodMonth", Period.Month);
                                                cmdAttendanceDetails.Parameters.AddWithValue("@PeriodYear", Period.Year);

                                                SQLDataAccess sdaAttendanceDetailsList = new SQLDataAccess(_configuration);
                                                SqlDataReader drAttendanceDetails = sdaAttendanceDetailsList.RetrieveData(cmdAttendanceDetails);
                                                decimal count1WeekTotal = 0;
                                                decimal count2WeekTotal = 0;
                                                decimal count3WeekTotal = 0;
                                                decimal count4WeekTotal = 0;
                                                decimal count5WeekTotal = 0;
                                                int SpecialOTHours = 0;

                                                Dictionary<int, decimal> weeklyHours = new Dictionary<int, decimal>();

                                                while (drAttendanceDetails.Read())
                                                {
                                                    if (drAttendanceDetails["SpecialOTHours"] != DBNull.Value)
                                                    {
                                                        int specialOT = drAttendanceDetails.GetInt32(drAttendanceDetails.GetOrdinal("SpecialOTHours"));
                                                        DateTime attendanceDate = drAttendanceDetails.GetDateTime(drAttendanceDetails.GetOrdinal("AttendanceDate"));

                                                        // Determine the Monday of the week
                                                        DateTime weekStart = attendanceDate.AddDays(-(int)(attendanceDate.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)attendanceDate.DayOfWeek - 1)));

                                                        // Create a unique key per week, like 20250902 (yyyyMMdd)
                                                        int weekKey = int.Parse(weekStart.ToString("yyyyMMdd"));

                                                        // Get NormalHours for the day
                                                        decimal hours = drAttendanceDetails.GetDecimal(drAttendanceDetails.GetOrdinal("NormalHours"));

                                                        // Accumulate hours per week
                                                        if (!weeklyHours.ContainsKey(weekKey))
                                                            weeklyHours[weekKey] = 0;

                                                        weeklyHours[weekKey] += hours;

                                                        // Save SpecialOTHours (assuming it's the same per record)
                                                        SpecialOTHours = specialOT;
                                                    }
                                                }

                                                // Calculate AdditionalOT
                                                foreach (var week in weeklyHours)
                                                {
                                                    if (week.Value >= 45)
                                                    {
                                                        AdditionalOT += SpecialOTHours;
                                                    }
                                                }
                                                drAttendanceDetails.Close();
                                                drAttendanceDetails.Dispose();
                                                cmdAttendanceDetails.Dispose();
                                                sdaAttendanceDetailsList.Dispose();

                                                if (drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) > 0)
                                                {

                                                    if (sSalaryStructure1000_3h == 3)
                                                    {
                                                        iKPIHours = 2;
                                                    }
                                                    //if (drEmployee["KPIHours"] != DBNull.Value)
                                                    //{
                                                    //    iKPIHours = drEmployee.GetInt32(drEmployee.GetOrdinal("KPIHours"));
                                                    //}
                                                    switch (drAttendance.GetInt32(drAttendance.GetOrdinal("Type")))
                                                    {
                                                        case 1://General Working
                                                            if (EmployeeID == 24524)
                                                            {
                                                                //decimal NormalHour = drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"));
                                                                //decimal DayRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                //int dayhours = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"));
                                                                int DAY = 21;
                                                            }
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            {
                                                                BasicSalary += Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"))), 2);
                                                                BasicSalaryDays += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                AllowanceDays += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                            }
                                                            else
                                                            {
                                                                if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == true)
                                                                {
                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    //OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                    //OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 176)
                                                                    {
                                                                        //amend on 6 Apr 2022 to follow the normal working day instead of fix 22 days
                                                                        //amend on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC
                                                                        bIncompleteMonth = false;
                                                                        //BasicSalary += 22 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        //BasicSalaryDays += 22;
                                                                        BasicSalary += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryDays += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));

                                                                        //AllowanceDays += 22;

                                                                        //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("Normaldays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        //BasicSalaryDays += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("Normaldays"));
                                                                        AllowanceDays += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("Normaldays"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                        OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                        OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    }
                                                                    else
                                                                    {

                                                                        if (Period.Month == 2)
                                                                        {

                                                                            if (bIncompleteMonth == true)
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                                OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                                OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                            }
                                                                            else
                                                                            {
                                                                                //amend on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC
                                                                                //BasicSalary += 22 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                //BasicSalaryDays += 22;
                                                                                BasicSalary += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryDays += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                                //AllowanceDays += 22;

                                                                                //BasicSalary += drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                                OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                                OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);

                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (bIncompleteMonth == true)
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                                OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                                OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                            }
                                                                            else
                                                                            {
                                                                                //amend on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC
                                                                                //BasicSalary += 22 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                //BasicSalaryDays += 22;
                                                                                BasicSalary += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryDays += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                                //AllowanceDays += 22;
                                                                                //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("Normaldays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                //BasicSalaryDays += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("Normaldays"));
                                                                                AllowanceDays += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("Normaldays"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                                OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                                OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                            }
                                                                        }


                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    //1 MAR 2020 RBA
                                                                    if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("RBA"))
                                                                    {
                                                                        int MonthInDay = DateTime.DaysInMonth(Period.Year, Period.Month);
                                                                        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 312)
                                                                        {
                                                                            BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                            BasicSalaryDays = drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                            AllowanceDays = drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                            BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            //OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                            //OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                            //OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                        }

                                                                    }
                                                                    //if (Branch.Substring(0,2) == "SW" && Period.Month == 2)

                                                                    if (Period.Month == 2)
                                                                    {
                                                                        if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                        {
                                                                            BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                            BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            WorkingDay = WorkingDay + BasicSalaryDays;
                                                                        }
                                                                        else if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                        {
                                                                            //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                            //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));


                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {
                                                                                    BasicSalaryDays += drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25;
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (BasicSalaryRate / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += BasicSalaryRate * drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));


                                                                                }
                                                                                else
                                                                                {
                                                                                    BasicSalaryDays += drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                                                    BasicSalary += BasicSalaryRate * drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                //BasicSalary = 24 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")); ;
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                            }

                                                                            AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));

                                                                            OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) + AdditionalOT;
                                                                            OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                            //amend on Additional OT - previous additional OT calculation need to change and follow the request by Jaya on 27 Sept 2023 (6th day work is 5 normal working hours + 5 hours OT) 
                                                                            //East West follow old OT Calculation
                                                                            if (CompanyCode.ToString().Trim() == "EASTWEST")
                                                                            {
                                                                                if (OverTimeSalaryRate != 0)
                                                                                {
                                                                                    OverTimeSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"))) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                                    OverTimeSalary = OverTimeSalary + ((OverTimeSalaryRate - (BasicSalaryRate / 8)) * AdditionalOT);
                                                                                }
                                                                            }
                                                                            else //if (CompanyCode.ToString().Trim() == "FWG" && Branch == "PF059")
                                                                            {
                                                                                OverTimeSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) + AdditionalOT) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                            }

                                                                        }
                                                                        else
                                                                        {

                                                                            BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {
                                                                                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25;
                                                                                    BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (BasicSalaryRate / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                    if (BasicSalaryDays == 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                                                    BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (BasicSalaryRate / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                    if (BasicSalaryDays == 24)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                            }


                                                                            OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) + AdditionalOT;
                                                                            OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                            //amend on Additional OT - previous additional OT calculation need to change and follow the request by Jaya on 27 Sept 2023 (6th day work is 5 normal working hours + 5 hours OT) 

                                                                            if (CompanyCode.ToString().Trim() == "EASTWEST")
                                                                            {
                                                                                if (OverTimeSalaryRate != 0)
                                                                                {
                                                                                    OverTimeSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"))) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                                    OverTimeSalary = OverTimeSalary + ((OverTimeSalaryRate - (BasicSalaryRate / 8)) * AdditionalOT);
                                                                                }
                                                                            }
                                                                            else //if (CompanyCode.ToString().Trim() == "FWG" && Branch == "PF059")
                                                                            {
                                                                                OverTimeSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) + AdditionalOT) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                            }
                                                                        }
                                                                        //BasicSalary += 22 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        //BasicSalaryDays += 22;
                                                                        //AllowanceDays += 22;
                                                                        //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        //OverTimeSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                        //OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                        //OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    }
                                                                    else
                                                                    {
                                                                        if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                        {
                                                                            BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                            BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            WorkingDay = WorkingDay + BasicSalaryDays;
                                                                        }
                                                                        else
                                                                        {
                                                                            BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                            BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));


                                                                            OverTimeSalaryHours += drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) + AdditionalOT;
                                                                            //OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);

                                                                            decimal generalDayOTRate = drEmployee.IsDBNull(drEmployee.GetOrdinal("GeneralDayOTRate"))
                                                                                ? 0
                                                                                : Convert.ToDecimal(drEmployee["GeneralDayOTRate"]);

                                                                            decimal generalDayRate = drEmployee.IsDBNull(drEmployee.GetOrdinal("GeneralDayRate"))
                                                                                ? 0
                                                                                : Convert.ToDecimal(drEmployee["GeneralDayRate"]);

                                                                            decimal generalDayHours = drEmployee.IsDBNull(drEmployee.GetOrdinal("GeneralDayHours"))
                                                                                ? 0
                                                                                : Convert.ToDecimal(drEmployee["GeneralDayHours"]);

                                                                            //OverTimeSalaryRate = generalDayHours == 0
                                                                            //    ? 0
                                                                            //    : Math.Round(generalDayOTRate * (generalDayRate / generalDayHours), 2);

                                                                            OverTimeSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);



                                                                            //amend on Additional OT - previous additional OT calculation need to change and follow the request by Jaya on 27 Sept 2023 (6th day work is 5 normal working hours + 5 hours OT) 
                                                                            //
                                                                            if (CompanyCode.ToString().Trim() == "EASTWEST")
                                                                            {
                                                                                if (OverTimeSalaryRate != 0)
                                                                                {
                                                                                    //OverTimeSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"))) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                                    decimal otHours = drAttendance.IsDBNull(drAttendance.GetOrdinal("OTHours"))
                                                                                        ? 0
                                                                                        : Convert.ToDecimal(drAttendance["OTHours"]);                                                                                   

                                                                                    decimal otRate = generalDayHours == 0
                                                                                        ? 0
                                                                                        : Math.Round(generalDayOTRate * (generalDayRate / generalDayHours), 2);

                                                                                    OverTimeSalary += otHours * otRate;

                                                                                    OverTimeSalary = OverTimeSalary + ((OverTimeSalaryRate - (BasicSalaryRate / 8)) * AdditionalOT);
                                                                                }
                                                                            }
                                                                            else //if (CompanyCode.ToString().Trim() == "FWG" && Branch == "PF059")
                                                                            {
                                                                                OverTimeSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) + AdditionalOT) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                            }
                                                                        }

                                                                    }
                                                                }
                                                            }

                                                            if (iKPIHours > 0)
                                                            {
                                                                if (drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) > 0)
                                                                {
                                                                    //iTotalKPIHours = iKPIHours * (int)Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("Normaldays")));
                                                                    iTotalKPIHours = iTotalKPIHours + iTotalNormalWorkingKPIHours;
                                                                    dTotalKPIAmount = dTotalKPIAmount + (iTotalNormalWorkingKPIHours * OverTimeSalaryRate);
                                                                    OverTimeSalaryHours = OverTimeSalaryHours - iTotalNormalWorkingKPIHours;
                                                                    OverTimeSalary = OverTimeSalary - ((iTotalNormalWorkingKPIHours * OverTimeSalaryRate));
                                                                }
                                                            }
                                                            OverTimeSalary = Math.Round(OverTimeSalary, 2, MidpointRounding.AwayFromZero);
                                                            break;
                                                        case 2:// off day
                                                            if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                            {
                                                                WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            }
                                                            break;

                                                        case 3:// off day working
                                                            //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                            //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == true)
                                                            {
                                                                OffdaySalaryRate = Math.Round((drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26)), 2);
                                                                OffdaySalary = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours"))) + (decimal)0.00000001, 2);

                                                                OffdayOTSalary = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                OffdayOTSalaryHours = drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                OffdayOTSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);

                                                            }
                                                            else if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                            {
                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            }

                                                            else
                                                            {
                                                                if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                                {
                                                                    OffdaySalaryDays = 0;
                                                                }
                                                                else
                                                                {
                                                                    OffdaySalaryDays = Math.Round(OffdaySalary / Math.Round((drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26)), 2), 0);
                                                                }

                                                                OffdaySalary = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours"))) + (decimal)0.00000001, 2);
                                                                OffdaySalaryDays = Math.Round(OffdaySalary / Math.Round((drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")))), 2), 0);
                                                                OffdaySalaryRate = Math.Round((drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")))), 2);
                                                                //OffdayOTSalary = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                OffdayOTSalary =
                                                                    Convert.ToDecimal(drAttendance["OTHours"]) *
                                                                    Math.Round(
                                                                        Convert.ToDecimal(drEmployee["OffDayOTRate"]) *
                                                                        (Convert.ToDecimal(drEmployee["GeneralDayRate"]) /
                                                                         Convert.ToDecimal(drEmployee["GeneralDayHours"])),
                                                                        2
                                                                    );

                                                                OffdayOTSalaryHours = drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                //OffdayOTSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("OffDayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                OffdayOTSalaryRate = Math.Round(
                                                                            Convert.ToDecimal(drEmployee["OffDayOTRate"]) *
                                                                            (Convert.ToDecimal(drEmployee["GeneralDayRate"]) /
                                                                             Convert.ToDecimal(drEmployee["GeneralDayHours"])),
                                                                            2
                                                                        );

                                                            }

                                                            if (iKPIHours > 0)
                                                            {
                                                                decimal dOffdayOTKPIAmount = 0;
                                                                if (drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) > 0)
                                                                {
                                                                    //iTotalKPIHours = iKPIHours * (int)Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("Normaldays")));
                                                                    //dTotalKPIAmount = dTotalKPIAmount + (iTotalKPIHours * OffdayOTSalaryRate);
                                                                    //dOffdayOTKPIAmount = iTotalKPIHours * OffdayOTSalaryRate;
                                                                    //OffdayOTSalaryHours = OffdayOTSalaryHours - iTotalKPIHours;
                                                                    //OffdayOTSalary = OffdayOTSalary - dOffdayOTKPIAmount;


                                                                    iTotalKPIHours = iTotalKPIHours + iTotalOffdayWorkingKPIHours;
                                                                    dTotalKPIAmount = dTotalKPIAmount + (iTotalOffdayWorkingKPIHours * OffdayOTSalaryRate);
                                                                    OffdayOTSalaryHours = OffdayOTSalaryHours - iTotalOffdayWorkingKPIHours;
                                                                    OffdayOTSalary = OffdayOTSalary - ((iTotalOffdayWorkingKPIHours * OffdayOTSalaryRate));
                                                                }
                                                            }
                                                            OffdayOTSalary = Math.Round(OffdayOTSalary, 2, MidpointRounding.AwayFromZero);
                                                            break;
                                                        case 4://Holiday
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            { }
                                                            else
                                                            {
                                                                if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                                {
                                                                    if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    }
                                                                    else if (Period.Month == 2)
                                                                    {
                                                                        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                        {
                                                                            //BasicSalary = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                            BasicSalaryDays = 24;
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                BasicSalary += (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary = 24 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")); ;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 24)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //amend below on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC  
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    //BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    if (bIncompleteMonth == true)
                                                                    {
                                                                        BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                        BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    }
                                                                }
                                                            }

                                                            break;
                                                        case 5://Holiday Working
                                                            if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == true)
                                                            {
                                                                //HolidaySalary = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))) + (decimal)0.00000001, 2);
                                                                //HolidaySalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22), 2);
                                                                //HolidaySalaryDays = Math.Round(HolidaySalary / Math.Round((drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22)), 2), 0);
                                                                HolidaySalaryDays = drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                HolidaySalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate"));
                                                                HolidaySalary = Math.Round(HolidaySalaryDays * HolidaySalaryRate, 2);

                                                                HolidayOTSalary = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                HolidayOTSalaryHours = drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                HolidayOTSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 26 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);

                                                                if (bIncompleteMonth == true)
                                                                {
                                                                    BasicSalary += (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * HolidaySalaryDays);
                                                                    BasicSalaryDays = BasicSalaryDays + HolidaySalaryDays;
                                                                    AllowanceDays = AllowanceDays + HolidaySalaryDays;
                                                                    BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));

                                                                }

                                                                //BasicSalary += (HolidaySalaryDays * HolidaySalaryRate);
                                                                //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                //amend below on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC  
                                                                //BasicSalary += (HolidaySalaryDays * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"))));
                                                                //BasicSalaryDays += HolidaySalaryDays;        
                                                            }

                                                            else if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                            {
                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            }

                                                            else
                                                            {
                                                                //if (Branch.Substring(0, 2) == "SW" && Period.Month == 2)
                                                                //{
                                                                //    if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                //    {
                                                                //        BasicSalary = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                //        BasicSalaryDays = 24;
                                                                //    }
                                                                //    else
                                                                //    {
                                                                //        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                //        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                //    }

                                                                //}
                                                                //else
                                                                //{
                                                                //    BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                //    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                //}
                                                                if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                                { }
                                                                else
                                                                {
                                                                    if (CompanyCode.ToString().Trim() == "Swack" && Period.Month == 2 && EmployeeNationality == "L")
                                                                    {
                                                                        //BasicSalaryDays += 24;
                                                                        //BasicSalary += (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24 / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        if (BasicSalaryDays == 24)
                                                                        {
                                                                            BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));

                                                                    }
                                                                }
                                                                HolidaySalary = Math.Round((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours"))) + (decimal)0.00000001, 2);
                                                                if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                                {
                                                                    HolidaySalaryDays = 0;
                                                                }
                                                                else
                                                                {
                                                                    HolidaySalaryDays = Math.Round(HolidaySalary / Math.Round((drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")))), 2), 0);
                                                                }
                                                                // AllowanceDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));

                                                                HolidaySalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"))), 2);
                                                                //HolidayOTSalary = (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) * Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                HolidayOTSalary =
                                                                    Convert.ToDecimal(drAttendance["OTHours"]) *
                                                                    Math.Round(
                                                                        Convert.ToDecimal(drEmployee["HolidayOTRate"]) *
                                                                        (Convert.ToDecimal(drEmployee["GeneralDayRate"]) /
                                                                         Convert.ToDecimal(drEmployee["GeneralDayHours"])),
                                                                        2
                                                                    );

                                                                HolidayOTSalaryHours = drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours"));
                                                                //HolidayOTSalaryRate = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayOTRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                HolidayOTSalaryRate = Math.Round(
                                                                        Convert.ToDecimal(drEmployee["HolidayOTRate"]) *
                                                                        (Convert.ToDecimal(drEmployee["GeneralDayRate"]) /
                                                                         Convert.ToDecimal(drEmployee["GeneralDayHours"])),
                                                                        2
                                                                    );

                                                            }

                                                            if (iKPIHours > 0)
                                                            {
                                                                decimal dHolidayOffdayOTKPIAmount = 0;
                                                                if (drAttendance.GetDecimal(drAttendance.GetOrdinal("OTHours")) > 0)
                                                                {
                                                                    iTotalKPIHours = iTotalKPIHours + iTotalHolidayWorkingKPIHours;
                                                                    dTotalKPIAmount = dTotalKPIAmount + (iTotalHolidayWorkingKPIHours * HolidayOTSalaryRate);
                                                                    HolidayOTSalaryHours = HolidayOTSalaryHours - iTotalHolidayWorkingKPIHours;
                                                                    HolidayOTSalary = HolidayOTSalary - ((iTotalHolidayWorkingKPIHours * HolidayOTSalaryRate));
                                                                }
                                                            }
                                                            HolidayOTSalary = Math.Round(HolidayOTSalary, 2, MidpointRounding.AwayFromZero);
                                                            break;
                                                        case 6://unpaid Leave

                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            {
                                                                OnSEMIDeduction = OnSEMIDeduction + ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")));
                                                            }

                                                            if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == true && bIncompleteMonth == false)
                                                            {

                                                                BasicSalaryDays = BasicSalaryDays - drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                BasicSalary = BasicSalary - (drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")));
                                                            }

                                                            //else
                                                            //{
                                                            //    if (Period.Month == 2)
                                                            //    {
                                                            //        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                            //        {
                                                            //            BasicSalaryDays = BasicSalaryDays - (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            //            if (Branch.Substring(0, 2) == "SW" && EmployeeNationality == "L")
                                                            //            {
                                                            //                BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                            //                BasicSalary = BasicSalaryDays * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24);
                                                            //            }
                                                            //            else
                                                            //            {
                                                            //                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            //                BasicSalary = BasicSalaryDays * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            //            }
                                                            //        }
                                                            //        else
                                                            //        {
                                                            //            if (DateTime.IsLeapYear(Period.Year) == true)
                                                            //            {
                                                            //                BasicSalaryDays = BasicSalaryDays - (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            //                if (Branch.Substring(0, 2) == "SW" && EmployeeNationality == "L")
                                                            //                {
                                                            //                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25;
                                                            //                    BasicSalary = BasicSalary - ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25));
                                                            //                }
                                                            //                else
                                                            //                {
                                                            //                    BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            //                    BasicSalary = BasicSalaryDays * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            //                }
                                                            //            }
                                                            //            else
                                                            //            {
                                                            //                BasicSalaryDays = BasicSalaryDays - (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            //                if (Branch.Substring(0, 2) == "SW" && EmployeeNationality == "L")
                                                            //                {
                                                            //                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                            //                    //BasicSalary = BasicSalaryDays * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24);
                                                            //                    BasicSalary = BasicSalary - ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24));
                                                            //                }
                                                            //                else
                                                            //                {
                                                            //                    BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            //                    BasicSalary = BasicSalaryDays * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            //                }
                                                            //            }
                                                            //        }


                                                            //    }
                                                            //}
                                                            break;
                                                        case 7://absent Leave
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            {
                                                                //OnSEMIDeduction += OnSEMIDeduction + drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                OnSEMIDeduction = OnSEMIDeduction + ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")));
                                                            }

                                                            if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == true && bIncompleteMonth == false)
                                                            {

                                                                BasicSalaryDays = BasicSalaryDays - drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                BasicSalary = BasicSalary - (drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")));
                                                            }

                                                            //else
                                                            //{
                                                            //    if (Period.Month == 2)
                                                            //    {
                                                            //        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                            //        {
                                                            //            BasicSalaryDays = BasicSalaryDays - (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            //            if (Branch.Substring(0, 2) == "SW" && EmployeeNationality == "L")
                                                            //            {
                                                            //                if (DateTime.IsLeapYear(Period.Year) == true)
                                                            //                {
                                                            //                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25;
                                                            //                    BasicSalary = BasicSalaryDays * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25);
                                                            //                }
                                                            //                else
                                                            //                {
                                                            //                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                            //                    BasicSalary = BasicSalaryDays * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24);
                                                            //                }
                                                            //            }
                                                            //            else
                                                            //            {
                                                            //                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            //                BasicSalary = BasicSalaryDays * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")); ;
                                                            //            }
                                                            //        }
                                                            //        else
                                                            //        {
                                                            //            BasicSalaryDays = BasicSalaryDays - (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                            //            if (Branch.Substring(0, 2) == "SW" && EmployeeNationality == "L")
                                                            //            {
                                                            //                if (DateTime.IsLeapYear(Period.Year) == true)
                                                            //                {
                                                            //                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25;
                                                            //                    //BasicSalary = BasicSalaryDays * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25);
                                                            //                    BasicSalary = BasicSalary - ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25));
                                                            //                }
                                                            //                else
                                                            //                {
                                                            //                    BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                            //                    //BasicSalary = BasicSalaryDays * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24);
                                                            //                    BasicSalary = BasicSalary - ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * ((drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 25));
                                                            //                }
                                                            //            }
                                                            //            else
                                                            //            {
                                                            //                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            //                BasicSalary = BasicSalaryDays * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")); ;
                                                            //            }
                                                            //        }
                                                            //    }
                                                            //}

                                                            break;
                                                        case 8://Annual Leave
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            { }
                                                            else
                                                            {
                                                                if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                                {
                                                                    if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    }
                                                                    else if (Period.Month == 2)
                                                                    {
                                                                        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                        {
                                                                            BasicSalaryDays = 24;
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                BasicSalary += (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary = 24 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")); ;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 24)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));

                                                                            }
                                                                        }

                                                                    }

                                                                    else
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }
                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                }
                                                                else
                                                                {
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //amend below on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC  
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    //BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    if (bIncompleteMonth == true)
                                                                    {
                                                                        BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                        BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    }
                                                                }
                                                            }

                                                            break;
                                                        case 9://Medical leave
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            { }
                                                            else
                                                            {
                                                                if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                                {
                                                                    if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    }
                                                                    else if (Period.Month == 2)
                                                                    {
                                                                        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                        {
                                                                            BasicSalaryDays = 24;
                                                                            //BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                                                BasicSalary = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalary = 24 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }

                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    if (BasicSalaryDays >= 24)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));


                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }
                                                                    //BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                    //BasicSalaryDays += ((int)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"));
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                }
                                                                else
                                                                {
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //amend below on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC  
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    //BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    if (BasicSalaryDays <= 0)
                                                                    {
                                                                        //BasicSalary += 22 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        //BasicSalaryDays += 22;
                                                                        BasicSalary += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryDays += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }

                                                                    if (bIncompleteMonth == true)
                                                                    {
                                                                        BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                        BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    }
                                                                }
                                                            }

                                                            break;
                                                        case 10:// Maternity Leave
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            { }
                                                            else
                                                            {
                                                                if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                                {
                                                                    if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    }
                                                                    else if (Period.Month == 2)
                                                                    {
                                                                        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                        {
                                                                            //BasicSalary = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                            BasicSalaryDays = 24;
                                                                            //BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;

                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 24)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 24)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }
                                                                    //BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                    //BasicSalaryDays += ((int)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"));
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                }
                                                                else
                                                                {
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //amend below on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC  
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    //BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    if (BasicSalaryDays <= 0)
                                                                    {
                                                                        //BasicSalary += 22 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        //BasicSalaryDays += 22;
                                                                        BasicSalary += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryDays += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }

                                                                    if (bIncompleteMonth == true)
                                                                    {
                                                                        BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                        BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    }
                                                                }
                                                            }

                                                            break;
                                                        case 11:// Paternity leave
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            { }
                                                            else
                                                            {
                                                                if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                                {
                                                                    if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    }
                                                                    else if (Period.Month == 2)
                                                                    {
                                                                        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                        {
                                                                            //BasicSalary = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                            BasicSalaryDays = 24;
                                                                            //BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 24)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }
                                                                    //BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                    //BasicSalaryDays += ((int)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"));
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                }
                                                                else
                                                                {
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //amend below on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC  
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    //BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    if (BasicSalaryDays <= 0)
                                                                    {
                                                                        //BasicSalary += 22 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        //BasicSalaryDays += 22;
                                                                        BasicSalary += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryDays += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }

                                                                    if (bIncompleteMonth == true)
                                                                    {
                                                                        BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                        BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    }
                                                                }
                                                            }

                                                            break;
                                                        case 12:// Hospitalization leave
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            { }
                                                            else
                                                            {
                                                                if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                                {
                                                                    if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    }
                                                                    else if (Period.Month == 2)
                                                                    {
                                                                        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                        {
                                                                            //BasicSalary = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                            BasicSalaryDays = 24;
                                                                            //BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }
                                                                    //BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                    //BasicSalaryDays += ((int)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"));
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                }
                                                                else
                                                                {
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //amend below on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC  
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    //BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    if (BasicSalaryDays <= 0)
                                                                    {
                                                                        //BasicSalary += 22 * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        //BasicSalaryDays += 22;
                                                                        BasicSalary += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryDays += drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }
                                                                    if (bIncompleteMonth == true)
                                                                    {
                                                                        BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                        BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    }
                                                                }
                                                            }

                                                            break;
                                                        case 14:// Non Schedule Day 
                                                            if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                            {
                                                                WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));


                                                            }
                                                            if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                            {
                                                                BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            }
                                                            else
                                                            {
                                                                if (bIncompleteMonth == true)
                                                                {
                                                                    BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                }
                                                            }
                                                            break;
                                                        case 15:// Replacement Leave
                                                            if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                            {
                                                                WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));

                                                            }
                                                            if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                            {
                                                                BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                            }
                                                            else
                                                            {
                                                                if (bIncompleteMonth == true)
                                                                {
                                                                    BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                }
                                                            }
                                                            break;
                                                        case 16:// Compensanate Leave
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            { }
                                                            else
                                                            {
                                                                if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                                {
                                                                    if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    }
                                                                    else if (Period.Month == 2)
                                                                    {
                                                                        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                        {
                                                                            //BasicSalary = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                            BasicSalaryDays = 24;
                                                                            //BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 24)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }
                                                                    //BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                    //BasicSalaryDays += ((int)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"));
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                }
                                                                else
                                                                {
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //amend below on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC  
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    //BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));

                                                                    if (bIncompleteMonth == true)
                                                                    {
                                                                        BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                        BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    }

                                                                }
                                                            }

                                                            break;
                                                        case 17:// Marriage Leave
                                                            if (drEmployee.GetString(drEmployee.GetOrdinal("NAME")).Contains("ONSEMI"))
                                                            { }
                                                            else
                                                            {
                                                                if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                                                {
                                                                    if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        WorkingDay = WorkingDay + (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays"));
                                                                    }
                                                                    else if (Period.Month == 2)
                                                                    {
                                                                        if ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) >= 192)
                                                                        {
                                                                            //BasicSalary = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")));
                                                                            BasicSalaryDays = 24;
                                                                            //BasicSalaryRate = (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"))) / 24;
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (CompanyCode.ToString().Trim() == "Swack" && EmployeeNationality == "L")
                                                                            {
                                                                                if (DateTime.IsLeapYear(Period.Year) == true)
                                                                                {

                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 25);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 25)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    //BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                                    BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 24);
                                                                                    BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                    if (BasicSalaryDays >= 24)
                                                                                    {
                                                                                        BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                                BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                                BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                            }
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        BasicSalary += (decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")));
                                                                        BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingHours")))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                        BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    }
                                                                    //BasicSalary += (decimal)(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))));
                                                                    //BasicSalaryDays += ((int)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours"))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"));
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                }
                                                                else
                                                                {
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand")) / 22 / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")))), 2);
                                                                    //BasicSalaryDays += ((decimal)drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalHours")) * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")) / (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours")) == 0 ? 1 : drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayHours"))))) / drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));
                                                                    //amend below on 14 Aug 2023, requested by Uma from FWG to strict to 22 days for EICC  
                                                                    //BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                    //BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    //BasicSalaryRate = drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate"));

                                                                    if (bIncompleteMonth == true)
                                                                    {
                                                                        BasicSalary += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")) * drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")), 2);
                                                                        BasicSalaryDays += Math.Round(drAttendance.GetDecimal(drAttendance.GetOrdinal("NormalDays")), 2);
                                                                    }

                                                                }
                                                            }

                                                            break;
                                                    }
                                                }
                                            }
                                        }

                                        dBonus = dBonus + dTotalKPIAmount;

                                        if (EmployeeType == "Guard" && drEmployee.GetBoolean(drEmployee.GetOrdinal("NonStructure")) == true)
                                        {
                                            if (BasicSalaryDays >= drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays")))
                                            {
                                                dSalaryBand = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                BasicSalary = drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays")) * BasicSalaryRate;
                                                BasicSalaryDays = drEmployee.GetDecimal(drEmployee.GetOrdinal("WorkingDays"));
                                            }
                                            else
                                            {
                                                dSalaryBand = drEmployee.GetDecimal(drEmployee.GetOrdinal("SalaryBand"));
                                                dTransferBasicBalance = (decimal)(dSalaryBand - BasicSalary);
                                                if (dTransferBasicBalance <= 1)
                                                {
                                                    dTransferBasicBalance = 0;
                                                }
                                            }

                                            if (WorkingDay >= 1)
                                            {

                                                int days = DateTime.DaysInMonth(Period.Year, Period.Month);
                                                int noOfAbsentDay = 0;
                                                decimal dNonstructureDayRate = 0;
                                                decimal dNonWorkingDeduction = 0;
                                                decimal dLumpSumRate = 0;
                                                noOfAbsentDay = (int)(days - WorkingDay);
                                                dLumpSumRate = (decimal)drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate"));


                                                dNonstructureDayRate = (decimal)((drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayOTRate")) + dSalaryBand) / 30);


                                                if (days == 31)
                                                {
                                                    dLumpSumRate = (decimal)dLumpSumRate + dNonstructureDayRate;
                                                }


                                                if (noOfAbsentDay > 0)
                                                {
                                                    dNonWorkingDeduction = (decimal)(noOfAbsentDay * dNonstructureDayRate);
                                                    dLumpSum_NonStructure = (decimal)(dTransferBasicBalance + (dLumpSumRate - dNonWorkingDeduction));
                                                }
                                                else
                                                {
                                                    dLumpSum_NonStructure = (decimal)dLumpSumRate;
                                                }


                                            }
                                        }

                                        decimal strEmployeeID = 0;
                                        strEmployeeID = EmployeeID;
                                        decimal ReAllowanceDays = 0;
                                        decimal PaidLeave = 0;
                                        PaidLeave = MaternityLeaveDay + HospitalizationLeaveDay + SocsoDay;

                                        if (EmployeeType == "Staff")
                                        {
                                            if (MPMbool)
                                            {
                                                //Default basic salary Day to 26 if hours > 188(All month) or 176(Feb)
                                                BasicSalaryDays = BasicSalaryDays + AnnualLeaveDay + MedicalLeaveDay + PaternityLeaveDay + MaternityLeaveDay + HolidaySalaryDays + HolidayDays + UnPaidLeaveDay + AbsentDay + HospitalizationLeaveDay + SocsoDay;
                                                AllowanceDays = BasicSalaryDays;

                                                //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;
                                                if (PaidLeave >= NoOfWorkingDays)
                                                {
                                                    ReAllowanceDays = 0;
                                                    dReAllowance = 0;
                                                }
                                                else
                                                {
                                                    BasicSalaryDays = BasicSalaryDays * 8;
                                                    if (BasicSalaryDays >= 188)
                                                    {
                                                        //Default Days
                                                        BasicSalaryDays = 26;
                                                        AllowanceDays = 26;

                                                        ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - MedicalLeaveDay - PaternityLeaveDay - MaternityLeaveDay - AnnualLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                        dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - MedicalLeaveDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowance;
                                                        dReAllowance = Math.Round(dReAllowance, 1);
                                                    }
                                                    else
                                                    {
                                                        BasicSalaryDays = AllowanceDays;
                                                        //ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - MedicalLeaveDay - PaternityLeaveDay - MaternityLeaveDay;
                                                        //dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay) * dReAllowance;
                                                        ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - MedicalLeaveDay - PaternityLeaveDay - MaternityLeaveDay - AnnualLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                        dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - MedicalLeaveDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowance;
                                                        dReAllowance = Math.Round(dReAllowance, 1);
                                                    }

                                                }

                                                //Default basic salary Day to 26 if hours > 188(All month) or 176(Feb)
                                                //BasicSalaryDays = BasicSalaryDays + AnnualLeaveDay + MedicalLeaveDay + PaternityLeaveDay + MaternityLeaveDay + HolidaySalaryDays + UnPaidLeaveDay + AbsentDay;
                                                BasicSalaryDays = BasicSalaryDays * 8;

                                                if (BasicSalaryDays >= 188)
                                                {
                                                    //Default Days
                                                    BasicSalaryDays = 26;
                                                    AllowanceDays = 26;
                                                }
                                                else
                                                {
                                                    if (Period.Month == 2 && BasicSalaryDays >= 176)
                                                    {
                                                        BasicSalaryDays = 26;
                                                        AllowanceDays = 26;
                                                        //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;
                                                    }
                                                    else
                                                    {
                                                        BasicSalaryDays = BasicSalaryDays / 8;
                                                        //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;

                                                        //cater for Month - FEB only
                                                        if (Period.Month == 2)
                                                        {
                                                            if (BasicSalaryDays >= 22)
                                                            {
                                                                BasicSalaryDays = 26;
                                                                AllowanceDays = 26;
                                                                //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;
                                                            }
                                                            else
                                                            {
                                                                if (BasicSalaryDays > 22 && BasicSalaryDays < 26)
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays + 4; //4th week
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                else if (BasicSalaryDays > 20 && BasicSalaryDays < 22)
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays + 3; // 3rd week
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                else if (BasicSalaryDays > 13 && BasicSalaryDays < 20)
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays + 2; //2nd week
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                else if (BasicSalaryDays > 6 && BasicSalaryDays < 13)
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays + 1; //1st week
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                else
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays;
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                //BasicSalaryDays = BasicSalaryDays + 4;
                                                                //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;
                                                            }
                                                        }
                                                    }
                                                }

                                                BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay - SocsoDay;
                                                BasicSalary = BasicSalaryDays * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")));
                                            }

                                            else//mpm is false //**not guard
                                            {
                                                if (PaidLeave >= NoOfWorkingDays)
                                                {
                                                    ReAllowanceDays = 0;
                                                    dReAllowance = 0;
                                                }
                                                else
                                                {
                                                    //ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - MedicalLeaveDay - PaternityLeaveDay - MaternityLeaveDay;
                                                    //dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - MedicalLeaveDay - PaternityLeaveDay - MaternityLeaveDay) * dReAllowance;
                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowance;
                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                }

                                                //Default basic salary Day to 26 if hours > 188(All month) or 176(Feb)
                                                BasicSalaryDays = BasicSalaryDays + AnnualLeaveDay + MedicalLeaveDay + PaternityLeaveDay + MaternityLeaveDay + HolidaySalaryDays + HolidayDays + UnPaidLeaveDay + AbsentDay + SocsoDay + HospitalizationLeaveDay;
                                                BasicSalaryDays = BasicSalaryDays * 8;

                                                if (BasicSalaryDays >= 188)
                                                {
                                                    BasicSalaryDays = 26;
                                                    AllowanceDays = 26;
                                                    //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;
                                                }
                                                else
                                                {
                                                    if (Period.Month == 2 && BasicSalaryDays >= 176)
                                                    {
                                                        BasicSalaryDays = 26;
                                                        AllowanceDays = 26;
                                                        //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;
                                                    }
                                                    else
                                                    {
                                                        BasicSalaryDays = BasicSalaryDays / 8;
                                                        //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;

                                                        //cater for Month - FEB only
                                                        if (Period.Month == 2)
                                                        {
                                                            if (BasicSalaryDays >= 22)
                                                            {
                                                                BasicSalaryDays = 26;
                                                                AllowanceDays = 26;
                                                                //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;
                                                            }
                                                            else
                                                            {
                                                                if (BasicSalaryDays > 22 && BasicSalaryDays <= 26)
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays + 4; //4th week
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                else if (BasicSalaryDays > 20 && BasicSalaryDays <= 22)
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays + 3; // 3rd week
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                else if (BasicSalaryDays > 13 && BasicSalaryDays <= 20)
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays + 2; //2nd week
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                else if (BasicSalaryDays > 6 && BasicSalaryDays <= 13)
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays + 1; //1st week
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                else
                                                                {
                                                                    BasicSalaryDays = BasicSalaryDays;
                                                                    AllowanceDays = BasicSalaryDays;
                                                                    ReAllowanceDays = AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                                                    dReAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dReAllowanceRate;
                                                                    dReAllowance = Math.Round(dReAllowance, 1);
                                                                }
                                                                //BasicSalaryDays = BasicSalaryDays + 4;
                                                                //BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay;
                                                            }
                                                        }
                                                    }
                                                }

                                                BasicSalaryDays = BasicSalaryDays - UnPaidLeaveDay - AbsentDay - SocsoDay;
                                                BasicSalary = BasicSalaryDays * (drEmployee.GetDecimal(drEmployee.GetOrdinal("GeneralDayRate")));
                                            }
                                        }
                                        else
                                        {
                                            if (drEmployee.GetBoolean(drEmployee.GetOrdinal("EICC")) == false)
                                            {
                                                ReAllowanceDays = AllowanceDays + OffdaySalaryDays + HolidaySalaryDays + HolidayDays;
                                            }
                                            else
                                            {
                                                ReAllowanceDays = AllowanceDays + OffdaySalaryDays + HolidaySalaryDays + HolidayDays;
                                                ReAllowanceDays = ReAllowanceDays - UnPaidLeaveDay - AbsentDay - MedicalLeaveDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay;
                                            }
                                            dReAllowance = (AllowanceDays + OffdaySalaryDays + HolidaySalaryDays + HolidayDays) * dReAllowance;
                                            dReAllowance = Math.Round(dReAllowance, 1);
                                        }

                                        if (EmployeeType == "Staff")
                                        {
                                            if (MPMbool)
                                            {
                                                if (PaidLeave >= NoOfWorkingDays)
                                                {
                                                    dAttendanceAllowance = 0;
                                                    BasicSalary = Math.Round(BasicSalary, 2);
                                                }
                                                else
                                                {
                                                    if ((AllowanceDays - UnPaidLeaveDayActual - AbsentDayActual - PaternityLeaveDayActual - MaternityLeaveDayActual - SocsoDayActual - HospitalizationLeaveDayActual) < NoOfWorkingDays)
                                                    {
                                                        dAttendanceAllowance = (AllowanceDays - UnPaidLeaveDayActual - AbsentDayActual - PaternityLeaveDayActual - MaternityLeaveDayActual - SocsoDayActual - HospitalizationLeaveDayActual) * dAttendanceAllowance / NoOfWorkingDays;
                                                        dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2);
                                                    }
                                                    BasicSalary = Math.Round(BasicSalary, 2);
                                                }
                                            }
                                            else
                                            {
                                                if (PaidLeave >= NoOfWorkingDays)
                                                {
                                                    dAttendanceAllowance = 0;
                                                    BasicSalary = Math.Round(BasicSalary, 2);
                                                }
                                                else
                                                {
                                                    if ((AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) < NoOfWorkingDays)
                                                    {
                                                        dAttendanceAllowance = (AllowanceDays - UnPaidLeaveDay - AbsentDay - PaternityLeaveDay - MaternityLeaveDay - SocsoDay - HospitalizationLeaveDay) * dAttendanceAllowance / NoOfWorkingDays;
                                                        dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2);
                                                    }
                                                    BasicSalary = Math.Round(BasicSalary, 2);
                                                }
                                            }
                                        }
                                        else//Guard
                                        {
                                            if (sAttendanceAllowanceFollowCalendar == "N" && (dAttendanceAllowanceDays == 0))
                                            {
                                                if (PaidLeave >= NoOfWorkingDays)
                                                {
                                                    dAttendanceAllowance = 0;
                                                    dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2) - dAttendanceDeduction;
                                                }
                                                else
                                                {
                                                    dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2) - dAttendanceDeduction;
                                                }
                                            }
                                            else
                                            {
                                                if (sAttendanceAllowanceFollowCalendar == "Y")
                                                {
                                                    int DaysInMonth = System.DateTime.DaysInMonth(Period.Year, Period.Month);

                                                    if (PaidLeave >= DaysInMonth)
                                                    {
                                                        dAttendanceAllowance = 0;
                                                        dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2) - dAttendanceDeduction;
                                                    }
                                                    else
                                                    {
                                                        if (AllowanceDays + OffdaySalaryDays + HolidaySalaryDays + HolidayDays < DaysInMonth)
                                                        {
                                                            dAttendanceAllowance = (AllowanceDays + OffdaySalaryDays + HolidaySalaryDays + HolidayDays) * (dAttendanceAllowance / DaysInMonth);
                                                            dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2) - dAttendanceDeduction;
                                                        }
                                                        else
                                                        {
                                                            dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2) - dAttendanceDeduction;
                                                        }
                                                    }

                                                }
                                                else
                                                {
                                                    if (PaidLeave >= dAttendanceAllowanceDays)
                                                    {
                                                        dAttendanceAllowance = 0;
                                                        dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2) - dAttendanceDeduction;
                                                    }
                                                    else
                                                    {
                                                        if (AllowanceDays + OffdaySalaryDays + HolidaySalaryDays + HolidayDays < dAttendanceAllowanceDays)
                                                        {
                                                            dAttendanceAllowance = (AllowanceDays + OffdaySalaryDays + HolidaySalaryDays + HolidayDays) * (dAttendanceAllowance / dAttendanceAllowanceDays);
                                                            dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2) - dAttendanceDeduction;
                                                        }
                                                        else
                                                        {
                                                            dAttendanceAllowance = Math.Round(dAttendanceAllowance, 2) - dAttendanceDeduction;
                                                        }
                                                    }
                                                }

                                            }


                                            if (Math.Round(BasicSalary, 2) == (decimal)(1499.99))
                                            {
                                                BasicSalary = (decimal)(1500.00);
                                            }
                                            else
                                            {
                                                BasicSalary = Math.Round(BasicSalary, 2, MidpointRounding.AwayFromZero);
                                            }
                                        }

                                        sbQuery = new StringBuilder();
                                        //sbQuery.Append("select attendance.shift2type,attendance.shift2rate,(case attendance.shift2type when 1 then count(attendanceid) else sum(DateDiff(hour,OTTimeStart,OTTimeEnd)) end),cast(((case attendance.shift2type when 1 then count(attendanceid) else sum(DateDiff(hour,OTTimeStart,OTTimeEnd)) end) * Shift2Rate) as NUMERIC(18,2))  ");
                                        //sbQuery.Append("from attendancedetails inner join attendance on attendance.id=AttendanceDetails.AttendanceID ");
                                        //sbQuery.Append("WHERE Month(AttendanceDetails.AttendanceDate)=@PeriodMonth AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear AND Attendance.EmployeeID=@EmployeeID ");
                                        //sbQuery.Append("and OTTimeStart IS NOT NULL and OTTimeEnd IS NOT NULL ");
                                        //sbQuery.Append("group by attendanceid,shift2type,Shift2Rate ");

                                        //sbQuery.Append("Select ");
                                        //sbQuery.Append("Shift2Type,Shift2Rate,sum(NormalShift2) as NormalShift2, ");
                                        //sbQuery.Append("Sum(HolidayShift2) as HolidayShift2,sum(TotalNormalAmount) as TotalNormalAmount ");
                                        //sbQuery.Append("From ");
                                        //sbQuery.Append("( ");
                                        //sbQuery.Append("select attendance.shift2type,attendance.shift2rate, ");
                                        //sbQuery.Append("(case attendance.shift2type when 1 then count(attendanceid) else sum(DateDiff(hour,OTTimeStart,OTTimeEnd)) end) as normalShift2, ");
                                        //sbQuery.Append("(case attendanceDetails.Type when 5 then ");
                                        //sbQuery.Append("(case attendance.shift2type when 1 then count(attendanceid) else sum(DateDiff(hour,OTTimeStart,OTTimeEnd)) end) ");
                                        //sbQuery.Append("else 0 end) as HolidayShift2, ");
                                        //sbQuery.Append("cast(((case attendance.shift2type when 1 then count(attendanceid) else sum(DateDiff(hour,OTTimeStart,OTTimeEnd)) end) * Shift2Rate) as NUMERIC(18,2)) as TotalNormalAmount ");
                                        //sbQuery.Append("from attendancedetails inner join attendance on attendance.id=AttendanceDetails.AttendanceID  ");
                                        //sbQuery.Append("WHERE  ");
                                        //sbQuery.Append("Month(AttendanceDetails.AttendanceDate)=@PeriodMonth   ");
                                        //sbQuery.Append("AND YEAR(AttendanceDetails.AttendanceDate)=@PeriodYear  ");
                                        //sbQuery.Append("AND Attendance.EmployeeID=@EmployeeID  ");
                                        //sbQuery.Append("and OTTimeStart IS NOT NULL and OTTimeEnd IS NOT NULL  ");
                                        //sbQuery.Append("group by attendanceid,shift2type,Shift2Rate,attendanceDetails.Type  ");
                                        //sbQuery.Append(")ShiftII  ");
                                        //sbQuery.Append("group by Shift2Type,Shift2Rate  ");

                                        //sbQuery.Append("SELECT ");
                                        //sbQuery.Append("    Shift2Type, Shift2Rate, ");
                                        //sbQuery.Append("    SUM(NormalShift2) AS NormalShift2, ");
                                        //sbQuery.Append("    SUM(HolidayShift2) AS HolidayShift2, ");
                                        //sbQuery.Append("    SUM(TotalNormalAmount) AS TotalNormalAmount ");
                                        //sbQuery.Append("FROM ( ");
                                        //sbQuery.Append("    SELECT ");
                                        //sbQuery.Append("        A.Shift2Type, ");
                                        //sbQuery.Append("        A.Shift2Rate, ");

                                        //sbQuery.Append("        (CASE A.Shift2Type ");
                                        //sbQuery.Append("             WHEN 1 THEN COUNT(AD.AttendanceID) ");
                                        //sbQuery.Append("             ELSE SUM(CASE WHEN AD.OTTimeEnd < AD.OTTimeStart ");
                                        //sbQuery.Append("                           THEN DATEDIFF(HOUR, AD.OTTimeStart, AD.OTTimeEnd) + 24 ");
                                        //sbQuery.Append("                           ELSE DATEDIFF(HOUR, AD.OTTimeStart, AD.OTTimeEnd) ");
                                        //sbQuery.Append("                      END) ");
                                        //sbQuery.Append("         END) AS NormalShift2, ");

                                        //sbQuery.Append("        (CASE AD.Type ");
                                        //sbQuery.Append("             WHEN 5 THEN ");
                                        //sbQuery.Append("                 (CASE A.Shift2Type ");
                                        //sbQuery.Append("                      WHEN 1 THEN COUNT(AD.AttendanceID) ");
                                        //sbQuery.Append("                      ELSE SUM(CASE WHEN AD.OTTimeEnd < AD.OTTimeStart ");
                                        //sbQuery.Append("                                    THEN DATEDIFF(HOUR, AD.OTTimeStart, AD.OTTimeEnd) + 24 ");
                                        //sbQuery.Append("                                    ELSE DATEDIFF(HOUR, AD.OTTimeStart, AD.OTTimeEnd) ");
                                        //sbQuery.Append("                               END) ");
                                        //sbQuery.Append("                  END) ");
                                        //sbQuery.Append("             ELSE 0 ");
                                        //sbQuery.Append("         END) AS HolidayShift2, ");

                                        //sbQuery.Append("        CAST( ( ");
                                        //sbQuery.Append("             (CASE A.Shift2Type ");
                                        //sbQuery.Append("                 WHEN 1 THEN COUNT(AD.AttendanceID) ");
                                        //sbQuery.Append("                 ELSE SUM(CASE WHEN AD.OTTimeEnd < AD.OTTimeStart ");
                                        //sbQuery.Append("                               THEN DATEDIFF(HOUR, AD.OTTimeStart, AD.OTTimeEnd) + 24 ");
                                        //sbQuery.Append("                               ELSE DATEDIFF(HOUR, AD.OTTimeStart, AD.OTTimeEnd) ");
                                        //sbQuery.Append("                          END) ");
                                        //sbQuery.Append("              END) * A.Shift2Rate ");
                                        //sbQuery.Append("        ) AS NUMERIC(18,2)) AS TotalNormalAmount ");

                                        //sbQuery.Append("    FROM AttendanceDetails AD ");
                                        //sbQuery.Append("    INNER JOIN Attendance A ON A.ID = AD.AttendanceID ");

                                        //sbQuery.Append("    WHERE ");
                                        //sbQuery.Append("        MONTH(AD.AttendanceDate) = @PeriodMonth ");
                                        //sbQuery.Append("        AND YEAR(AD.AttendanceDate) = @PeriodYear ");
                                        //sbQuery.Append("        AND A.EmployeeID = @EmployeeID ");
                                        //sbQuery.Append("        AND AD.OTTimeStart IS NOT NULL ");
                                        //sbQuery.Append("        AND AD.OTTimeEnd IS NOT NULL ");
                                        //sbQuery.Append("        AND NOT (CAST(AD.OTTimeStart AS TIME) = '00:00:00' ");
                                        //sbQuery.Append("        AND CAST(AD.OTTimeEnd AS TIME) = '00:00:00') ");

                                        //sbQuery.Append("    GROUP BY ");
                                        //sbQuery.Append("        A.Shift2Type, A.Shift2Rate, AD.Type ");
                                        //sbQuery.Append(") ShiftII ");

                                        //sbQuery.Append("GROUP BY Shift2Type, Shift2Rate; ");

                                        sbQuery.Append("SELECT ");
                                        sbQuery.Append("    Shift2Type, Shift2Rate, ");
                                        sbQuery.Append("    SUM(NormalShift2) AS NormalShift2, ");
                                        sbQuery.Append("    SUM(HolidayShift2) AS HolidayShift2, ");
                                        sbQuery.Append("    SUM(TotalNormalAmount) AS TotalNormalAmount ");
                                        sbQuery.Append("FROM ( ");
                                        sbQuery.Append("    SELECT ");
                                        sbQuery.Append("        A.Shift2Type, ");
                                        sbQuery.Append("        A.Shift2Rate, ");

                                        sbQuery.Append("        (CASE A.Shift2Type ");
                                        sbQuery.Append("             WHEN 1 THEN COUNT(AD.AttendanceID) ");
                                        sbQuery.Append("             ELSE SUM( ");
                                        sbQuery.Append("                 DATEDIFF(MINUTE, AD.OTTimeStart, ");
                                        sbQuery.Append("                     CASE WHEN AD.OTTimeEnd < AD.OTTimeStart ");
                                        sbQuery.Append("                          THEN DATEADD(DAY,1,AD.OTTimeEnd) ");
                                        sbQuery.Append("                          ELSE AD.OTTimeEnd END ");
                                        sbQuery.Append("                 ) / 60.0 ");
                                        sbQuery.Append("             ) ");
                                        sbQuery.Append("         END) AS NormalShift2, ");

                                        sbQuery.Append("        (CASE AD.Type ");
                                        sbQuery.Append("             WHEN 5 THEN ");
                                        sbQuery.Append("                 (CASE A.Shift2Type ");
                                        sbQuery.Append("                      WHEN 1 THEN COUNT(AD.AttendanceID) ");
                                        sbQuery.Append("                      ELSE SUM( ");
                                        sbQuery.Append("                          DATEDIFF(MINUTE, AD.OTTimeStart, ");
                                        sbQuery.Append("                              CASE WHEN AD.OTTimeEnd < AD.OTTimeStart ");
                                        sbQuery.Append("                                   THEN DATEADD(DAY,1,AD.OTTimeEnd) ");
                                        sbQuery.Append("                                   ELSE AD.OTTimeEnd END ");
                                        sbQuery.Append("                          ) / 60.0 ");
                                        sbQuery.Append("                      ) ");
                                        sbQuery.Append("                  END) ");
                                        sbQuery.Append("             ELSE 0 ");
                                        sbQuery.Append("         END) AS HolidayShift2, ");

                                        sbQuery.Append("        CAST( ( ");
                                        sbQuery.Append("             (CASE A.Shift2Type ");
                                        sbQuery.Append("                 WHEN 1 THEN COUNT(AD.AttendanceID) ");
                                        sbQuery.Append("                 ELSE SUM( ");
                                        sbQuery.Append("                     DATEDIFF(MINUTE, AD.OTTimeStart, ");
                                        sbQuery.Append("                         CASE WHEN AD.OTTimeEnd < AD.OTTimeStart ");
                                        sbQuery.Append("                              THEN DATEADD(DAY,1,AD.OTTimeEnd) ");
                                        sbQuery.Append("                              ELSE AD.OTTimeEnd END ");
                                        sbQuery.Append("                     ) / 60.0 ");
                                        sbQuery.Append("                 ) ");
                                        sbQuery.Append("              END) * A.Shift2Rate ");
                                        sbQuery.Append("        ) AS NUMERIC(18,2)) AS TotalNormalAmount ");

                                        sbQuery.Append("    FROM AttendanceDetails AD ");
                                        sbQuery.Append("    INNER JOIN Attendance A ON A.ID = AD.AttendanceID ");

                                        sbQuery.Append("    WHERE ");
                                        sbQuery.Append("        MONTH(AD.AttendanceDate) = @PeriodMonth ");
                                        sbQuery.Append("        AND YEAR(AD.AttendanceDate) = @PeriodYear ");
                                        sbQuery.Append("        AND A.EmployeeID = @EmployeeID ");
                                        sbQuery.Append("        AND AD.OTTimeStart IS NOT NULL ");
                                        sbQuery.Append("        AND AD.OTTimeEnd IS NOT NULL ");
                                        sbQuery.Append("        AND NOT (CAST(AD.OTTimeStart AS TIME) = '00:00:00' ");
                                        sbQuery.Append("        AND CAST(AD.OTTimeEnd AS TIME) = '00:00:00') ");

                                        sbQuery.Append("    GROUP BY ");
                                        sbQuery.Append("        A.Shift2Type, A.Shift2Rate, AD.Type ");
                                        sbQuery.Append(") ShiftII ");

                                        sbQuery.Append("GROUP BY Shift2Type, Shift2Rate; ");



                                        SqlCommand cmdShift2Attendance = new SqlCommand();
                                        cmdShift2Attendance.CommandText = sbQuery.ToString();
                                        cmdShift2Attendance.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        cmdShift2Attendance.Parameters.AddWithValue("@PeriodMonth", Period.Month);
                                        cmdShift2Attendance.Parameters.AddWithValue("@PeriodYear", Period.Year);
                                        SQLDataAccess sdaShift2AttendanceList = new SQLDataAccess(_configuration);
                                        SqlDataReader drShift2Attendance = sdaShift2AttendanceList.RetrieveData(cmdShift2Attendance);
                                        decimal Shift2type = 0;
                                        decimal Shift2rate = 0;
                                        decimal Shift2days = 0;
                                        decimal Shift2Holidays = 0;
                                        decimal Shift2Amount = 0;
                                        decimal Shift2HolidayAmount = 0;
                                        decimal Shift2NormalAmount = 0;
                                        while (drShift2Attendance.Read())
                                        {
                                            //Shift2type = drShift2Attendance.GetInt32(0);
                                            //Shift2rate = drShift2Attendance.GetDecimal(1);
                                            //Shift2days = drShift2Attendance.GetInt32(2);
                                            //Shift2Amount = drShift2Attendance.GetDecimal(3);
                                            //Shift2Amount = drShift2Attendance.GetDecimal(4); // Amount for normal days

                                            //Cater for Shift2 Holiday Rate base on Basic Salary Structure
                                            //HolidaySalaryRateForShift2 = Math.Round(drEmployee.GetDecimal(drEmployee.GetOrdinal("HolidayRate")), 2);

                                            Shift2type = drShift2Attendance.GetInt32(0);
                                            Shift2rate = drShift2Attendance.GetDecimal(1);
                                            Shift2days = drShift2Attendance.GetDecimal(2);
                                            Shift2Holidays = drShift2Attendance.GetDecimal(3);
                                            //Shift2HolidayAmount = Shift2Holidays * HolidaySalaryRateForShift2 * Shift2rate;
                                            //Shift2NormalAmount = (Shift2days - Shift2Holidays) * Shift2rate;
                                            Shift2NormalAmount = Shift2days * Shift2rate;
                                            Shift2Amount = Shift2NormalAmount + Shift2HolidayAmount;
                                            Shift2Amount = Math.Round(Shift2Amount, 2);


                                            //End
                                        }

                                        cmdShift2Attendance.Dispose();
                                        drShift2Attendance.Dispose();
                                        sdaShift2AttendanceList.Dispose();

                                        decimal dMiscEarnings = 0;
                                        decimal dMiscDeductions = 0;



                                        sbQuery = new StringBuilder();
                                        sbQuery.Append(" SELECT ISNULL(SUM(Amount),0) FROM MiscTrans WHERE ( Month(TransDate)= Month(@TransDate) AND Year(TransDate)= Year(@TransDate)) AND EmployeeID = @EmployeeID AND TransType = 1");
                                        SqlCommand cmdMiscEarnings = new SqlCommand();
                                        cmdMiscEarnings.CommandText = sbQuery.ToString();
                                        cmdMiscEarnings.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        cmdMiscEarnings.Parameters.AddWithValue("@TransDate", Period);

                                        SQLDataAccess sdaMiscEarnings = new SQLDataAccess(_configuration);
                                        using (SqlDataReader drMiscEarnings = sdaMiscEarnings.RetrieveData(cmdMiscEarnings))
                                        {

                                            while (drMiscEarnings.Read())
                                            {
                                                dMiscEarnings = drMiscEarnings.GetDecimal(0);
                                            }
                                            drMiscEarnings.Dispose();
                                        }

                                        cmdMiscEarnings.Dispose();
                                        sdaMiscEarnings.Dispose();

                                        dMiscEarnings = dMiscEarnings + dLumpSum_NonStructure;

                                        sbQuery = new StringBuilder();
                                        sbQuery.Append(" SELECT ISNULL(SUM(Amount),0) FROM MiscTrans WHERE ( Month(TransDate)= Month(@TransDate) AND Year(TransDate)= Year(@TransDate)) AND EmployeeID = @EmployeeID AND TransType = 2");
                                        SqlCommand cmdMiscDeductions = new SqlCommand();
                                        cmdMiscDeductions.CommandText = sbQuery.ToString();
                                        cmdMiscDeductions.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        cmdMiscDeductions.Parameters.AddWithValue("@TransDate", Period);

                                        SQLDataAccess sdaMiscDeductions = new SQLDataAccess(_configuration);
                                        using (SqlDataReader drMiscDeductions = sdaMiscDeductions.RetrieveData(cmdMiscDeductions))
                                        {

                                            while (drMiscDeductions.Read())
                                            {
                                                dMiscDeductions = drMiscDeductions.GetDecimal(0);
                                            }
                                            drMiscDeductions.Dispose();
                                        }

                                        cmdMiscDeductions.Dispose();
                                        sdaMiscDeductions.Dispose();
                                        dMiscDeductions = dMiscDeductions + OnSEMIDeduction;
                                        //sbQuery.Append(" SELECT SalaryAdvance.ID, SalaryAdvance.TransType, CAST((SalaryAdvance.Amount/SalaryAdvance.NoOfInstallments) AS Numeric(18,2)) AS Installment, ");
                                        //sbQuery.Append(" SalaryAdvance.Amount AS Advance,ISNULL(Sum(advancerepayment.Amount),0) as Payment ");
                                        //sbQuery.Append(" FROM SalaryAdvance  ");
                                        //sbQuery.Append(" LEFT OUTER JOIN  AdvanceRepayment ON AdvanceRepayment.AdvanceID = SalaryAdvance.ID ");
                                        //sbQuery.Append(" LEFT OUTER JOIN payslip on payslip.id=advancerepayment.payslipid  AND PaySlip.Period<@Period ");
                                        //sbQuery.Append(" WHERE SalaryAdvance.EmployeeID=@EmployeeID AND SalaryAdvance.AdvanceDate<@Period ");
                                        //sbQuery.Append(" group by SalaryAdvance.ID, SalaryAdvance.TransType, SalaryAdvance.Amount , SalaryAdvance.NoOfInstallments ");
                                        //sbQuery.Append(" having SalaryAdvance.Amount - ISNULL(SUM(AdvanceRepayment.Amount),0) >0");
                                        sbQuery = new StringBuilder();
                                        //sbQuery.Append(" SELECT SalaryAdvance.ID, SalaryAdvance.TransType, CAST((SalaryAdvance.Amount/SalaryAdvance.NoOfInstallments) AS Numeric(18,2)) AS Installment, ");
                                        //sbQuery.Append(" SalaryAdvance.Amount as Advance ");
                                        //sbQuery.Append(" FROM SalaryAdvance ");
                                        //sbQuery.Append(" WHERE SalaryAdvance.EmployeeID=@EmployeeID AND Month(SalaryAdvance.AdvanceDate)= Month(@Period) AND Year(SalaryAdvance.AdvanceDate)= Year(@Period) ");

                                        sbQuery.Append(" SELECT SalaryAdvance.ID, SalaryAdvance.TransType, CAST((SalaryAdvance.Amount/SalaryAdvance.NoOfInstallments) AS Numeric(18,2)) AS Installment, ");
                                        sbQuery.Append(" SalaryAdvance.Amount as Advance , ISNULL(Repayment.Payment,0) AS Payment ");
                                        sbQuery.Append(" FROM SalaryAdvance ");
                                        sbQuery.Append(" LEFT OUTER JOIN ( SELECT AdvanceRepayment.AdvanceID , ISNULL(SUM(AdvanceRepayment.Amount),0) as Payment ");
                                        sbQuery.Append(" FROM AdvanceRepayment ");
                                        sbQuery.Append(" INNER JOIN Payslip ON AdvanceRepayment.PaySlipID = PaySlip.ID ");
                                        sbQuery.Append(" INNER JOIN SalaryAdvance ON AdvanceRepayment.AdvanceID = SalaryAdvance.ID ");
                                        sbQuery.Append(" WHERE PaySlip.Period<>@Period AND SalaryAdvance.EmployeeID=@EmployeeID ");
                                        sbQuery.Append(" GROUP BY AdvanceRepayment.AdvanceID ) Repayment ON SalaryAdvance.ID = Repayment.AdvanceID  ");
                                        sbQuery.Append(" WHERE SalaryAdvance.EmployeeID=@EmployeeID AND SalaryAdvance.IsDeleted = 0 AND SalaryAdvance.AdvanceDate<= @Period AND SalaryAdvance.Amount-ISNULL(Repayment.Payment,0)>0 ");

                                        SqlCommand cmdAdvance = new SqlCommand();
                                        cmdAdvance.CommandText = sbQuery.ToString();
                                        cmdAdvance.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        cmdAdvance.Parameters.AddWithValue("@Period", Period);

                                        decimal DailyAdvance = 0;
                                        decimal MonthlyAdvance = 0;
                                        decimal SpecialAdvance = 0;
                                        decimal Loan = 0;
                                        decimal UniformIssue = 0;

                                        SQLDataAccess sdaAdvance = new SQLDataAccess(_configuration);
                                        sdaAdvance.ExecuteSQL("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED");
                                        using (SqlDataReader drAdvance = sdaAdvance.RetrieveData(cmdAdvance))
                                        {

                                            while (drAdvance.Read())
                                            {

                                                if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) < drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")))
                                                {
                                                    switch (drAdvance.GetInt32(drAdvance.GetOrdinal("TransType")))
                                                    {
                                                        case 1:
                                                            if (drEmployee["EMPPAY_DATE_RESIGNED"] != DBNull.Value)
                                                            {
                                                                DateTime dtResignedDate = drEmployee.GetDateTime(drEmployee.GetOrdinal("EMPPAY_DATE_RESIGNED"));
                                                                if ((dtResignedDate.Month == Period.Month) && (dtResignedDate.Year == Period.Year))
                                                                    DailyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                else
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                    {
                                                                        if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                        {
                                                                            DailyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                        }
                                                                        else
                                                                            DailyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                    }
                                                                    else
                                                                        DailyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                    {
                                                                        DailyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                    }
                                                                    else
                                                                        DailyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                }
                                                                else
                                                                    DailyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                            }
                                                            break;
                                                        case 2:
                                                            if (drEmployee["EMPPAY_DATE_RESIGNED"] != DBNull.Value)
                                                            {
                                                                DateTime dtResignedDate = drEmployee.GetDateTime(drEmployee.GetOrdinal("EMPPAY_DATE_RESIGNED"));
                                                                if ((dtResignedDate.Month == Period.Month) && (dtResignedDate.Year == Period.Year))
                                                                    MonthlyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                else
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                    {
                                                                        if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                        {
                                                                            MonthlyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                        }
                                                                        else
                                                                            MonthlyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                    }
                                                                    else
                                                                        MonthlyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                    {
                                                                        MonthlyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                    }
                                                                    else
                                                                        MonthlyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                }
                                                                else
                                                                    MonthlyAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                            }
                                                            break;
                                                        case 3:
                                                            if (drEmployee["EMPPAY_DATE_RESIGNED"] != DBNull.Value)
                                                            {
                                                                DateTime dtResignedDate = drEmployee.GetDateTime(drEmployee.GetOrdinal("EMPPAY_DATE_RESIGNED"));
                                                                if ((dtResignedDate.Month == Period.Month) && (dtResignedDate.Year == Period.Year))
                                                                    Loan += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                else
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                    {
                                                                        if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                        {
                                                                            Loan += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                        }
                                                                        else
                                                                            Loan += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                    }
                                                                    else
                                                                        Loan += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                    {
                                                                        Loan += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                    }
                                                                    else
                                                                        Loan += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                }
                                                                else
                                                                    Loan += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                            }
                                                            break;
                                                        case 4:
                                                            if (drEmployee["EMPPAY_DATE_RESIGNED"] != DBNull.Value)
                                                            {
                                                                DateTime dtResignedDate = drEmployee.GetDateTime(drEmployee.GetOrdinal("EMPPAY_DATE_RESIGNED"));
                                                                if ((dtResignedDate.Month == Period.Month) && (dtResignedDate.Year == Period.Year))
                                                                    UniformIssue += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                else
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                    {
                                                                        if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                        {
                                                                            UniformIssue += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                        }
                                                                        else
                                                                            UniformIssue += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                    }
                                                                    else
                                                                        UniformIssue += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                    {
                                                                        UniformIssue += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                    }
                                                                    else
                                                                        UniformIssue += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                }
                                                                else
                                                                    UniformIssue += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                            }
                                                            break;
                                                        case 5:
                                                            if (drEmployee["EMPPAY_DATE_RESIGNED"] != DBNull.Value)
                                                            {
                                                                DateTime dtResignedDate = drEmployee.GetDateTime(drEmployee.GetOrdinal("EMPPAY_DATE_RESIGNED"));
                                                                if ((dtResignedDate.Month == Period.Month) && (dtResignedDate.Year == Period.Year))
                                                                    SpecialAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                else
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                    {
                                                                        if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                        {
                                                                            SpecialAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                        }
                                                                        else
                                                                            SpecialAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                    }
                                                                    else
                                                                        SpecialAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) > drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")))
                                                                {
                                                                    if (drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment")) < (decimal)0.1)
                                                                    {
                                                                        SpecialAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                                    }
                                                                    else
                                                                        SpecialAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Installment"));
                                                                }
                                                                else
                                                                    SpecialAdvance += drAdvance.GetDecimal(drAdvance.GetOrdinal("Advance")) - drAdvance.GetDecimal(drAdvance.GetOrdinal("Payment"));
                                                            }
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                        cmdAdvance.Dispose();
                                        sdaAdvance.Dispose();
                                        decimal dEPFEmployee = 0;
                                        decimal dEPFEmployer = 0;

                                        decimal dGrossSalary = 0;
                                        dGrossSalary = BasicSalary + OverTimeSalary + OffdaySalary + OffdayOTSalary + HolidaySalary + HolidayOTSalary + dReAllowance + dAttendanceAllowance + dSpecialAllowance + dMiscEarnings + dBonus;

                                        // EPF deduction is controlled by the EPFDETECT flag in EmployeeSalaryDetails.
                                        // For Nepal employees, a flat 2% EPF rate is applied.
                                        if (DeductEPF)
                                        {
                                            if (sEmpNational.Equals("Nepal", StringComparison.OrdinalIgnoreCase))
                                            {
                                                dEPFEmployee = Math.Ceiling(dGrossSalary * 0.02M);
                                                dEPFEmployer = Math.Ceiling(dGrossSalary * 0.02M);
                                            }
                                            else
                                            {
                                                sbQuery = new StringBuilder();
                                                sbQuery.Append("select epf_worker,epf_boss,epf_worker55,epf_boss55 from EPF  where (epf_from)<=@BasicSalary and (epf_to)>=@BasicSalary");
                                                SqlCommand cmdEPF = new SqlCommand();
                                                cmdEPF.CommandText = sbQuery.ToString();
                                                cmdEPF.Parameters.AddWithValue("@BasicSalary", double.Parse(BasicSalary.ToString("N2")));

                                                SQLDataAccess sdaEPF = new SQLDataAccess(_configuration);
                                                SqlDataReader drEPF = sdaEPF.RetrieveData(cmdEPF);

                                                while (drEPF.Read())
                                                {
                                                    if (EmployeeAge > 59)//55 -->59 [Started on NOV 2013]
                                                    {
                                                        if (DeductEPFBeyond55)
                                                        {
                                                            //1 Mar 2020 direct deduct 4% from Employee above 60
                                                            decimal EPFRate60 = 0.04M;
                                                            dEPFEmployee = (Decimal)0.00;
                                                            dEPFEmployer = BasicSalary * EPFRate60;
                                                            dEPFEmployer = Math.Round(dEPFEmployer);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        dEPFEmployee = drEPF.GetDecimal(drEPF.GetOrdinal("epf_worker"));
                                                        dEPFEmployer = drEPF.GetDecimal(drEPF.GetOrdinal("epf_boss"));
                                                    }
                                                }
                                                drEPF.Close();
                                                drEPF.Dispose();
                                                cmdEPF.Dispose();
                                                sdaEPF.Dispose();
                                            }
                                        }
                                        decimal dSOCSOEmployee = 0;
                                        decimal dSOCSOEmployer = 0;
                                        decimal dSKBBK = 0; // SKBBK deduction (socso_foreigner2)
                                        decimal NetPay = 0;
                                        // SOCSO deduction is controlled entirely by the SOCSODETECT flag in EmployeeSalaryDetails.
                                        // This applies uniformly to all employees regardless of nationality or passport.
                                        if (DeductSOCSO)
                                        {
                                            // Calculate gross income for SOCSO bracket lookup
                                            NetPay = BasicSalary + OffdaySalary + OverTimeSalary + OffdayOTSalary;
                                            NetPay += HolidayOTSalary + HolidaySalary + dReAllowance + dAttendanceAllowance + dBonus + dSpecialAllowance + dMiscEarnings;

                                            sbQuery = new StringBuilder();
                                            sbQuery.Append("select top 1 socso_employer,socso_worker,socso_50year,socso_total,socso_foreigner,socso_foreigner2,socso_foreigner2_worker,SocsoSKBBK,Socsonormal from SOCSO  where socso_to >= @NetPay and socso_from <=@NetPay");
                                            SqlCommand cmdSOCSO = new SqlCommand();
                                            cmdSOCSO.CommandText = sbQuery.ToString();
                                            cmdSOCSO.Parameters.AddWithValue("@NetPay", NetPay);
                                            SQLDataAccess sdaSOCSO = new SQLDataAccess(_configuration);
                                            SqlDataReader drSOCSO = sdaSOCSO.RetrieveData(cmdSOCSO);

                                            while (drSOCSO.Read())
                                            {
                                                if (EmployeeAge > 59) //55 -->59 [Started on Jan 2013]
                                                {
                                                    dSOCSOEmployer = drSOCSO.GetDecimal(drSOCSO.GetOrdinal("socso_50year"));
                                                }
                                                else
                                                {
                                                    // Use Socsonormal as the correct SOCSO worker rate
                                                    // socso_worker in UAT DB may contain incorrect combined values
                                                    int socsoNormalOrdinal = -1;
                                                    try { socsoNormalOrdinal = drSOCSO.GetOrdinal("Socsonormal"); } catch { }
                                                    if (socsoNormalOrdinal >= 0 && !drSOCSO.IsDBNull(socsoNormalOrdinal))
                                                        dSOCSOEmployee = drSOCSO.GetDecimal(socsoNormalOrdinal);
                                                    else
                                                        dSOCSOEmployee = drSOCSO.GetDecimal(drSOCSO.GetOrdinal("socso_worker"));
                                                    dSOCSOEmployer = drSOCSO.GetDecimal(drSOCSO.GetOrdinal("socso_employer"));
                                                }
                                                // SKBBK: SocsoSKBBK rate from SOCSO table (only if DeductSKBBK = true)
                                                if (DeductSKBBK && !drSOCSO.IsDBNull(drSOCSO.GetOrdinal("SocsoSKBBK")))
                                                    dSKBBK = drSOCSO.GetDecimal(drSOCSO.GetOrdinal("SocsoSKBBK"));
                                            }
                                            drSOCSO.Close();
                                            drSOCSO.Dispose();
                                            cmdSOCSO.Dispose();
                                            sdaSOCSO.Dispose();
                                        }

                                        //SIP
                                        decimal dSIPEmployee = 0;
                                        decimal dSIPEmployer = 0;
                                        decimal dGrossIncomeSIP = 0;

                                        if (dSOCSOEmployee != 0)
                                        {
                                            if (CompanyCode.ToString().Trim() == "EASTWEST")
                                            {
                                                dGrossIncomeSIP = BasicSalary + OverTimeSalary + OffdaySalary + OffdayOTSalary + HolidaySalary + HolidayOTSalary + dReAllowance + dAttendanceAllowance + dSpecialAllowance + dMiscEarnings + Shift2Amount;
                                            }
                                            //2/11/2019 Aura to follow other company 
                                            //else if (CompanyCode.ToString().Trim() == "AURA")
                                            //{
                                            //    if (EmployeeType == "Staff")
                                            //    {
                                            //        dGrossIncomeSIP = BasicSalary + OverTimeSalary + OffdaySalary + OffdayOTSalary + HolidaySalary + HolidayOTSalary + dReAllowance + dAttendanceAllowance + dSpecialAllowance + dMiscEarnings;
                                            //    }
                                            //    else
                                            //    {
                                            //        dGrossIncomeSIP = BasicSalary + OverTimeSalary + OffdaySalary + OffdayOTSalary + HolidaySalary + HolidayOTSalary + dReAllowance + dAttendanceAllowance + dSpecialAllowance + dMiscEarnings;
                                            //    }

                                            //}
                                            else if (CompanyCode.ToString().Trim() == "FWG")
                                            {
                                                if (EmployeeType == "Staff")
                                                {
                                                    dGrossIncomeSIP = BasicSalary + OverTimeSalary + OffdaySalary + OffdayOTSalary + HolidaySalary + HolidayOTSalary + dReAllowance + dAttendanceAllowance + dSpecialAllowance + dMiscEarnings;
                                                }
                                                else
                                                {
                                                    //commented the code below as reported by Amuta that the salary processing for July is wrong on 2019 Aug 06
                                                    //dGrossIncomeSIP = BasicSalary + OverTimeSalary + HolidaySalary + HolidayOTSalary + dReAllowance + dAttendanceAllowance + dSpecialAllowance;
                                                    //Version 1.0 Change the new payslip for Guard which to calculate the Socso by Basic only

                                                    // 06/11/2019 rollback to previous version as Oct Salary having issue in Prod
                                                    //dGrossIncomeSIP = BasicSalary;
                                                    if (EmployeeID.ToString() == "27757" || EmployeeID.ToString() == "27885" || EmployeeID.ToString() == "28015")
                                                    {
                                                        dGrossIncomeSIP = 0;
                                                    }
                                                    else
                                                    {
                                                        dGrossIncomeSIP = BasicSalary + OverTimeSalary + OffdaySalary + OffdayOTSalary + HolidaySalary + HolidayOTSalary + dReAllowance + dAttendanceAllowance + dSpecialAllowance + dMiscEarnings + dBonus;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                dGrossIncomeSIP = BasicSalary + OverTimeSalary + OffdaySalary + OffdayOTSalary + HolidaySalary + HolidayOTSalary + dReAllowance + dAttendanceAllowance + dSpecialAllowance + dMiscEarnings + dBonus;
                                            }
                                            if (strCitizen == 0 && EmployeeAge < 60)
                                            {
                                                sbQuery = new StringBuilder();
                                                sbQuery.Append("select * from SIP where (SIP_from)<=@GrossSalary and (SIP_to)>=@GrossSalary");
                                                SqlCommand cmdSIP = new SqlCommand();
                                                cmdSIP.CommandText = sbQuery.ToString();
                                                cmdSIP.Parameters.AddWithValue("@GrossSalary", double.Parse(dGrossIncomeSIP.ToString("N2")));

                                                SQLDataAccess sdaSIP = new SQLDataAccess(_configuration);
                                                SqlDataReader drSIP = sdaSIP.RetrieveData(cmdSIP);

                                                while (drSIP.Read())
                                                {
                                                    dSIPEmployee = drSIP.GetDecimal(drSIP.GetOrdinal("SIP_worker"));
                                                    dSIPEmployer = drSIP.GetDecimal(drSIP.GetOrdinal("SIP_boss"));

                                                }
                                                drSIP.Close();
                                                drSIP.Dispose();
                                                cmdSIP.Dispose();
                                                sdaSIP.Dispose();
                                            }
                                        }
                                        //IncomeTax - portion
                                        decimal dIncomeTaxEmployee = 0;
                                        decimal dIncomeTaxSalaryCalc = 0;
                                        if (DeductIncomeTax)
                                        {

                                            sbQuery = new StringBuilder();
                                            sbQuery.Append("select [IT_Category1] from IncomeTax  where [IT_SAL_TO] >= @BasicSalary and [IT_SAL_FROM] <=@BasicSalary");
                                            SqlCommand cmdIncomeTax = new SqlCommand();
                                            cmdIncomeTax.CommandText = sbQuery.ToString();
                                            //dReAllowance + dAttendanceAllowance + dBonus + dSpecialAllowance
                                            dIncomeTaxSalaryCalc = BasicSalary + dAttendanceAllowance - dEPFEmployee;
                                            cmdIncomeTax.Parameters.AddWithValue("@BasicSalary", double.Parse(dIncomeTaxSalaryCalc.ToString("N2")));
                                            SQLDataAccess sdaIncomeTax = new SQLDataAccess(_configuration);
                                            SqlDataReader drIncomeTax = sdaIncomeTax.RetrieveData(cmdIncomeTax);

                                            while (drIncomeTax.Read())
                                            {
                                                //dIncomeTaxEmployee = drIncomeTax.GetDecimal(drIncomeTax.GetOrdinal("IT_Category1"));
                                                dIncomeTaxEmployee = decimal.Parse(drIncomeTax["IT_Category1"].ToString());
                                            }
                                            drIncomeTax.Close();
                                            drIncomeTax.Dispose();
                                            cmdIncomeTax.Dispose();
                                            sdaIncomeTax.Dispose();
                                        }

                                        SqlCommand cmdAdvanceRepay = new SqlCommand("DELETE FROM AdvanceRepayment WHERE PaySlipID = (SELECT ID FROM PaySlip WHERE Period=@Period AND EmployeeID=@EmployeeID)");
                                        cmdAdvanceRepay.Parameters.AddWithValue("@Period", Period);
                                        cmdAdvanceRepay.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        sdaPaySlip.ExecuteSQL(cmdAdvanceRepay);
                                        cmdAdvanceRepay.Dispose();

                                        SqlCommand cmdPaySlipDel = new SqlCommand("DELETE FROM PaySlip WHERE Period=@Period AND EmployeeID=@EmployeeID");
                                        cmdPaySlipDel.Parameters.AddWithValue("@Period", Period);
                                        cmdPaySlipDel.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        sdaPaySlip.ExecuteSQL(cmdPaySlipDel);
                                        cmdPaySlipDel.Dispose();

                                        //Commented by Kean Hong on 26 Apr 2020 to deduct the Gross Salary from Staff until it has been informed to remove 
                                        ////if (CompanyCode.ToString().Trim() == "FWG" && EmployeeType == "Staff")
                                        ////{
                                        ////    NetPay = BasicSalary + OffdaySalary + OverTimeSalary + OffdayOTSalary;
                                        ////    NetPay += HolidayOTSalary + HolidaySalary + dReAllowance + dAttendanceAllowance + dBonus + dSpecialAllowance;
                                        ////    if (EmployeeID == 17755 || EmployeeID == 4013 || EmployeeID == 20484)
                                        ////    {
                                        ////    }
                                        ////    else if (EmployeeID == 11487)
                                        ////    {
                                        ////        SpecialAdvance += (NetPay * 0.10M);
                                        ////        dMiscDeductions = dMiscDeductions + SpecialAdvance;
                                        ////    }
                                        ////    else
                                        ////    {
                                        ////        SpecialAdvance += (NetPay * 0.07M);
                                        ////        dMiscDeductions = dMiscDeductions + SpecialAdvance;
                                        ////    }

                                        ////}

                                        using (SqlCommand cmdPaySlip = new SqlCommand())
                                        {
                                            sbQuery = new StringBuilder();
                                            sbQuery.Append(" INSERT INTO PaySlip (Period, EmployeeID,BasicSalaryDays,BasicSalaryRate, BasicSalary, ");
                                            //sbQuery.Append(" INSERT INTO PaySlip (OverTimeSalaryHours,OverTimeSalaryRate,OverTimeSalary, OffDaySalaryDays, ");
                                            sbQuery.Append(" OverTimeSalaryHours,OverTimeSalaryRate,OverTimeSalary, OffDaySalaryDays, ");
                                            sbQuery.Append(" OffDaySalaryRate,OffDaySalary, OffDayOverTimeSalaryHours,OffDayOverTimeSalaryRate,OffDayOverTimeSalary, ");
                                            sbQuery.Append(" HolidaySalaryDays,HolidaySalaryRate,HolidaySalary, HolidayOverTimeSalaryHours,HolidayOverTimeSalaryRate, ");
                                            sbQuery.Append(" HolidayOverTimeSalary,Shift2SalaryDaysHours,Shift2SalaryRateType,Shift2SalaryRate,Shift2Salary, ");
                                            sbQuery.Append(" ReAllowanceDays,ReAllowance,AttendanceAllowance,SpecialAllowance, EPFDeductionAmount, SOCSODeductionAmount, ");
                                            sbQuery.Append(" EPFEmployerContribution, SOCSOEmployerContribution, ");
                                            sbQuery.Append(" DailyAdvanceRecovery, MonthlyAdvanceRecovery,SpecialAdvanceRecovery, UniformIssueRecovery, LoanRecovery,IncomeTaxDeduction, ");
                                            sbQuery.Append(" MiscAmount, MiscDeduction,Bonus,SalaryPayMode, ");
                                            sbQuery.Append(" LastUpdate,LastUpdatedBy,SIPEmployeeContribution,SIPEmployerContribution,SKBBKContribution) VALUES (@Period, @EmployeeID, @BasicSalaryDays,@BasicSalaryRate,@BasicSalary, ");
                                            sbQuery.Append(" @OverTimeSalaryHours,@OverTimeSalaryRate,@OverTimeSalary,@OffDaySalaryDays,@OffDaySalaryRate,@OffDaySalary, ");
                                            sbQuery.Append(" @OffDayOverTimeSalaryHours,@OffDayOverTimeSalaryRate,@OffDayOverTimeSalary,@HolidaySalaryDays, ");
                                            sbQuery.Append(" @HolidaySalaryRate,@HolidaySalary,@HolidayOverTimeSalaryHours,@HolidayOverTimeSalaryRate, ");
                                            sbQuery.Append(" @HolidayOverTimeSalary,@Shift2SalaryDaysHours,@Shift2SalaryRateType,@Shift2SalaryRate,@Shift2Salary, ");
                                            sbQuery.Append(" @ReAllowanceDays,@ReAllowance,@AttendanceAllowance,@SpecialAllowance, @EPFDeductionAmount, @SOCSODeductionAmount, ");
                                            sbQuery.Append(" @EPFEmployerContribution, @SOCSOEmployerContribution, ");
                                            sbQuery.Append(" @DailyAdvanceRecovery, @MonthlyAdvanceRecovery,@SpecialAdvanceRecovery, @UniformIssueRecovery, @LoanRecovery, @IncomeTaxDeduction, ");
                                            sbQuery.Append(" @MiscAmount, @MiscDeduction,@Bonus, @SalaryPayMode, ");
                                            sbQuery.Append(" @LASTUPDATE,@LastUpdatedBy,@SIPEmployeeContribution,@SIPEmployerContribution,@SKBBKContribution); SELECT SCOPE_IDENTITY()");
                                            cmdPaySlip.CommandText = sbQuery.ToString();
                                            cmdPaySlip.Parameters.AddWithValue("@Period", Period);
                                            cmdPaySlip.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                            cmdPaySlip.Parameters.AddWithValue("@BasicSalaryDays", BasicSalaryDays);
                                            cmdPaySlip.Parameters.AddWithValue("@BasicSalaryRate", BasicSalaryRate);
                                            cmdPaySlip.Parameters.AddWithValue("@BasicSalary", BasicSalary);
                                            cmdPaySlip.Parameters.AddWithValue("@OverTimeSalaryHours", OverTimeSalaryHours);
                                            cmdPaySlip.Parameters.AddWithValue("@OverTimeSalaryRate", OverTimeSalaryRate);
                                            cmdPaySlip.Parameters.AddWithValue("@OverTimeSalary", OverTimeSalary);
                                            cmdPaySlip.Parameters.AddWithValue("@OffDaySalaryDays", OffdaySalaryDays);
                                            cmdPaySlip.Parameters.AddWithValue("@OffDaySalaryRate", OffdaySalaryRate);
                                            cmdPaySlip.Parameters.AddWithValue("@OffDaySalary", OffdaySalary);
                                            cmdPaySlip.Parameters.AddWithValue("@OffDayOverTimeSalaryHours", OffdayOTSalaryHours);
                                            cmdPaySlip.Parameters.AddWithValue("@OffDayOverTimeSalaryRate", OffdayOTSalaryRate);
                                            cmdPaySlip.Parameters.AddWithValue("@OffDayOverTimeSalary", OffdayOTSalary);
                                            cmdPaySlip.Parameters.AddWithValue("@HolidaySalaryDays", HolidaySalaryDays);
                                            cmdPaySlip.Parameters.AddWithValue("@HolidaySalaryRate", HolidaySalaryRate);
                                            cmdPaySlip.Parameters.AddWithValue("@HolidaySalary", HolidaySalary);
                                            cmdPaySlip.Parameters.AddWithValue("@HolidayOverTimeSalaryHours", HolidayOTSalaryHours);
                                            cmdPaySlip.Parameters.AddWithValue("@HolidayOverTimeSalaryRate", HolidayOTSalaryRate);
                                            cmdPaySlip.Parameters.AddWithValue("@HolidayOverTimeSalary", HolidayOTSalary);
                                            cmdPaySlip.Parameters.AddWithValue("@Shift2SalaryDaysHours", Shift2days);
                                            cmdPaySlip.Parameters.AddWithValue("@Shift2SalaryRateType", Shift2type);
                                            cmdPaySlip.Parameters.AddWithValue("@Shift2SalaryRate", Shift2rate);
                                            cmdPaySlip.Parameters.AddWithValue("@Shift2Salary", Shift2Amount);
                                            cmdPaySlip.Parameters.AddWithValue("@ReAllowanceDays", ReAllowanceDays);
                                            cmdPaySlip.Parameters.AddWithValue("@ReAllowance", dReAllowance);
                                            cmdPaySlip.Parameters.AddWithValue("@AttendanceAllowance", dAttendanceAllowance);
                                            cmdPaySlip.Parameters.AddWithValue("@SpecialAllowance", (dSpecialAllowance));
                                            cmdPaySlip.Parameters.AddWithValue("@EPFDeductionAmount", dEPFEmployee);
                                            cmdPaySlip.Parameters.AddWithValue("@SOCSODeductionAmount", dSOCSOEmployee);
                                            cmdPaySlip.Parameters.AddWithValue("@EPFEmployerContribution", dEPFEmployer);
                                            cmdPaySlip.Parameters.AddWithValue("@SOCSOEmployerContribution", dSOCSOEmployer);
                                            cmdPaySlip.Parameters.AddWithValue("@DailyAdvanceRecovery", DailyAdvance);
                                            cmdPaySlip.Parameters.AddWithValue("@MonthlyAdvanceRecovery", MonthlyAdvance);
                                            cmdPaySlip.Parameters.AddWithValue("@SpecialAdvanceRecovery", SpecialAdvance);
                                            cmdPaySlip.Parameters.AddWithValue("@UniformIssueRecovery", UniformIssue);
                                            cmdPaySlip.Parameters.AddWithValue("@LoanRecovery", Loan);
                                            //cmdPaySlip.Parameters.AddWithValue("@IncomeTaxDeduction", 0); //IncomeTax dIncomeTaxEmployee
                                            cmdPaySlip.Parameters.AddWithValue("@IncomeTaxDeduction", dIncomeTaxEmployee); //IncomeTax 
                                            cmdPaySlip.Parameters.AddWithValue("@MiscAmount", dMiscEarnings);
                                            cmdPaySlip.Parameters.AddWithValue("@MiscDeduction", dMiscDeductions);
                                            cmdPaySlip.Parameters.AddWithValue("@Bonus", dBonus);//dBonus
                                            cmdPaySlip.Parameters.AddWithValue("@SalaryPayMode", SalaryPayMode);
                                            cmdPaySlip.Parameters.AddWithValue("@LastUpdate", DateTime.Now);
                                            cmdPaySlip.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);
                                            cmdPaySlip.Parameters.AddWithValue("@SIPEmployeeContribution", dSIPEmployee);
                                            cmdPaySlip.Parameters.AddWithValue("@SIPEmployerContribution", dSIPEmployer);
                                            cmdPaySlip.Parameters.AddWithValue("@SKBBKContribution", dSKBBK);
                                            SqlDataReader drTemp = sdaPaySlip.RetrieveData(cmdPaySlip);
                                            drTemp.Read();
                                            decimal dPaySlipID = drTemp.GetDecimal(0);
                                            drTemp.Close();
                                            sbQuery = new StringBuilder();
                                            sbQuery.Append("INSERT INTO AdvanceRepayment (AdvanceID,PaySlipID,Amount,LastUpdate) ");
                                            sbQuery.Append("SELECT ID,@PaySlipID, CASE WHEN Advance-Payment>Installment THEN Installment ELSE Advance-Payment END as CurrentPayment, @LastUpdate ");
                                            sbQuery.Append("FROM ");
                                            sbQuery.Append(" (SELECT SalaryAdvance.ID, CASE WHEN (MONTH(ISNULL(EmploymentDetails.EMPPAY_DATE_RESIGNED,CAST('2100-01-01' as DATETIME)))=MONTH(@Period) AND YEAR(ISNULL(EmploymentDetails.EMPPAY_DATE_RESIGNED,CAST('2100-01-01' as DATETIME)))=YEAR(@Period)) THEN SUM(SalaryAdvance.Amount)-ISNULL(SUM(paymentdetails.payment),0) ELSE CAST(SUM(SalaryAdvance.Amount/SalaryAdvance.NoOfInstallments) AS Numeric(18,2)) END AS Installment, ");
                                            sbQuery.Append(" SUM(SalaryAdvance.Amount) AS Advance,ISNULL(SUM(paymentdetails.payment),0) as Payment ");
                                            sbQuery.Append(" FROM SalaryAdvance  ");
                                            sbQuery.Append(" LEFT OUTER JOIN (select AdvanceID,Sum(advancerepayment.Amount) as Payment  ");
                                            sbQuery.Append(" from advancerepayment left outer join payslip on payslip.id=advancerepayment.payslipid where advancerepayment.advanceid in (select id  from SalaryAdvance where SalaryAdvance.EmployeeID=@EmployeeID AND SalaryAdvance.AdvanceDate<=@Period) and payslip.period<@Period group by advanceid) PaymentDetails  ON PaymentDetails.AdvanceID = SalaryAdvance.ID ");
                                            sbQuery.Append(" INNER JOIN Employee ON Employee.Emp_ID = @EmployeeID ");
                                            sbQuery.Append(" INNER JOIN EmploymentDetails ON EmploymentDetails.EmpPay_Code = Employee.Emp_Code ");
                                            sbQuery.Append(" WHERE SalaryAdvance.EmployeeID=@EmployeeID AND SalaryAdvance.AdvanceDate<=@Period AND SalaryAdvance.IsDeleted = 0 ");
                                            sbQuery.Append(" group by SalaryAdvance.ID, SalaryAdvance.TransType, EmploymentDetails.EMPPAY_DATE_RESIGNED ");
                                            sbQuery.Append(" having SUM(SalaryAdvance.Amount) - ISNULL(SUM(paymentdetails.payment),0) >0) PaymentDetail ");

                                            SqlCommand cmdRepayment = new SqlCommand();
                                            cmdRepayment.CommandText = sbQuery.ToString();
                                            cmdRepayment.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                            cmdRepayment.Parameters.AddWithValue("@PaySlipID", dPaySlipID);
                                            cmdRepayment.Parameters.AddWithValue("@Period", Period);
                                            cmdRepayment.Parameters.AddWithValue("@LastUpdate", DateTime.Now);
                                            sdaPaySlip.ExecuteSQL(cmdRepayment);
                                        }

                                        //Insert into PayslipAudit
                                        sbQuery = new StringBuilder();
                                        sbQuery.Append(" INSERT INTO PaySlipAudit (Period, EmployeeID,BasicSalaryDays,BasicSalaryRate, BasicSalary, ");
                                        sbQuery.Append(" OverTimeSalaryHours,OverTimeSalaryRate,OverTimeSalary, OffDaySalaryDays, ");
                                        sbQuery.Append(" OffDaySalaryRate,OffDaySalary, OffDayOverTimeSalaryHours,OffDayOverTimeSalaryRate,OffDayOverTimeSalary, ");
                                        sbQuery.Append(" HolidaySalaryDays,HolidaySalaryRate,HolidaySalary, HolidayOverTimeSalaryHours,HolidayOverTimeSalaryRate, ");
                                        sbQuery.Append(" HolidayOverTimeSalary,Shift2SalaryDaysHours,Shift2SalaryRateType,Shift2SalaryRate,Shift2Salary, ");
                                        sbQuery.Append(" ReAllowanceDays,ReAllowance,AttendanceAllowance,SpecialAllowance, EPFDeductionAmount, SOCSODeductionAmount, ");
                                        sbQuery.Append(" EPFEmployerContribution, SOCSOEmployerContribution, ");
                                        sbQuery.Append(" DailyAdvanceRecovery, MonthlyAdvanceRecovery,SpecialAdvanceRecovery, UniformIssueRecovery, LoanRecovery,IncomeTaxDeduction, ");
                                        sbQuery.Append(" MiscAmount, MiscDeduction,Bonus,SalaryPayMode, ");
                                        sbQuery.Append(" LastUpdate,LastUpdatedBy,Version,SIPEmployeeContribution,SIPEmployerContribution,SKBBKContribution) VALUES (@Period, @EmployeeID, @BasicSalaryDays,@BasicSalaryRate,@BasicSalary, ");
                                        sbQuery.Append(" @OverTimeSalaryHours,@OverTimeSalaryRate,@OverTimeSalary,@OffDaySalaryDays,@OffDaySalaryRate,@OffDaySalary, ");
                                        sbQuery.Append(" @OffDayOverTimeSalaryHours,@OffDayOverTimeSalaryRate,@OffDayOverTimeSalary,@HolidaySalaryDays, ");
                                        sbQuery.Append(" @HolidaySalaryRate,@HolidaySalary,@HolidayOverTimeSalaryHours,@HolidayOverTimeSalaryRate, ");
                                        sbQuery.Append(" @HolidayOverTimeSalary,@Shift2SalaryDaysHours,@Shift2SalaryRateType,@Shift2SalaryRate,@Shift2Salary, ");
                                        sbQuery.Append(" @ReAllowanceDays,@ReAllowance,@AttendanceAllowance,@SpecialAllowance, @EPFDeductionAmount, @SOCSODeductionAmount, ");
                                        sbQuery.Append(" @EPFEmployerContribution, @SOCSOEmployerContribution, ");
                                        sbQuery.Append(" @DailyAdvanceRecovery, @MonthlyAdvanceRecovery,@SpecialAdvanceRecovery, @UniformIssueRecovery, @LoanRecovery, @IncomeTaxDeduction, ");
                                        sbQuery.Append(" @MiscAmount, @MiscDeduction,@Bonus, @SalaryPayMode, ");
                                        sbQuery.Append(" @LASTUPDATE,@LastUpdatedBy,@Version,@SIPEmployeeContribution,@SIPEmployerContribution,@SKBBKContribution)");

                                        SqlCommand cmdPaySlipAudit = new SqlCommand();
                                        cmdPaySlipAudit.CommandText = sbQuery.ToString();
                                        cmdPaySlipAudit.Parameters.AddWithValue("@Period", Period);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@BasicSalaryDays", BasicSalaryDays);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@BasicSalaryRate", BasicSalaryRate);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@BasicSalary", BasicSalary);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@OverTimeSalaryHours", OverTimeSalaryHours);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@OverTimeSalaryRate", OverTimeSalaryRate);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@OverTimeSalary", OverTimeSalary);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@OffDaySalaryDays", OffdaySalaryDays);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@OffDaySalaryRate", OffdaySalaryRate);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@OffDaySalary", OffdaySalary);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@OffDayOverTimeSalaryHours", OffdayOTSalaryHours);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@OffDayOverTimeSalaryRate", OffdayOTSalaryRate);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@OffDayOverTimeSalary", OffdayOTSalary);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@HolidaySalaryDays", HolidaySalaryDays);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@HolidaySalaryRate", HolidaySalaryRate);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@HolidaySalary", HolidaySalary);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@HolidayOverTimeSalaryHours", HolidayOTSalaryHours);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@HolidayOverTimeSalaryRate", HolidayOTSalaryRate);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@HolidayOverTimeSalary", HolidayOTSalary);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@Shift2SalaryDaysHours", Shift2days);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@Shift2SalaryRateType", Shift2type);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@Shift2SalaryRate", Shift2rate);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@Shift2Salary", Shift2Amount);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@ReAllowanceDays", ReAllowanceDays);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@ReAllowance", dReAllowance);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@AttendanceAllowance", dAttendanceAllowance);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@SpecialAllowance", (dSpecialAllowance));
                                        cmdPaySlipAudit.Parameters.AddWithValue("@EPFDeductionAmount", dEPFEmployee);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@SOCSODeductionAmount", dSOCSOEmployee);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@EPFEmployerContribution", dEPFEmployer);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@SOCSOEmployerContribution", dSOCSOEmployer);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@DailyAdvanceRecovery", DailyAdvance);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@MonthlyAdvanceRecovery", MonthlyAdvance);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@SpecialAdvanceRecovery", SpecialAdvance);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@UniformIssueRecovery", UniformIssue);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@LoanRecovery", Loan);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@IncomeTaxDeduction", dIncomeTaxEmployee);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@MiscAmount", dMiscEarnings);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@MiscDeduction", dMiscDeductions);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@Bonus", dBonus);//dBonus
                                        cmdPaySlipAudit.Parameters.AddWithValue("@SalaryPayMode", SalaryPayMode);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@LastUpdate", DateTime.Now);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@Version", sSalaryProcessVersion);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@SIPEmployeeContribution", dSIPEmployee);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@SIPEmployerContribution", dSIPEmployer);
                                        cmdPaySlipAudit.Parameters.AddWithValue("@SKBBKContribution", dSKBBK);

                                        sdaPaySlip.ExecuteSQL(cmdPaySlipAudit);
                                        cmdPaySlipAudit.Dispose();
                                        //End Insert

                                        cmdEmployee.Dispose();
                                        drEmployee.Dispose();
                                        sdaEmployee.Dispose();
                                        cmdAttendance.Dispose();
                                        drAttendance.Dispose();
                                        sdaAttendanceList.Dispose();
                                        sdaAdvance.Dispose();
                                    }
                                }
                                sdaPaySlip.EndTransaction(true);
                                sdaPaySlip.Dispose();
                                return string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                decimal EID = 0;
                EID = EmployeeID;
                throw new Exception("EmployeeID: " + EID + " Error on Salary Processing: " + ex.Message.ToString().Trim());
            }
        }
    }
}
