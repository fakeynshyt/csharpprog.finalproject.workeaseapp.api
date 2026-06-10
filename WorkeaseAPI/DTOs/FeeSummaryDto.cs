namespace WorkeaseAPI.DTOs
{
    public class FeeSummaryDto
    {
        public int FeeId { get; set; }
        public string FeeRecordReceiptNo { get; set; } = string.Empty;
        public int ChildId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public int FeeMonth { get; set; }
        public int FeeYear { get; set; }
        public decimal FeeMonthlyAmount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? FeePaidDate { get; set; }
        public DateTime FeeDueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime FeeRecordCreatedAt { get; set; }
        public DateTime FeeRecordUpdatedAt { get; set; }
    }
}
