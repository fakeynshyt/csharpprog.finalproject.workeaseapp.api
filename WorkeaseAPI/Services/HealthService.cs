using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class HealthService : IHealthService
    {
        private readonly AppDbContext _db;

        public HealthService(AppDbContext db) => _db = db;

        public async Task<IEnumerable<HealthRecord>> GetFilteredHealthRecordsAsync(
            int? childId, int? centerId)
        {
            var query = _db.HealthRecords
                           .Include(h => h.Child)
                               .ThenInclude(c => c!.Center)
                           .AsQueryable();

            if (childId.HasValue)
                query = query.Where(h => h.ChildId == childId.Value);

            if (centerId.HasValue)
                query = query.Where(h => h.Child!.CenterId == centerId.Value);

            return await query
                         .OrderByDescending(h => h.HealthRecordDate)
                         .ToListAsync();
        }

        public async Task<IEnumerable<HealthSummaryDto>> GetHealthRecordByParentUserIdAsync(int parentUserId)
        {
            var child = await _db.Children
                                 .FirstOrDefaultAsync(c => c.GuardianId == parentUserId
                                                        && c.ChildIsActive == true);
            if (child is null) return Enumerable.Empty<HealthSummaryDto>();

            return await _db.HealthRecords
                            .Where(h => h.ChildId == child.ChildId)
                            .OrderByDescending(h => h.HealthRecordDate)
                            .Select(h => new HealthSummaryDto
                            {
                                HealthRecordDate = h.HealthRecordDate,
                                HealthWeightKg = h.HealthRecordWeigtKg,
                                HealthHeightCm = h.HealthRecordHeightCm,
                                HealthBmi = h.HealthRecordHeightCm > 0
                                    ? h.HealthRecordWeigtKg /
                                      ((h.HealthRecordHeightCm / 100) *
                                       (h.HealthRecordHeightCm / 100))
                                    : 0,
                                IsPresent = h.HealthRecordIsPresent,
                                HealthNotes = h.HealthRecordNotes
                            })
                            .ToListAsync();
        }

        public async Task<HealthRecord?> GetHealthRecordByIdAsync(int id) =>
            await _db.HealthRecords
                     .Include(h => h.Child)
                         .ThenInclude(c => c!.Center)
                     .FirstOrDefaultAsync(h => h.HealthRecordId == id);

        // ✅ Uses CreateHealthDto instead of raw model
        public async Task<HealthRecord> CreateHealthRecordAsync(CreateHealthDto dto, int recordedByUserId)
        {
            // Validate child exists
            var child = await _db.Children.FindAsync(dto.ChildId);
            if (child is null)
                throw new Exception($"Child with ID {dto.ChildId} not found.");

            var record = new HealthRecord
            {
                ChildId = dto.ChildId,
                HealthRecordDate = dto.HealthRecordDate,
                HealthRecordWeigtKg = dto.HealthRecordWeigtKg,
                HealthRecordHeightCm = dto.HealthRecordHeightCm,
                HealthRecordIsPresent = dto.HealthRecordIsPresent,
                HealthRecordNotes = dto.HealthRecordNotes,
                HealthRecordedByUserId = recordedByUserId,
                HealthRecordIsSync = true,
                HealthRecordCreatedAt = DateTime.UtcNow
            };

            _db.HealthRecords.Add(record);
            await _db.SaveChangesAsync();

            return await GetHealthRecordByIdAsync(record.HealthRecordId) ?? record;
        }

        // ✅ Uses UpdateHealthDto instead of raw model
        public async Task<bool> UpdateHealthRecordAsync(int id, UpdateHealthDto dto)
        {
            var record = await GetHealthRecordByIdAsync(id);
            if (record is null) return false;

            record.HealthRecordDate = dto.HealthRecordDate;
            record.HealthRecordWeigtKg = dto.HealthRecordWeigtKg;
            record.HealthRecordHeightCm = dto.HealthRecordHeightCm;
            record.HealthRecordIsPresent = dto.HealthRecordIsPresent;
            record.HealthRecordNotes = dto.HealthRecordNotes;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteHealthRecordAsync(int id)
        {
            var record = await GetHealthRecordByIdAsync(id);
            if (record is null) return false;

            _db.HealthRecords.Remove(record);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
