using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class ChildService : IChildService
    {
        private readonly AppDbContext _db;
        private readonly IAutoFeeService _autoFeeService;

        public ChildService(AppDbContext db, IAutoFeeService autoFeeService)
        {
            _db = db;
            _autoFeeService = autoFeeService;
        }

        public async Task<IEnumerable<ChildReadDto>> GetAllChildAsync() =>
            await _db.Children
                     .Include(c => c.Center)
                     .Where(c => c.ChildIsActive)
                     .OrderBy(c => c.ChildLastName)
                     .Select(c => new ChildReadDto
                     {
                         ChildId = c.ChildId,
                         ChildFullName = c.ChildFirstName + " " + c.ChildLastName,
                         ChildBirthDate = c.ChildBirthDate,
                         ChildGender = c.ChildGender,
                         CenterName = c.Center!.CenterName
                     })
                     .ToListAsync();

        public async Task<IEnumerable<ChildReadDto>> GetChildByCdwUserAsync(int cdwUserId)
        {
            var cdwUser = await _db.Users.FindAsync(cdwUserId);
            if (cdwUser?.CenterId is null)
                return Enumerable.Empty<ChildReadDto>();

            return await _db.Children
                            .Include(c => c.Center)
                            .Where(c => c.CenterId == cdwUser.CenterId
                                     && c.ChildIsActive == true)
                            .OrderBy(c => c.ChildLastName)
                            .Select(c => new ChildReadDto
                            {
                                ChildId = c.ChildId,
                                ChildFullName = c.ChildFirstName + " " + c.ChildLastName,
                                ChildBirthDate = c.ChildBirthDate,
                                ChildGender = c.ChildGender,
                                CenterName = c.Center!.CenterName
                            })
                            .ToListAsync();
        }

        public async Task<Child?> GetChildByIdAsync(int id) =>
            await _db.Children
                     .Include(c => c.Center)
                     .Include(c => c.Guardian)
                     .FirstOrDefaultAsync(c => c.ChildId == id && c.ChildIsActive);

        public async Task<GuardianChildDto?> GetChildByGuardianUserIdAsync(int parentUserId)
        {
            var child = await _db.Children
                                 .Include(c => c.Center)
                                 .FirstOrDefaultAsync(c => c.GuardianId == parentUserId
                                                        && c.ChildIsActive == true);
            if (child is null) return null;

            var healthHistory = await _db.HealthRecords
                                         .Where(h => h.ChildId == child.ChildId)
                                         .OrderByDescending(h => h.HealthRecordDate)
                                         .Select(h => new HealthSummaryDto
                                         {
                                             HealthRecordDate = h.HealthRecordDate,
                                             HealthWeightKg = h.HealthRecordWeigtKg,
                                             HealthHeightCm = h.HealthRecordHeightCm,
                                             HealthBmi = h.HealthRecordHeightCm > 0
                                                 ? h.HealthRecordWeigtKg /
                                                   ((h.HealthRecordHeightCm / 100) *
                                                    (h.HealthRecordHeightCm / 100))
                                                 : 0,
                                             IsPresent = h.HealthRecordIsPresent,
                                             HealthNotes = h.HealthRecordNotes
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
                                          FeeMonthlyAmount = f.FeeRecordMonthlyAmount,
                                          IsPaid = f.FeeRecordIsPaid,
                                          FeePaidDate = f.FeeRecordPaidDate
                                      })
                                      .ToListAsync();

            return new GuardianChildDto
            {
                ChildId = child.ChildId,
                ChildFullName = child.ChildFirstName + " " + child.ChildLastName,
                ChildBirthDate = child.ChildBirthDate,
                ChildGender = child.ChildGender,
                CenterName = child.Center?.CenterName ?? string.Empty,
                ChildHealthHistory = healthHistory,
                ChildFeeHistory = feeHistory
            };
        }

        // Services/ChildService.cs

        public async Task<Child> CreateChildWithGuardianAsync(CreateChildDto dto, int createdByUserId)
        {
            // Validate center
            var center = await _db.Centers.FindAsync(dto.CenterId);
            if (center is null)
                throw new Exception($"Center with ID {dto.CenterId} not found.");

            // Validate parent if provided
            if (dto.UserId.HasValue)
            {
                var parent = await _db.Users.FindAsync(dto.UserId.Value);
                if (parent is null)
                    throw new Exception($"User with ID {dto.UserId} not found.");

                if (parent.UserType != "Parent")
                    throw new Exception($"{parent.UserName} is not a Parent.");

                var alreadyLinked = await _db.Children
                                             .AnyAsync(c => c.GuardianId == dto.UserId
                                                         && c.ChildIsActive == true);
                if (alreadyLinked)
                    throw new Exception("This parent is already linked to another child.");
            }

            var child = new Child
            {
                ChildFirstName = dto.ChildFirstName,
                ChildLastName = dto.ChildLastName,
                ChildBirthDate = dto.ChildBirthDate,
                ChildGender = dto.ChildGender,
                CenterId = dto.CenterId,
                GuardianId = dto.UserId,
                ChildIsActive = true,
                ChildEnrolledDate = DateTime.UtcNow,  // ✅ enrollment date
                ChildUpdatedDate = DateTime.UtcNow
            };

            _db.Children.Add(child);
            await _db.SaveChangesAsync();

            // ✅ Auto generate first fee right after child is created
            await _autoFeeService.GenerateFirstFeeAsync(child.ChildId, createdByUserId);

            return await GetChildByIdAsync(child.ChildId) ?? child;
        }

        public async Task<bool> UpdateChildAsync(int id, UpdateChildDto dto)
        {
            var child = await GetChildByIdAsync(id);
            if (child is null) return false;

            // Validate center exists
            var center = await _db.Centers.FindAsync(dto.CenterId);
            if (center is null)
                throw new Exception($"Center with ID {dto.CenterId} not found.");

            child.ChildFirstName = dto.ChildFirstName;
            child.ChildLastName = dto.ChildLastName;
            child.ChildBirthDate = dto.ChildBirthDate;
            child.ChildGender = dto.ChildGender;
            child.CenterId = dto.CenterId;
            child.ChildUpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LinkParentAsync(int childId, int parentUserId)
        {
            var child = await _db.Children.FindAsync(childId);
            var parent = await _db.Users.FindAsync(parentUserId);

            if (child is null || parent is null || parent.UserType != "Parent")
                return false;

            child.GuardianId = parentUserId;
            child.ChildUpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteChildAsync(int id)
        {
            var child = await GetChildByIdAsync(id);
            if (child is null) return false;

            child.ChildIsActive = false;
            child.ChildUpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
