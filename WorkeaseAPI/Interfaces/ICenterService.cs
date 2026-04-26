using WorkeaseAPI.DTOs;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Interfaces
{
    public interface ICenterService
    {
        Task<IEnumerable<CenterDetailsDto>> GetAllCentersAsync();
        Task<CenterDetailsDto?> GetCenterByIdAsync(int centerId);
        Task<Center> CreateCenterAsync(Center center);
        Task<bool> UpdateCenterAsync(int centerId, Center updated);
        Task<bool> DeleteCenterAsync(int centerId);
    }
}
