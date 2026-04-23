namespace WorkeaseAPI.DTOs
{
    public class SyncResultDto
    {
        public int SyncedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    }
}
