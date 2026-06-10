namespace WorkeaseAPI.DTOs
{
    public class AttendanceSummaryDto
    {
        public int AttendanceId { get; set; }
        public int ChildId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public DateTime AttendanceDate { get; set; }
        public bool IsPresent { get; set; }
        public string RecordedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } 
        public DateTime UpdatedAt { get; set; } 
    }
}
