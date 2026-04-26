namespace WorkeaseAPI.Models
{
    public class Report
    {
        public int ReportId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int CenterId { get; set; }
        public Center? Center { get; set; }
        public int ReportMonth { get; set; }
        public int ReportYear { get; set; }
        public string ReportFormat { get; set; } = "PDF"; // "PDF" or "Word"
        public string Observations { get; set; } = string.Empty;
        public byte[]? ReportFileData { get; set; }
        public DateTime ReportGeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
