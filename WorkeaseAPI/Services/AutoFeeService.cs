using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using WorkeaseAPI.Data;
using WorkeaseAPI.Helpers;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class AutoFeeService : IAutoFeeService
    {
        private readonly AppDbContext _db;
        private const decimal MONTHLY_CONTRIBUTION = 100.00m;

        public AutoFeeService(AppDbContext db) => _db = db;

        public async Task GenerateFirstFeeAsync(int childId, int enrolledByUserId)
        {
            var child = await _db.Children.FindAsync(childId);
            if (child is null) return;

            // Get enrollment month and year
            var enrollMonth = child.ChildEnrolledDate.Month;
            var enrollYear = child.ChildEnrolledDate.Year;

            // Check if fee already exists for this month
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
                FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                FeeRecordCarryOver = 0.00m,
                FeeRecordTotalAmount = MONTHLY_CONTRIBUTION, // 100 only
                FeeRecordIsPaid = false,
                FeeRecordPaidDate = null,
                FeeRecordDueDate = DateHelper.GetEndOfMonth(enrollMonth, enrollYear),
                FeeRecordIsOverdue = false,
                FeeRecordedByUserId = enrolledByUserId
            };

            _db.FeeRecords.Add(fee);
            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ First fee generated for ChildId {childId}: " +
                              $"{enrollMonth}/{enrollYear} — ₱{fee.FeeRecordTotalAmount}");
        }

        public async Task GenerateMonthlyFeesAsync()
        {
            var now = DateTime.UtcNow;
            var nextMonth = now.Month == 12 ? 1 : now.Month + 1;
            var nextYear = now.Month == 12 ? now.Year + 1 : now.Year;

            // Get all active children
            var children = await _db.Children
                                    .Where(c => c.ChildIsActive)
                                    .ToListAsync();

            foreach (var child in children)
            {
                // Skip if fee for next month already exists
                var exists = await _db.FeeRecords
                                      .AnyAsync(f => f.ChildId == child.ChildId
                                                 && f.FeeRecordMonth == nextMonth
                                                 && f.FeeRecordYear == nextYear);
                if (exists) continue;

                // Get current month's fee to check for carry over
                var currentFee = await _db.FeeRecords
                                          .FirstOrDefaultAsync(f =>
                                              f.ChildId == child.ChildId
                                           && f.FeeRecordMonth == now.Month
                                           && f.FeeRecordYear == now.Year);

                // Calculate carry over from unpaid current month
                decimal carryOver = 0.00m;
                if (currentFee is not null && !currentFee.FeeRecordIsPaid)
                {
                    // Mark current as overdue
                    currentFee.FeeRecordIsOverdue = true;

                    // Carry the full unpaid total to next month
                    carryOver = currentFee.FeeRecordTotalAmount;
                }

                // Create next month's fee
                var nextFee = new FeeRecord
                {
                    ChildId = child.ChildId,
                    FeeRecordMonth = nextMonth,
                    FeeRecordYear = nextYear,
                    FeeRecordMonthlyAmount = MONTHLY_CONTRIBUTION,
                    FeeRecordCarryOver = carryOver,
                    FeeRecordTotalAmount = MONTHLY_CONTRIBUTION + carryOver,
                    FeeRecordIsPaid = false,
                    FeeRecordPaidDate = null,
                    FeeRecordDueDate = DateHelper.GetEndOfMonth(nextMonth, nextYear),
                    FeeRecordIsOverdue = false,
                    FeeRecordedByUserId = 1  // system/admin
                };

                _db.FeeRecords.Add(nextFee);

                Console.WriteLine($"✅ Fee generated for ChildId {child.ChildId}: " +
                                  $"{nextMonth}/{nextYear} — " +
                                  $"₱{MONTHLY_CONTRIBUTION} + " +
                                  $"₱{carryOver} carry over = " +
                                  $"₱{nextFee.FeeRecordTotalAmount}");
            }

            await _db.SaveChangesAsync();
        }

        public async Task ProcessOverdueFeesAsync()
        {
            var now = DateTime.UtcNow;

            // Get all unpaid fees where due date has passed
            var overdueFees = await _db.FeeRecords
                                       .Where(f => !f.FeeRecordIsPaid
                                                && !f.FeeRecordIsOverdue
                                                && f.FeeRecordDueDate < now)
                                       .ToListAsync();

            foreach (var fee in overdueFees)
            {
                fee.FeeRecordIsOverdue = true;
                Console.WriteLine($"⚠️ Fee overdue — ChildId {fee.ChildId}: " +
                                  $"{fee.FeeRecordMonth}/{fee.FeeRecordYear} " +
                                  $"₱{fee.FeeRecordTotalAmount}");
            }

            await _db.SaveChangesAsync();
            Console.WriteLine($"✅ Processed {overdueFees.Count} overdue fees.");
        }
    }
}
