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
        
        public async Task<HealthRecord> CreateHealthRecordAsync(HealthRecord healthRecord)
        {
            healthRecord.HealthRecordDate = DateTime.Now;
            healthRecord.HealthRecordIsSync = true;

            _db.HealthRecords.Add(healthRecord);

            await _db.SaveChangesAsync();

            return healthRecord;
        }

        public async Task<bool> DeleteHealthRecordAsync(int healthId)
        {
            var record = await GetHealthRecordByIdAsync(healthId);
            if (record is null) return false;

            _db?.HealthRecords.Remove(record);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<HealthRecord>> GetFilteredHealthRecordsAsync(int? childId, int? centerId)
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

        public async Task<IEnumerable<HealthSummaryDto>> GetHealthRecordByGuardianIdAsync(int guardianId)
        {
            var child = await _db.Children
                .FirstOrDefaultAsync(c => c.GuardianId == guardianId && c.ChildIsActive);

            if(child is null) return Enumerable.Empty<HealthSummaryDto>();

            return await _db.HealthRecords
                .Where(h => h.ChildId == child.ChildId)
                .OrderByDescending(h => h.HealthRecordDate)
                .Select(h => new HealthSummaryDto
                {
                    HealthRecordDate = h.HealthRecordDate,
                    HealthWeightKg = h.HealthRecordWeigtKg,
                    HealthHeightCm = h.HealthRecordHeightCm,
                    HealthBmi = h.HealthRecordBmi,
                    IsPresent = h.HealthRecordIsPresent,
                    HealthNotes = h.HealthRecordNotes
                })
                .ToListAsync();
        }

        public async Task<HealthRecord?> GetHealthRecordByIdAsync(int healthId) =>
            await _db.HealthRecords
                .Include(h => h.Child)
                .FirstOrDefaultAsync(h => h.HealthRecordId == healthId);

        public async Task<bool> UpdateHealthRecordAsync(int healthId, HealthRecord healthRecord)
        {
            var record = await GetHealthRecordByIdAsync(healthId);
            if(record is null) return false;

            record.HealthRecordDate = healthRecord.HealthRecordDate;
            record.HealthRecordWeigtKg = healthRecord.HealthRecordWeigtKg;
            record.HealthRecordHeightCm = healthRecord.HealthRecordHeightCm;
            record.HealthRecordIsPresent = healthRecord.HealthRecordIsPresent;
            record.HealthRecordNotes = healthRecord.HealthRecordNotes;

            _db.HealthRecords.Add(record);

            await _db.SaveChangesAsync();

            return true;
        }
    }
}
