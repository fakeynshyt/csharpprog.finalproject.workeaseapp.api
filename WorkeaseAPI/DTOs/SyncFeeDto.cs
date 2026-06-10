namespace WorkeaseAPI.DTOs
{
    public class SyncFeeDto
    {
        public int? ServerFeeRecordId { get; set; }
        public int LocalId { get; set; } // ✅ added
        public int ChildId { get; set; }
        public int FeeRecordMonth { get; set; }
        public int FeeRecordYear { get; set; }
        public bool FeeRecordIsPaid { get; set; }
        public DateTime? FeeRecordPaidDate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string SyncAction { get; set; } = "create";
    }
}
