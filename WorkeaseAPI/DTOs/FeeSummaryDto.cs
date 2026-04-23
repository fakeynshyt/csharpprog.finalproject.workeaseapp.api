namespace WorkeaseAPI.DTOs
{
    public class FeeSummaryDto
    {
        public int FeeMonth { get; set; }
        public int FeeYear { get; set; }
        public decimal FeeAmount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? FeePaidDate { get; set; }
    }
}
