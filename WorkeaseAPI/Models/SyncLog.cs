namespace WorkeaseAPI.Models
{
    public class SyncLog
    {
        public int SyncLogId { get; set; }
        public int SyncLogUserId { get; set; }
        public DateTime SyncLoggedAt { get; set; } = DateTime.UtcNow;
        public int SyncLogRecordHealthRecordsSynced { get; set; }
        public int SyncLogRecordFeeRecordsSynced { get; set; }
        public int SyncLogFailedSyncedCounts { get; set; }
    }
}
