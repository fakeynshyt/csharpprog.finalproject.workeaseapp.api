namespace WorkeaseAPI.DTOs
{
    public class ReportListDto
    {
        public int ReportId { get; set; }
        public string ReportTitle { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string ReportFormat { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = string.Empty;
        public string? CenterName { get; set; }
        public int? ReportMonth { get; set; }
        public int? ReportYear { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
