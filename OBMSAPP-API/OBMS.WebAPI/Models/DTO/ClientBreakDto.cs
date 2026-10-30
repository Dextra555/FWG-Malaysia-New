namespace OBMS.WebAPI.Models.DTO
{
    public class ClientBreakDto
    {
        public int ID { get; set; }

        public string ClientCode { get; set; }

        public decimal BreakHours { get; set; }

        public int SortOrder { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
