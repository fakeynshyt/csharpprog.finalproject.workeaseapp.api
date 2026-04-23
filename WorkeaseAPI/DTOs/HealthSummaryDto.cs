namespace WorkeaseAPI.DTOs
{
    public class HealthSummaryDto
    {
        public DateTime HealthRecordDate { get; set; }
        public decimal HealthWeightKg { get; set; }
        public decimal HealthHeightCm { get; set; }
        public decimal HealthBmi { get; set; }
        public bool IsPresent { get; set; }
        public string? HealthNotes { get; set; }
    }
}
