namespace WorkeaseAPI.DTOs
{
    public class FeesSummaryDto
    {
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalOverdue { get; set; }
        public int TotalPaid { get; set; }
        public int TotalUnpaid { get; set; }
        public int TotalOverdueCount { get; set; }
    }
}
