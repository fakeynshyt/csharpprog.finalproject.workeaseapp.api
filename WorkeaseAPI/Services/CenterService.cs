using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class CenterService : ICenterService
    {
        private readonly AppDbContext _db;

        public CenterService(AppDbContext db) => _db = db;

        // ── GET ALL CENTERS ───────────────────────────────────────
        // Lists all centers with their CDW workers and children
        public async Task<IEnumerable<CenterDetailsDto>> GetAllCentersAsync()
        {
            var centers = await _db.Centers.ToListAsync();
            var result = new List<CenterDetailsDto>();

            foreach (var center in centers)
            {
                var details = await BuildCenterDetailsAsync(center);
                result.Add(details);
            }

            return result;
        }

        // ── GET SINGLE CENTER ─────────────────────────────────────
        // Returns one center with its CDW workers and children
        public async Task<CenterDetailsDto?> GetCenterByIdAsync(int centerId)
        {
            var center = await _db.Centers.FindAsync(centerId);
            if (center is null) return null;

            return await BuildCenterDetailsAsync(center);
        }

        // ── CREATE CENTER ─────────────────────────────────────────
        public async Task<Center> CreateCenterAsync(Center center)
        {
            _db.Centers.Add(center);
            await _db.SaveChangesAsync();
            return center;
        }

        // ── UPDATE CENTER ─────────────────────────────────────────
        public async Task<bool> UpdateCenterAsync(int centerId, Center updated)
        {
            var center = await _db.Centers.FindAsync(centerId);
            if (center is null) return false;

            center.CenterName = updated.CenterName;
            center.CenterLocation = updated.CenterLocation;

            await _db.SaveChangesAsync();
            return true;
        }

        // ── DELETE CENTER ─────────────────────────────────────────
        // Only deletes if no children or CDW workers are assigned
        public async Task<bool> DeleteCenterAsync(int centerId)
        {
            var center = await _db.Centers.FindAsync(centerId);
            if (center is null) return false;

            // Check if center still has active children
            var hasChildren = await _db.Children
                                       .AnyAsync(c => c.CenterId == centerId
                                                   && c.ChildIsActive == true);
            if (hasChildren)
                throw new Exception(
                    "Cannot delete center — it still has active children enrolled.");

            // Check if center still has CDW workers
            var hasCdwWorkers = await _db.Users
                                         .AnyAsync(u => u.CenterId == centerId
                                                     && u.UserIsActive == true
                                                     && u.UserType == "CDW");
            if (hasCdwWorkers)
                throw new Exception(
                    "Cannot delete center — it still has CDW workers assigned.");

            _db.Centers.Remove(center);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── BUILDER — shared by GetAll and GetById ────────────────
        private async Task<CenterDetailsDto> BuildCenterDetailsAsync(Center center)
        {
            // Get all active CDW workers assigned to this center
            var cdwWorkers = await _db.Users
                                      .Where(u => u.CenterId == center.CenterId
                                               && u.UserType == "CDW"
                                               && u.UserIsActive == true)
                                      .Select(u => new CdwUserDto
                                      {
                                          UserId = u.UserId,
                                          UserName = u.UserName,
                                          UserEmail = u.UserEmail
                                      })
                                      .ToListAsync();

            // Get all active children enrolled in this center
            var children = await _db.Children
                                    .Where(c => c.CenterId == center.CenterId
                                             && c.ChildIsActive == true)
                                    .Select(c => new ChildSummaryDto
                                    {
                                        ChildId = c.ChildId,
                                        ChildFullName = c.ChildFirstName + " " + c.ChildLastName,
                                        ChildBirthDate = c.ChildBirthDate,
                                        ChildGender = c.ChildGender,
                                        HasParent = c.GuardianId != null // true if linked to parent
                                    })
                                    .ToListAsync();

            return new CenterDetailsDto
            {
                CenterId = center.CenterId,
                CenterName = center.CenterName,
                CenterLocation = center.CenterLocation,
                CdwWorkers = cdwWorkers,
                Children = children,
                TotalChildren = children.Count,
                TotalCdwWorkers = cdwWorkers.Count
            };
        }
    }
}
