using WorkeaseAPI.Models;

namespace WorkeaseAPI.DTOs
{
    public class SyncPayloadDto
    {
        public int CdwUserId { get; set; } 
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
        public List<HealthRecord> HealthRecords { get; set; } = new();
        public List<FeeRecord> FeeRecords { get; set; } = new();
    }
}
