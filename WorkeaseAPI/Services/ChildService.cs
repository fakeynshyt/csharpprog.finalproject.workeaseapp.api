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

        public async Task<IEnumerable<ChildSummaryDto>> GetAllChildAsync() =>
            await _db.Children
                     .Include(c => c.Center)
                     .Where(c => c.ChildIsActive)
                     .OrderBy(c => c.ChildLastName)
                     .Select(c => new ChildSummaryDto
                     {
                         ChildId = c.ChildId,
                         ChildFullName = c.ChildFirstName + " " + c.ChildLastName,
                         ChildAddress = c.ChildAddress,
                         ChildBirthDate = c.ChildBirthDate,
                         ChildGender = c.ChildGender,
                         CenterId = c.CenterId,
                         CenterName = c.Center!.CenterName,
                         ChildEnrolledDate = c.ChildEnrolledDate,
                         ChildIsActive = c.ChildIsActive,
                         ChildUpdatedDate = c.ChildUpdatedDate,
                         UserId = c.GuardianId // Populated field
                     })
                     .ToListAsync();

        public async Task<IEnumerable<ChildSummaryDto>> GetChildrenByCenterAsync(int centerId)
        {
            var center = await _db.Centers.FindAsync(centerId);
            if (center is null)
                return Enumerable.Empty<ChildSummaryDto>();

            return await _db.Children
                            .Include(c => c.Center)
                            .Where(c => c.CenterId == centerId
                                     && c.ChildIsActive == true)
                            .OrderBy(c => c.ChildLastName)
                            .Select(c => new ChildSummaryDto
                            {
                                ChildId = c.ChildId,
                                ChildFullName = c.ChildFirstName + " " + c.ChildLastName,
                                ChildAddress = c.ChildAddress,
                                ChildBirthDate = c.ChildBirthDate,
                                ChildGender = c.ChildGender,
                                CenterId = c.CenterId,
                                CenterName = c.Center!.CenterName,
                                ChildEnrolledDate = c.ChildEnrolledDate,
                                ChildIsActive = c.ChildIsActive,
                                ChildUpdatedDate = c.ChildUpdatedDate,
                                UserId = c.GuardianId // Populated field
                            })
                            .ToListAsync();
        }

        public async Task<IEnumerable<GuardianChildDto>> GetChildByGuardianUserIdAsync(int parentUserId)
        {
            var children = await _db.Children
                            .Include(c => c.Center)
                            .Where(c => c.GuardianId == parentUserId
                                     && c.ChildIsActive == true)
                            .OrderBy(c => c.ChildLastName)
                            .ToListAsync();

            return children.Select(c => new GuardianChildDto
            {
                ChildId = c.ChildId,
                ChildFullName = c.ChildFirstName + " " + c.ChildLastName,
                ChildAddress = c.ChildAddress,
                ChildBirthDate = c.ChildBirthDate,
                ChildGender = c.ChildGender,
                CenterId = c.CenterId,
                CenterName = c.Center?.CenterName ?? string.Empty,
                ChildEnrolledDate = c.ChildEnrolledDate,
                ChildIsActive = c.ChildIsActive,
                ChildUpdatedDate = c.ChildUpdatedDate
            });
        }

        public async Task<int?> GetCenterIdByUserAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            return user?.CenterId;
        }

        public async Task<ChildSummaryDto?> GetChildByIdAsync(int id)
        {
            var child = await _db.Children
                                 .Include(c => c.Center)
                                 .FirstOrDefaultAsync(c => c.ChildId == id
                                                        && c.ChildIsActive == true);
            if (child is null) return null;

            return new ChildSummaryDto
            {
                ChildId = child.ChildId,
                ChildFullName = child.ChildFirstName + " " + child.ChildLastName,
                ChildAddress = child.ChildAddress,
                ChildBirthDate = child.ChildBirthDate,
                ChildGender = child.ChildGender,
                CenterId = child.CenterId,
                CenterName = child.Center?.CenterName ?? string.Empty,
                ChildEnrolledDate = child.ChildEnrolledDate,
                ChildIsActive = child.ChildIsActive,
                ChildUpdatedDate = child.ChildUpdatedDate,
                UserId = child.GuardianId // Populated field
            };
        }

        public async Task<Child> CreateChildWithGuardianAsync(CreateChildDto dto, int createdByUserId)
        {
            var center = await _db.Centers.FindAsync(dto.CenterId);
            if (center is null)
                throw new Exception($"Center with ID {dto.CenterId} not found.");

            if (dto.UserId.HasValue)
            {
                var parent = await _db.Users.FindAsync(dto.UserId.Value);
                if (parent is null)
                    throw new Exception($"User with ID {dto.UserId} not found.");

                if (parent.UserType != "Parent")
                    throw new Exception($"{parent.UserName} is not a Parent.");
            }

            var child = new Child
            {
                ChildFirstName = dto.ChildFirstName,
                ChildLastName = dto.ChildLastName,
                ChildBirthDate = dto.ChildBirthDate,
                ChildGender = dto.ChildGender,
                ChildAddress = dto.ChildAddress,
                CenterId = dto.CenterId,
                GuardianId = dto.UserId,
                ChildIsActive = true,
                ChildEnrolledDate = DateTime.UtcNow,
                ChildUpdatedDate = DateTime.UtcNow
            };

            _db.Children.Add(child);
            await _db.SaveChangesAsync();

            await _autoFeeService.GenerateFirstFeeAsync(child.ChildId, createdByUserId);

            return await FindChildAsync(child.ChildId) ?? child;
        }

        public async Task<bool> UpdateChildAsync(int id, UpdateChildDto dto)
        {
            var child = await FindChildAsync(id);
            if (child is null) return false;

            var center = await _db.Centers.FindAsync(dto.CenterId);
            if (center is null)
                throw new Exception($"Center with ID {dto.CenterId} not found.");

            // Verification check for the parent user account
            var parent = await _db.Users.FindAsync(dto.UserId);
            if (parent is null)
                throw new Exception($"Parent User with ID {dto.UserId} not found.");

            if (parent.UserType != "Parent")
                throw new Exception($"{parent.UserName} is not configured as a Parent type.");

            child.ChildFirstName = dto.ChildFirstName;
            child.ChildLastName = dto.ChildLastName;
            child.ChildBirthDate = dto.ChildBirthDate;
            child.ChildGender = dto.ChildGender;
            child.ChildAddress = dto.ChildAddress;
            child.CenterId = dto.CenterId;

            // Direct Swap Assignment
            child.GuardianId = dto.UserId;
            child.ChildUpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Run metric growth triggers cleanly on the update link chain
            await EnsureGrowthRecordAsync(child.ChildId);

            return true;
        }

        public async Task<bool> LinkParentAsync(int childId, int parentUserId)
        {
            var child = await FindChildAsync(childId);
            if (child is null)
                throw new Exception($"Child with ID {childId} not found.");

            if (!child.ChildIsActive)
                throw new Exception($"Cannot link — child is deactivated.");

            var parent = await _db.Users.FindAsync(parentUserId);
            if (parent is null)
                throw new Exception($"User with ID {parentUserId} not found.");

            if (parent.UserType != "Parent")
                throw new Exception($"{parent.UserName} is not a Parent account.");

            if (!parent.UserIsActive)
                throw new Exception($"Cannot link — parent account {parent.UserName} is deactivated.");

            if (child.GuardianId.HasValue)
            {
                if (child.GuardianId == parentUserId)
                    throw new Exception($"{child.ChildFirstName} {child.ChildLastName} is already linked to {parent.UserName}.");

                var existingGuardian = await _db.Users.FindAsync(child.GuardianId.Value);
                throw new Exception($"{child.ChildFirstName} {child.ChildLastName} is already linked to {existingGuardian?.UserName ?? "another parent"}. Please unlink first.");
            }

            child.GuardianId = parentUserId;
            child.ChildUpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await EnsureGrowthRecordAsync(childId);

            return true;
        }

        public async Task<bool> UnlinkParentAsync(int childId)
        {
            var child = await FindChildAsync(childId);
            if (child is null)
                throw new Exception($"Child with ID {childId} not found.");

            if (!child.GuardianId.HasValue)
                throw new Exception($"{child.ChildFirstName} {child.ChildLastName} has no linked parent.");

            child.GuardianId = null;
            child.ChildUpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task EnsureGrowthRecordAsync(int childId)
        {
            var exists = await _db.Growths.AnyAsync(g => g.ChildId == childId);
            if (exists) return;

            _db.Growths.Add(new Growth
            {
                ChildId = childId,
                Reading = 0,
                Cognitive = 0,
                Motor = 0,
                Social = 0,
                Creative = 0,
                LifeSkills = 0,
                TotalPoints = 0,
                SpentPoints = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteChildAsync(int id)
        {
            var child = await FindChildAsync(id);
            if (child is null) return false;

            child.ChildIsActive = false;
            child.ChildUpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<Child?> FindChildAsync(int id) =>
            await _db.Children
                     .Include(c => c.Center)
                     .Include(c => c.Guardian)
                     .FirstOrDefaultAsync(c => c.ChildId == id && c.ChildIsActive == true);
    }
}