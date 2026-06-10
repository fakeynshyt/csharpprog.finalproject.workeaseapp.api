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

        public async Task<IEnumerable<Center>> GetAllCentersAsync() =>
        await _db.Centers
                 .OrderBy(c => c.CenterName)
                 .ToListAsync();

        public async Task<CenterDetailsDto?> GetCenterByIdAsync(int centerId)
        {
            var center = await _db.Centers.FindAsync(centerId);
            if (center is null) return null;

            return await BuildCenterDetailsAsync(center);
        }

        public async Task<Center> CreateCenterAsync(Center center)
        {
            _db.Centers.Add(center);
            await _db.SaveChangesAsync();
            return center;
        }

        public async Task<bool> UpdateCenterAsync(int centerId, Center updated)
        {
            var center = await _db.Centers.FindAsync(centerId);
            if (center is null) return false;

            center.CenterName = updated.CenterName;
            center.CenterLocation = updated.CenterLocation;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCenterAsync(int centerId)
        {
            var center = await _db.Centers.FindAsync(centerId);
            if (center is null) return false;

            var hasChildren = await _db.Children
                                       .AnyAsync(c => c.CenterId == centerId
                                                   && c.ChildIsActive == true);
            if (hasChildren)
                throw new Exception(
                    "Cannot delete center — it still has active children enrolled.");

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

        private async Task<CenterDetailsDto> BuildCenterDetailsAsync(Center center)
        {
            var cdwWorkers = await _db.Users
                                    .Where(u => u.CenterId == center.CenterId
                                             && u.UserType == "CDW"
                                             && u.UserIsActive == true)
                                    .Select(u => u.UserName)
                                    .ToListAsync();

            var children = await _db.Children
                                    .Where(c => c.CenterId == center.CenterId
                                             && c.ChildIsActive == true)
                                    .Select(c => c.ChildFirstName + " " + c.ChildLastName)
                                    .ToListAsync();

            return new CenterDetailsDto
            {
                CenterId = center.CenterId,
                CenterName = center.CenterName,
                CenterLocation = center.CenterLocation,
                CdwWorkers = cdwWorkers,
                Children = children
            };
        }
    }
}
