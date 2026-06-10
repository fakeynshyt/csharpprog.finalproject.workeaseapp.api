using WorkeaseAPI.Models;

namespace WorkeaseAPI.DTOs
{
    public class SyncPayloadDto
    {
        public int CdwUserId { get; set; } // filled by API from JWT

        // ── Health Records ────────────────────────────────────────────
        public List<SyncHealthDto> HealthRecords { get; set; } = new();

        // ── Attendance Records ────────────────────────────────────────
        public List<SyncAttendanceDto> AttendanceRecords { get; set; } = new();

        // ── Fee Records ───────────────────────────────────────────────
        public List<SyncFeeDto> FeeRecords { get; set; } = new();
    }
}
