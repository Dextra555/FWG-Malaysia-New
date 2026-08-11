namespace OBMS.WebAPI.Models.DTO
{
    public class BankStatementExcelDto
    {
        public string AccountNo { get; set; }
        public string Salary { get; set; }
        public string Name { get; set; }
        public string SecurityNo { get; set; }

        // Add other properties if needed...

        public BankStatementExcelDto(string name, string accountNo, string salary, string securityNo)
        {
            Name = name;
            AccountNo = accountNo;
            Salary = salary;
            SecurityNo = securityNo;
        }
    }

}
