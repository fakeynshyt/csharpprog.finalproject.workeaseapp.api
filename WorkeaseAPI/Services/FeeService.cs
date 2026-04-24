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
        
        public Task<FeeRecord> CreateFeeRecord(FeeRecord feeRecord)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteFeeRecordAsync(int feeId)
        {
            throw new NotImplementedException();
        }

        public async Task<FeeRecord?> GetFeeRecordById(int feeId) =>
            await _db.FeeRecords
            .Include(f => f.Child)
            .FirstOrDefaultAsync(f => f.FeeRecordId == feeId);
        

        public async Task<IEnumerable<FeeSummaryDto>> GetFeeRecordsByGuardianId(int guardianId)
        {
            var child = await _db.Children
                .FirstOrDefaultAsync(c => c.UserId == guardianId && c.ChildIsActive);

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

        public Task<bool> UpdateFeeRecordAsync(int feeId, FeeRecord feeRecord)
        {
            throw new NotImplementedException();
        }
    }
}
