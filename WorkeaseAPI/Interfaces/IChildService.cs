using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IChildService
    {
        Task<IEnumerable<ChildSummaryDto>> GetAllChildAsync();
        Task<IEnumerable<ChildSummaryDto>> GetChildrenByCenterAsync(int centerId);
        Task<int?> GetCenterIdByUserAsync(int userId);
        Task<ChildSummaryDto?> GetChildByIdAsync(int id);
        Task<IEnumerable<GuardianChildDto>> GetChildByGuardianUserIdAsync(int parentUserId);
        Task<Child> CreateChildWithGuardianAsync(CreateChildDto dto, int createdByUser);
        Task<bool> UpdateChildAsync(int id, UpdateChildDto dto);
        Task<bool> LinkParentAsync(int childId, int parentUserId);
        Task<bool> UnlinkParentAsync(int childId);            
        Task<bool> DeleteChildAsync(int id);
    }
}
