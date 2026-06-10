namespace WorkeaseAPI.Models
{
    public class FeeRecord
    {
        public int FeeRecordId { get; set; }
        public string FeeRecordReceiptNo { get; set; } = string.Empty;
        public int ChildId { get; set; }
        public Child? Child { get; set; }
        public int FeeRecordMonth { get; set; }
        public int FeeRecordYear { get; set; }
        public decimal FeeRecordMonthlyAmount { get; set; } = 100.00m;
        public decimal FeeRecordCarryOver { get; set; } = 0.00m;
        public decimal FeeRecordTotalAmount { get; set; }
        public bool FeeRecordIsPaid { get; set; } = false;
        public DateTime? FeeRecordPaidDate { get; set; }
        public DateTime FeeRecordDueDate { get; set; }
        public bool FeeRecordIsOverdue { get; set; } = false;
        public int FeeRecordedByUserId { get; set; }
        public User? RecordedByUser { get; set; }
        public DateTime FeeRecordCreatedAt { get; set; } = DateTime.UtcNow; 
        public DateTime FeeRecordUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
