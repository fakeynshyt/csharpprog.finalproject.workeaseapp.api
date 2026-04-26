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

        public async Task<IEnumerable<FeeRecord>> GetFilteredFeeRecordAsync(
            int? centerId, int? month, int? year)
        {
            var query = _db.FeeRecords
                           .Include(f => f.Child)
                               .ThenInclude(c => c!.Center)
                           .AsQueryable();

            if (centerId.HasValue)
                query = query.Where(f => f.Child!.CenterId == centerId.Value);

            if (month.HasValue)
                query = query.Where(f => f.FeeRecordMonth == month.Value);

            if (year.HasValue)
                query = query.Where(f => f.FeeRecordYear == year.Value);

            return await query
                         .OrderByDescending(f => f.FeeRecordYear)
                         .ThenByDescending(f => f.FeeRecordMonth)
                         .ToListAsync();
        }

        public async Task<IEnumerable<FeeSummaryDto>> GetFeeRecordByGuardianUserIdAsync(int parentUserId)
        {
            var child = await _db.Children
                                 .FirstOrDefaultAsync(c => c.GuardianId == parentUserId
                                                        && c.ChildIsActive == true);
            if (child is null) return Enumerable.Empty<FeeSummaryDto>();

            return await _db.FeeRecords
                            .Where(f => f.ChildId == child.ChildId)
                            .OrderByDescending(f => f.FeeRecordYear)
                            .ThenByDescending(f => f.FeeRecordMonth)
                            .Select(f => new FeeSummaryDto
                            {
                                FeeMonth = f.FeeRecordMonth,
                                FeeYear = f.FeeRecordYear,
                                FeeMonthlyAmount = f.FeeRecordMonthlyAmount,
                                IsPaid = f.FeeRecordIsPaid,
                                FeePaidDate = f.FeeRecordPaidDate
                            })
                            .ToListAsync();
        }

        public async Task<FeeRecord?> GetFeeRecordByIdAsync(int id) =>
            await _db.FeeRecords
                     .Include(f => f.Child)
                         .ThenInclude(c => c!.Center)
                     .FirstOrDefaultAsync(f => f.FeeRecordId == id);

        // ✅ Uses CreateFeeDto instead of raw model
        public async Task<FeeRecord> CreateFeeRecordAsync(CreateFeeDto dto, int recordedByUserId)
        {
            // Validate child exists
            var child = await _db.Children.FindAsync(dto.ChildId);
            if (child is null)
                throw new Exception($"Child with ID {dto.ChildId} not found.");

            // Check if fee already exists for this month/year
            var exists = await _db.FeeRecords
                                  .AnyAsync(f => f.ChildId == dto.ChildId
                                             && f.FeeRecordMonth == dto.FeeRecordMonth
                                             && f.FeeRecordYear == dto.FeeRecordYear);
            if (exists)
                throw new Exception(
                    $"Fee record for this child already exists for " +
                    $"{dto.FeeRecordMonth}/{dto.FeeRecordYear}.");

            var fee = new FeeRecord
            {
                ChildId = dto.ChildId,
                FeeRecordMonth = dto.FeeRecordMonth,
                FeeRecordYear = dto.FeeRecordYear,
                FeeRecordMonthlyAmount = dto.FeeRecordMonth,
                FeeRecordIsPaid = false,
                FeeRecordPaidDate = null,
                FeeRecordedByUserId = recordedByUserId
            };

            _db.FeeRecords.Add(fee);
            await _db.SaveChangesAsync();

            return await GetFeeRecordByIdAsync(fee.FeeRecordId) ?? fee;
        }

        public async Task<bool> MarkFeeRecordAsPaidAsync(int id)
        {
            var fee = await GetFeeRecordByIdAsync(id);
            if (fee is null) return false;

            fee.FeeRecordIsPaid = true;
            fee.FeeRecordPaidDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        // ✅ Uses UpdateFeeDto instead of raw model
        public async Task<bool> UpdateFeeRecordAsync(int id, UpdateFeeDto dto)
        {
            var fee = await GetFeeRecordByIdAsync(id);
            if (fee is null) return false;

            fee.FeeRecordMonth = dto.FeeRecordMonth;
            fee.FeeRecordYear = dto.FeeRecordYear;
            fee.FeeRecordMonthlyAmount = dto.FeeRecordAmount;
            fee.FeeRecordIsPaid = dto.FeeRecordIsPaid;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteFeeRecordAsync(int id)
        {
            var fee = await GetFeeRecordByIdAsync(id);
            if (fee is null) return false;

            _db.FeeRecords.Remove(fee);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
