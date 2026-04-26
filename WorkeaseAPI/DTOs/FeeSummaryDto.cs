namespace WorkeaseAPI.DTOs
{
    public class FeeSummaryDto
    {
        public int FeeMonth { get; set; }
        public int FeeYear { get; set; }
        public decimal FeeMonthlyAmount { get; set; }  // always 100
        public decimal FeeCarryOver { get; set; }  // from previous unpaid
        public decimal FeeTotalAmount { get; set; }  // monthly + carryover
        public bool IsPaid { get; set; }
        public DateTime? FeePaidDate { get; set; }
        public DateTime FeeDueDate { get; set; }
        public bool IsOverdue { get; set; }
    }
}
