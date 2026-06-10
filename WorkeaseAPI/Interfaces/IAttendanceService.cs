using WorkeaseAPI.DTOs;

namespace WorkeaseAPI.Interfaces
{
    public interface IAttendanceService
    {
        Task<IEnumerable<AttendanceSummaryDto>> GetFilteredAttendanceRecordAsync(int day, int month, int year,
                                                              int? childId, int? centerId);
        Task<AttendanceSummaryDto?> GetAttendanceRecordByIdAsync(int attendanceId);
        Task<AttendanceSummaryDto> CreateAttendanceRecordAsync(CreateAttendanceDto dto, int recordedByUserId);
        Task<bool> UpdateAttendanceRecordAsync(int attendanceId, UpdateAttendanceDto dto);
        Task<bool> DeleteAttendanceRecordAsync(int attendanceId);
    }
}
