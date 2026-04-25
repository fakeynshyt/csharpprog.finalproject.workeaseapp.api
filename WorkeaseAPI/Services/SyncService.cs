using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class SyncService : ISyncService
    {
        private readonly AppDbContext _db;
        public SyncService(AppDbContext db) => _db = db;
        public async Task<InitialPackageDto> GetInitialPackageAsync(int userId, string role)
        {
            var user = await _db.Users
                .Include(u => u.Center)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user is null) throw new Exception("User not found");

            var package = new InitialPackageDto
            {
                Role = role,
                PackagedAt = DateTime.UtcNow,
                UserProfile = new UserProfileDto
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    UserEmail = user.UserEmail,
                    UserRole = user.UserType,
                    CdwCenterId = user.CenterId
                }
            };

            switch (role)
            {
                case "Admin":
                    await BuildAdminPackageAsync(package);
                    break;
                case "Worker":
                    await BuildCdwUserPackageAsync(package, user);
                    break;
                case "Child or Parent":
                    await BuildGuardianPackageAsync(package, user.UserId);
                    break;
            }

            return package;
        }

        public async Task<SyncResultDto> ProcessSyncAsync(SyncPayloadDto payload)
        {
            var result = new SyncResultDto() { SyncedAt = DateTime.UtcNow };

            foreach (var record in payload.HealthRecords)
            {
                try
                {
                    var exists = await _db.HealthRecords
                        .AnyAsync(h => h.ChildId == record.ChildId && h.HealthRecordDate == record.HealthRecordDate);

                    if (!exists)
                    {
                        record.HealthRecordedByUserId = payload.CdwUserId;
                        record.HealthRecordIsSync = true;
                        _db.HealthRecords.Add(record);
                        result.SyncedCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Health ChildId {record.ChildId}: {ex.Message}");
                }
            }

            foreach (var fee in payload.FeeRecords)
            {
                try
                {
                    var existing = await _db.FeeRecords
                        .FirstOrDefaultAsync(f => f.ChildId == fee.ChildId && f.FeeRecordYear == fee.FeeRecordYear && f.FeeRecordMonth == fee.FeeRecordMonth);

                    if (existing is null)
                    {
                        fee.FeeRecordedByUserId = payload.CdwUserId;
                        _db.FeeRecords.Add(fee);
                        result.SyncedCount++;
                    }
                    else if (fee.FeeRecordIsPaid && !existing.FeeRecordIsPaid)
                    {
                        existing.FeeRecordIsPaid = true;
                        existing.FeeRecordPaidDate = fee.FeeRecordPaidDate ?? DateTime.UtcNow;
                        result.SyncedCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Fee ChildId {fee.ChildId} {fee.FeeRecordMonth}/{fee.FeeRecordYear}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();

            _db.SyncLogs.Add(new SyncLog
            {
                SyncLogUserId = payload.CdwUserId,
                SyncLoggedAt = DateTime.UtcNow,
                SyncLogRecordHealthRecordsSynced = payload.HealthRecords.Count,
                SyncLogRecordFeeRecordsSynced = payload.FeeRecords.Count,
                SyncLogFailedSyncedCounts = result.FailedCount
            });

            await _db.SaveChangesAsync();

            return result;
        }

        private async Task BuildCdwUserPackageAsync(InitialPackageDto package, User user)
        {
            if (user.CenterId is null) return;

            var centerId = user.CenterId.Value;

            var center = await _db.Centers.FindAsync(centerId);
            if (center is not null)
            {
                package.AssignedCenter = new CdwCenterDto
                {
                    CdwCenterId = center.CenterId,
                    CdwCenterName = center.CenterName,
                    CdwCenterLocation = center.CenterLocation
                };
            }

            package.Children = await _db.Children
                .Where(c => c.CenterId == centerId && c.ChildIsActive)
                .Select(c => new ChildReadDto
                {
                    ChildId = c.ChildId,
                    ChildFullName = c.ChildFirstName + " " + c.ChildLastName,
                    ChildBirthDate = c.ChildBirthDate,
                    ChildGender = c.ChildGender,
                    CdwCenterName = c.Center!.CenterName
                })
                .ToListAsync();

            var childIds = package.Children.Select(c => c.ChildId).ToList();

            var healthCutoff = DateTime.UtcNow.AddMonths(-6);

            package.HealthRecords = await _db.HealthRecords
                .Where(h => childIds.Contains(h.ChildId) && h.HealthRecordDate >= healthCutoff)
                .OrderByDescending(f => f.HealthRecordDate)
                .ToListAsync();

            var currentYear = DateTime.UtcNow.Year;
            package.FeeRecords = await _db.FeeRecords
                .Where(f => childIds.Contains(f.ChildId) && f.FeeRecordYear == currentYear)
                .ToListAsync();
        }

        private async Task BuildGuardianPackageAsync(InitialPackageDto package, int guardianId)
        {
            var child = await _db.Children
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.GuardianId == guardianId && c.ChildIsActive);

            if (child is null) return;

            var healthHistory = await _db.HealthRecords
                .Where(h => h.ChildId == child.ChildId)
                .OrderByDescending(h => h.HealthRecordDate)
                .Select(c => new HealthSummaryDto
                {
                    HealthRecordDate = c.HealthRecordDate,
                    HealthWeightKg = c.HealthRecordWeigtKg,
                    HealthHeightCm = c.HealthRecordHeightCm,
                    HealthBmi = c.HealthRecordBmi,
                    IsPresent = c.HealthRecordIsPresent,
                    HealthNotes = c.HealthRecordNotes
                })
                .ToListAsync();

            var feeHistory = await _db.FeeRecords
                .Where(f => f.ChildId == child.ChildId)
                .OrderByDescending(f => f.FeeRecordYear)
                .ThenByDescending(f => f.FeeRecordMonth)
                .Select(f => new FeeSummaryDto
                {
                    FeeMonth = f.FeeRecordMonth,
                    FeeYear = f.FeeRecordYear,
                    FeeAmount = f.FeeRecordAmount,
                    IsPaid = f.FeeRecordIsPaid,
                    FeePaidDate = f.FeeRecordPaidDate
                })
                .ToListAsync();

            package.MyChild = new GuardianChildDto
            {
                GuardianId = child.ChildId,
                GuardianFullName = child.ChildFirstName + " " + child.ChildLastName,
                GuardianBirthDate = child.ChildBirthDate,
                GuardianGender = child.ChildGender,
                CdwCenterName = child.Center?.CenterName ?? string.Empty,
                ChildHealthHistory = healthHistory,
                ChildFeeHistory = feeHistory
            };

        }

        private async Task BuildAdminPackageAsync(InitialPackageDto package)
        {
            package.Children = await _db.Children
                .Include(c => c.Center)
                .Where(c => c.ChildIsActive)
                .Select(c => new ChildReadDto
                {
                    ChildId = c.ChildId,
                    ChildFullName = c.ChildFirstName + " " + c.ChildLastName,
                    ChildBirthDate = c.ChildBirthDate,
                    ChildGender = c.ChildGender,
                    CdwCenterName = c.Center!.CenterName
                })
                .ToListAsync();
        }
    }
}
