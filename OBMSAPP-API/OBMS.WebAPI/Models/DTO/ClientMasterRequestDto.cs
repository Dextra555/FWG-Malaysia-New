namespace OBMS.WebAPI.Models.DTO
{
    public class ClientMasterRequestDto
    {
        public int ID { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string PostCode { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Phone { get; set; }

        public string Fax { get; set; }

        public string Email { get; set; }

        public string Branch { get; set; }

        public string Status { get; set; }

        public string SuperClientCode { get; set; }

        public string PersonIncharge { get; set; }

        public DateTime AgreementStart { get; set; }

        public DateTime AgreementEnd { get; set; }

        public string Shortname { get; set; }

        public bool IsClientHeadQuarters { get; set; }

        public DateTime LastUpdatedDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public string LastUpdatedBy { get; set; }

        public int? SpecialOTHours { get; set; }
        public int? KPIHours { get; set; }
    }
}
