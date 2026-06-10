namespace WorkeaseAPI.DTOs
{
    public class SyncResultDto
    {
        public int CdwUserId { get; set; }
        public int SyncedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

        // IDs mapped back to MAUI so it can update local records
        public List<SyncIdMapDto> HealthIdMaps { get; set; } = new();
        public List<SyncIdMapDto> AttendanceIdMaps { get; set; } = new();
        public List<SyncIdMapDto> FeeIdMaps { get; set; } = new();
    }
}
