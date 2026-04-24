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

        public ChildService(AppDbContext db) => _db = db;
        public async Task<Child> CreateChildAsync(Child child)
        {
            child.ChildEnrolledDate = DateTime.Now;
            child.ChildUpdatedDate = DateTime.Now;

            _db.Children.Add(child);

            await _db.SaveChangesAsync();
            return child;
        }

        public async Task<bool> DeleteChildAsync(int childId)
        {
            var child = await GetChildByIdAsync(childId);

            if (child is null) return false;

            child.ChildIsActive = false;
            child.ChildUpdatedDate = DateTime.Now;


            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<ChildReadDto>> GetAllChildByCdwUserAsync(int cdwUserId)
        {
            var cdwUser = await _db.Users.FindAsync(cdwUserId);
            if (cdwUser?.CenterId is null) return Enumerable.Empty<ChildReadDto>();

            return await _db.Children
                .Include(c => c.Center)
                .Where(c => c.CenterId == cdwUserId && c.ChildIsActive)
                .OrderBy(c => c.ChildLastName)
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

        public async Task<IEnumerable<ChildReadDto>> GetAllChildReadAsync() =>
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
                    CdwCenterName = c.Center!.CenterName

                })
                .ToListAsync();


        public async Task<Child?> GetChildByIdAsync(int childId) =>
            await _db.Children
                .Include(c => c.Center)
                .Include(c => c.Guardian)
                .FirstOrDefaultAsync(c => c.ChildId == childId && c.ChildIsActive);
       

        public async Task<GuardianChildDto> GetGuardianChildByIdAsync(int guardianUserId)
        {
            var child = await _db.Children
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.UserId == guardianUserId && c.ChildIsActive);

            if (child is null) return null;

            var healthHistory = await _db.HealthRecords
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

            return new GuardianChildDto
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

        public async Task<bool> LinkParentAsync(int childId, int guardianUserId)
        {
            var child = await _db.Children.FindAsync(childId);
            var guardian = await _db.Users.FindAsync(guardianUserId);

            if (child is null || guardian is null || guardian.UserType != "Parent")
                return false;

            child.UserId = guardianUserId;
            child.ChildUpdatedDate = DateTime.Now;

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateChildAsync(int childId, Child updatedChild)
        {
            var child = await GetChildByIdAsync(childId);

            if (child == null) return false;

            child.ChildFirstName = updatedChild.ChildFirstName;
            child.ChildLastName = updatedChild.ChildLastName;
            child.ChildBirthDate = updatedChild.ChildBirthDate;
            child.ChildGender = updatedChild.ChildGender;
            child.CenterId = updatedChild.CenterId;
            child.ChildUpdatedDate = DateTime.Now;

            await _db.SaveChangesAsync();

            return true;
        }
    }
}
