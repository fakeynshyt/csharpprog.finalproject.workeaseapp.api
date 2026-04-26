namespace WorkeaseAPI.DTOs
{
    public class ReportSummaryDto
    {
        public int ReportId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public int ReportMonth { get; set; }
        public int ReportYear { get; set; }
        public string ReportFormat { get; set; } = string.Empty;
        public DateTime ReportGeneratedAt { get; set; }
    }
}
