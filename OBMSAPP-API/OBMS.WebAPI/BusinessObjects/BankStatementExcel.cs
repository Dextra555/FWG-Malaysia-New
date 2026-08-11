using System.Data.SqlClient;

namespace OBMS.WebAPI.BusinessObjects
{
    public class BankStatementExcel
    {
        private static readonly IConfiguration configuration;
        private string sAccountNo;
        private string sSalary;
        private string sName;
        private string sPassport;
        private string sBranchCode;

        public string AccountNo
        {
            get
            {
                return sAccountNo;
            }
            set
            {
                sAccountNo = value;
            }
        }

        public string Salary
        {
            get
            {
                return sSalary;
            }
            set
            {
                sSalary = value;
            }
        }

        public string Name
        {
            get
            {
                return sName;
            }
            set
            {
                sName = value;
            }
        }

        public string Passport
        {
            get
            {
                return sPassport;
            }
            set
            {
                sPassport = value;
            }
        }

        public string BranchCode
        {
            get
            {
                return sBranchCode;
            }
            set
            {
                sBranchCode = value;
            }
        }

        public BankStatementExcel()
        {

        }
        static BankStatementExcel()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Base configuration file
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Environment-specific file
                .Build();
        }
        public BankStatementExcel(string AccountNo, string Salary, string Name, string Passport, string BranchCode)
        {
            sAccountNo = AccountNo;
            sSalary = Salary;
            sName = Name;
            sPassport = Passport;
            sBranchCode = BranchCode;
        }

        string sEmployeeName = string.Empty;
        string sEMPICNO = string.Empty;
        string sEPFNO = string.Empty;
        string sEPFEmployee = string.Empty;
        string sEPFEmployer = string.Empty;
        string sEMPJoinDate = string.Empty;
        string sSOCSONo = string.Empty;
        string sSOCSOEmployee = string.Empty;
        string sSOCSOEmployer = string.Empty;

        public string EmployeeName
        {
            get
            {
                return sEmployeeName;
            }
            set
            {
                sEmployeeName = value;
            }
        }

        public string EMPICNO
        {
            get
            {
                return sEMPICNO;
            }
            set
            {
                sEMPICNO = value;
            }
        }

        public string EPFNO
        {
            get
            {
                return sEPFNO;
            }
            set
            {
                sEPFNO = value;
            }
        }



        public string EPFEmployee
        {
            get
            {
                return sEPFEmployee;
            }
            set
            {
                sEPFEmployee = value;
            }
        }


        public string EPFEmployer
        {
            get
            {
                return sEPFEmployer;
            }
            set
            {
                sEPFEmployer = value;
            }
        }

        public string EMPJoinDate
        {
            get
            {
                return sEMPJoinDate;
            }
            set
            {
                sEMPJoinDate = value;
            }
        }

        public string SOCSONo
        {
            get
            {
                return sSOCSONo;
            }
            set
            {
                sSOCSONo = value;
            }
        }

        public string SOCSOEmployee
        {
            get
            {
                return sSOCSOEmployee;
            }
            set
            {
                sSOCSOEmployee = value;
            }
        }

        public string SOCSOEmployer
        {
            get
            {
                return sSOCSOEmployer;
            }
            set
            {
                sSOCSOEmployer = value;
            }
        }


        string sCompanyCode = string.Empty;
        string sSSM = string.Empty;
        string sSIPEmployee = string.Empty;
        string sSIPEmployer = string.Empty;
        string sSIPTotal = string.Empty;
        string sPeriod = string.Empty;
        string sEmpStatus = string.Empty;

        public string CompanyCode
        {
            get
            {
                return sCompanyCode;
            }
            set
            {
                sCompanyCode = value;
            }
        }

        public string SSM
        {
            get
            {
                return sSSM;
            }
            set
            {
                sSSM = value;
            }
        }

        public string SIPEmployee
        {
            get
            {
                return sSIPEmployee;
            }
            set
            {
                sSIPEmployee = value;
            }
        }

        public string SIPEmployer
        {
            get
            {
                return sSIPEmployer;
            }
            set
            {
                sSIPEmployer = value;
            }
        }

        public string SIPTotal
        {
            get
            {
                return sSIPTotal;
            }
            set
            {
                sSIPTotal = value;
            }
        }

        public string Period
        {
            get
            {
                return sPeriod;
            }
            set
            {
                sPeriod = value;
            }
        }

        public string EmpStatus
        {
            get
            {
                return sEmpStatus;
            }
            set
            {
                sEmpStatus = value;
            }
        }

        string strPaymentReceived = string.Empty;
        string dInvoiceDate = string.Empty;
        string strInvoiceNo = string.Empty;
        string strName = string.Empty;
        decimal dServiceCharges = 0;
        decimal dDiscount = 0;
        decimal dTaxAmount = 0;
        decimal dInvoiceAmount = 0;

        public string PaymentReceived
        {
            get
            {
                return strPaymentReceived;
            }
            set
            {
                strPaymentReceived = value;
            }
        }

        public string InvoiceDate
        {
            get
            {
                return dInvoiceDate;
            }
            set
            {
                dInvoiceDate = value;
            }
        }

        public string Name1
        {
            get
            {
                return strName;
            }
            set
            {
                strName = value;
            }
        }

        public string InvoiceNo
        {
            get
            {
                return strInvoiceNo;
            }
            set
            {
                strInvoiceNo = value;
            }
        }

        public decimal ServiceCharges
        {
            get
            {
                return dServiceCharges;
            }
            set
            {
                dServiceCharges = value;
            }
        }

        public decimal Discount
        {
            get
            {
                return dDiscount;
            }
            set
            {
                dDiscount = value;
            }
        }

        public decimal TaxAmount
        {
            get
            {
                return dTaxAmount;
            }
            set
            {
                dTaxAmount = value;
            }
        }

        public decimal InvoiceAmount
        {
            get
            {
                return dInvoiceAmount;
            }
            set
            {
                dInvoiceAmount = value;
            }
        }

        //EMPLOYEE NAME	I/C NO	EPF MEMBER NO	SALARY (RM)	EPF EMP'YEE (RM)	EPF EMP'YER (RM)
        public BankStatementExcel(string EmployeeName, string EMPICNO, string EPFNO, string Salary, string EPFEmployee, string EPFEmployer)
        {
            sEmployeeName = EmployeeName;
            sEMPICNO = EMPICNO;
            sEPFNO = EPFNO;
            sSalary = Salary;
            sEPFEmployee = EPFEmployee;
            sEPFEmployer = EPFEmployer;
        }
        string sSOCSONO = string.Empty;
        string sSKBBK = string.Empty;
        public string SKBBK
        {
            get { return sSKBBK; }
            set { sSKBBK = value; }
        }
        public BankStatementExcel(string EmployeeName, string EMPICNO, string EMPJoinDate, string SOCSONO, string Salary, string SOCSOEmployee, string SOCSOEmployer)
        {
            sEmployeeName = EmployeeName;
            sEMPICNO = EMPICNO;
            sEMPJoinDate = EMPJoinDate;
            sSOCSONO = SOCSONO;
            sSalary = Salary;
            sSOCSOEmployee = SOCSOEmployee;
            sSOCSOEmployer = SOCSOEmployer;
        }


        public BankStatementExcel(string CompanyCode, string SSM, string EMPICNO, string EmployeeName, string Period, string SIPTotal, string EMPJoinDate, string EMPStatus)
        {
            sCompanyCode = CompanyCode;
            sSSM = SSM;
            sEMPICNO = EMPICNO;
            sEmployeeName = EmployeeName;
            sPeriod = Period;
            sSIPTotal = SIPTotal;
            sEMPJoinDate = EMPJoinDate;
            sEmpStatus = EMPStatus;
        }


        public BankStatementExcel(string PaymentReceived, string InvoiceDate, string InvoiceNo, string Name, decimal ServiceCharges, decimal Discount, decimal TaxAmount, decimal InvoiceAmount, string PaymentReceived1)
        {
            strPaymentReceived = PaymentReceived;
            dInvoiceDate = InvoiceDate;
            strInvoiceNo = InvoiceNo;
            sName = Name;
            dServiceCharges = ServiceCharges;
            dDiscount = Discount;
            dTaxAmount = TaxAmount;
            dInvoiceAmount = InvoiceAmount;
            strPaymentReceived = PaymentReceived1;
        }
        public static List<BankStatementExcel> GetListWithBlankRow(string dtSalaryPeriod)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = "SELECT Employee.EMP_NAME, Employee.EMP_IC_OLD, Employee.EMP_IC_NEW,Employee.EMP_PASSPORT_NO, EmployeeSalaryDetails.EMPFL_BRANCHCODE, PaySlip.BasicSalary, PaySlip.OverTimeSalary, PaySlip.OffDaySalary, PaySlip.OffDayOverTimeSalary, PaySlip.HolidaySalary, PaySlip.HolidayOverTimeSalary, PaySlip.Shift2Salary, PaySlip.AttendanceAllowance, PaySlip.EPFDeductionAmount, PaySlip.SOCSODeductionAmount, PaySlip.DailyAdvanceRecovery, PaySlip.MonthlyAdvanceRecovery, PaySlip.UniformIssueRecovery, PaySlip.LoanRecovery, PaySlip.MiscDeduction, PaySlip.ReAllowance,PaySlip.MiscAmount,BranchMaster.[Name], EmployeeSalaryDetails.EMPFL_BK_ACCNO " +
                                      " FROM   (EmployeeSalaryDetails EmployeeSalaryDetails INNER JOIN (PaySlip PaySlip INNER JOIN Employee Employee ON PaySlip.EmployeeID=Employee.EMP_ID) ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE) INNER JOIN BranchMaster BranchMaster ON Employee.EMP_BRANCH_CODE=BranchMaster.Code " +
                                      " WHERE  PaySlip.Period =@SalaryPeriod " +
                                      " ORDER BY Employee.EMP_NAME ";

                    cmd.Parameters.AddWithValue("@SalaryPeriod", dtSalaryPeriod);
                    //cmd.Parameters.AddWithValue("@Client", Client);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<BankStatementExcel> sBankStatementExcel = new List<BankStatementExcel>();
                            decimal Salary = 0;
                            decimal GrossPay = 0;
                            decimal BasicSalary = 0;
                            decimal OverTimeSalary = 0;
                            decimal OffDaySalary = 0;
                            decimal OffDayOverTimeSalary = 0;
                            decimal HolidaySalary = 0;
                            decimal HolidayOverTimeSalary = 0;
                            decimal Shift2Salary = 0;
                            decimal AttendanceAllowance = 0;
                            decimal ReAllowance = 0;
                            decimal MiscAmount = 0;
                            decimal TotalDeduction = 0;
                            decimal EPFDeductionAmount = 0;
                            decimal SOCSODeductionAmount = 0;
                            decimal DailyAdvanceRecovery = 0;
                            decimal MonthlyAdvanceRecovery = 0;
                            decimal UniformIssueRecovery = 0;
                            decimal LoanRecovery = 0;
                            decimal MiscDeduction = 0;
                            string SecurityNo = "";

                            while (dr.Read())
                            {
                                BasicSalary = decimal.Parse(dr["BasicSalary"].ToString());
                                OverTimeSalary = decimal.Parse(dr["OverTimeSalary"].ToString());
                                OffDaySalary = decimal.Parse(dr["OffDaySalary"].ToString());
                                OffDayOverTimeSalary = decimal.Parse(dr["OffDayOverTimeSalary"].ToString());
                                HolidaySalary = decimal.Parse(dr["HolidaySalary"].ToString());
                                HolidayOverTimeSalary = decimal.Parse(dr["HolidayOverTimeSalary"].ToString());
                                Shift2Salary = decimal.Parse(dr["Shift2Salary"].ToString());
                                AttendanceAllowance = decimal.Parse(dr["AttendanceAllowance"].ToString());
                                ReAllowance = decimal.Parse(dr["ReAllowance"].ToString());
                                MiscAmount = decimal.Parse(dr["MiscAmount"].ToString());

                                EPFDeductionAmount = decimal.Parse(dr["EPFDeductionAmount"].ToString());
                                SOCSODeductionAmount = decimal.Parse(dr["SOCSODeductionAmount"].ToString());
                                DailyAdvanceRecovery = decimal.Parse(dr["DailyAdvanceRecovery"].ToString());
                                MonthlyAdvanceRecovery = decimal.Parse(dr["MonthlyAdvanceRecovery"].ToString());
                                UniformIssueRecovery = decimal.Parse(dr["UniformIssueRecovery"].ToString());
                                LoanRecovery = decimal.Parse(dr["LoanRecovery"].ToString());
                                MiscDeduction = decimal.Parse(dr["MiscDeduction"].ToString());

                                GrossPay = BasicSalary + OverTimeSalary + OffDaySalary + OffDayOverTimeSalary + HolidaySalary + HolidayOverTimeSalary + Shift2Salary + AttendanceAllowance + ReAllowance + MiscAmount;
                                TotalDeduction = EPFDeductionAmount + SOCSODeductionAmount + DailyAdvanceRecovery + MonthlyAdvanceRecovery + UniformIssueRecovery + LoanRecovery + MiscDeduction;
                                Salary = GrossPay - TotalDeduction;

                                if (!string.IsNullOrEmpty(dr["EMP_IC_NEW"].ToString()))
                                {
                                    SecurityNo = dr["EMP_IC_NEW"].ToString();
                                }
                                else if (!string.IsNullOrEmpty(dr["EMP_IC_OLD"].ToString()))
                                {
                                    SecurityNo = dr["EMP_IC_OLD"].ToString();
                                }
                                else
                                {
                                    SecurityNo = dr["EMP_PASSPORT_NO"].ToString();
                                }

                                sBankStatementExcel.Add(
                                 new BankStatementExcel(
                                 dr["EMPFL_BK_ACCNO"].ToString(),
                                 Salary.ToString(),
                                 dr["EMP_NAME"].ToString(),
                                 SecurityNo,
                                 dr["EMPFL_BRANCHCODE"].ToString())
                             );
                            }
                            return sBankStatementExcel;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static List<BankStatementExcel> GetListWithBlankRow(string dtSalaryPeriod, string Branch)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = "SELECT Employee.EMP_NAME, Employee.EMP_IC_OLD, Employee.EMP_IC_NEW,Employee.EMP_PASSPORT_NO, EmployeeSalaryDetails.EMPFL_BRANCHCODE, PaySlip.BasicSalary, PaySlip.OverTimeSalary, PaySlip.OffDaySalary, PaySlip.OffDayOverTimeSalary, PaySlip.HolidaySalary, PaySlip.HolidayOverTimeSalary, PaySlip.Shift2Salary, PaySlip.AttendanceAllowance, PaySlip.EPFDeductionAmount, PaySlip.SOCSODeductionAmount, PaySlip.DailyAdvanceRecovery, PaySlip.MonthlyAdvanceRecovery, PaySlip.UniformIssueRecovery, PaySlip.LoanRecovery, PaySlip.MiscDeduction, PaySlip.ReAllowance,PaySlip.MiscAmount,BranchMaster.[Name], EmployeeSalaryDetails.EMPFL_BK_ACCNO " +
                                      " FROM   (EmployeeSalaryDetails EmployeeSalaryDetails INNER JOIN (PaySlip PaySlip INNER JOIN Employee Employee ON PaySlip.EmployeeID=Employee.EMP_ID) ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE) INNER JOIN BranchMaster BranchMaster ON Employee.EMP_BRANCH_CODE=BranchMaster.Code " +
                                      " WHERE  PaySlip.Period =@SalaryPeriod AND EmployeeSalaryDetails.EMPFL_BRANCHCODE = @Branch " +
                                      " ORDER BY Employee.EMP_NAME ";

                    cmd.Parameters.AddWithValue("@SalaryPeriod", dtSalaryPeriod);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<BankStatementExcel> sBankStatementExcel = new List<BankStatementExcel>();
                            decimal Salary = 0;
                            decimal GrossPay = 0;
                            decimal BasicSalary = 0;
                            decimal OverTimeSalary = 0;
                            decimal OffDaySalary = 0;
                            decimal OffDayOverTimeSalary = 0;
                            decimal HolidaySalary = 0;
                            decimal HolidayOverTimeSalary = 0;
                            decimal Shift2Salary = 0;
                            decimal AttendanceAllowance = 0;
                            decimal ReAllowance = 0;
                            decimal MiscAmount = 0;
                            decimal TotalDeduction = 0;
                            decimal EPFDeductionAmount = 0;
                            decimal SOCSODeductionAmount = 0;
                            decimal DailyAdvanceRecovery = 0;
                            decimal MonthlyAdvanceRecovery = 0;
                            decimal UniformIssueRecovery = 0;
                            decimal LoanRecovery = 0;
                            decimal MiscDeduction = 0;
                            string SecurityNo = "";

                            while (dr.Read())
                            {
                                BasicSalary = decimal.Parse(dr["BasicSalary"].ToString());
                                OverTimeSalary = decimal.Parse(dr["OverTimeSalary"].ToString());
                                OffDaySalary = decimal.Parse(dr["OffDaySalary"].ToString());
                                OffDayOverTimeSalary = decimal.Parse(dr["OffDayOverTimeSalary"].ToString());
                                HolidaySalary = decimal.Parse(dr["HolidaySalary"].ToString());
                                HolidayOverTimeSalary = decimal.Parse(dr["HolidayOverTimeSalary"].ToString());
                                Shift2Salary = decimal.Parse(dr["Shift2Salary"].ToString());
                                AttendanceAllowance = decimal.Parse(dr["AttendanceAllowance"].ToString());
                                ReAllowance = decimal.Parse(dr["ReAllowance"].ToString());
                                MiscAmount = decimal.Parse(dr["MiscAmount"].ToString());

                                EPFDeductionAmount = decimal.Parse(dr["EPFDeductionAmount"].ToString());
                                SOCSODeductionAmount = decimal.Parse(dr["SOCSODeductionAmount"].ToString());
                                DailyAdvanceRecovery = decimal.Parse(dr["DailyAdvanceRecovery"].ToString());
                                MonthlyAdvanceRecovery = decimal.Parse(dr["MonthlyAdvanceRecovery"].ToString());
                                UniformIssueRecovery = decimal.Parse(dr["UniformIssueRecovery"].ToString());
                                LoanRecovery = decimal.Parse(dr["LoanRecovery"].ToString());
                                MiscDeduction = decimal.Parse(dr["MiscDeduction"].ToString());

                                GrossPay = BasicSalary + OverTimeSalary + OffDaySalary + OffDayOverTimeSalary + HolidaySalary + HolidayOverTimeSalary + Shift2Salary + AttendanceAllowance + ReAllowance + MiscAmount;
                                TotalDeduction = EPFDeductionAmount + SOCSODeductionAmount + DailyAdvanceRecovery + MonthlyAdvanceRecovery + UniformIssueRecovery + LoanRecovery + MiscDeduction;
                                Salary = GrossPay - TotalDeduction;

                                if (!string.IsNullOrEmpty(dr["EMP_IC_NEW"].ToString()))
                                {
                                    SecurityNo = dr["EMP_IC_NEW"].ToString();
                                }
                                else if (!string.IsNullOrEmpty(dr["EMP_IC_OLD"].ToString()))
                                {
                                    SecurityNo = dr["EMP_IC_OLD"].ToString();
                                }
                                else
                                {
                                    SecurityNo = dr["EMP_PASSPORT_NO"].ToString();
                                }

                                sBankStatementExcel.Add(
                                 new BankStatementExcel(
                                 dr["EMPFL_BK_ACCNO"].ToString(),
                                 Salary.ToString(),
                                 dr["EMP_NAME"].ToString(),
                                 SecurityNo,
                                 dr["EMPFL_BRANCHCODE"].ToString())
                             );
                            }
                            return sBankStatementExcel;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static List<BankStatementExcel> GetListWithBlankRow(string dtSalaryPeriod, string Branch, string EmployeeType)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = "SELECT Employee.EMP_NAME, Employee.EMP_IC_OLD, Employee.EMP_IC_NEW,Employee.EMP_PASSPORT_NO, EmployeeSalaryDetails.EMPFL_BRANCHCODE, PaySlip.BasicSalary, PaySlip.OverTimeSalary, PaySlip.OffDaySalary, PaySlip.OffDayOverTimeSalary, PaySlip.HolidaySalary, PaySlip.HolidayOverTimeSalary, PaySlip.Shift2Salary, PaySlip.AttendanceAllowance, PaySlip.EPFDeductionAmount, PaySlip.SOCSODeductionAmount, PaySlip.DailyAdvanceRecovery, PaySlip.MonthlyAdvanceRecovery, PaySlip.UniformIssueRecovery, PaySlip.LoanRecovery, PaySlip.MiscDeduction, PaySlip.ReAllowance,PaySlip.MiscAmount,BranchMaster.[Name], EmployeeSalaryDetails.EMPFL_BK_ACCNO " +
                                      " FROM   (EmployeeSalaryDetails EmployeeSalaryDetails INNER JOIN (PaySlip PaySlip INNER JOIN Employee Employee ON PaySlip.EmployeeID=Employee.EMP_ID) ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE) INNER JOIN BranchMaster BranchMaster ON Employee.EMP_BRANCH_CODE=BranchMaster.Code " +
                                      " WHERE  Employee.EMP_ROLE=@EmployeeType and PaySlip.Period =@SalaryPeriod AND EmployeeSalaryDetails.EMPFL_BRANCHCODE = @Branch " +
                                      " ORDER BY Employee.EMP_NAME ";

                    cmd.Parameters.AddWithValue("@SalaryPeriod", dtSalaryPeriod);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<BankStatementExcel> sBankStatementExcel = new List<BankStatementExcel>();
                            decimal Salary = 0;
                            decimal GrossPay = 0;
                            decimal BasicSalary = 0;
                            decimal OverTimeSalary = 0;
                            decimal OffDaySalary = 0;
                            decimal OffDayOverTimeSalary = 0;
                            decimal HolidaySalary = 0;
                            decimal HolidayOverTimeSalary = 0;
                            decimal Shift2Salary = 0;
                            decimal AttendanceAllowance = 0;
                            decimal ReAllowance = 0;
                            decimal MiscAmount = 0;
                            decimal TotalDeduction = 0;
                            decimal EPFDeductionAmount = 0;
                            decimal SOCSODeductionAmount = 0;
                            decimal DailyAdvanceRecovery = 0;
                            decimal MonthlyAdvanceRecovery = 0;
                            decimal UniformIssueRecovery = 0;
                            decimal LoanRecovery = 0;
                            decimal MiscDeduction = 0;
                            string SecurityNo = "";

                            while (dr.Read())
                            {
                                BasicSalary = decimal.Parse(dr["BasicSalary"].ToString());
                                OverTimeSalary = decimal.Parse(dr["OverTimeSalary"].ToString());
                                OffDaySalary = decimal.Parse(dr["OffDaySalary"].ToString());
                                OffDayOverTimeSalary = decimal.Parse(dr["OffDayOverTimeSalary"].ToString());
                                HolidaySalary = decimal.Parse(dr["HolidaySalary"].ToString());
                                HolidayOverTimeSalary = decimal.Parse(dr["HolidayOverTimeSalary"].ToString());
                                Shift2Salary = decimal.Parse(dr["Shift2Salary"].ToString());
                                AttendanceAllowance = decimal.Parse(dr["AttendanceAllowance"].ToString());
                                ReAllowance = decimal.Parse(dr["ReAllowance"].ToString());
                                MiscAmount = decimal.Parse(dr["MiscAmount"].ToString());

                                EPFDeductionAmount = decimal.Parse(dr["EPFDeductionAmount"].ToString());
                                SOCSODeductionAmount = decimal.Parse(dr["SOCSODeductionAmount"].ToString());
                                DailyAdvanceRecovery = decimal.Parse(dr["DailyAdvanceRecovery"].ToString());
                                MonthlyAdvanceRecovery = decimal.Parse(dr["MonthlyAdvanceRecovery"].ToString());
                                UniformIssueRecovery = decimal.Parse(dr["UniformIssueRecovery"].ToString());
                                LoanRecovery = decimal.Parse(dr["LoanRecovery"].ToString());
                                MiscDeduction = decimal.Parse(dr["MiscDeduction"].ToString());

                                GrossPay = BasicSalary + OverTimeSalary + OffDaySalary + OffDayOverTimeSalary + HolidaySalary + HolidayOverTimeSalary + Shift2Salary + AttendanceAllowance + ReAllowance + MiscAmount;
                                TotalDeduction = EPFDeductionAmount + SOCSODeductionAmount + DailyAdvanceRecovery + MonthlyAdvanceRecovery + UniformIssueRecovery + LoanRecovery + MiscDeduction;
                                Salary = GrossPay - TotalDeduction;

                                if (!string.IsNullOrEmpty(dr["EMP_IC_NEW"].ToString()))
                                {
                                    SecurityNo = dr["EMP_IC_NEW"].ToString();
                                }
                                else if (!string.IsNullOrEmpty(dr["EMP_IC_OLD"].ToString()))
                                {
                                    SecurityNo = dr["EMP_IC_OLD"].ToString();
                                }
                                else
                                {
                                    SecurityNo = dr["EMP_PASSPORT_NO"].ToString();
                                }

                                sBankStatementExcel.Add(
                                 new BankStatementExcel(
                                 dr["EMPFL_BK_ACCNO"].ToString(),
                                 Salary.ToString(),
                                 dr["EMP_NAME"].ToString(),
                                 SecurityNo,
                                 dr["EMPFL_BRANCHCODE"].ToString())
                             );
                            }
                            return sBankStatementExcel;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static List<BankStatementExcel> GetListWithBlankRow(string dtSalaryPeriod, string Branch, string EmployeeType, string Bank)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    if (EmployeeType.Contains("TEMPORARY"))
                    {
                        if (EmployeeType == "TEMPORARYGUARD")
                        {
                            EmployeeType = "GUARD";
                        }
                        else if (EmployeeType == "TEMPORARYSTAFF")
                        {
                            EmployeeType = "STAFF";
                        }

                        cmd.CommandText = "SELECT Employee.EMP_NAME, Employee.EMP_IC_OLD, Employee.EMP_IC_NEW,Employee.EMP_PASSPORT_NO, EmployeeSalaryDetails.EMPFL_BRANCHCODE, PaySlip.BasicSalary, PaySlip.OverTimeSalary, PaySlip.OffDaySalary, PaySlip.OffDayOverTimeSalary, PaySlip.HolidaySalary, PaySlip.HolidayOverTimeSalary, PaySlip.Shift2Salary, PaySlip.AttendanceAllowance, PaySlip.EPFDeductionAmount, PaySlip.SOCSODeductionAmount, PaySlip.DailyAdvanceRecovery, PaySlip.MonthlyAdvanceRecovery, PaySlip.UniformIssueRecovery, PaySlip.LoanRecovery, PaySlip.MiscDeduction, PaySlip.ReAllowance,PaySlip.MiscAmount,BranchMaster.[Name], EmployeeSalaryDetails.EMPFL_BK_ACCNO " +
                                      " FROM   (EmployeeSalaryDetails EmployeeSalaryDetails INNER JOIN (PaySlip PaySlip INNER JOIN Employee Employee ON PaySlip.EmployeeID=Employee.EMP_ID) ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE) INNER JOIN BranchMaster BranchMaster ON Employee.EMP_BRANCH_CODE=BranchMaster.Code " +
                                      " WHERE  Employee.EMP_ROLE =@EmployeeType and PaySlip.Period =@SalaryPeriod AND EmployeeSalaryDetails.EMPFL_BANK =@Bank And EmployeeSalaryDetails.EMPFL_BRANCHCODE = @Branch And EmployeeSalaryDetails.TMPGUARD=0 " +
                                      " ORDER BY Employee.EMP_NAME ";
                    }
                    else
                    {
                        cmd.CommandText = "SELECT Employee.EMP_NAME, Employee.EMP_IC_OLD, Employee.EMP_IC_NEW,Employee.EMP_PASSPORT_NO, EmployeeSalaryDetails.EMPFL_BRANCHCODE, PaySlip.BasicSalary, PaySlip.OverTimeSalary, PaySlip.OffDaySalary, PaySlip.OffDayOverTimeSalary, PaySlip.HolidaySalary, PaySlip.HolidayOverTimeSalary, PaySlip.Shift2Salary, PaySlip.AttendanceAllowance, PaySlip.EPFDeductionAmount, PaySlip.SOCSODeductionAmount, PaySlip.DailyAdvanceRecovery, PaySlip.MonthlyAdvanceRecovery, PaySlip.UniformIssueRecovery, PaySlip.LoanRecovery, PaySlip.MiscDeduction, PaySlip.ReAllowance,PaySlip.MiscAmount,BranchMaster.[Name], EmployeeSalaryDetails.EMPFL_BK_ACCNO " +
                                          " FROM   (EmployeeSalaryDetails EmployeeSalaryDetails INNER JOIN (PaySlip PaySlip INNER JOIN Employee Employee ON PaySlip.EmployeeID=Employee.EMP_ID) ON EmployeeSalaryDetails.EMPFL_CODE=Employee.EMP_CODE) INNER JOIN BranchMaster BranchMaster ON Employee.EMP_BRANCH_CODE=BranchMaster.Code " +
                                          " WHERE  Employee.EMP_ROLE =@EmployeeType and PaySlip.Period =@SalaryPeriod AND EmployeeSalaryDetails.EMPFL_BANK =@Bank And EmployeeSalaryDetails.EMPFL_BRANCHCODE = @Branch " +
                                          " ORDER BY Employee.EMP_NAME ";
                    }

                    cmd.Parameters.AddWithValue("@SalaryPeriod", dtSalaryPeriod);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    cmd.Parameters.AddWithValue("@EmployeeType", EmployeeType);
                    cmd.Parameters.AddWithValue("@Bank", Bank);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<BankStatementExcel> sBankStatementExcel = new List<BankStatementExcel>();
                            decimal Salary = 0;
                            decimal GrossPay = 0;
                            decimal BasicSalary = 0;
                            decimal OverTimeSalary = 0;
                            decimal OffDaySalary = 0;
                            decimal OffDayOverTimeSalary = 0;
                            decimal HolidaySalary = 0;
                            decimal HolidayOverTimeSalary = 0;
                            decimal Shift2Salary = 0;
                            decimal AttendanceAllowance = 0;
                            decimal ReAllowance = 0;
                            decimal MiscAmount = 0;
                            decimal TotalDeduction = 0;
                            decimal EPFDeductionAmount = 0;
                            decimal SOCSODeductionAmount = 0;
                            decimal DailyAdvanceRecovery = 0;
                            decimal MonthlyAdvanceRecovery = 0;
                            decimal UniformIssueRecovery = 0;
                            decimal LoanRecovery = 0;
                            decimal MiscDeduction = 0;
                            string SecurityNo = "";
                            while (dr.Read())
                            {
                                BasicSalary = decimal.Parse(dr["BasicSalary"].ToString());
                                OverTimeSalary = decimal.Parse(dr["OverTimeSalary"].ToString());
                                OffDaySalary = decimal.Parse(dr["OffDaySalary"].ToString());
                                OffDayOverTimeSalary = decimal.Parse(dr["OffDayOverTimeSalary"].ToString());
                                HolidaySalary = decimal.Parse(dr["HolidaySalary"].ToString());
                                HolidayOverTimeSalary = decimal.Parse(dr["HolidayOverTimeSalary"].ToString());
                                Shift2Salary = decimal.Parse(dr["Shift2Salary"].ToString());
                                AttendanceAllowance = decimal.Parse(dr["AttendanceAllowance"].ToString());
                                ReAllowance = decimal.Parse(dr["ReAllowance"].ToString());
                                MiscAmount = decimal.Parse(dr["MiscAmount"].ToString());

                                EPFDeductionAmount = decimal.Parse(dr["EPFDeductionAmount"].ToString());
                                SOCSODeductionAmount = decimal.Parse(dr["SOCSODeductionAmount"].ToString());
                                DailyAdvanceRecovery = decimal.Parse(dr["DailyAdvanceRecovery"].ToString());
                                MonthlyAdvanceRecovery = decimal.Parse(dr["MonthlyAdvanceRecovery"].ToString());
                                UniformIssueRecovery = decimal.Parse(dr["UniformIssueRecovery"].ToString());
                                LoanRecovery = decimal.Parse(dr["LoanRecovery"].ToString());
                                MiscDeduction = decimal.Parse(dr["MiscDeduction"].ToString());

                                GrossPay = BasicSalary + OverTimeSalary + OffDaySalary + OffDayOverTimeSalary + HolidaySalary + HolidayOverTimeSalary + Shift2Salary + AttendanceAllowance + ReAllowance + MiscAmount;
                                TotalDeduction = EPFDeductionAmount + SOCSODeductionAmount + DailyAdvanceRecovery + MonthlyAdvanceRecovery + UniformIssueRecovery + LoanRecovery + MiscDeduction;
                                Salary = GrossPay - TotalDeduction;

                                if (!string.IsNullOrEmpty(dr["EMP_IC_NEW"].ToString()))
                                {
                                    SecurityNo = dr["EMP_IC_NEW"].ToString();
                                }
                                else if (!string.IsNullOrEmpty(dr["EMP_IC_OLD"].ToString()))
                                {
                                    SecurityNo = dr["EMP_IC_OLD"].ToString();
                                }
                                else
                                {
                                    SecurityNo = dr["EMP_PASSPORT_NO"].ToString();
                                }

                                sBankStatementExcel.Add(
                                 new BankStatementExcel(
                                 dr["EMPFL_BK_ACCNO"].ToString(),
                                 Salary.ToString(),
                                 dr["EMP_NAME"].ToString(),
                                 SecurityNo,
                                 dr["EMPFL_BRANCHCODE"].ToString())
                             );
                            }
                            return sBankStatementExcel;
                        }
                    }

                }
            }
            catch
            {
                throw;
            }
        }

        public static List<BankStatementExcel> GetEPFToExcel(string Branch, DateTime Period, string EmployeeType)
        {
            try
            {
                List<BankStatementExcel> sEPFExcelList = new List<BankStatementExcel>();

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
                string strEmpName = string.Empty;
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
                    strSQL = strSQL + " LEFT OUTER JOIN EmployeeSalaryDetails C ON B.EMP_ID = C.EMPFL_ID ";
                    strSQL = strSQL + " WHERE YEAR(PERIOD)=@Year AND MONTH(Period)=@Month ";
                    strSQL = strSQL + " AND (EPFEmployerContribution > 0 OR EPFDeductionAmount > 0) ";
                    if (Branch != "")
                    {
                        strSQL = strSQL + " AND EMP_BRANCH_CODE = @Branch";
                    }

                    //if (EmployeeType != "")
                    //{
                    //    strSQL = strSQL + " AND EMP_ROLE = @EmployeeType";
                    //}
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

                                strEPFNo = dr.GetString(0).ToString();
                                strEmpSecurityNo = dr.GetString(1).ToString();
                                strEmpName1 = dr.GetString(2).ToString();
                                strEmpName2 = dr.GetString(3).ToString();
                                strEmpName = strEmpName1 + strEmpName2;
                                strEmpCode = dr.GetString(4).ToString();
                                dEPFER = dr.GetDecimal(5);
                                dEPFEE = dr.GetDecimal(6);
                                dBasicSalary = dr.GetDecimal(7);

                                sEPFExcelList.Add(
                                 new BankStatementExcel(
                                     strEmpName.ToString(),
                                     strEmpSecurityNo.ToString(),
                                     strEPFNo.ToString(),
                                     dBasicSalary.ToString(),
                                     dEPFEE.ToString(),
                                     dEPFER.ToString())
                                );
                            }
                            return sEPFExcelList;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static List<BankStatementExcel> GetEmployeeSocsoList(DateTime Period, string Branch)
        {
            try
            {
                List<BankStatementExcel> SocsoList = new List<BankStatementExcel>();
                string strSQL = string.Empty;
                string strEmpNewIC = string.Empty;
                string strEmpOldIC = string.Empty;
                string strEmpPassportNo = string.Empty;
                string strEmpSoscoNo = string.Empty;
                string strEmpSecurityCode = string.Empty;
                string strEmpName = string.Empty;
                decimal dBasicSalary = 0;
                decimal dEmpSoscoEE = 0;
                decimal dEmpSoscoER = 0;
                decimal dEmpSosco = 0;
                string strValue = string.Empty;
                DateTime dEMPJoinDate;

                using (SqlCommand cmd = new SqlCommand())
                {
                    strSQL = "SELECT EMP_IC_NEW,EMP_IC_OLD,EMP_PASSPORT_NO,ISNULL('',EMPFL_SOSCO_NO) AS EMPSOSCONO, ";
                    strSQL = strSQL + "EMP_NAME, SOCSODeductionAmount , SOCSOEmployerContribution, ";
                    strSQL = strSQL + "(SOCSODeductionAmount + SOCSOEmployerContribution) AS TotalSOCSO, ";
                    strSQL = strSQL + "EMPPAY_DATE_JOINED AS EMPJoinDate, ";
                    strSQL = strSQL + " isnull(BasicSalary,0) AS BasicSalary, ";
                    strSQL = strSQL + " isnull(SKBBKContribution,0) AS SKBBKContribution ";
                    strSQL = strSQL + "FROM  Employee  ";
                    strSQL = strSQL + "INNER JOIN EmployeeSalaryDetails On EMP_CODE = EMPFL_CODE ";
                    strSQL = strSQL + "INNER JOIN Payslip On EmployeeID = Emp_ID  ";
                    strSQL = strSQL + "LEFT OUTER JOIN EmploymentDetails ON EMPPAY_CODE = EMPFL_CODE ";
                    strSQL = strSQL + "Where Period =@Period ";
                    strSQL = strSQL + "AND SocsoDetect = 1 ";
                    strSQL = strSQL + "AND (SOCSODeductionAmount>0 OR SOCSOEmployerContribution>0) ";
                    if (Branch != "")
                    {
                        strSQL = strSQL + " AND EMP_BRANCH_CODE = @Branch";
                    }

                    strSQL = strSQL + " ORDER BY Employee.EMP_NAME ";

                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@Period", Period);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                strEmpNewIC = string.Empty;
                                strEmpOldIC = string.Empty;
                                strEmpPassportNo = string.Empty;
                                strEmpSoscoNo = string.Empty;
                                strEmpSecurityCode = string.Empty;
                                strEmpName = string.Empty;
                                dEmpSoscoEE = 0;
                                dEmpSoscoER = 0;
                                dEmpSosco = 0;
                                strValue = string.Empty;

                                if (dr["EMP_IC_NEW"] != DBNull.Value)
                                    strEmpNewIC = dr.GetString(0).Replace("-", string.Empty);

                                if (dr["EMP_IC_OLD"] != DBNull.Value)
                                    strEmpOldIC = dr.GetString(1);

                                if (dr["EMP_PASSPORT_NO"] != DBNull.Value)
                                    strEmpPassportNo = dr.GetString(2);

                                if (dr["EMPSOSCONO"] != DBNull.Value)
                                    strEmpSoscoNo = dr.GetString(3);

                                if (strEmpNewIC == string.Empty)
                                {
                                    if (strEmpOldIC == string.Empty)
                                        strEmpSecurityCode = strEmpPassportNo;
                                    else
                                        strEmpSecurityCode = strEmpOldIC;
                                }
                                else
                                {
                                    strEmpSecurityCode = strEmpNewIC;
                                }
                                strEmpName = dr.GetString(4);
                                dEmpSoscoEE = dr.GetDecimal(5);
                                dEmpSoscoER = dr.GetDecimal(6);
                                dEmpSosco = dr.GetDecimal(7);
                                dEMPJoinDate = dr.GetDateTime(8);
                                dBasicSalary = dr.GetDecimal(9);
                                decimal dSKBBK = dr["SKBBKContribution"] != DBNull.Value ? dr.GetDecimal(dr.GetOrdinal("SKBBKContribution")) : 0;
                                SocsoList.Add(
                                new BankStatementExcel(
                                    strEmpName.ToString(),
                                    strEmpSecurityCode.ToString(),
                                    dEMPJoinDate.ToString(),
                                    strEmpSoscoNo.ToString(),
                                    dBasicSalary.ToString(),
                                    dEmpSoscoEE.ToString(),
                                    dEmpSoscoER.ToString())
                                { SKBBK = dSKBBK.ToString() }
                               );
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

        public static List<BankStatementExcel> GetEmployeeSIPList(string CompanyCode, string SSM, DateTime Period, string Branch)
        {
            try
            {
                List<BankStatementExcel> SIPList = new List<BankStatementExcel>();
                string strSQL = string.Empty;
                string strEmpNewIC = string.Empty;
                string strEmpOldIC = string.Empty;
                string strEmpPassportNo = string.Empty;
                string strEmpSoscoNo = string.Empty;
                string strEmpSecurityCode = string.Empty;
                string strEmpName = string.Empty;
                decimal dEmpSIPEE = 0;
                decimal dEmpSIPER = 0;
                decimal dEmpSIP = 0;
                string strValue = string.Empty;
                string dEMPJoinDate;

                using (SqlCommand cmd = new SqlCommand())
                {
                    strSQL = "SELECT EMP_IC_NEW,EMP_IC_OLD,EMP_PASSPORT_NO,EMP_NAME, SIPEmployeeContribution , SIPEmployerContribution, ";
                    strSQL = strSQL + "(SIPEmployeeContribution + SIPEmployerContribution) AS TotalSIP, ";
                    strSQL = strSQL + "EMPPAY_DATE_JOINED AS EMPJoinDate, EMPPAY_DATE_RESIGNED ";
                    strSQL = strSQL + "FROM  Employee ";
                    strSQL = strSQL + "INNER JOIN Payslip On EmployeeID = Emp_ID ";
                    strSQL = strSQL + "LEFT OUTER JOIN EmploymentDetails ON EMPPAY_CODE = Employee.EMP_CODE ";
                    strSQL = strSQL + "Where Period =@Period ";
                    strSQL = strSQL + "AND (SIPEmployeeContribution>0 OR SIPEmployerContribution>0) ";
                    strSQL = strSQL + "AND (SOCSODeductionAmount>0 OR SOCSOEmployerContribution>0) ";
                    if (Branch != "")
                    {
                        strSQL = strSQL + " AND EMP_BRANCH_CODE = @Branch";
                    }
                    strSQL = strSQL + " ORDER BY Employee.EMP_NAME ";

                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@Period", Period);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                strEmpNewIC = string.Empty;
                                strEmpOldIC = string.Empty;
                                strEmpPassportNo = string.Empty;
                                strEmpSecurityCode = string.Empty;
                                strEmpName = string.Empty;
                                dEmpSIPEE = 0;
                                dEmpSIPER = 0;
                                dEmpSIP = 0;
                                strValue = string.Empty;

                                if (dr["EMP_IC_NEW"] != DBNull.Value)
                                    strEmpNewIC = dr.GetString(0).Replace("-", string.Empty);

                                if (dr["EMP_IC_OLD"] != DBNull.Value)
                                    strEmpOldIC = dr.GetString(1);

                                if (dr["EMP_PASSPORT_NO"] != DBNull.Value)
                                    strEmpPassportNo = dr.GetString(2);

                                if (strEmpNewIC == string.Empty)
                                {
                                    if (strEmpOldIC == string.Empty)
                                        strEmpSecurityCode = strEmpPassportNo;
                                    else
                                        strEmpSecurityCode = strEmpOldIC;
                                }
                                else
                                {
                                    strEmpSecurityCode = strEmpNewIC;
                                }
                                strEmpName = dr.GetString(3);
                                dEmpSIPEE = dr.GetDecimal(4);
                                dEmpSIPER = dr.GetDecimal(5);
                                dEmpSIP = dEmpSIPEE + dEmpSIPER;
                                dEMPJoinDate = dr.GetDateTime(7).ToString();

                                SIPList.Add(
                                 new BankStatementExcel(
                                     CompanyCode,
                                     SSM,
                                     strEmpSecurityCode.ToString(),
                                     strEmpName.ToString(),
                                     Period.ToString("MMyyyy"),
                                     dEmpSIP.ToString(),
                                     dEMPJoinDate.ToString(),
                                     "")
                                );
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

        public static List<BankStatementExcel> GetMonthlyInvoiceStatusList(string Start, string End, string Branch)
        {
            try
            {
                List<BankStatementExcel> MonthlyInvoiceStatusList = new List<BankStatementExcel>();
                string strSQL = string.Empty;
                string strPaymentReceived = string.Empty;
                string dInvoiceDate;
                string strInvoiceNo = String.Empty;
                string strName = string.Empty;
                decimal dServiceCharges = 0;
                decimal dDiscount = 0;
                decimal dTaxAmount = 0;
                decimal dInvoiceAmount = 0;
                DateTime dStart = DateTime.Parse(Start);
                DateTime dEnd = DateTime.Parse(End);
                using (SqlCommand cmd = new SqlCommand())
                {

                    strSQL = "SELECT ";
                    strSQL = strSQL + "CASE WHEN Payment + CreditNoteAmount >= InvoiceAmount THEN 'YES' WHEN Payment <> 0 THEN 'PARTIAL' ELSE  'NO' END AS PaymentReceived, ";
                    strSQL = strSQL + "A.InvoiceDate, A.Branch + ' ' + A.Invoiceno as InvoiceNo, B.Name, A.ServiceCharges,A.Discount, A.TaxAmount, A.InvoiceAmount ";
                    strSQL = strSQL + "FROM dbo.InvoiceDetails A inner join ClientMaster B on A.Branch = B.Branch and A.Client = B.Code ";
                    strSQL = strSQL + "WHERE A.InvoiceDate >= @Start and A.InvoiceDate <= @End and IsDeleted = 'N' ";
                    //strSQL = strSQL + "WHERE A.InvoiceDate BETWEEN '01/01/2018' and '10/31/2019' and IsDeleted = 'N' ";

                    if (Branch != "")
                    {
                        strSQL = strSQL + " AND A.BRANCH = @Branch";
                    }

                    cmd.CommandText = strSQL;
                    cmd.Parameters.AddWithValue("@Start", dStart);
                    cmd.Parameters.AddWithValue("@End", dEnd);
                    cmd.Parameters.AddWithValue("@Branch", Branch);
                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            while (dr.Read())
                            {
                                strPaymentReceived = string.Empty;
                                dInvoiceDate = string.Empty;
                                strInvoiceNo = String.Empty;
                                strName = string.Empty;
                                dServiceCharges = 0;
                                dDiscount = 0;
                                dTaxAmount = 0;
                                dInvoiceAmount = 0;

                                if (dr["PaymentReceived"] != DBNull.Value)
                                    strPaymentReceived = dr.GetString(0);

                                if (dr["InvoiceDate"] != DBNull.Value)
                                    dInvoiceDate = dr.GetDateTime(1).ToString();

                                if (dr["InvoiceNo"] != DBNull.Value)
                                    strInvoiceNo = dr.GetString(2);

                                if (dr["Name"] != DBNull.Value)
                                    strName = dr.GetString(3);

                                if (dr["ServiceCharges"] != DBNull.Value)
                                    dServiceCharges = dr.GetDecimal(4);

                                if (dr["Discount"] != DBNull.Value)
                                    dDiscount = dr.GetDecimal(5);

                                if (dr["TaxAmount"] != DBNull.Value)
                                    dTaxAmount = dr.GetDecimal(6);

                                if (dr["InvoiceAmount"] != DBNull.Value)
                                    dInvoiceAmount = dr.GetDecimal(7);


                                MonthlyInvoiceStatusList.Add(
                                 new BankStatementExcel(
                                     strPaymentReceived,
                                     dInvoiceDate,
                                     strInvoiceNo,
                                     strName,
                                     dServiceCharges,
                                     dDiscount,
                                     dTaxAmount,
                                     dInvoiceAmount,
                                     strPaymentReceived
                                     )
                                );
                            }
                            return MonthlyInvoiceStatusList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
