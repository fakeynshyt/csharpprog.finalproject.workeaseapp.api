namespace WorkeaseAPI.DTOs
{
    public class HealthSummaryDto
    {
        public int HealthRecordId { get; set; }
        public int ChildId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public decimal HealthWeightKg { get; set; }
        public decimal HealthHeightCm { get; set; }
        public decimal HealthBmi { get; set; }
        public string BmiStatus { get; set; } = string.Empty;
        public string? HealthNotes { get; set; }
        public string RecordedUserName { get; set; } = string.Empty;
        public DateTime HealthRecordCreatedAt { get; set; }
        public DateTime HealthRecordUpdatedAt { get; set; }

    }
}
