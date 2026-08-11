namespace OBMS.WebAPI.Models.DTO
{
    public class KKDNExcelListDto
    {
        public KKDNExcelListDto(
        int empID,
        string branchCode,
        string name,
        string ic,
        DateTime dob,
        string nationality,
        string citizen,
        string race,
        string gender,
        string address,
        DateTime dateJoin,
        string jobTitle,
        string epf,
        string sosco,
        bool kdnVetting,
        bool hasTransfer)
        {
            EmpID = empID;
            BranchCode = branchCode;
            Name = name;
            IC = ic;
            DOB = dob;
            Nationality = nationality;
            Citizen = citizen;
            Race = race;
            Gender = gender;
            Address = address;
            DateJoin = dateJoin;
            JobTitle = jobTitle;
            EPF = epf;
            SOSCO = sosco;
            KDNVetting = kdnVetting;
            HasTransfer = hasTransfer;
        }

        public int EmpID { get; }
        public string BranchCode { get; }
        public string Name { get; }
        public string IC { get; }
        public DateTime DOB { get; }
        public string Nationality { get; }
        public string Citizen { get; }
        public string Race { get; }
        public string Gender { get; }
        public string Address { get; }
        public DateTime DateJoin { get; }
        public string JobTitle { get; }
        public string EPF { get; }
        public string SOSCO { get; }
        public bool KDNVetting { get; }
        public bool HasTransfer { get; }
    }
}
