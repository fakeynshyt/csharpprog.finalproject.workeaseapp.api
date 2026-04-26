namespace WorkeaseAPI.DTOs
{
    public class GenerateReportRequest
    {
        public int ReportMonth { get; set; }
        public int ReportYear { get; set; }

        // "PDF" or "Word"
        public string ReportFormat { get; set; } = "PDF";

        // CDW writes their own story/observations for the month
        public string Observations { get; set; } = string.Empty;
    }
}
