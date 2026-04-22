namespace WorkeaseAPI.Models
{
    public class Report
    {
        public int ReportId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int CenterId { get; set; }
        public Center? Center { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Format { get; set; } = "PDF";
        public string Observations { get; set; } = string.Empty;
        public byte[]? FileData { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
