using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface IChildService
    {
        Task<IEnumerable<ChildReadDto>> GetAllChildReadAsync();
        Task<IEnumerable<ChildReadDto>> GetAllChildByCdwUserAsync(int cdwUserId);
        Task<Child?> GetChildByIdAsync(int childId);
        Task<GuardianChildDto> GetGuardianChildByIdAsync(int guardianUserId);
        Task<Child> CreateChildAsync(Child child);
        Task<bool> LinkParentAsync(int childId, int guardianUserId);
        Task<bool> UpdateChildAsync(int childId, Child updatedChild);
        Task<bool> DeleteChildAsync(int childId);
    }
}
