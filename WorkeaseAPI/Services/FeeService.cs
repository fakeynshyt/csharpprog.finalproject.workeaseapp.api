using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Helpers;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class FeeService : IFeeService
    {
        private readonly AppDbContext _db;
        private readonly IGrowthService _growthService;

        public FeeService(AppDbContext db, IGrowthService growthService)
        {
            _db = db;
            _growthService = growthService;
        }

        // ── Private — raw entity for internal use ─────────────────────
        private async Task<FeeRecord?> FindFeeAsync(int id) =>
            await _db.FeeRecords
                     .Include(f => f.Child)
                     .Include(f => f.RecordedByUser)
                     .FirstOrDefaultAsync(f => f.FeeRecordId == id);

        // ── Private mapper ────────────────────────────────────────────
        private static FeeSummaryDto MapToDto(FeeRecord f) =>
            new FeeSummaryDto
            {
                FeeId = f.FeeRecordId,
                FeeRecordReceiptNo = f.FeeRecordReceiptNo,
                ChildId = f.ChildId,
                ChildName = f.Child != null
            ? f.Child.ChildFirstName + " " + f.Child.ChildLastName
            : string.Empty,
                FeeMonth = f.FeeRecordMonth,
                FeeYear = f.FeeRecordYear,
                FeeMonthlyAmount = f.FeeRecordTotalAmount,
                IsPaid = f.FeeRecordIsPaid,
                FeePaidDate = f.FeeRecordPaidDate,
                FeeDueDate = f.FeeRecordDueDate,
                IsOverdue = f.FeeRecordIsOverdue,
                UserId = f.FeeRecordedByUserId,
                UserName = f.RecordedByUser?.UserName ?? string.Empty,
                FeeRecordCreatedAt = f.FeeRecordCreatedAt,
                FeeRecordUpdatedAt = f.FeeRecordUpdatedAt
            };

        public async Task<IEnumerable<FeeSummaryDto>> GetFilteredFeeRecordAsync(int? childId,
                                                                int? centerId,
                                                                string? receiptNo)
        {
            var query = _db.FeeRecords
                           .Include(f => f.Child)
                           .Include(f => f.RecordedByUser)
                           .Where(f => f.Child!.ChildIsActive == true)
                           .AsQueryable();

            // ── Filter by childId ─────────────────────────────────────────
            if (childId.HasValue)
                query = query.Where(f => f.ChildId == childId.Value);

            // ── Filter by centerId ────────────────────────────────────────
            if (centerId.HasValue)
                query = query.Where(f => f.Child!.CenterId == centerId.Value);

            // ── Filter by receipt number ──────────────────────────────────
            if (!string.IsNullOrWhiteSpace(receiptNo))
                query = query.Where(f => f.FeeRecordReceiptNo == receiptNo.Trim().ToUpper());

            var fees = await query
                            .OrderByDescending(f => f.FeeRecordYear)
                            .ThenByDescending(f => f.FeeRecordMonth)
                            .ToListAsync();

            // ── ChildId or ReceiptNo provided — return ALL records ────────
            if (childId.HasValue || !string.IsNullOrWhiteSpace(receiptNo))
                return fees.Select(MapToDto);

            // ── Center only — return LATEST fee per child ─────────────────
            if (centerId.HasValue)
                return fees
                       .GroupBy(f => f.ChildId)
                       .Select(g => g.OrderByDescending(f => f.FeeRecordYear)
                                      .ThenByDescending(f => f.FeeRecordMonth)
                                      .First())
                       .OrderBy(f => f.Child!.ChildLastName)
                       .Select(MapToDto);

            // ── No filter — return latest per child ───────────────────────
            return fees
                   .GroupBy(f => f.ChildId)
                   .Select(g => g.OrderByDescending(f => f.FeeRecordYear)
                                  .ThenByDescending(f => f.FeeRecordMonth)
                                  .First())
                   .Select(MapToDto);
        }

        public async Task<IEnumerable<FeeSummaryDto>> GetFeeRecordByGuardianUserIdAsync(
    int parentUserId, int? childId, int? month, int? year)
        {
            // ✅ Get ALL children of this parent
            var childIds = await _db.Children
                                    .Where(c => c.GuardianId == parentUserId
                                             && c.ChildIsActive == true)
                                    .Select(c => c.ChildId)
                                    .ToListAsync();

            if (!childIds.Any()) return Enumerable.Empty<FeeSummaryDto>();

            var query = _db.FeeRecords
                           .Include(f => f.Child)
                           .Include(f => f.RecordedByUser)
                           .Where(f => childIds.Contains(f.ChildId))
                           .AsQueryable();

            // ✅ Filter by specific child if provided
            if (childId.HasValue)
                query = query.Where(f => f.ChildId == childId.Value);

            if (month.HasValue)
                query = query.Where(f => f.FeeRecordMonth == month.Value);

            if (year.HasValue)
                query = query.Where(f => f.FeeRecordYear == year.Value);

            var fees = await query
                            .OrderByDescending(f => f.FeeRecordYear)
                            .ThenByDescending(f => f.FeeRecordMonth)
                            .ToListAsync();

            return fees.Select(MapToDto);
        }

        // ── GET BY ID — returns FeeSummaryDto ─────────────────────────
        public async Task<FeeSummaryDto?> GetFeeRecordByIdAsync(int id)
        {
            var fee = await FindFeeAsync(id);
            return fee is null ? null : MapToDto(fee);
        }
        

        // ── CREATE — returns FeeSummaryDto ────────────────────────────
        public async Task<FeeSummaryDto> CreateFeeRecordAsync(CreateFeeDto dto, int recordedByUserId)
        {
            var child = await _db.Children.FindAsync(dto.ChildId);
            if (child is null)
                throw new Exception($"Child with ID {dto.ChildId} not found.");

            var exists = await _db.FeeRecords
                                  .AnyAsync(f => f.ChildId == dto.ChildId
                                             && f.FeeRecordMonth == dto.FeeRecordMonth
                                             && f.FeeRecordYear == dto.FeeRecordYear);
            if (exists)
                throw new Exception(
                    $"Fee record already exists for " +
                    $"{dto.FeeRecordMonth}/{dto.FeeRecordYear}.");

            var fee = new FeeRecord
            {
                ChildId = dto.ChildId,
                FeeRecordMonth = dto.FeeRecordMonth,
                FeeRecordYear = dto.FeeRecordYear,
                FeeRecordMonthlyAmount = 100.00m,
                FeeRecordCarryOver = 0.00m,
                FeeRecordTotalAmount = 100.00m,
                FeeRecordIsPaid = false,
                FeeRecordPaidDate = null,
                FeeRecordDueDate = DateHelper.GetEndOfMonth(
                                 dto.FeeRecordMonth, dto.FeeRecordYear),
                FeeRecordIsOverdue = false,
                FeeRecordedByUserId = recordedByUserId,
                FeeRecordReceiptNo = ReceiptGenerator.GenerateUnique(
                                 dto.FeeRecordMonth, dto.FeeRecordYear),
                FeeRecordCreatedAt = DateTime.UtcNow, 
                FeeRecordUpdatedAt = DateTime.UtcNow
            };

            _db.FeeRecords.Add(fee);
            await _db.SaveChangesAsync();

            // ✅ Reload with relations then return DTO
            var created = await FindFeeAsync(fee.FeeRecordId);
            return MapToDto(created!);
        }

        // ── MARK PAID ─────────────────────────────────────────────────
        public async Task<bool> MarkFeeRecordAsPaidAsync(int id)
        {
            var fee = await FindFeeAsync(id);
            if (fee is null) return false;

            fee.FeeRecordIsPaid = true;
            fee.FeeRecordPaidDate = DateTime.UtcNow;
            fee.FeeRecordUpdatedAt = DateTime.UtcNow;

            if (fee.FeeRecordCarryOver > 0)
                await MarkPreviousUnpaidAsPaidAsync(
                    fee.ChildId,
                    fee.FeeRecordMonth,
                    fee.FeeRecordYear);

            await _db.SaveChangesAsync();
            await _growthService.RecalculatePointsAsync(fee.ChildId);

            return true;
        }

        private async Task MarkPreviousUnpaidAsPaidAsync(int childId,
                                                  int currentMonth,
                                                  int currentYear)
        {
            var previousUnpaid = await _db.FeeRecords
                                          .Where(f => f.ChildId == childId
                                                   && !f.FeeRecordIsPaid
                                                   && (f.FeeRecordYear < currentYear ||
                                                      (f.FeeRecordYear == currentYear &&
                                                       f.FeeRecordMonth < currentMonth)))
                                          .ToListAsync();

            foreach (var prev in previousUnpaid)
            {
                prev.FeeRecordIsPaid = true;
                prev.FeeRecordPaidDate = DateTime.UtcNow;
                prev.FeeRecordIsOverdue = false;
                prev.FeeRecordUpdatedAt = DateTime.UtcNow; // ✅ update on cascade pay
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────
        public async Task<bool> UpdateFeeRecordAsync(int id, UpdateFeeDto dto)
        {
            var fee = await FindFeeAsync(id);
            if (fee is null) return false;

            fee.FeeRecordMonth = dto.FeeRecordMonth;
            fee.FeeRecordYear = dto.FeeRecordYear;
            fee.FeeRecordTotalAmount = dto.FeeRecordAmount;
            fee.FeeRecordIsPaid = dto.FeeRecordIsPaid;
            fee.FeeRecordUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        // ── DELETE ────────────────────────────────────────────────────
        public async Task<bool> DeleteFeeRecordAsync(int id)
        {
            var fee = await FindFeeAsync(id);
            if (fee is null) return false;

            _db.FeeRecords.Remove(fee);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<FeeCalculatedDto> GetCalculatedFeeByChildAsync(int childId)
        {
            var fees = await _db.FeeRecords
                                .Where(f => f.ChildId == childId)
                                .OrderBy(f => f.FeeRecordYear)
                                .ThenBy(f => f.FeeRecordMonth)
                                .ToListAsync();

            decimal totalPaid = 0;
            decimal totalOverdue = 0;
            decimal totalRemaining = 0;

            for (int i = 0; i < fees.Count; i++)
            {
                var fee = fees[i];
                var nextFee = fees.ElementAtOrDefault(i + 1);

                bool absorbedByNextMonth = nextFee is not null
                                        && nextFee.FeeRecordIsPaid
                                        && nextFee.FeeRecordCarryOver > 0;

                if (fee.FeeRecordIsPaid)
                {
                    totalPaid += fee.FeeRecordMonthlyAmount;
                }
                else if (absorbedByNextMonth)
                {
                    totalPaid += fee.FeeRecordMonthlyAmount;
                }
                else
                {
                    totalRemaining += fee.FeeRecordMonthlyAmount;
                    if (fee.FeeRecordIsOverdue)
                        totalOverdue += fee.FeeRecordMonthlyAmount;
                }
            }

            return new FeeCalculatedDto
            {
                FeeTotalAmountPaid = totalPaid,
                FeeTotalAmountOverdue = totalOverdue,
                FeeTotalRemainingAmount = totalRemaining
            };
        }

        public async Task<FeesSummaryDto> GetOverallFeesSummaryAsync(int? centerId,
                                                                      int? month,
                                                                      int? year)
        {
            var query = _db.FeeRecords
                           .Include(f => f.Child)
                           .AsQueryable();

            if (centerId.HasValue)
                query = query.Where(f => f.Child!.CenterId == centerId.Value);

            if (month.HasValue)
                query = query.Where(f => f.FeeRecordMonth == month.Value);

            if (year.HasValue)
                query = query.Where(f => f.FeeRecordYear == year.Value);

            var fees = await query.ToListAsync();

            return new FeesSummaryDto
            {
                TotalCollected = fees.Where(f => f.FeeRecordIsPaid)
                                       .Sum(f => f.FeeRecordTotalAmount),
                TotalOutstanding = fees.Where(f => !f.FeeRecordIsPaid)
                                       .Sum(f => f.FeeRecordTotalAmount),
                TotalOverdue = fees.Where(f => !f.FeeRecordIsPaid && f.FeeRecordIsOverdue)
                                       .Sum(f => f.FeeRecordTotalAmount),
                TotalPaid = fees.Count(f => f.FeeRecordIsPaid),
                TotalUnpaid = fees.Count(f => !f.FeeRecordIsPaid),
                TotalOverdueCount = fees.Count(f => !f.FeeRecordIsPaid && f.FeeRecordIsOverdue)
            };
        }
    }
}
