using System.Data.SqlClient;

namespace OBMS.WebAPI.BusinessObjects
{
    /// <summary>
    /// Operational P&amp;L — one month row.
    /// Queries BranchPayments/BranchPaymentDetails directly (same source as
    /// VWSummaryProfitNLoss) and excludes Contra, BU, Transfer, Internal and
    /// Adjustment rows so the figures contain only true Sales/Income and Expenses.
    /// </summary>
    public class SeparatedProfitLoss
    {
        private static readonly IConfiguration _configuration;

        static SeparatedProfitLoss()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();
        }

        public string Month { get; set; } = string.Empty;
        public decimal OperationalIncome { get; set; }
        public decimal OperationalCN { get; set; }
        public decimal OperationalDiscount { get; set; }
        public decimal OperationalExpenses { get; set; }
        public decimal OperationalProfit { get; set; }

        public SeparatedProfitLoss() { }

        public SeparatedProfitLoss(
            string month,
            decimal operationalIncome,
            decimal operationalCN,
            decimal operationalDiscount,
            decimal operationalExpenses,
            decimal operationalProfit)
        {
            Month = month;
            OperationalIncome = operationalIncome;
            OperationalCN = operationalCN;
            OperationalDiscount = operationalDiscount;
            OperationalExpenses = operationalExpenses;
            OperationalProfit = operationalProfit;
        }

        // ── Non-operational exclusion filter (WHERE clause fragment) ────────
        // ItemCategory and PaymentPurpose live on BranchPayments, NOT on the view.
        private const string ExcludeNonOperational =
            "AND (bp.ItemCategory NOT LIKE '%Contra%'    OR bp.ItemCategory IS NULL) " +
            "AND (bp.ItemCategory NOT LIKE '%BU%'        OR bp.ItemCategory IS NULL) " +
            "AND (bp.ItemCategory NOT LIKE '%Transfer%'  OR bp.ItemCategory IS NULL) " +
            "AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Contra%'     OR bp.PaymentPurpose IS NULL) " +
            "AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Transfer%'   OR bp.PaymentPurpose IS NULL) " +
            "AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Internal%'   OR bp.PaymentPurpose IS NULL) " +
            "AND (CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) NOT LIKE '%Adjustment%' OR bp.PaymentPurpose IS NULL) ";

        // ── Month-label CASE expression ──────────────────────────────────────
        private const string MonthLabel =
            "(CASE " +
            "WHEN CONVERT(INT,VMONTH) = 1  THEN 'JAN'   " +
            "WHEN CONVERT(INT,VMONTH) = 2  THEN 'FEB'   " +
            "WHEN CONVERT(INT,VMONTH) = 3  THEN 'MAR'   " +
            "WHEN CONVERT(INT,VMONTH) = 4  THEN 'APR'   " +
            "WHEN CONVERT(INT,VMONTH) = 5  THEN 'MAY'   " +
            "WHEN CONVERT(INT,VMONTH) = 6  THEN 'JUN'   " +
            "WHEN CONVERT(INT,VMONTH) = 7  THEN 'JULY'  " +
            "WHEN CONVERT(INT,VMONTH) = 8  THEN 'AUG'   " +
            "WHEN CONVERT(INT,VMONTH) = 9  THEN 'SEPT'  " +
            "WHEN CONVERT(INT,VMONTH) = 10 THEN 'OCT'   " +
            "WHEN CONVERT(INT,VMONTH) = 11 THEN 'NOV'   " +
            "WHEN CONVERT(INT,VMONTH) = 12 THEN 'DEC'   " +
            "WHEN CONVERT(INT,VMONTH) = 13 THEN 'Total' " +
            "END)";

        // ── Public overloads ─────────────────────────────────────────────────

        /// <summary>All branches.</summary>
        public static List<SeparatedProfitLoss> GetList(DateTime dtStartPeriod, DateTime dtEndPeriod)
            => Query(dtStartPeriod, dtEndPeriod, null);

        /// <summary>Single branch.</summary>
        public static List<SeparatedProfitLoss> GetList(DateTime dtStartPeriod, DateTime dtEndPeriod, string branch)
            => Query(dtStartPeriod, dtEndPeriod, branch);

        // ── Core query ───────────────────────────────────────────────────────

        private static List<SeparatedProfitLoss> Query(DateTime dtStart, DateTime dtEnd, string? branch)
        {
            try
            {
                using SqlCommand cmd = new SqlCommand();

                // Outer: convert numeric month to label + sum per month
                string sSQL =
                    $"SELECT {MonthLabel} AS Month, " +
                    "SUM(Income) AS Income, SUM(CN) AS CN, SUM(Discount) AS Discount, " +
                    "SUM(Expenses) AS Expenses, SUM(Profit) AS Profit " +
                    "FROM ( " +

                    // Per-month rows
                    "SELECT bpd.Branch, " +
                    "(CASE " +
                    "WHEN MONTH(bp.PaymentDate) = 1  THEN '1'  " +
                    "WHEN MONTH(bp.PaymentDate) = 2  THEN '2'  " +
                    "WHEN MONTH(bp.PaymentDate) = 3  THEN '3'  " +
                    "WHEN MONTH(bp.PaymentDate) = 4  THEN '4'  " +
                    "WHEN MONTH(bp.PaymentDate) = 5  THEN '5'  " +
                    "WHEN MONTH(bp.PaymentDate) = 6  THEN '6'  " +
                    "WHEN MONTH(bp.PaymentDate) = 7  THEN '7'  " +
                    "WHEN MONTH(bp.PaymentDate) = 8  THEN '8'  " +
                    "WHEN MONTH(bp.PaymentDate) = 9  THEN '9'  " +
                    "WHEN MONTH(bp.PaymentDate) = 10 THEN '10' " +
                    "WHEN MONTH(bp.PaymentDate) = 11 THEN '11' " +
                    "WHEN MONTH(bp.PaymentDate) = 12 THEN '12' " +
                    "END) AS VMONTH, " +
                    "SUM(bpd.Amount) AS Income, 0 AS CN, 0 AS Discount, " +
                    "SUM(bpd.Amount) AS Expenses, 0 - SUM(bpd.Amount) AS Profit " +
                    "FROM BranchPayments bp " +
                    "INNER JOIN BranchPaymentDetails bpd ON bpd.PaymentID = bp.ID " +
                    "WHERE bp.IsDeleted = 0 " +
                    "AND ISNULL(bpd.IsDeleted, 0) = 0 " +
                    "AND bp.PaymentDate BETWEEN @StartPeriod AND @EndPeriod " +
                    ExcludeNonOperational +
                    "GROUP BY bpd.Branch, MONTH(bp.PaymentDate) " +

                    // Grand-total row (VMONTH = 13)
                    "UNION ALL " +
                    "SELECT bpd.Branch, '13' AS VMONTH, " +
                    "SUM(bpd.Amount) AS Income, 0 AS CN, 0 AS Discount, " +
                    "SUM(bpd.Amount) AS Expenses, 0 - SUM(bpd.Amount) AS Profit " +
                    "FROM BranchPayments bp " +
                    "INNER JOIN BranchPaymentDetails bpd ON bpd.PaymentID = bp.ID " +
                    "WHERE bp.IsDeleted = 0 " +
                    "AND ISNULL(bpd.IsDeleted, 0) = 0 " +
                    "AND bp.PaymentDate BETWEEN @StartPeriod AND @EndPeriod " +
                    ExcludeNonOperational +
                    "GROUP BY bpd.Branch " +
                    ") A ";

                cmd.Parameters.AddWithValue("@StartPeriod", dtStart);
                cmd.Parameters.AddWithValue("@EndPeriod", dtEnd);

                if (!string.IsNullOrWhiteSpace(branch))
                {
                    sSQL += "WHERE Branch = @Branch ";
                    cmd.Parameters.AddWithValue("@Branch", branch);
                }

                sSQL += "GROUP BY CONVERT(INT, VMONTH) ORDER BY CONVERT(INT, VMONTH)";
                cmd.CommandText = sSQL;

                using SQLDataAccess sda = new SQLDataAccess(_configuration);
                using SqlDataReader dr = sda.RetrieveData(cmd);

                var list = new List<SeparatedProfitLoss>();
                while (dr.Read())
                {
                    list.Add(new SeparatedProfitLoss(
                        dr.GetString(dr.GetOrdinal("Month")),
                        dr.GetDecimal(dr.GetOrdinal("Income")),
                        dr.GetDecimal(dr.GetOrdinal("CN")),
                        dr.GetDecimal(dr.GetOrdinal("Discount")),
                        dr.GetDecimal(dr.GetOrdinal("Expenses")),
                        dr.GetDecimal(dr.GetOrdinal("Profit"))
                    ));
                }
                return list;
            }
            catch { throw; }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // Non-Operational Transaction
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A single non-operational row: Contra, BU transfer, Internal transfer,
    /// or Adjustment entry. Queries BranchPayments directly.
    /// </summary>
    public class NonOperationalTransaction
    {
        private static readonly IConfiguration _configuration;

        static NonOperationalTransaction()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();
        }

        public string Month { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string ItemCategory { get; set; } = string.Empty;
        public string PaymentPurpose { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionNature { get; set; } = string.Empty;  // "Debit" / "Credit"
        public DateTime TransactionDate { get; set; }

        public NonOperationalTransaction() { }

        public NonOperationalTransaction(
            string month, string branch, string itemCategory,
            string paymentPurpose, decimal amount, string transactionNature,
            DateTime transactionDate)
        {
            Month = month;
            Branch = branch;
            ItemCategory = itemCategory;
            PaymentPurpose = paymentPurpose;
            Amount = amount;
            TransactionNature = transactionNature;
            TransactionDate = transactionDate;
        }

        // ── Public overloads ─────────────────────────────────────────────────

        /// <summary>All branches.</summary>
        public static List<NonOperationalTransaction> GetList(DateTime dtStartPeriod, DateTime dtEndPeriod)
            => Query(dtStartPeriod, dtEndPeriod, null);

        /// <summary>Single branch.</summary>
        public static List<NonOperationalTransaction> GetList(DateTime dtStartPeriod, DateTime dtEndPeriod, string branch)
            => Query(dtStartPeriod, dtEndPeriod, branch);

        // ── Core query ───────────────────────────────────────────────────────

        private static List<NonOperationalTransaction> Query(DateTime dtStart, DateTime dtEnd, string? branch)
        {
            try
            {
                using SqlCommand cmd = new SqlCommand();

                string sSQL =
                    "SELECT " +
                    "(CASE " +
                    "WHEN MONTH(bp.PaymentDate) = 1  THEN 'JAN'  " +
                    "WHEN MONTH(bp.PaymentDate) = 2  THEN 'FEB'  " +
                    "WHEN MONTH(bp.PaymentDate) = 3  THEN 'MAR'  " +
                    "WHEN MONTH(bp.PaymentDate) = 4  THEN 'APR'  " +
                    "WHEN MONTH(bp.PaymentDate) = 5  THEN 'MAY'  " +
                    "WHEN MONTH(bp.PaymentDate) = 6  THEN 'JUN'  " +
                    "WHEN MONTH(bp.PaymentDate) = 7  THEN 'JULY' " +
                    "WHEN MONTH(bp.PaymentDate) = 8  THEN 'AUG'  " +
                    "WHEN MONTH(bp.PaymentDate) = 9  THEN 'SEPT' " +
                    "WHEN MONTH(bp.PaymentDate) = 10 THEN 'OCT'  " +
                    "WHEN MONTH(bp.PaymentDate) = 11 THEN 'NOV'  " +
                    "WHEN MONTH(bp.PaymentDate) = 12 THEN 'DEC'  " +
                    "END) AS Month, " +
                    "bpd.Branch, " +
                    "ISNULL(bp.ItemCategory, '') AS ItemCategory, " +
                    "CAST(ISNULL(bp.PaymentPurpose, '') AS NVARCHAR(MAX)) AS PaymentPurpose, " +
                    "SUM(bpd.Amount) AS Amount, " +
                    "CASE WHEN SUM(bpd.Amount) >= 0 THEN 'Debit' ELSE 'Credit' END AS TransactionNature, " +
                    "MAX(bp.PaymentDate) AS TransactionDate " +
                    "FROM BranchPayments bp " +
                    "INNER JOIN BranchPaymentDetails bpd ON bpd.PaymentID = bp.ID " +
                    "WHERE bp.IsDeleted = 0 " +
                    "AND ISNULL(bpd.IsDeleted, 0) = 0 " +
                    "AND bp.PaymentDate BETWEEN @StartPeriod AND @EndPeriod " +
                    "AND ( " +
                    "    bp.ItemCategory LIKE '%Contra%' " +
                    "    OR bp.ItemCategory LIKE '%BU%' " +
                    "    OR bp.ItemCategory LIKE '%Transfer%' " +
                    "    OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Contra%' " +
                    "    OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Transfer%' " +
                    "    OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Internal%' " +
                    "    OR CAST(bp.PaymentPurpose AS NVARCHAR(MAX)) LIKE '%Adjustment%' " +
                    ") ";

                cmd.Parameters.AddWithValue("@StartPeriod", dtStart);
                cmd.Parameters.AddWithValue("@EndPeriod", dtEnd);

                if (!string.IsNullOrWhiteSpace(branch))
                {
                    sSQL += "AND bpd.Branch = @Branch ";
                    cmd.Parameters.AddWithValue("@Branch", branch);
                }

                sSQL +=
                    "GROUP BY MONTH(bp.PaymentDate), bpd.Branch, " +
                    "ISNULL(bp.ItemCategory, ''), " +
                    "CAST(ISNULL(bp.PaymentPurpose, '') AS NVARCHAR(MAX)) " +
                    "ORDER BY MONTH(bp.PaymentDate), bpd.Branch";

                cmd.CommandText = sSQL;

                using SQLDataAccess sda = new SQLDataAccess(_configuration);
                using SqlDataReader dr = sda.RetrieveData(cmd);

                var list = new List<NonOperationalTransaction>();
                while (dr.Read())
                {
                    list.Add(new NonOperationalTransaction(
                        dr.GetString(dr.GetOrdinal("Month")),
                        dr.GetString(dr.GetOrdinal("Branch")),
                        dr.GetString(dr.GetOrdinal("ItemCategory")),
                        dr.GetString(dr.GetOrdinal("PaymentPurpose")),
                        dr.GetDecimal(dr.GetOrdinal("Amount")),
                        dr.GetString(dr.GetOrdinal("TransactionNature")),
                        dr.GetDateTime(dr.GetOrdinal("TransactionDate"))
                    ));
                }
                return list;
            }
            catch { throw; }
        }
    }
}
