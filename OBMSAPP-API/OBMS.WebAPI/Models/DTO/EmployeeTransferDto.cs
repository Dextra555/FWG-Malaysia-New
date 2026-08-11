namespace OBMS.WebAPI.Models.DTO
{
    public class EmployeeTransferDto
    {
        public int EMP_ID { get; set; }

        public string FROM_BRANCH_ID { get; set; }

        public string TO_BRANCH_ID { get; set; }

        public DateTime TRANSFER_DATE { get; set; }
    }
}
