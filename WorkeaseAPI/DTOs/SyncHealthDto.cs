namespace WorkeaseAPI.DTOs
{
    public class SyncHealthDto
    {
        public int? ServerHealthRecordId { get; set; }
        public int LocalId { get; set; }
        public int ChildId { get; set; }
        public DateTime HealthRecordDate { get; set; }
        public decimal HealthRecordWeigtKg { get; set; }
        public decimal HealthRecordHeightCm { get; set; }
        public string? HealthRecordNotes { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string SyncAction { get; set; } = "create";
    }
}
