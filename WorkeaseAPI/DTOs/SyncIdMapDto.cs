namespace WorkeaseAPI.DTOs
{
    public class SyncIdMapDto
    {
        public int LocalId { get; set; } // MAUI SQLite local ID
        public int ServerId { get; set; } // API server ID
    }
}
