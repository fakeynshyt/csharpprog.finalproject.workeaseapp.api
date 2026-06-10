using System.ComponentModel.DataAnnotations;

namespace WorkeaseAPI.DTOs
{
    public class GenerateReportFeeDto
    {
        public string CycleInfo { get; set; } = "12th Cycle Implementation";
        public string SchoolYear { get; set; } = "CY 2025-2026";
        public string PreparedBy { get; set; } = string.Empty;
        public string NotedBy { get; set; } = string.Empty;
        public int Month { get; set; } = DateTime.UtcNow.Month;
        public int Year { get; set; } = DateTime.UtcNow.Year;
    }
}
