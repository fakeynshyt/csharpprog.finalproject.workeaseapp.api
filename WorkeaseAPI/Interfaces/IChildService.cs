using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IChildService
    {
        Task<IEnumerable<ChildReadDto>> GetAllChildAsync();
        Task<IEnumerable<ChildReadDto>> GetChildByCdwUserAsync(int cdwUserId);
        Task<Child?> GetChildByIdAsync(int id);
        Task<GuardianChildDto?> GetChildByGuardianUserIdAsync(int parentUserId);
        Task<Child> CreateChildWithGuardianAsync(CreateChildDto dto, int createdByUser);
        Task<bool> UpdateChildAsync(int id, UpdateChildDto dto);
        Task<bool> LinkParentAsync(int childId, int parentUserId);
        Task<bool> DeleteChildAsync(int id);
    }
}
