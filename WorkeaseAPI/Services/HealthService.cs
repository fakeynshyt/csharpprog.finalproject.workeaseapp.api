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

        private async Task<HealthRecord?> FindHealthRecordAsync(int id) =>
            await _db.HealthRecords
             .Include(h => h.Child)
                 .ThenInclude(c => c!.Center)
             .Include(h => h.RecordedByUser)
             .FirstOrDefaultAsync(h => h.HealthRecordId == id);

        private static HealthSummaryDto MapToDto(HealthRecord h)
        {
            var heightM = h.HealthRecordHeightCm / 100;
            var bmi = h.HealthRecordHeightCm > 0
                ? Math.Round(h.HealthRecordWeightKg / (heightM * heightM), 2)
                : 0;

            var bmiStatus = bmi switch
            {
                0 => "No Data",
                < 18.5m => "Underweight",
                < 25.0m => "Normal",
                _ => "Overweight"
            };

            return new HealthSummaryDto
            {
                HealthRecordId = h.HealthRecordId,
                ChildId = h.ChildId,
                ChildName = h.Child != null
                    ? h.Child.ChildFirstName + " " + h.Child.ChildLastName
                    : string.Empty,
                HealthWeightKg = h.HealthRecordWeightKg,
                HealthHeightCm = h.HealthRecordHeightCm,
                HealthBmi = bmi,
                BmiStatus = bmiStatus,
                HealthNotes = h.HealthRecordNotes,
                RecordedUserName = h.RecordedByUser?.UserName ?? string.Empty,
                HealthRecordCreatedAt = h.HealthRecordDate,
                HealthRecordUpdatedAt = h.HealthRecordUpdatedAt
            };
        }

        public async Task<IEnumerable<HealthSummaryDto>> GetFilteredHealthRecordsAsync(
    int? childId, int? centerId)
        {
            var query = _db.HealthRecords
                           .Include(h => h.Child)
                               .ThenInclude(c => c!.Center)
                           .Include(h => h.RecordedByUser)
                           .Where(h => h.Child!.ChildIsActive == true)
                           .AsQueryable();

            if (childId.HasValue)
                query = query.Where(h => h.ChildId == childId.Value);

            if (centerId.HasValue)
                query = query.Where(h => h.Child!.CenterId == centerId.Value);

            var records = await query
                               .OrderByDescending(h => h.HealthRecordDate)
                               .ToListAsync();

            if (childId.HasValue)
                return records.Select(MapToDto);

            if (centerId.HasValue)
                return records
                       .GroupBy(h => h.ChildId)
                       .Select(g => g.OrderByDescending(h => h.HealthRecordDate).First())
                       .OrderBy(h => h.Child!.ChildLastName)
                       .Select(MapToDto);

            return records
                   .GroupBy(h => h.ChildId)
                   .Select(g => g.OrderByDescending(h => h.HealthRecordDate).First())
                   .Select(MapToDto);
        }

        public async Task<IEnumerable<HealthSummaryDto>> GetHealthRecordByGuardianIdAsync(int parentUserId)
        {
            var child = await _db.Children
                                 .FirstOrDefaultAsync(c => c.GuardianId == parentUserId
                                                        && c.ChildIsActive == true);
            if (child is null) return Enumerable.Empty<HealthSummaryDto>();

            var records = await _db.HealthRecords
                                   .Include(h => h.Child)
                                   .Include(h => h.RecordedByUser)
                                   .Where(h => h.ChildId == child.ChildId
                                            && h.Child!.ChildIsActive == true)
                                   .OrderByDescending(h => h.HealthRecordDate)
                                   .ToListAsync();

            return records.Select(MapToDto);
        }

        public async Task<HealthSummaryDto?> GetHealthRecordByIdAsync(int id)
        {
            var record = await FindHealthRecordAsync(id);
            return record is null ? null : MapToDto(record);
        }

        public async Task<HealthSummaryDto> CreateHealthRecordAsync(CreateHealthDto dto,
                                                 int recordedByUserId)
        {
            var child = await _db.Children.FindAsync(dto.ChildId);
            if (child is null)
                throw new Exception($"Child with ID {dto.ChildId} not found.");

            var record = new HealthRecord
            {
                ChildId = dto.ChildId,
                HealthRecordDate = dto.HealthRecordDate,
                HealthRecordWeightKg = dto.HealthRecordWeigtKg,
                HealthRecordHeightCm = dto.HealthRecordHeightCm,
                HealthRecordNotes = dto.HealthRecordNotes,
                HealthRecordedByUserId = recordedByUserId,
                HealthRecordIsSync = true,
                HealthRecordCreatedAt = DateTime.UtcNow,
                HealthRecordUpdatedAt = DateTime.UtcNow
            };

            _db.HealthRecords.Add(record);
            await _db.SaveChangesAsync();

            return MapToDto(record);
        }

        public async Task<bool> UpdateHealthRecordAsync(int id, UpdateHealthDto dto)
        {
            var record = await FindHealthRecordAsync(id);
            if (record is null) return false;

            record.HealthRecordDate = dto.HealthRecordDate;
            record.HealthRecordWeightKg = dto.HealthRecordWeigtKg;
            record.HealthRecordHeightCm = dto.HealthRecordHeightCm;
            record.HealthRecordNotes = dto.HealthRecordNotes;
            record.HealthRecordUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteHealthRecordAsync(int id)
        {
            var record = await FindHealthRecordAsync(id);
            if (record is null) return false;

            _db.HealthRecords.Remove(record);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<AbnormalBmiDto>> GetAbnormalChildrenBmiAsync()
        {
            var latestRecords = await _db.HealthRecords
                                         .Include(h => h.Child)
                                         .Where(h => h.Child!.ChildIsActive == true
                                                  && h.HealthRecordHeightCm > 0
                                                  && h.HealthRecordWeightKg > 0)
                                         .GroupBy(h => h.ChildId)
                                         .Select(g => g.OrderByDescending(h => h.HealthRecordDate)
                                                       .First())
                                         .ToListAsync();

            var result = new List<AbnormalBmiDto>();

            foreach (var record in latestRecords)
            {
                var heightM = record.HealthRecordHeightCm / 100;
                var bmi = Math.Round(
                    record.HealthRecordWeightKg / (heightM * heightM), 2);

                if (bmi >= 18.5m && bmi < 25m) continue;

                result.Add(new AbnormalBmiDto
                {
                    ChildId = record.Child!.ChildId,
                    ChildFirstName = record.Child.ChildFirstName,
                    ChildLastName = record.Child.ChildLastName,
                    ChildBirthDate = record.Child.ChildBirthDate,
                    WeightKg = record.HealthRecordWeightKg,
                    HeightCm = record.HealthRecordHeightCm,
                    Bmi = bmi,
                    BmiStatus = bmi < 18.5m ? "Underweight" : "Overweight"
                });
            }

            return result.OrderBy(r => r.BmiStatus)
                         .ThenBy(r => r.ChildLastName);
        }

        // Updated GetByParentUserIdAsync
        public async Task<IEnumerable<HealthSummaryDto>> GetHealthRecordByGuardianIdAsync(
            int parentUserId, int? childId, int? month, int? year)
        {
            // ✅ Get ALL children of this parent
            var childIds = await _db.Children
                                    .Where(c => c.GuardianId == parentUserId
                                             && c.ChildIsActive == true)
                                    .Select(c => c.ChildId)
                                    .ToListAsync();

            if (!childIds.Any()) return Enumerable.Empty<HealthSummaryDto>();

            var query = _db.HealthRecords
                           .Include(h => h.Child)
                           .Include(h => h.RecordedByUser)
                           .Where(h => childIds.Contains(h.ChildId)
                                    && h.Child!.ChildIsActive == true)
                           .AsQueryable();

            // ✅ Filter by specific child if provided
            if (childId.HasValue)
                query = query.Where(h => h.ChildId == childId.Value);

            if (month.HasValue)
                query = query.Where(h => h.HealthRecordDate.Month == month.Value);

            if (year.HasValue)
                query = query.Where(h => h.HealthRecordDate.Year == year.Value);

            var records = await query
                               .OrderByDescending(h => h.HealthRecordDate)
                               .ToListAsync();

            return records.Select(MapToDto);
        }
    }
}
