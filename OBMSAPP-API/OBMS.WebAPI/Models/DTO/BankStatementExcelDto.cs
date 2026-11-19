namespace OBMS.WebAPI.Models.DTO
{
    public class BankStatementExcelDto
    {
        public string AccountNo { get; set; }
        public string Salary { get; set; }
        public string Name { get; set; }
        public string SecurityNo { get; set; }
        /// <summary>
        /// IC type indicator: "01" = New IC, "02" = Old IC, "03" = Passport
        /// </summary>
        public string IcType { get; set; }

        // Add other properties if needed...

        public BankStatementExcelDto(string name, string accountNo, string salary, string securityNo, string icType = "01")
        {
            Name = name;
            AccountNo = accountNo;
            Salary = salary;
            SecurityNo = securityNo;
            IcType = icType;
        }
    }

}
