namespace WorkeaseAPI.DTOs
{
    public class CreateFeeDto
    {
        public int ChildId { get; set; }
        public int FeeRecordMonth { get; set; }
        public int FeeRecordYear { get; set; }
        public decimal FeeRecordAmount { get; set; }
    }
}
