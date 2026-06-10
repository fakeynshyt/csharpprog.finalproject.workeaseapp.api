namespace WorkeaseAPI.Models
{
    public class Report
    {
        public int ReportId { get; set; }
        public string ReportTitle { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string ReportFormat { get; set; } = string.Empty;
        public byte[]? ReportFileData { get; set; }
        public int GeneratedByUserId { get; set; }
        public User? GeneratedByUser { get; set; }
        public int? CdwCenterId { get; set; }
        public Center? Center { get; set; }
        public int? ReportMonth { get; set; }
        public int? ReportYear { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
