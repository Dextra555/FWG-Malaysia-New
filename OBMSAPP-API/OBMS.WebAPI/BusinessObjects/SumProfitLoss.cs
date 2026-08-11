using System.Data.SqlClient;

namespace OBMS.WebAPI.BusinessObjects
{
    public class SumProfitLoss
    {
        private static readonly IConfiguration configuration;
        private string sMonth;
        private decimal dTotalIncome;
        private decimal dTotalCN;
        private decimal dTotalDiscount;
        private decimal dTotalExpenses;
        private decimal dTotalProfit;

        public string Month
        {
            get
            {
                return sMonth;
            }
            set
            {
                sMonth = value;
            }
        }
        public decimal TotalIncomeAmount
        {
            get
            {
                return dTotalIncome;
            }
            set
            {
                dTotalIncome = value;
            }
        }
        public decimal TotalCNAmount
        {
            get
            {
                return dTotalCN;
            }
            set
            {
                dTotalCN = value;
            }
        }
        public decimal TotalDiscountAmount
        {
            get
            {
                return dTotalDiscount;
            }
            set
            {
                dTotalDiscount = value;
            }
        }
        public decimal TotalExpensesAmount
        {
            get
            {
                return dTotalExpenses;
            }
            set
            {
                dTotalExpenses = value;
            }
        }
        public decimal TotalProfitAmount
        {
            get
            {
                return dTotalProfit;
            }
            set
            {
                dTotalProfit = value;
            }
        }
        static SumProfitLoss()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Base configuration file
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Environment-specific file
                .Build();
        }
        public SumProfitLoss(string Month, decimal TotalIncomeAmount, decimal TotalCNAmount, decimal TotalDiscountAmount, decimal TotalExpensesAmount, decimal TotalProfitAmount)
        {
            sMonth = Month;
            dTotalIncome = TotalIncomeAmount;
            dTotalCN = TotalCNAmount;
            dTotalDiscount = TotalDiscountAmount;
            dTotalExpenses = TotalExpensesAmount;
            dTotalProfit = TotalProfitAmount;
        }

        public static List<SumProfitLoss> GetList(DateTime dtStartPeriod, DateTime dtEndPeriod)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sSQL = string.Empty;
                    sSQL = "SELECT ";
                    sSQL = sSQL + "(CASE ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 1 THEN 'JAN' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 2 THEN 'FEB' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 3 THEN 'MAR' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 4 THEN 'APR' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 5 THEN 'MAY' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 6 THEN 'JUN' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 7 THEN 'JULY' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 8 THEN 'AUG' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 9 THEN 'SEPT' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 10 THEN 'OCT' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 11 THEN 'NOV' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 12 THEN 'DEC' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 13 THEN 'Total' ";
                    sSQL = sSQL + "END) AS Month, ";
                    sSQL = sSQL + "Sum(Income) AS Income, ";
                    sSQL = sSQL + "Sum(CN) AS CN, ";
                    sSQL = sSQL + "Sum(Discount) AS Discount, ";
                    sSQL = sSQL + "Sum(Expenses) AS Expenses, ";
                    sSQL = sSQL + "Sum(Profit) AS Profit ";
                    sSQL = sSQL + "FROM  ";
                    sSQL = sSQL + "( ";
                    sSQL = sSQL + "SELECT ";
                    sSQL = sSQL + "Branch, ";
                    sSQL = sSQL + "(CASE  ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 1 THEN '1' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 2 THEN '2' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 3 THEN '3' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 4 THEN '4' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 5 THEN '5' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 6 THEN '6' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 7 THEN '7' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 8 THEN '8' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 9 THEN '9' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 10 THEN '10' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 11 THEN '11' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 12 THEN '12' ";
                    sSQL = sSQL + "END) AS VMONTH, ";
                    sSQL = sSQL + "Sum(BranchIncome) AS Income, ";
                    sSQL = sSQL + "Sum(BranchCN) AS CN, ";
                    sSQL = sSQL + "Sum(BranchDiscount) AS Discount, ";
                    sSQL = sSQL + "Sum(BranchExpenses) AS Expenses, ";
                    sSQL = sSQL + "Sum(BranchProfit) AS Profit ";
                    sSQL = sSQL + "from VWSummaryProfitNLoss ";
                    sSQL = sSQL + "WHERE TransactionDate Between @StartPeriod and @EndPeriod ";
                    sSQL = sSQL + "Group By Branch,Month(TransactionDate) ";
                    sSQL = sSQL + "UNION ALL ";
                    sSQL = sSQL + "SELECT  ";
                    sSQL = sSQL + "Branch, ";
                    sSQL = sSQL + "'13' AS VMONTH, ";
                    sSQL = sSQL + "Sum(BranchIncome) AS Income, ";
                    sSQL = sSQL + "Sum(BranchCN) AS CN, ";
                    sSQL = sSQL + "Sum(BranchDiscount) AS Discount, ";
                    sSQL = sSQL + "Sum(BranchExpenses) AS Expenses, ";
                    sSQL = sSQL + "Sum(BranchProfit) AS Profit ";
                    sSQL = sSQL + "FROM VWSummaryProfitNLoss ";
                    sSQL = sSQL + "WHERE TransactionDate Between @StartPeriod and @EndPeriod ";
                    sSQL = sSQL + "Group By Branch ";
                    sSQL = sSQL + ")A ";
                    sSQL = sSQL + "Group By CONVERT(INT,VMONTH) ";
                    sSQL = sSQL + " Order By CONVERT(INT,VMONTH) ";


                    cmd.CommandText = sSQL;
                    cmd.Parameters.AddWithValue("@StartPeriod", dtStartPeriod);
                    cmd.Parameters.AddWithValue("@EndPeriod", dtEndPeriod);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<SumProfitLoss> SumProfitLossList = new List<SumProfitLoss>();
                            while (dr.Read())
                            {
                                SumProfitLossList.Add(
                                    new SumProfitLoss(
                                    dr.GetString(dr.GetOrdinal("Month")),
                                    dr.GetDecimal(dr.GetOrdinal("Income")),
                                    dr.GetDecimal(dr.GetOrdinal("CN")),
                                    dr.GetDecimal(dr.GetOrdinal("Discount")),
                                    dr.GetDecimal(dr.GetOrdinal("Expenses")),
                                    dr.GetDecimal(dr.GetOrdinal("Profit")))
                                );
                            }
                            return SumProfitLossList;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static List<SumProfitLoss> GetList(DateTime dtStartPeriod, DateTime dtEndPeriod, string Branch)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sSQL = string.Empty;
                    sSQL = "SELECT ";
                    sSQL = sSQL + "(CASE ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 1 THEN 'JAN' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 2 THEN 'FEB' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 3 THEN 'MAR' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 4 THEN 'APR' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 5 THEN 'MAY' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 6 THEN 'JUN' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 7 THEN 'JULY' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 8 THEN 'AUG' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 9 THEN 'SEPT' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 10 THEN 'OCT' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 11 THEN 'NOV' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 12 THEN 'DEC' ";
                    sSQL = sSQL + "WHEN CONVERT(INT,VMONTH) = 13 THEN 'Total' ";
                    sSQL = sSQL + "END) AS Month, ";
                    sSQL = sSQL + "Sum(Income) AS Income, ";
                    sSQL = sSQL + "Sum(CN) AS CN, ";
                    sSQL = sSQL + "Sum(Discount) AS Discount, ";
                    sSQL = sSQL + "Sum(Expenses) AS Expenses, ";
                    sSQL = sSQL + "Sum(Profit) AS Profit ";
                    sSQL = sSQL + "FROM  ";
                    sSQL = sSQL + "( ";
                    sSQL = sSQL + "SELECT ";
                    sSQL = sSQL + "Branch, ";
                    sSQL = sSQL + "(CASE  ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 1 THEN '1' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 2 THEN '2' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 3 THEN '3' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 4 THEN '4' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 5 THEN '5' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 6 THEN '6' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 7 THEN '7' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 8 THEN '8' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 9 THEN '9' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 10 THEN '10' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 11 THEN '11' ";
                    sSQL = sSQL + "WHEN Month(TransactionDate) = 12 THEN '12' ";
                    sSQL = sSQL + "END) AS VMONTH, ";
                    sSQL = sSQL + "Sum(BranchIncome) AS Income, ";
                    sSQL = sSQL + "Sum(BranchCN) AS CN, ";
                    sSQL = sSQL + "Sum(BranchDiscount) AS Discount, ";
                    sSQL = sSQL + "Sum(BranchExpenses) AS Expenses, ";
                    sSQL = sSQL + "Sum(BranchProfit) AS Profit ";
                    sSQL = sSQL + "from VWSummaryProfitNLoss ";
                    sSQL = sSQL + "WHERE TransactionDate Between @StartPeriod and @EndPeriod ";
                    sSQL = sSQL + "Group By Branch,Month(TransactionDate) ";
                    sSQL = sSQL + "UNION ALL ";
                    sSQL = sSQL + "SELECT  ";
                    sSQL = sSQL + "Branch, ";
                    sSQL = sSQL + "'13' AS VMONTH, ";
                    sSQL = sSQL + "Sum(BranchIncome) AS Income, ";
                    sSQL = sSQL + "Sum(BranchCN) AS CN, ";
                    sSQL = sSQL + "Sum(BranchDiscount) AS Discount, ";
                    sSQL = sSQL + "Sum(BranchExpenses) AS Expenses, ";
                    sSQL = sSQL + "Sum(BranchProfit) AS Profit ";
                    sSQL = sSQL + "FROM VWSummaryProfitNLoss ";
                    sSQL = sSQL + "WHERE TransactionDate Between @StartPeriod and @EndPeriod ";
                    sSQL = sSQL + "Group By Branch ";
                    sSQL = sSQL + ")A ";
                    sSQL = sSQL + "WHERE Branch =@Branch ";
                    sSQL = sSQL + "Group By CONVERT(INT,VMONTH) ";
                    sSQL = sSQL + " Order By CONVERT(INT,VMONTH) ";


                    cmd.CommandText = sSQL;
                    cmd.Parameters.AddWithValue("@StartPeriod", dtStartPeriod);
                    cmd.Parameters.AddWithValue("@EndPeriod", dtEndPeriod);
                    cmd.Parameters.AddWithValue("@Branch", Branch);

                    using (SQLDataAccess sdaFactory = new SQLDataAccess(configuration))
                    {
                        using (SqlDataReader dr = sdaFactory.RetrieveData(cmd))
                        {
                            List<SumProfitLoss> SumProfitLossList = new List<SumProfitLoss>();
                            while (dr.Read())
                            {
                                SumProfitLossList.Add(
                                    new SumProfitLoss(
                                    dr.GetString(dr.GetOrdinal("Month")),
                                    dr.GetDecimal(dr.GetOrdinal("Income")),
                                    dr.GetDecimal(dr.GetOrdinal("CN")),
                                    dr.GetDecimal(dr.GetOrdinal("Discount")),
                                    dr.GetDecimal(dr.GetOrdinal("Expenses")),
                                    dr.GetDecimal(dr.GetOrdinal("Profit")))
                                );
                            }
                            return SumProfitLossList;
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
