using WorkeaseAPI.Models;

namespace WorkeaseAPI.DTOs
{
    public class FeeDownloadResultDto
    {
        public int NewFeesCount { get; set; }
        public int UpdatedFeesCount { get; set; }
        public List<FeeRecord> Fees { get; set; } = new();
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
    }
}
