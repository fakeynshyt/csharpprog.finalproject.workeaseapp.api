namespace WorkeaseAPI.Models
{
    public class AttendanceRecord
    {
        public int AttendanceRecordId { get; set; }
        public int ChildId { get; set; }
        public Child? Child { get; set; }
        public DateTime AttendanceRecordDate { get; set; }
        public bool AttendanceRecordIsPresent { get; set; }
        public int AttendanceRecordedByUserId { get; set; }
        public User? AttendanceRecordedByUser { get; set; }
        public DateTime AttendanceRecordCreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime AttendanceRecordUpdatedAt { get; set; }
        public bool AttendanceRecordIsSync { get; set; } = true;
    }
}
