namespace WorkeaseAPI.DTOs
{
    public class SyncAttendanceDto
    {
        public int? ServerAttendanceId { get; set; }
        public int LocalId { get; set; } // ✅ MAUI sends this
        public int ChildId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public bool IsPresent { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string SyncAction { get; set; } = "create";
    }
}
