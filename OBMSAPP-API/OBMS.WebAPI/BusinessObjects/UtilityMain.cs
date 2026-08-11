using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace OBMS.WebAPI.BusinessObjects
{
    public class UtilityMain
    {
        private static readonly IConfiguration configuration;

        public UtilityMain()
        {
            
        }

        static UtilityMain()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Base configuration file
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Environment-specific file
                .Build();
        }        

        public static string GetConfig(string KeyValue, string Branch)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {

                    cmd.CommandText = " SELECT * FROM CONFIGURATION WHERE KEYValue=@KeyValue AND ACTIVE=1 AND ACTIVEFROM<=GETDATE() AND ACTIVETO>=GETDATE() AND Branch=@Branch";
                    cmd.Parameters.AddWithValue("@KeyValue", KeyValue);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    string sResult = string.Empty;
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                return dr.GetString(dr.GetOrdinal("Value"));
                            }
                            return string.Empty;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static List<string> GetEPFToCIMBList(string Branch, DateTime Period, string EmployeeType, string CompanyEPF, string CompanyPIC, string CompanyPICContact)
        {
            try
            {
                List<string> SalaryAdvanceList = new List<string>();

                string strSQL = string.Empty;
                decimal dEPFER = 0;
                decimal dEPFEE = 0;
                decimal dBasicSalary = 0;
                decimal dTotalEPFER = 0;
                decimal dTotalEPFEE = 0;
                string sEPFER = string.Empty;
                string sEPFEE = string.Empty;
                string sBasicSalary = string.Empty;
                string strEPFER = string.Empty;
                string strEPFEE = string.Empty;
                string strEmpSecurityNo = string.Empty;
                string strEPFNo = string.Empty;
                string strEmpCode = string.Empty;
                string strEmpName1 = string.Empty;
                string strEmpName2 = string.Empty;
                string strValue = string.Empty;
                decimal dSumEPFNo = 0;
                decimal dCount = 0;
                using (SqlCommand cmd = new SqlCommand())
                {

                    strSQL = " SELECT isnull(C.EMPFL_EPFNO,'') as EMPFL_EPFNO,";
                    strSQL = strSQL + " isnull((CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN ";
                    strSQL = strSQL + " CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' THEN ";
                    strSQL = strSQL + " REPLACE(EMP_PASSPORT_NO,'-','') ";
                    strSQL = strSQL + " ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                    strSQL = strSQL + " ELSE REPLACE(EMP_IC_NEW,'-','') END),'') AS EMP_SecurityNo, ";
                    strSQL = strSQL + " isnull(SUBSTRING(EMP_NAME,1,40),'') AS NAME1,";
                    strSQL = strSQL + " CASE WHEN LEN(EMP_NAME) > 40 THEN";
                    strSQL = strSQL + " SUBSTRING(EMP_NAME,41,40) ";
                    strSQL = strSQL + " ELSE";
                    strSQL = strSQL + " ''";
                    strSQL = strSQL + " END AS NAME2,";
                    strSQL = strSQL + " isnull(SUBSTRING(EMP_CODE,0,20),'') AS EMP_CODE,";
                    strSQL = strSQL + " isnull(EPFEmployerContribution,0) AS EPFEmployerContribution,";
                    strSQL = strSQL + " isnull(EPFDeductionAmount,0) AS EPFDeductionAmount,";
                    strSQL = strSQL + " isnull(BasicSalary,0) AS BasicSalary,";
                    strSQL = strSQL + " Period,";
                    strSQL = strSQL + " EMP_BRANCH_CODE,";
                    strSQL = strSQL + " EMP_ROLE";
                    strSQL = strSQL + " FROM payslip A INNER JOIN Employee B ON A.EmployeeID = B.EMP_ID";
                    strSQL = strSQL + " LEFT OUTER JOIN EmployeeSalaryDetails C ON B.EMP_CODE = C.EMPFL_CODE ";
                    strSQL = strSQL + " WHERE YEAR(PERIOD)=@Year AND MONTH(Period)=@Month ";
                    strSQL = strSQL + " AND (EPFEmployerContribution > 0 OR EPFDeductionAmount > 0) ";
                    //if (Branch != "")
                    //{
                    //    strSQL = strSQL + " AND EMP_BRANCH_CODE = @Branch";
                    //}

                    //if (EmployeeType != "")
                    //{
                    //    strSQL = strSQL + " AND EMP_ROLE = @EmployeeType";
                    //}
                    if (Branch == "0" || Branch == "")
                    {
                    }
                    else
                    {
                        strSQL = strSQL + " AND EMP_BRANCH_CODE = @Branch ";
                    }

                    strSQL = strSQL + " ORDER BY EMP_NAME";
                    cmd.CommandText = strSQL;
                    //cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    cmd.Parameters.AddWithValue("@Month", Period.Month);
                    cmd.Parameters.AddWithValue("@Year", Period.Year);
                    cmd.Parameters.AddWithValue("@Branch", Branch);


                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {

                                if (dCount == 0)
                                {
                                    strValue = "01";
                                    strValue = strValue + string.Format("{0,-20}", "EPF MONTHLY FORM A".ToString().Trim());
                                    strValue = strValue + CompanyEPF.ToString().PadLeft(19, '0');
                                    if (Period.Month.ToString().Length == 1)
                                    {
                                        strValue = strValue + "0" + Period.Month + Period.Year;
                                    }
                                    else
                                    {
                                        strValue = strValue + Period.Month + Period.Year;
                                    }
                                    strValue = strValue + "ITB";
                                    strValue = strValue + "03".ToString().PadLeft(9, '0');
                                    strValue = strValue + "005";
                                    strValue = strValue + string.Format("{0,-40}", CompanyPIC.ToString().Trim());
                                    strValue = strValue + string.Format("{0,-20}", CompanyPICContact.ToString().Trim());
                                    strValue = strValue + "N";
                                    strValue = strValue + "00";
                                    strValue = strValue + DateTime.Now.ToString("yyyyMMdd");
                                    strValue = strValue + DateTime.Now.ToString("HHMMss") + "00";
                                    strValue = strValue + DateTime.Now.ToString("yyyyMMddHHMMss") + "00";
                                    strValue = strValue + "N";
                                    //strValue = strValue + dTotalEPFER.ToString("########000.00").Replace(".", string.Empty).PadLeft(15, '0');
                                    //strValue = strValue + dTotalEPFEE.ToString("########000.00").Replace(".", string.Empty).PadLeft(15, '0');
                                    //strValue = strValue + dSumEPFNo.ToString().PadLeft(21, '0');
                                    SalaryAdvanceList.Add(strValue.ToString().ToUpper().Trim());
                                }
                                dCount = dCount + 1;
                                dEPFER = 0;
                                dEPFEE = 0;
                                dBasicSalary = 0;
                                strEPFER = string.Empty;
                                strEPFEE = string.Empty;
                                strEmpSecurityNo = string.Empty;
                                strEPFNo = string.Empty;
                                strEmpCode = string.Empty;
                                strEmpName1 = string.Empty;
                                strEmpName2 = string.Empty;
                                strValue = string.Empty;

                                strEPFNo = dr.GetString(0).ToString();
                                if (!string.IsNullOrEmpty(strEPFNo))
                                {
                                    long number1 = 0;
                                    bool canConvert = long.TryParse(strEPFNo, out number1);
                                    if (canConvert == true)
                                    {
                                        dSumEPFNo = dSumEPFNo + Int32.Parse(strEPFNo);
                                    }

                                }
                                strEmpSecurityNo = dr.GetString(1).ToString();
                                strEmpName1 = dr.GetString(2).ToString();
                                strEmpName2 = dr.GetString(3).ToString();
                                strEmpCode = dr.GetString(4).ToString();
                                dEPFER = dr.GetDecimal(5);
                                dEPFEE = dr.GetDecimal(6);
                                dBasicSalary = dr.GetDecimal(7);
                                dTotalEPFER = dTotalEPFER + dEPFER;
                                dTotalEPFEE = dTotalEPFEE + dEPFEE;

                                sEPFER = dEPFER.ToString("########000.00").Replace(".", string.Empty);
                                sEPFEE = dEPFEE.ToString("########000.00").Replace(".", string.Empty);
                                sBasicSalary = dBasicSalary.ToString("########000.00").Replace(".", string.Empty);
                                strValue = "02";
                                //strValue = strValue + string.Format("{0,-19}", strEPFNo.ToString().Trim());
                                strValue = strValue + strEPFNo.PadLeft(19, '0').ToString();
                                strValue = strValue + string.Format("{0,-15}", strEmpSecurityNo.ToString().Trim());
                                strValue = strValue + string.Format("{0,-40}", strEmpName1.ToString().Trim());
                                strValue = strValue + string.Format("{0,-40}", strEmpName2.ToString().Trim());
                                strValue = strValue + string.Format("{0,-20}", strEmpCode.ToString().Trim());
                                //strValue = strValue + string.Format("{0,-15}", sEPFER);
                                //strValue = strValue + string.Format("{0,-15}", sEPFEE);
                                //strValue = strValue + string.Format("{0,-15}", sBasicSalary);
                                strValue = strValue + sEPFER.PadLeft(15, '0');
                                strValue = strValue + sEPFEE.PadLeft(15, '0');
                                strValue = strValue + sBasicSalary.PadLeft(15, '0');
                                SalaryAdvanceList.Add(strValue.ToString().ToUpper().Trim());
                                if (SalaryAdvanceList.Count >= 730)
                                {
                                    int dcount = SalaryAdvanceList.Count;
                                }
                            }

                            if (SalaryAdvanceList.Count > 0)
                            {
                                int TotalStaff = SalaryAdvanceList.Count - 1;
                                strValue = "99";
                                strValue = strValue + TotalStaff.ToString().PadLeft(7, '0');
                                strValue = strValue + dTotalEPFER.ToString("########000.00").Replace(".", string.Empty).PadLeft(15, '0');
                                strValue = strValue + dTotalEPFEE.ToString("########000.00").Replace(".", string.Empty).PadLeft(15, '0');
                                strValue = strValue + dSumEPFNo.ToString().PadLeft(21, '0');
                                SalaryAdvanceList.Add(strValue.ToString().ToUpper().Trim());
                            }

                            return SalaryAdvanceList;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Formats a decimal amount into cents as a whole-number string (no decimal point).
        /// The value is multiplied by 100 and truncated — no rounding.
        /// e.g. 2461.54 -> "246154",  2096.97 -> "209697",  185.00 -> "18500"
        /// </summary>
        private static string FormatAmountNoCents(decimal amount)
        {
            return ((long)Math.Truncate(amount * 100)).ToString();
        }

        public static List<string> GetSocsoToCIMBList(string CompanyRegNumber, string SocsoCompanyCode, string Branch, DateTime Period, string EmployeeType, string EmpTempType)
        {
            try
            {
                List<string> SocsoList = new List<string>();

                string strSQL = string.Empty;
                string strValue = string.Empty;

                using (SqlCommand cmd = new SqlCommand())
                {
                    // Columns returned (by ordinal):
                    // 0:  EMPLOYERCODE          (SocsoCompanyCode)
                    // 1:  EMP_SecurityNo        (IC new / IC old / SOCSO no)
                    // 2:  EMP_NAME
                    // 3:  MonthContribution     (MMYYYY)
                    // 4:  GrossSalary
                    // 5:  SOCSOEmployerContribution
                    // 6:  SOCSODeductionAmount   (employee)
                    // 7:  SIPEmployeeContribution (EIS employee)
                    // 8:  SIPEmployerContribution (EIS employer)
                    // 9:  SKBBKContribution      (SKBBK employee)
                    strSQL = " SELECT '" + SocsoCompanyCode + "' AS EMPLOYERCODE, ";
                    strSQL = strSQL + " (CASE WHEN B.EMP_IC_NEW is null or B.EMP_IC_NEW = '' THEN ";
                    strSQL = strSQL + "   CASE WHEN B.EMP_IC_OLD is null or B.EMP_IC_OLD = '' THEN ";
                    strSQL = strSQL + "     CASE WHEN C.EMPFL_SOSCO_NO IS NULL OR C.EMPFL_SOSCO_NO = '' THEN '' ";
                    strSQL = strSQL + "     ELSE REPLACE(C.EMPFL_SOSCO_NO,'-','') END ";
                    strSQL = strSQL + "   ELSE REPLACE(B.EMP_IC_OLD,'-','') END ";
                    strSQL = strSQL + " ELSE REPLACE(B.EMP_IC_NEW,'-','') END) AS EMP_SecurityNo, ";
                    strSQL = strSQL + " B.EMP_NAME, ";
                    strSQL = strSQL + " CASE WHEN LEN(CONVERT(VARCHAR(2),MONTH(A.period))) = 1 THEN ";
                    strSQL = strSQL + "   '0' + CONVERT(VARCHAR(2),MONTH(A.period)) ";
                    strSQL = strSQL + " ELSE CONVERT(VARCHAR(2),MONTH(A.period)) END ";
                    strSQL = strSQL + " + CONVERT(VARCHAR(4),YEAR(A.period)) AS MonthContribution, ";
                    strSQL = strSQL + " (A.BasicSalary + A.OverTimeSalary + A.OffDaySalary + A.OffDayOverTimeSalary ";
                    strSQL = strSQL + "  + A.HolidaySalary + A.HolidayOverTimeSalary + A.Shift2Salary ";
                    strSQL = strSQL + "  + A.ReAllowance + A.AttendanceAllowance + A.SpecialAllowance + A.Bonus) AS GrossSalary, ";
                    strSQL = strSQL + " ISNULL(A.SOCSOEmployerContribution, 0) AS SOCSOEmployerContribution, ";
                    strSQL = strSQL + " ISNULL(A.SOCSODeductionAmount, 0) AS SOCSODeductionAmount, ";
                    strSQL = strSQL + " ISNULL(A.SIPEmployeeContribution, 0) AS SIPEmployeeContribution, ";
                    strSQL = strSQL + " ISNULL(A.SIPEmployerContribution, 0) AS SIPEmployerContribution, ";
                    strSQL = strSQL + " ISNULL(A.SKBBKContribution, 0) AS SKBBKContribution ";
                    strSQL = strSQL + " FROM payslip A INNER JOIN Employee B ON A.EmployeeID = B.EMP_ID  ";
                    strSQL = strSQL + " INNER JOIN EmployeeSalaryDetails C ON B.EMP_CODE = C.EMPFL_CODE ";
                    strSQL = strSQL + " LEFT OUTER JOIN EmploymentDetails D ON B.EMP_CODE = D.EMPPAY_CODE ";
                    strSQL = strSQL + " WHERE YEAR(A.PERIOD)=@Year AND MONTH(A.Period)= @Month  ";
                    strSQL = strSQL + " AND (A.SOCSODeductionAmount > 0 OR A.SOCSOEmployerContribution > 0) ";
                    if (!(Branch == "0" || Branch == ""))
                    {
                        strSQL = strSQL + " AND B.EMP_BRANCH_CODE = @Branch ";
                    }
                    // Filter by employee type (local / foreign / all)
                    if (EmployeeType == "Foreign Guard")
                    {
                        // Foreign workers only — passport length > 3
                        strSQL = strSQL + " AND LEN(B.EMP_PASSPORT_NO) > 3  ";
                    }
                    else if (EmployeeType == "Guard")
                    {
                        // Local guards/staff only — IC-based, passport short/empty
                        strSQL = strSQL + " AND B.EMP_ROLE IN ('Guard','Staff') AND LEN(B.EMP_PASSPORT_NO) <= 3  ";
                    }
                    // else "All" or empty — no passport/role filter, include everyone (matches Excel behaviour)

                    //20241210 to filter those AURA resource from socso statement and file
                    if (EmpTempType == "4")
                    {
                        strSQL = strSQL + " AND C.TMPGUARD = 1 ";
                    }
                    else if (EmpTempType == "7")
                    {
                        strSQL = strSQL + " AND C.TMPGUARD = 0 ";
                    }
                    // EmpTempType == "8" (default) — no TMPGUARD filter, include all employee types

                    strSQL = strSQL + " ORDER BY B.EMP_NAME  ";

                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@Month", Period.Month);
                    cmd.Parameters.AddWithValue("@Year", Period.Year);
                    cmd.Parameters.AddWithValue("@Branch", Branch);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                // Read fields
                                string strEmployerCode        = dr.GetString(0);
                                string strEmpSecurityNo       = dr.GetString(1);
                                string strEMPName             = dr.GetString(2);
                                string strMonthContribution   = dr.GetString(3);
                                decimal dGrossSalary          = dr.IsDBNull(4) ? 0 : dr.GetDecimal(4);
                                decimal dSOCSOEmployer        = dr.IsDBNull(5) ? 0 : dr.GetDecimal(5);
                                decimal dSOCSOEmployee        = dr.IsDBNull(6) ? 0 : dr.GetDecimal(6);
                                decimal dEISEmployee          = dr.IsDBNull(7) ? 0 : dr.GetDecimal(7);
                                decimal dEISEmployer          = dr.IsDBNull(8) ? 0 : dr.GetDecimal(8);
                                decimal dSKBBKEmployee        = dr.IsDBNull(9) ? 0 : dr.GetDecimal(9);

                                string fmtGross    = FormatAmountNoCents(dGrossSalary);
                                string fmtSOCSOEmp = FormatAmountNoCents(dSOCSOEmployer);
                                string fmtSOCSOWrk = FormatAmountNoCents(dSOCSOEmployee);
                                string fmtEISWrk   = FormatAmountNoCents(dEISEmployee);
                                string fmtEISEmp   = FormatAmountNoCents(dEISEmployer);
                                string fmtSKBBK    = FormatAmountNoCents(dSKBBKEmployee);

                                // Build line matching reference format:
                                // D4100019020Z[20 spaces]670611045105GANESAN...[name 150]062026[8 spaces]246154  4285  1225  0490  0490  1835
                                strValue = string.Format("{0,-12}", strEmployerCode.Trim());
                                strValue = strValue + new string(' ', 20); // 20 spaces after employer code
                                strValue = strValue + string.Format("{0,-12}", strEmpSecurityNo.Trim());
                                strValue = strValue + string.Format("{0,-150}", strEMPName.Trim());
                                strValue = strValue + string.Format("{0,-6}", strMonthContribution.Trim());
                                strValue = strValue + new string(' ', 8); // 8 spaces before gross salary
                                strValue = strValue + fmtGross.PadLeft(6, ' ');
                                strValue = strValue + "  " + fmtSOCSOEmp.PadLeft(4, '0');
                                strValue = strValue + "  " + fmtSOCSOWrk.PadLeft(4, '0');
                                strValue = strValue + "  " + fmtEISWrk.PadLeft(4, '0');
                                strValue = strValue + "  " + fmtEISEmp.PadLeft(4, '0');
                                strValue = strValue + "  " + fmtSKBBK.PadLeft(4, '0');

                                SocsoList.Add(strValue.ToUpper());
                            }

                            return SocsoList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static List<string> GetSIPToCIMBList(string CompanyRegNumber, string SIPCompanyCode, string Branch, DateTime Period, string EmployeeType)
        {
            try
            {
                List<string> SIPList = new List<string>();

                string strSQL = string.Empty;
                string strEmployerCode = string.Empty;
                string strSSMNumber = string.Empty;
                string strEmpSecurityNo = string.Empty;
                string strMonthContribution = string.Empty;
                string strEMPName = string.Empty;
                decimal dSIPContributionAmount = 0;
                string strSIPContributionAmount = string.Empty;
                DateTime dEMPDateJoin;
                string strValue = string.Empty;
                string strEmpDateJoin = string.Empty;
                using (SqlCommand cmd = new SqlCommand())
                {

                    strSQL = " SELECT isnull(C.EMPFL_EPFNO,'') as EMPFL_EPFNO,";
                    strSQL = strSQL + " isnull((CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN ";
                    //strSQL = " SELECT 'D4100019020Z' AS EMPLOYERCODE,'357119-K' AS SSMNUMBER, ";
                    strSQL = " SELECT '" + SIPCompanyCode + "' AS EMPLOYERCODE,'" + CompanyRegNumber + "' AS SSMNUMBER, ";
                    strSQL = strSQL + " (CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN  ";
                    strSQL = strSQL + " CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' THEN  ";
                    strSQL = strSQL + " REPLACE(EMP_PASSPORT_NO,'-','')  ";
                    strSQL = strSQL + " ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                    strSQL = strSQL + " ELSE REPLACE(EMP_IC_NEW,'-','') END) AS EMP_SecurityNo,  ";
                    strSQL = strSQL + " EMP_NAME, ";
                    strSQL = strSQL + " CASE WHEN LEN(CONVERT(VARCHAR(2),MONTH(period))) = 1 THEN ";
                    strSQL = strSQL + " '0' + CONVERT(VARCHAR(2),MONTH(period)) ";
                    strSQL = strSQL + " ELSE ";
                    strSQL = strSQL + " CONVERT(VARCHAR(2),MONTH(period)) ";
                    strSQL = strSQL + " END  ";
                    strSQL = strSQL + " + '' + CONVERT(VARCHAR(4),YEAR(period)) AS MonthContribution, ";
                    strSQL = strSQL + " SIPEmployeeContribution + SIPEmployerContribution AS SIPCONTRIBUTIONAMOUNT, ";
                    //strSQL = strSQL + " D.EMPPAY_DATE_JOINED  ";
                    strSQL = strSQL + " case when D.EMPPAY_DATE_JOINED is null then ";
                    strSQL = strSQL + " '' ";
                    strSQL = strSQL + " else ";
                    strSQL = strSQL + " D.EMPPAY_DATE_JOINED ";
                    strSQL = strSQL + " end as EMPPAY_DATE_JOINED ";
                    strSQL = strSQL + " FROM payslip A INNER JOIN Employee B ON A.EmployeeID = B.EMP_ID  ";
                    //strSQL = strSQL + " INNER JOIN EmployeeSalaryDetails C ON B.EMP_ID = C.EMPFL_ID  ";
                    strSQL = strSQL + " left outer JOIN EmploymentDetails D ON A.EmployeeID = D.EMPPAY_ID ";
                    strSQL = strSQL + " WHERE YEAR(PERIOD)=@Year AND MONTH(Period)= @Month  ";
                    strSQL = strSQL + " AND (SIPEmployeeContribution >0 OR SIPEmployerContribution > 0) ";
                    if (Branch == "0" || Branch == "")
                    {
                    }
                    else
                    {
                        strSQL = strSQL + " AND EMP_BRANCH_CODE = @Branch ";
                    }
                    strSQL = strSQL + " ORDER BY EMP_NAME  ";

                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@Month", Period.Month);
                    cmd.Parameters.AddWithValue("@Year", Period.Year);
                    cmd.Parameters.AddWithValue("@Branch", Branch);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                dSIPContributionAmount = 0;

                                strEmployerCode = dr.GetString(0);
                                strSSMNumber = dr.GetString(1);
                                strEmpSecurityNo = dr.GetString(2);
                                strEMPName = dr.GetString(3);
                                strMonthContribution = dr.GetString(4);
                                dSIPContributionAmount = dr.GetDecimal(5);

                                strSIPContributionAmount = dSIPContributionAmount.ToString("########000.00").Replace(".", string.Empty);

                                dEMPDateJoin = dr.GetDateTime(6);
                                strEmpDateJoin = dEMPDateJoin.ToString("ddMMyyyy");
                                if (strEmpDateJoin == "01011900")
                                    strEmpDateJoin = "";

                                strValue = string.Format("{0,-12}", strEmployerCode.ToString().Trim());
                                strValue = strValue + string.Format("{0,-20}", strSSMNumber.ToString().Trim());
                                strValue = strValue + string.Format("{0,-12}", strEmpSecurityNo.ToString().Trim());
                                strValue = strValue + string.Format("{0,-150}", strEMPName.ToString().Trim());
                                //strValue = strValue + strEMPName.PadRight(150, ' ');
                                strValue = strValue + string.Format("{0,-6}", strMonthContribution.ToString().Trim());
                                strValue = strValue + strSIPContributionAmount.PadLeft(14, '0');
                                strValue = strValue + string.Format("{0,-8}", strEmpDateJoin.ToString().Trim());
                                strValue = strValue + " ";

                                SIPList.Add(strValue.ToString().ToUpper());
                            }



                            return SIPList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static List<string> GetEmployeeSalarynAdvanceList(DateTime Period, string EmployeeType, string BankCode, string Type, string Company, string Source)
        {
            try
            {
                List<string> SalaryAdvanceList = new List<string>();

                string strSQL = string.Empty;
                decimal dAmount = 0;
                string strAmount = string.Empty;
                string strEmpSecurityNo = string.Empty;
                string strEmpBankAccNo = string.Empty;
                string strEmpName = string.Empty;
                string strValue = string.Empty;
                // SKBBKContribution is included as a deduction for salary periods after June 2026
                bool includeSKBBK = (Period.Year > 2026 || (Period.Year == 2026 && Period.Month > 6));

                using (SqlCommand cmd = new SqlCommand())
                {
                    if (Type == "SalaryAdvance")
                    {
                        strSQL = "SELECT SalaryAdvance.Amount,  ";
                        strSQL = strSQL + "SUBSTRING(Employee.EMP_NAME,1,40) AS EmployeeName,  ";
                        //if (BankCode.ToString().ToUpper() == "CIMB")
                        //{
                        //    strSQL = strSQL + "SUBSTRING(Employee.EMP_NAME,1,40) AS EmployeeName,  ";
                        //}
                        //else if (BankCode.ToString().ToUpper() == "BSN")
                        //{
                        //    strSQL = strSQL + "SUBSTRING(Employee.EMP_NAME,1,30) AS EmployeeName,  ";
                        //}
                        //else
                        //{
                        //    strSQL = strSQL + "Employee.EMP_NAME,  ";
                        //}

                        strSQL = strSQL + "(CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN  ";
                        strSQL = strSQL + "CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' ";
                        strSQL = strSQL + "THEN REPLACE(EMP_PASSPORT_NO,'-','') ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                        strSQL = strSQL + "ELSE REPLACE(EMP_IC_NEW,'-','') END) AS EMP_SecurityNo, ";
                        strSQL = strSQL + "Employee.EMP_IC_OLD, Employee.EMP_IC_NEW, Employee.EMP_PASSPORT_NO, ";
                        strSQL = strSQL + "REPLACE(EmployeeSalaryDetails.EMPFL_BK_ACCNO,'-','') AS EMP_BK_ACCNO ";
                        strSQL = strSQL + "FROM SalaryAdvance ";
                        strSQL = strSQL + "INNER JOIN Employee ON SalaryAdvance.EmployeeID=Employee.EMP_ID ";
                        strSQL = strSQL + "INNER JOIN EmployeeSalaryDetails ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE ";
                        strSQL = strSQL + "WHERE SalaryAdvance.TransType=1 ";
                        strSQL = strSQL + "AND SalaryAdvance.PaymentType='BANK' ";
                        strSQL = strSQL + "AND SalaryAdvance.IsDeleted=0 ";
                        strSQL = strSQL + "AND EMP_ROLE =@EmployeeType ";
                        strSQL = strSQL + "AND Month(AdvanceDate) = @Month ";
                        strSQL = strSQL + "AND Year(AdvanceDate) = @Year ";
                        if (BankCode == "0" || BankCode == string.Empty)
                        {
                        }
                        else
                        {
                            strSQL = strSQL + "AND EMPFL_BANK = @BankCode ";
                        }
                        //strSQL = strSQL + "AND EMPFL_BANK = @BankCode ";
                        strSQL = strSQL + " ORDER BY EMP_NAME ";
                    }
                    else if (Type == "Salary")
                    {
                        strSQL = "SELECT  ";
                        //Commented by Kean Hong which to split the salary by Guard1 and Guard2 as per KTMB requirement 20210201
                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "SUM(  ";
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                            strSQL = strSQL + ") AS AMOUNT, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "SUM(  ";
                            strSQL = strSQL + "(OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + "(UniformIssueRecovery+MiscDeduction) ";
                            strSQL = strSQL + ") AS AMOUNT, ";
                        }
                        else
                        {
                            strSQL = strSQL + "SUM(  ";
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance + Bonus) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                            strSQL = strSQL + ") AS AMOUNT, ";
                        }
                        strSQL = strSQL + "REPLACE(REPLACE(SUBSTRING(EMP_NAME,1,40),'-',''),'/','') AS EmployeeName,  ";

                        strSQL = strSQL + "EMP_SecurityNo,EMP_IC_OLD,  ";
                        strSQL = strSQL + "EMP_IC_NEW, EMP_PASSPORT_NO,EMP_BK_ACCNO ";
                        strSQL = strSQL + "FROM ";
                        strSQL = strSQL + "( ";

                        //Commented by Kean Hong which to split the salary by Guard1 and Guard2 as per KTMB requirement 20210201
                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "SELECT Distinct ";
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "SELECT Distinct ";
                            strSQL = strSQL + "OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus,";
                            strSQL = strSQL + "UniformIssueRecovery,MiscDeduction, ";
                        }
                        else
                        {
                            strSQL = strSQL + "SELECT Distinct ";
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution, Bonus, ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction, ";
                            if (includeSKBBK) strSQL = strSQL + "SKBBKContribution, ";
                        }

                        strSQL = strSQL + "EMP_NAME, ";
                        strSQL = strSQL + "(CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN ";
                        strSQL = strSQL + "CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' ";
                        strSQL = strSQL + "THEN REPLACE(EMP_PASSPORT_NO,'-','') ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                        strSQL = strSQL + "ELSE REPLACE(EMP_IC_NEW,'-','') END) AS EMP_SecurityNo, ";
                        strSQL = strSQL + "EMP_IC_OLD, EMP_IC_NEW, EMP_PASSPORT_NO, ";
                        if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "REPLACE(EMPFL_2ndBK_ACCNO,'-','') AS EMP_BK_ACCNO ";
                        }
                        else
                        {
                            strSQL = strSQL + "REPLACE(EMPFL_BK_ACCNO,'-','') AS EMP_BK_ACCNO ";
                        }
                        strSQL = strSQL + "FROM VWBankSalaryStatement ";
                        strSQL = strSQL + "WHERE SalaryPayMode ='BANK' ";
                        strSQL = strSQL + "AND Month(Period) = @Month ";
                        strSQL = strSQL + "AND Year(Period) = @Year ";
                        if (BankCode == "0" || BankCode == "")
                        {
                        }
                        else
                        {
                            strSQL = strSQL + "AND EMPFL_BANK =@BankCode ";
                        }
                        strSQL = strSQL + "AND EMP_ROLE = @EmployeeType ";

                        //Commented by Kean Hong which to split the salary by Guard1 and Guard2 as per KTMB requirement 20210201
                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "GROUP BY EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO, ";
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution, MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction ";
                            strSQL = strSQL + ") A ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "GROUP BY EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_2ndBK_ACCNO, ";
                            strSQL = strSQL + "OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus,";
                            strSQL = strSQL + "UniformIssueRecovery,MiscDeduction ";
                            strSQL = strSQL + ") A ";
                        }
                        else
                        {
                            strSQL = strSQL + "GROUP BY EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO, ";
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + ",SKBBKContribution";
                            strSQL = strSQL + " ) A ";
                        }

                        strSQL = strSQL + "Group By EMP_NAME,EMP_SecurityNo,EMP_IC_OLD,EMP_IC_NEW, EMP_PASSPORT_NO,EMP_BK_ACCNO ";

                        //Commented by Kean Hong which to split the salary by Guard1 and Guard2 as per KTMB requirement 20210201
                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "HAVING SUM( ";
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                            strSQL = strSQL + ") > 0 ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "HAVING SUM( ";
                            strSQL = strSQL + "(OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + "(UniformIssueRecovery+MiscDeduction) ";
                            strSQL = strSQL + ") > 0 ";
                        }
                        else
                        {
                            strSQL = strSQL + "HAVING SUM( ";
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + "HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance + Bonus) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                            strSQL = strSQL + ") > 0 ";
                        }


                        strSQL = strSQL + " ORDER BY EMP_NAME ";
                    }

                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    cmd.Parameters.AddWithValue("@Month", Period.Month);
                    cmd.Parameters.AddWithValue("@Year", Period.Year);
                    cmd.Parameters.AddWithValue("@BankCode", BankCode);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                dAmount = 0;
                                strEmpName = string.Empty;
                                strEmpSecurityNo = string.Empty;
                                strEmpBankAccNo = string.Empty;
                                strValue = string.Empty;

                                dAmount = (dr[0] == DBNull.Value) ? 0m : dr.GetDecimal(0);
                                strEmpName      = (dr[1] == DBNull.Value) ? string.Empty : dr.GetString(1);
                                strEmpSecurityNo = (dr[2] == DBNull.Value) ? string.Empty : dr.GetString(2);
                                strEmpBankAccNo  = (dr[6] == DBNull.Value) ? string.Empty : dr.GetString(6);

                                // Strip special characters from name � keep only letters, digits, spaces
                                strEmpName = System.Text.RegularExpressions.Regex.Replace(strEmpName, @"[^A-Za-z0-9 ]", "");

                                if (BankCode.ToString().ToUpper() == "CIMB")
                                {
                                    strAmount = dAmount.ToString("########000.00").Replace(".", string.Empty);
                                    // Truncate each field to its exact max width to prevent line overflow
                                    string accNo16  = strEmpBankAccNo.ToString().Trim().PadRight(16).Substring(0, 16);
                                    string name40   = strEmpName.ToString().Trim().PadRight(40).Substring(0, 40);
                                    string amt11    = strAmount.PadLeft(11, '0').PadRight(11).Substring(0, 11);
                                    string spaces30 = string.Empty.PadRight(30);
                                    string secNo20  = strEmpSecurityNo.ToString().Trim().PadRight(20).Substring(0, 20);
                                    strValue = accNo16 + name40 + amt11 + spaces30 + secNo20 + "2";
                                }
                                else if (BankCode.ToString().ToUpper() == "BSN")
                                {
                                    strAmount = dAmount.ToString("####0000000.00").Replace(".", string.Empty);
                                    string accNo16  = strEmpBankAccNo.ToString().Trim().PadRight(16).Substring(0, 16);
                                    string amt9     = strAmount.PadRight(9).Substring(0, 9);
                                    string name30   = strEmpName.ToString().Trim().PadRight(30).Substring(0, 30);
                                    string secNo15  = strEmpSecurityNo.ToString().Trim().PadRight(15).Substring(0, 15);
                                    strValue = accNo16 + amt9 + name30 + secNo15 + 0.ToString("000000000000000");
                                }
                                else
                                {
                                    strAmount = dAmount.ToString("########000.00").Replace(".", string.Empty);
                                    string accNo16  = strEmpBankAccNo.ToString().Trim().PadRight(16).Substring(0, 16);
                                    string name40   = strEmpName.ToString().Trim().PadRight(40).Substring(0, 40);
                                    string amt11    = strAmount.PadLeft(11, '0').PadRight(11).Substring(0, 11);
                                    string spaces30 = string.Empty.PadRight(30);
                                    string secNo20  = strEmpSecurityNo.ToString().Trim().PadRight(20).Substring(0, 20);
                                    strValue = accNo16 + name40 + amt11 + spaces30 + secNo20 + "2";
                                }

                                SalaryAdvanceList.Add(strValue.ToString().ToUpper().Trim());
                            }
                            return SalaryAdvanceList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static List<string> GetEmployeeSalarynAdvanceList(string Branch, DateTime Period, string EmployeeType, string BankCode, string Type, string Company, string Source)
        {
            try
            {
                List<string> SalaryAdvanceList = new List<string>();

                string strSQL = string.Empty;
                decimal dAmount = 0;
                string strAmount = string.Empty;
                string strEmpSecurityNo = string.Empty;
                string strEmpBankAccNo = string.Empty;
                string strEmpName = string.Empty;
                string strValue = string.Empty;
                // SKBBKContribution is included as a deduction for salary periods after June 2026
                bool includeSKBBK = (Period.Year > 2026 || (Period.Year == 2026 && Period.Month > 6));

                using (SqlCommand cmd = new SqlCommand())
                {
                    if (Type == "SalaryAdvance")
                    {
                        strSQL = "SELECT SalaryAdvance.Amount,  ";
                        //strSQL = strSQL + "SUBSTRING(Employee.EMP_NAME,1,40) AS EmployeeName,  ";
                        if (BankCode.ToString().ToUpper() == "CIMB" || BankCode.ToString().ToUpper() == "RHB")
                        {
                            strSQL = strSQL + "SUBSTRING(Employee.EMP_NAME,1,40) AS EmployeeName,  ";
                        }
                        else if (BankCode.ToString().ToUpper() == "BSN")
                        {
                            strSQL = strSQL + "SUBSTRING(Employee.EMP_NAME,1,30) AS EmployeeName,  ";
                        }
                        else
                        {
                            strSQL = strSQL + "Employee.EMP_NAME AS EmployeeName,  ";
                        }
                        strSQL = strSQL + "(CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN  ";
                        strSQL = strSQL + "CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' ";
                        strSQL = strSQL + "THEN REPLACE(EMP_PASSPORT_NO,'-','') ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                        strSQL = strSQL + "ELSE REPLACE(EMP_IC_NEW,'-','') END) AS EMP_SecurityNo, ";
                        strSQL = strSQL + "Employee.EMP_IC_OLD, Employee.EMP_IC_NEW, Employee.EMP_PASSPORT_NO, ";
                        strSQL = strSQL + "REPLACE(EmployeeSalaryDetails.EMPFL_BK_ACCNO,'-','') AS EMP_BK_ACCNO ";
                        strSQL = strSQL + "FROM SalaryAdvance ";
                        strSQL = strSQL + "INNER JOIN Employee ON SalaryAdvance.EmployeeID=Employee.EMP_ID ";
                        strSQL = strSQL + "INNER JOIN EmployeeSalaryDetails ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE ";
                        strSQL = strSQL + "WHERE SalaryAdvance.TransType=1 ";
                        strSQL = strSQL + "AND SalaryAdvance.PaymentType='BANK' ";
                        strSQL = strSQL + "AND SalaryAdvance.IsDeleted=0 ";
                        strSQL = strSQL + "AND EMP_ROLE =@EmployeeType ";
                        strSQL = strSQL + "AND Month(AdvanceDate) = @Month ";
                        strSQL = strSQL + "AND Year(AdvanceDate) = @Year ";
                        if (BankCode == "0" || BankCode == "")
                        {
                        }
                        else
                        {
                            strSQL = strSQL + "AND EMPFL_BANK =@BankCode ";
                        }

                        if (Branch == "0" || Branch == "")
                        {
                        }
                        else
                        {
                            strSQL = strSQL + "AND EMP_BRANCH_CODE = @Branch ";
                        }
                        strSQL = strSQL + " ORDER BY EMP_NAME ";
                    }
                    else if (Type == "Salary")
                    {
                        strSQL = "SELECT  ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "SUM(  ";
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SpecialAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                            strSQL = strSQL + ") AS AMOUNT, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "SUM(  ";
                            strSQL = strSQL + "(Shift2Salary+MiscAmount+Bonus+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + "(UniformIssueRecovery+MiscDeduction) ";
                            strSQL = strSQL + ") AS AMOUNT, ";
                        }
                        else
                        {
                            strSQL = strSQL + "SUM(  ";
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + "HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance + Bonus) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                            strSQL = strSQL + ") AS AMOUNT, ";
                        }


                        if (BankCode.ToString().ToUpper() == "CIMB" || BankCode.ToString().ToUpper() == "RHB")
                        {
                            strSQL = strSQL + "SUBSTRING(EMP_NAME,1,40) AS EmployeeName,  ";
                        }
                        else if (BankCode.ToString().ToUpper() == "BSN")
                        {
                            strSQL = strSQL + "SUBSTRING(EMP_NAME,1,30) AS EmployeeName,  ";
                        }
                        else
                        {
                            strSQL = strSQL + "EMP_NAME AS EmployeeName,  ";
                        }
                        //strSQL = strSQL + "SUBSTRING(EMP_NAME,1,40) AS EmployeeName,  ";
                        strSQL = strSQL + "EMP_SecurityNo,EMP_IC_OLD,  ";
                        strSQL = strSQL + "EMP_IC_NEW, EMP_PASSPORT_NO,EMP_BK_ACCNO ";
                        strSQL = strSQL + "FROM ";
                        strSQL = strSQL + "( ";
                        strSQL = strSQL + "SELECT Distinct ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SpecialAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "Shift2Salary,MiscAmount,Bonus,SpecialAllowance,ReAllowance, ";
                            strSQL = strSQL + "UniformIssueRecovery,MiscDeduction, ";
                        }
                        else
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction, ";
                            if (includeSKBBK) strSQL = strSQL + "SKBBKContribution, ";
                        }

                        strSQL = strSQL + "EMP_NAME, ";
                        strSQL = strSQL + "(CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN ";
                        strSQL = strSQL + "CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' ";
                        strSQL = strSQL + "THEN REPLACE(EMP_PASSPORT_NO,'-','') ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                        strSQL = strSQL + "ELSE REPLACE(EMP_IC_NEW,'-','') END) AS EMP_SecurityNo, ";
                        strSQL = strSQL + "EMP_IC_OLD, EMP_IC_NEW, EMP_PASSPORT_NO, ";
                        strSQL = strSQL + "REPLACE(EMPFL_BK_ACCNO,'-','') AS EMP_BK_ACCNO ";
                        strSQL = strSQL + "FROM VWBankSalaryStatement ";
                        strSQL = strSQL + "WHERE SalaryPayMode ='BANK' ";
                        strSQL = strSQL + "AND Month(Period) = @Month ";
                        strSQL = strSQL + "AND Year(Period) = @Year ";
                        if (BankCode == "0" || BankCode == "")
                        {
                        }
                        else
                        {
                            strSQL = strSQL + "AND EMPFL_BANK =@BankCode ";
                        }

                        strSQL = strSQL + "AND EMP_ROLE = @EmployeeType ";
                        if (Branch == "0" || Branch == "")
                        {
                        }
                        else
                        {
                            strSQL = strSQL + "AND CODE = @Branch ";
                        }

                        strSQL = strSQL + "GROUP BY EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO, ";
                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SpecialAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "Shift2Salary,MiscAmount,Bonus,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "UniformIssueRecovery,MiscDeduction ";
                        }
                        else
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + ",SKBBKContribution";
                        }


                        strSQL = strSQL + ") A ";
                        strSQL = strSQL + "Group By EMP_NAME,EMP_SecurityNo,EMP_IC_OLD,EMP_IC_NEW, EMP_PASSPORT_NO,EMP_BK_ACCNO ";
                        strSQL = strSQL + "HAVING SUM( ";
                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SpecialAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "(Shift2Salary+MiscAmount+Bonus+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + "(UniformIssueRecovery+MiscDeduction) ";
                        }
                        else
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + "HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance+Bonus) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                        }

                        strSQL = strSQL + ") > 0 ";
                        strSQL = strSQL + " ORDER BY EMP_NAME ";

                    }


                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    cmd.Parameters.AddWithValue("@Month", Period.Month);
                    cmd.Parameters.AddWithValue("@Year", Period.Year);
                    cmd.Parameters.AddWithValue("@BankCode", BankCode);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                dAmount = 0;
                                strEmpName = string.Empty;
                                strEmpSecurityNo = string.Empty;
                                strEmpBankAccNo = string.Empty;
                                strValue = string.Empty;

                                dAmount = (dr[0] == DBNull.Value) ? 0m : dr.GetDecimal(0);
                                strEmpName       = (dr[1] == DBNull.Value) ? string.Empty : dr.GetString(1);
                                strEmpSecurityNo = (dr[2] == DBNull.Value) ? string.Empty : dr.GetString(2);
                                strEmpBankAccNo  = (dr[6] == DBNull.Value) ? string.Empty : dr.GetString(6);

                                // Strip special characters from name � keep only letters, digits, spaces
                                strEmpName = System.Text.RegularExpressions.Regex.Replace(strEmpName, @"[^A-Za-z0-9 ]", "");

                                if (BankCode.ToString().ToUpper() == "CIMB")
                                {
                                    //strAmount = dAmount.ToString("########000.00").Replace(".", string.Empty);
                                    //strValue = string.Format("{0,-16}", strEmpBankAccNo.ToString().Trim());
                                    //strValue = strValue + string.Format("{0,-40}", strEmpName.ToString().Trim());
                                    //strValue = strValue + string.Format("{0,-11}", strAmount);
                                    //strValue = strValue + string.Format("{0,-30}", string.Empty);
                                    //strValue = strValue + string.Format("{0,-20}", strEmpSecurityNo.ToString().Trim());
                                    //strValue = strValue + string.Format("{0,-1}", "2");
                                    strAmount = dAmount.ToString("########000.00").Replace(".", string.Empty);
                                    string accNo16c  = strEmpBankAccNo.ToString().Trim().PadRight(16).Substring(0, 16);
                                    string name40c   = strEmpName.ToString().Trim().PadRight(40).Substring(0, 40);
                                    string amt11c    = strAmount.PadLeft(11, '0').PadRight(11).Substring(0, 11);
                                    string spaces30c = string.Empty.PadRight(30);
                                    string secNo20c  = strEmpSecurityNo.ToString().Trim().PadRight(20).Substring(0, 20);
                                    strValue = accNo16c + name40c + amt11c + spaces30c + secNo20c + "2";
                                }
                                else if (BankCode.ToString().ToUpper() == "RHB")
                                {
                                    strAmount = dAmount.ToString("########000.00").Replace(".", string.Empty);
                                    strValue = string.Format("{0,-15}", string.Empty);
                                    strValue = strValue + string.Format("{0,-14}", strEmpBankAccNo.ToString().Trim());
                                    strValue = strValue + string.Format("{0,-15}", strAmount.ToString().PadLeft(15, '0'));
                                    if (strEmpName.ToString().Trim().Length >= 20)
                                    {
                                        strValue = strValue + string.Format("{0,-20}", strEmpName.ToString().Trim().Substring(0, 20));
                                    }
                                    else
                                    {
                                        strValue = strValue + string.Format("{0,-20}", strEmpName.ToString().Trim());
                                    }
                                    strValue = strValue + string.Format("{0,-12}", strEmpSecurityNo.ToString().Trim());
                                    strValue = strValue + string.Format("{0,-2}", string.Empty);
                                }
                                else if (BankCode.ToString().ToUpper() == "BSN")
                                {
                                    strAmount = dAmount.ToString("####0000000.00").Replace(".", string.Empty);
                                    string accNo16b  = strEmpBankAccNo.ToString().Trim().PadRight(16).Substring(0, 16);
                                    string amt9b     = strAmount.PadRight(9).Substring(0, 9);
                                    string name30b   = strEmpName.ToString().Trim().PadRight(30).Substring(0, 30);
                                    string secNo15b  = strEmpSecurityNo.ToString().Trim().PadRight(15).Substring(0, 15);
                                    strValue = accNo16b + amt9b + name30b + secNo15b + 0.ToString("000000000000000");
                                }
                                else
                                {
                                    strAmount = dAmount.ToString("########000.00").Replace(".", string.Empty);
                                    string accNo16e  = strEmpBankAccNo.ToString().Trim().PadRight(16).Substring(0, 16);
                                    string name40e   = strEmpName.ToString().Trim().PadRight(40).Substring(0, 40);
                                    string amt11e    = strAmount.PadLeft(11, '0').PadRight(11).Substring(0, 11);
                                    string spaces30e = string.Empty.PadRight(30);
                                    string secNo20e  = strEmpSecurityNo.ToString().Trim().PadRight(20).Substring(0, 20);
                                    strValue = accNo16e + name40e + amt11e + spaces30e + secNo20e + "2";
                                }

                                if (BankCode.ToString().ToUpper() == "RHB")
                                {
                                    SalaryAdvanceList.Add(strValue.ToString().ToUpper());
                                }
                                else
                                {
                                    SalaryAdvanceList.Add(strValue.ToString().ToUpper().Trim());
                                }
                            }
                            return SalaryAdvanceList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static List<string> GetEmployeeSalarynAdvanceTotalList(DateTime Period, string EmployeeType, string BankCode, string Type, string Company, string Source)
        {
            try
            {
                List<string> SalaryAdvanceTotalList = new List<string>();

                string strSQL = string.Empty;
                string strTotalRecord = string.Empty;
                string strTotalAmount = string.Empty;
                string strValue = string.Empty;
                // SKBBKContribution is included as a deduction for salary periods after June 2026
                bool includeSKBBK = (Period.Year > 2026 || (Period.Year == 2026 && Period.Month > 6));

                using (SqlCommand cmd = new SqlCommand())
                {

                    if (Type == "SalaryAdvance")
                    {
                        strSQL = "SELECT ISNULL(Count(*),0) AS TotalRecord,ISNULL(SUM(SalaryAdvance.Amount),0) AS TotalAmount ";
                        strSQL = strSQL + "FROM SalaryAdvance ";
                        strSQL = strSQL + "INNER JOIN Employee ON SalaryAdvance.EmployeeID=Employee.EMP_ID ";
                        strSQL = strSQL + "INNER JOIN EmployeeSalaryDetails ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE ";
                        strSQL = strSQL + "WHERE SalaryAdvance.TransType=1 ";
                        strSQL = strSQL + "AND SalaryAdvance.PaymentType='BANK' ";
                        strSQL = strSQL + "AND SalaryAdvance.IsDeleted=0 ";
                        strSQL = strSQL + "AND EMP_ROLE =@EmployeeType ";
                        strSQL = strSQL + "AND Month(AdvanceDate) = @Month ";
                        strSQL = strSQL + "AND Year(AdvanceDate) = @Year ";
                        //strSQL = strSQL + "AND EMPFL_BANK = @BankCode ";

                        if (BankCode == "0" || BankCode == "")
                        {
                        }
                        else
                        {
                            strSQL = strSQL + "AND EMPFL_BANK =@BankCode ";
                        }

                    }
                    else if (Type == "Salary")
                    {
                        strSQL = " SELECT ISNULL(COUNT(*),0) AS TOTALRECORD, ISNULL(SUM(AMOUNT),0) AS TOTALAMOUNT ";
                        strSQL = strSQL + "FROM ";
                        strSQL = strSQL + "( ";
                        strSQL = strSQL + " SELECT EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO, ";
                        strSQL = strSQL + " SUM(  ";
                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + " (BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + " (EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + " (OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + " (UniformIssueRecovery+MiscDeduction) ";
                        }
                        else
                        {
                            strSQL = strSQL + " (BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + " HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance+Bonus) - ";
                            strSQL = strSQL + " (EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + " MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                        }

                        strSQL = strSQL + " ) AS AMOUNT ";
                        strSQL = strSQL + " FROM ";
                        strSQL = strSQL + " ( ";
                        strSQL = strSQL + "     SELECT Distinct ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "    BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "    EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "    OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus, ";
                            strSQL = strSQL + "    UniformIssueRecovery,MiscDeduction, ";
                        }
                        else
                        {
                            strSQL = strSQL + "    BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "    HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "    EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "    MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction, ";
                            if (includeSKBBK) strSQL = strSQL + "    SKBBKContribution, ";
                        }

                        strSQL = strSQL + "    EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO ";
                        strSQL = strSQL + "    FROM VWBankSalaryStatement ";
                        strSQL = strSQL + "    WHERE SalaryPayMode ='BANK' ";
                        strSQL = strSQL + "    AND Month(Period) = @Month ";
                        strSQL = strSQL + "    AND Year(Period) = @Year ";
                        if (BankCode == "0" || BankCode == "")
                        {
                        }
                        else
                        {
                            strSQL = strSQL + " AND EMPFL_BANK =@BankCode ";
                        }
                        strSQL = strSQL + "    AND EMP_ROLE = @EmployeeType ";
                        strSQL = strSQL + "    GROUP BY EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO, ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "    BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "    EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "    OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus, ";
                            strSQL = strSQL + "    UniformIssueRecovery,MiscDeduction ";
                        }
                        else
                        {
                            strSQL = strSQL + "    BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "    HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "    EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "    MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + ",SKBBKContribution";
                        }

                        strSQL = strSQL + " ) A ";
                        strSQL = strSQL + " Group By EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO ";
                        strSQL = strSQL + " HAVING SUM( ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + " (BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + " (EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + " (OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + " (UniformIssueRecovery+MiscDeduction) ";
                        }
                        else
                        {
                            strSQL = strSQL + " (BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + " HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance +Bonus) - ";
                            strSQL = strSQL + " (EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + " MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                        }

                        strSQL = strSQL + " ) > 0 ";
                        strSQL = strSQL + ")TOTAL ";
                    }


                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    cmd.Parameters.AddWithValue("@Month", Period.Month);
                    cmd.Parameters.AddWithValue("@Year", Period.Year);
                    cmd.Parameters.AddWithValue("@BankCode", BankCode);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                strTotalRecord = string.Empty;
                                strTotalAmount = string.Empty;
                                strValue = string.Empty;

                                if (BankCode.ToString().ToUpper() == "CIMB")
                                {
                                    strTotalRecord = dr.GetInt32(0).ToString("000000");
                                    strTotalAmount = dr.GetDecimal(1).ToString("00000000000.00").Replace(".", string.Empty);

                                    strValue = string.Format("{0,-6}", strTotalRecord);
                                    strValue = strValue + string.Format("{0,-13}", strTotalAmount);

                                }
                                else if (BankCode.ToString().ToUpper() == "BSN")
                                {
                                    strTotalRecord = (1 + dr.GetInt32(0) + 1).ToString("00000");
                                    strTotalAmount = dr.GetDecimal(1).ToString("0000000000000.00").Replace(".", string.Empty);

                                    strValue = string.Format("{0,-5}", strTotalRecord);
                                    strValue = strValue + string.Format("{0,-15}", strTotalAmount);
                                }
                                else
                                {
                                    strTotalRecord = dr.GetInt32(0).ToString("000000");
                                    strTotalAmount = dr.GetDecimal(1).ToString("00000000000.00").Replace(".", string.Empty);

                                    strValue = string.Format("{0,-6}", strTotalRecord);
                                    strValue = strValue + string.Format("{0,-13}", strTotalAmount);
                                }

                                SalaryAdvanceTotalList.Add(strValue.ToString().ToUpper().Trim());
                            }
                            return SalaryAdvanceTotalList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static List<string> GetEmployeeSalarynAdvanceTotalList(string Branch, DateTime Period, string EmployeeType, string BankCode, string Type, string Company, string Source)
        {
            try
            {
                List<string> SalaryAdvanceTotalList = new List<string>();

                string strSQL = string.Empty;
                string strTotalRecord = string.Empty;
                string strTotalAmount = string.Empty;
                string strValue = string.Empty;
                // SKBBKContribution is included as a deduction for salary periods after June 2026
                bool includeSKBBK = (Period.Year > 2026 || (Period.Year == 2026 && Period.Month > 6));
                using (SqlCommand cmd = new SqlCommand())
                {
                    if (Type == "SalaryAdvance")
                    {
                        strSQL = "SELECT ISNULL(Count(*),0) AS TotalRecord,ISNULL(SUM(SalaryAdvance.Amount),0) AS TotalAmount ";
                        strSQL = strSQL + "FROM SalaryAdvance ";
                        strSQL = strSQL + "INNER JOIN Employee ON SalaryAdvance.EmployeeID=Employee.EMP_ID ";
                        strSQL = strSQL + "INNER JOIN EmployeeSalaryDetails ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE ";
                        strSQL = strSQL + "WHERE SalaryAdvance.TransType=1 ";
                        strSQL = strSQL + "AND SalaryAdvance.PaymentType='BANK' ";
                        strSQL = strSQL + "AND SalaryAdvance.IsDeleted=0 ";
                        strSQL = strSQL + "AND EMP_ROLE =@EmployeeType ";
                        strSQL = strSQL + "AND Month(AdvanceDate) = @Month ";
                        strSQL = strSQL + "AND Year(AdvanceDate) = @Year ";
                        strSQL = strSQL + "AND EMPFL_BANK = @BankCode ";
                        strSQL = strSQL + "AND EMP_BRANCH_CODE = @Branch ";
                    }
                    else if (Type == "Salary")
                    {
                        strSQL = " SELECT ISNULL(COUNT(*),0) AS TOTALRECORD, ISNULL(SUM(AMOUNT),0) AS TOTALAMOUNT ";
                        strSQL = strSQL + "FROM ";
                        strSQL = strSQL + "( ";
                        strSQL = strSQL + " SELECT EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO, ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + " SUM(  ";
                            strSQL = strSQL + " (BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + " (EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                            strSQL = strSQL + " ) AS AMOUNT ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + " SUM(  ";
                            strSQL = strSQL + " (OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + " (UniformIssueRecovery+MiscDeduction) ";
                            strSQL = strSQL + " ) AS AMOUNT ";
                        }
                        else
                        {
                            strSQL = strSQL + " SUM(  ";
                            strSQL = strSQL + " (BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + " HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance+Bonus) - ";
                            strSQL = strSQL + " (EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + " MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                            strSQL = strSQL + " ) AS AMOUNT ";
                        }

                        strSQL = strSQL + " FROM ";
                        strSQL = strSQL + " ( ";
                        strSQL = strSQL + "     SELECT Distinct ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "    BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "    EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "    OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus, ";
                            strSQL = strSQL + "    UniformIssueRecovery,MiscDeduction, ";
                        }
                        else
                        {
                            strSQL = strSQL + "    BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "    HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "    EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "    MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction, ";
                            if (includeSKBBK) strSQL = strSQL + "    SKBBKContribution, ";
                        }

                        strSQL = strSQL + "    EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO ";
                        strSQL = strSQL + "    FROM VWBankSalaryStatement ";
                        strSQL = strSQL + "    WHERE SalaryPayMode ='BANK' ";
                        strSQL = strSQL + "    AND Month(Period) = @Month ";
                        strSQL = strSQL + "    AND Year(Period) = @Year ";
                        strSQL = strSQL + "    AND EMPFL_BANK =@BankCode ";
                        strSQL = strSQL + "    AND EMP_ROLE = @EmployeeType ";
                        strSQL = strSQL + "    AND CODE = @Branch ";
                        strSQL = strSQL + "    GROUP BY EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO, ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "    BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "    EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "    OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus, ";
                            strSQL = strSQL + "    UniformIssueRecovery,MiscDeduction ";
                        }
                        else
                        {
                            strSQL = strSQL + "    BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "    HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "    EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "    MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + ",SKBBKContribution";
                        }

                        strSQL = strSQL + " ) A ";
                        strSQL = strSQL + " Group By EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + " HAVING SUM( ";
                            strSQL = strSQL + " (BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + " (EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                            strSQL = strSQL + " ) > 0 ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + " HAVING SUM( ";
                            strSQL = strSQL + " (OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + " (UniformIssueRecovery+MiscDeduction) ";
                            strSQL = strSQL + " ) > 0 ";
                        }
                        else
                        {
                            strSQL = strSQL + " HAVING SUM( ";
                            strSQL = strSQL + " (BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + " HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance+Bonus) - ";
                            strSQL = strSQL + " (EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + " MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                            strSQL = strSQL + " ) > 0 ";
                        }

                        strSQL = strSQL + ")TOTAL ";
                    }

                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    cmd.Parameters.AddWithValue("@Month", Period.Month);
                    cmd.Parameters.AddWithValue("@Year", Period.Year);
                    cmd.Parameters.AddWithValue("@BankCode", BankCode);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                strTotalRecord = string.Empty;
                                strTotalAmount = string.Empty;
                                strValue = string.Empty;

                                if (BankCode.ToString().ToUpper() == "CIMB")
                                {
                                    strTotalRecord = dr.GetInt32(0).ToString("000000");
                                    strTotalAmount = dr.GetDecimal(1).ToString("00000000000.00").Replace(".", string.Empty);

                                    strValue = string.Format("{0,-6}", strTotalRecord);
                                    strValue = strValue + string.Format("{0,-13}", strTotalAmount);

                                }
                                else if (BankCode.ToString().ToUpper() == "BSN")
                                {
                                    strTotalRecord = (1 + dr.GetInt32(0) + 1).ToString("00000");
                                    strTotalAmount = dr.GetDecimal(1).ToString("0000000000000.00").Replace(".", string.Empty);

                                    strValue = string.Format("{0,-5}", strTotalRecord);
                                    strValue = strValue + string.Format("{0,-15}", strTotalAmount);
                                }
                                else
                                {
                                    strTotalRecord = dr.GetInt32(0).ToString("000000");
                                    strTotalAmount = dr.GetDecimal(1).ToString("00000000000.00").Replace(".", string.Empty);

                                    strValue = string.Format("{0,-6}", strTotalRecord);
                                    strValue = strValue + string.Format("{0,-13}", strTotalAmount);
                                }

                                SalaryAdvanceTotalList.Add(strValue.ToString().ToUpper().Trim());
                            }
                            return SalaryAdvanceTotalList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static List<string> GetEmployeeSalarynAdvanceHashTotalList(DateTime Period, string EmployeeType, string BankCode, string Type, string Company, string Source)
        {
            try
            {
                List<string> SalaryAdvanceHashTotalList = new List<string>();

                string strSQL = string.Empty;
                string strHashTotalAmount = string.Empty;
                string strValue = string.Empty;
                // SKBBKContribution is included as a deduction for salary periods after June 2026
                bool includeSKBBK = (Period.Year > 2026 || (Period.Year == 2026 && Period.Month > 6));

                using (SqlCommand cmd = new SqlCommand())
                {

                    if (Type == "SalaryAdvance")
                    {
                        strSQL = "SELECT ISNULL(RIGHT(RTRIM(SUM(ISNULL(TRY_CONVERT(INT,RIGHT(RTRIM(REPLACE(EMPFL_BK_ACCNO, '-', '')), 4)),0))), 4),0) AS HashTotal ";
                        strSQL = strSQL + "FROM SalaryAdvance ";
                        strSQL = strSQL + "INNER JOIN Employee ON SalaryAdvance.EmployeeID=Employee.EMP_ID ";
                        strSQL = strSQL + "INNER JOIN EmployeeSalaryDetails ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE ";
                        strSQL = strSQL + "WHERE SalaryAdvance.TransType=1 ";
                        strSQL = strSQL + "AND SalaryAdvance.PaymentType='BANK' ";
                        strSQL = strSQL + "AND SalaryAdvance.IsDeleted=0 ";
                        strSQL = strSQL + "AND Month(AdvanceDate) = @Month ";
                        strSQL = strSQL + "AND Year(AdvanceDate) = @Year ";
                        strSQL = strSQL + "AND EMPFL_BANK = @BankCode ";
                        strSQL = strSQL + "AND EMP_ROLE =@EmployeeType ";
                    }
                    else if (Type == "Salary")
                    {
                        strSQL = " SELECT ISNULL(RIGHT(RTRIM(SUM(ISNULL(TRY_CONVERT(INT,RIGHT(RTRIM(REPLACE(EMP_BK_ACCNO, '-', '')), 4)),0))), 4),0) AS HashTotal ";
                        strSQL = strSQL + "FROM ";
                        strSQL = strSQL + "( ";
                        strSQL = strSQL + "SELECT  ";
                        strSQL = strSQL + "SUM(  ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery) ";
                            strSQL = strSQL + ") AS AMOUNT, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "(OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + "(UniformIssueRecovery+MiscDeduction) ";
                            strSQL = strSQL + ") AS AMOUNT, ";
                        }
                        else
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + "HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance+Bonus) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                            strSQL = strSQL + ") AS AMOUNT, ";
                        }

                        strSQL = strSQL + "EMP_NAME,EMP_SecurityNo,EMP_IC_OLD,  ";
                        strSQL = strSQL + "EMP_IC_NEW, EMP_PASSPORT_NO,EMP_BK_ACCNO ";
                        strSQL = strSQL + "FROM ";
                        strSQL = strSQL + "( ";
                        strSQL = strSQL + "SELECT Distinct ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus, ";
                            strSQL = strSQL + "UniformIssueRecovery,MiscDeduction, ";
                        }
                        else
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction, ";
                            if (includeSKBBK) strSQL = strSQL + "SKBBKContribution, ";
                        }

                        strSQL = strSQL + "EMP_NAME, ";
                        strSQL = strSQL + "(CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN ";
                        strSQL = strSQL + "CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' ";
                        strSQL = strSQL + "THEN REPLACE(EMP_PASSPORT_NO,'-','') ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                        strSQL = strSQL + "ELSE REPLACE(EMP_IC_NEW,'-','') END) AS EMP_SecurityNo, ";
                        strSQL = strSQL + "EMP_IC_OLD, EMP_IC_NEW, EMP_PASSPORT_NO, ";
                        strSQL = strSQL + "REPLACE(EMPFL_BK_ACCNO,'-','') AS EMP_BK_ACCNO ";
                        strSQL = strSQL + "FROM VWBankSalaryStatement ";
                        strSQL = strSQL + "WHERE SalaryPayMode ='BANK' ";
                        strSQL = strSQL + "AND Month(Period) = @Month ";
                        strSQL = strSQL + "AND Year(Period) = @Year ";
                        strSQL = strSQL + "AND EMPFL_BANK =@BankCode ";
                        strSQL = strSQL + "AND EMP_ROLE = @EmployeeType ";
                        strSQL = strSQL + "GROUP BY EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO, ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus, ";
                            strSQL = strSQL + "UniformIssueRecovery,MiscDeduction ";
                        }
                        else
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction";
                            if (includeSKBBK) strSQL = strSQL + ",SKBBKContribution";
                        }

                        strSQL = strSQL + ") A ";
                        strSQL = strSQL + "Group By EMP_NAME,EMP_SecurityNo,EMP_IC_OLD,EMP_IC_NEW, EMP_PASSPORT_NO,EMP_BK_ACCNO ";
                        strSQL = strSQL + "HAVING SUM( ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery) ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "(OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + "(UniformIssueRecovery+MiscDeduction) ";
                        }
                        else
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + "HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance+Bonus) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                        }

                        strSQL = strSQL + ") > 0 ";
                        strSQL = strSQL + ")TOTAL ";
                    }

                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    cmd.Parameters.AddWithValue("@Month", Period.Month);
                    cmd.Parameters.AddWithValue("@Year", Period.Year);
                    cmd.Parameters.AddWithValue("@BankCode", BankCode);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                strHashTotalAmount = string.Empty;
                                strValue = string.Empty;

                                if (BankCode.ToString().ToUpper() == "CIMB")
                                {
                                    strHashTotalAmount = dr.GetString(0);
                                    strValue = string.Format("{0,-4}", strHashTotalAmount);
                                }
                                else if (BankCode.ToString().ToUpper() == "BSN")
                                {
                                    strHashTotalAmount = dr.GetString(0);
                                    strValue = string.Format("{0,-4}", strHashTotalAmount);
                                }
                                else
                                {
                                    strHashTotalAmount = dr.GetString(0);
                                    strValue = string.Format("{0,-4}", strHashTotalAmount);
                                }

                                SalaryAdvanceHashTotalList.Add(strValue.ToString().ToUpper().Trim());
                            }
                            return SalaryAdvanceHashTotalList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static List<string> GetEmployeeSalarynAdvanceHashTotalList(string Branch, DateTime Period, string EmployeeType, string BankCode, string Type, string Company, string Source)
        {
            try
            {
                List<string> SalaryAdvanceHashTotalList = new List<string>();

                string strSQL = string.Empty;
                string strHashTotalAmount = string.Empty;
                string strValue = string.Empty;
                // SKBBKContribution is included as a deduction for salary periods after June 2026
                bool includeSKBBK = (Period.Year > 2026 || (Period.Year == 2026 && Period.Month > 6));
                using (SqlCommand cmd = new SqlCommand())
                {
                    if (Type == "SalaryAdvance")
                    {
                        strSQL = "SELECT ISNULL(RIGHT(RTRIM(SUM(ISNULL(TRY_CONVERT(INT,RIGHT(RTRIM(REPLACE(EMPFL_BK_ACCNO, '-', '')), 4)),0))), 4),0) AS HashTotal ";
                        strSQL = strSQL + "FROM SalaryAdvance ";
                        strSQL = strSQL + "INNER JOIN Employee ON SalaryAdvance.EmployeeID=Employee.EMP_ID ";
                        strSQL = strSQL + "INNER JOIN EmployeeSalaryDetails ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE ";
                        strSQL = strSQL + "WHERE SalaryAdvance.TransType=1 ";
                        strSQL = strSQL + "AND SalaryAdvance.PaymentType='BANK' ";
                        strSQL = strSQL + "AND SalaryAdvance.IsDeleted=0 ";
                        strSQL = strSQL + "AND Month(AdvanceDate) = @Month ";
                        strSQL = strSQL + "AND Year(AdvanceDate) = @Year ";
                        strSQL = strSQL + "AND EMPFL_BANK =@BankCode ";
                        strSQL = strSQL + "AND EMP_ROLE = @EmployeeType ";
                        strSQL = strSQL + "AND EMP_BRANCH_CODE = @Branch ";
                    }
                    else if (Type == "Salary")
                    {
                        strSQL = " SELECT ISNULL(RIGHT(RTRIM(SUM(ISNULL(TRY_CONVERT(INT,RIGHT(RTRIM(REPLACE(EMP_BK_ACCNO, '-', '')), 4)),0))), 4),0) AS HashTotal ";
                        strSQL = strSQL + "FROM ";
                        strSQL = strSQL + "( ";
                        strSQL = strSQL + "SELECT  ";
                        strSQL = strSQL + "SUM(  ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery) ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "(OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + "(UniformIssueRecovery+MiscDeduction) ";
                        }
                        else
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + "HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance+Bonus) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                        }

                        strSQL = strSQL + ") AS AMOUNT, ";
                        strSQL = strSQL + "EMP_NAME,EMP_SecurityNo,EMP_IC_OLD,  ";
                        strSQL = strSQL + "EMP_IC_NEW, EMP_PASSPORT_NO,EMP_BK_ACCNO ";
                        strSQL = strSQL + "FROM ";
                        strSQL = strSQL + "( ";
                        strSQL = strSQL + "SELECT Distinct ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus, ";
                            strSQL = strSQL + "UniformIssueRecovery,MiscDeduction, ";
                        }
                        else
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction, ";
                            if (includeSKBBK) strSQL = strSQL + "SKBBKContribution, ";
                        }

                        strSQL = strSQL + "EMP_NAME, ";
                        strSQL = strSQL + "(CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN ";
                        strSQL = strSQL + "CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' ";
                        strSQL = strSQL + "THEN REPLACE(EMP_PASSPORT_NO,'-','') ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                        strSQL = strSQL + "ELSE REPLACE(EMP_IC_NEW,'-','') END) AS EMP_SecurityNo, ";
                        strSQL = strSQL + "EMP_IC_OLD, EMP_IC_NEW, EMP_PASSPORT_NO, ";
                        strSQL = strSQL + "REPLACE(EMPFL_BK_ACCNO,'-','') AS EMP_BK_ACCNO ";
                        strSQL = strSQL + "FROM VWBankSalaryStatement ";
                        strSQL = strSQL + "WHERE SalaryPayMode ='BANK' ";
                        strSQL = strSQL + "AND Month(Period) = @Month ";
                        strSQL = strSQL + "AND Year(Period) = @Year ";
                        strSQL = strSQL + "AND EMPFL_BANK =@BankCode ";
                        strSQL = strSQL + "AND EMP_ROLE = @EmployeeType ";
                        strSQL = strSQL + "AND Code = @Branch ";
                        strSQL = strSQL + "GROUP BY EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO, ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance,ReAllowance,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "OffDaySalary,OffDayOverTimeSalary,Shift2Salary,MiscAmount,Bonus, ";
                            strSQL = strSQL + "UniformIssueRecovery,MiscDeduction ";
                        }
                        else
                        {
                            strSQL = strSQL + "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            strSQL = strSQL + "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            strSQL = strSQL + "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction";
                            if (includeSKBBK) strSQL = strSQL + ",SKBBKContribution";
                        }

                        strSQL = strSQL + ") A ";
                        strSQL = strSQL + "Group By EMP_NAME,EMP_SecurityNo,EMP_IC_OLD,EMP_IC_NEW, EMP_PASSPORT_NO,EMP_BK_ACCNO ";
                        strSQL = strSQL + "HAVING SUM( ";

                        if (Source == "SalaryGuard1")
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance+ReAllowance+SpecialAllowance) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery) ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            strSQL = strSQL + "(OffDaySalary+OffDayOverTimeSalary+Shift2Salary+MiscAmount+Bonus) - ";
                            strSQL = strSQL + "(UniformIssueRecovery+MiscDeduction) ";
                        }
                        else
                        {
                            strSQL = strSQL + "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            strSQL = strSQL + "HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance+Bonus) - ";
                            strSQL = strSQL + "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            strSQL = strSQL + "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction";
                            if (includeSKBBK) strSQL = strSQL + "+SKBBKContribution";
                            strSQL = strSQL + ") ";
                        }

                        strSQL = strSQL + ") > 0 ";
                        strSQL = strSQL + ")TOTAL ";
                    }

                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    cmd.Parameters.AddWithValue("@Month", Period.Month);
                    cmd.Parameters.AddWithValue("@Year", Period.Year);
                    cmd.Parameters.AddWithValue("@BankCode", BankCode);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                strHashTotalAmount = string.Empty;
                                strValue = string.Empty;

                                if (BankCode.ToString().ToUpper() == "CIMB")
                                {
                                    strHashTotalAmount = dr.GetString(0);
                                    strValue = string.Format("{0,-4}", strHashTotalAmount);
                                }
                                else if (BankCode.ToString().ToUpper() == "BSN")
                                {
                                    strHashTotalAmount = dr.GetString(0);
                                    strValue = string.Format("{0,-4}", strHashTotalAmount);
                                }
                                else
                                {
                                    strHashTotalAmount = dr.GetString(0);
                                    strValue = string.Format("{0,-4}", strHashTotalAmount);
                                }

                                SalaryAdvanceHashTotalList.Add(strValue.ToString().ToUpper().Trim());
                            }
                            return SalaryAdvanceHashTotalList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static bool CheckExistAdvance(int EmployeeID, DateTime AdvanceDate, int LoanType)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " SELECT ID,AdvanceDate,EmployeeID,TransType FROM SalaryAdvance WHERE  AdvanceDate = @AdvanceDate AND EmployeeID = @EmployeeID AND TransType=@TransType AND IsDeleted=0 ";
                    cmd.Parameters.AddWithValue("@AdvanceDate", AdvanceDate);
                    cmd.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                    cmd.Parameters.AddWithValue("@TransType", LoanType);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                return true;
                            }
                            return false;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static string NewAdvanceVoucherNo(string Branch, int TransType)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " SELECT ISNULL(MAX(CAST(VoucherNo AS INT))+1,1) AS NEWVOUCHERNO FROM SalaryAdvance INNER JOIN Employee ON Employee.Emp_ID=SalaryAdvance.EmployeeID WHERE Employee.Emp_Branch_Code = @Branch AND TransType=@TransType";
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    cmd.Parameters.AddWithValue("@TransType", TransType);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                return dr.GetInt32(0).ToString("00000000");
                            }
                            return string.Empty;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static string GetNextChequeNumber(decimal Account)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    StringBuilder sbQuery = new StringBuilder();
                    sbQuery.Append("SELECT CASE ");
                    sbQuery.Append("WHEN ISNULL(MaxCheque.ChequeNo,0) >= ChequeBook.ChequeStart AND MaxCheque.ChequeNo<ChequeBook.ChequeEnd THEN MaxCheque.ChequeNo+1 ");
                    sbQuery.Append("WHEN ISNULL(MaxCheque.ChequeNo,0) < ChequeBook.ChequeStart THEN ChequeBook.ChequeStart ");
                    sbQuery.Append("WHEN ISNULL(MaxCheque.ChequeNo,0) = ChequeBook.ChequeEnd THEN NULL ");
                    sbQuery.Append("    ELSE NULL ");
                    sbQuery.Append(" END ");
                    sbQuery.Append("FROM ");
                    //sbQuery.Append("(SELECT BranchPayments.BankID,Max(BranchPayments.ChequeNo)as ChequeNo FROM BranchPayments INNER JOIN ChequeMaster ON ChequeMaster.BankID=BranchPayments.BankID AND IsActive=1 and isnumeric(Chequeno)=1 WHERE BranchPayments.BankID=@Bank AND ChequeNo BETWEEN ChequeMaster.ChequeStart AND ChequeMaster.ChequeEnd GROUP BY BranchPayments.BankID) MaxCheque RIGHT OUTER JOIN ");
                    sbQuery.Append("(SELECT BranchPayments.BankID,Max(Convert(numeric(18,0),BranchPayments.ChequeNo))as ChequeNo FROM BranchPayments INNER JOIN ChequeMaster ON ChequeMaster.BankID=BranchPayments.BankID AND IsActive=1 and isnumeric(Chequeno)=1 WHERE BranchPayments.BankID=@Bank AND ChequeNo BETWEEN ChequeMaster.ChequeStart AND ChequeMaster.ChequeEnd GROUP BY BranchPayments.BankID) MaxCheque RIGHT OUTER JOIN ");
                    sbQuery.Append("(SELECT BankID,ChequeStart,ChequeEnd FROM ChequeMaster WHERE BankID=@Bank AND IsActive=1) ChequeBook ON MaxCheque.BankID = ChequeBook.BankID ");
                    cmd.CommandText = sbQuery.ToString();
                    cmd.Parameters.AddWithValue("@Bank", Account);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                if (dr[0] != DBNull.Value)
                                    return dr.GetDecimal(0).ToString("#############0");
                                else
                                    return string.Empty;
                            }
                            return string.Empty;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static decimal GetNoOfCheques(decimal Account)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    StringBuilder sbQuery = new StringBuilder();
                    sbQuery.Append("SELECT CASE ");
                    sbQuery.Append("WHEN ISNULL(MaxCheque.ChequeNo,0) >= ChequeBook.ChequeStart AND MaxCheque.ChequeNo<ChequeBook.ChequeEnd THEN ChequeBook.ChequeEnd - (MaxCheque.ChequeNo+1) ");
                    sbQuery.Append("WHEN ISNULL(MaxCheque.ChequeNo,0) < ChequeBook.ChequeStart THEN ChequeBook.ChequeEnd - (ChequeBook.ChequeStart +1) ");
                    sbQuery.Append("WHEN ISNULL(MaxCheque.ChequeNo,0) = ChequeBook.ChequeEnd THEN 0 ");
                    sbQuery.Append("    ELSE NULL ");
                    sbQuery.Append(" END ");
                    sbQuery.Append("FROM ");
                    //sbQuery.Append("(SELECT BranchPayments.BankID,Max(BranchPayments.ChequeNo)as ChequeNo FROM BranchPayments INNER JOIN ChequeMaster ON ChequeMaster.BankID=BranchPayments.BankID AND IsActive=1 AND isnumeric(Chequeno)=1 WHERE BranchPayments.BankID=@Bank AND ChequeNo BETWEEN ChequeMaster.ChequeStart AND ChequeMaster.ChequeEnd GROUP BY BranchPayments.BankID) MaxCheque RIGHT OUTER JOIN ");
                    sbQuery.Append("(SELECT BranchPayments.BankID,Max(Convert(numeric(18,0),BranchPayments.ChequeNo))as ChequeNo FROM BranchPayments INNER JOIN ChequeMaster ON ChequeMaster.BankID=BranchPayments.BankID AND IsActive=1 AND isnumeric(Chequeno)=1 WHERE BranchPayments.BankID=@Bank AND ChequeNo BETWEEN ChequeMaster.ChequeStart AND ChequeMaster.ChequeEnd GROUP BY BranchPayments.BankID) MaxCheque RIGHT OUTER JOIN ");
                    sbQuery.Append("(SELECT BankID,ChequeStart,ChequeEnd FROM ChequeMaster WHERE BankID=@Bank AND IsActive=1) ChequeBook ON MaxCheque.BankID = ChequeBook.BankID ");
                    cmd.CommandText = sbQuery.ToString();
                    cmd.CommandText = sbQuery.ToString();
                    cmd.Parameters.AddWithValue("@Bank", Account);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                if (dr[0] != DBNull.Value)
                                    return dr.GetDecimal(0);
                                else
                                    return 0;
                            }
                            return 0;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static string GetBankCodeByBankID(decimal BankID)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " SELECT BankCode FROM BankMaster WHERE BankID = @BankID";
                    cmd.Parameters.AddWithValue("@BankID", BankID);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                return dr.GetString(0).ToString();
                            }
                            return string.Empty;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static string NewReceiptVoucherNoByYear(decimal BankID, int ReceiptDateYear)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    //cmd.CommandText = " SELECT ISNULL(MAX(CAST(VoucherNo AS INT))+1,1) AS NEWVOUCHERNO FROM Receipts WHERE ISNULL(BankID,0)=@BankID AND Year(ReceiptDate)=@Year";
                    cmd.CommandText = " SELECT ISNULL(MAX(CAST(VoucherNo AS INT))+1,1) AS NEWVOUCHERNO FROM Receipts WHERE ISNULL(BankID,0)=@BankID AND Year(ReceiptDate) > 2012";
                    cmd.Parameters.AddWithValue("@BankID", BankID);
                    cmd.Parameters.AddWithValue("@Year", ReceiptDateYear);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                return dr.GetInt32(0).ToString("##########");
                            }
                            return string.Empty;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static List<string> GetEmployeeLoanList(DateTime Period, string Branch, string EmployeeType, int TransType)
        {
            try
            {
                List<string> list = new List<string>();
                using SqlCommand sqlCommand = new SqlCommand();
                sqlCommand.CommandText = " SELECT Emp_Code FROM SalaryAdvance INNER JOIN Employee ON Employee.EMP_ID = SalaryAdvance.EmployeeID WHERE AdvanceDate = @AdvanceDate AND EMP_BRANCH_CODE = @Branch AND EMP_ROLE =@EmployeeType AND TransType=@TransType And SalaryAdvance.IsDeleted = 0 ";
                sqlCommand.Parameters.AddWithValue("@AdvanceDate", Period);
                sqlCommand.Parameters.AddWithValue("@Branch", Branch);
                sqlCommand.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                sqlCommand.Parameters.AddWithValue("@TransType", TransType);
                using SQLDataAccess sQLDataAccess = new SQLDataAccess(configuration);
                using SqlDataReader sqlDataReader = sQLDataAccess.RetrieveData(sqlCommand);
                while (sqlDataReader.Read())
                {
                    list.Add(sqlDataReader.GetString(0));
                }

                return list;
            }
            catch
            {
                throw;
            }
        }

        public static List<BankStatementExcelDto> GetSalaryList(DateTime Period, string Branch, string Client, string Bank, string PaymentType, string EmployeeType, string EmpTempType, string Source)
        {
            try
            {
                List<BankStatementExcelDto> list = new List<BankStatementExcelDto>();
                string commandText = string.Empty;
                string empty = string.Empty;
                string empty2 = string.Empty;
                string empty3 = string.Empty;
                string empty4 = string.Empty;
                string empty5 = string.Empty;
                string empty6 = string.Empty;
                decimal num = 0m;
                string empty7 = string.Empty;
                string empty8 = string.Empty;
                // SKBBKContribution is included as a deduction for salary periods after June 2026
                bool includeSKBBK = (Period.Year > 2026 || (Period.Year == 2026 && Period.Month > 6));
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    if (PaymentType == "SalaryAdvance")
                    {
                        commandText = "SELECT SalaryAdvance.Amount,Employee.EMP_NAME AS EmployeeName, ";
                        commandText += "(CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN ";
                        commandText += "CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' ";
                        commandText += "THEN REPLACE(EMP_PASSPORT_NO,'-','') ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                        commandText += "ELSE REPLACE(EMP_IC_NEW,'-','') END) AS EMP_SecurityNo, ";
                        commandText += "REPLACE(EmployeeSalaryDetails.EMPFL_BK_ACCNO,'-','') AS EMP_BK_ACCNO  ";
                        commandText += "FROM SalaryAdvance  ";
                        commandText += "INNER JOIN Employee ON SalaryAdvance.EmployeeID=Employee.EMP_ID  ";
                        commandText += "INNER JOIN EmployeeSalaryDetails ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE  ";
                        commandText += "WHERE SalaryAdvance.TransType=1  ";
                        commandText += "AND SalaryAdvance.PaymentType='BANK'  ";
                        commandText += "AND SalaryAdvance.IsDeleted=0  ";
                        commandText += "AND EMP_ROLE=@EmployeeType  ";
                        commandText += "AND Month(AdvanceDate)=@Month ";
                        commandText += "AND Year(AdvanceDate)=@Year ";
                        if (!(Bank == "0") && !(Bank == ""))
                        {
                            commandText += "AND EMPFL_BANK=@BankCode ";
                        }

                        if (!(Branch == "0") && !(Branch == ""))
                        {
                            commandText += "AND EMP_BRANCH_CODE = @Branch ";
                        }

                        commandText += "ORDER BY EMP_NAME  ";
                    }
                    else if (PaymentType == "Salary")
                    {
                        commandText = "SELECT  ";
                        if (Source == "SalaryGuard1")
                        {
                            commandText += "SUM(  ";
                            commandText += "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance) - ";
                            commandText += "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SpecialAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                            commandText += ") AS AMOUNT, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            commandText += "SUM(  ";
                            commandText += "(Shift2Salary+MiscAmount+Bonus+ReAllowance+SpecialAllowance) - ";
                            commandText += "(UniformIssueRecovery+MiscDeduction) ";
                            commandText += ") AS AMOUNT, ";
                        }
                        else
                        {
                            commandText += "SUM(  ";
                            commandText += "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            commandText += "HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance + Bonus) - ";
                            commandText += "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            commandText += "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) commandText += "+SKBBKContribution";
                            commandText += ") ";
                            commandText += ") AS AMOUNT, ";
                        }

                        commandText += "EMP_NAME AS EmployeeName,  ";
                        commandText += "EMP_SecurityNo, ";
                        commandText += "EMP_BK_ACCNO ";
                        commandText += "FROM ";
                        commandText += "( ";
                        commandText += "SELECT Distinct ";
                        if (Source == "SalaryGuard1")
                        {
                            commandText += "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance, ";
                            commandText += "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SpecialAdvanceRecovery,SIPEmployeeContribution,MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction, ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            commandText += "Shift2Salary,MiscAmount,Bonus,SpecialAllowance,ReAllowance, ";
                            commandText += "UniformIssueRecovery,MiscDeduction, ";
                        }
                        else
                        {
                            commandText += "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            commandText += "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            commandText += "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            commandText += "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction, ";
                            if (includeSKBBK) commandText += "SKBBKContribution, ";
                        }

                        commandText += "EMP_NAME, ";
                        commandText += "(CASE WHEN EMP_IC_NEW is null or EMP_IC_NEW = '' THEN ";
                        commandText += "CASE WHEN EMP_IC_OLD is null or EMP_IC_OLD = '' ";
                        commandText += "THEN REPLACE(EMP_PASSPORT_NO,'-','') ELSE REPLACE(EMP_IC_OLD,'-','') END ";
                        commandText += "ELSE REPLACE(EMP_IC_NEW,'-','') END) AS EMP_SecurityNo, ";
                        commandText += "EMP_IC_OLD, EMP_IC_NEW, EMP_PASSPORT_NO, ";
                        commandText += "REPLACE(EMPFL_BK_ACCNO,'-','') AS EMP_BK_ACCNO ";
                        commandText += "FROM VWBankSalaryStatement ";
                        commandText += "WHERE SalaryPayMode ='BANK' ";
                        commandText += "AND Month(Period) = @Month ";
                        commandText += "AND Year(Period) = @Year ";
                        if (!(Bank == "0") && !(Bank == ""))
                        {
                            commandText += "AND EMPFL_BANK =@BankCode ";
                        }

                        commandText += "AND EMP_ROLE = @EmployeeType ";
                        if (!(Branch == "0") && !(Branch == ""))
                        {
                            commandText += "AND CODE = @Branch ";
                        }

                        commandText += "GROUP BY EMP_NAME,EMP_IC_OLD,EMP_IC_NEW,EMP_PASSPORT_NO,EMPFL_BK_ACCNO, ";
                        if (Source == "SalaryGuard1")
                        {
                            commandText += "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary,HolidayOverTimeSalary,AttendanceAllowance, ";
                            commandText += "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,MonthlyAdvanceRecovery,LoanRecovery,IncomeTaxDeduction ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            commandText += "Shift2Salary,MiscAmount,Bonus,ReAllowance,SpecialAllowance, ";
                            commandText += "UniformIssueRecovery,MiscDeduction ";
                        }
                        else
                        {
                            commandText += "BasicSalary,OverTimeSalary,OffDaySalary,OffDayOverTimeSalary,HolidaySalary, ";
                            commandText += "HolidayOverTimeSalary,Shift2Salary,AttendanceAllowance,ReAllowance,MiscAmount,SpecialAllowance, ";
                            commandText += "EPFDeductionAmount,SOCSODeductionAmount,DailyAdvanceRecovery,SIPEmployeeContribution,Bonus, ";
                            commandText += "MonthlyAdvanceRecovery,SpecialAdvanceRecovery,UniformIssueRecovery,LoanRecovery,MiscDeduction,IncomeTaxDeduction";
                            if (includeSKBBK) commandText += ",SKBBKContribution";
                            commandText += " ";
                        }

                        commandText += ") A ";
                        commandText += "Group By EMP_NAME,EMP_SecurityNo,EMP_BK_ACCNO ";
                        commandText += "HAVING SUM( ";
                        if (Source == "SalaryGuard1")
                        {
                            commandText += "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+HolidayOverTimeSalary+AttendanceAllowance) - ";
                            commandText += "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SpecialAdvanceRecovery+SIPEmployeeContribution+MonthlyAdvanceRecovery+LoanRecovery+IncomeTaxDeduction) ";
                        }
                        else if (Source == "SalaryGuard2")
                        {
                            commandText += "(Shift2Salary+MiscAmount+Bonus+ReAllowance+SpecialAllowance) - ";
                            commandText += "(UniformIssueRecovery+MiscDeduction) ";
                        }
                        else
                        {
                            commandText += "(BasicSalary+OverTimeSalary+OffDaySalary+OffDayOverTimeSalary+HolidaySalary+ ";
                            commandText += "HolidayOverTimeSalary+Shift2Salary+AttendanceAllowance+ReAllowance+MiscAmount+SpecialAllowance+Bonus) - ";
                            commandText += "(EPFDeductionAmount+SOCSODeductionAmount+DailyAdvanceRecovery+SIPEmployeeContribution+ ";
                            commandText += "MonthlyAdvanceRecovery+SpecialAdvanceRecovery+UniformIssueRecovery+LoanRecovery+MiscDeduction+IncomeTaxDeduction";
                            if (includeSKBBK) commandText += "+SKBBKContribution";
                            commandText += ") ";
                        }

                        commandText += ") > 0 ";
                        commandText += " ORDER BY EMP_NAME ";
                    }

                    sqlCommand.CommandText = commandText;
                    sqlCommand.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    sqlCommand.Parameters.AddWithValue("@Month", Period.Month);
                    sqlCommand.Parameters.AddWithValue("@Year", Period.Year);
                    sqlCommand.Parameters.AddWithValue("@BankCode", Bank);
                    sqlCommand.Parameters.AddWithValue("@Branch", Branch);
                    using SQLDataAccess sQLDataAccess = new SQLDataAccess(configuration);
                    using SqlDataReader sqlDataReader = sQLDataAccess.RetrieveData(sqlCommand);
                    while (sqlDataReader.Read())
                    {
                        empty5 = string.Empty;
                        empty6 = string.Empty;
                        num = 0m;
                        empty8 = string.Empty;
                        empty7 = string.Empty;
                        num = ((sqlDataReader["Amount"] == DBNull.Value) ? 0m : sqlDataReader.GetDecimal(0));
                        empty6 = ((sqlDataReader["EmployeeName"] == DBNull.Value) ? string.Empty : sqlDataReader.GetString(1));
                        empty5 = ((sqlDataReader["EMP_SecurityNo"] == DBNull.Value) ? string.Empty : ("'" + sqlDataReader.GetString(2)));
                        empty8 = ((sqlDataReader["EMP_BK_ACCNO"] == DBNull.Value) ? string.Empty : ("'" + sqlDataReader.GetString(3)));
                        list.Add(new BankStatementExcelDto(empty6, empty8.ToString(), num.ToString("F2"), empty5));
                    }
                }

                return list;
            }
            catch
            {
                throw;
            }
        }

        public static List<BranchPaymentsDto> GetPaymentListByCategory(string CategoryID,DateTime dtStart,DateTime dtEnd)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = "SELECT ";
                    cmd.CommandText += " BranchPayments.VoucherNo, ";
                    cmd.CommandText += " BranchPayments.PaymentDate, ";
                    cmd.CommandText += " Suppliers.Name As SupplierName, ";
                    cmd.CommandText += " BankMaster.BankCode, ";
                    cmd.CommandText += " BranchPayments.Chequeno, ";
                    cmd.CommandText += " BranchPayments.PaymentTo, ";
                    cmd.CommandText += " BranchPayments.Particulars, ";
                    cmd.CommandText += " BranchPayments.Amount, ";
                    cmd.CommandText += " InventoryCategory.Name As Category, ";
                    cmd.CommandText += " BranchPayments.ItemCategory ";
                    cmd.CommandText += " FROM BranchPayments ";
                    cmd.CommandText += " INNER JOIN InventoryCategory ON InventoryCategory.ID = ItemCategory ";
                    cmd.CommandText += " INNER JOIN suppliers ON Suppliers.ID = Supplier ";
                    cmd.CommandText += " INNER JOIN BankMaster ON BankMaster.BankID = BranchPayments.BankID ";
                    cmd.CommandText += " WHERE PaymentDate BETWEEN @dtStart AND @dtEnd ";
                    cmd.CommandText += " AND IsDeleted = 0 ";
                    if (!string.IsNullOrEmpty(CategoryID))
                    {
                        cmd.CommandText += " AND ItemCategory = @CategoryID ";
                        cmd.Parameters.AddWithValue("@CategoryID", (object)CategoryID);
                    }
                    cmd.CommandText += " Order By InventoryCategory.Name,BranchPayments.PaymentDate ";
                    cmd.Parameters.AddWithValue("@dtStart", (object)dtStart);
                    cmd.Parameters.AddWithValue("@dtEnd", (object)dtEnd);
                    using (SQLDataAccess sqlDataAccess = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader sqlDataReader = sqlDataAccess.RetrieveData(cmd))
                        {
                            List<BranchPaymentsDto> list = new List<BranchPaymentsDto>();
                            while (sqlDataReader.Read())
                            {
                                if (sqlDataReader["ChequeNo"] != DBNull.Value)
                                    list.Add(new BranchPaymentsDto(sqlDataReader.GetString(sqlDataReader.GetOrdinal("VoucherNo")), sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal("PaymentDate")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("SupplierName")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("BankCode")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("ChequeNo")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("PaymentTo")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("Particulars")), sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("Amount")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category"))));
                                else
                                    list.Add(new BranchPaymentsDto(sqlDataReader.GetString(sqlDataReader.GetOrdinal("VoucherNo")), sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal("PaymentDate")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("SupplierName")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("BankCode")), string.Empty, sqlDataReader.GetString(sqlDataReader.GetOrdinal("PaymentTo")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("Particulars")), sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("Amount")), sqlDataReader.GetString(sqlDataReader.GetOrdinal("Category"))));
                            }
                            return list;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static Decimal GetBranchPaymentsTotalAmountByCategory(string CategoryID,DateTime dtStart, DateTime dtEnd)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(" SELECT SUM(Amount) As TotalAmount ");
                    stringBuilder.Append(" FROM BranchPayments ");
                    stringBuilder.Append(" WHERE IsDeleted = 0 ");
                    stringBuilder.Append(" AND PaymentDate BETWEEN @dtStart AND @dtEnd ");
                    if (!string.IsNullOrEmpty(CategoryID))
                    {
                        stringBuilder.Append(" AND ItemCategory = @CategoryID ");
                        cmd.Parameters.AddWithValue("@CategoryID", (object)CategoryID);
                    }
                    cmd.CommandText = stringBuilder.ToString();
                    cmd.Parameters.AddWithValue("@dtStart", (object)dtStart);
                    cmd.Parameters.AddWithValue("@dtEnd", (object)dtEnd);
                    using (SQLDataAccess sqlDataAccess = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader sqlDataReader = sqlDataAccess.RetrieveData(cmd))
                        {
                            if (!sqlDataReader.Read())
                                return 0M;
                            return sqlDataReader[0] != DBNull.Value ? sqlDataReader.GetDecimal(0) : 0M;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static List<ClientInvoiceListDto> GetMonthlyInvoiseList(DateTime InvoiceStartPeriod, DateTime InvoiceEndPeriod)
        {
            try
            {
                using SqlCommand sqlCommand = new SqlCommand();
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(" SELECT C.invoicedate as InvDate,convert(decimal,0) AS Row, Convert(varchar(20),C.invoicedate,106) as invoicedate,A.Name as BranchName,B.Name as ClientName,C.Branch + ' ' + C.InvoiceNo as InvoiceNumber ,c.ServiceCharges,C.Discount,c.TaxAmount,c.InvoiceAmount,Case when PAYMENT IS null then 'NO' when PAYMENT = InvoiceAmount then 'YES' else 'Partial' end as payment, ISNULL(AG.WorkPlace,'') as WorkPlace ");
                stringBuilder.Append(" FROM BranchMaster A  ");
                stringBuilder.Append(" INNER JOIN ClientMaster B ON A.Code=B.Branch  ");
                stringBuilder.Append(" INNER JOIN InvoiceDetails C ON B.Code=C.Client AND A.Code=C.Branch  ");
                stringBuilder.Append(" LEFT JOIN Agreement AG ON AG.ID = C.AgreementID ");
                stringBuilder.Append(" WHERE C.Branch IN  ");
                stringBuilder.Append(" ('PF-030-KEM', ");
                stringBuilder.Append(" 'PF005', ");
                stringBuilder.Append(" 'PF021-G', ");
                stringBuilder.Append(" 'PF022-JBSU', ");
                stringBuilder.Append(" 'PF023-IPH', ");
                stringBuilder.Append(" 'PF028-MEL', ");
                stringBuilder.Append(" 'PF035-HARI', ");
                stringBuilder.Append(" 'PF036-SABAH') and C.InvoiceDate between @InvoiceStartPeriod and @InvoiceEndPeriod ");
                stringBuilder.Append(" ORDER BY InvDate,C.InvoiceNo  ");
                sqlCommand.CommandText = stringBuilder.ToString();
                sqlCommand.Parameters.AddWithValue("@InvoiceStartPeriod", InvoiceStartPeriod);
                sqlCommand.Parameters.AddWithValue("@InvoiceEndPeriod", InvoiceEndPeriod);
                using SQLDataAccess sQLDataAccess = new SQLDataAccess(configuration);
                using SqlDataReader sqlDataReader = sQLDataAccess.RetrieveData(sqlCommand);
                List<ClientInvoiceListDto> list = new List<ClientInvoiceListDto>();
                while (sqlDataReader.Read())
                {
                    list.Add(new ClientInvoiceListDto
                    {
                        Row = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("Row")),
                        InvoiceDate = sqlDataReader.GetString(sqlDataReader.GetOrdinal("invoicedate")),
                        BranchName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("BranchName")),
                        ClientName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("ClientName")),
                        InvoiceNumber = sqlDataReader.GetString(sqlDataReader.GetOrdinal("InvoiceNumber")),
                        ServiceCharges = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("ServiceCharges")),
                        Discount = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("Discount")),
                        TaxAmount = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("TaxAmount")),
                        InvoiceAmount = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("InvoiceAmount")),
                        Payment = sqlDataReader.GetString(sqlDataReader.GetOrdinal("Payment")),
                        WorkPlace = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("WorkPlace")) ? "" : sqlDataReader.GetString(sqlDataReader.GetOrdinal("WorkPlace"))
                    });
                }

                return list;
            }
            catch
            {
                throw;
            }
        }

        public static DateTime GetInvoiceDate(decimal AgreementID)
        {
            try
            {
                using SqlCommand sqlCommand = new SqlCommand();
                sqlCommand.CommandText = " SELECT ISNULL(Max(InvoiceDate),CAST('1753-01-01' AS DATETIME)) AS InvoiceDate FROM CLIENTINVOICE WHERE IsDeleted='N' AND AgreementID=@AgreementID";
                sqlCommand.Parameters.AddWithValue("@AgreementID", AgreementID);
                using SQLDataAccess sQLDataAccess = new SQLDataAccess(configuration);
                using SqlDataReader sqlDataReader = sQLDataAccess.RetrieveData(sqlCommand);
                if (sqlDataReader.Read())
                {
                    return sqlDataReader.GetDateTime(0);
                }

                return default(DateTime);
            }
            catch
            {
                throw;
            }
        }

        public static bool IsInvoiceAvailable(decimal AgreementID)
        {
            try
            {
                using SqlCommand sqlCommand = new SqlCommand();
                sqlCommand.CommandText = " SELECT COUNT(ID) AS NOOFINVOICES FROM CLIENTINVOICE WHERE AgreementID=@AgreementID AND IsDeleted='N'";
                sqlCommand.Parameters.AddWithValue("@AgreementID", AgreementID);
                using SQLDataAccess sQLDataAccess = new SQLDataAccess(configuration);
                using SqlDataReader sqlDataReader = sQLDataAccess.RetrieveData(sqlCommand);
                if (sqlDataReader.Read())
                {
                    return sqlDataReader.GetInt32(0) > 0;
                }

                return false;
            }
            catch
            {
                throw;
            }
        }

        public static decimal GetPreviousSalaryProcessAuditVersion(string Branch, string EmployeeType, DateTime Period)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = " SELECT TOP 1 Version FROM SalaryProcessAudit " +
                                        " WHERE Branch =@Branch " +
                                        " AND EmployeeType =@EmployeeType " +
                                        " AND Period =@Period " +
                                        " Group By Version " +
                                        " Order by Version DESC ";

                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    cmd.Parameters.AddWithValue("@Period", Period);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                return (dr.GetDecimal(0) + 1);
                            }
                            return 1;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static string NewClientInvoiceNo(string BranchCode, DateTime InvoiceDate)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    if (InvoiceDate >= Convert.ToDateTime("2015-04-01 00:00:00.000"))
                    {
                        cmd.CommandText = " SELECT ISNULL(MAX(CAST(INVOICENO AS INT))+1,1) AS NEWCLIENTINVOICENO FROM CLIENTINVOICE WHERE BRANCH=@BranchCode AND InvoiceDate >='2015-04-01 00:00:00.000'";
                    }
                    else
                    {
                        cmd.CommandText = " SELECT ISNULL(MAX(CAST(INVOICENO AS INT))+1,1) AS NEWCLIENTINVOICENO FROM CLIENTINVOICE WHERE BRANCH=@BranchCode AND InvoiceDate < '2015-04-01 00:00:00.000'";
                    }

                    cmd.Parameters.AddWithValue("@BranchCode", BranchCode);
                    cmd.Parameters.AddWithValue("@InvoiceDate", InvoiceDate);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {

                                if (InvoiceDate >= Convert.ToDateTime("2015-04-01 00:00:00.000"))
                                {
                                    if (dr.GetInt32(0) >= 1000)
                                    {
                                        return dr.GetInt32(0).ToString().PadLeft(5, '0');
                                    }
                                    else
                                    {
                                        return dr.GetInt32(0).ToString().PadLeft(4, '0');
                                    }

                                }
                                else
                                {
                                    return dr.GetInt32(0).ToString();
                                }
                            }
                            return string.Empty;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static void GenerateClientStatement(DateTime startDate,DateTime endDate,string branchId,string clientId)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = "dbo.GetClientStatement";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@begindt", startDate);
                    cmd.Parameters.AddWithValue("@enddt", endDate);
                    cmd.Parameters.AddWithValue("@branch", branchId);
                    cmd.Parameters.AddWithValue("@client", clientId);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        sdaFactory.ExecuteSQL(cmd);
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static void ExecuteSupplierReport(DateTime startDate,DateTime endDate,string branch,decimal payTo, decimal client,string status, string category)
        {
            
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    if (payTo == 0)
                    {
                        // Call first SP
                        cmd.CommandText = "dbo.BfSupplierStatement";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@begindt", startDate);
                        cmd.Parameters.AddWithValue("@enddt", endDate);
                        cmd.Parameters.AddWithValue("@branch", branch);
                        cmd.Parameters.AddWithValue("@client", client);
                        cmd.Parameters.AddWithValue("@Status", status);
                    }
                    else
                    {
                        // Call second SP
                        cmd.CommandText = "dbo.BfSupplierStatement2";
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@begindt", startDate);
                        cmd.Parameters.AddWithValue("@enddt", endDate);
                        cmd.Parameters.AddWithValue("@branch", branch);
                        cmd.Parameters.AddWithValue("@client", payTo);
                        cmd.Parameters.AddWithValue("@Category", category);
                        // Uncomment below if Status is needed for second SP
                        // cmd.Parameters.AddWithValue("@Status", status);
                    }

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        sdaFactory.ExecuteSQL(cmd);
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static bool CreditorInvoice_CheckOnBranchPayments(decimal InvoiceID)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {

                    string sSQL = string.Empty;
                    sSQL = "SELECT COUNT(*) AS TotalRecord FROM BranchPaymentDetails ";
                    sSQL = sSQL + "INNER JOIN BranchPayments on BranchPaymentDetails.PaymentID=BranchPayments.ID ";
                    sSQL = sSQL + "WHERE InvoiceID=@InvoiceID ";
                    sSQL = sSQL + "AND BranchPayments.Isdeleted=0 ";
                    sSQL = sSQL + "AND BranchPayments.CreditorType=1 ";

                    cmd.CommandText = sSQL;
                    cmd.Parameters.AddWithValue("@InvoiceID", InvoiceID);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                return (dr.GetInt32(0) == 0);
                            }
                            return true;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }

        }

        public static bool DeleteExpensesByID(int ID,string CurrentUser)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    //cmd.CommandText = " UPDATE CreditorInvoice SET IsDeleted = 1,LastUpdatedBy=@LastUpdatedBy,LASTUPDATE=GetDate() WHERE [ID] = @ID";
                    //kean hong taken out is deleted
                    cmd.CommandText = " UPDATE CreditorInvoice SET IsDeleted = 1,LastUpdatedBy=@LastUpdatedBy,LASTUPDATE=GetDate() WHERE [ID] = @ID";
                    cmd.Parameters.AddWithValue("@LastUpdatedBy", CurrentUser);
                    cmd.Parameters.AddWithValue("@ID", ID);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        sdaFactory.StartTransaction();
                        sdaFactory.ExecuteSQL(cmd);
                        sdaFactory.EndTransaction(true);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}

