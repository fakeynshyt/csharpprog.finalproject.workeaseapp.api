namespace WorkeaseAPI.DTOs
{
    public class UpdateFeeDto
    {
        public int FeeRecordMonth { get; set; }
        public int FeeRecordYear { get; set; }
        public decimal FeeRecordAmount { get; set; }
        public bool FeeRecordIsPaid { get; set; }
    }
}
