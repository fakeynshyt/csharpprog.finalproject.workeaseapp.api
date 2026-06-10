using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.Helpers;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class AutoFeeService : IAutoFeeService
    {
        private readonly AppDbContext _db;

        public AutoFeeService(AppDbContext db) => _db = db;

        public async Task GenerateFirstFeeAsync(int childId, int enrolledByUserId)
        {
            var child = await _db.Children.FindAsync(childId);
            if (child is null) return;

            var enrollMonth = child.ChildEnrolledDate.Month;
            var enrollYear = child.ChildEnrolledDate.Year;

            var exists = await _db.FeeRecords
                                  .AnyAsync(f => f.ChildId == childId
                                             && f.FeeRecordMonth == enrollMonth
                                             && f.FeeRecordYear == enrollYear);
            if (exists) return;

            var fee = new FeeRecord
            {
                ChildId = childId,
                FeeRecordMonth = enrollMonth,
                FeeRecordYear = enrollYear,
                FeeRecordMonthlyAmount = 100.00m,
                FeeRecordCarryOver = 0.00m,
                FeeRecordTotalAmount = 100.00m,
                FeeRecordIsPaid = false,
                FeeRecordPaidDate = null,
                FeeRecordDueDate = DateHelper.GetEndOfMonth(enrollMonth, enrollYear),
                FeeRecordIsOverdue = false,
                FeeRecordedByUserId = enrolledByUserId,

                // ✅ Generate receipt on first fee too
                FeeRecordReceiptNo = ReceiptGenerator.GenerateUnique(enrollMonth, enrollYear)
            };

            _db.FeeRecords.Add(fee);
            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ First fee generated for ChildId {childId}: " +
                              $"{enrollMonth}/{enrollYear} — ₱100");
        }

        public async Task GenerateMonthlyFeesAsync()
        {
            var now = DateTime.UtcNow;

            // ── Get previous month ────────────────────────────────────────
            var prevMonth = now.Month == 1 ? 12 : now.Month - 1;
            var prevYear = now.Month == 1 ? now.Year - 1 : now.Year;

            // ── Check if previous month has ended ─────────────────────────
            var endOfPrevMonth = DateHelper.GetEndOfMonth(prevMonth, prevYear);

            if (now <= endOfPrevMonth)
                throw new Exception(
                    $"Cannot generate monthly fees yet. " +
                    $"Previous month ({new DateTime(prevYear, prevMonth, 1):MMMM yyyy}) " +
                    $"has not ended. " +
                    $"Please wait until after {endOfPrevMonth:MMMM dd, yyyy}.");

            // ── Generate for current month ────────────────────────────────
            var targetMonth = now.Month;
            var targetYear = now.Year;

            var children = await _db.Children
                                    .Where(c => c.ChildIsActive)
                                    .ToListAsync();

            int generated = 0;
            int skipped = 0;

            foreach (var child in children)
            {
                // Skip if already generated for this month
                var exists = await _db.FeeRecords
                                      .AnyAsync(f => f.ChildId == child.ChildId
                                                 && f.FeeRecordMonth == targetMonth
                                                 && f.FeeRecordYear == targetYear);
                if (exists)
                {
                    skipped++;
                    continue;
                }

                // Get all unpaid fees before this month
                var allUnpaid = await _db.FeeRecords
                                         .Where(f => f.ChildId == child.ChildId
                                                  && !f.FeeRecordIsPaid
                                                  && (f.FeeRecordYear < targetYear ||
                                                     (f.FeeRecordYear == targetYear &&
                                                      f.FeeRecordMonth < targetMonth)))
                                         .ToListAsync();

                decimal carryOver = allUnpaid.Sum(f => f.FeeRecordMonthlyAmount);

                // Mark all previous unpaid as overdue
                foreach (var unpaid in allUnpaid)
                    unpaid.FeeRecordIsOverdue = true;

                var newFee = new FeeRecord
                {
                    ChildId = child.ChildId,
                    FeeRecordMonth = targetMonth,
                    FeeRecordYear = targetYear,
                    FeeRecordMonthlyAmount = 100.00m,
                    FeeRecordCarryOver = carryOver,
                    FeeRecordTotalAmount = 100.00m + carryOver,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(targetMonth, targetYear),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = 1,
                    FeeRecordReceiptNo = ReceiptGenerator.GenerateUnique(targetMonth, targetYear)
                };

                _db.FeeRecords.Add(newFee);
                generated++;

                Console.WriteLine($"✅ Fee generated for ChildId {child.ChildId}: " +
                                  $"{targetMonth}/{targetYear} — " +
                                  $"₱100 + ₱{carryOver} carryover = " +
                                  $"₱{newFee.FeeRecordTotalAmount}");
            }

            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ Done — Generated: {generated}, Skipped: {skipped}");
        }

        public async Task ProcessOverdueFeesAsync()
        {
            var now = DateTime.UtcNow;

            var overdueFees = await _db.FeeRecords
                                       .Where(f => !f.FeeRecordIsPaid
                                                && !f.FeeRecordIsOverdue
                                                && f.FeeRecordDueDate < now)
                                       .ToListAsync();

            foreach (var fee in overdueFees)
            {
                fee.FeeRecordIsOverdue = true;
                Console.WriteLine($"⚠️ Overdue — ChildId {fee.ChildId}: " +
                                  $"{fee.FeeRecordMonth}/{fee.FeeRecordYear}");
            }

            await _db.SaveChangesAsync();
            Console.WriteLine($"✅ Processed {overdueFees.Count} overdue fees.");
        }
    }
}
