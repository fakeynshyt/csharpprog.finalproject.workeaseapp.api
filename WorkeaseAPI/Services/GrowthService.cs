using Microsoft.EntityFrameworkCore;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Services
{
    public class GrowthService : IGrowthService
    {
        private readonly AppDbContext _db;

        // ── Points per paid month ─────────────────────────────────
        private const int PointsPerPaidMonth = 75;

        public GrowthService(AppDbContext db) => _db = db;

        // ── Private mapper ────────────────────────────────────────
        private static GrowthDto MapToDto(Growth g) =>
            new GrowthDto
            {
                ChildId = g.ChildId,
                ChildName = g.Child != null
                    ? g.Child.ChildFirstName + " " + g.Child.ChildLastName
                    : string.Empty,
                Reading = g.Reading,
                Cognitive = g.Cognitive,
                Motor = g.Motor,
                Social = g.Social,
                Creative = g.Creative,
                LifeSkills = g.LifeSkills,
                TotalPoints = g.TotalPoints,
                SpentPoints = g.SpentPoints,
                UpdatedAt = g.UpdatedAt
            };

        // ✅ Get growth for all children of parent
        public async Task<IEnumerable<GrowthDto>> GetAllByParentUserIdAsync(int parentUserId)
        {
            var childIds = await _db.Children
                                    .Where(c => c.GuardianId == parentUserId
                                             && c.ChildIsActive == true)
                                    .Select(c => c.ChildId)
                                    .ToListAsync();

            var result = new List<GrowthDto>();

            foreach (var cid in childIds)
            {
                // Recalculate before returning
                await RecalculatePointsAsync(cid);

                var growth = await _db.Growths
                                      .Include(g => g.Child)
                                      .FirstOrDefaultAsync(g => g.ChildId == cid);

                if (growth is not null)
                    result.Add(MapToDto(growth));
            }

            return result;
        }

        // ── GET BY CHILD ID ───────────────────────────────────────
        public async Task<GrowthDto?> GetByChildIdAsync(int childId)
        {
            var growth = await _db.Growths
                                  .Include(g => g.Child)
                                  .FirstOrDefaultAsync(g => g.ChildId == childId);
            return growth is null ? null : MapToDto(growth);
        }

        // ── GET BY PARENT USER ID ─────────────────────────────────
        public async Task<GrowthDto?> GetByParentUserIdAsync(int parentUserId)
        {
            var child = await _db.Children
                                 .FirstOrDefaultAsync(c => c.GuardianId == parentUserId
                                                        && c.ChildIsActive == true);
            if (child is null) return null;

            // ✅ Recalculate points before returning
            await RecalculatePointsAsync(child.ChildId);

            var growth = await _db.Growths
                                  .Include(g => g.Child)
                                  .FirstOrDefaultAsync(g => g.ChildId == child.ChildId);

            return growth is null ? null : MapToDto(growth);
        }

        // ── ENSURE GROWTH RECORD EXISTS ───────────────────────────
        // Called when child is created or parent first views growth
        public async Task<GrowthDto> EnsureGrowthExistsAsync(int childId)
        {
            var existing = await _db.Growths
                                    .Include(g => g.Child)
                                    .FirstOrDefaultAsync(g => g.ChildId == childId);

            if (existing is not null)
                return MapToDto(existing);

            // Create new growth record for this child
            var growth = new Growth
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
            };

            _db.Growths.Add(growth);
            await _db.SaveChangesAsync();

            // Load with child info
            var created = await _db.Growths
                                   .Include(g => g.Child)
                                   .FirstOrDefaultAsync(g => g.ChildId == childId);

            System.Console.WriteLine(
                $"[Growth] Created for ChildId={childId}");

            return MapToDto(created!);
        }

        // ── UPDATE GROWTH ─────────────────────────────────────────
        // Parent allocates points to categories
        public async Task<GrowthDto> UpdateGrowthAsync(int childId, UpdateGrowthDto dto)
        {
            // Ensure growth record exists
            await EnsureGrowthExistsAsync(childId);

            // Recalculate latest points first
            await RecalculatePointsAsync(childId);

            var growth = await _db.Growths
                                  .Include(g => g.Child)
                                  .FirstOrDefaultAsync(g => g.ChildId == childId);

            if (growth is null)
                throw new Exception("Growth record not found.");

            // ✅ Calculate how many new points will be spent
            int newSpent =
                dto.Reading +
                dto.Cognitive +
                dto.Motor +
                dto.Social +
                dto.Creative +
                dto.LifeSkills;

            int availablePoints = growth.TotalPoints - growth.SpentPoints;

            // ✅ Calculate how many additional points are needed
            int currentSpent =
                growth.Reading +
                growth.Cognitive +
                growth.Motor +
                growth.Social +
                growth.Creative +
                growth.LifeSkills;

            int additionalPointsNeeded = newSpent - currentSpent;

            if (additionalPointsNeeded > availablePoints)
                throw new Exception(
                    $"Not enough points. " +
                    $"Available: {availablePoints}, " +
                    $"Additional needed: {additionalPointsNeeded}");

            // ✅ Each category can only increase — not decrease
            if (dto.Reading < growth.Reading ||
                dto.Cognitive < growth.Cognitive ||
                dto.Motor < growth.Motor ||
                dto.Social < growth.Social ||
                dto.Creative < growth.Creative ||
                dto.LifeSkills < growth.LifeSkills)
                throw new Exception(
                    "Growth values can only increase, not decrease.");

            // ✅ Apply updates
            growth.Reading = dto.Reading;
            growth.Cognitive = dto.Cognitive;
            growth.Motor = dto.Motor;
            growth.Social = dto.Social;
            growth.Creative = dto.Creative;
            growth.LifeSkills = dto.LifeSkills;
            growth.SpentPoints = newSpent;
            growth.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            System.Console.WriteLine(
                $"[Growth] Updated ChildId={childId} — " +
                $"Spent: {newSpent}/{growth.TotalPoints}");

            return MapToDto(growth);
        }

        // ── RECALCULATE POINTS ────────────────────────────────────
        // Called when a fee is paid
        // TotalPoints = count of paid fee months × 75
        public async Task RecalculatePointsAsync(int childId)
        {
            var growth = await _db.Growths
                                  .FirstOrDefaultAsync(g => g.ChildId == childId);

            if (growth is null)
            {
                // Auto create if not exists
                await EnsureGrowthExistsAsync(childId);
                growth = await _db.Growths
                                  .FirstOrDefaultAsync(g => g.ChildId == childId);
                if (growth is null) return;
            }

            // Count paid fee months
            var paidMonthsCount = await _db.FeeRecords
                                           .CountAsync(f => f.ChildId == childId
                                                         && f.FeeRecordIsPaid == true);

            // ✅ Total points = paid months × 75
            int totalPoints = paidMonthsCount * PointsPerPaidMonth;

            // ✅ Recalculate spent points from current allocations
            int spentPoints =
                growth.Reading +
                growth.Cognitive +
                growth.Motor +
                growth.Social +
                growth.Creative +
                growth.LifeSkills;

            growth.TotalPoints = totalPoints;
            growth.SpentPoints = spentPoints;
            growth.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            System.Console.WriteLine(
                $"[Growth] Points recalculated for ChildId={childId} — " +
                $"Paid months: {paidMonthsCount} × {PointsPerPaidMonth} = " +
                $"{totalPoints} total, {spentPoints} spent, " +
                $"{totalPoints - spentPoints} available");
        }
    }
}
