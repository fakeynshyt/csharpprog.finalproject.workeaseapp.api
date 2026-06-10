using System.ComponentModel.DataAnnotations;

namespace WorkeaseAPI.DTOs
{
    public class GenerateNarrativeDto
    {
        public int CenterId { get; set; }
        public string CycleInfo { get; set; } = "12th Cycle Implementation";
        public string SchoolYear { get; set; } = "CY 2025-2026";
        public string PreparedBy { get; set; } = string.Empty;
        public string NotedBy { get; set; } = string.Empty;
        public int Month { get; set; } = DateTime.UtcNow.Month;
        public int Year { get; set; } = DateTime.UtcNow.Year;
        public string Observations { get; set; } = string.Empty;
    }
}
