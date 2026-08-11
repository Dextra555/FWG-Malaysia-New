namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// DTO for one month row in the Operational P&amp;L section.
    /// Non-operational entries (Contra, BU, Transfer, Internal, Adjustment) are excluded
    /// from these figures and surfaced separately via <see cref="NonOperationalTransactionDto"/>.
    /// </summary>
    public class SeparatedProfitLossDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal OperationalIncome { get; set; }
        public decimal OperationalCN { get; set; }
        public decimal OperationalDiscount { get; set; }
        public decimal OperationalExpenses { get; set; }
        public decimal OperationalProfit { get; set; }
    }

    /// <summary>
    /// DTO for a single non-operational transaction (Contra, BU transfer,
    /// Internal transfer, Adjustment) shown in the secondary report section.
    /// </summary>
    public class NonOperationalTransactionDto
    {
        public string Month { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string ItemCategory { get; set; } = string.Empty;
        public string PaymentPurpose { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        /// <summary>"Credit" if the amount represents a credit, otherwise "Debit".</summary>
        public string TransactionNature { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
    }

    /// <summary>
    /// Combined response wrapper — operational P&amp;L rows plus the
    /// corresponding non-operational transactions for the same period.
    /// </summary>
    public class SeparatedProfitLossResponseDto
    {
        public List<SeparatedProfitLossDto> OperationalPnL { get; set; } = new();
        public List<NonOperationalTransactionDto> NonOperationalTransactions { get; set; } = new();
    }
}
