using System.Security.Cryptography;

namespace OBMS.WebAPI.Models.DTO
{
    public class EmployeeDailyAdvanceRow
    {
        public int ID { get; set; }
        public int EmployeeID { get; set; }
        public int Day { get; set; }
        public string? VoucherNo { get; set; }
        public decimal Amount { get; set; }
        public int NoOfInstallments { get; set; }
        public string? PaymentType { get; set; }
        public string? Particulars { get; set; }
        public int TransType { get; set; }
        public bool IsDeleted { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? AdvanceDate { get; set; }
        public DateTime? AdvanceTakenDate { get; set; }

        public EmployeeDailyAdvanceRow(int id, int employeeID, int day, decimal amount, string voucherNo, int noOfInstallments,
            string paymentType, string particulars, int transType, bool isDeleted, string lastUpdatedBy, DateTime advanceDate, DateTime advanceTakenDate)
        {
            ID = id;
            EmployeeID = employeeID;
            Day = day;
            Amount = amount;
            VoucherNo = voucherNo;
            NoOfInstallments = noOfInstallments;
            PaymentType = paymentType;
            Particulars = particulars;
            TransType = transType;
            IsDeleted = isDeleted;
            LastUpdatedBy = lastUpdatedBy;
            AdvanceDate = advanceDate;
            AdvanceTakenDate = advanceTakenDate;
        }
    }

}
