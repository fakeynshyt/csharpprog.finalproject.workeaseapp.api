using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _db;

        public AttendanceService(AppDbContext db) => _db = db;

        private async Task<AttendanceRecord?> FindAsync(int id) =>
            await _db.AttendanceRecords
                     .Include(a => a.Child)
                     .Include(a => a.AttendanceRecordedByUser)
                     .FirstOrDefaultAsync(a => a.AttendanceRecordId == id);

        private static AttendanceSummaryDto MapToDto(AttendanceRecord a) =>
            new AttendanceSummaryDto
            {
                AttendanceId = a.AttendanceRecordId,
                ChildId = a.ChildId,
                ChildName = a.Child != null
                    ? a.Child.ChildFirstName + " " + a.Child.ChildLastName
                    : string.Empty,
                AttendanceDate = a.AttendanceRecordDate,
                IsPresent = a.AttendanceRecordIsPresent,
                RecordedBy = a.AttendanceRecordedByUser?.UserName ?? string.Empty,
                CreatedAt = a.AttendanceRecordCreatedAt,  
                UpdatedAt = a.AttendanceRecordUpdatedAt 
            };

        public async Task<IEnumerable<AttendanceSummaryDto>> GetFilteredAttendanceRecordAsync(int day,
                                                                       int month,
                                                                       int year,
                                                                       int? childId,
                                                                       int? centerId)
        {
            // ✅ Validate day, month and year
            if (day < 1 || day > 31)
                throw new Exception("Day must be between 1 and 31.");

            if (month < 1 || month > 12)
                throw new Exception("Month must be between 1 and 12.");

            if (year < 2000 || year > 2100)
                throw new Exception("Please enter a valid year.");

            var query = _db.AttendanceRecords
                           .Include(a => a.Child)
                               .ThenInclude(c => c!.Center)
                           .Include(a => a.AttendanceRecordedByUser)
                           .Where(a => a.Child!.ChildIsActive == true
                                    && a.AttendanceRecordDate.Day == day
                                    && a.AttendanceRecordDate.Month == month
                                    && a.AttendanceRecordDate.Year == year)
                           .AsQueryable();

            if (childId.HasValue)
                query = query.Where(a => a.ChildId == childId.Value);

            if (centerId.HasValue)
                query = query.Where(a => a.Child!.CenterId == centerId.Value);

            var records = await query
                               .OrderByDescending(a => a.AttendanceRecordDate)
                               .ToListAsync();

            return records.Select(MapToDto);
        }

        public async Task<AttendanceSummaryDto?> GetAttendanceRecordByIdAsync(int attendanceId)
        {
            var record = await FindAsync(attendanceId);
            return record is null ? null : MapToDto(record);
        }

        public async Task<AttendanceSummaryDto> CreateAttendanceRecordAsync(CreateAttendanceDto dto,
                                                             int recordedByUserId)
        {
            var child = await _db.Children.FindAsync(dto.ChildId);
            if (child is null)
                throw new Exception($"Child with ID {dto.ChildId} not found.");

            var exists = await _db.AttendanceRecords
                                  .AnyAsync(a => a.ChildId == dto.ChildId
                                             && a.AttendanceRecordDate.Date == dto.AttendanceDate.Date);
            if (exists)
                throw new Exception(
                    $"Attendance for this child on " +
                    $"{dto.AttendanceDate:MMMM dd, yyyy} already exists.");

            var record = new AttendanceRecord
            {
                ChildId = dto.ChildId,
                AttendanceRecordDate = dto.AttendanceDate,
                AttendanceRecordIsPresent = dto.IsPresent,
                AttendanceRecordedByUserId = recordedByUserId,
                AttendanceRecordIsSync = true,
                AttendanceRecordCreatedAt = DateTime.UtcNow,
                AttendanceRecordUpdatedAt = DateTime.UtcNow  // ✅ set on create
            };

            _db.AttendanceRecords.Add(record);
            await _db.SaveChangesAsync();

            var created = await FindAsync(record.AttendanceRecordId);
            return MapToDto(created!);
        }

        public async Task<bool> UpdateAttendanceRecordAsync(int attendanceId, UpdateAttendanceDto dto)
        {
            var record = await FindAsync(attendanceId);
            if (record is null) return false;

            var conflict = await _db.AttendanceRecords
                                    .AnyAsync(a => a.ChildId == record.ChildId
                                               && a.AttendanceRecordDate.Date == dto.AttendanceDate.Date
                                               && a.AttendanceRecordId != attendanceId);
            if (conflict)
                throw new Exception(
                    $"Attendance for this child on " +
                    $"{dto.AttendanceDate:MMMM dd, yyyy} already exists.");

            record.AttendanceRecordDate = dto.AttendanceDate;
            record.AttendanceRecordIsPresent = dto.IsPresent;
            record.AttendanceRecordUpdatedAt = DateTime.UtcNow; // ✅ update on edit

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAttendanceRecordAsync(int attendanceId)
        {
            var record = await FindAsync(attendanceId);
            if (record is null) return false;

            _db.AttendanceRecords.Remove(record);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
