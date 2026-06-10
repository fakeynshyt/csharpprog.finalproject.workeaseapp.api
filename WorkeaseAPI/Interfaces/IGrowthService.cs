using WorkeaseAPI.DTOs;

namespace WorkeaseAPI.Interfaces
{
    public interface IGrowthService
    {
        Task<IEnumerable<GrowthDto>> GetAllByParentUserIdAsync(int parentUserId);
        Task<GrowthDto?> GetByChildIdAsync(int childId);
        Task<GrowthDto?> GetByParentUserIdAsync(int parentUserId);
        Task<GrowthDto> EnsureGrowthExistsAsync(int childId);
        Task<GrowthDto> UpdateGrowthAsync(int childId, UpdateGrowthDto dto);
        Task RecalculatePointsAsync(int childId);
    }
}
