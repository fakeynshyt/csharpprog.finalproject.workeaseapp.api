using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Helpers;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class SyncService : ISyncService
    {
        private readonly AppDbContext _db;
        private readonly IGrowthService _growthService;

        public SyncService(AppDbContext db, IGrowthService growthService)
        {
            _db = db;
            _growthService = growthService;
        }

        public async Task<InitialPackageDto> GetInitialPackageAsync(int userId, string role)
        {
            var user = await _db.Users
                                .Include(u => u.Center)
                                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user is null) throw new Exception("User not found.");

            var package = new InitialPackageDto
            {
                Role = role,
                PackagedAt = DateTime.UtcNow,
                UserProfile = new UserProfileDto
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    UserEmail = user.UserEmail,
                    UserType = user.UserType,
                    CdwCenterId = user.CenterId
                }
            };

            switch (role)
            {
                case "CDW":
                    await BuildCdwPackageAsync(package, user);
                    break;
            }

            return package;
        }

        public async Task<FeeDownloadResultDto> DownloadNewFeesAsync(int cdwUserId)
        {
            var result = new FeeDownloadResultDto { SyncedAt = DateTime.UtcNow };

            // Get CDW's center
            var cdwUser = await _db.Users.FindAsync(cdwUserId);
            if (cdwUser?.CenterId is null)
            {
                result.Message = "CDW has no center assigned.";
                return result;
            }

            var centerId = cdwUser.CenterId.Value;

            // Get all active children in center
            var childIds = await _db.Children
                                    .Where(c => c.CenterId == centerId
                                             && c.ChildIsActive == true)
                                    .Select(c => c.ChildId)
                                    .ToListAsync();

            if (!childIds.Any())
            {
                result.Message = "No active children found in center.";
                return result;
            }

            // ── Calculate school year range ───────────────────────────
            var now = DateTime.UtcNow;
            int schoolYearStart;
            int schoolYearEnd;

            if (now.Month >= 6)
            {
                schoolYearStart = now.Year;
                schoolYearEnd = now.Year + 1;
            }
            else
            {
                schoolYearStart = now.Year - 1;
                schoolYearEnd = now.Year;
            }

            // ── Get ALL fees for school year for these children ───────
            // June–Dec of start year + Jan–May of end year
            var allFees = await _db.FeeRecords
                                   .Include(f => f.Child)
                                   .Include(f => f.RecordedByUser)
                                   .Where(f => childIds.Contains(f.ChildId)
                                            && (
                                                (f.FeeRecordYear == schoolYearStart &&
                                                 f.FeeRecordMonth >= 6)
                                                ||
                                                (f.FeeRecordYear == schoolYearEnd &&
                                                 f.FeeRecordMonth <= 5)
                                               ))
                                   .OrderBy(f => f.ChildId)
                                   .ThenBy(f => f.FeeRecordYear)
                                   .ThenBy(f => f.FeeRecordMonth)
                                   .ToListAsync();

            result.Fees = allFees;
            result.NewFeesCount = allFees.Count;
            result.Message =
                $"Found {allFees.Count} fee record(s) for school year " +
                $"June {schoolYearStart} — May {schoolYearEnd}.";

            System.Console.WriteLine(
                $"[Sync] Fee download for CDW {cdwUserId} — " +
                $"Center {centerId} — " +
                $"Children: {childIds.Count} — " +
                $"Fees: {allFees.Count}");

            return result;
        }

        private async Task BuildCdwPackageAsync(InitialPackageDto package, User cdwUser)
        {
            if (cdwUser.CenterId is null) return;

            var centerId = cdwUser.CenterId.Value;
            var now = DateTime.UtcNow;

            // ── Center info ───────────────────────────────────────────
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

            // ── All active children in their center ───────────────────
            package.Children = await _db.Children
                                        .Where(c => c.CenterId == centerId
                                                 && c.ChildIsActive == true)
                                        .Select(c => new ChildSummaryDto
                                        {
                                            ChildId = c.ChildId,
                                            ChildFullName = c.ChildFirstName + " " + c.ChildLastName,
                                            ChildAddress = c.ChildAddress,
                                            ChildBirthDate = c.ChildBirthDate,
                                            ChildGender = c.ChildGender,
                                            CenterId = c.CenterId,
                                            CenterName = center!.CenterName,
                                            ChildEnrolledDate = c.ChildEnrolledDate,
                                            ChildIsActive = c.ChildIsActive,
                                            ChildUpdatedDate = c.ChildUpdatedDate
                                        })
                                        .ToListAsync();

            var childIds = package.Children.Select(c => c.ChildId).ToList();

            int schoolYearStart;
            int schoolYearEnd;

            if (now.Month >= 6)
            {
                // e.g. now = October 2025
                // school year = June 2025 → May 2026
                schoolYearStart = now.Year;
                schoolYearEnd = now.Year + 1;
            }
            else
            {
                // e.g. now = March 2026
                // school year = June 2025 → May 2026
                schoolYearStart = now.Year - 1;
                schoolYearEnd = now.Year;
            }

            // June 1 of school year start
            var schoolYearStartDate = new DateTime(schoolYearStart, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            // May 31 of school year end
            var schoolYearEndDate = new DateTime(schoolYearEnd, 5, 31, 23, 59, 59, DateTimeKind.Utc);

            System.Diagnostics.Debug.WriteLine(
                $"[InitialDownload] School Year: " +
                $"June {schoolYearStart} → May {schoolYearEnd}");

            System.Diagnostics.Debug.WriteLine(
                $"[InitialDownload] Fetching records from " +
                $"{schoolYearStartDate:MMMM dd, yyyy} to " +
                $"{schoolYearEndDate:MMMM dd, yyyy}");

            // ── Health records — full school year ─────────────────────
            package.HealthRecords = await _db.HealthRecords
                                             .Where(h => childIds.Contains(h.ChildId)
                                                      && h.HealthRecordDate >= schoolYearStartDate
                                                      && h.HealthRecordDate <= schoolYearEndDate)
                                             .OrderByDescending(h => h.HealthRecordDate)
                                             .ToListAsync();

            System.Diagnostics.Debug.WriteLine(
                $"[InitialDownload] Health records fetched: {package.HealthRecords.Count}");

            // ── Attendance records — full school year ─────────────────
            package.AttendanceRecords = await _db.AttendanceRecords
                                                 .Where(a => childIds.Contains(a.ChildId)
                                                          && a.AttendanceRecordDate >= schoolYearStartDate
                                                          && a.AttendanceRecordDate <= schoolYearEndDate)
                                                 .OrderByDescending(a => a.AttendanceRecordDate)
                                                 .ToListAsync();

            System.Diagnostics.Debug.WriteLine(
                $"[InitialDownload] Attendance records fetched: {package.AttendanceRecords.Count}");

            // ── Fee records — both school year months ─────────────────
            // June–December of schoolYearStart + January–May of schoolYearEnd
            package.FeeRecords = await _db.FeeRecords
                                          .Where(f => childIds.Contains(f.ChildId)
                                                   && (
                                                       // June–Dec of start year
                                                       (f.FeeRecordYear == schoolYearStart &&
                                                        f.FeeRecordMonth >= 6)
                                                       ||
                                                       // Jan–May of end year
                                                       (f.FeeRecordYear == schoolYearEnd &&
                                                        f.FeeRecordMonth <= 5)
                                                      ))
                                          .OrderBy(f => f.FeeRecordYear)
                                          .ThenBy(f => f.FeeRecordMonth)
                                          .ToListAsync();

            System.Diagnostics.Debug.WriteLine(
                $"[InitialDownload] Fee records fetched: {package.FeeRecords.Count}");
        }

        public async Task<SyncResultDto> ProcessSyncAsync(SyncPayloadDto payload)
        {
            var result = new SyncResultDto
            {
                SyncedAt = DateTime.UtcNow,
                CdwUserId = payload.CdwUserId
            };

            // ── Health Records ────────────────────────────────────────
            foreach (var item in payload.HealthRecords)
            {
                try
                {
                    switch (item.SyncAction.ToLower())
                    {
                        case "create":
                            await SyncCreateHealthAsync(item, payload.CdwUserId,
                                                        result);
                            break;
                        case "update":
                            await SyncUpdateHealthAsync(item, result);
                            break;
                        case "delete":
                            await SyncDeleteHealthAsync(item, result);
                            break;
                        default:
                            result.FailedCount++;
                            result.Errors.Add(
                                $"Health — Unknown action '{item.SyncAction}' " +
                                $"ChildId {item.ChildId}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add(
                        $"Health ChildId {item.ChildId}: {ex.Message}");
                }
            }

            foreach (var item in payload.AttendanceRecords)
            {
                try
                {
                    switch (item.SyncAction.ToLower())
                    {
                        case "create":
                            await SyncCreateAttendanceAsync(item, payload.CdwUserId,
                                                            result);
                            break;
                        case "update":
                            await SyncUpdateAttendanceAsync(item, result);
                            break;
                        case "delete":
                            await SyncDeleteAttendanceAsync(item, result);
                            break;
                        default:
                            result.FailedCount++;
                            result.Errors.Add(
                                $"Attendance — Unknown action '{item.SyncAction}' " +
                                $"ChildId {item.ChildId}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add(
                        $"Attendance ChildId {item.ChildId}: {ex.Message}");
                }
            }

            foreach (var item in payload.FeeRecords)
            {
                try
                {
                    switch (item.SyncAction.ToLower())
                    {
                        case "create":
                            await SyncCreateFeeAsync(item, payload.CdwUserId,
                                                      result);
                            break;
                        case "update":
                            await SyncUpdateFeeAsync(item, result);
                            break;
                        case "delete":
                            await SyncDeleteFeeAsync(item, result);
                            break;
                        default:
                            result.FailedCount++;
                            result.Errors.Add(
                                $"Fee — Unknown action '{item.SyncAction}' " +
                                $"ChildId {item.ChildId}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add(
                        $"Fee ChildId {item.ChildId} " +
                        $"{item.FeeRecordMonth}/{item.FeeRecordYear}: {ex.Message}");
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


        private async Task SyncCreateHealthAsync(SyncHealthDto item,
                                          int cdwUserId,
                                          SyncResultDto result)
        {
            var exists = await _db.HealthRecords
                                  .AnyAsync(h => h.ChildId == item.ChildId
                                             && h.HealthRecordDate.Date == item.HealthRecordDate.Date);
            if (exists)
            {
                result.SyncedCount++;
                return;
            }

            var record = new HealthRecord
            {
                ChildId = item.ChildId,
                HealthRecordDate = item.HealthRecordDate,
                HealthRecordWeightKg = item.HealthRecordWeigtKg,
                HealthRecordHeightCm = item.HealthRecordHeightCm,
                HealthRecordNotes = item.HealthRecordNotes,
                HealthRecordedByUserId = cdwUserId,
                HealthRecordIsSync = true,
                HealthRecordCreatedAt = DateTime.UtcNow,
                HealthRecordUpdatedAt = item.UpdatedAt
            };

            _db.HealthRecords.Add(record);
            await _db.SaveChangesAsync();

            // ✅ Use item.LocalId
            result.HealthIdMaps.Add(new SyncIdMapDto
            {
                LocalId = item.LocalId,
                ServerId = record.HealthRecordId
            });

            result.SyncedCount++;
        }

        private async Task SyncUpdateHealthAsync(SyncHealthDto item,
                                                  SyncResultDto result)
        {
            if (item.ServerHealthRecordId is null)
            {
                result.FailedCount++;
                result.Errors.Add(
                    $"Health Update — missing ServerHealthRecordId " +
                    $"for ChildId {item.ChildId}");
                return;
            }

            var record = await _db.HealthRecords
                                  .FirstOrDefaultAsync(h =>
                                      h.HealthRecordId == item.ServerHealthRecordId);

            if (record is null)
            {
                result.FailedCount++;
                result.Errors.Add(
                    $"Health Update — HealthRecordId " +
                    $"{item.ServerHealthRecordId} not found");
                return;
            }

            // ✅ Only update if MAUI version is newer
            if (item.UpdatedAt > record.HealthRecordUpdatedAt)
            {
                record.HealthRecordDate = item.HealthRecordDate;
                record.HealthRecordWeightKg = item.HealthRecordWeigtKg;
                record.HealthRecordHeightCm = item.HealthRecordHeightCm;
                record.HealthRecordNotes = item.HealthRecordNotes;
                record.HealthRecordUpdatedAt = item.UpdatedAt;
            }

            result.SyncedCount++;
        }

        private async Task SyncDeleteHealthAsync(SyncHealthDto item,
                                                  SyncResultDto result)
        {
            if (item.ServerHealthRecordId is null)
            {
                // Never reached server — nothing to do
                result.SyncedCount++;
                return;
            }

            var record = await _db.HealthRecords
                                  .FirstOrDefaultAsync(h =>
                                      h.HealthRecordId == item.ServerHealthRecordId);

            if (record is null)
            {
                // Already deleted — skip silently
                result.SyncedCount++;
                return;
            }

            _db.HealthRecords.Remove(record);
            result.SyncedCount++;
        }

        // ── ATTENDANCE SYNC HANDLERS ──────────────────────────────────

        // API Services/SyncService.cs

        private async Task SyncCreateAttendanceAsync(SyncAttendanceDto item,
                                                      int cdwUserId,
                                                      SyncResultDto result)
        {
            // ✅ Check if same child + same date already exists on server
            var existing = await _db.AttendanceRecords
                                    .FirstOrDefaultAsync(a =>
                                        a.ChildId == item.ChildId
                                     && a.AttendanceRecordDate.Year == item.AttendanceDate.Year
                                     && a.AttendanceRecordDate.Month == item.AttendanceDate.Month
                                     && a.AttendanceRecordDate.Day == item.AttendanceDate.Day);

            if (existing is not null)
            {
                // ✅ Already exists — UPDATE instead of INSERT
                // Only update if MAUI version is newer
                if (item.UpdatedAt > existing.AttendanceRecordUpdatedAt)
                {
                    existing.AttendanceRecordIsPresent = item.IsPresent;
                    existing.AttendanceRecordUpdatedAt = item.UpdatedAt;

                    System.Diagnostics.Debug.WriteLine(
                        $"[SYNC] Attendance already exists for ChildId={item.ChildId} " +
                        $"on {item.AttendanceDate:yyyy-MM-dd} — updating instead");
                }

                // ✅ Return the existing server ID so MAUI can update local record
                result.AttendanceIdMaps.Add(new SyncIdMapDto
                {
                    LocalId = item.LocalId,
                    ServerId = existing.AttendanceRecordId
                });

                result.SyncedCount++;
                return;
            }

            // ✅ Truly new — insert
            var record = new AttendanceRecord
            {
                ChildId = item.ChildId,
                AttendanceRecordDate = item.AttendanceDate,
                AttendanceRecordIsPresent = item.IsPresent,
                AttendanceRecordedByUserId = cdwUserId,
                AttendanceRecordIsSync = true,
                AttendanceRecordCreatedAt = DateTime.UtcNow,
                AttendanceRecordUpdatedAt = item.UpdatedAt
            };

            _db.AttendanceRecords.Add(record);
            await _db.SaveChangesAsync();

            result.AttendanceIdMaps.Add(new SyncIdMapDto
            {
                LocalId = item.LocalId,
                ServerId = record.AttendanceRecordId
            });

            result.SyncedCount++;
        }

        private async Task SyncUpdateAttendanceAsync(SyncAttendanceDto item,
                                              SyncResultDto result)
        {
            AttendanceRecord? record = null;

            // ✅ Try by ServerAttendanceId first
            if (item.ServerAttendanceId.HasValue)
            {
                record = await _db.AttendanceRecords
                                  .FirstOrDefaultAsync(a =>
                                      a.AttendanceRecordId == item.ServerAttendanceId.Value);
            }

            // ✅ Fallback — find by childId + date (handles cross-year anomaly)
            if (record is null)
            {
                record = await _db.AttendanceRecords
                                  .FirstOrDefaultAsync(a =>
                                      a.ChildId == item.ChildId
                                   && a.AttendanceRecordDate.Year == item.AttendanceDate.Year
                                   && a.AttendanceRecordDate.Month == item.AttendanceDate.Month
                                   && a.AttendanceRecordDate.Day == item.AttendanceDate.Day);
            }

            if (record is null)
            {
                // ✅ Record doesn't exist on server at all — create it instead
                System.Diagnostics.Debug.WriteLine(
                    $"[SYNC] Attendance update — record not found on server " +
                    $"ChildId={item.ChildId} {item.AttendanceDate:yyyy-MM-dd} " +
                    $"— creating instead");

                var newRecord = new AttendanceRecord
                {
                    ChildId = item.ChildId,
                    AttendanceRecordDate = item.AttendanceDate,
                    AttendanceRecordIsPresent = item.IsPresent,
                    AttendanceRecordedByUserId = result.CdwUserId,
                    AttendanceRecordIsSync = true,
                    AttendanceRecordCreatedAt = DateTime.UtcNow,
                    AttendanceRecordUpdatedAt = item.UpdatedAt
                };

                _db.AttendanceRecords.Add(newRecord);
                await _db.SaveChangesAsync();

                // Return new server ID so MAUI updates local record
                result.AttendanceIdMaps.Add(new SyncIdMapDto
                {
                    LocalId = item.LocalId,
                    ServerId = newRecord.AttendanceRecordId
                });

                result.SyncedCount++;
                return;
            }

            // ✅ Found — update only if MAUI is newer
            if (item.UpdatedAt > record.AttendanceRecordUpdatedAt)
            {
                record.AttendanceRecordIsPresent = item.IsPresent;
                record.AttendanceRecordDate = item.AttendanceDate;
                record.AttendanceRecordUpdatedAt = item.UpdatedAt;

                System.Diagnostics.Debug.WriteLine(
                    $"[SYNC] Attendance updated — ChildId={item.ChildId} " +
                    $"AttendanceId={record.AttendanceRecordId} " +
                    $"Date={item.AttendanceDate:yyyy-MM-dd}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SYNC] Attendance skipped — server has newer version " +
                    $"ChildId={item.ChildId} Date={item.AttendanceDate:yyyy-MM-dd}");
            }

            // ✅ Always return server ID so MAUI can correct its local ServerAttendanceId
            result.AttendanceIdMaps.Add(new SyncIdMapDto
            {
                LocalId = item.LocalId,
                ServerId = record.AttendanceRecordId
            });

            result.SyncedCount++;
        }

        private async Task SyncDeleteAttendanceAsync(SyncAttendanceDto item,
                                                      SyncResultDto result)
        {
            if (item.ServerAttendanceId is null)
            {
                result.SyncedCount++;
                return;
            }

            var record = await _db.AttendanceRecords
                                  .FirstOrDefaultAsync(a =>
                                      a.AttendanceRecordId == item.ServerAttendanceId);

            if (record is null)
            {
                result.SyncedCount++;
                return;
            }

            _db.AttendanceRecords.Remove(record);
            result.SyncedCount++;
        }

        private async Task SyncCreateFeeAsync(SyncFeeDto item,
                                           int cdwUserId,
                                           SyncResultDto result)
        {
            var existing = await _db.FeeRecords
                                    .FirstOrDefaultAsync(f =>
                                        f.ChildId == item.ChildId
                                     && f.FeeRecordMonth == item.FeeRecordMonth
                                     && f.FeeRecordYear == item.FeeRecordYear);

            if (existing is not null)
            {
                // Already exists — if CDW marked paid update it
                if (item.FeeRecordIsPaid && !existing.FeeRecordIsPaid)
                {
                    existing.FeeRecordIsPaid = true;
                    existing.FeeRecordPaidDate = item.FeeRecordPaidDate ?? DateTime.UtcNow;
                    existing.FeeRecordUpdatedAt = item.UpdatedAt;

                    if (existing.FeeRecordCarryOver > 0)
                        await MarkPreviousPaidAsync(
                            item.ChildId,
                            item.FeeRecordMonth,
                            item.FeeRecordYear);

                    await _db.SaveChangesAsync();

                    // ✅ Recalculate growth points
                    await _growthService.RecalculatePointsAsync(item.ChildId);

                    System.Console.WriteLine(
                        $"[Sync] Fee paid — ChildId={item.ChildId} " +
                        $"{item.FeeRecordMonth}/{item.FeeRecordYear} — " +
                        $"Growth points recalculated");
                }

                result.FeeIdMaps.Add(new SyncIdMapDto
                {
                    LocalId = item.LocalId,
                    ServerId = existing.FeeRecordId
                });

                result.SyncedCount++;
                return;
            }

            // New fee record
            var fee = new FeeRecord
            {
                ChildId = item.ChildId,
                FeeRecordMonth = item.FeeRecordMonth,
                FeeRecordYear = item.FeeRecordYear,
                FeeRecordMonthlyAmount = 100.00m,
                FeeRecordCarryOver = 0.00m,
                FeeRecordTotalAmount = 100.00m,
                FeeRecordIsPaid = item.FeeRecordIsPaid,
                FeeRecordPaidDate = item.FeeRecordPaidDate,
                FeeRecordDueDate = DateHelper.GetEndOfMonth(
                                             item.FeeRecordMonth, item.FeeRecordYear),
                FeeRecordIsOverdue = false,
                FeeRecordedByUserId = cdwUserId,
                FeeRecordReceiptNo = ReceiptGenerator.GenerateUnique(
                                             item.FeeRecordMonth, item.FeeRecordYear),
                FeeRecordCreatedAt = DateTime.UtcNow,
                FeeRecordUpdatedAt = item.UpdatedAt
            };

            _db.FeeRecords.Add(fee);
            await _db.SaveChangesAsync();

            // ✅ If created as paid, recalculate growth
            if (fee.FeeRecordIsPaid)
            {
                await _growthService.RecalculatePointsAsync(item.ChildId);

                System.Console.WriteLine(
                    $"[Sync] New paid fee — ChildId={item.ChildId} " +
                    $"{item.FeeRecordMonth}/{item.FeeRecordYear} — " +
                    $"Growth points recalculated");
            }

            result.FeeIdMaps.Add(new SyncIdMapDto
            {
                LocalId = item.LocalId,
                ServerId = fee.FeeRecordId
            });

            result.SyncedCount++;
        }

        // ── FEE UPDATE HANDLER ────────────────────────────────────────
        private async Task SyncUpdateFeeAsync(SyncFeeDto item,
                                               SyncResultDto result)
        {
            if (item.ServerFeeRecordId is null)
            {
                result.FailedCount++;
                result.Errors.Add(
                    $"Fee Update — missing ServerFeeRecordId " +
                    $"ChildId {item.ChildId}");
                return;
            }

            var fee = await _db.FeeRecords
                               .FirstOrDefaultAsync(f =>
                                   f.FeeRecordId == item.ServerFeeRecordId);

            if (fee is null)
            {
                result.FailedCount++;
                result.Errors.Add(
                    $"Fee Update — FeeRecordId " +
                    $"{item.ServerFeeRecordId} not found");
                return;
            }

            bool wasAlreadyPaid = fee.FeeRecordIsPaid;

            // ✅ Only update if MAUI version is newer
            if (item.UpdatedAt > fee.FeeRecordUpdatedAt)
            {
                if (item.FeeRecordIsPaid && !fee.FeeRecordIsPaid)
                {
                    fee.FeeRecordIsPaid = true;
                    fee.FeeRecordPaidDate = item.FeeRecordPaidDate ?? DateTime.UtcNow;
                    fee.FeeRecordUpdatedAt = item.UpdatedAt;

                    // Cascade previous unpaid to paid
                    if (fee.FeeRecordCarryOver > 0)
                        await MarkPreviousPaidAsync(
                            item.ChildId,
                            item.FeeRecordMonth,
                            item.FeeRecordYear);

                    await _db.SaveChangesAsync();

                    // ✅ Fee just got paid — recalculate growth points
                    await _growthService.RecalculatePointsAsync(item.ChildId);

                    System.Console.WriteLine(
                        $"[Sync] Fee updated to paid — ChildId={item.ChildId} " +
                        $"FeeId={fee.FeeRecordId} " +
                        $"{item.FeeRecordMonth}/{item.FeeRecordYear} — " +
                        $"Growth points recalculated");
                }
            }

            result.AttendanceIdMaps.Add(new SyncIdMapDto
            {
                LocalId = item.LocalId,
                ServerId = fee.FeeRecordId
            });

            result.SyncedCount++;
        }

        // ── MARK PREVIOUS PAID CASCADE ────────────────────────────────
        private async Task MarkPreviousPaidAsync(int childId,
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
                prev.FeeRecordUpdatedAt = DateTime.UtcNow;
            }

            // ✅ Save cascade changes
            if (previousUnpaid.Any())
            {
                await _db.SaveChangesAsync();

                System.Console.WriteLine(
                    $"[Sync] Cascade paid {previousUnpaid.Count} previous " +
                    $"fees for ChildId={childId}");
            }
        }

        private async Task SyncDeleteFeeAsync(SyncFeeDto item,
                                               SyncResultDto result)
        {
            if (item.ServerFeeRecordId is null)
            {
                result.SyncedCount++;
                return;
            }

            var fee = await _db.FeeRecords
                               .FirstOrDefaultAsync(f =>
                                   f.FeeRecordId == item.ServerFeeRecordId);

            if (fee is null)
            {
                result.SyncedCount++;
                return;
            }

            _db.FeeRecords.Remove(fee);
            result.SyncedCount++;
        }
    }
}
