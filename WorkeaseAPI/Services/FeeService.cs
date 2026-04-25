using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class FeeService : IFeeService
    {
        private readonly AppDbContext _db;

        public FeeService(AppDbContext db) => _db = db;
        
        public async Task<FeeRecord> CreateFeeRecord(FeeRecord feeRecord)
        {
            _db.FeeRecords.Add(feeRecord);

            await _db.SaveChangesAsync();

            return feeRecord;
        }

        public async Task<bool> DeleteFeeRecordAsync(int feeId)
        {
            var record = await GetFeeRecordById(feeId);
            if(record is null) return false;

            _db.FeeRecords.Remove(record);

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<FeeRecord?> GetFeeRecordById(int feeId) =>
            await _db.FeeRecords
            .Include(f => f.Child)
            .FirstOrDefaultAsync(f => f.FeeRecordId == feeId);
        

        public async Task<IEnumerable<FeeSummaryDto>> GetFeeRecordsByGuardianId(int guardianId)
        {
            var child = await _db.Children
                .FirstOrDefaultAsync(c => c.GuardianId == guardianId && c.ChildIsActive);

            if(child is null) return Enumerable.Empty<FeeSummaryDto>();

            return await _db.FeeRecords
                .Where(f => f.ChildId == child.ChildId)
                .OrderByDescending(f => f.FeeRecordYear)
                .ThenByDescending(f => f.FeeRecordMonth)
                .Select(f => new FeeSummaryDto
                {
                    FeeMonth = f.FeeRecordMonth,
                    FeeYear = f.FeeRecordYear,
                    FeeAmount = f.FeeRecordAmount,
                    IsPaid = f.FeeRecordIsPaid,
                    FeePaidDate = f.FeeRecordPaidDate,
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<FeeRecord>> GetFilteredFeeRecordsAsync(int? centerId, int? month, int? year)
        {
            var query = _db.FeeRecords
                .Include(f => f.Child)
                .AsQueryable();

            if(centerId.HasValue) 
                query = query.Where(f => f.Child!.CenterId == centerId);

            if(month.HasValue)
                query = query.Where(f => f.FeeRecordMonth ==  month.Value);

            if(year.HasValue)
                query = query.Where(f => f.FeeRecordYear == year.Value);

            return await query
                .OrderByDescending(f => f.FeeRecordYear)
                .ThenByDescending(f => f.FeeRecordMonth)
                .ToListAsync();
        }

        public async Task<bool> MarkFeeRecordPaidAsync(int feeId)
        {
            var record = await GetFeeRecordById(feeId);
            if(record is null) return false;

            record.FeeRecordIsPaid = true;
            record.FeeRecordPaidDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateFeeRecordAsync(int feeId, FeeRecord feeRecord)
        {
            var record = await GetFeeRecordById(feeId);
            if(record is null) return false;

            record.FeeRecordMonth = feeRecord.FeeRecordMonth;
            record.FeeRecordYear = feeRecord.FeeRecordYear;
            record.FeeRecordAmount = feeRecord.FeeRecordAmount;
            record.FeeRecordIsPaid = feeRecord.FeeRecordIsPaid;
           
            await _db.SaveChangesAsync();

            return true;
        }
    }
}
