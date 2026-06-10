using WorkeaseAPI.Models;

namespace WorkeaseAPI.DTOs
{
    public class InitialPackageDto
    {
        public UserProfileDto UserProfile { get; set; } = new();
        public string Role { get; set; } = string.Empty;
        public DateTime PackagedAt { get; set; } = DateTime.UtcNow;
        public CdwCenterDto? AssignedCenter { get; set; }
        public List<ChildSummaryDto> Children { get; set; } = new();
        public List<HealthRecord> HealthRecords { get; set; } = new();
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
        public List<FeeRecord> FeeRecords { get; set; } = new();

    }
}
