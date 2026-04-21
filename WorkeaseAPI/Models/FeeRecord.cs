namespace WorkeaseAPI.Models
{
    public class FeeRecord
    {
        public int FeeRecordId { get; set; }
        public int FeeRecordChildId { get; set; }
        public Child? Child { get; set; }
        public int FeeRecordMonth { get; set; } // 1-12 for January to December
        public int FeeRecordYear { get; set; }
        public decimal FeeRecordAmount { get; set; }
        public DateTime FeeRecordDueDate { get; set; }
        public bool FeeRecordIsPaid { get; set; } = false;
        public DateTime FeeRecordPaidDate { get; set; }
        public int FeeRecordedByUserId { get; set; }
        public bool FeeRecordIsSync { get; set; } = false;
    }
}
